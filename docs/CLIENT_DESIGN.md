# Visual Client Design

The Unity game client (v1.0, 2026-10-10): hot-seat 1v1 and vs. the bot, Windows, 16:9 landscape.
The IMGUI debug table (`Assets/Client/DebugTable.cs`) stays as a rules-testing tool.

Legend: 🔒 decided. The look (art, frames, colours) can still change in later releases; the rules it shows can't (GAME_DESIGN §0).

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

### 1.1 Layout sketch (as built)

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
whole uGUI table from code at runtime (Screen Space – Camera, 1920×1080 reference, scale with screen size; card text
and titles in TextMeshPro, small labels in legacy `Text`); `TableBuilder` (editor) makes `Assets/Scenes/Table.unity` (camera, event
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
- **Card faces** (`CardFaces`, LoR style, Decision Log 2026-10-10): full-bleed art window inside a cut-corner gold
  frame, type tab on top, name, keyword plates and text, faction icon and rarity gem at the bottom, round cost gem,
  Power / Health shields; units are the same card, smaller; tokens are domes. Buffs in green, damage in red. Art drops
  in from `Resources/CardArt/<card id>.png` with no code change. Colours: [COLOR_BASELINE.md](COLOR_BASELINE.md).
- **Start-up**: no Unity splash, a short loading screen, then the main menu (§2.3). Random seed unless `-seed` (shown
  bottom left). The table is a fixed 1920x1080 area scaled to fit the window (CanvasScaler Expand), so it never crops.
- **Debug** (F1 / Debug button, `TableView.Debug.cs`): engine and table state, every legal action as a button, the
  event log, reveal the opponent's hand, "bot moves for me", errors shown on screen, playtest logs saved to
  `Playtests/` next to the exe (automatically at game over).
- **Report bug** (`TableView.BugReport.cs`): the player types what went wrong; `BugReports/<time>/` next to the exe
  gets `screenshot.png` (without the dialog) and `report.txt` (note, setup, seed, both players' full state, Chain,
  legal actions, picker / combat stage, errors, log, and `MatchSession.History`, which replays the game exactly).
  The button is on every screen (the menu, deck editor, game over and handoff screens draw their own) and on F2.
- **Settings** (`TableView.Settings.cs`): main menu tile, or Escape at the table when nothing is being picked or
  zoomed (the bot waits). Speed, window mode, resolution, VSync, keyword hints, coin toss; PlayerPrefs.
  Text fields keep the UI focus (the table otherwise clears it each frame so Space never presses the last button).
- **Screenshot scenes** (`TableView.Shots.cs`, used by `Tools/GuideShots`): `-place id,id` / `-placeopp id,id` (cards on
  P1's / P2's battlefield; `id*2` = 2 damage, `id!` = tapped; Equipment goes on the last creature of its side, a Curse
  on the last of the other side), `-gold N`, `-settle` (the bot and the tutorial's passes act until it's the viewer's
  decision), `-act <card id>` (the viewer takes that card's first legal action), `-stage` (stage every attacker or
  blocker), `-nocoach` (hide the tutorial box), `-bugopen`, `-guide [page or search]`, `-soundboard`. Screenshots ignore the real mouse.
- **Command line**: `-menu`, `-settings`, `-debug`, `-reveal`, `-bugreport note`, `-seed N`, `-deck1/-deck2 N`, `-bot1`, `-human2` (hot-seat), `-autoplay N` (the bot plays for
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

- **Home** (`TableView.Menu.cs`): title, tiles Continue (in a game) / Play / Tutorial ("START HERE") / Deck editor / Tavern Guide + Settings (half tiles) / Quit, a hoverable fan of showcase cards (rarest creature per faction) and the faction emblems. **Play** is the setup page (seats human / bot, deck, Tavern Dweller, Battle).
- **Tutorial** (`Client/Logic/Tutorial.cs`, `TableView.Tutorial.cs`): `Tutorial.Setup` stacks the scripted cards on top (`PlayerSetup.KeepDeckOrder`, `GoesFirst`), the steps are Info (a box with Next; nothing moves), You (only `Allow`ed actions pass `MatchSession.HumanFilter`) and Opponent (`MatchSession.BotOverride`); the player's other priority passes are taken for them (`AutoHumanAction`). After the round 3 attack it releases into a normal bot game. Skip tutorial releases at once; Undo is off while it runs. `-tutorial` / `-tutorialstep N` start it (played up to step N) for screenshots; `TutorialTests` play it through.
- **Coin toss** (`StartToss`): before every game from the menu or a rematch, a coin flips (viewer's side blue with their Tavern Dweller's initials) and lands on whoever the engine picked to go first; click skips.
- **Playtest fixes** (2026-10-10): `ActionPicker` always asks target slots, even with one option. `KeywordGlossary` boxes beside the zoom (and a lifted hand card); an invisible `ZoomHit` area from the card's edge over the zoom keeps it open while hovered. `DrawPhases`: the phase tracker at the lane's right end (since the v1.0 sprint: only the current phase, lit, with the next one smaller and faded under it; "Combat damage" instead of "Damage"); `StepStartedEvent` banners for the opponent's attack and for blocks. Coach Info boxes: hidden while beats play, not closed by the context button, Space works after 0.5 s.

### 2.4 Tavern Guide (built 2026-10-10)

The in-game encyclopedia (user: "search for any mechanic in the game and get a visual and text explanation").
- **Content** (`Client/Logic/TavernGuide.cs`, no Unity): about 60 pages in 8 categories (Basics, Rounds & Mana, Cards,
  Combat, The Chain, Keywords, Rules Terms, Playing the Game). A page has rich text, search aliases, example card ids,
  screenshot frames with captions, and related pages. `Search` needs every word to match and ranks title > alias >
  category > text. `TavernGuideTests`: unique ids, links resolve, example cards exist, every screenshot exists, every
  keyword the cards explain (`KeywordGlossary`) has a page found first, every card type has a page.
- **Screen** (`TableView.Guide.cs`): its own layer above the overlay (table redraws don't touch it, so the search field
  keeps focus and filters on every keystroke). Left: search and the list (grouped by category, or ranked results).
  Right: title, category, the pictures (several frames play as a slideshow every 2.4 s, like a GIF; dots jump to a
  frame), the text, up to two example cards drawn live with `CardFaces`, and See also buttons. The game waits while it's
  open; Esc or Close shuts it; it hides while the bug report dialog is up.
- **Open it**: Tavern Guide tile on the main menu, Guide button in the table's top bar, the Settings panel, or F3.
- **Pictures** (`Assets/Resources/Guide/*.jpg`): real screenshots of the game, made by
  `python Tools/GuideShots/guide_shots.py [names]` from the built exe with the screenshot scene flags (§2.2), cropped
  and saved as JPEG. `GuideImageImporter` imports them as uncompressed-size sprites (no power-of-two scaling, no mipmaps).
  After a visual change, rebuild the exe and rerun the script.
- **Adding a mechanic later**: add a page in `TavernGuide.Build` (and a shot in `guide_shots.py` if it needs one); the
  tests fail if a new keyword in `KeywordGlossary` has no page.

### 2.5 Sound (built 2026-10-10)

- **Synthesis v2** (`Client/Logic/SfxSynth.cs`, no Unity; smooth, LoR-style, Decision Log): `Render(kind, faction)`
  gives mono 44.1 kHz samples. Each faction is an instrument of sine partials (higher ones fade faster, so notes mellow),
  with its own attack, chorus, vibrato and breath; gestures add pads, bells, muffled metal, low booms, a rumble and
  band-pass whooshes; then a small Schroeder hall reverb, a low-pass, a soft limiter and levelling to -18 dB RMS.
  Kinds (`SfxKind`, named after LoR's sound events): Creature (summon), Instant, Sorcery, Equipment, Relic, Curse,
  Ability, Power, Trigger, Equip, Countered, Pass, RoundStart, Attack (attack declare), Block (block declare), Death. `SfxSynthTests`: every sound audible, in range, short, fading out (no click), different per faction, deterministic.
- **Playback** (`TableView.Audio.cs`): clips are made on first use and cached; 12 `AudioSource`s; `SoundFor` is called
  for each event in `PlayBeats` with the beat's time, so a sound plays when its animation does (`PlayDelayed`). Attack,
  block and death play once per batch of events (one war drum for a whole attack). Pass
  sound only for real passes with an empty Chain in the action phase (not the engine's automatic ones).
- **Settings**: Sound (Off / Low / Medium / High, `AudioListener.volume`), Pass sound, Trigger ticks; **Sound board**
  (Settings → Sound board, `-soundboard`) plays every kind in every faction voice.
- **Checking sounds without ears**: Restarted Tavern → Export Sound Preview (`SfxPreview.Export`) writes
  `Builds/sfx_preview.wav` and one WAV per sound in `Builds/sfx/`; `python Tools/SoundCheck/sound_check.py` draws a
  spectrogram sheet and prints loudness, pitch, brightness, harshness, clicks and cut-off tails, flagging problems.

## 3. Status and later
Built: everything in §2. Later releases (additions only): painted card art and portraits, music, a mulligan screen
with card picks, the history rail as cards, multiplayer (3–4) layouts, online play.

## 4. Open (for later releases)
- Multiplayer (3–4) layout, with GAME_DESIGN §13.
- Art production: who paints (commission, AI-assisted, mixed).
