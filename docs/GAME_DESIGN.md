# Restarted Tavern — Game Design

The game's rules: what players see and do. This is a living document, built up through design sessions.

**Status tags**
- 🔒 **LOCKED**: decided. Change it only on purpose, and log the change in the Decision Log.
- 🟡 **PROPOSED**: the current best idea, not yet confirmed.
- ❓ **OPEN**: still needs a decision.

---

## 1. Vision

A trading-card-style dueling game with a structure close to MTG: zones, a battlefield, a graveyard and exile. It borrows Hearthstone's automatic mana growth, so there is no land screw. Two features set it apart:

1. **Permanent damage.** Creatures carry their wounds from round to round. Every fight matters, and healing is a real resource.
2. **Unused mana is not wasted.** Mana you don't spend turns into a second resource that can be saved up.

The game is designed for 1v1 first, with every rule written so that it also works for up to 4 players.

---

## 1.1 Rules Foundation: MTG by default 🔒
**Anything this document does not cover follows the Magic: The Gathering Comprehensive Rules.** That includes the stack and priority, state-based actions, layers for continuous effects, replacement effects, the combat step structure, attachments (Equipment and Auras/Curses), tokens, copies, control-changing effects, the Legendary rule and multiplayer elimination.

This game only **deviates** from MTG in these areas:

| Area | MTG | Restarted Tavern |
|---|---|---|
| **Turns** | One player's turn after another; summoning sickness | **Legends of Runeterra rounds**: a round is everyone's turn. Players alternate single actions, only the round leader may attack (attack token, passes every round), no summoning sickness (§6, §7.4). Wherever MTG says "turn", read "round" |
| **Mana** | Lands, colored mana, mana empties between steps | Colorless mana crystals: +1 max per round up to 10, refilled when a round starts and kept for the whole round (§5.1, §6). No lands |
| **Gold** | — | Unspent mana becomes Gold at the end of the round (cap 3). Gold pays for Instants, Sorceries and activated abilities (**Gold is used first**), and Invest is paid only with Gold. Permanents are mana only (§5.2) |
| **Damage** | Damage wears off in the cleanup step; toughness | **Health**. Damage is permanent until healed (§7.3), and **Heal** is a game action. A Health buff ending can't kill a creature (§7.3) |
| **Life gain** | Uncapped | Gaining life (Lifelink, drains) can't take you above your starting life (§11.1) |
| *Format numbers* | 20 life | 30 life (40 in multiplayer), 60 cards, 4 copies |
| *Deck identity* | Colors (Commander uses a command zone) | A **Tavern Dweller** in the Tavern Dweller zone sets the factions and acts as the player's face, with a Power paid like any activated ability (§9). It works like a commander that never enters the battlefield |
| *Going first* | The first player skips their draw | No compensation: the rounds share out going first (everyone draws in round 1, §3) |

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
| Maximum hand size | 7 (every player discards down to 7 at the end of the round) | 🔒 |
| Mulligan | **London mulligan**: shuffle and draw 7, then put 1 card on the bottom for each mulligan taken | 🔒 |
| Who goes first | Random | 🟡 |
| Going-first compensation | **None.** Everyone draws when a round starts, round 1 included. The round leader (who acts first and holds the attack token) changes every round (§6.1), which evens out going first: 45–54% first-player wins in all six bot mirrors | 🔒 |

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

Mana works like **Legends of Runeterra**: one shared round pool (§6), and unspent mana becomes spell mana (Gold).

### 5.1 Mana crystals 🔒
- A **round** is everyone's turn (§6). At the start of each round **every player's max mana goes up by 1** (cap **10**), and their mana refills to max.
- Mana lasts the **whole round**: spend it on your actions or on responses to other players' actions (§6).
- Mana has no color 🔒. Which cards a deck can use is decided by its Tavern Dweller's factions (§9).

### 5.2 Gold 🔒
Unused mana is not lost.

- At the **end of the round**, each point of every player's unspent mana becomes **1 Gold** (a separate counter): Runeterra's spell mana.
- Gold is **capped at 3** 🔒. Mana over the cap is lost. Invest costs stay at 3 or less.
- 🔒 **What Gold can pay for**:

  | Cost | Mana | Gold |
  |---|---|---|
  | Casting a **permanent** (creature, Equipment, Relic, Curse): anything that enters the battlefield | ✅ | ❌ |
  | **Instants** and **Sorceries** | ✅ | ✅ |
  | **Activated abilities**, including **Equip** and **Tavern Dweller Powers** | ✅ | ✅ |
  | **Invest X** (§11) | ❌ | ✅ **only Gold** |

- 🔒 **Gold is always used first** (decided 2026-10-10, like Runeterra's spell mana). When a cost can be paid with both, your Gold is spent first automatically, and mana pays only what Gold can't. There's no choosing the split. (Invest is separate: it is always paid with Gold.)
  - Gold can never pay for permanents, so spending it first is always right: casting a Sorcery before a creature can't use up the creature's mana.
  - Example: with 2 mana and 3 Gold, a 4-cost Sorcery uses the 3 Gold and then 1 mana.
- 🔒 Gold can't pay for permanents, so it can't be used to ramp out threats early. A few cards break this on purpose, and Gold is then spent first like for spells: **Retainer Mage** (Gold can pay for it whenever it's cast, decided 2026-10-09) and **Shady Moneylender** (Gold can pay for your creature spells). **Silent Partner** lets mana pay for Invest (the mana left after the cost, then Gold).
- 🔒 **Clarification**: the mana pool fills when a round starts and lasts until it ends. What's left at the end of the round is banked as Gold.
- 🔒 **Taxes** ("unless they pay N") can be paid with mana and Gold (Gold first).
- 🔒 **Gold-only parts are set aside first** (2026-10-10): when a spell also has Invest or "pay any amount of Gold (X)", the Gold for those is kept back before its cost takes Gold first, so the cost uses mana instead where it can.
- 🔒 Gold gained above the cap of 3 is lost.
- 🔒 **Bank** (rules term, introduced with set v0.2): when unspent mana becomes Gold at the end of the round, you **bank** the Gold you actually gain (mana lost to the cap isn't banked). Cards can say "Whenever you bank Gold" or "Whenever you bank 2 or more Gold". These trigger in the cleanup step. As in MTG 514.3a, players then get priority, and the cleanup step repeats afterwards.
- 🔒 Some cards change a player's Gold cap ("Your Gold cap is 8"). The cap is a per-player value that starts at the format's cap. If several effects set it, the newest one wins (MTG timestamp order).
- 🔒 **When a cap goes down** (the card that raised it leaves), Gold above the new cap is **lost at once**, checked like a state-based action (decided 2026-10-09). The cap always means the cap.
- 🔒 **Spend Gold** (rules term): a player spends Gold when they pay Gold for a cost: a spell, an activated ability, Invest, "pay any amount of Gold", or a tax. "Whenever you spend Gold" triggers **once per payment**, however much Gold it was; "3 or more Gold on a single spell or ability" looks at that one payment (decided 2026-10-09). Gold that is lost or taken (Tax Office, Debt Collector, Grand Heist) is not spent.
- 🔒 **"Gold equal to its cost"** (Counterfeit Coin, Golden Handshake, Hostile Takeover) means the **printed cost** (MTG mana value). Discounts and Invest don't change it (decided 2026-10-09).

**Why this works**
- It removes the bad feeling of "I held up mana for a trick and the opponent didn't attack". The mana is banked instead of wasted.
- It creates a real choice each round: develop the board now, or bank for answers later.
- It works naturally with alternating actions (§6): Gold is spell mana for answering the opponent's actions.

**Alternatives considered**
- **B. Gold can pay for anything, but converts at a 2:1 ratio.** Simpler, but it turns into generic ramp.
- **C. Gold can only be spent on "Invest" bonuses** (cards with an extra effect if you pay X Gold). Very clean design space, but it's narrow on its own.
- ✅ What we chose combines A and C: Gold pays for Instants, Sorceries and abilities (Gold first), and is the only way to pay for Invest.

🔒 Named **Gold**: unused mana is "banked" as money, which fits the tavern (paying your tab) and the Shadow Money Wizards.

---

## 6. Turn Structure 🔒 Legends of Runeterra rounds

A **round** is everyone's turn at once. Wherever the MTG rules or a card say "turn", read "round": "once each turn" is once each round, "until end of turn" lasts until the round ends, and every player's "your turn" is the round, so "at the start of your turn" triggers for every player.

1. **Round start**: every player gets +1 max mana (up to 10) and refills (§5.1). **Only the player who gets the attack token this round untaps their permanents** (MTG's untap step, on your attack rounds: in 1v1 every other round). So a creature that attacked stays tapped through the opponent's attack round and can't block, and a Tap ability keeps the creature tapped until your next attack round. "At the start of your turn" effects trigger for every player, the round leader's first.
2. **Draw**: every player draws 1 card, in round 1 too.
3. **Action phase**: players take **actions** one at a time, starting with the round leader, then in turn order.
   - An action is: play a card from your hand (any type: creatures and Sorceries too), activate an ability or your Tavern Dweller's Power, or attack (§6.1).
   - The Chain works as in §8: everyone can respond to an action, and responses don't use up an action. When the Chain is empty again, the next player has the action.
   - **Pass**: the next player has the action. When every player passes in a row with an empty Chain, the action phase ends.
4. **End of round**: "at end of turn" effects trigger for every player, every player discards down to 7 (from the leader, in turn order), unspent mana becomes Gold (§5.2), and "until end of turn" effects end. The next player in seat order becomes the round leader.

There is **no automatic healing** at the end of a round (see §7.3).

### 6.1 The attack token 🔒
- The **round leader** holds the attack token. Once in the round they may use an action to **attack**: combat follows §7.2 (declare attackers, then blockers, then damage, with the response windows of §8). After combat, the next player has the action.
- The token passes every round, so in 1v1 the players take turns attacking: A leads and attacks in round 1, B in round 2, and so on.
- **Why**: every play gets an answer before the next one, so nobody builds and attacks before the opponent can respond, and going first is shared out round by round. Bot mirrors: the first player wins 45–54% in all six decks.

---

## 7. Creatures & Combat

### 7.1 Stats
Creatures have **Power / Health**. Damage stays on the creature (it isn't removed at the end of the round), and the card shows **Health remaining** = max Health − damage.

### 7.2 Combat model 🔒 MTG-style blocking
1. **Declare attackers**: the attack token holder (§6.1) taps untapped creatures to attack (not with Vigilance). They stay tapped until their controller's next attack round (§6 step 1), so attacking costs you those blockers. Each attacker attacks a **player** (in multiplayer, the attacker picks which opponent for each creature).
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

### 7.4 No summoning sickness, no Haste 🔒
Creatures can attack, and use Tap abilities, the round they enter the battlefield. With alternating actions (§6) the opponent always gets at least one action between a creature arriving and attacking. **Haste** doesn't exist.

---

## 8. Timing & Responses 🔒 The Chain (full back-and-forth)
- When a player plays a spell, activates an ability or uses their Tavern Dweller Power, it goes on top of the **Chain**.
- **Priority** then passes around the table in turn order. Whoever has priority may add **one** Instant or ability to the Chain, or pass.
- After anything is added, priority goes around the table again, so players can respond to responses as long as they want.
- When **all players pass in a row**, the **top** item of the Chain resolves (last in, first out). Then the active player gets priority again, and the loop continues until the Chain is empty.
- If **all** of a spell's targets are no longer valid when it resolves, it **fizzles** (it goes to the graveyard and does nothing). If only some are, it resolves and skips the illegal ones (MTG 608.2b).
- Triggered abilities (Arrival, Last Breath…) also go on the Chain, so they can be responded to.
- **Fixed windows** where players get priority even when the Chain is empty: 🟡 the action phase (§6: whoever has the action), the start of combat, after attackers are declared, after blockers are declared, and the end of the round.
- 🟡 UX note: the client should auto-pass for players who have no legal response (or who choose "auto-pass this round"), so the back-and-forth stays fast, especially with 4 players.

### 8.1 Replacement effects 🔒 (MTG 614–616)
"If [something] would happen, [something else] happens instead." They don't use the Chain; they change the event as it happens.
- Events that can be replaced: a creature **dying**, **damage** being dealt (prevention, "double", "that much plus 1"), a permanent **entering** (with counters, tapped), **drawing** a card, **gaining life**, **gaining Gold** (banking included).
- They come from permanents and Tavern Dwellers (static abilities), or from spells and abilities for a while ("until end of turn", "the next time ...").
- Each replacement changes an event **at most once** (MTG 614.5). After one applies, the others are checked again: once a creature is exiled instead of dying, "if it would die" effects no longer apply.
- **Order** when several apply: self-replacement effects first (MTG 614.15), then the others **oldest first**. ❓ MTG lets the affected player choose this order (616.1); the engine uses the fixed order until game actions can pause for a choice. Decide this when the first card that needs it is designed.
- Keeper Z-00's "Your creatures with 5 or more Health enter with a +1/+1 counter" is a replacement effect.

---

## 9. Tavern Dwellers & Factions

### 9.1 The Tavern Dweller is your face 🔒
Every deck is led by a **Tavern Dweller**, a tavern regular you play *as*. The Tavern Dweller is not part of the 60 cards.

- The Tavern Dweller **is the player**: your 30 life is the Tavern Dweller's life, and "attack a player" means attacking their Tavern Dweller.
- The Tavern Dweller sits in the **Tavern Dweller zone** (public). In v0.1 it can't be removed from the game.
- **Tavern Dweller Power**: each Tavern Dweller has a unique activated power, paid with mana and/or Gold like any activated ability (Gold first, §5.2) 🔒. 🔒 It can be used **once each round** (§6), at instant speed, through the Chain, so opponents can respond to it.
- **Passive**: 🟡 each Tavern Dweller has one always-on ability: a triggered ability, a static ability, or a cost change. It works from the Tavern Dweller zone.
- 🔒 **Deck rule**: every deck has exactly one Tavern Dweller, and every card in it is from one of the Tavern Dweller's two factions or Neutral.
- 🔒 The Tavern Dweller **never attacks or blocks**, and Equipment only goes on creatures. Combat is entirely about creatures.
- Future singleton format: the Tavern Dweller becomes the commander-style deck leader.

### 9.2 Factions 🔒
There are **5 factions**. Every card belongs to one faction or is **Neutral** (playable in any deck). Each Tavern Dweller unlocks a **fixed pair** of factions, giving 10 possible pairs. 🟡 There may be several Tavern Dwellers per pair over time.

| Faction | Inspired by | Identity | Plays like |
|---|---|---|---|
| **Shadow Money Wizards** | — | Shady wizards who deal in money and magic | Big, impressive spells; wizard creatures that bring **value** (card draw, Gold, effects) but are rarely game-enders themselves. **Shady deals** 🔒: trading life or cards for Gold, or handing opponents Gold in exchange for a big effect |
| **Goobers** | MTG red + LoR Noxus | Goblins | Aggressive, wide boards, burn, raw strength, chaos |
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
| **Creature** | Your action (§6) | Battlefield | Has Power / Health; damage is permanent |
| **Sorcery** | Your action, with an empty Chain | Graveyard | |
| **Instant** | Whenever you have priority (§8); can be paid with Gold | Graveyard | |
| **Equipment** | Your action (mana only) | Battlefield | **Equip X** (your action, empty Chain; Gold first, then mana): attach to target creature you control. It's an activated ability, so it uses the Chain (MTG 701.3). Equipping it again moves it; the creature it leaves loses the bonus (which can't kill, §7.3). When the creature leaves, the Equipment stays on the battlefield unattached. Glitterworld's core type |
| **Relic** | Your action | Battlefield | A non-creature permanent with ongoing effects and/or activated abilities |
| **Curse** | Your action | Battlefield, attached to an **enemy creature or opponent** | A negative ongoing effect. Goes to the graveyard if what it's attached to leaves. Sensationalists' core type |

Locations were considered and rejected for now (they may return later).

---

## 11. Keywords (starting set) 🟡
Kept deliberately **small**. 🔒 Trample is the only damage-related core keyword; mechanics like Bloodied, Wound and Pristine were considered and rejected for now.

| Keyword | Meaning | Status |
|---|---|---|
| **Trample** | Combat damage beyond what's needed to kill the blockers goes to the attacked player | 🔒 |
| **Flying** | Can only be blocked by Flying / Reach | 🟡 |
| **Lifelink** | Damage dealt also heals its controller | 🟡 |
| **Invest X: …** | Optional extra cost, paid only with Gold, for a bonus effect | 🔒 |
| **Arrival** | Triggers when this creature enters the battlefield | 🟡 |
| **Last Breath** | Triggers when this creature dies | 🟡 |
| **Reach** | Can block creatures with Flying | 🟡 |
| **Vigilance** | Attacking doesn't tap it, so it can still block in the opponent's attack round (MTG). On defensive creatures (Decision Log 2026-10-10) | 🟡 |
| **Equip X** | (Glitterworld) Pay X: attach this Equipment to a creature you control. Only as one of your actions | 🟡 |

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
- **Counter** (MTG 701.5): a countered spell goes to its owner's graveyard without resolving; a countered ability does nothing. A countered "once each turn" ability or Tavern Dweller Power still counts as used. A tax ("unless its controller pays 3") is paid with Gold first, then mana (§5.2), and Gold paid this way is spent.
- **Gain control** (MTG): the permanent keeps its damage and counters. It can attack and use Tap abilities for its new controller right away (no summoning sickness, §7.4). A creature that changes controller leaves combat. "Until end of turn" control ends in the cleanup step. When a player leaves the game, what they controlled but didn't own goes back to its owner.
- **Return to hand** (bounce): the card comes back as a new object, so its damage is gone. A token stops existing.
- **Token**: a creature created by an effect. It doesn't exist outside the battlefield: when a token leaves the battlefield, it disappears.
- **Activated abilities** 🔒 (MTG 602): "[Cost]: [Effect]." Activating one puts it on the Chain; it resolves even if its source has left (MTG 113.7a). A **generic cost** ("(2)", "Equip 2", "X") is paid with Gold first, then mana (§5.2). **"Pay N Gold"** is paid **only with Gold**, like Invest (decided 2026-10-09). "Activate only once each turn" means once each round. "Only as a sorcery" means as one of your actions, with an empty Chain (§6).

---

## 12. Win / Loss
- A player at 0 or less life loses.
- A player who has to draw from an empty deck loses. 🔒
- Multiplayer: last player standing (see §13).

---

## 13. Multiplayer (3–4 players) 🔒 (future format)
- ❓ **Rounds in multiplayer**: how the action rounds and the attack token work with 3–4 players is open (with a rotating token, each player attacks once every 3–4 rounds). Until it's decided, the engine's multiplayer format uses the 1v1 rounds as they are: the token moves one seat each round.
- **Seating**: actions and the round leader go clockwise. Priority on the Chain also goes clockwise, starting from the player who has the action.
- **Starting life: 40.**
- **Going-first compensation: none**, as in 1v1: everyone draws in round 1.
- **Attacking**: free-for-all. **Each attacking creature chooses any opponent** to attack. Each defending player only declares blockers against attackers that are attacking *them*.
- **Elimination** 🟡: when a player loses, they leave the game. All cards they **own** leave with them, any of their spells or abilities on the Chain are removed, and control of anything of theirs that someone else controls ends. Effects that player controlled stop ("until end of turn" effects end immediately).
- 🟡 Card wording for multiplayer: "each opponent", "target opponent", "the player to your left/right". Effects like "each player" include you.
- 🟡 Politics are allowed (deals, threats), but deals are not binding in the rules.

### 13.1 Teams (2v2) ❓ future
- The engine gives every player a `teamId` from the start.
- 🟡 Draft idea: teammates sit across from each other, so actions alternate between teams. Life, Gold and hands are separate. You can't attack or target your teammate with "opponent" effects.

---

## Decision Log
| Date | Decision |
|---|---|
| 2026-10-10 | **Deck editor; gear tuck, crowded lane, token domes** (bug reports 2026-10-10_143751, _144700). The user wanted to try cards and combos: a deck editor in the main menu (player decks in persistentDataPath/custom_decks.json, listed after the prototype decks, which keep their indexes; sims never load them). Gear: tucked up and to the right behind its creature, and closing the fan puts it back under the host's current index (a stale index could leave it on top). Lane: 19 sideways attackers overlapped into a mess; declared attackers now tilt 12 degrees instead of lying sideways and the lane scales cards down to fit (min 0.5). Tokens are domes, as asked. |
| 2026-10-10 | **Playtest fixes and the bot under the untap rule** (bug reports 2026-10-10_140818, _141453, _141545; the user won as Auditor's Arsenal against the Goober Mob bot and asked whether the bots play the new tap rule badly). Client: hand cards shrink their art when the text needs room (Archon Lumen's text was cut off); hovering a creature with gear puts its zoom on the side away from the fanned-out Equipment, and the fan stays open while the mouse crosses to it; your triggering card glows green and an Arena-style curved targeting arrow runs from it (and from spells and abilities you cast) to the pointer, snapping gold onto a legal target. The "Pass turn" button keeps its label when the opponent can only pass (the engine passes for them and the round ends): saying "End round" would reveal that they have no plays. Bot: `BotStyle.TapRuleAware` (Tap abilities also cost the blocks until our next attack round; Vigilance attackers count as blockers for the crack-back) and `CrackBackWeight` (0.5, as before). Head-to-head (1000 games per mirror): TapRuleAware 50.2% (no measurable change); crack-back weight 1.0: 48.1%, 1.5: 46.3%, 0.25: 49.7%, so the current, aggressive weighting is the strongest the bot has. Balance with the new bot: spread 4.4 (was 4.3), Goober 61.5, Jungle 50.8, Zoo 46.2, Vesper 50.9, Sparkwrench 46.0, Auditor 44.7; Goober vs Auditor 73.2%. Conclusion: the bot isn't misplaying the untap rule in a way the sims can see; one human win at 27% is within chance, and a human can still exploit general bot weaknesses (blocks, Equipment timing). No card changes. |
| 2026-10-10 | **Untap only on your attack rounds** (user, bug report 2026-10-10_134627: "creatures untap when the opponent gets the attack token"). At the round start only the attack token holder untaps (MTG untap step). "Turn" still means round for everything else (start/end of turn triggers, once-each-turn, refill, draws). **Vigilance** added (attacks without tapping). Sims (500 games per pairing): the rule alone pushed the deck spread from 3.1 to 11.6 points (Goober Mob 56→80%, Auditor's Arsenal 52→29%; cheap wide aggro wins the race when attackers can't block). **Rebalance** (user-approved): Vigilance on Mossback Tortoise, Neon Executioner, Patrol Captain, Titan-Frame Guardian, Retired Champion, Vine Spider, Ironbark Grizzly and Snik; **Goober Warchief** cost 3→4, **Goober Rascal** 2/1→1/1, **Mob Rush** cost 4→5, **Pit Champion** cost 6→7. Result: spread 4.3, worst matchup 74.2% (was 71.4%), Goober 62.2%, Jungle 50.1, Zoo 46.5, Vesper 50.8, Sparkwrench 47.0, Auditor 43.5; games 10.3 rounds (was 10.5). Goober is still the top deck: watch it in playtests. |
| 2026-10-10 | **Arena board presentation** (user; NEXT_ARENA.md Phases 2-4). Tapped units lie **sideways**, and **attacking taps** them on the table as in the rules: declared attackers lie sideways in the combat lane (staged ones stay upright until confirmed). Untapping plays at the round start, after the gems refill. **Equipment and Curses on a creature are tucked behind it** (title strips peek out above, a gold "E" pip on the host); hovering the host fans them out, clicking one uses it. **Each side scales down as it fills** (one line down to half size, then two lines, then overlap). Player Curses stay in the row with an "on Player" tag for now. |
| 2026-10-10 | **Playtest fixes and the "may" rule** (user). **Archon Lumen**: one trigger, "At the end of your turn, if you control any Equipment, deal X damage to any target, where X is the number of Equipment you control." (`repeatCount` removed from the engine). **Ambush Predator**: "Arrival: This may fight up to one target creature you don't control." (no longer "damaged"; the card scan still has it below filler, -5.5, so no nerf). **Sabretooth Prowler**: "may fight" too (its text said "up to one", but the engine forced the fight). **Rule:** a card lets you choose only if its text says "may" or "up to" (or offers "one, two or three targets" / "… or lose N life"); everything else is forced. "Loses up to 2 Gold" is a cap, not a choice. A test keeps text and data in line. A declined or targetless trigger now says so on the table ("Ambush Predator: no target"), and a "may" trigger asks "Use <card>? Pick a target" / "Don't use <card>". |
| 2026-10-10 | **Trigger batching built** (user chose "one trigger with a count"). The same ability of the same source, triggered by several events before anyone gets priority (a sweeper kills 5 creatures and Scrap Collector watches), goes on the Chain as **one item that does it once per event**: totals stay exact, and per-creature effects still hit each creature. The table shows "×N". Targeted triggers stay one per event (each needs its target), and `"separate": true` on a trigger keeps them apart for future ping / storm cards (none yet; CARD_DESIGN §2.3). The engine counts triggers resolved per player each round (`UsesThisTurn["triggers:N"]`), next to spells cast. No card text changed. |
| 2026-10-10 | **Trigger count is a design resource.** One source should not flood the Chain with identical triggers for the same thing: by default they become **one trigger** that does it X times or with X as the amount (Archon Lumen next). This is a default, not a ban. Regions built around pinging, spell chains or a future **storm** playstyle get separate triggers on purpose, through an explicit per-trigger switch and text that says "each". The engine keeps exact counts (spells cast, triggers resolved, pings) so those cards can count them. Plan: docs/handoff/NEXT_ARENA.md Phase 1. |
| 2026-10-10 | **Bug hunt round 1** (Tools/BugHunt, 15,000+ games). **Scrap Collector** now reads "Whenever an equipped creature you control dies, gain 1 Gold." Before, moving an Equipment counted as unattaching it, so each Equip 1 refunded itself: infinite Equips, and infinite damage with Arc Welder. **Combat damage split** (§7.2.6) is now **asked one recipient at a time** ("how much to this blocker?"), and a recipient never gets more than lethal. Before, every whole split was listed at once: 10 damage into 10 blockers was ~40,000 options, which froze the game. The rules are unchanged; a whole split given in one action is still accepted. |
| 2026-10-10 | **Targets are always spelled out.** Every card that targets says so in its text: "target creature", "target enemy creature", "target opponent", "target spell", "target creature card", "any target"... (27 cards reworded, text only; Equip keeps the keyword shorthand). **The Chain is shown LoR-style** in the middle of the table as card bubbles, each one targetable (counters) and with lines to what it targets. |
| 2026-10-10 | **Playtest bug reports, round 1.** Activated abilities stay **instant speed by default**; the engine already allowed them after blocks (the Titan report was a clarity problem, now fixed in the client). **Equip and "only as a sorcery" stay sorcery speed** (MTG backbone; Equip mid-combat would be a big swing for 1 mana). The free Gilded Knuckles was Sparkwrench's "Equipment spells cost 1 less", not a bug: hand cards now show their current cost (green = cheaper). **LoR presentation** ([LOR_PRESENTATION.md](LOR_PRESENTATION.md)): steps 1+2 built together, tweens with **PrimeTween**. |
| 2026-10-10 | **One rule set.** The Standard rules (§5–6) are the only rules: the comparison formats (MTG turns, the rotating leader, the old Classic mana) and the experiment switches (going-second compensations, damage that wears off, games without Tavern Dwellers) are removed from the engine, the tools and the docs. Balance is fixed with cards from now on. |
| 2026-10-10 | **Visual client direction** ([CLIENT_DESIGN.md](CLIENT_DESIGN.md)): the screen works like Legends of Runeterra, the zones and the hand look like MTG Arena. LoR-style passing (one context button), mana gems with **Gold shown as LoR spell mana** (3 slots), and LoR combat (stage attackers in a lane, blockers in front of them, confirm once). Stylized painterly art, 2D board with a slight tilt, set on a tavern table, placeholder card frames first, LoR unit cards on the battlefield, the Tavern Dweller in LoR's Nexus spot (left edge), snappy animations with key moments. |
| 2026-10-10 | **Balance pass 1** (playtest/RULES_REVIEW.md, "Balance pass"; numbers from the card power scan, playtest/CARD_POWER.md). **Haste and summoning sickness are removed** from the game (§7.4): the 12 Haste cards lose the keyword, no compensation. **Nerfs**: Madame Morbida (no Lifelink, returns cost 2 or less), The Final Act (just "Destroy all creatures"), Exhumation Broadcast (cost 8), Grid Overload (twice), The Dealer (7 mana 3/5), Archon Lumen (no free Equip), Neon Executioner (7 mana, destroys at 1 Health or less). Final Broadcast, Hush Money and Mob Rush stay as printed. **Deck swaps**: Auditor's Arsenal (Rail Cannon → Hush Money, Overclock Rig → Archon Lumen), Sparkwrench Scrappers (Fuse Goober → Snik, Marksman Scope → Retired Champion), Vesper's Ledger (Fatal Rumor → Retired Champion). |
| 2026-10-10 | **Legends of Runeterra rounds** replace the turn structure (§6, §6.1, §7.4), after the design review (playtest/RULES_REVIEW.md, "Design review 2026-10-10"). A round is everyone's turn: everyone refills, untaps and draws, then players **alternate single actions** from the round leader; the leader holds the **attack token** and may use one action to attack; the token passes every round; **no summoning sickness**; everyone discards to 7 at the end of the round; **no going-first compensation** (everyone draws in round 1). "Turn" in the MTG rules and on cards means "round" (Powers: once each round). Bot mirrors: the first player wins 45–54% in every deck. Open: multiplayer rounds (§13). |
| 2026-10-10 | **Gold-only parts are set aside first** (§5.2): Invest and "pay any amount of Gold (X)" keep their Gold before the spell's cost takes Gold first (fixes a bug found in the design review: Gold first could eat the Invest Gold). |
| 2026-10-10 | **Cards are data**: every card and the prototype decks live in JSON files (`Assets/StreamingAssets/Cards`, `Decks`), built from the engine's building blocks (DEVELOPMENT §3). Changing a card no longer needs code. |
| 2026-10-10 | **Replacement effects are in the engine** (§8.1, MTG 614–616): dying, damage, entering, drawing, gaining life and gaining Gold can be replaced. Self-replacement effects first, then oldest first; each applies once per event. MTG's "the affected player chooses the order" is not asked yet (noted as open). No card uses them yet except Keeper Z-00, whose counter now goes through them. |
| 2026-10-10 | **"Choose" without "target" is chosen on resolution** (MTG 608.2d). Snik pays X on activation and chooses up to X other Goobers when the ability resolves, one at a time, and may stop early; a Goober that left in response just can't be chosen. |
| 2026-10-10 | **New Powers for three Tavern Dwellers** that sims showed were barely used (cards/tavern_dwellers.md): **Mukk** (3) a creature you control with Trample fights a creature you don't control; **Sparkwrench** (2) attach up to one target Equipment you control to target creature you control, and if none became attached, it gets +1/+1 until end of turn; **Auditor Prime** (2) draw a card, activate only if you have 3 or more Gold (checked before paying, MTG "activate only if"). |
| 2026-10-10 | **Runeterra-style mana is the Standard rules** (§5–6): a round pool (every player gains +1 max mana and refills when a round starts, mana lasts the round), unspent mana becomes Gold at the end of the round, **Gold cap 3**, spells and abilities **pay Gold first** (permanents mana only). Gold first also settles RULES_REVIEW #6 (the sequencing trap). **Cards for the cap of 3**: Velvet Embezzler draws at **3 or more Gold**; Compound Interest reads "If **3 or more Gold was spent to cast it**, draw three instead" (with Gold first, "if you have 3 Gold" after paying could never happen). |
| 2026-10-09 | **Set v0.3 approved: mana scarcity** (36 cards: 6 per faction and 6 Neutral; 2 card draw, 2 mana sinks, 2 finishers each). Goal: players count their mana out most rounds. **X costs**: the printed cost plus X, chosen on casting (at least 1), paid like the card (spells and abilities with mana and Gold, permanents with mana only); "(X): ..." abilities work the same way. **"Divided as you choose" without "target"** (Arc Cascade) is divided one point at a time on resolution. Invest costs stay at 3 or less so they work with a Gold cap of 3. |
| 2026-10-09 | **Player choices instead of automatic ones** (MTG defaults). **Legendary rule**: one per name *per controller*, and **you choose which to keep**; the others go to the graveyard (they die). **Combat damage** among several blockers: divided freely (§7.2.6, now 🔒), Trample needs lethal on every blocker first, and the game **only asks when the creature can't kill them all**. **Simultaneous triggers**: still APNAP between players, and **each player orders their own** (MTG 603.3b). Triggers of the same ability of the same card count as identical and aren't asked about (like MTG Arena). |
| 2026-10-09 | **Rest of set v0.1 into the engine.** **The Dealer**: an opponent needs 2 Gold to give (MTG: you can't pay what you don't have); with less, the Dealer's controller draws. |
| 2026-10-09 | **v0.2 card rulings**: Gold-Tooth Bruiser / Pickpocket Boss: "that player loses 1 Gold and you gain 1 Gold" are separate, so **you gain 1 even if they had none** (MTG reading). **Dice Game**: players choose **in the open, in turn order** from the active player (MTG 101.4). **Retainer Mage**: Gold can help pay for it **whenever it's cast** (Flash). All 70 v0.2 cards now run in the engine (DEVELOPMENT §7). **Deck pass approved**: each of the six prototype decks swaps 4 cards for v0.2 cards (Sparkwrench Scrappers gets Equipment). |
| 2026-10-09 | **v0.2 Gold rules** (§5.2): "whenever you spend Gold" triggers **once per payment**; when a Gold cap goes down, the excess Gold is **lost at once**; "Gold equal to its cost" is the **printed cost** (MTG mana value). Bank triggers use the cleanup step's priority (MTG 514.3a). v0.2 cards go into the six prototype decks in one pass after all engine batches, and the user approves the lists. |
| 2026-10-09 | **Patrons are renamed Tavern Dwellers** (in rules text, docs and code: `TavernDweller`). **"Pay N Gold" costs are Gold only**, like Invest. **Archon Lumen** deals one separate 1-damage ping per Equipment, each with its own target. Deck rule: every card is from the Tavern Dweller's factions or Neutral. Activated abilities, Equip and all 10 Tavern Dwellers are implemented (DEVELOPMENT §7). |
| 2026-10-09 | **Rules review** (playtest/RULES_REVIEW.md). Gold sinks: **Tavern Dweller Powers** are the fix, so implement them next and then re-measure the Gold cap. Game-length stalls: **no new rule**; add late-game sinks and finishers in cards first. **Life gain is capped at starting life** (§11.1). |
| 2026-10-09 | **Set v0.2 approved**: all 70 additions (10 per faction, 20 Neutral) confirmed. The rules terms they use are now locked: **bank**, per-player Gold cap, **damaged**, **can't be healed** (§5.2, §11.1). |
| 2026-10-09 | **Payment rules** (§5.2): casting any permanent (creature, Equipment, Relic, Curse) uses mana only. Instants, Sorceries, activated abilities, Equip and Tavern Dweller Powers can use Gold. **Overcharge is renamed Invest** and is the only cost paid only with Gold. Set v0.2 card drafts added: 10 per faction plus 20 Neutral. |
| 2026-10-09 | **Losing a buff can't kill** (§7.3): when a Health buff ends, damage is capped so the creature keeps 1 Health. This deviates from MTG. |
| 2026-10-09 | Tech: Unity 6000.6.4f1 + C#, PC (Windows) first, local first with online later. The rules engine is a Unity assembly with no engine references (DEVELOPMENT §0). |
| 2026-10-09 | **First set v0.1 complete**: 5 factions × 20 cards, 10 Neutral cards, 10 Tavern Dwellers (docs/cards/). |
| 2026-10-09 | **MTG rules are the default foundation**: everything outside mana and damage follows the MTG Comprehensive Rules (§1.1). |
| 2026-10-09 | Faction pie locked (CARD_DESIGN §4). Multiplayer: free-for-all attacks, 40 life, no turn-order compensation, teams (2v2) planned for later. |
| 2026-10-09 | Rarities: Common/Uncommon/Rare/Legendary (Legendary rule: only one with a given name on the battlefield). Vanilla stats = 2×cost+1. First set ~120 cards. See CARD_DESIGN.md. |
| 2026-10-09 | Chain uses full back-and-forth priority. Permanent types: Equipment, Relics, Curses (no Locations). London mulligan. |
| 2026-10-09 | Reserve renamed **Gold**. Shadow Money Wizards do money through *shady deals*. Tavern Dwellers don't fight. The Tavern Dweller drafts are a good direction. |
| 2026-10-09 | Five factions: Shadow Money Wizards, Goobers, Sensationalists, Evergrowing Wild, Glitterworld. The Tavern Dweller is the player's face. Trample is the only damage-related core keyword. |
| 2026-10-09 | Healing only through cards (no built-in rule). 5 factions, each Tavern Dweller unlocks a fixed pair, plus Neutral cards. Max hand size 7. Drawing from an empty deck makes you lose. |
| 2026-10-09 | Gold pays for Instants, activated abilities, Invest (Gold only) and Tavern Dweller powers. Combat uses MTG-style blocking. Timing uses a single response Chain. Deck identity comes from a Tavern Dweller card plus factions. |
| 2026-10-09 | Repo restarted from scratch. Locked: Standard = 60 cards, max 4 copies, 30 life, 7-card hand, +1 max mana each round up to 10, zones Deck/Hand/Battlefield/Graveyard/Exile, damage on creatures is permanent. Future goals: singleton/big-deck format, up to 4 players. |
