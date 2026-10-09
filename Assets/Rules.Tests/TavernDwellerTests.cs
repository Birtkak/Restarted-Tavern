using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>Tavern Dwellers (GAME_DESIGN §9): the Tavern Dweller zone, deck validation, Powers and the 10 passives.</summary>
    public class TavernDwellerTests
    {
        // ------------------------------------------------------------------ zone and deck rules

        [Test]
        public void TavernDweller_StartsInTheTavernDwellerZone()
        {
            var engine = new GameEngine(CardPool.CreateDatabase());
            var s = engine.CreateGame(FormatConfig.Standard(), new[]
            {
                new PlayerSetup { Deck = CardPool.ZooPatrolDeck(), TavernDwellerId = CardPool.ZooPatrolTavernDweller },
                new PlayerSetup { Deck = CardPool.GooberMobDeck(), TavernDwellerId = CardPool.GooberMobTavernDweller },
            }, 1);
            Assert.AreEqual("keeper_z00", s.Players[0].TavernDweller.DefinitionId);
            Assert.AreEqual(Zone.TavernDweller, s.Players[0].TavernDweller.Zone);
            Assert.AreEqual("skabba", s.CreateViewFor(s.Players[0].Id).Players[1].TavernDweller.DefinitionId, "the Tavern Dweller zone is public");
        }

        [Test]
        public void DeckValidation_ChecksTheTavernDwellersFactions()
        {
            var db = CardPool.CreateDatabase();
            var f = FormatConfig.Standard();
            Assert.DoesNotThrow(() => DeckValidator.Validate(db, f, CardPool.AuditorsArsenalDeck(), CardPool.AuditorsArsenalTavernDweller));
            Assert.Throws<ArgumentException>(() => DeckValidator.Validate(db, f, CardPool.ZooPatrolDeck()), "needs a Tavern Dweller");
            Assert.Throws<ArgumentException>(() => DeckValidator.Validate(db, f, CardPool.ZooPatrolDeck(), "skabba"),
                "Wild and Glitterworld cards are outside Goobers + Sensationalists");
            Assert.Throws<ArgumentException>(() => DeckValidator.Validate(db, f, CardPool.ZooPatrolDeck(), "hired_sellsword"));
            var withTavernDwellerCard = CardPool.ZooPatrolDeck();
            withTavernDwellerCard[0] = "keeper_z00";
            Assert.Throws<ArgumentException>(() => DeckValidator.Validate(db, f, withTavernDwellerCard, "keeper_z00"));
        }

        [Test]
        public void EveryPrototypeDeck_IsLegalWithItsTavernDweller()
        {
            var db = CardPool.CreateDatabase();
            Assert.AreEqual(10, db.All.Count(c => c.IsTavernDweller));
            foreach (var (deck, tavernDweller) in new (List<string>, string)[]
                     {
                         (CardPool.GooberMobDeck(), CardPool.GooberMobTavernDweller),
                         (CardPool.JungleStampedeDeck(), CardPool.JungleStampedeTavernDweller),
                         (CardPool.ZooPatrolDeck(), CardPool.ZooPatrolTavernDweller),
                         (CardPool.VespersLedgerDeck(), CardPool.VespersLedgerTavernDweller),
                         (CardPool.SparkwrenchScrappersDeck(), CardPool.SparkwrenchScrappersTavernDweller),
                         (CardPool.AuditorsArsenalDeck(), CardPool.AuditorsArsenalTavernDweller),
                     })
                Assert.DoesNotThrow(() => DeckValidator.Validate(db, FormatConfig.Standard(), deck, tavernDweller), tavernDweller);
        }

        // ------------------------------------------------------------------ Powers

        [Test]
        public void Power_OnceEachTurn_AtInstantSpeed_PaidWithGoldOnTheOpponentsTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var keeper = g.SetTavernDweller(me, "keeper_z00");
            var hurt = g.AddToBattlefield(me, "tavern_bouncer", damage: 4);
            g.SetMana(me, 3);
            g.P(me).Gold = 0;

            g.Do(g.Activations(me, keeper).Single(a => a.Target == Target.ForObject(hurt.Id)));
            Assert.AreEqual(1, g.P(me).Mana, "mana first");
            g.PassRound();
            Assert.AreEqual(1, hurt.Damage, "heal 3");
            Assert.IsEmpty(g.Activations(me, keeper), "once each turn");

            hurt.Damage = 4;
            g.PassToStep(Step.Main1, g.Other);
            g.P(me).Gold = 1;
            g.Pass();
            Assert.AreEqual(me, g.State.PriorityPlayer);
            Assert.AreEqual(0, g.P(me).Mana, "no mana on other players' turns (§5.2)");
            Assert.IsEmpty(g.Activations(me, keeper), "1 Gold can't pay (2)");

            g.P(me).Gold = 3;
            g.Do(g.Activations(me, keeper).Single(a => a.Target == Target.ForObject(hurt.Id)));
            Assert.AreEqual(1, g.P(me).Gold);
            var item = g.State.Chain.Single();
            Assert.IsTrue(item.IsTavernDwellerPower);
            g.PassRound();
            Assert.AreEqual(1, hurt.Damage, "usable again on the opponent's turn (MTG: once each turn)");
        }

        [Test]
        public void Power_CanBeRespondedTo()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var vox = g.SetTavernDweller(me, "vox_nocturne");
            var rascal = g.AddToBattlefield(g.Other, "goober_rascal"); // 2/1
            var tonic = g.AddToHand(g.Other, "barkeeps_tonic");
            g.SetMana(me, 2);
            g.P(g.Other).Gold = 1;

            g.Do(g.Activations(me, vox).Single(a => a.Target == Target.ForObject(rascal.Id)));
            g.Pass();
            Assert.AreEqual(g.Other, g.State.PriorityPlayer);
            Assert.IsTrue(g.Legal(g.Other).Any(a => a.Card == tonic.Id), "the opponent can respond to a Tavern Dweller Power");
        }

        // ------------------------------------------------------------------ the 10 Tavern Dwellers

        [Test]
        public void Grizzle_BigSpellsMakeAGoober_AndThePowerPings()
        {
            var g = TestGame.AtFirstMainPhase();
            var grizzle = g.SetTavernDweller(g.Active, "grizzle_coinflick");
            var champ = g.AddToHand(g.Active, "pit_champion"); // costs 6
            g.SetMana(g.Active, 8);
            g.Do(PlayerAction.Play(g.Active, champ.Id));
            Assert.AreEqual(2, g.State.Chain.Count, "the trigger goes on top of the spell");
            g.PassRound();
            Assert.IsNotNull(g.OnBattlefield(g.Active, CardPool.GooberToken));

            g.PassRound();
            g.Do(g.Activations(g.Active, grizzle).Single(a => a.Target == Target.ForPlayer(g.Other)));
            g.PassRound();
            Assert.AreEqual(29, g.P(g.Other).Life);
        }

        [Test]
        public void Vesper_GainsGold_WhenAnOpponentsCreatureDies()
        {
            var g = TestGame.AtFirstMainPhase();
            g.SetTavernDweller(g.Active, "madame_vesper");
            var theirs = g.AddToBattlefield(g.Other, "goober_rascal");
            var mine = g.AddToBattlefield(g.Active, "goober_rascal");
            var snot1 = g.AddToHand(g.Active, "spark_snot");
            var snot2 = g.AddToHand(g.Active, "spark_snot");
            g.SetMana(g.Active, 2);
            g.P(g.Active).Gold = 0;

            g.Do(PlayerAction.Play(g.Active, snot1.Id, Target.ForObject(mine.Id)));
            g.PassRound();
            Assert.AreEqual(0, g.P(g.Active).Gold, "own creature: nothing");
            g.Do(PlayerAction.Play(g.Active, snot2.Id, Target.ForObject(theirs.Id)));
            g.PassRound();
            g.PassRound();
            Assert.AreEqual(1, g.P(g.Active).Gold);
        }

        [Test]
        public void Mossbank_BigCardsCostOneLess()
        {
            var g = TestGame.AtFirstMainPhase();
            g.SetTavernDweller(g.Active, "old_mossbank");
            var behemoth = g.AddToHand(g.Active, "primeval_behemoth"); // 6
            var mammoth = g.AddToHand(g.Active, "tusked_mammoth");     // 5: no discount
            g.SetMana(g.Active, 5);
            Assert.IsTrue(g.Legal(g.Active).Any(a => a.Card == behemoth.Id));
            g.Do(PlayerAction.Play(g.Active, behemoth.Id));
            Assert.AreEqual(0, g.P(g.Active).Mana);
            g.PassRound(); // the Behemoth
            g.PassRound(); // its Arrival trigger
            g.SetMana(g.Active, 5);
            Assert.IsTrue(g.Legal(g.Active).Any(a => a.Card == mammoth.Id));
            g.SetMana(g.Active, 4);
            Assert.IsFalse(g.Legal(g.Active).Any(a => a.Card == mammoth.Id));
        }

        [Test]
        public void Auditor_LowersInvestAndEquip_AndThePowerLooksAtTheTopCard()
        {
            var g = TestGame.AtFirstMainPhase();
            var auditor = g.SetTavernDweller(g.Active, "auditor_prime");
            var ledger = g.AddToHand(g.Active, "the_grand_ledger"); // Invest 3
            g.SetMana(g.Active, 7);
            g.P(g.Active).Gold = 2;
            Assert.IsTrue(g.Legal(g.Active).Any(a => a.Card == ledger.Id && a.Invest), "Invest 3 → 2");

            var rail = g.AddToBattlefield(g.Active, "rail_cannon"); // Equip 3
            g.AddToBattlefield(g.Active, "hired_sellsword");
            g.SetMana(g.Active, 2);
            g.P(g.Active).Gold = 0;
            Assert.AreEqual(1, g.Activations(g.Active, rail).Count, "Equip 3 → 2");

            var top = g.P(g.Active).Deck[0].Id;
            g.Do(g.Activations(g.Active, auditor).Single());
            g.PassRound();
            Assert.AreEqual(DecisionKind.TopOrBottom, g.State.Pending.Kind);
            Assert.AreEqual(top, g.State.Pending.Card);
            g.Do(PlayerAction.ChooseOption(g.Active, 1));
            Assert.AreNotEqual(top, g.P(g.Active).Deck[0].Id);
            Assert.AreEqual("hired_sellsword", g.P(g.Active).Deck[g.P(g.Active).Deck.Count - 1].DefinitionId);
            Assert.AreEqual(g.Active, g.State.PriorityPlayer, "priority comes back after the choice");
        }

        [Test]
        public void Skabba_TriggersAtMostThreeTimesEachTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            g.SetTavernDweller(g.Other, "skabba");
            for (int i = 0; i < 4; i++) g.AddToBattlefield(g.Other, CardPool.GooberToken);
            var overload = g.AddToHand(g.Active, "grid_overload");
            g.SetMana(g.Active, 4);
            g.Do(PlayerAction.Play(g.Active, overload.Id));
            g.PassRound();
            Assert.AreEqual(3, g.State.Chain.Count, "4 Goobers died, but only 3 triggers");
            for (int i = 0; i < 3; i++) g.PassRound();
            Assert.AreEqual(27, g.P(g.Active).Life);
        }

        [Test]
        public void Mukk_TrampleCreaturesGetPlusOne_AndThePowerGivesTrample()
        {
            var g = TestGame.AtFirstMainPhase();
            var mukk = g.SetTavernDweller(g.Active, "mukk_the_grub_king");
            var hog = g.AddToBattlefield(g.Active, "hog_rider");      // 3/3 Trample
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword"); // 2/3
            Assert.AreEqual(4, g.Stats(hog).Power);
            Assert.AreEqual(2, g.Stats(sword).Power);
            g.SetMana(g.Active, 2);
            g.Do(g.Activations(g.Active, mukk).Single(a => a.Target == Target.ForObject(sword.Id)));
            g.PassRound();
            Assert.IsTrue(g.Stats(sword).Has(Keyword.Trample));
            Assert.AreEqual(3, g.Stats(sword).Power, "granted Trample counts for the passive (layer 6 before 7c)");
        }

        [Test]
        public void Sparkwrench_CheaperEquipment_AndThePowerMovesItAtInstantSpeed()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var wrench = g.SetTavernDweller(me, "sparkwrench");
            var shiv = g.AddToHand(me, "neon_shiv");
            var a = g.AddToBattlefield(me, "hired_sellsword");
            var b = g.AddToBattlefield(me, "tavern_bouncer");
            g.SetMana(me, 0);
            g.Do(PlayerAction.Play(me, shiv.Id));
            g.PassRound();
            var onField = g.OnBattlefield(me, "neon_shiv");
            onField.AttachedToObject = a.Id;

            g.PassToStep(Step.Main1, g.Other);
            g.P(me).Gold = 2;
            g.Pass();
            var moves = g.Activations(me, wrench);
            Assert.AreEqual(1, moves.Count, "only to another creature");
            g.Do(moves[0]);
            g.PassRound();
            Assert.AreEqual(b.Id, onField.AttachedToObject);
        }

        [Test]
        public void Rotmother_BigCreatureDiesIntoASpawn_AndThePowerReturnsACreature()
        {
            var g = TestGame.AtFirstMainPhase();
            var rot = g.SetTavernDweller(g.Active, "the_rotmother");
            var mammoth = g.AddToBattlefield(g.Active, "tusked_mammoth", damage: 4); // 5/5
            var snot = g.AddToHand(g.Other, "spark_snot");
            g.P(g.Other).Gold = 1;
            g.Pass();
            g.Do(PlayerAction.Play(g.Other, snot.Id, Target.ForObject(mammoth.Id)));
            g.PassRound();
            g.PassRound();
            Assert.IsNotNull(g.OnBattlefield(g.Active, CardPool.SpawnToken));

            var dead = g.P(g.Active).Graveyard.Single(c => c.DefinitionId == "tusked_mammoth");
            g.SetMana(g.Active, 3);
            g.Do(g.Activations(g.Active, rot).Single(x => x.Target == Target.ForObject(dead.Id)));
            g.PassRound();
            Assert.IsTrue(g.P(g.Active).Hand.Any(c => c.DefinitionId == "tusked_mammoth"));
            Assert.AreEqual(27, g.P(g.Active).Life);
        }

        [Test]
        public void Vox_GainsLife_AndThePowerDrawsOnAKill()
        {
            var g = TestGame.AtFirstMainPhase();
            var vox = g.SetTavernDweller(g.Active, "vox_nocturne");
            g.P(g.Active).Life = 20;
            var rascal = g.AddToBattlefield(g.Other, "goober_rascal"); // 2/1
            int hand = g.P(g.Active).Hand.Count;
            g.SetMana(g.Active, 2);
            g.Do(g.Activations(g.Active, vox).Single(a => a.Target == Target.ForObject(rascal.Id)));
            g.PassRound();
            Assert.AreEqual(hand + 1, g.P(g.Active).Hand.Count, "it died: draw");
            g.PassRound(); // passive trigger
            Assert.AreEqual(21, g.P(g.Active).Life);
        }

        [Test]
        public void Keeper_BigCreaturesEnterWithACounter()
        {
            var g = TestGame.AtFirstMainPhase();
            g.SetTavernDweller(g.Active, "keeper_z00");
            var grizzly = g.AddToHand(g.Active, "ironbark_grizzly"); // 4/5
            var sword = g.AddToHand(g.Active, "hired_sellsword");    // 2/3
            g.SetMana(g.Active, 6);
            g.Do(PlayerAction.Play(g.Active, grizzly.Id));
            g.PassRound();
            g.Do(PlayerAction.Play(g.Active, sword.Id));
            g.PassRound();
            Assert.AreEqual(1, g.OnBattlefield(g.Active, "ironbark_grizzly").PlusOneCounters);
            Assert.AreEqual(0, g.OnBattlefield(g.Active, "hired_sellsword").PlusOneCounters);
        }
    }
}
