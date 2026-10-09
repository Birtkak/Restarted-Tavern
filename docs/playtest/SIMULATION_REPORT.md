# Simulation Report

Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

500 games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time 155s.

Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) ± standard deviation · **Long** / **Short** = share of games over 25 / under 10 turns · **Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · **Chip→death** = share of *chip damage* (damage a creature carried into a later turn) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · **Wounded** = share of creatures carrying damage at the start of a turn · **Deaths** / **Heal** / **Gold spent** are per game · **Wasted** = share of unspent mana lost to the Gold cap.

## Round robin (current rules, Greedy bots)

*How long are games, and does damage on creatures decide anything? Every deck against every deck.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror | 56.6% | 78.8% | 13.5 ± 2.8 | 0.6% | 6.2% | 88.3% | 27.1% | 2.0% | 8.4 | 3.3 | 2.3 | 18.3% | 0 |
| Goober Mob vs Jungle Stampede | 31.6% | 62.6% | 14.9 ± 2.5 | 0.2% | 0.6% | 76.9% | 28.2% | 2.0% | 6.2 | 4.1 | 2.9 | 13.1% | 0 |
| Goober Mob vs Zoo Patrol | 54.6% | 74.4% | 16.2 ± 3.2 | 1.0% | 1.0% | 87.1% | 66.5% | 20.2% | 12.4 | 3.5 | 2.7 | 28.4% | 0 |
| Goober Mob vs Vesper's Ledger | 75.8% | 62.4% | 15.0 ± 2.1 | 0.0% | 2.2% | 79.1% | 30.4% | 1.6% | 7.0 | 8.0 | 4.5 | 7.7% | 0 |
| Goober Mob vs Sparkwrench Scrappers | 48.6% | 75.6% | 15.0 ± 2.5 | 0.0% | 1.6% | 89.9% | 66.7% | 8.8% | 12.6 | 1.4 | 2.1 | 13.3% | 0 |
| Jungle Stampede mirror | 51.8% | 69.6% | 18.4 ± 4.7 | 6.6% | 0.0% | 71.8% | 32.1% | 10.4% | 9.3 | 6.7 | 4.3 | 38.6% | 0 |
| Jungle Stampede vs Zoo Patrol | 44.8% | 73.4% | 17.5 ± 3.3 | 2.2% | 0.0% | 80.2% | 66.8% | 29.5% | 10.4 | 5.8 | 3.4 | 23.1% | 0 |
| Jungle Stampede vs Vesper's Ledger | 83.0% | 64.8% | 16.5 ± 2.7 | 1.0% | 0.0% | 69.4% | 36.4% | 4.5% | 6.8 | 10.4 | 5.2 | 17.8% | 0 |
| Jungle Stampede vs Sparkwrench Scrappers | 58.8% | 71.8% | 16.1 ± 2.8 | 0.8% | 0.0% | 81.8% | 61.1% | 10.8% | 9.4 | 2.6 | 2.6 | 18.0% | 0 |
| Zoo Patrol mirror | 51.6% | 68.2% | 23.2 ± 6.6 | 34.2% | 0.0% | 87.1% | 82.3% | 48.6% | 15.7 | 7.3 | 4.0 | 70.8% | 0 |
| Zoo Patrol vs Vesper's Ledger | 89.6% | 57.8% | 20.7 ± 4.2 | 10.8% | 0.0% | 86.2% | 75.6% | 25.2% | 16.0 | 9.5 | 7.1 | 56.2% | 0 |
| Zoo Patrol vs Sparkwrench Scrappers | 67.8% | 68.8% | 19.1 ± 4.5 | 8.0% | 0.8% | 87.0% | 74.3% | 31.5% | 16.2 | 3.8 | 2.9 | 53.2% | 0 |
| Vesper's Ledger mirror | 51.2% | 57.8% | 23.7 ± 5.3 | 33.0% | 0.0% | 80.2% | 41.1% | 9.2% | 18.9 | 27.0 | 12.6 | 71.1% | 0 |
| Vesper's Ledger vs Sparkwrench Scrappers | 11.6% | 51.8% | 17.5 ± 3.4 | 2.6% | 0.6% | 89.5% | 70.5% | 10.1% | 13.0 | 5.8 | 4.9 | 45.4% | 0 |
| Sparkwrench Scrappers mirror | 51.4% | 61.6% | 19.9 ± 4.5 | 11.8% | 0.4% | 95.2% | 76.7% | 21.1% | 22.5 | 0.0 | 2.1 | 60.5% | 0 |

## Bot play styles

*Do the results hold with a defensive player? Control blocks freely, keeps blockers home and saves Gold. In "Greedy vs Control", A win% is the Greedy side.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, Control vs Control | 56.4% | 65.0% | 20.3 ± 6.1 | 17.2% | 0.4% | 86.2% | 56.7% | 11.5% | 18.6 | 4.7 | 2.9 | 67.6% | 0 |
| Jungle Stampede mirror, Control vs Control | 50.8% | 66.6% | 24.1 ± 7.2 | 34.2% | 0.0% | 71.4% | 47.4% | 27.2% | 15.8 | 11.5 | 4.5 | 67.2% | 0 |
| Zoo Patrol mirror, Control vs Control | 51.8% | 65.6% | 25.5 ± 6.6 | 46.0% | 0.0% | 87.8% | 85.2% | 53.4% | 17.8 | 8.2 | 4.1 | 74.4% | 0 |
| Vesper's Ledger mirror, Control vs Control | 51.6% | 52.2% | 32.9 ± 9.3 | 78.4% | 0.0% | 80.5% | 62.1% | 17.2% | 30.5 | 33.5 | 17.3 | 84.0% | 0 |
| Sparkwrench Scrappers mirror, Control vs Control | 55.0% | 52.4% | 26.4 ± 7.6 | 46.2% | 0.0% | 97.1% | 86.3% | 22.4% | 30.1 | 0.0 | 1.9 | 82.0% | 0 |
| Goober Mob mirror, Greedy vs Control | 57.4% | 71.2% | 16.8 ± 4.7 | 4.4% | 2.2% | 84.9% | 46.1% | 7.4% | 13.7 | 4.2 | 2.6 | 50.1% | 0 |
| Jungle Stampede mirror, Greedy vs Control | 41.0% | 64.4% | 21.7 ± 6.1 | 20.4% | 0.0% | 69.7% | 40.4% | 20.9% | 12.8 | 9.7 | 4.5 | 57.4% | 0 |
| Zoo Patrol mirror, Greedy vs Control | 53.8% | 66.0% | 24.7 ± 6.7 | 40.4% | 0.0% | 87.6% | 84.5% | 51.7% | 17.1 | 7.9 | 4.1 | 73.2% | 0 |
| Vesper's Ledger mirror, Greedy vs Control | 49.0% | 52.8% | 28.7 ± 8.1 | 62.4% | 0.0% | 79.9% | 56.5% | 15.7% | 25.4 | 30.9 | 15.4 | 80.5% | 0 |
| Sparkwrench Scrappers mirror, Greedy vs Control | 49.0% | 53.6% | 22.7 ± 5.8 | 25.8% | 0.0% | 96.3% | 82.3% | 22.5% | 26.1 | 0.0 | 2.1 | 72.9% | 0 |

## Game length lever: starting life

*Can starting life even out game length between fast and slow decks? Compare with the 30-life mirrors in the round robin.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, 25 life | 55.4% | 78.4% | 12.8 ± 2.6 | 0.4% | 10.0% | 87.6% | 30.5% | 2.5% | 7.9 | 3.1 | 2.1 | 13.4% | 0 |
| Jungle Stampede mirror, 25 life | 54.0% | 71.0% | 18.0 ± 5.2 | 6.8% | 0.0% | 71.6% | 34.3% | 13.0% | 9.0 | 6.8 | 4.0 | 42.9% | 0 |
| Zoo Patrol mirror, 25 life | 53.0% | 68.4% | 21.0 ± 5.9 | 22.4% | 0.0% | 86.8% | 80.6% | 45.9% | 13.7 | 6.3 | 3.8 | 64.1% | 0 |
| Vesper's Ledger mirror, 25 life | 47.8% | 59.2% | 22.1 ± 5.3 | 24.6% | 0.0% | 79.2% | 41.3% | 9.9% | 16.8 | 23.8 | 11.7 | 68.7% | 0 |
| Sparkwrench Scrappers mirror, 25 life | 51.2% | 67.8% | 18.0 ± 4.1 | 5.0% | 1.4% | 95.4% | 76.9% | 20.2% | 19.9 | 0.0 | 2.0 | 50.0% | 0 |
| Goober Mob mirror, 35 life | 56.8% | 75.4% | 14.3 ± 3.0 | 0.6% | 3.8% | 88.3% | 24.2% | 1.8% | 9.2 | 3.5 | 2.4 | 21.9% | 0 |
| Jungle Stampede mirror, 35 life | 52.0% | 68.6% | 19.1 ± 4.6 | 7.8% | 0.0% | 72.6% | 32.4% | 9.5% | 9.8 | 6.8 | 4.8 | 37.7% | 0 |
| Zoo Patrol mirror, 35 life | 52.6% | 65.2% | 25.4 ± 7.1 | 45.2% | 0.0% | 87.5% | 84.3% | 50.7% | 17.5 | 8.1 | 4.2 | 75.2% | 0 |
| Vesper's Ledger mirror, 35 life | 49.2% | 56.6% | 24.9 ± 5.0 | 43.0% | 0.0% | 80.6% | 40.6% | 8.0% | 20.3 | 30.1 | 13.7 | 72.6% | 0 |
| Sparkwrench Scrappers mirror, 35 life | 51.8% | 57.6% | 21.8 ± 4.9 | 20.8% | 0.0% | 95.6% | 78.1% | 21.9% | 24.9 | 0.0 | 2.1 | 68.5% | 0 |

## Permanent damage (GAME_DESIGN §7.3)

*What changes when damage wears off at end of turn like in MTG? Compare with the round-robin mirrors (permanent damage).*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, damage wears off (MTG) | 56.4% | 79.0% | 13.4 ± 2.5 | 0.4% | 6.2% | 85.7% | 0.0% | 0.0% | 8.2 | 3.2 | 2.2 | 11.9% | 0 |
| Jungle Stampede mirror, damage wears off (MTG) | 51.2% | 69.0% | 17.6 ± 3.2 | 1.8% | 0.0% | 64.1% | 0.0% | 0.0% | 8.7 | 5.6 | 4.4 | 20.0% | 0 |
| Zoo Patrol mirror, damage wears off (MTG) | 50.6% | 66.4% | 19.1 ± 4.3 | 9.2% | 0.0% | 78.1% | 64.1% | 14.1% | 11.6 | 1.4 | 3.6 | 48.3% | 0 |
| Vesper's Ledger mirror, damage wears off (MTG) | 50.8% | 59.0% | 23.0 ± 4.6 | 26.8% | 0.0% | 69.5% | 0.0% | 0.0% | 17.5 | 26.4 | 12.4 | 67.6% | 0 |
| Sparkwrench Scrappers mirror, damage wears off (MTG) | 51.2% | 69.8% | 17.2 ± 2.7 | 1.2% | 0.4% | 84.5% | 0.0% | 0.0% | 18.4 | 0.0 | 2.0 | 31.7% | 0 |

## Going second (GAME_DESIGN §3)

*First-player win rate in mirrors (50% is fair). Current rule (MTG default): the first player skips their turn-1 draw, no other compensation.*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob mirror, current rule (MTG) | 56.6% | 78.8% | 13.5 ± 2.8 | 0.6% | 6.2% | 88.3% | 27.1% | 2.0% | 8.4 | 3.3 | 2.3 | 18.3% | 0 |
| Goober Mob mirror, everyone draws + 2nd player +1 mana on turn 1 | 55.0% | 72.4% | 13.2 ± 2.4 | 0.2% | 5.6% | 88.0% | 27.4% | 1.8% | 8.4 | 3.2 | 1.9 | 11.2% | 0 |
| Goober Mob mirror, everyone draws + 2nd player +1 mana and 1 Gold | 54.8% | 65.8% | 13.4 ± 2.4 | 0.2% | 3.6% | 89.5% | 21.9% | 1.3% | 8.9 | 3.4 | 2.7 | 14.9% | 0 |
| Jungle Stampede mirror, current rule (MTG) | 51.8% | 69.6% | 18.4 ± 4.7 | 6.6% | 0.0% | 71.8% | 32.1% | 10.4% | 9.3 | 6.7 | 4.3 | 38.6% | 0 |
| Jungle Stampede mirror, everyone draws + 2nd player +1 mana on turn 1 | 48.0% | 55.4% | 19.1 ± 5.3 | 10.4% | 0.0% | 72.6% | 35.7% | 11.7% | 10.3 | 7.4 | 4.5 | 44.7% | 0 |
| Jungle Stampede mirror, everyone draws + 2nd player +1 mana and 1 Gold | 51.2% | 52.6% | 19.0 ± 5.2 | 8.6% | 0.0% | 72.7% | 34.4% | 11.5% | 10.1 | 7.3 | 4.8 | 45.4% | 0 |
| Vesper's Ledger mirror, current rule (MTG) | 51.2% | 57.8% | 23.7 ± 5.3 | 33.0% | 0.0% | 80.2% | 41.1% | 9.2% | 18.9 | 27.0 | 12.6 | 71.1% | 0 |
| Vesper's Ledger mirror, everyone draws + 2nd player +1 mana on turn 1 | 53.2% | 57.8% | 23.3 ± 5.2 | 31.6% | 0.0% | 79.3% | 39.7% | 8.5% | 18.4 | 27.1 | 12.2 | 71.2% | 0 |
| Vesper's Ledger mirror, everyone draws + 2nd player +1 mana and 1 Gold | 55.4% | 60.8% | 23.4 ± 5.1 | 32.0% | 0.0% | 79.3% | 40.0% | 9.3% | 18.5 | 27.2 | 13.1 | 71.9% | 0 |

## Gold cap (GAME_DESIGN §5.2)

*Does the cap matter in the decks with the most Gold use?*

| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Vesper's Ledger mirror, Gold cap 3 | 51.0% | 58.0% | 23.6 ± 5.3 | 30.6% | 0.0% | 79.8% | 39.9% | 9.2% | 18.7 | 26.6 | 11.6 | 79.5% | 0 |
| Vesper's Ledger mirror, Gold cap 5 | 51.2% | 57.8% | 23.7 ± 5.3 | 33.0% | 0.0% | 80.2% | 41.1% | 9.2% | 18.9 | 27.0 | 12.6 | 71.1% | 0 |
| Vesper's Ledger mirror, Gold cap 8 | 51.6% | 59.0% | 23.7 ± 5.3 | 32.8% | 0.0% | 80.3% | 41.5% | 9.3% | 18.9 | 27.1 | 13.2 | 59.8% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 3 | 51.4% | 61.6% | 19.9 ± 4.5 | 11.8% | 0.4% | 95.2% | 76.8% | 21.1% | 22.5 | 0.0 | 2.1 | 69.8% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 5 | 51.4% | 61.6% | 19.9 ± 4.5 | 11.8% | 0.4% | 95.2% | 76.7% | 21.1% | 22.5 | 0.0 | 2.1 | 60.5% | 0 |
| Sparkwrench Scrappers mirror, Gold cap 8 | 51.4% | 61.6% | 19.9 ± 4.5 | 11.8% | 0.4% | 95.2% | 76.7% | 21.1% | 22.5 | 0.0 | 2.1 | 48.7% | 0 |
