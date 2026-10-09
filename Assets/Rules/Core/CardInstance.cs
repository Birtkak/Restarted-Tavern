namespace RestartedTavern.Rules
{
    /// <summary>
    /// A card (or token) as a game object in one zone. Moving it to another zone creates a
    /// new instance with a new <see cref="ObjectId"/>.
    /// </summary>
    public sealed class CardInstance
    {
        public ObjectId Id { get; set; }
        /// <summary>Null when this is a hidden card in a player's view (see GameState.CreateViewFor).</summary>
        public string DefinitionId { get; set; }
        public PlayerId Owner { get; set; }
        public PlayerId Controller { get; set; }
        public Zone Zone { get; set; }
        public bool IsToken { get; set; }

        /// <summary>
        /// Damage on this creature. Permanent: it never wears off on its own (GAME_DESIGN §7.3),
        /// only Heal removes it. Remaining Health = max Health − Damage.
        /// </summary>
        public int Damage { get; set; }

        public bool Tapped { get; set; }
        /// <summary>True until its controller starts a turn with it (MTG 302.6, GAME_DESIGN §7.4).</summary>
        public bool SummoningSick { get; set; }
        public int PlusOneCounters { get; set; }

        /// <summary>For Equipment and Curses: what this is attached to (an object or a player).</summary>
        public ObjectId AttachedToObject { get; set; }
        public PlayerId? AttachedToPlayer { get; set; }

        /// <summary>When it entered its current zone. Used for layers and the Legendary rule.</summary>
        public long Timestamp { get; set; }

        public bool IsHidden => DefinitionId == null;

        public CardInstance Clone() => (CardInstance)MemberwiseClone();

        public override string ToString() => (DefinitionId ?? "hidden") + Id;
    }
}
