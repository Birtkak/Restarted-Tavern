using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>Combat (GAME_DESIGN §7.2, MTG CR 506–510).</summary>
    internal sealed partial class GameRunner
    {
        /// <summary>Untapped, and not summoning sick unless it has Haste (§7.4).</summary>
        private bool CanAttack(CardInstance c)
        {
            if (!Def(c).IsCreature || c.Tapped) return false;
            return !c.SummoningSick || S.Format.NoSummoningSickness || Stats(c).Has(Keyword.Haste);
        }

        /// <summary>FormatConfig.AttackToken: only the player who started the round may attack in it.</summary>
        private bool HasAttackToken(PlayerState p) => !S.Format.AttackToken || p.Id == RoundLeader().Id;

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
        /// Every way to divide <paramref name="total"/> combat damage among <paramref name="recipients"/>
        /// creatures, 0 allowed (§7.2.6: "however they like").
        /// </summary>
        private static List<int[]> CombatDivisions(int total, int recipients)
        {
            var result = new List<int[]>();
            var parts = new int[recipients];
            Split(0, total);
            return result;

            void Split(int index, int left)
            {
                if (index == recipients - 1)
                {
                    parts[index] = left;
                    result.Add((int[])parts.Clone());
                    return;
                }
                for (int n = left; n >= 0; n--)
                {
                    parts[index] = n;
                    Split(index + 1, left - n);
                }
            }
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
