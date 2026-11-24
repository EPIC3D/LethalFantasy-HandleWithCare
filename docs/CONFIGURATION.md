# Configuration

Everything can be tuned in `<LethalFantasy>\BepInEx\config\com.epicopy.lfpoke.cfg`
(created on the first start). The game reads it at startup, so restart the game after a change.

## Settings

| Section | Setting | Default | What it does |
| --- | --- | --- | --- |
| General | `Enabled` | `true` | Turn the mod on/off |
| Mood | `LevelThresholds` | `0,3,6,10,15` | Annoyance where each of the 5 moods starts |
| Mood | `CalmDownDelay` / `CalmDownPerSecond` | `6` / `0.3` | How fast she forgives you (seconds of quiet before she calms down, annoyance forgiven per second) |
| Face | `EyeSize` / `MouthSize` | `0.3` / `0.45` | Eye and mouth zones, relative to the distance between her eyes |
| Chest | `BreastBoneShare` | `0.3` | How much the breast (and butt) physics bones must move a spot for it to count (higher = smaller area) |
| Chest | `AnnoyancePerPoke` | `2` | Chest poke strength; scales each character's own value (`2` = as designed) |
| Chest | `JiggleForce` | `120` | Physics push strength (game footsteps use 150-225), `0` = off |
| Headpat | `CalmPerPat` | `3` | How much a pat calms her; scales each character's own value (`3` = as designed) |
| Feet | `Enabled` | `true` | Tickling soles and tummy |
| Free | `Enabled` / `Reach` | `true` / `0.75` | Touching in free roam, and your arm's reach |
| Free | `LobbyClimbing` / `HoldToPickupSeconds` | `true` / `1` | Some perks matter in the lobby too. Off = the lobby as the game has it |
| Free | `PickupKey` | `None` | Optional key that asks the nearest character to pick you up |
| Free | `PickupAtLevel` | `-1` | Mood (0 giggle … 4 mad) at which she picks you up in free roam; `-1` = never |
| Crouch | `Enabled` / `FootPokesPerCrouch` / `StaySeconds` | `true` / `5` / `10` | One of Akipo's reactions to sole pokes in free roam |
| Stomp | `Enabled` / `AtLevel` | `true` / `3` | Another one, from this mood on |
| Growth | `Enabled` | `true` | Some things are better found out by yourself. |
| Growth | `ThighPokesPerGrowth` / `MaxMeters` / `ShrinkDelaySeconds` / `DestroyWhenGrown` | `18` / `0` / `3` / `true` | …and these belong to it (`MaxMeters` `0` = the game's own size setting) |
| Personality | `AffectionPatience` | `1.0` | How much affection increases patience, `0` = ignore affection |
| Personality | `FondAffection` | `0.8` | Affection (0..1) from which Red enjoys chest pokes |
| Personality | `AffectionOverrideAkipo/Red/Blue` | `-1` | Test value for affection (0..1), `-1` = the game's real value |
| Talk | `Enabled` / `Chance` / `CooldownSeconds` | `true` / `0.35` / `3` | How often she answers (the first touch and a mood change always talk) |
| Talk | `WhenUIHidden` | `true` | Also talk when the level-select UI is hidden with its eye button |
| Drop | `Enabled` / `MadPokesBeforeDrop` | `true` / `3` | Keep poking while she is mad and she has had enough |
| Drop | `Mode` | `Punish` | `Punish`: find out what she does. `Classic`: the old behaviour – she puts you down, and the game closes the second time |
| Drop | `ShrinkSeconds` / `ShrinkAmount` / `LaunchUp` | `60` / `0.75` / `1.5` | Punish mode tuning |
| Drop | `QuitGame` / `DropsBeforeQuit` / `QuitDelaySeconds` | `true` / `2` / `2.5` | Classic mode: close the game on the second drop, after a short pause |
| Sound | `FaceSound` / `FootSound` / `ShoeSound` / `GlassesSound` / `BreastSound` / `HeadpatSound3` | `ItemNatureGrab` / `ItemMeatGrap` / `PlayerStepWalkway` / `ItemGlassGrab` / `Cauldron` / `PlayerStepGrass` | Sound per touch: a game sound type name, `Boop`, `Pat` or empty |
| Sound | `SlapSound` | `Slap` | Punish mode sound: `Slap` (built-in), a game sound type name or empty |
| Debug | `LogClicks` | `false` | Log every click (hit body part, bones, distances) |

## Custom sounds

Optional: create a folder under `BepInEx\plugins\LFPoke\sounds\`
and put `.ogg`/`.wav` files in it – `touch` (face), `foot` (legs and soles),
`tickle` (tummy), `boob` (chest and butt), `pat` (headpat), `slap`, or
`level0`-`level4` (one per mood). None of these folders are needed; the mod
only creates `sounds\boob` itself when it records the cauldron sound.

See the [README](../README.md) for what the mod does.
