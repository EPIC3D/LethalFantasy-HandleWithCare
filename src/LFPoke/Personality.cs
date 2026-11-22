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

        internal static readonly Personality Akipo = new Personality
        {
            facePerPoke = 1f, chestPerPoke = 2.5f, patCalm = 4f,
            chestFace1 = ("embarrased eyes", "embarrased mouth"),
        };

        // Flirty: chest pokes stay smug/teasing for a long time.
        internal static readonly Personality Red = new Personality
        {
            facePerPoke = 0.6f, chestPerPoke = 1f, patCalm = 3f,
            chestLevels = new[] { 2, 2, 2, 3, 4 },
            chestFace2 = ("smug eyes", "smug mouth"),
            likesChestWhenFond = true,
        };

        internal static readonly Personality Blue = new Personality
        {
            facePerPoke = 1.3f, chestPerPoke = 2f, patCalm = 2f,
            chestFace1 = ("shock eyes", "shy mouth"),
        };

        internal static readonly Personality Default = new Personality();

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
