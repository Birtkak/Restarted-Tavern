# Playtesting (Roadmap Step 3)

Goal (DEVELOPMENT §5): tune the **Gold cap**, the **curve**, and the **impact of permanent damage** before the full set gets built. Two sources feed this:

1. **Bot simulations**: thousands of `GreedyBot` games per rules variant. They give fast, repeatable answers to "what does this rule change?", but the bot is a careful beginner, so they're hints, not balance. Full tables: [SIMULATION_REPORT.md](SIMULATION_REPORT.md).
2. **Human playtests** on the debug table. These decide. Use the template at the bottom.

---

## How to run

- **Debug table**: open `Assets/Scenes/DebugTable.unity` → Play, or run the built exe (**Restarted Tavern → Build Windows Debug Table**). Tick *P2 bot* to play solo against the GreedyBot. The *P1/P2 deck* buttons pick the decks for the next *New game*.
- **Simulation report**: **Restarted Tavern → Run Simulation Report**, or headless:
  ```
  Unity.exe -batchmode -quit -projectPath . -executeMethod RestartedTavern.Client.Editor.SimulationMenu.RunReport -simGames 1000
  ```
  It rewrites `docs/playtest/SIMULATION_REPORT.md`. Games run in parallel; the full suite (84 matchups) takes about **6 minutes at the default 500 games per row**. Add `-simSections "round,second"` to run only the sections whose title contains those words. Experiments live in `Assets/Rules/AI/Experiments.cs`; experiment-only rule switches are on `FormatConfig` (`DamageWearsOff`, `SecondPlayerExtraCards`, `SecondPlayerStartingGold`).

---

> **Deck lists changed on 2026-10-09** (after these findings): each prototype deck swapped 4 cards for set v0.2 cards, and Sparkwrench Scrappers now has Equipment. The numbers below come from the old lists, so re-run the report before comparing.

## Findings: Tavern Dwellers and abilities (2026-10-09, 500 games per row)

Tavern Dwellers (passives and Powers), activated abilities and Equip are now in the engine. Six decks, each with its Tavern Dweller: Goober Mob (Skabba), Jungle Stampede (Mukk), Zoo Patrol (Keeper Z-00), Vesper's Ledger (Madame Vesper), Sparkwrench Scrappers (Sparkwrench) and the new Equipment deck **Auditor's Arsenal** (Auditor Prime). The "Tavern Dwellers" section of the report plays every mirror with and without them.

### 1. Are Tavern Dwellers the Gold sink? Partly: it depends on the Power

| Mirror | Wasted mana (with / without) | Gold spent per game | Powers per game | Turns (with / without) | Games over 25 turns |
|---|---|---|---|---|---|
| Goober Mob (Skabba) | **0.5%** / 18.3% | 2.2 / 2.3 | 1.4 | 13.0 / 13.5 | 0% / 1% |
| Zoo Patrol (Keeper Z-00) | **35.6%** / 70.8% | 7.2 / 4.0 | 7.2 | **20.2** / 23.2 | **13%** / 34% |
| Vesper's Ledger (Madame Vesper) | 80.2% / 71.2% | **23.8** / 12.6 | 6.6 | 23.8 / 23.7 | 35% / 33% |
| Jungle Stampede (Mukk) | 73.2% / 38.6% | 8.8 / 4.3 | 1.9 | 25.4 ± 15.9 / 18.4 | 25% / 7% |
| Sparkwrench Scrappers (Sparkwrench) | 60.5% / 60.5% | 2.1 / 2.1 | **0** | 19.9 / 19.9 | 12% / 12% |
| Auditor's Arsenal (Auditor Prime) | 0.2% / 0.4% | 1.5 / 1.8 | ~0 | 15.3 / 15.8 | 0% / 0% |

- **Powers that are always useful work as the universal sink.** Keeper Z-00's "(2) Heal 3" halves the wasted mana in the Zoo mirror and cuts long games from 34% to 13%: the board stops wearing down for nothing. Skabba's sacrifice-to-draw empties the bank in the Goober mirror.
- **Madame Vesper spends twice the Gold, but her passive also gives Gold** (1 per enemy creature death), so more of it hits the cap. Her deck needs more ways to spend it.
- **Situational Powers don't drain Gold.** Mukk's Trample grant only matters in combat, Sparkwrench's needs Equipment (the Sparkwrench Scrappers deck has none, so his Power and passive never do anything), and Auditor Prime's "look at the top card" is rarely worth a Gold to the bot. These Tavern Dwellers need a deck built around them, or a second use for the Power.
- **About 10–40% of Powers are used on the opponent's turn**, paid with Gold, which is what the rule is for. The bot holds instant-speed Powers for the opponent's end step unless there's a kill or a combat trick.
- ⚠ **The Jungle mirror is longer because of a bot limit, not because of Mukk.** The longest game (seed 12, 86 turns) is a board stall: from turn 30 both players sit at 4 life with 11–13 creatures each and 5 Gold, and neither attacks again until one decks out. The bot only goes all-in when the defender has no untapped blockers, and Mukk's +1/+0 makes more single blocks "unsafe". A human would attack with everything. A better attack AI (alpha strikes) is needed before game-length numbers for big-creature mirrors can be trusted.

### 2. The Gold cap now changes how much is wasted, but still not who wins

| Mirror | Wasted at cap 3 / 5 / 8 | Gold spent | Turns |
|---|---|---|---|
| Zoo Patrol | 46% / 36% / 27% | 6.8 / 7.2 / 7.3 | 20.2 at every cap |
| Vesper's Ledger | 85% / 80% / 71% | 21.4 / 23.8 / 24.5 | 24.1 / 23.8 / 23.8 |
| Sparkwrench Scrappers | 70% / 61% / 49% | 2.1 at every cap | 19.9 at every cap |
| Auditor's Arsenal | 1% / 0% / 0% | 1.6 / 1.5 / 1.5 | 15.3 at every cap |

- Win rates stay within ±2% and game length within 0.3 turns. Gold sinks cost 1–3 per turn, so a bank of 3 already covers them; a bigger cap only stores more Gold that nothing buys.
- Keep the cap at 5 for now. It will matter once the v0.2 Gold cards (Tip Jar, Settle the Tab, Offshore Account, Interest Broker…) are in decks, and with bigger Invest costs.

### 3. Equipment: works, the deck is weak
- **Auditor's Arsenal** uses about 6.4 abilities per game (mostly Equip) and spends almost all its mana: 0.2% wasted, the best of any deck.
- It loses to the creature decks (Goober Mob 80–20, Jungle 73–27, Zoo 65–35, Sparkwrench 72–28) and goes even with Vesper's Ledger. Games are short and few creatures die (3.4 per game in the mirror), so cheap bodies plus Equipment don't hold the board. That's card balance or the bot's Equip choices, not the Equip rule.
- The first player wins **80%** of the Auditor mirror, the highest of any deck: tempo decides a deck of cheap creatures and Equipment.

### 4. Other numbers to keep in mind
- **Going first** is still a big edge in every mirror (60–81%). Nothing in this change affects that; see RULES_REVIEW issue 1.
- **Matchup spread** is wide: Zoo Patrol beats Vesper's Ledger 93–7, Jungle beats Vesper 91–9. That's card balance in 15-card prototype decks.

---

## Earlier findings (2026-10-09, 1000 games per row)

➡ **The latest analysis, with 5 decks, 2 bot styles and the chip-damage metric, is in [RULES_REVIEW.md](RULES_REVIEW.md).** The sections below are from the earlier 3-deck runs.

Three prototype decks, all legal 60-card Standard decks: **Goober Mob** (Goobers + Neutral), **Jungle Stampede** (Wild + big Goobers + Neutral) and **Zoo Patrol** (Wild + Glitterworld: pings, fights, healing).

### 1. Going first: improved, still an edge ⚠️
The rule changed on 2026-10-09 from "the second player starts with 1 Gold" to **"+1 mana on the second player's first turn"** (GAME_DESIGN §3). First-player win rate in mirrors (50% is fair):

| 2nd-player compensation | Goober mirror | Jungle mirror |
|---|---|---|
| Draw skip only | 76.8% | 67.8% |
| 1 starting Gold (old rule) | 73.5% | 67.1% |
| **+1 mana on first turn (current rule)** | **66.4%** | **59.9%** |
| +1 mana on first turn and 1 Gold | 61.0% | 56.3% |
| +2 mana on first turn | 43.5% | 53.7% |
| +1 card and +1 mana on first turn | 67.2% | 67.8% |

- Gold barely helps on its own because it can't pay for creatures. Extra first-turn mana works.
- +2 mana overshoots for the aggressive Goober deck (the second player then wins 56%).
- **Candidate if human playtests agree:** +1 mana on the first turn **and** 1 Gold (61% / 56%).
- An extra card does nothing: the 7-card hand limit makes the second player discard it.

### 2. Permanent damage matters once the cards create damage ✅
With pings and fights in the pool (Zoo Patrol), the effect is large. Zoo Patrol mirror:

| | Permanent damage | Damage wears off (MTG) |
|---|---|---|
| Creatures carrying damage at turn start | **46.8%** | 13.1% |
| Creature deaths per game | **15.3** | 10.9 |
| Healing per game | **8.0** | 1.4 |
| Game length (turns) | **22.7** | 18.4 |

- Healing becomes a real card role (about 6× more healing used), as GAME_DESIGN §7.3 intends.
- Games get **longer**, not shorter: boards wear each other down instead of racing.
- In the creature-only decks (Goober Mob, Jungle Stampede) it barely matters (2–11% wounded). Big bodies trade and removal kills, so pings and fights are what make the mechanic show up. Every faction needs some chip damage, or players won't feel the rule.

### 3. The Gold cap can't be tuned yet
Gold spent per game is identical for caps 3, 5, 7 and 10. Only the wasted mana changes. In long games most unspent mana is lost: **69% in the Zoo mirror**, because Gold sits at the cap.
- Since 2026-10-09, Gold pays for everything except creatures (GAME_DESIGN §5.2). Gold spent went up (Zoo mirror: 3.2 → 4.6 per game), but outcomes barely moved: the bot prefers mana on its own turn, and these decks are mostly creatures.
- The cap becomes a real knob once Gold sinks exist: activated abilities, Tavern Dweller Powers, and the v0.2 Gold cards (Tip Jar, Settle the Tab, Offshore Account…). Rerun this experiment after those land.

### 4. Matchups and pace
- Jungle Stampede beats Goober Mob about **69–31**. Zoo Patrol beats Jungle Stampede **55–45**, and Goober Mob beats Zoo Patrol **56–44**. That's a rough rock-paper-scissors.
- Game length (both players' turns): about **13** (Goober mirror), **19** (Jungle mirror) and **23** (Zoo mirror).
- The bot races more and blocks less than people do, so treat tempo-related numbers as upper bounds.

---

## What to look for in human playtests

1. **Going second**: with +1 mana on your first turn, does going second still feel like a disadvantage?
2. **Gold**: do you ever hold mana back on purpose to bank Gold? When does the cap of 5 bite?
3. **Permanent damage**: do wounded creatures change your decisions (attack, block, heal)? Is it readable on the table?
4. **Curve**: are there turns where you have nothing to do? Does the 7-card hand limit force discards?
5. **Healing**: are Barkeep's Tonic and Jungle Remedy ever worth a card?

### Session note template
```
Date / seed / decks / who went first / winner / turns
What felt good:
What felt bad:
Rules questions that came up:
Cards that stood out (too strong / too weak / confusing):
```
