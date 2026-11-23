using System.Collections.Generic;

namespace LFPoke
{
    /// <summary>Existing game dialogue lines used as poke reactions, per character.
    /// Only translation keys are stored here; the game shows its own (localized) text.</summary>
    internal static class Lines
    {
        internal struct Line
        {
            public string key;
            public DialogueExpression expression;
            public Line(string k, DialogueExpression e) { key = k; expression = e; }
        }

        internal class Set
        {
            public Line[][] Face;                 // annoyance level 0..4
            public Dictionary<int, Line[]> Chest; // chest reaction levels 1..4 (Red: 2 = flirty)
            public Line[] HeadpatHappy, HeadpatGrudging, Drop, Quit;
            public Line[] ChestLiked;             // chest poke she enjoys (high affection, Red)
            public Line[] Tickle;                 // soles / tummy
            public Line[] Eye, Mouth, Butt;       // poke in the eye, boop on the lips, butt pinch
            public Line[] Feet;                   // foot jokes, mixed into sole touches
            public Line[] Thigh, Shin;            // leg pokes (Akipo's shrine-blessing reactions)
        }

        private static Line L(string key, DialogueExpression e) => new Line(key, e);

        internal static Set For(CharacterType c)
        {
            switch (c)
            {
                case CharacterType.Akipo: return Akipo;
                case CharacterType.RedTwin: return Red;
                case CharacterType.BlueTwin: return Blue;
                default: return null;
            }
        }

        // ---------------------------------------------------------------- Akipo
        private static readonly Line[] AkipoMad = new[] {
            L("AKIPO_DIALOGUE_EXPLOSION_ATTACK_FIRST_1_1", DialogueExpression.Angry),
            L("AKIPO_DIALOGUE_EXPLOSION_WARNING_REPEAT_1_1", DialogueExpression.Angry),
            L("AKIPO_DIALOGUE_ENDGAME_UNFAIR_REACH_2_1", DialogueExpression.Angry),
            L("AKIPO_DIALOGUE_BLESSING_CROTCH_REPEAT_4", DialogueExpression.Angry),
            L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_1", DialogueExpression.Screaming),
            L("AKIPO_DIALOGUE_ENDGAME_UNFAIR_REACH_3_1", DialogueExpression.Angry),
            L("AKIPO_DIALOGUE_IDLE_STARING_12", DialogueExpression.Frown),
            L("AKIPO_DIALOGUE_RECOIL_START_REPEAT_2", DialogueExpression.Angry),
        };
        private static readonly Set Akipo = new Set
        {
            Thigh = new[] {
                L("AKIPO_DIALOGUE_BLESSING_THIGH_1_1", DialogueExpression.NeutralTilt),
                L("AKIPO_DIALOGUE_BLESSING_THIGH_1_2", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_BLESSING_THIGH_2", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_BLESSING_THIGH_3_1", DialogueExpression.Smug),
                L("AKIPO_DIALOGUE_BLESSING_THIGH_3_2", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_BLESSING_THIGH_4", DialogueExpression.Glowing),
            },
            Shin = new[] {
                L("AKIPO_DIALOGUE_BLESSING_SHIN_3_1", DialogueExpression.NeutralTilt),
                L("AKIPO_DIALOGUE_BLESSING_SHIN_4_1", DialogueExpression.Smug),
                L("AKIPO_DIALOGUE_BLESSING_SHIN_4_2", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_BLESSING_SHIN_2_2", DialogueExpression.Glowing),
            },
            Feet = new[] {
                L("AKIPO_DIALOGUE_BLESSING_FOOT_4", DialogueExpression.Smug),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_5", DialogueExpression.Embarrased),
                L("AKIPO_DIALOGUE_FOOTGRAB_GRAB_FOOT_3", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_BLESSING_FOOT_2", DialogueExpression.Glowing),
            },
            Face = new[]
            {
                new[] {
                    L("AKIPO_DIALOGUE_FOOTGRAB_GRAB_FOOT_1", DialogueExpression.Laugh),
                    L("AKIPO_DIALOGUE_GROW_END_31_1", DialogueExpression.Laugh),
                    L("AKIPO_DIALOGUE_FOOTGRAB_GRAB_BAREFOOT_3", DialogueExpression.Laugh),
                    L("AKIPO_DIALOGUE_BLESSING_FOOT_1_2", DialogueExpression.Glowing),
                    L("AKIPO_DIALOGUE_IDLE_STARING_8", DialogueExpression.Glowing),
                    L("AKIPO_DIALOGUE_EVENT_PET_WHY_NICE_WARM_3", DialogueExpression.Laugh),
                    L("AKIPO_DIALOGUE_EVENT_MOMMOMENT_REWARD_2", DialogueExpression.Glowing),
                    L("AKIPO_DIALOGUE_FOOTGRAB_LETGO_STAM_7", DialogueExpression.Glowing),
                },
                new[] {
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_INTRO_K_1", DialogueExpression.Embarrased),
                    L("AKIPO_DIALOGUE_EVENT_PET_RESPONSE_MOMMY_MID_2", DialogueExpression.Embarrased),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_IDLE_HIGHTENSION_5", DialogueExpression.Embarrased),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_IDLE_HIGHTENSION_2", DialogueExpression.Embarrased),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_IDLE_HIGHTENSION_4", DialogueExpression.Embarrased),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_IDLE_LOWTENSION_1", DialogueExpression.Embarrased),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_Q_P", DialogueExpression.Embarrased),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_INTRO_K_2", DialogueExpression.Embarrased),
                },
                new[] {
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_D_1", DialogueExpression.Angry),
                    L("AKIPO_DIALOGUE_ATTACK_STOMP_DEATH_REPEAT_6_1", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_ANGRY_REPEAT_4", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_ATTACK_EXPLOSION_START_2_1", DialogueExpression.Shocked),
                    L("AKIPO_DIALOGUE_RECOIL_START_REPEAT_1", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_B_1", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_WALKING_WARN_SURVIVE_2_2", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_ENDGAME_UNFAIR_5_1", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_A_1", DialogueExpression.Embarrased),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_C_2", DialogueExpression.Frown),
                },
                new[] {
                    L("AKIPO_DIALOGUE_ENDGAME_UNFAIR_REACH_1_1", DialogueExpression.Angry),
                    L("AKIPO_DIALOGUE_EXPLOSION_ATTACK_REPEAT_1_1", DialogueExpression.Angry),
                    L("AKIPO_DIALOGUE_ENDGAME_UNFAIR_2_2", DialogueExpression.Angry),
                    L("AKIPO_DIALOGUE_RECOIL_START_REPEAT_10", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_ANGRY_REPEAT_1", DialogueExpression.Angry),
                    L("AKIPO_DIALOGUE_ENDGAME_UNFAIR_1_2", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_D_2", DialogueExpression.Angry),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_B_3", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_ANGRY_REPEAT_2", DialogueExpression.Angry),
                },
                AkipoMad,
            },
            Chest = new Dictionary<int, Line[]>
            {
                [1] = new[] {
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_8_1", DialogueExpression.EmbarrasedMax),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_IDLE_HIGHTENSION_3", DialogueExpression.EmbarrasedMax),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_6_1", DialogueExpression.EmbarrasedMax),
                    L("AKIPO_DIALOGUE_BLESSING_CHEST_1", DialogueExpression.EmbarrasedMax),
                    L("AKIPO_DIALOGUE_BLESSING_CHEST_3_1", DialogueExpression.Embarrased),
                    L("AKIPO_DIALOGUE_BLESSING_CHEST_4", DialogueExpression.Smug),
                    L("AKIPO_DIALOGUE_EVENT_HUG_INTRO_CARETAKER_2", DialogueExpression.EmbarrasedMax),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_FAULT_1", DialogueExpression.EmbarrasedMax),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_1_1", DialogueExpression.EmbarrasedMax),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_2_2", DialogueExpression.EmbarrasedMax),
                },
                [3] = new[] {
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_D_1", DialogueExpression.Angry),
                    L("AKIPO_DIALOGUE_EXPLOSION_ATTACK_FIRST_2_1", DialogueExpression.Angry),
                    L("AKIPO_DIALOGUE_ANGRY_REPEAT_4", DialogueExpression.Angry),
                    L("AKIPO_DIALOGUE_IDLE_STARING_2", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_EVENT_PET_RESPONSE_MOMMY_QUEST_WHY_3", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_EVENT_PET_RESPONSE_MOMMY_MID_1", DialogueExpression.Frown),
                    L("AKIPO_DIALOGUE_BLESSING_CROTCH_REPEAT_2", DialogueExpression.Angry),
                },
                [4] = new[] {
                    L("AKIPO_DIALOGUE_BLESSING_CROTCH_1", DialogueExpression.Screaming),
                    L("AKIPO_DIALOGUE_BLESSING_CROTCH_3", DialogueExpression.Screaming),
                    L("AKIPO_DIALOGUE_BLESSING_CROTCH_2", DialogueExpression.Screaming),
                    L("AKIPO_DIALOGUE_BLESSING_CROTCH_REPEAT_1", DialogueExpression.Screaming),
                    L("AKIPO_DIALOGUE_BLESSING_CROTCH_REPEAT_3", DialogueExpression.Screaming),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_E_1", DialogueExpression.Screaming),
                    L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_E_3", DialogueExpression.Frown),
                },
            },
            Tickle = new[] {
                L("AKIPO_DIALOGUE_FOOTGRAB_GRAB_FOOT_1", DialogueExpression.Laugh),
                L("AKIPO_DIALOGUE_GROW_END_31_1", DialogueExpression.Laugh),
                L("AKIPO_DIALOGUE_FOOTGRAB_GRAB_BAREFOOT_3", DialogueExpression.Laugh),
                L("AKIPO_DIALOGUE_FOOTGRAB_GRAB_OTHERLEG_5", DialogueExpression.Laugh),
                L("AKIPO_DIALOGUE_FOOTGRAB_GRAB_FOOT_2", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_EVENT_HUG_BOTHERING_WAYYOUARE_E4", DialogueExpression.Laugh),
                L("AKIPO_DIALOGUE_EVENT_PET_WHY_NICE_WARM_3", DialogueExpression.Laugh),
            },
            HeadpatHappy = new[] {
                L("AKIPO_DIALOGUE_BLESSING_HEAD_3_1", DialogueExpression.Happy),
                L("AKIPO_DIALOGUE_EVENT_LAP_RESTART_5", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_BLESSING_SHIN_3_2", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_BLESSING_HEAD_1", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_BLESSING_HEAD_2", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_BLESSING_HEAD_3_2", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_IDLE_1", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_CRUCIBLE_GRAB_2", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_RESULT_PERFECT_2", DialogueExpression.Happy),
                L("AKIPO_DIALOGUE_EVENT_PET_INTRO_F_5", DialogueExpression.OpenSmile),
                L("AKIPO_DIALOGUE_EVENT_LAP_INTRO_GODDESS_5", DialogueExpression.Glowing),
            },
            HeadpatGrudging = new[] {
                L("AKIPO_DIALOGUE_ATTACK_ESCAPED_END_1", DialogueExpression.Frustrated),
                L("AKIPO_DIALOGUE_ATTACK_ESCAPED_END_2", DialogueExpression.Frustrated),
                L("AKIPO_DIALOGUE_ATTACK_ESCAPED_END_3", DialogueExpression.Frustrated),
                L("AKIPO_DIALOGUE_ATTACK_ESCAPED_END_4", DialogueExpression.Frustrated),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_NEUTRAL_1", DialogueExpression.Glowing),
            },
            Eye = new[] {
                L("AKIPO_DIALOGUE_RECOIL_EXPLOSION_1", DialogueExpression.Hurt),
                L("AKIPO_DIALOGUE_RECOIL_EXPLOSION_2", DialogueExpression.Hurt),
                L("AKIPO_DIALOGUE_RECOIL_EXPLOSION_3", DialogueExpression.Hurt),
                L("AKIPO_DIALOGUE_RECOIL_EXPLOSION_4", DialogueExpression.Hurt),
                L("AKIPO_DIALOGUE_RECOIL_EXPLOSION_5", DialogueExpression.Hurt),
                L("AKIPO_DIALOGUE_FOOTGRAB_GRAB_BAREFOOT_1", DialogueExpression.Hurt),
            },
            Mouth = new[] {
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_INTRO_K_1", DialogueExpression.Embarrased),
                L("AKIPO_DIALOGUE_EVENT_LAP_SIZE_MOTHER_1", DialogueExpression.Shocked),
                L("AKIPO_DIALOGUE_EVENT_LAP_RAMBLE_77", DialogueExpression.Shocked),
                L("AKIPO_DIALOGUE_IDLE_STARING_8", DialogueExpression.Glowing),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_Q_P", DialogueExpression.Embarrased),
            },
            Drop = new[] {
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_11", DialogueExpression.Sad),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_B_2", DialogueExpression.Frown),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_13", DialogueExpression.Frown),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_E_6", DialogueExpression.Frown),
                L("AKIPO_DIALOGUE_ATTACK_STOMP_DEATH_REPEAT_4_1", DialogueExpression.Frown),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_C_3", DialogueExpression.Sad),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_10", DialogueExpression.Cry),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_F_7", DialogueExpression.Sad),
            },
            Quit = new[] {
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_E_1", DialogueExpression.Screaming),
                L("AKIPO_DIALOGUE_ENDGAME_UNFAIR_REACH_3_2", DialogueExpression.Screaming),
                L("AKIPO_DIALOGUE_ANGRY_FIRST_1", DialogueExpression.Angry),
                L("AKIPO_DIALOGUE_ANGRY_FIRST_3", DialogueExpression.Angry),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_E_3", DialogueExpression.Angry),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_F_2", DialogueExpression.Sad),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_F_1", DialogueExpression.Sad),
                L("AKIPO_DIALOGUE_EVENT_FMASSAGE_TENSIONBROKE_ANGER_F_8", DialogueExpression.Sad),
            },
        };

        // ---------------------------------------------------------------- Red
        private static readonly Line[] RedMad = new[] {
            L("REDTWIN_DIALOGUE_ENDGAME_UNFAIR_REACH_1_1", DialogueExpression.Screaming),
            L("REDTWIN_DIALOGUE_ANGRY_REPEAT_4", DialogueExpression.Screaming),
            L("REDTWIN_DIALOGUE_ANGRY_FIRST_1", DialogueExpression.Angry),
            L("REDTWIN_DIALOGUE_ANGRY_FIRST_3", DialogueExpression.Screaming),
            L("REDTWIN_DIALOGUE_ANGRY_REPEAT_1", DialogueExpression.Angry),
            L("BLUETWIN_DIALOGUE_ENDGAME_UNFAIR_REACH_1_3_R", DialogueExpression.Angry),
            L("REDTWIN_DIALOGUE_EXPLOSION_WARNING_FIRST_1_1", DialogueExpression.Screaming),
            L("REDTWIN_DIALOGUE_RECOIL_START_REPEAT_4", DialogueExpression.Screaming),
            L("REDTWIN_DIALOGUE_STATUE_ATTACK_REPEAT_3_1", DialogueExpression.Screaming),
            L("REDTWIN_DIALOGUE_STATUE_ATTACK_FIRST_1_2", DialogueExpression.Angry),
            L("REDTWIN_DIALOGUE_ANGRY_REPEAT_3", DialogueExpression.Angry),
        };
        private static readonly Set Red = new Set
        {
            Feet = new[] {
                L("REDTWIN_DIALOGUE_ENDGAME_CHASE_POOR_RESULT_4_2", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_HOUSE_DEATH_SUCCESS_5_4", DialogueExpression.OpenSmile),
                L("REDTWIN_DIALOGUE_HOUSE_SURVIVED_BARELY_REPEAT_1_1", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_WALKING_WARN_12", DialogueExpression.Happy),
                L("REDTWIN_DIALOGUE_GROW_START2_11", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_WALKING_ACCIDENT_DEATH_5_1", DialogueExpression.Smug),
            },
            Face = new[]
            {
                new[] {
                    L("REDTWIN_DIALOGUE_TITLE_RANDOM_11_2", DialogueExpression.Laugh),
                    L("BLUETWIN_DIALOGUE_ATTACK_STOMP_DODGE_4_F", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_ATTACK_STOMP_TOOSMALL_2_F", DialogueExpression.Laugh),
                    L("BLUETWIN_DIALOGUE_ATTACK_STOMP_SURVIVE_4_F", DialogueExpression.Smug),
                    L("REDTWIN_DIALOGUE_WALKING_WARN_STOMP_2", DialogueExpression.Smug),
                    L("REDTWIN_DIALOGUE_HOUSE_START_2_1", DialogueExpression.OpenSmile),
                    L("REDTWIN_DIALOGUE_IDLE_SHIELD_LOW_1", DialogueExpression.OpenSmile),
                },
                new[] {
                    L("REDTWIN_DIALOGUE_GROW_END_13_1", DialogueExpression.Embarrased),
                    L("BLUETWIN_DIALOGUE_LEVELSTART_MEADOW_1_R", DialogueExpression.Embarrased),
                    L("REDTWIN_DIALOGUE_WALKING_ACCIDENT_STOMP_5", DialogueExpression.Glowing),
                    L("REDTWIN_DIALOGUE_HOUSE_ESCAPE_EARLY_REPEAT_1_2", DialogueExpression.Smug),
                    L("REDTWIN_DIALOGUE_HOUSE_SURVIVED_NORMAL_REPEAT_4_2", DialogueExpression.Embarrased),
                    L("BLUETWIN_DIALOGUE_WALKING_ACCIDENT_SURVIVE_3_F", DialogueExpression.Smug),
                    L("REDTWIN_DIALOGUE_IDLE_STARING_8", DialogueExpression.Happy),
                    L("REDTWIN_DIALOGUE_ATTACK_STOMP_DODGE_1_3", DialogueExpression.NeutralTilt),
                },
                new[] {
                    L("REDTWIN_DIALOGUE_EXPLOSION_ATTACK_FIRST_1_1", DialogueExpression.Frown),
                    L("REDTWIN_DIALOGUE_ENDGAME_CHASE_POOR_RESULT_3_1", DialogueExpression.Frustrated),
                    L("REDTWIN_DIALOGUE_HOUSE_DEATH_SUCCESS_4_1", DialogueExpression.Frustrated),
                    L("REDTWIN_DIALOGUE_RECOIL_START_REPEAT_3", DialogueExpression.Shocked),
                    L("REDTWIN_DIALOGUE_IDLE_STARING_9", DialogueExpression.Frown),
                    L("BLUETWIN_DIALOGUE_LEVELSTART_PLAINS_REPEAT10_1_R", DialogueExpression.Smug),
                    L("REDTWIN_DIALOGUE_ATTACK_STOMP_TOOSMALL_2_1", DialogueExpression.Frustrated),
                },
                new[] {
                    L("REDTWIN_DIALOGUE_EXPLOSION_WARNING_REPEAT_1_3", DialogueExpression.Frustrated),
                    L("REDTWIN_DIALOGUE_ENDGAME_UNFAIR_2_2", DialogueExpression.Angry),
                    L("REDTWIN_DIALOGUE_STATUE_WARNING_REPEAT_1_3", DialogueExpression.Frustrated),
                    L("REDTWIN_DIALOGUE_EXPLOSION_WARNING_FIRST_2_2", DialogueExpression.Angry),
                    L("REDTWIN_DIALOGUE_STATUE_WARNING_REPEAT_4_2", DialogueExpression.Frown),
                    L("REDTWIN_DIALOGUE_EXPLOSION_ATTACK_FIRST_1_2", DialogueExpression.Angry),
                    L("REDTWIN_DIALOGUE_EXPLOSION_ATTACK_REPEAT_2_1", DialogueExpression.Screaming),
                    L("REDTWIN_DIALOGUE_ANGRY_REPEAT_2", DialogueExpression.Screaming),
                    L("BLUETWIN_DIALOGUE_LARGEMODE_DEATH_3_R", DialogueExpression.Frown),
                },
                RedMad,
            },
            Chest = new Dictionary<int, Line[]>
            {
                [1] = new[] {
                    L("REDTWIN_DIALOGUE_GROW_END_13_1", DialogueExpression.EmbarrasedMax),
                    L("REDTWIN_DIALOGUE_GROW_END_26_1", DialogueExpression.EmbarrasedMax),
                    L("REDTWIN_DIALOGUE_HOUSE_SURVIVED_NORMAL_REPEAT_4_2", DialogueExpression.EmbarrasedMax),
                },
                [2] = new[] {
                    L("REDTWIN_DIALOGUE_TITLE_RANDOM_11_2", DialogueExpression.Laugh),
                    L("BLUETWIN_DIALOGUE_LEVELSTART_GENERIC_5_R", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_9_R", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_11_R", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_GROW_END_27_3_R", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_HOUSE_IDLE_6_R", DialogueExpression.Smug),
                    L("REDTWIN_DIALOGUE_IDLE_STARING_2", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_HOUSE_IDLE_9_R", DialogueExpression.Smug),
                    L("REDTWIN_DIALOGUE_GROW_END_15_3", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_15_2_R", DialogueExpression.Smug),
                    L("REDTWIN_DIALOGUE_HOUSE_START_REPEAT_4_2", DialogueExpression.Happy),
                },
                [3] = new[] {
                    L("REDTWIN_DIALOGUE_EXPLOSION_ATTACK_FIRST_1_1", DialogueExpression.Frown),
                    L("REDTWIN_DIALOGUE_ANGRY_REPEAT_2", DialogueExpression.Screaming),
                    L("REDTWIN_DIALOGUE_ENDGAME_UNFAIR_2_2", DialogueExpression.Angry),
                    L("REDTWIN_DIALOGUE_EXPLOSION_ATTACK_FIRST_2_1", DialogueExpression.Angry),
                    L("REDTWIN_DIALOGUE_STATUE_WARNING_FIRST_3_1", DialogueExpression.Angry),
                    L("REDTWIN_DIALOGUE_RECOIL_START_REPEAT_5", DialogueExpression.Angry),
                    L("REDTWIN_DIALOGUE_RECOIL_START_REPEAT_1", DialogueExpression.Angry),
                },
                [4] = RedMad,
            },
            Tickle = new[] {
                L("REDTWIN_DIALOGUE_GROW_END_28_4", DialogueExpression.Laugh),
                L("REDTWIN_DIALOGUE_LEVELSTART_DAWN_FIRST_2_3", DialogueExpression.Laugh),
                L("REDTWIN_DIALOGUE_TITLE_RANDOM_11_2", DialogueExpression.Laugh),
                L("BLUETWIN_DIALOGUE_ATTACK_STOMP_TOOSMALL_2_F", DialogueExpression.Laugh),
                L("REDTWIN_DIALOGUE_WALKING_ACCIDENT_DEATH_5_4", DialogueExpression.Laugh),
            },
            HeadpatHappy = new[] {
                L("REDTWIN_DIALOGUE_ATTACK_STOMP_DODGE_2_2", DialogueExpression.Happy),
                L("REDTWIN_DIALOGUE_CRUCIBLE_GRAB_5", DialogueExpression.Happy),
                L("BLUETWIN_DIALOGUE_LEVELSTART_DAWN_REPEAT100_1_R", DialogueExpression.Happy),
                L("BLUETWIN_DIALOGUE_ENDGAME_CHASE_END_RESULT_4_3_R", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_HOUSE_SURVIVED_NORMAL_2_1", DialogueExpression.Glowing),
                L("REDTWIN_DIALOGUE_GROW_END_26_3", DialogueExpression.Glowing),
                L("REDTWIN_DIALOGUE_WALKING_ACCIDENT_SURVIVE_3_2", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_GROW_END_7_2", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_ENDGAME_CHASE_START_2_1", DialogueExpression.Smug),
            },
            HeadpatGrudging = new[] {
                L("REDTWIN_DIALOGUE_ATTACK_EARLY_MISS_1_3", DialogueExpression.Frustrated),
                L("REDTWIN_DIALOGUE_ATTACK_ESCAPED_START_2_2", DialogueExpression.Frustrated),
                L("REDTWIN_DIALOGUE_STATUE_WARNING_REPEAT_3_2", DialogueExpression.Sad),
                L("REDTWIN_DIALOGUE_STATUE_WARNING_REPEAT_2_1", DialogueExpression.Frown),
                L("REDTWIN_DIALOGUE_ATTACK_STOMP_DODGE_1_3", DialogueExpression.NeutralTilt),
                L("REDTWIN_DIALOGUE_ENDGAME_CHASE_GOOD_RESULT_2_2", DialogueExpression.OpenSmile),
            },
            Eye = new[] {
                L("REDTWIN_DIALOGUE_ATTACK_STOMP_TOOSMALL_1_1", DialogueExpression.Hurt),
                L("REDTWIN_DIALOGUE_STATUE_WARNING_FIRST_1_2", DialogueExpression.Angry),
                L("REDTWIN_DIALOGUE_STATUE_WARNING_FIRST_3_1", DialogueExpression.Hurt),
                L("REDTWIN_DIALOGUE_EXPLOSION_ATTACK_FIRST_2_2", DialogueExpression.Hurt),
                L("REDTWIN_DIALOGUE_STATUE_WARNING_FIRST_2_1", DialogueExpression.Angry),
            },
            Mouth = new[] {
                L("REDTWIN_DIALOGUE_HOUSE_ARRIVE_REPEAT_1_2", DialogueExpression.Glowing),
                L("REDTWIN_DIALOGUE_HOUSE_ARRIVE_REPEAT_4_3", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_HOUSE_ARRIVE_2_3", DialogueExpression.Smug),
                L("TWIN_INTRO_DIALOGUE_FOLLOW_1_R", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_HOUSE_IDLE_1", DialogueExpression.Smug),
            },
            Butt = new[] {
                L("REDTWIN_DIALOGUE_GROW_END_15_2", DialogueExpression.Glowing),
                L("REDTWIN_DIALOGUE_WALKING_WARN_STOMP_2", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_HOUSE_ESCAPE_EARLY_REPEAT_1_2", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_ATTACK_STOMP_DODGE_4_2", DialogueExpression.Smug),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_9_R", DialogueExpression.Smug),
            },
            ChestLiked = new[] {
                L("REDTWIN_DIALOGUE_ATTACK_STOMP_CAUGHT_4", DialogueExpression.Glowing),
                L("REDTWIN_DIALOGUE_ATTACK_STOMP_START_2", DialogueExpression.Smug),
                L("BLUETWIN_DIALOGUE_LEVELSTART_GENERIC_5_R", DialogueExpression.Smug),
                L("BLUETWIN_DIALOGUE_GROW_END_9_R", DialogueExpression.Glowing),
                L("BLUETWIN_DIALOGUE_LEVELSTART_MEADOW_1_R", DialogueExpression.Glowing),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_11_R", DialogueExpression.Smug),
                L("BLUETWIN_DIALOGUE_GROW_END_27_3_R", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_GROW_END_2_2", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_ENDGAME_START_QUICK_5_2", DialogueExpression.Smug),
                L("REDTWIN_DIALOGUE_WALKING_ACCIDENT_DEATH_5_2", DialogueExpression.Glowing),
                L("REDTWIN_DIALOGUE_HOUSE_SURVIVED_BARELY_1_2", DialogueExpression.Happy),
                L("REDTWIN_DIALOGUE_IDLE_STARING_1", DialogueExpression.OpenSmile),
            },
            Drop = new[] {
                L("REDTWIN_DIALOGUE_ENDGAME_TERRIBLE_RESULT_3_3", DialogueExpression.Frown),
                L("REDTWIN_DIALOGUE_ENDGAME_TERRIBLE_RESULT_2_3", DialogueExpression.Frustrated),
                L("REDTWIN_DIALOGUE_ENDGAME_GOHOME_2_3", DialogueExpression.Angry),
                L("REDTWIN_DIALOGUE_HOUSE_DEATH_FAILURE_5_4", DialogueExpression.Frown),
                L("REDTWIN_DIALOGUE_ENDGAME_TERRIBLE_RESULT_2_2", DialogueExpression.Frown),
            },
            Quit = new[] {
                L("REDTWIN_DIALOGUE_ANGRY_FIRST_2", DialogueExpression.Angry),
                L("REDTWIN_DIALOGUE_ENDGAME_UNFAIR_REACH_1_2", DialogueExpression.Angry),
                L("REDTWIN_DIALOGUE_ENDGAME_UNFAIR_REACH_2_3", DialogueExpression.Frown),
                L("REDTWIN_DIALOGUE_STATUE_ATTACK_REPEAT_2_2", DialogueExpression.Angry),
            },
        };

        // ---------------------------------------------------------------- Blue
        private static readonly Line[] BlueMad = new[] {
            L("BLUETWIN_DIALOGUE_STATUE_WARNING_FIRST_1_1", DialogueExpression.Screaming),
            L("BLUETWIN_DIALOGUE_RECOIL_START_REPEAT_2", DialogueExpression.Screaming),
            L("BLUETWIN_DIALOGUE_ANGRY_FIRST_3", DialogueExpression.Angry),
            L("BLUETWIN_DIALOGUE_ANGRY_REPEAT_4", DialogueExpression.Angry),
            L("BLUETWIN_DIALOGUE_RECOIL_START_REPEAT_5", DialogueExpression.Screaming),
            L("BLUETWIN_DIALOGUE_RECOIL_START_REPEAT_3", DialogueExpression.Angry),
            L("BLUETWIN_DIALOGUE_ANGRY_REPEAT_3", DialogueExpression.Angry),
            L("BLUETWIN_DIALOGUE_ANGRY_FIRST_2", DialogueExpression.Angry),
            L("BLUETWIN_DIALOGUE_ANGRY_REPEAT_1", DialogueExpression.Frown),
            L("BLUETWIN_DIALOGUE_EXPLOSION_WARNING_FIRST_2_1", DialogueExpression.Screaming),
        };
        private static readonly Set Blue = new Set
        {
            Feet = new[] {
                L("BLUETWIN_DIALOGUE_GROW_START2_4", DialogueExpression.Frown),
                L("BLUETWIN_DIALOGUE_IDLE_STARING_3", DialogueExpression.Smug),
                L("BLUETWIN_DIALOGUE_LARGEMODE_DEATH_4_1", DialogueExpression.Neutral),
            },
            Face = new[]
            {
                new[] {
                    L("BLUETWIN_DIALOGUE_IDLE_INSIDEBUILDING_4_1", DialogueExpression.NeutralTilt),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_17_3", DialogueExpression.Happy),
                    L("BLUETWIN_DIALOGUE_RECOIL_START_1", DialogueExpression.Neutral),
                    L("BLUETWIN_DIALOGUE_RECOIL_START_2", DialogueExpression.Neutral),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_21_4", DialogueExpression.OpenSmile),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_14_4", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_IDLE_STARING_7", DialogueExpression.Smug),
                },
                new[] {
                    L("BLUETWIN_DIALOGUE_ENDGAME_CHASE_GREAT_RESULT_2_1", DialogueExpression.Embarrased),
                    L("BLUETWIN_DIALOGUE_GROW_END_26_3", DialogueExpression.Embarrased),
                    L("BLUETWIN_DIALOGUE_ATTACK_ESCAPED_START_3_2", DialogueExpression.Shocked),
                    L("BLUETWIN_DIALOGUE_RECOIL_START_3", DialogueExpression.Frustrated),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_18_4", DialogueExpression.Embarrased),
                    L("BLUETWIN_DIALOGUE_IDLE_STARING_1", DialogueExpression.OpenSmile),
                },
                new[] {
                    L("BLUETWIN_DIALOGUE_EXPLOSION_ATTACK_FIRST_2_1", DialogueExpression.Frustrated),
                    L("BLUETWIN_DIALOGUE_ATTACK_EARLY_MISS_3_2", DialogueExpression.Frustrated),
                    L("BLUETWIN_DIALOGUE_WALKING_ACCIDENT_STOMP_3", DialogueExpression.Shocked),
                    L("BLUETWIN_DIALOGUE_STATUE_ATTACK_FIRST_1_1", DialogueExpression.Frown),
                    L("BLUETWIN_DIALOGUE_LARGEMODE_DEATH_1_1", DialogueExpression.Frustrated),
                    L("BLUETWIN_DIALOGUE_WALKING_ACCIDENT_DEATH_1_1", DialogueExpression.Frown),
                    L("BLUETWIN_DIALOGUE_RECOIL_START_REPEAT_4", DialogueExpression.Frustrated),
                    L("BLUETWIN_DIALOGUE_WALKING_WARN_LAUNCH_2", DialogueExpression.Frown),
                    L("BLUETWIN_DIALOGUE_WALKING_WARN_SURVIVE_3_2", DialogueExpression.NeutralTilt),
                },
                new[] {
                    L("BLUETWIN_DIALOGUE_ENDGAME_UNFAIR_2_3", DialogueExpression.Frustrated),
                    L("BLUETWIN_DIALOGUE_ANGRY_REPEAT_2", DialogueExpression.Angry),
                    L("BLUETWIN_DIALOGUE_RECOIL_START_REPEAT_1", DialogueExpression.Angry),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_15_3", DialogueExpression.Angry),
                    L("BLUETWIN_DIALOGUE_WALKING_ACCIDENT_DEATH_5_1", DialogueExpression.Frown),
                    L("BLUETWIN_DIALOGUE_ATTACK_EARLY_MISS_3_3", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_LARGEMODE_DEATH_3_3", DialogueExpression.Frown),
                },
                BlueMad,
            },
            Chest = new Dictionary<int, Line[]>
            {
                [1] = new[] {
                    L("BLUETWIN_DIALOGUE_ATTACK_EARLY_MISS_4_1", DialogueExpression.EmbarrasedMax),
                    L("BLUETWIN_DIALOGUE_EXPLOSION_WARNING_FIRST_1_1", DialogueExpression.Shocked),
                    L("BLUETWIN_DIALOGUE_ENDGAME_CHASE_GREAT_RESULT_2_1", DialogueExpression.EmbarrasedMax),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_18_4", DialogueExpression.Embarrased),
                    L("BLUETWIN_DIALOGUE_GROW_END_26_3", DialogueExpression.Embarrased),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_18_3", DialogueExpression.Embarrased),
                    L("BLUETWIN_DIALOGUE_RECOIL_START_3", DialogueExpression.Frustrated),
                },
                [3] = new[] {
                    L("BLUETWIN_DIALOGUE_EXPLOSION_ATTACK_FIRST_2_1", DialogueExpression.Frustrated),
                    L("BLUETWIN_DIALOGUE_ANGRY_REPEAT_2", DialogueExpression.Angry),
                    L("BLUETWIN_DIALOGUE_CRUCIBLE_WALK_7_1", DialogueExpression.Frown),
                    L("BLUETWIN_DIALOGUE_ENDGAME_CHASE_START_5_1", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_WALKING_WARN_LAUNCH_2", DialogueExpression.Frown),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_15_3", DialogueExpression.Angry),
                    L("BLUETWIN_DIALOGUE_WALKING_WARN_SURVIVE_4_1", DialogueExpression.Neutral),
                },
                [4] = BlueMad,
            },
            Tickle = new[] {
                L("BLUETWIN_DIALOGUE_ATTACK_STOMP_TOOSMALL_2_1", DialogueExpression.Frown),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_23_R", DialogueExpression.Frustrated),
                L("BLUETWIN_DIALOGUE_ATTACK_EARLY_MISS_3_2", DialogueExpression.Frustrated),
                L("BLUETWIN_DIALOGUE_WALKING_ACCIDENT_DEATH_4_4", DialogueExpression.Screaming),
                L("BLUETWIN_DIALOGUE_ATTACK_EARLY_MISS_1_2", DialogueExpression.Frown),
            },
            HeadpatHappy = new[] {
                L("BLUETWIN_DIALOGUE_ENDGAME_CHASE_PERFECT_RESULT_3_4", DialogueExpression.Smug),
                L("BLUETWIN_DIALOGUE_ENDGAME_CHASE_GREAT_RESULT_3_1", DialogueExpression.Happy),
                L("BLUETWIN_DIALOGUE_ENDGAME_CHASE_PERFECT_RESULT_2_1", DialogueExpression.Happy),
                L("BLUETWIN_DIALOGUE_QUESTION_WHYPLACE_A_1_4", DialogueExpression.Happy),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_21_4", DialogueExpression.OpenSmile),
                L("BLUETWIN_DIALOGUE_WALKING_WARN_DEATH_4_2", DialogueExpression.Happy),
                L("BLUETWIN_DIALOGUE_QUESTION_STEPPED_A_1_10", DialogueExpression.OpenSmile),
            },
            HeadpatGrudging = new[] {
                L("BLUETWIN_DIALOGUE_ATTACK_STOMP_SURVIVE_2_1", DialogueExpression.Neutral),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_17_3", DialogueExpression.Embarrased),
                L("BLUETWIN_DIALOGUE_ATTACK_EARLY_MISS_2_3", DialogueExpression.OpenSmile),
                L("BLUETWIN_DIALOGUE_QUESTION_REVIVE_A_1_8", DialogueExpression.Happy),
                L("BLUETWIN_DIALOGUE_GROW_END_27_4", DialogueExpression.Neutral),
            },
            Eye = new[] {
                L("BLUETWIN_DIALOGUE_EXPLOSION_WARNING_FIRST_1_1", DialogueExpression.Hurt),
                L("BLUETWIN_DIALOGUE_GROW_START1_13", DialogueExpression.Hurt),
                L("BLUETWIN_DIALOGUE_ENDGAME_TERRIBLE_RESULT_1_1", DialogueExpression.Frustrated),
            },
            Mouth = new[] {
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_19_3", DialogueExpression.Embarrased),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_18_3", DialogueExpression.Embarrased),
                L("BLUETWIN_DIALOGUE_LEVELSTART_GENERIC_6_4", DialogueExpression.Embarrased),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_18_4", DialogueExpression.Embarrased),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_20_3", DialogueExpression.Frustrated),
            },
            Butt = new[] {
                L("BLUETWIN_DIALOGUE_ATTACK_EARLY_MISS_4_1", DialogueExpression.Shocked),
                L("BLUETWIN_DIALOGUE_EXPLOSION_WARNING_FIRST_1_1", DialogueExpression.Shocked),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_18_4", DialogueExpression.Embarrased),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_15_3", DialogueExpression.Angry),
                L("BLUETWIN_DIALOGUE_WALKING_WARN_LAUNCH_2", DialogueExpression.Frown),
            },
            Drop = new[] {
                L("BLUETWIN_DIALOGUE_ATTACK_EARLY_MISS_4_2", DialogueExpression.Frown),
                L("BLUETWIN_DIALOGUE_ANGRY_FIRST_1", DialogueExpression.Frown),
                L("BLUETWIN_DIALOGUE_IDLE_STARING_1", DialogueExpression.OpenSmile),
                L("BLUETWIN_DIALOGUE_WALKING_WARN_SURVIVE_3_2", DialogueExpression.NeutralTilt),
                L("BLUETWIN_DIALOGUE_LARGEMODE_REPEAT_7", DialogueExpression.OpenSmile),
            },
            Quit = new[] {
                L("BLUETWIN_DIALOGUE_ATTACK_STOMP_START_1", DialogueExpression.Frown),
                L("BLUETWIN_DIALOGUE_ENDGAME_UNFAIR_REACH_1_2", DialogueExpression.Angry),
                L("BLUETWIN_DIALOGUE_ENDGAME_UNFAIR_REACH_2_2", DialogueExpression.Screaming),
            },
        };
    }
}
