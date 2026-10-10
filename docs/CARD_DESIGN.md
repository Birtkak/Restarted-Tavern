# Restarted Tavern — Card Design Guide

How to design cards that fit the rules ([GAME_DESIGN.md](GAME_DESIGN.md)) and each other.
Status tags: 🔒 locked, 🟡 proposed, ❓ open.

---

## 1. Rarity 🔒

| Rarity | Role |
|---|---|
| **Common** | The backbone: simple, efficient, one idea per card |
| **Uncommon** | Synergy and faction mechanics; a little more text |
| **Rare** | Build-around cards, strong effects, flashy spells |
| **Legendary** | Unique characters and artifacts. **Legendary rule**: you may control only **one** permanent with a given Legendary name at a time. If you'd have two, you choose one to keep and the other goes to the graveyard (MTG 704.5j; other players can have their own copy). The deck copy limit stays 4 |

---

## 2. Stat Budget 🔒

**Vanilla baseline: Power + Health = 2 × cost + 1**

| Cost | Vanilla examples |
|---|---|
| 1 | 1/2 · 2/1 |
| 2 | 2/3 · 3/2 |
| 3 | 3/4 · 4/3 |
| 4 | 4/5 · 5/4 |
| 5 | 5/6 · 6/5 |
| 7 | 7/8 |
| 10 | 10/11 |

### 2.1 Ability costs (stat points) 🟡
Starting values for the budget, to be tuned in playtesting.

| Ability | Cost in stat points |
|---|---|
| Flying, Trample, Lifelink | ~1 each |
| Haste | 0 under the Standard rules (no summoning sickness, GAME_DESIGN §7.4) ❓ |
| Small Arrival effect (1 damage, draw if X, +1/+1 to another) | ~2 |
| Card draw (1 card) | ~2–3 |
| Removal on a creature (kills something) | most of the budget |
| Drawback (lose life, discard, give opponents Gold) | gives back ~1–2 |
| Invest | Not counted against the base stats. The **Invest** bonus is priced on its own: about 1 Gold ≈ 1 stat point of effect |

### 2.2 Guidelines 🟡
- **Health matters more than usual**: damage is permanent, so extra Health is worth more over a game than extra Power. A 2/3 is noticeably better than a 3/2.
- **Pings** (1 damage) are strong here: they leave lasting damage. Price them higher than in Hearthstone.
- **Healing** cards should be efficient. Healing is card-only, so an overcosted heal is never played.
- **Top-end exception**: Legendary creatures costing 7+ may go about 3 points over budget. They are the payoff for surviving to 10 mana.
- **Gold is worth less than mana**: it has a cap (3) and can only pay for some things, so 1 Gold ≈ 0.7 mana when pricing effects that give Gold. A player already at the cap gains nothing, so "gains N Gold" downsides are smaller than they look (❓ see playtest/RULES_REVIEW.md, design review 2026-10-10, R9).
- **Spells and abilities are effectively cheaper**: Instants, Sorceries, Equip and activated abilities spend banked Gold first (GAME_DESIGN §5.2). That's up to 3 extra "mana" on a burst turn. Permanents can't use Gold, so a creature-heavy turn can't be stretched. Price spells, Equip costs and abilities with that in mind.

---

## 3. First Set Composition 🔒 ~120 cards

| Group | Cards | Rarity split (C/U/R/L) |
|---|---|---|
| Each faction (×5) | 20 | 9 / 6 / 4 / 1 |
| Neutral | 10 | 5 / 3 / 2 / 0 |
| Tavern Dwellers | 10 | (one per pair, see GAME_DESIGN §9.3) |
| **Total** | **120** | |

🔒 **Set v0.2 (approved 2026-10-09):** +10 cards per faction (5 C / 3 U / 2 R) and +20 Neutral (10 C / 6 U / 4 R), so 30 per faction, 30 Neutral and 10 Tavern Dwellers (190 total). The goal is depth for the mechanics already in place, not new keywords. The cards are in each card list under "Set v0.2 additions".

🔒 **Set v0.3 (approved 2026-10-09): mana scarcity.** +6 cards per faction and +6 Neutral (36), 2 C / 2 U / 2 R each: 2 card draw, 2 mana sinks (X spells, repeatable "(N): ..." or "(X): ..." abilities, Invest 3), 2 finishers (X burn to face, drains, inevitability). Pricing: X spells cost about X+1 for X damage or X cards; repeatable abilities cost 2-4 per use; Invest stays at 3 or less (Gold cap 3 with Runeterra mana).

🟡 Mana curve for each faction's 20 cards: about 3 one-drops, 4 two-drops, 4 three-drops, 3 four-drops, 3 five-drops, 2 six-drops, and 1 card at 7–10. Plus about 6 non-creature cards spread across the curve.

---

## 4. Faction Pie 🔒
What each faction **does best** (primary), **can do** (secondary), and **never does**. This keeps factions distinct and makes pairs matter.

| Effect | Wizards | Goobers | Sensationalists | Wild | Glitterworld |
|---|---|---|---|---|---|
| **Removal style** | Bounce, bribe (steal for a turn), counter | Burn (direct damage) | Destroy, sacrifice, -X/-X | **Fight** (a creature deals its Power to another) | **Pings** (lots of 1s) |
| **Card draw** | ★ primary | rummage (discard, then draw) | pay life to draw | draw when big creatures arrive | — |
| **Gold** | ★ primary (shady deals) | steal Gold | — | — | Equip costs paid with Gold |
| **Healing** | — | — | drain / Lifelink | ★ heal creatures | repair *machines* |
| **Big creatures** | — | — | a few, with drawbacks | ★ primary | via Equipment |
| **Go wide (many small creatures)** | — | ★ primary | spawn tokens from deaths | critter swarms | — |
| **Buffs** | — | team-wide, temporary | — | +1/+1 counters | ★ Equipment |
| **Keywords** | Flying | Haste, Trample | Flying, Lifelink | Trample | Flying (drones) |
| **Graveyard** | — | — | ★ primary | — | — |

---

## 5. Sample Cards 🟡
Three per faction, to set the tone. Numbers follow §2.

Favorites so far: **Silver-Tongued Deal** (shady deals: a strong effect that also helps opponents) and **Apex of the Green Deep** (huge, game-ending top-end payoffs). Push more cards in these directions.

### Shadow Money Wizards
| Name | Cost | Type | Stats | Text |
|---|---|---|---|---|
| **Back-Alley Appraiser** | 2 | Creature: Wizard (C) | 1/3 | Arrival: You may discard a card. If you do, gain 2 Gold. |
| **Silver-Tongued Deal** | 3 | Instant (U) | — | Gain control of target creature until end of turn. Untap it. It gains Haste. Each opponent gains 2 Gold. |
| **The Grand Ledger** | 7 | Sorcery (R) | — | Draw 4 cards. Invest 3: Draw 2 more and gain 3 life. |

### Goobers
| Name | Cost | Type | Stats | Text |
|---|---|---|---|---|
| **Goober Rascal** | 1 | Creature: Goober (C) | 2/1 | Haste. Can't block. |
| **Fuse Goober** | 2 | Creature: Goober (C) | 1/2 | Last Breath: Deal 2 damage to any target. |
| **Mob Rush** | 4 | Sorcery (U) | — | Create three 1/1 Goobers with Haste. Your creatures get +1/+0 until end of turn. |

### Sensationalists
| Name | Cost | Type | Stats | Text |
|---|---|---|---|---|
| **Candlelit Acolyte** | 2 | Creature: Human (C) | 2/2 | Lifelink. |
| **Hex of Withering** | 3 | Curse (C) | — | Attach to an enemy creature. It gets -2/-0. At the start of its controller's turn, deal 1 damage to it. |
| **Midnight Ritual** | 1 | Instant (U) | — | As an extra cost, sacrifice a creature. Draw 2 cards. Invest 2: Return a creature card with cost 3 or less from your graveyard to the battlefield. |

### Evergrowing Wild
| Name | Cost | Type | Stats | Text |
|---|---|---|---|---|
| **Canopy Critter** | 1 | Creature: Critter (C) | 1/1 | Arrival: Heal 2 from another creature. |
| **Thornback Ravager** | 5 | Creature: Beast (U) | 4/4 | Trample. Arrival: Draw a card. |
| **Apex of the Green Deep** | 10 | Creature: Leviathan (L) | 12/12 | Trample. At the start of your turn, heal this creature fully. |

### Glitterworld
| Name | Cost | Type | Stats | Text |
|---|---|---|---|---|
| **Spark Drone** | 2 | Creature: Construct (C) | 1/1 | Flying. Arrival: Deal 1 damage to a creature. |
| **Pulse Blade** | 2 | Equipment (C) | — | Equipped creature gets +1/+1 and has "Whenever this creature attacks, deal 1 damage to a creature." Equip 2. |
| **Grid Overload** | 4 | Sorcery (R) | — | Deal 1 damage to each enemy creature three times. |

### Neutral
| Name | Cost | Type | Stats | Text |
|---|---|---|---|---|
| **Tavern Bouncer** | 3 | Creature: Human (C) | 2/5 | — |
| **Barkeep's Tonic** | 1 | Instant (C) | — | Heal 3 from a creature or your Tavern Dweller. Invest 1: Draw a card. |
