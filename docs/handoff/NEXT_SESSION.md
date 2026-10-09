# Next session: handoff prompt

Paste everything below the line into a new session.

---

You're continuing work on Restarted Tavern, a Unity 6 (6000.6.4f1) + C# trading-card game.
Repo: C:\Users\Birre\Desktop\Claude shizzle\Restarted-Tavern (GitHub: Birtkak/Restarted-Tavern, main).
Last commit of the previous session: 2cf840f "Activated abilities, Equip and Tavern Dwellers (formerly Patrons)", pushed.

GOAL OF THIS SESSION: ask the user which of the next steps below to do (AskUserQuestion, multiple
choice, recommended option first), then build it with tests.

READ FIRST
- docs/GAME_DESIGN.md: the rules. MTG Comprehensive Rules are the backbone (§1.1): anything not
  covered there works like MTG. The Decision Log at the bottom is the source of truth.
- docs/DEVELOPMENT.md §7: what the engine does today and the "Not yet implemented" list.
- docs/cards/*.md: card lists (v0.1 and the approved v0.2 additions, all ✅). The v0.2 "Engine" column
  says "✓ implemented", "✓" (runnable but not in the prototype pool yet) or names the missing feature.
- docs/cards/tavern_dwellers.md: the 10 Tavern Dwellers (source of truth).
- docs/playtest/PLAYTEST.md ("Findings: Tavern Dwellers and abilities") and RULES_REVIEW.md.

NAMING (decided 2026-10-09)
- "Patron" was renamed **Tavern Dweller** everywhere. Write "Tavern Dweller" in rules text, docs and
  UI, and `TavernDweller` in code (CardType.TavernDweller, PlayerState.TavernDwellerZone,
  PlayerSetup.TavernDwellerId, ActivatedAbility.IsTavernDwellerPower, FormatConfig.TavernDwellersEnabled).
  Never reintroduce "Patron".

RULES DECIDED IN THE LAST SESSION (all in the Decision Log)
- Tavern Dweller Power: once EACH turn (MTG), instant speed, on the Chain, paid mana first then Gold.
- "Pay N Gold" costs are Gold only (like Invest). Generic ability costs (Equip, X, Powers) are mana
  first, then Gold. Permanents are mana only.
- Archon Lumen: one separate 1-damage ping per Equipment, each with its own target.
- Every deck has one Tavern Dweller; every card is from its two factions or Neutral.

WHAT EXISTS (Assets/Rules, assembly RestartedTavern.Rules, noEngineReferences)
- GameEngine: CreateGame / GetLegalActions / Apply / WaitingOn / GetAbilities. CacheLegalActions is an
  opt-in speed-up used by MatchRunner. All work happens in the partial class GameRunner
  (Flow/, Combat/, StateBasedActions/, Core/GameRunner.GameActions.cs).
- Activated abilities: Core/ActivatedAbility.cs + Flow/GameRunner.Abilities.cs (costs, X, Tap and
  summoning sickness, sacrifice, once each turn via GameState.UsesThisTurn, granted abilities).
- Equip and Equipment bonuses: AttachedCreatureModifier (also grants triggers/abilities); layer 6
  before 7c in CharacteristicsCalculator. Cost changes: CostModifierAbility + Core/Payment.cs (Costs).
- Watcher triggers (TriggerEvent.CreatureDies, SpellCast, EquipActivated, EquipmentUnattached) with
  Subject / MinPower / MinCost / MaxPerTurn; optional ("you may") targets; RepeatCount; a
  mid-resolution choice (DecisionKind.TopOrBottom + ActionKind.ChooseOption).
- Cards: Cards/PrototypeCards.cs (+ .Abilities.cs, .TavernDwellers.cs). Six decks, each with its
  Tavern Dweller: Goober Mob (Skabba), Jungle Stampede (Mukk), Zoo Patrol (Keeper Z-00), Vesper's
  Ledger (Madame Vesper), Sparkwrench Scrappers (Sparkwrench), Auditor's Arsenal (Auditor Prime).
- AI/GreedyBot.cs (values abilities, Equip and Powers; saves Gold for the opponent's end step),
  AI/MatchRunner.cs + AI/Experiments.cs (report in docs/playtest/SIMULATION_REPORT.md).
- Client/DebugTable.cs: hot-seat IMGUI table with the Tavern Dweller row.
- 106 EditMode tests, all green.

CANDIDATE NEXT STEPS (offer these; the user picks)
1. Smarter bot attacks (recommended first). The bot only goes all-in when the defender has no
   untapped blockers, so big-creature mirrors stall (seed 12 of the Jungle mirror: both at 4 life
   with full boards from turn 30 until deck-out at turn 86). Add alpha strikes / multi-creature attack
   evaluation. Game-length numbers can't be trusted until this is fixed.
2. v0.2 engine features, in batches: "bank" and spend-Gold triggers (cleanup step, GAME_DESIGN §5.2),
   per-player Gold cap (Offshore Account), "damaged" and remaining-Health target filters, counterspells
   (Hush Money, Counterfeit Coin, Bribe the Referee), extra costs (sacrifice, pay life, X Gold),
   bounce and control change, "can't be healed", damage prevention. Then add the v0.2 cards to decks.
3. The weak Tavern Dwellers: Mukk, Sparkwrench and Auditor Prime barely use their Powers in sims
   (Sparkwrench Scrappers has no Equipment at all). Option A: decks built around them. Option B:
   Power changes — design question for the user, show options.
4. Visual client (DEVELOPMENT §5 roadmap step 4): a real Unity hot-seat table for human playtests.

HOW TO WORK WITH THIS USER
- The user has the vision and wants Claude to propose details. For design questions, use
  AskUserQuestion with multiple-choice options, recommended option first, and show the MTG default
  next to alternatives. Record every decision in the docs and the Decision Log.
- **Don't run the simulation report or bot playtests unless the user asks** (said 2026-10-09). Unit
  tests are fine and expected.
- Commit when a piece of work is done; ask before pushing to GitHub. (gh is logged in as Birtkak;
  the repo has a local git identity.)
- Card text says "an opponent" / "each opponent", never "your opponent".

PRACTICAL NOTES
- Run tests headless (about 40 s):
  "C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -nographics
    -projectPath <repo> -runTests -testPlatform EditMode -testResults <file>.xml -logFile <log>
  Only one Unity instance can open the project at a time. Write results/logs outside the repo.
- Build the debug table: -executeMethod RestartedTavern.Client.Editor.DebugTableBuilder.BuildWindows
  (output Builds/DebugTable/RestartedTavern.exe). Check the UI with
  -bot1 -bot2 -deck1 N -deck2 N -autoplay N -autoshot <png> and look at the screenshot.
- Simulation report (only when asked): -executeMethod RestartedTavern.Client.Editor.SimulationMenu.RunReport
  [-simGames 500] [-simSections "round,cap"]. The full suite (84 matchups) takes about 6 minutes.
- On this Windows box, Bash heredocs containing apostrophes break. Write multi-line edit scripts to the
  scratchpad with the Write tool and run them with python.
- No standalone .NET SDK is installed; Unity compiles everything. New .cs files get .meta files on the
  next Unity run: commit them together.
