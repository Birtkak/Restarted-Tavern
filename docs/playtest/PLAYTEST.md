# Playtesting (Roadmap Step 3)

Goal (DEVELOPMENT §5): tune the **Gold cap**, the **curve**, and the **impact of permanent damage** before the full set gets built. Two sources feed this:

1. **Bot simulations**: thousands of `GreedyBot` games. They give fast, repeatable answers, but the bot is a careful beginner, so they're hints, not balance. Tables: [SIMULATION_REPORT.md](SIMULATION_REPORT.md) (game shape) and [CARD_POWER.md](CARD_POWER.md) (card power). Analysis: [RULES_REVIEW.md](RULES_REVIEW.md).
2. **Human playtests** on the debug table. These decide. Use the template at the bottom.

---

## How to run

- **Debug table** (human playtests): run `Builds/DebugTable/RestartedTavern.exe` (build it with **Restarted Tavern → Build Windows Debug Table**), or open `Assets/Scenes/DebugTable.unity` → Play.
  - You are **P1 (bottom)** against the GreedyBot (P2 bot is on by default; untick it, or start with `-hotseat`, for two humans).
  - The *P1/P2 deck* buttons pick the decks for the next *New game*. **Rules** shows a one-page rules summary.
  - Hover over any card to read it in **Card details** (stats, damage, keywords, full text, attachments). Your possible actions are the buttons on the right; click a card to filter them.
  - Every finished game is saved to `Builds/DebugTable/Playtests/` (*Save log* saves the current one). Each file has the decks, seed, who went first, the result and the full log, plus a **Notes** line for your feedback.
- **Simulation report** (fast way): `Tools/SimRunner`, a small .NET 8 console app that compiles the same `Assets/Rules` source files. Build it with the .NET SDK that ships with Unity, then run it from the repo:
  ```
  "C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Data/DotNetSdk/dotnet.exe" build Tools/SimRunner -c Release
  Tools/SimRunner/bin/Release/net8.0/SimRunner.exe [-simGames 500] [-simSections "round,styles"] [-out file.md]
  ```
  The report (33 matchups, 16,500 games) takes about **35 seconds** on 12 cores. Unity's Mono runtime barely uses more than one core for this, so the same report inside Unity takes several minutes. Results are identical either way.
- **Simulation report** (inside Unity): **Restarted Tavern → Run Simulation Report**, or headless `Unity.exe -batchmode -quit -projectPath . -executeMethod RestartedTavern.Client.Editor.SimulationMenu.RunReport`. Same arguments.
- Both rewrite `docs/playtest/SIMULATION_REPORT.md` by default. `-simSections "round"` runs only the sections whose title contains that word. The report is built in `Assets/Rules/AI/Experiments.cs`.
- **Balance tools** (SimRunner): `-balance [-cards]` (win-rate matrix), `-scan` (card power table), `-impact`, `-optimize` (deck tuning by measurement), `-data <folder>` (a changed copy of StreamingAssets), `-trace` (one readable bot game), `-h2h` (bot against bot versions). See DEVELOPMENT.md §7.

---

## Set v0.3 deck pass (approved 2026-10-09)

The 36 v0.3 cards (mana scarcity) went into the decks only where they held their own against the card they replace:
Goober: Overrun the Gates → Goober Avalanche · Jungle: Titanback Colossus → Rampaging Titan, Brawling Runt → Mossgut Grower, Kick 'Em → Gift of the Grove · Zoo: Primal Clash → Arc Cascade, Chain Zap → Overclocked Analyst · Vesper: Apprentice Forger → Final Broadcast, Compound Interest → Séance Hotline · Sparkwrench: Goober Rascal → Market Data Feed, Chain Zap → Orbital Laser, Scrap Collector → Goober Bookie · Auditor: Hardlight Aegis → Eviction Notice.

Balance pass 1 (2026-10-10) changed the lists again: see RULES_REVIEW.md, "Balance pass 1".

## Bot improvements (BotStyle switches)

Method: read single bot games (`SimRunner -trace`), fix what looks wrong behind a `BotStyle` switch, then measure head-to-head (`SimRunner -h2h`: the current bot against `BotStyle.Baseline()`, or with `-off Switch` against itself minus one switch), seats swapped.

| Found in traces | Change (BotStyle switch) | Status |
|---|---|---|
| Skabba's "sacrifice: draw" used with a full hand, then discarding at the end of the round | `AvoidOverdraw`: cards beyond the free hand space are worth almost nothing | on |
| Attacks are rare with the attack token | `AttackTokenUrgency` 1.4: damage on an attack round is worth 40% more | on |
| — | `CrackBackCountsTheirBuildTurn` (more careful attacks) | off (worse) |
| Spark Drone played into an empty board kills itself with its own Arrival | `ValueArrivalDamage`: Arrival damage counts its best target, or the harm when only own creatures are left | on |
| — | `AttackFirstInRound`: attack as the first action instead of after developing | off (deck-dependent: Goober prefers last, Zoo first) |

---

## What to look for in human playtests

1. **The rounds** (GAME_DESIGN §6): does trading single actions feel good, or slow? Do you hold back to answer, or pass to make the opponent commit first? Does either seat feel stronger? With no summoning sickness, do new creatures attacking right away feel fair? Do the "whenever you attack" cards feel weak?
2. **Gold**: do you ever hold mana back on purpose to bank Gold? When does the cap of 3 bite? Does "Gold first" ever pay with Gold you wanted to keep?
3. **Permanent damage**: do wounded creatures change your decisions (attack, block, heal)? Is it readable on the table?
4. **Curve**: are there rounds where you have nothing to do? Does the 7-card hand limit force discards?
5. **Healing**: are Barkeep's Tonic and Jungle Remedy ever worth a card?

### Session note template
```
Date / seed / decks / who went first / winner / rounds
What felt good:
What felt bad:
Rules questions that came up:
Cards that stood out (too strong / too weak / confusing):
```
