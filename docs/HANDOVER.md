# Handover: Restarted Tavern v1.0

The final sprint (weekend of 2026-10-10): get the game ready to share on GitHub with a few friends. This page is the
single place to start: what v1.0 is, how to ship it, what friends need to know, and how the game grows from here.

---

## 1. What v1.0 is

A 1v1 trading card game for Windows. MTG is the rules backbone, with Legends of Runeterra rounds, permanent damage on
creatures and Gold (banked unspent mana).

| Area | State |
|---|---|
| **Rules** | **Frozen as v1.0** ([GAME_DESIGN.md §0](GAME_DESIGN.md)). Releases add cards, keywords, mechanics and formats; the fundamentals never change. |
| **Cards** | 216 cards and 10 Tavern Dwellers in 5 factions, all in the rules engine. Six prototype decks. |
| **Play** | Against the bot, or hot-seat with a friend on one screen. |
| **Onboarding** | A scripted 3-round **tutorial**, and the **Tavern Guide** (F3): about 60 searchable pages with game screenshots, slideshows and live example cards. |
| **Sound** | Smooth, LoR-style synthesized effects (no audio files): every card type, attack, block, death, pass and round start, each faction its own instrument (brass, marimba, celesta, choir, harp). Volume, pass sound, trigger ticks and a **Sound board** in Settings. |
| **Other screens** | Main menu, deck editor, settings (Esc), phase tracker, **Report bug** on every screen (F2). |
| **Quality** | 278 Unity tests, 250 .NET rules tests (also in CI), BugHunt (about 6,000 bot and random games with invariant checks), bot balance sims. |

## 2. Ship it: the checklist

- [x] Rules locked, docs cleaned up, Tavern Guide, settings, bug reports (commit `efb90dd`)
- [x] Sound effects (v2: smooth, LoR-style), sound settings, sound board, sound checking tools
- [x] **Commit** the sound work (`edbd840`)
- [x] **Rewrite the GitHub README** (§2.1) so it shows where the game is now
- [x] **Push** `main` to GitHub (github.com/Birtkak/Restarted-Tavern, public)
- [x] **Build**: Unity → **Restarted Tavern → Build Windows** (or headless, DEVELOPMENT §7) → `Builds/Table/`
- [x] **Zip**: `python Tools/Release/make_release_zip.py v1.0` → `Builds/RestartedTavern-v1.0-win64.zip` (~43 MB; your
      own BugReports/ and Playtests/ are left out)
- [x] **Release** on GitHub, so friends don't need Unity:
      ```
      gh release create v1.0 Builds/RestartedTavern-v1.0-win64.zip --title "Restarted Tavern v1.0" --notes-file docs/RELEASE_NOTES.md
      ```
- [ ] **Smoke test the zip** in a fresh folder (or on another PC): run it, play the tutorial, open the Guide, hear a
      card being played, save a bug report.
- [ ] **Send friends** the release link and §3.

### 2.1 The GitHub README (done)

The README is the repo's front page, the first thing friends see. It's still a short player note; rewrite it to cover:

1. **Where the game is now**: v1.0 for friends, rules frozen, what's playable (216 cards, 10 Tavern Dwellers, 5
   factions, 6 decks, bot and hot-seat, tutorial, Tavern Guide, deck editor, sound), and a screenshot or two
   (`Assets/Resources/Guide/table.jpg`, `wide.jpg`, the menu).
2. **Inspirations, and what came from where**:
   - **Magic: The Gathering**: the rules backbone (anything not covered follows the MTG Comprehensive Rules): zones,
     the stack (our Chain) and priority, blocking, Trample / Flying / Lifelink / Vigilance, Equipment and Auras (our
     Curses), the Legendary rule, the London mulligan, 60-card decks with 4 copies.
   - **Legends of Runeterra**: rounds where everyone acts, alternating single actions, the attack token passing every
     round, no summoning sickness, spell mana (our Gold, capped at 3), the one context button, the combat lane, the
     card look and the presentation (beats, banners), and the sound events.
   - **Hearthstone**: mana that grows by itself every round (no lands), the tavern setting, the deck editor's
     collection layout, the hero-like face (our Tavern Dweller).
   - **MTG Arena**: the fanned hand, the board layout, tapped cards turning, tucked Equipment, the targeting arrow.
3. **What's unique**: **permanent damage** (creatures keep their wounds; healing is a resource; losing a buff can't
   kill), **Gold** (unspent mana banked for spells and abilities, spent first; Invest is paid only with Gold), **Tavern
   Dwellers** (your face, with a passive and a Power, unlocking a fixed pair of 5 factions), the five factions
   themselves (Shadow Money Wizards' shady deals, Goobers, Sensationalists, Evergrowing Wild, Glitterworld), and one
   trigger per source instead of a flood (×N).
4. **Roadmap**: friends playtest and balance from bug reports; card art and music; new cards and sets, keywords and
   Tavern Dwellers (additions only, GAME_DESIGN §0); a stronger bot; multiplayer (3–4 players) and a singleton format;
   online play.
5. **Play / build / docs**: keep the current download, build-from-source and docs links.

## 3. For friends: how to play

1. Download `RestartedTavern-v1.0-win64.zip` from the **Releases** page, unzip anywhere, run `RestartedTavern.exe`.
   Windows SmartScreen may warn about an unrecognised app (it isn't signed): **More info → Run anyway**.
2. Start with **TUTORIAL** on the main menu (about 5 minutes), then **PLAY** against the bot or a friend.
3. Rules question? **TAVERN GUIDE** (main menu, the Guide button in game, or **F3**): search any mechanic.
4. Controls: drag cards from your hand to play them; drag creatures into the middle lane to attack, onto attackers to
   block. **Space** = the big button on the right, **Esc** = cancel / Settings, **Ctrl+Z** = undo, **F2** = report a bug.
5. Too loud, or curious? **Settings** (Esc) → Sound, and the **Sound board** to hear every sound.
6. **Something odd?** Press **Report bug** (or F2), write a line about what happened, save. Each report is a folder in
   `BugReports/` next to the exe (screenshot plus the full game state, which replays exactly). Zip and send the folders
   (Discord, email, or a GitHub issue). Balance opinions are just as welcome: which deck felt too strong or weak?

## 4. Known limitations (v1.0)

- Placeholder card art (faction emblems). Sound effects but no music yet.
- Windows only. 1v1 only (bot or hot-seat); no online play.
- The bot is a careful beginner: good to learn against, beatable once you know the game.
- Balance: Goober Mob is the strongest prototype deck in bot sims (about 62%); friends' feedback decides what changes.
- Custom decks are saved per PC (`%USERPROFILE%\AppData\LocalLow\...\custom_decks.json`).

## 5. Growing the game after v1.0

The rule for every release: **add, don't change** (GAME_DESIGN §0). Balance is fixed with cards, never with rules.

| To add | Where | Then |
|---|---|---|
| A card | `Assets/StreamingAssets/Cards/<faction>.json` (built from existing building blocks, DEVELOPMENT §3) and its row in `docs/cards/<faction>.md` | Tests; BugHunt before pushing |
| A mechanic or keyword | A building block in `Assets/Rules` with rules tests; GAME_DESIGN §11 and a Decision Log line | A **Tavern Guide** page (`Client/Logic/TavernGuide.cs`) and a `KeywordGlossary` line (`TavernGuideTests` fails otherwise) |
| A Guide picture | A shot in `Tools/GuideShots/guide_shots.py` (scene flags: CLIENT_DESIGN §2.2) | Build, run the script, rebuild |
| A sound | A `SfxKind` and its recipe in `Client/Logic/SfxSynth.cs`, hooked in `TableView.Audio.cs` `SoundFor` | Export Sound Preview, then `python Tools/SoundCheck/sound_check.py`: no flags, about -17 dB like the others |
| A deck | `Assets/StreamingAssets/Decks/prototype_decks.json` | SimRunner `-balance` |

**Before every push**: Unity EditMode tests, `Tools/BugHunt` (exit code 0), and for balance changes a SimRunner run
(DEVELOPMENT §7, [playtest/PLAYTEST.md](playtest/PLAYTEST.md)).

**Working agreements** from the design sessions: every design decision goes in the GAME_DESIGN Decision Log; MTG is the
default for anything the rules don't cover, with alternatives offered next to it; new cards are drafted in batches.

## 6. Tools

| Tool | What it does |
|---|---|
| `Tools/BugHunt` | Thousands of games with engine and client invariant checks and replay checks; run before every push |
| `Tools/SimRunner` | Bot-vs-bot simulation report, balance matrix, card power scan, deck tuning |
| `Tools/RulesTests` | The rules tests on plain .NET (what GitHub Actions runs) |
| `Tools/GuideShots` | Remakes the Tavern Guide's screenshots from the built game |
| `Tools/SoundCheck` | Spectrogram sheet and measurements of every sound, so sounds can be reviewed without listening |
| `Tools/Release` | Packages the built game as the release zip |

## 7. Docs map

| Doc | What's in it |
|---|---|
| [GAME_DESIGN.md](GAME_DESIGN.md) | The rules (v1.0, frozen) and the Decision Log |
| [CARD_DESIGN.md](CARD_DESIGN.md) | How to design cards: rarity, stat budget, faction pie |
| [cards/](cards/) | Every card per faction, and the Tavern Dwellers |
| [CLIENT_DESIGN.md](CLIENT_DESIGN.md) | The game client: table, menu, tutorial, deck editor, settings, Tavern Guide, sound, command-line flags |
| [DEVELOPMENT.md](DEVELOPMENT.md) | Tech stack, architecture, card data format, build and test commands, tools |
| [LOR_PRESENTATION.md](LOR_PRESENTATION.md) | Why the table looks and animates the way it does (research) |
| [COLOR_BASELINE.md](COLOR_BASELINE.md) | The colour palette and why |
| [playtest/](playtest/PLAYTEST.md) | Playtesting, simulation reports, card power, rules review |
| [RELEASE_NOTES.md](RELEASE_NOTES.md) | What's in each release (the GitHub Release text) |
