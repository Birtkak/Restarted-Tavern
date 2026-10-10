# Release notes

## v1.0.1 (2026-10-10): first playtest fixes

Fixes from the first bug reports. No rules changes.

- **Curses on a player sit on that player's side** of the table, tagged **Your Curse** / **Their Curse** (the caster
  still controls it, as in MTG). They used to stay on the caster's side with a small "on P2" label.
- **Choosing a target**: hovering a card that isn't a legal target no longer opens its zoom, which could cover the card
  you wanted to click.
- **Recent plays** show which cards came back from a graveyard (e.g. "You: Exhumation Broadcast → Interest Broker,
  Courier Bot"); the game log says so too.
- **Button sounds**: a soft tick on hover and a tap on click (Settings → Button sounds to turn them off).
- **Grizzled Innkeeper** reads more clearly: "heal 1 from target creature you control for each Gold you banked" (same
  ability).
- **Engine**: an attacker with more power than its lone blocker needed dealt the damage as two hits; it's one hit now
  (it only matters for "is dealt damage" triggers).

## v1.0 (2026-10-10): first release for friends

A 1v1 trading card game: MTG-style rules with Legends of Runeterra rounds, **permanent damage** on creatures, and
**Gold**, unspent mana that you bank for later.

**How to play:** unzip, run `RestartedTavern.exe` (SmartScreen: More info → Run anyway), start with the **Tutorial**.

**What's in it**
- 216 cards and 10 Tavern Dwellers in 5 factions; six ready-made decks and a deck editor for your own.
- Play against the bot, or a friend on the same screen (hot-seat).
- A 3-round guided tutorial, and the **Tavern Guide** (F3): every rule and mechanic, searchable, with pictures and
  example cards.
- **Sound effects**: every kind of card has its own sound, played on its faction's instrument (brass, marimba, celesta,
  choir, harp), plus attacks, blocks, deaths, passing and each new round. Settings has the volume and a Sound board to hear them all.
- Settings (Esc): sound, animation speed, window, resolution, VSync, keyword hints, coin toss.
- **Report bug** (F2) on every screen: saves a screenshot and the full game state. Please send those folders!

**Known limitations:** placeholder art, sound effects but no music, Windows only, no online play.
