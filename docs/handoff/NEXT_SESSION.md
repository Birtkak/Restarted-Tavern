# Next session: handoff prompt

Paste everything below the line into a new session.

---

You're continuing work on Restarted Tavern, a Unity 6 (6000.6.4f1) + C# trading-card game.
Repo: C:\Users\Birre\Desktop\Claude shizzle\Restarted-Tavern (GitHub: Birtkak/Restarted-Tavern, main).
State at the end of the previous session (2026-10-09): every v0.1 and v0.2 card is in the engine, and the
choices the engine used to make automatically are now player choices: which Legendary to keep, how to divide
combat damage among several creatures, and the order of your own simultaneous triggers. Then a housekeeping
pass: PrototypeCards* was renamed to CardPool*, GreedyBot was split into partial files, the docs were refreshed.
199 EditMode tests, all green. Later the same day: GreedyBot got a whole-attack planner
(GreedyBot.Combat.cs: ChooseAttack / ScoreAttack), 203 tests.

GOAL OF THIS SESSION: ask the user which of the next steps below to do (AskUserQuestion, multiple
choice, recommended option first), then build it with tests.

READ FIRST
- docs/GAME_DESIGN.md: the rules. MTG Comprehensive Rules are the backbone (§1.1): anything not
  covered there works like MTG. The Decision Log at the bottom is the source of truth.
- docs/DEVELOPMENT.md §7: what the engine does today and the "Not yet implemented" list.
- docs/cards/*.md: card lists (v0.1 and the approved v0.2 additions, all ✅). Every card is in the engine.
- docs/cards/tavern_dwellers.md: the 10 Tavern Dwellers (source of truth).
- docs/playtest/PLAYTEST.md ("Findings: Tavern Dwellers and abilities") and RULES_REVIEW.md.

NAMING (decided 2026-10-09)
- "Patron" was renamed **Tavern Dweller** everywhere. Write "Tavern Dweller" in rules text, docs and
  UI, and `TavernDweller` in code (CardType.TavernDweller, PlayerState.TavernDwellerZone,
  PlayerSetup.TavernDwellerId, ActivatedAbility.IsTavernDwellerPower, FormatConfig.TavernDwellersEnabled).
  Never reintroduce "Patron".

RULES DECIDED IN THE LAST SESSIONS (all in the Decision Log)
- Tavern Dweller Power: once EACH turn (MTG), instant speed, on the Chain, paid mana first then Gold.
- "Pay N Gold" costs are Gold only (like Invest). Generic ability costs (Equip, X, Powers) are mana
  first, then Gold. Permanents are mana only (exceptions: Retainer Mage, Shady Moneylender).
- "Whenever you spend Gold" triggers once per payment. A lowered Gold cap loses the excess at once.
  "Gold equal to its cost" = printed cost. Bank triggers use cleanup-step priority (MTG 514.3a).
- Gold-Tooth Bruiser / Pickpocket Boss: you gain 1 Gold even if they had none. Dice Game: open
  choices in turn order (MTG 101.4). Retainer Mage: Gold can help pay whenever it's cast.
- Every deck has one Tavern Dweller; every card is from its two factions or Neutral.
- Player choices (MTG defaults): Legendary rule per controller, you pick which to keep; combat damage among
  several creatures is divided freely (§7.2.6, Trample needs lethal on every blocker first), asked only when
  the creature can't kill them all; each player orders their own simultaneous triggers (APNAP between players),
  except triggers of the same ability of the same card.

WHAT EXISTS (Assets/Rules, assembly RestartedTavern.Rules, noEngineReferences)
- GameEngine: CreateGame / GetLegalActions / Apply / WaitingOn / GetAbilities. CacheLegalActions is an
  opt-in speed-up used by MatchRunner. All work happens in the partial class GameRunner
  (Flow/, Combat/, StateBasedActions/, Core/GameRunner.GameActions.cs).
- Activated abilities: Core/ActivatedAbility.cs + Flow/GameRunner.Abilities.cs (costs, X, Tap and
  summoning sickness, sacrifice, once each turn via GameState.UsesThisTurn, granted abilities).
- Equip and Equipment bonuses: AttachedCreatureModifier (also grants triggers/abilities); layer 6
  before 7c in CharacteristicsCalculator. Cost changes: CostModifierAbility + Core/Payment.cs (Costs).
- Watcher triggers (TriggerEvent: CreatureDies, SpellCast, EquipActivated, EquipmentUnattached,
  GoldBanked, GoldSpent, CreatureDealtDamage, CreatureHealed, CreatureEnters, CreatureDealsCombatDamageToPlayer,
  PlayerAttacks, CurseToGraveyard, GoldPaidForCreatureSpell) with Subject / MinPower / MinCost / MinAmount /
  OthersOnly / SubjectSubtype / OnlyAttachedCreature / MaxRemainingHealth / MaxPerTurn. Triggers carry
  EventAmount / EventObject / EventPlayer. Delayed triggers (GameState.DelayedTriggers).
- Mid-resolution choices: DecisionKind TopOrBottom, DiscardCards, PayTax, ChooseFromTop, PayAnyGold, ChooseObject,
  YesNo. Rules choices: KeepLegendary (state-based actions), AssignCombatDamage (start of the combat damage
  step, ActionKind.AssignCombatDamage with PlayerAction.Division), OrderTriggers (PutPendingTriggersOnChain).
- Chain items have object ids (counterspells target them). Control change (permanent and until end of
  turn), bounce, per-turn damage caps, "can't be healed", extra costs on spells (life, sacrifice, X Gold).
- Cards: Cards/CardPool.cs (+ .Abilities.cs, .TavernDwellers.cs, .V01.cs, .V02.cs). Six decks, each with its
  Tavern Dweller: Goober Mob (Skabba), Jungle Stampede (Mukk), Zoo Patrol (Keeper Z-00), Vesper's
  Ledger (Madame Vesper), Sparkwrench Scrappers (Sparkwrench), Auditor's Arsenal (Auditor Prime).
- AI/GreedyBot*.cs (partial class: decisions, .Abilities, .Effects, .Combat; values abilities, Equip and
  Powers; saves Gold for the opponent's end step; answers the new choices),
  AI/MatchRunner.cs + AI/Experiments.cs (report in docs/playtest/SIMULATION_REPORT.md).
- Client/DebugTable.cs: hot-seat IMGUI table with the Tavern Dweller row.
- 199 EditMode tests, all green. Every card in docs/cards is implemented (CardPool*.cs).

CANDIDATE NEXT STEPS (offer these; the user picks)
1. Going first (RULES_REVIEW #1), now the strongest open rules question: with the attack planner the
   first player wins 83% of Jungle mirrors and 77% of Goober mirrors (PLAYTEST.md, latest findings).
   Design question for the user: show the MTG default next to the measured alternatives.
2. Payment order warning (RULES_REVIEW #6): mana is spent first, so casting a Sorcery before a creature can
   strand the creature. The UI should warn or order plays.
3. The weak Tavern Dwellers: Mukk, Sparkwrench and Auditor Prime barely used their Powers in sims.
   Sparkwrench Scrappers now has some Equipment (deck pass), but nothing is measured yet. Option A:
   decks built around them. Option B: Power changes — design question for the user, show options.
4. Visual client (DEVELOPMENT §5 roadmap step 4, recommended): a real Unity hot-seat table for human playtests.
5. Engine gaps (DEVELOPMENT §7 "Not yet implemented"): Snik copying by target with last known information,
   replacement effects, loading card data from JSON instead of C#.
(Done 2026-10-09: every v0.1 and v0.2 card is in the engine; player choices for the Legendary rule, combat
damage division and trigger order; GreedyBot plans whole attacks (alpha strikes, crack-back); 203 tests.)

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
- Simulation report (only when asked): build Tools/SimRunner with Unity's bundled SDK
  ("C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Data/DotNetSdk/dotnet.exe" build Tools/SimRunner -c Release),
  then run Tools/SimRunner/bin/Release/net8.0/SimRunner.exe [-simGames 500] [-simSections "round,cap"].
  The full suite takes ~15 s (inside Unity ~9 min: Mono barely scales across cores). Same results.
  The same SDK could also run the unit tests outside Unity later (not set up).
- Multi-line edits: a quoted Bash heredoc (python - <<'EOF') works, apostrophes included; or write the script
  to the scratchpad and run it with python. Open files with newline='' when writing: otherwise Windows writes
  CRLF, and the repo's .gitattributes keeps .cs/.md files as LF.
- No standalone .NET SDK is installed; Unity compiles everything. New .cs files get .meta files on the
  next Unity run: commit them together.
