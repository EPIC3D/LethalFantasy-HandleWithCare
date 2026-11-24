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
    [BepInPlugin("com.epicopy.lfpoke", "Handle With Care", "0.4.0")]
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
        internal static bool LobbyClimbing = true;

        /// <summary>
        /// Is the game's "Foot Climber" minor ability chosen (level select)? Lobby climbing, growth and destruction
        /// only work with it – so they are off (and out of the way) when it isn't chosen.
        /// </summary>
        internal static bool FootClimberPerk()
        {
            try
            {
                var data = SceneLoader.InstanceData;
                if (data == null) return false;
                return data.GetLoadedMinorBlessing is LevelBlessingFootGrab || data.minorBlessing == typeof(LevelBlessingFootGrab);
            }
            catch { return false; }
        }

        /// <summary>Lobby climbing right now: the setting is on and the Foot Climber perk is chosen.</summary>
        internal static bool ClimbActive => LobbyClimbing && (FootClimberPerk() || ShrunkNow);

        /// <summary>Shrunk as punishment: climbing her is open for a while, Foot Climber perk or not.</summary>
        internal static bool ShrunkNow;


        private ConfigEntry<bool> _enabled, _checkUI, _debug;
        private ConfigEntry<float> _reactionSeconds, _calmDelay, _calmPerSecond, _volume;
        private ConfigEntry<string> _thresholds;
        private ConfigEntry<bool> _chestEnabled, _headEnabled;
        private ConfigEntry<float> _chestAnnoyance, _jiggleForce;
        private ConfigEntry<float> _patCalm;
        private ConfigEntry<float> _affPatience, _fondAt, _affAkipo, _affRed, _affBlue;
        private ConfigEntry<bool> _feetEnabled, _freeEnabled, _lobbyClimb;
        private ConfigEntry<KeyCode> _pickupKey;
        private ConfigEntry<float> _holdSeconds, _growMaxMeters, _growStep, _shrinkDelay;
        private ConfigEntry<int> _patsPerGrowth, _pokesPerCrawl;
        private ConfigEntry<bool> _crawlEnabled;
        private ConfigEntry<float> _crawlSeconds;
        private ConfigEntry<bool> _stompEnabled;
        private ConfigEntry<int> _stompAtLevel;
        private ConfigEntry<bool> _growEnabled;
        private ConfigEntry<float> _reach;
        private ConfigEntry<int> _pickupLevel;
        private ConfigEntry<float> _breastShare, _eyeSize, _mouthSize;
        private ConfigEntry<bool> _talkHidden;
        private ConfigEntry<bool> _talk, _dropEnabled, _quitEnabled;
        private ConfigEntry<int> _dropAfter, _quitAfter;
        private static int _drops;          // survives the lobby reload (runner is DontDestroyOnLoad)
        private ConfigEntry<string> _dropMode;
        private ConfigEntry<float> _shrinkSeconds, _shrinkAmount, _launchUp;
        private ConfigEntry<string> _slapSound;
        private bool PunishMode => !string.Equals((_dropMode?.Value ?? "").Trim(), "Classic", StringComparison.OrdinalIgnoreCase);
        // Punishment waiting for the reloaded lobby: who put you down, and whether you get shrunk.
        private CharacterType _punishBy = CharacterType.None;
        private bool _punishShrink, _punishing, _shrunk;
        private float _punishAfter;
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
            _calmDelay = Config.Bind("Mood", "CalmDownDelay", 6f, "Seconds without pokes before she starts calming down.");
            _calmPerSecond = Config.Bind("Mood", "CalmDownPerSecond", 0.3f, "Pokes forgiven per second while calming down.");
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
            _dropMode = Config.Bind("Drop", "Mode", "Punish", "What happens when she has had enough: Punish = she puts you down (lobby), stomps angrily and the danger effect flashes; from the second time on she also shrinks you for a while. Classic = put down, and the game closes the second time.");
            _shrinkSeconds = Config.Bind("Drop", "ShrinkSeconds", 60f, "Punish mode: how long you stay shrunk.");
            _slapSound = Config.Bind("Sound", "SlapSound", "Slap", "Sound when she sends you flying: Slap (a built-in cartoon slap), a game SoundType name, or empty. Files in sounds/slap override it.");
            _launchUp = Config.Bind("Drop", "LaunchUp", 1.5f, "Punish mode: punished again while still tiny = she sends you flying (the game's knock-back), this much upward (1 = the game's normal). 0 = off.");
            _shrinkAmount = Config.Bind("Drop", "ShrinkAmount", 0.75f, "Punish mode: how much she shrinks you (0.75 = to a quarter of your size). Uses the game's temporary shrink effect, so it stacks with shrink perks/enchants.");
            _quitEnabled = Config.Bind("Drop", "QuitGame", true, "Classic mode: after enough drops she closes the game.");
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
            _lobbyClimb = Config.Bind("Free", "LobbyClimbing", true, "Lobby, with the game's Foot Climber perk chosen: tap \"E\" on her foot to climb her (hold it to be picked up). Off = no climbing in the lobby.");
            LobbyClimbing = _lobbyClimb.Value;
            _holdSeconds = Config.Bind("Free", "HoldToPickupSeconds", 1f, "Lobby, aiming at her foot: tap \"E\" to climb, hold it this long to be picked up.");
            _growEnabled = Config.Bind("Growth", "Enabled", true, "Akipo, Foot Climber perk only: climb her leg in the lobby and poke her thigh to fill her growth bar; when it is full she grows a stage with the game's own growth animation.");
            _patsPerGrowth = Config.Bind("Growth", "ThighPokesPerGrowth", 18, "Pokes on her thigh while climbing her (lobby) needed for one growth stage.");
            _growStep = Config.Bind("Growth", "StageMultiplier", 1.3f, "Lobby: size multiplier of one growth stage (1.3 = +30%).");
            _destroyWhenGrown = Config.Bind("Growth", "DestroyWhenGrown", true, "While she is grown (Foot Climber perk only), her body has the game's normal strength: houses and other destructibles she touches break (the lobby resets them on reload). Off = the lobby's soft body, nothing breaks.").Value;
            _shrinkDelay = Config.Bind("Growth", "ShrinkDelaySeconds", 3f, "After you let go of her (jump off / release) while she is grown, she shrinks back to her lobby size after this many seconds.");
            _growMaxMeters = Config.Bind("Growth", "MaxMeters", 0f, "She doesn't grow beyond this height (meters). 0 = follow the game's own max giantess size setting (level select).");
            _crawlEnabled = Config.Bind("Crouch", "Enabled", true, "Akipo, lobby free roam: poke her feet and she crouches down (the game's crouch animation), stays a while, then stands up again.");
            _pokesPerCrawl = Config.Bind("Crouch", "FootPokesPerCrouch", 5, "Foot pokes (free roam, lobby) that make her crouch down.");
            _crawlSeconds = Config.Bind("Crouch", "StaySeconds", 10f, "How long she stays crouched before standing up.");
            _stompEnabled = Config.Bind("Stomp", "Enabled", true, "Akipo, lobby free roam: tickle her soles too much and she stomps her foot (the game's tease stomp).");
            _stompAtLevel = Config.Bind("Stomp", "AtLevel", 3, "Mood level (0 giggle .. 4 mad) from which a sole tickle makes her stomp.");
            _pickupKey = Config.Bind("Free", "PickupKey", KeyCode.None, "Lobby: key that asks the nearest character to pick you up (\"E\" climbs her foot when climbing is on).");
            _pickupLevel = Config.Bind("Free", "PickupAtLevel", -1, "Free roam: when your teasing gets her to this mood level (0 giggle .. 4 mad), she picks you up. -1 = never (default: she punishes you on the spot instead).");
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
            if (_debug.Value && Input.GetKeyDown(KeyCode.F10)) LogDestructibles();
            MarkFreeRoam();
            if (ClimbActive) HandleFootKey();
            if (_punishBy != CharacterType.None && !_punishing && Time.realtimeSinceStartup > _punishAfter) TryStartPunish();
            if (BaseScale.Count > 0) WatchRelease();
            if (LobbyClimbing && _pickupKey.Value != KeyCode.None && Input.GetKeyDown(_pickupKey.Value)) PickupNearest();
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
                if (_debug.Value) LogClimbState(best);
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
            // Shoes block the tickle; open footwear (heels, slippers, geta/pokkuri) leaves the top of the foot bare:
            // a hit on her skin there tickles, a hit on the footwear itself doesn't.
            // ...but never from below: the sole side stays covered by the footwear.
            if (bestHit.part == BodyPart.Foot && ((WearsShoes(best) && (!OpenTopped(best) || bestHit.normal.y < -0.2f)) || IsFootwear(bestHit.renderer)))
            {
                if (_debug.Value) Log.LogInfo($"[Hit] foot covered ({ShoeName(best)}, mesh {bestHit.renderer}) -> no tickle");
                if (freeRoam) CountFootPoke(best);
                if (!(_checkUI.Value && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
                {
                    PlayGameSound(_shoeSound.Value);
                    var shoeLines = ClimbLineNow() ? FootwearLines(best, Lines.For(best.dialogue != null ? best.dialogue.character : CharacterType.None)) : null;
                    if (shoeLines != null) Say(best, shoeLines, false);
                }
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
            ExtendShrink();
            switch (bestHit.part)
            {
                case BodyPart.Head: HeadPat(best); break;
                case BodyPart.Foot: Tickle(best, foot: true); if (freeRoam) CountFootPoke(best); break;
                case BodyPart.Tummy: Tickle(best, foot: false); break;
                case BodyPart.Leg:
                    {
                        // Thigh or shin/ankle: from the dominant bone of the hit.
                        string topBone = (bestHit.bone ?? "").Split(' ')[0].ToLowerInvariant();
                        _legIsThigh = topBone.Contains("thigh") || topBone.Contains("upleg") || topBone.Contains("upperleg") || topBone.Contains("thight");
                        _legInStockings = (bestHit.renderer ?? "").ToLowerInvariant().Contains("stocking");
                        Poke(best, false, leg: true);
                        // Lobby: climbed up to her thigh and poking it fills her growth bar.
                        var pl = PlayerDriver.LocalPlayer;
                        if (_legIsThigh && pl != null && pl.footGrabbed && SetupManager.Instance != null && SetupManager.Instance.isLobby)
                            AddGrowthCharge(best);
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
            // Soles: the stomp comes first (its level), the pickup only one level later.
            int pickAt = bestHit.part == BodyPart.Foot && _stompEnabled.Value ? Math.Max(_pickupLevel.Value, _stompAtLevel.Value + 1) : _pickupLevel.Value;
            if (freeRoam && !_dropping && _pickupLevel.Value >= 0 && _moods.TryGetValue(best, out var mood) && LevelOf(mood.annoyance) >= pickAt)
                StartCoroutine(PickupLater(best, 1.2f));
        }

        private readonly HashSet<Giantess> _climbSetup = new HashSet<Giantess>();
        private LevelBlessingFootGrab _footGrabBlessing;
        private bool _footGrabSearched;
        private float _nextBlessingSearch;

        /// <summary>
        /// The game only sets up foot climbing (a level blessing) outside the lobby. In the lobby we run the
        /// same setup ourselves, with the game's own foot-climb blessing, so "E" on her foot climbs.
        /// </summary>
        private void EnableClimbing(Giantess g)
        {
            _climbSetup.RemoveWhere(x => x == null);   // characters from a previous lobby
            if (_climbSetup.Contains(g)) return;
            try
            {
                if (g.footGrabManager == null) { _climbSetup.Add(g); return; }
                if (g.footGrabManager.script != null) { _climbSetup.Add(g); return; }
                // Search again after a lobby reload: the old blessing object is gone then.
                if ((!_footGrabSearched || _footGrabBlessing == null) && Time.time >= _nextBlessingSearch)
                {
                    _footGrabSearched = true;
                    _nextBlessingSearch = Time.time + 2f;
                    _footGrabBlessing = (SceneLoader.InstanceData?.GetLoadedMinorBlessing as LevelBlessingFootGrab)
                        ?? Resources.FindObjectsOfTypeAll<LevelBlessingFootGrab>().FirstOrDefault();
                    if (_debug.Value || _footGrabBlessing != null) Log.LogInfo(_footGrabBlessing != null ? "[Climb] found the foot-climb blessing" : "[Climb] foot-climb blessing not loaded in the lobby");
                }
                if (_footGrabBlessing == null) return;
                _footGrabBlessing.Setup(g);
                _climbSetup.Add(g);
                Log.LogInfo($"[Climb] climbing enabled on {g.dialogue?.character}");
            }
            catch (Exception e) { _climbSetup.Add(g); Log.LogWarning($"[Climb] could not enable climbing on {g.dialogue?.character}: {e.Message}"); }
        }

        // ---------------------------------------------------------------- growth (Akipo, headpats)

        private bool _growWhyLogged;

        // Her normal lobby size, and whether she is bigger than that right now.
        internal static readonly Dictionary<Giantess, float> BaseScale = new Dictionary<Giantess, float>();
        internal static bool IsGrown(Giantess g) => g != null && BaseScale.TryGetValue(g, out float b) && g.scale > b * 1.02f;

        private bool _wasClimbing, _shrinking;
        private float _releasedAt = -1f;

        /// <summary>After you let go of her (jump off / "R") while she is grown, she shrinks back after a moment.</summary>
        private void WatchRelease()
        {
            var player = PlayerDriver.LocalPlayer;
            if (player == null) return;
            // Grown (however the growth started): full-strength body, so she breaks what she touches.
            if (_destroyWhenGrown && FootClimberPerk())
                foreach (var gg in BaseScale.Keys)
                    if (gg != null && !_hardened.Contains(gg) && IsGrown(gg)) { _hardened.Add(gg); HardenColliders(gg); }
            bool climbing = player.footGrabbed;
            if (_wasClimbing && !climbing) _releasedAt = Time.time;
            _wasClimbing = climbing;
            if (!climbing && _releasedAt >= 0f && BaseScale.Keys.Any(IsGrowing)) _releasedAt = Time.time;   // wait until the growth is over
            if (climbing || _shrinking || _releasedAt < 0f || Time.time - _releasedAt < _shrinkDelay.Value) return;
            _releasedAt = -1f;
            foreach (var kv in BaseScale.ToList())
                if (kv.Key != null && IsGrown(kv.Key)) { StartCoroutine(ShrinkBack(kv.Key, kv.Value)); break; }
        }

        /// <summary>The end-game "shrink" pieces: jump-turn-shrink animation, shrinking particles and sound; feet kept on the ground.</summary>
        private IEnumerator ShrinkBack(Giantess g, float baseScale)
        {
            _shrinking = true;
            float start = g.scale;
            Log.LogInfo($"[Grow] {g.dialogue?.character} shrinks back {Meters(g):F0} m -> {baseScale * 30f:F0} m");
            try { g.sfxManager?.shrinkingParticleSystem?.Play(); } catch { }
            try { g.sfxManager?.growthStartAudioSource?.PlayAudioOneshot(); } catch { }
            float dur = 2f;
            // The animator state she is in now (her lobby idle). The shrink clip has no way back out on its
            // own in the lobby controller, so we return to this state afterwards.
            Animator anim = null; int backHash = 0; bool played = false;
            try
            {
                anim = g.giantessIK != null ? g.giantessIK.animator : g.animator;
                if (anim != null)
                {
                    backHash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
                    int hash = Animator.StringToHash("Jump Turn Shrink");
                    if (anim.HasState(0, hash)) { anim.CrossFadeInFixedTime(hash, 0.2f, 0); played = true; }
                }
            }
            catch { }
            Vector3 startPos = g.transform.position; Quaternion startRot = g.transform.rotation;
            var feet = SnapFeet(g);
            bool hasPose = BasePose.TryGetValue(g, out var pose);
            float t = 0f;
            while (t < dur && g != null)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                g.scale = Mathf.Lerp(start, baseScale, k);
                if (hasPose)
                {
                    try { g.SetPosition(Vector3.Lerp(startPos, pose.pos, k)); } catch { }
                    g.transform.rotation = Quaternion.Slerp(startRot, pose.rot, k);
                }
                try { g.UpdateScale(); PlaceFeet(g, feet); } catch { }
                yield return null;
            }
            if (g != null)
            {
                g.scale = baseScale;
                if (hasPose) { try { g.SetPosition(pose.pos); } catch { } g.transform.rotation = pose.rot; }
                try { g.UpdateScale(); PlaceFeet(g, feet); } catch { }
                try { g.sfxManager?.shrinkingParticleSystem?.Stop(); } catch { }
                try { g.sfxManager?.PlayGrowthImpactSmall(); } catch { }
                try { g.UpdateFootstepAudio(); } catch { }   // her footsteps sound her size again (the game does this after growing)
                if (g.growth != null) { g.growth.growthCharge = 0f; g.growth.wantsToGrowTrigger = false; }
                try { if (played && anim != null && backHash != 0) anim.CrossFadeInFixedTime(backHash, 0.3f, 0); } catch { }
                RestoreLobbyState(g);
            }
            // The idle pivot only re-places the left foot; once it has started, re-place the right one too.
            float until = Time.time + 1.5f;
            while (g != null && Time.time < until)
            {
                var beh = g.aiLogic?.currentBehaviour as GiantessBehaviourStandIdle;
                var ik = g.giantessIK;
                if (beh == null || ik == null) break;
                if (ik.leftFootIK != null && ik.leftFootIK.nextStepState == FollowingStepState.FinishMatchIdle)
                {
                    if (ik.rightFootIK != null) ik.rightFootIK.nextStepState = FollowingStepState.FinishMatchIdle;
                    break;
                }
                yield return null;
            }
            _shrinking = false;
        }

        internal static readonly Dictionary<Giantess, GiantessBehaviour> LobbyBehaviour = new Dictionary<Giantess, GiantessBehaviour>();
        internal static readonly Dictionary<Giantess, float> LobbyMaxScale = new Dictionary<Giantess, float>();
        internal static readonly Dictionary<Giantess, (Vector3 pos, Quaternion rot)> BasePose = new Dictionary<Giantess, (Vector3, Quaternion)>();

        /// <summary>
        /// After the shrink: put her back exactly as the lobby had her – her lobby behaviour (the one that
        /// handles pickup), no grow behaviour, the lobby's size limit and scale cap.
        /// </summary>
        private static void RestoreLobbyState(Giantess g)
        {
            try
            {
                var ai = g.aiLogic;
                if (ai != null)
                {
                    ai.nextBehaviourQueue = default;
                    ai.nextMovementPath = null;
                    if (LobbyBehaviour.TryGetValue(g, out var lobby) && lobby != null && ai.currentBehaviour != lobby)
                    {
                        try { ai.currentBehaviour?.BehaviourEnd(ai); } catch { }
                        ai.previousBehaviour = ai.currentBehaviour;
                        ai.currentBehaviour = lobby;
                        lobby.BehaviourStart(ai);
                    }
                    var grow = g.gtsBehGrowScript;
                    if (grow != null && ai.behaviours != null && ai.currentBehaviour != grow)
                    {
                        ai.behaviours = ai.behaviours.Where(b => b != grow).ToArray();
                        ai.typeToBehaviour?.Remove(typeof(GiantessBehaviourGrow));
                        g.gtsBehGrowScript = null;
                        UnityEngine.Object.Destroy(grow);
                    }
                }
                // The feet keep their old (big) footing until she moves: let her idle fidget/pivot right away,
                // which re-places the feet like after any normal step.
                if (ai?.currentBehaviour is GiantessBehaviourStandIdle idle) idle.interruptTime = Time.time + 0.4f;
                var setup = SetupManager.Instance;
                if (setup != null && LobbyMaxScale.TryGetValue(g, out float max)) setup.gts_MaxScale = max;
                if (g.growth != null) g.growth.reachedScaleCap = true;
                SoftenColliders(g); _hardened.Remove(g);
                LobbyBehaviour.Remove(g); LobbyMaxScale.Remove(g); BaseScale.Remove(g); BasePose.Remove(g);
                Log.LogInfo($"[Grow] {g.dialogue?.character} back to normal (behaviour {ai?.currentBehaviour?.GetType().Name ?? "none"})");
            }
            catch (Exception e) { Log.LogWarning("[Grow] restore failed: " + e.Message); }
        }

        // ---- Crouch (test): foot pokes make her crouch down in place, then stand up again.
        internal static readonly HashSet<Giantess> Crawling = new HashSet<Giantess>();
        internal static readonly Dictionary<Giantess, Vector3> CrawlSpot = new Dictionary<Giantess, Vector3>();
        private int _footPokes;
        private float _lastFootPoke;

        /// <summary>Akipo standing idle in the lobby, free for an animation of ours (crouch, stomp).</summary>
        private bool CanCrawl(Giantess g) => _crawlEnabled.Value && CanAnimate(g);

        private bool CanAnimate(Giantess g)
        {
            if (g == null || g.dialogue == null || g.dialogue.character != CharacterType.Akipo) return false;
            if (SetupManager.Instance == null || !SetupManager.Instance.isLobby) return false;
            if (Crawling.Contains(g) || _shrinking || IsGrown(g) || IsGrowing(g)) return false;
            return g.aiLogic != null && g.aiLogic.currentBehaviour is GiantessBehaviourStandIdle;
        }

        private void CountFootPoke(Giantess g)
        {
            if (!CanCrawl(g)) return;
            // Crouching is her playful reaction: only while she is still giggling (then the stomp, then the pickup).
            if (_moods.TryGetValue(g, out var mood) && LevelOf(mood.annoyance) >= 2) { _footPokes = 0; return; }
            if (Time.time - _lastFootPoke > 10f) _footPokes = 0;
            _lastFootPoke = Time.time;
            if (++_footPokes < Mathf.Max(1, _pokesPerCrawl.Value)) return;
            _footPokes = 0;
            StartCoroutine(CrawlDown(g));
        }

        // ---- Tease stomp: tickle her soles too much (free roam, lobby) and she stomps her foot.
        private float _lastStomp = -99f;

        private void MaybeStomp(Giantess g, int lvl)
        {
            if (!_stompEnabled.Value || lvl < _stompAtLevel.Value || Time.time - _lastStomp < 8f) return;
            if (!CanAnimate(g)) return;   // standing idle in the lobby = nobody holds you
            _lastStomp = Time.time;
            StartCoroutine(TeaseStomp(g));
        }

        private IEnumerator TeaseStomp(Giantess g)
        {
            var ik = g.giantessIK; var anim = g.animator;
            if (ik == null || anim == null) yield break;
            int stomp = Animator.StringToHash("TeaseStomp"), idle = Animator.StringToHash("Idle");
            if (!anim.HasState(0, stomp)) { Log.LogInfo("[Stomp] no TeaseStomp animation on " + g.dialogue?.character); yield break; }
            var beh = g.aiLogic.currentBehaviour as GiantessBehaviourStandIdle;
            if (beh != null) beh.interruptTime = Time.time + 9999f;
            Log.LogInfo($"[Stomp] {g.dialogue?.character} stomps her foot");
            var feet = SnapFeet(g);
            Crawling.Add(g); CrawlSpot[g] = g.transform.position;
            if (ik.rightFootIK != null) ik.rightFootIK.nextStepState = FollowingStepState.MatchIdle;
            anim.CrossFadeInFixedTime(stomp, 0.15f, 0);
            yield return new WaitForSeconds(0.3f);
            float timeout = Time.time + 6f;
            while (g != null && Time.time < timeout)
            {
                var st = anim.GetCurrentAnimatorStateInfo(0);
                if (st.shortNameHash == idle && !anim.IsInTransition(0)) break;
                if (st.shortNameHash == stomp && st.normalizedTime > 0.92f) { anim.CrossFadeInFixedTime(idle, 0.3f, 0); yield return new WaitForSeconds(0.35f); break; }
                yield return null;
            }
            if (g != null)
            {
                if (CrawlSpot.TryGetValue(g, out var spot)) { try { g.SetPosition(spot); } catch { } }
                PlaceFeet(g, feet);
                foreach (var l in new GiantessLimbIK[] { ik.leftFootIK, ik.rightFootIK })
                    if (l != null) l.nextStepState = FollowingStepState.CompleteIdle;
                if (beh != null) beh.interruptTime = Time.time + UnityEngine.Random.Range(10f, 30f);
                Log.LogInfo($"[Stomp] {g.dialogue?.character} done");
            }
            Crawling.Remove(g); CrawlSpot.Remove(g);
        }

        private IEnumerator CrawlDown(Giantess g)
        {
            var ik = g.giantessIK; var anim = g.animator;
            if (ik == null || anim == null) yield break;
            int down = Animator.StringToHash("Crouch Down"), crouching = Animator.StringToHash("Crouching"),
                up = Animator.StringToHash("Crouch Up"), idle = Animator.StringToHash("Idle");
            int target = anim.HasState(0, down) ? down : anim.HasState(0, crouching) ? crouching : 0;
            if (target == 0 || !anim.HasState(0, up)) { Log.LogInfo($"[Crouch] no crouch animation on {g.dialogue?.character}"); yield break; }
            var beh = g.aiLogic.currentBehaviour as GiantessBehaviourStandIdle;
            if (beh != null) beh.interruptTime = Time.time + 9999f;   // no idle fidget while she is down
            Log.LogInfo($"[Crouch] {g.dialogue?.character} crouches down");
            var feet = SnapFeet(g);
            Crawling.Add(g); CrawlSpot[g] = g.transform.position;
            try
            {
                var ex = g.expressionManager;
                ex?.eyeOverride.SetBehaviour(ExpressionEnum.Smug);
                ex?.mouthOverride.SetBehaviour(ExpressionEnum.MouthSmile);
            }
            catch { }
            anim.CrossFadeInFixedTime(target, 0.6f, 0);
            float until = Time.time + 1.5f + Mathf.Max(0.5f, _crawlSeconds.Value);
            while (g != null && Time.time < until) yield return null;
            if (g == null) yield break;
            Log.LogInfo($"[Crouch] {g.dialogue?.character} stands up");
            anim.CrossFadeInFixedTime(up, 0.3f, 0);
            try { g.expressionManager?.eyeOverride.ClearBehaviour(); g.expressionManager?.mouthOverride.ClearBehaviour(); } catch { }
            yield return new WaitForSeconds(0.5f);
            float timeout = Time.time + 6f;
            bool toIdle = false;
            while (g != null && Time.time < timeout)
            {
                var st = anim.GetCurrentAnimatorStateInfo(0);
                if (st.shortNameHash == idle && !anim.IsInTransition(0)) break;
                if (!toIdle && st.shortNameHash == up && st.normalizedTime > 0.9f) { anim.CrossFadeInFixedTime(idle, 0.4f, 0); toIdle = true; }
                yield return null;
            }
            if (g != null)
            {
                if (CrawlSpot.TryGetValue(g, out var spot)) { try { g.SetPosition(spot); } catch { } }
                PlaceFeet(g, feet);
                if (beh != null) beh.interruptTime = Time.time + UnityEngine.Random.Range(10f, 30f);
                Log.LogInfo($"[Crouch] {g.dialogue?.character} is standing again");
            }
            Crawling.Remove(g); CrawlSpot.Remove(g);
        }

        // ---- Destruction while grown: the lobby softens her body colliders (force 4, things break from 5).
        private static readonly Dictionary<ArmatureCollider, (ArmatureCollider.CapsuleProperties mv, ArmatureCollider.CapsuleProperties st, ArmatureCollider.CapsuleProperties lt)> _softProps
            = new Dictionary<ArmatureCollider, (ArmatureCollider.CapsuleProperties, ArmatureCollider.CapsuleProperties, ArmatureCollider.CapsuleProperties)>();

        /// <summary>Grown: give her body colliders the game's normal (level) strength, so she breaks what she touches.</summary>
        internal static void HardenColliders(Giantess g)
        {
            if (!_destroyWhenGrown) return;
            int n = 0;
            foreach (var c in g.GetComponentsInChildren<ArmatureCollider>(true))
            {
                if (!_softProps.ContainsKey(c)) _softProps[c] = (c.movementCapsuleProps, c.stepCapsuleProps, c.stepLightCapsuleProps);
                c.movementCapsuleProps = new ArmatureCollider.CapsuleProperties(1f, 1f, 5f, 5f);
                c.stepCapsuleProps = new ArmatureCollider.CapsuleProperties(4f, 20f, 5f, 1f);
                c.stepLightCapsuleProps = new ArmatureCollider.CapsuleProperties(2f, 10f, 5f, 1f);
                n++;
            }
            Log.LogInfo($"[Destruct] {g.dialogue?.character} is grown: {n} body colliders at full strength");
        }

        /// <summary>Back to her lobby size: the lobby's soft colliders again.</summary>
        internal static void SoftenColliders(Giantess g)
        {
            foreach (var c in g.GetComponentsInChildren<ArmatureCollider>(true))
                if (_softProps.TryGetValue(c, out var o)) { c.movementCapsuleProps = o.mv; c.stepCapsuleProps = o.st; c.stepLightCapsuleProps = o.lt; _softProps.Remove(c); }
        }

        internal static bool _destroyWhenGrown = true;
        private static readonly HashSet<Giantess> _hardened = new HashSet<Giantess>();

        /// <summary>Debug (F10): the lobby's destructibles near each character, and her body colliders.</summary>
        private void LogDestructibles()
        {
            try
            {
                var mgr = KiriHLODManager.Instance;
                var tree = mgr != null ? mgr.destructibleTree : null;
                var all = tree != null ? tree.destructibles : null;
                var pl = PlayerDriver.LocalPlayer;
                if (pl != null)
                {
                    float r = 1.5f * Mathf.Max(0.2f, pl.playerScale.GetModifiedValue);
                    foreach (var c in Physics.OverlapSphere(pl.transform.position, r, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (c == null) continue;
                        var gg = c.GetComponentInParent<Giantess>();
                        string path = c.name; for (var x = c.transform.parent; x != null && path.Length < 160; x = x.parent) path = x.name + "/" + path;
                        Log.LogInfo($"[Near] {(gg != null ? gg.dialogue?.character.ToString() : "-")} layer {LayerMask.LayerToName(c.gameObject.layer)} {c.GetType().Name} {path}");
                    }
                }
                Log.LogInfo($"[Destruct] manager={(mgr != null)} tree={(tree != null)} destructibles={(all == null ? -1 : all.Length)}");
                var scene = FindObjectsOfType<DestructibleParent>();
                Log.LogInfo($"[Destruct] DestructibleParent objects in scene: {scene.Length} (active {scene.Count(d => d.active)}, undestructible {scene.Count(d => d.undestructible)}, colliderOnly {scene.Count(d => d.onlyApplyOnColliders)}, inTree {(all == null ? 0 : scene.Count(d => System.Array.IndexOf(all, d) >= 0))})");
                foreach (var g in FindObjectsOfType<Giantess>())
                {
                    var name = g.dialogue != null ? g.dialogue.character.ToString() : g.name;
                    Vector3 pos = g.transform.position;
                    foreach (var d in scene.OrderBy(d => Vector3.Distance(d.transform.position, pos)).Take(4))
                    {
                        Vector3 dp = d.transform.position - pos; float h = new Vector2(dp.x, dp.z).magnitude;
                        Log.LogInfo($"[Destruct]   {name}: '{d.name}' {h:F1} away (her size {g.scale:F2}) active={d.active} undestructible={d.undestructible} colliderOnly={d.onlyApplyOnColliders}");
                    }
                    var cols = g.GetComponentsInChildren<ArmatureCollider>(true);
                    int on = cols.Count(c => c.enabled && c.gameObject.activeInHierarchy), owned = cols.Count(c => c.IsGiantessOwned);
                    var c0 = cols.FirstOrDefault();
                    Log.LogInfo($"[Destruct]   {name}: body colliders {cols.Length} (enabled {on}, set up {owned})" + (c0 != null ? $", movement force {c0.movementCapsuleProps.closeForce}/{c0.movementCapsuleProps.farForce}, step force {c0.stepCapsuleProps.closeForce}/{c0.stepCapsuleProps.farForce}" : ""));
                }
            }
            catch (Exception e) { Log.LogWarning("[Destruct] " + e); }
        }

        /// <summary>Is her growth running, queued, or about to be queued?</summary>
        private bool IsGrowing(Giantess g)
        {
            if (g == null) return false;
            if (_growPending.Contains(g)) return true;
            var ai = g.aiLogic;
            if (ai == null) return false;
            return ai.currentBehaviour is GiantessBehaviourGrow
                || ai.nextBehaviourQueue.behaviour == typeof(GiantessBehaviourGrow)
                || (g.growth != null && g.growth.wantsToGrowTrigger);
        }

        /// <summary>Her feet's footing, in her own space (position/rotation relative to her, in units of her size).</summary>
        private struct FeetSnapshot { public Vector3 lp, ln, rp, rn; public Quaternion lpr, lnr, rpr, rnr; public bool ok; }

        private static FeetSnapshot SnapFeet(Giantess g)
        {
            var s = new FeetSnapshot();
            var ik = g.giantessIK; if (ik == null || ik.leftFootIK == null || ik.rightFootIK == null) return s;
            Vector3 pos = g.transform.position; Quaternion inv = Quaternion.Inverse(g.transform.rotation); float sc = Mathf.Max(g.scale, 0.0001f);
            Vector3 L(Vector3 w) => inv * (w - pos) / sc;
            s.lp = L(ik.leftFootIK.lastGroundedPosition); s.ln = L(ik.leftFootIK.nextGroundedPosition);
            s.rp = L(ik.rightFootIK.lastGroundedPosition); s.rn = L(ik.rightFootIK.nextGroundedPosition);
            s.lpr = inv * ik.leftFootIK.lastGroundedRotation; s.lnr = inv * ik.leftFootIK.nextGroundedRotation;
            s.rpr = inv * ik.rightFootIK.lastGroundedRotation; s.rnr = inv * ik.rightFootIK.nextGroundedRotation;
            s.ok = true;
            return s;
        }

        /// <summary>Put the feet's footing where it belongs for her current position, rotation and size.</summary>
        private static void PlaceFeet(Giantess g, FeetSnapshot s)
        {
            if (!s.ok) { KeepFeetGrounded(g); return; }
            var ik = g.giantessIK; if (ik == null) return;
            Vector3 pos = g.transform.position; Quaternion rot = g.transform.rotation; float sc = g.scale;
            Vector3 W(Vector3 l) => pos + rot * (l * sc);
            ik.leftFootIK.lastGroundedPosition = W(s.lp); ik.leftFootIK.nextGroundedPosition = W(s.ln);
            ik.rightFootIK.lastGroundedPosition = W(s.rp); ik.rightFootIK.nextGroundedPosition = W(s.rn);
            ik.leftFootIK.lastGroundedRotation = rot * s.lpr; ik.leftFootIK.nextGroundedRotation = rot * s.lnr;
            ik.rightFootIK.lastGroundedRotation = rot * s.rpr; ik.rightFootIK.nextGroundedRotation = rot * s.rnr;
            foreach (var f in new[] { ik.leftFootIK, ik.rightFootIK })
            {
                f.lastGiantessPosition = g.GetPosition();
                f.lastGiantessRotation = g.GetRotation();
            }
        }

        /// <summary>Like the game's growth does: move the feet's grounded positions to where they should be now.</summary>
        private static void KeepFeetGrounded(Giantess g)
        {
            var ik = g.giantessIK; if (ik == null) return;
            foreach (var f in new[] { ik.leftFootIK, ik.rightFootIK })
                if (f != null) f.lastGroundedPosition = f.nextGroundedPosition;
        }

        /// <summary>
        /// The lobby characters lack the game's growth behaviour. Add it next to her other behaviours and
        /// register it with her AI, so the game's own growth (animation, grounded feet, lines, sounds) can run.
        /// </summary>
        private static void EnsureGrowBehaviour(Giantess g)
        {
            if (g.gtsBehGrowScript != null) return;
            try
            {
                var ai = g.aiLogic;
                if (ai == null || ai.behaviours == null || ai.behaviours.Length == 0) return;
                var host = ai.behaviours[0].gameObject;
                var grow = host.AddComponent<GiantessBehaviourGrow>();   // its Awake sets g.gtsBehGrowScript
                if (g.gtsBehGrowScript == null) g.gtsBehGrowScript = grow;
                ai.typeToBehaviour[typeof(GiantessBehaviourGrow)] = grow;
                var list = new List<GiantessBehaviour>(ai.behaviours) { grow };
                ai.behaviours = list.ToArray();
                Log.LogInfo($"[Grow] added the game's growth behaviour to {g.dialogue?.character}");
            }
            catch (Exception e) { Log.LogWarning("[Grow] could not add the growth behaviour: " + e.Message); }
        }

        /// <summary>
        /// The lobby pins her size (min scale == max scale, so "scale cap reached"). Raise the game's max
        /// scale to MaxMeters and clear the cap, so its own growth can actually grow her. A lobby reload resets it.
        /// </summary>
        private void UnlockLobbyGrowth(Giantess g)
        {
            try
            {
                var setup = SetupManager.Instance;
                if (setup == null || !setup.isLobby || g.growth == null) return;
                float perScale = 30f;
                try { if (LootZoneManager.Instance != null) perScale = LootZoneManager.Instance.meterPerScale; } catch { }
                float maxScale = MaxGrowScale(perScale);
                if (!LobbyMaxScale.ContainsKey(g)) LobbyMaxScale[g] = setup.gts_MaxScale;
                if (setup.gts_MaxScale < maxScale)
                {
                    setup.gts_MaxScale = maxScale;
                    Log.LogInfo($"[Grow] lobby max size raised to {maxScale * perScale:F0} m (scale {maxScale:F2}, now {g.scale:F2})");
                }
                if (g.growth.reachedScaleCap && g.scale < maxScale - 0.001f) g.growth.reachedScaleCap = false;
                if (!BaseScale.ContainsKey(g))
                {
                    BaseScale[g] = g.scale;
                    BasePose[g] = (g.transform.position, g.transform.rotation);
                }
                var curBeh = g.aiLogic?.currentBehaviour;
                if (!LobbyBehaviour.ContainsKey(g) && curBeh != null && !(curBeh is GiantessBehaviourGrow)) LobbyBehaviour[g] = curBeh;
                EnsureGrowBehaviour(g);
                // The lobby computes a zero-sized growth bar: use the game's normal base size.
                if (!(g.growth.growthMaxCharge > 0f)) g.growth.growthMaxCharge = 100f;
                if (float.IsNaN(g.growth.growthCharge)) g.growth.growthCharge = 0f;
            }
            catch (Exception e) { Log.LogWarning("[Grow] " + e.Message); }
        }

        /// <summary>
        /// Her size limit (scale units): MaxMeters if set, otherwise the game's own max giantess size setting
        /// (the level select slider, SceneLoader.InstanceData), so the lobby grows her as far as the game would.
        /// </summary>
        private float MaxGrowScale(float perScale)
        {
            if (_growMaxMeters.Value > 0f) return _growMaxMeters.Value / perScale;
            try
            {
                var data = SceneLoader.InstanceData;
                if (data != null)
                {
                    float sc = data.GetMaxSizeScale();
                    if (sc > 0f) return sc;
                }
            }
            catch { }
            return 90f / perScale;   // fallback: 90 m
        }

        /// <summary>Her height in meters (the lobby has no LootZoneManager: use the game's default 30 m per scale unit).</summary>
        private static float Meters(Giantess g)
        {
            float perScale = 30f;
            try { if (LootZoneManager.Instance != null) perScale = LootZoneManager.Instance.meterPerScale; } catch { }
            return g.scale * perScale;
        }

        private bool CanGrow(Giantess g)
        {
            if (!_growEnabled.Value || g == null || !FootClimberPerk()) return false;
            if (g.dialogue == null || g.dialogue.character != CharacterType.Akipo) return false;
            UnlockLobbyGrowth(g);
            string why = g.growth == null ? "no growth component" : g.growth.disableGrowth ? "growth disabled by the game (scale cap / nutrition)" : g.scale >= MaxGrowScale(Meters(g) / Mathf.Max(g.scale, 0.0001f)) - 0.001f ? $"already {Meters(g):F0} m (max)" : null;
            if (why != null)
            {
                if (!_growWhyLogged) { _growWhyLogged = true; Log.LogInfo($"[Grow] {g.dialogue.character} can't grow: {why}"); }
                return false;
            }
            return true;
        }


        /// <summary>A climbing step fills her growth bar; when it is full she grows a stage (the game's own growth).</summary>
        private void AddGrowthCharge(Giantess g)
        {
            if (!CanGrow(g)) return;
            var gr = g.growth;
            float step = gr.growthMaxCharge / Mathf.Max(1, _patsPerGrowth.Value);   // climbs per stage
            gr.UpdateScaleGrowthHelper(step);
            Log.LogInfo($"[Grow] {g.dialogue?.character} bar {gr.growthCharge / gr.growthMaxCharge:P0} ({Meters(g):F0} m)");
            if (gr.WantsToGrowSpurt && !_growPending.Contains(g)) StartCoroutine(GrowSoon(g));
        }

        private readonly HashSet<Giantess> _growPending = new HashSet<Giantess>();
        private bool _growing;

        /// <summary>One growth stage: the game's growth sounds and glow, a smooth size change, and her GROW_END line.</summary>
        private IEnumerator OwnGrowth(Giantess g)
        {
            if (_growing) yield break;
            _growing = true;
            float perScale = 30f;
            try { if (LootZoneManager.Instance != null) perScale = LootZoneManager.Instance.meterPerScale; } catch { }
            float start = g.scale, end = Mathf.Min(start * _growStep.Value, MaxGrowScale(perScale));
            g.growth.growthCharge = 0f;
            try { g.sfxManager?.PlayGrowthStart(); } catch { }
            try { g.fresnelAnimation?.ApplyGrowthSmallBurst(); } catch { }
            Log.LogInfo($"[Grow] {g.dialogue?.character} grows {start * perScale:F0} m -> {end * perScale:F0} m");
            float t = 0f, dur = 2.5f;
            while (t < dur && g != null)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                g.scale = Mathf.Lerp(start, end, k);
                try { g.UpdateScale(); } catch { }
                yield return null;
            }
            if (g != null)
            {
                g.scale = end;
                try { g.UpdateScale(); } catch { }
                try { g.sfxManager?.PlayGrowthImpact(); } catch { }
                try { g.fresnelAnimation?.ApplyGrowthSmallBurst(); } catch { }
                try { DialogueManager.Instance?.AddDialgoueGroup(g, g.GetDialogueKey("GROW_END")); } catch (Exception e) { Log.LogInfo("[Grow] no GROW_END line: " + e.Message); }
            }
            _growing = false;
        }

        /// <summary>After her happy reaction, start the growth – unless the game already did.</summary>
        private IEnumerator GrowSoon(Giantess g)
        {
            _growPending.Add(g);
            yield return new WaitForSeconds(1.5f);
            _growPending.Remove(g);
            if (g == null || g.growth == null || !g.growth.WantsToGrowSpurt) yield break;
            if (g.gtsBehGrowScript == null)
            {
                // The lobby has no growth behaviour: play the growth ourselves with the game's own pieces.
                StartCoroutine(OwnGrowth(g));
                yield break;
            }
            try
            {
                var ai = g.aiLogic;
                var cur = ai?.currentBehaviour;
                if (cur == null) yield break;
                if (float.IsNaN(g.scaleExponentialScale) || g.scaleExponentialScale <= 0f) g.scaleExponentialScale = g.scale * 0.1f;
                if (float.IsNaN(g.scale)) yield break;
                if (!LobbyBehaviour.ContainsKey(g) && !(cur is GiantessBehaviourGrow)) LobbyBehaviour[g] = cur;
                var payload = g.growth.CreateGrowBehaviourPayload();
                cur.BehaviourAttemptSendInterrupt(ai, payload);
                g.growth.wantsToGrowTrigger = true;
                ai.QueueBehaviour(payload);
                Log.LogInfo($"[Grow] {g.dialogue?.character} grows with the game's growth ({Meters(g):F0} m)");
            }
            catch (Exception e) { Log.LogWarning("[Grow] game growth failed, using the simple one: " + e.Message); StartCoroutine(OwnGrowth(g)); }
        }

        /// <summary>Debug: why can't you climb her in the lobby? (GrabFoot state)</summary>
        private static void LogClimbState(Giantess g)
        {
            try
            {
                var player = PlayerDriver.LocalPlayer;
                var gfs = g.GetComponentsInChildren<GrabFoot>(true);
                string state = gfs.Length == 0 ? "no GrabFoot component" : string.Join("; ", gfs.Select(gf =>
                    $"{gf.name} active={gf.gameObject.activeInHierarchy} enabled={gf.enabled} canGrab={(player != null && gf.CanMainInteract(player))}"));
                Log.LogInfo($"[Climb] {g.dialogue?.character}: {state} | footGrabManager={(g.footGrabManager != null ? (g.footGrabManager.isActiveAndEnabled ? "on" : "off") : "missing")} | angry={g.aiLogic?.IsCurrentBehaviourAngry()}");
            }
            catch (Exception e) { Log.LogInfo("[Climb] " + e.Message); }
        }

        /// <summary>Pickup key: the nearest character in free roam that can pick you up does so.</summary>
        private void PickupNearest()
        {
            var player = PlayerDriver.LocalPlayer;
            var setup = SetupManager.Instance;
            if (player == null || setup == null || !setup.isLobby || player.footGrabbed) return;
            Vector3 me = PlayerDriver.GetPlayerPosition();
            Giantess best = null; float bestD = float.MaxValue;
            foreach (var kv in Plugin_Active.Poses)
            {
                if (kv.Key == null || kv.Value != Pose.Free) continue;
                var call = kv.Key.GetComponentInChildren<CallGiantess>();
                if (call == null) continue;
                float d = Vector3.Distance(me, kv.Key.transform.position);
                if (d < bestD) { bestD = d; best = kv.Key; }
            }
            if (best == null) return;
            RequestPickup(best);
        }

        internal static bool LetClimbWinPaused, ClimbBypass;
        internal static GrabFoot AimedFoot;
        internal static int AimedFootFrame = -99;
        internal static float AimedFootTime = -99f;
        private GrabFoot _heldFoot;
        private float _heldSince = -1f;
        private bool _heldPicked;

        /// <summary>"E" on her foot in the lobby: a tap climbs (the game's own climb), holding asks her to pick you up.</summary>
        private void HandleFootKey()
        {
            var player = PlayerDriver.LocalPlayer;
            if (player == null || player.footGrabbed) { _heldFoot = null; return; }
            bool aiming = AimedFoot != null && Time.frameCount - AimedFootFrame <= 2;
            if (Input.GetKeyDown(KeyCode.E) && aiming) { _heldFoot = AimedFoot; _heldSince = Time.time; _heldPicked = false; }
            if (_heldFoot == null) return;
            if (Input.GetKey(KeyCode.E))
            {
                if (!_heldPicked && Time.time - _heldSince >= _holdSeconds.Value)
                {
                    _heldPicked = true;
                    var g = _heldFoot.giantess;
                    if (g != null) RequestPickup(g);
                }
                return;
            }
            // released
            var foot = _heldFoot; _heldFoot = null;
            if (_heldPicked || foot == null) return;
            ClimbBypass = true;
            try
            {
                if (foot.CanMainInteract(player)) { foot.OnMainInteract(player); Log.LogInfo("[Climb] climbing"); }
            }
            catch (Exception e) { Log.LogWarning("[Climb] " + e.Message); }
            finally { ClimbBypass = false; }
        }

        /// <summary>Same as pressing "E" at her: the game's own call/pickup interaction.</summary>
        private void RequestPickup(Giantess g)
        {
            var player = PlayerDriver.LocalPlayer;
            var call = g.GetComponentInChildren<CallGiantess>();
            if (player == null || call == null) { Log.LogInfo("[Pickup] no call interaction found"); return; }
            if (IsGrown(g)) { Log.LogInfo("[Pickup] she is grown – no pickup until she is back to normal"); return; }
            if (Crawling.Contains(g)) { Log.LogInfo("[Pickup] she is busy (crouching / stomping) – no pickup now"); return; }
            LetClimbWinPaused = true;   // our own pickup must not step aside for climbing
            try
            {
                if (!call.CanMainInteract(player)) { Log.LogInfo("[Pickup] she cannot pick you up right now"); return; }
                call.OnMainInteract(player);
            }
            finally { LetClimbWinPaused = false; }
            Log.LogInfo($"[Pickup] {g.dialogue?.character} picks you up");
        }

        private bool _pickupPending;

        private IEnumerator PickupLater(Giantess g, float delay)
        {
            if (_pickupPending) yield break;
            _pickupPending = true;
            yield return new WaitForSeconds(delay);
            float wait = Time.time + 8f;   // let a crouch / stomp finish first
            while (g != null && Crawling.Contains(g) && Time.time < wait) yield return null;
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
                if (ClimbActive) EnableClimbing(g);
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

        /// <summary>Footwear that leaves the top of her foot bare (heels, slippers, strapped sandals).</summary>
        private static bool OpenTopped(Giantess g)
        {
            try
            {
                var shoe = g.variation?.currentShoeVariation;
                if (shoe == null) return false;
                switch (shoe.shoeType)
                {
                    case ShoeType.HeelTall:
                    case ShoeType.NightHeels:
                    case ShoeType.Slippers:
                    case ShoeType.Geta:
                    case ShoeType.HeelGeta:
                    case ShoeType.Pokkuri:
                        return true;
                    default: return false;
                }
            }
            catch { return false; }
        }

        /// <summary>Akipo's foot-grab lines for what she wears on her feet (null = none for this footwear).</summary>
        private static Lines.Line[] FootwearLines(Giantess g, Lines.Set ls)
        {
            if (ls == null) return null;
            ShoeType t;
            try { var v = g.variation?.currentShoeVariation; if (v == null) return null; t = v.shoeType; } catch { return null; }
            Lines.Line[] Join(params Lines.Line[][] parts) { var l = new List<Lines.Line>(); foreach (var a in parts) if (a != null) l.AddRange(a); return l.Count > 0 ? l.ToArray() : null; }
            switch (t)
            {
                case ShoeType.Bare: return ls.FootBare;
                case ShoeType.Socks: return ls.FootSocks;
                case ShoeType.Slippers: return ls.FootSlippers;
                case ShoeType.HeelTall:
                case ShoeType.NightHeels: return Join(ls.FootHeels, ls.FootShoes);
                case ShoeType.HeelGeta: return Join(ls.FootHeels, ls.FootStrings, ls.FootShoes);
                case ShoeType.Geta:
                case ShoeType.Pokkuri: return Join(ls.FootStrings, ls.FootShoes);
                default: return ls.FootShoes;
            }
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



        private bool _legIsThigh, _legInStockings;

        /// <summary>The foot-grab lines come from the game's climbing scene: only while you climb her.</summary>
        private static bool IsClimbing() { var pl = PlayerDriver.LocalPlayer; return pl != null && pl.footGrabbed; }

        /// <summary>Climbing lines now and then only (they get repetitive): while climbing, with this chance.</summary>
        private static bool ClimbLineNow() => IsClimbing() && UnityEngine.Random.value < 0.3f;

        private Lines.Line[] LegLines(Lines.Set ls)
        {
            var l = new List<Lines.Line>(_legIsThigh ? ls.Thigh : ls.Shin);
            var extra = !ClimbLineNow() ? null : _legInStockings ? ls.LegStockings : ls.LegBare;
            if (extra != null) l.AddRange(extra);
            return l.ToArray();
        }

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
                : (leg && lvl < 3 && (_legIsThigh ? ls.Thigh : ls.Shin) != null) ? LegLines(ls)
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
                var worn = foot && lvl < 4 && ClimbLineNow() ? FootwearLines(g, ls) : null;
                if (worn != null) set = set.Concat(worn).ToArray();
                Say(g, set, true);
            }
            Log.LogInfo($"[Tickle] {ch} annoyance={m.annoyance:F1} level={lvl}");
            if (foot) MaybeStomp(g, lvl);
            m.madPokes = lvl >= 4 ? m.madPokes + 1 : 0;
            if (foot && _dropEnabled.Value && !_dropping && m.madPokes >= _dropAfter.Value && Plugin_Active.Poses.TryGetValue(g, out var tp) && tp == Pose.Free)
            {
                m.madPokes = 0;
                StartCoroutine(DropPlayer(g));
            }
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
        private const int TouchKey = 101, PatKey = 102, BoobKey = 103, TickleKey = 104, FootKey = 105, SlapKey = 106;

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
            else if (string.Equals(setting?.Trim(), "Slap", StringComparison.OrdinalIgnoreCase)) clip = _slap ??= MakeSlap();
            if (clip == null) { PlayGameSound(setting); return; }
            if (_touchAudio == null) { _touchAudio = gameObject.AddComponent<AudioSource>(); _touchAudio.playOnAwake = false; _touchAudio.spatialBlend = 0f; }
            _touchAudio.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            _touchAudio.PlayOneShot(clip, _touchVolume.Value);
        }

        private AudioClip _slap;

        /// <summary>A cartoon slap: a sharp burst of noise with a quick, slightly muffled decay.</summary>
        private static AudioClip MakeSlap()
        {
            const int rate = 44100;
            int n = (int)(rate * 0.18f);
            var data = new float[n];
            var rnd = new System.Random(7);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.55f;            // takes the hiss off
                lp2 += (lp - lp2) * 0.35f;              // body of the hand
                float crack = Mathf.Exp(-t * 90f);      // the sharp "crack"
                float body = Mathf.Exp(-t * 28f) * 0.6f;
                float thump = Mathf.Sin(2f * Mathf.PI * 140f * t) * Mathf.Exp(-t * 35f) * 0.35f;
                data[i] = Mathf.Clamp(lp * crack + lp2 * body + thump, -1f, 1f) * 0.9f;
            }
            var clip = AudioClip.Create("LFPokeSlap", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
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
            bool quit = !PunishMode && _quitEnabled.Value && _drops >= _quitAfter.Value;
            var em = g.expressionManager;
            em.eyeOverride.SetEvent("mad eyes");
            em.mouthOverride.SetEvent("mad mouth");
            var dm = DialogueManager.Instance;
            var lsd = Lines.For(g.dialogue != null ? g.dialogue.character : CharacterType.None);
            if (dm != null && lsd != null)
            {
                dm.InterruptCurrentDialogue();
                var set = quit || (PunishMode && _drops >= _quitAfter.Value) ? lsd.Quit : lsd.Drop;
                var line = PickLine(g, set);
                dm.AddDialgoue(new DialogueExpressionAndText(g.dialogue.character, line.expression, line.key));
                Log.LogInfo($"[Talk] {line.key}");
            }
            bool standing = PunishMode && Plugin_Active.Poses.TryGetValue(g, out var dropPose) && dropPose == Pose.Free;
            Log.LogInfo($"[Drop] #{_drops}{(quit ? " -> quitting the game" : standing ? " -> punished right here" : " -> back to the lobby")}");
            if (standing)
            {
                yield return new WaitForSeconds(1.5f);
                _dropping = false;
                _moods.Remove(g); _lastLevel.Remove(g);
                StartCoroutine(Punish(g.dialogue != null ? g.dialogue.character : CharacterType.None, _drops >= _quitAfter.Value));
                yield break;
            }
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
            if (PunishMode)
            {
                _punishBy = g.dialogue != null ? g.dialogue.character : CharacterType.None;
                _punishShrink = _drops >= _quitAfter.Value;
                _punishAfter = Time.realtimeSinceStartup + 1f;
            }
            if (SceneLoader.Instance != null) SceneLoader.Instance.LoadScene(0);
        }

        // ---------------------------------------------------------------- punishment (instead of closing the game)

        private float _punishReadySince = -1f;

        /// <summary>The lobby is back: wait until you stand in it for a moment, then she punishes you.</summary>
        private void TryStartPunish()
        {
            var setup = SetupManager.Instance; var player = PlayerDriver.LocalPlayer;
            bool ready = setup != null && setup.isLobby && !SceneLoader.InSceneTransition && player != null && !player.killed;
            if (!ready) { _punishReadySince = -1f; return; }
            if (_punishReadySince < 0f) { _punishReadySince = Time.time; return; }
            if (Time.time - _punishReadySince < 2f) return;
            _punishReadySince = -1f;
            var who = _punishBy; bool shrink = _punishShrink;
            _punishBy = CharacterType.None;
            StartCoroutine(Punish(who, shrink));
        }

        private IEnumerator Punish(CharacterType who, bool shrink)
        {
            _punishing = true;
            var player = PlayerDriver.LocalPlayer;
            Giantess g = FindObjectsOfType<Giantess>().FirstOrDefault(x => x.dialogue != null && x.dialogue.character == who);
            Log.LogInfo($"[Punish] {who}: stomp{(shrink ? " + shrink" : "")}");
            if (g != null)
            {
                try { g.expressionManager.eyeOverride.SetEvent("mad eyes"); g.expressionManager.mouthOverride.SetEvent("mad mouth"); } catch { }
                if (CanAnimate(g)) StartCoroutine(TeaseStomp(g));   // Akipo: her angry stomp
            }
            StartCoroutine(DangerFlash(player, shrink ? 3f : 2f));
            if (shrink && player != null && !_shrunk) StartCoroutine(ShrinkPlayer(player));
            else if (shrink && player != null && _shrunk && _launchUp.Value > 0f)
            {
                // Already tiny: she sends you flying (the game's own knock-back stun), and you stay tiny longer.
                ExtendShrink();
                yield return new WaitForSeconds(0.4f);   // with her stomp
                try
                {
                    Vector3 toHer = g != null ? (g.transform.position - player.transform.position) : player.transform.forward;
                    if (toHer.sqrMagnitude < 0.0001f) toHer = player.transform.forward;
                    PlayTouch(SlapKey, _slapSound.Value);
                    try { player.shieldAnimator.Flash(1f); } catch { }   // the game's hit flash + sound (no damage)
                    player.SendFlyingStun(toHer.normalized, player.controller.m_Jumping, 1f, _launchUp.Value, pierceShrunkImmunity: true);
                    Log.LogInfo("[Punish] already tiny: sent flying");
                }
                catch (Exception e) { Log.LogWarning("[Punish] launch: " + e.Message); }
            }
            yield return new WaitForSeconds(2f);
            try { if (g != null) { g.expressionManager.eyeOverride.ClearEvent(); g.expressionManager.mouthOverride.ClearEvent(); } } catch { }
            _punishing = false;
        }

        /// <summary>The game's danger effect (red edges, heartbeat) for a moment – like Akipo's angry foot massage.</summary>
        private IEnumerator DangerFlash(PlayerDriver player, float seconds)
        {
            if (player == null) yield break;
            float v0 = player.dangerVisualRatio, a0 = player.dangerAudioRatio; int d0 = player.inDangerVisual;
            try { player.dangerVisualRatio = 0.35f; player.dangerAudioRatio = 0.35f; player.inDangerVisual = 1; } catch { }
            yield return new WaitForSeconds(seconds);
            if (player == null) yield break;
            try { player.dangerVisualRatio = v0; player.dangerAudioRatio = a0; player.inDangerVisual = d0; } catch { }
        }

        /// <summary>
        /// She shrinks you for a while – the game's own temporary shrink effect (like the dust / turret shrink):
        /// it shrinks you, slows you down and grows you back with its own sounds, stacking with any perk/enchant.
        /// </summary>
        private PlayerBuffTemplates.TemporaryShrinkBuff _shrinkBuff;

        /// <summary>Shrunk and touching someone again: the shrink lasts until you leave everyone alone for its full time.</summary>
        private void ExtendShrink()
        {
            var b = _shrinkBuff;
            if (b == null || !_shrunk) return;
            float dur = Mathf.Max(60f, _shrinkSeconds.Value);
            float left = b.duration - b.elapsed;
            if (left > 1.2f && left < dur) b.duration = b.elapsed + dur;   // not while already growing back
        }

        private IEnumerator ShrinkPlayer(PlayerDriver player)
        {
            _shrunk = true;
            ShrunkNow = true;
            float dur = Mathf.Max(60f, _shrinkSeconds.Value);
            try
            {
                _shrinkBuff = new PlayerBuffTemplates.TemporaryShrinkBuff("lfpoke_punish_shrink", dur, Mathf.Clamp(_shrinkAmount.Value, 0.1f, 0.95f), increaseSpeed: false);
                player.buffManager.AddBuff(_shrinkBuff);
                player.playerSFX.PlayShrinkParticlesSFX();
                Log.LogInfo($"[Punish] you are tiny for {dur:F0} s");
            }
            catch (Exception e) { Log.LogWarning("[Punish] shrink: " + e.Message); _shrunk = false; ShrunkNow = false; yield break; }
            // Tiny: the character controller's step height must shrink too, or Unity refuses to move you at all
            // ("Step Offset must be less or equal to scaled Height + scaled Radius * 2").
            var cc = player.controller != null ? player.controller.m_CharacterController : null;
            float step0 = cc != null ? cc.stepOffset : 0f;
            // Until the shrink is over: touching anyone keeps you tiny (see ExtendShrink).
            while (player != null && _shrinkBuff != null && _shrinkBuff.elapsed < _shrinkBuff.duration)
            {
                if (cc != null)
                {
                    float sc = Mathf.Max(0.01f, player.playerScale.GetModifiedValue);
                    float max = (cc.height + cc.radius * 2f) * sc * 0.9f;
                    cc.stepOffset = Mathf.Min(step0 * sc, max);
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.2f);
            if (cc != null) cc.stepOffset = step0;
            _shrinkBuff = null;
            _shrunk = false;
            ShrunkNow = false;
            Log.LogInfo("[Punish] you are back to normal size");
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
            var folders = Enumerable.Range(0, Reactions.Length).Select(i => (i, "level" + i)).Append((HeadpatKey, "headpat")).Append((TouchKey, "touch")).Append((PatKey, "pat")).Append((BoobKey, "boob")).Append((TickleKey, "tickle")).Append((FootKey, "foot")).Append((SlapKey, "slap"));
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
        /// <summary>
        /// Lobby: "E" is shared by the pickup (CallGiantess) and climbing (GrabFoot). While you aim at a
        /// grabbable spot on her foot/leg, the pickup steps aside so "E" climbs; aim elsewhere to be picked up.
        /// </summary>
        /// <summary>Lobby, not climbing yet: the mod handles "E" on her foot (tap = climb, hold = pickup).</summary>
        /// <summary>
        /// Lobby, aiming at her foot: one steady prompt for the shared "E" (tap = climb, hold = call/pickup)
        /// instead of the climb and call prompts taking turns.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(InteractionDriver), nameof(InteractionDriver.GetTargetItemPrompt))]
        private static void FootPrompt(ref string __result)
        {
            try
            {
                var setup = SetupManager.Instance; var player = PlayerDriver.LocalPlayer;
                if (setup == null || !setup.isLobby || player == null || player.footGrabbed || !PokeRunner.ClimbActive) return;
                if (PokeRunner.AimedFoot == null || Time.time - PokeRunner.AimedFootTime > 0.3f) return;
                string key = PokeRunner.AimedFoot.GetMainInteractKeyString(player);
                __result = Localization.CreateButtonPromptText(key, "GAMEPLAY_FOOT_GRAB") + "\n" + Localization.CreateButtonHoldPromptText(key, "GAMEPLAY_CALL");
            }
            catch { }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GrabFoot), nameof(GrabFoot.CanMainInteract))]
        private static void TakeOverFirstGrab(GrabFoot __instance, PlayerDriver player, ref bool __result)
        {
            var setupL = SetupManager.Instance;
            if (setupL != null && setupL.isLobby && !PokeRunner.ClimbActive) { __result = false; return; }   // no perk: no climbing in the lobby
            if (!__result || !PokeRunner.LobbyClimbing || PokeRunner.ClimbBypass) return;
            var setup = SetupManager.Instance;
            if (setup == null || !setup.isLobby || player == null || player.footGrabbed) return;
            PokeRunner.AimedFoot = __instance;
            PokeRunner.AimedFootFrame = Time.frameCount;
            PokeRunner.AimedFootTime = Time.time;
            __result = false;
        }

        /// <summary>
        /// The lobby computes an invalid (NaN) growth scale. The game's growth uses it for the end size, which
        /// made her size NaN (crash). Keep it valid in the lobby: 10% of her size per stage step, bar size 100.
        /// </summary>
        /// <summary>
        /// When the game's growth finishes in the lobby, the AI would pick a random next behaviour (walking etc.,
        /// which need level paths the lobby doesn't have). Go straight back to her lobby behaviour instead.
        /// </summary>
        /// <summary>The game re-applies her collider strength now and then: keep full strength while she is grown.</summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ArmatureCollider), nameof(ArmatureCollider.SetCapsuleProperties))]
        private static void KeepGrownStrength(ArmatureCollider __instance, Giantess giantess)
        {
            if (!PokeRunner._destroyWhenGrown || !PokeRunner.IsGrown(giantess) || !PokeRunner.FootClimberPerk()) return;
            __instance.movementCapsuleProps = new ArmatureCollider.CapsuleProperties(1f, 1f, 5f, 5f);
            __instance.stepCapsuleProps = new ArmatureCollider.CapsuleProperties(4f, 20f, 5f, 1f);
            __instance.stepLightCapsuleProps = new ArmatureCollider.CapsuleProperties(2f, 10f, 5f, 1f);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GiantessAI), nameof(GiantessAI.OnCustomAnimatorIK))]
        private static bool CrawlInPlace(GiantessAI __instance)
        {
            var g = __instance.giantess;
            if (g == null || !PokeRunner.Crawling.Contains(g)) return true;
            try { if (PokeRunner.CrawlSpot.TryGetValue(g, out var spot)) g.SetPosition(spot); } catch { }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GiantessAI), nameof(GiantessAI.TransitionUpdate))]
        private static bool BackToLobbyAfterGrow(GiantessAI __instance)
        {
            try
            {
                var g = __instance.giantess;
                var cur = __instance.currentBehaviour;
                if (g == null || !(cur is GiantessBehaviourGrow) || __instance.nextBehaviourQueue.behaviour != null) return true;
                if (SetupManager.Instance == null || !SetupManager.Instance.isLobby) return true;
                if (!PokeRunner.LobbyBehaviour.TryGetValue(g, out var lobby) || lobby == null) return true;
                if (!cur.IsDone(__instance)) return true;
                cur.BehaviourEnd(__instance);
                __instance.previousBehaviour = cur;
                __instance.currentBehaviour = lobby;
                lobby.BehaviourStart(__instance);
                return false;
            }
            catch { return true; }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Giantess), nameof(Giantess.UpdateGiantessValues))]
        private static void KeepGrowthValuesValid(Giantess __instance)
        {
            var setup = SetupManager.Instance;
            if (setup == null || !setup.isLobby || __instance == null) return;
            if (float.IsNaN(__instance.scale) || __instance.scale <= 0f) return;
            if (float.IsNaN(__instance.scaleExponentialScale) || __instance.scaleExponentialScale <= 0f)
                __instance.scaleExponentialScale = __instance.scale * 0.1f;
            var gr = __instance.growth;
            if (gr != null && !(gr.growthMaxCharge > 0f)) gr.growthMaxCharge = 100f;
        }

        /// <summary>Lobby: every climbing step on her foot/leg fills her growth bar a bit.</summary>
        [HarmonyPostfix, HarmonyPatch(typeof(GrabFoot), nameof(GrabFoot.OnMainInteract))]
        private static void ClimbStep(GrabFoot __instance)
        {
            var setup = SetupManager.Instance;
            if (setup == null || !setup.isLobby || __instance == null || __instance.giantess == null) return;
            // (climbing itself doesn't charge; poking her thigh while climbing does)
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CallGiantess), nameof(CallGiantess.CanMainInteract))]
        private static bool LetClimbWin(CallGiantess __instance, PlayerDriver player, ref bool __result)
        {
            var gg = __instance.giantessAI != null ? __instance.giantessAI.giantess : null;
            if (PokeRunner.IsGrown(gg) || (gg != null && PokeRunner.Crawling.Contains(gg))) { __result = false; return false; }   // grown: pickup would break the lobby
            if (!PokeRunner.ClimbActive || PokeRunner.LetClimbWinPaused) return true;
            try
            {
                var setup = SetupManager.Instance;
                if (setup == null || !setup.isLobby || player == null || player.footGrabbed) return true;
                var g = __instance.giantessAI != null ? __instance.giantessAI.giantess : null;
                if (g == null) return true;
                PokeRunner.ClimbBypass = true;
                try
                {
                    foreach (var gf in g.GetComponentsInChildren<GrabFoot>())
                        if (gf != null && gf.isActiveAndEnabled && gf.CanMainInteract(player)) { __result = false; return false; }
                }
                finally { PokeRunner.ClimbBypass = false; }
            }
            catch { }
            return true;
        }

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
