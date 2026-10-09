# Restarted Tavern — Development Guide

How the game will be built. Rules live in [GAME_DESIGN.md](GAME_DESIGN.md); this document covers architecture and how we work.

**Status:** pre-production. The first card set (v0.1) is designed. The tech stack is chosen (§0).

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
│  ├─ Rules.Tests/                RestartedTavern.Rules.Tests.asmdef (EditMode, NUnit)
│  ├─ Cards/                      card data (JSON) + rare custom card scripts
│  └─ Client/                     Unity presentation: scenes, UI, animations (refers to Rules)
└─ Server/  (later)               a .NET host that compiles the same Assets/Rules source files
```
- `noEngineReferences: true` on the Rules assembly **enforces** at compile time that rules code can't touch `UnityEngine`. This keeps it portable to a server and fast to test.
- The Rules assembly targets the C# subset that Unity 6 supports (C# 9), and avoids reflection-heavy libraries, so it can also compile as a plain .NET library later.
- 🟡 Card data is stored as **JSON** (Unity's built-in parsing works without extra packages). YAML stays possible later.


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

- `CardInstance.currentHealth` stores the **permanent damage** model directly. There is no "damage marked this turn" field.
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
2. **Rules engine prototype**: the Rules assembly with EditMode tests, playable through a minimal debug UI in Unity, with about 20 test cards.
3. **Playtest** (paper or the debug UI): tune the Gold cap, the curve and the impact of permanent damage.
4. **Minimal visual client** in Unity (Windows build): hot-seat 1v1.
5. Implement the full first set (120 cards) and a basic AI.
6. Later: multiplayer (3–4 players), singleton format, online play.

---

## 6. Open Technical Questions ❓
- Card art pipeline and card frame rendering.
- AI approach for vs.-AI play (rule-based first? Monte Carlo search, which works because the engine is deterministic?).
- CI: running Unity tests on GitHub Actions needs a Unity license setup (GameCI). Decide when the prototype has tests worth guarding.
- Online (later): hosting, and how matchmaking works.
