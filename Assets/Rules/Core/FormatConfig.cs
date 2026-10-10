namespace RestartedTavern.Rules
{
    /// <summary>
    /// All format numbers live here, never hard-coded (DEVELOPMENT §1.5, GAME_DESIGN §2–3).
    /// The rules themselves are fixed: Legends of Runeterra rounds (GAME_DESIGN §5–6). A round is everyone's turn: at its
    /// start every player gains +1 max mana, refills, untaps and draws. Then players take <b>actions</b> one at a time,
    /// starting with the round leader, who holds the attack token. Unspent mana is banked as Gold at the end of the round,
    /// and spells and abilities pay Gold first.
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
        /// <summary>Gold is spell mana: 3, like Runeterra's three spell-mana gems (§5.2).</summary>
        public int GoldCap { get; set; } = 3;

        /// <summary>Set to false in tests or tools that build decks freely.</summary>
        public bool EnforceDeckRules { get; set; } = true;

        public static FormatConfig Standard() => new FormatConfig();

        /// <summary>❓ How the rounds and the attack token should work with 3–4 players is open (GAME_DESIGN §13).</summary>
        public static FormatConfig MultiplayerStandard() => new FormatConfig
        {
            Name = "Multiplayer Standard",
            MinPlayers = 3,
            MaxPlayers = 4,
            StartingLife = 40,
        };

        public FormatConfig Clone() => (FormatConfig)MemberwiseClone();
    }
}
