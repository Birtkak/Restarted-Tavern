# Rules Review

A check of the Standard rules (GAME_DESIGN.md: Legends of Runeterra rounds, Gold first, Gold cap 3) for **fundamental** problems, based on bot games ([SIMULATION_REPORT.md](SIMULATION_REPORT.md), [CARD_POWER.md](CARD_POWER.md)) and a read-through of the rules.

Bots are careful beginners. They race more than people do, so tempo effects are probably exaggerated. Treat the numbers as a direction, not exact values.

Severity: 🔴 fundamental (affects every game), 🟠 significant (affects a core promise of the game), 🟡 watch (an interaction to keep an eye on).

## Settled

| Issue | Answer |
|---|---|
| Going first | **The rounds** (GAME_DESIGN §6): every play gets an answer before the next, the attack token passes every round, everyone draws in round 1, no compensation. The first player wins 43–54% in every bot mirror. |
| Gold sits unused | **Tavern Dweller Powers** are the universal Gold sink. Always-useful Powers (Keeper Z-00, Skabba) are used several times a game; Mukk, Sparkwrench and Auditor Prime got new Powers because theirs were barely used. |
| Paying in the wrong order | **Gold is always spent first** (§5.2). Gold can't pay for permanents, so casting a Sorcery before a creature can't strand the creature. Gold-only parts (Invest, "pay X Gold") are set aside before the cost takes Gold. |
| Gain life | **Capped at starting life** (§11.1). |
| Slow games stall | No rule needed: under the rounds the average game is 9–13 rounds in the Greedy round robin, and almost no game passes 25 rounds (Control mirrors: Jungle 7.6%, the rest under 1%). |

## Open

### 🔴 R2. The attack token shifts card values
Each player attacks every other round, and the cards weren't repriced for that.
- "Whenever you attack" cards (Grakka, Pulse Blade, Sparkwrench's attack payoffs) trigger only in the rounds you hold the attack token.
- Encore From Beyond and Silver-Tongued Deal are much weaker in rounds without the token.
- "At the start of your turn" triggers for every player every round (Closing Bell, the Glitterworld pings, Wild heal-per-round creatures), so they are worth more per attack.
- To fix with cards (repricing, or new cards), not with rules.

### 🔴 R4. Multiplayer rounds
With 4 players and a rotating token, each player attacks once every 4 rounds, and one round's mana covers responses to 3 opponents. GAME_DESIGN §13 leaves this open; the engine's multiplayer format uses the 1v1 rounds as they are.

### 🟠 R5. "Unused mana is not wasted" isn't true in long games
In the slower decks a large share of leftover mana is lost to the Gold cap of 3: 20–33% in the Jungle and Zoo mirrors, up to 48% in Control mirrors (Wasted column in the report). Options: more Gold sinks in cards, raise the cap, or turn overflow mana into something else.

### 🟠 R6. Chip damage matters most in ping decks
"Chip→death" is the share of damage carried into a later round that was still on a creature when it died. It's highest in the creature-heavy mirrors (Jungle 91%, Goober 76%) and lowest in Auditor's games (37–60%), where healing and avoiding combat undo it. Card-pool rule: **every faction needs some chip damage and some payoff for it** (the "damaged" payoffs: Kick 'Em While They're Down, Blood Price, Ambush Predator, Finisher Protocol, Smart Rounds).

### 🟠 R8. Gold first works against "hold Gold" payoffs
Loan Shark, Velvet Embezzler, The Dealer and Invest reward holding Gold, but Gold is always spent first and the player can't choose.

### 🟠 R9. Gold cap 3 removes most of the shady-deal downside
Golden Handshake, Everything Has a Price ("That player gains 5 Gold") and Hostile Takeover give the opponent Gold, but an opponent at the cap gets at most 3. Everything Has a Price can't do what its text says.

### 🟡 R10. Temporary Health buffs are damage shields
Because "losing a buff can't kill" (§7.3), a "+0/+X until end of turn" effect absorbs up to X damage and the creature still survives at 1 Health. Example: Bark Skin makes a 2/2 into a 3/6 with a counter; it can take 5 damage and ends the round as a 3/3 at 1 Health. Repeatable buffs (Old Mossbank's "(2) +2/+2", once each round, at instant speed) make it a shield in every combat. Legal and intended, but price Health buffs like healing plus protection.

---

# Balance pass 1 (2026-10-10)

Before it, cross-faction balance was poor: Goober 71%, Zoo 69%, Jungle 66%, Vesper 42%, Sparkwrench 28%, Auditor 23% against the other five decks (spread 18.9 points, worst matchup 94%). Target agreed with the user: **each deck 45–55% against the field, no matchup past 65%**.

**Method** (SimRunner, Greedy bots):
- `-scan`: every card in the pool (4 copies) in each prototype deck's weakest slot, against the field; power = win% change against a plain 2/3 filler. Table: [CARD_POWER.md](CARD_POWER.md).
- `-optimize`: for a deck, find its weakest card, screen every legal replacement, confirm the best on fresh seeds.
- `-balance`: the win-rate matrix; card tweaks tested on a copy of the card files (`-data`) before anything changed.

**Findings**
- The pool had bombs the prototype decks didn't play: one swap moved a deck by 15–31 points (Madame Morbida, The Final Act, Grid Overload, Archon Lumen, Snik, Hush Money, Exhumation Broadcast). Deck-only tuning just became an arms race (Vesper reached 94%).
- Many non-creature cards are worse than a vanilla 2/3 for 2 (Relics, Equipment, Gold cards: Insider Trading −17, Golden Parachute −13, Mercenary Contract −13). Part of that may be the bot.

**Decisions** (user, Decision Log 2026-10-10)

| Change | Measured |
|---|---|
| Madame Morbida: no Lifelink, returns cost 2 or less | power +17.6 → +9.8 |
| The Final Act: "Destroy all creatures." (no drain) | +16.3 → +7.2 |
| Exhumation Broadcast: cost 5 → 8 | +13.6 → +7.9 |
| Grid Overload: twice instead of three times | +11.7 → +3.8 |
| The Dealer: 7 mana 3/5 (was 6, 4/6) | +10.4 → +1.3 |
| Archon Lumen: no "Equip costs are 0" | +9.3 → +7.8 |
| Neon Executioner: 7 mana, destroys at 1 Health or less | Zoo vs Sparkwrench 72% → 67% |
| Haste and summoning sickness removed from the game | Haste did nothing under the rounds |
| Deck swaps: Auditor (Rail Cannon → Hush Money, Overclock Rig → Archon Lumen), Sparkwrench (Fuse Goober → Snik, Marksman Scope → Retired Champion), Vesper (Fatal Rumor → Retired Champion) | spread 18.5 → 3.9 |
| Kept as printed: Final Broadcast (X+3 measured +7.7), Hush Money (every version stayed +9 to +11), Mob Rush (two Goobers: Goober 57% → 52%) | |

**Result** (2,000 games per pairing): Goober 58.4%, Sparkwrench 51.8%, Auditor 48.2%, Zoo 48.1%, Vesper 47.2%, Jungle 46.3%; spread 3.4 points. Past 65%: Goober vs Auditor 69.4%, Auditor vs Zoo 67.5%. Goober was left at 58% by choice; swapping Mob Rush out of the Goober list for Chaos Engine measured 52.4% (spread 2.0) if it's wanted later.

**Going forward**: balance is fixed with new cards (user, 2026-10-10). Watch list from the power table: Sproutling (+12.3: it grows every round), Final Broadcast (+12.5), and the weak buffs at the bottom.

---

## Not a problem (checked)

- **The Chain, priority, state-based actions, fizzling and multi-targets** behave like MTG in every test.
- **Hidden information**: each player's view hides opponents' hands and all decks.
- **Hand size 7 with everyone drawing**: a player sometimes discards in round 1. That's rare and harmless.
- **The Legendary rule, tokens, Curses falling off, and Equipment staying** all work as written.
