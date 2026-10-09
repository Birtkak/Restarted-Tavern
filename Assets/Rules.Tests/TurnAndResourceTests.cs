using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>GAME_DESIGN §3 (setup), §5 (mana and Gold), §6 (turn structure).</summary>
    public class TurnAndResourceTests
    {
        [Test]
        public void Setup_StartingValues()
        {
            var g = TestGame.AtFirstMainPhase();
            Assert.AreEqual(30, g.P(g.Active).Life);
            Assert.AreEqual(30, g.P(g.Other).Life);
            Assert.AreEqual(7, g.P(g.Active).Hand.Count, "first player skips the turn-1 draw");
            Assert.AreEqual(7, g.P(g.Other).Hand.Count);
            Assert.AreEqual(0, g.P(g.Active).Gold);
            Assert.AreEqual(1, g.P(g.Other).Gold, "second player starts with 1 Gold");
            Assert.AreEqual(1, g.P(g.Active).MaxMana);
            Assert.AreEqual(1, g.P(g.Active).Mana);
        }

        [Test]
        public void Mulligan_London_PutsCardsOnTheBottom()
        {
            var engine = new GameEngine(PrototypeCards.CreateDatabase());
            var format = FormatConfig.Standard();
            format.EnforceDeckRules = false;
            var deck = Enumerable.Repeat("hired_sellsword", 20).ToList();
            var state = engine.CreateGame(format, new[] { new PlayerSetup { Deck = deck }, new PlayerSetup { Deck = deck } }, 7);

            var first = state.Pending.Player;
            engine.Apply(state, PlayerAction.Mulligan(first));
            Assert.AreEqual(7, state.GetPlayer(first).Hand.Count, "London: always draw 7");
            engine.Apply(state, PlayerAction.Keep(first));
            Assert.AreEqual(DecisionKind.BottomCards, state.Pending.Kind);
            engine.Apply(state, PlayerAction.BottomCard(first, state.GetPlayer(first).Hand[0].Id));
            Assert.AreEqual(6, state.GetPlayer(first).Hand.Count);
            Assert.AreEqual(20 - 6, state.GetPlayer(first).Deck.Count);
            Assert.AreEqual(DecisionKind.Mulligan, state.Pending.Kind);
            Assert.AreNotEqual(first, state.Pending.Player, "next player decides");
        }

        [Test]
        public void Mana_GrowsByOneEachTurn_UpToTen()
        {
            var g = TestGame.AtFirstMainPhase();
            var first = g.Active;
            var second = g.Other;
            g.PassToStep(Step.Main1, second);
            Assert.AreEqual(1, g.P(second).MaxMana);
            g.PassToStep(Step.Main1, first);
            Assert.AreEqual(2, g.P(first).MaxMana);
            Assert.AreEqual(2, g.P(first).Mana);

            g.P(first).MaxMana = 10;
            g.PassToStep(Step.Main1, second);
            g.PassToStep(Step.Main1, first);
            Assert.AreEqual(10, g.P(first).MaxMana, "capped at 10");
        }

        [Test]
        public void UnspentMana_BecomesGold_AtEndOfTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var first = g.Active;
            g.PassToStep(Step.Main1, g.Other);
            Assert.AreEqual(1, g.P(first).Gold, "1 unspent mana banked");
            Assert.AreEqual(0, g.P(first).Mana, "no mana on other players' turns");
        }

        [Test]
        public void Gold_IsCappedAtFive()
        {
            var g = TestGame.AtFirstMainPhase();
            var first = g.Active;
            g.P(first).Gold = 4;
            g.SetMana(first, 5);
            g.PassToStep(Step.Main1, g.Other);
            Assert.AreEqual(5, g.P(first).Gold);
        }

        [Test]
        public void Gold_CannotPayForCreatures()
        {
            var g = TestGame.AtFirstMainPhase();
            g.SetMana(g.Active, 0);
            g.P(g.Active).Gold = 5;
            var actions = g.Legal(g.Active);
            Assert.IsFalse(actions.Any(a => a.Kind == ActionKind.PlayCard));
        }

        [Test]
        public void Gold_PaysForInstants_InAnyMix()
        {
            var g = TestGame.AtFirstMainPhase();
            var target = g.AddToBattlefield(g.Other, "hired_sellsword");
            var snot = g.AddToHand(g.Active, "spark_snot");
            g.SetMana(g.Active, 1);
            g.P(g.Active).Gold = 1;
            var plays = g.Legal(g.Active).Where(a => a.Card == snot.Id && a.Target == Target.ForObject(target.Id)).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, plays.Select(a => a.GoldPaid));
        }

        [Test]
        public void Gold_LetsYouCastInstants_OnAnOpponentsTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var creature = g.AddToBattlefield(g.Active, "hired_sellsword");
            var snot = g.AddToHand(g.Other, "spark_snot"); // the other player has 1 Gold, no mana

            g.Pass(); // active passes in main phase 1 → other gets priority
            Assert.AreEqual(g.Other, g.State.PriorityPlayer);
            g.Do(PlayerAction.Play(g.Other, snot.Id, Target.ForObject(creature.Id), goldPaid: 1));
            Assert.AreEqual(0, g.P(g.Other).Gold);
            g.PassRound();
            Assert.AreEqual(2, creature.Damage);
        }

        [Test]
        public void Cleanup_DiscardsDownToSeven()
        {
            var g = TestGame.AtFirstMainPhase();
            var first = g.Active;
            g.AddToHand(first, "spark_snot");
            g.AddToHand(first, "spark_snot");
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DiscardToHandSize);
            Assert.AreEqual(2, g.State.Pending.Count);
            g.Do(PlayerAction.Discard(first, g.P(first).Hand[0].Id));
            g.Do(PlayerAction.Discard(first, g.P(first).Hand[0].Id));
            Assert.AreEqual(7, g.P(first).Hand.Count);
            Assert.AreEqual(2, g.P(first).Graveyard.Count);
            Assert.AreNotEqual(first, g.Active, "next turn started");
        }

        [Test]
        public void SorcerySpeed_OnlyInOwnMainPhase_WithEmptyChain()
        {
            var g = TestGame.AtFirstMainPhase();
            g.SetMana(g.Active, 2);
            var gang = g.AddToHand(g.Active, "gob_gang");
            Assert.IsTrue(g.Legal(g.Active).Any(a => a.Card == gang.Id));

            g.PassRound(); // → beginning of combat
            Assert.AreEqual(Step.BeginCombat, g.State.Step);
            Assert.IsFalse(g.Legal(g.Active).Any(a => a.Card == gang.Id));
        }
    }
}
