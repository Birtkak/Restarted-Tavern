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
    /// to survive. Deterministic: same state, same choice. A <see cref="BotStyle"/> tunes it
    /// (Greedy by default, or Control).
    /// Activated abilities and Tavern Dweller Powers are worth their effect minus what they cost: mana
    /// that would only be banked is cheap, Gold that would overflow the cap is cheap, and Gold on
    /// your own turn costs extra (it's your only resource on other turns). Instant-speed Powers
    /// wait for the opponent's end step unless something big is on offer (a kill, a combat trick).
    /// </summary>
    public sealed class GreedyBot
    {
        private readonly GameEngine _engine;
        private readonly BotStyle _style;
        private CardDatabase Db => _engine.Cards;

        public GreedyBot(GameEngine engine, BotStyle style = null)
        {
            _engine = engine;
            _style = style ?? BotStyle.Greedy();
        }

        public BotStyle Style => _style;

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
                case DecisionKind.DiscardCards:
                    return legal.OrderByDescending(a => Def(s, a.Card).Cost).First();
                case DecisionKind.DeclareAttackers: return ChooseAttack(s, me, legal);
                case DecisionKind.DeclareBlockers: return ChooseBlock(s, me, legal);
                case DecisionKind.ChooseTriggerTarget:
                {
                    var trigger = s.Pending.Trigger;
                    return legal.OrderByDescending(a => EffectValue(s, me, trigger.Ability.Effects, a.Targets, trigger.SourceId, trigger.Amount)).First();
                }
                case DecisionKind.TopOrBottom:
                {
                    // Keep it if it can be cast soon; bottom expensive cards.
                    var card = s.FindObject(s.Pending.Card);
                    bool bottom = card != null && Db.Get(card.DefinitionId).Cost > s.GetPlayer(me).MaxMana + 2;
                    return legal.First(a => a.Option == (bottom ? 1 : 0));
                }
            }

            PlayerAction best = null;
            double bestScore = 0;
            foreach (var a in legal)
            {
                double score;
                if (a.Kind == ActionKind.PlayCard) score = PlayValue(s, me, a) - 0.9; // anything below 0.9 isn't worth a card
                else if (a.Kind == ActionKind.ActivateAbility) score = AbilityValue(s, me, a) - 0.25;
                else continue;
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

            if (def.Type == CardType.Curse)
            {
                value = CurseValue(s, me, def, a.Target);
            }
            else if (def.Type == CardType.Equipment && !p.Battlefield.Any(c => Db.Get(c.DefinitionId).IsCreature))
            {
                value = def.Cost; // nothing to equip yet
            }
            else if (def.IsPermanent)
            {
                // Develop: bigger is better, so the curve gets used.
                value = 2 * def.Cost + 1;
                if (def.Keywords.HasFlag(Keyword.Haste) && s.Step == Step.Main1) value += 1;
            }
            else
            {
                value = EffectValue(s, me, def.SpellEffects, a.Targets, a.Card, a.X);
            }
            if (def.XGoldExtraCost) value -= a.X * GoldUnitValue(s, p);

            if (a.Invest) value += EffectValue(s, me, def.InvestEffects, a.Targets, a.Card) + 0.1;

            // Gold is flexible (instant speed); spend mana first when it's our turn.
            if (s.ActivePlayer == me) value -= _style.GoldOnOwnTurnPenalty * Math.Max(0, Payment.GoldNeeded(s, Db, p, def));
            return value;
        }

        // ------------------------------------------------------------------ abilities and Tavern Dweller Powers

        private double AbilityValue(GameState s, PlayerId me, PlayerAction a)
        {
            var p = s.GetPlayer(me);
            var source = s.FindObject(a.Card);
            var ab = _engine.GetAbilities(s, source)[a.AbilityIndex];
            double v = EffectValue(s, me, ab.Effects, a.Targets, source.Id);

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
                return blocked ? (power + health) * 0.6 + trample : power * 0.6;
            }
            bool canAttack = !c.Tapped && (!c.SummoningSick || Stats(s, c).Has(Keyword.Haste));
            if (s.ActivePlayer == me && (s.Step == Step.Main1 || s.Step == Step.BeginCombat) && canAttack)
                return 0.3 * (power + 0.5 * health) + 0.3 * trample;
            return 0;
        }

        /// <summary>How good these effects are for <paramref name="me"/> with these targets.</summary>
        /// <summary><paramref name="x"/>: the X paid, or a trigger's event amount ("heal that much").</summary>
        private double EffectValue(GameState s, PlayerId me, List<Effect> effects, IReadOnlyList<Target> targets, ObjectId source,
            int x = 0)
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
                    case HealEffect h: v += HealValue(s, me, h.Fully ? 99 : h.AmountFromEvent ? x : h.Amount, target); break;
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
                    case DrawCardsEffect d: v += 2.0 * (d.CountIsX ? x : d.Count); break;
                    case DrawIfGoldEffect d: v += 2.0 * (s.GetPlayer(me).Gold >= d.GoldAtLeast ? d.CountIfGold : d.Count); break;
                    case DiscardCardsEffect d: v -= 1.2 * d.Count; break;
                    case TapTargetEffect _:
                    {
                        // Tapping an enemy matters before it can attack or block; tapping our own never helps.
                        var c = target.HasValue && !target.Value.IsPlayer ? s.FindOnBattlefield(target.Value.Object) : null;
                        if (c == null || c.Controller == me || c.Tapped) break;
                        bool beforeTheirAttack = s.ActivePlayer == c.Controller && s.Step < Step.DeclareAttackers;
                        bool beforeOurAttack = s.ActivePlayer == me && (s.Step == Step.Main1 || s.Step == Step.BeginCombat);
                        if (beforeTheirAttack || beforeOurAttack) v += 0.5 + 0.4 * Stats(s, c).Power;
                        break;
                    }
                    case GainGoldEffect g: v += (g.EachOpponent ? -0.5 : 0.5) * g.Amount; break;
                    case GainLifeEffect l: v += HealValue(s, me, l.Amount, Target.ForPlayer(me)); break;
                    case LoseLifeEffect l: v -= LifeLossValue(s.GetPlayer(me), l.Amount); break;
                    case DrainEffect d: v += 0.6 * d.Amount + HealValue(s, me, d.Amount, Target.ForPlayer(me)); break;
                    case DestroyEffect _:
                    {
                        var c = Creature(s, target);
                        if (c != null) v += c.Controller == me ? -Worth(s, c) - 2 : Worth(s, c) + 1;
                        break;
                    }
                    case CreateTokensEffect t: v += 2.0 * t.Count; break;
                    case AddCountersEffect c: v += CreatureOwnerSign(s, me, target) * 2.0 * c.Count; break;
                    case PumpTargetEffect pt when pt.Health < 0:
                    {
                        // A -X/-X kills if it takes all the Health that's left (Fatal Rumor).
                        var c = Creature(s, target);
                        if (c != null && -pt.Health >= Stats(s, c).RemainingHealth)
                            v += c.Controller == me ? -Worth(s, c) - 2 : Worth(s, c) + 1;
                        else
                            v += CreatureOwnerSign(s, me, target) * (pt.Power + pt.Health) * 0.5;
                        break;
                    }
                    case PumpTargetEffect pt when effects.Any(e2 => e2 is FightEffect):
                        v += CreatureOwnerSign(s, me, target) * (pt.Power + pt.Health) * 0.5;
                        pumps[pt.TargetIndex] = (pt.Power, pt.Health);
                        break;
                    case PumpTargetEffect pt:
                        v += PumpValue(s, me, Creature(s, target), pt.Power, pt.Health, pt.Grants);
                        break;
                    case PumpSourceEffect ps:
                        v += PumpValue(s, me, s.FindOnBattlefield(source), ps.Power, ps.Health, ps.Grants);
                        break;
                    case AttachSourceEffect _:
                        v += EquipValue(s, me, s.FindOnBattlefield(source), Creature(s, target));
                        break;
                    case AttachTargetEquipmentEffect at:
                        v += EquipValue(s, me, Creature(s, target),
                            at.ToSource ? s.FindOnBattlefield(source) : Creature(s, T(at.TargetIndex + 1)));
                        break;
                    case CreateTokenCopiesEffect _:
                        foreach (var t in targets)
                        {
                            var c = Creature(s, t);
                            if (c != null) v += 0.8 * (2 * Db.Get(c.DefinitionId).Cost + 1);
                        }
                        break;
                    case ReturnToHandFromGraveyardEffect _:
                    {
                        var card = target.HasValue && !target.Value.IsPlayer ? s.FindObject(target.Value.Object) : null;
                        if (card != null) v += 1.5 + 0.2 * Db.Get(card.DefinitionId).Cost;
                        break;
                    }
                    case LookAtTopMayBottomEffect _: v += 0.4; break;
                    case DamageThenDrawIfLethalEffect dd:
                    {
                        v += DamageValue(s, me, dd.Amount, target);
                        var c = Creature(s, target);
                        if (c != null && c.Controller != me && dd.Amount >= Stats(s, c).RemainingHealth) v += 2.0;
                        break;
                    }
                    case HealYourMachinesEffect hm: v += 0.5 * hm.Amount; break;
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

        /// <summary>A Curse is worth what its -X/-Y does to the creature it's attached to.</summary>
        private double CurseValue(GameState s, PlayerId me, CardDefinition curse, Target? target)
        {
            var c = Creature(s, target);
            if (c == null) return 2 * curse.Cost; // a Curse on a player: count it as a normal permanent
            double v = 0;
            foreach (var st in curse.Statics.OfType<AttachedCreatureModifier>())
            {
                bool kills = -st.Health >= Stats(s, c).RemainingHealth;
                v += kills ? Worth(s, c) + 1 : -(st.Power + st.Health) * 0.7;
            }
            return c.Controller == me ? -v : v;
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
            double gain = kills ? Worth(s, theirs) + 1 : _style.ChipDamageValue * myPower;     // wounds stick (§7.3)
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
            return kills ? worth + 1 : _style.ChipDamageValue * amount;
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

            // Control keeps some untapped creatures home as blockers.
            if (_style.KeepBackShare > 0)
            {
                int enemyCreatures = s.Players.Where(p => s.AreOpponents(me, p.Id))
                    .Sum(p => p.Battlefield.Count(c => Db.Get(c.DefinitionId).IsCreature));
                int untapped = s.GetPlayer(me).Battlefield.Count(c => Db.Get(c.DefinitionId).IsCreature && !c.Tapped);
                int keepBack = (int)Math.Ceiling(enemyCreatures * _style.KeepBackShare);
                if (untapped <= keepBack) return legal.First(a => a.Kind == ActionKind.FinishAttacks);
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
                    else if (kills && cost <= attDef.Cost + _style.TradeCostSlack && cost < tradeCost) { trade = a; tradeCost = cost; }
                    else if (survives && cost < wallCost) { wall = a; wallCost = cost; }
                    else if (cost < chumpCost) { chump = a; chumpCost = cost; }
                }

                if (good != null) return good;
                if (trade != null) return trade;
                // Soaking a hit costs permanent damage (§7.3), so only wall up when life is getting low.
                if (wall != null && (lethal || s.GetPlayer(me).Life <= _style.WallBlockAtLife)) return wall;
                if (lethal && chump != null) return chump;
            }
            return finish;
        }
    }
}
