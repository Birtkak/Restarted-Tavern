using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>GAME_DESIGN §7: blocking, permanent damage, Trample, Flying/Reach, no summoning sickness.</summary>
    public class CombatTests
    {
        private static CardDefinition Bat => new CardDefinition
        {
            Id = "test_bat", Name = "Test Bat", Type = CardType.Creature, Cost = 1, Power = 1, Health = 1,
            Keywords = Keyword.Flying,
        };

        private static CardDefinition Leech => new CardDefinition
        {
            Id = "test_leech", Name = "Test Leech", Type = CardType.Creature, Cost = 2, Power = 3, Health = 3,
            Keywords = Keyword.Lifelink,
        };

        private static void ToDeclareAttackers(TestGame g) =>
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);

        private static void ToDeclareBlockers(TestGame g) =>
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareBlockers);

        [Test]
        public void UnblockedAttacker_DamagesTheDefendingPlayer()
        {
            var g = TestGame.AtFirstMainPhase();
            var attacker = g.AddToBattlefield(g.Active, "hired_sellsword");
            ToDeclareAttackers(g);
            g.Do(PlayerAction.Attack(g.Active, attacker.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            Assert.IsTrue(attacker.Tapped);
            g.PassUntil(s => s.Step == Step.Main2);
            Assert.AreEqual(28, g.P(g.Other).Life);
        }

        [Test]
        public void Damage_IsPermanent_AcrossTurns()
        {
            var g = TestGame.AtFirstMainPhase();
            var attacker = g.AddToBattlefield(g.Active, "hired_sellsword"); // 2/3
            var blocker = g.AddToBattlefield(g.Other, "tavern_bouncer");   // 2/5
            ToDeclareAttackers(g);
            g.Do(PlayerAction.Attack(g.Active, attacker.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            ToDeclareBlockers(g);
            g.Do(PlayerAction.Block(g.Other, blocker.Id, attacker.Id));
            g.Do(PlayerAction.FinishBlocks(g.Other));
            g.PassUntil(s => s.Step == Step.Main2);
            Assert.AreEqual(2, attacker.Damage);
            Assert.AreEqual(2, blocker.Damage);

            var first = g.Active;
            g.PassToStep(Step.Main1, g.Other);
            g.PassToStep(Step.Main1, first);
            Assert.AreEqual(2, attacker.Damage, "no healing at end of turn (§7.3)");
            Assert.AreEqual(1, g.Stats(attacker).RemainingHealth);
            Assert.AreEqual(3, g.Stats(blocker).RemainingHealth);
        }

        [Test]
        public void Trample_UsesTheBlockersRemainingHealth()
        {
            var g = TestGame.AtFirstMainPhase();
            var hog = g.AddToBattlefield(g.Active, "hog_rider");                      // 3/3 Trample
            var wounded = g.AddToBattlefield(g.Other, "hired_sellsword", damage: 2);  // 2/3, 1 left
            ToDeclareAttackers(g);
            g.Do(PlayerAction.Attack(g.Active, hog.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            ToDeclareBlockers(g);
            g.Do(PlayerAction.Block(g.Other, wounded.Id, hog.Id));
            g.Do(PlayerAction.FinishBlocks(g.Other));
            g.PassUntil(s => s.Step == Step.Main2);

            Assert.AreEqual(28, g.P(g.Other).Life, "1 to the blocker, 2 tramples over");
            Assert.IsNull(g.OnBattlefield(g.Other, "hired_sellsword"), "blocker died");
            Assert.AreEqual(2, hog.Damage);
        }

        [Test]
        public void Flying_CanOnlyBeBlockedByFlyingOrReach()
        {
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { Bat });
            var bat = g.AddToBattlefield(g.Active, "test_bat");
            var ground = g.AddToBattlefield(g.Other, "hired_sellsword");
            var spider = g.AddToBattlefield(g.Other, "vine_spider");
            ToDeclareAttackers(g);
            g.Do(PlayerAction.Attack(g.Active, bat.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            ToDeclareBlockers(g);
            var blocks = g.Legal(g.Other).Where(a => a.Kind == ActionKind.DeclareBlocker).Select(a => a.Card).ToList();
            CollectionAssert.AreEqual(new[] { spider.Id }, blocks);
            Assert.IsFalse(blocks.Contains(ground.Id));
        }

        [Test]
        public void NoSummoningSickness_NewCreaturesCanAttack()
        {
            var g = TestGame.AtFirstMainPhase();
            g.SetMana(g.Active, 4);
            var sword = g.AddToHand(g.Active, "hired_sellsword");
            g.Do(PlayerAction.Play(g.Active, sword.Id));
            g.PassRound();

            ToDeclareAttackers(g);
            var attackers = g.Legal(g.Active).Where(a => a.Kind == ActionKind.DeclareAttacker)
                .Select(a => g.State.FindObject(a.Card).DefinitionId).ToList();
            CollectionAssert.AreEqual(new[] { "hired_sellsword" }, attackers, "§7.4: no summoning sickness");
        }

        [Test]
        public void CantBlock_IsRespected()
        {
            var g = TestGame.AtFirstMainPhase();
            var attacker = g.AddToBattlefield(g.Active, "hired_sellsword");
            g.AddToBattlefield(g.Other, "goober_rascal");
            ToDeclareAttackers(g);
            g.Do(PlayerAction.Attack(g.Active, attacker.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            g.PassUntil(s => s.Step == Step.Main2);
            Assert.AreEqual(28, g.P(g.Other).Life, "rascal couldn't block, so no blocker step for it");
        }

        [Test]
        public void Lifelink_HealsItsController_UpToStartingLife()
        {
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { Leech });
            var leech = g.AddToBattlefield(g.Active, "test_leech");
            g.P(g.Active).Life = 29;
            ToDeclareAttackers(g);
            g.Do(PlayerAction.Attack(g.Active, leech.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            g.PassUntil(s => s.Step == Step.Main2);
            Assert.AreEqual(27, g.P(g.Other).Life);
            Assert.AreEqual(30, g.P(g.Active).Life, "healing can't go above starting life");
        }

        [Test]
        public void MultipleBlockers_AttackerDividesTheDamage()
        {
            var g = TestGame.AtFirstMainPhase();
            var fighter = g.AddToBattlefield(g.Active, "pit_fighter"); // 4/4 Trample
            var a = g.AddToBattlefield(g.Other, "goober_rascal");       // can't block
            var b = g.AddToBattlefield(g.Other, "hired_sellsword");     // 2/3
            var c = g.AddToBattlefield(g.Other, "vine_spider");         // 2/3
            ToDeclareAttackers(g);
            g.Do(PlayerAction.Attack(g.Active, fighter.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            ToDeclareBlockers(g);
            g.Do(PlayerAction.Block(g.Other, b.Id, fighter.Id));
            g.Do(PlayerAction.Block(g.Other, c.Id, fighter.Id));
            g.Do(PlayerAction.FinishBlocks(g.Other));
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.AssignCombatDamage);
            Assert.AreEqual(g.Active, g.State.Pending.Player, "4 damage can't kill both (6 Health): the attacker divides it");
            g.Do(PlayerAction.AssignDamage(g.Active, new[] { 3, 1 }));
            g.PassUntil(s => s.Step == Step.Main2);

            Assert.IsNull(g.State.FindOnBattlefield(b.Id), "first blocker got lethal (3)");
            Assert.AreEqual(1, g.State.FindOnBattlefield(c.Id).Damage, "second blocker got the remaining 1");
            Assert.AreEqual(30, g.P(g.Other).Life);
            Assert.IsNull(g.State.FindOnBattlefield(fighter.Id), "took 4 from two blockers");
            Assert.IsNotNull(g.State.FindOnBattlefield(a.Id));
        }
    }
}
