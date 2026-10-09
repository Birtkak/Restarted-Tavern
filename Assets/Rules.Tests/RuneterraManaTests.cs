using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>The Runeterra-style mana experiment (FormatConfig.Runeterra): round pool, rotating leader, attack token, Gold first.</summary>
    public class RuneterraManaTests
    {
        private static TestGame Game(bool summoningSickness = true)
        {
            var f = FormatConfig.Runeterra(3);
            f.NoSummoningSickness = !summoningSickness;
            return TestGame.AtFirstMainPhase(format: f);
        }

        /// <summary>The seats of the next <paramref name="count"/> turns, starting with the current one.</summary>
        private static List<PlayerId> TurnOrder(TestGame g, int count)
        {
            var order = new List<PlayerId> { g.Active };
            int turn = g.State.TurnNumber;
            while (order.Count < count)
            {
                g.PassUntil(s => s.TurnNumber > turn && (s.PriorityPlayer.HasValue || s.Pending != null));
                turn = g.State.TurnNumber;
                order.Add(g.Active);
            }
            return order;
        }

        [Test]
        public void RoundLeaderRotates_ABBA()
        {
            var g = Game();
            var a = g.Active;
            var b = g.Other;
            CollectionAssert.AreEqual(new[] { a, b, b, a, a, b }, TurnOrder(g, 6));
        }

        [Test]
        public void EveryoneRefillsAtRoundStart_AndBanksGoldAtRoundEnd()
        {
            var g = Game();
            var a = g.Active;
            var b = g.Other;
            Assert.AreEqual(1, g.P(a).Mana);
            Assert.AreEqual(1, g.P(b).Mana, "the second player has this round's mana during the first player's turn");

            g.PassUntil(s => s.ActivePlayer == b && s.Step == Step.Main1);
            Assert.AreEqual(1, g.P(a).Mana, "unspent mana stays until the round ends");
            Assert.AreEqual(0, g.P(a).Gold);

            g.PassUntil(s => s.TurnNumber == 3 && s.Step == Step.Main1);
            Assert.AreEqual(b, g.Active, "round 2 starts with the other player");
            Assert.AreEqual(1, g.P(a).Gold, "round 1's unspent mana became Gold");
            Assert.AreEqual(1, g.P(b).Gold);
            Assert.AreEqual(2, g.P(a).MaxMana);
            Assert.AreEqual(2, g.P(b).Mana);
        }

        [Test]
        public void OnlyTheRoundLeaderMayAttack()
        {
            var g = Game();
            var a = g.Active;
            var b = g.Other;
            g.AddToBattlefield(a, "hired_sellsword");
            g.AddToBattlefield(b, "hired_sellsword");
            g.PassUntil(s => s.Step == Step.DeclareAttackers);
            Assert.AreEqual(DecisionKind.DeclareAttackers, g.State.Pending?.Kind, "round 1: the first player has the attack token");
            g.Do(PlayerAction.FinishAttacks(a));

            g.PassUntil(s => s.ActivePlayer == b && s.Step >= Step.BeginCombat);
            var steps = new List<DecisionKind?>();
            g.PassUntil(s =>
            {
                if (s.ActivePlayer == b && s.TurnNumber == 2) steps.Add(s.Pending?.Kind);
                return s.TurnNumber == 3;
            });
            CollectionAssert.DoesNotContain(steps, DecisionKind.DeclareAttackers, "the second player's turn in round 1 has no attack");

            g.PassUntil(s => s.Step == Step.DeclareAttackers && s.Pending != null);
            Assert.AreEqual(b, g.State.Pending.Player, "round 2: the token passed to the other player");
        }

        [Test]
        public void SpellsPayGoldFirst()
        {
            var g = Game();
            var me = g.Active;
            g.P(me).Gold = 1;
            g.P(me).Mana = 2;
            var shock = g.AddToHand(me, "static_shock"); // Instant, cost 1
            g.AddToBattlefield(g.Other, "hired_sellsword"); // something to target
            g.Do(g.Legal(me).First(a => a.Kind == ActionKind.PlayCard && a.Card == shock.Id));
            Assert.AreEqual(0, g.P(me).Gold, "Gold (spell mana) is spent first");
            Assert.AreEqual(2, g.P(me).Mana);
        }

        [Test]
        public void SummoningSickness_CanBeTurnedOff()
        {
            var g = Game(summoningSickness: false);
            var me = g.Active;
            g.SetMana(me, 1);
            var drone = g.AddToHand(me, "goober_rascal"); // cost 1
            g.Do(g.Legal(me).First(a => a.Kind == ActionKind.PlayCard && a.Card == drone.Id));
            g.PassRound(); // resolve it
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            Assert.IsTrue(g.Legal(me).Any(a => a.Kind == ActionKind.DeclareAttacker), "it can attack the turn it arrives");
        }
    }
}
