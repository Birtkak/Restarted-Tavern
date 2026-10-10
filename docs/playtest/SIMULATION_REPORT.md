# Simulation Report

Bot-vs-bot results under the Standard rules (Legends of Runeterra rounds). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

500 games per row · decks swap seats every game, and who goes first is random · games over 120 rounds count as draws · run time 33s.

Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · **Rounds** = average game length ± standard deviation · **Long** / **Short** = share of games over 25 / under 10 rounds · **Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · **Chip→death** = share of *chip damage* (damage a creature carried into a later round) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · **Wounded** = share of creatures carrying damage at the start of a round · **Deaths** / **Heal** / **Gold spent** are per game · **Powers** = Tavern Dweller Powers used per game (in brackets: share used while an opponent had the action) · **Abil.** = other activated abilities per game (Equip, Tap abilities...) · **Off-turn** = spells cast and abilities/Powers used while an opponent had the action, per game · **Wasted** = share of unspent mana lost to the Gold cap.

## Round robin (Greedy bots)

*How long are games, and does damage on creatures decide anything? Every deck against every deck.*

| Matchup | A win% | 1st win% | Rounds | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
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

| Matchup | A win% | 1st win% | Rounds | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |
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
