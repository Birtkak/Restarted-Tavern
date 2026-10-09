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
  It rewrites `docs/playtest/SIMULATION_REPORT.md`. About 2 minutes for 1000 games per row. Experiments live in `Assets/Rules/AI/Experiments.cs`; experiment-only rule switches are on `FormatConfig` (`DamageWearsOff`, `SecondPlayerExtraCards`, `SecondPlayerStartingGold`).

---

## Findings so far (2026-10-09, 1000 games per row)

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
Gold spent per game is identical for caps 3, 5, 7 and 10. Only the wasted mana changes. The prototype pool has nothing to spend Gold on except a few cheap instants. In long games most unspent mana is lost: **70% in the Zoo mirror**, because Gold sits at the cap. Gold sinks (activated abilities, Patron Powers, Gold-paid relics) are needed before the cap means anything.

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
