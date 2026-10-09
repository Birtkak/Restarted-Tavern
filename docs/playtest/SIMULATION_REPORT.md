# Simulation Report

Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

500 games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time 336s.

Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) ± standard deviation · **Long** / **Short** = share of games over 25 / under 10 turns · **Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · **Chip→death** = share of *chip damage* (damage a creature carried into a later turn) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · **Wounded** = share of creatures carrying damage at the start of a turn · **Deaths** / **Heal** / **Gold spent** are per game · **Powers** = Tavern Dweller Powers used per game (in brackets: share used on an opponent's turn) · **Abil.** = other activated abilities per game (Equip, Tap abilities...) · **Wasted** = share of unspent mana lost to the Gold cap.

## Round robin (current rules, Greedy bots)

*How long are games, and does damage on creatures decide anything? Every deck against every deck.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror | 53.0% | 77.6% | 13.0 ± 2.0 | 0.0% | 6.6% | 87.8% | 27.3% | 1.5% | 8.5 | 3.3 | 2.2 | 1.4 (9.1%) | 0.0 | 0.5% | 0 |
| Goober Mob vs Jungle Stampede | 23.2% | 59.8% | 14.6 ± 3.4 | 1.6% | 1.0% | 80.9% | 28.9% | 1.5% | 7.0 | 4.0 | 2.9 | 1.4 (12.4%) | 0.0 | 23.8% | 0 |
| Goober Mob vs Zoo Patrol | 73.4% | 70.8% | 14.7 ± 2.2 | 0.0% | 1.8% | 80.9% | 53.8% | 15.5% | 10.0 | 4.8 | 2.7 | 2.1 (5.1%) | 0.0 | 2.6% | 0 |
| Goober Mob vs Vesper's Ledger | 83.8% | 63.2% | 14.8 ± 1.9 | 0.0% | 1.2% | 78.8% | 25.0% | 1.7% | 8.5 | 8.3 | 8.2 | 2.6 (19.2%) | 0.0 | 2.9% | 0 |
| Goober Mob vs Sparkwrench Scrappers | 69.4% | 74.0% | 14.1 ± 2.2 | 0.0% | 2.4% | 88.8% | 62.4% | 8.1% | 11.2 | 1.2 | 2.0 | 0.4 (5.5%) | 0.0 | 4.3% | 0 |
| Goober Mob vs Auditor's Arsenal | 80.2% | 70.0% | 13.5 ± 2.6 | 0.0% | 10.6% | 80.6% | 41.9% | 4.4% | 5.5 | 1.3 | 1.7 | 1.0 (9.4%) | 1.9 | 0.5% | 0 |
| Jungle Stampede mirror | 51.2% | 69.0% | 25.4 ± 15.9 | 24.6% | 0.0% | 85.5% | 37.3% | 2.3% | 12.8 | 7.9 | 8.8 | 1.9 (40.5%) | 0.0 | 73.2% | 0 |
| Jungle Stampede vs Zoo Patrol | 52.0% | 73.4% | 17.2 ± 3.1 | 1.0% | 0.0% | 78.4% | 60.0% | 21.3% | 9.8 | 8.1 | 3.9 | 2.0 (13.5%) | 0.0 | 14.7% | 0 |
| Jungle Stampede vs Vesper's Ledger | 91.0% | 61.6% | 15.6 ± 2.4 | 0.4% | 0.0% | 70.2% | 35.5% | 3.3% | 6.6 | 9.6 | 6.6 | 0.7 (17.1%) | 0.0 | 7.5% | 0 |
| Jungle Stampede vs Sparkwrench Scrappers | 73.6% | 68.2% | 15.4 ± 2.9 | 0.2% | 0.0% | 82.7% | 60.1% | 10.1% | 8.9 | 2.4 | 2.7 | 0.2 (32.1%) | 0.0 | 14.3% | 0 |
| Jungle Stampede vs Auditor's Arsenal | 72.6% | 74.0% | 15.1 ± 2.3 | 0.0% | 0.0% | 77.0% | 41.8% | 6.1% | 4.6 | 2.3 | 2.4 | 0.1 (48.6%) | 2.7 | 1.2% | 0 |
| Zoo Patrol mirror | 52.0% | 65.0% | 20.2 ± 4.4 | 13.0% | 0.0% | 73.6% | 69.4% | 31.8% | 12.2 | 17.8 | 7.2 | 7.2 (22.4%) | 0.0 | 35.6% | 0 |
| Zoo Patrol vs Vesper's Ledger | 93.0% | 58.8% | 19.0 ± 3.0 | 2.8% | 0.0% | 79.3% | 65.7% | 17.3% | 13.0 | 12.6 | 9.9 | 4.1 (14.7%) | 0.0 | 27.5% | 0 |
| Zoo Patrol vs Sparkwrench Scrappers | 68.2% | 68.0% | 17.8 ± 3.3 | 2.0% | 1.0% | 80.7% | 59.9% | 23.8% | 14.0 | 7.5 | 3.5 | 3.1 (8.6%) | 0.0 | 30.7% | 0 |
| Zoo Patrol vs Auditor's Arsenal | 65.2% | 76.6% | 16.4 ± 2.2 | 0.4% | 0.0% | 71.3% | 45.1% | 25.0% | 6.9 | 6.7 | 3.0 | 2.2 (9.7%) | 3.8 | 3.5% | 0 |
| Vesper's Ledger mirror | 50.2% | 60.4% | 23.8 ± 5.5 | 34.6% | 0.0% | 78.9% | 47.4% | 13.0% | 19.2 | 26.4 | 23.8 | 6.6 (17.4%) | 0.0 | 80.2% | 0 |
| Vesper's Ledger vs Sparkwrench Scrappers | 11.0% | 51.2% | 17.4 ± 3.3 | 3.2% | 0.4% | 90.5% | 75.0% | 9.9% | 12.7 | 5.7 | 8.0 | 1.2 (14.6%) | 0.0 | 46.8% | 0 |
| Vesper's Ledger vs Auditor's Arsenal | 50.2% | 73.2% | 16.8 ± 2.6 | 1.0% | 0.0% | 79.2% | 41.9% | 6.0% | 7.5 | 8.2 | 7.7 | 1.4 (26.5%) | 5.0 | 19.1% | 0 |
| Sparkwrench Scrappers mirror | 51.4% | 61.6% | 19.9 ± 4.5 | 11.8% | 0.4% | 95.2% | 76.7% | 21.1% | 22.5 | 0.0 | 2.1 | 0.0 | 0.0 | 60.5% | 0 |
| Sparkwrench Scrappers vs Auditor's Arsenal | 71.8% | 70.4% | 15.2 ± 2.4 | 0.0% | 3.0% | 79.6% | 57.5% | 13.5% | 7.0 | 0.8 | 1.8 | 0.0 (61.5%) | 3.0 | 6.5% | 0 |
| Auditor's Arsenal mirror | 52.8% | 80.6% | 15.3 ± 1.4 | 0.0% | 0.0% | 63.9% | 44.1% | 13.6% | 3.4 | 1.1 | 1.5 | 0.0 (50.0%) | 6.4 | 0.2% | 0 |

## Bot play styles

*Do the results hold with a defensive player? Control blocks freely, keeps blockers home and saves Gold. In "Greedy vs Control", A win% is the Greedy side.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, Control vs Control | 58.0% | 75.4% | 17.0 ± 3.2 | 2.0% | 1.2% | 84.8% | 56.9% | 10.9% | 16.6 | 4.7 | 2.9 | 3.6 (10.7%) | 0.0 | 22.4% | 0 |
| Jungle Stampede mirror, Control vs Control | 51.8% | 68.0% | 33.3 ± 18.2 | 56.2% | 0.0% | 82.1% | 52.7% | 9.2% | 22.3 | 13.0 | 12.2 | 5.2 (30.9%) | 0.0 | 77.7% | 0 |
| Zoo Patrol mirror, Control vs Control | 50.8% | 66.6% | 22.9 ± 5.2 | 28.6% | 0.0% | 72.1% | 75.5% | 35.7% | 14.9 | 24.7 | 9.5 | 9.9 (25.6%) | 0.0 | 41.6% | 0 |
| Vesper's Ledger mirror, Control vs Control | 51.0% | 60.8% | 30.7 ± 7.7 | 72.4% | 0.0% | 80.7% | 63.7% | 17.9% | 28.5 | 33.2 | 34.0 | 12.3 (19.4%) | 0.0 | 85.5% | 0 |
| Sparkwrench Scrappers mirror, Control vs Control | 55.0% | 52.4% | 26.4 ± 7.6 | 46.2% | 0.0% | 97.1% | 86.3% | 22.4% | 30.1 | 0.0 | 1.9 | 0.0 | 0.0 | 82.0% | 0 |
| Auditor's Arsenal mirror, Control vs Control | 55.2% | 74.6% | 20.0 ± 4.6 | 6.4% | 0.0% | 72.8% | 64.4% | 21.6% | 8.2 | 5.8 | 2.8 | 1.1 (48.1%) | 17.6 | 43.2% | 0 |
| Goober Mob mirror, Greedy vs Control | 58.4% | 77.4% | 14.8 ± 2.5 | 0.2% | 2.8% | 84.6% | 47.7% | 7.4% | 12.4 | 3.9 | 2.4 | 2.2 (8.0%) | 0.0 | 6.5% | 0 |
| Jungle Stampede mirror, Greedy vs Control | 38.0% | 66.2% | 30.0 ± 18.5 | 37.8% | 0.0% | 81.7% | 44.6% | 5.1% | 17.1 | 10.7 | 10.1 | 3.1 (36.9%) | 0.0 | 77.9% | 0 |
| Zoo Patrol mirror, Greedy vs Control | 53.8% | 65.2% | 21.6 ± 4.9 | 19.6% | 0.0% | 72.8% | 72.9% | 34.6% | 13.7 | 21.4 | 8.3 | 8.6 (24.4%) | 0.0 | 39.0% | 0 |
| Vesper's Ledger mirror, Greedy vs Control | 51.2% | 60.2% | 27.5 ± 7.2 | 58.0% | 0.0% | 79.1% | 56.8% | 17.1% | 24.4 | 29.9 | 29.7 | 9.6 (19.1%) | 0.0 | 83.1% | 0 |
| Sparkwrench Scrappers mirror, Greedy vs Control | 49.0% | 53.6% | 22.7 ± 5.8 | 25.8% | 0.0% | 96.3% | 82.3% | 22.5% | 26.1 | 0.0 | 2.1 | 0.0 | 0.0 | 72.9% | 0 |
| Auditor's Arsenal mirror, Greedy vs Control | 56.4% | 75.4% | 17.4 ± 2.6 | 1.2% | 0.0% | 70.8% | 60.0% | 21.5% | 5.7 | 2.7 | 1.9 | 0.2 (46.4%) | 10.8 | 13.2% | 0 |

## Game length lever: starting life

*Can starting life even out game length between fast and slow decks? Compare with the 30-life mirrors in the round robin.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, 25 life | 53.8% | 77.6% | 12.2 ± 2.1 | 0.0% | 12.8% | 86.6% | 28.2% | 2.2% | 7.9 | 3.0 | 2.0 | 1.1 (9.4%) | 0.0 | 0.3% | 0 |
| Jungle Stampede mirror, 25 life | 50.8% | 71.0% | 24.5 ± 16.0 | 20.2% | 0.0% | 83.6% | 37.9% | 2.5% | 12.4 | 8.1 | 8.1 | 1.8 (40.5%) | 0.0 | 74.1% | 0 |
| Zoo Patrol mirror, 25 life | 51.6% | 69.4% | 18.5 ± 4.0 | 5.8% | 0.0% | 74.1% | 68.3% | 31.5% | 10.6 | 14.5 | 6.0 | 5.8 (19.2%) | 0.0 | 28.8% | 0 |
| Vesper's Ledger mirror, 25 life | 48.2% | 60.4% | 22.2 ± 5.6 | 24.8% | 0.0% | 78.0% | 47.2% | 13.3% | 17.3 | 23.5 | 21.7 | 4.9 (19.7%) | 0.0 | 77.5% | 0 |
| Sparkwrench Scrappers mirror, 25 life | 51.2% | 67.8% | 18.0 ± 4.1 | 5.0% | 1.4% | 95.4% | 76.9% | 20.2% | 19.9 | 0.0 | 2.0 | 0.0 | 0.0 | 50.0% | 0 |
| Auditor's Arsenal mirror, 25 life | 53.4% | 78.0% | 14.7 ± 1.5 | 0.0% | 0.0% | 64.4% | 43.3% | 14.1% | 3.2 | 1.2 | 1.4 | 0.0 (37.5%) | 5.6 | 1.0% | 0 |
| Goober Mob mirror, 35 life | 53.6% | 75.4% | 13.7 ± 2.2 | 0.2% | 4.6% | 88.4% | 26.4% | 1.4% | 9.5 | 3.4 | 2.3 | 1.6 (10.1%) | 0.0 | 5.4% | 0 |
| Jungle Stampede mirror, 35 life | 49.4% | 71.2% | 26.9 ± 16.0 | 30.8% | 0.0% | 85.7% | 38.0% | 1.9% | 13.6 | 8.3 | 9.8 | 2.1 (39.2%) | 0.0 | 73.6% | 0 |
| Zoo Patrol mirror, 35 life | 52.2% | 67.2% | 21.6 ± 4.7 | 19.6% | 0.0% | 72.8% | 69.8% | 32.2% | 13.5 | 20.8 | 8.4 | 8.6 (24.3%) | 0.0 | 39.4% | 0 |
| Vesper's Ledger mirror, 35 life | 51.8% | 58.4% | 25.0 ± 5.5 | 43.8% | 0.0% | 78.8% | 45.0% | 12.0% | 20.6 | 28.5 | 26.3 | 8.4 (16.5%) | 0.0 | 79.0% | 0 |
| Sparkwrench Scrappers mirror, 35 life | 51.8% | 57.6% | 21.8 ± 4.9 | 20.8% | 0.0% | 95.6% | 78.1% | 21.9% | 24.9 | 0.0 | 2.1 | 0.0 | 0.0 | 68.5% | 0 |
| Auditor's Arsenal mirror, 35 life | 50.8% | 80.2% | 15.7 ± 1.2 | 0.0% | 0.0% | 65.5% | 49.4% | 12.8% | 3.4 | 0.7 | 1.6 | 0.0 (57.1%) | 6.8 | 0.2% | 0 |

## Permanent damage (GAME_DESIGN §7.3)

*What changes when damage wears off at end of turn like in MTG? Compare with the round-robin mirrors (permanent damage).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, damage wears off (MTG) | 54.0% | 77.4% | 13.0 ± 2.0 | 0.0% | 6.6% | 86.3% | 0.0% | 0.0% | 8.5 | 3.3 | 2.2 | 1.4 (9.5%) | 0.0 | 0.5% | 0 |
| Jungle Stampede mirror, damage wears off (MTG) | 50.8% | 70.2% | 24.0 ± 13.3 | 23.2% | 0.0% | 81.0% | 0.0% | 0.0% | 12.6 | 6.9 | 9.0 | 2.2 (38.0%) | 0.0 | 67.4% | 0 |
| Zoo Patrol mirror, damage wears off (MTG) | 49.2% | 68.6% | 17.7 ± 3.3 | 3.0% | 0.0% | 70.7% | 0.0% | 0.0% | 9.9 | 6.9 | 5.3 | 2.7 (31.2%) | 0.0 | 22.7% | 0 |
| Vesper's Ledger mirror, damage wears off (MTG) | 49.0% | 62.4% | 22.9 ± 4.6 | 29.0% | 0.0% | 63.5% | 0.0% | 0.0% | 17.4 | 25.5 | 22.9 | 6.4 (16.9%) | 0.0 | 74.4% | 0 |
| Sparkwrench Scrappers mirror, damage wears off (MTG) | 51.2% | 69.8% | 17.2 ± 2.7 | 1.2% | 0.4% | 84.5% | 0.0% | 0.0% | 18.4 | 0.0 | 2.0 | 0.0 | 0.0 | 31.7% | 0 |
| Auditor's Arsenal mirror, damage wears off (MTG) | 53.6% | 82.2% | 15.0 ± 1.2 | 0.0% | 0.0% | 52.4% | 0.0% | 0.0% | 2.6 | 0.3 | 1.4 | 0.0 (33.3%) | 5.6 | 0.5% | 0 |

## Going second (GAME_DESIGN §3)

*First-player win rate in mirrors (50% is fair). Current rule (MTG default): the first player skips their turn-1 draw, no other compensation.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, current rule (MTG) | 53.0% | 77.6% | 13.0 ± 2.0 | 0.0% | 6.6% | 87.8% | 27.3% | 1.5% | 8.5 | 3.3 | 2.2 | 1.4 (9.1%) | 0.0 | 0.5% | 0 |
| Goober Mob mirror, everyone draws + 2nd player +1 mana on turn 1 | 52.6% | 71.6% | 12.7 ± 2.0 | 0.0% | 6.8% | 88.0% | 28.1% | 1.7% | 8.7 | 3.1 | 1.8 | 1.3 (8.0%) | 0.0 | 2.0% | 0 |
| Goober Mob mirror, everyone draws + 2nd player +1 mana and 1 Gold | 58.2% | 67.6% | 12.9 ± 2.1 | 0.0% | 5.2% | 89.1% | 25.0% | 1.7% | 9.1 | 3.1 | 2.7 | 1.5 (8.9%) | 0.0 | 2.6% | 0 |
| Jungle Stampede mirror, current rule (MTG) | 51.2% | 69.0% | 25.4 ± 15.9 | 24.6% | 0.0% | 85.5% | 37.3% | 2.3% | 12.8 | 7.9 | 8.8 | 1.9 (40.5%) | 0.0 | 73.2% | 0 |
| Jungle Stampede mirror, everyone draws + 2nd player +1 mana on turn 1 | 50.4% | 59.8% | 25.9 ± 15.8 | 25.6% | 0.0% | 85.7% | 40.5% | 1.9% | 13.3 | 8.5 | 9.1 | 2.0 (42.7%) | 0.0 | 73.2% | 0 |
| Jungle Stampede mirror, everyone draws + 2nd player +1 mana and 1 Gold | 48.0% | 56.6% | 26.5 ± 16.5 | 26.6% | 0.0% | 85.9% | 38.9% | 1.9% | 13.3 | 8.8 | 9.7 | 2.1 (43.7%) | 0.0 | 75.8% | 0 |
| Vesper's Ledger mirror, current rule (MTG) | 50.2% | 60.4% | 23.8 ± 5.5 | 34.6% | 0.0% | 78.9% | 47.4% | 13.0% | 19.2 | 26.4 | 23.8 | 6.6 (17.4%) | 0.0 | 80.2% | 0 |
| Vesper's Ledger mirror, everyone draws + 2nd player +1 mana on turn 1 | 54.2% | 61.6% | 23.7 ± 5.6 | 35.6% | 0.0% | 78.1% | 45.6% | 12.8% | 19.1 | 27.0 | 23.8 | 6.5 (17.7%) | 0.0 | 82.3% | 0 |
| Vesper's Ledger mirror, everyone draws + 2nd player +1 mana and 1 Gold | 51.4% | 60.0% | 23.7 ± 5.3 | 35.6% | 0.0% | 79.1% | 46.9% | 13.3% | 19.6 | 27.3 | 24.9 | 6.6 (19.7%) | 0.0 | 81.6% | 0 |

## Tavern Dwellers (GAME_DESIGN §9)

*What do Tavern Dwellers (passives and Powers, the universal Gold sink) change? Each mirror with and without Tavern Dwellers. Watch Wasted (mana lost to the Gold cap), Gold spent and game length.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, with Tavern Dwellers | 53.0% | 77.6% | 13.0 ± 2.0 | 0.0% | 6.6% | 87.8% | 27.3% | 1.5% | 8.5 | 3.3 | 2.2 | 1.4 (9.1%) | 0.0 | 0.5% | 0 |
| Goober Mob mirror, no Tavern Dwellers | 56.6% | 78.8% | 13.5 ± 2.8 | 0.6% | 6.2% | 88.3% | 27.1% | 2.0% | 8.4 | 3.3 | 2.3 | 0.0 | 0.0 | 18.3% | 0 |
| Jungle Stampede mirror, with Tavern Dwellers | 51.2% | 69.0% | 25.4 ± 15.9 | 24.6% | 0.0% | 85.5% | 37.3% | 2.3% | 12.8 | 7.9 | 8.8 | 1.9 (40.5%) | 0.0 | 73.2% | 0 |
| Jungle Stampede mirror, no Tavern Dwellers | 51.8% | 69.6% | 18.4 ± 4.7 | 6.6% | 0.0% | 71.8% | 32.1% | 10.4% | 9.3 | 6.7 | 4.3 | 0.0 | 0.0 | 38.6% | 0 |
| Zoo Patrol mirror, with Tavern Dwellers | 52.0% | 65.0% | 20.2 ± 4.4 | 13.0% | 0.0% | 73.6% | 69.4% | 31.8% | 12.2 | 17.8 | 7.2 | 7.2 (22.4%) | 0.0 | 35.6% | 0 |
| Zoo Patrol mirror, no Tavern Dwellers | 51.6% | 68.2% | 23.2 ± 6.6 | 34.2% | 0.0% | 87.1% | 82.3% | 48.6% | 15.7 | 7.3 | 4.0 | 0.0 | 0.0 | 70.8% | 0 |
| Vesper's Ledger mirror, with Tavern Dwellers | 50.2% | 60.4% | 23.8 ± 5.5 | 34.6% | 0.0% | 78.9% | 47.4% | 13.0% | 19.2 | 26.4 | 23.8 | 6.6 (17.4%) | 0.0 | 80.2% | 0 |
| Vesper's Ledger mirror, no Tavern Dwellers | 50.4% | 57.4% | 23.7 ± 5.3 | 33.4% | 0.0% | 80.1% | 40.8% | 9.2% | 18.9 | 27.1 | 12.6 | 0.0 | 0.0 | 71.2% | 0 |
| Sparkwrench Scrappers mirror, with Tavern Dwellers | 51.4% | 61.6% | 19.9 ± 4.5 | 11.8% | 0.4% | 95.2% | 76.7% | 21.1% | 22.5 | 0.0 | 2.1 | 0.0 | 0.0 | 60.5% | 0 |
| Sparkwrench Scrappers mirror, no Tavern Dwellers | 51.4% | 61.6% | 19.9 ± 4.5 | 11.8% | 0.4% | 95.2% | 76.7% | 21.1% | 22.5 | 0.0 | 2.1 | 0.0 | 0.0 | 60.5% | 0 |
| Auditor's Arsenal mirror, with Tavern Dwellers | 52.8% | 80.6% | 15.3 ± 1.4 | 0.0% | 0.0% | 63.9% | 44.1% | 13.6% | 3.4 | 1.1 | 1.5 | 0.0 (50.0%) | 6.4 | 0.2% | 0 |
| Auditor's Arsenal mirror, no Tavern Dwellers | 54.4% | 77.0% | 15.8 ± 1.2 | 0.0% | 0.0% | 63.5% | 41.4% | 12.2% | 3.3 | 1.0 | 1.8 | 0.0 | 4.9 | 0.4% | 0 |

## Gold cap (GAME_DESIGN §5.2)

*Does the cap matter now that Tavern Dweller Powers and Equip spend Gold?*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Zoo Patrol mirror, Gold cap 3 | 50.2% | 64.8% | 20.2 ± 4.6 | 13.2% | 0.0% | 73.8% | 69.4% | 31.6% | 12.2 | 17.5 | 6.8 | 7.1 (21.3%) | 0.0 | 46.4% | 0 |
| Zoo Patrol mirror, Gold cap 5 | 52.0% | 65.0% | 20.2 ± 4.4 | 13.0% | 0.0% | 73.6% | 69.4% | 31.8% | 12.2 | 17.8 | 7.2 | 7.2 (22.4%) | 0.0 | 35.6% | 0 |
| Zoo Patrol mirror, Gold cap 8 | 51.6% | 65.8% | 20.2 ± 4.4 | 13.6% | 0.0% | 73.5% | 69.3% | 31.7% | 12.2 | 17.8 | 7.3 | 7.2 (22.4%) | 0.0 | 27.2% | 0 |
| Vesper's Ledger mirror, Gold cap 3 | 50.4% | 59.0% | 24.1 ± 5.5 | 38.6% | 0.0% | 78.7% | 46.7% | 13.5% | 19.3 | 26.7 | 21.4 | 6.7 (15.3%) | 0.0 | 84.5% | 0 |
| Vesper's Ledger mirror, Gold cap 5 | 50.2% | 60.4% | 23.8 ± 5.5 | 34.6% | 0.0% | 78.9% | 47.4% | 13.0% | 19.2 | 26.4 | 23.8 | 6.6 (17.4%) | 0.0 | 80.2% | 0 |
| Vesper's Ledger mirror, Gold cap 8 | 51.4% | 60.8% | 23.8 ± 5.5 | 35.0% | 0.0% | 79.2% | 48.4% | 13.1% | 19.2 | 26.5 | 24.5 | 6.6 (17.4%) | 0.0 | 70.9% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 3 | 51.4% | 61.6% | 19.9 ± 4.5 | 11.8% | 0.4% | 95.2% | 76.8% | 21.1% | 22.5 | 0.0 | 2.1 | 0.0 | 0.0 | 69.8% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 5 | 51.4% | 61.6% | 19.9 ± 4.5 | 11.8% | 0.4% | 95.2% | 76.7% | 21.1% | 22.5 | 0.0 | 2.1 | 0.0 | 0.0 | 60.5% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 8 | 51.4% | 61.6% | 19.9 ± 4.5 | 11.8% | 0.4% | 95.2% | 76.7% | 21.1% | 22.5 | 0.0 | 2.1 | 0.0 | 0.0 | 48.7% | 0 |
| Auditor's Arsenal mirror, Gold cap 3 | 52.8% | 80.6% | 15.3 ± 1.5 | 0.0% | 0.0% | 64.1% | 44.4% | 13.6% | 3.4 | 1.1 | 1.6 | 0.1 (87.3%) | 6.4 | 0.9% | 0 |
| Auditor's Arsenal mirror, Gold cap 5 | 52.8% | 80.6% | 15.3 ± 1.4 | 0.0% | 0.0% | 63.9% | 44.1% | 13.6% | 3.4 | 1.1 | 1.5 | 0.0 (50.0%) | 6.4 | 0.2% | 0 |
| Auditor's Arsenal mirror, Gold cap 8 | 52.8% | 80.6% | 15.3 ± 1.4 | 0.0% | 0.0% | 63.9% | 44.1% | 13.6% | 3.4 | 1.1 | 1.5 | 0.0 (0.0%) | 6.4 | 0.0% | 0 |
