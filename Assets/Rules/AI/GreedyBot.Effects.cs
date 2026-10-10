using System;
using System.Collections.Generic;
using System.Linq;

namespace RestartedTavern.Rules.AI
{
    /// <summary>GreedyBot: what effects, Curses, damage and healing are worth to the bot.</summary>
    public sealed partial class GreedyBot
    {
        /// <summary>How good these effects are for <paramref name="me"/> with these targets.</summary>
        /// <summary><paramref name="x"/>: the X paid, a trigger's event amount ("heal that much"), or a sacrificed creature's Power.</summary>
        private double EffectValue(GameState s, PlayerId me, List<Effect> effects, IReadOnlyList<Target> targets, ObjectId source,
            int x = 0, CardDefinition sourceDef = null, bool invested = false)
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
                        foreach (var t in targets) v += DamageValue(s, me, DamageAmount(s, me, d, t, x), t);
                        break;
                    case DealDamageEffect d: v += DamageValue(s, me, DamageAmount(s, me, d, target, x), target); break;
                    case DealDamageToEachCreatureEffect d:
                        foreach (var p in s.Players)
                            foreach (var c in p.Battlefield)
                                if (Db.Get(c.DefinitionId).IsCreature && !(d.ExcludeSource && c.Id == source))
                                    v += DamageValue(s, me, d.Amount, Target.ForObject(c.Id));
                        break;
                    case DamageTargetThenEachDamagedEffect d:
                        v += DamageValue(s, me, d.Amount, target);
                        foreach (var p in s.Players)
                            foreach (var c in p.Battlefield)
                                if (Db.Get(c.DefinitionId).IsCreature && c.Damage > 0 && Target.ForObject(c.Id) != target)
                                    v += DamageValue(s, me, d.Amount, Target.ForObject(c.Id));
                        break;
                    case CounterTargetEffect ct:
                    {
                        var item = ChainTarget(s, target);
                        if (item == null) break;
                        double worth = ChainItemWorth(item);
                        int gold = ct.ItsControllerGainsGold + (ct.ItsControllerGainsGoldEqualToCost && item.Card != null
                            ? Db.Get(item.Card.DefinitionId).Cost : 0);
                        double payoff = 0.3 * gold + 2.0 * ct.ItsControllerDraws;
                        v += item.Controller == me ? -worth - payoff : worth - payoff;
                        break;
                    }
                    case CounterUnlessPaysEffect cu:
                    {
                        var item = ChainTarget(s, target);
                        if (item == null) break;
                        if (item.Controller == me) { v -= ChainItemWorth(item); break; }
                        bool canPay = Payment.GoldNeeded(s.GetPlayer(item.Controller), cu.Amount, true) >= 0;
                        v += canPay ? 0.3 * cu.Amount + 0.3 * cu.RewardGoldIfPaid : ChainItemWorth(item);
                        break;
                    }
                    case ReturnToHandEffect rh when sourceDef != null && sourceDef.TargetMaxCostIsX
                                                    && Creature(s, target) is CardInstance tooBig && Db.Get(tooBig.DefinitionId).Cost > x:
                        break; // costs more than X: nothing happens
                    case ReturnToHandEffect rh:
                    {
                        var c = Creature(s, target);
                        if (c == null) break;
                        int gold = rh.ItsControllerGainsGold + (rh.ItsControllerGainsGoldEqualToCost ? Db.Get(c.DefinitionId).Cost : 0);
                        if (c.Controller != me) v += 0.7 * Worth(s, c) - 0.3 * gold - (c.IsToken ? -1 : 0.5 * Db.Get(c.DefinitionId).Cost);
                        else v += 0.6 * c.Damage + 0.4 * gold - 1.5 - (c.IsToken ? Worth(s, c) : 0);
                        break;
                    }
                    case ReturnAllCreaturesEffect ra:
                        foreach (var p in s.Players)
                            foreach (var c in p.Battlefield)
                                if (Db.Get(c.DefinitionId).IsCreature)
                                {
                                    v += c.Controller == me ? -0.6 * Worth(s, c) : 0.6 * Worth(s, c);
                                    if (c.Owner == me) v += 2.0 * ra.DrawPerCreatureYouOwned;
                                }
                        break;
                    case GainControlEffect gc:
                    {
                        var c = Creature(s, target);
                        if (c == null || c.Controller == me) break;
                        if (gc.UntilEndOfTurn)
                        {
                            // Only worth it if it can attack for us now (or to remove a blocker).
                            bool beforeAttacks = s.ActivePlayer == me && (s.Step == Step.Main1 || s.Step == Step.BeginCombat);
                            v += beforeAttacks ? 1.0 + 0.8 * Stats(s, c).Power : 0;
                        }
                        else
                        {
                            v += 2 * Worth(s, c) - (gc.PreviousControllerGainsGoldEqualToCost ? 0.3 * Db.Get(c.DefinitionId).Cost : 0)
                                 - 2.0 * gc.PreviousControllerDraws;
                        }
                        break;
                    }
                    case DestroyAllCreaturesEffect da:
                    {
                        int count = 0;
                        foreach (var p in s.Players)
                            foreach (var c in p.Battlefield)
                                if (Db.Get(c.DefinitionId).IsCreature)
                                {
                                    v += c.Controller == me ? -Worth(s, c) : Worth(s, c);
                                    count++;
                                }
                        v += 0.6 * count * da.DrainPerCreature;
                        break;
                    }
                    case ExileTargetCardEffect _: v += 0.3; break;
                    case StealAllGoldEffect _:
                    {
                        int taken = s.Players.Where(p => s.AreOpponents(me, p.Id)).Sum(p => p.Gold);
                        int room = GoldRules.Cap(s, Db, me) - s.GetPlayer(me).Gold;
                        v += 0.3 * taken + 0.3 * Math.Min(taken, room);
                        break;
                    }
                    case CreateTokensEffect t when t.CountFromRemembered:
                    {
                        int taken = s.Players.Where(p => s.AreOpponents(me, p.Id)).Sum(p => p.Gold);
                        v += 2.0 * Math.Min(taken, GoldRules.Cap(s, Db, me) - s.GetPlayer(me).Gold);
                        break;
                    }
                    case LookAtTopPutOneInHandEffect _: v += 2.3; break;
                    case LookAtTopPutOneInHandRestOnBottomEffect _: v += 2.2; break;
                    case RevealUntilCreatureEffect _: v += 8; break;
                    case EachPlayerDrawsEffect _: break; // everyone gets the same
                    case SacrificeChoiceEffect sc when sc.Who == Chooser.EachOpponent:
                        foreach (var p in s.Players.Where(p => s.AreOpponents(me, p.Id)))
                        {
                            var creatures = p.Battlefield.Where(c => Db.Get(c.DefinitionId).IsCreature).ToList();
                            if (creatures.Count > 0) v += creatures.Min(c => Worth(s, c)) + 1;
                        }
                        break;
                    case ChooseCreatureCardToBattlefieldEffect cc:
                        foreach (var p in s.Players.Where(p => !cc.OnlyYourGraveyard || p.Id == me))
                        {
                            var cards = p.Graveyard.Where(c => Db.Get(c.DefinitionId).IsCreature
                                && (!cc.MaxCost.HasValue || Db.Get(c.DefinitionId).Cost <= cc.MaxCost.Value)).ToList();
                            if (cards.Count > 0) v += 2 * cards.Max(c => Db.Get(c.DefinitionId).Cost) + 1;
                        }
                        break;
                    case EverythingHasAPriceEffect ep:
                        foreach (var p in s.Players.Where(p => s.AreOpponents(me, p.Id)))
                        {
                            var creatures = p.Battlefield.Where(c => Db.Get(c.DefinitionId).IsCreature).ToList();
                            if (creatures.Count > 0) v += 2 * creatures.Max(c => Worth(s, c)) - 0.3 * ep.Gold - 2.0 * ep.Cards;
                        }
                        break;
                    case AllCreaturesGetEffect all:
                        foreach (var p in s.Players)
                            foreach (var c in p.Battlefield)
                            {
                                if (!Db.Get(c.DefinitionId).IsCreature || -all.Health < Stats(s, c).RemainingHealth) continue;
                                v += c.Controller == me ? -Worth(s, c) : Worth(s, c) + all.GainLifePerDeath * 0.4;
                            }
                        break;
                    case DiceGameEffect _: v += 1.5; break;
                    case ReanimateEffect _:
                    {
                        var card = target.HasValue && !target.Value.IsPlayer ? s.FindObject(target.Value.Object) : null;
                        if (card != null) v += s.Step == Step.Main1 ? 1.0 + 0.8 * Db.Get(card.DefinitionId).Power : 0.3;
                        break;
                    }
                    case HealOrCountersEffect ho:
                    {
                        var c = Creature(s, target);
                        if (c != null && c.Damage > 0) v += HealValue(s, me, ho.Amount, target);
                        else v += CreatureOwnerSign(s, me, target) * 2.0 * ho.CountersIfUndamaged;
                        break;
                    }
                    case DealDamageToEachOpponentEffect d: v += FaceValue(s, me, d.AmountIsX ? x : d.Amount); break;
                    case EachOpponentLosesLifeEffect l: v += FaceValue(s, me, l.AmountIsX ? x : l.Amount); break;
                    case AttachedPlayerLosesLifeEffect l: v += FaceValue(s, me, l.Amount) + (l.YouGainLife ? 0.3 * l.Amount : 0); break;
                    case DrawPerBigCreatureEffect d:
                        v += DrawValue(s, me, System.Math.Max(1, s.GetPlayer(me).Battlefield.Count(c =>
                            Db.Get(c.DefinitionId).IsCreature && Stats(s, c).Power >= d.MinPower)));
                        break;
                    case RummageAnyEffect _: v += 1.0; break;
                    case GrantKeywordToSourceEffect _: v += 1.0; break;
                    case DivideXDamageEffect _: v += DivideValue(s, me, x); break;
                    case ReturnGraveyardCreatureEffect rg:
                    {
                        var card = target.HasValue && !target.Value.IsPlayer ? s.FindObject(target.Value.Object) : null;
                        if (card != null) v += Db.Get(card.DefinitionId).Cost * (rg.ToBattlefieldIfInvested && invested ? 2.0 : 1.0) + 0.5;
                        break;
                    }
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
                    case DrawCardsEffect d: v += DrawValue(s, me, d.CountIsX ? x : d.Count); break;
                    case DrawIfGoldEffect d: v += DrawValue(s, me, s.GetPlayer(me).Gold >= d.GoldAtLeast ? d.CountIfGold : d.Count); break;
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
                    case GainGoldEffect g: v += (g.EachOpponent ? -0.5 : 0.5) * (g.AmountIsX ? x : g.Amount); break;
                    case GainLifeEffect l: v += HealValue(s, me, l.Amount, Target.ForPlayer(me)); break;
                    case LoseLifeEffect l: v -= LifeLossValue(s.GetPlayer(me), l.Amount); break;
                    case DrainEffect d:
                    {
                        int amount = d.AmountIsX ? x : d.Amount;
                        v += FaceValue(s, me, amount) + HealValue(s, me, amount, Target.ForPlayer(me));
                        break;
                    }
                    case DestroyEffect _:
                    {
                        var c = Creature(s, target);
                        if (c != null) v += c.Controller == me ? -Worth(s, c) - 2 : Worth(s, c) + 1;
                        break;
                    }
                    case CreateTokensEffect t: v += 2.0 * (t.CountIsX ? x : t.Count); break;
                    case AddCountersEffect c: v += CreatureOwnerSign(s, me, target) * 2.0 * (c.CountIsX ? x : c.Count); break;
                    case PumpTargetEffect pt when pt.Health + pt.HealthPerX * x < 0:
                    {
                        // A -X/-X kills if it takes all the Health that's left (Fatal Rumor, Wither Away).
                        int pp = pt.Power + pt.PowerPerX * x, ph = pt.Health + pt.HealthPerX * x;
                        var c = Creature(s, target);
                        if (c != null && -ph >= Stats(s, c).RemainingHealth)
                            v += c.Controller == me ? -Worth(s, c) - 2 : Worth(s, c) + 1;
                        else
                            v += CreatureOwnerSign(s, me, target) * (pp + ph) * 0.5;
                        break;
                    }
                    case PumpTargetEffect pt when effects.Any(e2 => e2 is FightEffect):
                    {
                        int pp = pt.Power + pt.PowerPerX * x, ph = pt.Health + pt.HealthPerX * x;
                        v += CreatureOwnerSign(s, me, target) * (pp + ph) * 0.5;
                        pumps[pt.TargetIndex] = (pp, ph);
                        break;
                    }
                    case PumpTargetEffect pt:
                        v += PumpValue(s, me, Creature(s, target), pt.Power + pt.PowerPerX * x, pt.Health + pt.HealthPerX * x, pt.Grants);
                        break;
                    case PumpSourceEffect ps:
                        v += PumpValue(s, me, s.FindOnBattlefield(source), ps.Power + ps.PowerPerX * x, ps.Health, ps.Grants);
                        break;
                    case AttachSourceEffect _:
                        v += EquipValue(s, me, s.FindOnBattlefield(source), Creature(s, target));
                        break;
                    case AttachTargetEquipmentEffect at:
                        v += EquipValue(s, me, Creature(s, target),
                            at.ToSource ? s.FindOnBattlefield(source) : Creature(s, T(at.TargetIndex + 1)));
                        break;
                    case AttachEquipmentOrPumpEffect ap:
                    {
                        var creature = Creature(s, target);
                        var equipment = Creature(s, T(ap.TargetIndex + 1));
                        v += equipment != null && equipment.AttachedToObject != creature?.Id
                            ? EquipValue(s, me, equipment, creature)
                            : PumpValue(s, me, creature, ap.Power, ap.Health, Keyword.None);
                        break;
                    }
                    case ChooseUpToXYourCreaturesEffect cu:
                    {
                        // The X best ones it could choose now (they're chosen on resolution).
                        var best = s.GetPlayer(me).Battlefield
                            .Where(c => c.Id != source && Db.Get(c.DefinitionId).IsCreature
                                        && (cu.Subtype == null || Db.Get(c.DefinitionId).HasSubtype(cu.Subtype)))
                            .Select(c => Db.Get(c.DefinitionId).Cost).OrderByDescending(cost => cost).Take(x).ToList();
                        if (cu.Then.Any(e => e is CreateTokenCopiesEffect))
                            foreach (int cost in best) v += 0.8 * (2 * cost + 1);
                        if (best.Count < x) v -= 0.5 * (x - best.Count); // X paid for nothing
                        break;
                    }
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

        /// <summary>A Curse is worth what its -X/-Y does to the creature it's attached to, plus its other drawbacks.</summary>
        private double CurseValue(GameState s, PlayerId me, CardDefinition curse, Target? target)
        {
            var c = Creature(s, target);
            if (c == null) return 2 * curse.Cost; // a Curse on a player: count it as a normal permanent
            int power = 0, health = 0;
            var asIfAttached = new CardInstance { Controller = me, AttachedToObject = c.Id };
            foreach (var st in curse.Statics)
            {
                if (st is AttachedCreatureModifier m) { power += m.Power; health += m.Health; }
                if (st is AttachedScalingModifier sc)
                {
                    int n = sc.Count(s, Db, asIfAttached);
                    power += sc.PowerPer * n;
                    health += sc.HealthPer * n;
                }
            }
            bool kills = -health >= Stats(s, c).RemainingHealth;
            double v = kills ? Worth(s, c) + 1 : -(power + health) * 0.7;
            if (curse.Statics.OfType<CantBeHealedAbility>().Any()) v += 1 + 0.3 * c.Damage + 0.1 * Worth(s, c);
            v += 0.8 * curse.Triggers.Count;
            return c.Controller == me ? -v : v;
        }

        /// <summary>A DealDamageEffect's amount for this target (Kick 'Em: more if damaged; Fling the Runt: the sacrificed Power).</summary>
        private int DamageAmount(GameState s, PlayerId me, DealDamageEffect d, Target? target, int x)
        {
            if (d.AmountIsX) return x;
            if (d.AmountIsSacrificedPower) return x;
            if (d.PlusOnePerYourCreatureOfSubtype != null)
                return d.Amount + s.GetPlayer(me).Battlefield.Count(c =>
                    Db.Get(c.DefinitionId).IsCreature && Db.Get(c.DefinitionId).HasSubtype(d.PlusOnePerYourCreatureOfSubtype));
            if (d.AmountIfDamaged.HasValue && target.HasValue && !target.Value.IsPlayer)
            {
                var c = s.FindOnBattlefield(target.Value.Object);
                if (c != null && c.Damage > 0) return d.AmountIfDamaged.Value;
            }
            return d.Amount;
        }

        private static CardInstance Creature(GameState s, Target? t) =>
            t.HasValue && !t.Value.IsPlayer ? s.FindOnBattlefield(t.Value.Object) : null;

        private static ChainItem ChainTarget(GameState s, Target? t) =>
            t.HasValue && !t.Value.IsPlayer ? s.FindOnChain(t.Value.Object) : null;

        /// <summary>Roughly what a spell or ability on the Chain is worth to its controller.</summary>
        private double ChainItemWorth(ChainItem item) =>
            item.Card != null ? 2 * Db.Get(item.Card.DefinitionId).Cost + 1 : item.IsTavernDwellerPower ? 3 : 2.5;

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

        /// <summary>
        /// A card drawn is worth 2. With AvoidOverdraw, cards beyond the free hand space are worth 0.3 (a little
        /// selection): they'll be discarded at the end of our turn (max hand size).
        /// </summary>
        private double DrawValue(GameState s, PlayerId me, int count)
        {
            if (!_style.AvoidOverdraw) return 2.0 * count;
            int room = Math.Max(0, s.Format.MaxHandSize - s.GetPlayer(me).Hand.Count);
            return 2.0 * Math.Min(count, room) + 0.3 * Math.Max(0, count - room);
        }

        /// <summary>
        /// "Arrival: deal N damage to target ...": the value of the best legal target. A mandatory trigger with no enemy
        /// target hits our own creature, possibly the new one itself (worth its whole card if that kills it).
        /// </summary>
        private double ArrivalDamageValue(GameState s, PlayerId me, CardDefinition def)
        {
            double total = 0;
            foreach (var t in def.Triggers)
            {
                if (t.When != TriggerEvent.Arrival || t.Target == TargetSpec.None) continue;
                var dmg = t.Effects.OfType<DealDamageEffect>().FirstOrDefault();
                if (dmg == null || dmg.EachTarget) continue;
                int amount = DamageAmount(s, me, dmg, null, 0);
                var spec = t.Target;
                bool enemyCreatures = spec == TargetSpec.AnyTarget || spec == TargetSpec.Creature
                                      || spec == TargetSpec.CreatureYouDontControl || spec == TargetSpec.EnemyCreatureOrOpponent;
                bool ownCreatures = spec == TargetSpec.AnyTarget || spec == TargetSpec.Creature || spec == TargetSpec.CreatureYouControl;
                bool opponents = spec == TargetSpec.AnyTarget || spec == TargetSpec.Player || spec == TargetSpec.Opponent
                                 || spec == TargetSpec.EnemyCreatureOrOpponent;
                if (!enemyCreatures && !ownCreatures && !opponents) continue; // other target kinds: not judged here

                double best = double.NegativeInfinity;
                foreach (var p in s.Players)
                {
                    if (p.HasLost) continue;
                    bool mine = p.Id == me;
                    if (!mine && opponents) best = Math.Max(best, DamageValue(s, me, amount, Target.ForPlayer(p.Id)));
                    if (mine ? !ownCreatures : !enemyCreatures) continue;
                    foreach (var c in p.Battlefield)
                        if (Db.Get(c.DefinitionId).IsCreature) best = Math.Max(best, DamageValue(s, me, amount, Target.ForObject(c.Id)));
                }
                if (ownCreatures && !t.TargetNotSelf && def.IsCreature)
                    best = Math.Max(best, amount >= def.Health ? -(2 * def.Cost + 1) : -_style.ChipDamageValue * amount);
                if (t.TargetOptional) best = Math.Max(best, 0);
                if (!double.IsNegativeInfinity(best)) total += best;
            }
            return total;
        }

        /// <summary>Damage or life loss to each opponent: lethal is worth everything, otherwise more the lower their life.</summary>
        private double FaceValue(GameState s, PlayerId me, int amount)
        {
            if (amount <= 0) return 0;
            int lowest = int.MaxValue;
            foreach (var p in s.Players)
                if (!p.HasLost && s.AreOpponents(me, p.Id)) lowest = Math.Min(lowest, p.Life);
            if (lowest == int.MaxValue) return 0;
            return amount >= lowest ? 100 : amount * (0.6 + 4.0 / Math.Max(1, lowest));
        }

        /// <summary>X damage divided freely: given greedily, one point at a time, to whatever gains the most.</summary>
        private double DivideValue(GameState s, PlayerId me, int x)
        {
            var assigned = new Dictionary<Target, int>();
            var choices = new List<Target>();
            foreach (var p in s.Players)
            {
                if (p.HasLost) continue;
                if (s.AreOpponents(me, p.Id)) choices.Add(Target.ForPlayer(p.Id));
                foreach (var c in p.Battlefield)
                    if (Db.Get(c.DefinitionId).IsCreature) choices.Add(Target.ForObject(c.Id));
            }
            double total = 0;
            for (int i = 0; i < x && choices.Count > 0; i++)
            {
                double best = double.NegativeInfinity;
                Target pick = choices[0];
                foreach (var t in choices)
                {
                    assigned.TryGetValue(t, out int k);
                    double gain = DamageValue(s, me, k + 1, t) - DamageValue(s, me, k, t);
                    if (gain > best) { best = gain; pick = t; }
                }
                assigned.TryGetValue(pick, out int had);
                assigned[pick] = had + 1;
                total += best;
            }
            return total;
        }

        /// <summary>Divide-damage decision: the point that gains the most right now (kills first).</summary>
        private PlayerAction ChooseDividePoint(GameState s, PlayerId me, List<PlayerAction> legal)
        {
            var d = s.Pending;
            return legal.OrderByDescending(a =>
            {
                var t = a.Target.Value;
                int k = d.Assigned.Count(x => x == t);
                return DamageValue(s, me, k + 1, t) - DamageValue(s, me, k, t);
            }).First();
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
    }
}
