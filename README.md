# Restarted Tavern

A 1v1 trading card game. MTG is the rules backbone, with **Legends of Runeterra rounds** (players alternate single
actions, the attack token passes every round), **permanent damage** (creatures keep their wounds) and **Gold**
(unspent mana is banked to pay for spells and abilities later). Five factions, each deck led by a **Tavern Dweller**.

**Version 1.0: the rules are frozen.** Future releases add cards and mechanics, not rule changes.

## Play
- **Download** the latest zip from [Releases](https://github.com/Birtkak/Restarted-Tavern/releases), unzip, run
  `RestartedTavern.exe` (Windows). SmartScreen may warn about an unsigned app: More info → Run anyway.
- Start with **Tutorial**, then **Play** against the bot or a friend. Rules questions: the in-game **Tavern Guide** (F3).
- Found a bug? **Report bug** (F2) saves a folder in `BugReports/` next to the game; send it over.

## Build from source
Unity **6000.6.4f1**. Open the project, then **Restarted Tavern → Build Windows** (writes `Builds/Table/`), or open
`Assets/Scenes/Table.unity` and press Play. Tests, tools and commands: [Development](docs/DEVELOPMENT.md).

## Docs
- [Handover](docs/HANDOVER.md): the state of v1.0, how to ship it, how to grow the game
- [Game Design](docs/GAME_DESIGN.md): the rules (v1.0, frozen) and the Decision Log
- [Card Design Guide](docs/CARD_DESIGN.md) · card lists: [Shadow Money Wizards](docs/cards/shadow_money_wizards.md) · [Goobers](docs/cards/goobers.md) · [Sensationalists](docs/cards/sensationalists.md) · [Evergrowing Wild](docs/cards/evergrowing_wild.md) · [Glitterworld](docs/cards/glitterworld.md) · [Neutral](docs/cards/neutral.md) · [Tavern Dwellers](docs/cards/tavern_dwellers.md)
- [Client Design](docs/CLIENT_DESIGN.md) · [Development](docs/DEVELOPMENT.md) · [Playtesting](docs/playtest/PLAYTEST.md) · [Release notes](docs/RELEASE_NOTES.md)
