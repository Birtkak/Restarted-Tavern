using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// Set v0.2 batch C: counterspells (MTG 701.5) with taxes, targeting things on the Chain, bounce,
    /// control change (MTG 613 layer 2, 302.6), destroy all, graveyard targets.
    /// </summary>
    public class ChainAndControlTests
    {
        private static ChainItem Top(TestGame g) => g.State.Chain[g.State.Chain.Count - 1];

        /// <summary>The active player casts Hog-Rider (3) with 6 mana; the other player gets priority holding <paramref name="answer"/>.</summary>
        private static (CardInstance answer, ChainItem rider) OpponentCastsHogRider(TestGame g, string answer, int answerGold)
        {
            var rider = g.AddToHand(g.Active, "hog_rider");
            g.SetMana(g.Active, 6);
            g.Do(PlayerAction.Play(g.Active, rider.Id));
            var item = Top(g);
            var card = g.AddToHand(g.Other, answer);
            g.P(g.Other).Gold = answerGold;
            g.Pass();
            Assert.AreEqual(g.Other, g.State.PriorityPlayer);
            return (card, item);
        }

        [Test]
        public void HushMoney_ControllerPays_TheSpellResolves_AndYouGainGold()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var (hush, rider) = OpponentCastsHogRider(g, "hush_money", 2);
            g.Do(PlayerAction.Play(g.Other, hush.Id, Target.ForObject(rider.ObjectId)));
            g.PassRound();

            Assert.AreEqual(DecisionKind.PayTax, g.State.Pending?.Kind);
            Assert.AreEqual(me, g.State.Pending.Player);
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, g.Legal(me).Select(a => a.Option));
            g.Do(PlayerAction.ChooseOption(me, 1));
            Assert.AreEqual(0, g.P(me).Mana, "3 mana paid (mana first)");
            Assert.AreEqual(2, g.P(g.Other).Gold, "0 left after casting, +2 because they paid");
            g.PassRound();
            Assert.IsNotNull(g.OnBattlefield(me, "hog_rider"));
        }

        [Test]
        public void HushMoney_CantPay_IsCountered()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var (hush, rider) = OpponentCastsHogRider(g, "hush_money", 2);
            g.P(me).Mana = 2; // spent elsewhere: can't pay 3
            g.Do(PlayerAction.Play(g.Other, hush.Id, Target.ForObject(rider.ObjectId)));
            g.PassRound();
            Assert.IsNull(g.State.Pending, "no choice when they can't pay");
            Assert.IsEmpty(g.State.Chain);
            Assert.IsTrue(g.P(me).Graveyard.Any(c => c.DefinitionId == "hog_rider"), "a countered spell goes to the graveyard");
            Assert.IsTrue(g.Events.OfType<CounteredEvent>().Any());
        }

        [Test]
        public void CounterfeitCoin_OnlyCheapSpells_ControllerGetsGoldForItsPrintedCost()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var champion = g.AddToHand(me, "pit_champion");
            g.SetMana(me, 10);
            g.Do(PlayerAction.Play(me, champion.Id));
            var coin = g.AddToHand(g.Other, "counterfeit_coin");
            g.P(g.Other).Gold = 3;
            g.Pass();
            Assert.IsFalse(g.Legal(g.Other).Any(a => a.Card == coin.Id), "Pit Champion costs 6");
            g.Pass(); // Pit Champion resolves

            var hog = g.AddToHand(me, "hog_rider");
            g.Do(PlayerAction.Play(me, hog.Id));
            var rider = Top(g);
            g.Pass();
            g.Do(PlayerAction.Play(g.Other, coin.Id, Target.ForObject(rider.ObjectId)));
            g.PassRound();
            Assert.IsEmpty(g.State.Chain, "countered");
            Assert.IsNull(g.OnBattlefield(me, "hog_rider"));
            Assert.AreEqual(3, g.P(me).Gold, "Hog-Rider's printed cost");
        }

        [Test]
        public void BribeTheReferee_CountersATavernDwellerPower_WhichStaysUsed()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var vesper = g.SetTavernDweller(me, "madame_vesper"); // (3) Draw a card and lose 2 life.
            g.SetMana(me, 3);
            int hand = g.P(me).Hand.Count;
            g.Do(g.Activations(me, vesper).Single());
            var power = Top(g);
            Assert.IsTrue(power.IsTavernDwellerPower);

            var bribe = g.AddToHand(g.Other, "bribe_the_referee");
            g.P(g.Other).Gold = 4;
            g.Pass();
            g.Do(PlayerAction.Play(g.Other, bribe.Id, Target.ForObject(power.ObjectId)));
            g.PassRound();
            Assert.IsEmpty(g.State.Chain);
            Assert.AreEqual(30, g.P(me).Life, "the Power never resolved");
            Assert.AreEqual(hand + 1, g.P(me).Hand.Count, "its controller draws a card from the Bribe, not the Power");
            Assert.AreEqual(3, g.P(me).Gold);
            g.SetMana(me, 3);
            Assert.IsEmpty(g.Activations(me, vesper), "MTG: a countered once-each-turn ability was still activated");
        }

        [Test]
        public void BribeTheReferee_CanCounterATriggeredAbility()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var bomber = g.AddToHand(me, "barrel_bomber"); // Arrival: 2 damage to any target
            g.SetMana(me, 4);
            g.Do(PlayerAction.Play(me, bomber.Id));
            g.PassRound();
            Assert.AreEqual(DecisionKind.ChooseTriggerTarget, g.State.Pending?.Kind);
            g.Do(PlayerAction.ChooseTarget(me, Target.ForPlayer(g.Other)));
            var trigger = Top(g);
            Assert.AreEqual(ChainItemKind.TriggeredAbility, trigger.Kind);

            var bribe = g.AddToHand(g.Other, "bribe_the_referee");
            g.P(g.Other).Gold = 4;
            g.Pass();
            g.Do(PlayerAction.Play(g.Other, bribe.Id, Target.ForObject(trigger.ObjectId)));
            g.PassRound();
            Assert.IsEmpty(g.State.Chain);
            Assert.AreEqual(30, g.P(g.Other).Life);
        }

        [Test]
        public void BouncedCheck_ReturnsCheapCreatures_DamageAndTokensDontSurvive()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var hurt = g.AddToBattlefield(g.Other, "hog_rider", damage: 2);
            var big = g.AddToBattlefield(g.Other, "pit_fighter"); // cost 4
            var token = g.AddToBattlefield(g.Other, PrototypeCards.GooberToken);
            token.IsToken = true;
            var check = g.AddToHand(me, "bounced_check");
            g.SetMana(me, 2);
            Assert.IsFalse(g.Legal(me).Any(a => a.Card == check.Id && a.Target == Target.ForObject(big.Id)), "cost 4");

            g.Do(PlayerAction.Play(me, check.Id, Target.ForObject(hurt.Id)));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(hurt.Id));
            Assert.IsTrue(g.P(g.Other).Hand.Any(c => c.DefinitionId == "hog_rider"), "back in its owner's hand");
            Assert.AreEqual(1, g.P(g.Other).Gold);

            var check2 = g.AddToHand(me, "bounced_check");
            g.SetMana(me, 2);
            int hand = g.P(g.Other).Hand.Count;
            g.Do(PlayerAction.Play(me, check2.Id, Target.ForObject(token.Id)));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(token.Id));
            Assert.AreEqual(hand, g.P(g.Other).Hand.Count, "a token stops existing");
        }

        [Test]
        public void GoldenParachute_RescuesYourCreature_ForGold()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var hurt = g.AddToBattlefield(me, "tavern_bouncer", damage: 4);
            var chute = g.AddToHand(me, "golden_parachute");
            g.SetMana(me, 3);
            g.Do(PlayerAction.Play(me, chute.Id, Target.ForObject(hurt.Id)));
            g.PassRound();
            Assert.IsTrue(g.P(me).Hand.Any(c => c.DefinitionId == "tavern_bouncer"));
            Assert.AreEqual(3, g.P(me).Gold);
        }

        [Test]
        public void SilverTonguedDeal_StealsUntilEndOfTurn_WithHaste()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var other = g.Other;
            var victim = g.AddToBattlefield(other, "hog_rider");
            victim.Tapped = true;
            var deal = g.AddToHand(me, "silver_tongued_deal");
            g.SetMana(me, 3);
            g.Do(PlayerAction.Play(me, deal.Id, Target.ForObject(victim.Id)));
            g.PassRound();

            Assert.AreEqual(me, victim.Controller);
            Assert.IsTrue(g.P(me).Battlefield.Contains(victim));
            Assert.IsFalse(victim.Tapped, "untapped");
            Assert.AreEqual(2, g.P(other).Gold);
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            Assert.IsTrue(g.Legal(me).Any(a => a.Kind == ActionKind.DeclareAttacker && a.Card == victim.Id), "Haste: it can attack for us");

            g.PassUntil(s => s.ActivePlayer == other);
            Assert.AreEqual(other, victim.Controller, "control ends in the cleanup step");
            Assert.IsTrue(g.P(other).Battlefield.Contains(victim));
        }

        [Test]
        public void HostileTakeover_IsPermanent_AndPaysThePreviousController()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var other = g.Other;
            var victim = g.AddToBattlefield(other, "pit_champion"); // cost 6
            int hand = g.P(other).Hand.Count;
            var takeover = g.AddToHand(me, "hostile_takeover");
            g.SetMana(me, 6);
            g.Do(PlayerAction.Play(me, takeover.Id, Target.ForObject(victim.Id)));
            g.PassRound();
            Assert.AreEqual(me, victim.Controller);
            Assert.AreEqual(5, g.P(other).Gold, "6 Gold, capped at 5");
            Assert.AreEqual(hand + 1, g.P(other).Hand.Count);
            Assert.IsTrue(victim.SummoningSick, "MTG 302.6: not under our control since the turn began");

            g.PassUntil(s => s.ActivePlayer == other && s.Step == Step.Main1 && s.PriorityPlayer.HasValue);
            Assert.AreEqual(me, victim.Controller, "still ours");
        }

        [Test]
        public void TheFinalAct_DestroysAll_AndDrainsPerCreature()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "hired_sellsword");
            g.AddToBattlefield(g.Other, "hired_sellsword");
            g.AddToBattlefield(g.Other, "tavern_bouncer");
            g.P(me).Life = 20;
            var act = g.AddToHand(me, "the_final_act");
            g.SetMana(me, 7);
            g.Do(PlayerAction.Play(me, act.Id));
            g.PassRound();
            Assert.IsFalse(g.State.AllPermanents().Any());
            Assert.AreEqual(27, g.P(g.Other).Life);
            Assert.AreEqual(23, g.P(me).Life);
        }

        [Test]
        public void BodySnatcher_ExilesACreatureCardFromAnyGraveyard_AndGainsLife()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var dead = new CardInstance
            {
                Id = new ObjectId(g.State.NextObjectId++), DefinitionId = "hog_rider", Owner = g.Other, Controller = g.Other,
                Zone = Zone.Graveyard,
            };
            g.P(g.Other).Graveyard.Add(dead);
            g.P(me).Life = 25;
            var snatcher = g.AddToHand(me, "body_snatcher");
            g.SetMana(me, 3);
            g.Do(PlayerAction.Play(me, snatcher.Id));
            g.PassRound(); // the only legal target is chosen automatically
            Assert.AreEqual(1, g.State.Chain.Count);
            g.PassRound();
            Assert.IsEmpty(g.P(g.Other).Graveyard);
            Assert.AreEqual(1, g.P(g.Other).Exile.Count);
            Assert.AreEqual(27, g.P(me).Life);
        }

        [Test]
        public void GrandIllusion_ReturnsEverything_AndDrawsForYourCreatures()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "hired_sellsword");
            g.AddToBattlefield(me, "tavern_bouncer", damage: 3);
            g.AddToBattlefield(g.Other, "pit_champion");
            var illusion = g.AddToHand(me, "grand_illusion");
            g.SetMana(me, 6);
            int hand = g.P(me).Hand.Count;
            g.Do(PlayerAction.Play(me, illusion.Id));
            g.PassRound();
            Assert.IsFalse(g.State.AllPermanents().Any());
            Assert.AreEqual(hand - 1 + 2 + 2, g.P(me).Hand.Count, "2 creatures back + 2 cards drawn");
        }
    }

}
