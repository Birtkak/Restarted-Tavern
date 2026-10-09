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

        /// <summary>The original bot: develops, races, blocks only good or even trades.</summary>
        public static BotStyle Greedy() => new BotStyle { Name = "Greedy" };

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
