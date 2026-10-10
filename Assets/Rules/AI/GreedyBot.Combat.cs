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
                    int dealt = Math.Max(0, a.Division[i]);
                    score += dealt >= left ? Worth(s, c) : 0.3 * Worth(s, c) * dealt / left;
                }
                return score;
            }
            // 3+ recipients are asked one at a time (steps with -1 for later ones): plan the whole split greedily instead
            // (the engine accepts a whole split): kill what is worth most per point of damage, chip with the rest.
            if (s.Pending.DamageSoFar == null && legal.Any(a => Array.IndexOf(a.Division, -1) >= 0))
                return PlayerAction.AssignDamage(s.Pending.Player, PlanSplit(s, recipients, s.Pending.Count));
            return legal.OrderByDescending(a => Score(a)).First();
        }

        private int[] PlanSplit(GameState s, List<CardInstance> recipients, int total)
        {
            int n = recipients.Count;
            var lethal = recipients.Select(c => c == null ? 0 : Math.Max(0, Stats(s, c).RemainingHealth)).ToArray();
            var split = new int[n];
            int left = total;
            foreach (int i in Enumerable.Range(0, n).Where(i => recipients[i] != null && lethal[i] > 0)
                         .OrderByDescending(i => Worth(s, recipients[i]) / lethal[i]))
                if (lethal[i] <= left) { split[i] = lethal[i]; left -= lethal[i]; }
            for (int i = 0; i < n && left > 0; i++)
            {
                int room = Math.Min(left, Math.Min(total, lethal[i]) - split[i]);
                if (room > 0) { split[i] += room; left -= room; }
            }
            for (int i = 0; i < n && left > 0; i++)
                if (recipients[i] == null) { split[i] += left; left = 0; } // a recipient that's gone soaks the rest
            return split;
        }

        /// <summary>
        /// Attacks are planned as a whole: a few candidate attacks (nothing more, the individually safe attackers,
        /// everyone, everyone but the k best blockers) are scored against a predicted defence (value blocks,
        /// then chump blocks if the attack would be lethal) and the crack-back next turn (the opponent's surviving
        /// creatures against the creatures left home). The best plan's next undeclared attacker is declared; when
        /// the plan is complete, attacks are finished. An attack that is lethal through every block always wins.
        /// </summary>
        private PlayerAction ChooseAttack(GameState s, PlayerId me, List<PlayerAction> legal)
        {
            var finish = legal.First(a => a.Kind == ActionKind.FinishAttacks);
            var attacks = legal.Where(a => a.Kind == ActionKind.DeclareAttacker).ToList();
            if (attacks.Count == 0) return finish;

            PlayerAction best = finish;
            double bestScore = double.NegativeInfinity;
            foreach (var group in attacks.GroupBy(a => a.Defender))
            {
                var defender = s.GetPlayer(group.Key);
                var fixedIds = s.Combat.Attacks.Where(x => x.Defender == defender.Id).Select(x => x.Attacker).ToList();
                var candidates = group.Select(a => a.Card).Distinct().Select(id => s.FindOnBattlefield(id)).ToList();
                foreach (var plan in AttackPlans(s, defender, candidates))
                {
                    double score = ScoreAttack(s, me, defender, fixedIds, plan);
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = plan.Count == 0 ? finish : group.First(a => a.Card == plan[0].Id);
                }
            }
            return best;
        }

        /// <summary>Candidate sets of new attackers, from fewest to most (ties go to the earlier, more careful plan).</summary>
        private List<List<CardInstance>> AttackPlans(GameState s, PlayerState defender, List<CardInstance> candidates)
        {
            var plans = new List<List<CardInstance>> { new List<CardInstance>() };
            plans.Add(candidates.Where(c => IsSafeAttack(s, c, defender)).ToList());
            // Everyone but the k best blockers, k going down: the best blockers stay home longest.
            var byDefence = candidates.OrderByDescending(c => Stats(s, c).RemainingHealth + Stats(s, c).Power).ToList();
            for (int keep = byDefence.Count - 1; keep >= 0; keep--)
                plans.Add(byDefence.Skip(keep).ToList());
            return plans;
        }

        /// <summary>A creature as the combat planner sees it.</summary>
        private sealed class Fighter
        {
            public CardInstance Card;
            public int Power, Health, Cost;
            public bool Trample, Flying, Reach, CantBlock;
            public double Worth;
        }

        private Fighter ToFighter(GameState s, CardInstance c)
        {
            var st = Stats(s, c);
            return new Fighter
            {
                Card = c, Power = Math.Max(0, st.Power), Health = Math.Max(1, st.RemainingHealth), Cost = Db.Get(c.DefinitionId).Cost,
                Trample = st.Has(Keyword.Trample), Flying = st.Has(Keyword.Flying), Reach = st.Has(Keyword.Reach),
                CantBlock = st.Has(Keyword.CantBlock), Worth = Worth(s, c),
            };
        }

        private static bool CanBlock(Fighter blocker, Fighter attacker) =>
            !blocker.CantBlock && (!attacker.Flying || blocker.Flying || blocker.Reach);

        /// <summary>Damage is worth more the closer the player is to dying.</summary>
        private static double LifePointValue(int life) => 1 + 8.0 / Math.Max(1, life);

        private double ScoreAttack(GameState s, PlayerId me, PlayerState defender, List<ObjectId> fixedIds, List<CardInstance> plan)
        {
            var mine = s.GetPlayer(me);
            var attackers = fixedIds.Select(id => s.FindOnBattlefield(id)).Where(c => c != null).Concat(plan)
                .Select(c => ToFighter(s, c)).ToList();
            var theirs = defender.Battlefield.Where(c => Db.Get(c.DefinitionId).IsCreature).Select(c => ToFighter(s, c)).ToList();
            var blockers = theirs.Where(f => !f.Card.Tapped && !f.CantBlock).ToList();

            var blocks = PredictBlocks(attackers, blockers, defender.Life);
            int through = 0;
            double score = 0;
            var theirDead = new HashSet<Fighter>();
            foreach (var a in attackers)
            {
                if (!blocks.TryGetValue(a, out var b))
                {
                    through += a.Power;
                    continue;
                }
                if (a.Trample) through += Math.Max(0, a.Power - b.Health);
                if (b.Power >= a.Health) score -= a.Worth;
                else score -= _style.ChipDamageValue * b.Power;
                if (a.Power >= b.Health)
                {
                    score += b.Worth;
                    theirDead.Add(b);
                }
                else score += _style.ChipDamageValue * a.Power;
            }
            if (through >= defender.Life) return 1000 + through;
            score += through * LifePointValue(defender.Life) * _style.AttackTokenUrgency;

            // Attackers stay tapped through the crack-back (Decision Log 2026-10-10), except with Vigilance.
            var home = mine.Battlefield.Where(c => Db.Get(c.DefinitionId).IsCreature && !c.Tapped
                                                   && (!plan.Contains(c) && !fixedIds.Contains(c.Id) || _style.TapRuleAware && Stats(s, c).Has(Keyword.Vigilance)))
                .Select(c => ToFighter(s, c)).ToList();
            // Control keeps a share of the opponent's creature count home as blockers.
            if (_style.KeepBackShare > 0 && plan.Count > 0 && home.Count < Math.Ceiling(theirs.Count * _style.KeepBackShare))
                return double.NegativeInfinity;

            // Crack-back: next turn everything of theirs that survives can attack into what stayed home.
            var theirNext = theirs.Where(f => !theirDead.Contains(f)).ToList();
            if (_style.CrackBackCountsTheirBuildTurn && defender.Hand.Count > 0)
                theirNext.Add(new Fighter { Power = Math.Min(s.Format.ManaCap, defender.MaxMana + 1), Health = defender.MaxMana + 2 });
            int back = MinDamageThrough(theirNext, home);
            if (back >= mine.Life) score -= 500;
            else score -= _style.CrackBackWeight * back * LifePointValue(mine.Life);
            return score;
        }

        /// <summary>
        /// How the defender is expected to block: blocks that kill and survive, or trade evenly (like
        /// <see cref="ChooseBlock"/>); then, if what's left would be lethal, chump blocks to survive.
        /// </summary>
        private static Dictionary<Fighter, Fighter> PredictBlocks(List<Fighter> attackers, List<Fighter> blockers, int life)
        {
            var blocks = new Dictionary<Fighter, Fighter>();
            var free = new List<Fighter>(blockers);
            foreach (var a in attackers.OrderByDescending(x => x.Power))
            {
                var killers = free.Where(b => CanBlock(b, a) && b.Power >= a.Health).ToList();
                var pick = killers.Where(b => a.Power < b.Health).OrderBy(b => b.Cost).FirstOrDefault()
                           ?? killers.Where(b => b.Cost <= a.Cost).OrderBy(b => b.Cost).FirstOrDefault();
                if (pick == null) continue;
                blocks[a] = pick;
                free.Remove(pick);
            }

            int through = 0;
            foreach (var a in attackers)
                through += blocks.TryGetValue(a, out var b) ? (a.Trample ? Math.Max(0, a.Power - b.Health) : 0) : a.Power;
            if (through < life) return blocks;

            foreach (var a in attackers.Where(x => !blocks.ContainsKey(x)).OrderByDescending(x => x.Power))
            {
                var pick = BestChump(a, free);
                if (pick == null) continue;
                blocks[a] = pick;
                free.Remove(pick);
            }
            return blocks;
        }

        /// <summary>The blocker that stops the most of this attacker's damage for the lowest price.</summary>
        private static Fighter BestChump(Fighter attacker, List<Fighter> free)
        {
            var legal = free.Where(b => CanBlock(b, attacker));
            return attacker.Trample
                ? legal.OrderByDescending(b => b.Health).ThenBy(b => b.Worth).FirstOrDefault()
                : legal.OrderBy(b => b.Flying || b.Reach ? 1 : 0).ThenBy(b => b.Worth).FirstOrDefault();
        }

        /// <summary>The least damage that gets through when the defender blocks only to save life (one blocker each).</summary>
        private static int MinDamageThrough(List<Fighter> attackers, List<Fighter> blockers)
        {
            var free = blockers.Where(b => !b.CantBlock).ToList();
            int through = 0;
            foreach (var a in attackers.Where(x => x.Power > 0).OrderByDescending(x => x.Power))
            {
                var pick = BestChump(a, free);
                if (pick == null)
                {
                    through += a.Power;
                    continue;
                }
                free.Remove(pick);
                if (a.Trample) through += Math.Max(0, a.Power - pick.Health);
            }
            return through;
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
