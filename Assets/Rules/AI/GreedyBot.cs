using System;
using System.Collections.Generic;
using System.Linq;

namespace RestartedTavern.Rules.AI
{
    /// <summary>
    /// A simple rule-based player for simulations and the debug table (DEVELOPMENT §6: "rule-based
    /// first"). It looks one action ahead and never bluffs, so it plays like a careful beginner:
    /// it develops its biggest threat, uses removal only when it kills, attacks when no blocker
    /// would eat its creature for free, blocks when it wins or trades evenly, and chump-blocks only
    /// to survive. Deterministic: same state, same choice.
    /// </summary>
    public sealed class GreedyBot
    {
        private readonly GameEngine _engine;
        private CardDatabase Db => _engine.Cards;

        public GreedyBot(GameEngine engine) { _engine = engine; }

        public PlayerAction Choose(GameState s, PlayerId me)
        {
            var legal = _engine.GetLegalActions(s, me);
            if (legal.Count == 0) throw new InvalidOperationException(me + " has no legal actions.");
            if (legal.Count == 1) return legal[0];

            switch (s.Pending?.Kind)
            {
                case DecisionKind.Mulligan: return KeepOrMulligan(s, me, legal);
                case DecisionKind.BottomCards:
                case DecisionKind.DiscardToHandSize:
                    return legal.OrderByDescending(a => Def(s, a.Card).Cost).First();
                case DecisionKind.DeclareAttackers: return ChooseAttack(s, me, legal);
                case DecisionKind.DeclareBlockers: return ChooseBlock(s, me, legal);
                case DecisionKind.ChooseTriggerTarget:
                {
                    var trigger = s.Pending.Trigger;
                    return legal.OrderByDescending(a => EffectValue(s, me, trigger.Ability.Effects, a.Targets, trigger.SourceId)).First();
                }
            }

            PlayerAction best = null;
            double bestScore = 0.9; // anything below this isn't worth a card
            foreach (var a in legal)
            {
                if (a.Kind != ActionKind.PlayCard) continue;
                double score = PlayValue(s, me, a);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = a;
                }
            }
            return best ?? legal.First(a => a.Kind == ActionKind.PassPriority);
        }

        // ------------------------------------------------------------------ helpers

        private CardDefinition Def(GameState s, ObjectId id) => Db.Get(s.FindObject(id).DefinitionId);
        private Characteristics Stats(GameState s, CardInstance c) => _engine.GetCharacteristics(s, c);

        /// <summary>Keep a hand with 2–5 early plays (cost 3 or less); mulligan at most once.</summary>
        private PlayerAction KeepOrMulligan(GameState s, PlayerId me, List<PlayerAction> legal)
        {
            var p = s.GetPlayer(me);
            int early = p.Hand.Count(c => Db.Get(c.DefinitionId).Cost <= 3);
            bool keep = p.MulligansTaken >= 1 || (early >= 2 && early <= 5);
            return legal.First(a => a.Kind == (keep ? ActionKind.Keep : ActionKind.Mulligan));
        }

        private double PlayValue(GameState s, PlayerId me, PlayerAction a)
        {
            var p = s.GetPlayer(me);
            var card = p.Hand.Find(c => c.Id == a.Card);
            var def = Db.Get(card.DefinitionId);
            double value;

            if (def.IsPermanent)
            {
                // Develop: bigger is better, so the curve gets used.
                value = 2 * def.Cost + 1;
                if (def.Keywords.HasFlag(Keyword.Haste) && s.Step == Step.Main1) value += 1;
            }
            else
            {
                value = EffectValue(s, me, def.SpellEffects, a.Targets, a.Card);
            }

            if (a.Overcharge) value += EffectValue(s, me, def.OverchargeEffects, a.Targets, a.Card) + 0.1;

            // Gold is flexible (instant speed); spend mana first when it's our turn.
            if (s.ActivePlayer == me) value -= 0.4 * a.GoldPaid;
            return value;
        }

        /// <summary>How good these effects are for <paramref name="me"/> with these targets.</summary>
        private double EffectValue(GameState s, PlayerId me, List<Effect> effects, IReadOnlyList<Target> targets, ObjectId source)
        {
            Target? T(int i) => i < targets.Count ? targets[i] : (Target?)null;
            var pumps = new Dictionary<int, (int power, int health)>(); // "+2/+2, then it fights"
            double v = 0;
            foreach (var e in effects)
            {
                var target = T(e.TargetIndex);
                switch (e)
                {
                    case DealDamageEffect d when d.EachTarget:
                        foreach (var t in targets) v += DamageValue(s, me, d.Amount, t);
                        break;
                    case DealDamageEffect d: v += DamageValue(s, me, d.Amount, target); break;
                    case DealDamageToEachOpponentEffect d: v += 0.6 * d.Amount; break;
                    case DealDamageToEachEnemyCreatureEffect d:
                        foreach (var p in s.Players)
                            if (s.AreOpponents(me, p.Id))
                                foreach (var c in p.Battlefield)
                                    if (Db.Get(c.DefinitionId).IsCreature) v += DamageValue(s, me, d.Amount, Target.ForObject(c.Id));
                        if (d.AlsoOpponents) v += 0.6 * d.Amount;
                        break;
                    case HealEffect h: v += HealValue(s, me, h.Fully ? 99 : h.Amount, target); break;
                    case HealOtherCreaturesYouControlEffect h:
                        foreach (var c in s.GetPlayer(me).Battlefield)
                            if (c.Id != source && Db.Get(c.DefinitionId).IsCreature) v += HealValue(s, me, h.Fully ? 99 : h.Amount, Target.ForObject(c.Id));
                        break;
                    case FightEffect f:
                        var first = f.SourceFights ? s.FindOnBattlefield(source) : Creature(s, T(f.TargetIndex));
                        var second = Creature(s, T(f.SourceFights ? f.TargetIndex : f.TargetIndex + 1));
                        pumps.TryGetValue(f.SourceFights ? -1 : f.TargetIndex, out var bonus);
                        v += FightValue(s, me, first, second, bonus.power, bonus.health);
                        break;
                    case DrawCardsEffect d: v += 2.0 * d.Count; break;
                    case GainGoldEffect g: v += 0.5 * g.Amount; break;
                    case CreateTokensEffect t: v += 2.0 * t.Count; break;
                    case AddCountersEffect c: v += CreatureOwnerSign(s, me, target) * 2.0 * c.Count; break;
                    case PumpTargetEffect pt:
                        v += CreatureOwnerSign(s, me, target) * (pt.Power + pt.Health) * 0.5;
                        pumps[pt.TargetIndex] = (pt.Power, pt.Health);
                        break;
                    case PumpYourCreaturesEffect pump:
                        // Only worth it right before combat, scaled by how many creatures can attack.
                        if (s.Step == Step.Main1 && s.ActivePlayer == me)
                        {
                            int attackers = s.GetPlayer(me).Battlefield.Count(c => Db.Get(c.DefinitionId).IsCreature && !c.Tapped
                                && (!c.SummoningSick || Stats(s, c).Has(Keyword.Haste)));
                            // Tokens made by the same spell also attack (Mob Rush gives them Haste).
                            attackers += effects.OfType<CreateTokensEffect>().Where(t => t.GrantUntilEndOfTurn.HasFlag(Keyword.Haste)).Sum(t => t.Count);
                            v += attackers * (pump.Power + (pump.Grants.HasFlag(Keyword.Trample) ? 0.5 : 0)) * (attackers >= 2 ? 1.0 : 0.3);
                        }
                        break;
                }
            }
            return v;
        }

        private static CardInstance Creature(GameState s, Target? t) =>
            t.HasValue && !t.Value.IsPlayer ? s.FindOnBattlefield(t.Value.Object) : null;

        /// <summary>Value of <paramref name="mine"/> (with a temporary bonus) fighting <paramref name="theirs"/>.</summary>
        private double FightValue(GameState s, PlayerId me, CardInstance mine, CardInstance theirs, int bonusPower, int bonusHealth)
        {
            if (mine == null || theirs == null || mine.Controller != me || theirs.Controller == me) return -1;
            var a = Stats(s, mine);
            var b = Stats(s, theirs);
            int myPower = a.Power + bonusPower, myRemaining = a.RemainingHealth + bonusHealth;
            bool kills = myPower >= b.RemainingHealth;
            bool dies = b.Power >= myRemaining;
            double gain = kills ? Worth(s, theirs) + 1 : 0.4 * myPower;     // wounds stick (§7.3)
            double loss = dies ? Worth(s, mine) : 0.4 * b.Power;
            return gain - loss;
        }

        private double Worth(GameState s, CardInstance c) =>
            2 * Db.Get(c.DefinitionId).Cost + Stats(s, c).Power + (c.IsToken ? -1 : 0);

        private double CreatureOwnerSign(GameState s, PlayerId me, Target? target)
        {
            if (!target.HasValue || target.Value.IsPlayer) return 0;
            var c = s.FindOnBattlefield(target.Value.Object);
            return c == null ? 0 : c.Controller == me ? 1 : -1;
        }

        private double DamageValue(GameState s, PlayerId me, int amount, Target? target)
        {
            if (!target.HasValue) return 0;
            var t = target.Value;
            if (t.IsPlayer)
            {
                if (t.Player == me) return -10;
                var life = s.GetPlayer(t.Player).Life;
                return amount >= life ? 100 : 0.6 * amount;
            }
            var c = s.FindOnBattlefield(t.Object);
            if (c == null) return 0;
            var st = Stats(s, c);
            bool kills = amount >= st.RemainingHealth;
            double worth = Worth(s, c);
            if (c.Controller == me) return kills ? -worth - 2 : -amount;
            // Chip damage sticks (§7.3), so it has some value even when it doesn't kill.
            return kills ? worth + 1 : 0.4 * amount;
        }

        private double HealValue(GameState s, PlayerId me, int amount, Target? target)
        {
            if (!target.HasValue) return 0;
            var t = target.Value;
            if (t.IsPlayer)
            {
                if (t.Player != me) return -1;
                var p = s.GetPlayer(me);
                int healed = Math.Min(amount, s.Format.StartingLife - p.Life);
                return healed * (p.Life < 10 ? 1.2 : 0.3);
            }
            var c = s.FindOnBattlefield(t.Object);
            if (c == null) return 0;
            int restored = Math.Min(amount, c.Damage);
            if (c.Controller != me) return -restored;
            var def = Db.Get(c.DefinitionId);
            // Saving a nearly dead creature matters more than topping up a healthy one.
            double urgency = Stats(s, c).RemainingHealth <= 2 ? 1.5 : 0.8;
            return restored * urgency * (1 + def.Cost / 5.0);
        }

        // ------------------------------------------------------------------ combat

        private PlayerAction ChooseAttack(GameState s, PlayerId me, List<PlayerAction> legal)
        {
            var attacks = legal.Where(a => a.Kind == ActionKind.DeclareAttacker).ToList();
            if (attacks.Count == 0) return legal.First(a => a.Kind == ActionKind.FinishAttacks);

            // All in when the attack could be lethal through every possible block.
            foreach (var group in attacks.GroupBy(a => a.Defender))
            {
                var defender = s.GetPlayer(group.Key);
                int potential = group.Select(a => a.Card).Distinct()
                    .Sum(id => Stats(s, s.FindOnBattlefield(id)).Power);
                int blockers = defender.Battlefield.Count(c => Db.Get(c.DefinitionId).IsCreature && !c.Tapped);
                if (potential >= defender.Life && blockers == 0) return group.First();
            }

            foreach (var a in attacks)
                if (IsSafeAttack(s, s.FindOnBattlefield(a.Card), s.GetPlayer(a.Defender)))
                    return a;
            return legal.First(a => a.Kind == ActionKind.FinishAttacks);
        }

        /// <summary>Safe = no single blocker kills it without dying too (or a trade at least as good for us).</summary>
        private bool IsSafeAttack(GameState s, CardInstance attacker, PlayerState defender)
        {
            var me = Stats(s, attacker);
            var myDef = Db.Get(attacker.DefinitionId);
            foreach (var b in defender.Battlefield)
            {
                var bDef = Db.Get(b.DefinitionId);
                if (!bDef.IsCreature || b.Tapped) continue;
                var bs = Stats(s, b);
                if (bs.Has(Keyword.CantBlock)) continue;
                if (me.Has(Keyword.Flying) && !bs.Has(Keyword.Flying) && !bs.Has(Keyword.Reach)) continue;
                bool dies = bs.Power >= me.RemainingHealth;
                bool kills = me.Power >= bs.RemainingHealth;
                if (dies && !kills) return false;
                if (dies && kills && bDef.Cost < myDef.Cost) return false;
            }
            return true;
        }

        private PlayerAction ChooseBlock(GameState s, PlayerId me, List<PlayerAction> legal)
        {
            var blocks = legal.Where(a => a.Kind == ActionKind.DeclareBlocker).ToList();
            var finish = legal.First(a => a.Kind == ActionKind.FinishBlocks);
            if (blocks.Count == 0) return finish;

            int incoming = 0;
            foreach (var attack in s.Combat.Attacks)
            {
                if (attack.Defender != me || attack.Blocked) continue;
                var att = s.FindOnBattlefield(attack.Attacker);
                if (att != null) incoming += Stats(s, att).Power;
            }
            bool lethal = incoming >= s.GetPlayer(me).Life;

            // Biggest unblocked attacker first.
            var targets = blocks.Select(a => a.BlockedAttacker).Distinct()
                .Where(id => !s.Combat.AttackOf(id).Blocked)
                .OrderByDescending(id => Stats(s, s.FindOnBattlefield(id)).Power);

            foreach (var attackerId in targets)
            {
                var attacker = s.FindOnBattlefield(attackerId);
                var att = Stats(s, attacker);
                var attDef = Db.Get(attacker.DefinitionId);
                PlayerAction good = null, trade = null, wall = null, chump = null;
                int goodCost = int.MaxValue, tradeCost = int.MaxValue, wallCost = int.MaxValue, chumpCost = int.MaxValue;

                foreach (var a in blocks.Where(b => b.BlockedAttacker == attackerId))
                {
                    var blocker = s.FindOnBattlefield(a.Card);
                    var bs = Stats(s, blocker);
                    int cost = Db.Get(blocker.DefinitionId).Cost;
                    bool kills = bs.Power >= att.RemainingHealth;
                    bool survives = att.Power < bs.RemainingHealth;
                    if (kills && survives && cost < goodCost) { good = a; goodCost = cost; }
                    else if (kills && cost <= attDef.Cost && cost < tradeCost) { trade = a; tradeCost = cost; }
                    else if (survives && cost < wallCost) { wall = a; wallCost = cost; }
                    else if (cost < chumpCost) { chump = a; chumpCost = cost; }
                }

                if (good != null) return good;
                if (trade != null) return trade;
                // Soaking a hit costs permanent damage (§7.3), so only wall up when life is getting low.
                if (wall != null && (lethal || s.GetPlayer(me).Life <= 12)) return wall;
                if (lethal && chump != null) return chump;
            }
            return finish;
        }
    }
}
