# Changelog

## 0.2.0 – personalities and affection

Tested against LethalFantasy beta 1.0.3.

- Each character tolerates pokes in her own way:
  - **Akipo** (young, innocent, shy): very embarrassed by chest pokes and annoyed
    quickly; headpats calm her the most.
  - **Red** (extroverted, vain, flirty): face pokes barely bother her, chest pokes
    keep her teasing and smug for a long time – and once she is fond of you she
    enjoys them instead of getting annoyed.
  - **Blue** (introverted): loses patience faster, is startled and then coldly
    annoyed by chest pokes; headpats work less on her.
- The game's own **affection** progress makes everyone more patient: at full
  affection a poke annoys her half as much and she calms down twice as fast
  (`[Personality] AffectionPatience`, `FondAffection`).
- Test override per character (`AffectionOverrideAkipo/Red/Blue`, `-1` = the
  game's real value), e.g. for the Twins before their affection has grown.
- More line variety for all three characters (Red especially: flirty, vain and
  teasing lines; Akipo: shy reactions; Blue: quiet, reserved ones).

## 0.1.0 – first release

Tested against LethalFantasy beta 1.0.3.

- Poke the face or chest, or pat the head of Akipo, Red or Blue while she holds
  you in the lobby.
- Five escalating moods (giggle, flustered, smug, frown, mad) using the game's
  dialogue-portrait faces on the 3D model, with existing game lines per character
  (voiced if LethalFantasy-TTS is installed).
- Chest pokes: each side separately; only the poked side jiggles, pushed from
  the player's direction (the game's own breast physics).
- Headpats make her happy and calm her down.
- Keep poking while she is mad and she puts you down (back to the lobby); the
  second time she closes the game.
- Sounds from the game: the cauldron "bloop" for chest pokes (recorded once on
  the player's own PC after the first level), a grass rustle for headpats.
- All zones scale with the screen resolution; everything is configurable.
