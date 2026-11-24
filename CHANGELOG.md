# Changelog

## 0.4.0 – Secret, punishments, more reactions

Tested against LethalFantasy beta 1.0.3.

**Lobby**

- With a specific minor perk chosen, there is a new hidden feature.
- Akipo crouches down after a few playful sole pokes, and stomps her foot when
  the tickling gets too much.

**Punishment**

- When she has had enough, she no longer closes the game – the punishment has
  changed, with measurable consequences. Find out for yourself
  (`[Drop] Mode = Classic` brings back the old one).
- She no longer picks you up when you tease her in free roam
  (`[Free] PickupAtLevel`, now off by default).
- Her anger fades more slowly (`[Mood] CalmDownDelay` / `CalmDownPerSecond`).

**Reactions**

- Akipo reacts to what she wears on her feet.
- Open footwear (heels, slippers, geta, pokkuri) leaves the top of her foot
  ticklable – from above, not from the sole side.
- Tolerance scale toned down: Red +0.5, Blue -0.5 (was +1 / -1).

## 0.3.0 – real hitboxes, more places, more to say

Tested against LethalFantasy beta 1.0.3.

**Hitbox and places**

- New hitbox: the character's own 3D model. A click is tested against her current
  pose, and the bones that move the hit spot decide the body part. Exact in every
  camera view and pose (level-select tabs, Twins, hug, lap, foot massage), for
  Akipo, Red and Blue alike – no more hand-placed circles or zone settings.
- New body parts: eyes, mouth, soles, tummy, legs (shin/thigh) and – for the
  Twins – the butt (also through the coat/skirt). The breast that was actually
  hit is the one that jiggles; a butt pinch jiggles the pinched side.
- Hair above the eyes counts as the top of the head (headpat); hair in front of
  the face below the eyes counts as the face.
- Akipo's poses: after the hug, lap and foot massage (with the level select open)
  she can be touched too – hug: face, eyes, mouth, headpat, chest; lap: chest and
  tummy; foot massage: soles only.
- Free roam in the lobby: when nobody holds you, every character within arm's
  reach can be touched (`[Free] Reach`, measured from your body). Tease her up to
  the "stop it" mood and she picks you up (`PickupAtLevel`); the "E" pickup stays
  instant.
- Shoes block the sole tickle (the game's own footwear state; bare feet and socks
  are ticklish), a gravel step sound plays instead. Blue's glasses protect her
  eyes (a glass "tink" instead).

**Characters**

- Tolerance scale: Akipo neutral, Red +1 (tolerant, forgiving), Blue -1. Blue
  dislikes every touch except headpats; affection only softens her at its maximum.
- Red: butt pinches are flirty only briefly. Blue: never giggles, tickling is never
  funny to her.
- Eye pokes hurt (a "hurt" face, three times as annoying); mouth pokes make Akipo
  and Blue blush and Red flirt.

**Lines**

- All line sets reviewed per character and situation; misfits removed, many new
  ones added (around 330 lines), with own sets for eyes, mouth, butt, tickling,
  foot jokes, shin/thigh (Akipo's shrine-blessing lines) and the two drop rounds:
  Akipo is hurt and disappointed the first time and harsh the second time.
- A character never says the same line twice in a row.
- The first touch after a quiet moment always gets an answer, tickles always talk.
- `[Talk] WhenUIHidden`: lines also play when the level-select UI is hidden with
  its eye button (on by default; off = no lines and no dialogue start sound then).

**Sounds**

- Face poke: picking up regular food; legs and soles: picking up heavy food;
  shoe: a gravel step; glasses: glass; chest and butt: the cauldron "bloop";
  headpat: a grass rustle. Optional own sounds in `sounds\touch`, `foot`, `tickle`.
- A short pause after her last line before the game closes
  (`[Drop] QuitDelaySeconds`).

**Removed**

- The old hand-placed zone settings (face radius, breast/cleavage/limit offsets,
  headpat oval) – the 3D model hitbox replaces them.

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
