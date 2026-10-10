using System;
using System.Collections.Generic;
using System.Linq;

namespace RestartedTavern.Rules.AI
{
    /// <summary>GreedyBot: what activated abilities, Tavern Dweller Powers, Equip and the resources they cost are worth.</summary>
    public sealed partial class GreedyBot
    {
        private double AbilityValue(GameState s, PlayerId me, PlayerAction a)
        {
            var p = s.GetPlayer(me);
            var source = s.FindObject(a.Card);
            var ab = _engine.GetAbilities(s, source)[a.AbilityIndex];
            double v = EffectValue(s, me, ab.Effects, a.Targets, source.Id, a.X);

            if (!a.Sacrifice.IsNone)
            {
                var victim = s.FindOnBattlefield(a.Sacrifice);
                if (victim != null) v -= Worth(s, victim) + (victim.Id == source.Id ? 0 : 0.5);
            }
            if (ab.TapCost && Db.Get(source.DefinitionId).IsCreature) v -= TapPenalty(s, me, source);
            if (ab.LifeCost > 0) v -= LifeLossValue(p, ab.LifeCost);

            int generic = Costs.AbilityCost(s, Db, me, ab) + (ab.HasX ? a.X : 0);
            int gold = Payment.GoldNeeded(p, generic, true, ab.GoldCost);
            int mana = generic - (gold - ab.GoldCost);
            v -= mana * ManaUnitValue(s, p) + gold * GoldUnitValue(s, p);

            // Instant-speed abilities on an opponent's turn: wait for their end step, unless it's
            // a combat trick (after blockers) or something big like a kill.
            if (s.ActivePlayer != me && s.Step != Step.End && s.Step != Step.DeclareBlockers && v < 3) return 0;
            return v;
        }

        /// <summary>What one mana is worth if it isn't spent now. Unspent mana becomes Gold, unless the cap wastes it.</summary>
        private double ManaUnitValue(GameState s, PlayerState p)
        {
            if (MyTurnStillAhead(s, p.Id)) return DevelopmentManaValue;
            if (s.Step == Step.Main1 || s.Step == Step.BeginCombat) return 0.35;
            return p.Gold + p.Mana > GoldCap(s, p) ? 0.05 : 0.25;
        }

        /// <summary>What one Gold is worth. Gold above the cap is lost, so Gold that would overflow is cheap.</summary>
        private double GoldUnitValue(GameState s, PlayerState p)
        {
            int cap = GoldCap(s, p);
            if (s.ActivePlayer == p.Id)
                return p.Gold + p.Mana > cap ? 0.1 : 0.3 + 0.5 * _style.GoldOnOwnTurnPenalty;
            return p.Gold >= cap ? 0.12 : 0.3;
        }

        private int GoldCap(GameState s, PlayerState p) => GoldRules.Cap(s, Db, p.Id);

        /// <summary>One mana that would otherwise build the board on your own turn (a permanent is worth about 2 per mana).</summary>
        private const double DevelopmentManaValue = 1.2;

        /// <summary>Round pool: it's someone else's turn, and this player's own turn is still to come in this round.</summary>
        private static bool MyTurnStillAhead(GameState s, PlayerId me)
        {
            if (!s.Format.ManaPerRound || s.Format.AlternatingActions || s.ActivePlayer == me) return false;
            int n = s.Players.Count;
            int RoundPos(PlayerId id) => (s.GetPlayer(id).Seat - s.RoundLeaderSeat + n) % n;
            return RoundPos(me) > RoundPos(s.ActivePlayer);
        }

        /// <summary>A tapped creature can't attack, and stays tapped through the opponent's turn (no blocking).</summary>
        private double TapPenalty(GameState s, PlayerId me, CardInstance creature)
        {
            int power = Stats(s, creature).Power;
            if (s.ActivePlayer == me)
            {
                bool beforeAttacks = s.Step == Step.Main1 || s.Step == Step.BeginCombat;
                return beforeAttacks ? 0.5 + 0.5 * power : 0.3 + 0.2 * power;
            }
            bool beforeBlocks = s.Step < Step.DeclareBlockers || (s.Step == Step.DeclareBlockers && s.Pending != null);
            return beforeBlocks ? 0.3 + 0.3 * power : 0; // after combat it untaps in our untap step anyway
        }

        private double LifeLossValue(PlayerState p, int amount)
        {
            if (p.Life <= amount) return 100;
            return amount * (p.Life <= 10 ? 0.8 : 0.3);
        }

        /// <summary>What attaching <paramref name="equipment"/> to <paramref name="creature"/> adds, minus what it leaves behind.</summary>
        private double EquipValue(GameState s, PlayerId me, CardInstance equipment, CardInstance creature)
        {
            if (equipment == null || creature == null || creature.Controller != me) return -1;
            if (equipment.AttachedToObject == creature.Id) return -1;
            double v = BonusValue(s, equipment, creature);
            var current = s.FindOnBattlefield(equipment.AttachedToObject);
            if (current != null) v -= BonusValue(s, equipment, current) + 0.3;
            return v;
        }

        private double BonusValue(GameState s, CardInstance equipment, CardInstance creature)
        {
            double v = 0;
            foreach (var mod in Db.Get(equipment.DefinitionId).Statics.OfType<AttachedCreatureModifier>())
            {
                v += 0.8 * mod.Power + 0.6 * mod.Health;
                if (mod.Grants.HasFlag(Keyword.Flying)) v += 1.5;
                if (mod.Grants.HasFlag(Keyword.Trample)) v += 0.6;
                v += 1.2 * (mod.Triggers.Count + mod.Abilities.Count);
                if (mod.MaxDamageEachTurn > 0) v += 1.5;
            }
            var st = Stats(s, creature);
            // Bigger, healthy creatures carry Equipment better; a creature about to die doesn't.
            if (st.RemainingHealth <= 1) v *= 0.6;
            if (st.Has(Keyword.CantBlock)) v *= 0.9;
            return v * (1 + st.Power / 8.0);
        }

        /// <summary>
        /// A temporary +P/+H on one of our creatures only matters in combat: after blockers are
        /// declared, or before our attack. Penalties on enemy creatures are valued as before.
        /// </summary>
        private double PumpValue(GameState s, PlayerId me, CardInstance c, int power, int health, Keyword grants)
        {
            if (c == null) return 0;
            if (c.Controller != me) return -(power + health) * 0.5;
            bool inCombat = s.Combat != null && (s.Combat.IsAttacking(c.Id) || s.Combat.IsBlocking(c.Id));
            double trample = grants.HasFlag(Keyword.Trample) && !Stats(s, c).Has(Keyword.Trample) ? 1 : 0;
            if (inCombat && s.Step == Step.DeclareBlockers && s.Pending == null)
            {
                bool blocked = s.Combat.IsBlocking(c.Id) || (s.Combat.AttackOf(c.Id)?.Blocked ?? false);
                var attack = s.Combat.AttackOf(c.Id);
                if (!blocked && attack != null && power > 0 && Stats(s, c).Power + power >= s.GetPlayer(attack.Defender).Life) return 100;
                return blocked ? (power + health) * 0.6 + trample : power * 0.6;
            }
            bool canAttack = !c.Tapped && (!c.SummoningSick || Stats(s, c).Has(Keyword.Haste));
            if (s.ActivePlayer == me && (s.Step == Step.Main1 || s.Step == Step.BeginCombat) && canAttack)
                return 0.3 * (power + 0.5 * health) + 0.3 * trample;
            return 0;
        }
    }
}
