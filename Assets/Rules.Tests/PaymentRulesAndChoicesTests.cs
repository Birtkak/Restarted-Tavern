using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// Set v0.2 batch D: payment rules (Retainer Mage, Silent Partner, Shady Moneylender), Gold theft on
    /// combat damage, attack-count triggers, Goober team pumps, choosing from the top of the deck, Curse
    /// watchers, delayed triggers (MTG 603.7) and the Dice Game auction (MTG 101.4).
    /// </summary>
    public class PaymentRulesAndChoicesTests
    {
        private static void Attack(TestGame g, params CardInstance[] attackers)
        {
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            foreach (var a in attackers) g.Do(PlayerAction.Attack(g.Active, a.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
        }

        [Test]
        public void RetainerMage_HasFlash_AndGoldCanPayForIt()
        {
            var g = TestGame.AtFirstMainPhase();
            var mage = g.AddToHand(g.Other, "retainer_mage");
            g.P(g.Other).Gold = 3;
            g.Pass();
            Assert.IsTrue(g.Legal(g.Other).Any(a => a.Card == mage.Id), "cast on the opponent's turn, paid with Gold");

            var sellsword = g.AddToHand(g.Other, "hired_sellsword");
            Assert.IsFalse(g.Legal(g.Other).Any(a => a.Card == sellsword.Id), "other creatures still need a main phase and mana");
            g.Do(g.Legal(g.Other).First(a => a.Card == mage.Id));
            Assert.AreEqual(0, g.P(g.Other).Gold);
        }

        [Test]
        public void SilentPartner_LetsManaPayInvest()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var tonic = g.AddToHand(me, "barkeeps_tonic"); // 1, Invest 1
            g.AddToBattlefield(me, "tavern_bouncer", damage: 1);
            g.SetMana(me, 2);
            Assert.IsFalse(g.Legal(me).Any(a => a.Card == tonic.Id && a.Invest), "Invest is Gold only");

            g.AddToBattlefield(me, "silent_partner");
            var invest = g.Legal(me).First(a => a.Card == tonic.Id && a.Invest);
            g.Do(invest);
            Assert.AreEqual(0, g.P(me).Mana, "1 for the spell, 1 for Invest");
            Assert.AreEqual(0, g.P(me).Gold);
        }

        [Test]
        public void ShadyMoneylender_GoldPaysForCreatures_AndOpponentsGetGold()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var rider = g.AddToHand(me, "hog_rider"); // 3
            g.SetMana(me, 1);
            g.P(me).Gold = 2;
            Assert.IsFalse(g.Legal(me).Any(a => a.Card == rider.Id), "§5.2: Gold can't pay for permanents");

            g.AddToBattlefield(me, "shady_moneylender");
            g.Do(g.Legal(me).First(a => a.Card == rider.Id));
            Assert.AreEqual(0, g.P(me).Mana);
            Assert.AreEqual(0, g.P(me).Gold, "mana first, then Gold");
            Assert.AreEqual(2, g.State.Chain.Count, "the Moneylender triggers");
            g.PassRound();
            Assert.AreEqual(1, g.P(g.Other).Gold);
        }

        [Test]
        public void DiceGame_HighestBidDrawsTwo_TiesDrawOne()
        {
            var g = TestGame.Classic();
            var me = g.Active;
            var other = g.Other;
            var dice = g.AddToHand(me, "dice_game");
            g.SetMana(me, 2);
            g.P(me).Gold = 3;
            g.P(other).Gold = 4;
            int myHand = g.P(me).Hand.Count, theirHand = g.P(other).Hand.Count;
            g.Do(PlayerAction.Play(me, dice.Id));
            g.PassRound();

            Assert.AreEqual(DecisionKind.PayAnyGold, g.State.Pending?.Kind);
            Assert.AreEqual(me, g.State.Pending.Player, "the active player chooses first");
            Assert.AreEqual(4, g.Legal(me).Count, "0 to 3 Gold");
            g.Do(PlayerAction.ChooseOption(me, 2));
            Assert.AreEqual(other, g.State.Pending.Player);
            g.Do(PlayerAction.ChooseOption(other, 3));
            Assert.IsNull(g.State.Pending);
            Assert.AreEqual(theirHand + 2, g.P(other).Hand.Count, "paid the most");
            Assert.AreEqual(myHand - 1, g.P(me).Hand.Count);
            Assert.AreEqual(1, g.P(me).Gold);
            Assert.AreEqual(1, g.P(other).Gold);

            // A tie: both draw one. A player with no Gold isn't asked (pays 0).
            var dice2 = g.AddToHand(me, "dice_game");
            g.SetMana(me, 2);
            g.P(other).Gold = 0;
            g.P(me).Gold = 0;
            myHand = g.P(me).Hand.Count;
            theirHand = g.P(other).Hand.Count;
            g.Do(PlayerAction.Play(me, dice2.Id));
            g.PassRound();
            Assert.IsNull(g.State.Pending, "nobody has Gold: nothing to choose");
            Assert.AreEqual(myHand - 1 + 1, g.P(me).Hand.Count);
            Assert.AreEqual(theirHand + 1, g.P(other).Hand.Count);
        }

        [Test]
        public void GoldToothBruiser_StealsGold_AndStillPaysAtZero()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var bruiser = g.AddToBattlefield(me, "gold_tooth_bruiser");
            g.P(g.Other).Gold = 0;
            Attack(g, bruiser);
            g.PassUntil(s => s.Step == Step.CombatDamage && s.Chain.Count > 0);
            g.PassRound();
            Assert.AreEqual(0, g.P(g.Other).Gold);
            Assert.AreEqual(1, g.P(me).Gold, "decided 2026-10-09: you gain 1 even if they had none");
        }

        [Test]
        public void PickpocketBoss_WatchesOtherGoobers()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var boss = g.AddToBattlefield(me, "pickpocket_boss");
            var runt = g.AddToBattlefield(me, "brawling_runt");      // a Goober
            var sellsword = g.AddToBattlefield(me, "hired_sellsword"); // not a Goober
            g.P(g.Other).Gold = 3;
            Attack(g, boss, runt, sellsword);
            g.PassUntil(s => s.Step == Step.CombatDamage && s.Chain.Count > 0);
            Assert.AreEqual(1, g.State.Chain.Count, "only the other Goober (Brawling Runt) counts");
            g.PassRound();
            Assert.AreEqual(2, g.P(g.Other).Gold);
            Assert.AreEqual(1, g.P(me).Gold);
        }

        [Test]
        public void RallyDrummer_PumpsAttackers_WhenThreeOrMoreAttack()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var drummer = g.AddToBattlefield(me, "rally_drummer");
            var a = g.AddToBattlefield(me, "hired_sellsword");
            var b = g.AddToBattlefield(me, "hired_sellsword");
            var home = g.AddToBattlefield(me, "hired_sellsword");
            Attack(g, drummer, a, b);
            Assert.AreEqual(1, g.State.Chain.Count);
            g.PassRound();
            Assert.AreEqual(3, g.Stats(a).Power);
            Assert.AreEqual(3, g.Stats(drummer).Power);
            Assert.AreEqual(2, g.Stats(home).Power, "only attacking creatures");
        }

        [Test]
        public void RallyDrummer_TwoAttackersAreNotEnough()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var drummer = g.AddToBattlefield(me, "rally_drummer");
            var a = g.AddToBattlefield(me, "hired_sellsword");
            Attack(g, drummer, a);
            Assert.IsEmpty(g.State.Chain);
        }

        [Test]
        public void RecklessCharge_InvestPumpsYourOtherGoobers()
        {
            var g = TestGame.Classic();
            var me = g.Active;
            var target = g.AddToBattlefield(me, "brawling_runt");   // Goober 2/2
            var goober = g.AddToBattlefield(me, "goober_rascal");   // Goober 2/1
            var human = g.AddToBattlefield(me, "hired_sellsword");
            var charge = g.AddToHand(me, "reckless_charge");
            g.SetMana(me, 1);
            g.P(me).Gold = 1;
            g.Do(PlayerAction.Play(me, charge.Id, Target.ForObject(target.Id), invest: true));
            g.PassRound();
            Assert.AreEqual(4, g.Stats(target).Power, "+2/+0, not the Invest bonus");
            Assert.IsTrue(g.Stats(target).Has(Keyword.Trample));
            Assert.AreEqual(3, g.Stats(goober).Power);
            Assert.AreEqual(2, g.Stats(human).Power);
        }

        [Test]
        public void GrandHeist_TakesAllTheirGold_InvestMakesGoobersForWhatYouGained()
        {
            var g = TestGame.Classic();
            var me = g.Active;
            var heist = g.AddToHand(me, "grand_heist");
            g.SetMana(me, 3);
            g.P(me).Gold = 3;
            g.P(g.Other).Gold = 5;
            g.Do(PlayerAction.Play(me, heist.Id, System.Array.Empty<Target>(), invest: true));
            Assert.AreEqual(1, g.P(me).Gold, "Invest 2 paid with Gold");
            g.PassRound();
            Assert.AreEqual(0, g.P(g.Other).Gold);
            Assert.AreEqual(5, g.P(me).Gold, "your cap limits the take: gained 4");
            Assert.AreEqual(4, g.P(me).Battlefield.Count(c => c.DefinitionId == Cards.CardPool.GooberToken));
        }

        [Test]
        public void GraveGossip_OneToHand_RestToGraveyard()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var gossip = g.AddToHand(me, "grave_gossip");
            g.SetMana(me, 1);
            var top = g.P(me).Deck.Take(3).ToList();
            int hand = g.P(me).Hand.Count;
            g.Do(PlayerAction.Play(me, gossip.Id));
            g.PassRound();
            Assert.AreEqual(DecisionKind.ChooseFromTop, g.State.Pending?.Kind);
            Assert.AreEqual(3, g.Legal(me).Count);
            g.Do(PlayerAction.ChooseOption(me, 1));
            Assert.AreEqual(hand, g.P(me).Hand.Count, "cast one, got one");
            Assert.AreEqual(3, g.P(me).Graveyard.Count, "two cards + Grave Gossip");
            Assert.AreEqual(29, g.P(me).Life);
            Assert.IsFalse(g.P(me).Deck.Any(c => top.Contains(c)));
        }

        [Test]
        public void StageMedium_DrawsWhenYourCurseFallsOff()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "stage_medium");
            var victim = g.AddToBattlefield(g.Other, "goober_rascal"); // 2/1
            var hex = g.AddToBattlefield(me, "hex_of_frailty");
            hex.AttachedToObject = victim.Id;
            victim.Damage = 0;
            var snot = g.AddToHand(me, "spark_snot");
            g.SetMana(me, 1);
            int hand = g.P(me).Hand.Count;
            g.Do(PlayerAction.Play(me, snot.Id, Target.ForObject(victim.Id)));
            g.PassRound(); // the Rascal dies, then the Hex goes to the graveyard (state-based actions)
            Assert.AreEqual(1, g.State.Chain.Count);
            g.PassRound();
            Assert.AreEqual(hand, g.P(me).Hand.Count, "cast Spark Snot, drew one");
        }

        [Test]
        public void EncoreFromBeyond_Returns_ThenIsExiledAtEndOfTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var dead = new CardInstance
            {
                Id = new ObjectId(g.State.NextObjectId++), DefinitionId = "hog_rider", Owner = me, Controller = me, Zone = Zone.Graveyard,
            };
            g.P(me).Graveyard.Add(dead);
            var encore = g.AddToHand(me, "encore_from_beyond");
            g.SetMana(me, 3);
            g.Do(PlayerAction.Play(me, encore.Id, Target.ForObject(dead.Id)));
            g.PassRound();
            var rider = g.OnBattlefield(me, "hog_rider");
            Assert.IsNotNull(rider);
            Assert.AreEqual(1, g.State.DelayedTriggers.Count);

            g.PassUntil(s => s.Step == Step.End && s.Chain.Count > 0);
            g.PassRound();
            Assert.IsNull(g.OnBattlefield(me, "hog_rider"));
            Assert.IsTrue(g.P(me).Exile.Any(c => c.DefinitionId == "hog_rider"));
            Assert.IsEmpty(g.State.DelayedTriggers);
        }
    }
}
