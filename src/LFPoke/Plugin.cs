using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace LFPoke
{
    /// <summary>
    /// Lobby fun: while a giantess holds the player in the lobby (level select) and follows the mouse,
    /// clicking her face makes her react, getting more annoyed with every poke (Warcraft 2 style).
    /// </summary>
    [BepInPlugin("com.epicopy.lfpoke", "Handle With Care", "0.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static ConfigFile Cfg;
        internal static string Dir;

        private void Awake()
        {
            Log = Logger;
            Cfg = Config;
            Dir = Path.GetDirectoryName(Info.Location);
            // The game kills the BepInEx manager object, so run on our own persistent object.
            var go = new GameObject("LFPoke");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<PokeRunner>();
            new Harmony("com.epicopy.lfpoke").PatchAll(typeof(Patches));
            Log.LogInfo("Handle With Care loaded.");
        }
    }

    public class PokeRunner : MonoBehaviour
    {
        private static ManualLogSource Log => Plugin.Log;
        internal static PokeRunner Instance;


        private ConfigEntry<bool> _enabled, _checkUI, _debug;
        private ConfigEntry<float> _hitRadius, _reactionSeconds, _calmDelay, _calmPerSecond, _volume;
        private ConfigEntry<string> _thresholds;
        private ConfigEntry<bool> _chestEnabled, _headEnabled;
        private ConfigEntry<float> _breastRadius, _breastLX, _breastLY, _breastRX, _breastRY, _chestAnnoyance, _jiggleForce;
        private ConfigEntry<float> _headX, _headY, _headW, _headH, _patCalm;
        private ConfigEntry<float> _affPatience, _fondAt, _affAkipo, _affRed, _affBlue;
        private ConfigEntry<float> _cleavTopX, _cleavTopY, _cleavBotX, _cleavBotY;
        private ConfigEntry<float> _topLX, _topLY, _topRX, _topRY;
        private ConfigEntry<bool> _talk, _dropEnabled, _quitEnabled;
        private ConfigEntry<int> _dropAfter, _quitAfter;
        private static int _drops;          // survives the lobby reload (runner is DontDestroyOnLoad)
        private bool _dropping;
        private ConfigEntry<float> _talkChance, _talkCooldown, _touchVolume;
        private ConfigEntry<string> _pokeSound, _patSound, _boobSound;
        private float _nextTalk;
        private readonly Dictionary<Giantess, int> _lastLevel = new Dictionary<Giantess, int>();

        private enum Zone { Face, Head, BreastLeft, BreastRight }   // Left/Right = screen side

        private class Mood
        {
            public float annoyance;
            public float lastPoke = -999f;
            public float clearAt;
            public bool showing;
            public int madPokes;
            public float affection;
        }

        private readonly Dictionary<Giantess, Mood> _moods = new Dictionary<Giantess, Mood>();

        // Reaction per annoyance level: eye + mouth shape names (the dialogue-portrait set).
        private static readonly (string eye, string mouth)[] Reactions =
        {
            ("laugh eyes",  "laugh mouth"),   // 0: giggle, "that tickles"
            ("shy eyes",    "shy mouth"),     // 1: flustered
            ("smug eyes",   "smug mouth"),    // 2: "hey..."
            ("frown eyes",  "frown mouth"),   // 3: "stop it"
            ("mad eyes",    "mad mouth"),     // 4: angry, darkened eyes (portrait "Angry")
        };


        private int[] _levelAt = { 0, 3, 6, 10, 15 };
        private readonly Dictionary<int, List<AudioClip>> _clips = new Dictionary<int, List<AudioClip>>();
        private AudioSource _audio;

        internal static Dictionary<Giantess, int> Active => Plugin_Active.Map;

        private void Awake()
        {
            Instance = this;
            var Config = Plugin.Cfg;
            _enabled = Config.Bind("General", "Enabled", true, "Click the giantess' face in the lobby to tease her.");
            _checkUI = Config.Bind("General", "IgnoreClicksOnUI", true, "Ignore clicks that land on a UI element (buttons, level list).");
            _hitRadius = Config.Bind("General", "FaceRadius", 1.0f, "Size multiplier of the clickable face area.");
            _reactionSeconds = Config.Bind("Mood", "ReactionSeconds", 1.4f, "How long a reaction face is held after a poke.");
            _thresholds = Config.Bind("Mood", "LevelThresholds", "0,3,6,10,15", "Poke count where each of the 5 levels starts (giggle, flustered, smug, serious, angry).");
            _calmDelay = Config.Bind("Mood", "CalmDownDelay", 2.5f, "Seconds without pokes before she starts calming down.");
            _calmPerSecond = Config.Bind("Mood", "CalmDownPerSecond", 1.0f, "Pokes forgiven per second while calming down.");
            _volume = Config.Bind("Sound", "Volume", 0.8f, "Volume of the reaction sounds (sounds/level0..level4/*.ogg|wav).");
            _debug = Config.Bind("Debug", "LogClicks", false, "Log every click with distance to the face (for tuning).");
            _chestEnabled = Config.Bind("Chest", "Enabled", true, "Breast targets (reactions: flustered -> serious -> angry, plus jiggle).");
            _breastRadius = Config.Bind("Chest", "BreastRadius", 0.14f, "Clickable radius of each breast, fraction of screen height.");
            _breastLX = Config.Bind("Chest", "ScreenLeftBreastX", -0.006f, "Screen-left breast centre relative to the chest bone (fraction of screen height, + = right).");
            _breastLY = Config.Bind("Chest", "ScreenLeftBreastY", 0.026f, "(+ = up)");
            _breastRX = Config.Bind("Chest", "ScreenRightBreastX", 0.175f, "Screen-right breast centre relative to the chest bone (+ = right).");
            _breastRY = Config.Bind("Chest", "ScreenRightBreastY", 0.02f, "(+ = up)");
            _chestAnnoyance = Config.Bind("Chest", "AnnoyancePerPoke", 2f, "How much one breast poke annoys her (face poke = 1).");
            _jiggleForce = Config.Bind("Chest", "JiggleForce", 120f, "Physics push on a breast poke (game footsteps use 150-225). 0 = off.");
            _cleavTopX = Config.Bind("Chest", "CleavageTopX", 0.078f, "Cleavage line top point relative to the chest bone (fraction of screen height, + = right). Clicks left of the line = screen-left breast.");
            _cleavTopY = Config.Bind("Chest", "CleavageTopY", 0.117f, "(+ = up)");
            _cleavBotX = Config.Bind("Chest", "CleavageBottomX", 0.227f, "Cleavage line bottom point relative to the chest bone (+ = right).");
            _cleavBotY = Config.Bind("Chest", "CleavageBottomY", -0.078f, "(+ = up)");
            _topLX = Config.Bind("Chest", "TopLineLeftX", -0.197f, "Upper limit of the breast zones: left point relative to the chest bone (fraction of screen height). No breast hits above this line.");
            _topLY = Config.Bind("Chest", "TopLineLeftY", 0.099f, "(+ = up)");
            _topRX = Config.Bind("Chest", "TopLineRightX", 0.30f, "Upper limit: right point relative to the chest bone.");
            _topRY = Config.Bind("Chest", "TopLineRightY", 0.141f, "(+ = up)");
            _talk = Config.Bind("Talk", "Enabled", true, "Akipo answers with existing game lines (shown as dialogue; voiced if LFTTS is installed).");
            _talkChance = Config.Bind("Talk", "Chance", 0.35f, "Chance to say a line on a poke when her mood level did not change (a level change always talks).");
            _talkCooldown = Config.Bind("Talk", "CooldownSeconds", 3f, "Minimum time between two lines.");
            _pokeSound = Config.Bind("Sound", "TouchSound4", "", "Sound on a face poke: empty = none, Boop (built-in), UIReject (anvil), a game SoundType name (e.g. ItemPaperGrab, UIAccept), or empty = none. Files in sounds/touch override it.");
            _patSound = Config.Bind("Sound", "HeadpatSound3", "PlayerStepGrass", "Sound on a headpat: PlayerStepGrass (soft grass rustle), Pat (built-in), a game SoundType name, or empty. Files in sounds/pat override it.");
            _boobSound = Config.Bind("Sound", "BreastSound", "Cauldron", "Sound on a breast poke: Cauldron (the cauldron 'bloop', captured the first time you see a cauldron in a level and saved to sounds/boob), Boop, a game SoundType name, or empty = use TouchSound3.");
            _touchVolume = Config.Bind("Sound", "TouchVolume", 0.6f, "Volume of the touch sound.");
            _dropEnabled = Config.Bind("Drop", "Enabled", true, "When she is mad and you keep poking, she puts you down (reloads the lobby, like the pause menu's Lobby button).");
            _dropAfter = Config.Bind("Drop", "MadPokesBeforeDrop", 3, "Pokes while she is mad before she puts you down.");
            _quitEnabled = Config.Bind("Drop", "QuitGame", true, "After enough drops she closes the game.");
            _quitAfter = Config.Bind("Drop", "DropsBeforeQuit", 2, "Drop number that closes the game instead (counted per game session).");
            _affPatience = Config.Bind("Personality", "AffectionPatience", 1f, "How much the game's affection makes her more patient: annoyance per poke is divided by (1 + this x affection), calming is multiplied by it. 0 = ignore affection.");
            _fondAt = Config.Bind("Personality", "FondAffection", 0.8f, "Affection (0..1) from which Red enjoys chest pokes instead of getting annoyed.");
            _affAkipo = Config.Bind("Personality", "AffectionOverrideAkipo", -1f, "Test override for Akipo's affection (0..1). -1 = use the game's real value.");
            _affRed = Config.Bind("Personality", "AffectionOverrideRed", -1f, "Test override for Red's affection (0..1). -1 = use the game's real value.");
            _affBlue = Config.Bind("Personality", "AffectionOverrideBlue", -1f, "Test override for Blue's affection (0..1). -1 = use the game's real value.");
            _headEnabled = Config.Bind("Headpat", "Enabled", true, "Click the top of her head for a headpat: she gets happy and calms down.");
            _headX = Config.Bind("Headpat", "OffsetX", -0.01f, "Oval centre relative to the face centre (fraction of screen height, + = right).");
            _headY = Config.Bind("Headpat", "OffsetY", 0.253f, "(+ = up)");
            _headW = Config.Bind("Headpat", "HalfWidth", 0.19f, "Oval half width, fraction of screen height.");
            _headH = Config.Bind("Headpat", "HalfHeight", 0.08f, "Oval half height, fraction of screen height.");
            _patCalm = Config.Bind("Headpat", "CalmPerPat", 3f, "How much annoyance one headpat removes.");
            ParseThresholds();

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            StartCoroutine(LoadSounds());

        }

        private void ParseThresholds()
        {
            try
            {
                var v = _thresholds.Value.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
                if (v.Length == Reactions.Length) _levelAt = v;
            }
            catch { Log.LogWarning("Bad LevelThresholds, using defaults."); }
        }

        private int LevelOf(float annoyance)
        {
            int lvl = 0;
            for (int i = 0; i < _levelAt.Length; i++)
                if (annoyance >= _levelAt[i]) lvl = i;
            return lvl;
        }

        private void Update()
        {
            int frame = Time.frameCount;
            foreach (var g in Active.Where(kv => kv.Key == null || frame - kv.Value > 3).Select(kv => kv.Key).ToList())
                Active.Remove(g);

            float now = Time.time;
            foreach (var kv in _moods.ToList())
            {
                var g = kv.Key; var m = kv.Value;
                if (g == null) { _moods.Remove(g); continue; }
                if (m.showing && (now >= m.clearAt || !Active.ContainsKey(g))) ClearFace(g, m);
                if (now - m.lastPoke > _calmDelay.Value)
                    m.annoyance = Mathf.Max(0f, m.annoyance - _calmPerSecond.Value * (1f + _affPatience.Value * m.affection) * Time.deltaTime);
            }

            if (_debug.Value && Input.GetKeyDown(KeyCode.F9)) DumpAllClips();
            if (!_enabled.Value || _dropping || !Input.GetMouseButtonDown(0)) return;
            if (Active.Count == 0) { if (_debug.Value) LogDiagnostics(); return; }

            Camera cam = Camera.main;
            if (cam == null) return;
            Vector2 mouse = Input.mousePosition;

            Giantess best = null; Zone bestZone = Zone.Face; float bestScore = float.MaxValue;
            float h = Screen.height;
            void Try(Giantess g, Zone z, Vector2 c, float rx, float ry)
            {
                float dx = (mouse.x - c.x) / rx, dy = (mouse.y - c.y) / ry;
                float score = Mathf.Sqrt(dx * dx + dy * dy);
                if (_debug.Value) Log.LogInfo($"[Click] {g.dialogue?.character} {z} score={score:F2} mouse={mouse} centre={c}");
                if (score < bestScore) { bestScore = score; best = g; bestZone = z; }
            }
            foreach (var g in Active.Keys)
            {
                if (FaceOnScreen(g, cam, out Vector2 fc, out float fr))
                {
                    Try(g, Zone.Face, fc, fr, fr);
                    if (_headEnabled.Value)
                        Try(g, Zone.Head, fc + new Vector2(_headX.Value, _headY.Value) * h, _headW.Value * h, _headH.Value * h);
                }
                if (_chestEnabled.Value && ChestBoneOnScreen(g, cam, out Vector2 cb))
                {
                    float br = _breastRadius.Value * h;
                    // Nothing above the upper limit line (collarbone area).
                    Vector2 la = cb + new Vector2(_topLX.Value, _topLY.Value) * h, lb = cb + new Vector2(_topRX.Value, _topRY.Value) * h;
                    float lt = Mathf.Approximately(la.x, lb.x) ? 0.5f : (mouse.x - la.x) / (lb.x - la.x);
                    float limitY = Mathf.LerpUnclamped(la.y, lb.y, lt);
                    if (mouse.y > limitY) { if (_debug.Value) Log.LogInfo($"[Click] above breast limit (y={mouse.y:F0} > {limitY:F0})"); continue; }
                    Try(g, Zone.BreastLeft, cb + new Vector2(_breastLX.Value, _breastLY.Value) * h, br, br);
                    Try(g, Zone.BreastRight, cb + new Vector2(_breastRX.Value, _breastRY.Value) * h, br, br);
                }
            }
            if (best == null || bestScore > 1f) return;

            if (_checkUI.Value && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (_debug.Value) Log.LogInfo("[Click] on target but over UI -> ignored");
                return;
            }
            if (bestZone == Zone.Head) { HeadPat(best); return; }
            bool chest = bestZone != Zone.Face;
            Poke(best, chest);
            if (chest)
            {
                // Which breast: side of the slanted cleavage line (the front breast curves under to the right).
                bool left = bestZone == Zone.BreastLeft;
                if (ChestBoneOnScreen(best, cam, out Vector2 bone))
                {
                    Vector2 a = bone + new Vector2(_cleavTopX.Value, _cleavTopY.Value) * h;
                    Vector2 b = bone + new Vector2(_cleavBotX.Value, _cleavBotY.Value) * h;
                    float t = Mathf.Approximately(a.y, b.y) ? 0.5f : (mouse.y - a.y) / (b.y - a.y);
                    float lineX = Mathf.LerpUnclamped(a.x, b.x, t);
                    left = mouse.x < lineX;
                    if (_debug.Value) Log.LogInfo($"[Side] mouse={mouse} lineX={lineX:F0} -> {(left ? "screen-left" : "screen-right")}");
                }
                Jiggle(best, cam, left);
            }
        }

        private void LogDiagnostics()
        {
            var tab = LevelSelectTabGiantess.Instance;
            foreach (var ai in FindObjectsOfType<GiantessAI>())
            {
                var beh = ai.currentBehaviour;
                object st = beh?.GetType().GetField("state")?.GetValue(beh);
                var ch = ai.giantess?.dialogue?.character ?? CharacterType.None;
                bool sel = tab != null && tab.IsRecentCharacterSelected(ch);
                Log.LogInfo($"[Diag] {ch}: behaviour={beh?.GetType().Name ?? "none"} state={st} tabSelected={sel} active={(ai.giantess != null && Active.ContainsKey(ai.giantess))}");
            }
            if (tab == null) Log.LogInfo("[Diag] LevelSelectTabGiantess.Instance == null");
        }

        /// <summary>Screen-space circle covering the face: between head bone and mouth, a bit above the mouth.</summary>
        private bool FaceOnScreen(Giantess g, Camera cam, out Vector2 center, out float radius)
        {
            center = default; radius = 0;
            if (g.headTransform == null) return false;
            Vector3 head = g.headTransform.position;
            Vector3 mouth = g.mouthTransform != null ? g.mouthTransform.position : head;
            float size = Mathf.Max((mouth - head).magnitude, 0.0001f);
            Vector3 face = mouth + Vector3.up * size * 0.6f;   // around the nose / eyes
            float worldR = size * 1.3f * _hitRadius.Value;

            Vector3 sc = cam.WorldToScreenPoint(face);
            if (sc.z <= 0) return false;
            Vector3 edge = cam.WorldToScreenPoint(face + cam.transform.right * worldR);
            center = sc;
            center.y -= Screen.height * 0.07f;   // measured: projected point sits between the brows, face centre is lower
            radius = Mathf.Max(Vector2.Distance(sc, edge), Screen.height * 0.2f * _hitRadius.Value);
            if (_debug.Value && Input.GetMouseButtonDown(0))
            {
                Vector3 hs = cam.WorldToScreenPoint(head), ms = cam.WorldToScreenPoint(mouth);
                Log.LogInfo($"[Geo] mouse={(Vector2)Input.mousePosition} face={center} head={(Vector2)hs} mouth={(Vector2)ms} headMouth={size:F3}m scale={g.scale:F2} screen={Screen.width}x{Screen.height}");
            }
            return true;
        }

        /// <summary>Screen position of the (upper) chest bone; the breast zones are placed relative to it.</summary>
        private bool ChestBoneOnScreen(Giantess g, Camera cam, out Vector2 pos)
        {
            pos = default;
            var anim = g.animator;
            if (anim == null || !anim.isHuman) return false;
            Transform t = anim.GetBoneTransform(HumanBodyBones.UpperChest);
            if (t == null) t = anim.GetBoneTransform(HumanBodyBones.Chest);
            if (t == null) return false;
            Vector3 sc = cam.WorldToScreenPoint(t.position);
            if (sc.z <= 0) return false;
            pos = sc;
            return true;
        }

        /// <summary>Push the clicked breast (by screen side) away from the player (camera).</summary>
        private void Jiggle(Giantess g, Camera cam, bool screenLeft)
        {
            var outfit = g.variation?.currentOutfitVariation;
            if (outfit == null || _jiggleForce.Value <= 0f) return;
            var r = outfit.Breast_R; var l = outfit.Breast_L;
            bool okR = r != null && r.isActiveAndEnabled, okL = l != null && l.isActiveAndEnabled;
            if (!okR && !okL) return;
            Vector3 pr = BreastPos(r), pl = BreastPos(l);

            MagicaCloth2.MagicaCloth target;
            if (okR && okL)
            {
                bool rIsLeft = cam.WorldToScreenPoint(pr).x < cam.WorldToScreenPoint(pl).x;
                target = screenLeft == rIsLeft ? r : l;
            }
            else target = okR ? r : l;
            if (_debug.Value) Log.LogInfo($"[Jiggle] {(screenLeft ? "screen-left" : "screen-right")} -> {(target == r ? "Breast_R" : "Breast_L")}");

            Vector3 dir = ((target == r ? pr : pl) - cam.transform.position).normalized;
            target.AddForce(dir, _jiggleForce.Value);
        }

        /// <summary>World position of the breast: the cloth's first root bone (the component itself sits at the outfit root).</summary>
        private static Vector3 BreastPos(MagicaCloth2.MagicaCloth c)
        {
            if (c == null) return Vector3.zero;
            var bones = c.SerializeData?.rootBones;
            if (bones != null)
                foreach (var b in bones)
                    if (b != null) return b.position;
            return c.transform.position;
        }


        private void Poke(Giantess g, bool chest)
        {
            if (!_moods.TryGetValue(g, out var m)) _moods[g] = m = new Mood();
            var ch = g.dialogue != null ? g.dialogue.character : CharacterType.None;
            var pers = Personality.For(ch);
            m.affection = AffectionOf(ch);
            m.lastPoke = Time.time;

            // Red, when fond of you: chest pokes please her instead of annoying her.
            if (chest && pers.likesChestWhenFond && m.affection >= _fondAt.Value)
            {
                ChestLiked(g, m);
                return;
            }

            float gain = (chest ? pers.chestPerPoke * (_chestAnnoyance.Value / 2f) : pers.facePerPoke) / (1f + _affPatience.Value * m.affection);
            m.annoyance = Mathf.Min(m.annoyance + gain, _levelAt[_levelAt.Length - 1] + 3f);
            int lvl = LevelOf(m.annoyance);
            if (chest) lvl = pers.chestLevels[lvl];
            var (eye, mouth) = Reactions[lvl];
            if (chest && lvl == 1 && pers.chestFace1.HasValue) (eye, mouth) = pers.chestFace1.Value;
            if (chest && lvl == 2 && pers.chestFace2.HasValue) (eye, mouth) = pers.chestFace2.Value;

            var em = g.expressionManager;
            em.eyeOverride.SetEvent(eye);
            em.mouthOverride.SetEvent(mouth);
            m.showing = true;
            m.clearAt = Time.time + _reactionSeconds.Value * (1f + lvl * 0.25f);

            PlaySound(lvl);
            if (chest && !HasClips(BoobKey) && !_cauldronLookedUp) { _cauldronLookedUp = true; FindCauldronClip(); }
            if (chest && (HasClips(BoobKey) || !string.Equals(_boobSound.Value?.Trim(), "Cauldron", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(_boobSound.Value)))
            {
                PlayTouch(BoobKey, _boobSound.Value);
            }
            else
                PlayTouch(TouchKey, _pokeSound.Value);
            m.madPokes = lvl >= 4 ? m.madPokes + 1 : 0;
            if (_dropEnabled.Value && !_dropping && m.madPokes >= _dropAfter.Value)
            {
                m.madPokes = 0;
                StartCoroutine(DropPlayer(g));
                Log.LogInfo($"[Poke] {g.dialogue?.character} -> drop");
                return;
            }
            bool changed = !_lastLevel.TryGetValue(g, out int prev) || prev != lvl;
            _lastLevel[g] = lvl;
            var ls = Lines.For(g.dialogue != null ? g.dialogue.character : CharacterType.None);
            Lines.Line[] set = ls == null ? null : chest ? (ls.Chest.TryGetValue(lvl, out var cs) ? cs : null) : ls.Face[lvl];
            Say(g, set, changed);
            Log.LogInfo($"[Poke] {g.dialogue?.character} {(chest ? "chest" : "face")} affection={m.affection:F2} annoyance={m.annoyance:F1} level={lvl} ({eye}/{mouth})");
        }

        private float AffectionOf(CharacterType ch)
        {
            float o = ch == CharacterType.Akipo ? _affAkipo.Value : ch == CharacterType.RedTwin ? _affRed.Value : ch == CharacterType.BlueTwin ? _affBlue.Value : -1f;
            return o >= 0f ? Mathf.Clamp01(o) : Personality.Affection(ch);
        }

        private void ChestLiked(Giantess g, Mood m)
        {
            m.annoyance = Mathf.Max(0f, m.annoyance - 1f);
            m.madPokes = 0;
            var em = g.expressionManager;
            bool glow = UnityEngine.Random.value < 0.5f;
            em.eyeOverride.SetEvent(glow ? "happy eyes" : "smug eyes");
            em.mouthOverride.SetEvent(glow ? "smile mouth" : "smug mouth");
            m.showing = true;
            m.clearAt = Time.time + _reactionSeconds.Value * 1.5f;
            if (HasClips(BoobKey)) PlayTouch(BoobKey, _boobSound.Value);
            _lastLevel[g] = -2;
            var ls = Lines.For(g.dialogue.character);
            if (ls?.ChestLiked != null) Say(g, ls.ChestLiked, false);
            Log.LogInfo($"[Poke] {g.dialogue?.character} chest (liked) affection={m.affection:F2} annoyance={m.annoyance:F1}");
        }

        private void HeadPat(Giantess g)
        {
            if (!_moods.TryGetValue(g, out var m)) _moods[g] = m = new Mood();
            var pp = Personality.For(g.dialogue != null ? g.dialogue.character : CharacterType.None);
            m.annoyance = Mathf.Max(0f, m.annoyance - pp.patCalm * (_patCalm.Value / 3f));
            int lvl = LevelOf(m.annoyance);
            // Still quite annoyed -> grudging blush; otherwise happy.
            var (eye, mouth) = lvl >= 3 ? ("shy eyes", "shy mouth") : ("happy eyes", "happy mouth");
            var em = g.expressionManager;
            em.eyeOverride.SetEvent(eye);
            em.mouthOverride.SetEvent(mouth);
            m.showing = true;
            m.clearAt = Time.time + _reactionSeconds.Value * 1.5f;
            PlayFolder(HeadpatKey);
            PlayTouch(PatKey, _patSound.Value);
            m.madPokes = 0;
            _lastLevel[g] = -1;
            var lsp = Lines.For(g.dialogue != null ? g.dialogue.character : CharacterType.None);
            if (lsp != null) Say(g, lvl >= 3 ? lsp.HeadpatGrudging : lsp.HeadpatHappy, false);
            Log.LogInfo($"[Pat] {g.dialogue?.character} annoyance={m.annoyance:F1} ({eye}/{mouth})");
        }

        private static void ClearFace(Giantess g, Mood m)
        {
            m.showing = false;
            g.expressionManager.eyeOverride.ClearEvent();
            g.expressionManager.mouthOverride.ClearEvent();
        }

        private const int HeadpatKey = 100;

        private AudioSource _touchAudio, _sfxAudio;
        private const int TouchKey = 101, PatKey = 102, BoobKey = 103;

        /// <summary>F9: save every loaded AudioClip as WAV (to pick sounds by ear).</summary>
        private void DumpAllClips()
        {
            string dir = Path.Combine(Plugin.Dir, "dump");
            Directory.CreateDirectory(dir);
            int ok = 0, fail = 0;
            foreach (var clip in Resources.FindObjectsOfTypeAll<AudioClip>())
            {
                if (clip == null || clip.length > 20f) continue;   // skip music / long ambience
                string name = string.Concat((clip.name.Length > 0 ? clip.name : "unnamed").Split(Path.GetInvalidFileNameChars()));
                string file = Path.Combine(dir, name + ".wav");
                if (File.Exists(file)) continue;
                try { if (Wav.Save(clip, file)) ok++; else fail++; } catch { fail++; }
            }
            Log.LogInfo($"[Dump] saved {ok} clip(s) to {dir} ({fail} could not be read)");
        }

        /// <summary>Find the cauldron "bloop" even when the cauldrons are inactive: by clip name, then via inactive cauldrons.</summary>
        private bool _cauldronLookedUp, _recordingClip;

        private void FindCauldronClip()
        {
            var clip = Resources.FindObjectsOfTypeAll<AudioClip>().FirstOrDefault(c => c != null && c.name == "cauldron_drop");
            if (clip == null)
                foreach (var c in Resources.FindObjectsOfTypeAll<CauldronCrafter>())
                    if (c != null && c.dropAudioRandomizer != null && c.dropAudioRandomizer.soundClip != null) { clip = c.dropAudioRandomizer.soundClip; break; }
            Log.LogInfo($"[Cauldron] lookup -> {(clip != null ? clip.name : "not found")}");
            if (clip == null) return;
            if (!_clips.TryGetValue(BoobKey, out var list)) _clips[BoobKey] = list = new List<AudioClip>();
            if (!list.Contains(clip)) list.Add(clip);
            string file = Path.Combine(Plugin.Dir, "sounds", "boob", "cauldron_drop.wav");
            if (!File.Exists(file) && !_recordingClip)
            {
                _recordingClip = true;
                StartCoroutine(ClipRecorder.Record(clip, file, ok => { _recordingClip = false; Log.LogInfo(ok ? $"[Cauldron] saved {file}" : "[Cauldron] could not record"); }));
            }
        }

        private bool HasClips(int key) => _clips.TryGetValue(key, out var l) && l.Count > 0;

        /// <summary>Called when a cauldron loads in a level: grab its drop "bloop" clips for breast pokes.</summary>
        internal static void CaptureCauldron(CauldronCrafter c)
        {
            var me = Instance; if (me == null || c == null || c.dropAudioRandomizer == null) return;
            var r = c.dropAudioRandomizer;
            var found = new List<AudioClip>();
            if (r.soundClip != null) found.Add(r.soundClip);
            if (r.soundType != SoundType.None && AudioManager.Instance != null)
            {
                try { var arr = AudioManager.Instance.GetRandomAudioClips(r.soundType); if (arr != null) found.AddRange(arr.Where(a => a != null)); } catch { }
            }
            found = found.Distinct().ToList();
            Log.LogInfo($"[Cauldron] soundType={r.soundType} clip={(r.soundClip != null ? r.soundClip.name : "-")} captured={found.Count}");
            if (found.Count == 0) return;
            if (!me._clips.TryGetValue(BoobKey, out var list)) me._clips[BoobKey] = list = new List<AudioClip>();
            string dir = Path.Combine(Plugin.Dir, "sounds", "boob");
            foreach (var clip in found)
            {
                if (!list.Contains(clip)) list.Add(clip);
                try
                {
                    Directory.CreateDirectory(dir);
                    string file = Path.Combine(dir, string.Concat(clip.name.Split(Path.GetInvalidFileNameChars())) + ".wav");
                    if (!File.Exists(file) && !me._recordingClip)
                    {
                        me._recordingClip = true;
                        me.StartCoroutine(ClipRecorder.Record(clip, file, ok => { me._recordingClip = false; Log.LogInfo(ok ? $"[Cauldron] saved {file}" : $"[Cauldron] could not record {clip.name}"); }));
                    }
                }
                catch (Exception e) { Log.LogWarning($"[Cauldron] could not save {clip.name}: {e.Message}"); }
            }
        }
        private AudioClip _boop, _pat;

        private void PlayTouch(int folderKey, string setting)
        {
            AudioClip clip = null;
            if (_clips.TryGetValue(folderKey, out var files) && files.Count > 0) clip = files[UnityEngine.Random.Range(0, files.Count)];
            else if (string.Equals(setting?.Trim(), "Boop", StringComparison.OrdinalIgnoreCase)) clip = _boop ??= MakeTone("LFPokeBoop", 0.09f, 880f, 520f, 38f);
            else if (string.Equals(setting?.Trim(), "Pat", StringComparison.OrdinalIgnoreCase)) clip = _pat ??= MakeTone("LFPokePat", 0.12f, 300f, 180f, 30f);
            if (clip == null) { PlayGameSound(setting); return; }
            if (_touchAudio == null) { _touchAudio = gameObject.AddComponent<AudioSource>(); _touchAudio.playOnAwake = false; _touchAudio.spatialBlend = 0f; }
            _touchAudio.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            _touchAudio.PlayOneShot(clip, _touchVolume.Value);
        }

        /// <summary>Short soft tone with a falling pitch and fast decay (a cartoon "boop").</summary>
        private static AudioClip MakeTone(string name, float seconds, float f0, float f1, float decay)
        {
            const int rate = 44100;
            int n = (int)(rate * seconds);
            var data = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += 2 * Math.PI * f / rate;
                float env = Mathf.Min(1f, i / (rate * 0.004f)) * Mathf.Exp(-decay * t * seconds * 10f);
                data[i] = (float)Math.Sin(phase) * env * 0.5f + (float)Math.Sin(phase * 2) * env * 0.12f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>She has had enough: an angry line, then back to the lobby (or the game closes).</summary>
        private IEnumerator DropPlayer(Giantess g)
        {
            _dropping = true;
            _drops++;
            bool quit = _quitEnabled.Value && _drops >= _quitAfter.Value;
            var em = g.expressionManager;
            em.eyeOverride.SetEvent("mad eyes");
            em.mouthOverride.SetEvent("mad mouth");
            var dm = DialogueManager.Instance;
            var lsd = Lines.For(g.dialogue != null ? g.dialogue.character : CharacterType.None);
            if (dm != null && lsd != null)
            {
                dm.InterruptCurrentDialogue();
                var set = quit ? lsd.Quit : lsd.Drop;
                var line = set[UnityEngine.Random.Range(0, set.Length)];
                dm.AddDialgoue(new DialogueExpressionAndText(g.dialogue.character, line.expression, line.key));
                Log.LogInfo($"[Talk] {line.key}");
            }
            Log.LogInfo($"[Drop] #{_drops}{(quit ? " -> quitting the game" : " -> back to the lobby")}");
            yield return new WaitForSeconds(quit ? 3.5f : 2.5f);
            _dropping = false;
            if (quit) { Application.Quit(); yield break; }
            _moods.Clear();
            _lastLevel.Clear();
            if (SceneLoader.Instance != null) SceneLoader.Instance.LoadScene(0);
        }

        private void PlayGameSound(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || AudioManager.Instance == null) return;
            if (!Enum.TryParse(name.Trim(), true, out SoundType type)) { Log.LogWarning($"Unknown SoundType '{name}'"); return; }
            AudioClip clip;
            try { clip = AudioManager.Instance.GetRandomAudioClip(type); } catch { return; }
            if (clip == null) return;
            if (_sfxAudio == null) { _sfxAudio = gameObject.AddComponent<AudioSource>(); _sfxAudio.playOnAwake = false; _sfxAudio.spatialBlend = 0f; }
            // Game SFX are mastered loud; the game itself plays e.g. UIReject at 0.1 volume, 0.9 pitch.
            _sfxAudio.pitch = UnityEngine.Random.Range(0.85f, 0.95f);
            _sfxAudio.PlayOneShot(clip, _touchVolume.Value * 0.2f);
        }

        /// <summary>Queue one existing Akipo line (game dialogue box; LFTTS voices it).</summary>
        private void Say(Giantess g, Lines.Line[] set, bool force)
        {
            if (!_talk.Value || set == null || set.Length == 0) return;
            if (g.dialogue == null || Lines.For(g.dialogue.character) == null) return;
            var dm = DialogueManager.Instance;
            if (dm == null || dm.IsInProgress || Time.time < _nextTalk) return;
            if (!force && UnityEngine.Random.value > _talkChance.Value) return;
            var line = set[UnityEngine.Random.Range(0, set.Length)];
            dm.AddDialgoue(new DialogueExpressionAndText(g.dialogue.character, line.expression, line.key));
            _nextTalk = Time.time + _talkCooldown.Value;
            Log.LogInfo($"[Talk] {line.key}");
        }

        private void PlayFolder(int key)
        {
            if (_clips.TryGetValue(key, out var list) && list.Count > 0)
            {
                _audio.Stop();
                _audio.clip = list[UnityEngine.Random.Range(0, list.Count)];
                _audio.volume = _volume.Value;
                _audio.Play();
            }
        }

        private void PlaySound(int lvl)
        {
            // Use the closest lower level that has sounds.
            for (int l = lvl; l >= 0; l--)
            {
                if (_clips.TryGetValue(l, out var list) && list.Count > 0)
                {
                    _audio.Stop();
                    _audio.clip = list[UnityEngine.Random.Range(0, list.Count)];
                    _audio.volume = _volume.Value;
                    _audio.Play();
                    return;
                }
            }
        }

        private IEnumerator LoadSounds()
        {
            string root = Path.Combine(Plugin.Dir, "sounds");
            if (!Directory.Exists(root)) { Log.LogInfo($"No sounds folder ({root}), poking is silent."); yield break; }
            var folders = Enumerable.Range(0, Reactions.Length).Select(i => (i, "level" + i)).Append((HeadpatKey, "headpat")).Append((TouchKey, "touch")).Append((PatKey, "pat")).Append((BoobKey, "boob"));
            foreach (var (lvl, name) in folders)
            {
                string dir = Path.Combine(root, name);
                if (!Directory.Exists(dir)) continue;
                var list = new List<AudioClip>();
                foreach (var file in Directory.GetFiles(dir))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    AudioType type = ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".wav" ? AudioType.WAV : AudioType.UNKNOWN;
                    if (type == AudioType.UNKNOWN) continue;
                    using (var req = UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri, type))
                    {
                        yield return req.SendWebRequest();
                        if (req.result == UnityWebRequest.Result.Success) list.Add(DownloadHandlerAudioClip.GetContent(req));
                        else Log.LogWarning($"Sound load failed: {file}: {req.error}");
                    }
                }
                if (_clips.TryGetValue(lvl, out var existing)) existing.AddRange(list); else _clips[lvl] = list;   // keep clips captured meanwhile (cauldron)
                Log.LogInfo($"{name}: {list.Count} sound(s)");
            }
        }
    }

    internal static class Plugin_Active
    {
        // Giantesses currently holding the player and following the mouse -> last frame seen.
        internal static readonly Dictionary<Giantess, int> Map = new Dictionary<Giantess, int>();
    }

    internal static class Patches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(CauldronCrafter), "Awake")]
        private static void CauldronAwake(CauldronCrafter __instance)
        {
            try { PokeRunner.CaptureCauldron(__instance); } catch (Exception e) { Plugin.Log.LogWarning("[Cauldron] " + e.Message); }
        }

        private static void Mark(Giantess g)
        {
            if (g == null) return;
            var tab = LevelSelectTabGiantess.Instance;
            if (tab != null && !tab.IsRecentCharacterSelected(g.dialogue.character)) return;
            Plugin_Active.Map[g] = Time.frameCount;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GiantessBehaviourGrabPlayer), nameof(GiantessBehaviourGrabPlayer.BehaviourUpdate))]
        private static void AkipoHold(GiantessBehaviourGrabPlayer __instance, GiantessAI giantessAI)
        {
            if (__instance.state == GiantessBehaviourGrabPlayer.BehaviourState.LevelSelect) Mark(giantessAI.giantess);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GiantessBehaviourGrabPlayerBackground), nameof(GiantessBehaviourGrabPlayerBackground.BehaviourUpdate))]
        private static void TwinsHold(GiantessBehaviourGrabPlayerBackground __instance, GiantessAI giantessAI)
        {
            if (__instance.state == GiantessBehaviourGrabPlayerBackground.BehaviourState.LevelSelect) Mark(giantessAI.giantess);
        }
    }
}
