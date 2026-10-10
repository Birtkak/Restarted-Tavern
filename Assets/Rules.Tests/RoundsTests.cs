using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// The round structure (GAME_DESIGN §6): Legends of Runeterra rounds. Everyone refills, untaps
    /// and draws when a round starts; players then alternate actions; the round leader holds the attack token; passing
    /// in a row ends the round; no summoning sickness.
    /// </summary>
    public class RoundsTests
    {
        private static TestGame Game() => TestGame.AtFirstMainPhase();

        private static PlayerAction PlaySellsword(TestGame g, PlayerId p) =>
            g.Legal(p).First(a => a.Kind == ActionKind.PlayCard && g.State.FindObject(a.Card).DefinitionId == "hired_sellsword");

        /// <summary>Everyone passes until the Chain is empty again (without handing the action back).</summary>
        private static void Resolve(TestGame g)
        {
            while (g.State.Chain.Count > 0) g.PassPriority();
        }

        [Test]
        public void RoundStart_EveryoneDrawsAndRefills_LeaderActsFirst()
        {
            var g = Game();
            var a = g.Active;
            var b = g.Other;
            Assert.AreEqual(8, g.P(a).Hand.Count, "the leader draws in round 1 too");
            Assert.AreEqual(8, g.P(b).Hand.Count, "everyone draws when a round starts");
            Assert.AreEqual(1, g.P(a).Mana);
            Assert.AreEqual(1, g.P(b).Mana);
            Assert.AreEqual(a, g.State.PriorityPlayer);
            Assert.IsTrue(g.Legal(b).Count == 0, "the other player waits for the action");
        }

        [Test]
        public void AfterAnActionResolves_TheNextPlayerHasTheAction()
        {
            var g = Game();
            var a = g.Active;
            var b = g.Other;
            g.SetMana(a, 2);
            g.SetMana(b, 2);
            g.Do(PlaySellsword(g, a));
            Assert.AreEqual(a, g.State.PriorityPlayer, "MTG 117.3c: the caster gets priority first");
            Resolve(g);

            Assert.AreEqual(b, g.State.ActivePlayer, "the action passed to the other player");
            Assert.AreEqual(b, g.State.PriorityPlayer);
            Assert.AreEqual(Step.Main1, g.State.Step);
            Assert.IsTrue(g.Legal(b).Any(x => x.Kind == ActionKind.PlayCard), "a creature on someone else's action: sorcery speed is your action");
            Assert.IsFalse(g.Legal(b).Any(x => x.Kind == ActionKind.GoToCombat), "only the attack token holder attacks");
        }

        [Test]
        public void AResponse_DoesNotUseAnAction()
        {
            var g = Game();
            var a = g.Active;
            var b = g.Other;
            g.SetMana(a, 2);
            g.SetMana(b, 2);
            g.AddToBattlefield(b, "hired_sellsword", damage: 2);
            var remedy = g.AddToHand(b, "jungle_remedy");
            g.Do(PlaySellsword(g, a));
            g.PassPriority(); // a passes with the Sellsword on the Chain
            g.Do(g.Legal(b).First(x => x.Card == remedy.Id && !x.Invest));
            Resolve(g);

            Assert.AreEqual(b, g.State.ActivePlayer, "a's action ended; b's response didn't use b's action");
            Assert.AreEqual(b, g.State.PriorityPlayer);
        }

        [Test]
        public void PassingHandsTheActionOn_TwoPassesEndTheRound()
        {
            var g = Game();
            var a = g.Active;
            var b = g.Other;
            g.PassPriority();
            Assert.AreEqual(b, g.State.ActivePlayer, "passing hands the action on");
            Assert.AreEqual(Step.Main1, g.State.Step);
            g.SetMana(b, 2);
            g.Do(PlaySellsword(g, b));
            Resolve(g);
            Assert.AreEqual(a, g.State.ActivePlayer, "b acted, so the round goes on");

            g.PassUntil(s => s.RoundNumber == 2 && s.Step == Step.Main1);
            Assert.AreEqual(b, g.State.ActivePlayer, "round 2: the other player leads");
            Assert.AreEqual(2, g.P(a).Mana);
            Assert.AreEqual(1, g.P(a).Gold, "a's unspent round-1 mana became Gold");
            Assert.AreEqual(0, g.P(b).Gold, "b spent everything");
        }

        [Test]
        public void AttackIsAnAction_LeaderOnly_OncePerRound_NoSummoningSickness()
        {
            var g = Game();
            var a = g.Active;
            var b = g.Other;
            g.SetMana(a, 2);
            g.Do(PlaySellsword(g, a));
            Resolve(g);
            g.PassPriority(); // b passes the action back

            Assert.AreEqual(a, g.State.ActivePlayer);
            var attack = g.Legal(a).FirstOrDefault(x => x.Kind == ActionKind.GoToCombat);
            Assert.IsNotNull(attack, "a creature can attack the round it arrives (no summoning sickness)");
            g.Do(attack);
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            var sellsword = g.OnBattlefield(a, "hired_sellsword");
            g.Do(PlayerAction.Attack(a, sellsword.Id, b));
            g.Do(PlayerAction.FinishAttacks(a));
            g.PassUntil(s => s.Step == Step.Main1);

            Assert.AreEqual(28, g.P(b).Life);
            Assert.AreEqual(b, g.State.ActivePlayer, "after combat the defender has the action");
            g.PassPriority();
            Assert.AreEqual(a, g.State.ActivePlayer);
            Assert.IsFalse(g.Legal(a).Any(x => x.Kind == ActionKind.GoToCombat), "one attack per round");
        }

        [Test]
        public void TheAttackTokenPassesEachRound()
        {
            var g = Game();
            var a = g.Active;
            var b = g.Other;
            g.AddToBattlefield(a, "hired_sellsword");
            g.AddToBattlefield(b, "hired_sellsword");
            Assert.IsTrue(g.Legal(a).Any(x => x.Kind == ActionKind.GoToCombat), "round 1: the leader has the token");
            g.PassPriority();
            Assert.IsFalse(g.Legal(b).Any(x => x.Kind == ActionKind.GoToCombat));

            g.PassUntil(s => s.RoundNumber == 2 && s.Step == Step.Main1);
            Assert.AreEqual(b, g.State.ActivePlayer, "round 2: the other player leads");
            Assert.IsTrue(g.Legal(b).Any(x => x.Kind == ActionKind.GoToCombat), "and holds the token");
            g.PassPriority();
            Assert.IsFalse(g.Legal(a).Any(x => x.Kind == ActionKind.GoToCombat));
        }

        [Test]
        public void CleanupMakesEveryoneDiscardToSeven()
        {
            var g = Game();
            var a = g.Active;
            var b = g.Other;
            g.AddToHand(b, "hired_sellsword");
            Assert.AreEqual(8, g.P(a).Hand.Count);
            Assert.AreEqual(9, g.P(b).Hand.Count);
            g.PassUntil(s => s.RoundNumber == 2 && s.Step == Step.Main1);
            Assert.AreEqual(8, g.P(a).Hand.Count, "7 after cleanup, then the round-2 draw");
            Assert.AreEqual(8, g.P(b).Hand.Count);
        }
    }
}
