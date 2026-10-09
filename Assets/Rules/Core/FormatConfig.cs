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

        /// <summary>1v1 (§3, MTG default): the first player skips their turn-1 draw. Multiplayer: everyone draws (§13).</summary>
        public bool FirstPlayerSkipsDraw { get; set; } = true;

        /// <summary>Gold the second player starts with. 0 since 2026-10-09 (replaced by the first-turn mana bonus); kept for experiments.</summary>
        public int SecondPlayerStartingGold { get; set; }

        /// <summary>Experiment switch (going-second compensation): extra cards the second player draws before the first turn.</summary>
        public int SecondPlayerExtraCards { get; set; }

        /// <summary>Experiment switch: extra mana for the second player's first turn only (a Hearthstone-style Coin). 0 in the rules (§3).</summary>
        public int SecondPlayerFirstTurnBonusMana { get; set; }

        /// <summary>
        /// Mana model under test (2026-10-09). false: today's rule, mana refills at the start of your own turn and
        /// is banked as Gold at the end of it. true: the <b>round pool</b>: at the start of each round every player
        /// gains +1 max mana and refills, mana can be spent on any turn of that round (permanents still only on
        /// your own turn), and at the end of the round everyone's unspent mana is banked as Gold.
        /// </summary>
        public bool ManaPerRound { get; set; }

        /// <summary>
        /// Payment order under test (2026-10-09): on your own turn you pay mana first, then Gold (§5.2); on other
        /// players' turns you pay Gold first, then mana, so answering on their turn doesn't eat the mana you need
        /// for your own turn later in the round.
        /// </summary>
        public bool GoldFirstOffTurn { get; set; }

        /// <summary>
        /// Turn order under test (2026-10-09): each round starts with the next player in seat order, so in 1v1 the
        /// order is A B | B A | A B ... (each player gets two turns in a row at a round boundary). false: A B A B.
        /// </summary>
        public bool RotateRoundLeader { get; set; }

        /// <summary>Going-first experiment: the first player gets no max mana on their first turn, so they stay one step behind.</summary>
        public bool FirstPlayerSkipsFirstMana { get; set; }

        /// <summary>
        /// Experiment switch, not a real rule: when true, damage on creatures is removed in the
        /// cleanup step like in MTG. Used to measure what permanent damage (§7.3) changes.
        /// </summary>
        public bool DamageWearsOff { get; set; }

        /// <summary>
        /// Experiment switch, not a real rule: when false, games are played without Tavern Dwellers (no
        /// Tavern Dweller zone, no passives, no Powers). Used to measure what Tavern Dweller Powers change.
        /// </summary>
        public bool TavernDwellersEnabled { get; set; } = true;

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
        };

        public FormatConfig Clone() => (FormatConfig)MemberwiseClone();
    }
}
