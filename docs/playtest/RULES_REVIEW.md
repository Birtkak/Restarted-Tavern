# Rules Review (2026-10-09)

A check of the core rules (GAME_DESIGN.md) for **fundamental** problems, based on 56,000 bot games (5 decks, 2 bot styles; full tables in [SIMULATION_REPORT.md](SIMULATION_REPORT.md)) and a read-through of the rules.

Bots are careful beginners. They race more than people do, so tempo effects are probably exaggerated. Treat the numbers as a direction, not exact values.

Severity: 🔴 fundamental (affects every game), 🟠 significant (affects a core promise of the game), 🟡 watch (an interaction to keep an eye on).

## Decisions (2026-10-09)

| Issue | Decision |
|---|---|
| 1. Going first | **MTG default**: the first player skips their turn-1 draw, no other compensation. Judge it in human playtests (bots race more than people). ⚠ This is the variant with the biggest first-player edge in the bot runs: Greedy-bot mirrors at 500 games give Goober 79%, Jungle 70%, Zoo 68%, Sparkwrench 62% and Vesper 58%. If playtests confirm the edge, the best variant measured was "everyone draws + 2nd player +1 mana and 1 Gold" (Goober 66%, Jungle 53%). |
| 2. Gold sits unused | **Patron Powers** are the universal Gold sink. Implement them next, then re-measure the Gold cap. |
| 3. Long, stalled games | **No new rule.** Add late-game sinks and finishers through cards, re-measure, and decide later. |
| 4. Chip damage outside ping decks | Card-pool work: every faction needs some chip damage and some payoff for it (v0.2 already adds some). |
| 7. Gain life | **Capped at starting life** (GAME_DESIGN §11.1). |

---

## 🔴 1. Going first is a big advantage

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
- **Why:** Gold can only buy Instants, Sorceries and abilities, and creature decks run few of those. The pool has no activated abilities and no Patron Powers yet.
- This threatens Vision point 2 ("unused mana is not wasted"). It's probably fixed by **Patron Powers**, since every deck has one: a universal Gold sink. Re-measure after they exist before changing the rule itself.

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
- Options: universal late-game sinks (Patron Powers again), late-game finishers in every faction, or a rule that forces games to end (see the questions).

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

## 🟡 6. Mana-first payment creates a sequencing trap

Mana is spent first automatically (§5.2). If you cast a 3-mana Sorcery before a 3-mana creature while holding 3 mana and 3 Gold, the Sorcery eats the mana and the creature can't be cast. The fix is to cast the creature first. Fine for experienced players, but the UI should warn or order plays. It also means you can never use Gold to save mana for banking.

## 🟡 7. Is "gain life" capped?

Healing your Patron stops at starting life (§11.1). Lifelink "heals". The engine currently treats "you gain N life" (drains, The Grand Ledger) the same way, so it's capped at 30. MTG has no cap. This shapes the Sensationalist drain identity and should be decided on purpose.

---

## Not a problem (checked)

- **The Chain, priority, state-based actions, fizzling and multi-targets** behave like MTG in every test.
- **Hidden information**: each player's view hides opponents' hands and all decks.
- **Hand size 7 with everyone drawing**: the first player sometimes discards on turn 1. That's rare and harmless.
- **The Legendary rule, tokens, Curses falling off, and Equipment staying** all work as written.
- **Matchup spread** (Vesper's Ledger loses most matchups) is a **card balance** issue in a 15-card prototype deck, not a rules issue.
