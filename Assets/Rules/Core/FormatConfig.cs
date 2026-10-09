namespace RestartedTavern.Rules
{
    /// <summary>
    /// All format numbers live here, never hard-coded (DEVELOPMENT §1.5, GAME_DESIGN §2–3).
    /// </summary>
    public sealed class FormatConfig
    {
        public string Name { get; set; } = "Standard";
        public int DeckSize { get; set; } = 60;
        public int CopyLimit { get; set; } = 4;
        public int MinPlayers { get; set; } = 2;
        public int MaxPlayers { get; set; } = 2;
        public int StartingLife { get; set; } = 30;
        public int StartingHand { get; set; } = 7;
        public int MaxHandSize { get; set; } = 7;
        public int ManaCap { get; set; } = 10;
        public int GoldCap { get; set; } = 5;

        /// <summary>1v1: the first player skips their turn-1 draw (§3). Multiplayer: everyone draws (§13).</summary>
        public bool FirstPlayerSkipsDraw { get; set; } = true;

        /// <summary>1v1: the second player starts with 1 Gold (§3). Multiplayer: no compensation (§13).</summary>
        public int SecondPlayerStartingGold { get; set; } = 1;

        /// <summary>Experiment switch (going-second compensation): extra cards the second player draws before the first turn.</summary>
        public int SecondPlayerExtraCards { get; set; }

        /// <summary>
        /// Experiment switch (going-second compensation, like Hearthstone's Coin): extra mana for the
        /// second player's first turn only. Unspent, it becomes Gold as usual.
        /// </summary>
        public int SecondPlayerFirstTurnBonusMana { get; set; }

        /// <summary>
        /// Experiment switch, not a real rule: when true, damage on creatures is removed in the
        /// cleanup step like in MTG. Used to measure what permanent damage (§7.3) changes.
        /// </summary>
        public bool DamageWearsOff { get; set; }

        /// <summary>Set to false in tests or tools that build decks freely.</summary>
        public bool EnforceDeckRules { get; set; } = true;

        public static FormatConfig Standard() => new FormatConfig();

        public static FormatConfig MultiplayerStandard() => new FormatConfig
        {
            Name = "Multiplayer Standard",
            MinPlayers = 3,
            MaxPlayers = 4,
            StartingLife = 40,
            FirstPlayerSkipsDraw = false,
            SecondPlayerStartingGold = 0,
        };

        public FormatConfig Clone() => (FormatConfig)MemberwiseClone();
    }
}
