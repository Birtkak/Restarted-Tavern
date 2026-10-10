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
| `TableControls` | The LoR context button (`Main`: Keep / Pass / End round / OK / Continue / Attack / Block / No blocks / Handoff / Waiting, with the action it submits) and the choice panel (`Choices`: sourceless actions with `GameText` labels: options, declining a target, mulligan). `Describe` labels picker options. Tested: in hot-seat bot games every legal action is reachable from some control, and Pass / End round predict the engine. |
| `PresentationQueue` | Events → beats to animate one by one, with a weight (Instant / Short / Normal / Long, default seconds in `Seconds(weight, speed)`) and a hidden flag (opponent draws, hand↔deck moves). When the queue is empty, rebind to a fresh `TableSnapshot`. Note: object ids change on every zone change (`ZoneChangedEvent.OldId → NewId`), so card views must be re-keyed on that event. |

### 2.1 Context button logic 🔒 (`TableControls.Main`)
Game over → disabled. Handoff pending → "P2, take the table". Not the viewer's call → "Opponent's action" (or
"Attacking..." while a committed attack waits on a response). Mulligan → Keep (Mulligan is in the choice panel).
Declaring attackers / blockers → Attack / Block with something staged, else Don't attack / No blocks. Another decision
→ its prompt, chosen on the table or in the choice panel. A staged attack in the action phase → Attack. Something on
the Chain → OK. A combat priority window → Continue. Otherwise Pass, or End round when everyone else has passed.

### 2.2 The table scene (built 2026-10-10)
`Assets/Client/Table/` (in `RestartedTavern.Client`, which now references `UnityEngine.UI`). `TableView` builds the
whole uGUI table from code at runtime (Screen Space – Camera, 1920×1080 reference, scale with screen size, legacy
`Text` so no TextMeshPro import is needed); `TableBuilder` (editor) makes `Assets/Scenes/Table.unity` (camera, event
system, TableView) and builds `Builds/Table/RestartedTavern.exe`. The table is rebuilt from a fresh `TableSnapshot`
after every change; the events of the change then animate on top (FLIP slides + effects, `TableView.Beats.cs`, [LOR_PRESENTATION.md](LOR_PRESENTATION.md) §6).
- **Layout** as §1.1: Tavern Dwellers left (life on the portrait, Power button beside it), opponent's hand as backs at
  the top, rows of units above and below the combat lane, MTGA fanned hand at the bottom (lift + grow on hover), piles
  (deck / graveyard / exile, click to browse) and mana column + 3 Gold diamonds + attack token on the right, the Chain
  above the context button (top item largest).
- **Glows**: blue = can be used, red = legal target, orange = can attack / block, white = picked, green = your card
  whose triggered ability is going off (choosing its target, or on the Chain) and its Chain bubble; green wins over
  red when it may target itself.
- **Playing**: click a glowing card (or drag it from the hand above the hand line) → `ActionPicker`; targets are
  clicked on the table (or the card is dropped straight onto one), with an MTG Arena targeting arrow from the source (also from your trigger's card while it picks a target): a
  segmented curve to the pointer that snaps onto a legal target under it and turns gold. Once picked, the Chain
  bubble's target lines show the choice. Real
  choices (modes, X, Invest, options, mulligan, off-table targets) appear in a box in the middle of the lane; "No more
  targets" and Cancel sit beside the prompt bar. Right-click or Esc cancels.
- **Hand cards**: the art window shrinks (42% down to 25% of the card) when the rules text wouldn't fit at a readable
  size.
- **Combat**: drag units into the lane (or click a unit that has no ability) to stage attackers, drag them out to
  unstage; to block, drag a unit onto an attacker (or click the blocker, then the attacker). The context button
  commits (`CommitCombat`).
- **Other**: hover / right-click (pin) zoom for units and Tavern Dwellers; hot-seat cover; game-over screen with
  Rematch / Undo; Undo, New game and bot speed buttons; Space = context button, Esc = cancel, Ctrl+Z = undo.
- **Placeholder frames** (`CardFaces`): faction colour + accent, emblem text in the art window, cost / Power /
  Health gems, rarity pip, text box; units show no cost, buffs in green, damage in red, keyword tags. Art drops in
  from `Resources/CardArt/<card id>.png` with no code change. Faction colours are a proposal (still to confirm).
- **Start-up** (user request 2026-10-10): no Unity splash, a short loading screen, then the **main menu**: per
  player Human / Bot, one of the prototype decks and a Tavern Dweller that can lead it (the deck rule: its factions
  must cover the deck, so today each deck has only its own), then **Battle**. Menu button in game, Rematch / Main
  menu at game over. Random seed unless `-seed` (shown bottom left). The table is a fixed 1920x1080 area scaled to
  fit the window (CanvasScaler Expand), so it never crops.
- **Debug** (F1 / Debug button, `TableView.Debug.cs`): engine and table state, every legal action as a button, the
  event log, reveal the opponent's hand, "bot moves for me", errors shown on screen, playtest logs saved to
  `Playtests/` next to the exe (automatically at game over).
- **Report bug** (`TableView.BugReport.cs`): the player types what went wrong; `BugReports/<time>/` next to the exe
  gets `screenshot.png` (without the dialog) and `report.txt` (note, setup, seed, both players' full state, Chain,
  legal actions, picker / combat stage, errors, log, and `MatchSession.History`, which replays the game exactly).
- **Command line**: `-menu`, `-debug`, `-reveal`, `-bugreport note`, `-seed N`, `-deck1/-deck2 N`, `-bot1`, `-human2` (hot-seat), `-autoplay N` (the bot plays for
  everyone, `MatchSession.AutoStep`), `-until attack|block` (stop there and stage everything), `-autopick`,
  `-autoshot file.png` (use an absolute path), `-shotat seconds` (shoot that long after the table is drawn, to catch the
  beats), `-board N` (N permanents on each side, some tapped or equipped, to check the board layout), `-hover <card id>` (the screenshot hovers that card in hand), `-editor` (opens the deck editor).
- **Board** (`TableView.Board.cs`, MTG Arena style, Decision Log 2026-10-10): tapped units sideways, attachments tucked
  behind their host (stacked up and to the right, a corner peeking out) and fanned out on hover (hover a fanned one to
  zoom it), each side scaled to fit. Token creatures are domes (a half circle with a flat underside). Attackers in the
  lane only tilt instead of lying sideways, and a crowded lane shrinks them to fit (down to half size). See
  LOR_PRESENTATION §6.
- **Deck editor** (`TableView.DeckEditor.cs`, "Deck editor" in the main menu), Hearthstone's collection layout in the
  LoR look: "My decks" tiles (a prototype deck opens as a copy) and New deck, which picks a Tavern Dweller first; faction
  tabs over a 4 x 2 book of cards with page arrows; mana gems (All, 0-7+), type and search (Enter) below; on the right
  the Tavern Dweller, the name, LoR deck rows (cost, name, copies), curve and count, Done / Save / Delete. Click a card to
  add a copy, right-click it or click its row to take one out, hover to zoom. Decks go
  to `custom_decks.json` in Unity's persistentDataPath (`CardPool.CustomDecksFile`, the prototype deck format) and
  come after the prototype decks in the menu; BATTLE refuses a deck that isn't legal yet (60 cards, 4 copies,
  factions). Sims and tests don't see them.

---

### 2.3 Main menu, tutorial, coin toss (built 2026-10-10)

- **Home** (`TableView.Menu.cs`): title, tiles Continue (in a game) / Play / Tutorial ("START HERE") / Deck editor / Quit, a hoverable fan of showcase cards (rarest creature per faction) and the faction emblems. **Play** is the setup page (seats human / bot, deck, Tavern Dweller, Battle).
- **Tutorial** (`Client/Logic/Tutorial.cs`, `TableView.Tutorial.cs`): `Tutorial.Setup` stacks the scripted cards on top (`PlayerSetup.KeepDeckOrder`, `GoesFirst`), the steps are Info (a box with Next; nothing moves), You (only `Allow`ed actions pass `MatchSession.HumanFilter`) and Opponent (`MatchSession.BotOverride`); the player's other priority passes are taken for them (`AutoHumanAction`). After the round 3 attack it releases into a normal bot game. Skip tutorial releases at once; Undo is off while it runs. `-tutorial` / `-tutorialstep N` start it (played up to step N) for screenshots; `TutorialTests` play it through.
- **Coin toss** (`StartToss`): before every game from the menu or a rematch, a coin flips (viewer's side blue with their Tavern Dweller's initials) and lands on whoever the engine picked to go first; click skips.
- **Playtest fixes** (2026-10-10): `ActionPicker` always asks target slots, even with one option. `KeywordGlossary` boxes beside the zoom (and a lifted hand card); an invisible `ZoomHit` area from the card's edge over the zoom keeps it open while hovered. `DrawPhases`: the phase tracker at the lane's right end; `StepStartedEvent` banners for the opponent's attack and for blocks. Coach Info boxes: hidden while beats play, not closed by the context button, Space works after 0.5 s.

## 3. Build plan 🟡
1. ~~**Table scene skeleton**~~ done (§2.2).
2. ~~**Drag and drop**~~ done (§2.2), apart from playing a card by dropping it on the board.
3. **Card frames**: placeholder frames per faction (hand card + unit card variants), keyword icons.
4. **Animations**: LoR clarity + core beats built with PrimeTween ([LOR_PRESENTATION.md](LOR_PRESENTATION.md) §6). The round ceremony, the Chain as cards and the history rail are next.
5. **Hot-seat screens**: handoff cover, mulligan screen, game over, deck pick.
6. **Tavern table art pass**: board background, portraits, gem art (placeholder → painted).

## 4. Open ❓
- ~~Tween library~~: PrimeTween 1.3.3 from OpenUPM 🔒 (2026-10-10).
- Multiplayer (3–4) layout: later, with §13 of GAME_DESIGN.
- Art production: who paints (commission, AI-assisted, mixed) once the frames are in.
