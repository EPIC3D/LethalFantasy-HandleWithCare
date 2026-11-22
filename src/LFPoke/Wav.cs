using System;
using System.IO;
using UnityEngine;

namespace LFPoke
{
    /// <summary>Writes an AudioClip to a 16-bit PCM WAV file.</summary>
    internal static class Wav
    {
        internal static bool Save(AudioClip clip, string path)
        {
            if (clip == null || clip.samples <= 0) return false;
            if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0)) return false;   // fails for clips that stay compressed in memory

            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = data.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)clip.channels);
                w.Write(clip.frequency);
                w.Write(clip.frequency * clip.channels * 2);
                w.Write((short)(clip.channels * 2));
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                foreach (float f in data) w.Write((short)(Mathf.Clamp(f, -1f, 1f) * short.MaxValue));
            }
            return true;
        }
    }
}
