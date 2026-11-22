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
            public Dictionary<int, Line[]> Chest; // breast pokes use levels 1, 3, 4
            public Line[] HeadpatHappy, HeadpatGrudging, Drop, Quit;
            public Line[] ChestLiked;             // chest poke she enjoys (high affection, Red)
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
        private const string A = "AKIPO_DIALOGUE_";
        private static readonly Line[] AkipoMad =
        {
            L(A + "EXPLOSION_ATTACK_FIRST_1_1", DialogueExpression.Angry),
            L(A + "EXPLOSION_WARNING_REPEAT_1_1", DialogueExpression.Angry),
            L(A + "EVENT_FMASSAGE_TENSIONBROKE_ANGER_E_1", DialogueExpression.Screaming),
            L(A + "ANGRY_FIRST_1", DialogueExpression.Angry),
            L(A + "RECOIL_START_REPEAT_2", DialogueExpression.Angry),
            L(A + "ENDGAME_UNFAIR_REACH_2_1", DialogueExpression.Angry),
            L(A + "BLESSING_CROTCH_REPEAT_4", DialogueExpression.Angry),
        };
        private static readonly Set Akipo = new Set
        {
            Face = new[]
            {
                new[] {
                    L(A + "FOOTGRAB_GRAB_FOOT_1", DialogueExpression.Laugh),
                    L(A + "GROW_END_31_1", DialogueExpression.Laugh),
                    L(A + "FOOTGRAB_GRAB_BAREFOOT_3", DialogueExpression.Laugh),
                    L(A + "BLESSING_FOOT_1_2", DialogueExpression.Glowing),
                },
                new[] {
                    L(A + "EVENT_FMASSAGE_INTRO_K_1", DialogueExpression.Embarrased),
                    L(A + "EVENT_PET_RESPONSE_MOMMY_MID_2", DialogueExpression.Embarrased),
                    L(A + "FOOTGRAB_GRAB_OTHERLEG_5", DialogueExpression.Laugh),
                    L(A + "TITLE_RANDOM_21_2", DialogueExpression.Embarrased),
                    L(A + "EVENT_FMASSAGE_IDLE_HIGHTENSION_5", DialogueExpression.Embarrased),
                    L(A + "BLESSING_GENERIC_5_1", DialogueExpression.NeutralTilt),
                },
                new[] {
                    L(A + "EVENT_FMASSAGE_TENSIONBROKE_ANGER_D_1", DialogueExpression.Angry),
                    L(A + "ATTACK_EXPLOSION_START_2_1", DialogueExpression.Shocked),
                    L(A + "ATTACK_STOMP_DEATH_REPEAT_6_1", DialogueExpression.Frown),
                    L(A + "ANGRY_REPEAT_4", DialogueExpression.Frown),
                    L(A + "ATTACK_EXPLOSION_START_5_1", DialogueExpression.Shocked),
                },
                new[] {
                    L(A + "ENDGAME_UNFAIR_REACH_1_1", DialogueExpression.Angry),
                    L(A + "EXPLOSION_ATTACK_REPEAT_1_1", DialogueExpression.Angry),
                    L(A + "ENDGAME_UNFAIR_2_2", DialogueExpression.Angry),
                    L(A + "ATTACK_STOMP_DEATH_REPEAT_4_1", DialogueExpression.Frown),
                    L(A + "RECOIL_START_REPEAT_10", DialogueExpression.Frown),
                    L(A + "ANGRY_REPEAT_1", DialogueExpression.Angry),
                },
                AkipoMad,
            },
            Chest = new Dictionary<int, Line[]>
            {
                [1] = new[] {
                    L(A + "ATTACK_EXPLOSION_START_1_1", DialogueExpression.EmbarrasedMax),
                    L(A + "EVENT_FMASSAGE_TENSIONBROKE_8_1", DialogueExpression.EmbarrasedMax),
                    L(A + "EVENT_FMASSAGE_IDLE_HIGHTENSION_3", DialogueExpression.EmbarrasedMax),
                    L(A + "EVENT_FMASSAGE_TENSIONBROKE_6_1", DialogueExpression.EmbarrasedMax),
                    L(A + "BLESSING_CHEST_1", DialogueExpression.EmbarrasedMax),
                    L(A + "BLESSING_CHEST_2", DialogueExpression.EmbarrasedMax),
                },
                [3] = new[] {
                    L(A + "EVENT_FMASSAGE_TENSIONBROKE_ANGER_D_1", DialogueExpression.Angry),
                    L(A + "EXPLOSION_ATTACK_FIRST_2_1", DialogueExpression.Angry),
                    L(A + "ENDGAME_UNFAIR_REACH_2_1", DialogueExpression.Frown),
                    L(A + "ANGRY_REPEAT_4", DialogueExpression.Angry),
                    L(A + "BLESSING_CHEST_4", DialogueExpression.Frown),
                    L(A + "BLESSING_CROTCH_REPEAT_2", DialogueExpression.Angry),
                },
                [4] = new[] {
                    L(A + "BLESSING_CROTCH_1", DialogueExpression.Screaming),
                    L(A + "BLESSING_CROTCH_3", DialogueExpression.Screaming),
                    L(A + "EXPLOSION_ATTACK_FIRST_1_1", DialogueExpression.Angry),
                    L(A + "EVENT_FMASSAGE_TENSIONBROKE_ANGER_E_1", DialogueExpression.Screaming),
                    L(A + "BLESSING_CROTCH_REPEAT_4", DialogueExpression.Angry),
                },
            },
            HeadpatHappy = new[] {
                L(A + "BLESSING_HEAD_3_1", DialogueExpression.Happy),
                L(A + "EVENT_LAP_RESTART_5", DialogueExpression.Glowing),
                L(A + "BLESSING_SHIN_3_2", DialogueExpression.Glowing),
                L(A + "EVENT_FMASSAGE_IDLE_LOWTENSION_4_1", DialogueExpression.Embarrased),
                L(A + "BLESSING_HEAD_1", DialogueExpression.Glowing),
                L(A + "BLESSING_HEAD_2", DialogueExpression.Glowing),
                L(A + "BLESSING_HEAD_3_2", DialogueExpression.Glowing),
            },
            HeadpatGrudging = new[] {
                L(A + "ATTACK_ESCAPED_END_1", DialogueExpression.Frustrated),
                L(A + "ATTACK_ESCAPED_END_2", DialogueExpression.Frustrated),
            },
            Drop = new[] {
                L(A + "EVENT_FMASSAGE_TENSIONBROKE_ANGER_E_1", DialogueExpression.Screaming),
                L(A + "ANGRY_REPEAT_3", DialogueExpression.Angry),
            },
            Quit = new[] {
                L(A + "ANGRY_FIRST_3", DialogueExpression.Angry),
            },
        };

        // ---------------------------------------------------------------- Red twin (bratty)
        private const string R = "REDTWIN_DIALOGUE_";
        private static readonly Line[] RedMad =
        {
            L(R + "ENDGAME_UNFAIR_REACH_1_1", DialogueExpression.Screaming),
            L(R + "ANGRY_REPEAT_4", DialogueExpression.Screaming),
            L(R + "ANGRY_FIRST_1", DialogueExpression.Angry),
            L(R + "ANGRY_FIRST_3", DialogueExpression.Screaming),
            L(R + "ANGRY_REPEAT_1", DialogueExpression.Angry),
            L("BLUETWIN_DIALOGUE_ENDGAME_UNFAIR_REACH_1_3_R", DialogueExpression.Angry),
        };
        private static readonly Set Red = new Set
        {
            Face = new[]
            {
                new[] {
                    L(R + "TITLE_RANDOM_11_2", DialogueExpression.Laugh),
                    L(R + "ATTACK_STOMP_CAUGHT_1", DialogueExpression.Glowing),
                    L("BLUETWIN_DIALOGUE_ATTACK_STOMP_DODGE_4_F", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_ATTACK_STOMP_TOOSMALL_2_F", DialogueExpression.Laugh),
                },
                new[] {
                    L(R + "GROW_END_13_1", DialogueExpression.Embarrased),
                    L("BLUETWIN_DIALOGUE_LEVELSTART_GENERIC_10_4_R", DialogueExpression.Glowing),
                    L("BLUETWIN_DIALOGUE_LEVELSTART_MEADOW_1_R", DialogueExpression.Embarrased),
                },
                new[] {
                    L(R + "EXPLOSION_ATTACK_FIRST_1_1", DialogueExpression.Frown),
                    L(R + "EXPLOSION_WARNING_FIRST_1_1", DialogueExpression.Screaming),
                    L(R + "ATTACK_STOMP_TOOSMALL_2_1", DialogueExpression.Frustrated),
                    L("BLUETWIN_DIALOGUE_ATTACK_STOMP_DEATH_2_F", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_LEVELSTART_PLAINS_REPEAT10_1_R", DialogueExpression.Smug),
                },
                new[] {
                    L(R + "EXPLOSION_WARNING_REPEAT_1_3", DialogueExpression.Frustrated),
                    L(R + "ENDGAME_UNFAIR_1_2", DialogueExpression.Frown),
                    L(R + "EXPLOSION_ATTACK_REPEAT_2_1", DialogueExpression.Screaming),
                    L(R + "ANGRY_REPEAT_2", DialogueExpression.Screaming),
                    L("BLUETWIN_DIALOGUE_LARGEMODE_DEATH_3_R", DialogueExpression.Frown),
                    L("BLUETWIN_DIALOGUE_ENDGAME_START_QUICK_3_6_R", DialogueExpression.Frown),
                },
                RedMad,
            },
            Chest = new Dictionary<int, Line[]>
            {
                [2] = new[] {
                    L(R + "ATTACK_STOMP_CAUGHT_1", DialogueExpression.Smug),
                    L(R + "TITLE_RANDOM_11_2", DialogueExpression.Laugh),
                    L(R + "ATTACK_STOMP_SURVIVE_1_3", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_LEVELSTART_GENERIC_5_R", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_9_R", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_TITLE_RANDOM_11_R", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_GROW_END_27_3_R", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_HOUSE_IDLE_6_R", DialogueExpression.Smug),
                    L("BLUETWIN_DIALOGUE_ENDGAME_CHASE_END_RESULT_4_3_R", DialogueExpression.Happy),
                },
                [1] = new[] {
                    L(R + "GROW_END_13_1", DialogueExpression.EmbarrasedMax),
                    L(R + "GROW_END_26_1", DialogueExpression.EmbarrasedMax),
                    L("BLUETWIN_DIALOGUE_LEVELSTART_GENERIC_10_4_R", DialogueExpression.EmbarrasedMax),
                },
                [3] = new[] {
                    L(R + "EXPLOSION_ATTACK_FIRST_1_1", DialogueExpression.Frown),
                    L(R + "ANGRY_REPEAT_2", DialogueExpression.Screaming),
                },
                [4] = RedMad,
            },
            HeadpatHappy = new[] {
                L(R + "ATTACK_STOMP_DODGE_2_2", DialogueExpression.Happy),
                L(R + "CRUCIBLE_GRAB_5", DialogueExpression.Happy),
                L("BLUETWIN_DIALOGUE_LEVELSTART_DAWN_REPEAT100_1_R", DialogueExpression.Happy),
                L("BLUETWIN_DIALOGUE_ENDGAME_CHASE_END_RESULT_4_3_R", DialogueExpression.Smug),
            },
            HeadpatGrudging = new[] {
                L(R + "ATTACK_EARLY_MISS_1_3", DialogueExpression.Frustrated),
                L(R + "ATTACK_ESCAPED_START_2_2", DialogueExpression.Frustrated),
            },
            Drop = new[] {
                L(R + "ENDGAME_TERRIBLE_RESULT_3_3", DialogueExpression.Frown),
                L(R + "ENDGAME_UNFAIR_REACH_1_1", DialogueExpression.Screaming),
            },
            ChestLiked = new[] {
                L(R + "ATTACK_STOMP_CAUGHT_4", DialogueExpression.Glowing),
                L(R + "ATTACK_STOMP_START_2", DialogueExpression.Smug),
                L("BLUETWIN_DIALOGUE_LEVELSTART_GENERIC_5_R", DialogueExpression.Smug),
                L(R + "ATTACK_STOMP_CAUGHT_1", DialogueExpression.Glowing),
                L("BLUETWIN_DIALOGUE_GROW_END_9_R", DialogueExpression.Glowing),
                L("BLUETWIN_DIALOGUE_LEVELSTART_MEADOW_1_R", DialogueExpression.Glowing),
                L("BLUETWIN_DIALOGUE_TITLE_RANDOM_11_R", DialogueExpression.Smug),
                L("BLUETWIN_DIALOGUE_GROW_END_27_3_R", DialogueExpression.Smug),
            },
            Quit = new[] {
                L(R + "ANGRY_FIRST_2", DialogueExpression.Angry),
            },
        };

        // ---------------------------------------------------------------- Blue twin (composed)
        private const string B = "BLUETWIN_DIALOGUE_";
        private static readonly Line[] BlueMad =
        {
            L(B + "STATUE_WARNING_FIRST_1_1", DialogueExpression.Screaming),
            L(B + "RECOIL_START_REPEAT_2", DialogueExpression.Screaming),
            L(B + "ANGRY_FIRST_3", DialogueExpression.Angry),
            L(B + "ANGRY_REPEAT_4", DialogueExpression.Angry),
            L(B + "ANGRY_RETURN_GENERIC_1", DialogueExpression.Angry),
            L(B + "ATTACK_STOMP_CAUGHT_3", DialogueExpression.Angry),
        };
        private static readonly Set Blue = new Set
        {
            Face = new[]
            {
                new[] {
                    L(B + "GROW_END_25_1", DialogueExpression.Glowing),
                    L(B + "ATTACK_STOMP_START_2", DialogueExpression.Smug),
                    L(B + "IDLE_INSIDEBUILDING_4_1", DialogueExpression.NeutralTilt),
                    L(B + "TITLE_RANDOM_17_3", DialogueExpression.Embarrased),
                },
                new[] {
                    L(B + "ENDGAME_CHASE_GREAT_RESULT_2_1", DialogueExpression.Embarrased),
                    L(B + "GROW_END_26_3", DialogueExpression.Embarrased),
                    L(B + "LEVELSTART_GENERIC_6_4", DialogueExpression.Embarrased),
                },
                new[] {
                    L(B + "EXPLOSION_ATTACK_FIRST_2_1", DialogueExpression.Frustrated),
                    L(B + "ATTACK_EARLY_MISS_3_2", DialogueExpression.Frustrated),
                    L(B + "EXPLOSION_WARNING_FIRST_2_1", DialogueExpression.Screaming),
                    L(B + "WALKING_ACCIDENT_STOMP_3", DialogueExpression.Shocked),
                    L(B + "STATUE_ATTACK_FIRST_1_1", DialogueExpression.Frown),
                },
                new[] {
                    L(B + "ENDGAME_UNFAIR_1_2", DialogueExpression.Frustrated),
                    L(B + "ENDGAME_UNFAIR_2_3", DialogueExpression.Frustrated),
                    L(B + "ANGRY_REPEAT_2", DialogueExpression.Angry),
                    L(B + "ATTACK_STOMP_TOOSMALL_2_1", DialogueExpression.Frown),
                },
                BlueMad,
            },
            Chest = new Dictionary<int, Line[]>
            {
                [1] = new[] {
                    L(B + "ATTACK_EARLY_MISS_4_1", DialogueExpression.EmbarrasedMax),
                    L(B + "EXPLOSION_WARNING_FIRST_1_1", DialogueExpression.Shocked),
                    L(B + "ENDGAME_CHASE_GREAT_RESULT_2_1", DialogueExpression.EmbarrasedMax),
                    L(B + "IDLE_INSIDEBUILDING_4_1", DialogueExpression.Embarrased),
                    L(B + "ENDGAME_CHASE_START_5_1", DialogueExpression.Smug),
                },
                [3] = new[] {
                    L(B + "EXPLOSION_ATTACK_FIRST_2_1", DialogueExpression.Frustrated),
                    L(B + "ANGRY_REPEAT_2", DialogueExpression.Angry),
                    L(B + "CRUCIBLE_WALK_7_1", DialogueExpression.Frown),
                },
                [4] = BlueMad,
            },
            HeadpatHappy = new[] {
                L(B + "ENDGAME_CHASE_PERFECT_RESULT_3_4", DialogueExpression.Smug),
                L(B + "ENDGAME_CHASE_GREAT_RESULT_3_1", DialogueExpression.Happy),
                L(B + "ENDGAME_CHASE_PERFECT_RESULT_2_1", DialogueExpression.Happy),
                L(B + "LEVELSTART_DUSK_2_4", DialogueExpression.Smug),
                L(B + "GROW_END_25_1", DialogueExpression.Glowing),
            },
            HeadpatGrudging = new[] {
                L(B + "ATTACK_EARLY_MISS_4_2", DialogueExpression.Frown),
                L(B + "ATTACK_STOMP_SURVIVE_2_1", DialogueExpression.Neutral),
                L(B + "TITLE_RANDOM_17_3", DialogueExpression.Embarrased),
            },
            Drop = new[] {
                L(B + "ENDGAME_UNFAIR_REACH_2_2", DialogueExpression.Screaming),
                L(B + "ANGRY_FIRST_1", DialogueExpression.Frown),
            },
            Quit = new[] {
                L(B + "ATTACK_STOMP_START_1", DialogueExpression.Frown),
            },
        };
    }
}
