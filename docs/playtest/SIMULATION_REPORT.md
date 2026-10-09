# Simulation Report

Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Both players are `GreedyBot`: a simple, deterministic, rule-based player. It plays like a careful beginner, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.

1000 games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time 145s.

Columns: **A win%** = the first-named deck's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) · **Wasted** = the share of unspent mana lost to the Gold cap · **Gold spent** and **Opp-turn casts** are per game · **Wounded** = the share of creatures carrying damage at the start of a turn · **Deaths** = creature deaths per game · **Heal** = healing per game.

## Baseline

*How do the two prototype decks do against each other with the current rules?*

| Matchup | A win% | 1st win% | Draws | Turns | Wasted | Gold spent | Opp-turn casts | Wounded | Deaths | Heal |
|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob vs Jungle Stampede | 33.5% | 64.9% | 0 | 14.8 | 15.2% | 3.0 | 1.2 | 2.7% | 6.0 | 3.6 |
| Goober Mob mirror | 52.3% | 73.5% | 0 | 13.2 | 7.2% | 2.2 | 1.3 | 1.6% | 7.7 | 3.1 |
| Jungle Stampede mirror | 50.5% | 67.1% | 0 | 18.4 | 38.9% | 4.9 | 1.4 | 10.9% | 9.2 | 5.9 |

## Going-second compensation (GAME_DESIGN §3)

*How much should going second be compensated? Look at the first-player win rate in mirrors (50% is fair). Current rule: the first player skips their first draw, and the second player starts with 1 Gold.*

| Matchup | A win% | 1st win% | Draws | Turns | Wasted | Gold spent | Opp-turn casts | Wounded | Deaths | Heal |
|---|---|---|---|---|---|---|---|---|---|---|
| Goober mirror, 2nd player: no compensation besides the draw skip | 52.4% | 76.8% | 0 | 13.3 | 8.0% | 1.7 | 1.3 | 1.7% | 7.6 | 3.3 |
| Jungle mirror, 2nd player: no compensation besides the draw skip | 52.0% | 67.8% | 0 | 18.5 | 37.8% | 4.4 | 1.2 | 10.5% | 9.4 | 6.9 |
| Goober mirror, 2nd player: 1 Gold (current rule) | 52.3% | 73.5% | 0 | 13.2 | 7.2% | 2.2 | 1.3 | 1.6% | 7.7 | 3.1 |
| Jungle mirror, 2nd player: 1 Gold (current rule) | 50.5% | 67.1% | 0 | 18.4 | 38.9% | 4.9 | 1.4 | 10.9% | 9.2 | 5.9 |
| Goober mirror, 2nd player: 2 Gold | 51.7% | 73.9% | 0 | 13.2 | 7.6% | 2.8 | 1.7 | 1.7% | 7.8 | 2.6 |
| Jungle mirror, 2nd player: 2 Gold | 51.2% | 61.4% | 0 | 18.6 | 40.2% | 5.8 | 1.5 | 11.5% | 9.5 | 5.9 |
| Goober mirror, 2nd player: +1 card, no Gold | 53.5% | 76.9% | 0 | 13.5 | 15.1% | 1.7 | 1.3 | 2.3% | 8.2 | 3.6 |
| Jungle mirror, 2nd player: +1 card, no Gold | 50.4% | 74.0% | 0 | 18.3 | 38.7% | 4.4 | 1.3 | 10.6% | 9.3 | 7.0 |
| Goober mirror, 2nd player: +1 card and 1 Gold | 53.0% | 73.8% | 0 | 13.6 | 19.0% | 2.2 | 1.2 | 2.5% | 8.5 | 3.4 |
| Jungle mirror, 2nd player: +1 card and 1 Gold | 51.4% | 72.0% | 0 | 18.3 | 41.4% | 4.9 | 1.4 | 11.5% | 9.4 | 5.8 |
| Goober mirror, 2nd player: +1 mana on first turn (Coin), no Gold | 52.0% | 66.4% | 0 | 13.1 | 7.2% | 1.5 | 1.3 | 1.4% | 7.6 | 3.1 |
| Jungle mirror, 2nd player: +1 mana on first turn (Coin), no Gold | 49.5% | 59.9% | 0 | 18.7 | 39.3% | 4.5 | 1.6 | 10.6% | 9.9 | 7.1 |
| Goober mirror, 2nd player: +1 card and +1 mana on first turn | 50.2% | 67.2% | 0 | 13.3 | 12.5% | 1.5 | 1.2 | 1.7% | 8.2 | 3.4 |
| Jungle mirror, 2nd player: +1 card and +1 mana on first turn | 51.4% | 67.8% | 0 | 18.5 | 38.9% | 4.4 | 1.7 | 10.5% | 9.6 | 7.3 |

## Gold cap (GAME_DESIGN §5.2)

*What does the cap of 5 do? Compare wasted mana, Gold spent and instant-speed play.*

| Matchup | A win% | 1st win% | Draws | Turns | Wasted | Gold spent | Opp-turn casts | Wounded | Deaths | Heal |
|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob vs Jungle Stampede, Gold cap 3 | 33.5% | 64.7% | 0 | 14.8 | 20.9% | 3.0 | 1.2 | 2.7% | 6.0 | 3.6 |
| Goober Mob vs Jungle Stampede, Gold cap 5 | 33.5% | 64.9% | 0 | 14.8 | 15.2% | 3.0 | 1.2 | 2.7% | 6.0 | 3.6 |
| Goober Mob vs Jungle Stampede, Gold cap 7 | 33.5% | 64.9% | 0 | 14.8 | 11.9% | 3.0 | 1.2 | 2.7% | 6.0 | 3.6 |
| Goober Mob vs Jungle Stampede, Gold cap 10 | 33.5% | 64.9% | 0 | 14.8 | 8.2% | 3.0 | 1.2 | 2.7% | 6.0 | 3.6 |

## Permanent damage (GAME_DESIGN §7.3)

*What does permanent damage change compared to MTG-style damage that wears off at end of turn?*

| Matchup | A win% | 1st win% | Draws | Turns | Wasted | Gold spent | Opp-turn casts | Wounded | Deaths | Heal |
|---|---|---|---|---|---|---|---|---|---|---|
| Goober Mob vs Jungle Stampede, permanent damage | 33.5% | 64.9% | 0 | 14.8 | 15.2% | 3.0 | 1.2 | 2.7% | 6.0 | 3.6 |
| Goober mirror, permanent damage | 52.3% | 73.5% | 0 | 13.2 | 7.2% | 2.2 | 1.3 | 1.6% | 7.7 | 3.1 |
| Jungle mirror, permanent damage | 50.5% | 67.1% | 0 | 18.4 | 38.9% | 4.9 | 1.4 | 10.9% | 9.2 | 5.9 |
| Goober Mob vs Jungle Stampede, damage wears off (MTG) | 34.9% | 66.3% | 0 | 14.6 | 6.9% | 3.0 | 1.2 | 0.0% | 5.8 | 3.4 |
| Goober mirror, damage wears off (MTG) | 52.7% | 73.7% | 0 | 13.2 | 4.6% | 2.2 | 1.3 | 0.0% | 7.5 | 3.1 |
| Jungle mirror, damage wears off (MTG) | 50.9% | 68.9% | 0 | 17.5 | 17.7% | 4.9 | 1.5 | 0.0% | 8.6 | 4.7 |
