using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// The rest of set v0.1, batch F: choices during resolution (DecisionKind.ChooseObject / YesNo, queued in
    /// turn order) and divided damage chosen on casting (MTG 601.2d).
    /// </summary>
    public class ChoiceCardTests
    {
        private static CardInstance Cast(TestGame g, PlayerId player, string card)
        {
            var c = g.AddToHand(player, card);
            g.SetMana(player, 10);
            g.Do(g.Legal(player).First(a => a.Kind == ActionKind.PlayCard && a.Card == c.Id && !a.Invest));
            return c;
        }

        private static CardInstance InGraveyard(TestGame g, PlayerId owner, string card)
        {
            var c = new CardInstance
            {
                Id = new ObjectId(g.State.NextObjectId++), DefinitionId = card, Owner = owner, Controller = owner, Zone = Zone.Graveyard,
            };
            g.P(owner).Graveyard.Add(c);
            return c;
        }

        /// <summary>Cast a creature, let it resolve, and let its Arrival trigger resolve up to its choice.</summary>
        private static void CastAndResolveArrival(TestGame g, PlayerId player, string card)
        {
            Cast(g, player, card);
            g.PassRound(); // the creature
            g.PassRound(); // its Arrival trigger
        }

        [Test]
        public void LedgerImp_YouMayLoseLifeForGold()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            CastAndResolveArrival(g, me, "ledger_imp");
            Assert.AreEqual(DecisionKind.YesNo, g.State.Pending?.Kind);
            g.Do(PlayerAction.ChooseOption(me, 1));
            Assert.AreEqual(28, g.P(me).Life);
            Assert.AreEqual(1, g.P(me).Gold);
            Assert.AreEqual(g.Other, g.State.PriorityPlayer, "my action is over");

            g.HandTheActionBack();
            CastAndResolveArrival(g, me, "ledger_imp");
            g.Do(PlayerAction.ChooseOption(me, 0));
            Assert.AreEqual(28, g.P(me).Life, "said no: nothing happens");
        }

        [Test]
        public void GooberShaman_MayRummage()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            int hand = g.P(me).Hand.Count;
            CastAndResolveArrival(g, me, "goober_shaman");
            Assert.AreEqual(DecisionKind.ChooseObject, g.State.Pending?.Kind);
            Assert.AreEqual(hand + 1, g.Legal(me).Count, "any card in hand, or nothing");
            var discarded = g.P(me).Hand[0];
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(discarded.Id)));
            Assert.AreEqual(hand, g.P(me).Hand.Count, "discarded one, drew one");
            Assert.IsTrue(g.P(me).Graveyard.Any(c => c.DefinitionId == discarded.DefinitionId));
        }

        [Test]
        public void SpectacleOfBlood_TheOpponentChoosesWhatToSacrifice()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var small = g.AddToBattlefield(g.Other, "goober_rascal");
            var big = g.AddToBattlefield(g.Other, "pit_champion");
            Cast(g, me, "spectacle_of_blood");
            g.PassRound();
            Assert.AreEqual(DecisionKind.ChooseObject, g.State.Pending?.Kind);
            Assert.AreEqual(g.Other, g.State.Pending.Player);
            Assert.AreEqual(2, g.Legal(g.Other).Count, "must choose: no 'nothing'");
            g.Do(PlayerAction.ChooseTarget(g.Other, Target.ForObject(small.Id)));
            Assert.IsNull(g.State.FindOnBattlefield(small.Id));
            Assert.IsNotNull(g.State.FindOnBattlefield(big.Id));
            Assert.AreEqual(g.Other, g.State.PriorityPlayer, "my action is over");
        }

        [Test]
        public void MidnightRingmaster_MaySacrificeAnotherToDraw()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var fodder = g.AddToBattlefield(me, "goober_rascal");
            int hand = g.P(me).Hand.Count;
            CastAndResolveArrival(g, me, "midnight_ringmaster");
            var choices = g.Legal(me);
            Assert.AreEqual(2, choices.Count, "the other creature, or nothing (not itself)");
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(fodder.Id)));
            Assert.IsNull(g.State.FindOnBattlefield(fodder.Id));
            Assert.AreEqual(hand + 2, g.P(me).Hand.Count, "the helper adds the card it casts");
        }

        [Test]
        public void AbyssalHeadliner_SacrificeOrLoseLife()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "abyssal_headliner");
            g.ToNextRoundStartTrigger();
            g.PassRound();
            Assert.AreEqual(27, g.P(me).Life, "no other creature: lose 3 life");

            var fodder = g.AddToBattlefield(me, "goober_rascal");
            g.ToNextRoundStartTrigger();
            g.PassRound();
            Assert.AreEqual(DecisionKind.ChooseObject, g.State.Pending?.Kind);
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(fodder.Id)));
            Assert.AreEqual(27, g.P(me).Life);
            Assert.IsNull(g.State.FindOnBattlefield(fodder.Id));
        }

        [Test]
        public void TheDealer_OpponentsPayOrYouDraw()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "the_dealer");
            g.NextRound();
            g.PassUntil(s => s.ActivePlayer == me && s.Chain.Count > 0);
            g.P(g.Other).Gold = 3; // set after their turn: they bank mana in their cleanup
            g.P(me).Gold = 0;
            g.PassRound();
            Assert.AreEqual(DecisionKind.YesNo, g.State.Pending?.Kind);
            Assert.AreEqual(g.Other, g.State.Pending.Player, "the opponent decides");
            g.Do(PlayerAction.ChooseOption(g.Other, 1));
            Assert.AreEqual(1, g.P(g.Other).Gold);
            Assert.AreEqual(2, g.P(me).Gold);

            // With less than 2 Gold they can't give (decided 2026-10-09): the Dealer's controller draws.
            g.NextRound();
            g.PassUntil(s => s.ActivePlayer == me && s.Chain.Count > 0);
            g.P(g.Other).Gold = 1;
            int hand = g.P(me).Hand.Count;
            g.PassRound();
            Assert.IsNull(g.State.Pending);
            Assert.AreEqual(hand + 2, g.P(me).Hand.Count, "the Dealer's card, then the normal draw step");
        }

        [Test]
        public void ExhumationBroadcast_OneFromEachGraveyard_QueuedInTurnOrder()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            InGraveyard(g, me, "hired_sellsword");
            var mine = InGraveyard(g, me, "hog_rider");
            InGraveyard(g, g.Other, "goober_rascal");
            var theirs = InGraveyard(g, g.Other, "pit_champion");
            Cast(g, me, "exhumation_broadcast");
            g.PassRound();
            Assert.AreEqual(DecisionKind.ChooseObject, g.State.Pending?.Kind);
            Assert.AreEqual(1, g.State.ChoiceQueue.Count, "the second graveyard waits its turn");
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(mine.Id)));
            Assert.AreEqual(DecisionKind.ChooseObject, g.State.Pending?.Kind);
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(theirs.Id)));
            Assert.IsNotNull(g.OnBattlefield(me, "hog_rider"));
            Assert.IsNotNull(g.OnBattlefield(me, "pit_champion"), "under your control");
            Assert.AreEqual(g.Other, g.State.PriorityPlayer, "my action is over");
        }

        [Test]
        public void EverythingHasAPrice_StealsTheMostExpensive_OwnerPicksOnTies()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var other = g.Other;
            var a = g.AddToBattlefield(other, "pit_fighter");  // 4
            var b = g.AddToBattlefield(other, "barrel_bomber"); // 4
            g.AddToBattlefield(other, "hired_sellsword");      // 2
            int hand = g.P(other).Hand.Count;
            Cast(g, me, "everything_has_a_price");
            g.PassRound();
            Assert.AreEqual(other, g.State.Pending?.Player, "a tie: its controller chooses");
            Assert.AreEqual(2, g.Legal(other).Count);
            g.Do(PlayerAction.ChooseTarget(other, Target.ForObject(b.Id)));
            Assert.AreEqual(me, b.Controller);
            Assert.AreEqual(other, a.Controller);
            Assert.AreEqual(3, g.P(other).Gold, "4 Gold, capped at 3");
            Assert.AreEqual(hand + 2, g.P(other).Hand.Count);
        }

        [Test]
        public void MidnightRitual_InvestCanBringBackTheSacrificedCreature()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var victim = g.AddToBattlefield(me, "hog_rider"); // cost 3
            var ritual = g.AddToHand(me, "midnight_ritual");
            g.SetMana(me, 1);
            g.P(me).Gold = 2;
            int hand = g.P(me).Hand.Count;
            g.Do(g.Legal(me).Single(a => a.Card == ritual.Id && a.Invest && a.Sacrifice == victim.Id));
            g.PassRound();
            Assert.AreEqual(hand - 1 + 2, g.P(me).Hand.Count);
            Assert.IsNotNull(g.OnBattlefield(me, "hog_rider"), "the only creature card in the graveyard comes back");
        }

        [Test]
        public void FirecrackerVolley_DividesThreeDamage()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var rascal = g.AddToBattlefield(g.Other, "goober_rascal"); // 2/1
            var volley = g.AddToHand(me, "firecracker_volley");
            g.SetMana(me, 3);
            var plays = g.Legal(me).Where(a => a.Card == volley.Id).ToList();
            Assert.IsTrue(plays.All(a => a.Division.Sum() == 3 && a.Division.Length == a.Targets.Length && a.Division.All(n => n >= 1)));
            var play = plays.Single(a => a.Targets.Length == 2 && a.Targets.Contains(Target.ForObject(rascal.Id))
                && a.Targets.Contains(Target.ForPlayer(g.Other))
                && a.Division[System.Array.IndexOf(a.Targets, Target.ForObject(rascal.Id))] == 1);
            g.Do(play);
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(rascal.Id));
            Assert.AreEqual(28, g.P(g.Other).Life);
        }
    }
}
