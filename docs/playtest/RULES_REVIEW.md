# Rules Review (2026-10-09)

A check of the core rules (GAME_DESIGN.md) for **fundamental** problems, based on 56,000 bot games (5 decks, 2 bot styles; full tables in [SIMULATION_REPORT.md](SIMULATION_REPORT.md)) and a read-through of the rules.

Bots are careful beginners. They race more than people do, so tempo effects are probably exaggerated. Treat the numbers as a direction, not exact values.

Severity: 🔴 fundamental (affects every game), 🟠 significant (affects a core promise of the game), 🟡 watch (an interaction to keep an eye on).

## Decisions (2026-10-09)

| Issue | Decision |
|---|---|
| 1. Going first | *(Superseded 2026-10-10 by Runeterra rounds, see below.)* **MTG default**: the first player skips their turn-1 draw, no other compensation. Judge it in human playtests (bots race more than people). ⚠ This is the variant with the biggest first-player edge in the bot runs: Greedy-bot mirrors at 500 games give Goober 79%, Jungle 70%, Zoo 68%, Sparkwrench 62% and Vesper 58%. If playtests confirm the edge, the best variant measured was "everyone draws + 2nd player +1 mana and 1 Gold" (Goober 66%, Jungle 53%). |
| 2. Gold sits unused | **Tavern Dweller Powers** are the universal Gold sink. *Implemented and re-measured (PLAYTEST.md, "Findings: Tavern Dwellers and abilities"): always-useful Powers (Keeper Z-00, Skabba) cut wasted mana by half or more; situational ones (Mukk, Sparkwrench, Auditor Prime) don't. The cap now changes waste but still not outcomes.* |
| 3. Long, stalled games | **No new rule.** Add late-game sinks and finishers through cards, re-measure, and decide later. *Re-measured: the Zoo mirror's long games dropped from 34% to 13% with Keeper Z-00. Some remaining stalls are the bot never alpha-striking (PLAYTEST.md).* |
| 4. Chip damage outside ping decks | Card-pool work: every faction needs some chip damage and some payoff for it (v0.2 already adds some). |
| 7. Gain life | **Capped at starting life** (GAME_DESIGN §11.1). |

---

## 🔴 1. Going first is a big advantage

**Update 2026-10-09 (after the attack planner): the edge grew** to 77-83% in the aggro mirrors (Goober, Jungle, Auditor). Decision: keep the MTG default for now and let human playtests judge. What was measured (report section "Mana model", first-player win% per mirror, 50% is fair):

| Variant | Goober | Jungle | Zoo | Vesper | Spark | Auditor | Avg off 50% |
|---|---|---|---|---|---|---|---|
| Today (MTG default) | 77 | 83 | 68 | 57 | 58 | 76 | 19.8 |
| Round pool (refill per round, bank at round end) | 86 | 86 | 57 | 60 | 59 | 76 | 20.7 |
| Round pool + Gold first on other players' turns | 83 | 83 | 53 | 56 | 59 | 76 | 18.5 |
| **Rotating first player** (A B, B A, A B...) | 53 | 53 | 53 | 41 | 47 | 47 | **4.0** |
| **Rotating + round pool + Gold first** | 48 | 51 | 53 | 45 | 46 | 46 | **3.3** |
| First player skips their first mana | 18 | 16 | 41 | 36 | 33 | 23 | 22.2 |

- **Why:** players already have equal mana every round; the edge is acting first in every round. The round pool doesn't fix that: the first player develops, then reacts on the opponent's turn with leftovers that would be banked anyway, while the second player must react *before* developing.
- **Rotating the first player** removes it. Cost: in 1v1 each player gets two turns in a row at every other round boundary (a creature played on the first can attack on the second).
- Switches (`FormatConfig`): `ManaPerRound`, `GoldFirstOffTurn`, `RotateRoundLeader`, `FirstPlayerSkipsFirstMana`. Not yet measured: rotation without the turn-1 draw skip.

Original analysis (before the v0.2 decks and the attack planner):

| Mirror (Greedy bots) | 1st player wins |
|---|---|
| Goober Mob (aggro) | **71%** |
| Sparkwrench Scrappers (burn) | 60% |
| Vesper's Ledger | 59% |
| Zoo Patrol | 59% |
| Jungle Stampede | 54% |

- 50% is fair; MTG sits around 52–55%.
- With defensive (Control) bots it shrinks (Goober 63%, Sparkwrench 49%), so part of it comes from bots racing. The aggro numbers stay high either way.
- **Why:** there are no lands and both players curve out, so the first player is always one mana ahead. Permanent damage makes this worse: the first creature on the board deals the first wounds, and they stick.
- Today's change (everyone draws, the second player gets +1 mana on their first turn) helped Jungle (60 → 54%) but hurt Goober (68 → 71%): the extra card matters less to aggro than tempo does.
- Adding **1 starting Gold** on top was the best variant measured (Goober 66%, Jungle 51%). Gold can now pay for Instants and Sorceries, so it's no longer useless early.

## 🟠 2. Gold mostly sits unused, and the cap changes nothing

| | Gold cap 3 | 5 | 8 |
|---|---|---|---|
| Vesper mirror win rate / game length | 51.5% / 23.8 | 51.1% / 23.8 | 51.2% / 23.7 |
| Mana wasted (lost to the cap) | 80% | 72% | 60% |

- In every non-aggro mirror, **43–84% of unspent mana is wasted**. Gold reaches the cap around turn 6 and stays there.
- Changing the cap changes nothing about who wins or how long games last.
- **Why:** Gold can only buy Instants, Sorceries and abilities, and creature decks run few of those. The pool has no activated abilities and no Tavern Dweller Powers yet.
- This threatens Vision point 2 ("unused mana is not wasted"). It's probably fixed by **Tavern Dweller Powers**, since every deck has one: a universal Gold sink. Re-measure after they exist before changing the rule itself.

## 🟠 3. Game length varies a lot, and slow games stall

| Mirror | Turns (avg ± sd) | Games over 25 turns |
|---|---|---|
| Goober Mob | 13 ± 2 | 0% |
| Jungle Stampede | 19 ± 5 | 10% |
| Zoo Patrol | 23 ± 7 | 32% |
| Vesper's Ledger | 24 ± 5 | 35% |
| Vesper's Ledger, Control bots | **33 ± 10** | **76%** |

- **Starting life doesn't fix the spread.** 25 or 35 life moves every deck by about 1–2 turns, so fast decks stay fast and slow ones stay slow.
- The long games are the ones with **lots of wasted mana** (70–84%) and **lots of healing** (Vesper: 28 per game). Players have nothing left to spend on, and the board is stuck.
- Permanent damage makes the slow mirrors longer (Zoo 19 → 23 turns), because attacking into blockers leaves wounds that never go away, so players attack less.
- **Why:** nothing in the rules pushes a stalled game to an end. Mana stops at 10, Gold stops at 5, and healing undoes progress.
- Options: universal late-game sinks (Tavern Dweller Powers again), late-game finishers in every faction, or a rule that forces games to end (see the questions).

## 🟠 4. Chip damage only matters in ping decks

"Chip→death" is the share of damage carried into a later turn that was still on the creature when it died.

| Mirror | Chip→death | Creatures wounded at turn start |
|---|---|---|
| Goober Mob | 29% | 2% |
| Jungle Stampede | 36% | 12% |
| Vesper's Ledger | 42% | 9% |
| Zoo Patrol (pings, fights) | **82%** | 47% |
| Sparkwrench Scrappers (pings, burn) | **79%** | 21% |

- In plain creature decks, **60–70% of chip damage never decides anything**: it gets healed, or sits on a creature that survives. Switching to MTG-style damage changes their win rates by about 1%.
- In ping and fight decks, chip damage is the plan, and it works.
- **Why:** combat mostly ends in "something dies" or "nobody blocks". Wounds that survive come from blocks where both creatures live, and those creatures are often healed or just survive to the end.
- This is a card-pool question, not a rules flaw. The v0.2 "damaged" payoffs (Kick 'Em While They're Down, Blood Price, Ambush Predator, Finisher Protocol, Smart Rounds) exist exactly for this. The design rule to keep: **every faction needs some chip damage and some payoff for it**.

## 🟡 5. Temporary Health buffs work as damage shields

Because "losing a buff can't kill" (§7.3), a "+0/+X until end of turn" effect absorbs up to X damage and the creature still survives with 1 Health. Example: Bark Skin makes a 2/2 into a 3/6 with a counter. It can take 5 damage, and at end of turn it becomes a 3/3 at 1 Health left. That's legal and intended, but it makes Health buffs much stronger than in MTG. Price them like healing plus protection.

## ✅ 6. Mana-first payment creates a sequencing trap (solved 2026-10-10)

**Solved:** the Standard rules now pay Gold first (GAME_DESIGN §5.2). Gold can't pay for permanents, so spending it first never strands a creature. Tested in `RuneterraManaTests.GoldFirst_NoSequencingTrap_SorceryThenCreature`.


Mana is spent first automatically (§5.2). If you cast a 3-mana Sorcery before a 3-mana creature while holding 3 mana and 3 Gold, the Sorcery eats the mana and the creature can't be cast. The fix is to cast the creature first. Fine for experienced players, but the UI should warn or order plays. It also means you can never use Gold to save mana for banking.

## 🟡 7. Is "gain life" capped?

Healing your Tavern Dweller stops at starting life (§11.1). Lifelink "heals". The engine currently treats "you gain N life" (drains, The Grand Ledger) the same way, so it's capped at 30. MTG has no cap. This shapes the Sensationalist drain identity and should be decided on purpose.

---

# Design review (2026-10-10): Runeterra-style mana

A read-through of the Standard rules after the switch to Runeterra-style mana (GAME_DESIGN §5–6.1), using the bot runs in [SIMULATION_REPORT.md](SIMULATION_REPORT.md) ("Runeterra-style mana") and [PLAYTEST.md](PLAYTEST.md). Numbers are from mirrors only; cross-faction games under these rules haven't been measured yet.

## Decisions (2026-10-10 review)

| Flaw | Decision |
|---|---|
| R1, R3. Double turns, going first | ✅ **Legends of Runeterra rounds** (GAME_DESIGN §6): alternating single actions, the round leader holds the attack token (passes every round), no summoning sickness, everyone draws in round 1, no going-first compensation. Measured below. |
| R2. Attack token and card values | Token kept (real LoR). Still open: "whenever you attack" cards trigger every other round, and **Haste is now blank** (GAME_DESIGN §7.4, 12 cards). Tavern Dweller Powers are now once each round instead of up to twice. Card pass needed. |
| R4. Multiplayer | Still open (GAME_DESIGN §13). The engine's multiplayer format keeps MTG turns for now. |
| 8. Invest payment bug | ✅ Fixed: Gold-only parts are set aside before the cost takes Gold first (`Payment.TrySplit`, GAME_DESIGN §5.2). |
| 11. Weak Tavern Dwellers | ✅ New Powers for Mukk, Sparkwrench and Auditor Prime (cards/tavern_dwellers.md). |
| 12. Doc contradictions | ✅ Fixed; Runeterra mana confirmed and locked by the user (Decision Log). |

## Turn structure measurements (2026-10-10)

First-player win% in bot mirrors, 400–500 games each (50% is fair). Full rows: SIMULATION_REPORT.md, "Turn structure and going first"; SimRunner `-goingfirst` (MTG-turn variants with `-rules mtg`).

| Turn structure | Goober | Jungle | Zoo | Vesper | Spark | Auditor | Avg off 50% |
|---|---|---|---|---|---|---|---|
| MTG turns (A B A B), round pool, draw skip | 85 | 85 | 58 | 50 | 77 | 73 | 21 |
| … + 2nd player 1–2 starting Gold | 78 | 82 | 56 | 49 | 70 | 60 | 16 |
| … + 2nd player +1 mana on their first turn | 75 | 82 | 52 | 49 | 70 | 65 | 16 |
| … + 2nd player +1 mana on their first 3 turns | 44 | 57 | 44 | 37 | 56 | 50 | 6.4 |
| … 1st player one max mana behind all game | 17 | 30 | 26 | 27 | 33 | 17 | 25 |
| … mana until your next turn (like MTG lands) | 85 | 79 | 70 | 60 | 75 | 78 | 24.5 |
| A B \| B A + attack token (morning of 2026-10-10) | 48 | 50 | 48 | 41 | 51 | 51 | 2.8 |
| **Runeterra rounds (Standard)** | 54 | 47 | 47 | 45 | 51 | 52 | **~3** |

- With MTG turns the edge held with defensive bots, 40 life, damage wearing off and without Tavern Dwellers, and a smarter bot (saving mana to build on its own turn) changed nothing: it is the rules, not the bots. Without lands both players curve out perfectly, and the player who acts first in each round is always a play ahead. One mana flips the result (rows 5 and 6), so every compensation felt bad for one side.
- The rounds fix the cause: every play gets an answer before the next, and the attack alternates. Variants measured: with summoning sickness (fair, but games about 10% longer and more mana lost to the cap), everyone may attack once a round (fair, games 25% shorter, less waste).
- Bot timing: attacking as the first action vs after developing is deck-dependent (Goober prefers last 62%, Zoo first 63%); `BotStyle.AttackFirstInRound`, off by default.
- Cross-faction balance is poor under every turn structure (16–18 points spread, SimRunner `-balance`): a card and deck question.

## ✅ R1. Double turns remove the opponent's sorcery-speed window (solved 2026-10-10: Runeterra rounds)

Under A B | B A, each player's build turn is followed straight away by their own attack turn. Creatures played on the build turn attack next turn, and the opponent can only answer with Instants in between. Summoning sickness almost never matters, and Haste only matters on attack turns. (PLAYTEST.md's open question "does the double turn feel good?" is this.)

## 🔴 R2. The attack token shifts card values without repricing

- Goober Rascal (Haste, can't block) does nothing on build turns; Goobers' identity (go wide, attack every turn) is weakened most.
- "Whenever you attack" cards trigger half as often (Grakka, Pulse Blade, Sparkwrench's attack triggers).
- Encore From Beyond and Silver-Tongued Deal are dead cards on build turns.
- "At the start of your turn" effects are worth twice as much per attack (Closing Bell, the Glitterworld pings, Wild heal-per-turn creatures).

## ✅ R3. The going-first rule is now backwards (solved 2026-10-10: Runeterra rounds, no compensation)

§3 still has the first player skip their turn-1 draw, but with rotation the **second** player gets the first real attack (plays turn 2, attacks turn 3; the first player's next attack is turn 5). The first player wins only 41–50% under Runeterra rules (Vesper 40.8%). Rotation without the draw skip was never measured.

## 🔴 R4. The attack token breaks multiplayer

With 4 players and a rotating token, each player attacks once every 4 rounds, and one round's mana pool covers 4 turns of Instants. §13 isn't updated for rounds or the token.

## 🟠 R5. "Unused mana is not wasted" is false in long games

35–65% of leftover mana is lost to Gold cap 3 (Jungle, Zoo, Vesper, Sparkwrench). The cap never changes who wins (3, 5 and 8 give the same results), so the cap only adds waste. Options: raise the cap, or have overflow mana become something else.

## 🟠 R6. Permanent damage is invisible in 4 of 6 decks

Making damage wear off like MTG changes win rates by about 1%. Only the ping decks (Zoo and Sparkwrench) feel it. (Same finding as #4 above, still true under the new mana.)

## 🟠 R7. Stalls remain

Runeterra mana made games 10–20% longer. The Vesper mirror has 21% of games over 25 turns and ~34 healing per game. No rule ends stalled games, and the v0.3 finishers aren't measured for this yet.

## 🟠 R8. Gold first works against "hold Gold" payoffs

Loan Shark, Velvet Embezzler, The Dealer and Invest reward holding Gold, but Gold is always spent first and the player can't choose. The engine bug in this area (the spell's cost eating the Invest Gold) is fixed; the design tension remains.

## 🟠 R9. Gold cap 3 removes most of the shady-deal downside

Golden Handshake ("up to 5 Gold"), Everything Has a Price ("gains 5 Gold") and Hostile Takeover give the opponent Gold, but an opponent already at the cap gets nothing. Everything Has a Price's text can't do what it says.

## 🟡 R10. Temporary Health buffs are repeatable damage shields

"Losing a buff can't kill" (§7.3) plus repeatable buffs: Old Mossbank's "(2) +2/+2" can be used once each turn. Under Runeterra rounds that is once each round, as a response, so it's a damage shield in every combat.

## ✅ R11. Weak Tavern Dwellers (solved 2026-10-10)

Auditor Prime's Power was used 0–0.5 times per game; Mukk's and Sparkwrench's depended on the situation. With one Tavern Dweller per pair, a weak one weakens the whole pair. All three have new Powers.

---

## Not a problem (checked)

- **The Chain, priority, state-based actions, fizzling and multi-targets** behave like MTG in every test.
- **Hidden information**: each player's view hides opponents' hands and all decks.
- **Hand size 7 with everyone drawing**: the first player sometimes discards on turn 1. That's rare and harmless.
- **The Legendary rule, tokens, Curses falling off, and Equipment staying** all work as written.
- **Matchup spread** (Vesper's Ledger loses most matchups) is a **card balance** issue in a 15-card prototype deck, not a rules issue.
