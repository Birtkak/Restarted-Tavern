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
            QueueTriggers(attacker, TriggerEvent.Attacks);
        }

        private void FinishAttacks()
        {
            S.Pending = null;
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

            foreach (var block in S.Combat.Blocks)
            {
                var blocker = S.FindOnBattlefield(block.Blocker);
                var attacker = S.FindOnBattlefield(block.Attacker);
                if (blocker == null || attacker == null) continue;
                int power = Stats(blocker).Power;
                if (power > 0) hits.Add((blocker.Id, Target.ForObject(attacker.Id), power));
            }

            foreach (var (source, target, amount) in hits)
                DealDamage(source, target, amount, true);
        }
    }
}
