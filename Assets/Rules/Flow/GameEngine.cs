using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    public sealed class PlayerSetup
    {
        public string TavernDwellerId { get; set; }
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
                foreach (var p in players) DeckValidator.Validate(Cards, format, p.Deck, p.TavernDwellerId);
            foreach (var p in players)
                if (p.TavernDwellerId != null && !Cards.Get(p.TavernDwellerId).IsTavernDweller)
                    throw new ArgumentException(p.TavernDwellerId + " is not a Tavern Dweller.");

            var state = new GameState { Format = format.Clone(), Rng = new DeterministicRng(seed) };
            var runner = new GameRunner(Cards, state, events ?? new List<GameEvent>());
            runner.SetUpGame(players);
            runner.RunAutomaticActions();
            return state;
        }

        /// <summary>
        /// Opt-in speed-up for simulations: remember the last legal-action list so a bot's
        /// GetLegalActions and the validation in Apply don't both enumerate it. Only safe when the
        /// state is changed through Apply alone (tests and tools that edit the state directly
        /// should leave it off). Not thread-safe: use one engine per thread.
        /// </summary>
        public bool CacheLegalActions { get; set; }

        private GameState _cachedState;
        private long _cachedVersion;
        private PlayerId _cachedPlayer;
        private List<PlayerAction> _cachedActions;

        public List<GameEvent> Apply(GameState state, PlayerAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (!GetLegalActions(state, action.Player).Contains(action))
                throw new IllegalActionException("Illegal action: " + action);

            state.Version++;
            var events = new List<GameEvent>();
            var runner = new GameRunner(Cards, state, events);
            runner.Perform(action);
            runner.RunAutomaticActions();
            return events;
        }

        public List<PlayerAction> GetLegalActions(GameState state, PlayerId player)
        {
            if (!CacheLegalActions) return new GameRunner(Cards, state, null).LegalActions(player);
            if (ReferenceEquals(state, _cachedState) && state.Version == _cachedVersion && player == _cachedPlayer)
                return _cachedActions;
            var actions = new GameRunner(Cards, state, null).LegalActions(player);
            _cachedState = state;
            _cachedVersion = state.Version;
            _cachedPlayer = player;
            _cachedActions = actions;
            return actions;
        }

        /// <summary>The activated abilities of an object (printed, then granted), as indexed by PlayerAction.AbilityIndex.</summary>
        public List<ActivatedAbility> GetAbilities(GameState state, CardInstance obj) =>
            new GameRunner(Cards, state, null).AbilitiesOf(obj);

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

    /// <summary>
    /// GAME_DESIGN §2: deck size and copy limit. Tokens and Tavern Dwellers can't be in decks.
    /// §9: every deck has a Tavern Dweller, and every card is from one of its two factions or Neutral.
    /// </summary>
    public static class DeckValidator
    {
        public static void Validate(CardDatabase db, FormatConfig format, List<string> deck, string tavernDwellerId = null)
        {
            if (tavernDwellerId == null)
                throw new ArgumentException("Every deck needs a Tavern Dweller (GAME_DESIGN §9.1).");
            CardDefinition tavernDweller = null;
            if (tavernDwellerId != null)
            {
                tavernDweller = db.Get(tavernDwellerId);
                if (!tavernDweller.IsTavernDweller) throw new ArgumentException(tavernDwellerId + " is not a Tavern Dweller.");
            }
            if (deck.Count != format.DeckSize)
                throw new ArgumentException("Deck must have exactly " + format.DeckSize + " cards, has " + deck.Count + ".");
            var counts = new Dictionary<string, int>();
            foreach (var id in deck)
            {
                var def = db.Get(id);
                if (def.IsToken) throw new ArgumentException("Tokens can't be put in a deck: " + id);
                if (def.IsTavernDweller) throw new ArgumentException("Tavern Dwellers aren't part of the deck: " + id);
                if (tavernDweller != null && def.Faction != "neutral" && Array.IndexOf(tavernDweller.TavernDwellerFactions, def.Faction) < 0)
                    throw new ArgumentException(def.Name + " (" + def.Faction + ") is outside " + tavernDweller.Name + "'s factions.");
                counts.TryGetValue(id, out int n);
                counts[id] = n + 1;
                if (n + 1 > format.CopyLimit)
                    throw new ArgumentException("Too many copies of " + id + " (max " + format.CopyLimit + ").");
            }
        }
    }
}
