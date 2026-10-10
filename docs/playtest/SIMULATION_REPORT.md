# Simulation Report

Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

1000 games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time 48s.

Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) ± standard deviation · **Long** / **Short** = share of games over 25 / under 10 turns · **Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · **Chip→death** = share of *chip damage* (damage a creature carried into a later turn) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · **Wounded** = share of creatures carrying damage at the start of a turn · **Deaths** / **Heal** / **Gold spent** are per game · **Powers** = Tavern Dweller Powers used per game (in brackets: share used on an opponent's turn) · **Abil.** = other activated abilities per game (Equip, Tap abilities...) · **Off-turn** = spells cast and abilities/Powers used on an opponent's turn, per game · **Wasted** = share of unspent mana lost to the Gold cap.

## Round robin (current rules, Greedy bots)

*How long are games, and does damage on creatures decide anything? Every deck against every deck.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror | 49.5% | 84.3% | 12.1 ± 1.6 | 0.0% | 6.6% | 92.7% | 67.8% | 7.1% | 11.5 | 0.0 | 3.4 | 1.4 (23.8%) | 0.0 | 1.9 | 1.8% | 0 |
| Goober Mob vs Jungle Stampede | 66.4% | 78.0% | 12.8 ± 1.8 | 0.0% | 5.0% | 90.7% | 60.5% | 7.8% | 8.6 | 0.4 | 4.3 | 1.0 (21.5%) | 0.0 | 1.8 | 1.3% | 0 |
| Goober Mob vs Zoo Patrol | 91.9% | 59.1% | 13.9 ± 2.1 | 0.0% | 1.5% | 88.3% | 62.6% | 12.6% | 11.2 | 1.8 | 3.7 | 1.4 (12.4%) | 0.0 | 2.1 | 1.9% | 0 |
| Goober Mob vs Vesper's Ledger | 85.1% | 60.3% | 14.4 ± 2.0 | 0.0% | 0.8% | 90.6% | 52.8% | 6.2% | 11.2 | 8.6 | 11.2 | 2.4 (21.5%) | 1.1 | 3.8 | 5.3% | 0 |
| Goober Mob vs Sparkwrench Scrappers | 92.2% | 59.2% | 13.3 ± 2.2 | 0.0% | 4.1% | 93.8% | 71.9% | 10.7% | 11.1 | 0.0 | 3.0 | 1.7 (10.7%) | 0.8 | 2.8 | 3.3% | 0 |
| Goober Mob vs Auditor's Arsenal | 97.6% | 54.0% | 12.2 ± 1.8 | 0.0% | 7.2% | 81.5% | 60.3% | 6.2% | 5.9 | 0.1 | 2.4 | 0.8 (16.8%) | 0.9 | 2.1 | 0.9% | 0 |
| Jungle Stampede mirror | 51.1% | 87.3% | 17.2 ± 3.1 | 1.3% | 0.0% | 93.3% | 72.5% | 0.9% | 11.8 | 0.1 | 7.0 | 0.3 (57.6%) | 0.6 | 1.5 | 8.2% | 0 |
| Jungle Stampede vs Zoo Patrol | 71.4% | 73.4% | 16.3 ± 2.8 | 0.4% | 0.0% | 84.5% | 65.6% | 17.6% | 8.3 | 3.2 | 5.7 | 0.9 (11.5%) | 0.2 | 1.7 | 5.5% | 0 |
| Jungle Stampede vs Vesper's Ledger | 79.5% | 65.3% | 15.0 ± 2.0 | 0.0% | 0.0% | 84.8% | 46.4% | 0.9% | 6.0 | 8.9 | 8.5 | 0.6 (19.8%) | 1.1 | 2.9 | 2.0% | 0 |
| Jungle Stampede vs Sparkwrench Scrappers | 82.5% | 67.7% | 15.3 ± 2.5 | 0.2% | 0.0% | 89.7% | 70.3% | 12.0% | 8.5 | 0.6 | 4.2 | 1.4 (3.5%) | 1.5 | 2.6 | 3.6% | 0 |
| Jungle Stampede vs Auditor's Arsenal | 91.5% | 60.3% | 13.7 ± 2.2 | 0.0% | 0.0% | 77.2% | 37.9% | 3.3% | 3.6 | 0.4 | 3.0 | 0.0 (36.8%) | 1.6 | 1.9 | 1.0% | 0 |
| Zoo Patrol mirror | 50.9% | 71.1% | 19.5 ± 3.3 | 4.8% | 0.0% | 73.8% | 66.0% | 25.9% | 10.4 | 12.1 | 7.8 | 5.5 (16.3%) | 0.0 | 4.4 | 13.0% | 0 |
| Zoo Patrol vs Vesper's Ledger | 72.9% | 59.3% | 19.4 ± 3.3 | 4.8% | 0.0% | 88.3% | 73.7% | 10.1% | 12.0 | 14.0 | 12.3 | 3.2 (16.0%) | 1.0 | 5.8 | 16.0% | 0 |
| Zoo Patrol vs Sparkwrench Scrappers | 65.4% | 69.4% | 18.5 ± 2.9 | 1.9% | 0.0% | 83.0% | 66.7% | 22.5% | 11.1 | 5.2 | 5.1 | 5.0 (8.5%) | 2.2 | 4.8 | 14.6% | 0 |
| Zoo Patrol vs Auditor's Arsenal | 66.0% | 73.8% | 17.0 ± 2.1 | 0.6% | 0.0% | 72.5% | 42.8% | 19.8% | 5.6 | 4.5 | 3.1 | 1.6 (9.2%) | 3.6 | 3.1 | 2.6% | 0 |
| Vesper's Ledger mirror | 52.6% | 60.4% | 20.9 ± 3.9 | 12.4% | 0.0% | 94.2% | 63.7% | 2.4% | 13.6 | 31.6 | 23.9 | 4.9 (12.7%) | 4.5 | 7.2 | 16.0% | 0 |
| Vesper's Ledger vs Sparkwrench Scrappers | 31.1% | 62.3% | 17.8 ± 2.6 | 0.6% | 0.0% | 92.1% | 70.6% | 9.9% | 10.6 | 10.5 | 10.3 | 3.7 (11.3%) | 3.1 | 5.6 | 15.3% | 0 |
| Vesper's Ledger vs Auditor's Arsenal | 81.1% | 64.1% | 16.2 ± 2.1 | 0.0% | 0.0% | 84.5% | 48.3% | 2.4% | 5.6 | 11.3 | 7.5 | 1.0 (13.0%) | 5.1 | 4.0 | 2.7% | 0 |
| Sparkwrench Scrappers mirror | 52.6% | 74.6% | 19.7 ± 3.5 | 5.4% | 0.0% | 97.1% | 88.0% | 22.0% | 15.8 | 0.0 | 3.9 | 6.4 (6.5%) | 5.6 | 5.7 | 26.9% | 0 |
| Sparkwrench Scrappers vs Auditor's Arsenal | 77.6% | 71.2% | 15.2 ± 2.0 | 0.0% | 0.0% | 81.5% | 50.6% | 14.3% | 5.0 | 0.7 | 1.8 | 1.3 (1.2%) | 3.5 | 3.3 | 0.7% | 0 |
| Auditor's Arsenal mirror | 52.9% | 79.1% | 15.9 ± 1.8 | 0.0% | 0.0% | 68.6% | 35.1% | 6.9% | 2.5 | 0.9 | 1.2 | 0.0 (71.4%) | 6.0 | 2.3 | 0.4% | 0 |

## Bot play styles

*Do the results hold with a defensive player? Control blocks freely, keeps blockers home and saves Gold. In "Greedy vs Control", A win% is the Greedy side.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, Control vs Control | 51.5% | 85.3% | 14.4 ± 2.2 | 0.0% | 1.4% | 94.1% | 81.2% | 13.9% | 18.7 | 0.0 | 3.3 | 2.2 (14.8%) | 0.0 | 1.9 | 4.8% | 0 |
| Jungle Stampede mirror, Control vs Control | 52.4% | 76.6% | 22.2 ± 4.6 | 20.0% | 0.0% | 92.3% | 77.8% | 9.6% | 19.5 | 2.5 | 10.1 | 0.8 (61.6%) | 1.1 | 4.3 | 28.1% | 0 |
| Zoo Patrol mirror, Control vs Control | 51.8% | 67.6% | 23.6 ± 4.5 | 32.6% | 0.0% | 75.3% | 74.8% | 30.8% | 14.6 | 17.6 | 10.5 | 7.8 (23.0%) | 0.0 | 7.0 | 29.0% | 0 |
| Vesper's Ledger mirror, Control vs Control | 49.9% | 60.7% | 22.5 ± 4.4 | 23.4% | 0.0% | 94.3% | 74.5% | 3.1% | 14.7 | 33.5 | 26.2 | 6.1 (12.7%) | 5.3 | 7.9 | 14.1% | 0 |
| Sparkwrench Scrappers mirror, Control vs Control | 51.4% | 68.0% | 25.1 ± 5.4 | 43.8% | 0.0% | 98.3% | 91.9% | 22.8% | 23.9 | 0.0 | 5.5 | 11.0 (6.9%) | 11.1 | 8.8 | 50.5% | 0 |
| Auditor's Arsenal mirror, Control vs Control | 50.8% | 72.4% | 19.1 ± 3.0 | 3.0% | 0.0% | 74.0% | 57.6% | 20.3% | 5.6 | 3.3 | 2.0 | 0.1 (61.1%) | 13.8 | 3.4 | 3.4% | 0 |
| Goober Mob mirror, Greedy vs Control | 56.9% | 84.3% | 13.2 ± 1.9 | 0.0% | 2.7% | 93.8% | 78.5% | 10.5% | 14.7 | 0.0 | 3.2 | 1.7 (18.8%) | 0.0 | 1.9 | 2.4% | 0 |
| Jungle Stampede mirror, Greedy vs Control | 49.6% | 83.4% | 19.7 ± 3.6 | 7.0% | 0.0% | 92.9% | 76.8% | 6.1% | 15.8 | 1.3 | 8.6 | 0.5 (59.4%) | 0.9 | 3.1 | 16.1% | 0 |
| Zoo Patrol mirror, Greedy vs Control | 53.8% | 68.2% | 21.4 ± 3.9 | 14.0% | 0.0% | 74.7% | 70.3% | 29.6% | 12.4 | 14.7 | 8.9 | 6.6 (19.2%) | 0.0 | 5.6 | 20.5% | 0 |
| Vesper's Ledger mirror, Greedy vs Control | 54.0% | 61.6% | 22.0 ± 4.3 | 18.4% | 0.0% | 93.9% | 71.9% | 2.8% | 14.5 | 33.3 | 25.4 | 5.6 (12.5%) | 5.0 | 7.6 | 15.9% | 0 |
| Sparkwrench Scrappers mirror, Greedy vs Control | 48.8% | 68.2% | 22.2 ± 4.3 | 21.5% | 0.0% | 97.8% | 90.6% | 22.8% | 19.8 | 0.0 | 4.6 | 8.6 (7.1%) | 8.0 | 7.2 | 40.0% | 0 |
| Auditor's Arsenal mirror, Greedy vs Control | 59.5% | 76.9% | 17.6 ± 2.5 | 0.4% | 0.0% | 71.9% | 52.2% | 17.4% | 4.1 | 1.9 | 1.5 | 0.0 (73.9%) | 9.6 | 2.8 | 0.3% | 0 |

## Game length lever: starting life

*Can starting life even out game length between fast and slow decks? Compare with the 30-life mirrors in the round robin.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, 25 life | 50.1% | 83.5% | 11.2 ± 1.5 | 0.0% | 17.4% | 92.3% | 67.0% | 6.7% | 9.9 | 0.0 | 2.9 | 1.1 (25.7%) | 0.0 | 1.7 | 0.2% | 0 |
| Jungle Stampede mirror, 25 life | 51.2% | 89.4% | 16.1 ± 2.9 | 0.8% | 0.0% | 93.6% | 69.2% | 1.2% | 10.5 | 0.2 | 6.4 | 0.3 (60.7%) | 0.4 | 1.4 | 5.3% | 0 |
| Zoo Patrol mirror, 25 life | 52.0% | 70.0% | 18.3 ± 3.1 | 2.4% | 0.0% | 74.4% | 65.4% | 25.7% | 9.1 | 10.1 | 6.9 | 4.6 (15.0%) | 0.0 | 4.1 | 9.9% | 0 |
| Vesper's Ledger mirror, 25 life | 51.9% | 62.3% | 19.1 ± 3.5 | 5.1% | 0.0% | 94.3% | 65.2% | 2.5% | 11.7 | 26.0 | 20.8 | 3.7 (13.5%) | 3.6 | 6.6 | 15.7% | 0 |
| Sparkwrench Scrappers mirror, 25 life | 51.4% | 76.4% | 18.1 ± 3.3 | 2.3% | 0.1% | 96.7% | 86.1% | 22.0% | 13.4 | 0.0 | 3.3 | 5.2 (6.7%) | 4.3 | 5.4 | 18.6% | 0 |
| Auditor's Arsenal mirror, 25 life | 51.9% | 79.9% | 15.1 ± 1.7 | 0.0% | 0.0% | 67.9% | 36.1% | 7.3% | 2.4 | 0.8 | 1.1 | 0.0 (66.7%) | 5.1 | 2.2 | 0.0% | 0 |
| Goober Mob mirror, 35 life | 51.3% | 84.5% | 13.0 ± 1.6 | 0.0% | 2.4% | 93.5% | 71.5% | 7.2% | 13.2 | 0.0 | 3.9 | 1.7 (23.1%) | 0.0 | 2.0 | 2.8% | 0 |
| Jungle Stampede mirror, 35 life | 50.4% | 86.0% | 18.3 ± 3.3 | 2.7% | 0.0% | 93.3% | 77.5% | 0.7% | 13.2 | 0.2 | 7.7 | 0.4 (62.6%) | 0.8 | 1.7 | 11.6% | 0 |
| Zoo Patrol mirror, 35 life | 50.6% | 70.2% | 20.8 ± 3.5 | 10.2% | 0.0% | 72.9% | 64.6% | 26.0% | 11.5 | 14.1 | 8.7 | 6.5 (17.7%) | 0.0 | 4.8 | 18.5% | 0 |
| Vesper's Ledger mirror, 35 life | 52.7% | 59.9% | 22.3 ± 4.0 | 20.2% | 0.0% | 94.6% | 66.7% | 2.3% | 15.6 | 35.9 | 26.7 | 5.9 (11.7%) | 5.6 | 7.7 | 15.0% | 0 |
| Sparkwrench Scrappers mirror, 35 life | 52.8% | 71.6% | 21.2 ± 3.9 | 13.6% | 0.0% | 97.2% | 88.5% | 21.8% | 17.9 | 0.0 | 4.5 | 7.7 (6.3%) | 6.9 | 6.1 | 34.3% | 0 |
| Auditor's Arsenal mirror, 35 life | 50.9% | 77.9% | 16.6 ± 1.7 | 0.0% | 0.0% | 69.0% | 40.4% | 6.8% | 2.7 | 0.8 | 1.3 | 0.0 (71.4%) | 6.9 | 2.3 | 0.4% | 0 |

## Permanent damage (GAME_DESIGN §7.3)

*What changes when damage wears off at end of turn like in MTG? Compare with the round-robin mirrors (permanent damage).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, damage wears off (MTG) | 50.1% | 81.7% | 11.9 ± 1.5 | 0.0% | 7.1% | 89.4% | 0.0% | 0.0% | 11.0 | 0.0 | 3.6 | 1.3 (24.4%) | 0.0 | 1.9 | 0.6% | 0 |
| Jungle Stampede mirror, damage wears off (MTG) | 51.4% | 86.8% | 17.2 ± 3.0 | 1.2% | 0.0% | 92.2% | 0.0% | 0.0% | 11.7 | 0.0 | 7.0 | 0.3 (55.5%) | 0.6 | 1.5 | 7.9% | 0 |
| Zoo Patrol mirror, damage wears off (MTG) | 51.3% | 68.3% | 17.5 ± 2.5 | 1.0% | 0.0% | 70.5% | 0.0% | 0.0% | 8.5 | 4.6 | 6.6 | 1.9 (29.1%) | 0.0 | 3.2 | 6.2% | 0 |
| Vesper's Ledger mirror, damage wears off (MTG) | 49.8% | 59.4% | 20.9 ± 3.9 | 13.3% | 0.0% | 90.9% | 0.0% | 0.0% | 13.7 | 32.1 | 24.1 | 4.8 (12.9%) | 4.8 | 7.2 | 16.6% | 0 |
| Sparkwrench Scrappers mirror, damage wears off (MTG) | 50.7% | 78.7% | 18.2 ± 3.0 | 1.6% | 0.0% | 93.9% | 0.0% | 0.0% | 13.2 | 0.0 | 3.3 | 4.8 (5.5%) | 4.1 | 4.9 | 18.3% | 0 |
| Auditor's Arsenal mirror, damage wears off (MTG) | 51.1% | 78.7% | 15.8 ± 1.7 | 0.0% | 0.0% | 60.2% | 0.0% | 0.0% | 2.1 | 0.2 | 1.2 | 0.0 (100.0%) | 5.7 | 2.2 | 0.0% | 0 |

## Going second (GAME_DESIGN §3)

*First-player win rate in mirrors (50% is fair). Current rule (MTG default): the first player skips their turn-1 draw, no other compensation.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, current rule (MTG) | 49.5% | 84.3% | 12.1 ± 1.6 | 0.0% | 6.6% | 92.7% | 67.8% | 7.1% | 11.5 | 0.0 | 3.4 | 1.4 (23.8%) | 0.0 | 1.9 | 1.8% | 0 |
| Goober Mob mirror, everyone draws + 2nd player +1 mana on turn 1 | 50.3% | 76.3% | 12.2 ± 1.6 | 0.0% | 6.0% | 93.2% | 68.7% | 6.9% | 12.2 | 0.0 | 3.1 | 1.4 (23.7%) | 0.0 | 2.1 | 0.5% | 0 |
| Goober Mob mirror, everyone draws + 2nd player +1 mana and 1 Gold | 50.0% | 64.0% | 12.3 ± 1.6 | 0.0% | 5.3% | 93.8% | 74.2% | 7.4% | 12.9 | 0.0 | 4.0 | 1.6 (24.5%) | 0.0 | 2.0 | 0.1% | 0 |
| Jungle Stampede mirror, current rule (MTG) | 51.1% | 87.3% | 17.2 ± 3.1 | 1.3% | 0.0% | 93.3% | 72.5% | 0.9% | 11.8 | 0.1 | 7.0 | 0.3 (57.6%) | 0.6 | 1.5 | 8.2% | 0 |
| Jungle Stampede mirror, everyone draws + 2nd player +1 mana on turn 1 | 52.8% | 81.0% | 17.2 ± 2.9 | 0.9% | 0.0% | 93.2% | 73.1% | 0.9% | 12.0 | 0.1 | 7.5 | 0.4 (63.7%) | 0.5 | 1.6 | 7.9% | 0 |
| Jungle Stampede mirror, everyone draws + 2nd player +1 mana and 1 Gold | 52.6% | 77.4% | 17.1 ± 2.8 | 0.5% | 0.0% | 93.1% | 72.9% | 0.9% | 11.8 | 0.1 | 8.0 | 0.4 (66.8%) | 0.6 | 1.4 | 8.6% | 0 |
| Vesper's Ledger mirror, current rule (MTG) | 52.6% | 60.4% | 20.9 ± 3.9 | 12.4% | 0.0% | 94.2% | 63.7% | 2.4% | 13.6 | 31.6 | 23.9 | 4.9 (12.7%) | 4.5 | 7.2 | 16.0% | 0 |
| Vesper's Ledger mirror, everyone draws + 2nd player +1 mana on turn 1 | 50.1% | 62.7% | 20.8 ± 3.9 | 11.5% | 0.0% | 94.2% | 61.6% | 2.4% | 13.8 | 32.6 | 24.1 | 4.6 (12.5%) | 4.6 | 7.3 | 18.2% | 0 |
| Vesper's Ledger mirror, everyone draws + 2nd player +1 mana and 1 Gold | 50.0% | 60.2% | 20.5 ± 3.9 | 10.2% | 0.0% | 94.0% | 62.1% | 2.5% | 13.6 | 31.7 | 24.7 | 4.7 (12.8%) | 4.6 | 6.9 | 15.2% | 0 |

## Mana model: round pool (GAME_DESIGN §5)

*Does the round pool (everyone refills when a round starts, mana usable on any turn of the round, unspent mana banked at the end of the round) take away the first player's edge? Watch 1st win% (50% is fair).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, mana per turn (current) | 49.5% | 84.3% | 12.1 ± 1.6 | 0.0% | 6.6% | 92.7% | 67.8% | 7.1% | 11.5 | 0.0 | 3.4 | 1.4 (23.8%) | 0.0 | 1.9 | 1.8% | 0 |
| Goober Mob mirror, round pool | 49.6% | 85.2% | 12.3 ± 1.6 | 0.0% | 6.0% | 92.8% | 67.0% | 6.2% | 11.9 | 0.0 | 3.0 | 2.0 (35.1%) | 0.0 | 2.5 | 0.7% | 0 |
| Goober Mob mirror, round pool + Gold first off-turn | 50.5% | 84.9% | 12.3 ± 1.6 | 0.0% | 5.6% | 92.7% | 66.1% | 6.4% | 11.9 | 0.0 | 3.2 | 2.0 (36.0%) | 0.0 | 2.7 | 0.7% | 0 |
| Goober Mob mirror, rotating first player | 50.6% | 48.4% | 12.0 ± 1.6 | 0.0% | 6.5% | 93.9% | 75.1% | 3.8% | 10.2 | 0.0 | 3.7 | 1.4 (23.9%) | 0.0 | 1.5 | 1.6% | 0 |
| Goober Mob mirror, rotating first player + round pool + Gold first | 49.7% | 46.7% | 12.3 ± 1.5 | 0.0% | 3.5% | 94.1% | 77.1% | 3.8% | 11.2 | 0.0 | 3.6 | 2.3 (45.7%) | 0.0 | 2.8 | 0.7% | 0 |
| Goober Mob mirror, 1st player skips first mana | 49.2% | 13.8% | 13.1 ± 1.7 | 0.0% | 0.5% | 93.2% | 70.5% | 6.7% | 11.5 | 0.0 | 3.4 | 1.3 (22.6%) | 0.0 | 2.0 | 1.4% | 0 |
| Goober Mob mirror, 1st player skips first mana + round pool + Gold first | 50.5% | 18.5% | 13.4 ± 1.7 | 0.0% | 0.1% | 93.2% | 68.6% | 6.4% | 12.3 | 0.0 | 3.3 | 1.9 (37.9%) | 0.0 | 2.6 | 0.8% | 0 |
| Jungle Stampede mirror, mana per turn (current) | 51.1% | 87.3% | 17.2 ± 3.1 | 1.3% | 0.0% | 93.3% | 72.5% | 0.9% | 11.8 | 0.1 | 7.0 | 0.3 (57.6%) | 0.6 | 1.5 | 8.2% | 0 |
| Jungle Stampede mirror, round pool | 50.4% | 87.6% | 16.7 ± 3.0 | 0.9% | 0.0% | 93.4% | 70.7% | 0.8% | 10.9 | 0.1 | 6.0 | 0.1 (35.8%) | 0.4 | 1.1 | 5.1% | 0 |
| Jungle Stampede mirror, round pool + Gold first off-turn | 52.0% | 87.4% | 17.2 ± 3.1 | 1.4% | 0.0% | 93.4% | 66.6% | 0.7% | 11.8 | 0.2 | 6.9 | 0.3 (60.8%) | 0.6 | 1.6 | 6.1% | 0 |
| Jungle Stampede mirror, rotating first player | 51.1% | 54.7% | 16.9 ± 2.6 | 0.5% | 0.0% | 93.4% | 60.7% | 0.7% | 10.3 | 0.1 | 6.8 | 0.2 (67.6%) | 0.6 | 1.3 | 5.2% | 0 |
| Jungle Stampede mirror, rotating first player + round pool + Gold first | 51.1% | 55.3% | 16.8 ± 2.6 | 0.8% | 0.0% | 93.6% | 66.8% | 0.6% | 10.3 | 0.1 | 6.7 | 0.3 (69.9%) | 0.6 | 1.5 | 4.7% | 0 |
| Jungle Stampede mirror, 1st player skips first mana | 49.0% | 12.4% | 18.1 ± 2.8 | 1.6% | 0.0% | 93.0% | 69.0% | 0.9% | 11.7 | 0.2 | 6.9 | 0.3 (63.3%) | 0.5 | 1.6 | 6.7% | 0 |
| Jungle Stampede mirror, 1st player skips first mana + round pool + Gold first | 49.3% | 12.3% | 18.0 ± 2.7 | 1.0% | 0.0% | 93.0% | 66.8% | 0.8% | 11.6 | 0.2 | 6.5 | 0.4 (66.1%) | 0.5 | 1.5 | 6.2% | 0 |
| Zoo Patrol mirror, mana per turn (current) | 50.9% | 71.1% | 19.5 ± 3.3 | 4.8% | 0.0% | 73.8% | 66.0% | 25.9% | 10.4 | 12.1 | 7.8 | 5.5 (16.3%) | 0.0 | 4.4 | 13.0% | 0 |
| Zoo Patrol mirror, round pool | 48.8% | 60.0% | 19.8 ± 3.1 | 5.4% | 0.0% | 68.2% | 56.6% | 24.7% | 10.0 | 15.3 | 6.2 | 7.1 (26.7%) | 0.0 | 6.0 | 8.1% | 0 |
| Zoo Patrol mirror, round pool + Gold first off-turn | 49.7% | 58.3% | 19.8 ± 3.1 | 4.6% | 0.0% | 68.1% | 56.3% | 24.0% | 10.1 | 15.7 | 8.2 | 7.4 (28.8%) | 0.0 | 6.4 | 7.1% | 0 |
| Zoo Patrol mirror, rotating first player | 52.2% | 53.2% | 19.1 ± 3.0 | 3.8% | 0.0% | 75.4% | 65.9% | 26.0% | 9.9 | 10.5 | 7.4 | 4.5 (21.3%) | 0.0 | 2.6 | 12.5% | 0 |
| Zoo Patrol mirror, rotating first player + round pool + Gold first | 50.1% | 47.7% | 19.3 ± 2.8 | 2.7% | 0.0% | 69.4% | 58.8% | 23.7% | 9.6 | 14.0 | 7.8 | 6.4 (37.7%) | 0.0 | 6.0 | 6.6% | 0 |
| Zoo Patrol mirror, 1st player skips first mana | 49.7% | 33.7% | 20.7 ± 3.4 | 9.3% | 0.0% | 74.0% | 64.9% | 26.3% | 10.7 | 12.2 | 7.6 | 5.6 (16.0%) | 0.0 | 4.6 | 13.8% | 0 |
| Zoo Patrol mirror, 1st player skips first mana + round pool + Gold first | 46.2% | 26.0% | 20.1 ± 3.1 | 5.6% | 0.0% | 68.1% | 56.1% | 24.1% | 9.5 | 14.3 | 7.4 | 6.6 (26.4%) | 0.0 | 5.6 | 6.7% | 0 |
| Vesper's Ledger mirror, mana per turn (current) | 52.6% | 60.4% | 20.9 ± 3.9 | 12.4% | 0.0% | 94.2% | 63.7% | 2.4% | 13.6 | 31.6 | 23.9 | 4.9 (12.7%) | 4.5 | 7.2 | 16.0% | 0 |
| Vesper's Ledger mirror, round pool | 48.3% | 57.5% | 21.6 ± 3.9 | 16.8% | 0.0% | 94.6% | 69.4% | 2.2% | 13.7 | 32.2 | 22.0 | 4.7 (11.3%) | 4.7 | 7.1 | 21.9% | 0 |
| Vesper's Ledger mirror, round pool + Gold first off-turn | 49.1% | 52.1% | 21.5 ± 3.9 | 15.6% | 0.0% | 94.5% | 68.2% | 2.3% | 13.7 | 32.3 | 24.7 | 5.3 (14.9%) | 4.9 | 8.1 | 11.3% | 0 |
| Vesper's Ledger mirror, rotating first player | 45.7% | 41.3% | 19.7 ± 3.5 | 5.4% | 0.0% | 93.1% | 57.7% | 1.8% | 11.7 | 28.8 | 21.2 | 4.2 (25.8%) | 4.3 | 5.8 | 16.0% | 0 |
| Vesper's Ledger mirror, rotating first player + round pool + Gold first | 51.2% | 42.8% | 19.9 ± 3.4 | 6.4% | 0.0% | 93.8% | 64.8% | 1.6% | 10.8 | 27.9 | 20.8 | 4.7 (26.1%) | 4.2 | 7.3 | 7.8% | 0 |
| Vesper's Ledger mirror, 1st player skips first mana | 48.4% | 31.8% | 21.6 ± 4.2 | 17.7% | 0.0% | 94.4% | 65.0% | 2.4% | 13.4 | 31.1 | 23.4 | 4.6 (12.8%) | 4.5 | 7.2 | 17.4% | 0 |
| Vesper's Ledger mirror, 1st player skips first mana + round pool + Gold first | 49.1% | 27.3% | 21.5 ± 4.0 | 16.0% | 0.0% | 94.2% | 64.5% | 2.1% | 12.6 | 30.4 | 23.3 | 4.6 (15.5%) | 4.5 | 7.6 | 12.1% | 0 |
| Sparkwrench Scrappers mirror, mana per turn (current) | 52.6% | 74.6% | 19.7 ± 3.5 | 5.4% | 0.0% | 97.1% | 88.0% | 22.0% | 15.8 | 0.0 | 3.9 | 6.4 (6.5%) | 5.6 | 5.7 | 26.9% | 0 |
| Sparkwrench Scrappers mirror, round pool | 52.1% | 75.3% | 19.8 ± 3.6 | 6.0% | 0.0% | 96.9% | 86.4% | 22.3% | 15.6 | 0.0 | 3.6 | 7.2 (13.9%) | 5.7 | 6.9 | 21.4% | 0 |
| Sparkwrench Scrappers mirror, round pool + Gold first off-turn | 51.5% | 75.7% | 19.7 ± 3.6 | 5.9% | 0.0% | 97.0% | 86.9% | 22.1% | 15.6 | 0.0 | 3.9 | 7.2 (14.3%) | 5.4 | 6.8 | 20.9% | 0 |
| Sparkwrench Scrappers mirror, rotating first player | 48.6% | 51.8% | 18.8 ± 2.9 | 1.5% | 0.0% | 96.1% | 86.1% | 20.6% | 13.8 | 0.0 | 3.6 | 5.5 (5.0%) | 4.5 | 2.8 | 19.1% | 0 |
| Sparkwrench Scrappers mirror, rotating first player + round pool + Gold first | 49.3% | 48.5% | 19.2 ± 3.1 | 2.1% | 0.0% | 96.1% | 85.2% | 21.4% | 14.2 | 0.0 | 3.9 | 6.6 (15.4%) | 4.8 | 4.5 | 14.5% | 0 |
| Sparkwrench Scrappers mirror, 1st player skips first mana | 47.3% | 23.9% | 20.8 ± 3.7 | 11.2% | 0.0% | 97.0% | 87.3% | 21.7% | 16.0 | 0.0 | 3.7 | 6.5 (5.8%) | 5.4 | 5.6 | 29.0% | 0 |
| Sparkwrench Scrappers mirror, 1st player skips first mana + round pool + Gold first | 47.6% | 30.2% | 21.3 ± 3.7 | 14.6% | 0.0% | 97.3% | 88.6% | 22.4% | 16.5 | 0.0 | 3.6 | 8.2 (13.3%) | 5.6 | 6.5 | 23.0% | 0 |
| Auditor's Arsenal mirror, mana per turn (current) | 52.9% | 79.1% | 15.9 ± 1.8 | 0.0% | 0.0% | 68.6% | 35.1% | 6.9% | 2.5 | 0.9 | 1.2 | 0.0 (71.4%) | 6.0 | 2.3 | 0.4% | 0 |
| Auditor's Arsenal mirror, round pool | 50.8% | 73.6% | 16.2 ± 1.9 | 0.0% | 0.0% | 69.5% | 39.2% | 7.6% | 2.5 | 0.8 | 1.5 | 0.0 (55.6%) | 6.8 | 2.7 | 0.0% | 0 |
| Auditor's Arsenal mirror, round pool + Gold first off-turn | 50.6% | 73.8% | 16.2 ± 1.9 | 0.0% | 0.0% | 69.6% | 39.1% | 7.6% | 2.5 | 0.8 | 1.6 | 0.0 (80.0%) | 6.8 | 2.7 | 0.0% | 0 |
| Auditor's Arsenal mirror, rotating first player | 50.5% | 50.1% | 15.7 ± 1.6 | 0.0% | 0.0% | 66.8% | 32.5% | 6.4% | 2.1 | 0.5 | 1.1 | 0.0 (72.7%) | 5.4 | 1.1 | 0.0% | 0 |
| Auditor's Arsenal mirror, rotating first player + round pool + Gold first | 49.9% | 48.5% | 16.2 ± 1.8 | 0.0% | 0.0% | 69.9% | 37.2% | 7.7% | 2.3 | 0.5 | 2.0 | 0.0 (44.4%) | 6.8 | 2.9 | 0.2% | 0 |
| Auditor's Arsenal mirror, 1st player skips first mana | 50.4% | 22.2% | 16.9 ± 1.8 | 0.0% | 0.0% | 69.0% | 36.2% | 6.5% | 2.5 | 0.8 | 1.0 | 0.0 (80.0%) | 5.8 | 2.4 | 0.0% | 0 |
| Auditor's Arsenal mirror, 1st player skips first mana + round pool + Gold first | 47.6% | 19.2% | 16.9 ± 1.9 | 0.0% | 0.0% | 69.6% | 38.4% | 7.1% | 2.4 | 0.7 | 1.6 | 0.0 (69.2%) | 6.5 | 2.9 | 0.1% | 0 |

## Runeterra-style mana

*Legends of Runeterra's mana in full turns: everyone refills when a round starts, unspent mana becomes Gold (spell mana) at the end of the round, spells and abilities spend Gold first, the round leader alternates and only they may attack (attack token), creatures can attack the turn they arrive. Watch Turns, Off-turn and 1st win%.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, today's rules | 49.5% | 84.3% | 12.1 ± 1.6 | 0.0% | 6.6% | 92.7% | 67.8% | 7.1% | 11.5 | 0.0 | 3.4 | 1.4 (23.8%) | 0.0 | 1.9 | 1.8% | 0 |
| Goober Mob mirror, Runeterra | 50.0% | 46.2% | 13.9 ± 1.6 | 0.0% | 0.1% | 94.0% | 69.9% | 3.4% | 12.7 | 0.0 | 4.1 | 3.2 (45.3%) | 0.0 | 3.5 | 2.3% | 0 |
| Goober Mob mirror, Runeterra, Gold cap 5 | 50.3% | 46.7% | 13.8 ± 1.6 | 0.0% | 0.1% | 93.9% | 70.6% | 3.4% | 12.9 | 0.0 | 4.3 | 3.1 (45.3%) | 0.0 | 3.5 | 1.4% | 0 |
| Goober Mob mirror, Runeterra, no summoning sickness | 50.0% | 48.6% | 12.3 ± 1.2 | 0.0% | 1.2% | 95.5% | 40.0% | 0.1% | 8.8 | 0.0 | 3.9 | 2.5 (51.8%) | 0.0 | 3.1 | 0.9% | 0 |
| Goober Mob mirror, Runeterra without the attack token | 50.3% | 48.7% | 12.3 ± 1.5 | 0.0% | 3.9% | 94.3% | 76.0% | 3.9% | 11.1 | 0.0 | 3.7 | 2.4 (44.7%) | 0.0 | 2.9 | 1.1% | 0 |
| Jungle Stampede mirror, today's rules | 51.1% | 87.3% | 17.2 ± 3.1 | 1.3% | 0.0% | 93.3% | 72.5% | 0.9% | 11.8 | 0.1 | 7.0 | 0.3 (57.6%) | 0.6 | 1.5 | 8.2% | 0 |
| Jungle Stampede mirror, Runeterra | 47.2% | 45.6% | 18.8 ± 2.7 | 0.9% | 0.0% | 93.5% | 68.8% | 0.7% | 12.8 | 0.3 | 9.1 | 0.3 (55.7%) | 0.9 | 2.3 | 17.8% | 0 |
| Jungle Stampede mirror, Runeterra, Gold cap 5 | 48.0% | 46.8% | 18.8 ± 2.6 | 0.8% | 0.0% | 93.5% | 65.0% | 0.7% | 12.8 | 0.3 | 9.7 | 0.3 (63.0%) | 0.9 | 2.3 | 9.4% | 0 |
| Jungle Stampede mirror, Runeterra, no summoning sickness | 48.8% | 31.4% | 11.6 ± 0.9 | 0.0% | 0.0% | 91.9% | 0.0% | 0.0% | 0.9 | 0.0 | 4.3 | 0.0 (100.0%) | 0.0 | 0.2 | 8.9% | 0 |
| Jungle Stampede mirror, Runeterra without the attack token | 51.3% | 55.9% | 17.1 ± 2.8 | 1.1% | 0.0% | 93.8% | 66.2% | 0.6% | 10.9 | 0.2 | 7.4 | 0.3 (63.9%) | 0.7 | 2.0 | 13.6% | 0 |
| Zoo Patrol mirror, today's rules | 50.9% | 71.1% | 19.5 ± 3.3 | 4.8% | 0.0% | 73.8% | 66.0% | 25.9% | 10.4 | 12.1 | 7.8 | 5.5 (16.3%) | 0.0 | 4.4 | 13.0% | 0 |
| Zoo Patrol mirror, Runeterra | 48.9% | 50.7% | 22.1 ± 2.9 | 9.5% | 0.0% | 68.6% | 61.6% | 24.3% | 12.5 | 19.3 | 11.8 | 8.7 (39.0%) | 0.0 | 7.2 | 18.2% | 0 |
| Zoo Patrol mirror, Runeterra, Gold cap 5 | 47.9% | 51.3% | 22.1 ± 2.8 | 9.2% | 0.0% | 68.7% | 61.9% | 24.2% | 12.5 | 19.2 | 12.2 | 8.6 (39.0%) | 0.0 | 7.1 | 13.3% | 0 |
| Zoo Patrol mirror, Runeterra, no summoning sickness | 51.9% | 49.1% | 17.9 ± 2.0 | 0.3% | 0.0% | 68.4% | 56.1% | 23.8% | 7.8 | 11.5 | 8.7 | 5.3 (37.1%) | 0.0 | 5.5 | 5.1% | 0 |
| Zoo Patrol mirror, Runeterra without the attack token | 48.3% | 46.9% | 19.3 ± 2.9 | 2.8% | 0.0% | 70.1% | 59.6% | 23.6% | 9.8 | 13.6 | 9.7 | 6.2 (36.7%) | 0.0 | 5.9 | 10.6% | 0 |
| Vesper's Ledger mirror, today's rules | 52.6% | 60.4% | 20.9 ± 3.9 | 12.4% | 0.0% | 94.2% | 63.7% | 2.4% | 13.6 | 31.6 | 23.9 | 4.9 (12.7%) | 4.5 | 7.2 | 16.0% | 0 |
| Vesper's Ledger mirror, Runeterra | 49.6% | 40.8% | 22.8 ± 3.8 | 21.3% | 0.0% | 92.8% | 68.0% | 1.6% | 13.5 | 34.0 | 26.1 | 6.0 (19.1%) | 6.1 | 8.4 | 16.6% | 0 |
| Vesper's Ledger mirror, Runeterra, Gold cap 5 | 50.2% | 43.6% | 22.7 ± 3.5 | 19.7% | 0.0% | 92.8% | 71.0% | 1.5% | 13.7 | 33.8 | 27.9 | 5.5 (23.6%) | 6.6 | 8.7 | 9.8% | 0 |
| Vesper's Ledger mirror, Runeterra, no summoning sickness | 49.3% | 46.3% | 17.2 ± 2.5 | 0.3% | 0.0% | 85.1% | 57.4% | 0.3% | 5.9 | 22.9 | 15.6 | 3.5 (20.6%) | 2.8 | 5.6 | 6.6% | 0 |
| Vesper's Ledger mirror, Runeterra without the attack token | 53.1% | 45.1% | 19.8 ± 3.4 | 5.2% | 0.0% | 93.4% | 64.3% | 1.8% | 10.9 | 27.6 | 21.8 | 5.0 (21.6%) | 4.0 | 7.2 | 12.6% | 0 |
| Sparkwrench Scrappers mirror, today's rules | 52.6% | 74.6% | 19.7 ± 3.5 | 5.4% | 0.0% | 97.1% | 88.0% | 22.0% | 15.8 | 0.0 | 3.9 | 6.4 (6.5%) | 5.6 | 5.7 | 26.9% | 0 |
| Sparkwrench Scrappers mirror, Runeterra | 51.4% | 50.4% | 21.6 ± 3.1 | 8.1% | 0.0% | 96.2% | 86.0% | 21.4% | 16.8 | 0.0 | 6.2 | 8.5 (18.0%) | 5.6 | 5.4 | 37.7% | 0 |
| Sparkwrench Scrappers mirror, Runeterra, Gold cap 5 | 51.5% | 50.3% | 21.5 ± 3.0 | 7.7% | 0.0% | 96.1% | 86.0% | 21.4% | 16.8 | 0.0 | 6.6 | 8.5 (18.1%) | 5.6 | 5.3 | 26.4% | 0 |
| Sparkwrench Scrappers mirror, Runeterra, no summoning sickness | 49.1% | 47.9% | 16.0 ± 1.9 | 0.0% | 0.0% | 89.3% | 65.1% | 24.0% | 7.3 | 0.0 | 3.9 | 3.8 (21.6%) | 2.8 | 3.9 | 3.7% | 0 |
| Sparkwrench Scrappers mirror, Runeterra without the attack token | 49.0% | 49.0% | 19.2 ± 3.1 | 2.7% | 0.0% | 96.2% | 85.6% | 21.4% | 14.3 | 0.0 | 4.6 | 6.6 (15.0%) | 4.6 | 4.5 | 21.5% | 0 |
| Auditor's Arsenal mirror, today's rules | 52.9% | 79.1% | 15.9 ± 1.8 | 0.0% | 0.0% | 68.6% | 35.1% | 6.9% | 2.5 | 0.9 | 1.2 | 0.0 (71.4%) | 6.0 | 2.3 | 0.4% | 0 |
| Auditor's Arsenal mirror, Runeterra | 47.0% | 49.0% | 18.7 ± 1.9 | 0.2% | 0.0% | 65.7% | 28.1% | 7.1% | 2.5 | 0.6 | 2.6 | 0.5 (20.0%) | 10.5 | 3.3 | 3.6% | 0 |
| Auditor's Arsenal mirror, Runeterra, Gold cap 5 | 47.4% | 49.4% | 18.7 ± 1.9 | 0.2% | 0.0% | 65.8% | 28.0% | 7.1% | 2.6 | 0.6 | 2.7 | 0.3 (16.6%) | 10.5 | 3.2 | 2.4% | 0 |
| Auditor's Arsenal mirror, Runeterra, no summoning sickness | 50.2% | 45.4% | 15.9 ± 1.7 | 0.0% | 0.0% | 59.0% | 25.0% | 6.1% | 1.1 | 0.2 | 2.1 | 0.2 (12.9%) | 6.0 | 3.0 | 0.0% | 0 |
| Auditor's Arsenal mirror, Runeterra without the attack token | 50.3% | 48.7% | 16.2 ± 1.8 | 0.0% | 0.0% | 70.4% | 37.2% | 7.6% | 2.3 | 0.5 | 2.2 | 0.2 (18.4%) | 6.7 | 3.0 | 0.8% | 0 |

## Tavern Dwellers (GAME_DESIGN §9)

*What do Tavern Dwellers (passives and Powers, the universal Gold sink) change? Each mirror with and without Tavern Dwellers. Watch Wasted (mana lost to the Gold cap), Gold spent and game length.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, with Tavern Dwellers | 49.5% | 84.3% | 12.1 ± 1.6 | 0.0% | 6.6% | 92.7% | 67.8% | 7.1% | 11.5 | 0.0 | 3.4 | 1.4 (23.8%) | 0.0 | 1.9 | 1.8% | 0 |
| Goober Mob mirror, no Tavern Dwellers | 49.8% | 86.4% | 13.1 ± 2.1 | 0.0% | 4.0% | 93.0% | 70.8% | 7.9% | 12.9 | 0.0 | 3.5 | 0.0 | 0.0 | 2.0 | 17.2% | 0 |
| Jungle Stampede mirror, with Tavern Dwellers | 51.1% | 87.3% | 17.2 ± 3.1 | 1.3% | 0.0% | 93.3% | 72.5% | 0.9% | 11.8 | 0.1 | 7.0 | 0.3 (57.6%) | 0.6 | 1.5 | 8.2% | 0 |
| Jungle Stampede mirror, no Tavern Dwellers | 52.4% | 82.0% | 16.7 ± 2.9 | 1.2% | 0.0% | 83.7% | 65.1% | 1.8% | 8.8 | 0.4 | 6.4 | 0.0 | 0.6 | 0.9 | 7.2% | 0 |
| Zoo Patrol mirror, with Tavern Dwellers | 50.9% | 71.1% | 19.5 ± 3.3 | 4.8% | 0.0% | 73.8% | 66.0% | 25.9% | 10.4 | 12.1 | 7.8 | 5.5 (16.3%) | 0.0 | 4.4 | 13.0% | 0 |
| Zoo Patrol mirror, no Tavern Dwellers | 50.3% | 69.3% | 20.9 ± 4.1 | 12.9% | 0.0% | 86.8% | 77.7% | 38.7% | 12.9 | 4.7 | 6.9 | 0.0 | 0.0 | 5.4 | 38.7% | 0 |
| Vesper's Ledger mirror, with Tavern Dwellers | 52.6% | 60.4% | 20.9 ± 3.9 | 12.4% | 0.0% | 94.2% | 63.7% | 2.4% | 13.6 | 31.6 | 23.9 | 4.9 (12.7%) | 4.5 | 7.2 | 16.0% | 0 |
| Vesper's Ledger mirror, no Tavern Dwellers | 50.2% | 59.8% | 22.0 ± 4.4 | 20.2% | 0.0% | 94.2% | 61.7% | 2.6% | 15.1 | 31.9 | 15.5 | 0.0 | 5.2 | 6.8 | 39.2% | 0 |
| Sparkwrench Scrappers mirror, with Tavern Dwellers | 52.6% | 74.6% | 19.7 ± 3.5 | 5.4% | 0.0% | 97.1% | 88.0% | 22.0% | 15.8 | 0.0 | 3.9 | 6.4 (6.5%) | 5.6 | 5.7 | 26.9% | 0 |
| Sparkwrench Scrappers mirror, no Tavern Dwellers | 51.5% | 74.7% | 19.7 ± 3.4 | 4.7% | 0.0% | 96.6% | 85.9% | 20.1% | 16.5 | 0.0 | 3.1 | 0.0 | 7.9 | 4.4 | 35.1% | 0 |
| Auditor's Arsenal mirror, with Tavern Dwellers | 52.9% | 79.1% | 15.9 ± 1.8 | 0.0% | 0.0% | 68.6% | 35.1% | 6.9% | 2.5 | 0.9 | 1.2 | 0.0 (71.4%) | 6.0 | 2.3 | 0.4% | 0 |
| Auditor's Arsenal mirror, no Tavern Dwellers | 51.9% | 77.5% | 16.7 ± 1.8 | 0.0% | 0.0% | 67.1% | 34.2% | 5.8% | 2.7 | 0.9 | 1.4 | 0.0 | 4.7 | 2.9 | 0.0% | 0 |

## Gold cap (GAME_DESIGN §5.2)

*Does the cap matter now that Tavern Dweller Powers and Equip spend Gold?*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Zoo Patrol mirror, Gold cap 3 | 51.6% | 71.2% | 19.5 ± 3.3 | 4.8% | 0.0% | 73.8% | 65.6% | 26.0% | 10.4 | 12.0 | 7.4 | 5.5 (15.7%) | 0.0 | 4.4 | 20.6% | 0 |
| Zoo Patrol mirror, Gold cap 5 | 50.9% | 71.1% | 19.5 ± 3.3 | 4.8% | 0.0% | 73.8% | 66.0% | 25.9% | 10.4 | 12.1 | 7.8 | 5.5 (16.3%) | 0.0 | 4.4 | 13.0% | 0 |
| Zoo Patrol mirror, Gold cap 8 | 51.0% | 70.8% | 19.5 ± 3.2 | 4.8% | 0.0% | 73.8% | 66.2% | 25.9% | 10.4 | 12.1 | 7.9 | 5.5 (16.1%) | 0.0 | 4.4 | 8.0% | 0 |
| Vesper's Ledger mirror, Gold cap 3 | 51.6% | 62.0% | 20.9 ± 3.9 | 12.7% | 0.0% | 94.1% | 63.2% | 2.6% | 13.5 | 31.4 | 21.4 | 4.9 (11.5%) | 4.5 | 6.9 | 22.1% | 0 |
| Vesper's Ledger mirror, Gold cap 5 | 52.6% | 60.4% | 20.9 ± 3.9 | 12.4% | 0.0% | 94.2% | 63.7% | 2.4% | 13.6 | 31.6 | 23.9 | 4.9 (12.7%) | 4.5 | 7.2 | 16.0% | 0 |
| Vesper's Ledger mirror, Gold cap 8 | 53.1% | 60.3% | 20.9 ± 3.9 | 12.8% | 0.0% | 94.3% | 64.4% | 2.4% | 13.7 | 32.0 | 24.4 | 4.9 (12.8%) | 4.6 | 7.2 | 10.9% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 3 | 52.6% | 74.4% | 19.7 ± 3.5 | 5.3% | 0.0% | 97.1% | 88.1% | 22.1% | 15.8 | 0.0 | 3.7 | 6.4 (6.4%) | 5.6 | 5.7 | 37.0% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 5 | 52.6% | 74.6% | 19.7 ± 3.5 | 5.4% | 0.0% | 97.1% | 88.0% | 22.0% | 15.8 | 0.0 | 3.9 | 6.4 (6.5%) | 5.6 | 5.7 | 26.9% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 8 | 52.8% | 74.4% | 19.7 ± 3.5 | 5.5% | 0.0% | 97.1% | 87.9% | 22.0% | 15.8 | 0.0 | 3.9 | 6.4 (6.5%) | 5.6 | 5.7 | 16.8% | 0 |
| Auditor's Arsenal mirror, Gold cap 3 | 52.9% | 78.9% | 15.9 ± 1.8 | 0.1% | 0.0% | 68.7% | 35.1% | 6.9% | 2.5 | 0.9 | 1.2 | 0.0 (80.0%) | 6.0 | 2.3 | 0.8% | 0 |
| Auditor's Arsenal mirror, Gold cap 5 | 52.9% | 79.1% | 15.9 ± 1.8 | 0.0% | 0.0% | 68.6% | 35.1% | 6.9% | 2.5 | 0.9 | 1.2 | 0.0 (71.4%) | 6.0 | 2.3 | 0.4% | 0 |
| Auditor's Arsenal mirror, Gold cap 8 | 52.9% | 79.1% | 15.9 ± 1.8 | 0.0% | 0.0% | 68.6% | 35.1% | 6.9% | 2.5 | 0.9 | 1.2 | 0.0 (50.0%) | 6.0 | 2.2 | 0.2% | 0 |
