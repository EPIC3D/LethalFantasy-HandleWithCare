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
    [BepInPlugin("com.epicopy.lfpoke", "Handle With Care", "0.3.0")]
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
        private ConfigEntry<float> _reactionSeconds, _calmDelay, _calmPerSecond, _volume;
        private ConfigEntry<string> _thresholds;
        private ConfigEntry<bool> _chestEnabled, _headEnabled;
        private ConfigEntry<float> _chestAnnoyance, _jiggleForce;
        private ConfigEntry<float> _patCalm;
        private ConfigEntry<float> _affPatience, _fondAt, _affAkipo, _affRed, _affBlue;
        private ConfigEntry<bool> _feetEnabled, _freeEnabled;
        private ConfigEntry<float> _reach;
        private ConfigEntry<int> _pickupLevel;
        private ConfigEntry<float> _breastShare, _eyeSize, _mouthSize;
        private ConfigEntry<bool> _talkHidden;
        private ConfigEntry<bool> _talk, _dropEnabled, _quitEnabled;
        private ConfigEntry<int> _dropAfter, _quitAfter;
        private static int _drops;          // survives the lobby reload (runner is DontDestroyOnLoad)
        private bool _dropping;
        private ConfigEntry<float> _talkChance, _talkCooldown, _touchVolume;
        private ConfigEntry<string> _pokeSound, _patSound, _boobSound, _footSound, _shoeSound, _glassSound;
        private ConfigEntry<float> _quitDelay;
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
            _reactionSeconds = Config.Bind("Mood", "ReactionSeconds", 1.4f, "How long a reaction face is held after a poke.");
            _thresholds = Config.Bind("Mood", "LevelThresholds", "0,3,6,10,15", "Poke count where each of the 5 levels starts (giggle, flustered, smug, serious, angry).");
            _calmDelay = Config.Bind("Mood", "CalmDownDelay", 2.5f, "Seconds without pokes before she starts calming down.");
            _calmPerSecond = Config.Bind("Mood", "CalmDownPerSecond", 1.0f, "Pokes forgiven per second while calming down.");
            _volume = Config.Bind("Sound", "Volume", 0.8f, "Volume of the reaction sounds (sounds/level0..level4/*.ogg|wav).");
            _debug = Config.Bind("Debug", "LogClicks", false, "Log every click with distance to the face (for tuning).");
            _chestEnabled = Config.Bind("Chest", "Enabled", true, "Breast targets (reactions: flustered -> serious -> angry, plus jiggle).");
            _chestAnnoyance = Config.Bind("Chest", "AnnoyancePerPoke", 2f, "How much one breast poke annoys her (face poke = 1).");
            _jiggleForce = Config.Bind("Chest", "JiggleForce", 120f, "Physics push on a breast poke (game footsteps use 150-225). 0 = off.");
            _talk = Config.Bind("Talk", "Enabled", true, "Akipo answers with existing game lines (shown as dialogue; voiced if LFTTS is installed).");
            _talkHidden = Config.Bind("Talk", "WhenUIHidden", true, "Also talk when the game UI (dialogue box) is hidden. Off by default: the text can't be seen then, only its start sound is heard.");
            _talkChance = Config.Bind("Talk", "Chance", 0.35f, "Chance to say a line on a poke when her mood level did not change (a level change always talks).");
            _talkCooldown = Config.Bind("Talk", "CooldownSeconds", 3f, "Minimum time between two lines.");
            _pokeSound = Config.Bind("Sound", "FaceSound", "ItemNatureGrab", "Sound on a face poke: ItemNatureGrab (picking up regular food), empty = none, Boop (built-in), UIReject (anvil), a game SoundType name (e.g. ItemPaperGrab, UIAccept), or empty = none. Files in sounds/touch override it.");
            _patSound = Config.Bind("Sound", "HeadpatSound3", "PlayerStepGrass", "Sound on a headpat: PlayerStepGrass (soft grass rustle), Pat (built-in), a game SoundType name, or empty. Files in sounds/pat override it.");
            _boobSound = Config.Bind("Sound", "BreastSound", "Cauldron", "Sound on a breast poke: Cauldron (the cauldron 'bloop', captured the first time you see a cauldron in a level and saved to sounds/boob), Boop, a game SoundType name, or empty = use TouchSound3.");
            _footSound = Config.Bind("Sound", "FootSound", "ItemMeatGrap", "Sound on touching her legs/soles: ItemMeatGrap (picking up heavy food), a game SoundType name, Boop, or empty. Files in sounds/foot override it.");
            _shoeSound = Config.Bind("Sound", "ShoeSound", "PlayerStepWalkway", "Sound when you touch her shoe (the shoe blocks the tickle): PlayerStepWalkway (gravel path step), a game SoundType name, or empty.");
            _glassSound = Config.Bind("Sound", "GlassesSound", "ItemGlassGrab", "Sound when you poke her glasses (they protect her eyes): a game SoundType name or empty.");
            _quitDelay = Config.Bind("Drop", "QuitDelaySeconds", 2.5f, "Extra seconds after her last line before the game closes.");
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
            _breastShare = Config.Bind("Chest", "BreastBoneShare", 0.3f, "How much the breast physics bones must move a spot for it to count as the breast (0..1). Higher = smaller breast area.");
            MeshPick.BreastShare = _breastShare.Value;
            _eyeSize = Config.Bind("Face", "EyeSize", 0.3f, "Size of each eye zone, relative to the distance between her eyes.");
            _mouthSize = Config.Bind("Face", "MouthSize", 0.45f, "Size of the mouth zone, relative to the distance between her eyes.");
            MeshPick.EyeSize = _eyeSize.Value; MeshPick.MouthSize = _mouthSize.Value;
            _freeEnabled = Config.Bind("Free", "Enabled", true, "In the lobby, when nobody holds you: poke any character within arm's reach.");
            _reach = Config.Bind("Free", "Reach", 0.75f, "Arm's reach in world units (from your camera to the touched spot). LogClicks shows the measured distance.");
            _pickupLevel = Config.Bind("Free", "PickupAtLevel", 3, "Free roam: when your teasing gets her to this mood level (0 giggle .. 4 mad), she picks you up.");
            _feetEnabled = Config.Bind("Feet", "Enabled", true, "Akipo: click her soles to tickle her (e.g. in the footwear view).");
            _headEnabled = Config.Bind("Headpat", "Enabled", true, "Click the top of her head for a headpat: she gets happy and calms down.");
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
                    m.annoyance = Mathf.Max(0f, m.annoyance - _calmPerSecond.Value * (1f + _affPatience.Value * PatienceAffection(g.dialogue != null ? g.dialogue.character : CharacterType.None, m.affection)) * Time.deltaTime);
            }

            if (_debug.Value && Input.GetKeyDown(KeyCode.F9)) DumpAllClips();
            MarkFreeRoam();
            if (!_enabled.Value || _dropping || !Input.GetMouseButtonDown(0)) return;
            if (Active.Count == 0) { if (_debug.Value) LogDiagnostics(); return; }

            Camera cam = Camera.main;
            if (cam == null) return;
            Vector2 mouse = Cursor.lockState == CursorLockMode.Locked ? new Vector2(Screen.width / 2f, Screen.height / 2f) : (Vector2)Input.mousePosition;

            // Hitbox = the character's own 3D model (see MeshPick).
            Ray ray = cam.ScreenPointToRay(mouse);
            Giantess best = null; MeshPick.Hit bestHit = default; bestHit.distance = float.MaxValue;
            foreach (var g in Active.Keys)
                if (MeshPick.Pick(g, ray, out var h) && h.distance < bestHit.distance) { best = g; bestHit = h; }
            if (_debug.Value)
                Log.LogInfo(best == null ? $"[Hit] nothing at {mouse} ({MeshPick.LastDiag}, active={Active.Count})" : $"[Hit] {best.dialogue?.character} part={bestHit.part} mesh={bestHit.renderer} bones={bestHit.bone} | {MeshPick.LastDiag}");
            if (best == null || bestHit.part == BodyPart.None) return;
            Plugin_Active.Poses.TryGetValue(best, out Pose pose);
            if (pose == Pose.Free)
            {
                // Arm's reach: horizontal distance from the player's body, and within the player's
                // height (so the ground at her feet counts as reachable, not measured from the eyes).
                Vector3 feet = PlayerDriver.GetPlayerPosition(), eye = PlayerDriver.GetCameraPosition();
                Vector3 d = bestHit.point - feet;
                float horiz = new Vector2(d.x, d.z).magnitude;
                float low = Mathf.Min(feet.y, eye.y) - 0.3f, high = Mathf.Max(feet.y, eye.y) + _reach.Value;
                bool inReach = horiz <= _reach.Value && bestHit.point.y >= low && bestHit.point.y <= high;
                if (_debug.Value) Log.LogInfo($"[Hit] free roam: {bestHit.part} horizontal {horiz:F2} height {bestHit.point.y - feet.y:F2} (reach {_reach.Value:F2}) -> {(inReach ? "ok" : "out of reach")}");
                if (!inReach) return;
            }
            if (bestHit.part == BodyPart.Leg && pose != Pose.Free) return;
            bool freeRoam = pose == Pose.Free;
            if (!Allowed(pose, bestHit.part))
            {
                if (_debug.Value) Log.LogInfo($"[Hit] {bestHit.part} not available in pose {pose}");
                return;
            }
            if (bestHit.part == BodyPart.Foot && !_feetEnabled.Value) return;
            // Shoes/boots block the tickle (socks and stockings don't).
            if (bestHit.part == BodyPart.Foot && (WearsShoes(best) || IsFootwear(bestHit.renderer)))
            {
                if (_debug.Value) Log.LogInfo($"[Hit] foot covered ({ShoeName(best)}, mesh {bestHit.renderer}) -> no tickle");
                if (!(_checkUI.Value && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
                    PlayGameSound(_shoeSound.Value);
                return;
            }
            if (bestHit.part == BodyPart.Tummy && !_feetEnabled.Value) return;
            if (bestHit.part == BodyPart.Head && !_headEnabled.Value) return;
            if ((bestHit.part == BodyPart.BreastLeft || bestHit.part == BodyPart.BreastRight) && !_chestEnabled.Value) return;

            if (_checkUI.Value && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (_debug.Value) Log.LogInfo("[Click] on target but over UI -> ignored");
                return;
            }
            switch (bestHit.part)
            {
                case BodyPart.Head: HeadPat(best); break;
                case BodyPart.Foot: Tickle(best, foot: true); break;
                case BodyPart.Tummy: Tickle(best, foot: false); break;
                case BodyPart.Leg:
                    {
                        // Thigh or shin/ankle: from the dominant bone of the hit.
                        string topBone = (bestHit.bone ?? "").Split(' ')[0].ToLowerInvariant();
                        _legIsThigh = topBone.Contains("thigh") || topBone.Contains("upleg") || topBone.Contains("upperleg") || topBone.Contains("thight");
                        Poke(best, false, leg: true);
                    }
                    break;
                case BodyPart.ButtLeft:
                case BodyPart.ButtRight:
                    Poke(best, true, butt: true);
                    JiggleButt(best, cam, bestHit.part == BodyPart.ButtLeft);
                    break;
                case BodyPart.Face: Poke(best, false); break;
                case BodyPart.Eye: Poke(best, false, pokeEye: true); break;
                case BodyPart.Glasses: PlayGameSound(_glassSound.Value); if (_debug.Value) Log.LogInfo("[Hit] glasses protect her eyes"); break;
                case BodyPart.Mouth: Poke(best, false, pokeMouth: true); break;
                default:
                    Poke(best, true);
                    JiggleBreast(best, cam, bestHit.part == BodyPart.BreastLeft);
                    break;
            }
            // Free roam: tease her enough and she picks you up (the "E" pickup stays instant).
            if (freeRoam && !_dropping && _moods.TryGetValue(best, out var mood) && LevelOf(mood.annoyance) >= _pickupLevel.Value)
                StartCoroutine(PickupLater(best, 1.2f));
        }

        /// <summary>Same as pressing "E" at her: the game's own call/pickup interaction.</summary>
        private void RequestPickup(Giantess g)
        {
            var player = PlayerDriver.LocalPlayer;
            var call = g.GetComponentInChildren<CallGiantess>();
            if (player == null || call == null) { Log.LogInfo("[Pickup] no call interaction found"); return; }
            if (!call.CanMainInteract(player)) { Log.LogInfo("[Pickup] she cannot pick you up right now"); return; }
            call.OnMainInteract(player);
            Log.LogInfo($"[Pickup] {g.dialogue?.character} picks you up");
        }

        private bool _pickupPending;

        private IEnumerator PickupLater(Giantess g, float delay)
        {
            if (_pickupPending) yield break;
            _pickupPending = true;
            yield return new WaitForSeconds(delay);
            _pickupPending = false;
            if (g != null && !_dropping) RequestPickup(g);
        }

        private readonly List<Giantess> _lobbyGiantesses = new List<Giantess>();
        private float _nextScan;

        /// <summary>Lobby, nobody holds you: every character is touchable within arm's reach.</summary>
        private void MarkFreeRoam()
        {
            if (!_freeEnabled.Value) return;
            var setup = SetupManager.Instance;
            if (setup == null || !setup.isLobby) return;
            int frame = Time.frameCount;
            // Held / hugged / ... by someone right now? Then no free roam.
            foreach (var kv in Plugin_Active.PoseFrame) if (kv.Key != null && frame - kv.Value <= 3) return;
            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + 1f;
                _lobbyGiantesses.Clear();
                foreach (var ai in FindObjectsOfType<GiantessAI>()) if (ai != null && ai.giantess != null) _lobbyGiantesses.Add(ai.giantess);
            }
            foreach (var g in _lobbyGiantesses)
            {
                if (g == null) continue;
                Plugin_Active.Map[g] = frame;
                Plugin_Active.Poses[g] = Pose.Free;
            }
        }

        private void JiggleButt(Giantess g, Camera cam, bool herLeft)
        {
            var outfit = g.variation?.currentOutfitVariation;
            if (outfit == null || _jiggleForce.Value <= 0f) return;
            var cloths = herLeft ? outfit.ButtPhysics_L : outfit.ButtPhysics_R;
            if (cloths == null) return;
            foreach (var c in cloths)
            {
                if (c == null || !c.isActiveAndEnabled) continue;
                Vector3 dir = (BreastPos(c) - cam.transform.position).normalized;
                c.AddForce(dir, _jiggleForce.Value);
            }
        }

        /// <summary>The game's own footwear state: only bare feet and socks can be tickled.</summary>
        private static bool WearsShoes(Giantess g)
        {
            try
            {
                var shoe = g.variation?.currentShoeVariation;
                if (shoe == null) return false;
                return shoe.shoeType != ShoeType.Bare && shoe.shoeType != ShoeType.Socks;
            }
            catch { return false; }
        }

        private static string ShoeName(Giantess g)
        {
            try { return g.variation?.currentShoeVariation?.shoeType.ToString() ?? "?"; } catch { return "?"; }
        }

        /// <summary>
        /// Anything covering the foot blocks the tickle, except her skin (body/leg meshes) and
        /// socks / stockings / tights.
        /// </summary>
        private static bool IsFootwear(string mesh)
        {
            if (string.IsNullOrEmpty(mesh)) return false;
            string n = mesh.ToLowerInvariant();
            string[] open = { "leg", "feet", "foot", "body", "bare", "skin", "decal", "sock", "stocking", "tight", "pantyhose", "hose" };
            foreach (var k in open) if (n.Contains(k)) return false;
            return true;
        }

        /// <summary>What can be touched in each pose.</summary>
        private static bool Allowed(Pose pose, BodyPart part)
        {
            switch (pose)
            {
                case Pose.Free: return true;
                case Pose.Massage: return part == BodyPart.Foot;
                case Pose.Lap: return part == BodyPart.BreastLeft || part == BodyPart.BreastRight || part == BodyPart.Tummy;
                case Pose.Hug: return part == BodyPart.BreastLeft || part == BodyPart.BreastRight || part == BodyPart.Face || part == BodyPart.Eye || part == BodyPart.Mouth || part == BodyPart.Glasses || part == BodyPart.Head;
                default: return part != BodyPart.Tummy;   // Hold: face, headpat, breasts, soles
            }
        }

        /// <summary>Push the breast that was actually hit (her left/right) away from the player.</summary>
        private void JiggleBreast(Giantess g, Camera cam, bool herLeft)
        {
            var outfit = g.variation?.currentOutfitVariation;
            if (outfit == null || _jiggleForce.Value <= 0f) return;
            var cloth = herLeft ? outfit.Breast_L : outfit.Breast_R;
            if (cloth == null || !cloth.isActiveAndEnabled) return;
            Vector3 dir = (BreastPos(cloth) - cam.transform.position).normalized;
            cloth.AddForce(dir, _jiggleForce.Value);
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



        private bool _legIsThigh;

        private void Poke(Giantess g, bool chest, bool butt = false, bool leg = false, bool pokeEye = false, bool pokeMouth = false)
        {
            if (!_moods.TryGetValue(g, out var m)) _moods[g] = m = new Mood();
            var ch = g.dialogue != null ? g.dialogue.character : CharacterType.None;
            m.affection = AffectionOf(ch);
            var pers = Personality.For(ch, m.affection);
            m.lastPoke = Time.time;

            // Red, when fond of you: chest pokes please her instead of annoying her.
            if (chest && !butt && pers.likesChestWhenFond && m.affection >= _fondAt.Value)
            {
                ChestLiked(g, m);
                return;
            }

            float gain = (pokeEye ? pers.facePerPoke * 3f : pokeMouth ? pers.facePerPoke * 1.2f : butt ? pers.buttGain : chest ? pers.chestPerPoke * (_chestAnnoyance.Value / 2f) : pers.facePerPoke) / (1f + _affPatience.Value * PatienceAffection(g.dialogue != null ? g.dialogue.character : CharacterType.None, m.affection));
            m.annoyance = Mathf.Min(m.annoyance + gain, _levelAt[_levelAt.Length - 1] + 3f);
            int lvl = LevelOf(m.annoyance);
            if (butt) lvl = pers.buttLevels[lvl];
            else if (chest) lvl = pers.chestLevels[lvl];
            else lvl = pers.faceLevels[lvl];
            if (pokeEye) lvl = Mathf.Max(lvl, 2);                       // a poke in the eye is never funny
            var (eye, mouth) = Reactions[lvl];
            if (pokeEye && lvl < 4) (eye, mouth) = ("hurt eyes", "hurt mouth");
            if (pokeMouth && lvl < 3) (eye, mouth) = pers.buttFlirty ? ("smug eyes", "smug mouth") : ("embarrased eyes", "embarrased mouth");
            if (butt && lvl == 1) (eye, mouth) = ("shock eyes", "shy mouth");
            if (butt && lvl == 2 && pers.buttFlirty) (eye, mouth) = ("smug eyes", "smug mouth");
            if (chest && !butt && lvl == 1 && pers.chestFace1.HasValue) (eye, mouth) = pers.chestFace1.Value;
            if (chest && !butt && lvl == 2 && pers.chestFace2.HasValue) (eye, mouth) = pers.chestFace2.Value;

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
            else if (leg)
                PlayTouch(FootKey, _footSound.Value);
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
            Lines.Line[] set = ls == null ? null
                : (leg && lvl < 3 && (_legIsThigh ? ls.Thigh : ls.Shin) != null) ? (_legIsThigh ? ls.Thigh : ls.Shin)
                : (pokeEye && lvl < 4 && ls.Eye != null) ? ls.Eye
                : (pokeMouth && lvl < 3 && ls.Mouth != null) ? ls.Mouth
                : (butt && lvl < 4 && ls.Butt != null && (!pers.buttFlirty || lvl == 2)) ? ls.Butt
                : (chest && !butt) ? (ls.Chest.TryGetValue(lvl, out var cs) ? cs : null) : ls.Face[lvl];
            Say(g, set, changed);
            Log.LogInfo($"[Poke] {g.dialogue?.character} {(pokeEye ? "eye" : pokeMouth ? "mouth" : butt ? "butt" : chest ? "chest" : "face")} affection={m.affection:F2} annoyance={m.annoyance:F1} level={lvl} ({eye}/{mouth})");
        }

        /// <summary>Affection as it affects patience (Blue: nothing until it is maxed out).</summary>
        private static float PatienceAffection(CharacterType ch, float affection)
        {
            var p = Personality.For(ch);
            return p.affectionOnlyAtMax && affection < 0.999f ? 0f : affection;
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

        private void Tickle(Giantess g, bool foot)
        {
            if (!_moods.TryGetValue(g, out var m)) _moods[g] = m = new Mood();
            var ch = g.dialogue != null ? g.dialogue.character : CharacterType.None;
            m.affection = AffectionOf(ch);
            m.lastPoke = Time.time;
            var pers = Personality.For(ch, m.affection);
            m.annoyance = Mathf.Min(m.annoyance + pers.tickleGain / (1f + _affPatience.Value * PatienceAffection(g.dialogue != null ? g.dialogue.character : CharacterType.None, m.affection)), _levelAt[_levelAt.Length - 1] + 3f);
            int lvl = LevelOf(m.annoyance);
            bool annoyed = lvl >= pers.tickleAnnoyedAt;
            var em = g.expressionManager;
            int rl = annoyed ? pers.faceLevels[lvl] : lvl;   // e.g. Blue: never the giggle face
            var (eye, mouth) = annoyed ? Reactions[rl] : ("laugh eyes", "laugh mouth");
            em.eyeOverride.SetEvent(eye);
            em.mouthOverride.SetEvent(mouth);
            m.showing = true;
            m.clearAt = Time.time + _reactionSeconds.Value;
            if (foot) PlayTouch(FootKey, _footSound.Value);
            else if (HasClips(TickleKey)) PlayTouch(TickleKey, "");
            var ls = Lines.For(ch);
            // Annoyed by tickling: her face lines; Blue (never funny) has her own grumpy tickle lines.
            if (ls != null)
            {
                var set = (annoyed && !(pers.tickleAnnoyedAt == 0 && ls.Tickle != null && lvl < 4)) ? ls.Face[rl] : (ls.Tickle ?? ls.Face[0]);
                // Soles: foot jokes mixed in (until she is really mad).
                if (foot && ls.Feet != null && lvl < 4) set = set.Concat(ls.Feet).ToArray();
                Say(g, set, true);
            }
            Log.LogInfo($"[Tickle] {ch} annoyance={m.annoyance:F1} level={lvl}");
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
        private const int TouchKey = 101, PatKey = 102, BoobKey = 103, TickleKey = 104, FootKey = 105;

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
                var line = PickLine(g, set);
                dm.AddDialgoue(new DialogueExpressionAndText(g.dialogue.character, line.expression, line.key));
                Log.LogInfo($"[Talk] {line.key}");
            }
            Log.LogInfo($"[Drop] #{_drops}{(quit ? " -> quitting the game" : " -> back to the lobby")}");
            if (quit)
            {
                // Her angry line first, then a short pause before the game closes.
                yield return new WaitForSeconds(3.5f + _quitDelay.Value);
            }
            else yield return new WaitForSeconds(2.5f);
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

        /// <summary>
        /// False when the level-select UI was hidden with its eye button (the dialogue box is hidden
        /// then, only the text's start sound would be heard).
        /// </summary>
        private static bool DialogueVisible(DialogueManager dm)
        {
            try
            {
                var ls = LevelSelect.Instance;
                if (ls == null || ls.canvas == null || ls.hideCanvas == null) return true;
                bool hiddenByEye = !ls.canvas.enabled && ls.hideCanvas.enabled;
                return !hiddenByEye;
            }
            catch { return true; }
        }

        private readonly Dictionary<Giantess, float> _lastTouch = new Dictionary<Giantess, float>();

        // Last line key said per character, so the same line is never repeated back to back.
        private readonly Dictionary<CharacterType, string> _lastLine = new Dictionary<CharacterType, string>();

        private Lines.Line PickLine(Giantess g, Lines.Line[] set)
        {
            var ch = g.dialogue != null ? g.dialogue.character : CharacterType.None;
            _lastLine.TryGetValue(ch, out var last);
            var pool = set.Where(l => l.key != last).ToArray();
            if (pool.Length == 0) pool = set;
            var line = pool[UnityEngine.Random.Range(0, pool.Length)];
            _lastLine[ch] = line.key;
            return line;
        }

        /// <summary>Queue one existing Akipo line (game dialogue box; LFTTS voices it).</summary>
        private void Say(Giantess g, Lines.Line[] set, bool force)
        {
            if (!_talk.Value || set == null || set.Length == 0) return;
            if (g.dialogue == null || Lines.For(g.dialogue.character) == null) return;
            var dm = DialogueManager.Instance;
            // First touch after a quiet moment always gets an answer, otherwise it feels like nothing happened.
            bool first = !_lastTouch.TryGetValue(g, out float lastT) || Time.time - lastT > 8f;
            _lastTouch[g] = Time.time;
            if (dm == null || dm.IsInProgress) return;
            if (!_talkHidden.Value && !DialogueVisible(dm))
            {
                if (_debug.Value) Log.LogInfo("[Talk] skipped: dialogue box hidden");
                return;
            }
            if (!first && Time.time < _nextTalk) return;
            if (!first && !force && UnityEngine.Random.value > _talkChance.Value) return;
            var line = PickLine(g, set);
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
            var folders = Enumerable.Range(0, Reactions.Length).Select(i => (i, "level" + i)).Append((HeadpatKey, "headpat")).Append((TouchKey, "touch")).Append((PatKey, "pat")).Append((BoobKey, "boob")).Append((TickleKey, "tickle")).Append((FootKey, "foot"));
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

    /// <summary>Which pose she is in: decides what can be touched.</summary>
    internal enum Pose { Free, Hold, Hug, Lap, Massage }

    internal static class Plugin_Active
    {
        internal static readonly Dictionary<Giantess, Pose> Poses = new Dictionary<Giantess, Pose>();
        internal static readonly Dictionary<Giantess, int> PoseFrame = new Dictionary<Giantess, int>();
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

        private static void Mark(Giantess g, Pose pose)
        {
            if (g == null) return;
            Plugin_Active.Poses[g] = pose;
            Plugin_Active.PoseFrame[g] = Time.frameCount;
            Mark(g);
        }

        private static void MarkIfLevelSelect(object behaviour, GiantessAI ai, Pose pose)
        {
            var st = Traverse.Create(behaviour).Field("state").GetValue();
            if (st != null && st.ToString() == "LevelSelect") Mark(ai?.giantess, pose);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GiantessBehaviourEventHugAkipo), nameof(GiantessBehaviourEventHugAkipo.BehaviourUpdate))]
        private static void HugPose(GiantessBehaviourEventHugAkipo __instance, GiantessAI giantessAI) => MarkIfLevelSelect(__instance, giantessAI, Pose.Hug);

        [HarmonyPostfix, HarmonyPatch(typeof(GiantessBehaviourEventLapAkipo), nameof(GiantessBehaviourEventLapAkipo.BehaviourUpdate))]
        private static void LapPose(GiantessBehaviourEventLapAkipo __instance, GiantessAI giantessAI) => MarkIfLevelSelect(__instance, giantessAI, Pose.Lap);

        [HarmonyPostfix, HarmonyPatch(typeof(GiantessBehaviourEventFootMassageAkipo), nameof(GiantessBehaviourEventFootMassageAkipo.BehaviourUpdate))]
        private static void MassagePose(GiantessBehaviourEventFootMassageAkipo __instance, GiantessAI giantessAI) => MarkIfLevelSelect(__instance, giantessAI, Pose.Massage);

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
            if (__instance.state == GiantessBehaviourGrabPlayer.BehaviourState.LevelSelect) Mark(giantessAI.giantess, Pose.Hold);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GiantessBehaviourGrabPlayerBackground), nameof(GiantessBehaviourGrabPlayerBackground.BehaviourUpdate))]
        private static void TwinsHold(GiantessBehaviourGrabPlayerBackground __instance, GiantessAI giantessAI)
        {
            if (__instance.state == GiantessBehaviourGrabPlayerBackground.BehaviourState.LevelSelect) Mark(giantessAI.giantess, Pose.Hold);
        }
    }
}
