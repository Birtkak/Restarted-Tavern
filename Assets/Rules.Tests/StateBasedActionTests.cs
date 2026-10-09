using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>MTG CR 704 as used by GAME_DESIGN §12 and §10.</summary>
    public class StateBasedActionTests
    {
        [Test]
        public void DrawingFromAnEmptyDeck_Loses()
        {
            var g = TestGame.AtFirstMainPhase();
            var first = g.Active;
            var second = g.Other;
            g.P(second).Deck.Clear();
            g.PassUntil(s => s.IsGameOver);
            CollectionAssert.AreEqual(new[] { first }, g.State.Winners);
            Assert.AreEqual("drew from empty deck", g.Events.OfType<PlayerLostEvent>().Single().Reason);
        }

        [Test]
        public void ZeroLife_Loses()
        {
            var g = TestGame.AtFirstMainPhase();
            var attacker = g.AddToBattlefield(g.Active, "hired_sellsword");
            g.P(g.Other).Life = 2;
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            g.Do(PlayerAction.Attack(g.Active, attacker.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            g.PassUntil(s => s.IsGameOver);
            CollectionAssert.AreEqual(new[] { g.Active }, g.State.Winners);
            Assert.AreEqual(Step.GameOver, g.State.Step);
            Assert.IsEmpty(g.Legal(g.Active));
        }

        [Test]
        public void LegendaryRule_KeepsOnlyOneWithTheSameName()
        {
            var legend = new CardDefinition
            {
                Id = "test_legend", Name = "Test Legend", Type = CardType.Creature, Cost = 3, Power = 3, Health = 3,
                Rarity = Rarity.Legendary,
            };
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { legend });
            g.AddToBattlefield(g.Active, "test_legend");
            var newer = g.AddToBattlefield(g.Active, "test_legend");
            g.AddToBattlefield(g.Other, "test_legend"); // other controllers don't count
            g.PassRound(); // moving to the next step checks state-based actions

            Assert.AreEqual(1, g.P(g.Active).Battlefield.Count(c => c.DefinitionId == "test_legend"));
            Assert.AreEqual(newer.Id, g.OnBattlefield(g.Active, "test_legend").Id);
            Assert.AreEqual(1, g.P(g.Other).Battlefield.Count(c => c.DefinitionId == "test_legend"));
        }

        [Test]
        public void PlayerView_HidesOpponentsHandAndAllDecks()
        {
            var g = TestGame.AtFirstMainPhase();
            var view = g.State.CreateViewFor(g.Active);
            Assert.IsTrue(view.GetPlayer(g.Other).Hand.All(c => c.IsHidden));
            Assert.IsTrue(view.GetPlayer(g.Active).Hand.All(c => !c.IsHidden));
            Assert.IsTrue(view.Players.All(p => p.Deck.All(c => c.IsHidden)));
            Assert.IsNull(view.Rng);
            Assert.IsFalse(g.State.GetPlayer(g.Other).Hand.Any(c => c.IsHidden), "the real state is untouched");
        }
    }
}
