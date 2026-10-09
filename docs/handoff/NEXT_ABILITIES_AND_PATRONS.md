# Handoff: Activated Abilities and Patron Powers

A ready-to-paste prompt for the next Claude Code session. It's written to be self-contained.

---

```
You're continuing work on Restarted Tavern, a Unity 6 (6000.6.4f1) + C# trading-card game.
Repo: C:\Users\Birre\Desktop\Claude shizzle\Restarted-Tavern (GitHub: Birtkak/Restarted-Tavern, main).

GOAL OF THIS SESSION: implement activated abilities, Equip, and Patrons (Patron zone, Patron Powers,
Patron passives) in the rules engine, then make them usable by the bot, the debug table and the
simulations.

READ FIRST
- docs/GAME_DESIGN.md: the rules. MTG Comprehensive Rules are the backbone (§1.1): anything not
  covered there works like MTG. Key sections: §5.2 Gold, §7.4 summoning sickness for Tap abilities,
  §8 the Chain, §9 Patrons, §10 card types (Equipment), and the Decision Log at the bottom.
- docs/DEVELOPMENT.md §7: what the engine does today, plus the "Not yet implemented" list.
- docs/cards/patrons.md: the 10 approved Patrons. It is the source of truth; the table copied into
  GAME_DESIGN §9.3 is older.
- docs/cards/*.md: card lists. "Set v0.2 additions" sections are 🟡 DRAFTS awaiting the user's
  review. Don't implement draft cards until the user approves them, but use their "Engine" column
  to see which features they need.
- docs/playtest/PLAYTEST.md: simulation findings so far.

RULES THAT MATTER FOR THIS WORK (all locked unless marked)
- Payment (GAME_DESIGN §5.2): everything except creatures can be paid with ANY MIX of mana and
  Gold, chosen by the player. That includes activated abilities, Equip and Patron Powers. Creatures
  are mana only. Overcharge is Gold only. On other players' turns you have no mana, only Gold.
- Activated abilities use the Chain (MTG 602). Abilities whose cost includes Tap can't be used the
  turn the creature arrives unless it has Haste (§7.4). Respect "Activate only once per turn" and
  "only as a sorcery".
- Equip: MTG rules. Sorcery speed only, targets a creature you control. When the creature leaves,
  the Equipment stays on the battlefield unattached (the state-based action already exists).
  Equipment grants its bonuses through continuous effects (CharacteristicsCalculator).
- Patrons (§9.1): the Patron is the player's face, sits in the public Patron zone, never attacks or
  blocks, and can't be removed in v0.1. Each Patron has a passive and a Power. The Power can be used
  once per turn at instant speed (so also on opponents' turns), goes on the Chain, and is paid with
  mana and/or Gold. A deck's Patron sets its two factions (deck validation should check that every
  card is from those factions or Neutral).
- Card text says "an opponent" / "each opponent", never "your opponent" (multiplayer-ready).

WHAT EXISTS (Assets/Rules, assembly RestartedTavern.Rules with noEngineReferences)
- GameEngine: CreateGame / GetLegalActions / Apply (mutates the state; Clone() first if needed) /
  WaitingOn. All work happens in the partial class GameRunner: Flow/ (turns, priority, the Chain,
  legal actions), Combat/, StateBasedActions/, Core/GameRunner.GameActions.cs (draw, damage, heal,
  zone moves, fight).
- Cards are CardDefinition objects built from Effect building blocks (Effects/BasicEffects.cs),
  TriggeredAbility and StaticAbility (Core/Characteristics.cs). There are multi-target slots
  (TargetSlot), and the 37 prototype cards plus 3 decks are in Cards/PrototypeCards.cs.
- PlayerAction is plain data. Legal actions are fully enumerated, including every Gold/mana split
  and every target combination, and Apply only accepts listed actions. Add new ActionKinds
  (e.g. ActivateAbility, PatronPower) the same way.
- AI/GreedyBot.cs (rule-based bot), AI/MatchRunner.cs + AI/Experiments.cs (bot-vs-bot experiments,
  report in docs/playtest/SIMULATION_REPORT.md).
- Client/DebugTable.cs: hot-seat IMGUI debug table (scene Assets/Scenes/DebugTable.unity).

SUGGESTED PLAN
1. Activated abilities: an ActivatedAbility definition (cost: mana/Gold amount, X, Tap, sacrifice,
   pay life; timing flags; targets; effects), ChainItemKind.ActivatedAbility, legal-action
   enumeration with payment splits, summoning-sickness check for Tap costs, and once-per-turn
   tracking (reset each turn). Tests for each.
2. Equip as an activated ability on Equipment, plus Equipment static bonuses ("equipped creature
   gets +X/+Y and has ...", including granted triggered abilities). Tests: equip, re-equip, the
   creature dies and the Equipment stays.
3. Patrons: PatronDefinition (factions, passive, Power), PlayerState.PatronId is already there.
   Add a Patron zone/view, Patron Power activation (once per turn, instant speed, mana+Gold) and
   passives (static, triggered and cost-reduction kinds: see patrons.md). Implement all 10 Patrons
   and validate decks against the Patron's factions.
4. Implement the approved v0.1 cards that were blocked on these features: Snik, Grove Elder,
   Mercenary Contract, Back-Street Mechanic, and the Glitterworld Equipment (Neon Shiv, Pulse Blade,
   Overclock Rig, Rail Cannon, Megacorp Exosuit) and Equipment-related creatures, as far as their
   other needs allow. Give the prototype decks Patrons (e.g. Zoo Patrol -> Keeper Z-00).
5. GreedyBot: value abilities and Patron Powers (spend Gold when it's useful; keep Gold for
   instant-speed Powers on the opponent's turn). Debug table: show the Patron, list abilities and
   Powers as actions.
6. Rerun the simulations (Restarted Tavern > Run Simulation Report). The Gold-cap experiment
   (GAME_DESIGN §5.2, cap 5) is the one to watch: until now the cap changed nothing because Gold
   had almost no sinks. Update docs/playtest/PLAYTEST.md with the findings.
7. Update DEVELOPMENT §7 and GAME_DESIGN (Decision Log for anything decided). Commit.

HOW TO WORK WITH THIS USER
- The user has the vision and wants Claude to propose details. For design questions, use
  AskUserQuestion with multiple-choice options, recommended option first, and always show the MTG
  default next to alternatives. Record every decision in the docs and the Decision Log.
- Ask before pushing to GitHub. (gh is logged in as Birtkak; the repo has a local git identity.)
- Open question to raise early: should Overcharge stay Gold-only now that Gold pays for everything
  except creatures? (It's locked as Gold-only. The user's rule "everything can use gold and mana
  except creatures" could be read to include it.)

PRACTICAL NOTES
- Run tests headless (about 40 s; results XML gives the totals):
  "C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -nographics
    -projectPath <repo> -runTests -testPlatform EditMode -testResults <file>.xml -logFile <log>
  64 EditMode tests pass at the start of this session. Keep them green; add tests for every rule.
- Build the debug table: -executeMethod RestartedTavern.Client.Editor.DebugTableBuilder.BuildWindows
  (output Builds/DebugTable/RestartedTavern.exe). To check the UI, run it with
  -bot1 -bot2 -autoplay N -autoshot <png> and look at the screenshot.
- Simulation report: -executeMethod RestartedTavern.Client.Editor.SimulationMenu.RunReport -simGames 1000
- On this Windows box, Bash heredocs containing apostrophes (e.g. "don't") break. Write multi-line
  edit scripts to the scratchpad with the Write tool instead.
- No standalone .NET SDK is installed; Unity compiles everything.
```
