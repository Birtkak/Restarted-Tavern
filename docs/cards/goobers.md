# Goobers — Card List

**Identity:** goblins (MTG red + LoR Noxus). Loud, reckless, many of them.
**Faction pie:** go wide ★ (many small creatures), burn as removal, rummage instead of card draw, team-wide temporary buffs, steal Gold. Keywords: Haste, Trample. No healing, no graveyard play.
**Role with permanent damage:** Goobers **create** damage and don't mind losing their small creatures.

**Token:** *Goober*, a 1/1 Creature: Goober.

**Target:** 20 cards, 9 Common / 6 Uncommon / 4 Rare / 1 Legendary. Curve 3 / 4 / 4 / 3 / 3 / 2 / 1.

Status tags: ✅ approved · 🟡 draft · ✏️ needs changes · ❌ cut

---

| # | Name | Cost | Type | Rarity | Stats | Text | Design notes | Status |
|---|---|---|---|---|---|---|---|---|
| 1 | **Goober Rascal** | 1 | Creature: Goober | C | 2/1 | Haste. Can't block. | The classic aggressive 1-drop | ✅ |
| 2 | **Spark Snot** | 1 | Instant | C | — | Deal 2 damage to a creature. | Cheap burn. The damage sticks, so it also finishes off a creature that's already wounded | ✅ |
| 3 | **Grubby Pickpocket** | 1 | Creature: Goober | C | 1/2 | Whenever this attacks, the defending player loses 1 Gold and you gain 1 Gold. | Steals Gold, and cuts down the opponent's instant-speed answers | ✅ |
| 4 | **Fuse Goober** | 2 | Creature: Goober | C | 1/2 | Last Breath: Deal 2 damage to any target. | Trades up even when it dies | ✅ |
| 5 | **Gob Gang** | 2 | Sorcery | C | — | Create two 1/1 Goobers. | Goes wide | ✅ |
| 6 | **Goober Shaman** | 2 | Creature: Goober | U | 2/2 | Arrival: You may discard a card. If you do, draw a card. | Rummage: Goobers' version of card selection | ✅ |
| 7 | **Brawling Runt** | 2 | Creature: Goober | C | 2/2 | Haste. | A plain, efficient aggro body | ✅ |
| 8 | **Goober Warchief** | 3 | Creature: Goober | U | 2/3 | Your other Goobers get +1/+0. | The "lord": makes tokens a real threat | ✅ |
| 9 | **Firecracker Volley** | 3 | Sorcery | U | — | Deal 3 damage divided as you choose among any number of creatures and/or players. | Flexible burn: kill small creatures, or spread lasting damage | ✅ |
| 10 | **Hog-Rider** | 3 | Creature: Goober | C | 3/3 | Trample. | A goblin on a war pig | ✅ |
| 11 | **Gold-Snatcher Crew** | 3 | Creature: Goober | U | 2/2 | Haste. Whenever this deals combat damage to a player, that player loses up to 2 Gold and you gain that much. | Gold theft on a body | ✅ |
| 12 | **Mob Rush** | 4 | Sorcery | U | — | Create three 1/1 Goobers with Haste. Your creatures get +1/+0 until end of turn. | Instant pressure | ✅ |
| 13 | **Barrel Bomber** | 4 | Creature: Goober | C | 3/3 | Arrival: Deal 2 damage to any target. | Removal on a body | ✅ |
| 14 | **Pit-Fighter** | 4 | Creature: Goober | C | 4/4 | Trample. | A solid mid-sized beater | ✅ |
| 15 | **Goober Demolisher** | 5 | Creature: Goober | R | 4/5 | Trample. Whenever another Goober you control dies, deal 1 damage to each opponent. | Payoff for trading tokens away | ✅ |
| 16 | **Overrun the Gates** | 5 | Sorcery | U | — | Your creatures get +2/+0 and Trample until end of turn. | A finisher for a wide board | ✅ |
| 17 | **Scrapheap Inferno** | 5 | Sorcery | R | — | Deal X damage to any target, where X is 2 plus the number of Goobers you control. | Burn that scales with your board. Can finish the opponent off | ✅ |
| 18 | **Pit Champion** | 6 | Creature: Goober | R | 6/5 | Haste. Trample. | Big, immediate damage | ✅ |
| 19 | **Grakka, Queen of the Rabble** | 7 | Creature: Goober | L | 5/6 | Haste. Your Goobers get +1/+1. Whenever you attack, create a 1/1 Goober that's tapped and attacking. | The face of the faction. Over budget on purpose (Legendary top-end exception) | ✅ |
| 20 | **Snik, the Goober Doubler** | 4 | Creature: Goober | L | 2/4 | X, Tap: Choose up to X other Goobers you control. For each one, create a token copy of it. The copies gain Haste until end of turn. | Requested by the user. X can be paid with mana **or Gold**, since it's an activated ability, so stolen Gold fuels it. Copying a Warchief stacks the lord bonus. A copy of a Legendary (Grakka) dies to the Legendary rule. Tap ability: can't be used the turn Snik arrives | ✅ |

**Progress:** 20 / 20 · C 9 · U 6 · R 3 · L 2 (the second Legendary, Snik, replaced the rare Call the Horde)
**Curve:** 1-drops: 3 · 2: 4 · 3: 4 · 4: 4 · 5: 3 · 6: 1 · 7+: 1 · Creatures: 14 · Spells: 6

✅ **Faction complete (v0.1)**

---

## Set v0.2 additions (approved 2026-10-09)

Goal: **more depth for the mechanics already in place**: Gold (banking, spending, the cap), permanent damage and healing, Invest, the Chain, Arrival and Last Breath. Target per faction: +10 cards, 5 Common / 3 Uncommon / 2 Rare.

**Engine** column: ✓ = the engine can run it today; otherwise it names what's missing (see DEVELOPMENT §7).
New rules terms used here are defined in GAME_DESIGN §5.2 (**bank**) and §11.1 (**damaged**, **can't be healed**).

| # | Name | Cost | Type | Rarity | Stats | Text | Design notes | Engine | Status |
|---|---|---|---|---|---|---|---|---|---|
| 21 | **Kick 'Em While They're Down** | 2 | Sorcery | C | — | Deal 2 damage to target creature. If it was already damaged, deal 4 damage instead. | Goobers create damage, and this finishes off what they started. Plain 'damaged' text, not a keyword | ✓ implemented | ✅ |
| 22 | **Loot Splitter** | 2 | Creature: Goober | C | 2/2 | Invest 2: Create a 1/1 Goober. | Stolen Gold turns into more bodies | ✓ implemented | ✅ |
| 23 | **Goober Sapper** | 3 | Creature: Goober | C | 2/2 | Haste. Last Breath: Deal 1 damage to each enemy creature. | Trading it away wounds the whole enemy board, and those wounds stay | ✓ implemented | ✅ |
| 24 | **Reckless Charge** | 1 | Instant | C | — | Target creature gets +2/+0 and Trample until end of turn. Invest 1: Your other Goobers get +1/+0 until end of turn. | A combat trick after blockers are declared, with a Gold-paid team version | ✓ implemented | ✅ |
| 25 | **Gold-Tooth Bruiser** | 4 | Creature: Goober | C | 4/3 | Trample. Whenever this deals combat damage to a player, that player loses 1 Gold and you gain 1 Gold. | Steals Gold on a body that can trample through | ✓ implemented | ✅ |
| 26 | **Pickpocket Boss** | 3 | Creature: Goober | U | 2/3 | Whenever another Goober you control deals combat damage to a player, that player loses 1 Gold and you gain 1 Gold. | Turns a wide attack into a Gold drain. Takes away the opponent's instant-speed answers | ✓ implemented | ✅ |
| 27 | **Fling the Runt** | 1 | Instant | U | — | As an extra cost, sacrifice a creature. Deal damage equal to its Power to any target. | Sacrifice a Fuse Goober for a double hit. Can save a creature from a Curse by turning it into damage | ✓ implemented | ✅ |
| 28 | **Rally Drummer** | 3 | Creature: Goober | U | 2/3 | Whenever you attack with three or more creatures, attacking creatures you control get +1/+0 until end of turn. | A team-wide temporary buff, the Goober way | ✓ implemented | ✅ |
| 29 | **Chaos Engine** | 5 | Creature: Goober | R | 4/4 | Haste. At the start of your turn, deal 1 damage to each other creature. | Symmetric chip damage: it hurts your Goobers too, but they're cheap. Big creatures slowly erode | ✓ implemented | ✅ |
| 30 | **Grand Heist** | 3 | Sorcery | R | — | Each opponent loses all their Gold. Gain that much Gold. Invest 2: Create a 1/1 Goober for each Gold you gained this way. | Punishes banking. Your own Gold cap limits the take | ✓ implemented | ✅ |

**v0.2 progress:** 10 approved · C 5 · U 3 · R 2
