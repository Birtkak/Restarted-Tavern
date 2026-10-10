# Colour baseline

The starting palette for cards and the table (Decision Log 2026-10-10). The code is the source of truth:
`CardFaces.cs` (factions, rarity, gems) and `Ui.Oklch`. This page explains why the values are what they are.

## Rules

1. **OKLCH, not HSL or hex picking.** OKLCH has perceptual lightness: two colours with the same L look equally bright
   whatever their hue (HSL's 50% yellow is far brighter than its 50% blue). The palette is written as
   `Ui.Oklch(L, C, hue)`, so "same weight, different hue" is one number apart.
2. **Each channel of meaning gets its own visual job, and the jobs don't share space:**

   | What | Where on the card | Colour role |
   |---|---|---|
   | Faction | the big frame / body background, the bottom label, the art emblem | hue (one per faction), mid-dark L |
   | Rarity | LoR's diamond gem on the bottom edge | hue (LoR gem colours) |
   | Mana | top-left gem | blue (hue 260), reserved, no faction uses it |
   | Power / Health | bottom-corner gems | amber / red, the same on every card |
   | Text | cream on a dark fade over the art (LoR) | contrast first |
3. **Never colour alone** (WCAG 1.4.1): Legendary has its own heavier frame, factions have a written label and emblem,
   keywords are spelled out in the text and have badges, buffs / damage also change the number.
4. **Contrast:** rules text is cream (L ~0.93) on a near-black fade (well above WCAG's 4.5:1); light accents (L 0.84) sit
   on dark frames (L 0.40). The frame is gold on every card, as in LoR.
5. **Equal loudness:** every faction frame is L 0.40, C 0.10, so no faction looks more important than another. Neutral
   is the same lightness with almost no chroma.

## Factions (frame L 0.40 C 0.10, accent L 0.84 C 0.12)

| Faction | Frame hue | Accent hue | Why |
|---|---|---|---|
| Goobers | 30 (red) | 55 | rowdy, fire |
| Evergrowing Wild | 145 (green) | 130 | nature |
| Glitterworld | 205 (teal / cyan) | 200 | neon tech |
| Shadow Money Wizards | 300 (violet) | 90 (gold) | arcane plus money |
| Sensationalists | 350 (magenta) | 345 | showbiz, spectacle; moved off violet so it no longer looks like the Wizards |
| Neutral | 70, C 0.015 (warm grey) | 70 | no faction |
| Tavern Dweller | 65, C 0.07 (bronze) | 85 | the tavern |

The hues are at least 50 degrees apart, and blue (around 260) stays free for mana.

## Rarity (Legends of Runeterra gem colours, user 2026-10-10)

| Rarity | LoR equivalent | Colour | OKLCH |
|---|---|---|---|
| Common | Common | green | L 0.72, C 0.17, hue 150 |
| Uncommon | Rare | blue | L 0.68, C 0.15, hue 245 |
| Rare | Epic | purple | L 0.62, C 0.20, hue 305 |
| Legendary | Champion | gold | L 0.83, C 0.16, hue 85 (heavier frame with corner gems) |

Shown as LoR's faceted diamond gem on the card's bottom edge. The gem
and the mana gem are both blue-ish for Uncommon, but the shapes differ (diamond vs round) and they sit apart.

## Open for later

- A colour-blind check with a simulator (deuteranopia: Goobers red vs Wild green are told apart by lightness and the
  label, but check it).
- Real frame art: the card frame is procedural (`Ui.CardShapeSprite`, metal gradient, `GoldGlint`); `Ui.FrameSprite`
  (buttons, panels) takes PNGs from `Resources/CardFrames/`. Card art goes in `Resources/CardArt/<card id>`.
- The UI theme: slate blue panels (`Wood`/`WoodDark`/`Felt` in TableView, renamed later), gold trim `Ui.Gold`, cream text
  `Ui.Cream`; buttons are lit bodies in a gold 9-slice rim.
