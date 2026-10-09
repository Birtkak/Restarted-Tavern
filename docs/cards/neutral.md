# Neutral — Card List

**Identity:** the Tavern itself: barkeeps, regulars, sellswords and drinks. Playable in **every** deck.
**Role:** fill gaps that the faction pie leaves open, without outshining faction cards. Neutral cards are a little less efficient than faction cards on purpose.
- **Healing for everyone.** Healing is card-only, so every deck needs access to some.
- **An answer to non-creature permanents.** No faction removes Equipment, Relics or Curses well, so Neutral has a generic answer.
- **Solid bodies**, and a **Gold sink** that any deck can use.

**Token:** *Mercenary*, a 2/2 Creature: Human.

**Target:** 10 cards, 5 Common / 3 Uncommon / 2 Rare.

Status tags: ✅ approved · 🟡 draft · ✏️ needs changes · ❌ cut

---

| # | Name | Cost | Type | Rarity | Stats | Text | Design notes | Status |
|---|---|---|---|---|---|---|---|---|
| 1 | **Barkeep's Tonic** | 1 | Instant | C | — | Heal 3 from a creature or your Tavern Dweller. Invest 1: Draw a card. | From the samples. Healing for every deck | ✅ |
| 2 | **Pot Boy** | 1 | Creature: Human | C | 1/2 | Arrival: Heal 1 from a creature. | A small helper | ✅ |
| 3 | **Hired Sellsword** | 2 | Creature: Human | C | 2/3 | — | A plain, honest 2-drop | ✅ |
| 4 | **Last Call** | 2 | Instant | C | — | Destroy target Equipment, Relic or Curse. | The generic answer to non-creature permanents | ✅ |
| 5 | **Tavern Bouncer** | 3 | Creature: Human | C | 2/5 | — | From the samples. A defensive wall | ✅ |
| 6 | **Round on the House** | 3 | Sorcery | U | — | Each player draws a card. Then you draw a card. | Multiplayer politics: everyone gets something, you get more | ✅ |
| 7 | **Wandering Adventurer** | 4 | Creature: Human | U | 3/4 | Arrival: Draw a card. | A value body for any deck | ✅ |
| 8 | **Old Tavern Keeper** | 4 | Creature: Human | R | 2/5 | At the end of your turn, heal 2 from each other creature you control. | A healing engine for every deck. Rare, so it's limited | ✅ |
| 9 | **Mercenary Contract** | 4 | Relic | R | — | Pay 2 Gold: Create a 2/2 Mercenary. Activate only once per turn and only as a sorcery. | A Gold sink for every deck. Turns leftover mana into bodies | ✅ |
| 10 | **Retired Champion** | 5 | Creature: Human | U | 5/6 | Can block an additional creature each combat. | An old arena hero who still holds the line | ✅ |

**Progress:** 10 / 10 · C 5/5 · U 3/3 · R 2/2

✅ **Complete (v0.1)**

---

## Set v0.2 additions (approved 2026-10-09)

Goal: **more depth for the mechanics already in place**: Gold (banking, spending, the cap), permanent damage and healing, Invest, the Chain, Arrival and Last Breath. Target for Neutral: +20 cards, 10 Common / 6 Uncommon / 4 Rare. Focus: **Gold sinks** for every deck (simulations showed Gold piling up with nothing to buy), healing, and generic interaction.

**Engine** column: ✓ = the engine can run it today; ✓ implemented = it's in the prototype card pool; otherwise it names what's missing (see DEVELOPMENT §7).
New rules terms used here are defined in GAME_DESIGN §5.2 (**bank**) and §11.1 (**damaged**, **can't be healed**).

| # | Name | Cost | Type | Rarity | Stats | Text | Design notes | Engine | Status |
|---|---|---|---|---|---|---|---|---|---|
| 11 | **Cellar Rat** | 1 | Creature: Rat | C | 1/2 | Last Breath: Gain 1 Gold. | A weaker Gilded Rat (Wizards) for every deck | ✓ implemented | ✅ |
| 12 | **Spilled Drink** | 1 | Instant | C | — | Tap target creature. Invest 1: Draw a card. | Stops an attacker or clears a blocker at instant speed. The Gold-paid bonus makes it a full card | ✓ implemented | ✅ |
| 13 | **Tip Jar** | 1 | Relic | C | — | Pay 3 Gold: Draw a card. Activate only once per turn. | A Gold sink for every deck. Simulations showed Gold piling up at the cap with nothing to buy | ✓ implemented | ✅ |
| 14 | **Field Medic** | 2 | Creature: Human | C | 1/3 | Arrival: Heal 2 from target creature. | Healing for every deck, on a body | ✓ implemented | ✅ |
| 15 | **Called Shot** | 2 | Instant | C | — | Deal 2 damage to target attacking or blocking creature. | Generic combat removal, weaker than faction burn. The damage stays if it doesn't kill | ✓ implemented | ✅ |
| 16 | **House Special** | 2 | Sorcery | C | — | Draw a card. Invest 3: Draw two more cards. | A Gold sink at sorcery speed. Turns a banked 3 Gold into cards | ✓ implemented | ✅ |
| 17 | **Tavern Recruiter** | 3 | Creature: Human | C | 2/3 | Invest 2: Create a 2/2 Mercenary. | An Invest creature for every deck | ✓ implemented | ✅ |
| 18 | **Hired Muscle** | 3 | Creature: Human | C | 3/3 | Pay 2 Gold: This gets +2/+0 until end of turn. Activate only once per turn. | A Gold-fuelled combat threat: a bluff with Gold up | ✓ implemented | ✅ |
| 19 | **Doorman** | 4 | Creature: Human | C | 3/5 | Arrival: Gain 1 Gold. | A solid body that tops up Gold | ✓ implemented | ✅ |
| 20 | **Caravan Guard** | 5 | Creature: Human | C | 4/6 | Reach. | An anti-flyer for every deck. Big Health shrugs off pings | ✓ implemented | ✅ |
| 21 | **Bar Brawl** | 3 | Sorcery | U | — | Deal 1 damage to each creature. | Symmetric chip damage. It hurts Goober tokens and Critters most, and wounds everything else | ✓ implemented | ✅ |
| 22 | **Scarred Veteran** | 3 | Creature: Human | U | 2/4 | This gets +1/+0 for each damage on it. | Permanent-damage depth: wounds make it hit harder, so healing it is a real choice | ✓ implemented | ✅ |
| 23 | **Traveling Bard** | 2 | Creature: Human | U | 2/2 | Whenever you spend 3 or more Gold on a single spell or ability, draw a card. | Rewards big Gold moves (Invest, Equipment paid with Gold) | ✓ implemented | ✅ |
| 24 | **Wound Dresser** | 3 | Creature: Human | U | 2/3 | Pay 2 Gold: Heal 2 from target creature. Activate only once per turn. | Repeatable healing paid with Gold, at instant speed | ✓ implemented | ✅ |
| 25 | **Settle the Tab** | 3 | Sorcery | U | — | As an extra cost, pay any amount of Gold (X). Draw X cards, then discard a card. | Cash in a full bank for cards | ✓ implemented | ✅ |
| 26 | **Dice Game** | 2 | Sorcery | U | — | Each player may pay any amount of Gold. The player who paid the most draws two cards. If players tie for the most, each of them draws one card. | Multiplayer politics: a Gold auction | simultaneous choices | ✅ |
| 27 | **Shady Moneylender** | 3 | Creature: Human | R | 2/3 | You may spend Gold as though it were mana to cast creature spells. Whenever you do, each opponent gains 1 Gold. | Breaks the 'Gold can't buy creatures' rule for a price. A test of how strong that rule is | payment rule + trigger | ✅ |
| 28 | **Champion's Belt** | 3 | Equipment | R | — | Equipped creature gets +2/+2. Whenever equipped creature destroys a creature in combat, heal it fully. Equip 3. | Equipment for every deck. A champion who wins fights stays fresh | ✓ implemented | ✅ |
| 29 | **Grizzled Innkeeper** | 4 | Creature: Human | R | 3/5 | Whenever you bank Gold, heal that much from target creature you control. | Banking and healing in one card: not spending mana repairs your board | ✓ implemented | ✅ |
| 30 | **Tavern Brawl Night** | 5 | Sorcery | R | — | Deal 2 damage to each creature. Then heal 2 from each creature you control. | Rules depth: state-based actions are only checked after the spell, so your creatures at 2 Health survive while theirs die | ✓ implemented | ✅ |

**v0.2 progress:** 20 approved · C 10 · U 6 · R 4
