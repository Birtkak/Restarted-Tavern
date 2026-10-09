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
        /// <summary>
        /// Can Gold help pay for casting this card? Instants and Sorceries: yes. Permanents: no (§5.2), unless
        /// the card says so (Retainer Mage) or a payment rule lets Gold pay for creature spells (Shady Moneylender).
        /// </summary>
        public static bool GoldAllowed(GameState s, CardDatabase db, PlayerId player, CardDefinition def)
        {
            if (!def.IsPermanent || def.GoldMayPay) return true;
            return def.IsCreature && HasPaymentRule(s, db, player, r => r.GoldForCreatureSpells);
        }

        /// <summary>"You may pay Invest costs with mana as well as Gold" (Silent Partner).</summary>
        public static bool InvestMayUseMana(GameState s, CardDatabase db, PlayerId player) =>
            HasPaymentRule(s, db, player, r => r.InvestWithMana);

        private static bool HasPaymentRule(GameState s, CardDatabase db, PlayerId player, Func<PaymentRuleAbility, bool> rule)
        {
            var p = s.GetPlayer(player);
            foreach (var list in new[] { p.TavernDwellerZone, p.Battlefield })
                foreach (var source in list)
                    foreach (var st in db.Get(source.DefinitionId).Statics)
                        if (st is PaymentRuleAbility r && rule(r)) return true;
            return false;
        }

        /// <summary>
        /// Gold needed for a cost after mana is used up (or Gold first, see PlayerState.PaysGoldFirst), or -1 if it can't be paid.
        /// <paramref name="generic"/> is paid with mana first, then Gold if <paramref name="goldAllowed"/>;
        /// <paramref name="goldOnly"/> is paid only with Gold.
        /// </summary>
        public static int GoldNeeded(PlayerState p, int generic, bool goldAllowed, int goldOnly = 0)
        {
            if (p.PaysGoldFirst && goldAllowed)
            {
                // Gold first, then mana (FormatConfig.GoldFirstOffTurn).
                int spare = p.Gold - goldOnly;
                if (spare < 0) return -1;
                int fromGold = Math.Min(spare, Math.Max(0, generic));
                return generic - fromGold <= p.Mana ? fromGold + goldOnly : -1;
            }
            int rest = Math.Max(0, generic - p.Mana);
            if (rest > 0 && !goldAllowed) return -1;
            int gold = rest + goldOnly;
            return p.Gold >= gold ? gold : -1;
        }

        /// <summary>Gold casting this card would take after mana is used up, or -1 if it can't be paid at all.</summary>
        public static int GoldNeeded(GameState s, CardDatabase db, PlayerState p, CardDefinition def) =>
            GoldNeeded(p, Costs.SpellCost(s, db, p.Id, def), GoldAllowed(s, db, p.Id, def));

        public static bool CanPay(GameState s, CardDatabase db, PlayerState p, CardDefinition def) => GoldNeeded(s, db, p, def) >= 0;

        /// <summary>
        /// How the Invest cost would be paid after the main cost: Gold only, or mana first and then Gold
        /// with Silent Partner. Returns false if it can't be paid.
        /// </summary>
        public static bool InvestSplit(GameState s, CardDatabase db, PlayerState p, CardDefinition def, out int mana, out int gold)
        {
            mana = 0;
            gold = 0;
            if (!def.InvestCost.HasValue) return false;
            int goldForCost = GoldNeeded(s, db, p, def);
            if (goldForCost < 0) return false;
            int manaLeft = p.Mana - (Costs.SpellCost(s, db, p.Id, def) - goldForCost);
            int invest = Costs.InvestCost(s, db, p.Id, def);
            if (InvestMayUseMana(s, db, p.Id)) mana = Math.Min(Math.Max(0, manaLeft), invest);
            gold = invest - mana;
            return goldForCost + gold <= p.Gold;
        }

        /// <summary>Can the Invest cost be paid too, after the main cost?</summary>
        public static bool CanInvest(GameState s, CardDatabase db, PlayerState p, CardDefinition def) =>
            InvestSplit(s, db, p, def, out _, out _);
    }

    /// <summary>GAME_DESIGN §5.2: each player's Gold cap.</summary>
    public static class GoldRules
    {
        /// <summary>
        /// The format's cap, unless one of the player's permanents or their Tavern Dweller sets it
        /// ("Your Gold cap is 8"); the newest such effect wins.
        /// </summary>
        public static int Cap(GameState s, CardDatabase db, PlayerId player)
        {
            var p = s.GetPlayer(player);
            int cap = s.Format.GoldCap;
            long newest = long.MinValue;
            foreach (var list in new[] { p.TavernDwellerZone, p.Battlefield })
                foreach (var source in list)
                    foreach (var st in db.Get(source.DefinitionId).Statics)
                        if (st is GoldCapAbility g && source.Timestamp > newest)
                        {
                            newest = source.Timestamp;
                            cap = g.Cap;
                        }
            return cap;
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
