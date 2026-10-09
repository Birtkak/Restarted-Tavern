# Restarted Tavern — Game Design

The game's rules: what players see and do. This is a living document, built up through design sessions.

**Status tags**
- 🔒 **LOCKED**: decided. Change it only on purpose, and log the change in the Decision Log.
- 🟡 **PROPOSED**: the current best idea, not yet confirmed.
- ❓ **OPEN**: still needs a decision.

---

## 1. Vision

A trading-card-style dueling game with a structure close to MTG: zones, a battlefield, a graveyard and exile. It borrows Hearthstone's automatic mana growth, so there is no land screw. Two features set it apart:

1. **Permanent damage.** Creatures carry their wounds from turn to turn. Every fight matters, and healing is a real resource.
2. **Unused mana is not wasted.** Mana you don't spend turns into a second resource that can be saved up.

The game is designed for 1v1 first, with every rule written so that it also works for up to 4 players.

---

## 2. Formats

| Format | Deck size | Copy limit | Players | Starting life | Status |
|---|---|---|---|---|---|
| **Standard** | exactly 60 | max 4 of each card | 2 | 30 | 🔒 |
| Singleton / big deck | ❓ (100?) | 1 of each | 2–4 | ❓ | future |
| Multiplayer Standard | 60 | 4 | 3–4 | 40 🔒 | future |
| Teams (2v2) | 60 | 4 | 4 | ❓ | future (the engine supports teams from the start) |

🟡 Rule-writing principle: card text never says "your opponent". It says "an opponent", "each opponent" or "target opponent", so every card works in multiplayer without needing errata.

---

## 3. Game Setup

| Rule | Value | Status |
|---|---|---|
| Starting life | 30 | 🔒 |
| Starting hand | 7 | 🔒 |
| Maximum hand size | 7 (discard down to 7 at the end of your turn) | 🔒 |
| Mulligan | **London mulligan**: shuffle and draw 7, then put 1 card on the bottom for each mulligan taken | 🔒 |
| Who goes first | Random | 🟡 |
| Going-second compensation | The first player skips their turn-1 draw; the second player starts with **1 Gold** | 🔒 |

---

## 4. Zones 🔒

| Zone | Public? | Ordered? | Notes |
|---|---|---|---|
| Deck (library) | Hidden | Yes | Drawing from an empty deck makes you lose 🔒 |
| Hand | Hidden to opponents | No | |
| Battlefield | Public | No | Creatures and other permanents |
| Graveyard | Public | Yes | Destroyed or used cards |
| Exile | Public (by default) | No | Removed from the game; recursion can't easily reach it |
| Patron zone | Public | No | Holds your Patron, who is your face (§9) |

🟡 Possible extra zone later: a **Stack / Chain** for resolving spells (see §8).

---

## 5. Mana & Gold

### 5.1 Mana crystals 🔒
- At the start of your turn your **max mana goes up by 1** (cap **10**), and your mana refills to max.
- Mana has no color 🔒. Which cards a deck can use is decided by its Patron's factions (§9).

### 5.2 Gold 🔒
Unused mana is not lost.

- At the end of your turn, each point of unspent mana becomes **1 Gold** (a separate counter).
- Gold is **capped at 5** 🔒. Mana over the cap is lost. This allows big save-up turns, so Overcharge costs must be balanced with a 5-Gold burst in mind.
- Gold **can be spent on**:
  - **Instants** (cards played on any player's turn; normal mana works too),
  - **activated abilities** on permanents,
  - **Overcharge X** bonuses (see §11), which can **only** be paid with Gold, and
  - 🟡 your **Patron's power** (see §9).
- Gold **cannot** pay for creatures, sorceries or other main-phase permanents, so it can't be used to ramp out big threats early.
- 🟡 When paying for an Instant, the player chooses how to split the cost between mana and Gold.
- 🔒 **Clarification**: your mana pool is only filled during your own turn. On other players' turns you have **no mana, only Gold**, so Gold is how you cast Instants on opponents' turns.
- 🔒 **Taxes** ("unless they pay N") can be paid with any mix of mana and Gold.
- 🔒 Gold gained above the cap of 5 is lost.

**Why this works**
- It removes the bad feeling of "I held up mana for a trick and the opponent didn't attack". The mana is banked instead of wasted.
- It creates a real choice each turn: develop the board now, or bank for reactions later.
- It works naturally in multiplayer: Gold is how you interact on other players' turns.
- It gives a clean way to compensate the player who goes second (start with 1 Gold).

**Alternatives considered**
- **B. Gold can pay for anything, but converts at a 2:1 ratio.** Simpler, but it turns into generic ramp.
- **C. Gold can only be spent on "Overcharge" bonuses** (cards with an extra effect if you pay X Gold). Very clean design space, but it's narrow on its own.
- 🟡 **Combining A and C** is possible: Gold pays for instants and abilities, and also feeds Overcharge.

🔒 Named **Gold**: unused mana is "banked" as money, which fits the tavern (paying your tab) and the Shadow Money Wizards.

---

## 6. Turn Structure 🔒

1. **Start phase**: raise max mana by 1 (to a max of 10), refill mana, untap your permanents, trigger "at start of turn" effects.
2. **Draw phase**: draw 1 card (the first player skips their draw on turn 1).
3. **Main phase 1**: play creatures, sorceries and permanents; activate abilities.
4. **Combat phase**: declare attackers, then declare blockers, then deal damage (see §7).
5. **Main phase 2**: the same as main phase 1. Lets you react to the result of combat (for example, finish off wounded creatures).
6. **End phase**: "at end of turn" effects trigger, you discard down to 7 cards, then unspent mana turns into Gold (up to the cap of 5).

There is **no automatic healing** at end of turn (see §7.3).

---

## 7. Creatures & Combat

### 7.1 Stats
Creatures have **Power / Health**. Damage is tracked as **Health remaining**, not as damage marked on the card.

### 7.2 Combat model 🔒 MTG-style blocking
1. **Declare attackers**: the active player taps untapped, non-summoning-sick creatures to attack. Each attacker attacks a **player** (in multiplayer, the attacker picks which opponent for each creature).
2. **Response window** (§8).
3. **Declare blockers**: each defending player assigns their untapped creatures as blockers. 🟡 Each blocker blocks one attacker; one attacker can be blocked by several blockers.
4. **Response window** (§8).
5. **Damage**: all combat damage is dealt at the same time. Unblocked attackers damage the player they attacked. Blocked attackers and their blockers damage each other.
6. 🟡 When several creatures block one attacker, the attacker's controller divides its damage among the blockers however they like (simpler than MTG's damage-assignment order).

🟡 Blocking does not tap the blocker.
🟡 Damage dealt to a creature that has no Health left beyond what it needed to die is lost, unless the attacker has **Trample**.

### 7.3 Permanent damage 🔒
Damage stays on a creature until it is healed or the creature dies. Health can't go above its max unless an effect says so.

Design consequences:
- Chip damage builds up, so big creatures are worn down over time instead of being a wall you have to "answer or lose to".
- Healing becomes a real card role. It needs to be common enough that big creatures aren't just liabilities.
- 🔒 **No built-in healing.** Healing only comes from cards: healing spells, **Lifelink**-style effects and Patron powers.
- Design rule 🟡: every faction needs *some* answer to accumulated damage (healing, sacrifice-for-value, or just cheap creatures you don't mind losing), so that no faction is stuck with crippled creatures.
- Damage needs clear UI support: show current/max health.

### 7.4 Summoning sickness 🟡
Creatures can't attack the turn they enter the battlefield (unless they have **Haste**). They *can* block right away.
🟡 The same rule applies to **Tap abilities** (abilities whose cost includes tapping the creature): they can't be used the turn the creature arrives, unless it has Haste.

---

## 8. Timing & Responses 🔒 The Chain (full back-and-forth)
- When a player plays a spell, activates an ability or uses their Patron Power, it goes on top of the **Chain**.
- **Priority** then passes around the table in turn order. Whoever has priority may add **one** Instant or ability to the Chain, or pass.
- After anything is added, priority goes around the table again, so players can respond to responses as long as they want.
- When **all players pass in a row**, the **top** item of the Chain resolves (last in, first out). Then the active player gets priority again, and the loop continues until the Chain is empty.
- If a spell's targets are no longer valid when it resolves, it **fizzles** (it goes to the graveyard and does nothing).
- Triggered abilities (Arrival, Last Breath…) also go on the Chain, so they can be responded to.
- **Fixed windows** where players get priority even when the Chain is empty: 🟡 each main phase, the start of combat, after attackers are declared, after blockers are declared, and the end phase.
- 🟡 UX note: the client should auto-pass for players who have no legal response (or who choose "auto-pass this turn"), so the back-and-forth stays fast, especially with 4 players.

---

## 9. Patrons & Factions

### 9.1 The Patron is your face 🔒
Every deck is led by a **Patron**, a tavern regular you play *as*. The Patron is not part of the 60 cards.

- The Patron **is the player**: your 30 life is the Patron's life, and "attack a player" means attacking their Patron.
- The Patron sits in the **Patron zone** (public). In v0.1 it can't be removed from the game.
- **Patron Power**: each Patron has a unique activated power, **paid with Gold**. 🟡 It can be used once per turn, at instant speed (so it can also be used on opponents' turns, through the Chain).
- **Passive**: 🟡 each Patron has one always-on ability.
- 🔒 The Patron **never attacks or blocks**, and Equipment only goes on creatures. Combat is entirely about creatures.
- Future singleton format: the Patron becomes the commander-style deck leader.

### 9.2 Factions 🔒
There are **5 factions**. Every card belongs to one faction or is **Neutral** (playable in any deck). Each Patron unlocks a **fixed pair** of factions, giving 10 possible pairs. 🟡 There may be several Patrons per pair over time.

| Faction | Inspired by | Identity | Plays like |
|---|---|---|---|
| **Shadow Money Wizards** | — | Shady wizards who deal in money and magic | Big, impressive spells; wizard creatures that bring **value** (card draw, Gold, effects) but are rarely game-enders themselves. **Shady deals** 🔒: trading life or cards for Gold, or handing opponents Gold in exchange for a big effect |
| **Goobers** | MTG red + LoR Noxus | Goblins | Aggressive, wide boards, Haste, burn, raw strength, chaos |
| **Sensationalists** | MTG black + LoR Shadow Isles | Occult humans | Death, sacrifice, graveyard, draining life, Last Breath, curses |
| **Evergrowing Wild** | MTG green + LoR Freljord | A jungle planet, from tiny critters to the biggest, scariest creatures in the universe | Creatures that grow, huge bodies, **Trample**, toughness |
| **Glitterworld** | — | A huge high-tech city that houses every kind of creature in the game | A bit of everything, but focused on **pings** (small direct damage, which sticks because damage is permanent) and **buffing with Equipment** |

🟡 How each faction relates to permanent damage:
- **Goobers** create damage (and accept losing their small creatures).
- **Glitterworld** spreads damage with pings, then finishes damaged creatures off.
- **Wild** creatures are big enough to *survive* chip damage.
- **Sensationalists** don't care about damage: they profit when things die.
- **Wizards** avoid combat and win through value and spells.
- 🟡 Healing (cards only) is spread out: Wild (creatures that endure), Sensationalists (drain/lifelink), Glitterworld (repair *machines*).

### 9.3 Patron drafts (one per pair) 🟡
These are first flavor drafts, with numbers to be tuned in playtesting. "Power (N)" means it costs N Gold.

| Pair | Patron | Passive | Power |
|---|---|---|---|
| Wizards + Goobers | **Grizzle Coinflick**, goblin pyromancer-for-hire | Whenever you cast a spell that costs 6+, create a 1/1 Goober | (2) Deal 1 damage to any target |
| Wizards + Sensationalists | **Madame Vesper**, the debt collector | Whenever a creature an opponent controls dies, gain 1 Gold | (3) Draw a card and lose 2 life |
| Wizards + Wild | **Old Mossbank**, the druid banker | Your creatures with 6+ Power cost 1 less | (2) Give a creature +2/+2 until end of turn |
| Wizards + Glitterworld | **Auditor Prime**, a construct accountant | Your Overcharge costs are 1 lower (minimum 1) | (1) Look at the top card of your deck; you may put it on the bottom |
| Goobers + Sensationalists | **Skabba**, goblin cult chieftain | Whenever one of your creatures dies, deal 1 damage to each opponent | (1) Sacrifice a creature: draw a card |
| Goobers + Wild | **Mukk the Grub King**, a goblin riding a giant beast | Your creatures with Trample get +1 Power | (2) A creature you control gains Trample until end of turn |
| Goobers + Glitterworld | **Sparkwrench**, goblin mechanic | Equipment costs 1 less | (2) Move an Equipment to another creature you control |
| Sensationalists + Wild | **The Rotmother**, a jungle witch of rot and rebirth | When a creature with 5+ Power you control dies, create a 2/2 Spawn | (3) Return a creature card from your graveyard to your hand, then lose 3 life |
| Sensationalists + Glitterworld | **Vox Nocturne**, a cult leader who broadcasts horror live on the city's screens | Whenever an opponent's creature dies, you gain 1 life | (2) Deal 1 damage to a creature; if it dies, draw a card |
| Wild + Glitterworld | **Keeper Z-00**, the city's zookeeper unit | Your creatures with 5+ Health enter with a +1/+1 counter | (2) Heal 3 from a creature |

---

## 10. Card Types 🔒
| Type | When played | Goes to | Notes |
|---|---|---|---|
| **Creature** | Your main phase | Battlefield | Has Power / Health; damage is permanent |
| **Sorcery** | Your main phase, with an empty Chain | Graveyard | |
| **Instant** | Whenever you have priority (§8); can be paid with Gold | Graveyard | |
| **Equipment** | Your main phase | Battlefield | **Equip X** (main phase): attach to a creature you control. When the creature leaves, the Equipment stays on the battlefield unattached. Glitterworld's core type |
| **Relic** | Your main phase | Battlefield | A non-creature permanent with ongoing effects and/or activated abilities |
| **Curse** | Your main phase | Battlefield, attached to an **enemy creature or opponent** | A negative ongoing effect. Goes to the graveyard if what it's attached to leaves. Sensationalists' core type |

Locations were considered and rejected for now (they may return later).

---

## 11. Keywords (starting set) 🟡
Kept deliberately **small**. 🔒 Trample is the only damage-related core keyword; mechanics like Bloodied, Wound and Pristine were considered and rejected for now.

| Keyword | Meaning | Status |
|---|---|---|
| **Trample** | Combat damage beyond what's needed to kill the blockers goes to the attacked player | 🔒 |
| **Haste** | Can attack the turn it enters | 🟡 |
| **Flying** | Can only be blocked by Flying / Reach | 🟡 |
| **Lifelink** | Damage dealt also heals its controller | 🟡 |
| **Overcharge X: …** | Optional extra cost, paid only with Gold, for a bonus effect | 🔒 |
| **Arrival** | Triggers when this creature enters the battlefield | 🟡 |
| **Last Breath** | Triggers when this creature dies | 🟡 |
| **Equip X** | (Glitterworld) Pay X: attach this Equipment to a creature you control. Main phase only | 🟡 |

---

## 12. Win / Loss
- A player at 0 or less life loses.
- A player who has to draw from an empty deck loses. 🔒
- Multiplayer: last player standing (see §13).

---

## 13. Multiplayer (3–4 players) 🔒 (future format)
- **Seating and turns**: turn order goes clockwise. Priority on the Chain also goes clockwise, starting from the active player.
- **Starting life: 40.**
- **Turn order compensation: none.** Every player draws on their first turn, including the first player (unlike 1v1).
- **Attacking**: free-for-all. **Each attacking creature chooses any opponent** to attack. Each defending player only declares blockers against attackers that are attacking *them*.
- **Elimination** 🟡: when a player loses, they leave the game. All cards they **own** leave with them, any of their spells or abilities on the Chain are removed, and control of anything of theirs that someone else controls ends. Effects that player controlled stop ("until end of turn" effects end immediately).
- 🟡 Card wording for multiplayer: "each opponent", "target opponent", "the player to your left/right". Effects like "each player" include you.
- 🟡 Politics are allowed (deals, threats), but deals are not binding in the rules.

### 13.1 Teams (2v2) ❓ future
- The engine gives every player a `teamId` from the start.
- 🟡 Draft idea: teammates sit across from each other, so turns alternate between teams. Life, Gold and hands are separate. You can't attack or target your teammate with "opponent" effects.

---

## Decision Log
| Date | Decision |
|---|---|
| 2026-10-09 | Faction pie locked (CARD_DESIGN §4). Multiplayer: free-for-all attacks, 40 life, no turn-order compensation, teams (2v2) planned for later. |
| 2026-10-09 | Rarities: Common/Uncommon/Rare/Legendary (Legendary rule: only one with a given name on the battlefield). Vanilla stats = 2×cost+1. First set ~120 cards. See CARD_DESIGN.md. |
| 2026-10-09 | Two main phases. Chain uses full back-and-forth priority. Permanent types: Equipment, Relics, Curses (no Locations). London mulligan. |
| 2026-10-09 | Reserve renamed **Gold**. Shadow Money Wizards do money through *shady deals*. Patrons don't fight. The Patron drafts are a good direction. |
| 2026-10-09 | Five factions: Shadow Money Wizards, Goobers, Sensationalists, Evergrowing Wild, Glitterworld. The Patron is the player's face. Trample is the only damage-related core keyword. The second player starts with 1 Gold. |
| 2026-10-09 | Healing only through cards (no built-in rule). Gold capped at 5. 5 factions, each Patron unlocks a fixed pair, plus Neutral cards. Max hand size 7. Drawing from an empty deck makes you lose. |
| 2026-10-09 | Gold pays for Instants, activated abilities, Overcharge (Gold only) and Patron powers. Combat uses MTG-style blocking. Timing uses a single response Chain. Deck identity comes from a Patron card plus factions. |
| 2026-10-09 | Repo restarted from scratch. Locked: Standard = 60 cards, max 4 copies, 30 life, 7-card hand, +1 mana per turn up to 10, zones Deck/Hand/Battlefield/Graveyard/Exile, damage on creatures is permanent. Future goals: singleton/big-deck format, up to 4 players. |
