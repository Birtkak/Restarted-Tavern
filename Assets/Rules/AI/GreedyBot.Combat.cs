using System;
using System.Collections.Generic;
using System.Linq;

namespace RestartedTavern.Rules.AI
{
    /// <summary>GreedyBot: attacks, blocks and the combat choices (damage division, which Legendary to keep).</summary>
    public sealed partial class GreedyBot
    {
        private double KeepValue(GameState s, CardInstance c)
        {
            if (c == null) return 0;
            double equipment = s.AllPermanents().Count(e => e.AttachedToObject == c.Id);
            return Def(s, c.Id).IsCreature ? Stats(s, c).RemainingHealth + 2 * equipment : equipment;
        }

        /// <summary>
        /// Divide combat damage among enemy creatures (§7.2.6): kill the most valuable ones it can, and put
        /// what's left on the others (damage stays, §7.3).
        /// </summary>
        private PlayerAction ChooseDamageSplit(GameState s, List<PlayerAction> legal)
        {
            var recipients = s.Pending.Choices.Select(id => s.FindOnBattlefield(id)).ToList();
            double Score(PlayerAction a)
            {
                double score = 0;
                for (int i = 0; i < recipients.Count; i++)
                {
                    var c = recipients[i];
                    if (c == null) continue;
                    int left = Math.Max(1, Stats(s, c).RemainingHealth);
                    score += a.Division[i] >= left ? Worth(s, c) : 0.3 * Worth(s, c) * a.Division[i] / left;
                }
                return score;
            }
            return legal.OrderByDescending(Score).First();
        }

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
