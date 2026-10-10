using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// Runeterra-style mana in MTG turns (the Standard rules on the morning of 2026-10-10, now
    /// <see cref="FormatConfig.RuneterraRotation"/>): round pool, rotating leader, attack token, Gold first, Gold cap 3.
    /// The mana and Gold rules here are still the Standard ones; the rounds themselves are in <see cref="RoundsTests"/>.
    /// </summary>
    public class RuneterraManaTests
    {
        private static TestGame Game() => TestGame.AtFirstMainPhase(format: FormatConfig.RuneterraRotation());

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
        public void GoldIsCappedAtThree()
        {
            var g = TestGame.AtFirstMainPhase();
            Assert.AreEqual(3, g.State.Format.GoldCap);
            var me = g.Active;
            g.P(me).Gold = 2;
            g.SetMana(me, 4);
            g.PassUntil(s => s.TurnNumber > 2);
            Assert.AreEqual(3, g.P(me).Gold, "banked up to the cap, the rest is lost");
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
            var creature = g.AddToHand(me, "tavern_bouncer");    // creature, cost 3
            g.Do(PlayerAction.Play(me, sorcery.Id));
            Assert.AreEqual(0, g.P(me).Gold, "the Sorcery is paid with Gold");
            Assert.AreEqual(3, g.P(me).Mana, "the mana is still there for the creature");
            g.PassRound();
            g.Do(PlayerAction.Play(me, creature.Id));
            g.PassRound();
            Assert.IsNotNull(g.OnBattlefield(me, "tavern_bouncer"));
        }

        [Test]
        public void GoldFirst_KeepsTheGoldInvestNeeds()
        {
            // Bug found 2026-10-10: Gold first used to pay the cost with the Gold the Invest needed.
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
    }
}
