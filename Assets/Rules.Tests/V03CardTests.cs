using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.AI;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>Set v0.3 (mana scarcity): X costs, mana sinks, drains and finishers.</summary>
    public class V03CardTests
    {
        private static PlayerAction PlayWithX(TestGame g, PlayerId p, CardInstance card, int x, Target? target = null) =>
            g.Legal(p).First(a => a.Kind == ActionKind.PlayCard && a.Card == card.Id && a.X == x
                                  && (!target.HasValue || (a.Targets.Length > 0 && a.Targets[0] == target.Value)));

        private static void Resolve(TestGame g) => g.PassRound();

        [Test]
        public void XSpell_OffersEveryPayableX_AndUsesGoldToo()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 2);
            g.P(me).Gold = 2;
            var boom = g.AddToHand(me, "big_boom"); // X+1
            var xs = g.Legal(me).Where(a => a.Card == boom.Id).Select(a => a.X).Distinct().OrderBy(x => x).ToList();
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, xs, "2 mana + 2 Gold pays 1 + X up to X = 3");

            g.Do(PlayWithX(g, me, boom, 3, Target.ForPlayer(g.Other)));
            Resolve(g);
            Assert.AreEqual(27, g.P(g.Other).Life);
            Assert.AreEqual(0, g.P(me).Mana);
            Assert.AreEqual(0, g.P(me).Gold);
        }

        [Test]
        public void EvictionNotice_OnlyTargetsCreaturesCostingXOrLess()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 4);
            var big = g.AddToBattlefield(g.Other, "pit_fighter");  // cost 4
            var small = g.AddToBattlefield(g.Other, "spark_drone"); // cost 2
            var notice = g.AddToHand(me, "eviction_notice");        // X+1
            var plays = g.Legal(me).Where(a => a.Card == notice.Id).ToList();
            Assert.IsFalse(plays.Any(a => a.Target.Value.Object == big.Id), "Pit-Fighter costs 4; at most X = 3 is payable");
            Assert.IsTrue(plays.Any(a => a.Target.Value.Object == small.Id && a.X == 2));
            Assert.IsFalse(plays.Any(a => a.Target.Value.Object == small.Id && a.X == 1));

            int hand = g.P(me).Hand.Count;
            g.Do(PlayWithX(g, me, notice, 2, Target.ForObject(small.Id)));
            Resolve(g);
            Assert.IsNull(g.State.FindOnBattlefield(small.Id));
            Assert.AreEqual(hand, g.P(me).Hand.Count, "cast one, drew one");
        }

        [Test]
        public void GooberAvalanche_CreatesXHastyGoobers()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 4);
            var avalanche = g.AddToHand(me, "goober_avalanche");
            g.Do(PlayWithX(g, me, avalanche, 3));
            Resolve(g);
            var goobers = g.P(me).Battlefield.Where(c => c.DefinitionId == CardPool.GooberToken).ToList();
            Assert.AreEqual(3, goobers.Count);
            Assert.IsTrue(goobers.All(c => g.Stats(c).Has(Keyword.Haste)));
        }

        [Test]
        public void WitherAway_GivesMinusXMinusX()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 4);
            var grizzly = g.AddToBattlefield(g.Other, "ironbark_grizzly"); // 4/5
            var wither = g.AddToHand(me, "wither_away");
            Assert.IsFalse(g.Legal(me).Any(a => a.Card == wither.Id && a.X > 3));
            g.Do(PlayWithX(g, me, wither, 3, Target.ForObject(grizzly.Id)));
            Resolve(g);
            Assert.AreEqual(1, g.Stats(grizzly).Power);
            Assert.AreEqual(2, g.Stats(grizzly).RemainingHealth, "-3/-3 on a 4/5");
        }

        [Test]
        public void ArcCascade_DividesXDamageOnePointAtATime()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 4);
            var spider = g.AddToBattlefield(g.Other, "vine_spider"); // 2/3
            var arc = g.AddToHand(me, "arc_cascade");
            g.Do(PlayWithX(g, me, arc, 3));
            Resolve(g);
            Assert.AreEqual(DecisionKind.DivideDamage, g.State.Pending.Kind);
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(spider.Id)));
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(spider.Id)));
            Assert.AreEqual(0, spider.Damage, "nothing is dealt until every point is assigned");
            g.Do(PlayerAction.ChooseTarget(me, Target.ForPlayer(g.Other)));
            Assert.AreEqual(2, spider.Damage);
            Assert.AreEqual(29, g.P(g.Other).Life);
            Assert.IsNull(g.State.Pending);
        }

        [Test]
        public void Foreclosure_DrainsXLifeAndGivesXGold()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 5);
            var fore = g.AddToHand(me, "foreclosure"); // X+3
            g.Do(PlayWithX(g, me, fore, 2));
            Resolve(g);
            Assert.AreEqual(28, g.P(g.Other).Life);
            Assert.AreEqual(2, g.P(me).Gold);
        }

        [Test]
        public void LoanShark_DrainsYourGoldAtEndOfTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var opponent = g.Other;
            g.AddToBattlefield(me, "loan_shark");
            g.P(me).Gold = 3;
            g.SetMana(me, 0);
            g.PassUntil(s => s.ActivePlayer != me);
            Assert.AreEqual(27, g.P(opponent).Life);
        }

        [Test]
        public void CurseOfPrimeTime_DrainsAtTheirTurnStart_AndWithItsAbility()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var opponent = g.Other;
            g.SetMana(me, 7);
            var curse = g.AddToHand(me, "curse_of_prime_time");
            g.Do(g.Legal(me).First(a => a.Card == curse.Id));
            Resolve(g);
            var onBoard = g.OnBattlefield(me, "curse_of_prime_time");
            Assert.AreEqual(g.Other, onBoard.AttachedToPlayer);
            g.Do(g.Activations(me, onBoard).Single());
            Resolve(g);
            Assert.AreEqual(29, g.P(g.Other).Life);
            Assert.AreEqual(30, g.P(me).Life, "life gain is capped at starting life");
            g.PassUntil(s => s.ActivePlayer != me && s.Step == Step.Main1);
            Assert.AreEqual(28, g.P(opponent).Life, "lost 1 at the start of their turn");
        }

        [Test]
        public void DumpsterDive_DiscardTwoDrawThree()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 2);
            var dive = g.AddToHand(me, "dumpster_dive");
            g.Do(g.Legal(me).First(a => a.Card == dive.Id));
            Resolve(g);
            int before = g.P(me).Hand.Count;
            g.Do(g.Legal(me).First(a => a.Target.HasValue));
            g.Do(g.Legal(me).First(a => a.Target.HasValue));
            g.Do(g.Legal(me).First(a => !a.Target.HasValue)); // stop discarding
            Assert.AreEqual(before - 2 + 3, g.P(me).Hand.Count);
            Assert.AreEqual(2 + 1, g.P(me).Graveyard.Count, "two discards and the spell");
        }

        [Test]
        public void RampagingTitan_PumpsByX()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 3);
            var titan = g.AddToBattlefield(me, "rampaging_titan");
            var xs = g.Activations(me, titan).Select(a => a.X).OrderBy(x => x).ToList();
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, xs);
            g.Do(g.Activations(me, titan).Single(a => a.X == 3));
            Resolve(g);
            Assert.AreEqual(10, g.Stats(titan).Power);
        }

        [Test]
        public void GildedMercenary_InvestGivesCountersAndTrample()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 4);
            g.P(me).Gold = 3;
            var merc = g.AddToHand(me, "gilded_mercenary");
            g.Do(g.Legal(me).First(a => a.Card == merc.Id && a.Invest));
            Resolve(g);
            var onBoard = g.OnBattlefield(me, "gilded_mercenary");
            Assert.AreEqual(6, g.Stats(onBoard).Power);
            Assert.IsTrue(g.Stats(onBoard).Has(Keyword.Trample));
        }

        [Test]
        public void OpenCasket_ToHand_OrBattlefieldWhenInvested()
        {
            var g = TestGame.Classic();
            var me = g.Active;
            var dead = g.AddToBattlefield(me, "spark_drone");
            g.P(me).Battlefield.Remove(dead);
            dead.Zone = Zone.Graveyard;
            g.P(me).Graveyard.Add(dead);
            g.SetMana(me, 3);
            g.P(me).Gold = 3;
            var casket = g.AddToHand(me, "open_casket");
            g.Do(g.Legal(me).First(a => a.Card == casket.Id && a.Invest));
            Resolve(g);
            Assert.IsNotNull(g.OnBattlefield(me, "spark_drone"));
        }

        [Test]
        public void MarketDataFeed_DrawsOnAttach_OncePerTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "market_data_feed");
            var creature = g.AddToBattlefield(me, "hired_sellsword");
            var rig1 = g.AddToBattlefield(me, "turret_rig");
            var rig2 = g.AddToBattlefield(me, "turret_rig");
            g.SetMana(me, 4);
            int hand = g.P(me).Hand.Count;
            g.Do(g.Activations(me, rig1).First(a => a.Target.Value.Object == creature.Id));
            Resolve(g);
            Resolve(g); // the draw trigger
            g.Do(g.Activations(me, rig2).First(a => a.Target.Value.Object == creature.Id));
            Resolve(g);
            Assert.AreEqual(hand + 1, g.P(me).Hand.Count, "triggers at most once each turn");
        }

        [Test]
        public void ClosingBell_OnlyWhenEveryOpponentIsLow()
        {
            var g = TestGame.Classic();
            var me = g.Active;
            g.AddToBattlefield(me, "closing_bell");
            g.PassUntil(s => s.ActivePlayer != me);
            g.PassUntil(s => s.ActivePlayer == me && s.Step == Step.Main1);
            Assert.AreEqual(30, g.P(g.Other).Life, "30 life: no drain");
            g.P(g.Other).Life = 10;
            g.PassUntil(s => s.ActivePlayer != me);
            g.PassUntil(s => s.ActivePlayer == me && s.Step == Step.Main1);
            Assert.AreEqual(8, g.P(g.Other).Life);
        }

        [Test]
        public void OrbitalLaser_AlsoPingsAtXFiveOrMore()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 7);
            var spider = g.AddToBattlefield(g.Other, "vine_spider");
            var laser = g.AddToHand(me, "orbital_laser"); // X+2
            g.Do(PlayWithX(g, me, laser, 5, Target.ForPlayer(g.Other)));
            Resolve(g);
            Assert.AreEqual(25, g.P(g.Other).Life);
            Assert.AreEqual(1, spider.Damage);
        }

        [Test]
        public void CallOfTheDeep_PumpsThenFights()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.SetMana(me, 5);
            var mine = g.AddToBattlefield(me, "hired_sellsword");  // 2/3
            var theirs = g.AddToBattlefield(g.Other, "pit_fighter"); // 4/4
            var call = g.AddToHand(me, "call_of_the_deep");          // X+2
            g.Do(g.Legal(me).First(a => a.Card == call.Id && a.X == 3 && a.Targets.Length == 2 && a.Targets[1] == Target.ForObject(theirs.Id)));
            Resolve(g);
            Assert.IsNull(g.State.FindOnBattlefield(theirs.Id), "5 damage kills the 4/4");
            Assert.AreEqual(5, g.Stats(mine).Power);
            Assert.AreEqual(2, g.Stats(mine).RemainingHealth, "a 5/6 that took 4");
        }

        [Test]
        public void EveryV03Card_IsPlayedByBots_WithoutErrors()
        {
            var db = CardPool.CreateDatabase();
            var engine = new GameEngine(db);
            var ids = new[]
            {
                "ledger_clerk", "insider_trading", "eviction_notice", "audit_the_books", "foreclosure", "loan_shark",
                "dumpster_dive", "goober_bookie", "goober_avalanche", "fireworks_stand", "big_boom", "grand_finale",
                "blood_oath", "seance_hotline", "wither_away", "open_casket", "final_broadcast", "curse_of_prime_time",
                "gift_of_the_grove", "watering_hole", "overgrowth", "mossgut_grower", "call_of_the_deep", "rampaging_titan",
                "market_data_feed", "overclocked_analyst", "arc_cascade", "turret_rig", "orbital_laser", "satellite_uplink",
                "last_orders", "night_shift_barkeep", "tavern_brawl_champion", "gilded_mercenary", "tavern_legend", "closing_bell",
            };
            foreach (var id in ids) Assert.IsTrue(db.Contains(id), id);
            var deck = ids.Concat(ids).Take(60).ToList();
            var format = FormatConfig.Standard();
            format.EnforceDeckRules = false;
            var bot = new GreedyBot(engine);
            for (ulong seed = 1; seed <= 6; seed++)
            {
                var state = engine.CreateGame(format, new[] { new PlayerSetup { Deck = deck }, new PlayerSetup { Deck = new List<string>(deck) } }, seed);
                for (int i = 0; i < 4000 && !state.IsGameOver; i++)
                {
                    var who = engine.WaitingOn(state).Value;
                    engine.Apply(state, bot.Choose(state, who));
                }
            }
        }
    }
}
