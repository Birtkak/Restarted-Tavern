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

## 1.1 Rules Foundation: MTG by default 🔒
**Anything this document does not cover follows the Magic: The Gathering Comprehensive Rules.** That includes the stack and priority, state-based actions, layers for continuous effects, replacement effects, the combat step structure, attachments (Equipment and Auras/Curses), tokens, copies, control-changing effects, the Legendary rule and multiplayer elimination.

This game only **deviates** from MTG in these areas:

| Area | MTG | Restarted Tavern |
|---|---|---|
| **Mana** | Lands, colored mana, mana empties between steps | Colorless mana crystals: +1 max per turn up to 10, refilled each turn (§5.1). No lands |
| **Gold** | — | Unspent mana becomes Gold (cap 5). Gold pays for Instants, Sorceries and activated abilities (mana is used first), and Invest is paid only with Gold. Permanents are mana only (§5.2) |
| **Damage** | Damage wears off in the cleanup step; toughness | **Health**. Damage is permanent until healed (§7.3), and **Heal** is a game action. A Health buff ending can't kill a creature (§7.3) |
| **Life gain** | Uncapped | Gaining life (Lifelink, drains) can't take you above your starting life (§11.1) |
| *Format numbers* | 20 life | 30 life (40 in multiplayer), 60 cards, 4 copies |
| *Deck identity* | Colors (Commander uses a command zone) | A **Tavern Dweller** in the Tavern Dweller zone sets the factions and acts as the player's face, with a Power paid like any activated ability (§9). It works like a commander that never enters the battlefield |
| *Going second* | The first player skips their draw | Same as MTG (no other compensation) |

Renamed terms, which work exactly as in MTG: the **Chain** is the stack, **Arrival** is an enters-the-battlefield trigger, **Last Breath** is a dies trigger, **Relic** is a non-creature artifact or enchantment, a **Curse** is an Aura attached to an enemy creature or player, and **Health** is toughness (plus remaining-damage tracking).

When a rule here and the MTG rules conflict, **this document wins**. When this document is silent, look up the MTG Comprehensive Rules.

**MTG is the backbone, not a cage.** When a new idea comes up, present options: the MTG default next to alternatives, so we can choose what works best for this game. Any change away from MTG gets added to the table above and to the Decision Log.

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
| Going-second compensation | **MTG default: the first player skips their turn-1 draw.** No bonus mana or Gold for the second player. Chosen on 2026-10-09 after trying "1 starting Gold" and "+1 first-turn mana" in simulations. Bots showed a first-player edge (playtest/RULES_REVIEW.md §1), but bots race more than people do, so human playtests will judge it. Since 2026-10-10 the round leader rotates (§6.1), so who goes first alternates every round | 🔒 |

---

## 4. Zones 🔒

| Zone | Public? | Ordered? | Notes |
|---|---|---|---|
| Deck (library) | Hidden | Yes | Drawing from an empty deck makes you lose 🔒 |
| Hand | Hidden to opponents | No | |
| Battlefield | Public | No | Creatures and other permanents |
| Graveyard | Public | Yes | Destroyed or used cards |
| Exile | Public (by default) | No | Removed from the game; recursion can't easily reach it |
| Tavern Dweller zone | Public | No | Holds your Tavern Dweller, who is your face (§9) |

The **Chain** (MTG: the stack) holds spells and abilities while they wait to resolve (§8).

---

## 5. Mana & Gold

Since 2026-10-10 the Standard rules use **Legends of Runeterra-style mana**, adapted to full turns (locked after the first human playtest asked for slower mana and more interaction). The old rules (mana per turn, mana first, Gold cap 5) are kept in the engine as `FormatConfig.Classic()` for comparisons.

### 5.1 Mana crystals 🔒
- A **round** is one turn for each player (§6.1). At the start of each round **every player's max mana goes up by 1** (cap **10**), and their mana refills to max.
- Mana lasts the **whole round**: you can spend it on Instants, activated abilities and Tavern Dweller Powers during other players' turns too. Permanents and Sorceries are still cast only in your own main phase.
- Mana has no color 🔒. Which cards a deck can use is decided by its Tavern Dweller's factions (§9).

### 5.2 Gold 🔒
Unused mana is not lost.

- At the **end of the round**, each point of every player's unspent mana becomes **1 Gold** (a separate counter): Runeterra's spell mana.
- Gold is **capped at 3** 🔒 (5 until 2026-10-10). Mana over the cap is lost. Invest costs stay at 3 or less.
- 🔒 **What Gold can pay for**:

  | Cost | Mana | Gold |
  |---|---|---|
  | Casting a **permanent** (creature, Equipment, Relic, Curse): anything that enters the battlefield | ✅ | ❌ |
  | **Instants** and **Sorceries** | ✅ | ✅ |
  | **Activated abilities**, including **Equip** and **Tavern Dweller Powers** | ✅ | ✅ |
  | **Invest X** (§11) | ❌ | ✅ **only Gold** |

- 🔒 **Gold is always used first** (decided 2026-10-10, like Runeterra's spell mana). When a cost can be paid with both, your Gold is spent first automatically, and mana pays only what Gold can't. There's no choosing the split. (Invest is separate: it is always paid with Gold.)
  - Gold can never pay for permanents, so spending it first is always right: casting a Sorcery before a creature can't use up the creature's mana any more (this was RULES_REVIEW #6 under the old mana-first rule).
  - Example: with 2 mana and 3 Gold, a 4-cost Sorcery uses the 3 Gold and then 1 mana.
- 🔒 Gold can't pay for permanents, so it can't be used to ramp out threats early. A few cards break this on purpose, and mana is still spent first: **Retainer Mage** (Gold can pay for it whenever it's cast, decided 2026-10-09) and **Shady Moneylender** (Gold can pay for your creature spells). **Silent Partner** lets mana pay for Invest (mana first, then Gold).
- 🔒 **Clarification**: the mana pool fills when a round starts and lasts until it ends, so mana you didn't spend on your own turn can still pay for Instants on an opponent's turn in the same round. What's left at the end of the round is banked as Gold.
- 🔒 **Taxes** ("unless they pay N") can be paid with mana and Gold (mana first).
- 🔒 Gold gained above the cap of 3 is lost.
- 🔒 **Bank** (rules term, introduced with set v0.2): when unspent mana becomes Gold at the end of the round, you **bank** the Gold you actually gain (mana lost to the cap isn't banked). Cards can say "Whenever you bank Gold" or "Whenever you bank 2 or more Gold". These trigger in the cleanup step. As in MTG 514.3a, players then get priority, and the cleanup step repeats afterwards.
- 🔒 Some cards change a player's Gold cap ("Your Gold cap is 8"). The cap is a per-player value that starts at the format's cap. If several effects set it, the newest one wins (MTG timestamp order).
- 🔒 **When a cap goes down** (the card that raised it leaves), Gold above the new cap is **lost at once**, checked like a state-based action (decided 2026-10-09). The cap always means the cap.
- 🔒 **Spend Gold** (rules term): a player spends Gold when they pay Gold for a cost: a spell, an activated ability, Invest, "pay any amount of Gold", or a tax. "Whenever you spend Gold" triggers **once per payment**, however much Gold it was; "3 or more Gold on a single spell or ability" looks at that one payment (decided 2026-10-09). Gold that is lost or taken (Tax Office, Debt Collector, Grand Heist) is not spent.
- 🔒 **"Gold equal to its cost"** (Counterfeit Coin, Golden Handshake, Hostile Takeover) means the **printed cost** (MTG mana value). Discounts and Invest don't change it (decided 2026-10-09).

**Why this works**
- It removes the bad feeling of "I held up mana for a trick and the opponent didn't attack". The mana is banked instead of wasted.
- It creates a real choice each turn: develop the board now, or bank for reactions later.
- It works naturally in multiplayer: Gold is how you interact on other players' turns.
- ~~It gives a clean way to compensate the player who goes second (start with 1 Gold).~~ Simulations showed starting Gold barely helps, because Gold can't buy creatures. The second player gets first-turn mana instead (§3).

**Alternatives considered**
- **B. Gold can pay for anything, but converts at a 2:1 ratio.** Simpler, but it turns into generic ramp.
- **C. Gold can only be spent on "Invest" bonuses** (cards with an extra effect if you pay X Gold). Very clean design space, but it's narrow on its own.
- ✅ What we chose combines A and C: Gold pays for Instants, Sorceries and abilities (after mana), and is the only way to pay for Invest.

🔒 Named **Gold**: unused mana is "banked" as money, which fits the tavern (paying your tab) and the Shadow Money Wizards.

---

## 6. Turn Structure 🔒

1. **Start phase**: if this turn starts a round, every player raises max mana by 1 (to a max of 10) and refills (§5.1). Untap your permanents, trigger "at start of turn" effects.
2. **Draw phase**: draw 1 card (in 1v1 the first player skips their draw on turn 1, §3).
3. **Main phase 1**: play creatures, sorceries and permanents; activate abilities.
4. **Combat phase**: declare attackers, then declare blockers, then deal damage (see §7).
5. **Main phase 2**: the same as main phase 1. Lets you react to the result of combat (for example, finish off wounded creatures).
6. **End phase**: "at end of turn" effects trigger, you discard down to 7 cards. If this turn ends the round, every player's unspent mana turns into Gold (up to the cap of 3).

There is **no automatic healing** at end of turn (see §7.3).

### 6.1 Rounds and the attack token 🔒 (Legends of Runeterra, 2026-10-10)
- A **round** is one turn for each player. The player who takes the first turn of a round is the **round leader**.
- The round leader **rotates**: in 1v1 the turn order is A B | B A | A B ... So each player takes two turns in a row across the round boundary, and going first is shared out.
- **Attack token**: only the round leader may declare attackers in that round. The other player's turn is for building, Instants and blocking next round.
- Summoning sickness stays (§7.4).

---

## 7. Creatures & Combat

### 7.1 Stats
Creatures have **Power / Health**. Damage stays on the creature (it isn't removed at end of turn), and the card shows **Health remaining** = max Health − damage.

### 7.2 Combat model 🔒 MTG-style blocking
1. **Declare attackers**: the active player taps untapped, non-summoning-sick creatures to attack. Each attacker attacks a **player** (in multiplayer, the attacker picks which opponent for each creature).
2. **Response window** (§8).
3. **Declare blockers**: each defending player assigns their untapped creatures as blockers. 🟡 Each blocker blocks one attacker; one attacker can be blocked by several blockers.
4. **Response window** (§8).
5. **Damage**: all combat damage is dealt at the same time. Unblocked attackers damage the player they attacked. Blocked attackers and their blockers damage each other.
6. 🔒 When several creatures block one attacker, the attacker's controller divides its damage among the blockers however they like (like MTG since 2024, no damage-assignment order). A creature blocking several attackers divides its damage the same way, chosen by its controller. With **Trample**, damage only goes to the player once every blocker has been assigned lethal damage (MTG 702.19b). The attacking player divides first, then each defender in turn order (MTG 510.1). The game only asks when the creature can't give each of them lethal damage; otherwise each gets lethal damage and the rest goes to the player (Trample) or onto the first blocker.

🟡 Blocking does not tap the blocker.
🟡 Damage dealt to a creature that has no Health left beyond what it needed to die is lost, unless the attacker has **Trample**.

### 7.3 Permanent damage 🔒
Damage stays on a creature until it is healed or the creature dies. Health can't go above its max unless an effect says so.

🔒 **Losing a buff can't kill.** When a Health bonus ends (an "until end of turn" effect wears off, or the lord or anthem giving it leaves the battlefield), a creature that was alive keeps at least 1 Health: its damage is lowered to max Health − 1. Real damage and real Health penalties still kill. For example, a 2/3 with 2 damage gets +0/+2, then takes 2 more (a 2/5 with 4 damage). When the buff ends, it becomes a 2/3 with 2 damage, at 1 Health left. In MTG it would die.

Design consequences:
- Chip damage builds up, so big creatures are worn down over time instead of being a wall you have to "answer or lose to".
- Healing becomes a real card role. It needs to be common enough that big creatures aren't just liabilities.
- 🔒 **No built-in healing.** Healing only comes from cards: healing spells, **Lifelink**-style effects and Tavern Dweller powers.
- Design rule 🟡: every faction needs *some* answer to accumulated damage (healing, sacrifice-for-value, or just cheap creatures you don't mind losing), so that no faction is stuck with crippled creatures.
- Damage needs clear UI support: show current/max health.

### 7.4 Summoning sickness 🔒 (MTG)
Creatures can't attack the turn they enter the battlefield (unless they have **Haste**). They *can* block right away.
🔒 The same rule applies to **Tap abilities** (abilities whose cost includes tapping the creature): they can't be used the turn the creature arrives, unless it has Haste (MTG 302.6). Non-creature permanents can use Tap abilities right away.

---

## 8. Timing & Responses 🔒 The Chain (full back-and-forth)
- When a player plays a spell, activates an ability or uses their Tavern Dweller Power, it goes on top of the **Chain**.
- **Priority** then passes around the table in turn order. Whoever has priority may add **one** Instant or ability to the Chain, or pass.
- After anything is added, priority goes around the table again, so players can respond to responses as long as they want.
- When **all players pass in a row**, the **top** item of the Chain resolves (last in, first out). Then the active player gets priority again, and the loop continues until the Chain is empty.
- If **all** of a spell's targets are no longer valid when it resolves, it **fizzles** (it goes to the graveyard and does nothing). If only some are, it resolves and skips the illegal ones (MTG 608.2b).
- Triggered abilities (Arrival, Last Breath…) also go on the Chain, so they can be responded to.
- **Fixed windows** where players get priority even when the Chain is empty: 🟡 each main phase, the start of combat, after attackers are declared, after blockers are declared, and the end phase.
- 🟡 UX note: the client should auto-pass for players who have no legal response (or who choose "auto-pass this turn"), so the back-and-forth stays fast, especially with 4 players.

---

## 9. Tavern Dwellers & Factions

### 9.1 The Tavern Dweller is your face 🔒
Every deck is led by a **Tavern Dweller**, a tavern regular you play *as*. The Tavern Dweller is not part of the 60 cards.

- The Tavern Dweller **is the player**: your 30 life is the Tavern Dweller's life, and "attack a player" means attacking their Tavern Dweller.
- The Tavern Dweller sits in the **Tavern Dweller zone** (public). In v0.1 it can't be removed from the game.
- **Tavern Dweller Power**: each Tavern Dweller has a unique activated power, paid with mana and/or Gold like any activated ability (mana first, §5.2) 🔒. 🔒 It can be used **once each turn** (MTG "once each turn": once on your turn and once on each opponent's turn), at instant speed, through the Chain, so opponents can respond to it.
- **Passive**: 🟡 each Tavern Dweller has one always-on ability: a triggered ability, a static ability, or a cost change. It works from the Tavern Dweller zone.
- 🔒 **Deck rule**: every deck has exactly one Tavern Dweller, and every card in it is from one of the Tavern Dweller's two factions or Neutral.
- 🔒 The Tavern Dweller **never attacks or blocks**, and Equipment only goes on creatures. Combat is entirely about creatures.
- Future singleton format: the Tavern Dweller becomes the commander-style deck leader.

### 9.2 Factions 🔒
There are **5 factions**. Every card belongs to one faction or is **Neutral** (playable in any deck). Each Tavern Dweller unlocks a **fixed pair** of factions, giving 10 possible pairs. 🟡 There may be several Tavern Dwellers per pair over time.

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

### 9.3 Tavern Dweller list
The 10 Tavern Dwellers (one per faction pair) are in [cards/tavern_dwellers.md](cards/tavern_dwellers.md). That list is the source of truth.

---

## 10. Card Types 🔒
| Type | When played | Goes to | Notes |
|---|---|---|---|
| **Creature** | Your main phase | Battlefield | Has Power / Health; damage is permanent |
| **Sorcery** | Your main phase, with an empty Chain | Graveyard | |
| **Instant** | Whenever you have priority (§8); can be paid with Gold | Graveyard | |
| **Equipment** | Your main phase (mana only) | Battlefield | **Equip X** (main phase, empty Chain; mana, then Gold): attach to target creature you control. It's an activated ability, so it uses the Chain (MTG 701.3). Equipping it again moves it; the creature it leaves loses the bonus (which can't kill, §7.3). When the creature leaves, the Equipment stays on the battlefield unattached. Glitterworld's core type |
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
| **Invest X: …** | Optional extra cost, paid only with Gold, for a bonus effect | 🔒 |
| **Arrival** | Triggers when this creature enters the battlefield | 🟡 |
| **Last Breath** | Triggers when this creature dies | 🟡 |
| **Reach** | Can block creatures with Flying | 🟡 |
| **Equip X** | (Glitterworld) Pay X: attach this Equipment to a creature you control. Main phase only | 🟡 |

---

### 11.1 Rules terms 🔒 (MTG, except Heal)
- **Fight**: two creatures each deal damage equal to their Power to the other, at the same time. This is not combat, so Trample doesn't apply. The damage is permanent, like all damage.
- **Heal X**: remove up to X damage from a creature. Its Health can't go above its maximum. "Heal fully" removes all of its damage. Healing the Tavern Dweller restores life, up to the starting life total.
- **Gain life** 🔒: works exactly like healing your Tavern Dweller, so life can't go above the starting life total (30 in Standard). This covers Lifelink, drains ("you gain 1 life") and any other life gain. Deviation from MTG, where life gain is uncapped (decided 2026-10-09).
- **Sacrifice**: put a permanent you control into its owner's graveyard. This can't be prevented.
- **Damaged** 🔒: a creature with damage on it (Health remaining below its max). This is plain card text, not a keyword: Bloodied, Wound and Pristine stay rejected as keywords (§11). "Health remaining" is max Health minus damage.
- **Can't be healed** 🔒: Heal effects remove no damage from it. It can still get bigger (+1/+1 counters, buffs).
- **Destroys a creature in combat** (Champion's Belt): it dealt combat damage to that creature, and the creature now has lethal damage. It triggers once for each creature destroyed, even if both creatures die.
- **"Can't be dealt more than N damage each turn"** (Hardlight Aegis): damage over the limit is prevented, so it isn't dealt (no Lifelink, no "is dealt damage" triggers). Damage the creature took earlier in the same turn counts, even if the effect started later (MTG). Trample still assigns lethal damage as if nothing were prevented (MTG 702.19c).
- **Counter** (MTG 701.5): a countered spell goes to its owner's graveyard without resolving; a countered ability does nothing. A countered "once each turn" ability or Tavern Dweller Power still counts as used. A tax ("unless its controller pays 3") is paid with mana first, then Gold (§5.2), and Gold paid this way is spent.
- **Gain control** (MTG): the permanent keeps its damage and counters. A creature can't attack or use Tap abilities until its new controller's next turn, unless it has Haste (MTG 302.6), and a creature that changes controller leaves combat. "Until end of turn" control ends in the cleanup step. When a player leaves the game, what they controlled but didn't own goes back to its owner.
- **Return to hand** (bounce): the card comes back as a new object, so its damage is gone. A token stops existing.
- **Token**: a creature created by an effect. It doesn't exist outside the battlefield: when a token leaves the battlefield, it disappears.
- **Activated abilities** 🔒 (MTG 602): "[Cost]: [Effect]." Activating one puts it on the Chain; it resolves even if its source has left (MTG 113.7a). A **generic cost** ("(2)", "Equip 2", "X") is paid with mana first, then Gold (§5.2). **"Pay N Gold"** is paid **only with Gold**, like Invest (decided 2026-10-09). "Activate only once each turn" means once in each turn, yours or not (MTG). "Only as a sorcery" means your main phase with an empty Chain.

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
| 2026-10-10 | **"Choose" without "target" is chosen on resolution** (MTG 608.2d). Snik pays X on activation and chooses up to X other Goobers when the ability resolves, one at a time, and may stop early; a Goober that left in response just can't be chosen. |
| 2026-10-10 | **New Powers for three Tavern Dwellers** that sims showed were barely used (cards/tavern_dwellers.md): **Mukk** (3) a creature you control with Trample fights a creature you don't control; **Sparkwrench** (2) attach up to one target Equipment you control to target creature you control, and if none became attached, it gets +1/+1 until end of turn; **Auditor Prime** (2) draw a card, activate only if you have 3 or more Gold (checked before paying, MTG "activate only if"). |
| 2026-10-10 | **Runeterra-style mana is the Standard rules** (§5–6): a round pool (every player gains +1 max mana and refills when a round starts, mana lasts the round), unspent mana becomes Gold at the end of the round, **Gold cap 3**, spells and abilities **pay Gold first** (permanents mana only), the round leader rotates (A B, B A ...) and only the round leader may attack (attack token), summoning sickness stays. Gold first also settles RULES_REVIEW #6 (the sequencing trap). The old rules stay in the engine as `FormatConfig.Classic()`. **Cards for the cap of 3**: Velvet Embezzler draws at **3 or more Gold**; Compound Interest reads "If **3 or more Gold was spent to cast it**, draw three instead" (with Gold first, "if you have 3 Gold" after paying could never happen). |
| 2026-10-09 | **Set v0.3 approved: mana scarcity** (36 cards: 6 per faction and 6 Neutral; 2 card draw, 2 mana sinks, 2 finishers each). Goal: players count their mana out most rounds. **X costs**: the printed cost plus X, chosen on casting (at least 1), paid like the card (spells and abilities with mana and Gold, permanents with mana only); "(X): ..." abilities work the same way. **"Divided as you choose" without "target"** (Arc Cascade) is divided one point at a time on resolution. Invest costs stay at 3 or less so they work with a Gold cap of 3. |
| 2026-10-09 | **Going first: keep the MTG default** (first player skips the turn-1 draw, nothing else) and let human playtests judge. Measured alternatives stay as experiment switches (playtest/RULES_REVIEW.md §1): a **round mana pool** (everyone refills when a round starts, unspent mana banked at the end of the round) doesn't help on its own, because the first player gets the free reaction window; a **rotating first player** (A B, B A, A B...) brings every mirror to 41-53% (3.3 points off 50% with the round pool and "Gold first on other players' turns"). If playtests confirm the edge, rotation is the candidate. |
| 2026-10-09 | **Player choices instead of automatic ones** (MTG defaults). **Legendary rule**: one per name *per controller*, and **you choose which to keep**; the others go to the graveyard (they die). **Combat damage** among several blockers: divided freely (§7.2.6, now 🔒), Trample needs lethal on every blocker first, and the game **only asks when the creature can't kill them all**. **Simultaneous triggers**: still APNAP between players, and **each player orders their own** (MTG 603.3b). Triggers of the same ability of the same card count as identical and aren't asked about (like MTG Arena). |
| 2026-10-09 | **Rest of set v0.1 into the engine.** Velvet Embezzler now reads "if you have **5 or more** Gold" (Offshore Account can raise the cap to 8). **The Dealer**: an opponent needs 2 Gold to give (MTG: you can't pay what you don't have); with less, the Dealer's controller draws. |
| 2026-10-09 | **v0.2 card rulings**: Gold-Tooth Bruiser / Pickpocket Boss: "that player loses 1 Gold and you gain 1 Gold" are separate, so **you gain 1 even if they had none** (MTG reading). **Dice Game**: players choose **in the open, in turn order** from the active player (MTG 101.4). **Retainer Mage**: Gold can help pay for it **whenever it's cast** (Flash, mana first). All 70 v0.2 cards now run in the engine (DEVELOPMENT §7). **Deck pass approved**: each of the six prototype decks swaps 4 cards for v0.2 cards (Sparkwrench Scrappers gets Equipment). |
| 2026-10-09 | **v0.2 Gold rules** (§5.2): "whenever you spend Gold" triggers **once per payment**; when a Gold cap goes down, the excess Gold is **lost at once**; "Gold equal to its cost" is the **printed cost** (MTG mana value). Bank triggers use the cleanup step's priority (MTG 514.3a). v0.2 cards go into the six prototype decks in one pass after all engine batches, and the user approves the lists. |
| 2026-10-09 | **Patrons are renamed Tavern Dwellers** (in rules text, docs and code: `TavernDweller`). **Tavern Dweller Powers: once each turn** (MTG default), so up to once on your turn and once on each opponent's turn. **"Pay N Gold" costs are Gold only**, like Invest; generic ability costs (Equip, X, Powers) stay mana first. **Archon Lumen** deals one separate 1-damage ping per Equipment, each with its own target. Deck rule: every card is from the Tavern Dweller's factions or Neutral. Activated abilities, Equip and all 10 Tavern Dwellers are implemented (DEVELOPMENT §7). |
| 2026-10-09 | **Rules review** (playtest/RULES_REVIEW.md). Going first: back to the **MTG default** (the first player skips their turn-1 draw, no other compensation); human playtests will judge it. Gold sinks: **Tavern Dweller Powers** are the fix, so implement them next and then re-measure the Gold cap. Game-length stalls: **no new rule**; add late-game sinks and finishers in cards first. **Life gain is capped at starting life** (§11.1). |
| 2026-10-09 | **Set v0.2 approved**: all 70 additions (10 per faction, 20 Neutral) confirmed. The rules terms they use are now locked: **bank**, per-player Gold cap, **damaged**, **can't be healed** (§5.2, §11.1). |
| 2026-10-09 | **No draw skip**: the first player now draws on turn 1. The second player keeps +1 mana on their first turn; the first player gets no bonus mana (§3). |
| 2026-10-09 | **Payment rules** (§5.2): casting any permanent (creature, Equipment, Relic, Curse) uses mana only. Instants, Sorceries, activated abilities, Equip and Tavern Dweller Powers can use Gold, **mana is always spent first automatically**. **Overcharge is renamed Invest** and is the only cost paid only with Gold. Set v0.2 card drafts added: 10 per faction plus 20 Neutral. |
| 2026-10-09 | **Going second**: the 1 starting Gold is replaced by **+1 mana on the second player's first turn** (§3). In bot mirrors the first-player win rate dropped from 73–67% to 66–60%. Next card work: more Glitterworld and Wild cards (pings, fights), so permanent damage shows up in tests. |
| 2026-10-09 | **Losing a buff can't kill** (§7.3): when a Health buff ends, damage is capped so the creature keeps 1 Health. This deviates from MTG. |
| 2026-10-09 | Tech: Unity 6000.6.4f1 + C#, PC (Windows) first, local first with online later. The rules engine is a Unity assembly with no engine references (DEVELOPMENT §0). |
| 2026-10-09 | **First set v0.1 complete**: 5 factions × 20 cards, 10 Neutral cards, 10 Tavern Dwellers (docs/cards/). |
| 2026-10-09 | **MTG rules are the default foundation**: everything outside mana and damage follows the MTG Comprehensive Rules (§1.1). |
| 2026-10-09 | Faction pie locked (CARD_DESIGN §4). Multiplayer: free-for-all attacks, 40 life, no turn-order compensation, teams (2v2) planned for later. |
| 2026-10-09 | Rarities: Common/Uncommon/Rare/Legendary (Legendary rule: only one with a given name on the battlefield). Vanilla stats = 2×cost+1. First set ~120 cards. See CARD_DESIGN.md. |
| 2026-10-09 | Two main phases. Chain uses full back-and-forth priority. Permanent types: Equipment, Relics, Curses (no Locations). London mulligan. |
| 2026-10-09 | Reserve renamed **Gold**. Shadow Money Wizards do money through *shady deals*. Tavern Dwellers don't fight. The Tavern Dweller drafts are a good direction. |
| 2026-10-09 | Five factions: Shadow Money Wizards, Goobers, Sensationalists, Evergrowing Wild, Glitterworld. The Tavern Dweller is the player's face. Trample is the only damage-related core keyword. The second player starts with 1 Gold. |
| 2026-10-09 | Healing only through cards (no built-in rule). Gold capped at 5. 5 factions, each Tavern Dweller unlocks a fixed pair, plus Neutral cards. Max hand size 7. Drawing from an empty deck makes you lose. |
| 2026-10-09 | Gold pays for Instants, activated abilities, Invest (Gold only) and Tavern Dweller powers. Combat uses MTG-style blocking. Timing uses a single response Chain. Deck identity comes from a Tavern Dweller card plus factions. |
| 2026-10-09 | Repo restarted from scratch. Locked: Standard = 60 cards, max 4 copies, 30 life, 7-card hand, +1 mana per turn up to 10, zones Deck/Hand/Battlefield/Graveyard/Exile, damage on creatures is permanent. Future goals: singleton/big-deck format, up to 4 players. |
