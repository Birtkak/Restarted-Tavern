# Restarted Tavern — Development Guide

How the game will be built. Rules live in [GAME_DESIGN.md](GAME_DESIGN.md); this document covers architecture and how we work.

**Status:** engine prototype. The first card set (v0.1) is designed, and the rules engine skeleton runs with 62 passing EditMode tests. A hot-seat **debug table** in Unity can play it (§7).

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
│  │   ├─ Flow/                   turn structure, phases, priority, the Chain
│  │   ├─ Combat/                 attackers, blockers, damage, Trample
│  │   ├─ Effects/                effect building blocks (damage, heal, draw, create token...)
│  │   └─ StateBasedActions/      deaths, life loss, Legendary rule, detached Curses
│  │   └─ Cards/                  PrototypeCards.cs: the ~20 prototype cards, in C# until the data format is settled
│  ├─ Rules.Tests/                RestartedTavern.Rules.Tests.asmdef (EditMode, NUnit)
│  ├─ Cards/          (later)     card data (JSON) + rare custom card scripts
│  ├─ Client/                     RestartedTavern.Client.asmdef: DebugTable.cs (IMGUI debug table); Editor/ builds the scene and the exe
│  └─ Scenes/                     DebugTable.unity
└─ Server/  (later)               a .NET host that compiles the same Assets/Rules source files
```
- `noEngineReferences: true` on the Rules assembly **enforces** at compile time that rules code can't touch `UnityEngine`. This keeps it portable to a server and fast to test.
- The Rules assembly targets the C# subset that Unity 6 supports (C# 9), and avoids reflection-heavy libraries, so it can also compile as a plain .NET library later.
- 🟡 Card data is stored as **JSON**. YAML stays possible later. ❓ Unity's built-in `JsonUtility` lives in `UnityEngine`, so the Rules assembly can't use it. Options: the Client loads the JSON and hands `CardDefinition`s to the engine, or the Rules assembly gets its own small JSON reader (also needed for the server).


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
4. **N players from day one.** Players live in an ordered list, and turn order and response order run around that list. Nothing in the engine assumes exactly 2 players, even though only 2-player Standard ships first.
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

PlayerState    { id, teamId, seat, eliminated, patronId, life, maxMana, mana, gold, attachedCurses[],
                 zones: { deck, hand, battlefield, graveyard, exile } }

CardDefinition { id, name, type, cost, power?, health?, keywords[],
                 abilities[], overcharge?, equipCost?, faction | "neutral",
                 rarity, text }

PatronDefinition { id, name, factions[2], passive, power { goldCost, effect } }

CardInstance   { objectId, definitionId, owner, controller,
                 currentHealth, tapped, summoningSick, counters{}, attachments[] }
```

- 🟡 Implemented as `CardInstance.Damage`: damage that **never wears off** (no "damage marked this turn"). Remaining Health is computed as max Health − Damage. This matches MTG's damage counters except for the cleanup reset. 🔒 Exception (GAME_DESIGN §7.3): when a Health buff ends, damage is capped at max Health − 1, so losing a buff can't kill.
- `chain` is a LIFO list of pending spells/abilities. When every player has passed in a row (`passesInRow == players.length`), the top item resolves. The engine auto-passes for players who have no legal response.
- Curses can attach to a creature *or* a player, so attachments target `ObjectId | PlayerId`.
- `objectId` is new every time a card changes zone (as in MTG), so "that creature" effects stop applying once it leaves.

---

## 3. Card Data Format 🟡

Cards are written in a text data file (JSON or YAML), one entry per card. Example:

```yaml
id: tavern_brawler
name: Tavern Brawler
type: creature
cost: 3
power: 3
health: 4
keywords: [armor_1]
abilities:
  - trigger: arrival
    effect: { deal_damage: { amount: 1, target: any_creature } }
text: "Armor 1. Arrival: Deal 1 damage to any creature."
```

🟡 The rules `text` should eventually be *generated* from `abilities`, so the text and the behavior can never disagree.

---

## 4. Testing Strategy 🟡
- **Rules unit tests** for every rule in GAME_DESIGN.md. When a rule changes, its test changes in the same commit.
- **Card tests**: each card with a scripted effect gets at least one scenario test.
- **Random-play soak tests**: run thousands of games between random-action bots and check invariants (no negative mana, the total number of cards is conserved, the game always ends).
- **Replay tests**: replay saved action logs and compare the final state.

---

## 5. Roadmap (draft)
1. ✅ **Ruleset v0.1 and first set**: 5 factions × 20 cards, 10 Neutral cards, 10 Patrons.
2. ✅ **Rules engine prototype**: the Rules assembly with EditMode tests, playable through a minimal debug UI in Unity, with about 20 test cards (§7).
3. 🚧 **Playtest** (paper or the debug UI): tune the Gold cap, the curve and the impact of permanent damage. *Bot simulations and the first findings are in [playtest/PLAYTEST.md](playtest/PLAYTEST.md); human playtests are next.*
4. **Minimal visual client** in Unity (Windows build): hot-seat 1v1.
5. Implement the full first set (120 cards) and a basic AI.
6. Later: multiplayer (3–4 players), singleton format, online play.

---

## 6. Open Technical Questions ❓
- Card art pipeline and card frame rendering.
- AI approach for vs.-AI play (rule-based first? Monte Carlo search, which works because the engine is deterministic?).
- CI: running Unity tests on GitHub Actions needs a Unity license setup (GameCI). Decide when the prototype has tests worth guarding.
- Online (later): hosting, and how matchmaking works.

---

## 7. Engine Prototype Status

**API** (`GameEngine`): `CreateGame(format, players, seed)`, `GetLegalActions(state, player)`, `Apply(state, action) → events`, `WaitingOn(state)`. `Apply` **changes the state in place** and only accepts actions from the legal list. Call `GameState.Clone()` first to keep the old state (for AI search or undo). `GameState.CreateViewFor(player)` hides the other players' hands, all decks and the RNG.

**Implemented**
- Setup: a random first player, 7-card hands, the **London mulligan**, the first player skips the turn-1 draw, and the second player gets 1 Gold. `FormatConfig.MultiplayerStandard()` uses 40 life and no compensation.
- Turn structure (§6): Start, Draw, Main 1, the combat steps, Main 2, End, and Cleanup (discard down to 7, then unspent mana becomes Gold capped at 5, then "until end of turn" effects end).
- Mana and Gold (§5): mana is only available on your own turn. Instants can be paid with any mix of mana and Gold. Overcharge is paid with Gold only.
- **The Chain** (§8): LIFO; the caster keeps priority; it resolves when every living player passes in a row; spells fizzle when their target is illegal; the fixed priority windows; auto-pass for players who have no other option (`GameState.AutoPass`).
- **Multiple targets** (MTG 115, 608.2b): a spell has a list of target slots (optional slots for "up to N"). Targets are distinct, and illegal targets are skipped at resolution; the spell only fizzles when every target is gone. **Fight** (§11.1).
- Triggers: Arrival, Last Breath, Attacks, Start/End of your turn. They use APNAP order and a target choice when they're put on the Chain. A trigger with no legal target is removed.
- Combat (§7): attackers and blockers are declared one creature at a time, and every attacker picks which opponent it attacks. Also implemented: summoning sickness and Haste, Flying/Reach, Can't block, Trample, Lifelink, and multiple blockers.
- **Permanent damage**, with Heal capped at max Health or starting life. Losing a buff can't kill (§7.3).
- State-based actions (MTG 704): 0 life, drawing from an empty deck, lethal damage, the Legendary rule, illegal Curses, unattaching Equipment, and the game ending when one team is left.
- Continuous effects: static anthems/lords, +1/+1 counters and until-end-of-turn modifiers, applied in MTG layer order.
- New object ids on every zone change. Tokens stop existing when they leave the battlefield.
- 37 prototype cards (Goobers, Evergrowing Wild, Glitterworld without Equipment, Neutral) and three legal 60-card decks (Goober Mob, Jungle Stampede, Zoo Patrol) in `Assets/Rules/Cards/PrototypeCards.cs`.

**Tests** (`Assets/Rules.Tests`, 62 tests): rules unit tests per area, card scenario tests, a **random-play soak test** (100 full games between random bots with invariant checks after every action) and **determinism** tests (same seed and actions give the same game). Run them headless:
```
"C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml
```

**Debug table** (`Assets/Client/DebugTable.cs`, scene `Assets/Scenes/DebugTable.unity`). This is a hot-seat IMGUI table for 1v1 with the three prototype decks (`PrototypeCards`); the P1/P2 deck buttons choose them for the next game.
- Waiting on someone: the top bar names them and their header turns green.
- Your options: their legal actions are listed as buttons. Cards they can act with are tinted green, and clicking a card filters the list to the actions that involve it.
- What's on the table: the board shows Power/Health, damage, keywords, and tapped/sick/attacking/blocking states, plus the Chain (top first) and the current combat.
- Hidden information: a hand is shown only while its owner is the one to act (or with *Show all hands*). In hot-seat the log doesn't name drawn cards.
- Controls: *Undo* (last 200 states), *New game* (with a seed), *Auto-pass*, and either player can be handed to a random bot.
- Build: menu **Restarted Tavern → Build Windows Debug Table**, or headless:
  ```
  Unity.exe -batchmode -quit -projectPath . -executeMethod RestartedTavern.Client.Editor.DebugTableBuilder.BuildWindows
  ```
  This writes `Builds/DebugTable/RestartedTavern.exe` (git-ignored). Command-line flags: `-seed N`, `-bot1`, `-bot2`, `-autoplay N` (the bots play N actions at startup), and `-autoshot file.png` (take a screenshot, then quit), for automated checks.
- The P1/P2 bot toggles use `GreedyBot` (`Assets/Rules/AI`), a deterministic rule-based player. `MatchRunner` and `Experiments` run bot-vs-bot balance experiments (see [playtest/PLAYTEST.md](playtest/PLAYTEST.md)).
- `GameText` (in Rules) turns cards, actions and events into readable text. It is also used by tests and will be useful for replays.

**Not yet implemented** (next steps)
- Activated abilities (Tap: …, Pay X Gold: …) and **Patron powers and passives**.
- Equip, and casting Curses/Relics with real cards (the rules support exists, but no cards use it yet).
- Player choices that are currently automatic: how an attacker splits damage among several blockers (§7.2), which Legendary to keep, and ordering your own simultaneous triggers.
- Divided damage ("deal 3 damage divided as you choose"), X costs, "may" choices, rummaging, and "whenever this is dealt damage" triggers (Worldroot Hydra).
- Replacement effects, control-changing effects, and filtering events by hidden information.
- Loading card data from JSON (see §0.1).

