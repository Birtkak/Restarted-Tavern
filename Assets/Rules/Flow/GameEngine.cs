using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    public sealed class PlayerSetup
    {
        public string PatronId { get; set; }
        /// <summary>Card definition ids, in any order (the deck is shuffled at game start).</summary>
        public List<string> Deck { get; set; } = new List<string>();
        /// <summary>Null = own team (free-for-all).</summary>
        public int? TeamId { get; set; }
    }

    public sealed class IllegalActionException : Exception
    {
        public IllegalActionException(string message) : base(message) { }
    }

    /// <summary>
    /// The public face of the rules engine (DEVELOPMENT §1.3):
    /// <code>
    /// CreateGame(format, players, seed) -> GameState
    /// GetLegalActions(state, player)    -> [PlayerAction]
    /// Apply(state, action)              -> [GameEvent]   (mutates state)
    /// </code>
    /// Apply changes the given state in place; call <see cref="GameState.Clone"/> first to keep
    /// the old one (AI search, undo, replays).
    /// </summary>
    public sealed class GameEngine
    {
        public CardDatabase Cards { get; }

        public GameEngine(CardDatabase cards)
        {
            Cards = cards ?? throw new ArgumentNullException(nameof(cards));
        }

        public GameState CreateGame(FormatConfig format, IReadOnlyList<PlayerSetup> players, ulong seed,
            List<GameEvent> events = null)
        {
            if (players.Count < format.MinPlayers || players.Count > format.MaxPlayers)
                throw new ArgumentException(format.Name + " needs " + format.MinPlayers + "–" + format.MaxPlayers + " players.");
            if (format.EnforceDeckRules)
                foreach (var p in players) DeckValidator.Validate(Cards, format, p.Deck);

            var state = new GameState { Format = format.Clone(), Rng = new DeterministicRng(seed) };
            var runner = new GameRunner(Cards, state, events ?? new List<GameEvent>());
            runner.SetUpGame(players);
            runner.RunAutomaticActions();
            return state;
        }

        public List<GameEvent> Apply(GameState state, PlayerAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (!GetLegalActions(state, action.Player).Contains(action))
                throw new IllegalActionException("Illegal action: " + action);

            var events = new List<GameEvent>();
            var runner = new GameRunner(Cards, state, events);
            runner.Perform(action);
            runner.RunAutomaticActions();
            return events;
        }

        public List<PlayerAction> GetLegalActions(GameState state, PlayerId player) =>
            new GameRunner(Cards, state, null).LegalActions(player);

        /// <summary>The player the game is waiting on, or null when the game is over.</summary>
        public PlayerId? WaitingOn(GameState state)
        {
            if (state.IsGameOver) return null;
            if (state.Pending != null) return state.Pending.Player;
            return state.PriorityPlayer;
        }

        public Characteristics GetCharacteristics(GameState state, CardInstance card) =>
            CharacteristicsCalculator.Compute(state, Cards, card);
    }

    /// <summary>GAME_DESIGN §2: deck size and copy limit. Tokens can't be in decks.</summary>
    public static class DeckValidator
    {
        public static void Validate(CardDatabase db, FormatConfig format, List<string> deck)
        {
            if (deck.Count != format.DeckSize)
                throw new ArgumentException("Deck must have exactly " + format.DeckSize + " cards, has " + deck.Count + ".");
            var counts = new Dictionary<string, int>();
            foreach (var id in deck)
            {
                if (db.Get(id).IsToken) throw new ArgumentException("Tokens can't be put in a deck: " + id);
                counts.TryGetValue(id, out int n);
                counts[id] = n + 1;
                if (n + 1 > format.CopyLimit)
                    throw new ArgumentException("Too many copies of " + id + " (max " + format.CopyLimit + ").");
            }
        }
    }
}
