# Handover: Restarted Tavern v1.0

The weekend sprint (2026-10-10): get the game ready to share on GitHub with a few friends. This page says what's in
v1.0, how to ship it, how friends play and send feedback, and how the game grows from here.

---

## 1. Where things stand

**v1.0 is playable end to end** on Windows:
- **Rules frozen** ([GAME_DESIGN.md §0](GAME_DESIGN.md)): future releases add cards, keywords, mechanics and formats.
  The fundamentals (rounds, mana and Gold, permanent damage, combat, the Chain, Tavern Dwellers) don't change.
- **216 cards + 10 Tavern Dwellers** in 5 factions, all running in the rules engine. Six prototype decks.
- **The game**: main menu, a scripted 3-round **tutorial**, Play against the **bot** or a friend (**hot-seat**), a
  **deck editor**, **settings**, an in-game encyclopedia (**Tavern Guide**, F3) and **Report bug** on every screen (F2).
- **Quality**: about 275 Unity tests, the .NET rules tests in CI, BugHunt (thousands of bot and random games with
  invariant checks), and bot simulations for balance ([playtest/](playtest/PLAYTEST.md)).

## 2. Ship it (sprint checklist)

- [x] Lock the rules and clean up the docs
- [x] Tavern Guide, settings, bug reports everywhere
- [x] Commit
- [ ] **Push** to GitHub (`main`; the repo is public: github.com/Birtkak/Restarted-Tavern)
- [ ] **Build** the exe: Unity → **Restarted Tavern → Build Windows** (or the headless command in DEVELOPMENT §7)
- [ ] **Zip** it: `python Tools/Release/make_release_zip.py v1.0` → `Builds/RestartedTavern-v1.0-win64.zip` (~43 MB;
      leaves out your own BugReports/ and Playtests/)
- [ ] **Release** it on GitHub, so friends download the zip instead of installing Unity:
      ```
      gh release create v1.0 Builds/RestartedTavern-v1.0-win64.zip --title "Restarted Tavern v1.0" --notes-file docs/RELEASE_NOTES.md
      ```
- [ ] **Smoke test the zip** on another PC (or a fresh folder): unzip, run, play the tutorial, open the Guide, save a
      bug report.
- [ ] **Send friends** the release link plus §3 below.

## 3. For friends: how to play

1. Download `RestartedTavern-v1.0-win64.zip` from the GitHub **Releases** page, unzip it anywhere, run
   `RestartedTavern.exe`. Windows SmartScreen may warn that the app is unrecognised (it isn't signed): **More info →
   Run anyway**.
2. Start with **TUTORIAL** on the main menu (three guided rounds, about 5 minutes). Then **PLAY** against the bot.
3. Stuck on a rule? **TAVERN GUIDE** (main menu, the Guide button in game, or **F3**): search any mechanic, with
   pictures and example cards.
4. Controls: drag cards from your hand to play them, drag creatures into the middle lane to attack. **Space** = the big
   button on the right, **Esc** = cancel / settings, **Ctrl+Z** = undo, **F2** = report a bug.
5. **Something odd?** Press **Report bug** (or F2), write one line about what happened, save. Each report is a folder in
   `BugReports/` next to the exe (screenshot + full game state that replays exactly). Zip the folder(s) and send them
   (Discord, email, or attach to a GitHub issue). Balance opinions are welcome too: which deck felt too strong or weak.

## 4. Known limitations (v1.0)

- Placeholder card art (faction emblems), no sound yet.
- Windows only. 1v1 only: the bot, or hot-seat on one screen. No online play.
- The bot is a careful beginner: good for learning, beatable once you know the game.
- Balance: Goober Mob is the strongest prototype deck in bot sims (about 62%); human feedback decides what changes.
- Custom decks from the deck editor are saved per PC (`%USERPROFILE%/AppData/LocalLow/.../custom_decks.json`).

## 5. Growing the game after v1.0

The rule for every release: **add, don't change** (GAME_DESIGN §0). Balance is fixed with cards.

| To add | Where | Then |
|---|---|---|
| A card | `Assets/StreamingAssets/Cards/<faction>.json` (built from existing building blocks, DEVELOPMENT §3) and its row in `docs/cards/<faction>.md` | Run the tests; BugHunt before pushing |
| A new mechanic or keyword | A building block in `Assets/Rules` (with rules tests), a GAME_DESIGN §11 entry and Decision Log line | A **Tavern Guide** page in `Assets/Client/Logic/TavernGuide.cs` and a `KeywordGlossary` line; `TavernGuideTests` fails if a glossary word has no page |
| A Guide picture | A shot in `Tools/GuideShots/guide_shots.py` (scene flags: CLIENT_DESIGN §2.2) | Build the exe, run the script, rebuild |
| A deck | `Assets/StreamingAssets/Decks/prototype_decks.json` | `-balance` in SimRunner to check it |

**Before every push**: Unity EditMode tests, `Tools/BugHunt` (exit code 0), and for balance changes a SimRunner run
([playtest/PLAYTEST.md](playtest/PLAYTEST.md) "How to run"). Commands: DEVELOPMENT §7.

**Working agreements** (from the design sessions): every design decision goes in the GAME_DESIGN Decision Log; MTG is
the default for anything the rules don't cover, with alternatives offered next to it; new cards are drafted in batches.

## 6. Docs map

| Doc | What's in it |
|---|---|
| [GAME_DESIGN.md](GAME_DESIGN.md) | The rules (v1.0, frozen) and the Decision Log |
| [CARD_DESIGN.md](CARD_DESIGN.md) | How to design cards: rarity, stat budget, faction pie |
| [cards/](cards/) | Every card, per faction, plus the Tavern Dwellers |
| [CLIENT_DESIGN.md](CLIENT_DESIGN.md) | The game client: table, menu, tutorial, deck editor, settings, Tavern Guide, command-line flags |
| [DEVELOPMENT.md](DEVELOPMENT.md) | Tech stack, architecture, card data format, build and test commands, tools |
| [LOR_PRESENTATION.md](LOR_PRESENTATION.md) | Why the table looks and animates the way it does (research) |
| [COLOR_BASELINE.md](COLOR_BASELINE.md) | The colour palette and why |
| [playtest/](playtest/PLAYTEST.md) | Playtesting, simulation reports, card power, rules review |
| [RELEASE_NOTES.md](RELEASE_NOTES.md) | What's in each release (the GitHub Release text) |
