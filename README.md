# LethalFantasy – Handle With Care

*Don't poke and boop!*

A small, silly BepInEx plugin for **LethalFantasy**. Poke, pat and tickle Akipo
and the Twins – while they hold you in the lobby, in Akipo's hug, lap and foot
massage poses, or when you walk up to them in the lobby – and they react. Keep
poking and they get more and more annoyed, a bit like clicking a unit too often
in an old RTS.

Push your luck too far and she will make you regret it. Handle with care.

| You do | She does |
| --- | --- |
| Poke her face | Giggles, gets flustered, then smug, frowns – and finally gets mad (darkened eyes) |
| Poke her eye | It hurts! A hurt face, and it annoys her much faster (Blue's glasses protect her eyes) |
| Boop her lips | Akipo and Blue blush, Red flirts |
| Poke her chest (each side separately) | Blushes or teases, depending on who she is; only the poked side jiggles (the game's own breast physics, pushed from your direction) |
| Pinch a Twin's butt | Red flirts briefly, Blue hates it; the pinched side jiggles |
| Tickle her soles or tummy | Laughs (Blue doesn't think it's funny); shoes block it, socks don't |
| Poke her legs | Akipo answers with her shin/thigh lines |
| Pat the top of her head | Gets happy and calms down (a grudging reaction if she is still annoyed) |
| Tickle her soles too much | Akipo crouches, then stomps her foot |
| Keep poking while she is mad | She has had enough – find out what she does (or set `[Drop] Mode = Classic`: she puts you down, then closes the game) |

- **Real hitboxes:** the click is tested against the character's own 3D model in
  her current pose, so it works in every camera view and pose.
- **Expressions** use the game's own dialogue-portrait faces, shown on the 3D model.
- **Lines** are existing game lines, picked per character and situation (around
  330). They appear in the normal dialogue box in your game language, and are
  voiced if [LethalFantasy-TTS](https://github.com/EPIC3D/LethalFantasy-TTS) is
  installed. The same line never comes twice in a row.
- **Each character has her own personality:**

  | | Akipo – young, innocent, shy | Red – extroverted, vain, flirty | Blue – introverted |
  | --- | --- | --- | --- |
  | Tolerance | Neutral | Tolerant (+1) | Low (-1) – dislikes every touch except headpats |
  | Face | Giggles, then gets annoyed | Barely bothers her | Never giggles |
  | Chest | Very embarrassed | Teasing and smug for a long time; when she is fond of you, she enjoys it | Startled, then coldly annoyed |
  | Tickling | Laughs | Laughs for a long time | Not funny to her |
  | Headpats | Calm her | Calm her more (forgiving) | The only touch she likes |

- **Affection** – the game's own affection progress – makes everyone more
  patient: at full affection a poke annoys her half as much and she calms down
  twice as fast. Blue only softens at maximum affection.
- **Where:**
  - *Held in the lobby* (any level-select tab): face, eyes, mouth, headpat, chest,
    soles.
  - *Akipo's hug:* face, eyes, mouth, headpat, chest. *Lap:* chest and tummy.
    *Foot massage:* soles only.
  - *Free roam in the lobby:* anything within arm's reach (Akipo is tall – her
    legs and feet). With one of the game's perk you can also do extreme sports.
- **Sounds** come from the game itself: picking up food (face, legs), a gravel
  step (shoes), glass (Blue's glasses), the cauldron "bloop" (chest, butt) and a
  grass rustle (headpat). The cauldron sound only loads after you have played a
  level once – the first time you return to the lobby, the mod records it
  silently on your own PC and uses it from then on.
- She calms down by herself if you leave her alone for a few seconds.

Supports **LethalFantasy beta 1.0.3** (Akipo, Red twin, Blue twin).

**LethalFantasy** is made by **LadyAlice** – support the original game on
[Patreon](https://www.patreon.com/cw/LadyAliceR18). This is an unofficial
fan-made mod and is not affiliated with or endorsed by the game's creator.

> **Disclaimer:** This software is provided "as is", without warranty of any
> kind. Use it at your own risk. The author accepts no responsibility for any
> damage, data loss or game issues resulting from its use. In `Classic` drop
> mode the mod **can close the game on purpose** – save your progress first.
> See [LICENSE](LICENSE) for details.

## Download

Get the ready-to-use package from the [Releases](../../releases) page. It
contains only `BepInEx\plugins\LFPoke\LFPoke.dll`, the BepInEx plugin.

## Installation

1. Install **BepInEx 5 (x64)** into the LethalFantasy folder and start the game once.
2. Unzip the package into the LethalFantasy folder (so the DLL ends up in
   `<LethalFantasy>\BepInEx\plugins\LFPoke\`).
3. Start the game, go to the lobby and let a character pick you up – or walk up
   to one.

Optional: play one level and return to the lobby once, so the cauldron
"bloop" gets recorded for chest pokes.

Upgrading from 0.1/0.2: the old zone settings are no longer used and can be
deleted from the config (or delete the whole config file to get a clean one).

## Configuration

Everything can be tuned in `<LethalFantasy>\BepInEx\config\com.epicopy.lfpoke.cfg`
(created on the first start) – reach, patience, talk frequency, sounds and more.
See **[docs/CONFIGURATION.md](docs/CONFIGURATION.md)** for every setting and for
adding your own sounds.

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
src/LFPoke/Plugin.cs        Poses, moods, reactions, jiggle, free roam, drop/quit, sounds
src/LFPoke/MeshPick.cs      3D model hitbox: which body part a click hit
src/LFPoke/Lines.cs         Dialogue line keys per character and situation
src/LFPoke/Personality.cs   Per-character tolerance and the game's affection
src/LFPoke/ClipRecorder.cs  Records the cauldron sound once to a local WAV file
src/LFPoke/Wav.cs           WAV writer
docs/CONFIGURATION.md       All settings and custom sounds
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
