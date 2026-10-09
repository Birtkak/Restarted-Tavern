# Restarted Tavern — Development Guide

How the game will be built. Rules live in [GAME_DESIGN.md](GAME_DESIGN.md); this document covers architecture and how we work.

**Status:** pre-production. The engine and tech stack are ❓ not chosen yet. The design ruleset comes first.

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

## 2. Core Model (draft)

```
FormatConfig   { deckSize, copyLimit, minPlayers, maxPlayers, startingLife,
                 startingHand, maxHandSize?, manaCap, goldCap, ... }

GameState      { players[], activePlayer, priorityPlayer, passesInRow,
                 turnNumber, phase, chain[], rngState, nextObjectId }

PlayerState    { id, patronId, life, maxMana, mana, gold, attachedCurses[],
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
1. **Ruleset v0.1**: finish the ❓ items in GAME_DESIGN.md for 2-player Standard.
2. **Rules engine prototype**: headless and text/CLI-playable, with about 20 test cards.
3. **Paper / CLI playtest**: tune the Gold cap, the curve and the impact of permanent damage.
4. **Choose the client engine**, then build a minimal visual client.
5. A first set of about 100 cards and a basic AI.
6. Later: multiplayer (3–4 players), singleton format, online play.

---

## 6. Open Technical Questions ❓
- Client engine: Unity (C#) / Godot / web (TypeScript)? This decides the language of the rules engine too, unless the rules engine runs on a server.
- Online multiplayer: authoritative server needed? (Recommended if online play is a goal.)
- Card art pipeline and card frame rendering.
