namespace RestartedTavern.Rules
{
    /// <summary>
    /// All format numbers live here, never hard-coded (DEVELOPMENT §1.5, GAME_DESIGN §2–3).
    /// The defaults are the Standard rules: Legends of Runeterra rounds (GAME_DESIGN §5–6, 2026-10-10).
    /// <see cref="MtgTurns"/>, <see cref="RuneterraRotation"/> and <see cref="Classic"/> keep earlier turn structures
    /// for comparisons and older rules tests.
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

        /// <summary>The first player skips their first draw (MTG 103.8a). Off in Standard: going first is evened out by the rounds (§3, §6).</summary>
        public bool FirstPlayerSkipsDraw { get; set; }

        // ---------------------------------------------------------------- turns and mana (§5–6)

        /// <summary>
        /// Legends of Runeterra rounds (§6). A round is one turn for everyone: at its start every player refills, untaps
        /// and draws. Then players take <b>actions</b> one at a time, starting with the round leader: an action is playing
        /// a card (any speed), activating an ability, or (once per round, with the attack token) attacking. When the
        /// action has resolved, the next player has the action. Passing hands it on; when everyone passes in a row, the
        /// round ends. Responses on the Chain don't use up an action. Needs ManaPerRound.
        /// false: MTG turns, one player's turn after another.
        /// </summary>
        public bool AlternatingActions { get; set; } = true;

        /// <summary>
        /// The <b>round pool</b> (§5.1): at the start of each round every player gains +1 max mana and refills, mana can be
        /// spent at any time in that round, and at the end of the round everyone's unspent mana is banked as Gold.
        /// false (Classic): mana refills at the start of your own turn and is banked at its end.
        /// </summary>
        public bool ManaPerRound { get; set; } = true;

        /// <summary>
        /// Each round starts with the next player in seat order (§6.1). With alternating actions the round leader acts
        /// first and holds the attack token. MTG turns: the order becomes A B | B A | A B ...
        /// </summary>
        public bool RotateRoundLeader { get; set; } = true;

        /// <summary>The attack token (§6.1, Legends of Runeterra): only the round leader may attack in the round.</summary>
        public bool AttackToken { get; set; } = true;

        /// <summary>Spells and abilities pay Gold first, then mana (§5.2, Runeterra spends spell mana first). Permanents use mana only.</summary>
        public bool GoldFirstAlways { get; set; } = true;

        // ---------------------------------------------------------------- experiment switches (not rules)

        /// <summary>
        /// MTG turns with ManaPerRound false: your mana refills at the start of your own turn and stays until your next
        /// turn starts, like untapped MTG lands. What's left when your next turn starts is banked as Gold first.
        /// </summary>
        public bool ManaUntilYourNextTurn { get; set; }

        /// <summary>Gold the second player starts with (going-second experiments).</summary>
        public int SecondPlayerStartingGold { get; set; }

        /// <summary>Extra cards the second player draws before the first turn (going-second experiments).</summary>
        public int SecondPlayerExtraCards { get; set; }

        /// <summary>How many of the second player's first turns get SecondPlayerFirstTurnBonusMana (going-second experiments).</summary>
        public int SecondPlayerBonusTurns { get; set; } = 1;

        /// <summary>Extra mana for the second player's first turn(s) (going-second experiments, MTG turns).</summary>
        public int SecondPlayerFirstTurnBonusMana { get; set; }

        /// <summary>Damage on creatures is removed in the cleanup step like in MTG. Used to measure what permanent damage (§7.3) changes.</summary>
        public bool DamageWearsOff { get; set; }

        /// <summary>Games without Tavern Dwellers (no zone, passives or Powers). Used to measure what Tavern Dwellers change.</summary>
        public bool TavernDwellersEnabled { get; set; } = true;

        /// <summary>Set to false in tests or tools that build decks freely.</summary>
        public bool EnforceDeckRules { get; set; } = true;

        /// <summary>The Standard rules: Legends of Runeterra rounds (2026-10-10).</summary>
        public static FormatConfig Standard() => new FormatConfig();

        /// <summary>
        /// MTG turns (A B A B, everyone attacks on their own turn, the first player skips their first draw) with the round
        /// pool and Gold first. There is no summoning sickness in any format since 2026-10-10 (§7.4). Measured 2026-10-10: the first player wins 75–85% of aggro mirrors.
        /// </summary>
        public static FormatConfig MtgTurns() => new FormatConfig
        {
            Name = "MTG turns",
            AlternatingActions = false,
            RotateRoundLeader = false,
            AttackToken = false,
            FirstPlayerSkipsDraw = true,
        };

        /// <summary>
        /// The Standard rules on the morning of 2026-10-10: MTG turns with a rotating round leader (A B | B A) and the
        /// attack token. Kept for comparisons.
        /// </summary>
        public static FormatConfig RuneterraRotation()
        {
            var f = MtgTurns();
            f.Name = "Runeterra rotation";
            f.RotateRoundLeader = true;
            f.AttackToken = true;
            return f;
        }

        /// <summary>
        /// The rules before 2026-10-09: mana refills on your own turn and is banked at its end (Gold cap 5), mana first,
        /// turns alternate A B A B and every player may attack every turn. For comparisons and older rules tests.
        /// </summary>
        public static FormatConfig Classic()
        {
            var f = MtgTurns();
            f.Name = "Classic";
            f.GoldCap = 5;
            f.ManaPerRound = false;
            f.GoldFirstAlways = false;
            return f;
        }

        /// <summary>
        /// ❓ Multiplayer keeps MTG turns for now: how the rounds and the attack token work with 3–4 players is open (§13).
        /// </summary>
        public static FormatConfig MultiplayerStandard()
        {
            var f = MtgTurns();
            f.Name = "Multiplayer Standard";
            f.MinPlayers = 3;
            f.MaxPlayers = 4;
            f.StartingLife = 40;
            f.FirstPlayerSkipsDraw = false;
            return f;
        }

        public FormatConfig Clone() => (FormatConfig)MemberwiseClone();
    }
}
