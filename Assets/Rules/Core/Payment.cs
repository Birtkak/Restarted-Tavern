using System;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// GAME_DESIGN §5.2 payment rules. Payment is automatic, there's no choosing a split:
    /// permanents are paid with mana only; Instants, Sorceries and activated abilities use mana
    /// first and Gold for the rest; Invest and "Pay N Gold" costs are paid only with Gold.
    /// </summary>
    public static class Payment
    {
        public static bool GoldAllowed(CardDefinition def) => !def.IsPermanent;

        /// <summary>
        /// Gold needed for a cost after mana is used up, or -1 if it can't be paid.
        /// <paramref name="generic"/> is paid with mana first, then Gold if <paramref name="goldAllowed"/>;
        /// <paramref name="goldOnly"/> is paid only with Gold.
        /// </summary>
        public static int GoldNeeded(PlayerState p, int generic, bool goldAllowed, int goldOnly = 0)
        {
            int rest = Math.Max(0, generic - p.Mana);
            if (rest > 0 && !goldAllowed) return -1;
            int gold = rest + goldOnly;
            return p.Gold >= gold ? gold : -1;
        }

        /// <summary>Gold casting this card would take after mana is used up, or -1 if it can't be paid at all.</summary>
        public static int GoldNeeded(GameState s, CardDatabase db, PlayerState p, CardDefinition def) =>
            GoldNeeded(p, Costs.SpellCost(s, db, p.Id, def), GoldAllowed(def));

        public static bool CanPay(GameState s, CardDatabase db, PlayerState p, CardDefinition def) => GoldNeeded(s, db, p, def) >= 0;

        /// <summary>Can the Invest cost be paid too, from the Gold left after the main cost?</summary>
        public static bool CanInvest(GameState s, CardDatabase db, PlayerState p, CardDefinition def)
        {
            if (!def.InvestCost.HasValue) return false;
            int gold = GoldNeeded(s, db, p, def);
            return gold >= 0 && p.Gold - gold >= Costs.InvestCost(s, db, p.Id, def);
        }
    }

    /// <summary>
    /// What a player actually pays after cost modifiers (<see cref="CostModifierAbility"/>) from
    /// their Tavern Dweller and their permanents.
    /// </summary>
    public static class Costs
    {
        public static int SpellCost(GameState s, CardDatabase db, PlayerId player, CardDefinition def)
        {
            int cost = def.Cost;
            ForEachModifier(s, db, player, m =>
            {
                if (m.AppliesToSpell(def)) cost = Reduce(cost, m);
            });
            return cost;
        }

        public static int InvestCost(GameState s, CardDatabase db, PlayerId player, CardDefinition def)
        {
            if (!def.InvestCost.HasValue) return 0;
            return Modified(s, db, player, CostKind.Invest, def.InvestCost.Value);
        }

        /// <summary>The generic part of an ability's cost, without X.</summary>
        public static int AbilityCost(GameState s, CardDatabase db, PlayerId player, ActivatedAbility ability) =>
            ability.IsEquip ? Modified(s, db, player, CostKind.Equip, ability.Cost) : ability.Cost;

        private static int Modified(GameState s, CardDatabase db, PlayerId player, CostKind kind, int cost)
        {
            bool zero = false;
            ForEachModifier(s, db, player, m =>
            {
                if (m.Kind != kind) return;
                if (m.SetToZero) zero = true;
                else cost = Reduce(cost, m);
            });
            return zero ? 0 : cost;
        }

        private static int Reduce(int cost, CostModifierAbility m)
        {
            if (m.NotBelowOne) return cost <= 1 ? cost : Math.Max(1, cost - m.Reduction);
            return Math.Max(0, cost - m.Reduction);
        }

        private static void ForEachModifier(GameState s, CardDatabase db, PlayerId player, Action<CostModifierAbility> apply)
        {
            var p = s.GetPlayer(player);
            foreach (var c in p.TavernDwellerZone) Visit(db, c, apply);
            foreach (var c in p.Battlefield) Visit(db, c, apply);
        }

        private static void Visit(CardDatabase db, CardInstance source, Action<CostModifierAbility> apply)
        {
            foreach (var st in db.Get(source.DefinitionId).Statics)
                if (st is CostModifierAbility m) apply(m);
        }
    }
}
