# Next session: handoff prompt

Paste everything below the line into a new session.

---

You're continuing work on Restarted Tavern, a Unity 6 (6000.6.4f1) + C# trading-card game.
Repo: C:\Users\Birre\Desktop\Claude shizzle\Restarted-Tavern (GitHub: Birtkak/Restarted-Tavern, main).

STATE AT THE END OF THE LAST SESSION (2026-10-10), 249 EditMode tests, all green:
- **Standard rules = Runeterra-style mana** (GAME_DESIGN §5–6, §6.1): a round pool (everyone gains +1 max mana and
  refills when a round starts; mana lasts the round), unspent mana becomes Gold at the end of the round, Gold cap 3,
  spells and abilities pay **Gold first**, the round leader rotates (A B | B A) and only the round leader may attack
  (attack token). `FormatConfig.Classic()` keeps the old rules; `TestGame.Classic()` runs old card tests under them.
- Gold first solved RULES_REVIEW #6 (the sequencing trap). Gold-only parts (Invest, "pay X Gold") are set aside
  before the cost takes Gold (`Payment.TrySplit`). Velvet Embezzler draws at 3+ Gold; Compound Interest checks
  "if 3 or more Gold was spent to cast it".
- New Powers: Mukk (3) a Trample creature you control fights a creature you don't control; Sparkwrench (2) attach up to
  one Equipment to a creature you control, else it gets +1/+1; Auditor Prime (2) draw a card, only with 3+ Gold.
- Snik chooses its Goobers on resolution (`DecisionKind.ChooseUpTo`, MTG 608.2d).
- **Replacement effects** (GAME_DESIGN §8.1, MTG 614–616): dying, damage, entering, drawing, gaining life / Gold.
  Fixed order (self-replacement, then oldest first); the affected player doesn't choose yet.
- **Cards are data**: every card is in `Assets/StreamingAssets/Cards/*.json`, the decks in
  `Assets/StreamingAssets/Decks/prototype_decks.json` (DEVELOPMENT §3). C# only has the building blocks.
  Editing a card = editing JSON, then run the tests (`CardDataTests` checks the canonical format).

GOAL OF THIS SESSION: ask the user what's next (AskUserQuestion, multiple choice, recommended option first).

CANDIDATE NEXT STEPS
0. **The 2026-10-10 design review** (docs/handoff/NEXT_DESIGN_FLAWS.md, written by a parallel session): flaws 1–7, 9
   and 10 are open design questions (double turns and the attack token, going first under rotation, multiplayer, Gold
   cap waste, permanent damage, stalls, shady deals at cap 3, Health buffs). Offer it next to the visual client.
1. **Visual client** (DEVELOPMENT §5 roadmap step 4, recommended; the user wanted the to-dos done first and they are):
   a real Unity hot-seat table for human playtests.
2. Human playtests of the new Standard rules on the debug table (Going first stays the MTG default until
   playtests judge it; RULES_REVIEW #1).
3. Balance pass with the new Powers and the 3-Gold changes (sims are fine to run, see below).
4. Smaller engine gaps (DEVELOPMENT §7 "Not yet implemented"): the affected player choosing the order of
   replacement effects, filtering events by hidden information, a targetable Tavern Dweller zone (design question).
5. Generate rules text from the card data, so text and behavior can't disagree (DEVELOPMENT §3).

READ FIRST
- docs/GAME_DESIGN.md: the rules. MTG Comprehensive Rules are the backbone (§1.1). The Decision Log is the source of truth.
- docs/DEVELOPMENT.md §3 (card data) and §7 (engine status, "Not yet implemented").
- docs/cards/*.md: the card lists (design docs). The engine's truth is the JSON in Assets/StreamingAssets/Cards.
- docs/playtest/PLAYTEST.md and RULES_REVIEW.md.

NAMING: "Tavern Dweller" (never "Patron"); `TavernDweller` in code. Card text says "an opponent" / "each opponent".

HOW TO WORK WITH THIS USER
- The user has the vision and wants Claude to propose details. For design questions, use AskUserQuestion with
  multiple-choice options, recommended option first, and show the MTG default next to alternatives. Record every
  decision in the docs and the Decision Log.
- Simulations and bot experiments may be run without asking (quick now; the user lifted the old rule on 2026-10-10). Unit tests are expected.
- Commit when a piece of work is done; ask before pushing to GitHub.

PRACTICAL NOTES
- Run tests headless (~40 s): "C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -nographics
  -projectPath <repo> -runTests -testPlatform EditMode -testResults <file>.xml -logFile <log>
  Only one Unity instance can open the project at a time. Write results/logs outside the repo.
- Build the debug table: -executeMethod RestartedTavern.Client.Editor.DebugTableBuilder.BuildWindows
  (Builds/DebugTable/RestartedTavern.exe; it reads the card files from RestartedTavern_Data/StreamingAssets).
  `-autoshot <png>` takes a screenshot and quits.
- SimRunner (~15-30 s, run freely): "C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Data/DotNetSdk/dotnet.exe" build
  Tools/SimRunner -c Release, then Tools/SimRunner/bin/Release/net8.0/SimRunner.exe [-balance] [-trace] [-h2h] ...
  It finds the card files by walking up from the working directory to Assets/StreamingAssets.
- Multi-line edits: write a Python script to the scratchpad and run it; open files with newline='' (the repo keeps
  .cs/.md/.json as LF). Bash heredocs that contain apostrophes sometimes break in this shell.
- New files get .meta files on the next Unity run: commit them together.
