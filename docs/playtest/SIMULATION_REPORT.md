# Simulation Report

Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

500 games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time 28s.

Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) ± standard deviation · **Long** / **Short** = share of games over 25 / under 10 turns · **Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · **Chip→death** = share of *chip damage* (damage a creature carried into a later turn) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · **Wounded** = share of creatures carrying damage at the start of a turn · **Deaths** / **Heal** / **Gold spent** are per game · **Powers** = Tavern Dweller Powers used per game (in brackets: share used on an opponent's turn) · **Abil.** = other activated abilities per game (Equip, Tap abilities...) · **Off-turn** = spells cast and abilities/Powers used on an opponent's turn, per game · **Wasted** = share of unspent mana lost to the Gold cap.

## Round robin (current rules, Greedy bots)

*How long are games, and does damage on creatures decide anything? Every deck against every deck.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror | 54.2% | 81.2% | 12.2 ± 1.9 | 0.0% | 10.8% | 93.4% | 72.1% | 5.9% | 10.9 | 0.0 | 3.3 | 1.4 (24.6%) | 0.0 | 1.9 | 1.6% | 0 |
| Goober Mob vs Jungle Stampede | 63.4% | 80.0% | 13.0 ± 2.0 | 0.0% | 7.4% | 91.9% | 63.5% | 5.4% | 8.8 | 0.4 | 3.6 | 0.9 (17.3%) | 0.0 | 1.8 | 1.9% | 0 |
| Goober Mob vs Zoo Patrol | 89.6% | 63.0% | 14.4 ± 2.5 | 0.2% | 1.2% | 86.3% | 60.8% | 13.7% | 11.0 | 2.6 | 4.3 | 1.8 (13.2%) | 0.0 | 2.4 | 9.2% | 0 |
| Goober Mob vs Vesper's Ledger | 90.2% | 61.2% | 14.7 ± 2.4 | 0.0% | 1.6% | 93.2% | 61.4% | 3.2% | 12.0 | 5.9 | 11.1 | 2.8 (24.8%) | 0.0 | 3.9 | 7.5% | 0 |
| Goober Mob vs Sparkwrench Scrappers | 81.2% | 70.6% | 13.8 ± 2.3 | 0.0% | 3.6% | 94.2% | 76.1% | 11.6% | 12.3 | 0.0 | 3.0 | 2.1 (11.6%) | 1.1 | 2.6 | 8.0% | 0 |
| Goober Mob vs Auditor's Arsenal | 97.6% | 57.0% | 11.8 ± 1.9 | 0.0% | 12.4% | 81.1% | 57.9% | 4.7% | 5.1 | 0.1 | 2.2 | 0.7 (19.9%) | 1.0 | 2.0 | 1.3% | 0 |
| Jungle Stampede mirror | 53.0% | 82.8% | 17.2 ± 3.2 | 1.4% | 0.0% | 93.6% | 74.3% | 0.9% | 12.5 | 0.3 | 6.0 | 0.5 (56.8%) | 0.0 | 2.4 | 21.3% | 0 |
| Jungle Stampede vs Zoo Patrol | 78.8% | 70.2% | 17.1 ± 3.7 | 1.6% | 0.0% | 84.7% | 68.6% | 20.1% | 9.7 | 4.6 | 6.1 | 1.3 (16.8%) | 0.0 | 2.4 | 21.3% | 0 |
| Jungle Stampede vs Vesper's Ledger | 87.2% | 62.6% | 15.3 ± 2.1 | 0.0% | 0.0% | 86.6% | 47.5% | 0.9% | 7.3 | 5.6 | 8.3 | 1.2 (19.6%) | 0.0 | 3.3 | 3.6% | 0 |
| Jungle Stampede vs Sparkwrench Scrappers | 75.4% | 74.8% | 15.1 ± 2.3 | 0.0% | 0.0% | 88.9% | 65.7% | 12.2% | 9.5 | 0.9 | 3.5 | 1.7 (5.5%) | 1.5 | 2.8 | 10.1% | 0 |
| Jungle Stampede vs Auditor's Arsenal | 96.8% | 57.4% | 13.4 ± 2.0 | 0.0% | 0.0% | 79.1% | 38.8% | 2.6% | 3.7 | 0.3 | 2.4 | 0.0 (37.5%) | 1.5 | 1.9 | 1.6% | 0 |
| Zoo Patrol mirror | 51.8% | 66.8% | 22.7 ± 5.1 | 25.6% | 0.0% | 73.8% | 70.0% | 28.4% | 12.2 | 17.4 | 13.1 | 8.1 (29.8%) | 0.0 | 6.6 | 45.9% | 0 |
| Zoo Patrol vs Vesper's Ledger | 89.0% | 57.2% | 21.3 ± 4.5 | 15.2% | 0.0% | 88.5% | 75.5% | 10.4% | 17.0 | 11.1 | 15.8 | 5.2 (15.0%) | 0.0 | 6.6 | 37.6% | 0 |
| Zoo Patrol vs Sparkwrench Scrappers | 78.8% | 64.6% | 20.0 ± 3.9 | 7.0% | 0.0% | 82.1% | 61.7% | 22.5% | 13.4 | 7.5 | 6.2 | 7.0 (9.8%) | 4.2 | 5.2 | 42.5% | 0 |
| Zoo Patrol vs Auditor's Arsenal | 72.2% | 70.4% | 16.9 ± 2.1 | 0.4% | 0.0% | 71.7% | 41.6% | 22.1% | 6.1 | 6.3 | 3.8 | 2.3 (10.6%) | 4.0 | 3.3 | 9.1% | 0 |
| Vesper's Ledger mirror | 51.6% | 58.2% | 25.1 ± 5.5 | 44.8% | 0.0% | 95.9% | 78.8% | 4.6% | 26.9 | 27.6 | 38.0 | 9.6 (15.3%) | 0.0 | 9.2 | 60.1% | 0 |
| Vesper's Ledger vs Sparkwrench Scrappers | 24.2% | 54.0% | 17.6 ± 3.4 | 3.8% | 0.0% | 93.9% | 72.6% | 9.3% | 13.7 | 6.7 | 11.9 | 5.0 (14.2%) | 2.4 | 5.4 | 39.7% | 0 |
| Vesper's Ledger vs Auditor's Arsenal | 85.0% | 63.2% | 16.0 ± 2.2 | 0.2% | 0.0% | 87.7% | 52.6% | 2.0% | 6.2 | 8.4 | 8.1 | 2.2 (13.2%) | 3.9 | 4.0 | 3.0% | 0 |
| Sparkwrench Scrappers mirror | 54.0% | 63.0% | 20.6 ± 5.5 | 16.6% | 0.2% | 98.1% | 89.0% | 22.3% | 18.7 | 0.0 | 4.5 | 7.9 (7.4%) | 8.9 | 5.6 | 64.2% | 0 |
| Sparkwrench Scrappers vs Auditor's Arsenal | 92.8% | 61.0% | 13.7 ± 1.9 | 0.0% | 0.8% | 81.8% | 58.5% | 12.4% | 4.9 | 0.5 | 1.3 | 1.3 (1.1%) | 2.9 | 2.8 | 4.5% | 0 |
| Auditor's Arsenal mirror | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 2.1 | 0.0% | 0 |

## Bot play styles

*Do the results hold with a defensive player? Control blocks freely, keeps blockers home and saves Gold. In "Greedy vs Control", A win% is the Greedy side.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, Control vs Control | 54.0% | 79.0% | 14.8 ± 2.5 | 0.0% | 2.4% | 94.5% | 84.3% | 12.3% | 17.1 | 0.0 | 3.5 | 2.4 (18.4%) | 0.0 | 2.1 | 9.1% | 0 |
| Jungle Stampede mirror, Control vs Control | 53.2% | 76.2% | 21.7 ± 4.6 | 15.8% | 0.0% | 92.4% | 76.1% | 7.4% | 18.3 | 2.1 | 7.8 | 1.2 (54.2%) | 0.0 | 4.1 | 46.8% | 0 |
| Zoo Patrol mirror, Control vs Control | 49.2% | 64.6% | 25.0 ± 5.5 | 40.2% | 0.0% | 72.7% | 73.6% | 32.8% | 14.0 | 21.8 | 15.2 | 10.0 (31.9%) | 0.0 | 8.4 | 49.8% | 0 |
| Vesper's Ledger mirror, Control vs Control | 49.0% | 55.6% | 26.8 ± 5.8 | 55.2% | 0.0% | 95.8% | 81.5% | 5.0% | 28.7 | 27.4 | 40.6 | 12.2 (20.1%) | 0.0 | 10.7 | 52.2% | 0 |
| Sparkwrench Scrappers mirror, Control vs Control | 49.4% | 46.8% | 27.3 ± 8.8 | 50.8% | 0.0% | 98.7% | 91.0% | 25.6% | 24.5 | 0.0 | 5.5 | 12.3 (7.8%) | 17.9 | 8.6 | 78.9% | 0 |
| Auditor's Arsenal mirror, Control vs Control | 52.4% | 77.0% | 18.9 ± 2.9 | 2.8% | 0.0% | 67.9% | 54.6% | 22.8% | 5.6 | 5.1 | 1.4 | 0.4 (44.2%) | 15.2 | 3.6 | 31.1% | 0 |
| Goober Mob mirror, Greedy vs Control | 61.6% | 78.2% | 13.5 ± 2.2 | 0.0% | 5.2% | 94.9% | 84.7% | 9.6% | 14.2 | 0.0 | 3.3 | 1.9 (19.1%) | 0.0 | 2.0 | 2.7% | 0 |
| Jungle Stampede mirror, Greedy vs Control | 48.4% | 79.0% | 19.6 ± 3.8 | 6.4% | 0.0% | 93.0% | 73.4% | 4.2% | 15.7 | 1.2 | 7.0 | 0.8 (56.3%) | 0.0 | 3.3 | 34.2% | 0 |
| Zoo Patrol mirror, Greedy vs Control | 50.0% | 66.6% | 23.7 ± 5.2 | 31.2% | 0.0% | 73.0% | 72.0% | 31.0% | 13.0 | 19.6 | 14.3 | 9.1 (31.3%) | 0.0 | 7.6 | 46.5% | 0 |
| Vesper's Ledger mirror, Greedy vs Control | 60.4% | 58.6% | 26.0 ± 5.8 | 50.2% | 0.0% | 95.7% | 79.2% | 4.7% | 27.9 | 27.2 | 39.3 | 10.9 (17.6%) | 0.0 | 10.0 | 56.5% | 0 |
| Sparkwrench Scrappers mirror, Greedy vs Control | 48.6% | 50.0% | 23.9 ± 6.8 | 34.6% | 0.0% | 98.4% | 89.6% | 24.1% | 21.9 | 0.0 | 5.2 | 10.2 (7.9%) | 13.0 | 7.0 | 72.5% | 0 |
| Auditor's Arsenal mirror, Greedy vs Control | 58.4% | 77.8% | 17.2 ± 2.1 | 0.4% | 0.0% | 68.0% | 49.9% | 19.3% | 4.1 | 2.7 | 1.0 | 0.0 (42.1%) | 10.5 | 2.7 | 3.7% | 0 |

## Game length lever: starting life

*Can starting life even out game length between fast and slow decks? Compare with the 30-life mirrors in the round robin.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, 25 life | 53.0% | 80.8% | 11.3 ± 1.8 | 0.0% | 21.8% | 93.1% | 72.4% | 5.9% | 9.5 | 0.0 | 2.8 | 1.0 (26.9%) | 0.0 | 1.7 | 0.6% | 0 |
| Jungle Stampede mirror, 25 life | 52.6% | 84.4% | 16.0 ± 3.0 | 0.6% | 0.0% | 94.2% | 72.6% | 1.0% | 11.1 | 0.2 | 5.3 | 0.4 (58.7%) | 0.0 | 2.1 | 13.5% | 0 |
| Zoo Patrol mirror, 25 life | 51.4% | 69.2% | 21.0 ± 4.7 | 16.2% | 0.0% | 74.9% | 71.1% | 28.3% | 11.1 | 14.7 | 11.7 | 6.8 (27.3%) | 0.0 | 5.9 | 41.0% | 0 |
| Vesper's Ledger mirror, 25 life | 51.6% | 59.4% | 22.9 ± 4.9 | 28.4% | 0.0% | 95.1% | 74.1% | 4.8% | 23.0 | 23.6 | 32.8 | 7.1 (15.8%) | 0.0 | 8.4 | 56.0% | 0 |
| Sparkwrench Scrappers mirror, 25 life | 52.0% | 66.2% | 18.3 ± 4.6 | 6.6% | 0.4% | 97.4% | 85.8% | 21.7% | 16.2 | 0.0 | 3.6 | 6.2 (7.6%) | 6.3 | 5.1 | 55.3% | 0 |
| Auditor's Arsenal mirror, 25 life | 53.2% | 77.0% | 15.0 ± 1.5 | 0.0% | 0.0% | 64.1% | 32.6% | 7.2% | 2.2 | 1.0 | 0.9 | 0.0 | 5.7 | 2.1 | 0.0% | 0 |
| Goober Mob mirror, 35 life | 53.0% | 81.6% | 13.2 ± 2.0 | 0.0% | 4.8% | 94.0% | 74.9% | 5.9% | 12.4 | 0.0 | 3.8 | 1.7 (24.1%) | 0.0 | 2.1 | 3.9% | 0 |
| Jungle Stampede mirror, 35 life | 53.6% | 81.8% | 18.2 ± 3.2 | 1.6% | 0.0% | 93.6% | 69.5% | 0.7% | 13.6 | 0.3 | 6.5 | 0.7 (56.7%) | 0.0 | 2.6 | 25.4% | 0 |
| Zoo Patrol mirror, 35 life | 51.4% | 65.6% | 24.2 ± 5.4 | 37.4% | 0.0% | 72.8% | 70.1% | 28.6% | 13.3 | 20.3 | 14.7 | 9.4 (32.3%) | 0.0 | 7.3 | 49.7% | 0 |
| Vesper's Ledger mirror, 35 life | 47.8% | 56.4% | 26.3 ± 5.3 | 52.4% | 0.0% | 95.8% | 79.2% | 4.2% | 28.9 | 29.6 | 42.0 | 11.7 (13.9%) | 0.0 | 9.8 | 53.4% | 0 |
| Sparkwrench Scrappers mirror, 35 life | 52.2% | 56.8% | 22.8 ± 6.1 | 27.6% | 0.0% | 98.2% | 89.6% | 22.8% | 20.7 | 0.0 | 5.3 | 9.5 (7.8%) | 11.3 | 6.1 | 69.6% | 0 |
| Auditor's Arsenal mirror, 35 life | 53.6% | 79.0% | 16.3 ± 1.5 | 0.0% | 0.0% | 65.5% | 32.9% | 6.7% | 2.6 | 1.0 | 1.0 | 0.0 (57.1%) | 7.8 | 2.2 | 0.0% | 0 |

## Permanent damage (GAME_DESIGN §7.3)

*What changes when damage wears off at end of turn like in MTG? Compare with the round-robin mirrors (permanent damage).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, damage wears off (MTG) | 53.6% | 78.2% | 12.1 ± 1.8 | 0.0% | 11.8% | 90.7% | 0.0% | 0.0% | 10.4 | 0.0 | 3.4 | 1.4 (25.4%) | 0.0 | 1.9 | 1.5% | 0 |
| Jungle Stampede mirror, damage wears off (MTG) | 52.6% | 82.8% | 17.2 ± 3.1 | 1.0% | 0.0% | 92.8% | 0.0% | 0.0% | 12.4 | 0.0 | 6.0 | 0.5 (56.6%) | 0.0 | 2.3 | 20.0% | 0 |
| Zoo Patrol mirror, damage wears off (MTG) | 49.4% | 63.6% | 19.7 ± 4.0 | 8.0% | 0.0% | 73.2% | 0.0% | 0.0% | 10.3 | 8.7 | 10.7 | 3.9 (39.6%) | 0.0 | 4.9 | 33.9% | 0 |
| Vesper's Ledger mirror, damage wears off (MTG) | 52.8% | 57.0% | 25.1 ± 5.3 | 43.0% | 0.0% | 92.3% | 0.0% | 0.0% | 26.6 | 29.5 | 38.2 | 9.8 (15.1%) | 0.0 | 9.2 | 59.7% | 0 |
| Sparkwrench Scrappers mirror, damage wears off (MTG) | 49.2% | 66.6% | 18.0 ± 4.0 | 3.6% | 0.2% | 94.5% | 0.0% | 0.0% | 15.8 | 0.0 | 3.5 | 5.6 (6.9%) | 5.3 | 4.6 | 50.3% | 0 |
| Auditor's Arsenal mirror, damage wears off (MTG) | 51.2% | 75.8% | 15.6 ± 1.3 | 0.0% | 0.0% | 58.4% | 0.0% | 0.0% | 2.1 | 0.1 | 0.9 | 0.0 (50.0%) | 6.4 | 2.1 | 1.2% | 0 |

## Going second (GAME_DESIGN §3)

*First-player win rate in mirrors (50% is fair). Current rule (MTG default): the first player skips their turn-1 draw, no other compensation.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, current rule (MTG) | 54.2% | 81.2% | 12.2 ± 1.9 | 0.0% | 10.8% | 93.4% | 72.1% | 5.9% | 10.9 | 0.0 | 3.3 | 1.4 (24.6%) | 0.0 | 1.9 | 1.6% | 0 |
| Goober Mob mirror, everyone draws + 2nd player +1 mana on turn 1 | 55.8% | 74.0% | 12.1 ± 1.8 | 0.0% | 11.0% | 93.8% | 73.9% | 5.3% | 11.3 | 0.0 | 3.1 | 1.4 (25.8%) | 0.0 | 2.0 | 0.4% | 0 |
| Goober Mob mirror, everyone draws + 2nd player +1 mana and 1 Gold | 53.0% | 62.0% | 12.3 ± 1.8 | 0.0% | 8.0% | 94.2% | 75.2% | 6.2% | 12.1 | 0.0 | 4.0 | 1.6 (25.5%) | 0.0 | 2.0 | 0.6% | 0 |
| Jungle Stampede mirror, current rule (MTG) | 53.0% | 82.8% | 17.2 ± 3.2 | 1.4% | 0.0% | 93.6% | 74.3% | 0.9% | 12.5 | 0.3 | 6.0 | 0.5 (56.8%) | 0.0 | 2.4 | 21.3% | 0 |
| Jungle Stampede mirror, everyone draws + 2nd player +1 mana on turn 1 | 53.8% | 77.2% | 17.3 ± 3.2 | 1.6% | 0.0% | 93.8% | 75.6% | 0.7% | 12.7 | 0.3 | 5.8 | 0.5 (60.3%) | 0.0 | 2.5 | 20.2% | 0 |
| Jungle Stampede mirror, everyone draws + 2nd player +1 mana and 1 Gold | 54.4% | 74.6% | 17.2 ± 3.1 | 1.4% | 0.0% | 93.8% | 74.7% | 0.7% | 12.6 | 0.2 | 6.5 | 0.6 (64.5%) | 0.0 | 2.3 | 21.5% | 0 |
| Vesper's Ledger mirror, current rule (MTG) | 51.6% | 58.2% | 25.1 ± 5.5 | 44.8% | 0.0% | 95.9% | 78.8% | 4.6% | 26.9 | 27.6 | 38.0 | 9.6 (15.3%) | 0.0 | 9.2 | 60.1% | 0 |
| Vesper's Ledger mirror, everyone draws + 2nd player +1 mana on turn 1 | 51.2% | 63.0% | 23.9 ± 5.2 | 33.8% | 0.0% | 94.9% | 73.2% | 4.1% | 24.7 | 26.2 | 35.9 | 9.1 (15.0%) | 0.0 | 8.8 | 53.5% | 0 |
| Vesper's Ledger mirror, everyone draws + 2nd player +1 mana and 1 Gold | 52.2% | 66.0% | 23.7 ± 4.9 | 33.6% | 0.0% | 95.2% | 77.8% | 4.2% | 24.9 | 26.2 | 36.3 | 8.8 (15.5%) | 0.0 | 8.5 | 54.9% | 0 |

## Mana model: round pool (GAME_DESIGN §5)

*Does the round pool (everyone refills when a round starts, mana usable on any turn of the round, unspent mana banked at the end of the round) take away the first player's edge? Watch 1st win% (50% is fair).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, mana per turn (current) | 54.2% | 81.2% | 12.2 ± 1.9 | 0.0% | 10.8% | 93.4% | 72.1% | 5.9% | 10.9 | 0.0 | 3.3 | 1.4 (24.6%) | 0.0 | 1.9 | 1.6% | 0 |
| Goober Mob mirror, round pool | 53.6% | 85.0% | 12.5 ± 1.8 | 0.0% | 7.2% | 94.1% | 71.2% | 5.3% | 11.5 | 0.0 | 2.9 | 2.0 (37.2%) | 0.0 | 2.7 | 0.5% | 0 |
| Goober Mob mirror, round pool + Gold first off-turn | 54.6% | 83.6% | 12.5 ± 1.8 | 0.0% | 7.2% | 93.6% | 69.5% | 5.4% | 11.5 | 0.0 | 3.2 | 1.9 (35.5%) | 0.0 | 2.8 | 0.7% | 0 |
| Goober Mob mirror, rotating first player | 49.8% | 48.8% | 12.0 ± 1.7 | 0.0% | 8.4% | 94.1% | 74.8% | 3.3% | 9.5 | 0.0 | 3.4 | 1.4 (27.4%) | 0.0 | 1.5 | 1.0% | 0 |
| Goober Mob mirror, rotating first player + round pool + Gold first | 50.6% | 45.6% | 12.4 ± 1.7 | 0.0% | 4.4% | 94.1% | 77.5% | 3.6% | 10.6 | 0.0 | 3.3 | 2.1 (44.9%) | 0.0 | 2.8 | 0.9% | 0 |
| Goober Mob mirror, 1st player skips first mana | 45.6% | 12.2% | 13.0 ± 1.9 | 0.0% | 0.4% | 93.9% | 72.8% | 5.5% | 10.6 | 0.0 | 3.2 | 1.2 (26.0%) | 0.0 | 2.0 | 1.2% | 0 |
| Goober Mob mirror, 1st player skips first mana + round pool + Gold first | 46.6% | 18.4% | 13.5 ± 1.9 | 0.0% | 0.2% | 93.5% | 70.1% | 5.3% | 11.6 | 0.0 | 3.2 | 1.8 (39.1%) | 0.0 | 2.6 | 0.5% | 0 |
| Jungle Stampede mirror, mana per turn (current) | 53.0% | 82.8% | 17.2 ± 3.2 | 1.4% | 0.0% | 93.6% | 74.3% | 0.9% | 12.5 | 0.3 | 6.0 | 0.5 (56.8%) | 0.0 | 2.4 | 21.3% | 0 |
| Jungle Stampede mirror, round pool | 53.0% | 86.4% | 16.6 ± 3.2 | 0.8% | 0.0% | 93.6% | 66.4% | 1.0% | 11.5 | 0.2 | 4.4 | 0.3 (36.2%) | 0.0 | 2.1 | 16.4% | 0 |
| Jungle Stampede mirror, round pool + Gold first off-turn | 52.8% | 83.4% | 17.3 ± 3.2 | 1.2% | 0.0% | 93.7% | 73.9% | 0.8% | 12.6 | 0.3 | 5.9 | 0.6 (59.0%) | 0.0 | 2.6 | 17.2% | 0 |
| Jungle Stampede mirror, rotating first player | 49.0% | 52.8% | 16.7 ± 2.9 | 0.8% | 0.0% | 94.7% | 65.2% | 0.5% | 10.7 | 0.1 | 5.7 | 0.4 (68.1%) | 0.0 | 1.8 | 15.9% | 0 |
| Jungle Stampede mirror, rotating first player + round pool + Gold first | 50.0% | 51.0% | 16.7 ± 2.8 | 0.8% | 0.0% | 94.7% | 69.1% | 0.4% | 10.7 | 0.1 | 5.5 | 0.4 (69.7%) | 0.0 | 2.1 | 11.3% | 0 |
| Jungle Stampede mirror, 1st player skips first mana | 47.0% | 16.0% | 18.1 ± 3.1 | 2.4% | 0.0% | 94.2% | 70.5% | 0.8% | 12.4 | 0.2 | 5.9 | 0.6 (60.8%) | 0.0 | 2.5 | 20.0% | 0 |
| Jungle Stampede mirror, 1st player skips first mana + round pool + Gold first | 47.0% | 16.0% | 18.1 ± 3.1 | 2.6% | 0.0% | 94.1% | 70.1% | 0.8% | 12.4 | 0.3 | 5.2 | 0.6 (62.4%) | 0.0 | 2.2 | 18.6% | 0 |
| Zoo Patrol mirror, mana per turn (current) | 51.8% | 66.8% | 22.7 ± 5.1 | 25.6% | 0.0% | 73.8% | 70.0% | 28.4% | 12.2 | 17.4 | 13.1 | 8.1 (29.8%) | 0.0 | 6.6 | 45.9% | 0 |
| Zoo Patrol mirror, round pool | 50.8% | 56.2% | 22.5 ± 4.8 | 24.4% | 0.0% | 69.8% | 66.9% | 28.0% | 11.7 | 20.7 | 7.9 | 9.6 (32.6%) | 0.0 | 7.4 | 43.8% | 0 |
| Zoo Patrol mirror, round pool + Gold first off-turn | 52.0% | 52.2% | 22.5 ± 4.8 | 22.0% | 0.0% | 68.9% | 64.6% | 27.3% | 11.8 | 21.5 | 12.5 | 9.9 (35.4%) | 0.0 | 8.1 | 37.2% | 0 |
| Zoo Patrol mirror, rotating first player | 48.4% | 54.2% | 22.6 ± 5.3 | 24.0% | 0.0% | 76.0% | 75.7% | 27.1% | 12.2 | 16.1 | 12.8 | 7.3 (35.3%) | 0.0 | 5.1 | 48.5% | 0 |
| Zoo Patrol mirror, rotating first player + round pool + Gold first | 49.6% | 50.6% | 22.2 ± 4.6 | 20.0% | 0.0% | 70.6% | 67.4% | 26.5% | 11.6 | 19.9 | 12.3 | 9.0 (44.1%) | 0.0 | 7.7 | 37.6% | 0 |
| Zoo Patrol mirror, 1st player skips first mana | 47.2% | 37.8% | 24.2 ± 5.2 | 35.8% | 0.0% | 73.7% | 70.9% | 28.0% | 12.8 | 18.2 | 13.6 | 8.6 (31.1%) | 0.0 | 7.1 | 48.2% | 0 |
| Zoo Patrol mirror, 1st player skips first mana + round pool + Gold first | 48.4% | 29.8% | 23.1 ± 4.9 | 29.4% | 0.0% | 69.3% | 66.0% | 26.8% | 11.6 | 20.3 | 12.3 | 9.5 (33.7%) | 0.0 | 7.4 | 38.4% | 0 |
| Vesper's Ledger mirror, mana per turn (current) | 51.6% | 58.2% | 25.1 ± 5.5 | 44.8% | 0.0% | 95.9% | 78.8% | 4.6% | 26.9 | 27.6 | 38.0 | 9.6 (15.3%) | 0.0 | 9.2 | 60.1% | 0 |
| Vesper's Ledger mirror, round pool | 52.2% | 61.6% | 25.3 ± 5.1 | 44.8% | 0.0% | 95.5% | 73.7% | 4.2% | 25.6 | 28.2 | 32.0 | 9.3 (13.2%) | 0.0 | 8.8 | 62.9% | 0 |
| Vesper's Ledger mirror, round pool + Gold first off-turn | 51.8% | 56.0% | 25.4 ± 5.5 | 43.4% | 0.0% | 95.6% | 76.3% | 4.4% | 26.5 | 28.0 | 38.6 | 10.4 (19.5%) | 0.0 | 10.3 | 53.7% | 0 |
| Vesper's Ledger mirror, rotating first player | 49.2% | 35.4% | 21.3 ± 4.4 | 16.0% | 0.0% | 94.9% | 71.7% | 2.8% | 18.7 | 21.8 | 28.7 | 7.7 (28.6%) | 0.0 | 7.2 | 40.3% | 0 |
| Vesper's Ledger mirror, rotating first player + round pool + Gold first | 48.4% | 38.2% | 21.8 ± 4.2 | 17.2% | 0.0% | 94.7% | 69.7% | 3.0% | 18.8 | 21.8 | 29.8 | 8.7 (32.7%) | 0.0 | 8.9 | 27.1% | 0 |
| Vesper's Ledger mirror, 1st player skips first mana | 45.8% | 32.0% | 25.2 ± 5.2 | 46.8% | 0.0% | 95.5% | 75.6% | 4.5% | 25.5 | 26.0 | 36.5 | 9.2 (15.9%) | 0.0 | 9.0 | 58.3% | 0 |
| Vesper's Ledger mirror, 1st player skips first mana + round pool + Gold first | 47.4% | 24.0% | 25.0 ± 5.1 | 42.6% | 0.0% | 95.5% | 75.2% | 4.4% | 24.2 | 25.3 | 35.8 | 9.5 (20.2%) | 0.0 | 9.6 | 44.0% | 0 |
| Sparkwrench Scrappers mirror, mana per turn (current) | 54.0% | 63.0% | 20.6 ± 5.5 | 16.6% | 0.2% | 98.1% | 89.0% | 22.3% | 18.7 | 0.0 | 4.5 | 7.9 (7.4%) | 8.9 | 5.6 | 64.2% | 0 |
| Sparkwrench Scrappers mirror, round pool | 52.2% | 61.2% | 20.7 ± 5.3 | 16.6% | 0.2% | 97.9% | 87.7% | 22.7% | 18.4 | 0.0 | 3.1 | 8.6 (13.5%) | 8.9 | 6.4 | 65.9% | 0 |
| Sparkwrench Scrappers mirror, round pool + Gold first off-turn | 51.6% | 61.4% | 20.5 ± 5.2 | 15.8% | 0.2% | 97.8% | 87.7% | 23.0% | 18.3 | 0.0 | 4.1 | 8.6 (14.5%) | 8.3 | 6.4 | 61.2% | 0 |
| Sparkwrench Scrappers mirror, rotating first player | 50.4% | 43.4% | 19.0 ± 3.7 | 5.4% | 0.0% | 97.1% | 87.3% | 21.3% | 16.6 | 0.0 | 4.2 | 6.5 (10.6%) | 5.9 | 2.7 | 51.2% | 0 |
| Sparkwrench Scrappers mirror, rotating first player + round pool + Gold first | 47.2% | 41.8% | 19.5 ± 4.1 | 7.2% | 0.0% | 97.0% | 85.8% | 22.3% | 16.8 | 0.0 | 4.1 | 7.5 (19.0%) | 6.3 | 4.3 | 51.3% | 0 |
| Sparkwrench Scrappers mirror, 1st player skips first mana | 48.6% | 30.8% | 21.3 ± 5.0 | 17.8% | 0.0% | 97.7% | 87.8% | 22.5% | 18.8 | 0.0 | 4.1 | 7.6 (7.7%) | 8.5 | 5.8 | 61.0% | 0 |
| Sparkwrench Scrappers mirror, 1st player skips first mana + round pool + Gold first | 46.2% | 33.6% | 22.1 ± 5.6 | 22.4% | 0.0% | 97.9% | 88.6% | 23.5% | 19.4 | 0.0 | 4.4 | 9.3 (11.0%) | 9.2 | 5.9 | 62.3% | 0 |
| Auditor's Arsenal mirror, mana per turn (current) | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 2.1 | 0.0% | 0 |
| Auditor's Arsenal mirror, round pool | 53.6% | 76.2% | 15.4 ± 1.4 | 0.0% | 0.0% | 64.6% | 34.9% | 7.1% | 2.3 | 0.9 | 1.2 | 0.0 (33.3%) | 6.4 | 1.7 | 0.0% | 0 |
| Auditor's Arsenal mirror, round pool + Gold first off-turn | 53.6% | 76.2% | 15.4 ± 1.4 | 0.0% | 0.0% | 64.4% | 34.5% | 7.1% | 2.3 | 0.9 | 1.2 | 0.0 (33.3%) | 6.4 | 1.7 | 0.0% | 0 |
| Auditor's Arsenal mirror, rotating first player | 48.8% | 47.4% | 15.5 ± 1.5 | 0.0% | 0.0% | 67.7% | 35.2% | 6.0% | 2.3 | 0.6 | 0.9 | 0.0 | 6.2 | 1.2 | 0.0% | 0 |
| Auditor's Arsenal mirror, rotating first player + round pool + Gold first | 51.0% | 46.0% | 15.2 ± 1.4 | 0.0% | 0.0% | 64.9% | 32.1% | 6.6% | 2.1 | 0.6 | 1.7 | 0.0 (0.0%) | 6.1 | 0.9 | 0.1% | 0 |
| Auditor's Arsenal mirror, 1st player skips first mana | 46.2% | 23.2% | 16.7 ± 1.6 | 0.0% | 0.0% | 66.9% | 31.1% | 6.3% | 2.4 | 1.2 | 0.8 | 0.0 | 6.8 | 2.3 | 0.0% | 0 |
| Auditor's Arsenal mirror, 1st player skips first mana + round pool + Gold first | 47.8% | 20.8% | 16.3 ± 1.5 | 0.0% | 0.0% | 66.2% | 33.1% | 6.3% | 2.2 | 0.9 | 1.2 | 0.0 | 6.2 | 1.8 | 0.0% | 0 |

## Runeterra-style mana

*Legends of Runeterra's mana in full turns: everyone refills when a round starts, unspent mana becomes Gold (spell mana) at the end of the round, spells and abilities spend Gold first, the round leader alternates and only they may attack (attack token), creatures can attack the turn they arrive. Watch Turns, Off-turn and 1st win%.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, today's rules | 54.2% | 81.2% | 12.2 ± 1.9 | 0.0% | 10.8% | 93.4% | 72.1% | 5.9% | 10.9 | 0.0 | 3.3 | 1.4 (24.6%) | 0.0 | 1.9 | 1.6% | 0 |
| Goober Mob mirror, Runeterra | 53.8% | 46.8% | 13.8 ± 1.8 | 0.0% | 0.0% | 94.1% | 72.4% | 3.1% | 11.9 | 0.0 | 4.1 | 2.7 (45.5%) | 0.0 | 3.4 | 4.4% | 0 |
| Goober Mob mirror, Runeterra, Gold cap 5 | 53.6% | 47.0% | 13.8 ± 1.8 | 0.0% | 0.0% | 94.1% | 72.1% | 3.1% | 11.9 | 0.0 | 4.3 | 2.7 (45.4%) | 0.0 | 3.3 | 2.3% | 0 |
| Goober Mob mirror, Runeterra, no summoning sickness | 50.4% | 47.4% | 12.2 ± 1.3 | 0.0% | 2.0% | 97.0% | 25.0% | 0.0% | 8.2 | 0.0 | 3.8 | 2.2 (50.8%) | 0.0 | 3.0 | 1.9% | 0 |
| Goober Mob mirror, Runeterra without the attack token | 51.6% | 45.0% | 12.4 ± 1.7 | 0.0% | 4.6% | 94.0% | 77.9% | 3.6% | 10.6 | 0.0 | 3.6 | 2.1 (45.0%) | 0.0 | 2.8 | 2.7% | 0 |
| Jungle Stampede mirror, today's rules | 53.0% | 82.8% | 17.2 ± 3.2 | 1.4% | 0.0% | 93.6% | 74.3% | 0.9% | 12.5 | 0.3 | 6.0 | 0.5 (56.8%) | 0.0 | 2.4 | 21.3% | 0 |
| Jungle Stampede mirror, Runeterra | 49.2% | 45.4% | 18.8 ± 2.9 | 1.8% | 0.0% | 94.0% | 43.8% | 0.5% | 12.8 | 0.4 | 8.2 | 0.6 (54.2%) | 0.0 | 3.0 | 35.0% | 0 |
| Jungle Stampede mirror, Runeterra, Gold cap 5 | 49.0% | 46.8% | 18.7 ± 2.9 | 2.0% | 0.0% | 93.7% | 41.9% | 0.5% | 12.8 | 0.4 | 8.7 | 0.6 (61.5%) | 0.0 | 3.1 | 24.3% | 0 |
| Jungle Stampede mirror, Runeterra, no summoning sickness | 46.0% | 33.0% | 11.7 ± 0.9 | 0.0% | 0.0% | 97.9% | 0.0% | 0.0% | 1.7 | 0.0 | 3.9 | 0.0 (83.3%) | 0.0 | 0.8 | 9.0% | 0 |
| Jungle Stampede mirror, Runeterra without the attack token | 49.4% | 48.8% | 17.0 ± 2.8 | 0.6% | 0.0% | 94.4% | 54.5% | 0.5% | 11.3 | 0.3 | 6.7 | 0.4 (66.4%) | 0.0 | 2.6 | 21.7% | 0 |
| Zoo Patrol mirror, today's rules | 51.8% | 66.8% | 22.7 ± 5.1 | 25.6% | 0.0% | 73.8% | 70.0% | 28.4% | 12.2 | 17.4 | 13.1 | 8.1 (29.8%) | 0.0 | 6.6 | 45.9% | 0 |
| Zoo Patrol mirror, Runeterra | 47.2% | 55.0% | 26.4 ± 4.6 | 47.0% | 0.0% | 69.5% | 71.8% | 26.7% | 14.4 | 27.1 | 20.1 | 12.4 (46.0%) | 0.0 | 9.9 | 52.5% | 0 |
| Zoo Patrol mirror, Runeterra, Gold cap 5 | 49.0% | 55.2% | 26.0 ± 4.5 | 45.4% | 0.0% | 69.6% | 71.0% | 26.3% | 14.2 | 26.5 | 22.8 | 12.0 (46.2%) | 0.0 | 9.7 | 44.2% | 0 |
| Zoo Patrol mirror, Runeterra, no summoning sickness | 49.4% | 50.4% | 19.8 ± 3.0 | 4.2% | 0.0% | 71.2% | 67.9% | 27.1% | 9.4 | 15.3 | 12.0 | 6.9 (39.8%) | 0.0 | 6.4 | 35.0% | 0 |
| Zoo Patrol mirror, Runeterra without the attack token | 49.6% | 49.8% | 22.0 ± 4.5 | 18.6% | 0.0% | 72.1% | 70.3% | 26.6% | 11.6 | 18.6 | 14.4 | 8.5 (42.4%) | 0.0 | 7.5 | 44.2% | 0 |
| Vesper's Ledger mirror, today's rules | 51.6% | 58.2% | 25.1 ± 5.5 | 44.8% | 0.0% | 95.9% | 78.8% | 4.6% | 26.9 | 27.6 | 38.0 | 9.6 (15.3%) | 0.0 | 9.2 | 60.1% | 0 |
| Vesper's Ledger mirror, Runeterra | 48.4% | 44.2% | 25.4 ± 4.0 | 44.0% | 0.0% | 94.0% | 75.5% | 3.1% | 24.1 | 24.2 | 39.7 | 10.9 (25.0%) | 0.0 | 9.8 | 37.9% | 0 |
| Vesper's Ledger mirror, Runeterra, Gold cap 5 | 49.6% | 40.6% | 25.2 ± 4.2 | 41.8% | 0.0% | 94.3% | 76.2% | 2.8% | 24.2 | 24.2 | 44.6 | 10.5 (29.0%) | 0.0 | 10.1 | 32.2% | 0 |
| Vesper's Ledger mirror, Runeterra, no summoning sickness | 54.4% | 42.2% | 18.0 ± 2.7 | 0.4% | 0.0% | 88.5% | 58.5% | 1.4% | 9.4 | 18.4 | 20.6 | 5.9 (24.9%) | 0.0 | 6.1 | 13.3% | 0 |
| Vesper's Ledger mirror, Runeterra without the attack token | 48.2% | 41.2% | 21.8 ± 4.0 | 15.0% | 0.0% | 94.3% | 70.1% | 3.6% | 19.2 | 20.6 | 32.5 | 9.0 (26.0%) | 0.0 | 8.3 | 28.5% | 0 |
| Sparkwrench Scrappers mirror, today's rules | 54.0% | 63.0% | 20.6 ± 5.5 | 16.6% | 0.2% | 98.1% | 89.0% | 22.3% | 18.7 | 0.0 | 4.5 | 7.9 (7.4%) | 8.9 | 5.6 | 64.2% | 0 |
| Sparkwrench Scrappers mirror, Runeterra | 44.0% | 41.0% | 22.5 ± 4.7 | 20.2% | 0.0% | 97.5% | 88.4% | 23.3% | 19.4 | 0.0 | 11.3 | 9.2 (18.1%) | 8.3 | 4.7 | 64.2% | 0 |
| Sparkwrench Scrappers mirror, Runeterra, Gold cap 5 | 43.8% | 40.8% | 22.5 ± 4.6 | 20.0% | 0.0% | 97.5% | 88.8% | 23.1% | 19.3 | 0.0 | 13.1 | 9.2 (18.0%) | 8.3 | 4.7 | 53.3% | 0 |
| Sparkwrench Scrappers mirror, Runeterra, no summoning sickness | 49.8% | 53.6% | 14.2 ± 1.7 | 0.0% | 0.0% | 85.3% | 60.9% | 26.0% | 7.1 | 0.0 | 2.7 | 3.1 (25.8%) | 2.2 | 3.1 | 14.6% | 0 |
| Sparkwrench Scrappers mirror, Runeterra without the attack token | 46.2% | 41.6% | 19.3 ± 3.8 | 7.0% | 0.0% | 96.9% | 86.1% | 21.7% | 16.6 | 0.0 | 7.0 | 7.4 (19.0%) | 6.0 | 4.3 | 53.2% | 0 |
| Auditor's Arsenal mirror, today's rules | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 2.1 | 0.0% | 0 |
| Auditor's Arsenal mirror, Runeterra | 49.4% | 56.8% | 17.4 ± 1.4 | 0.0% | 0.0% | 63.5% | 27.7% | 5.9% | 2.3 | 0.4 | 2.1 | 0.3 (6.3%) | 9.2 | 1.1 | 1.3% | 0 |
| Auditor's Arsenal mirror, Runeterra, Gold cap 5 | 49.8% | 56.8% | 17.4 ± 1.4 | 0.0% | 0.0% | 63.7% | 28.5% | 6.0% | 2.3 | 0.4 | 2.0 | 0.1 (1.6%) | 9.2 | 1.1 | 1.0% | 0 |
| Auditor's Arsenal mirror, Runeterra, no summoning sickness | 52.8% | 44.2% | 15.0 ± 1.5 | 0.0% | 0.0% | 54.5% | 28.9% | 4.9% | 0.9 | 0.3 | 1.8 | 0.1 (6.7%) | 5.6 | 1.0 | 0.0% | 0 |
| Auditor's Arsenal mirror, Runeterra without the attack token | 51.0% | 46.0% | 15.3 ± 1.4 | 0.0% | 0.0% | 65.2% | 32.4% | 6.6% | 2.1 | 0.6 | 1.9 | 0.1 (6.6%) | 6.1 | 0.9 | 0.5% | 0 |

## Tavern Dwellers (GAME_DESIGN §9)

*What do Tavern Dwellers (passives and Powers, the universal Gold sink) change? Each mirror with and without Tavern Dwellers. Watch Wasted (mana lost to the Gold cap), Gold spent and game length.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, with Tavern Dwellers | 54.2% | 81.2% | 12.2 ± 1.9 | 0.0% | 10.8% | 93.4% | 72.1% | 5.9% | 10.9 | 0.0 | 3.3 | 1.4 (24.6%) | 0.0 | 1.9 | 1.6% | 0 |
| Goober Mob mirror, no Tavern Dwellers | 53.2% | 81.8% | 13.0 ± 2.3 | 0.0% | 7.4% | 93.8% | 73.9% | 6.2% | 11.5 | 0.0 | 3.3 | 0.0 | 0.0 | 2.0 | 12.7% | 0 |
| Jungle Stampede mirror, with Tavern Dwellers | 53.0% | 82.8% | 17.2 ± 3.2 | 1.4% | 0.0% | 93.6% | 74.3% | 0.9% | 12.5 | 0.3 | 6.0 | 0.5 (56.8%) | 0.0 | 2.4 | 21.3% | 0 |
| Jungle Stampede mirror, no Tavern Dwellers | 51.2% | 79.8% | 17.0 ± 4.2 | 2.6% | 0.0% | 85.7% | 67.1% | 1.3% | 10.0 | 0.6 | 5.2 | 0.0 | 0.0 | 1.7 | 29.0% | 0 |
| Zoo Patrol mirror, with Tavern Dwellers | 51.8% | 66.8% | 22.7 ± 5.1 | 25.6% | 0.0% | 73.8% | 70.0% | 28.4% | 12.2 | 17.4 | 13.1 | 8.1 (29.8%) | 0.0 | 6.6 | 45.9% | 0 |
| Zoo Patrol mirror, no Tavern Dwellers | 50.2% | 65.6% | 28.3 ± 8.2 | 59.6% | 0.0% | 91.0% | 86.0% | 47.9% | 16.4 | 4.6 | 9.0 | 0.0 | 0.0 | 7.6 | 76.3% | 0 |
| Vesper's Ledger mirror, with Tavern Dwellers | 51.6% | 58.2% | 25.1 ± 5.5 | 44.8% | 0.0% | 95.9% | 78.8% | 4.6% | 26.9 | 27.6 | 38.0 | 9.6 (15.3%) | 0.0 | 9.2 | 60.1% | 0 |
| Vesper's Ledger mirror, no Tavern Dwellers | 50.6% | 52.8% | 26.9 ± 6.4 | 53.0% | 0.0% | 95.3% | 71.0% | 4.7% | 26.2 | 28.7 | 20.7 | 0.0 | 0.0 | 7.8 | 71.0% | 0 |
| Sparkwrench Scrappers mirror, with Tavern Dwellers | 54.0% | 63.0% | 20.6 ± 5.5 | 16.6% | 0.2% | 98.1% | 89.0% | 22.3% | 18.7 | 0.0 | 4.5 | 7.9 (7.4%) | 8.9 | 5.6 | 64.2% | 0 |
| Sparkwrench Scrappers mirror, no Tavern Dwellers | 53.0% | 62.0% | 20.7 ± 5.3 | 16.0% | 0.0% | 97.8% | 87.2% | 21.7% | 19.1 | 0.0 | 2.6 | 0.0 | 11.9 | 5.0 | 71.5% | 0 |
| Auditor's Arsenal mirror, with Tavern Dwellers | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 2.1 | 0.0% | 0 |
| Auditor's Arsenal mirror, no Tavern Dwellers | 52.4% | 78.2% | 16.4 ± 1.4 | 0.0% | 0.0% | 65.1% | 32.4% | 5.8% | 2.6 | 1.0 | 1.1 | 0.0 | 5.5 | 2.8 | 0.0% | 0 |

## Gold cap (GAME_DESIGN §5.2)

*Does the cap matter now that Tavern Dweller Powers and Equip spend Gold?*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Zoo Patrol mirror, Gold cap 3 | 52.0% | 65.4% | 23.0 ± 5.3 | 27.4% | 0.0% | 74.5% | 71.1% | 28.3% | 12.4 | 16.9 | 11.7 | 8.0 (28.1%) | 0.0 | 6.5 | 57.8% | 0 |
| Zoo Patrol mirror, Gold cap 5 | 51.8% | 66.8% | 22.7 ± 5.1 | 25.6% | 0.0% | 73.8% | 70.0% | 28.4% | 12.2 | 17.4 | 13.1 | 8.1 (29.8%) | 0.0 | 6.6 | 45.9% | 0 |
| Zoo Patrol mirror, Gold cap 8 | 50.6% | 66.0% | 22.6 ± 5.0 | 26.0% | 0.0% | 73.6% | 69.8% | 28.1% | 12.2 | 17.4 | 13.5 | 8.1 (29.9%) | 0.0 | 6.6 | 36.5% | 0 |
| Vesper's Ledger mirror, Gold cap 3 | 49.2% | 61.0% | 24.6 ± 5.3 | 35.6% | 0.0% | 95.2% | 76.3% | 4.9% | 25.0 | 26.7 | 30.5 | 9.7 (13.6%) | 0.0 | 8.3 | 61.1% | 0 |
| Vesper's Ledger mirror, Gold cap 5 | 51.6% | 58.2% | 25.1 ± 5.5 | 44.8% | 0.0% | 95.9% | 78.8% | 4.6% | 26.9 | 27.6 | 38.0 | 9.6 (15.3%) | 0.0 | 9.2 | 60.1% | 0 |
| Vesper's Ledger mirror, Gold cap 8 | 50.4% | 57.4% | 25.1 ± 5.4 | 45.0% | 0.0% | 95.8% | 76.2% | 4.7% | 27.1 | 28.2 | 39.9 | 9.7 (15.8%) | 0.0 | 9.3 | 51.4% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 3 | 53.6% | 62.6% | 20.6 ± 5.5 | 16.8% | 0.2% | 98.1% | 89.1% | 22.3% | 18.7 | 0.0 | 4.4 | 7.9 (7.3%) | 8.9 | 5.6 | 73.1% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 5 | 54.0% | 63.0% | 20.6 ± 5.5 | 16.6% | 0.2% | 98.1% | 89.0% | 22.3% | 18.7 | 0.0 | 4.5 | 7.9 (7.4%) | 8.9 | 5.6 | 64.2% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 8 | 54.2% | 62.8% | 20.6 ± 5.5 | 16.8% | 0.2% | 98.1% | 89.0% | 22.3% | 18.7 | 0.0 | 4.5 | 7.9 (7.4%) | 8.9 | 5.6 | 51.9% | 0 |
| Auditor's Arsenal mirror, Gold cap 3 | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.0% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 2.1 | 0.0% | 0 |
| Auditor's Arsenal mirror, Gold cap 5 | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 2.1 | 0.0% | 0 |
| Auditor's Arsenal mirror, Gold cap 8 | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 2.1 | 0.0% | 0 |
