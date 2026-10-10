using System;
using System.Collections.Generic;
using System.Linq;

namespace RestartedTavern.Rules
{
    /// <summary>Combat (GAME_DESIGN §7.2, MTG CR 506–510).</summary>
    internal sealed partial class GameRunner
    {
        /// <summary>An untapped creature. There's no summoning sickness (§7.4).</summary>
        private bool CanAttack(CardInstance c) => Def(c).IsCreature && !c.Tapped;

        /// <summary>The attack token (§6.1): only the round leader may attack in the round.</summary>
        private bool HasAttackToken(PlayerState p) => p.Id == RoundLeader().Id;

        /// <summary>Untapped creatures can block, even the turn they arrive (§7.4). Blocking doesn't tap.</summary>
        private bool CanBlock(CardInstance c) =>
            Def(c).IsCreature && !c.Tapped && !Stats(c).Has(Keyword.CantBlock);

        /// <summary>Can this creature block one more attacker? One, plus "can block an additional creature" (Retired Champion).</summary>
        private bool HasBlockLeft(CardInstance c)
        {
            int blocks = 0;
            foreach (var b in S.Combat.Blocks) if (b.Blocker == c.Id) blocks++;
            return blocks < 1 + Def(c).ExtraBlocks;
        }

        /// <summary>Flying can only be blocked by Flying or Reach (§11).</summary>
        private bool CanBlockAttacker(CardInstance blocker, CardInstance attacker)
        {
            if (!Stats(attacker).Has(Keyword.Flying)) return true;
            var b = Stats(blocker);
            return b.Has(Keyword.Flying) || b.Has(Keyword.Reach);
        }

        private bool HasPossibleAttacker(PlayerState p)
        {
            foreach (var c in p.Battlefield)
                if (CanAttack(c)) return true;
            return false;
        }

        private void DeclareAttacker(PlayerId player, ObjectId attackerId, PlayerId defender)
        {
            var attacker = S.FindOnBattlefield(attackerId);
            attacker.Tapped = true; // no Vigilance yet
            S.Combat.Attacks.Add(new AttackDeclaration { Attacker = attackerId, Defender = defender });
            Emit(new AttackerDeclaredEvent { Attacker = attackerId, Defender = defender });
            QueueTriggers(attacker, TriggerEvent.Attacks, defender); // "the defending player" (Grubby Pickpocket)
        }

        private void FinishAttacks()
        {
            S.Pending = null;
            // "Whenever you attack with three or more creatures" (Rally Drummer).
            int attackers = S.Combat?.Attacks.Count ?? 0;
            if (attackers > 0)
                QueueWatcherTriggers(TriggerEvent.PlayerAttacks, S.ActivePlayer, (t, _) => attackers >= t.MinAmount, attackers);
            GivePriority(S.ActivePlayer);
        }

        /// <summary>
        /// Defending players declare blockers one at a time, in turn order (§13: each defender
        /// only blocks attackers that are attacking them).
        /// </summary>
        private void AskNextDefenderForBlockers()
        {
            foreach (var defender in S.LivingPlayersFrom(S.ActivePlayer))
            {
                if (defender.Id == S.ActivePlayer || S.Combat.DoneBlocking.Contains(defender.Id)) continue;
                if (!HasPossibleBlock(defender))
                {
                    S.Combat.DoneBlocking.Add(defender.Id);
                    continue;
                }
                S.Pending = new PendingDecision { Kind = DecisionKind.DeclareBlockers, Player = defender.Id };
                return;
            }
            GivePriority(S.ActivePlayer);
        }

        private bool HasPossibleBlock(PlayerState defender)
        {
            foreach (var attack in S.Combat.Attacks)
            {
                if (attack.Defender != defender.Id) continue;
                var attacker = S.FindOnBattlefield(attack.Attacker);
                if (attacker == null) continue;
                foreach (var c in defender.Battlefield)
                    if (CanBlock(c) && CanBlockAttacker(c, attacker)) return true;
            }
            return false;
        }

        private void DeclareBlocker(ObjectId blocker, ObjectId attacker)
        {
            S.Combat.Blocks.Add(new BlockDeclaration { Blocker = blocker, Attacker = attacker });
            S.Combat.AttackOf(attacker).Blocked = true;
            Emit(new BlockerDeclaredEvent { Blocker = blocker, Attacker = attacker });
        }

        private void FinishBlocks(PlayerId player)
        {
            S.Pending = null;
            S.Combat.DoneBlocking.Add(player);
            AskNextDefenderForBlockers();
        }

        /// <summary>
        /// Start of the combat damage step (MTG 510.1): every creature that deals damage to several creatures
        /// (an attacker with several blockers, a blocker blocking several attackers) has its damage divided by
        /// its controller, the attacking player first, then the defenders in turn order. §7.2.6: any division
        /// is allowed. The game only asks when the creature can't give each of them lethal damage; otherwise
        /// <see cref="AutoSplit"/> does (that kills them all, and Trample sends the rest to the player).
        /// When every choice is made, all combat damage is dealt.
        /// </summary>
        private void AskNextDamageAssignment()
        {
            foreach (var (dealer, recipients) in DamageSplits())
            {
                if (S.Combat.Assignments.Exists(x => x.Dealer == dealer.Id)) continue;
                int power = Stats(dealer).Power;
                if (power <= 0 || power >= TotalLethal(recipients)) continue;
                S.Pending = new PendingDecision
                {
                    Kind = DecisionKind.AssignCombatDamage, Player = dealer.Controller, Card = dealer.Id, Count = power,
                    Choices = recipients.ConvertAll(r => r.Id),
                };
                return;
            }
            S.Pending = null;
            DealCombatDamage();
            GivePriority(S.ActivePlayer);
        }

        private void AnswerDamageAssignment(PlayerAction a)
        {
            // One step of the split (later recipients still -1): remember it and ask for the next recipient.
            if (Array.IndexOf(a.Division, -1) >= 0)
            {
                S.Pending.DamageSoFar = (int[])a.Division.Clone();
                return;
            }
            S.Combat.Assignments.Add(new DamageAssignment
            {
                Dealer = S.Pending.Card, Recipients = new List<ObjectId>(S.Pending.Choices), Amounts = (int[])a.Division.Clone(),
            });
            AskNextDamageAssignment();
        }

        /// <summary>
        /// Creatures that deal combat damage to two or more creatures, with those creatures in block order:
        /// attackers first, then blockers grouped by defending player in turn order.
        /// </summary>
        private List<(CardInstance dealer, List<CardInstance> recipients)> DamageSplits()
        {
            var result = new List<(CardInstance, List<CardInstance>)>();
            foreach (var attack in S.Combat.Attacks)
            {
                var attacker = S.FindOnBattlefield(attack.Attacker);
                if (attacker == null) continue;
                var blockers = BlockersOf(attacker.Id);
                if (blockers.Count >= 2) result.Add((attacker, blockers));
            }
            foreach (var defender in S.LivingPlayersFrom(S.ActivePlayer))
            {
                var done = new HashSet<ObjectId>();
                foreach (var block in S.Combat.Blocks)
                {
                    var blocker = S.FindOnBattlefield(block.Blocker);
                    if (blocker == null || blocker.Controller != defender.Id || !done.Add(blocker.Id)) continue;
                    var attackers = AttackersBlockedBy(blocker.Id);
                    if (attackers.Count >= 2) result.Add((blocker, attackers));
                }
            }
            return result;
        }

        private List<CardInstance> BlockersOf(ObjectId attacker)
        {
            var result = new List<CardInstance>();
            foreach (var block in S.Combat.Blocks)
            {
                if (block.Attacker != attacker) continue;
                var b = S.FindOnBattlefield(block.Blocker);
                if (b != null) result.Add(b);
            }
            return result;
        }

        private List<CardInstance> AttackersBlockedBy(ObjectId blocker)
        {
            var result = new List<CardInstance>();
            foreach (var block in S.Combat.Blocks)
            {
                if (block.Blocker != blocker) continue;
                var a = S.FindOnBattlefield(block.Attacker);
                if (a != null) result.Add(a);
            }
            return result;
        }

        /// <summary>Lethal damage = remaining Health, so earlier wounds count (§7.3). Prevention is ignored (MTG 702.19c).</summary>
        private int Lethal(CardInstance c) => System.Math.Max(0, Stats(c).RemainingHealth);

        private int TotalLethal(List<CardInstance> creatures)
        {
            int total = 0;
            foreach (var c in creatures) total += Lethal(c);
            return total;
        }

        /// <summary>
        /// The division nobody has to choose: lethal damage to each recipient in order, then the rest to
        /// <paramref name="trampleTo"/> (Trample) or onto the first recipient.
        /// </summary>
        private void AutoSplit(ObjectId dealer, int power, List<CardInstance> recipients, Target? trampleTo,
            List<(ObjectId source, Target target, int amount)> hits)
        {
            int left = power;
            foreach (var r in recipients)
            {
                int dmg = System.Math.Min(Lethal(r), left);
                if (dmg > 0) hits.Add((dealer, Target.ForObject(r.Id), dmg));
                left -= dmg;
            }
            if (left <= 0) return;
            if (trampleTo.HasValue) hits.Add((dealer, trampleTo.Value, left));
            else hits.Add((dealer, Target.ForObject(recipients[0].Id), left));
        }

        /// <summary>Use the division the controller chose, if there was a choice. Returns false if there wasn't.</summary>
        private bool UseChosenSplit(ObjectId dealer, List<(ObjectId source, Target target, int amount)> hits)
        {
            var chosen = S.Combat.Assignments.Find(x => x.Dealer == dealer);
            if (chosen == null) return false;
            for (int i = 0; i < chosen.Recipients.Count; i++)
                if (chosen.Amounts[i] > 0 && S.FindOnBattlefield(chosen.Recipients[i]) != null)
                    hits.Add((dealer, Target.ForObject(chosen.Recipients[i]), chosen.Amounts[i]));
            return true;
        }

        /// <summary>
        /// The combat damage split (§7.2.6: "however they like"), asked **one recipient at a time**: the legal actions are
        /// the amounts for the next recipient, the earlier ones fixed, later ones -1. With two recipients left, the
        /// amount also fixes the last one, so that step is a whole split. A recipient never gets more than lethal (past
        /// lethal changes nothing). Listing every whole split at once exploded: 10 power into 10 blockers is ~40,000
        /// splits, which froze the bots and would have drawn 40,000 buttons (bug-hunt sims 2026-10-10). Now a step has at
        /// most power + 1 choices. The game only asks when the damage can't kill them all, so a split always exists.
        /// </summary>
        private List<int[]> DamageSplitSteps()
        {
            var d = S.Pending;
            int total = d.Count;
            var caps = DamageCaps();
            int n = caps.Length;
            var done = d.DamageSoFar ?? Enumerable.Repeat(-1, n).ToArray();
            int index = Array.IndexOf(done, -1);
            int used = 0;
            for (int i = 0; i < index; i++) used += done[i];
            int left = total - used;
            int restCap = 0; // the most the recipients after this one can take
            for (int i = index + 1; i < n; i++) restCap += caps[i];
            var result = new List<int[]>();
            if (index < 0) return result;
            for (int amount = Math.Min(left, caps[index]); amount >= 0; amount--)
            {
                if (left - amount > restCap) break;
                var step = (int[])done.Clone();
                step[index] = amount;
                if (index == n - 2) step[n - 1] = left - amount; // the last one takes the rest
                else if (index == n - 1 && amount != left) continue;
                result.Add(step);
            }
            return result;
        }

        /// <summary>The most each recipient can usefully take: its lethal damage (capped at the damage dealt).</summary>
        private int[] DamageCaps()
        {
            var d = S.Pending;
            return d.Choices.Select(id =>
            {
                var c = S.FindOnBattlefield(id);
                return c != null ? Math.Min(d.Count, Lethal(c)) : d.Count;
            }).ToArray();
        }

        /// <summary>
        /// A whole split given in one action (tests, replays of older bug reports): every recipient gets 0 or more, the
        /// total is the damage dealt, nobody gets more than lethal, and it agrees with the steps already chosen.
        /// </summary>
        internal bool IsWholeDamageSplit(PlayerAction a)
        {
            var d = S.Pending;
            if (a.Kind != ActionKind.AssignCombatDamage || d == null || d.Kind != DecisionKind.AssignCombatDamage || a.Player != d.Player) return false;
            if (a.Division == null || a.Division.Length != d.Choices.Count || a.Division.Sum() != d.Count) return false;
            var caps = DamageCaps();
            for (int i = 0; i < caps.Length; i++)
            {
                if (a.Division[i] < 0 || a.Division[i] > caps[i]) return false;
                if (d.DamageSoFar != null && d.DamageSoFar[i] >= 0 && d.DamageSoFar[i] != a.Division[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// All combat damage is dealt at the same time (§7.2.5), as divided in <see cref="AskNextDamageAssignment"/>.
        /// </summary>
        private void DealCombatDamage()
        {
            var hits = new List<(ObjectId source, Target target, int amount)>();

            foreach (var attack in S.Combat.Attacks)
            {
                var attacker = S.FindOnBattlefield(attack.Attacker);
                if (attacker == null) continue;
                var stats = Stats(attacker);
                int power = stats.Power;
                if (power <= 0) continue;
                var defenderTarget = Target.ForPlayer(attack.Defender);

                if (!attack.Blocked)
                {
                    hits.Add((attacker.Id, defenderTarget, power));
                    continue;
                }

                var blockers = BlockersOf(attacker.Id);
                if (blockers.Count == 0)
                {
                    // MTG 509.1h / 702.19e: blocked, but all blockers are gone.
                    if (stats.Has(Keyword.Trample)) hits.Add((attacker.Id, defenderTarget, power));
                    continue;
                }
                if (!UseChosenSplit(attacker.Id, hits))
                    AutoSplit(attacker.Id, power, blockers, stats.Has(Keyword.Trample) ? defenderTarget : (Target?)null, hits);
            }

            // A blocker that blocks several attackers (Retired Champion) divides its damage like an attacker.
            var blockersDone = new HashSet<ObjectId>();
            foreach (var block in S.Combat.Blocks)
            {
                if (!blockersDone.Add(block.Blocker)) continue;
                var blocker = S.FindOnBattlefield(block.Blocker);
                if (blocker == null) continue;
                int power = Stats(blocker).Power;
                if (power <= 0) continue;
                var blocked = AttackersBlockedBy(blocker.Id);
                if (blocked.Count == 0) continue;
                if (!UseChosenSplit(blocker.Id, hits)) AutoSplit(blocker.Id, power, blocked, null, hits);
            }

            var damagedBy = new List<(ObjectId source, ObjectId creature)>();
            foreach (var (source, target, amount) in hits)
                if (DealDamage(source, target, amount, true) > 0 && !target.IsPlayer)
                    damagedBy.Add((source, target.Object));

            // "Whenever this destroys a creature in combat" (Champion's Belt): it dealt combat damage to a
            // creature that now has lethal damage; state-based actions will destroy it.
            foreach (var (source, creatureId) in damagedBy)
            {
                var creature = S.FindOnBattlefield(creatureId);
                var dealer = S.FindOnBattlefield(source);
                if (creature != null && dealer != null && Stats(creature).RemainingHealth <= 0)
                    QueueTriggers(dealer, TriggerEvent.DestroysCreatureInCombat);
            }
        }
    }
}
