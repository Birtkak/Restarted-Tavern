# Glitterworld — Card List

**Identity:** a huge, high-tech city that houses every kind of creature in the game: robots, drones, aliens, citizens of every species. A bit of everything, focused on **pings** and **buffing with Equipment**.
**Faction pie:** Equipment buffs ★, removal through **pings** (lots of 1-damage effects, which stick because damage is permanent), healing only for **machines** (Constructs and equipped creatures), big threats built with Equipment. Keyword: Flying (drones). Gold: **Equip is an activated ability, so Equip costs can use Gold** once mana runs out (GAME_DESIGN §5.2). Casting the Equipment itself takes mana, like every permanent. No graveyard play, no fight.
**Role with permanent damage:** Glitterworld **spreads** damage with pings, then finishes the wounded creatures off.

**Token:** *Drone*, a 1/1 Creature: Construct with Flying.
**Equipment rules:** MTG rules. Equip only at sorcery speed. When the creature leaves, the Equipment stays on the battlefield.

**Target:** 20 cards, 9 Common / 6 Uncommon / 4 Rare / 1 Legendary. Curve 3 / 4 / 4 / 3 / 3 / 2 / 1.

Status tags: ✅ approved · 🟡 draft · ✏️ needs changes · ❌ cut

---

| # | Name | Cost | Type | Rarity | Stats | Text | Design notes | Status |
|---|---|---|---|---|---|---|---|---|
| 1 | **Static Shock** | 1 | Instant | C | — | Deal 1 damage to target creature. Draw a card. | A ping that replaces itself | ✅ |
| 2 | **Neon Shiv** | 1 | Equipment | C | — | Equipped creature gets +1/+1. Equip 1. | The basic Equipment | ✅ |
| 3 | **Courier Bot** | 1 | Creature: Construct | C | 1/2 | Arrival: You may attach target Equipment you control to this. | Saves an Equip cost | ✅ |
| 4 | **Spark Drone** | 2 | Creature: Construct | C | 1/1 | Flying. Arrival: Deal 1 damage to a creature. | From the samples | ✅ |
| 5 | **Pulse Blade** | 2 | Equipment | C | — | Equipped creature gets +1/+1 and has "Whenever this creature attacks, deal 1 damage to a creature." Equip 2. | From the samples. A ping engine on any creature | ✅ |
| 6 | **Back-Street Mechanic** | 2 | Creature: Citizen | U | 2/2 | Tap: Heal 2 from target Construct or equipped creature. | Healing for machines, as the faction pie says | ✅ |
| 7 | **Alley Tinker** | 2 | Creature: Citizen | C | 2/2 | Arrival: Create a *Scrap Plating* Equipment token with "Equipped creature gets +0/+2. Equip 1." | Body plus Equipment | ✅ |
| 8 | **Chain Zap** | 3 | Sorcery | C | — | Deal 1 damage to each of up to three target creatures. | Spreads damage across a board | ✅ |
| 9 | **Overclock Rig** | 3 | Equipment | U | — | Equipped creature gets +2/+1 and has "Tap: Deal 1 damage to any target." Equip 2. | Turns any creature into a ping turret | ✅ |
| 10 | **Sky Patrol Drone** | 3 | Creature: Construct | C | 2/3 | Flying. | An evasive Equipment carrier | ✅ |
| 11 | **Arc Welder** | 3 | Creature: Citizen | U | 2/3 | Whenever you pay an Equip cost, deal 1 damage to any target. | Ties the two themes together: equipping causes pings | ✅ |
| 12 | **Rail Cannon** | 4 | Equipment | U | — | Equipped creature gets +3/+0 and has "Whenever this creature attacks, deal 1 damage to a creature." Equip 3. | Heavy weaponry | ✅ |
| 13 | **Riot Suppressor** | 4 | Creature: Construct | U | 3/3 | Arrival: Deal 1 damage to each enemy creature. | A mini-sweeper on a body. Wrecks Goober tokens | ✅ |
| 14 | **Grid Overload** | 4 | Sorcery | R | — | Deal 1 damage to each enemy creature three times. | From the samples. Each ping is a separate damage event | ✅ |
| 15 | **Megacorp Exosuit** | 5 | Equipment | R | — | Equipped creature gets +3/+3 and has Flying and Trample. Equip 3. | Turns any citizen into a big threat | ✅ |
| 16 | **Patrol Captain** | 5 | Creature: Citizen | U | 4/5 | Equipped creatures you control get +1/+1. | An Equipment "lord" | ✅ |
| 17 | **Hover Tank** | 5 | Creature: Construct | C | 4/5 | Arrival: Deal 1 damage to a creature. | A sturdy body with a ping | ✅ |
| 18 | **Orbital Strike Network** | 6 | Relic | R | — | At the start of your turn, deal 1 damage to each enemy creature and each opponent. | A ping engine that slowly grinds the opposing board down | ✅ |
| 19 | **Titan-Frame Guardian** | 6 | Creature: Construct | R | 5/7 | Whenever an Equipment becomes attached to this, heal it fully and draw a card. | A huge Equipment carrier that repairs itself | ✅ |
| 20 | **Archon Lumen, Mind of the City** | 7 | Creature: Construct | L | 5/7 | Flying. Your Equip costs are 0. At the end of your turn, deal 1 damage to any target for each Equipment you control. | The city's AI. Both themes at full power | ✅ |

**Progress:** 20 / 20 · C 9/9 · U 6/6 · R 4/4 · L 1/1
**Curve:** 1-drops: 3 · 2: 4 · 3: 4 · 4: 3 · 5: 3 · 6: 2 · 7+: 1 · Creatures: 11 · Equipment: 5 · Spells/Relics: 4

✅ **Faction complete (v0.1)**

---

## Set v0.2 additions (approved 2026-10-09)

Goal: **more depth for the mechanics already in place**: Gold (banking, spending, the cap), permanent damage and healing, Invest, the Chain, Arrival and Last Breath. Target per faction: +10 cards, 5 Common / 3 Uncommon / 2 Rare.

**Engine** column: ✓ = the engine can run it today; ✓ implemented = it's in the prototype card pool; otherwise it names what's missing (see DEVELOPMENT §7).
New rules terms used here are defined in GAME_DESIGN §5.2 (**bank**) and §11.1 (**damaged**, **can't be healed**).

| # | Name | Cost | Type | Rarity | Stats | Text | Design notes | Engine | Status |
|---|---|---|---|---|---|---|---|---|---|
| 21 | **Gilded Knuckles** | 1 | Equipment | C | — | Equipped creature gets +2/+0. Equip 1. | Cheap to cast; the Equip can be paid with banked Gold | ✓ implemented | ✅ |
| 22 | **Patch-Up Drone** | 2 | Creature: Construct | C | 1/1 | Flying. Arrival: Heal 2 from target Construct or equipped creature. | Machine repair on a drone | ✓ implemented | ✅ |
| 23 | **Scrap Collector** | 2 | Creature: Construct | C | 2/2 | Whenever an Equipment you control becomes unattached, gain 1 Gold. | When an equipped creature dies, you get Gold back to re-equip | ✓ implemented | ✅ |
| 24 | **Finisher Protocol** | 2 | Instant | C | — | Destroy target creature with 2 or less Health remaining. | The ping plan's closer: spread damage, then delete | ✓ implemented | ✅ |
| 25 | **Marksman Scope** | 2 | Equipment | C | — | Equipped creature gets +1/+0 and has "Whenever this deals combat damage to a player, deal 1 damage to a creature." Equip 1. | Ping engine on any attacker | ✓ implemented | ✅ |
| 26 | **Smart Rounds** | 3 | Sorcery | U | — | Deal 1 damage to target creature. Then deal 1 damage to each other creature that already had damage. | Chains across a wounded board, and rewards having pinged before | ✓ implemented | ✅ |
| 27 | **Repair Bay** | 3 | Relic | U | — | At the start of your turn, heal 1 from each Construct and each equipped creature you control. | Machine-only healing, as the faction pie says | ✓ implemented | ✅ |
| 28 | **Drone Launcher** | 3 | Equipment | U | — | Equipped creature has "Whenever this attacks, create a 1/1 Drone with Flying." Equip 2. | Go wide through Equipment | ✓ implemented | ✅ |
| 29 | **Hardlight Aegis** | 4 | Equipment | R | — | Equipped creature gets +0/+3 and can't be dealt more than 2 damage each turn. Equip 2. | Permanent-damage defense: big hits get capped, so wounds pile up slowly | ✓ implemented | ✅ |
| 30 | **Neon Executioner** | 6 | Creature: Construct | R | 4/6 | Whenever an enemy creature is dealt damage, if it has 2 or less Health remaining, destroy it. | Turns every ping into a potential kill. The faction's damage payoff | ✓ implemented | ✅ |

**v0.2 progress:** 10 approved · C 5 · U 3 · R 2

---

## Set v0.3 draft: mana scarcity (🟡 for review, 2026-10-09)

Goal: players should have to count their mana out most rounds. Each faction gets 2 **card draw**, 2 **mana sinks** (X spells, repeatable abilities or a big Invest) and 2 **finishers** (X burn or drains), 2 C / 2 U / 2 R.
**X costs:** the printed cost plus X, chosen on casting. Paid like the card: spells and abilities use mana and Gold (Gold first with Runeterra mana), permanents use mana only. Invest costs stay at 3 or less, so they work with a Gold cap of 3.

| # | Name | Cost | Type | Rarity | Stats | Text | Role · design notes | Status |
|---|---|---|---|---|---|---|---|---|
| 31 | **Market Data Feed** | 2 | Relic | U | — | Whenever an Equipment becomes attached to a creature you control, draw a card. This triggers at most once each turn. | Draw · Equipment ★ | 🟡 |
| 32 | **Overclocked Analyst** | 3 | Creature: Construct | C | 2/3 | (3), Tap: Draw a card, then discard a card. | Draw · mana sink · card selection on a body | 🟡 |
| 33 | **Arc Cascade** | X+1 | Sorcery | U | — | Deal X damage divided as you choose among any number of creatures and/or opponents. | Mana sink · pings ★, scaled up | 🟡 |
| 34 | **Turret Rig** | 2 | Equipment | C | — | Equipped creature has "(2), Tap: Deal 1 damage to any target." Equip 2. | Mana sink · a repeatable ping you move around | 🟡 |
| 35 | **Orbital Laser** | X+2 | Sorcery | R | — | Deal X damage to any target. If X is 5 or more, also deal 1 damage to each enemy creature. | Finisher (X burn) · the city's big gun | 🟡 |
| 36 | **Satellite Uplink** | 6 | Relic | R | — | (3), Tap: Deal 2 damage to each opponent. | Finisher (inevitability) · mana sink · spare mana becomes face damage every round | 🟡 |
