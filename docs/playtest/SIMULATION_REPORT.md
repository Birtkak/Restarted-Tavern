# Simulation Report

Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Both players are `GreedyBot`: a simple, deterministic, rule-based player. It plays like a careful beginner, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

1000 games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time 217s.

Columns: **A win%** = the first-named deck's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) · **Wasted** = the share of unspent mana lost to the Gold cap · **Gold spent** and **Opp-turn casts** are per game · **Wounded** = the share of creatures carrying damage at the start of a turn · **Deaths** = creature deaths per game · **Heal** = healing per game.

## Baseline

*How do the two prototype decks do against each other with the current rules?*

| Matchup | A win% | 1st win% | Draws | Turns | Wasted | Gold spent | Opp-turn casts | Wounded | Deaths | Heal |
|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob vs Jungle Stampede | 31.3% | 60.9% | 0 | 14.5 | 7.7% | 2.6 | 1.2 | 1.8% | 5.9 | 3.9 |
| Goober Mob mirror | 52.0% | 67.6% | 0 | 13.1 | 8.5% | 1.9 | 1.2 | 1.5% | 7.9 | 2.9 |
| Jungle Stampede mirror | 49.5% | 59.9% | 0 | 18.7 | 39.3% | 4.5 | 1.6 | 10.6% | 9.9 | 7.1 |
| Zoo Patrol vs Goober Mob | 45.0% | 67.8% | 0 | 16.0 | 26.4% | 2.8 | 2.5 | 19.7% | 12.3 | 3.4 |
| Zoo Patrol vs Jungle Stampede | 59.2% | 67.4% | 0 | 17.7 | 22.4% | 3.5 | 2.6 | 31.3% | 10.9 | 5.9 |
| Zoo Patrol mirror | 48.9% | 61.9% | 0 | 23.0 | 68.8% | 4.6 | 7.0 | 49.8% | 15.7 | 7.9 |

## Going-second compensation (GAME_DESIGN §3)

*How much should going second be compensated? Look at the first-player win rate in mirrors (50% is fair). Current rule (since 2026-10-09): the first player skips their first draw, and the second player has +1 mana on their first turn.*

| Matchup | A win% | 1st win% | Draws | Turns | Wasted | Gold spent | Opp-turn casts | Wounded | Deaths | Heal |
|---|---|---|---|---|---|---|---|---|---|---|
| Goober mirror, 2nd player: no compensation besides the draw skip | 53.9% | 78.1% | 0 | 13.5 | 13.8% | 2.2 | 1.2 | 1.9% | 8.3 | 3.2 |
| Jungle mirror, 2nd player: no compensation besides the draw skip | 52.0% | 67.8% | 0 | 18.5 | 37.8% | 4.4 | 1.2 | 10.5% | 9.4 | 6.9 |
| Goober mirror, 2nd player: 1 starting Gold (old rule) | 49.8% | 71.2% | 0 | 13.4 | 9.7% | 2.8 | 1.1 | 1.5% | 8.4 | 3.0 |
| Jungle mirror, 2nd player: 1 starting Gold (old rule) | 50.5% | 67.1% | 0 | 18.4 | 38.9% | 4.9 | 1.4 | 10.9% | 9.2 | 5.9 |
| Goober mirror, 2nd player: +1 mana on first turn (current rule) | 52.0% | 67.6% | 0 | 13.1 | 8.5% | 1.9 | 1.2 | 1.5% | 7.9 | 2.9 |
| Jungle mirror, 2nd player: +1 mana on first turn (current rule) | 49.5% | 59.9% | 0 | 18.7 | 39.3% | 4.5 | 1.6 | 10.6% | 9.9 | 7.1 |
| Goober mirror, 2nd player: +1 mana on first turn and 1 Gold | 53.0% | 61.8% | 0 | 13.3 | 16.8% | 2.7 | 1.2 | 1.2% | 8.3 | 3.1 |
| Jungle mirror, 2nd player: +1 mana on first turn and 1 Gold | 49.3% | 56.3% | 0 | 18.6 | 40.3% | 4.9 | 1.2 | 10.4% | 9.7 | 6.9 |
| Goober mirror, 2nd player: +2 mana on first turn | 51.2% | 44.4% | 0 | 12.8 | 11.9% | 1.7 | 1.2 | 1.7% | 7.7 | 2.9 |
| Jungle mirror, 2nd player: +2 mana on first turn | 50.3% | 53.7% | 0 | 18.7 | 41.6% | 4.1 | 1.5 | 11.4% | 10.2 | 7.6 |
| Goober mirror, 2nd player: +1 card and +1 mana on first turn | 49.1% | 66.7% | 0 | 13.3 | 14.5% | 1.8 | 1.1 | 1.9% | 8.5 | 3.1 |
| Jungle mirror, 2nd player: +1 card and +1 mana on first turn | 51.4% | 67.8% | 0 | 18.5 | 38.9% | 4.4 | 1.7 | 10.5% | 9.6 | 7.3 |

## Gold cap (GAME_DESIGN §5.2)

*What does the cap of 5 do? Compare wasted mana, Gold spent and instant-speed play.*

| Matchup | A win% | 1st win% | Draws | Turns | Wasted | Gold spent | Opp-turn casts | Wounded | Deaths | Heal |
|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob vs Jungle Stampede, Gold cap 3 | 31.4% | 60.8% | 0 | 14.5 | 12.0% | 2.6 | 1.2 | 1.8% | 5.9 | 3.9 |
| Goober Mob vs Jungle Stampede, Gold cap 5 | 31.3% | 60.9% | 0 | 14.5 | 7.7% | 2.6 | 1.2 | 1.8% | 5.9 | 3.9 |
| Goober Mob vs Jungle Stampede, Gold cap 7 | 31.3% | 60.9% | 0 | 14.5 | 5.3% | 2.6 | 1.2 | 1.8% | 5.9 | 3.9 |
| Goober Mob vs Jungle Stampede, Gold cap 10 | 31.3% | 60.9% | 0 | 14.5 | 3.0% | 2.6 | 1.2 | 1.8% | 5.9 | 3.9 |

## Permanent damage (GAME_DESIGN §7.3)

*What does permanent damage change compared to MTG-style damage that wears off at end of turn?*

| Matchup | A win% | 1st win% | Draws | Turns | Wasted | Gold spent | Opp-turn casts | Wounded | Deaths | Heal |
|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob vs Jungle Stampede, permanent damage | 31.3% | 60.9% | 0 | 14.5 | 7.7% | 2.6 | 1.2 | 1.8% | 5.9 | 3.9 |
| Goober mirror, permanent damage | 52.0% | 67.6% | 0 | 13.1 | 8.5% | 1.9 | 1.2 | 1.5% | 7.9 | 2.9 |
| Jungle mirror, permanent damage | 49.5% | 59.9% | 0 | 18.7 | 39.3% | 4.5 | 1.6 | 10.6% | 9.9 | 7.1 |
| Zoo Patrol vs Jungle Stampede, permanent damage | 59.2% | 67.4% | 0 | 17.7 | 22.4% | 3.5 | 2.6 | 31.3% | 10.9 | 5.9 |
| Zoo Patrol mirror, permanent damage | 48.9% | 61.9% | 0 | 23.0 | 68.8% | 4.6 | 7.0 | 49.8% | 15.7 | 7.9 |
| Goober Mob vs Jungle Stampede, damage wears off (MTG) | 32.7% | 61.3% | 0 | 14.5 | 5.4% | 2.6 | 1.2 | 0.0% | 5.8 | 3.8 |
| Goober mirror, damage wears off (MTG) | 52.8% | 67.8% | 0 | 13.0 | 7.0% | 1.9 | 1.2 | 0.0% | 7.8 | 2.9 |
| Jungle mirror, damage wears off (MTG) | 49.3% | 61.1% | 0 | 17.8 | 17.0% | 4.5 | 1.8 | 0.0% | 9.1 | 6.0 |
| Zoo Patrol vs Jungle Stampede, damage wears off (MTG) | 57.1% | 67.5% | 0 | 16.6 | 11.2% | 3.9 | 2.6 | 6.2% | 8.7 | 3.1 |
| Zoo Patrol mirror, damage wears off (MTG) | 49.8% | 61.6% | 0 | 18.9 | 46.1% | 4.1 | 4.7 | 16.4% | 11.6 | 1.4 |
