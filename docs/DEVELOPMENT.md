# Restarted Tavern — Development Guide

How the game will be built. Rules live in [GAME_DESIGN.md](GAME_DESIGN.md); this document covers architecture and how we work.

**Status:** engine prototype. Sets v0.1 and v0.2 are designed and every card runs in the rules engine (254 passing EditMode tests). A hot-seat **debug table** in Unity can play it, against itself or the GreedyBot (§7).

---

## 0. Tech Stack 🔒

| Item | Choice |
|---|---|
| Engine | **Unity 6000.6.4f1** (Unity 6), already installed. Pin this version in `ProjectSettings/ProjectVersion.txt` |
| Language | **C#** |
| First platform | **PC (Windows)**. Android, Mac and WebGL stay possible later because the rules engine has no Unity code |
| Online | **Local first** (hot-seat and vs. AI). The engine is built online-ready (deterministic, per-player hidden-information views). An authoritative server comes later, reusing the same rules library |
| Tests | NUnit through the **Unity Test Framework** (EditMode tests). They can run headless from the command line with `-runTests` |

### 0.1 Project layout 🟡
```
Restarted-Tavern/
├─ docs/                          design + development docs, card lists
├─ Assets/
│  ├─ Rules/                      RestartedTavern.Rules.asmdef  (noEngineReferences: true)
│  │   ├─ Core/                   GameState, PlayerState, CardInstance, ids, RNG
│  │   ├─ Flow/                   rounds and actions, steps, priority, the Chain
│  │   ├─ Combat/                 attackers, blockers, damage, Trample
│  │   ├─ Effects/                effect building blocks (damage, heal, draw, create token...)
│  │   ├─ StateBasedActions/      deaths, life loss, Legendary rule, detached Curses
│  │   ├─ Core/Data/              Json (reader/writer) and CardJson (card files <-> CardDefinition)
│  │   ├─ Cards/                  CardPool.cs: loads the card and deck files
│  │   └─ AI/                     GreedyBot (rule-based player), MatchRunner + Experiments (bot-vs-bot simulations, the simulation report)
│  ├─ Rules.Tests/                RestartedTavern.Rules.Tests.asmdef (EditMode, NUnit)
│  ├─ StreamingAssets/Cards/      card data: one JSON file per faction + tavern_dwellers.json (§3)
│  ├─ StreamingAssets/Decks/      prototype_decks.json
│  ├─ Client/                     RestartedTavern.Client.asmdef: DebugTable.cs (IMGUI debug table); Editor/ builds the scene and the exe
│  │   ├─ Logic/                  RestartedTavern.Client.Logic.asmdef (noEngineReferences): session, snapshot, picker, combat stage (CLIENT_DESIGN §2)
│  │   └─ Logic.Tests/            its EditMode tests
│  └─ Scenes/                     DebugTable.unity
└─ Server/  (later)               a .NET host that compiles the same Assets/Rules source files
```
- `noEngineReferences: true` on the Rules assembly **enforces** at compile time that rules code can't touch `UnityEngine`. This keeps it portable to a server and fast to test.
- The Rules assembly targets the C# subset that Unity 6 supports (C# 9), and avoids outside libraries, so it also compiles as a plain .NET library (Tools/SimRunner does).
- 🔒 Card data is stored as **JSON** (§3), read by the Rules assembly's own small JSON reader. `Assets/link.xml` keeps the building-block classes from being stripped in builds, since they're created by reflection.


---

## 1. Core Architectural Principles 🟡

1. **The rules engine is separate from the presentation.** The game logic is a pure, UI-free library. The client (whatever engine we choose) only renders the state and sends player *actions*. This lets us run the same rules for AI, tests, servers and replays.
2. **Deterministic.** Given the same starting state, seed and action list, the result is always the same. All randomness goes through one seeded RNG. This makes replays, bug reports and networking possible.
3. **State in, actions in, state + events out.**
   ```
   apply(GameState, PlayerAction) -> (GameState, [GameEvent])
   legalActions(GameState, PlayerId) -> [PlayerAction]
   ```
   The UI animates from `GameEvent`s. It never guesses from differences between states.
4. **N players from day one.** Players live in an ordered list, and the action order and response order run around that list. Nothing in the engine assumes exactly 2 players, even though only 2-player Standard ships first.
5. **Formats are data.** Deck size, copy limit, starting life, hand size, mana cap and Gold cap all live in a `FormatConfig`. They are not hard-coded.
6. **Cards are data plus a small number of scripted effects.** Most cards are built from a fixed library of effect building blocks (deal damage, heal, draw, summon, …). Only rare cards need custom code.
7. **Hidden information is enforced by the engine.** Each player gets a *view* of the state with hidden zones removed. This is required for online play and fair AI.

---

8. **Structure the engine like the MTG Comprehensive Rules.** MTG is the rules foundation (GAME_DESIGN §1.1), so the engine should mirror its architecture: a priority/stack loop (the Chain), **state-based actions** checked whenever a player would receive priority (a creature at 0 Health dies, a player at 0 life loses, the Legendary rule, unattached Curses go to the graveyard), a **layer system** for continuous effects, and replacement effects. Rule code should cite the matching GAME_DESIGN section, or the MTG CR rule number when it implements default MTG behavior.

## 2. Core Model (draft)

```
FormatConfig   { deckSize, copyLimit, minPlayers, maxPlayers, startingLife,
                 startingHand, maxHandSize?, manaCap, goldCap, ... }

GameState      { players[], activePlayer, priorityPlayer, passesInRow,
                 turnNumber, phase, chain[], rngState, nextObjectId }

PlayerState    { id, teamId, seat, eliminated, tavernDwellerId, life, maxMana, mana, gold, attachedCurses[],
                 zones: { deck, hand, battlefield, graveyard, exile } }

CardDefinition { id, name, type, cost, power?, health?, keywords[],
                 abilities[], invest?, equipCost?, faction | "neutral",
                 rarity, text }

CardDefinition (type TavernDweller) { id, name, tavernDwellerFactions[2], triggers/statics (passive), abilities[0] (Power) }

CardInstance   { objectId, definitionId, owner, controller,
                 currentHealth, tapped, counters{}, attachments[] }
```

- 🟡 Implemented as `CardInstance.Damage`: damage that **never wears off** (no "damage marked this turn"). Remaining Health is computed as max Health − Damage. This matches MTG's damage counters except for the cleanup reset. 🔒 Exception (GAME_DESIGN §7.3): when a Health buff ends, damage is capped at max Health − 1, so losing a buff can't kill.
- `chain` is a LIFO list of pending spells/abilities. When every player has passed in a row (`passesInRow == players.length`), the top item resolves. The engine auto-passes for players who have no legal response.
- Curses can attach to a creature *or* a player, so attachments target `ObjectId | PlayerId`.
- `objectId` is new every time a card changes zone (as in MTG), so "that creature" effects stop applying once it leaves.

---

## 3. Card Data Format 🔒 (JSON, since 2026-10-10)

Every card lives in a data file, not in code: `Assets/StreamingAssets/Cards/<faction>.json` (one per faction, tokens included)
and `tavern_dwellers.json`. The prototype decks are in `Assets/StreamingAssets/Decks/prototype_decks.json`
(`{"card_id": copies}`). StreamingAssets ships with builds, so a built debug table reads the same files.

A card is a JSON object whose fields are the `CardDefinition` properties in camelCase. Abilities are made of the
engine's **building blocks**: effects (`Assets/Rules/Effects`, e.g. `DealDamageEffect`), statics (`AnthemAbility`,
`CostModifierAbility`, `ReplacementAbility`...), triggers (`TriggeredAbility`) and activated abilities
(`ActivatedAbility`). A building block names its class in `"$type"`:

```json
{
  "id": "barrel_bomber",
  "name": "Barrel Bomber",
  "type": "Creature",
  "cost": 4,
  "power": 3,
  "health": 3,
  "subtypes": ["Goober"],
  "rarity": "Common",
  "faction": "goobers",
  "text": "Arrival: Deal 2 damage to any target.",
  "triggers": [
    {
      "when": "Arrival",
      "target": "AnyTarget",
      "effects": [
        {"$type": "DealDamageEffect", "amount": 2}
      ]
    }
  ]
}
```

- **Loading**: `CardPool.All()` / `CreateDatabase()` read the files once (`CardPool.DataRoot` finds `Assets/StreamingAssets`
  from the working directory; the debug table sets it to `Application.streamingAssetsPath`). `Data/CardJson.cs` maps JSON to
  the classes by reflection; `Data/Json.cs` is a small JSON reader/writer (the rules assembly can't use `UnityEngine`).
- **Fails loudly**: an unknown field, building block or enum name stops loading with the file, card id and field.
- **Canonical form**: values equal to the class default are left out (except the fields every card shows: type, cost,
  rarity, faction, text, and Power/Health for creatures). `CardDataTests` checks every file reads and writes back
  byte-for-byte, so hand edits must keep the format (2-space indent, field order as in the classes).
- **Changing a card** is a data edit: change the numbers or swap building blocks, then run the tests. A card that needs
  something new still needs a new building block in C# first (a new effect class or a new field).
- Conditions and counts are data too: `TriggerCondition` (`SourceHasNoDamage`, `YouHaveGoldAtLeast`,
  `EachOpponentHasLifeAtMost`) and `DynamicCount` (`EquipmentYouControl`, `CreatureCardsInYourGraveyard`).
- 🟡 The rules `text` is still written by hand. Generating it from the abilities would keep text and behavior from disagreeing.

---

## 4. Testing Strategy 🟡
- **Rules unit tests** for every rule in GAME_DESIGN.md. When a rule changes, its test changes in the same commit.
- **Card tests**: each card with a scripted effect gets at least one scenario test.
- **Random-play soak tests**: run thousands of games between random-action bots and check invariants (no negative mana, the total number of cards is conserved, the game always ends).
- **Replay tests**: replay saved action logs and compare the final state.

---

## 5. Roadmap (draft)
1. ✅ **Ruleset v0.1 and first set**: 5 factions × 20 cards, 10 Neutral cards, 10 Tavern Dwellers.
2. ✅ **Rules engine prototype**: the Rules assembly with EditMode tests, playable through a minimal debug UI in Unity, with about 20 test cards (§7).
3. 🚧 **Playtest** (paper or the debug UI): tune the Gold cap, the curve and the impact of permanent damage. *Bot simulations and the first findings are in [playtest/PLAYTEST.md](playtest/PLAYTEST.md); human playtests are next.*
4. 🚧 **Visual client** in Unity (Windows build): hot-seat 1v1 and vs. the bot. Direction and client logic: [CLIENT_DESIGN.md](CLIENT_DESIGN.md) (logic layer in `Assets/Client/Logic`, 2026-10-10); the table scene is next.
5. ✅ Implement the full first set (120 cards) and a basic AI. *All v0.1 and v0.2 cards run in the engine (2026-10-09); GreedyBot plays them.*
6. Later: multiplayer (3–4 players), singleton format, online play.

---

## 6. Open Technical Questions ❓
- Card art pipeline and card frame rendering.
- AI approach for vs.-AI play (rule-based first? Monte Carlo search, which works because the engine is deterministic?).
- CI: GitHub Actions runs the rules tests without Unity (`Tools/RulesTests`, .NET 8 + NUnit, `.github/workflows/dotnet.yml`) and builds SimRunner on Windows with a short simulation smoke test, uploading it as an artifact (`dotnet-desktop.yml`). Running the Unity tests themselves (client logic, the scene) would need a Unity license setup (GameCI).
- Online (later): hosting, and how matchmaking works.

---

## 7. Engine Prototype Status

**API** (`GameEngine`): `CreateGame(format, players, seed)`, `GetLegalActions(state, player)`, `Apply(state, action) → events`, `WaitingOn(state)`. `Apply` **changes the state in place** and only accepts actions from the legal list. Call `GameState.Clone()` first to keep the old state (for AI search or undo). `GameState.CreateViewFor(player)` hides the other players' hands, all decks and the RNG.

**Implemented**
- Setup: a random first player, 7-card hands, the **London mulligan**, everyone draws in round 1 (§3). `FormatConfig.MultiplayerStandard()` uses 40 life and the same rounds.
- Rules (§5–6): there is one rule set, the Standard rules. `FormatConfig` only holds the numbers (life, hand, deck size, mana and Gold caps).
- Round structure (§6): **Legends of Runeterra rounds**. A round (`GameState.RoundNumber`, `RoundStartedEvent`) runs: Start (everyone refills, untaps; start-of-turn triggers for everyone), Draw (everyone), then the action phase in Main 1: `GameState.ActivePlayer` is whoever has the action, an action starts when they put something on an empty Chain (`ActionInProgress`) and when the Chain is empty again `GivePriority` hands the action on; a pass with an empty Chain hands it on too, and all players passing in a row ends the phase. Attacking is an action (`ActionKind.GoToCombat`, round leader with the attack token, once per round, `AttackedThisRound`); after combat the next player has the action. End and Cleanup run for everyone (everyone discards to 7, mana banked, "until end of turn" ends), then the next seat leads the next round. Tests drive games through `TestGame`: its `Active` is the round leader ("me"), and when my action has resolved the other player passes the action back as soon as the test acts as me again; `RoundsTests` checks the action order itself.
- Mana and Gold (§5): every player refills when a round starts and the mana lasts the round. Permanents are paid with mana only; Instants, Sorceries and abilities use **Gold first**, then mana, automatically. Invest and "Pay N Gold" are paid with Gold only.
- **The Chain** (§8): LIFO; the caster keeps priority; it resolves when every living player passes in a row; spells fizzle when their target is illegal; the fixed priority windows; auto-pass for players who have no other option (`GameState.AutoPass`).
- **Multiple targets** (MTG 115, 608.2b): a spell has a list of target slots (optional slots for "up to N"). Targets are distinct, and illegal targets are skipped at resolution; the spell only fizzles when every target is gone. **Fight** (§11.1).
- Triggers: Arrival, Last Breath, Attacks, Start/End of your turn (every round, for every player). They use APNAP order, and each player **orders their own** simultaneous triggers (`DecisionKind.OrderTriggers`, MTG 603.3b; the same ability of the same card isn't asked about). Targets are chosen as each one goes on the Chain. A trigger with no legal target is removed.
- Combat (§7): attackers and blockers are declared one creature at a time, and every attacker picks which opponent it attacks. No summoning sickness and no Haste (§7.4). Also implemented: Flying/Reach, Can't block, Trample, Lifelink, and multiple blockers. A creature that deals damage to several creatures has its controller **divide the damage** (`DecisionKind.AssignCombatDamage`, `ActionKind.AssignCombatDamage`, §7.2.6), attacking player first; the game only asks when it can't kill them all.
- **Permanent damage**, with Heal capped at max Health or starting life. Losing a buff can't kill (§7.3).
- State-based actions (MTG 704): 0 life, drawing from an empty deck, lethal damage, the Legendary rule (the controller picks which to keep: `DecisionKind.KeepLegendary`), illegal Curses, unattaching Equipment, and the game ending when one team is left.
- Continuous effects: static anthems/lords, +1/+1 counters and until-end-of-turn modifiers, applied in MTG layer order.
- New object ids on every zone change. Tokens stop existing when they leave the battlefield.
- **Activated abilities** (MTG 602, `Core/ActivatedAbility.cs`, `Flow/GameRunner.Abilities.cs`): generic costs (Gold first, then mana, §5.2), X costs, "Pay N Gold" (Gold only), Tap (usable the round it arrives, §7.4), sacrifice and life costs; "only as a sorcery" and "once each turn" (once each round; tracked in `GameState.UsesThisTurn`, reset every round). They go on the Chain as `ChainItemKind.ActivatedAbility` and resolve even if the source left. Legal actions list every target combination and sacrifice choice (`ActionKind.ActivateAbility`, `PlayerAction.AbilityIndex/X/Sacrifice`).
- **Equip** (§10, MTG 701.3): a sorcery-speed ability with Equip cost modifiers; Equipment bonuses are continuous effects (`AttachedCreatureModifier`), including **granted triggered and activated abilities** (Pulse Blade, Overclock Rig). Moving Equipment ends the old bonus without killing (§7.3); when the creature leaves, the Equipment stays unattached.
- **Layers**: `CharacteristicsCalculator` applies layer 6 (keywords) before layer 7c (Power/Health), so "creatures with Trample get +1/+0" sees granted Trample.
- **Tavern Dwellers** (§9): a `CardType.TavernDweller` card in the public Tavern Dweller zone (`PlayerState.TavernDwellerZone`). Passives are triggered abilities (new watcher triggers: a creature dies, a spell is cast, Equip is paid, Equipment becomes unattached; with "you / opponents", min Power/cost and "at most N times each turn"), statics (anthems that work from the Tavern Dweller zone), cost modifiers (`CostModifierAbility`: spells, Invest, Equip) and "enters with a counter". The Power is an activated ability, once each round, at instant speed. Deck validation checks that every card is from the Tavern Dweller's factions or Neutral.
- Other new mechanics: optional ("you may") trigger targets, triggers that fire once per Equipment (Archon Lumen), combat-damage-to-a-player triggers, token copies, graveyard targets, and a mid-resolution choice (`DecisionKind.TopOrBottom`, `ActionKind.ChooseOption`).
- **Cards** (`Assets/StreamingAssets/Cards/*.json`, loaded by `CardPool`, §3): every card in docs/cards (sets v0.1, v0.2 and v0.3), all 10 Tavern Dwellers and the tokens. Six legal decks, each with its Tavern Dweller: Goober Mob (Skabba), Jungle Stampede (Mukk), Zoo Patrol (Keeper Z-00), Vesper's Ledger (Madame Vesper), Sparkwrench Scrappers (Sparkwrench) and Auditor's Arsenal (Auditor Prime, the Equipment deck). On 2026-10-09 each deck swapped 4 cards for v0.2 cards (user-approved).
- **Gold economy (set v0.2, batch A)**: the **per-player Gold cap** (`GoldCapAbility`, `GoldRules.Cap`; Gold above a lowered cap is lost as a state-based action); **bank triggers** (`TriggerEvent.GoldBanked`, with `MinAmount` for "2 or more"): they go on the Chain in the cleanup step, players get priority, and the cleanup step repeats (MTG 514.3a); **spend-Gold triggers** (`TriggerEvent.GoldSpent`, once per payment of a spell, ability, Invest or X); trigger amounts reach effects as `EffectContext.EventAmount` ("heal that much"); **"pay any amount of Gold (X)"** on spells (`CardDefinition.XGoldExtraCost`, `PlayerAction.X`); a mid-resolution discard (`DecisionKind.DiscardCards`); Tap effects.
- **Damage and healing (set v0.2, batch B)**: target filters on `TargetSlot` (**damaged**, **Health remaining**, attacking or blocking), also for triggers (`TriggeredAbility.TargetDamaged`); **can't be healed** (`CantBeHealedAbility`, on an enchanted creature or every creature of an enchanted player); **damage prevention** (`AttachedCreatureModifier.MaxDamageEachTurn`; damage per creature per turn is tracked in `UsesThisTurn`); damage to each creature (`DealDamageToEachCreatureEffect`); damage-based and scaling stats (`PowerPerDamageAbility`, `AttachedScalingModifier`); new watcher triggers `CreatureDealtDamage` (with "only the attached creature" and an intervening "if N or less Health remaining"), `CreatureHealed` (who healed), `CreatureEnters` ("another", min Power); `DestroysCreatureInCombat`; Curse triggers at the start of the enchanted player's turn. Triggers carry the event's object and player (`EffectContext.EventObject/EventPlayer`). **Extra costs on spells**: pay life (`ExtraLifeCost`) and sacrifice a creature (`SacrificeCreatureCost`, `PlayerAction.Sacrifice`, last known Power in `EffectContext.SacrificedPower`).
- **The Chain and control (set v0.2, batch C)**: everything on the Chain has an object id (`ChainItem.ObjectId`), so spells and abilities can be targeted (`TargetSpec.SpellOnChain`, `SpellOrAbilityOnChain`, `TargetSlot.MaxCost`); **counterspells** (`GameRunner.Counter`, `CounterTargetEffect`) and **taxes** (`DecisionKind.PayTax`, Gold first, then mana); **bounce** (`ReturnToHandEffect`, `ReturnAllCreaturesEffect`); **control change** (`GainControlEffect`, permanent or until end of turn via `GameState.ControlUntilEndOfTurn`; leaves combat; control returns when a player leaves the game); destroy all creatures; creature cards in any graveyard as targets.
- **The last v0.2 mechanics (batch D)**: payment rules (`CardDefinition.Flash` and `GoldMayPay`; `PaymentRuleAbility` for "Invest with mana" and "Gold for creature spells"; `Payment.InvestSplit`); watcher triggers `CreatureDealsCombatDamageToPlayer` (with `SubjectSubtype`), `PlayerAttacks` (number of attackers), `CurseToGraveyard`, `GoldPaidForCreatureSpell`; Gold theft (`DrainGoldEffect`, `StealAllGoldEffect` with `EffectContext.Remembered` for "for each Gold gained this way"); **delayed triggers** (`GameState.DelayedTriggers`, MTG 603.7: "at the end of your turn, exile it"); mid-resolution choices `DecisionKind.ChooseFromTop` (Grave Gossip) and `PayAnyGold` (Dice Game: open choices in turn order).
- **Rest of set v0.1 (batch E)**: intervening "if" on triggers (`TriggeredAbility.Condition`, checked on trigger and on resolution, MTG 603.4); death watchers with "another", a subtype, or "a creature that player controls" (Curses on players); "your second spell each turn" (spell count in `UsesThisTurn`); "whenever this is dealt damage and survives"; triggers granted to your other creatures (`GrantTriggerToYourCreaturesAbility`); Curse triggers at the start of the enchanted creature's controller's turn; "can block an additional creature" (`CardDefinition.ExtraBlocks`, the blocker splits its damage); tokens that enter tapped and attacking; graveyard targets with a cost limit; reveal-until; look at the top N with the rest on the bottom.
- **Choices during resolution (batch F)**: `DecisionKind.ChooseObject` ("choose a creature / card", optional or not) and `YesNo` ("you may", "may give you 2 Gold"), each with follow-up effects (`PendingDecision.Then/Else`; EventObject = the choice, EventPlayer = the chooser). Several choices from one effect wait in `GameState.ChoiceQueue` and are asked one after another. **Divided damage** (`CardDefinition.DividedDamage`, `PlayerAction.Division`, chosen on casting, MTG 601.2d). **Every card in docs/cards (set v0.1 and v0.2) and all 10 Tavern Dwellers are now in the engine.**
- **Player choices instead of automatic ones** (2026-10-09): the Legendary rule, dividing combat damage, ordering your own triggers (see above). GreedyBot answers all three.
- **Set v0.3** (mana scarcity, 36 cards) and new Powers for Mukk, Sparkwrench and Auditor Prime (2026-10-10: `TargetSlot.Keyword`, `AttachEquipmentOrPumpEffect`, `ActivatedAbility.ActivateOnlyWithGold`).
- **"Choose up to N" on resolution** (`DecisionKind.ChooseUpTo`, MTG 608.2d): Snik pays X on activation and picks its Goobers when the ability resolves, one at a time.
- **Replacement effects** (MTG 614–616, `Core/Replacement.cs`, `Flow/GameRunner.Replacements.cs`): `ReplacementAbility` (a static, all data: event, filters, outcome) for dying, damage, entering, drawing, gaining life and gaining Gold; temporary ones from `AddReplacementEffect` live in `GameState.Replacements` ("until end of turn", "the next time"). Self-replacement first, then oldest first; each applies once per event and the rest are re-checked. The affected player doesn't choose the order yet (game actions can't pause). Keeper Z-00 uses it.
- `GameEngine.CacheLegalActions` (opt-in, used by `MatchRunner`): the bot's legal-action list is reused by `Apply`'s validation, so it isn't enumerated twice.

**Tests** (`Assets/Rules.Tests` and `Assets/Client/Logic.Tests`, 256 tests): rules unit tests per area, card scenario tests, a **random-play soak test** (100 full games between random bots with invariant checks after every action) and **determinism** tests (same seed and actions give the same game). Run them headless:
```
"C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml
```

**Debug table** (`Assets/Client/DebugTable.cs`, scene `Assets/Scenes/DebugTable.unity`). This is a hot-seat IMGUI table for 1v1 with the six prototype decks (`CardPool`); the P1/P2 deck buttons choose them for the next game.
- Waiting on someone: the top bar names them and their header turns green.
- Your options: their legal actions are listed as buttons. Cards they can act with are tinted green, and clicking a card filters the list to the actions that involve it.
- What's on the table: each player's Tavern Dweller (text, factions, "Power used this turn"), the board with Power/Health, damage, keywords, tapped/sick/attacking/blocking/equipped states and what each Equipment is attached to, plus the Chain (top first, with ability texts and Tavern Dweller Powers marked) and the current combat. Abilities and Powers appear in the action list like any other action; click the Tavern Dweller or a permanent to see only its actions.
- Hidden information: a hand is shown only while its owner is the one to act (or with *Show all hands*). In hot-seat the log doesn't name drawn cards.
- Controls: *Undo* (last 200 states), *New game* (with a seed), *Auto-pass*, and either player can be handed to the GreedyBot.
- Build: menu **Restarted Tavern → Build Windows Debug Table**, or headless:
  ```
  Unity.exe -batchmode -quit -projectPath . -executeMethod RestartedTavern.Client.Editor.DebugTableBuilder.BuildWindows
  ```
  This writes `Builds/DebugTable/RestartedTavern.exe` (git-ignored). Command-line flags: `-seed N`, `-bot1`, `-bot2`, `-deck1 N`, `-deck2 N`, `-autoplay N` (the bots play N actions at startup), and `-autoshot file.png` (take a screenshot, then quit), for automated checks.
- The P1/P2 bot toggles use `GreedyBot` (`Assets/Rules/AI`), a deterministic rule-based player. It plans each attack as a whole: candidate attacks are scored against the defender's likely blocks and the crack-back next turn, so it alpha-strikes through blockers and holds back when the swing back would kill. `MatchRunner` plays bot-vs-bot games and `Experiments` builds the simulation report (see [playtest/PLAYTEST.md](playtest/PLAYTEST.md)).
- **Bug hunt** (`Tools/BugHunt`, same SDK): run it before a push.
  - It plays three sets of games:
    - every prototype deck × Tavern Dweller pairing with the greedy bot
    - random play with the prototype decks
    - random play with random 60-card decks from the whole pool
  - After every action it checks:
    - the engine invariants (mana, Gold, card conservation, dead creatures, Equipment)
    - the client logic: the context button's action is legal and labelled, choice buttons are legal, every legal target is on the table (Chain bubbles too), and the picker only builds legal actions
  - At the end of each game it checks that the history replays to the same game.
  - A watchdog prints any game running over 15 s (hangs, choices that explode).
  - Default run: about 6,000 games in about 15 s. Exit code 1 on any failure.
  - Options: `-seeds N`, `-random N`, `-randomDecks N`, `-seed S` (one game, with a line per combat damage choice), `-data path`.
  - Command: `Editor/Data/DotNetSdk/dotnet.exe run -c Release --project Tools/BugHunt`.
- **Simulations outside Unity**: `Tools/SimRunner` (a .NET 8 console app built with the SDK in Unity's `Editor/Data/DotNetSdk`) compiles the `Assets/Rules` sources and runs the simulation report in ~35 s instead of several minutes in Unity (Mono's GC keeps the parallel games from scaling). See [playtest/PLAYTEST.md](playtest/PLAYTEST.md) "How to run". `-trace` prints one readable bot game; `-h2h` plays the current GreedyBot against `BotStyle.Baseline()` (or, with `-off Switch`, against itself minus one switch) in every mirror. Balance tools: `-balance [-cards]` (win matrix, per-card cast stats), `-scan` (card power table, playtest/CARD_POWER.md), `-impact`, `-optimize` (deck tuning by measurement), and `-data <folder>` to load a changed copy of StreamingAssets; `MatchResult.CardGames/CardWins` track which cards each side cast.
- `GameText` (in Rules) turns cards, actions and events into readable text. It is also used by tests and will be useful for replays.

**Not yet implemented** (next steps; a ready-made prompt for the next session is in [handoff/NEXT_SESSION.md](handoff/NEXT_SESSION.md))
- A Tavern Dweller zone that can be targeted or removed (v0.1: it can't), and Tavern Dwellers in multiplayer politics.
- Filtering events by hidden information. The affected player choosing the order of replacement effects (GAME_DESIGN §8.1).

