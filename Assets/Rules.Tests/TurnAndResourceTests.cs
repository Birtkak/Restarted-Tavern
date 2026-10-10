using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>GAME_DESIGN §3 (setup), §5 (mana and Gold). The round structure itself is in <see cref="RoundsTests"/>.</summary>
    public class TurnAndResourceTests
    {
        [Test]
        public void Setup_StartingValues_NoGoingFirstCompensation()
        {
            var g = TestGame.AtFirstMainPhase();
            foreach (var p in new[] { g.Active, g.Other })
            {
                Assert.AreEqual(30, g.P(p).Life);
                Assert.AreEqual(8, g.P(p).Hand.Count, "§3: 7 cards, then everyone draws in round 1");
                Assert.AreEqual(1, g.P(p).MaxMana);
                Assert.AreEqual(1, g.P(p).Mana);
                Assert.AreEqual(0, g.P(p).Gold);
            }
        }

        [Test]
        public void Mulligan_London_PutsCardsOnTheBottom()
        {
            var engine = new GameEngine(CardPool.CreateDatabase());
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
        public void Mana_GrowsByOneEachRound_UpToTen()
        {
            var g = TestGame.AtFirstMainPhase();
            var a = g.Active;
            var b = g.Other;
            g.PassUntil(s => s.RoundNumber == 2 && s.Step == Step.Main1);
            Assert.AreEqual(2, g.P(a).MaxMana);
            Assert.AreEqual(2, g.P(b).Mana);

            g.P(a).MaxMana = 10;
            g.PassUntil(s => s.RoundNumber == 3 && s.Step == Step.Main1);
            Assert.AreEqual(10, g.P(a).MaxMana, "capped at 10");
            Assert.AreEqual(10, g.P(a).Mana);
        }

        [Test]
        public void UnspentMana_BecomesGold_AtTheEndOfTheRound()
        {
            var g = TestGame.AtFirstMainPhase();
            var a = g.Active;
            var b = g.Other;
            g.Pass();
            Assert.AreEqual(1, g.P(a).Mana, "unspent mana stays until the round ends");
            Assert.AreEqual(0, g.P(a).Gold);

            g.PassUntil(s => s.RoundNumber == 2 && s.Step == Step.Main1);
            Assert.AreEqual(1, g.P(a).Gold, "round 1's unspent mana became Gold");
            Assert.AreEqual(1, g.P(b).Gold);
        }

        [Test]
        public void Gold_IsCappedAtThree()
        {
            var g = TestGame.AtFirstMainPhase();
            Assert.AreEqual(3, g.State.Format.GoldCap);
            var me = g.Active;
            g.P(me).Gold = 2;
            g.SetMana(me, 4);
            g.PassUntil(s => s.RoundNumber == 2);
            Assert.AreEqual(3, g.P(me).Gold, "banked up to the cap, the rest is lost");
        }

        [Test]
        public void Gold_CannotPayForCreatures()
        {
            var g = TestGame.AtFirstMainPhase();
            g.SetMana(g.Active, 0);
            g.P(g.Active).Gold = 3;
            var actions = g.Legal(g.Active);
            Assert.IsFalse(actions.Any(a => a.Kind == ActionKind.PlayCard));
        }

        [Test]
        public void Permanents_AreManaOnly_EvenEquipment()
        {
            var shiv = new CardDefinition { Id = "test_shiv", Name = "Test Shiv", Type = CardType.Equipment, Cost = 2 };
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { shiv });
            var sword = g.AddToHand(g.Active, "hired_sellsword");
            var equipment = g.AddToHand(g.Active, "test_shiv");
            g.SetMana(g.Active, 0);
            g.P(g.Active).Gold = 3;
            Assert.IsFalse(g.Legal(g.Active).Any(a => a.Card == sword.Id || a.Card == equipment.Id), "§5.2: permanents can't use Gold");

            g.SetMana(g.Active, 2);
            g.Do(PlayerAction.Play(g.Active, equipment.Id));
            Assert.AreEqual(0, g.P(g.Active).Mana);
            Assert.AreEqual(3, g.P(g.Active).Gold);
        }

        [Test]
        public void SorcerySpeed_IsYourAction_WithAnEmptyChain()
        {
            var g = TestGame.AtFirstMainPhase();
            var a = g.Active;
            var b = g.Other;
            g.SetMana(a, 2);
            g.SetMana(b, 0);
            g.P(b).Gold = 2;
            var gang = g.AddToHand(b, "gob_gang"); // Sorcery, cost 2
            g.Do(g.Legal(a).First(x => x.Kind == ActionKind.PlayCard));
            g.Pass(); // a passes with the creature on the Chain
            Assert.AreEqual(b, g.State.PriorityPlayer);
            Assert.IsFalse(g.Legal(b).Any(x => x.Card == gang.Id), "not in response");

            g.Pass(); // b passes too: the creature resolves and a's action is over
            Assert.AreEqual(b, g.State.ActivePlayer, "b has the action now");
            Assert.IsTrue(g.Legal(b).Any(x => x.Card == gang.Id), "Gold pays for Sorceries on your action");
        }

        [Test]
        public void SpellsPayGoldFirst()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.P(me).Gold = 1;
            g.SetMana(me, 2);
            var shock = g.AddToHand(me, "static_shock"); // Instant, cost 1
            g.AddToBattlefield(g.Other, "hired_sellsword"); // something to target
            g.Do(g.Legal(me).First(a => a.Kind == ActionKind.PlayCard && a.Card == shock.Id));
            Assert.AreEqual(0, g.P(me).Gold, "Gold (spell mana) is spent first");
            Assert.AreEqual(2, g.P(me).Mana);
        }

        [Test]
        public void GoldFirst_NoSequencingTrap_SorceryThenCreature()
        {
            // RULES_REVIEW #6: with mana first, a Sorcery cast before a creature ate the creature's mana.
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 3);
            g.P(me).Gold = 3;
            var sorcery = g.AddToHand(me, "round_on_the_house"); // Sorcery, cost 3
            g.Do(PlayerAction.Play(me, sorcery.Id));
            Assert.AreEqual(0, g.P(me).Gold, "the Sorcery is paid with Gold");
            Assert.AreEqual(3, g.P(me).Mana, "the mana is still there for a creature");
        }

        [Test]
        public void GoldFirst_KeepsTheGoldInvestNeeds()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 2);
            g.P(me).Gold = 3;
            var special = g.AddToHand(me, "house_special"); // Sorcery 2, Invest 3
            var invest = g.Legal(me).SingleOrDefault(a => a.Card == special.Id && a.Invest);
            Assert.IsNotNull(invest, "2 mana for the cost, 3 Gold for the Invest");
            g.Do(invest);
            Assert.AreEqual(0, g.P(me).Mana);
            Assert.AreEqual(0, g.P(me).Gold);
        }

        [Test]
        public void GoldFirst_KeepsTheGoldForPayAnyAmountOfGold()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 3);
            g.P(me).Gold = 3;
            var tab = g.AddToHand(me, "settle_the_tab"); // Sorcery 3, "pay any amount of Gold (X)"
            var xs = g.Legal(me).Where(a => a.Card == tab.Id).Select(a => a.X).OrderBy(x => x).ToArray();
            Assert.AreEqual(new[] { 0, 1, 2, 3 }, xs, "the cost can use mana so every Gold can go to X");
            g.Do(g.Legal(me).Single(a => a.Card == tab.Id && a.X == 1));
            Assert.AreEqual(2, g.P(me).Mana, "X = 1 Gold set aside; the cost took the other 2 Gold first, then 1 mana");
            Assert.AreEqual(0, g.P(me).Gold);
        }

        [Test]
        public void Invest_IsAlwaysPaidWithGold_AfterTheCost()
        {
            var g = TestGame.AtFirstMainPhase();
            var beast = g.AddToBattlefield(g.Active, "ironbark_grizzly", damage: 4);
            var remedy = g.AddToHand(g.Active, "jungle_remedy"); // cost 1, Invest 1
            g.SetMana(g.Active, 0);
            g.P(g.Active).Gold = 1;
            Assert.IsFalse(g.Legal(g.Active).Any(a => a.Card == remedy.Id && a.Invest), "1 Gold pays the cost, nothing left to Invest");

            g.P(g.Active).Gold = 2;
            g.Do(PlayerAction.Play(g.Active, remedy.Id, Target.ForObject(beast.Id), invest: true));
            Assert.AreEqual(0, g.P(g.Active).Gold);
        }

        [Test]
        public void Instants_AnswerOnSomeoneElsesAction()
        {
            var g = TestGame.AtFirstMainPhase();
            var a = g.Active;
            var b = g.Other;
            var creature = g.AddToBattlefield(a, "hired_sellsword");
            var snot = g.AddToHand(b, "spark_snot");
            g.SetMana(a, 2);
            g.P(b).Gold = 1;
            g.Do(g.Legal(a).First(x => x.Kind == ActionKind.PlayCard));
            g.Pass(); // a passes with the creature on the Chain
            g.Do(PlayerAction.Play(b, snot.Id, Target.ForObject(creature.Id)));
            Assert.AreEqual(0, g.P(b).Gold, "Gold first");
            Assert.AreEqual(1, g.P(b).Mana);
            g.PassRound();
            Assert.AreEqual(2, creature.Damage);
        }
    }
}
