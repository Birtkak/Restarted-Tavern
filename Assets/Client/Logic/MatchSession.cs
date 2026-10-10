using System;
using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Rules;
using RestartedTavern.Rules.AI;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Client.Logic
{
    public enum SeatKind
    {
        Human,
        Bot,
    }

    /// <summary>How a match is set up: decks, who sits where, rules and seed.</summary>
    public sealed class MatchSetup
    {
        public List<CardPool.DeckList> Decks = new List<CardPool.DeckList>();
        public List<SeatKind> Seats = new List<SeatKind>();
        public FormatConfig Format = FormatConfig.Standard();
        public ulong Seed = 1;
        /// <summary>The decks are dealt in list order, top card first (no opening shuffle): the scripted tutorial.</summary>
        public bool StackedDecks;
        /// <summary>This seat goes first, or -1 for a random one.</summary>
        public int FirstSeat = -1;

        /// <summary>1v1 with two prototype decks (indexes into CardPool.PrototypeDecks()).</summary>
        public static MatchSetup Duel(int deck1, int deck2, SeatKind seat1 = SeatKind.Human, SeatKind seat2 = SeatKind.Bot, ulong seed = 1)
        {
            var decks = CardPool.PrototypeDecks();
            return new MatchSetup
            {
                Decks = { decks[deck1], decks[deck2] },
                Seats = { seat1, seat2 },
                Seed = seed,
            };
        }

        /// <summary>
        /// 1v1 with two prototype decks, each played with a chosen Tavern Dweller (null = the deck's own). The Tavern
        /// Dweller must be one of <see cref="TavernDwellersFor"/>.
        /// </summary>
        public static MatchSetup Duel(int deck1, string tavernDweller1, int deck2, string tavernDweller2,
            SeatKind seat1, SeatKind seat2, ulong seed)
        {
            var decks = CardPool.PrototypeDecks();
            return new MatchSetup
            {
                Decks = { WithTavernDweller(decks[deck1], tavernDweller1), WithTavernDweller(decks[deck2], tavernDweller2) },
                Seats = { seat1, seat2 },
                Seed = seed,
            };
        }

        private static CardPool.DeckList WithTavernDweller(CardPool.DeckList deck, string tavernDweller) =>
            tavernDweller == null || tavernDweller == deck.TavernDweller ? deck : new CardPool.DeckList
            {
                Id = deck.Id, Name = deck.Name, TavernDweller = tavernDweller, Description = deck.Description,
                Cards = new List<string>(deck.Cards),
            };

        /// <summary>
        /// The Tavern Dwellers that can lead this deck: their factions cover every non-neutral card in it
        /// (the deck rule in GameEngine.CreateGame). The deck's own Tavern Dweller comes first.
        /// </summary>
        public static List<string> TavernDwellersFor(CardPool.DeckList deck, CardDatabase db)
        {
            var factions = deck.Cards.Select(id => db.Get(id).Faction).Where(f => f != "neutral").Distinct().ToList();
            return CardPool.All()
                .Where(d => d.IsTavernDweller && factions.All(f => d.TavernDwellerFactions.Contains(f)))
                .Select(d => d.Id)
                .OrderBy(id => id == deck.TavernDweller ? 0 : 1)
                .ToList();
        }
    }

    /// <summary>
    /// One game being played at the table: the engine, the state, who is human, undo, and whose eyes the
    /// screen belongs to. The visual client calls <see cref="Submit"/> for human actions and
    /// <see cref="StepBot"/> on a timer; both return the events to animate.
    ///
    /// Hot-seat: the screen shows the human the game waits on. When that changes from one human to another,
    /// <see cref="HandoffPending"/> is set so the client can cover the table ("pass the device to P2") until
    /// <see cref="AcknowledgeHandoff"/>.
    /// </summary>
    public sealed class MatchSession
    {
        private const int MaxUndo = 200;

        private readonly List<GameState> _undo = new List<GameState>();
        /// <summary>For each undo state: how long the history was then.</summary>
        private readonly List<int> _undoHistory = new List<int>();
        private readonly List<PlayerAction> _history = new List<PlayerAction>();
        private readonly GreedyBot _bot;

        public GameEngine Engine { get; }
        public GameState State { get; private set; }
        public MatchSetup Setup { get; }
        public GameText Text { get; }

        /// <summary>The player whose hand and choices the screen shows.</summary>
        public PlayerId Viewer { get; private set; }
        public bool HandoffPending { get; private set; }

        /// <summary>
        /// Every action sent to the engine since the start, in order (undone ones removed). Replaying them on a new game
        /// with the same setup and seed gives the same state (bug reports).
        /// </summary>
        public IReadOnlyList<PlayerAction> History => _history;

        /// <summary>Raised with every batch of events (game start, each action), in order.</summary>
        public event Action<IReadOnlyList<GameEvent>> EventsApplied;

        public MatchSession(MatchSetup setup)
        {
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            if (setup.Decks.Count != setup.Seats.Count) throw new ArgumentException("One deck per seat.");
            var db = CardPool.CreateDatabase();
            Engine = new GameEngine(db);
            Text = new GameText(db);
            _bot = new GreedyBot(Engine);
            StartEvents = new List<GameEvent>();
            State = Engine.CreateGame(setup.Format,
                setup.Decks.Select((d, i) => new PlayerSetup
                {
                    Deck = new List<string>(d.Cards), TavernDwellerId = d.TavernDweller,
                    KeepDeckOrder = setup.StackedDecks, GoesFirst = i == setup.FirstSeat,
                }).ToList(),
                setup.Seed, StartEvents);
            Text.Remember(State, StartEvents);
            Viewer = State.Players[Math.Max(0, setup.Seats.IndexOf(SeatKind.Human))].Id;
            UpdateViewer();
            HandoffPending = false; // nobody to hand over to at the start
        }

        /// <summary>The events from setting up the game (shuffles, opening hands, mulligan decision).</summary>
        public List<GameEvent> StartEvents { get; }

        public SeatKind SeatOf(PlayerId p) => Setup.Seats[State.GetPlayer(p).Seat];

        public PlayerId? WaitingOn => Engine.WaitingOn(State);

        /// <summary>The game waits on a bot: call <see cref="StepBot"/> (after a short delay, so humans can follow).</summary>
        public bool BotToAct => WaitingOn is PlayerId p && SeatOf(p) == SeatKind.Bot;

        /// <summary>The game waits on the human at the screen and the table isn't covered for a handoff.</summary>
        public bool HumanToAct => WaitingOn is PlayerId p && p == Viewer && SeatOf(p) == SeatKind.Human && !HandoffPending;

        /// <summary>
        /// Narrows what the human may do (the scripted tutorial: "play this card now"). Null = everything legal.
        /// Only the table's choices go through it; <see cref="CommitCombat"/> still sends its own passes.
        /// </summary>
        public Func<PlayerAction, bool> HumanFilter { get; set; }

        /// <summary>Picks the bot's action instead of the bot (the scripted tutorial); null or a null result = the bot decides.</summary>
        public Func<MatchSession, PlayerId, PlayerAction> BotOverride { get; set; }

        /// <summary>The legal actions of the viewer, or none when it isn't their call.</summary>
        public List<PlayerAction> LegalForViewer()
        {
            if (!HumanToAct) return new List<PlayerAction>();
            var legal = Engine.GetLegalActions(State, Viewer);
            return HumanFilter == null ? legal : legal.Where(HumanFilter).ToList();
        }

        /// <summary>A fresh picker for the viewer's current legal actions.</summary>
        public ActionPicker NewPicker() => new ActionPicker(LegalForViewer());

        /// <summary>What the viewer may see right now.</summary>
        public TableSnapshot Snapshot() => TableSnapshot.Build(Engine, State, Viewer);

        public List<GameEvent> Submit(PlayerAction action)
        {
            if (!HumanToAct || action.Player != Viewer) throw new InvalidOperationException("Not " + action.Player + "'s call.");
            return Apply(action, keepUndo: true);
        }

        public List<GameEvent> StepBot()
        {
            if (!BotToAct) throw new InvalidOperationException("No bot to act.");
            var p = WaitingOn.Value;
            var scripted = BotOverride?.Invoke(this, p);
            if (scripted != null && !Engine.GetLegalActions(State, p).Contains(scripted)) scripted = null;
            return Apply(scripted ?? _bot.Choose(State, p), keepUndo: false);
        }

        /// <summary>
        /// The bot plays one action for whoever the game waits on, human seats included (automated playtests and
        /// screenshots). Not undoable.
        /// </summary>
        public List<GameEvent> AutoStep()
        {
            if (!(WaitingOn is PlayerId p)) throw new InvalidOperationException("The game is over.");
            return Apply(_bot.Choose(State, p), keepUndo: false);
        }

        public bool CanUndo => _undo.Count > 0;

        /// <summary>Back to the state before the last human action (bot actions after it are undone too).</summary>
        public void Undo()
        {
            if (_undo.Count == 0) return;
            State = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            int keep = _undoHistory[_undoHistory.Count - 1];
            _undoHistory.RemoveAt(_undoHistory.Count - 1);
            _history.RemoveRange(keep, _history.Count - keep);
            _combat = null;
            UpdateViewer();
            HandoffPending = false;
        }

        public void AcknowledgeHandoff() => HandoffPending = false;

        /// <summary>
        /// Send a staged LoR-style attack or block to the engine (<see cref="CombatStage"/>). The parts that wait on
        /// other players (the priority window after the attack action) run on as soon as the game comes back to
        /// the stager, from <see cref="Submit"/> or <see cref="StepBot"/>.
        /// </summary>
        public List<GameEvent> CommitCombat(CombatStage stage)
        {
            if (!HumanToAct || stage.Player != Viewer) throw new InvalidOperationException("Not " + stage.Player + "'s call.");
            KeepUndo();
            _combat = stage;
            _combatStarted = State.Pending?.Kind == DecisionKind.DeclareAttackers || stage.IsBlocking;
            var events = ContinueCombat();
            UpdateViewer();
            EventsApplied?.Invoke(events);
            return events;
        }

        /// <summary>A committed attack is still waiting to be declared (on an opponent's response).</summary>
        public bool CombatInProgress => _combat != null && !State.IsGameOver;

        private CombatStage _combat;
        private bool _combatStarted;

        private List<GameEvent> ContinueCombat()
        {
            var events = new List<GameEvent>();
            while (_combat != null && !State.IsGameOver)
            {
                // The attack window closed without a declaration (a response left nothing able to attack).
                if (_combatStarted && !_combat.IsBlocking && State.Step != Step.BeginCombat
                    && State.Pending?.Kind != DecisionKind.DeclareAttackers)
                {
                    _combat = null;
                    break;
                }
                if (WaitingOn != _combat.Player) break;
                var p = _combat.Player;
                var declaring = _combat.IsBlocking ? DecisionKind.DeclareBlockers : DecisionKind.DeclareAttackers;
                if (State.Pending?.Kind == declaring)
                {
                    foreach (var c in _combat.Staged)
                    {
                        var action = _combat.ToAction(c);
                        if (Engine.GetLegalActions(State, p).Contains(action)) events.AddRange(ApplyToEngine(action));
                    }
                    events.AddRange(ApplyToEngine(_combat.IsBlocking ? PlayerAction.FinishBlocks(p) : PlayerAction.FinishAttacks(p)));
                    _combat = null;
                    break;
                }
                var legal = Engine.GetLegalActions(State, p);
                if (!_combatStarted && legal.Contains(PlayerAction.GoToCombat(p)))
                {
                    _combatStarted = true;
                    events.AddRange(ApplyToEngine(PlayerAction.GoToCombat(p)));
                    continue;
                }
                // Something on the Chain (an opponent responded): the player answers it by hand, then this runs on.
                if (!_combatStarted || State.Chain.Count > 0 || !legal.Contains(PlayerAction.Pass(p)))
                {
                    if (!_combatStarted) _combat = null; // the attack is no longer possible
                    break;
                }
                events.AddRange(ApplyToEngine(PlayerAction.Pass(p)));
            }
            return events;
        }

        private void KeepUndo()
        {
            _undo.Add(State.Clone());
            _undoHistory.Add(_history.Count);
            if (_undo.Count > MaxUndo)
            {
                _undo.RemoveAt(0);
                _undoHistory.RemoveAt(0);
            }
        }

        private List<GameEvent> ApplyToEngine(PlayerAction action)
        {
            _history.Add(action);
            var events = Engine.Apply(State, action);
            Text.Remember(State, events);
            return events;
        }

        private List<GameEvent> Apply(PlayerAction action, bool keepUndo)
        {
            if (keepUndo) KeepUndo();
            var events = ApplyToEngine(action);
            events.AddRange(ContinueCombat());
            UpdateViewer();
            EventsApplied?.Invoke(events);
            return events;
        }

        /// <summary>The screen follows the human the game waits on; bots never take the screen.</summary>
        private void UpdateViewer()
        {
            if (!(WaitingOn is PlayerId p) || SeatOf(p) != SeatKind.Human || p == Viewer) return;
            Viewer = p;
            HandoffPending = true;
        }
    }
}
