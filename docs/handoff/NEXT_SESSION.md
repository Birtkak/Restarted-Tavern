# Next session: handoff prompt

Paste everything below the line into a new session.

---

You're continuing work on Restarted Tavern, a Unity 6 (6000.6.4f1) + C# trading-card game.
Repo: C:\Users\Birre\Desktop\Claude shizzle\Restarted-Tavern (GitHub: Birtkak/Restarted-Tavern, main).
Run `git status` and `git log origin/main..` first and tell the user what's pending.

STATE AT THE END OF THE LAST SESSION (2026-10-10), 254 EditMode tests, all green:
- **Standard rules = Legends of Runeterra rounds** (GAME_DESIGN §6, §6.1): a round is everyone's turn. Everyone gets
  +1 max mana, refills, untaps and draws; then players alternate single actions (play a card of any type, use an
  ability or Power, attack, or pass) from the round leader; responses on the Chain don't use an action; two passes in a
  row end the round. The round leader holds the attack token (one attack per round, passes every round). No going-first
  compensation (first player 45–54% in all bot mirrors). Unspent mana becomes Gold at round end (cap 3), Gold is spent
  first on spells and abilities, permanents use mana only.
- **No summoning sickness and no Haste** (§7.4): removed from the engine and every card.
- "Turn" in MTG rules and on cards means "round" (Powers once each round, "until end of turn" = the round).
- Earlier structures stay as `FormatConfig.MtgTurns()`, `RuneterraRotation()`, `Classic()`. Card tests run under
  `MtgTurns()` (`TestGame.AtFirstMainPhase`); the rounds have `RoundsTests`.
- **Balance pass 1** (playtest/RULES_REVIEW.md "Balance pass 1", Decision Log): 6 nerfs (Madame Morbida, The Final Act,
  Exhumation Broadcast, Grid Overload, The Dealer, Archon Lumen, Neon Executioner) and 5 deck swaps. Prototype decks
  vs the field: Goober 58% (user kept it), the others 46–52%, spread 3.4. Matchups past 65%: Goober vs Auditor 69%,
  Auditor vs Zoo 68%.
- Cards are data (`Assets/StreamingAssets/Cards/*.json`, decks in `Decks/prototype_decks.json`, DEVELOPMENT §3).

GOAL OF THIS SESSION: ask the user what's next (AskUserQuestion, multiple choice, recommended option first).

CANDIDATE NEXT STEPS
1. **Balance pass 2**: card power table (playtest/CARD_POWER.md). Top: Final Broadcast +12.5 (user kept it once),
   Sproutling +12.3 (grows every round now), Tusked Mammoth, The Final Act, Ironbark Grizzly. Bottom (worse than a
   vanilla 2/3): Insider Trading, Golden Parachute, Mercenary Contract, Watering Hole, Satellite Uplink, most Relics and
   Equipment, Gold cards. Propose nerfs/buffs in batches, measure each, then re-tune the decks. Goober Mob at 58%: the
   user declined both a Mob Rush nerf and a deck swap (Mob Rush → Chaos Engine gave 52%); ask again after playtests.
2. **Open design flaws** (RULES_REVIEW, design review 2026-10-10): R2 ("whenever you attack" cards trigger every other
   round, Powers once per round), R4 (multiplayer rounds and the attack token), R5 (Gold cap waste: 26% of leftover
   mana), R6 (permanent damage barely matters outside ping decks), R7 (stalls), R9 (Gold cap 3 vs "gain 5 Gold"
   downsides), R10 (repeatable Health buffs).
3. **Visual client** (DEVELOPMENT §5 roadmap step 4): a real Unity hot-seat table for human playtests of the rounds.
4. **Bot**: attack timing in rounds (`BotStyle.AttackFirstInRound` is deck-dependent; a per-round decision would help);
   the bot sometimes "attacks" with nothing.

TOOLS (Tools/SimRunner, run freely, ~15 s–5 min)
- Build: "C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Data/DotNetSdk/dotnet.exe" build Tools/SimRunner -c Release
- Run: Tools/SimRunner/bin/Release/net8.0/SimRunner.exe
  - (no mode) the report (docs/playtest/SIMULATION_REPORT.md); `-balance [-cards]` win matrix (+ per-card cast stats);
  - `-scan [-only ids] [-out file]` card power table; `-impact -decks 0,2` each card vs a 2/3 filler;
  - `-optimize -decks 5 -swaps 3 -target 0.5` deck tuning by measurement; `-goingfirst`; `-h2h [-off Switch]`; `-trace`;
  - `-data <folder>` loads a copy of StreamingAssets (test card changes without editing the repo);
  - `-rules standard|mtg|rotation|classic`. Decks: 0 Goober, 1 Jungle, 2 Zoo, 3 Vesper, 4 Sparkwrench, 5 Auditor.

HOW TO WORK WITH THIS USER
- The user has the vision and wants Claude to propose details. Ask design questions with AskUserQuestion, recommended
  option first, MTG default next to alternatives. Record every decision in GAME_DESIGN.md and its Decision Log.
- Simulations and bot experiments may be run without asking. Unit tests are expected.
- Card text says "an opponent" / "each opponent", never "your opponent". Say "Tavern Dweller", never "Patron".
- Commit when a piece of work is done; ask before pushing.

PRACTICAL NOTES
- Tests headless (~40 s): "C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -nographics
  -projectPath <repo> -runTests -testPlatform EditMode -testResults <file>.xml -logFile <log> (results outside the repo).
- Multi-line edits: Python scripts in the scratchpad, files opened with newline='' (LF). Card JSON: edit by exact text
  replacement to keep the canonical format (`CardDataTests`).
- New files get .meta files on the next Unity run: commit them together.
