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
            return !c.SummoningSick || Stats(c).Has(Keyword.Haste);
        }

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
        /// All combat damage is dealt at the same time (§7.2.5).
        /// Several blockers: the attacker assigns lethal damage to each blocker in block order
        /// (lethal = its <b>remaining</b> Health, so earlier wounds count); what's left goes to the
        /// defending player with Trample, otherwise onto the first blocker.
        /// TODO: §7.2.6 says the attacker's controller divides the damage freely — make that a choice.
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

                var blockers = new List<CardInstance>();
                foreach (var block in S.Combat.Blocks)
                {
                    if (block.Attacker != attacker.Id) continue;
                    var b = S.FindOnBattlefield(block.Blocker);
                    if (b != null) blockers.Add(b);
                }

                if (blockers.Count == 0)
                {
                    // MTG 509.1h / 702.19e: blocked, but all blockers are gone.
                    if (stats.Has(Keyword.Trample)) hits.Add((attacker.Id, defenderTarget, power));
                    continue;
                }

                int left = power;
                foreach (var b in blockers)
                {
                    int lethal = System.Math.Max(0, Stats(b).RemainingHealth);
                    int dmg = System.Math.Min(lethal, left);
                    if (dmg > 0) hits.Add((attacker.Id, Target.ForObject(b.Id), dmg));
                    left -= dmg;
                }
                if (left > 0)
                {
                    if (stats.Has(Keyword.Trample)) hits.Add((attacker.Id, defenderTarget, left));
                    else hits.Add((attacker.Id, Target.ForObject(blockers[0].Id), left));
                }
            }

            // A blocker that blocks several attackers (Retired Champion) splits its damage like an attacker:
            // lethal to each in block order, the rest onto the first one.
            var blockersDone = new HashSet<ObjectId>();
            foreach (var block in S.Combat.Blocks)
            {
                if (!blockersDone.Add(block.Blocker)) continue;
                var blocker = S.FindOnBattlefield(block.Blocker);
                if (blocker == null) continue;
                int left = Stats(blocker).Power;
                if (left <= 0) continue;
                var blocked = new List<CardInstance>();
                foreach (var b in S.Combat.Blocks)
                {
                    if (b.Blocker != blocker.Id) continue;
                    var attacker = S.FindOnBattlefield(b.Attacker);
                    if (attacker != null) blocked.Add(attacker);
                }
                if (blocked.Count == 0) continue;
                for (int i = 0; i < blocked.Count && left > 0; i++)
                {
                    int dmg = i == blocked.Count - 1 ? left : System.Math.Min(left, System.Math.Max(0, Stats(blocked[i]).RemainingHealth));
                    if (dmg > 0) hits.Add((blocker.Id, Target.ForObject(blocked[i].Id), dmg));
                    left -= dmg;
                }
                if (left > 0) hits.Add((blocker.Id, Target.ForObject(blocked[0].Id), left));
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
