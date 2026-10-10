# Next session: build the table scene (and extras)

Paste everything below the line into a new session.

---

You're continuing work on Restarted Tavern, a Unity 6 (6000.6.4f1) + C# trading-card game.
Repo: C:\Users\Birre\Desktop\Claude shizzle\Restarted-Tavern (GitHub: Birtkak/Restarted-Tavern, main).
Run `git status` and `git log origin/main..` first and tell the user what's pending.

GOAL OF THIS SESSION: build the **visual table scene**, the first real playable client (hot-seat 1v1 and vs. the bot,
Windows), on top of the client logic layer that already exists. Read **docs/CLIENT_DESIGN.md** first: it's the source
of truth for the direction the user locked on 2026-10-10.

THE DIRECTION (user-decided, don't re-ask)
- **The screen works like Legends of Runeterra; the zones and the hand look like MTG Arena.**
- MTGA: fanned hand at the bottom (lift on hover, drag up to play), opponent's hand as backs at the top, deck /
  graveyard / exile piles with counts (click to browse), the Chain on the right side like MTGA's stack.
- LoR: one big round **context button** at the right edge; **mana gems** in a column (filled / spent), **Gold shown as
  LoR spell mana** (3 gem slots under the mana); **combat lane** in the middle: drag attackers into it and confirm,
  the defender drags blockers in front of attackers and confirms.
- LoR **unit cards** on the battlefield (portrait crop, big Power / Health, keyword icons, damage = red Health).
  Hover / right-click → the full card.
- **Tavern Dweller in LoR's Nexus spot on the left edge** (yours lower left, the opponent's upper left): portrait,
  life, the Power as a button beside it (lit when usable). Targetable like a Nexus.
- Stylized painterly art, **2D board with a slight tilt**, set on a **worn wooden tavern table**.
- **Placeholder card frames first** (faction colour, cost / Power / Health gems, rarity, name, text box, faction
  emblem in the art window). Art must drop in later by card id (e.g. `Resources/CardArt/<card id>.png`) without code.
- **Snappy animations with key moments** (round start, Tavern Dweller Powers, big spells, deaths, game over) and a
  speed setting.
- Layout sketch: CLIENT_DESIGN §1.1.

WHAT EXISTS (Assets/Client/Logic, `RestartedTavern.Client.Logic`, no Unity references, 10 tests; 264 tests total green)
- `MatchSession`: `MatchSetup.Duel(deck1, deck2, seat1, seat2, seed)`; `Submit(action)`, `StepBot()` (call on a
  timer when `BotToAct`), `CommitCombat(stage)`, `Undo()`, `Snapshot()`, `HandoffPending` / `AcknowledgeHandoff()`
  (hot-seat cover screen), `EventsApplied` event, `Text` (GameText for names / card descriptions / action labels).
- `TableSnapshot`: everything to draw for the viewer (players, hand, units with current stats and damage, mana, Gold,
  Gold cap, attack token / round leader, Chain, decision prompt). Hidden cards have `DefinitionId == null`.
- `ActionPicker`: `Sources` (what to glow), `Begin(card)` → `Prompt` (dimension + options, `Targets` to highlight),
  `ChooseTarget(t)` / `Choose(option)` → `Ready` (the action to `Submit`). `ChooseTargetAction(t)` for trigger targets.
- `CombatStage`: `ForAttack/ForBlock(session)`, `Candidates()`, `StageAttacker`, `StageBlocker(blocker, attacker)`,
  `BlockableBy`, `Unstage`; commit with `session.CommitCombat(stage)`.
- `TableControls`: `Main(session, stage)` = the context button (mode, label, enabled, action to submit; Attack / Block
  commit the stage, Handoff acknowledges); `Choices(session)` = the choice panel (mulligan, options, declining a target,
  "done choosing"); `Describe(session, picker, option)` = labels for picker prompts.
- `PresentationQueue`: `Enqueue(events, state, viewer)`, `TryDequeue(out beat)`, `Seconds(weight, speed)`;
  `beat.Hidden` = show a card back. **Object ids change on every zone change** (`ZoneChangedEvent.OldId → NewId`):
  re-key card views on that event. When the queue is empty, rebind everything to a fresh snapshot (animations only
  need to get close; the snapshot is the truth).
- The old IMGUI debug table (`Assets/Client/DebugTable.cs`) stays as the rules tool. Its builder
  (`Assets/Client/Editor/DebugTableBuilder.cs`) shows the pattern: build the scene **from code** in an editor script so
  it's reproducible headless, plus a Windows build method and `-autoshot file.png` / `-autoplay N` / `-seed` / `-bot1`
  flags for automated screenshots.

DONE (2026-10-10, session 8): steps 1 and 2 below. The table scene exists (`Assets/Client/Table/`, CLIENT_DESIGN
§2.2): playable vs the bot and hot-seat, clicks and drags through the picker, combat lane staging, targeting arrow,
zoom, Chain, gems, piles, game over. **Next: step 3 (ask the user about the faction colours first), then 4–6.** The
user hasn't played it by hand yet: ask for feedback on the feel first.
Build: `-executeMethod RestartedTavern.Client.Editor.TableBuilder.BuildWindows`; screenshots:
`Builds/Table/RestartedTavern.exe -screen-width 1920 -screen-height 1080 -autoplay 200 -until attack -autoshot x.png`.

BUILD PLAN (CLIENT_DESIGN §3; commit after each step that works)
1. **Table scene skeleton**: `Assets/Client/Table/` (a new asmdef referencing Rules + Client.Logic + UGUI, or inside
   RestartedTavern.Client), a `TableBuilder` editor script that creates `Assets/Scenes/Table.unity` (uGUI Canvas,
   Screen Space – Camera, 1920×1080 reference, scale with screen size) and a Windows build
   (`Builds/Table/RestartedTavern.exe`, keep the debug table build working). Panels for every zone from §1.1, bound to
   `TableSnapshot`; hand, units, Tavern Dwellers with life, mana + Gold gems, attack token, Chain list, context button,
   choice panel, prompt bar. Click a glowing card → picker → click targets → submit. Bot seat stepping on a timer.
2. **Drag and drop**: drag from hand to play (drop on the board, or onto a target for targeted spells: use
   `Prompt.Targets`), a targeting arrow (LoR / MTGA style), drag units into the combat lane (attack) or in front of an
   attacker (block), drag back out to unstage.
3. **Placeholder card frames**: hand-card and unit-card prefabs built from code or simple sprites; faction colours
   (Wizards purple / gold, Goobers red, Sensationalists black / violet, Wild green, Glitterworld cyan / neon, Neutral
   grey: propose and ask), keyword icons, rarity gem, damage in red, buffs in green.
4. **Animations** from `PresentationQueue`: write a tiny in-house tweener (no DOTween: it isn't on the Unity registry;
   ask before adding any package). Draws, plays, attacks moving into the lane, damage numbers, deaths, Gold banking
   into the spell-mana gems, round banner. Speed slider.
5. **Hot-seat and flow screens**: handoff cover ("P2, take the table"), mulligan screen (Keep / Mulligan, choose
   bottom cards), deck pick per seat + human / bot toggle + seed, game over with rematch, undo button, a log drawer
   (GameText.Describe of each event), card zoom on hover.
6. **Tavern table pass**: wooden table background, portrait frames, gem sprites (still placeholders, but on-theme).

EXTRAS (after the core works; ask the user which first, recommended first)
- Settings: animation speed, auto-pass when you have no plays, confirm End round.
- Saving playtest logs like the debug table does (`Playtests/`).
- Sound hooks (no assets yet): play, attack, damage, death, round start.
- Keyboard shortcuts: Space = context button, Esc = cancel picking, Ctrl+Z = undo.
- A "show bot thinking" toggle that highlights the bot's chosen action before it's applied.

VERIFY AS YOU GO
- EditMode tests headless (~40 s, results outside the repo):
  "C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -nographics -projectPath <repo>
  -runTests -testPlatform EditMode -testResults <scratch>/r.xml -logFile <scratch>/r.log
- Put new non-visual logic in Client.Logic with tests, not in MonoBehaviours.
- Build the exe headless (-executeMethod ...TableBuilder.BuildWindows), run it with `-autoplay N -autoshot shot.png`
  and look at the screenshot (Read tool) to check the layout at 1920×1080 and 1280×720. Send the user screenshots
  (SendUserFile) at milestones.
- Unity may be open in the editor: if a batch run fails on a locked project, ask the user to close it.

HOW TO WORK WITH THIS USER
- The user has the vision and wants Claude to propose details: ask with AskUserQuestion, multiple choice, recommended
  option first. Record decisions in CLIENT_DESIGN.md (client) and the GAME_DESIGN.md Decision Log (game-wide).
- Simulations and bot experiments may run without asking. Unit tests are expected.
- Card text says "an opponent" / "each opponent", never "your opponent". Say "Tavern Dweller", never "Patron".
- Commit when a piece of work is done; ask before pushing.
- Multi-line edits: Python scripts in the scratchpad, files opened with newline='' (LF). New files get .meta files on
  the next Unity run: commit them together. Another Claude session may work in the repo: check `git status`, don't
  `git add -A` blindly.
