# Simulation Report

Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

1000 games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time 306s.

Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) ± standard deviation · **Long** / **Short** = share of games over 25 / under 10 turns · **Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · **Chip→death** = share of *chip damage* (damage a creature carried into a later turn) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · **Wounded** = share of creatures carrying damage at the start of a turn · **Deaths** / **Heal** / **Gold spent** are per game · **Wasted** = share of unspent mana lost to the Gold cap.

## Round robin (current rules, Greedy bots)

*How long are games, and does damage on creatures decide anything? Every deck against every deck.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror | 53.5% | 71.3% | 13.2 ± 2.4 | 0.2% | 5.7% | 88.6% | 28.6% | 1.6% | 8.2 | 3.1 | 1.8 | 9.5% | 0 |
| Goober Mob vs Jungle Stampede | 33.8% | 63.2% | 14.7 ± 2.5 | 0.3% | 0.7% | 77.3% | 23.4% | 2.3% | 6.3 | 4.1 | 2.6 | 12.2% | 0 |
| Goober Mob vs Zoo Patrol | 61.4% | 69.2% | 16.0 ± 3.0 | 0.6% | 1.3% | 86.8% | 64.9% | 18.9% | 12.2 | 3.5 | 2.5 | 23.4% | 0 |
| Goober Mob vs Vesper's Ledger | 76.3% | 59.9% | 14.9 ± 2.1 | 0.1% | 1.2% | 79.0% | 24.9% | 1.6% | 7.1 | 8.6 | 4.2 | 8.9% | 0 |
| Goober Mob vs Sparkwrench Scrappers | 48.8% | 70.8% | 15.0 ± 2.5 | 0.1% | 2.2% | 89.8% | 65.6% | 8.6% | 12.8 | 1.5 | 1.8 | 15.0% | 0 |
| Jungle Stampede mirror | 49.1% | 53.5% | 19.2 ± 5.1 | 9.7% | 0.0% | 73.0% | 35.7% | 11.6% | 10.5 | 7.6 | 4.6 | 43.4% | 0 |
| Jungle Stampede vs Zoo Patrol | 43.7% | 66.9% | 17.5 ± 3.2 | 1.9% | 0.0% | 80.3% | 65.6% | 28.5% | 10.7 | 6.1 | 3.2 | 24.4% | 0 |
| Jungle Stampede vs Vesper's Ledger | 84.5% | 60.5% | 16.6 ± 2.5 | 0.6% | 0.0% | 67.3% | 29.8% | 4.1% | 7.1 | 10.7 | 5.2 | 15.6% | 0 |
| Jungle Stampede vs Sparkwrench Scrappers | 58.5% | 66.3% | 15.9 ± 2.7 | 0.6% | 0.0% | 81.7% | 60.7% | 11.0% | 9.3 | 2.6 | 2.3 | 18.1% | 0 |
| Zoo Patrol mirror | 51.5% | 58.5% | 23.4 ± 6.9 | 32.0% | 0.0% | 86.9% | 82.1% | 47.3% | 16.1 | 7.4 | 4.2 | 71.0% | 0 |
| Zoo Patrol vs Vesper's Ledger | 91.0% | 55.8% | 20.6 ± 4.3 | 11.2% | 0.0% | 86.3% | 75.5% | 24.9% | 16.1 | 10.1 | 7.1 | 56.4% | 0 |
| Zoo Patrol vs Sparkwrench Scrappers | 69.0% | 61.4% | 19.2 ± 4.5 | 8.9% | 0.3% | 87.3% | 75.6% | 30.4% | 16.7 | 4.1 | 2.6 | 53.7% | 0 |
| Vesper's Ledger mirror | 51.1% | 58.9% | 23.8 ± 5.4 | 34.6% | 0.0% | 80.3% | 41.9% | 9.3% | 19.4 | 28.4 | 12.9 | 71.7% | 0 |
| Vesper's Ledger vs Sparkwrench Scrappers | 11.5% | 53.5% | 17.4 ± 3.1 | 1.4% | 0.1% | 89.2% | 70.8% | 9.2% | 13.1 | 5.8 | 4.6 | 42.6% | 0 |
| Sparkwrench Scrappers mirror | 51.7% | 59.7% | 19.7 ± 4.6 | 11.0% | 1.1% | 95.7% | 79.3% | 20.9% | 22.7 | 0.0 | 1.9 | 59.6% | 0 |

## Bot play styles

*Do the results hold with a defensive player? Control blocks freely, keeps blockers home and saves Gold. In "Greedy vs Control", A win% is the Greedy side.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, Control vs Control | 50.8% | 63.2% | 20.6 ± 6.2 | 14.9% | 0.2% | 86.3% | 58.3% | 10.7% | 19.4 | 4.9 | 2.7 | 67.7% | 0 |
| Jungle Stampede mirror, Control vs Control | 49.4% | 50.2% | 24.9 ± 8.0 | 35.4% | 0.0% | 71.6% | 48.9% | 27.9% | 16.4 | 12.1 | 4.4 | 70.3% | 0 |
| Zoo Patrol mirror, Control vs Control | 49.4% | 56.6% | 26.1 ± 7.3 | 48.9% | 0.0% | 87.9% | 85.9% | 52.5% | 18.5 | 8.4 | 4.2 | 76.4% | 0 |
| Vesper's Ledger mirror, Control vs Control | 51.0% | 53.4% | 32.6 ± 9.6 | 76.2% | 0.0% | 81.0% | 62.6% | 17.8% | 31.0 | 34.0 | 17.2 | 83.9% | 0 |
| Sparkwrench Scrappers mirror, Control vs Control | 52.6% | 48.6% | 26.1 ± 7.5 | 44.3% | 0.0% | 97.2% | 87.1% | 22.1% | 30.6 | 0.0 | 1.8 | 81.3% | 0 |
| Goober Mob mirror, Greedy vs Control | 53.4% | 67.2% | 16.8 ± 4.8 | 4.5% | 0.7% | 85.3% | 48.0% | 8.1% | 14.2 | 4.2 | 2.3 | 51.9% | 0 |
| Jungle Stampede mirror, Greedy vs Control | 40.1% | 50.7% | 21.9 ± 6.4 | 19.8% | 0.0% | 70.0% | 40.6% | 20.5% | 13.1 | 10.0 | 4.3 | 58.9% | 0 |
| Zoo Patrol mirror, Greedy vs Control | 51.4% | 56.4% | 25.2 ± 7.2 | 42.4% | 0.0% | 87.5% | 84.4% | 51.0% | 17.7 | 8.1 | 4.2 | 74.9% | 0 |
| Vesper's Ledger mirror, Greedy vs Control | 48.4% | 54.2% | 28.2 ± 8.0 | 58.2% | 0.0% | 79.4% | 54.6% | 15.4% | 25.2 | 31.2 | 15.0 | 80.1% | 0 |
| Sparkwrench Scrappers mirror, Greedy vs Control | 51.8% | 54.2% | 22.6 ± 5.8 | 26.0% | 0.4% | 96.5% | 83.5% | 21.8% | 26.5 | 0.0 | 1.9 | 72.8% | 0 |

## Game length lever: starting life

*Can starting life even out game length between fast and slow decks? Compare with the 30-life mirrors in the round robin.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, 25 life | 52.4% | 69.8% | 12.5 ± 2.4 | 0.1% | 10.9% | 87.7% | 29.9% | 2.4% | 7.9 | 3.0 | 1.7 | 8.1% | 0 |
| Jungle Stampede mirror, 25 life | 48.0% | 56.6% | 18.3 ± 4.8 | 8.5% | 0.0% | 72.5% | 36.5% | 12.5% | 9.7 | 7.3 | 4.2 | 41.8% | 0 |
| Zoo Patrol mirror, 25 life | 50.7% | 59.3% | 21.0 ± 6.2 | 19.6% | 0.0% | 86.2% | 80.2% | 45.0% | 14.0 | 6.6 | 3.9 | 64.1% | 0 |
| Vesper's Ledger mirror, 25 life | 50.2% | 59.6% | 22.2 ± 5.2 | 25.0% | 0.0% | 79.3% | 41.1% | 10.1% | 17.1 | 24.5 | 11.8 | 69.1% | 0 |
| Sparkwrench Scrappers mirror, 25 life | 52.2% | 65.8% | 17.8 ± 4.1 | 4.6% | 1.7% | 95.5% | 77.9% | 19.9% | 20.0 | 0.0 | 1.8 | 48.0% | 0 |
| Goober Mob mirror, 35 life | 52.7% | 70.3% | 13.9 ± 2.7 | 0.7% | 3.8% | 89.0% | 24.3% | 1.7% | 8.8 | 3.4 | 2.0 | 20.7% | 0 |
| Jungle Stampede mirror, 35 life | 48.8% | 53.6% | 19.4 ± 4.4 | 8.7% | 0.0% | 72.6% | 31.2% | 9.1% | 10.4 | 7.5 | 5.0 | 37.4% | 0 |
| Zoo Patrol mirror, 35 life | 50.6% | 56.0% | 25.5 ± 7.6 | 44.2% | 0.0% | 87.1% | 83.4% | 49.2% | 17.7 | 8.2 | 4.3 | 75.6% | 0 |
| Vesper's Ledger mirror, 35 life | 49.2% | 57.4% | 24.8 ± 5.3 | 40.4% | 0.0% | 80.5% | 38.3% | 8.1% | 20.5 | 30.9 | 13.5 | 73.1% | 0 |
| Sparkwrench Scrappers mirror, 35 life | 50.8% | 55.4% | 21.4 ± 5.1 | 18.6% | 0.9% | 95.8% | 79.5% | 21.4% | 24.8 | 0.0 | 2.0 | 67.4% | 0 |

## Permanent damage (GAME_DESIGN §7.3)

*What changes when damage wears off at end of turn like in MTG? Compare with the round-robin mirrors (permanent damage).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, damage wears off (MTG) | 53.2% | 71.0% | 13.1 ± 2.3 | 0.2% | 5.7% | 86.9% | 0.0% | 0.0% | 8.2 | 3.1 | 1.9 | 8.1% | 0 |
| Jungle Stampede mirror, damage wears off (MTG) | 51.6% | 56.0% | 18.0 ± 3.1 | 1.7% | 0.0% | 63.3% | 0.0% | 0.0% | 9.4 | 6.3 | 4.6 | 17.1% | 0 |
| Zoo Patrol mirror, damage wears off (MTG) | 50.2% | 61.4% | 18.7 ± 4.1 | 7.3% | 0.0% | 77.4% | 64.9% | 12.2% | 11.5 | 1.5 | 3.6 | 45.2% | 0 |
| Vesper's Ledger mirror, damage wears off (MTG) | 49.7% | 58.5% | 22.8 ± 4.5 | 25.8% | 0.0% | 69.2% | 0.0% | 0.0% | 17.5 | 27.0 | 12.4 | 67.9% | 0 |
| Sparkwrench Scrappers mirror, damage wears off (MTG) | 51.0% | 67.0% | 17.1 ± 3.0 | 1.4% | 1.3% | 84.6% | 0.0% | 0.0% | 18.3 | 0.0 | 1.8 | 33.6% | 0 |

## Going second (GAME_DESIGN §3)

*First-player win rate in mirrors (50% is fair). Current rule: everyone draws on turn 1; the second player has +1 mana on their first turn.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, current rule | 53.5% | 71.3% | 13.2 ± 2.4 | 0.2% | 5.7% | 88.6% | 28.6% | 1.6% | 8.2 | 3.1 | 1.8 | 9.5% | 0 |
| Goober Mob mirror, old rule (first player skips draw) | 52.0% | 67.6% | 13.1 ± 2.3 | 0.1% | 5.4% | 89.2% | 24.3% | 1.5% | 7.9 | 2.9 | 1.9 | 8.5% | 0 |
| Goober Mob mirror, current rule + 1 starting Gold | 53.9% | 65.9% | 13.4 ± 2.4 | 0.1% | 3.7% | 89.3% | 22.1% | 1.5% | 8.8 | 3.3 | 2.7 | 12.9% | 0 |
| Jungle Stampede mirror, current rule | 49.1% | 53.5% | 19.2 ± 5.1 | 9.7% | 0.0% | 73.0% | 35.7% | 11.6% | 10.5 | 7.6 | 4.6 | 43.4% | 0 |
| Jungle Stampede mirror, old rule (first player skips draw) | 49.5% | 59.9% | 18.7 ± 4.7 | 9.1% | 0.0% | 73.4% | 34.9% | 10.6% | 9.9 | 7.1 | 4.5 | 39.3% | 0 |
| Jungle Stampede mirror, current rule + 1 starting Gold | 49.9% | 51.1% | 18.9 ± 5.0 | 9.0% | 0.0% | 72.5% | 33.3% | 11.5% | 10.1 | 7.5 | 4.9 | 43.9% | 0 |
| Vesper's Ledger mirror, current rule | 51.1% | 58.9% | 23.8 ± 5.4 | 34.6% | 0.0% | 80.3% | 41.9% | 9.3% | 19.4 | 28.4 | 12.9 | 71.7% | 0 |
| Vesper's Ledger mirror, old rule (first player skips draw) | 50.7% | 56.3% | 23.7 ± 5.2 | 32.4% | 0.0% | 79.7% | 39.8% | 9.0% | 18.8 | 28.1 | 13.1 | 69.9% | 0 |
| Vesper's Ledger mirror, current rule + 1 starting Gold | 52.2% | 59.8% | 23.5 ± 5.2 | 31.7% | 0.0% | 79.8% | 40.0% | 9.4% | 18.8 | 27.3 | 13.3 | 71.6% | 0 |

## Gold cap (GAME_DESIGN §5.2)

*Does the cap matter in the decks with the most Gold use?*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Vesper's Ledger mirror, Gold cap 3 | 51.5% | 58.5% | 23.8 ± 5.3 | 35.0% | 0.0% | 79.9% | 40.7% | 9.0% | 19.1 | 27.9 | 11.7 | 79.8% | 0 |
| Vesper's Ledger mirror, Gold cap 5 | 51.1% | 58.9% | 23.8 ± 5.4 | 34.6% | 0.0% | 80.3% | 41.9% | 9.3% | 19.4 | 28.4 | 12.9 | 71.7% | 0 |
| Vesper's Ledger mirror, Gold cap 8 | 51.2% | 59.0% | 23.7 ± 5.4 | 34.0% | 0.0% | 80.2% | 42.2% | 9.4% | 19.3 | 28.4 | 13.5 | 60.0% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 3 | 51.7% | 59.9% | 19.7 ± 4.6 | 11.0% | 1.1% | 95.7% | 79.4% | 20.9% | 22.7 | 0.0 | 1.9 | 69.3% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 5 | 51.7% | 59.7% | 19.7 ± 4.6 | 11.0% | 1.1% | 95.7% | 79.3% | 20.9% | 22.7 | 0.0 | 1.9 | 59.6% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 8 | 51.7% | 59.7% | 19.7 ± 4.6 | 11.0% | 1.1% | 95.7% | 79.3% | 20.9% | 22.7 | 0.0 | 1.9 | 47.6% | 0 |
