# Visual Client Design

The real Unity table (DEVELOPMENT §5, roadmap step 4): hot-seat 1v1 and vs. the bot, Windows, 16:9 landscape.
The IMGUI debug table (`Assets/Client/DebugTable.cs`) stays as the rules-testing tool.

Legend: 🔒 decided · 🟡 proposal · ❓ open

---

## 1. Direction 🔒 (user, 2026-10-10)

**The screen works like Legends of Runeterra; the zones and the hand look like MTG Arena.**

| Area | Model | What it means here |
|---|---|---|
| Hand | MTGA | A fanned hand at the bottom edge, cards lift on hover, drag a card up onto the board to play it. The opponent's hand is a fan of card backs at the top. |
| Zones | MTGA | Graveyard and exile as piles with a count (click to browse), the deck as a pile with a count. The Chain shows on the side like MTGA's stack, top item largest. Equipment and Curses sit under / beside what they're attached to. |
| Passing | LoR | One big round context button at the right edge: **Pass**, **End Round** (when the other player just passed), **Attack** / **Block** (confirm a staged combat), **OK** (resolve the Chain / confirm a choice). It says whose action it is. |
| Mana | LoR | Mana as a column of gems at the right (filled = available, outline = spent this round, up to 10). **Gold = LoR spell mana**: a separate row of 3 gem slots under the mana, which already matches the rules (unspent mana banks as Gold at round end, cap 3, spent first on spells). The attack token sits next to the leader's gems. |
| Attack / block | LoR | The attacker drags creatures into the **combat lane** in the middle and presses Attack; the defender drags blockers in front of attackers in the lane and presses Block. Nothing is sent to the engine until the confirm, so creatures can be rearranged. |
| Art style | — | **Stylized painterly** (warm, chunky, slightly cartoony; Hearthstone / LoR family). |
| Board view | — | **2D with a slight tilt** on the battlefield (like LoR / MTGA). |
| Setting | — | **The tavern table**: cards are played on a worn wooden tavern table. |
| Card art | — | **Placeholder frames first**: real frames (faction colours, cost / Power / Health gems, rarity, text box) with the faction emblem in the art window. Art drops in later by card id, no code change. |
| Creatures on the battlefield | LoR | **Unit cards**: portrait crop with big Power and Health and keyword icons. Damage shows as a red Health number (permanent damage must be obvious). Hover / right-click shows the full card. |
| Tavern Dweller | LoR | **The Nexus spot on the left edge**: your portrait lower left, the opponent's upper left. Life on the portrait, the Power as a button beside it (lit when usable). Target it with spells and attacks like a Nexus. |
| Animations | — | **Snappy with key moments**: quick readable moves for normal actions; bigger moments for a new round, Tavern Dweller Powers, big spells, deaths and game over. A speed setting for playtests. |

### 1.1 Layout sketch 🟡

```
┌────────────────────────────────────────────────────────────────────┐
│ [Opp Dweller]        opponent hand (backs)            [deck][gy] ◆◆◆│ ← opp mana gems
│  portrait/life                                                  ◇◇◇ │ ← opp Gold
│  [Power]        ┌─ opponent back row (units, Relics) ─┐             │
│                 │                                      │   Chain    │
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─  COMBAT LANE  ─ ─ ─ ─ ─ ─ ─ ─ ─ │  (stack)   │
│                 │                                      │      (PASS)│ ← context button
│  [Power]        └─ your back row (units, Relics) ─────┘             │
│ [Your Dweller]                                                  ◆◆◆ │ ← your mana gems
│  portrait/life  ⚔ token      your hand (fanned, MTGA)  [deck][gy] ◇◇◇│ ← your Gold
└────────────────────────────────────────────────────────────────────┘
```

---

## 2. Client logic 🔒 (built 2026-10-10)

`Assets/Client/Logic/` (`RestartedTavern.Client.Logic`, no Unity references, tested in
`Assets/Client/Logic.Tests/`). The visual layer only talks to these classes; it never reads `GameState` or calls the
engine itself.

| Class | Job |
|---|---|
| `MatchSession` | One game: engine, state, seats (Human / Bot), undo (human actions), `Submit`, `StepBot`, `CommitCombat`. **Hot-seat**: the screen follows the human the game waits on; when that changes, `HandoffPending` covers the table ("pass to P2") until `AcknowledgeHandoff`. |
| `TableSnapshot` | What the viewer may see: players (life, mana, Gold, Gold cap, attack token, hand, deck count, zones, Tavern Dweller), unit stats with damage and keywords, attackers / blockers, the Chain, the decision being made. Hidden cards have no definition. |
| `ActionPicker` | Turns the flat legal-action list into clicks: pick a source (hand card, permanent, Tavern Dweller) and it asks one question at a time (mode → sacrifice → each target → Invest → X → division → defender → blocked attacker) until one legal action is left. `Sources` = what to glow; `Prompt.Targets` = what to highlight while dragging. Tested to reach exactly the legal actions in bot games. |
| `CombatStage` | LoR combat: stage attackers into the lane / blockers in front of attackers, unstage, then `MatchSession.CommitCombat` replays it as the engine's attack action, priority passes and one-by-one declarations. Legality is checked on a copy of the state. If an opponent responds in the attack window, the player answers by hand and the commit carries on. |
| `PresentationQueue` | Events → beats to animate one by one, with a weight (Instant / Short / Normal / Long) and a hidden flag (opponent draws, hand↔deck moves). When the queue is empty, rebind to a fresh `TableSnapshot`. |

### 2.1 Context button logic 🟡
From the session and snapshot: a staged combat → Attack / Block; a decision pending → its prompt (OK, Keep / Mulligan
on a mulligan); something on the Chain → OK (pass, let it resolve); the other player just passed → End Round;
otherwise → Pass. Disabled with "Opponent's action" when it isn't the viewer's call.

---

## 3. Build plan 🟡
1. **Table scene skeleton**: uGUI canvas (1920×1080 reference), the zones from §1.1 as plain panels, bound to
   `TableSnapshot`; hand + units + mana gems + context button + Chain. Clicks through `ActionPicker`.
2. **Drag and drop**: play from hand by dragging, targeting arrow, combat lane staging.
3. **Card frames**: placeholder frames per faction (hand card + unit card variants), keyword icons.
4. **Animations** from `PresentationQueue` (DOTween-style tweens written in-house or a package, ❓).
5. **Hot-seat screens**: handoff cover, mulligan screen, game over, deck pick.
6. **Tavern table art pass**: board background, portraits, gem art (placeholder → painted).

## 4. Open ❓
- Tween library: a small in-house tweener vs. a package (DOTween is not on the Unity registry).
- Multiplayer (3–4) layout: later, with §13 of GAME_DESIGN.
- Art production: who paints (commission, AI-assisted, mixed) once the frames are in.
