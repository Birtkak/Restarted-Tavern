# Simulation Report

Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

500 games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time 20s.

Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) ± standard deviation · **Long** / **Short** = share of games over 25 / under 10 turns · **Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · **Chip→death** = share of *chip damage* (damage a creature carried into a later turn) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · **Wounded** = share of creatures carrying damage at the start of a turn · **Deaths** / **Heal** / **Gold spent** are per game · **Powers** = Tavern Dweller Powers used per game (in brackets: share used on an opponent's turn) · **Abil.** = other activated abilities per game (Equip, Tap abilities...) · **Wasted** = share of unspent mana lost to the Gold cap.

## Round robin (current rules, Greedy bots)

*How long are games, and does damage on creatures decide anything? Every deck against every deck.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror | 53.8% | 77.2% | 11.9 ± 2.0 | 0.0% | 17.4% | 93.2% | 69.9% | 5.7% | 10.4 | 0.0 | 3.4 | 1.9 (20.3%) | 0.0 | 1.2% | 0 |
| Goober Mob vs Jungle Stampede | 59.2% | 78.2% | 13.0 ± 2.0 | 0.0% | 8.0% | 91.4% | 61.6% | 5.2% | 9.1 | 0.4 | 3.7 | 1.3 (13.3%) | 0.0 | 1.8% | 0 |
| Goober Mob vs Zoo Patrol | 91.8% | 61.6% | 14.4 ± 2.4 | 0.0% | 2.2% | 86.0% | 61.3% | 15.8% | 11.1 | 2.8 | 4.2 | 2.2 (10.1%) | 0.0 | 3.7% | 0 |
| Goober Mob vs Vesper's Ledger | 88.4% | 61.0% | 14.7 ± 2.4 | 0.0% | 3.2% | 93.7% | 62.7% | 3.4% | 11.7 | 5.8 | 10.8 | 3.1 (25.2%) | 0.0 | 15.7% | 0 |
| Goober Mob vs Sparkwrench Scrappers | 81.0% | 70.4% | 13.6 ± 2.5 | 0.0% | 6.8% | 94.4% | 77.5% | 11.8% | 12.1 | 0.0 | 2.8 | 2.2 (11.1%) | 1.0 | 7.6% | 0 |
| Goober Mob vs Auditor's Arsenal | 97.8% | 56.8% | 11.8 ± 2.0 | 0.0% | 15.8% | 80.4% | 51.8% | 4.8% | 5.2 | 0.1 | 2.3 | 1.0 (16.0%) | 0.9 | 0.8% | 0 |
| Jungle Stampede mirror | 53.0% | 82.8% | 17.2 ± 3.2 | 1.4% | 0.0% | 93.6% | 74.3% | 0.9% | 12.5 | 0.3 | 6.0 | 0.5 (56.8%) | 0.0 | 21.3% | 0 |
| Jungle Stampede vs Zoo Patrol | 81.0% | 68.0% | 17.1 ± 3.6 | 2.2% | 0.0% | 84.4% | 66.6% | 21.1% | 9.7 | 4.6 | 5.8 | 1.3 (16.5%) | 0.0 | 18.1% | 0 |
| Jungle Stampede vs Vesper's Ledger | 90.0% | 59.4% | 15.3 ± 2.2 | 0.0% | 0.0% | 87.3% | 50.0% | 0.9% | 7.1 | 5.2 | 7.9 | 1.1 (18.9%) | 0.0 | 4.2% | 0 |
| Jungle Stampede vs Sparkwrench Scrappers | 76.4% | 71.8% | 15.1 ± 2.5 | 0.6% | 0.0% | 89.4% | 66.0% | 12.0% | 9.6 | 0.9 | 3.4 | 1.8 (5.0%) | 1.5 | 14.2% | 0 |
| Jungle Stampede vs Auditor's Arsenal | 96.8% | 57.4% | 13.4 ± 2.0 | 0.0% | 0.0% | 79.1% | 38.8% | 2.6% | 3.7 | 0.3 | 2.4 | 0.0 (37.5%) | 1.5 | 1.6% | 0 |
| Zoo Patrol mirror | 51.0% | 67.6% | 22.8 ± 5.0 | 28.0% | 0.0% | 74.5% | 72.7% | 30.3% | 12.6 | 17.9 | 12.3 | 8.2 (29.7%) | 0.0 | 45.4% | 0 |
| Zoo Patrol vs Vesper's Ledger | 91.2% | 57.8% | 21.5 ± 4.9 | 16.6% | 0.0% | 88.1% | 76.7% | 12.0% | 15.8 | 10.8 | 14.7 | 5.1 (14.7%) | 0.0 | 46.3% | 0 |
| Zoo Patrol vs Sparkwrench Scrappers | 74.4% | 65.0% | 19.7 ± 3.9 | 7.0% | 0.0% | 81.8% | 63.4% | 24.1% | 12.9 | 7.3 | 5.5 | 6.8 (9.5%) | 3.8 | 44.0% | 0 |
| Zoo Patrol vs Auditor's Arsenal | 69.0% | 71.2% | 16.9 ± 1.9 | 0.4% | 0.0% | 70.2% | 40.9% | 23.9% | 6.0 | 6.6 | 3.6 | 2.4 (10.2%) | 3.9 | 6.6% | 0 |
| Vesper's Ledger mirror | 47.2% | 56.6% | 23.9 ± 5.2 | 35.0% | 0.0% | 94.8% | 69.7% | 4.3% | 20.9 | 22.9 | 29.2 | 9.0 (16.9%) | 0.0 | 71.2% | 0 |
| Vesper's Ledger vs Sparkwrench Scrappers | 18.4% | 50.6% | 17.4 ± 3.5 | 4.2% | 0.0% | 94.0% | 76.4% | 9.9% | 12.4 | 5.8 | 10.8 | 4.6 (13.6%) | 2.1 | 51.7% | 0 |
| Vesper's Ledger vs Auditor's Arsenal | 77.2% | 62.2% | 16.3 ± 2.3 | 0.2% | 0.0% | 86.4% | 49.6% | 2.0% | 6.3 | 8.2 | 7.7 | 2.0 (11.0%) | 4.4 | 12.2% | 0 |
| Sparkwrench Scrappers mirror | 51.4% | 58.4% | 20.6 ± 5.2 | 15.8% | 0.2% | 98.4% | 90.4% | 22.4% | 18.7 | 0.0 | 4.4 | 7.9 (7.5%) | 8.8 | 63.5% | 0 |
| Sparkwrench Scrappers vs Auditor's Arsenal | 93.6% | 61.0% | 13.8 ± 2.0 | 0.0% | 0.8% | 81.8% | 58.4% | 13.0% | 5.0 | 0.6 | 1.4 | 1.2 (1.1%) | 3.1 | 5.5% | 0 |
| Auditor's Arsenal mirror | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 0.0% | 0 |

## Bot play styles

*Do the results hold with a defensive player? Control blocks freely, keeps blockers home and saves Gold. In "Greedy vs Control", A win% is the Greedy side.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, Control vs Control | 52.0% | 79.0% | 14.7 ± 2.5 | 0.0% | 2.6% | 94.4% | 85.1% | 11.2% | 16.9 | 0.0 | 3.6 | 2.9 (17.5%) | 0.0 | 6.8% | 0 |
| Jungle Stampede mirror, Control vs Control | 53.2% | 76.2% | 21.7 ± 4.6 | 15.8% | 0.0% | 92.4% | 76.1% | 7.4% | 18.3 | 2.1 | 7.8 | 1.2 (54.2%) | 0.0 | 46.8% | 0 |
| Zoo Patrol mirror, Control vs Control | 48.6% | 66.0% | 24.5 ± 5.4 | 38.0% | 0.0% | 72.6% | 74.5% | 34.3% | 13.9 | 22.0 | 13.8 | 9.8 (30.7%) | 0.0 | 47.6% | 0 |
| Vesper's Ledger mirror, Control vs Control | 48.8% | 56.6% | 25.5 ± 5.2 | 50.2% | 0.0% | 95.0% | 76.5% | 4.7% | 23.0 | 22.2 | 32.5 | 11.6 (19.4%) | 0.0 | 64.9% | 0 |
| Sparkwrench Scrappers mirror, Control vs Control | 50.8% | 44.6% | 27.7 ± 8.7 | 53.0% | 0.0% | 98.9% | 92.1% | 24.4% | 24.8 | 0.0 | 5.6 | 12.5 (8.8%) | 18.3 | 79.4% | 0 |
| Auditor's Arsenal mirror, Control vs Control | 52.4% | 77.0% | 18.9 ± 2.9 | 2.8% | 0.0% | 67.9% | 54.6% | 22.8% | 5.6 | 5.1 | 1.4 | 0.4 (44.2%) | 15.2 | 31.1% | 0 |
| Goober Mob mirror, Greedy vs Control | 64.4% | 78.2% | 13.2 ± 2.4 | 0.0% | 8.6% | 94.1% | 82.3% | 9.2% | 13.5 | 0.0 | 3.5 | 2.4 (18.3%) | 0.0 | 1.3% | 0 |
| Jungle Stampede mirror, Greedy vs Control | 48.4% | 79.0% | 19.6 ± 3.8 | 6.4% | 0.0% | 93.0% | 73.4% | 4.2% | 15.7 | 1.2 | 7.0 | 0.8 (56.3%) | 0.0 | 34.2% | 0 |
| Zoo Patrol mirror, Greedy vs Control | 51.8% | 68.4% | 23.8 ± 5.3 | 34.2% | 0.0% | 73.7% | 73.6% | 32.5% | 13.4 | 19.8 | 13.1 | 9.1 (30.7%) | 0.0 | 47.3% | 0 |
| Vesper's Ledger mirror, Greedy vs Control | 50.8% | 55.4% | 24.8 ± 5.4 | 42.6% | 0.0% | 94.9% | 74.9% | 4.5% | 22.3 | 22.8 | 31.0 | 10.1 (17.9%) | 0.0 | 69.9% | 0 |
| Sparkwrench Scrappers mirror, Greedy vs Control | 49.8% | 49.6% | 24.3 ± 6.9 | 36.6% | 0.0% | 98.7% | 91.4% | 23.7% | 22.2 | 0.0 | 5.1 | 10.4 (8.0%) | 13.4 | 74.0% | 0 |
| Auditor's Arsenal mirror, Greedy vs Control | 58.4% | 77.8% | 17.2 ± 2.1 | 0.4% | 0.0% | 68.0% | 49.9% | 19.3% | 4.1 | 2.7 | 1.0 | 0.0 (42.1%) | 10.5 | 3.7% | 0 |

## Game length lever: starting life

*Can starting life even out game length between fast and slow decks? Compare with the 30-life mirrors in the round robin.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, 25 life | 52.8% | 79.4% | 11.0 ± 1.8 | 0.0% | 29.2% | 92.9% | 69.0% | 5.6% | 9.2 | 0.0 | 2.9 | 1.5 (20.9%) | 0.0 | 0.3% | 0 |
| Jungle Stampede mirror, 25 life | 52.6% | 84.4% | 16.0 ± 3.0 | 0.6% | 0.0% | 94.2% | 72.6% | 1.0% | 11.1 | 0.2 | 5.3 | 0.4 (58.7%) | 0.0 | 13.5% | 0 |
| Zoo Patrol mirror, 25 life | 49.8% | 68.0% | 21.1 ± 4.6 | 15.4% | 0.0% | 75.3% | 72.3% | 30.5% | 11.4 | 15.0 | 10.8 | 6.7 (27.3%) | 0.0 | 39.6% | 0 |
| Vesper's Ledger mirror, 25 life | 49.6% | 55.8% | 22.3 ± 5.2 | 27.4% | 0.0% | 94.6% | 70.2% | 4.4% | 18.8 | 20.8 | 26.0 | 6.8 (16.7%) | 0.0 | 71.1% | 0 |
| Sparkwrench Scrappers mirror, 25 life | 48.0% | 65.0% | 18.2 ± 4.4 | 6.6% | 1.2% | 97.7% | 87.2% | 21.9% | 16.1 | 0.0 | 3.5 | 6.1 (7.3%) | 6.2 | 54.2% | 0 |
| Auditor's Arsenal mirror, 25 life | 53.2% | 77.0% | 15.0 ± 1.5 | 0.0% | 0.0% | 64.1% | 32.6% | 7.2% | 2.2 | 1.0 | 0.9 | 0.0 | 5.7 | 0.0% | 0 |
| Goober Mob mirror, 35 life | 54.0% | 78.6% | 12.8 ± 2.1 | 0.0% | 8.0% | 93.3% | 74.3% | 5.7% | 11.8 | 0.0 | 3.9 | 2.3 (21.7%) | 0.0 | 1.7% | 0 |
| Jungle Stampede mirror, 35 life | 53.6% | 81.8% | 18.2 ± 3.2 | 1.6% | 0.0% | 93.6% | 69.5% | 0.7% | 13.6 | 0.3 | 6.5 | 0.7 (56.7%) | 0.0 | 25.4% | 0 |
| Zoo Patrol mirror, 35 life | 50.4% | 65.0% | 24.4 ± 5.3 | 37.4% | 0.0% | 73.5% | 72.4% | 30.4% | 13.7 | 20.5 | 13.8 | 9.5 (31.6%) | 0.0 | 49.3% | 0 |
| Vesper's Ledger mirror, 35 life | 48.0% | 56.2% | 25.3 ± 4.9 | 46.6% | 0.0% | 94.7% | 69.9% | 4.2% | 22.8 | 25.6 | 32.4 | 11.2 (15.4%) | 0.0 | 70.3% | 0 |
| Sparkwrench Scrappers mirror, 35 life | 50.8% | 57.4% | 22.9 ± 5.9 | 26.6% | 0.0% | 98.3% | 90.1% | 22.5% | 20.7 | 0.0 | 5.1 | 9.5 (7.7%) | 11.5 | 69.3% | 0 |
| Auditor's Arsenal mirror, 35 life | 53.6% | 79.0% | 16.3 ± 1.5 | 0.0% | 0.0% | 65.5% | 32.9% | 6.7% | 2.6 | 1.0 | 1.0 | 0.0 (57.1%) | 7.8 | 0.0% | 0 |

## Permanent damage (GAME_DESIGN §7.3)

*What changes when damage wears off at end of turn like in MTG? Compare with the round-robin mirrors (permanent damage).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, damage wears off (MTG) | 54.0% | 76.6% | 11.8 ± 1.9 | 0.0% | 17.6% | 90.4% | 0.0% | 0.0% | 10.0 | 0.0 | 3.4 | 1.9 (20.1%) | 0.0 | 1.5% | 0 |
| Jungle Stampede mirror, damage wears off (MTG) | 52.6% | 82.8% | 17.2 ± 3.1 | 1.0% | 0.0% | 92.8% | 0.0% | 0.0% | 12.4 | 0.0 | 6.0 | 0.5 (56.6%) | 0.0 | 20.0% | 0 |
| Zoo Patrol mirror, damage wears off (MTG) | 50.0% | 66.2% | 19.3 ± 4.0 | 8.4% | 0.0% | 73.8% | 0.0% | 0.0% | 10.2 | 8.4 | 9.7 | 3.7 (38.5%) | 0.0 | 31.5% | 0 |
| Vesper's Ledger mirror, damage wears off (MTG) | 47.8% | 56.0% | 23.9 ± 5.0 | 36.6% | 0.0% | 91.6% | 0.0% | 0.0% | 20.8 | 24.0 | 29.9 | 9.2 (17.3%) | 0.0 | 69.7% | 0 |
| Sparkwrench Scrappers mirror, damage wears off (MTG) | 49.6% | 66.6% | 17.8 ± 3.8 | 4.4% | 0.2% | 94.2% | 0.0% | 0.0% | 15.5 | 0.0 | 3.4 | 5.3 (7.4%) | 5.2 | 50.4% | 0 |
| Auditor's Arsenal mirror, damage wears off (MTG) | 51.2% | 75.8% | 15.6 ± 1.3 | 0.0% | 0.0% | 58.4% | 0.0% | 0.0% | 2.1 | 0.1 | 0.9 | 0.0 (50.0%) | 6.4 | 1.2% | 0 |

## Going second (GAME_DESIGN §3)

*First-player win rate in mirrors (50% is fair). Current rule (MTG default): the first player skips their turn-1 draw, no other compensation.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, current rule (MTG) | 53.8% | 77.2% | 11.9 ± 2.0 | 0.0% | 17.4% | 93.2% | 69.9% | 5.7% | 10.4 | 0.0 | 3.4 | 1.9 (20.3%) | 0.0 | 1.2% | 0 |
| Goober Mob mirror, everyone draws + 2nd player +1 mana on turn 1 | 52.0% | 73.8% | 11.9 ± 2.0 | 0.0% | 16.2% | 93.5% | 72.1% | 5.4% | 11.1 | 0.0 | 3.2 | 2.0 (20.5%) | 0.0 | 0.4% | 0 |
| Goober Mob mirror, everyone draws + 2nd player +1 mana and 1 Gold | 51.8% | 61.6% | 12.0 ± 2.0 | 0.0% | 12.4% | 93.7% | 72.1% | 6.5% | 11.8 | 0.0 | 4.1 | 2.2 (21.2%) | 0.0 | 0.3% | 0 |
| Jungle Stampede mirror, current rule (MTG) | 53.0% | 82.8% | 17.2 ± 3.2 | 1.4% | 0.0% | 93.6% | 74.3% | 0.9% | 12.5 | 0.3 | 6.0 | 0.5 (56.8%) | 0.0 | 21.3% | 0 |
| Jungle Stampede mirror, everyone draws + 2nd player +1 mana on turn 1 | 53.8% | 77.2% | 17.3 ± 3.2 | 1.6% | 0.0% | 93.8% | 75.6% | 0.7% | 12.7 | 0.3 | 5.8 | 0.5 (60.3%) | 0.0 | 20.2% | 0 |
| Jungle Stampede mirror, everyone draws + 2nd player +1 mana and 1 Gold | 54.4% | 74.6% | 17.2 ± 3.1 | 1.4% | 0.0% | 93.8% | 74.7% | 0.7% | 12.6 | 0.2 | 6.5 | 0.6 (64.5%) | 0.0 | 21.5% | 0 |
| Vesper's Ledger mirror, current rule (MTG) | 47.2% | 56.6% | 23.9 ± 5.2 | 35.0% | 0.0% | 94.8% | 69.7% | 4.3% | 20.9 | 22.9 | 29.2 | 9.0 (16.9%) | 0.0 | 71.2% | 0 |
| Vesper's Ledger mirror, everyone draws + 2nd player +1 mana on turn 1 | 54.4% | 56.6% | 23.5 ± 4.9 | 32.2% | 0.0% | 94.5% | 68.0% | 4.0% | 20.5 | 23.0 | 29.2 | 8.8 (16.5%) | 0.0 | 71.7% | 0 |
| Vesper's Ledger mirror, everyone draws + 2nd player +1 mana and 1 Gold | 51.4% | 58.0% | 23.3 ± 4.9 | 32.6% | 0.0% | 94.9% | 69.6% | 3.9% | 20.6 | 23.0 | 29.6 | 8.7 (17.1%) | 0.0 | 72.3% | 0 |

## Mana model: round pool (GAME_DESIGN §5)

*Does the round pool (everyone refills when a round starts, mana usable on any turn of the round, unspent mana banked at the end of the round) take away the first player's edge? Watch 1st win% (50% is fair).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, mana per turn (current) | 53.8% | 77.2% | 11.9 ± 2.0 | 0.0% | 17.4% | 93.2% | 69.9% | 5.7% | 10.4 | 0.0 | 3.4 | 1.9 (20.3%) | 0.0 | 1.2% | 0 |
| Goober Mob mirror, round pool | 52.2% | 86.0% | 12.3 ± 2.0 | 0.0% | 13.4% | 93.2% | 71.2% | 5.1% | 11.4 | 0.0 | 2.9 | 2.8 (38.3%) | 0.0 | 0.9% | 0 |
| Goober Mob mirror, round pool + Gold first off-turn | 54.2% | 83.2% | 12.3 ± 1.9 | 0.0% | 13.4% | 93.4% | 73.9% | 5.1% | 11.4 | 0.0 | 3.2 | 2.8 (38.2%) | 0.0 | 0.8% | 0 |
| Goober Mob mirror, rotating first player | 50.2% | 53.2% | 11.7 ± 1.9 | 0.0% | 16.2% | 93.9% | 72.5% | 3.1% | 9.4 | 0.0 | 3.6 | 1.9 (29.7%) | 0.0 | 0.6% | 0 |
| Goober Mob mirror, rotating first player + round pool + Gold first | 47.8% | 48.0% | 12.4 ± 1.9 | 0.0% | 7.6% | 94.1% | 71.2% | 3.6% | 11.3 | 0.0 | 3.5 | 3.2 (46.4%) | 0.0 | 1.0% | 0 |
| Goober Mob mirror, 1st player skips first mana | 50.6% | 18.0% | 12.9 ± 2.1 | 0.0% | 0.6% | 93.5% | 68.6% | 5.8% | 10.7 | 0.0 | 3.3 | 1.9 (19.8%) | 0.0 | 0.9% | 0 |
| Goober Mob mirror, 1st player skips first mana + round pool + Gold first | 49.2% | 25.4% | 13.6 ± 2.0 | 0.0% | 0.0% | 93.9% | 70.5% | 5.4% | 12.2 | 0.0 | 3.3 | 2.8 (37.1%) | 0.0 | 0.4% | 0 |
| Jungle Stampede mirror, mana per turn (current) | 53.0% | 82.8% | 17.2 ± 3.2 | 1.4% | 0.0% | 93.6% | 74.3% | 0.9% | 12.5 | 0.3 | 6.0 | 0.5 (56.8%) | 0.0 | 21.3% | 0 |
| Jungle Stampede mirror, round pool | 53.0% | 86.4% | 16.6 ± 3.2 | 0.8% | 0.0% | 93.6% | 66.4% | 1.0% | 11.5 | 0.2 | 4.4 | 0.3 (36.2%) | 0.0 | 16.4% | 0 |
| Jungle Stampede mirror, round pool + Gold first off-turn | 52.8% | 83.4% | 17.3 ± 3.2 | 1.2% | 0.0% | 93.7% | 73.9% | 0.8% | 12.6 | 0.3 | 5.9 | 0.6 (59.0%) | 0.0 | 17.2% | 0 |
| Jungle Stampede mirror, rotating first player | 49.0% | 52.8% | 16.7 ± 2.9 | 0.8% | 0.0% | 94.7% | 65.2% | 0.5% | 10.7 | 0.1 | 5.7 | 0.4 (68.1%) | 0.0 | 15.9% | 0 |
| Jungle Stampede mirror, rotating first player + round pool + Gold first | 50.0% | 51.0% | 16.7 ± 2.8 | 0.8% | 0.0% | 94.7% | 69.1% | 0.4% | 10.7 | 0.1 | 5.5 | 0.4 (69.7%) | 0.0 | 11.3% | 0 |
| Jungle Stampede mirror, 1st player skips first mana | 47.0% | 16.0% | 18.1 ± 3.1 | 2.4% | 0.0% | 94.2% | 70.5% | 0.8% | 12.4 | 0.2 | 5.9 | 0.6 (60.8%) | 0.0 | 20.0% | 0 |
| Jungle Stampede mirror, 1st player skips first mana + round pool + Gold first | 47.0% | 16.0% | 18.1 ± 3.1 | 2.6% | 0.0% | 94.1% | 70.1% | 0.8% | 12.4 | 0.3 | 5.2 | 0.6 (62.4%) | 0.0 | 18.6% | 0 |
| Zoo Patrol mirror, mana per turn (current) | 51.0% | 67.6% | 22.8 ± 5.0 | 28.0% | 0.0% | 74.5% | 72.7% | 30.3% | 12.6 | 17.9 | 12.3 | 8.2 (29.7%) | 0.0 | 45.4% | 0 |
| Zoo Patrol mirror, round pool | 49.4% | 57.2% | 23.1 ± 4.9 | 28.8% | 0.0% | 70.5% | 67.3% | 30.1% | 12.3 | 21.3 | 7.0 | 9.7 (32.6%) | 0.0 | 46.8% | 0 |
| Zoo Patrol mirror, round pool + Gold first off-turn | 50.6% | 52.8% | 22.9 ± 4.9 | 26.0% | 0.0% | 69.7% | 66.7% | 28.4% | 12.3 | 21.6 | 12.0 | 9.9 (37.1%) | 0.0 | 38.6% | 0 |
| Zoo Patrol mirror, rotating first player | 48.8% | 53.0% | 22.7 ± 5.0 | 23.8% | 0.0% | 76.7% | 76.2% | 29.3% | 12.5 | 16.1 | 12.1 | 7.2 (34.9%) | 0.0 | 47.2% | 0 |
| Zoo Patrol mirror, rotating first player + round pool + Gold first | 48.6% | 52.8% | 22.5 ± 4.7 | 22.6% | 0.0% | 71.2% | 69.3% | 28.0% | 12.0 | 20.2 | 11.4 | 9.0 (42.8%) | 0.0 | 37.3% | 0 |
| Zoo Patrol mirror, 1st player skips first mana | 45.2% | 41.4% | 24.6 ± 5.7 | 38.2% | 0.0% | 74.0% | 71.5% | 30.4% | 13.0 | 18.4 | 12.8 | 8.5 (30.3%) | 0.0 | 50.9% | 0 |
| Zoo Patrol mirror, 1st player skips first mana + round pool + Gold first | 48.2% | 28.0% | 23.3 ± 4.9 | 30.6% | 0.0% | 70.2% | 67.2% | 29.5% | 12.1 | 20.6 | 11.4 | 9.4 (33.4%) | 0.0 | 38.2% | 0 |
| Vesper's Ledger mirror, mana per turn (current) | 47.2% | 56.6% | 23.9 ± 5.2 | 35.0% | 0.0% | 94.8% | 69.7% | 4.3% | 20.9 | 22.9 | 29.2 | 9.0 (16.9%) | 0.0 | 71.2% | 0 |
| Vesper's Ledger mirror, round pool | 53.8% | 59.6% | 24.8 ± 4.9 | 41.0% | 0.0% | 94.7% | 67.5% | 4.2% | 21.4 | 24.1 | 25.6 | 8.8 (13.9%) | 0.0 | 74.3% | 0 |
| Vesper's Ledger mirror, round pool + Gold first off-turn | 50.6% | 56.4% | 24.4 ± 4.8 | 39.2% | 0.0% | 94.6% | 66.3% | 4.2% | 21.0 | 23.0 | 30.8 | 10.0 (19.9%) | 0.0 | 60.8% | 0 |
| Vesper's Ledger mirror, rotating first player | 51.6% | 40.6% | 21.4 ± 4.5 | 18.6% | 0.0% | 94.2% | 64.6% | 2.7% | 16.2 | 19.8 | 24.5 | 7.5 (27.1%) | 0.0 | 60.5% | 0 |
| Vesper's Ledger mirror, rotating first player + round pool + Gold first | 52.0% | 44.6% | 22.0 ± 4.2 | 18.4% | 0.0% | 94.9% | 69.7% | 2.7% | 16.4 | 19.5 | 26.0 | 8.6 (31.9%) | 0.0 | 44.8% | 0 |
| Vesper's Ledger mirror, 1st player skips first mana | 46.6% | 35.6% | 24.2 ± 4.9 | 41.0% | 0.0% | 94.7% | 67.0% | 4.4% | 20.3 | 21.4 | 28.7 | 8.7 (16.5%) | 0.0 | 70.4% | 0 |
| Vesper's Ledger mirror, 1st player skips first mana + round pool + Gold first | 46.0% | 26.6% | 24.4 ± 5.3 | 42.6% | 0.0% | 94.3% | 66.0% | 4.2% | 20.1 | 21.8 | 29.0 | 9.3 (20.4%) | 0.0 | 57.7% | 0 |
| Sparkwrench Scrappers mirror, mana per turn (current) | 51.4% | 58.4% | 20.6 ± 5.2 | 15.8% | 0.2% | 98.4% | 90.4% | 22.4% | 18.7 | 0.0 | 4.4 | 7.9 (7.5%) | 8.8 | 63.5% | 0 |
| Sparkwrench Scrappers mirror, round pool | 50.8% | 59.0% | 20.5 ± 5.2 | 16.8% | 0.2% | 97.9% | 88.3% | 23.0% | 18.2 | 0.0 | 2.9 | 8.4 (13.5%) | 8.5 | 65.2% | 0 |
| Sparkwrench Scrappers mirror, round pool + Gold first off-turn | 51.8% | 59.2% | 20.5 ± 5.1 | 16.2% | 0.2% | 98.0% | 88.4% | 23.3% | 18.2 | 0.0 | 4.0 | 8.6 (14.9%) | 8.2 | 60.8% | 0 |
| Sparkwrench Scrappers mirror, rotating first player | 51.8% | 46.8% | 18.5 ± 3.7 | 4.0% | 0.2% | 96.8% | 86.4% | 21.8% | 16.0 | 0.0 | 3.9 | 6.1 (10.4%) | 5.7 | 48.1% | 0 |
| Sparkwrench Scrappers mirror, rotating first player + round pool + Gold first | 51.0% | 45.6% | 19.1 ± 4.1 | 6.0% | 0.2% | 97.0% | 87.2% | 23.0% | 16.6 | 0.0 | 4.0 | 7.2 (18.4%) | 5.9 | 50.5% | 0 |
| Sparkwrench Scrappers mirror, 1st player skips first mana | 48.0% | 32.6% | 21.5 ± 5.4 | 18.8% | 0.0% | 98.0% | 88.7% | 22.1% | 18.9 | 0.0 | 4.1 | 7.9 (8.4%) | 8.6 | 63.6% | 0 |
| Sparkwrench Scrappers mirror, 1st player skips first mana + round pool + Gold first | 47.0% | 36.0% | 22.0 ± 5.5 | 21.4% | 0.0% | 98.1% | 89.6% | 23.2% | 19.2 | 0.0 | 4.3 | 9.3 (11.3%) | 8.9 | 62.7% | 0 |
| Auditor's Arsenal mirror, mana per turn (current) | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 0.0% | 0 |
| Auditor's Arsenal mirror, round pool | 53.6% | 76.2% | 15.4 ± 1.4 | 0.0% | 0.0% | 64.6% | 34.9% | 7.1% | 2.3 | 0.9 | 1.2 | 0.0 (33.3%) | 6.4 | 0.0% | 0 |
| Auditor's Arsenal mirror, round pool + Gold first off-turn | 53.6% | 76.2% | 15.4 ± 1.4 | 0.0% | 0.0% | 64.4% | 34.5% | 7.1% | 2.3 | 0.9 | 1.2 | 0.0 (33.3%) | 6.4 | 0.0% | 0 |
| Auditor's Arsenal mirror, rotating first player | 48.8% | 47.4% | 15.5 ± 1.5 | 0.0% | 0.0% | 67.7% | 35.2% | 6.0% | 2.3 | 0.6 | 0.9 | 0.0 | 6.2 | 0.0% | 0 |
| Auditor's Arsenal mirror, rotating first player + round pool + Gold first | 51.0% | 46.0% | 15.2 ± 1.4 | 0.0% | 0.0% | 64.9% | 32.1% | 6.6% | 2.1 | 0.6 | 1.7 | 0.0 (0.0%) | 6.1 | 0.1% | 0 |
| Auditor's Arsenal mirror, 1st player skips first mana | 46.2% | 23.2% | 16.7 ± 1.6 | 0.0% | 0.0% | 66.9% | 31.1% | 6.3% | 2.4 | 1.2 | 0.8 | 0.0 | 6.8 | 0.0% | 0 |
| Auditor's Arsenal mirror, 1st player skips first mana + round pool + Gold first | 47.8% | 20.8% | 16.3 ± 1.5 | 0.0% | 0.0% | 66.2% | 33.1% | 6.3% | 2.2 | 0.9 | 1.2 | 0.0 | 6.2 | 0.0% | 0 |

## Tavern Dwellers (GAME_DESIGN §9)

*What do Tavern Dwellers (passives and Powers, the universal Gold sink) change? Each mirror with and without Tavern Dwellers. Watch Wasted (mana lost to the Gold cap), Gold spent and game length.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, with Tavern Dwellers | 53.8% | 77.2% | 11.9 ± 2.0 | 0.0% | 17.4% | 93.2% | 69.9% | 5.7% | 10.4 | 0.0 | 3.4 | 1.9 (20.3%) | 0.0 | 1.2% | 0 |
| Goober Mob mirror, no Tavern Dwellers | 54.0% | 81.0% | 12.6 ± 2.5 | 0.2% | 12.8% | 92.9% | 72.7% | 6.4% | 10.7 | 0.0 | 3.3 | 0.0 | 0.0 | 17.0% | 0 |
| Jungle Stampede mirror, with Tavern Dwellers | 53.0% | 82.8% | 17.2 ± 3.2 | 1.4% | 0.0% | 93.6% | 74.3% | 0.9% | 12.5 | 0.3 | 6.0 | 0.5 (56.8%) | 0.0 | 21.3% | 0 |
| Jungle Stampede mirror, no Tavern Dwellers | 51.2% | 79.8% | 17.0 ± 4.2 | 2.6% | 0.0% | 85.7% | 67.1% | 1.3% | 10.0 | 0.6 | 5.2 | 0.0 | 0.0 | 29.0% | 0 |
| Zoo Patrol mirror, with Tavern Dwellers | 51.0% | 67.6% | 22.8 ± 5.0 | 28.0% | 0.0% | 74.5% | 72.7% | 30.3% | 12.6 | 17.9 | 12.3 | 8.2 (29.7%) | 0.0 | 45.4% | 0 |
| Zoo Patrol mirror, no Tavern Dwellers | 50.8% | 65.8% | 28.0 ± 7.7 | 57.2% | 0.0% | 90.9% | 86.2% | 51.0% | 16.8 | 5.0 | 8.2 | 0.0 | 0.0 | 75.3% | 0 |
| Vesper's Ledger mirror, with Tavern Dwellers | 47.2% | 56.6% | 23.9 ± 5.2 | 35.0% | 0.0% | 94.8% | 69.7% | 4.3% | 20.9 | 22.9 | 29.2 | 9.0 (16.9%) | 0.0 | 71.2% | 0 |
| Vesper's Ledger mirror, no Tavern Dwellers | 51.2% | 58.2% | 25.4 ± 6.0 | 44.4% | 0.0% | 95.1% | 70.8% | 4.1% | 21.7 | 24.5 | 17.1 | 0.0 | 0.0 | 74.5% | 0 |
| Sparkwrench Scrappers mirror, with Tavern Dwellers | 51.4% | 58.4% | 20.6 ± 5.2 | 15.8% | 0.2% | 98.4% | 90.4% | 22.4% | 18.7 | 0.0 | 4.4 | 7.9 (7.5%) | 8.8 | 63.5% | 0 |
| Sparkwrench Scrappers mirror, no Tavern Dwellers | 51.0% | 56.8% | 20.8 ± 5.5 | 16.4% | 0.0% | 97.8% | 87.5% | 22.3% | 18.9 | 0.0 | 2.5 | 0.0 | 12.1 | 72.2% | 0 |
| Auditor's Arsenal mirror, with Tavern Dwellers | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 0.0% | 0 |
| Auditor's Arsenal mirror, no Tavern Dwellers | 52.4% | 78.2% | 16.4 ± 1.4 | 0.0% | 0.0% | 65.1% | 32.4% | 5.8% | 2.6 | 1.0 | 1.1 | 0.0 | 5.5 | 0.0% | 0 |

## Gold cap (GAME_DESIGN §5.2)

*Does the cap matter now that Tavern Dweller Powers and Equip spend Gold?*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Zoo Patrol mirror, Gold cap 3 | 50.4% | 65.0% | 22.9 ± 5.0 | 27.0% | 0.0% | 74.8% | 72.8% | 31.0% | 12.7 | 17.4 | 11.0 | 7.9 (27.9%) | 0.0 | 54.7% | 0 |
| Zoo Patrol mirror, Gold cap 5 | 51.0% | 67.6% | 22.8 ± 5.0 | 28.0% | 0.0% | 74.5% | 72.7% | 30.3% | 12.6 | 17.9 | 12.3 | 8.2 (29.7%) | 0.0 | 45.4% | 0 |
| Zoo Patrol mirror, Gold cap 8 | 50.4% | 67.8% | 22.7 ± 5.0 | 26.8% | 0.0% | 74.3% | 72.7% | 30.0% | 12.5 | 17.8 | 12.3 | 8.1 (29.5%) | 0.0 | 35.7% | 0 |
| Vesper's Ledger mirror, Gold cap 3 | 51.6% | 54.2% | 24.0 ± 5.1 | 36.0% | 0.0% | 94.7% | 69.0% | 4.6% | 20.4 | 22.9 | 25.6 | 9.2 (15.9%) | 0.0 | 77.8% | 0 |
| Vesper's Ledger mirror, Gold cap 5 | 47.2% | 56.6% | 23.9 ± 5.2 | 35.0% | 0.0% | 94.8% | 69.7% | 4.3% | 20.9 | 22.9 | 29.2 | 9.0 (16.9%) | 0.0 | 71.2% | 0 |
| Vesper's Ledger mirror, Gold cap 8 | 47.2% | 57.0% | 23.8 ± 5.3 | 34.0% | 0.0% | 94.7% | 68.9% | 4.2% | 20.8 | 23.1 | 30.0 | 9.0 (17.0%) | 0.0 | 59.9% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 3 | 51.6% | 58.2% | 20.6 ± 5.2 | 15.8% | 0.2% | 98.3% | 90.3% | 22.5% | 18.7 | 0.0 | 4.4 | 7.9 (7.6%) | 8.8 | 72.7% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 5 | 51.4% | 58.4% | 20.6 ± 5.2 | 15.8% | 0.2% | 98.4% | 90.4% | 22.4% | 18.7 | 0.0 | 4.4 | 7.9 (7.5%) | 8.8 | 63.5% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 8 | 51.0% | 58.4% | 20.6 ± 5.2 | 16.0% | 0.2% | 98.3% | 90.3% | 22.4% | 18.7 | 0.0 | 4.4 | 7.9 (7.7%) | 8.8 | 50.7% | 0 |
| Auditor's Arsenal mirror, Gold cap 3 | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.0% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 0.0% | 0 |
| Auditor's Arsenal mirror, Gold cap 5 | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 0.0% | 0 |
| Auditor's Arsenal mirror, Gold cap 8 | 52.2% | 76.0% | 15.6 ± 1.4 | 0.0% | 0.0% | 65.6% | 35.1% | 7.1% | 2.5 | 1.0 | 0.9 | 0.0 (50.0%) | 6.7 | 0.0% | 0 |
