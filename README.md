# LethalFantasy – Handle With Care

*Don't poke and boop!*

A small, silly BepInEx plugin for **LethalFantasy**. While Akipo or the Twins
hold you in the lobby and follow your mouse, you can **poke their face, poke
their chest or pat their head** – and they react. Keep poking and they get
more and more annoyed, a bit like clicking a unit too often in an old RTS.

Push your luck too far and she puts you down. Do it again and she closes the
game. Handle with care.

| You do | She does |
| --- | --- |
| Poke her face | Giggles, gets flustered, then smug, frowns – and finally gets mad (darkened eyes) |
| Poke her chest (each side separately) | Blushes hard and gets angry faster; only the poked side jiggles (the game's own breast physics, pushed from your direction) |
| Pat the top of her head | Gets happy and calms down (a grudging blush if she is still annoyed) |
| Keep poking while she is mad | Puts you down – back to the lobby, like the pause menu's *Lobby* button |
| …and do it again | Closes the game |

- **Expressions** use the game's own dialogue-portrait faces, shown on the 3D model.
- **Lines** are existing game lines, picked per character. They appear in the
  normal dialogue box in your game language, and are voiced if
  [LethalFantasy-TTS](https://github.com/EPIC3D/LethalFantasy-TTS) is installed.
- **Each character has her own personality:**

  | | Akipo – young, innocent, shy | Red – extroverted, vain, flirty | Blue – introverted |
  | --- | --- | --- | --- |
  | Face pokes | Normal patience | Barely bothers her | Wears her patience thin faster |
  | Chest pokes | Very embarrassed, annoyed quickly | Teasing and smug for a long time; when she is fond of you, she enjoys it | Startled, then coldly annoyed |
  | Headpats | Work best | Work | Shy, work less |

- **Affection** – the game's own affection progress – makes everyone more
  patient: at full affection a poke annoys her half as much and she calms down
  twice as fast.
- **Sounds** come from the game itself: a chest poke plays the cauldron's
  "bloop", a headpat a soft grass rustle. The cauldron sound only loads after
  you have played a level once – the first time you return to the lobby, the
  mod records it silently on your own PC and uses it from then on.
- She calms down by herself if you leave her alone for a few seconds.

Supports **LethalFantasy beta 1.0.3** (Akipo, Red twin, Blue twin).

**LethalFantasy** is made by **LadyAlice** – support the original game on
[Patreon](https://www.patreon.com/cw/LadyAliceR18). This is an unofficial
fan-made mod and is not affiliated with or endorsed by the game's creator.

> **Disclaimer:** This software is provided "as is", without warranty of any
> kind. Use it at your own risk. The author accepts no responsibility for any
> damage, data loss or game issues resulting from its use. Note that the mod
> **can close the game on purpose** – save your progress first. See
> [LICENSE](LICENSE) for details.

## Download

Get the ready-to-use package from the [Releases](../../releases) page. It
contains only `BepInEx\plugins\LFPoke\LFPoke.dll`, the BepInEx plugin.

## Installation

1. Install **BepInEx 5 (x64)** into the LethalFantasy folder and start the game once.
2. Unzip the package into the LethalFantasy folder (so the DLL ends up in
   `<LethalFantasy>\BepInEx\plugins\LFPoke\`).
3. Start the game, go to the lobby and let a character pick you up.

Optional: play one level and return to the lobby once, so the cauldron
"bloop" gets recorded for chest pokes.

## Configuration

Everything can be tuned in `<LethalFantasy>\BepInEx\config\com.epicopy.lfpoke.cfg`
(created on the first start). The game reads it at startup.

| Section | Setting | Default | What it does |
| --- | --- | --- | --- |
| General | `Enabled` | `true` | Turn the mod on/off |
| General | `FaceRadius` | `1.0` | Size of the clickable face area |
| Mood | `LevelThresholds` | `0,3,6,10,15` | Pokes where each of the 5 moods starts |
| Mood | `CalmDownDelay` / `CalmDownPerSecond` | `2.5` / `1.0` | How fast she forgives you |
| Chest | `BreastRadius`, `ScreenLeftBreastX/Y`, `ScreenRightBreastX/Y` | – | Chest zones, relative to the chest bone |
| Chest | `CleavageTop/BottomX/Y`, `TopLineLeft/RightX/Y` | – | Left/right split and the upper limit of the chest zones |
| Chest | `AnnoyancePerPoke` | `2` | Chest poke strength; scales each character's own value (`2` = as designed) |
| Chest | `JiggleForce` | `120` | Physics push strength (game footsteps use 150-225), `0` = off |
| Headpat | `OffsetX/Y`, `HalfWidth/HalfHeight` | – | Headpat oval above the face |
| Headpat | `CalmPerPat` | `3` | How much a pat calms her; scales each character's own value (`3` = as designed) |
| Personality | `AffectionPatience` | `1.0` | How much affection increases patience, `0` = ignore affection |
| Personality | `FondAffection` | `0.8` | Affection (0..1) from which Red enjoys chest pokes |
| Personality | `AffectionOverrideAkipo/Red/Blue` | `-1` | Test value for affection (0..1), `-1` = the game's real value |
| Talk | `Enabled` / `Chance` / `CooldownSeconds` | `true` / `0.35` / `3` | How often she answers (a mood change always talks) |
| Drop | `MadPokesBeforeDrop` | `3` | Pokes while mad before she puts you down |
| Drop | `QuitGame` / `DropsBeforeQuit` | `true` / `2` | Close the game on the second drop (`false` to disable) |
| Sound | `TouchSound4` / `BreastSound` / `HeadpatSound3` | – / `Cauldron` / `PlayerStepGrass` | Sound per touch: a game sound type name, `Boop`, `Pat` or empty |
| Debug | `LogClicks` | `false` | Log click positions (for tuning zones) |

All zone values are fractions of the screen height, so they work at any
resolution.

Custom sounds (optional): create a folder under `BepInEx\plugins\LFPoke\sounds\`
and put `.ogg`/`.wav` files in it – `touch` (face poke), `boob` (chest poke),
`pat` (headpat), or `level0`-`level4` (one per mood). None of these folders are
needed; the mod only creates `sounds\boob` itself when it records the cauldron
sound.

## Building from source

Requirements: .NET SDK (any version that can build `netstandard2.1`) and a local
LethalFantasy install **with BepInEx 5** – the build references the game's own
`Assembly-CSharp.dll`, Unity, MagicaCloth and BepInEx assemblies, which are not
part of this repository.

```powershell
# 1. Tell the build where your game is
Copy-Item GamePath.props.example GamePath.props
#    then edit GamePath.props -> <LethalFantasyDir>

# 2. Build
dotnet build src\LFPoke\LFPoke.csproj -c Release
```

You can also pass the path directly:
`dotnet build src\LFPoke\LFPoke.csproj -c Release -p:LethalFantasyDir="D:\Games\LethalFantasy"`

### Repository layout

```
src/LFPoke/Plugin.cs        Zones, moods, reactions, jiggle, drop/quit, sounds
src/LFPoke/Lines.cs         Dialogue line keys per character and mood
src/LFPoke/Personality.cs   Per-character tolerance and the game's affection
src/LFPoke/ClipRecorder.cs  Records the cauldron sound once to a local WAV file
src/LFPoke/Wav.cs           WAV writer
```

## Notes

- This is an unofficial fan mod and is not affiliated with the LethalFantasy developers.
- No game content is included or redistributed: no game, Unity, BepInEx or
  Harmony binaries, no game audio and no dialogue text. The mod only refers to
  the game's own lines and sounds by their internal names; the game shows and
  plays them from your own installation.

## License

[MIT](LICENSE)

## Support

If you enjoy the game, please support its author, LadyAlice, first:
<https://www.patreon.com/cw/LadyAliceR18>.

If you'd also like to support this mod:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/E6Z42AUOBQ)
