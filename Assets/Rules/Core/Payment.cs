namespace RestartedTavern.Rules
{
    /// <summary>
    /// GAME_DESIGN §5.2 payment rules. Payment is automatic, there's no choosing a split:
    /// permanents are paid with mana only; Instants, Sorceries and abilities use mana first and
    /// Gold for the rest; Invest is paid only with Gold.
    /// </summary>
    public static class Payment
    {
        public static bool GoldAllowed(CardDefinition def) => !def.IsPermanent;

        /// <summary>Gold the card would take after mana is used up, or -1 if it can't be paid at all.</summary>
        public static int GoldNeeded(PlayerState p, CardDefinition def)
        {
            if (p.Mana >= def.Cost) return 0;
            if (!GoldAllowed(def)) return -1;
            int rest = def.Cost - p.Mana;
            return p.Gold >= rest ? rest : -1;
        }

        public static bool CanPay(PlayerState p, CardDefinition def) => GoldNeeded(p, def) >= 0;

        /// <summary>Can the Invest cost be paid too, from the Gold left after the main cost?</summary>
        public static bool CanInvest(PlayerState p, CardDefinition def)
        {
            int gold = GoldNeeded(p, def);
            return def.InvestCost.HasValue && gold >= 0 && p.Gold - gold >= def.InvestCost.Value;
        }
    }
}
