namespace RestartedTavern.Rules
{
    /// <summary>
    /// All format numbers live here, never hard-coded (DEVELOPMENT §1.5, GAME_DESIGN §2–3).
    /// The defaults are the Standard rules: Runeterra-style mana (GAME_DESIGN §5–6, adopted 2026-10-09).
    /// <see cref="Classic"/> keeps the old per-turn mana for comparisons and older rules tests.
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

        /// <summary>1v1 (§3, MTG default): the first player skips their turn-1 draw. Multiplayer: everyone draws (§13).</summary>
        public bool FirstPlayerSkipsDraw { get; set; } = true;

        // ---------------------------------------------------------------- the mana model (§5–6)

        /// <summary>
        /// The <b>round pool</b> (§5.1): at the start of each round every player gains +1 max mana and refills, mana can be
        /// spent on any turn of that round (permanents still only on your own turn), and at the end of the round everyone's
        /// unspent mana is banked as Gold. false (Classic): mana refills at the start of your own turn and is banked at its end.
        /// </summary>
        public bool ManaPerRound { get; set; } = true;

        /// <summary>
        /// Each round starts with the next player in seat order (§6.1), so in 1v1 the order is A B | B A | A B ...
        /// false (Classic): A B A B.
        /// </summary>
        public bool RotateRoundLeader { get; set; } = true;

        /// <summary>
        /// The attack token (§6.1, Legends of Runeterra): only the player who starts the round may attack in it. With
        /// RotateRoundLeader the token alternates between rounds.
        /// </summary>
        public bool AttackToken { get; set; } = true;

        /// <summary>Spells and abilities pay Gold first, then mana (§5.2, Runeterra spends spell mana first). Permanents use mana only.</summary>
        public bool GoldFirstAlways { get; set; } = true;

        // ---------------------------------------------------------------- experiment switches (not rules)

        /// <summary>Creatures can attack and use Tap abilities the turn they arrive. Off in the rules (§7.4).</summary>
        public bool NoSummoningSickness { get; set; }

        /// <summary>Pay Gold first only on other players' turns (an earlier experiment; GoldFirstAlways replaced it).</summary>
        public bool GoldFirstOffTurn { get; set; }

        /// <summary>Gold the second player starts with (going-second experiments).</summary>
        public int SecondPlayerStartingGold { get; set; }

        /// <summary>Extra cards the second player draws before the first turn (going-second experiments).</summary>
        public int SecondPlayerExtraCards { get; set; }

        /// <summary>Extra mana for the second player's first turn only (Classic going-second experiments).</summary>
        public int SecondPlayerFirstTurnBonusMana { get; set; }

        /// <summary>The first player gets no max mana on their first turn (going-second experiment).</summary>
        public bool FirstPlayerSkipsFirstMana { get; set; }

        /// <summary>Damage on creatures is removed in the cleanup step like in MTG. Used to measure what permanent damage (§7.3) changes.</summary>
        public bool DamageWearsOff { get; set; }

        /// <summary>Games without Tavern Dwellers (no zone, passives or Powers). Used to measure what Tavern Dwellers change.</summary>
        public bool TavernDwellersEnabled { get; set; } = true;

        /// <summary>Set to false in tests or tools that build decks freely.</summary>
        public bool EnforceDeckRules { get; set; } = true;

        /// <summary>The Standard rules (Runeterra-style mana).</summary>
        public static FormatConfig Standard() => new FormatConfig();

        /// <summary>Standard with a different Gold cap or without summoning sickness (experiments).</summary>
        public static FormatConfig Runeterra(int goldCap = 3, bool summoningSickness = true)
        {
            var f = Standard();
            f.GoldCap = goldCap;
            f.NoSummoningSickness = !summoningSickness;
            return f;
        }

        /// <summary>
        /// The rules before 2026-10-09: mana refills on your own turn and is banked at its end (Gold cap 5), mana first,
        /// turns alternate A B A B and every player may attack every turn. For comparisons and older rules tests.
        /// </summary>
        public static FormatConfig Classic() => new FormatConfig
        {
            Name = "Classic",
            GoldCap = 5,
            ManaPerRound = false,
            RotateRoundLeader = false,
            AttackToken = false,
            GoldFirstAlways = false,
        };

        /// <summary>❓ Multiplayer keeps the Standard mana model for now; the attack token in multiplayer is an open question (§13).</summary>
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
