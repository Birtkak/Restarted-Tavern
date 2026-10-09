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

        /// <summary>
        /// Experiment switch: the first player skips their turn-1 draw (the MTG rule). Off since
        /// 2026-10-09: everyone draws (§3, §13).
        /// </summary>
        public bool FirstPlayerSkipsDraw { get; set; }

        /// <summary>Gold the second player starts with. 0 since 2026-10-09 (replaced by the first-turn mana bonus); kept for experiments.</summary>
        public int SecondPlayerStartingGold { get; set; }

        /// <summary>Experiment switch (going-second compensation): extra cards the second player draws before the first turn.</summary>
        public int SecondPlayerExtraCards { get; set; }

        /// <summary>
        /// 1v1 (§3): the second player has +1 mana on their first turn only (like Hearthstone's Coin).
        /// Unspent, it becomes Gold as usual. Multiplayer: no compensation (§13).
        /// </summary>
        public int SecondPlayerFirstTurnBonusMana { get; set; } = 1;

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
            SecondPlayerFirstTurnBonusMana = 0,
        };

        public FormatConfig Clone() => (FormatConfig)MemberwiseClone();
    }
}
