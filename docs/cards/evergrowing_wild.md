# Evergrowing Wild — Card List

**Identity:** a jungle planet, home to everything from tiny critters to the biggest, scariest creatures in the universe (MTG green + LoR Freljord).
**Faction pie:** big creatures ★, healing creatures ★, removal through **fight**, card draw when big creatures arrive, critter swarms, +1/+1 counters. Keywords: Trample, Reach. No Flying, no Gold, no graveyard play.
**Role with permanent damage:** Wild creatures are big and tough enough to **survive** chip damage, and they can heal it.

**Token:** *Critter*, a 1/1 Creature: Critter.

**Target:** 20 cards, 9 Common / 6 Uncommon / 4 Rare / 1 Legendary. Curve 3 / 4 / 4 / 3 / 3 / 2 / 1.

Status tags: ✅ approved · 🟡 draft · ✏️ needs changes · ❌ cut

---

| # | Name | Cost | Type | Rarity | Stats | Text | Design notes | Status |
|---|---|---|---|---|---|---|---|---|
| 1 | **Canopy Critter** | 1 | Creature: Critter | C | 1/1 | Arrival: Heal 2 from another creature. | From the samples | ✅ |
| 2 | **Jungle Remedy** | 1 | Instant | C | — | Heal 4 from a creature. Invest 1: Put a +1/+1 counter on it. | Efficient healing, as the guidelines require | ✅ |
| 3 | **Sproutling** | 1 | Creature: Plant | C | 1/2 | At the end of your turn, if this has no damage, put a +1/+1 counter on it. | Grows if you protect it | ✅ |
| 4 | **Vine Spider** | 2 | Creature: Spider | C | 2/3 | Reach. | The faction's answer to fliers | ✅ |
| 5 | **Primal Clash** | 2 | Sorcery | C | — | Target creature you control fights target creature you don't control. | Fight removal. Your creature keeps its damage too | ✅ |
| 6 | **Growth Spurt** | 2 | Instant | U | — | Put two +1/+1 counters on target creature. Heal 2 from it. | A permanent combat trick | ✅ |
| 7 | **Mossback Tortoise** | 2 | Creature: Turtle | C | 1/4 | At the start of your turn, heal 1 from this. | A defensive wall that slowly recovers | ✅ |
| 8 | **Razorhide Boar** | 3 | Creature: Beast | C | 3/3 | Trample. | Solid 3-drop | ✅ |
| 9 | **Den Mother** | 3 | Creature: Beast | U | 2/4 | Whenever another creature with 5 or more Power enters under your control, draw a card. | Card draw from big creatures, as the faction pie says | ✅ |
| 10 | **Apex Instinct** | 3 | Instant | U | — | Target creature you control gets +2/+2 until end of turn, then it fights target creature you don't control. | Instant-speed fight | ✅ |
| 11 | **Grove Elder** | 3 | Creature: Shaman | U | 2/3 | Tap: Heal 2 from target creature. | A repeatable healer | ✅ |
| 12 | **Ironbark Grizzly** | 4 | Creature: Beast | C | 4/5 | — | Plain and sturdy | ✅ |
| 13 | **Sabretooth Prowler** | 4 | Creature: Cat | R | 4/4 | Trample. Arrival: This fights up to one target creature you don't control. | Removal on a big body. It keeps the wounds from the fight | ✅ |
| 14 | **Grove Warden** | 4 | Creature: Treefolk | R | 3/5 | Your other creatures have "At the start of your turn, heal 1 from this." | A healing engine for the whole board | ✅ |
| 15 | **Thornback Ravager** | 5 | Creature: Beast | U | 4/4 | Trample. Arrival: Draw a card. | From the samples | ✅ |
| 16 | **Tusked Mammoth** | 5 | Creature: Beast | C | 5/5 | Trample. | Freljord-style beater | ✅ |
| 17 | **Call of the Deep Jungle** | 5 | Sorcery | R | — | Reveal cards from the top of your deck until you reveal a creature card with cost 5 or more. Put it onto the battlefield. Put the other revealed cards on the bottom of your deck in a random order. | Cheats out a big creature early. Best with Apex | ✅ |
| 18 | **Primeval Behemoth** | 6 | Creature: Beast | U | 6/6 | Trample. Arrival: Heal all other creatures you control fully. | A big body that also repairs the board | ✅ |
| 19 | **Worldroot Hydra** | 6 | Creature: Hydra | R | 5/5 | Whenever this is dealt damage and survives, put a +1/+1 counter on it. | Grows from permanent damage, so chip damage makes it stronger | ✅ |
| 20 | **Apex of the Green Deep** | 10 | Creature: Leviathan | L | 12/12 | Trample. At the start of your turn, heal this creature fully. | From the samples (a favorite) | ✅ |

**Progress:** 20 / 20 · C 9/9 · U 6/6 · R 4/4 · L 1/1
**Curve:** 1-drops: 3 · 2: 4 · 3: 4 · 4: 3 · 5: 3 · 6: 2 · 7+: 1 · Creatures: 15 · Spells: 5

✅ **Faction complete (v0.1)**

---

## Set v0.2 additions (approved 2026-10-09)

Goal: **more depth for the mechanics already in place**: Gold (banking, spending, the cap), permanent damage and healing, Invest, the Chain, Arrival and Last Breath. Target per faction: +10 cards, 5 Common / 3 Uncommon / 2 Rare.

**Engine** column: ✓ = the engine can run it today; otherwise it names what's missing (see DEVELOPMENT §7).
New rules terms used here are defined in GAME_DESIGN §5.2 (**bank**) and §11.1 (**damaged**, **can't be healed**).

| # | Name | Cost | Type | Rarity | Stats | Text | Design notes | Engine | Status |
|---|---|---|---|---|---|---|---|---|---|
| 21 | **Bark Skin** | 1 | Instant | C | — | Target creature gets +0/+3 until end of turn. Put a +1/+1 counter on it. | A combat trick that leaves something behind. Losing the +0/+3 can't kill it (§7.3) | ✓ implemented | ✅ |
| 22 | **Overflowing Spring** | 2 | Instant | U | — | Heal 4 from target creature. If it had no damage, put two +1/+1 counters on it instead. | Never a dead card: it heals when it's needed, and grows the creature when it's not | ✓ implemented | ✅ |
| 23 | **Sap Mender** | 2 | Creature: Plant | U | 1/3 | Whenever you heal a creature, put a +1/+1 counter on it. | A healing payoff. Every heal also grows the creature | ✓ implemented | ✅ |
| 24 | **Critter Burrow** | 3 | Sorcery | C | — | Create three 1/1 Critters. | Critter swarm (the faction's Critter token) | ✓ implemented | ✅ |
| 25 | **Ambush Predator** | 3 | Creature: Cat | C | 3/2 | Arrival: This fights up to one target damaged creature you don't control. | Fight removal that only works once something is wounded. Pairs well with Glitterworld pings | ✓ implemented | ✅ |
| 26 | **Regrowth Rain** | 3 | Instant | C | — | Heal 3 from each creature you control. | Mass repair at instant speed: heal after blocks | ✓ implemented | ✅ |
| 27 | **Herd Matriarch** | 4 | Creature: Beast | U | 3/5 | Whenever another creature with 5 or more Power enters the battlefield under your control, put two +1/+1 counters on it. | Makes the big creatures bigger, which also lifts their max Health above chip-damage range | ✓ implemented | ✅ |
| 28 | **Mudwallow Hippo** | 5 | Creature: Beast | C | 3/7 | At the end of your turn, heal 2 from this. | A wall that recovers. Chip damage doesn't stick to it | ✓ implemented | ✅ |
| 29 | **Stampede of the Deep** | 6 | Sorcery | R | — | Creatures you control get +2/+2 and Trample until end of turn. Heal them fully. | A Wild finisher: the whole army attacks fresh. Losing the buff can't kill them afterwards | ✓ implemented | ✅ |
| 30 | **Titanback Colossus** | 8 | Creature: Beast | R | 8/8 | Trample. Whenever this attacks, heal it fully. | Chip damage never adds up on it while it keeps attacking | ✓ implemented | ✅ |

**v0.2 progress:** 10 approved · C 5 · U 3 · R 2
