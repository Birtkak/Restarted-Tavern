using System.Collections.Generic;
using System.Text;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// The complete state of one game. Holds only plain data (card definitions are looked up
    /// by id in a <see cref="CardDatabase"/>), so <see cref="Clone"/> is cheap and complete.
    /// </summary>
    public sealed class GameState
    {
        public FormatConfig Format { get; set; }
        /// <summary>In seat (= turn) order.</summary>
        public List<PlayerState> Players { get; set; } = new List<PlayerState>();

        public int ActiveIndex { get; set; }
        public int StartingPlayerIndex { get; set; }
        /// <summary>Seat of the player who starts the current round (FormatConfig.RotateRoundLeader moves it each round).</summary>
        public int RoundLeaderSeat { get; set; }
        /// <summary>Turns already finished in the current round.</summary>
        public int TurnsThisRound { get; set; }
        public int TurnNumber { get; set; }
        public Step Step { get; set; } = Step.Mulligan;

        /// <summary>Who may act on the Chain right now, if anyone (GAME_DESIGN §8).</summary>
        public PlayerId? PriorityPlayer { get; set; }
        /// <summary>Consecutive passes. When it reaches the number of living players, the top of the Chain resolves (or the step ends).</summary>
        public int PassesInRow { get; set; }
        /// <summary>Who gets priority once pending trigger targets are chosen.</summary>
        public PlayerId? ResumePriorityTo { get; set; }

        /// <summary>The Chain. The last item is the top.</summary>
        public List<ChainItem> Chain { get; set; } = new List<ChainItem>();
        public List<PendingTrigger> PendingTriggers { get; set; } = new List<PendingTrigger>();
        /// <summary>Delayed triggers waiting for their moment (MTG 603.7).</summary>
        public List<DelayedTrigger> DelayedTriggers { get; set; } = new List<DelayedTrigger>();
        public PendingDecision Pending { get; set; }
        /// <summary>
        /// Choices that wait their turn: when one resolving effect asks several players (each opponent
        /// sacrifices a creature), they choose one after another in turn order.
        /// </summary>
        public List<PendingDecision> ChoiceQueue { get; set; } = new List<PendingDecision>();

        public CombatState Combat { get; set; }
        public List<TemporaryModifier> UntilEndOfTurn { get; set; } = new List<TemporaryModifier>();
        /// <summary>"Gain control of target creature until end of turn": who gets it back in the cleanup step.</summary>
        public List<TemporaryControl> ControlUntilEndOfTurn { get; set; } = new List<TemporaryControl>();
        /// <summary>
        /// How often something was used this turn, for "once each turn" abilities, Tavern Dweller Powers and
        /// "triggers at most N times each turn". Keys are made by GameRunner; cleared when a turn starts.
        /// </summary>
        public Dictionary<string, int> UsesThisTurn { get; set; } = new Dictionary<string, int>();
        /// <summary>Bumped by every GameEngine.Apply, so cached legal actions know when they're stale.</summary>
        public long Version { get; set; }

        public DeterministicRng Rng { get; set; }
        public int NextObjectId { get; set; } = 1;
        public int NextChainId { get; set; } = 1;
        public long NextTimestamp { get; set; } = 1;

        /// <summary>
        /// When true, the engine passes priority for a player whose only legal action is to pass
        /// (GAME_DESIGN §8 UX note). Tests and tools can turn it off to step manually.
        /// </summary>
        public bool AutoPass { get; set; } = true;

        public bool IsGameOver { get; set; }
        /// <summary>The winning team's players, once the game is over. Empty on a draw.</summary>
        public List<PlayerId> Winners { get; set; } = new List<PlayerId>();

        public PlayerState ActivePlayerState => Players[ActiveIndex];
        public PlayerId ActivePlayer => Players[ActiveIndex].Id;

        public PlayerState GetPlayer(PlayerId id)
        {
            foreach (var p in Players)
                if (p.Id == id) return p;
            throw new KeyNotFoundException("No player " + id);
        }

        /// <summary>Living players in turn order, starting from <paramref name="from"/>.</summary>
        public List<PlayerState> LivingPlayersFrom(PlayerId from)
        {
            var result = new List<PlayerState>();
            int start = GetPlayer(from).Seat;
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[(start + i) % Players.Count];
                if (!p.HasLost) result.Add(p);
            }
            return result;
        }

        public int LivingPlayerCount
        {
            get
            {
                int n = 0;
                foreach (var p in Players) if (!p.HasLost) n++;
                return n;
            }
        }

        public bool AreOpponents(PlayerId a, PlayerId b) => GetPlayer(a).TeamId != GetPlayer(b).TeamId;

        /// <summary>The spell or ability on the Chain with this object id, or null.</summary>
        public ChainItem FindOnChain(ObjectId id)
        {
            if (id.IsNone) return null;
            foreach (var item in Chain)
                if (item.ObjectId == id) return item;
            return null;
        }

        /// <summary>Find a game object in any zone, including spells on the Chain. Null if it no longer exists.</summary>
        public CardInstance FindObject(ObjectId id)
        {
            if (id.IsNone) return null;
            foreach (var p in Players)
            {
                foreach (var c in p.Battlefield) if (c.Id == id) return c;
                foreach (var c in p.Hand) if (c.Id == id) return c;
                foreach (var c in p.Graveyard) if (c.Id == id) return c;
                foreach (var c in p.Exile) if (c.Id == id) return c;
                foreach (var c in p.Deck) if (c.Id == id) return c;
                foreach (var c in p.TavernDwellerZone) if (c.Id == id) return c;
            }
            foreach (var item in Chain)
                if (item.Card != null && item.Card.Id == id) return item.Card;
            return null;
        }

        public CardInstance FindOnBattlefield(ObjectId id)
        {
            if (id.IsNone) return null;
            foreach (var p in Players)
                foreach (var c in p.Battlefield)
                    if (c.Id == id) return c;
            return null;
        }

        /// <summary>Every permanent on the battlefield, in seat order then entry order.</summary>
        public IEnumerable<CardInstance> AllPermanents()
        {
            foreach (var p in Players)
                foreach (var c in p.Battlefield)
                    yield return c;
        }

        public GameState Clone()
        {
            var s = (GameState)MemberwiseClone();
            s.Format = Format.Clone();
            s.Players = new List<PlayerState>(Players.Count);
            foreach (var p in Players) s.Players.Add(p.Clone());
            s.Chain = new List<ChainItem>(Chain.Count);
            foreach (var c in Chain) s.Chain.Add(c.Clone());
            s.PendingTriggers = new List<PendingTrigger>(PendingTriggers.Count);
            foreach (var t in PendingTriggers) s.PendingTriggers.Add(t.Clone());
            s.DelayedTriggers = new List<DelayedTrigger>(DelayedTriggers.Count);
            foreach (var t in DelayedTriggers) s.DelayedTriggers.Add(t.Clone());
            s.Pending = Pending?.Clone();
            s.ChoiceQueue = new List<PendingDecision>(ChoiceQueue.Count);
            foreach (var d in ChoiceQueue) s.ChoiceQueue.Add(d.Clone());
            s.Combat = Combat?.Clone();
            s.UntilEndOfTurn = new List<TemporaryModifier>(UntilEndOfTurn.Count);
            foreach (var m in UntilEndOfTurn) s.UntilEndOfTurn.Add(m.Clone());
            s.ControlUntilEndOfTurn = new List<TemporaryControl>(ControlUntilEndOfTurn.Count);
            foreach (var c in ControlUntilEndOfTurn) s.ControlUntilEndOfTurn.Add(c.Clone());
            s.UsesThisTurn = new Dictionary<string, int>(UsesThisTurn);
            s.Rng = Rng?.Clone();
            s.Winners = new List<PlayerId>(Winners);
            return s;
        }

        /// <summary>
        /// What <paramref name="viewer"/> is allowed to see (DEVELOPMENT §1.7): other players'
        /// hands and every deck are hidden, and the RNG state is removed so the future can't be predicted.
        /// </summary>
        public GameState CreateViewFor(PlayerId viewer)
        {
            var view = Clone();
            view.Rng = null;
            foreach (var p in view.Players)
            {
                foreach (var c in p.Deck) c.DefinitionId = null;
                if (p.Id != viewer)
                    foreach (var c in p.Hand) c.DefinitionId = null;
            }
            return view;
        }

        /// <summary>
        /// A canonical text dump of the state. Two games with the same seed and actions must
        /// produce the same fingerprint (determinism tests, replays, desync checks).
        /// </summary>
        public string Fingerprint()
        {
            var sb = new StringBuilder();
            sb.Append("T").Append(TurnNumber).Append(' ').Append(Step)
              .Append(" A").Append(ActiveIndex)
              .Append(" Pr").Append(PriorityPlayer?.ToString() ?? "-")
              .Append(" Over").Append(IsGameOver)
              .Append(" Rng").Append(Rng?.State ?? 0).Append('\n');
            foreach (var p in Players)
            {
                sb.Append(p.Id).Append(" life=").Append(p.Life).Append(" mana=").Append(p.Mana)
                  .Append('/').Append(p.MaxMana).Append(" gold=").Append(p.Gold)
                  .Append(" lost=").Append(p.HasLost).Append('\n');
                AppendZone(sb, "deck", p.Deck);
                AppendZone(sb, "hand", p.Hand);
                AppendZone(sb, "bf", p.Battlefield);
                AppendZone(sb, "gy", p.Graveyard);
                AppendZone(sb, "ex", p.Exile);
                AppendZone(sb, "dweller", p.TavernDwellerZone);
            }
            foreach (var item in Chain) sb.Append("chain ").Append(item).Append('\n');
            return sb.ToString();
        }

        private static void AppendZone(StringBuilder sb, string name, List<CardInstance> zone)
        {
            sb.Append("  ").Append(name).Append(':');
            foreach (var c in zone)
            {
                sb.Append(' ').Append(c.DefinitionId).Append(c.Id);
                if (c.Damage != 0) sb.Append('d').Append(c.Damage);
                if (c.Tapped) sb.Append('T');
                if (c.PlusOneCounters != 0) sb.Append('+').Append(c.PlusOneCounters);
                if (!c.AttachedToObject.IsNone) sb.Append('@').Append(c.AttachedToObject.Value);
            }
            sb.Append('\n');
        }
    }
}
