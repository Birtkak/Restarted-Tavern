namespace RestartedTavern.Rules.AI
{
    /// <summary>
    /// How a <see cref="GreedyBot"/> plays. Simulations pair styles to see how the rules behave
    /// with different kinds of players, not just with racers.
    /// </summary>
    public sealed class BotStyle
    {
        public string Name { get; set; }

        /// <summary>Value of 1 damage that doesn't kill (it stays: §7.3).</summary>
        public double ChipDamageValue { get; set; } = 0.4;
        /// <summary>Reluctance to spend Gold on your own turn (Gold is your only resource on other turns).</summary>
        public double GoldOnOwnTurnPenalty { get; set; } = 0.4;
        /// <summary>Block with a creature that survives but doesn't kill when life is at or below this.</summary>
        public int WallBlockAtLife { get; set; } = 12;
        /// <summary>Trade a blocker that costs up to this much more than the attacker.</summary>
        public int TradeCostSlack { get; set; }
        /// <summary>Keep this share of the opponent's creature count home as blockers (0 = attack freely).</summary>
        public double KeepBackShare { get; set; }

        // Bot iteration 2026-10-09 (Runeterra-style mana). Each switch is on by default and off in Baseline().

        /// <summary>A drawn card that would be discarded to hand size at the end of the bot's turn is worth almost nothing.</summary>
        public bool AvoidOverdraw { get; set; } = true;

        /// <summary>
        /// Attack token: the next chance to attack is two rounds away, so damage dealt now is worth this much more.
        /// 1 = no bonus.
        /// </summary>
        public double AttackTokenUrgency { get; set; } = 1.4;

        /// <summary>
        /// Attack token: before the opponent can strike back they get a build turn, so the crack-back estimate adds a
        /// creature with Power equal to their max mana (when they have cards in hand).
        /// </summary>
        public bool CrackBackCountsTheirBuildTurn { get; set; }

        /// <summary>
        /// A creature's "Arrival: deal N damage to target ..." adds the best target's value; when it's mandatory and only
        /// our own creatures are left (or the creature itself), it costs what it destroys (Spark Drone on an empty board).
        /// </summary>
        public bool ValueArrivalDamage { get; set; } = true;

        /// <summary>The original bot: develops, races, blocks only good or even trades.</summary>
        public static BotStyle Greedy() => new BotStyle { Name = "Greedy" };

        /// <summary>
        /// Greedy with every improvement since the bot iteration of 2026-10-09 switched off: the yardstick for
        /// head-to-head tests (SimRunner -h2h). Each improvement adds a switch here.
        /// </summary>
        public static BotStyle Baseline() => new BotStyle
        {
            Name = "Baseline", AvoidOverdraw = false, AttackTokenUrgency = 1, CrackBackCountsTheirBuildTurn = false,
            ValueArrivalDamage = false,
        };

        /// <summary>Defensive: holds blockers back, blocks freely, trades up, values chip damage and keeps Gold for answers.</summary>
        public static BotStyle Control() => new BotStyle
        {
            Name = "Control",
            ChipDamageValue = 0.8,
            GoldOnOwnTurnPenalty = 1.5,
            WallBlockAtLife = 99,
            TradeCostSlack = 1,
            KeepBackShare = 0.5,
        };
    }
}
