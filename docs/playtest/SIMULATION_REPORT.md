# Simulation Report

Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

500 games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time 32s.

Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) ± standard deviation · **Long** / **Short** = share of games over 25 / under 10 turns · **Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · **Chip→death** = share of *chip damage* (damage a creature carried into a later turn) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · **Wounded** = share of creatures carrying damage at the start of a turn · **Deaths** / **Heal** / **Gold spent** are per game · **Powers** = Tavern Dweller Powers used per game (in brackets: share used on an opponent's turn) · **Abil.** = other activated abilities per game (Equip, Tap abilities...) · **Off-turn** = spells cast and abilities/Powers used on an opponent's turn, per game · **Wasted** = share of unspent mana lost to the Gold cap.

## Round robin (current rules, Greedy bots)

*How long are games, and does damage on creatures decide anything? Every deck against every deck.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror | 51.6% | 54.2% | 9.0 ± 1.1 | 0.0% | 72.2% | 93.9% | 76.2% | 9.8% | 28.2 | 0.0 | 4.3 | 3.2 (21.4%) | 0.0 | 8.0 | 14.1% | 0 |
| Goober Mob vs Jungle Stampede | 58.4% | 47.4% | 8.9 ± 1.2 | 0.0% | 72.2% | 91.8% | 75.7% | 14.5% | 18.6 | 1.3 | 5.6 | 5.1 (47.9%) | 0.3 | 9.7 | 7.5% | 0 |
| Goober Mob vs Zoo Patrol | 44.6% | 50.0% | 9.9 ± 1.3 | 0.0% | 38.0% | 88.8% | 58.1% | 12.5% | 23.0 | 4.2 | 6.2 | 3.6 (22.9%) | 0.1 | 8.8 | 15.7% | 0 |
| Goober Mob vs Vesper's Ledger | 70.0% | 52.2% | 9.3 ± 1.3 | 0.0% | 57.6% | 91.4% | 64.1% | 8.2% | 18.3 | 14.3 | 16.4 | 4.0 (17.4%) | 2.2 | 9.6 | 21.4% | 0 |
| Goober Mob vs Sparkwrench Scrappers | 87.4% | 54.8% | 9.3 ± 1.5 | 0.0% | 60.8% | 94.2% | 81.7% | 12.5% | 21.9 | 0.0 | 4.5 | 4.6 (27.8%) | 1.6 | 10.0 | 30.2% | 0 |
| Goober Mob vs Auditor's Arsenal | 93.0% | 55.2% | 8.2 ± 1.1 | 0.0% | 91.4% | 80.1% | 61.8% | 8.3% | 10.7 | 0.3 | 3.2 | 1.4 (34.9%) | 3.2 | 9.0 | 3.6% | 0 |
| Jungle Stampede mirror | 51.6% | 47.0% | 13.3 ± 3.0 | 0.2% | 9.2% | 97.0% | 91.9% | 7.4% | 25.3 | 2.7 | 16.6 | 10.6 (54.7%) | 0.7 | 15.4 | 32.6% | 0 |
| Jungle Stampede vs Zoo Patrol | 41.6% | 48.2% | 11.5 ± 2.1 | 0.0% | 15.0% | 91.8% | 88.4% | 21.4% | 17.1 | 4.6 | 12.1 | 5.7 (50.7%) | 0.3 | 10.8 | 25.8% | 0 |
| Jungle Stampede vs Vesper's Ledger | 71.4% | 54.4% | 10.5 ± 1.7 | 0.0% | 27.8% | 91.6% | 79.8% | 8.1% | 13.7 | 18.2 | 16.4 | 6.8 (39.1%) | 1.5 | 12.6 | 8.3% | 0 |
| Jungle Stampede vs Sparkwrench Scrappers | 87.4% | 50.4% | 11.3 ± 2.2 | 0.0% | 22.0% | 95.7% | 88.7% | 17.0% | 19.4 | 1.5 | 9.2 | 9.8 (39.9%) | 3.3 | 13.2 | 25.5% | 0 |
| Jungle Stampede vs Auditor's Arsenal | 91.6% | 50.6% | 9.4 ± 1.5 | 0.0% | 57.8% | 83.8% | 55.4% | 12.8% | 9.0 | 2.0 | 5.5 | 3.6 (55.2%) | 5.3 | 11.4 | 4.7% | 0 |
| Zoo Patrol mirror | 49.8% | 47.2% | 11.9 ± 1.8 | 0.0% | 6.4% | 69.6% | 65.2% | 29.7% | 14.2 | 20.4 | 13.2 | 8.7 (34.2%) | 0.2 | 12.6 | 23.2% | 0 |
| Zoo Patrol vs Vesper's Ledger | 69.8% | 51.6% | 11.2 ± 1.8 | 0.0% | 14.8% | 89.1% | 76.7% | 10.4% | 14.2 | 17.4 | 16.8 | 4.2 (18.8%) | 1.6 | 9.9 | 24.3% | 0 |
| Zoo Patrol vs Sparkwrench Scrappers | 94.2% | 52.0% | 11.1 ± 1.7 | 0.0% | 13.4% | 82.9% | 63.6% | 22.4% | 14.8 | 7.3 | 8.3 | 7.5 (26.5%) | 2.5 | 11.6 | 25.6% | 0 |
| Zoo Patrol vs Auditor's Arsenal | 66.8% | 48.2% | 10.4 ± 1.2 | 0.0% | 21.2% | 76.0% | 52.9% | 17.0% | 8.6 | 7.3 | 5.5 | 2.0 (43.0%) | 7.1 | 11.1 | 4.8% | 0 |
| Vesper's Ledger mirror | 52.8% | 44.6% | 12.9 ± 2.7 | 0.0% | 8.0% | 95.5% | 76.2% | 3.1% | 18.5 | 39.7 | 32.0 | 6.6 (22.3%) | 6.3 | 16.0 | 31.0% | 0 |
| Vesper's Ledger vs Sparkwrench Scrappers | 52.0% | 45.8% | 10.8 ± 1.7 | 0.0% | 19.8% | 92.7% | 74.4% | 9.5% | 13.6 | 14.9 | 15.6 | 6.0 (20.1%) | 4.9 | 12.0 | 34.4% | 0 |
| Vesper's Ledger vs Auditor's Arsenal | 66.8% | 55.0% | 10.2 ± 1.3 | 0.0% | 33.2% | 86.0% | 53.9% | 3.6% | 9.0 | 15.8 | 12.0 | 1.9 (36.5%) | 9.8 | 14.0 | 6.5% | 0 |
| Sparkwrench Scrappers mirror | 49.6% | 51.0% | 13.5 ± 2.4 | 0.0% | 4.0% | 98.1% | 91.9% | 21.0% | 25.8 | 0.0 | 10.5 | 12.8 (28.9%) | 11.5 | 23.7 | 49.8% | 0 |
| Sparkwrench Scrappers vs Auditor's Arsenal | 65.4% | 53.6% | 10.0 ± 1.5 | 0.0% | 40.4% | 81.4% | 56.1% | 14.0% | 8.3 | 2.5 | 3.0 | 2.8 (33.2%) | 9.4 | 13.3 | 18.9% | 0 |
| Auditor's Arsenal mirror | 51.4% | 52.4% | 10.2 ± 1.1 | 0.0% | 26.4% | 78.7% | 45.8% | 5.7% | 4.4 | 1.5 | 2.8 | 0.2 (10.0%) | 17.5 | 14.0 | 7.1% | 0 |

## Bot play styles

*Do the results hold with a defensive player? Control blocks freely, keeps blockers home and saves Gold. In "Greedy vs Control", A win% is the Greedy side.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, Control vs Control | 51.4% | 48.0% | 10.5 ± 1.4 | 0.0% | 23.0% | 95.0% | 85.2% | 14.5% | 39.1 | 0.0 | 5.4 | 5.1 (22.1%) | 0.0 | 10.0 | 32.5% | 0 |
| Jungle Stampede mirror, Control vs Control | 45.8% | 50.4% | 17.5 ± 4.8 | 7.0% | 0.6% | 96.7% | 93.4% | 17.9% | 33.5 | 4.9 | 27.4 | 17.4 (49.0%) | 1.0 | 21.9 | 48.9% | 0 |
| Zoo Patrol mirror, Control vs Control | 49.6% | 48.6% | 16.3 ± 3.4 | 1.2% | 0.6% | 68.7% | 79.2% | 37.3% | 20.9 | 34.9 | 25.0 | 14.5 (35.3%) | 0.6 | 18.7 | 48.8% | 0 |
| Vesper's Ledger mirror, Control vs Control | 49.4% | 48.0% | 14.1 ± 2.9 | 0.0% | 2.4% | 95.1% | 78.6% | 2.5% | 18.4 | 42.1 | 34.4 | 8.0 (23.6%) | 8.2 | 17.7 | 25.2% | 0 |
| Sparkwrench Scrappers mirror, Control vs Control | 52.0% | 51.0% | 18.0 ± 3.8 | 3.2% | 0.4% | 98.5% | 92.7% | 24.2% | 36.5 | 0.0 | 17.6 | 19.6 (29.1%) | 18.0 | 35.4 | 60.7% | 0 |
| Auditor's Arsenal mirror, Control vs Control | 50.0% | 50.2% | 13.4 ± 2.4 | 0.0% | 1.6% | 74.1% | 64.1% | 15.4% | 10.5 | 11.4 | 8.1 | 1.1 (6.2%) | 41.4 | 20.0 | 36.6% | 0 |
| Goober Mob mirror, Greedy vs Control | 67.6% | 54.6% | 9.7 ± 1.3 | 0.0% | 46.0% | 94.5% | 81.1% | 12.4% | 33.2 | 0.0 | 4.7 | 4.2 (21.4%) | 0.0 | 9.0 | 20.5% | 0 |
| Jungle Stampede mirror, Greedy vs Control | 61.0% | 49.6% | 15.3 ± 4.2 | 2.2% | 5.0% | 97.0% | 93.5% | 13.9% | 29.4 | 3.7 | 22.0 | 13.4 (50.5%) | 0.9 | 18.8 | 44.3% | 0 |
| Zoo Patrol mirror, Greedy vs Control | 54.8% | 46.6% | 13.9 ± 2.6 | 0.0% | 1.4% | 69.3% | 74.2% | 35.3% | 17.3 | 27.0 | 17.6 | 11.3 (34.4%) | 0.4 | 15.2 | 39.2% | 0 |
| Vesper's Ledger mirror, Greedy vs Control | 59.6% | 49.0% | 13.3 ± 2.9 | 0.0% | 6.2% | 95.1% | 78.7% | 2.7% | 17.9 | 39.7 | 32.3 | 7.1 (22.6%) | 6.8 | 16.5 | 29.2% | 0 |
| Sparkwrench Scrappers mirror, Greedy vs Control | 63.6% | 50.2% | 15.5 ± 3.1 | 0.2% | 1.0% | 98.5% | 93.4% | 22.2% | 31.3 | 0.0 | 13.9 | 15.9 (30.0%) | 14.3 | 29.2 | 55.7% | 0 |
| Auditor's Arsenal mirror, Greedy vs Control | 68.4% | 50.6% | 11.5 ± 1.8 | 0.0% | 8.2% | 78.3% | 61.9% | 15.4% | 7.4 | 5.1 | 4.5 | 0.5 (9.7%) | 26.8 | 16.6 | 20.6% | 0 |

## Game length lever: starting life

*Can starting life even out game length between fast and slow decks? Compare with the 30-life mirrors in the round robin.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, 25 life | 50.0% | 53.4% | 8.2 ± 1.0 | 0.0% | 90.8% | 93.3% | 70.2% | 9.2% | 23.1 | 0.0 | 3.6 | 2.5 (21.4%) | 0.0 | 7.3 | 4.3% | 0 |
| Jungle Stampede mirror, 25 life | 51.4% | 48.0% | 12.0 ± 2.8 | 0.2% | 17.6% | 96.7% | 89.3% | 7.4% | 22.5 | 2.5 | 13.8 | 8.8 (55.6%) | 0.6 | 13.8 | 25.6% | 0 |
| Zoo Patrol mirror, 25 life | 49.0% | 44.8% | 10.8 ± 1.6 | 0.0% | 18.4% | 69.7% | 64.3% | 29.6% | 12.1 | 16.9 | 11.3 | 7.2 (34.3%) | 0.2 | 11.2 | 14.9% | 0 |
| Vesper's Ledger mirror, 25 life | 50.0% | 46.6% | 11.6 ± 2.4 | 0.0% | 19.0% | 94.9% | 71.9% | 3.0% | 14.8 | 32.5 | 26.8 | 5.1 (22.0%) | 4.6 | 13.6 | 29.8% | 0 |
| Sparkwrench Scrappers mirror, 25 life | 51.0% | 50.0% | 12.3 ± 2.3 | 0.0% | 9.8% | 97.7% | 91.8% | 20.8% | 22.5 | 0.0 | 8.1 | 10.8 (29.2%) | 8.9 | 20.8 | 44.8% | 0 |
| Auditor's Arsenal mirror, 25 life | 49.0% | 53.2% | 9.6 ± 1.1 | 0.0% | 50.0% | 76.9% | 41.9% | 6.1% | 4.0 | 1.4 | 2.3 | 0.1 (14.6%) | 14.4 | 13.3 | 3.7% | 0 |
| Goober Mob mirror, 35 life | 50.6% | 54.8% | 9.6 ± 1.3 | 0.0% | 48.0% | 94.1% | 77.6% | 10.0% | 32.9 | 0.0 | 5.3 | 4.0 (21.6%) | 0.0 | 8.8 | 24.1% | 0 |
| Jungle Stampede mirror, 35 life | 52.8% | 47.8% | 14.7 ± 3.5 | 1.0% | 2.8% | 97.2% | 91.8% | 7.8% | 28.3 | 3.0 | 20.0 | 12.3 (54.5%) | 0.9 | 17.1 | 41.2% | 0 |
| Zoo Patrol mirror, 35 life | 48.8% | 47.8% | 12.9 ± 2.1 | 0.0% | 1.6% | 69.3% | 68.5% | 30.1% | 16.0 | 23.9 | 15.7 | 10.2 (35.2%) | 0.3 | 14.0 | 32.3% | 0 |
| Vesper's Ledger mirror, 35 life | 49.6% | 45.0% | 14.1 ± 2.9 | 0.0% | 3.0% | 96.0% | 76.4% | 2.7% | 21.0 | 46.2 | 36.1 | 7.9 (22.7%) | 7.7 | 17.8 | 33.4% | 0 |
| Sparkwrench Scrappers mirror, 35 life | 52.0% | 51.0% | 14.5 ± 2.6 | 0.0% | 1.4% | 98.3% | 92.2% | 20.6% | 28.3 | 0.0 | 12.5 | 14.4 (29.0%) | 13.5 | 26.1 | 52.9% | 0 |
| Auditor's Arsenal mirror, 35 life | 51.2% | 49.4% | 10.8 ± 1.2 | 0.0% | 10.0% | 79.5% | 44.4% | 5.6% | 5.1 | 1.9 | 3.7 | 0.3 (7.5%) | 21.3 | 15.0 | 14.7% | 0 |

## Permanent damage (GAME_DESIGN §7.3)

*What changes when damage wears off at end of turn like in MTG? Compare with the round-robin mirrors (permanent damage).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, damage wears off (MTG) | 51.8% | 54.0% | 8.7 ± 1.1 | 0.0% | 80.0% | 91.2% | 0.0% | 0.0% | 26.2 | 0.0 | 4.4 | 2.9 (22.5%) | 0.0 | 7.6 | 8.4% | 0 |
| Jungle Stampede mirror, damage wears off (MTG) | 53.0% | 48.8% | 13.1 ± 2.9 | 0.2% | 8.2% | 96.6% | 0.0% | 0.0% | 24.8 | 2.6 | 16.0 | 10.5 (53.8%) | 0.8 | 14.9 | 31.2% | 0 |
| Zoo Patrol mirror, damage wears off (MTG) | 50.4% | 45.8% | 11.3 ± 1.8 | 0.0% | 12.2% | 69.1% | 0.0% | 0.0% | 12.8 | 12.6 | 11.8 | 6.3 (26.7%) | 0.2 | 10.1 | 22.3% | 0 |
| Vesper's Ledger mirror, damage wears off (MTG) | 52.4% | 43.8% | 13.0 ± 2.7 | 0.0% | 8.6% | 93.1% | 0.0% | 0.0% | 18.2 | 40.4 | 31.8 | 6.6 (22.5%) | 6.5 | 15.6 | 32.1% | 0 |
| Sparkwrench Scrappers mirror, damage wears off (MTG) | 51.8% | 50.0% | 13.1 ± 2.3 | 0.0% | 5.0% | 96.8% | 0.0% | 0.0% | 24.1 | 0.0 | 10.0 | 11.8 (27.5%) | 10.0 | 21.0 | 51.0% | 0 |
| Auditor's Arsenal mirror, damage wears off (MTG) | 51.2% | 52.6% | 10.2 ± 1.1 | 0.0% | 27.8% | 75.8% | 0.0% | 0.0% | 3.9 | 0.9 | 2.8 | 0.2 (14.6%) | 17.0 | 13.6 | 5.1% | 0 |

## Turn structure and going first (GAME_DESIGN §3, §6)

*First-player win% in mirrors (50% is fair). Standard = Legends of Runeterra rounds: everyone refills, untaps and draws when a round starts, players alternate actions, the round leader holds the attack token, no summoning sickness. Turns = rounds with Runeterra rounds (one round is everyone's turn), turns otherwise.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, Runeterra rounds (Standard) | 51.6% | 54.2% | 9.0 ± 1.1 | 0.0% | 72.2% | 93.9% | 76.2% | 9.8% | 28.2 | 0.0 | 4.3 | 3.2 (21.4%) | 0.0 | 8.0 | 14.1% | 0 |
| Goober Mob mirror, rounds with summoning sickness | 52.0% | 50.2% | 9.6 ± 1.3 | 0.0% | 49.6% | 94.2% | 73.8% | 10.1% | 30.4 | 0.0 | 4.7 | 3.8 (21.4%) | 0.0 | 8.3 | 24.6% | 0 |
| Goober Mob mirror, rounds, everyone attacks once a round | 49.6% | 53.0% | 7.1 ± 0.9 | 0.0% | 99.6% | 93.7% | 69.9% | 9.6% | 18.8 | 0.0 | 3.6 | 1.6 (23.2%) | 0.0 | 6.6 | 0.8% | 0 |
| Goober Mob mirror, MTG turns (A B A B), draw skip | 51.8% | 84.8% | 12.4 ± 1.6 | 0.0% | 5.4% | 92.8% | 68.0% | 6.5% | 12.2 | 0.0 | 3.5 | 2.2 (36.3%) | 0.0 | 2.8 | 1.2% | 0 |
| Goober Mob mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 47.4% | 43.2% | 12.3 ± 1.6 | 0.0% | 4.0% | 93.4% | 73.6% | 7.2% | 12.9 | 0.0 | 3.1 | 2.3 (35.3%) | 0.0 | 3.0 | 1.9% | 0 |
| Goober Mob mirror, A B | B A + attack token (2026-10-10 morning) | 48.6% | 47.6% | 13.8 ± 1.6 | 0.0% | 0.0% | 93.7% | 71.4% | 3.5% | 12.7 | 0.0 | 4.2 | 3.2 (46.0%) | 0.0 | 3.5 | 2.5% | 0 |
| Jungle Stampede mirror, Runeterra rounds (Standard) | 51.6% | 47.0% | 13.3 ± 3.0 | 0.2% | 9.2% | 97.0% | 91.9% | 7.4% | 25.3 | 2.7 | 16.6 | 10.6 (54.7%) | 0.7 | 15.4 | 32.6% | 0 |
| Jungle Stampede mirror, rounds with summoning sickness | 48.6% | 49.2% | 19.0 ± 5.9 | 13.0% | 0.2% | 97.2% | 92.7% | 9.6% | 36.1 | 4.4 | 33.1 | 19.8 (48.3%) | 1.1 | 20.1 | 51.5% | 0 |
| Jungle Stampede mirror, rounds, everyone attacks once a round | 52.0% | 49.8% | 9.2 ± 2.0 | 0.0% | 62.2% | 96.2% | 86.7% | 5.8% | 15.2 | 1.4 | 8.7 | 4.7 (61.5%) | 0.4 | 10.1 | 8.0% | 0 |
| Jungle Stampede mirror, MTG turns (A B A B), draw skip | 52.4% | 85.0% | 19.4 ± 6.1 | 16.8% | 0.0% | 96.8% | 86.6% | 5.9% | 15.0 | 1.4 | 10.4 | 7.2 (46.9%) | 0.2 | 6.7 | 15.9% | 0 |
| Jungle Stampede mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 48.2% | 56.4% | 21.9 ± 6.2 | 25.4% | 0.0% | 97.0% | 87.4% | 7.1% | 19.0 | 1.8 | 11.2 | 8.9 (46.9%) | 0.4 | 7.9 | 19.1% | 0 |
| Jungle Stampede mirror, A B | B A + attack token (2026-10-10 morning) | 45.8% | 50.4% | 26.3 ± 5.9 | 47.6% | 0.0% | 96.7% | 85.5% | 9.4% | 23.4 | 2.7 | 16.7 | 13.1 (46.4%) | 0.5 | 9.1 | 26.3% | 0 |
| Zoo Patrol mirror, Runeterra rounds (Standard) | 49.8% | 47.2% | 11.9 ± 1.8 | 0.0% | 6.4% | 69.6% | 65.2% | 29.7% | 14.2 | 20.4 | 13.2 | 8.7 (34.2%) | 0.2 | 12.6 | 23.2% | 0 |
| Zoo Patrol mirror, rounds with summoning sickness | 48.6% | 47.2% | 13.4 ± 2.2 | 0.0% | 1.4% | 67.9% | 68.5% | 31.4% | 16.7 | 26.4 | 17.0 | 11.2 (34.7%) | 0.2 | 14.4 | 35.6% | 0 |
| Zoo Patrol mirror, rounds, everyone attacks once a round | 48.4% | 44.6% | 9.1 ± 1.3 | 0.0% | 63.0% | 70.4% | 60.3% | 30.2% | 8.7 | 11.2 | 8.6 | 4.9 (33.1%) | 0.1 | 9.1 | 5.0% | 0 |
| Zoo Patrol mirror, MTG turns (A B A B), draw skip | 51.8% | 55.2% | 19.8 ± 3.0 | 5.2% | 0.0% | 68.6% | 58.5% | 24.0% | 10.3 | 15.3 | 10.0 | 7.3 (27.7%) | 0.0 | 6.2 | 9.6% | 0 |
| Zoo Patrol mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 49.6% | 43.0% | 19.0 ± 3.0 | 1.8% | 0.0% | 67.8% | 57.5% | 24.5% | 9.7 | 14.8 | 8.8 | 7.0 (28.3%) | 0.0 | 5.9 | 6.9% | 0 |
| Zoo Patrol mirror, A B | B A + attack token (2026-10-10 morning) | 50.6% | 48.4% | 22.2 ± 2.9 | 10.2% | 0.0% | 68.0% | 61.0% | 24.3% | 12.5 | 19.6 | 11.9 | 8.8 (38.9%) | 0.0 | 7.3 | 19.2% | 0 |
| Vesper's Ledger mirror, Runeterra rounds (Standard) | 52.8% | 44.6% | 12.9 ± 2.7 | 0.0% | 8.0% | 95.5% | 76.2% | 3.1% | 18.5 | 39.7 | 32.0 | 6.6 (22.3%) | 6.3 | 16.0 | 31.0% | 0 |
| Vesper's Ledger mirror, rounds with summoning sickness | 54.4% | 47.4% | 14.4 ± 3.0 | 0.0% | 1.4% | 96.1% | 74.3% | 2.1% | 19.4 | 42.0 | 34.9 | 7.5 (21.7%) | 8.6 | 17.6 | 33.3% | 0 |
| Vesper's Ledger mirror, rounds, everyone attacks once a round | 53.2% | 45.0% | 9.9 ± 1.8 | 0.0% | 45.2% | 94.6% | 67.5% | 3.6% | 12.4 | 30.0 | 22.2 | 4.1 (25.1%) | 2.9 | 12.3 | 17.0% | 0 |
| Vesper's Ledger mirror, MTG turns (A B A B), draw skip | 47.2% | 49.8% | 21.6 ± 4.1 | 18.2% | 0.0% | 94.7% | 65.8% | 2.3% | 13.7 | 32.6 | 25.9 | 5.6 (12.0%) | 5.0 | 8.0 | 15.8% | 0 |
| Vesper's Ledger mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 50.8% | 35.8% | 20.7 ± 4.1 | 11.4% | 0.0% | 93.8% | 67.0% | 2.3% | 13.2 | 31.0 | 25.1 | 5.4 (11.9%) | 4.6 | 7.4 | 12.7% | 0 |
| Vesper's Ledger mirror, A B | B A + attack token (2026-10-10 morning) | 49.6% | 40.2% | 23.0 ± 3.9 | 23.2% | 0.0% | 92.7% | 69.1% | 1.8% | 13.4 | 35.3 | 26.0 | 6.0 (19.6%) | 6.2 | 8.6 | 16.2% | 0 |
| Sparkwrench Scrappers mirror, Runeterra rounds (Standard) | 49.6% | 51.0% | 13.5 ± 2.4 | 0.0% | 4.0% | 98.1% | 91.9% | 21.0% | 25.8 | 0.0 | 10.5 | 12.8 (28.9%) | 11.5 | 23.7 | 49.8% | 0 |
| Sparkwrench Scrappers mirror, rounds with summoning sickness | 50.0% | 53.4% | 15.0 ± 2.8 | 0.4% | 0.8% | 97.5% | 90.8% | 21.3% | 28.2 | 0.0 | 13.0 | 15.0 (27.6%) | 13.8 | 26.6 | 54.5% | 0 |
| Sparkwrench Scrappers mirror, rounds, everyone attacks once a round | 48.4% | 49.8% | 10.6 ± 1.7 | 0.0% | 28.4% | 98.1% | 91.4% | 22.3% | 19.0 | 0.0 | 5.6 | 7.9 (30.5%) | 6.4 | 18.3 | 27.8% | 0 |
| Sparkwrench Scrappers mirror, MTG turns (A B A B), draw skip | 54.2% | 75.6% | 19.6 ± 3.6 | 5.2% | 0.0% | 96.7% | 86.1% | 23.1% | 15.3 | 0.0 | 5.3 | 7.9 (15.5%) | 5.3 | 7.0 | 20.0% | 0 |
| Sparkwrench Scrappers mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 53.8% | 55.2% | 20.2 ± 3.5 | 6.8% | 0.0% | 97.1% | 88.1% | 23.4% | 17.0 | 0.0 | 4.9 | 8.9 (16.2%) | 5.7 | 6.5 | 27.0% | 0 |
| Sparkwrench Scrappers mirror, A B | B A + attack token (2026-10-10 morning) | 52.8% | 50.6% | 21.4 ± 3.0 | 8.4% | 0.0% | 95.9% | 85.1% | 22.0% | 16.4 | 0.0 | 7.1 | 9.6 (19.2%) | 5.8 | 5.6 | 30.7% | 0 |
| Auditor's Arsenal mirror, Runeterra rounds (Standard) | 51.4% | 52.4% | 10.2 ± 1.1 | 0.0% | 26.4% | 78.7% | 45.8% | 5.7% | 4.4 | 1.5 | 2.8 | 0.2 (10.0%) | 17.5 | 14.0 | 7.1% | 0 |
| Auditor's Arsenal mirror, rounds with summoning sickness | 51.2% | 52.6% | 11.0 ± 1.3 | 0.0% | 10.8% | 81.9% | 48.2% | 4.3% | 4.3 | 1.4 | 4.1 | 0.4 (9.7%) | 21.7 | 14.2 | 13.6% | 0 |
| Auditor's Arsenal mirror, rounds, everyone attacks once a round | 54.4% | 51.8% | 8.2 ± 1.0 | 0.0% | 90.2% | 75.3% | 51.0% | 10.9% | 3.7 | 1.2 | 1.5 | 0.0 (62.5%) | 8.3 | 11.2 | 0.0% | 0 |
| Auditor's Arsenal mirror, MTG turns (A B A B), draw skip | 53.8% | 71.2% | 16.2 ± 1.9 | 0.0% | 0.0% | 69.1% | 38.6% | 7.8% | 2.5 | 0.8 | 1.7 | 0.0 (20.0%) | 6.7 | 4.2 | 0.0% | 0 |
| Auditor's Arsenal mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 52.6% | 48.0% | 15.9 ± 1.8 | 0.0% | 0.0% | 69.5% | 36.2% | 7.8% | 2.6 | 0.8 | 1.5 | 0.0 (75.0%) | 6.7 | 4.2 | 0.0% | 0 |
| Auditor's Arsenal mirror, A B | B A + attack token (2026-10-10 morning) | 48.8% | 47.4% | 18.7 ± 1.9 | 0.2% | 0.0% | 64.4% | 27.7% | 7.0% | 2.6 | 0.6 | 2.8 | 0.1 (54.4%) | 10.5 | 3.8 | 3.5% | 0 |

## Tavern Dwellers (GAME_DESIGN §9)

*What do Tavern Dwellers (passives and Powers, the universal Gold sink) change? Each mirror with and without Tavern Dwellers. Watch Wasted (mana lost to the Gold cap), Gold spent and game length.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, with Tavern Dwellers | 51.6% | 54.2% | 9.0 ± 1.1 | 0.0% | 72.2% | 93.9% | 76.2% | 9.8% | 28.2 | 0.0 | 4.3 | 3.2 (21.4%) | 0.0 | 8.0 | 14.1% | 0 |
| Goober Mob mirror, no Tavern Dwellers | 54.2% | 51.6% | 10.9 ± 2.3 | 0.0% | 28.6% | 94.8% | 82.1% | 12.4% | 36.5 | 0.0 | 7.3 | 0.0 | 0.0 | 18.9 | 69.4% | 0 |
| Jungle Stampede mirror, with Tavern Dwellers | 51.6% | 47.0% | 13.3 ± 3.0 | 0.2% | 9.2% | 97.0% | 91.9% | 7.4% | 25.3 | 2.7 | 16.6 | 10.6 (54.7%) | 0.7 | 15.4 | 32.6% | 0 |
| Jungle Stampede mirror, no Tavern Dwellers | 51.2% | 43.8% | 11.8 ± 2.4 | 0.0% | 16.2% | 80.0% | 79.4% | 0.8% | 15.8 | 0.2 | 11.2 | 0.0 | 1.9 | 4.9 | 38.3% | 0 |
| Zoo Patrol mirror, with Tavern Dwellers | 49.8% | 47.2% | 11.9 ± 1.8 | 0.0% | 6.4% | 69.6% | 65.2% | 29.7% | 14.2 | 20.4 | 13.2 | 8.7 (34.2%) | 0.2 | 12.6 | 23.2% | 0 |
| Zoo Patrol mirror, no Tavern Dwellers | 47.6% | 47.4% | 12.3 ± 2.3 | 0.0% | 6.2% | 87.1% | 83.6% | 39.1% | 16.3 | 6.5 | 12.6 | 0.0 | 0.4 | 15.0 | 50.3% | 0 |
| Vesper's Ledger mirror, with Tavern Dwellers | 52.8% | 44.6% | 12.9 ± 2.7 | 0.0% | 8.0% | 95.5% | 76.2% | 3.1% | 18.5 | 39.7 | 32.0 | 6.6 (22.3%) | 6.3 | 16.0 | 31.0% | 0 |
| Vesper's Ledger mirror, no Tavern Dwellers | 51.8% | 44.4% | 14.4 ± 3.2 | 0.2% | 3.8% | 94.8% | 75.9% | 3.0% | 21.5 | 41.0 | 23.9 | 0.0 | 8.4 | 23.1 | 53.6% | 0 |
| Sparkwrench Scrappers mirror, with Tavern Dwellers | 49.6% | 51.0% | 13.5 ± 2.4 | 0.0% | 4.0% | 98.1% | 91.9% | 21.0% | 25.8 | 0.0 | 10.5 | 12.8 (28.9%) | 11.5 | 23.7 | 49.8% | 0 |
| Sparkwrench Scrappers mirror, no Tavern Dwellers | 52.6% | 51.6% | 12.9 ± 2.2 | 0.0% | 4.4% | 97.6% | 89.4% | 20.0% | 24.5 | 0.0 | 9.2 | 0.0 | 15.4 | 38.4 | 58.8% | 0 |
| Auditor's Arsenal mirror, with Tavern Dwellers | 51.4% | 52.4% | 10.2 ± 1.1 | 0.0% | 26.4% | 78.7% | 45.8% | 5.7% | 4.4 | 1.5 | 2.8 | 0.2 (10.0%) | 17.5 | 14.0 | 7.1% | 0 |
| Auditor's Arsenal mirror, no Tavern Dwellers | 49.2% | 51.8% | 10.7 ± 1.3 | 0.0% | 15.6% | 75.9% | 45.9% | 5.0% | 4.4 | 1.7 | 2.8 | 0.0 | 14.7 | 14.9 | 5.0% | 0 |

## Gold cap (GAME_DESIGN §5.2)

*Does the cap matter now that Tavern Dweller Powers and Equip spend Gold?*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Zoo Patrol mirror, Gold cap 3 | 49.8% | 47.2% | 11.9 ± 1.8 | 0.0% | 6.4% | 69.6% | 65.2% | 29.7% | 14.2 | 20.4 | 13.2 | 8.7 (34.2%) | 0.2 | 12.6 | 23.2% | 0 |
| Zoo Patrol mirror, Gold cap 5 | 50.8% | 46.2% | 11.8 ± 1.8 | 0.0% | 6.6% | 69.6% | 64.4% | 29.6% | 14.1 | 20.4 | 13.6 | 8.7 (34.2%) | 0.1 | 12.4 | 17.1% | 0 |
| Zoo Patrol mirror, Gold cap 8 | 50.4% | 45.4% | 11.8 ± 1.8 | 0.0% | 7.0% | 69.7% | 64.9% | 29.7% | 14.2 | 20.2 | 13.8 | 8.6 (34.0%) | 0.0 | 12.3 | 12.0% | 0 |
| Vesper's Ledger mirror, Gold cap 3 | 52.8% | 44.6% | 12.9 ± 2.7 | 0.0% | 8.0% | 95.5% | 76.2% | 3.1% | 18.5 | 39.7 | 32.0 | 6.6 (22.3%) | 6.3 | 16.0 | 31.0% | 0 |
| Vesper's Ledger mirror, Gold cap 5 | 52.8% | 45.0% | 12.9 ± 2.5 | 0.0% | 7.4% | 95.5% | 79.8% | 2.9% | 18.7 | 38.9 | 34.8 | 6.0 (19.7%) | 6.5 | 15.5 | 23.5% | 0 |
| Vesper's Ledger mirror, Gold cap 8 | 50.8% | 44.2% | 12.9 ± 2.6 | 0.0% | 7.6% | 95.4% | 74.4% | 2.8% | 19.3 | 39.2 | 36.4 | 5.5 (19.2%) | 6.5 | 15.1 | 20.8% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 3 | 49.6% | 51.0% | 13.5 ± 2.4 | 0.0% | 4.0% | 98.1% | 91.9% | 21.0% | 25.8 | 0.0 | 10.5 | 12.8 (28.9%) | 11.5 | 23.7 | 49.8% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 5 | 49.8% | 51.6% | 13.5 ± 2.4 | 0.0% | 4.0% | 97.9% | 91.8% | 20.9% | 25.5 | 0.0 | 12.4 | 12.6 (29.1%) | 11.5 | 23.5 | 38.6% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 8 | 49.6% | 52.2% | 13.4 ± 2.3 | 0.0% | 4.0% | 97.9% | 91.5% | 20.9% | 25.3 | 0.0 | 13.5 | 12.4 (29.0%) | 11.5 | 23.2 | 27.2% | 0 |
| Auditor's Arsenal mirror, Gold cap 3 | 51.4% | 52.4% | 10.2 ± 1.1 | 0.0% | 26.4% | 78.7% | 45.8% | 5.7% | 4.4 | 1.5 | 2.8 | 0.2 (10.0%) | 17.5 | 14.0 | 7.1% | 0 |
| Auditor's Arsenal mirror, Gold cap 5 | 51.2% | 52.6% | 10.2 ± 1.1 | 0.0% | 26.6% | 78.3% | 44.8% | 5.7% | 4.4 | 1.5 | 3.0 | 0.2 (13.2%) | 17.6 | 13.9 | 3.5% | 0 |
| Auditor's Arsenal mirror, Gold cap 8 | 50.8% | 52.6% | 10.2 ± 1.1 | 0.0% | 26.6% | 78.3% | 45.0% | 5.7% | 4.4 | 1.5 | 3.2 | 0.2 (12.0%) | 17.5 | 13.9 | 1.5% | 0 |
