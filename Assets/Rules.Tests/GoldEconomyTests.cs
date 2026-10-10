using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// Set v0.2 batch A, the Gold economy (GAME_DESIGN §5.2): banking and "whenever you bank Gold"
    /// (cleanup-step triggers, MTG 514.3a), "whenever you spend Gold" (once per payment), the
    /// per-player Gold cap, and "pay any amount of Gold (X)".
    /// </summary>
    public class GoldEconomyTests
    {
        [Test]
        public void BankTrigger_GoesOnTheChainInCleanup_ThenCleanupRepeats()
        {
            var g = TestGame.Classic();
            var me = g.Active;
            var other = g.Other;
            g.AddToBattlefield(me, "interest_broker");
            g.SetMana(me, 3);
            int hand = g.P(me).Hand.Count;
            Assert.AreEqual(7, hand);

            g.PassUntil(s => s.Step == Step.Cleanup && s.Chain.Count > 0);
            Assert.AreEqual(3, g.P(me).Gold, "3 unspent mana banked");
            Assert.AreEqual(me, g.State.PriorityPlayer, "MTG 514.3a: players get priority in the cleanup step");
            Assert.AreEqual(me, g.State.ActivePlayer, "still the same turn");

            g.PassRound();
            Assert.AreEqual(hand + 1, g.P(me).Hand.Count, "Interest Broker drew a card");
            Assert.AreEqual(Step.Cleanup, g.State.Step, "the cleanup step repeats");
            Assert.AreEqual(DecisionKind.DiscardToHandSize, g.State.Pending?.Kind, "8 cards: discard again in the new cleanup step");

            g.Do(PlayerAction.Discard(me, g.P(me).Hand[0].Id));
            Assert.AreEqual(other, g.State.ActivePlayer, "then the next turn starts");
            Assert.AreEqual(3, g.P(me).Gold, "nothing is banked twice");
        }

        [Test]
        public void BankTrigger_CountsOnlyTheGoldActuallyGained()
        {
            var g = TestGame.Classic();
            var me = g.Active;
            g.AddToBattlefield(me, "interest_broker"); // "2 or more"
            g.SetMana(me, 3);
            g.P(me).Gold = 4;                          // cap 5: only 1 is banked

            g.PassUntil(s => s.ActivePlayer != me);
            Assert.AreEqual(5, g.P(me).Gold);
            Assert.AreEqual(7, g.P(me).Hand.Count, "banked 1, so no draw");
            var bank = g.Events.OfType<GoldBankedEvent>().Last();
            Assert.AreEqual(3, bank.UnspentMana);
            Assert.AreEqual(1, bank.Banked);
        }

        [Test]
        public void GrizzledInnkeeper_HealsTheAmountBanked()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "grizzled_innkeeper");
            var bouncer = g.AddToBattlefield(me, "tavern_bouncer", damage: 4);
            g.SetMana(me, 3);

            g.PassUntil(s => s.Pending?.Kind == DecisionKind.ChooseTriggerTarget);
            Assert.AreEqual(Step.Cleanup, g.State.Step);
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(bouncer.Id)));
            g.PassRound();
            Assert.AreEqual(1, bouncer.Damage, "healed 3, the Gold banked");
        }

        [Test]
        public void OffshoreAccount_RaisesTheCap_AndExcessIsLostWhenItLeaves()
        {
            var g = TestGame.Classic();
            var me = g.Active;
            var account = g.AddToBattlefield(me, "offshore_account");
            g.SetMana(me, 6);
            g.P(me).Gold = 4;
            Assert.AreEqual(8, GoldRules.Cap(g.State, g.Engine.Cards, me));
            Assert.AreEqual(5, GoldRules.Cap(g.State, g.Engine.Cards, g.Other), "the cap is per player");

            g.PassUntil(s => s.ActivePlayer != me && s.Step == Step.Main1 && s.PriorityPlayer.HasValue);
            Assert.AreEqual(8, g.P(me).Gold, "banked 4 of 6, up to the new cap");

            g.P(me).Battlefield.Remove(account); // test setup: the Account leaves
            g.PassRound();                       // next step: state-based actions are checked
            Assert.AreEqual(5, g.P(me).Gold, "decided 2026-10-09: Gold above the cap is lost at once");
        }

        [Test]
        public void FeeCollector_TriggersOncePerPayment_OnlyForOpponents()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "fee_collector");
            var target = g.AddToBattlefield(me, "tavern_bouncer");
            var snot = g.AddToHand(g.Other, "spark_snot");
            g.P(g.Other).Gold = 3;

            g.Pass();
            g.Do(PlayerAction.Play(g.Other, snot.Id, Target.ForObject(target.Id)));
            Assert.AreEqual(2, g.State.Chain.Count, "the trigger goes on top of the spell");
            g.PassRound();
            Assert.AreEqual(1, g.P(me).Gold);

            // Spending your own Gold doesn't count.
            g.PassRound();
            var jar = g.AddToBattlefield(me, "tip_jar");
            g.P(me).Gold = 3;
            g.Do(g.Activations(me, jar).Single());
            Assert.AreEqual(1, g.State.Chain.Count, "no Fee Collector trigger");
        }

        [Test]
        public void TravelingBard_NeedsThreeGoldOnOneSpellOrAbility_InvestCounts()
        {
            var g = TestGame.Classic();
            var me = g.Active;
            g.AddToBattlefield(me, "traveling_bard");
            var muscle = g.AddToBattlefield(me, "hired_muscle");
            var jar = g.AddToBattlefield(me, "tip_jar");
            g.P(me).Gold = 5;

            g.Do(g.Activations(me, muscle).Single()); // Pay 2 Gold
            Assert.AreEqual(1, g.State.Chain.Count, "2 Gold: no trigger");
            g.PassRound();

            g.Do(g.Activations(me, jar).Single());    // Pay 3 Gold
            Assert.AreEqual(2, g.State.Chain.Count, "3 Gold: Bard triggers");
            g.PassRound();
            g.PassRound();

            // House Special: 2 mana for the spell, then Invest 3 is paid with Gold.
            var special = g.AddToHand(me, "house_special");
            g.SetMana(me, 2);
            g.P(me).Gold = 3;
            g.Do(PlayerAction.Play(me, special.Id, System.Array.Empty<Target>(), invest: true));
            Assert.AreEqual(2, g.State.Chain.Count, "Invest Gold is spent Gold too");
        }

        [Test]
        public void CoinJuggler_PumpsWhenYouSpendGold_NotMana()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var juggler = g.AddToBattlefield(me, "coin_juggler");
            var victim = g.AddToBattlefield(g.Other, "tavern_bouncer");
            var snot = g.AddToHand(me, "spark_snot");
            g.SetMana(me, 1);
            g.Do(PlayerAction.Play(me, snot.Id, Target.ForObject(victim.Id)));
            Assert.AreEqual(1, g.State.Chain.Count, "paid with mana: no trigger");
            g.PassRound();

            var jar = g.AddToBattlefield(me, "tip_jar");
            g.P(me).Gold = 3;
            g.Do(g.Activations(me, jar).Single());
            g.PassRound();
            Assert.AreEqual(2, g.Stats(juggler).Power);
        }

        [Test]
        public void SettleTheTab_PayAnyAmountOfGold_DrawXThenDiscard()
        {
            var g = TestGame.Classic();
            var me = g.Active;
            var settle = g.AddToHand(me, "settle_the_tab");
            g.SetMana(me, 3);
            g.P(me).Gold = 4;

            var plays = g.Legal(me).Where(a => a.Kind == ActionKind.PlayCard && a.Card == settle.Id).ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4 }, plays.Select(a => a.X), "X from 0 up to the Gold you have");

            int hand = g.P(me).Hand.Count;
            g.Do(plays.Single(a => a.X == 3));
            Assert.AreEqual(1, g.P(me).Gold, "X is paid with Gold only");
            Assert.AreEqual(0, g.P(me).Mana);
            g.PassRound();
            Assert.AreEqual(hand - 1 + 3, g.P(me).Hand.Count, "drew 3");
            Assert.AreEqual(DecisionKind.DiscardCards, g.State.Pending?.Kind);
            Assert.IsTrue(g.Legal(me).All(a => a.Kind == ActionKind.Discard));

            g.Do(PlayerAction.Discard(me, g.P(me).Hand[0].Id));
            Assert.AreEqual(hand + 1, g.P(me).Hand.Count);
            Assert.IsNull(g.State.Pending);
            Assert.AreEqual(me, g.State.PriorityPlayer);
        }

        [Test]
        public void CompoundInterest_DrawsThreeWhenThreeGoldWasSpent()
        {
            // Spells pay Gold first (§5.2): at the cap of 3 Gold, casting it spends 3 Gold.
            foreach (var (gold, draws) in new[] { (2, 2), (3, 3) })
            {
                var g = TestGame.AtFirstMainPhase();
                var me = g.Active;
                var spell = g.AddToHand(me, "compound_interest");
                g.SetMana(me, 4);
                g.P(me).Gold = gold;
                int hand = g.P(me).Hand.Count;
                g.Do(PlayerAction.Play(me, spell.Id));
                g.PassRound();
                Assert.AreEqual(hand - 1 + draws, g.P(me).Hand.Count, gold + " Gold");
            }
        }

        [Test]
        public void SpilledDrink_TapsTheCreature()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var blocker = g.AddToBattlefield(g.Other, "tavern_bouncer");
            var drink = g.AddToHand(me, "spilled_drink");
            g.SetMana(me, 1);
            g.Do(PlayerAction.Play(me, drink.Id, Target.ForObject(blocker.Id)));
            g.PassRound();
            Assert.IsTrue(blocker.Tapped);
        }

        [Test]
        public void GooberSapper_LastBreath_DamagesEachEnemyCreature()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var sapper = g.AddToBattlefield(me, "goober_sapper");
            var mine = g.AddToBattlefield(me, "tavern_bouncer");
            var a = g.AddToBattlefield(g.Other, "tavern_bouncer");
            var b = g.AddToBattlefield(g.Other, "hired_sellsword");
            var snot = g.AddToHand(me, "spark_snot");
            g.SetMana(me, 1);
            g.Do(PlayerAction.Play(me, snot.Id, Target.ForObject(sapper.Id)));
            g.PassRound();
            Assert.AreEqual(1, g.State.Chain.Count, "Last Breath trigger");
            g.PassRound();
            Assert.AreEqual(1, a.Damage);
            Assert.AreEqual(1, b.Damage);
            Assert.AreEqual(0, mine.Damage);
        }

        [Test]
        public void StampedeOfTheDeep_PumpsAndHealsFully()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var bouncer = g.AddToBattlefield(me, "tavern_bouncer", damage: 4);
            var stampede = g.AddToHand(me, "stampede_of_the_deep");
            g.SetMana(me, 6);
            g.Do(PlayerAction.Play(me, stampede.Id));
            g.PassRound();
            var st = g.Stats(bouncer);
            Assert.AreEqual(0, bouncer.Damage);
            Assert.AreEqual(4, st.Power);
            Assert.AreEqual(7, st.MaxHealth);
            Assert.IsTrue(st.Has(Keyword.Trample));
        }
    }
}
