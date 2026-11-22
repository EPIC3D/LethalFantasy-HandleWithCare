using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LFPoke
{
    /// <summary>
    /// Saves a clip that cannot be read with GetData (compressed in memory) by playing it once
    /// through a muted AudioSource and capturing the decoded samples in OnAudioFilterRead.
    /// </summary>
    internal class ClipRecorder : MonoBehaviour
    {
        private readonly List<float> _buffer = new List<float>();
        private readonly object _lock = new object();
        private bool _recording;
        private int _channels = 2;

        internal static IEnumerator Record(AudioClip clip, string path, Action<bool> done)
        {
            var go = new GameObject("LFPokeRecorder");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f; src.volume = 1f; src.pitch = 1f;
            src.bypassListenerEffects = true; src.bypassReverbZones = true;
            var rec = go.AddComponent<ClipRecorder>();   // filter after the source: gets its output
            src.clip = clip;
            lock (rec._lock) { rec._buffer.Clear(); rec._recording = true; }
            src.Play();
            float end = Time.realtimeSinceStartup + clip.length + 0.3f;
            while (Time.realtimeSinceStartup < end && (src.isPlaying || Time.realtimeSinceStartup < end - 0.25f)) yield return null;
            float[] data;
            int ch;
            lock (rec._lock) { rec._recording = false; data = rec._buffer.ToArray(); ch = rec._channels; }
            Destroy(go);
            bool ok = data.Length > 0 && Trim(ref data, ch) && WriteWav(path, data, ch, AudioSettings.outputSampleRate);
            done?.Invoke(ok);
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            lock (_lock)
            {
                if (_recording) { _channels = channels; _buffer.AddRange(data); }
            }
            Array.Clear(data, 0, data.Length);   // muted: the player never hears the recording
        }

        // Cut leading/trailing silence.
        private static bool Trim(ref float[] d, int ch)
        {
            int a = 0, b = d.Length - 1;
            while (a < d.Length && Mathf.Abs(d[a]) < 0.0005f) a++;
            while (b > a && Mathf.Abs(d[b]) < 0.0005f) b--;
            if (b <= a) return false;
            a -= a % ch; b += ch - 1 - (b % ch);
            b = Mathf.Min(b, d.Length - 1);
            var t = new float[b - a + 1];
            Array.Copy(d, a, t, 0, t.Length);
            d = t;
            return true;
        }

        private static bool WriteWav(string path, float[] data, int ch, int rate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var w = new BinaryWriter(new FileStream(path, FileMode.Create)))
            {
                int bytes = data.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' }); w.Write(16);
                w.Write((short)1); w.Write((short)ch); w.Write(rate); w.Write(rate * ch * 2);
                w.Write((short)(ch * 2)); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (float f in data) w.Write((short)(Mathf.Clamp(f, -1f, 1f) * short.MaxValue));
            }
            return true;
        }
    }
}
