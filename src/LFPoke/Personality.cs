using UnityEngine;

namespace LFPoke
{
    /// <summary>
    /// How much each character tolerates: Akipo is young and shy, Red is extroverted,
    /// vain and flirty (patient, teasing), Blue is introverted (sensitive).
    /// Affection (the game's own progress) makes everyone more patient.
    /// </summary>
    internal class Personality
    {
        public float facePerPoke = 1f;      // annoyance per face poke
        public float chestPerPoke = 2f;     // annoyance per chest poke
        public float patCalm = 3f;          // annoyance removed per headpat
        public int[] chestLevels = { 1, 1, 3, 3, 4 };   // mood level -> chest reaction level
        public (string eye, string mouth)? chestFace1;  // face for chest level 1 (null = mood face)
        public (string eye, string mouth)? chestFace2;  // face for chest level 2
        public bool likesChestWhenFond;                 // high affection: chest pokes please her (Red)
        public int[] faceLevels = { 0, 1, 2, 3, 4 };    // mood level -> face reaction level
        public int[] buttLevels = { 1, 2, 3, 4, 4 };    // mood level -> butt reaction level
        public float buttGain = 3f;                     // annoyance per butt pinch
        public bool buttFlirty;                         // level 2 = flirty (Red, briefly)
        public bool affectionOnlyAtMax;                 // affection changes nothing until it is maxed out (Blue)
        public Personality whenMaxed;                   // softer personality at max affection
        public float tickleGain = 0.5f;                 // annoyance per tickle (soles, tummy)
        public int tickleAnnoyedAt = 3;                 // mood level from which tickling stops being funny

        internal static readonly Personality Akipo = new Personality
        {
            // Neutral (0) on the tolerance scale.
            facePerPoke = 1f, chestPerPoke = 2f, patCalm = 3f,
            chestFace1 = ("embarrased eyes", "embarrased mouth"),
        };

        // Flirty: chest pokes stay smug/teasing for a long time.
        internal static readonly Personality Red = new Personality
        {
            // +0.5: tolerant and forgiving.
            facePerPoke = 0.8f, chestPerPoke = 1.5f, patCalm = 3.5f,
            tickleGain = 0.4f, tickleAnnoyedAt = 4,
            chestLevels = new[] { 2, 2, 2, 3, 4 },
            chestFace2 = ("smug eyes", "smug mouth"),
            likesChestWhenFond = true,
            buttLevels = new[] { 2, 2, 3, 4, 4 }, buttGain = 2.5f, buttFlirty = true,   // flirty, but only briefly
        };

        // Blue at max affection: she lets you closer – giggles sometimes, tolerates a little tickling.
        private static readonly Personality BlueMaxed = new Personality
        {
            facePerPoke = 1.15f, chestPerPoke = 2f, patCalm = 3.5f,
            faceLevels = new[] { 0, 1, 2, 3, 4 },
            tickleGain = 1f, tickleAnnoyedAt = 2,
            buttGain = 3f,
            chestFace1 = ("shock eyes", "shy mouth"),
        };

        internal static readonly Personality Blue = new Personality
        {
            affectionOnlyAtMax = true,
            whenMaxed = BlueMaxed,
            // -0.5: Blue doesn't like being touched anywhere – only headpats.
            facePerPoke = 1.4f, chestPerPoke = 2.5f, patCalm = 3.5f,
            faceLevels = new[] { 1, 2, 3, 4, 4 },          // never giggles at a poke
            tickleGain = 1.25f, tickleAnnoyedAt = 0,        // tickling is never funny to her
            buttGain = 3.5f,
            chestFace1 = ("shock eyes", "shy mouth"),
        };

        internal static readonly Personality Default = new Personality();

        /// <summary>The personality in effect for this affection (0..1).</summary>
        internal static Personality For(CharacterType c, float affection)
        {
            var p = For(c);
            return p.whenMaxed != null && affection >= 0.999f ? p.whenMaxed : p;
        }

        internal static Personality For(CharacterType c)
        {
            switch (c)
            {
                case CharacterType.Akipo: return Akipo;
                case CharacterType.RedTwin: return Red;
                case CharacterType.BlueTwin: return Blue;
                default: return Default;
            }
        }

        /// <summary>The game's affection for this character as 0..1.</summary>
        internal static float Affection(CharacterType c)
        {
            try
            {
                if (c == CharacterType.Akipo)
                {
                    int max = SettingsPref.GetMaxAffectionLevel(CharacterLevelType.Akipo);
                    return Mathf.Clamp01(SettingsPref.GetAffectionLevelPref(CharacterLevelType.Akipo) / (float)Mathf.Max(1, max));
                }
                // Twins: only levels 0..1 exist, so use the progress toward level 1 as well.
                float best = 0f;
                foreach (var t in new[] { CharacterLevelType.Twins, CharacterLevelType.RedTwin, CharacterLevelType.BlueTwin })
                {
                    int lvl = SettingsPref.GetAffectionLevelPref(t);
                    int max = SettingsPref.GetMaxAffectionLevel(t);
                    float v = lvl >= max ? 1f : SettingsPref.GetAffectionProgressPref(t) / (float)SettingsPref.GetNextAffectionProgressRequired(t, lvl);
                    best = Mathf.Max(best, Mathf.Clamp01(v));
                }
                return best;
            }
            catch { return 0f; }
        }
    }
}
