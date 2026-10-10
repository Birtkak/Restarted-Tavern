# Simulation Report

Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

500 games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time 55s.

Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) ± standard deviation · **Long** / **Short** = share of games over 25 / under 10 turns · **Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · **Chip→death** = share of *chip damage* (damage a creature carried into a later turn) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · **Wounded** = share of creatures carrying damage at the start of a turn · **Deaths** / **Heal** / **Gold spent** are per game · **Powers** = Tavern Dweller Powers used per game (in brackets: share used on an opponent's turn) · **Abil.** = other activated abilities per game (Equip, Tap abilities...) · **Off-turn** = spells cast and abilities/Powers used on an opponent's turn, per game · **Wasted** = share of unspent mana lost to the Gold cap.

## Round robin (current rules, Greedy bots)

*How long are games, and does damage on creatures decide anything? Every deck against every deck.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror | 51.4% | 53.2% | 8.9 ± 1.1 | 0.0% | 73.4% | 94.1% | 76.2% | 9.3% | 28.1 | 0.0 | 4.3 | 3.2 (21.3%) | 0.0 | 7.9 | 13.4% | 0 |
| Goober Mob vs Jungle Stampede | 59.6% | 47.0% | 8.9 ± 1.1 | 0.0% | 71.8% | 91.9% | 75.6% | 14.5% | 18.5 | 1.3 | 5.5 | 5.2 (47.9%) | 0.3 | 9.8 | 5.6% | 0 |
| Goober Mob vs Zoo Patrol | 56.4% | 51.0% | 9.8 ± 1.4 | 0.0% | 41.8% | 87.2% | 63.9% | 14.5% | 22.5 | 4.2 | 6.0 | 3.6 (23.5%) | 0.0 | 8.6 | 16.5% | 0 |
| Goober Mob vs Vesper's Ledger | 46.6% | 48.0% | 9.4 ± 1.3 | 0.0% | 55.8% | 91.4% | 55.2% | 9.9% | 18.0 | 13.4 | 15.6 | 3.9 (17.6%) | 2.0 | 8.8 | 19.2% | 0 |
| Goober Mob vs Sparkwrench Scrappers | 53.6% | 49.0% | 9.5 ± 1.3 | 0.0% | 52.0% | 92.1% | 72.8% | 13.1% | 23.9 | 0.0 | 5.1 | 3.4 (23.7%) | 2.9 | 9.8 | 19.0% | 0 |
| Goober Mob vs Auditor's Arsenal | 67.4% | 50.8% | 9.0 ± 1.1 | 0.0% | 69.6% | 80.1% | 53.0% | 11.0% | 13.0 | 0.8 | 3.8 | 1.5 (42.2%) | 3.0 | 10.1 | 9.1% | 0 |
| Jungle Stampede mirror | 51.8% | 47.6% | 13.2 ± 3.0 | 0.4% | 9.6% | 96.9% | 91.3% | 7.2% | 25.1 | 2.7 | 16.5 | 10.5 (54.7%) | 0.7 | 15.4 | 32.6% | 0 |
| Jungle Stampede vs Zoo Patrol | 50.2% | 46.8% | 11.3 ± 2.0 | 0.0% | 18.6% | 90.8% | 88.7% | 24.3% | 16.5 | 4.6 | 11.9 | 5.6 (47.8%) | 0.3 | 10.6 | 23.3% | 0 |
| Jungle Stampede vs Vesper's Ledger | 45.0% | 51.2% | 10.9 ± 1.7 | 0.0% | 22.0% | 91.8% | 72.2% | 9.2% | 14.8 | 18.3 | 17.6 | 7.5 (39.5%) | 1.5 | 12.6 | 5.6% | 0 |
| Jungle Stampede vs Sparkwrench Scrappers | 43.6% | 43.8% | 11.7 ± 2.4 | 0.0% | 19.4% | 95.9% | 89.6% | 16.7% | 21.4 | 1.6 | 11.2 | 8.8 (44.7%) | 3.9 | 13.2 | 24.4% | 0 |
| Jungle Stampede vs Auditor's Arsenal | 50.8% | 50.2% | 10.5 ± 1.4 | 0.0% | 23.2% | 83.3% | 59.9% | 12.4% | 10.7 | 2.4 | 7.2 | 4.6 (54.4%) | 4.7 | 13.3 | 6.9% | 0 |
| Zoo Patrol mirror | 47.8% | 45.2% | 11.7 ± 1.8 | 0.0% | 7.6% | 66.5% | 61.8% | 32.8% | 13.1 | 21.0 | 12.6 | 8.9 (35.7%) | 0.3 | 12.8 | 19.9% | 0 |
| Zoo Patrol vs Vesper's Ledger | 54.4% | 54.2% | 10.8 ± 1.7 | 0.0% | 20.0% | 86.0% | 74.6% | 15.5% | 13.2 | 15.8 | 15.8 | 4.0 (23.2%) | 1.5 | 9.1 | 19.1% | 0 |
| Zoo Patrol vs Sparkwrench Scrappers | 63.6% | 48.6% | 11.3 ± 2.0 | 0.0% | 16.8% | 82.7% | 72.8% | 28.7% | 17.4 | 7.5 | 9.5 | 6.3 (27.8%) | 3.8 | 11.6 | 23.0% | 0 |
| Zoo Patrol vs Auditor's Arsenal | 31.2% | 43.0% | 10.4 ± 1.2 | 0.0% | 20.8% | 71.3% | 52.2% | 19.3% | 8.4 | 6.5 | 6.2 | 2.0 (47.2%) | 5.1 | 11.9 | 6.8% | 0 |
| Vesper's Ledger mirror | 50.4% | 49.8% | 11.7 ± 2.5 | 0.0% | 16.0% | 92.4% | 71.2% | 4.2% | 13.7 | 33.2 | 25.1 | 4.7 (25.1%) | 4.8 | 13.0 | 22.1% | 0 |
| Vesper's Ledger vs Sparkwrench Scrappers | 36.4% | 49.0% | 10.0 ± 1.4 | 0.0% | 39.6% | 89.9% | 69.5% | 10.7% | 12.8 | 11.8 | 13.0 | 3.3 (17.8%) | 4.8 | 9.7 | 13.1% | 0 |
| Vesper's Ledger vs Auditor's Arsenal | 42.4% | 47.0% | 10.7 ± 1.4 | 0.0% | 17.8% | 80.0% | 46.5% | 4.0% | 9.2 | 14.8 | 13.1 | 2.2 (38.4%) | 7.7 | 14.3 | 14.5% | 0 |
| Sparkwrench Scrappers mirror | 49.4% | 49.6% | 10.8 ± 2.0 | 0.0% | 24.2% | 93.6% | 70.8% | 23.3% | 23.4 | 0.0 | 8.0 | 5.0 (24.7%) | 8.0 | 13.0 | 17.0% | 0 |
| Sparkwrench Scrappers vs Auditor's Arsenal | 59.8% | 50.0% | 9.6 ± 1.2 | 0.0% | 45.0% | 76.6% | 52.6% | 13.7% | 7.9 | 1.4 | 4.1 | 1.3 (36.9%) | 6.5 | 11.9 | 3.9% | 0 |
| Auditor's Arsenal mirror | 46.0% | 43.4% | 10.4 ± 1.2 | 0.0% | 20.8% | 58.8% | 36.7% | 5.5% | 4.4 | 2.8 | 4.2 | 0.3 (20.2%) | 9.5 | 15.8 | 14.0% | 0 |

## Bot play styles

*Do the results hold with a defensive player? Control blocks freely, keeps blockers home and saves Gold. In "Greedy vs Control", A win% is the Greedy side.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, Control vs Control | 50.6% | 49.2% | 10.5 ± 1.4 | 0.0% | 21.4% | 95.0% | 86.7% | 14.3% | 39.6 | 0.0 | 5.4 | 5.1 (21.8%) | 0.0 | 10.0 | 32.8% | 0 |
| Jungle Stampede mirror, Control vs Control | 46.6% | 50.8% | 17.5 ± 4.7 | 7.6% | 0.6% | 96.7% | 93.5% | 17.8% | 33.5 | 4.9 | 27.4 | 17.3 (49.1%) | 1.0 | 22.1 | 48.2% | 0 |
| Zoo Patrol mirror, Control vs Control | 49.2% | 47.4% | 15.9 ± 3.3 | 0.4% | 0.6% | 66.5% | 78.5% | 41.5% | 20.0 | 37.1 | 23.8 | 15.0 (37.6%) | 0.7 | 19.5 | 45.2% | 0 |
| Vesper's Ledger mirror, Control vs Control | 47.4% | 48.8% | 13.0 ± 2.7 | 0.0% | 5.4% | 92.7% | 80.1% | 4.7% | 14.5 | 36.7 | 28.2 | 6.6 (25.5%) | 6.8 | 15.0 | 16.3% | 0 |
| Sparkwrench Scrappers mirror, Control vs Control | 48.0% | 51.4% | 11.8 ± 2.4 | 0.0% | 13.0% | 92.7% | 78.0% | 23.2% | 31.4 | 0.0 | 8.8 | 5.3 (24.5%) | 9.3 | 15.9 | 27.6% | 0 |
| Auditor's Arsenal mirror, Control vs Control | 48.4% | 46.6% | 14.3 ± 2.4 | 0.0% | 0.2% | 62.1% | 59.4% | 15.1% | 13.9 | 22.2 | 12.5 | 1.5 (21.8%) | 30.6 | 27.7 | 38.0% | 0 |
| Goober Mob mirror, Greedy vs Control | 67.2% | 54.2% | 9.7 ± 1.3 | 0.0% | 46.4% | 94.6% | 81.7% | 12.2% | 33.4 | 0.0 | 4.6 | 4.2 (21.4%) | 0.0 | 9.0 | 19.7% | 0 |
| Jungle Stampede mirror, Greedy vs Control | 60.4% | 49.0% | 15.3 ± 4.3 | 2.6% | 5.2% | 97.0% | 93.7% | 14.1% | 29.4 | 3.8 | 22.1 | 13.4 (50.9%) | 0.9 | 18.9 | 44.2% | 0 |
| Zoo Patrol mirror, Greedy vs Control | 57.4% | 49.2% | 13.7 ± 2.4 | 0.0% | 2.2% | 67.2% | 72.5% | 39.3% | 16.4 | 28.0 | 17.0 | 11.7 (37.1%) | 0.5 | 15.8 | 34.4% | 0 |
| Vesper's Ledger mirror, Greedy vs Control | 56.8% | 49.0% | 12.3 ± 2.5 | 0.0% | 10.2% | 92.9% | 78.9% | 4.5% | 13.8 | 34.7 | 26.3 | 5.8 (24.6%) | 5.4 | 13.8 | 18.6% | 0 |
| Sparkwrench Scrappers mirror, Greedy vs Control | 48.2% | 48.4% | 11.4 ± 2.2 | 0.2% | 18.8% | 93.0% | 75.4% | 23.6% | 26.6 | 0.0 | 8.5 | 5.4 (23.9%) | 8.6 | 14.6 | 24.8% | 0 |
| Auditor's Arsenal mirror, Greedy vs Control | 59.0% | 46.8% | 12.0 ± 1.5 | 0.0% | 2.6% | 61.0% | 51.5% | 16.1% | 8.4 | 10.0 | 7.1 | 0.7 (23.5%) | 17.9 | 21.0 | 26.0% | 0 |

## Game length lever: starting life

*Can starting life even out game length between fast and slow decks? Compare with the 30-life mirrors in the round robin.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, 25 life | 50.2% | 52.0% | 8.1 ± 1.0 | 0.0% | 91.0% | 93.5% | 73.5% | 8.9% | 23.0 | 0.0 | 3.5 | 2.5 (21.5%) | 0.0 | 7.2 | 5.4% | 0 |
| Jungle Stampede mirror, 25 life | 51.6% | 48.6% | 12.0 ± 2.9 | 0.2% | 18.2% | 96.7% | 90.3% | 7.2% | 22.4 | 2.5 | 14.0 | 8.8 (55.6%) | 0.6 | 13.7 | 26.3% | 0 |
| Zoo Patrol mirror, 25 life | 49.4% | 46.8% | 10.8 ± 1.6 | 0.0% | 20.4% | 67.0% | 60.4% | 32.0% | 11.3 | 17.4 | 10.8 | 7.4 (35.2%) | 0.2 | 11.5 | 13.9% | 0 |
| Vesper's Ledger mirror, 25 life | 51.0% | 47.6% | 10.8 ± 2.3 | 0.0% | 32.0% | 92.4% | 71.7% | 4.4% | 11.8 | 28.0 | 22.3 | 3.7 (24.6%) | 3.7 | 11.4 | 20.3% | 0 |
| Sparkwrench Scrappers mirror, 25 life | 49.6% | 46.6% | 10.4 ± 2.0 | 0.0% | 32.0% | 93.0% | 68.6% | 23.6% | 20.9 | 0.0 | 7.4 | 4.6 (24.1%) | 7.3 | 12.3 | 14.4% | 0 |
| Auditor's Arsenal mirror, 25 life | 46.6% | 43.6% | 9.9 ± 1.2 | 0.0% | 38.4% | 57.2% | 37.6% | 6.5% | 3.9 | 2.8 | 3.6 | 0.2 (22.8%) | 8.1 | 15.0 | 9.6% | 0 |
| Goober Mob mirror, 35 life | 49.6% | 51.0% | 9.7 ± 1.3 | 0.0% | 46.4% | 94.4% | 77.9% | 9.7% | 33.3 | 0.0 | 5.4 | 4.1 (21.6%) | 0.0 | 8.7 | 26.0% | 0 |
| Jungle Stampede mirror, 35 life | 52.6% | 48.8% | 14.6 ± 3.5 | 0.8% | 2.6% | 97.2% | 92.0% | 7.6% | 28.2 | 3.0 | 20.0 | 12.3 (54.3%) | 0.8 | 17.1 | 41.0% | 0 |
| Zoo Patrol mirror, 35 life | 50.0% | 46.2% | 12.6 ± 1.9 | 0.0% | 2.0% | 66.6% | 65.3% | 33.9% | 14.8 | 24.7 | 14.5 | 10.4 (36.4%) | 0.4 | 14.2 | 27.1% | 0 |
| Vesper's Ledger mirror, 35 life | 48.6% | 50.0% | 12.4 ± 2.4 | 0.0% | 6.8% | 92.7% | 72.8% | 3.6% | 14.5 | 37.1 | 26.8 | 5.7 (25.3%) | 5.7 | 14.2 | 17.5% | 0 |
| Sparkwrench Scrappers mirror, 35 life | 49.8% | 46.8% | 11.3 ± 2.1 | 0.0% | 17.4% | 93.7% | 69.3% | 23.8% | 26.0 | 0.0 | 8.8 | 5.4 (25.7%) | 8.8 | 13.7 | 20.6% | 0 |
| Auditor's Arsenal mirror, 35 life | 48.8% | 49.0% | 10.9 ± 1.2 | 0.0% | 8.2% | 59.7% | 38.9% | 5.2% | 4.9 | 3.1 | 5.0 | 0.4 (16.9%) | 11.2 | 16.5 | 20.1% | 0 |

## Permanent damage (GAME_DESIGN §7.3)

*What changes when damage wears off at end of turn like in MTG? Compare with the round-robin mirrors (permanent damage).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, damage wears off (MTG) | 52.8% | 52.6% | 8.6 ± 1.1 | 0.0% | 79.4% | 91.5% | 0.0% | 0.0% | 26.3 | 0.0 | 4.3 | 2.9 (23.3%) | 0.0 | 7.5 | 9.1% | 0 |
| Jungle Stampede mirror, damage wears off (MTG) | 53.2% | 49.0% | 13.0 ± 2.9 | 0.2% | 8.6% | 96.6% | 0.0% | 0.0% | 24.7 | 2.6 | 15.9 | 10.4 (53.8%) | 0.8 | 14.9 | 30.8% | 0 |
| Zoo Patrol mirror, damage wears off (MTG) | 51.0% | 46.4% | 11.3 ± 1.9 | 0.0% | 14.4% | 66.3% | 0.0% | 0.0% | 12.0 | 13.1 | 11.7 | 6.6 (26.9%) | 0.4 | 10.4 | 19.6% | 0 |
| Vesper's Ledger mirror, damage wears off (MTG) | 49.8% | 50.8% | 11.8 ± 2.4 | 0.0% | 14.6% | 87.1% | 0.0% | 0.0% | 13.6 | 33.9 | 25.3 | 4.8 (25.9%) | 5.1 | 12.8 | 18.6% | 0 |
| Sparkwrench Scrappers mirror, damage wears off (MTG) | 49.8% | 48.4% | 10.6 ± 1.9 | 0.0% | 28.6% | 91.7% | 0.0% | 0.0% | 21.8 | 0.0 | 7.7 | 4.5 (25.0%) | 7.5 | 12.1 | 17.9% | 0 |
| Auditor's Arsenal mirror, damage wears off (MTG) | 44.6% | 44.0% | 10.4 ± 1.2 | 0.0% | 21.8% | 52.4% | 0.0% | 0.0% | 4.1 | 1.3 | 4.1 | 0.3 (18.8%) | 9.0 | 15.2 | 14.1% | 0 |

## Turn structure and going first (GAME_DESIGN §3, §6)

*First-player win% in mirrors (50% is fair). Standard = Legends of Runeterra rounds: everyone refills, untaps and draws when a round starts, players alternate actions, the round leader holds the attack token. Turns = rounds with Runeterra rounds (one round is everyone's turn), turns otherwise.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, Runeterra rounds (Standard) | 51.4% | 53.2% | 8.9 ± 1.1 | 0.0% | 73.4% | 94.1% | 76.2% | 9.3% | 28.1 | 0.0 | 4.3 | 3.2 (21.3%) | 0.0 | 7.9 | 13.4% | 0 |
| Goober Mob mirror, rounds, everyone attacks once a round | 50.8% | 51.0% | 7.0 ± 0.8 | 0.0% | 99.6% | 93.8% | 72.0% | 9.9% | 18.7 | 0.0 | 3.4 | 1.6 (24.2%) | 0.0 | 6.5 | 1.9% | 0 |
| Goober Mob mirror, MTG turns (A B A B), draw skip | 51.0% | 85.2% | 10.3 ± 1.1 | 0.0% | 35.6% | 92.8% | 61.3% | 0.3% | 7.3 | 0.0 | 3.4 | 1.5 (42.7%) | 0.0 | 2.0 | 0.8% | 0 |
| Goober Mob mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 48.4% | 36.6% | 9.8 ± 1.1 | 0.0% | 35.2% | 93.3% | 45.2% | 0.4% | 7.8 | 0.0 | 3.3 | 1.7 (45.1%) | 0.0 | 2.2 | 1.2% | 0 |
| Goober Mob mirror, A B | B A + attack token (2026-10-10 morning) | 49.2% | 46.2% | 12.2 ± 1.2 | 0.0% | 1.2% | 95.5% | 37.5% | 0.0% | 9.0 | 0.0 | 4.0 | 2.5 (53.1%) | 0.0 | 3.1 | 1.0% | 0 |
| Jungle Stampede mirror, Runeterra rounds (Standard) | 51.8% | 47.6% | 13.2 ± 3.0 | 0.4% | 9.6% | 96.9% | 91.3% | 7.2% | 25.1 | 2.7 | 16.5 | 10.5 (54.7%) | 0.7 | 15.4 | 32.6% | 0 |
| Jungle Stampede mirror, rounds, everyone attacks once a round | 51.8% | 50.0% | 9.2 ± 2.0 | 0.0% | 62.0% | 96.1% | 85.1% | 5.7% | 15.2 | 1.4 | 8.8 | 4.7 (61.9%) | 0.3 | 10.1 | 8.1% | 0 |
| Jungle Stampede mirror, MTG turns (A B A B), draw skip | 52.8% | 86.6% | 11.6 ± 1.7 | 0.0% | 16.4% | 94.4% | 76.9% | 5.6% | 4.6 | 0.5 | 5.7 | 2.3 (62.6%) | 0.0 | 2.4 | 5.1% | 0 |
| Jungle Stampede mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 49.4% | 43.2% | 11.4 ± 1.6 | 0.0% | 6.6% | 93.8% | 67.8% | 7.7% | 5.0 | 0.5 | 5.0 | 2.4 (67.4%) | 0.0 | 2.5 | 5.8% | 0 |
| Jungle Stampede mirror, A B | B A + attack token (2026-10-10 morning) | 45.6% | 52.6% | 15.8 ± 2.2 | 0.0% | 0.0% | 93.7% | 76.5% | 9.0% | 8.8 | 1.5 | 8.3 | 5.3 (61.1%) | 0.2 | 4.8 | 4.0% | 0 |
| Zoo Patrol mirror, Runeterra rounds (Standard) | 47.8% | 45.2% | 11.7 ± 1.8 | 0.0% | 7.6% | 66.5% | 61.8% | 32.8% | 13.1 | 21.0 | 12.6 | 8.9 (35.7%) | 0.3 | 12.8 | 19.9% | 0 |
| Zoo Patrol mirror, rounds, everyone attacks once a round | 50.8% | 45.4% | 9.1 ± 1.3 | 0.0% | 63.6% | 68.5% | 57.7% | 32.4% | 8.4 | 11.4 | 8.2 | 4.9 (34.0%) | 0.1 | 9.2 | 4.7% | 0 |
| Zoo Patrol mirror, MTG turns (A B A B), draw skip | 50.6% | 58.0% | 15.6 ± 1.9 | 0.0% | 0.0% | 65.8% | 51.9% | 26.2% | 5.7 | 8.7 | 7.2 | 4.2 (29.7%) | 0.0 | 4.3 | 2.8% | 0 |
| Zoo Patrol mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 50.8% | 34.2% | 14.8 ± 1.9 | 0.0% | 0.0% | 66.4% | 52.6% | 25.8% | 5.5 | 8.2 | 6.2 | 3.9 (28.7%) | 0.0 | 3.7 | 3.3% | 0 |
| Zoo Patrol mirror, A B | B A + attack token (2026-10-10 morning) | 48.4% | 51.0% | 17.9 ± 1.9 | 0.2% | 0.0% | 66.3% | 54.2% | 25.5% | 7.3 | 11.9 | 8.4 | 5.5 (37.5%) | 0.0 | 5.7 | 4.9% | 0 |
| Vesper's Ledger mirror, Runeterra rounds (Standard) | 50.4% | 49.8% | 11.7 ± 2.5 | 0.0% | 16.0% | 92.4% | 71.2% | 4.2% | 13.7 | 33.2 | 25.1 | 4.7 (25.1%) | 4.8 | 13.0 | 22.1% | 0 |
| Vesper's Ledger mirror, rounds, everyone attacks once a round | 49.0% | 47.2% | 8.8 ± 1.4 | 0.0% | 73.6% | 90.8% | 61.6% | 4.4% | 9.4 | 22.4 | 17.3 | 2.8 (30.8%) | 1.8 | 9.9 | 7.9% | 0 |
| Vesper's Ledger mirror, MTG turns (A B A B), draw skip | 51.0% | 60.8% | 14.0 ± 2.1 | 0.0% | 0.2% | 85.7% | 58.6% | 1.3% | 4.6 | 20.1 | 11.2 | 1.8 (13.9%) | 1.1 | 2.8 | 3.7% | 0 |
| Vesper's Ledger mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 45.0% | 34.0% | 13.5 ± 2.1 | 0.0% | 0.2% | 86.0% | 56.4% | 1.7% | 4.6 | 20.4 | 10.7 | 1.7 (17.2%) | 1.1 | 2.6 | 3.3% | 0 |
| Vesper's Ledger mirror, A B | B A + attack token (2026-10-10 morning) | 46.8% | 44.2% | 15.8 ± 2.1 | 0.0% | 0.0% | 80.3% | 56.9% | 0.5% | 4.2 | 19.7 | 12.0 | 2.4 (26.4%) | 1.9 | 4.0 | 3.4% | 0 |
| Sparkwrench Scrappers mirror, Runeterra rounds (Standard) | 49.4% | 49.6% | 10.8 ± 2.0 | 0.0% | 24.2% | 93.6% | 70.8% | 23.3% | 23.4 | 0.0 | 8.0 | 5.0 (24.7%) | 8.0 | 13.0 | 17.0% | 0 |
| Sparkwrench Scrappers mirror, rounds, everyone attacks once a round | 46.8% | 44.2% | 9.3 ± 1.5 | 0.0% | 57.8% | 93.8% | 74.0% | 21.8% | 15.6 | 0.0 | 6.3 | 3.8 (23.0%) | 5.2 | 11.2 | 5.3% | 0 |
| Sparkwrench Scrappers mirror, MTG turns (A B A B), draw skip | 51.2% | 78.6% | 13.1 ± 1.9 | 0.0% | 1.6% | 90.6% | 63.1% | 15.3% | 5.2 | 0.0 | 4.8 | 1.3 (9.0%) | 2.4 | 3.2 | 0.7% | 0 |
| Sparkwrench Scrappers mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 50.2% | 37.2% | 12.5 ± 1.9 | 0.0% | 1.0% | 90.7% | 64.8% | 17.2% | 5.1 | 0.0 | 3.8 | 1.1 (11.3%) | 2.2 | 2.8 | 1.0% | 0 |
| Sparkwrench Scrappers mirror, A B | B A + attack token (2026-10-10 morning) | 51.8% | 52.0% | 14.8 ± 2.0 | 0.0% | 0.0% | 86.4% | 51.5% | 17.2% | 5.1 | 0.0 | 5.1 | 1.5 (14.1%) | 3.1 | 2.9 | 0.9% | 0 |
| Auditor's Arsenal mirror, Runeterra rounds (Standard) | 46.0% | 43.4% | 10.4 ± 1.2 | 0.0% | 20.8% | 58.8% | 36.7% | 5.5% | 4.4 | 2.8 | 4.2 | 0.3 (20.2%) | 9.5 | 15.8 | 14.0% | 0 |
| Auditor's Arsenal mirror, rounds, everyone attacks once a round | 45.2% | 41.4% | 8.4 ± 1.0 | 0.0% | 87.4% | 61.7% | 46.2% | 11.0% | 3.9 | 2.2 | 2.3 | 0.0 (58.3%) | 4.7 | 11.9 | 0.2% | 0 |
| Auditor's Arsenal mirror, MTG turns (A B A B), draw skip | 51.0% | 70.0% | 13.7 ± 1.6 | 0.0% | 0.0% | 58.1% | 38.7% | 8.9% | 1.7 | 0.4 | 1.9 | 0.0 (50.0%) | 2.5 | 2.8 | 0.2% | 0 |
| Auditor's Arsenal mirror, MTG turns, 2nd player +1 mana on their first 3 turns | 47.2% | 28.2% | 12.8 ± 1.5 | 0.0% | 0.0% | 53.9% | 40.0% | 8.6% | 1.5 | 0.4 | 1.7 | 0.0 (33.3%) | 2.5 | 2.1 | 0.3% | 0 |
| Auditor's Arsenal mirror, A B | B A + attack token (2026-10-10 morning) | 46.6% | 42.0% | 16.2 ± 1.8 | 0.0% | 0.0% | 53.9% | 31.2% | 6.3% | 1.5 | 0.3 | 2.6 | 0.0 (43.8%) | 4.1 | 4.2 | 1.1% | 0 |

## Tavern Dwellers (GAME_DESIGN §9)

*What do Tavern Dwellers (passives and Powers, the universal Gold sink) change? Each mirror with and without Tavern Dwellers. Watch Wasted (mana lost to the Gold cap), Gold spent and game length.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, with Tavern Dwellers | 51.4% | 53.2% | 8.9 ± 1.1 | 0.0% | 73.4% | 94.1% | 76.2% | 9.3% | 28.1 | 0.0 | 4.3 | 3.2 (21.3%) | 0.0 | 7.9 | 13.4% | 0 |
| Goober Mob mirror, no Tavern Dwellers | 56.0% | 52.6% | 10.9 ± 2.2 | 0.0% | 27.6% | 94.9% | 83.8% | 11.8% | 37.0 | 0.0 | 7.2 | 0.0 | 0.0 | 18.9 | 69.2% | 0 |
| Jungle Stampede mirror, with Tavern Dwellers | 51.8% | 47.6% | 13.2 ± 3.0 | 0.4% | 9.6% | 96.9% | 91.3% | 7.2% | 25.1 | 2.7 | 16.5 | 10.5 (54.7%) | 0.7 | 15.4 | 32.6% | 0 |
| Jungle Stampede mirror, no Tavern Dwellers | 50.4% | 43.8% | 11.7 ± 2.4 | 0.0% | 16.6% | 80.1% | 80.0% | 0.8% | 15.7 | 0.2 | 11.2 | 0.0 | 1.9 | 5.0 | 36.9% | 0 |
| Zoo Patrol mirror, with Tavern Dwellers | 47.8% | 45.2% | 11.7 ± 1.8 | 0.0% | 7.6% | 66.5% | 61.8% | 32.8% | 13.1 | 21.0 | 12.6 | 8.9 (35.7%) | 0.3 | 12.8 | 19.9% | 0 |
| Zoo Patrol mirror, no Tavern Dwellers | 46.4% | 47.0% | 12.0 ± 2.2 | 0.0% | 8.2% | 84.4% | 80.9% | 40.8% | 15.3 | 7.3 | 12.2 | 0.0 | 0.4 | 14.2 | 47.1% | 0 |
| Vesper's Ledger mirror, with Tavern Dwellers | 50.4% | 49.8% | 11.7 ± 2.5 | 0.0% | 16.0% | 92.4% | 71.2% | 4.2% | 13.7 | 33.2 | 25.1 | 4.7 (25.1%) | 4.8 | 13.0 | 22.1% | 0 |
| Vesper's Ledger mirror, no Tavern Dwellers | 49.2% | 49.4% | 12.8 ± 2.8 | 0.0% | 9.6% | 92.1% | 71.4% | 4.6% | 16.2 | 35.0 | 18.7 | 0.0 | 6.7 | 20.4 | 37.7% | 0 |
| Sparkwrench Scrappers mirror, with Tavern Dwellers | 49.4% | 49.6% | 10.8 ± 2.0 | 0.0% | 24.2% | 93.6% | 70.8% | 23.3% | 23.4 | 0.0 | 8.0 | 5.0 (24.7%) | 8.0 | 13.0 | 17.0% | 0 |
| Sparkwrench Scrappers mirror, no Tavern Dwellers | 48.8% | 49.4% | 10.6 ± 1.8 | 0.0% | 27.4% | 92.5% | 66.5% | 25.0% | 23.7 | 0.0 | 5.7 | 0.0 | 8.5 | 17.6 | 31.3% | 0 |
| Auditor's Arsenal mirror, with Tavern Dwellers | 46.0% | 43.4% | 10.4 ± 1.2 | 0.0% | 20.8% | 58.8% | 36.7% | 5.5% | 4.4 | 2.8 | 4.2 | 0.3 (20.2%) | 9.5 | 15.8 | 14.0% | 0 |
| Auditor's Arsenal mirror, no Tavern Dwellers | 45.0% | 44.0% | 10.5 ± 1.3 | 0.0% | 20.8% | 58.1% | 36.7% | 5.1% | 4.3 | 2.9 | 4.0 | 0.0 | 8.3 | 15.5 | 18.2% | 0 |

## Gold cap (GAME_DESIGN §5.2)

*Does the cap matter now that Tavern Dweller Powers and Equip spend Gold?*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Zoo Patrol mirror, Gold cap 3 | 47.8% | 45.2% | 11.7 ± 1.8 | 0.0% | 7.6% | 66.5% | 61.8% | 32.8% | 13.1 | 21.0 | 12.6 | 8.9 (35.7%) | 0.3 | 12.8 | 19.9% | 0 |
| Zoo Patrol mirror, Gold cap 5 | 49.2% | 43.8% | 11.7 ± 1.8 | 0.0% | 8.2% | 66.6% | 61.5% | 32.7% | 13.1 | 20.9 | 13.1 | 8.9 (35.8%) | 0.1 | 12.6 | 16.6% | 0 |
| Zoo Patrol mirror, Gold cap 8 | 49.4% | 43.6% | 11.7 ± 1.8 | 0.0% | 8.6% | 66.7% | 61.9% | 32.9% | 13.1 | 20.9 | 13.4 | 8.8 (35.8%) | 0.0 | 12.5 | 12.0% | 0 |
| Vesper's Ledger mirror, Gold cap 3 | 50.4% | 49.8% | 11.7 ± 2.5 | 0.0% | 16.0% | 92.4% | 71.2% | 4.2% | 13.7 | 33.2 | 25.1 | 4.7 (25.1%) | 4.8 | 13.0 | 22.1% | 0 |
| Vesper's Ledger mirror, Gold cap 5 | 47.4% | 51.2% | 11.6 ± 2.3 | 0.0% | 17.2% | 92.4% | 71.2% | 4.0% | 14.0 | 32.3 | 27.4 | 4.1 (20.2%) | 4.9 | 12.2 | 14.4% | 0 |
| Vesper's Ledger mirror, Gold cap 8 | 49.6% | 50.6% | 11.7 ± 2.4 | 0.0% | 17.0% | 92.4% | 70.3% | 4.1% | 14.0 | 33.0 | 28.3 | 3.8 (19.3%) | 5.3 | 12.2 | 10.2% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 3 | 49.4% | 49.6% | 10.8 ± 2.0 | 0.0% | 24.2% | 93.6% | 70.8% | 23.3% | 23.4 | 0.0 | 8.0 | 5.0 (24.7%) | 8.0 | 13.0 | 17.0% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 5 | 49.4% | 49.6% | 10.8 ± 2.0 | 0.0% | 24.2% | 93.6% | 71.9% | 23.5% | 23.6 | 0.0 | 8.3 | 5.0 (24.7%) | 8.0 | 13.0 | 12.1% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 8 | 49.0% | 49.2% | 10.8 ± 2.0 | 0.0% | 24.2% | 93.6% | 71.8% | 23.6% | 23.6 | 0.0 | 8.5 | 5.0 (24.7%) | 8.1 | 13.0 | 6.9% | 0 |
| Auditor's Arsenal mirror, Gold cap 3 | 46.0% | 43.4% | 10.4 ± 1.2 | 0.0% | 20.8% | 58.8% | 36.7% | 5.5% | 4.4 | 2.8 | 4.2 | 0.3 (20.2%) | 9.5 | 15.8 | 14.0% | 0 |
| Auditor's Arsenal mirror, Gold cap 5 | 45.6% | 43.4% | 10.4 ± 1.2 | 0.0% | 20.8% | 58.9% | 36.4% | 5.4% | 4.4 | 2.7 | 4.4 | 0.3 (20.6%) | 9.6 | 15.8 | 7.6% | 0 |
| Auditor's Arsenal mirror, Gold cap 8 | 45.8% | 43.6% | 10.4 ± 1.2 | 0.0% | 20.8% | 58.7% | 35.8% | 5.4% | 4.4 | 2.7 | 4.6 | 0.3 (18.1%) | 9.6 | 15.8 | 2.5% | 0 |
