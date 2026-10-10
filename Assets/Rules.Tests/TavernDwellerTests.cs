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
        public void Power_OnceEachRound_AtInstantSpeed_GoldFirst()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var keeper = g.SetTavernDweller(me, "keeper_z00");
            var hurt = g.AddToBattlefield(me, "tavern_bouncer", damage: 4);
            g.SetMana(me, 3);
            g.P(me).Gold = 0;

            g.Do(g.Activations(me, keeper).Single(a => a.Target == Target.ForObject(hurt.Id)));
            Assert.AreEqual(1, g.P(me).Mana, "no Gold: paid with mana");
            g.PassRound();
            Assert.AreEqual(1, hurt.Damage, "heal 3");
            Assert.IsEmpty(g.Activations(me, keeper), "once each turn");

            hurt.Damage = 4;
            g.NextRound(); // the other player leads round 2
            var them = g.Active;
            g.SetMana(me, 0);
            g.P(me).Gold = 1;
            g.SetMana(them, 2);
            g.Do(g.Legal(them).First(a => a.Kind == ActionKind.PlayCard));
            g.Pass(); // they pass with their creature on the Chain
            Assert.AreEqual(me, g.State.PriorityPlayer);
            Assert.IsEmpty(g.Activations(me, keeper), "1 Gold can't pay (2)");

            g.P(me).Gold = 3;
            g.Do(g.Activations(me, keeper).Single(a => a.Target == Target.ForObject(hurt.Id)));
            Assert.AreEqual(1, g.P(me).Gold);
            var item = g.State.Chain.Last();
            Assert.IsTrue(item.IsTavernDwellerPower);
            g.PassRound();
            Assert.AreEqual(1, hurt.Damage, "usable again in the next round, in response on their action");
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
        public void Auditor_LowersInvestAndEquip_AndThePowerDrawsAtThreeGold()
        {
            var g = TestGame.AtFirstMainPhase();
            var auditor = g.SetTavernDweller(g.Active, "auditor_prime");
            var ledger = g.AddToHand(g.Active, "the_grand_ledger"); // Invest 3
            g.SetMana(g.Active, 7);
            g.P(g.Active).Gold = 2;
            Assert.IsTrue(g.Legal(g.Active).Any(a => a.Card == ledger.Id && a.Invest), "Invest 3 -> 2");

            var rail = g.AddToBattlefield(g.Active, "rail_cannon"); // Equip 3
            g.AddToBattlefield(g.Active, "hired_sellsword");
            g.SetMana(g.Active, 2);
            g.P(g.Active).Gold = 0;
            Assert.AreEqual(1, g.Activations(g.Active, rail).Count, "Equip 3 -> 2");
        }

        [Test]
        public void Auditor_Power_OnlyWithThreeOrMoreGold_PaidGoldFirst()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var auditor = g.SetTavernDweller(me, "auditor_prime");
            g.SetMana(me, 2);
            g.P(me).Gold = 2;
            Assert.AreEqual(0, g.Activations(me, auditor).Count, "2 Gold: can't activate, even with mana to pay");
            g.P(me).Gold = 3;
            int hand = g.P(me).Hand.Count;
            g.Do(g.Activations(me, auditor).Single());
            Assert.AreEqual(1, g.P(me).Gold, "paid with Gold first");
            Assert.AreEqual(2, g.P(me).Mana);
            g.PassRound();
            Assert.AreEqual(hand + 1, g.P(me).Hand.Count);
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
        public void Mukk_TrampleCreaturesGetPlusOne_AndThePowerFightsWithATrampler()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var mukk = g.SetTavernDweller(me, "mukk_the_grub_king");
            var hog = g.AddToBattlefield(me, "hog_rider");      // 3/3 Trample
            var sword = g.AddToBattlefield(me, "hired_sellsword"); // 2/3, no Trample
            var enemy = g.AddToBattlefield(g.Other, "tavern_bouncer"); // 2/5
            Assert.AreEqual(4, g.Stats(hog).Power);
            Assert.AreEqual(2, g.Stats(sword).Power);
            g.SetMana(me, 2);
            Assert.AreEqual(0, g.Activations(me, mukk).Count, "costs 3");
            g.SetMana(me, 3);
            var fights = g.Activations(me, mukk);
            Assert.AreEqual(1, fights.Count, "only the Trample creature can fight, only against their creature");
            Assert.AreEqual(Target.ForObject(hog.Id), fights[0].Targets[0]);
            g.Do(fights[0]);
            g.PassRound();
            Assert.AreEqual(4, enemy.Damage, "the passive's +1/+0 counts in the fight");
            Assert.AreEqual(2, hog.Damage);
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

            g.NextRound();
            g.P(me).Gold = 2;
            g.Pass();
            var uses = g.Activations(me, wrench);
            Assert.AreEqual(4, uses.Count, "each creature, with or without the Equipment");
            g.Do(uses.Single(u => u.Targets[0] == Target.ForObject(b.Id) && u.Targets.Length == 2));
            g.PassRound();
            Assert.AreEqual(b.Id, onField.AttachedToObject);
        }

        [Test]
        public void Sparkwrench_PowerPumpsWhenNoEquipmentAttaches()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var wrench = g.SetTavernDweller(me, "sparkwrench");
            var a = g.AddToBattlefield(me, "hired_sellsword"); // 2/3
            g.SetMana(me, 2);
            var uses = g.Activations(me, wrench);
            Assert.AreEqual(1, uses.Count, "no Equipment: just the creature");
            g.Do(uses[0]);
            g.PassRound();
            Assert.AreEqual(3, g.Stats(a).Power);
            Assert.AreEqual(4, g.Stats(a).MaxHealth);
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
