# Playtesting (Roadmap Step 3)

Goal (DEVELOPMENT §5): tune the **Gold cap**, the **curve**, and the **impact of permanent damage** before the full set gets built. Two sources feed this:

1. **Bot simulations**: thousands of `GreedyBot` games per rules variant. They give fast, repeatable answers to "what does this rule change?", but the bot is a careful beginner, so they're hints, not balance. Full tables: [SIMULATION_REPORT.md](SIMULATION_REPORT.md).
2. **Human playtests** on the debug table. These decide. Use the template at the bottom.

---

## How to run

- **Debug table**: open `Assets/Scenes/DebugTable.unity` → Play, or run the built exe (**Restarted Tavern → Build Windows Debug Table**). Tick *P2 bot* to play solo against the GreedyBot.
- **Simulation report**: **Restarted Tavern → Run Simulation Report**, or headless:
  ```
  Unity.exe -batchmode -quit -projectPath . -executeMethod RestartedTavern.Client.Editor.SimulationMenu.RunReport -simGames 1000
  ```
  It rewrites `docs/playtest/SIMULATION_REPORT.md`. About 2 minutes for 1000 games per row. Experiments live in `Assets/Rules/AI/Experiments.cs`; experiment-only rule switches are on `FormatConfig` (`DamageWearsOff`, `SecondPlayerExtraCards`, `SecondPlayerFirstTurnBonusMana`).

---

## Findings so far (2026-10-09, 1000 games per row)

### 1. Going first is a big advantage ⚠️
In mirror matches the player who goes first wins **73% (Goober Mob) and 67% (Jungle Stampede)** with the current rule (the first player skips their first draw, and the second player gets 1 Gold). 50% would be fair; MTG is around 52–55%.

| 2nd-player compensation | Goober mirror, 1st wins | Jungle mirror, 1st wins |
|---|---|---|
| Draw skip only | 76.8% | 67.8% |
| **1 Gold (current)** | **73.5%** | **67.1%** |
| 2 Gold | 73.9% | 61.4% |
| +1 card | 76.9% | 74.0% |
| +1 mana on the first turn (a "Coin") | **66.4%** | **59.9%** |
| +1 card and +1 mana on the first turn | 67.2% | 67.8% |

- **Gold barely helps the second player**, because Gold can't pay for creatures (§5.2). It only fuels instants, and these decks have few.
- **An extra card does nothing**: with a 7-card hand limit, the second player usually discards it at their first cleanup.
- **Extra mana on the first turn helps most.** It lets the second player keep up on board, which is where these decks fight.
- Caveat: the bot races more and blocks less than people do, which makes tempo matter more. The real number is probably lower, but likely still well above 55%.

### 2. The Gold cap can't be tuned yet
Gold spent per game is **identical for caps 3, 5, 7 and 10** (3.0 per game in Goobers vs Jungle). Only the wasted mana changes (21% → 8%). The prototype pool has **nothing to spend Gold on** except four cheap instants: no activated abilities, no Patron Powers, and no Gold-paid relics. The cap only becomes a real knob once those exist.

### 3. Permanent damage barely matters in this card pool
Switching to MTG-style damage (wears off at end of turn) changes win rates by about 1% and game length by under 1 turn. At the start of a turn only **2–11% of creatures carry damage**. These decks deal lethal-or-nothing damage: big bodies trade, removal kills. The cards that make wounds matter aren't in the prototype pool yet: Glitterworld pings, Wild fights, and payoffs for damaged creatures.

### 4. Matchup and pace
- Jungle Stampede beats Goober Mob about **66–34**. Big Wild bodies (4/5, 5/5 Trample) outclass Goober tokens, and the Goober deck has no reach beyond combat.
- Games are short: **13 turns** (Goober mirror) to **18 turns** (Jungle mirror) counted over both players, so about 7–9 turns each.

---

## What to look for in human playtests

1. **Going second**: does it feel hopeless? Try the Coin variant by hand (give yourself +1 mana on your first turn).
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
