using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// The rest of set v0.1, batch E: intervening "if" (MTG 603.4), death watchers, "your second spell",
    /// extra blocks, granted triggers, Curse triggers, graveyard returns, attacking tokens, look-at-top.
    /// </summary>
    public class V01RestCardTests
    {
        private static CardInstance Cast(TestGame g, PlayerId player, string card, params Target[] targets)
        {
            var c = g.AddToHand(player, card);
            g.SetMana(player, 10);
            g.Do(g.Legal(player).First(a => a.Kind == ActionKind.PlayCard && a.Card == c.Id && a.Targets.SequenceEqual(targets) && !a.Invest));
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

        private static void Attack(TestGame g, params CardInstance[] attackers)
        {
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            foreach (var a in attackers) g.Do(PlayerAction.Attack(g.Active, a.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
        }

        [Test]
        public void Sproutling_GrowsOnlyWithoutDamage()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var fresh = g.AddToBattlefield(me, "sproutling");
            var hurt = g.AddToBattlefield(me, "sproutling", damage: 1);
            g.PassUntil(s => s.ActivePlayer != me);
            Assert.AreEqual(1, fresh.PlusOneCounters);
            Assert.AreEqual(0, hurt.PlusOneCounters, "intervening if: it has damage");
        }

        [Test]
        public void VelvetEmbezzler_DrawsAtFiveOrMoreGold()
        {
            foreach (var (gold, draws) in new[] { (4, 0), (5, 1), (7, 1) })
            {
                var g = TestGame.AtFirstMainPhase();
                var me = g.Active;
                g.AddToBattlefield(me, "velvet_embezzler");
                g.AddToBattlefield(me, "offshore_account");
                g.P(me).Gold = gold;
                g.SetMana(me, 0);
                int hand = g.P(me).Hand.Count;
                g.PassUntil(s => s.Step == Step.Cleanup || s.ActivePlayer != me);
                Assert.AreEqual(hand + draws, g.P(me).Hand.Count, gold + " Gold");
            }
        }

        [Test]
        public void CardShark_DrawsOnTheSecondSpellOnly()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "card_shark");
            var target = g.AddToBattlefield(g.Other, "tavern_bouncer");
            Cast(g, me, "spark_snot", Target.ForObject(target.Id));
            Assert.AreEqual(1, g.State.Chain.Count, "first spell");
            g.PassRound();
            Cast(g, me, "spark_snot", Target.ForObject(target.Id));
            Assert.AreEqual(2, g.State.Chain.Count, "second spell: Card Shark triggers");
            g.PassRound();
            g.PassRound();
            Cast(g, me, "barkeeps_tonic", Target.ForPlayer(me));
            Assert.AreEqual(1, g.State.Chain.Count, "third spell");
        }

        [Test]
        public void TaxOffice_TakesGold_OrPaysYou()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(g.Other, "tax_office");
            g.P(me).Gold = 2;
            var victim = g.AddToBattlefield(me, "tavern_bouncer");
            Cast(g, me, "spark_snot", Target.ForObject(victim.Id));
            g.PassRound();
            Assert.AreEqual(1, g.P(me).Gold);

            g.P(me).Gold = 0;
            g.PassRound();
            Cast(g, me, "spark_snot", Target.ForObject(victim.Id));
            g.PassRound();
            Assert.AreEqual(1, g.P(g.Other).Gold, "they couldn't lose Gold: the Tax Office's controller gains 1");
        }

        [Test]
        public void RetiredChampion_BlocksTwoAttackers_AndSplitsItsDamage()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var a = g.AddToBattlefield(me, "hired_sellsword"); // 2/3
            var b = g.AddToBattlefield(me, "hog_rider");       // 3/3
            var champ = g.AddToBattlefield(g.Other, "retired_champion"); // 5/6
            Attack(g, a, b);
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareBlockers);
            g.Do(PlayerAction.Block(g.Other, champ.Id, a.Id));
            Assert.IsTrue(g.Legal(g.Other).Any(x => x.Kind == ActionKind.DeclareBlocker && x.Card == champ.Id && x.BlockedAttacker == b.Id));
            Assert.IsFalse(g.Legal(g.Other).Any(x => x.Kind == ActionKind.DeclareBlocker && x.BlockedAttacker == a.Id), "not the same one twice");
            g.Do(PlayerAction.Block(g.Other, champ.Id, b.Id));
            Assert.IsFalse(g.Legal(g.Other).Any(x => x.Kind == ActionKind.DeclareBlocker), "only one extra");
            g.Do(PlayerAction.FinishBlocks(g.Other));
            g.PassUntil(s => s.Step == Step.Main2);
            Assert.IsNull(g.State.FindOnBattlefield(a.Id), "3 lethal to the first");
            Assert.AreEqual(2, b.Damage, "the rest (2) to the second");
            Assert.AreEqual(5, champ.Damage);
            Assert.AreEqual(30, g.P(g.Other).Life);
        }

        [Test]
        public void Grakka_PumpsGoobers_AndAddsAnAttackingToken()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var grakka = g.AddToBattlefield(me, "grakka_queen_of_the_rabble");
            var runt = g.AddToBattlefield(me, "brawling_runt");
            Assert.AreEqual(3, g.Stats(runt).Power, "Your Goobers get +1/+1");
            Attack(g, grakka);
            g.PassRound();
            var token = g.P(me).Battlefield.Single(c => c.DefinitionId == PrototypeCards.GooberToken);
            Assert.IsTrue(token.Tapped);
            Assert.IsTrue(g.State.Combat.IsAttacking(token.Id));
            g.PassUntil(s => s.Step == Step.Main2);
            Assert.AreEqual(30 - 6 - 2, g.P(g.Other).Life, "Grakka 6 + a 2/2 token");
        }

        [Test]
        public void CurseOfTheSpotlight_PunishesTheirDeaths()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var victim = g.AddToBattlefield(g.Other, "goober_rascal");
            Cast(g, me, "curse_of_the_spotlight", Target.ForPlayer(g.Other));
            g.PassRound();
            int hand = g.P(me).Hand.Count;
            Cast(g, me, "spark_snot", Target.ForObject(victim.Id));
            g.PassRound();
            g.PassRound();
            Assert.AreEqual(28, g.P(g.Other).Life);
            Assert.AreEqual(hand + 1, g.P(me).Hand.Count, "drew a card (the helper adds Spark Snot before casting it)");
        }

        [Test]
        public void WorldrootHydra_GrowsWhenItSurvivesDamage()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var hydra = g.AddToBattlefield(g.Other, "worldroot_hydra"); // 5/5
            Cast(g, me, "spark_snot", Target.ForObject(hydra.Id));
            g.PassRound();
            g.PassRound();
            Assert.AreEqual(1, hydra.PlusOneCounters);
            Assert.AreEqual(4, g.Stats(hydra).RemainingHealth, "6 max - 2 damage");
        }

        [Test]
        public void MassHysteria_KillsSmallCreatures_AndGainsLifePerDeath()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.P(me).Life = 20;
            g.AddToBattlefield(me, "hired_sellsword");   // 2/3 dies
            g.AddToBattlefield(g.Other, "goober_rascal"); // dies
            var big = g.AddToBattlefield(g.Other, "tavern_bouncer"); // 2/5 survives
            Cast(g, me, "mass_hysteria");
            g.PassRound();
            Assert.AreEqual(22, g.P(me).Life);
            Assert.AreEqual(1, g.State.AllPermanents().Count());
            Assert.IsNotNull(g.State.FindOnBattlefield(big.Id));
        }

        [Test]
        public void Seance_ReturnsUpToTwoCheapCreatureCards()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var a = InGraveyard(g, me, "hired_sellsword");
            var b = InGraveyard(g, me, "hog_rider");
            var big = InGraveyard(g, me, "pit_champion");
            var seance = g.AddToHand(me, "seance");
            g.SetMana(me, 2);
            var plays = g.Legal(me).Where(x => x.Card == seance.Id).ToList();
            Assert.IsFalse(plays.Any(x => x.Targets.Contains(Target.ForObject(big.Id))), "cost 6");
            g.Do(plays.Single(x => x.Targets.Length == 2));
            g.PassRound();
            Assert.IsTrue(g.P(me).Hand.Any(c => c.DefinitionId == "hired_sellsword"));
            Assert.IsTrue(g.P(me).Hand.Any(c => c.DefinitionId == "hog_rider"));
            Assert.AreEqual(2, g.P(me).Graveyard.Count, "Pit Champion and the Séance");
        }

        [Test]
        public void LastCall_DestroysEquipment()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var blade = g.AddToBattlefield(g.Other, "neon_shiv");
            Cast(g, me, "last_call", Target.ForObject(blade.Id));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(blade.Id));
        }

        [Test]
        public void PocketChange_OneToHandOneToBottom_InvestTakesBoth()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var top = g.P(me).Deck.Take(2).ToList();
            int hand = g.P(me).Hand.Count;
            Cast(g, me, "pocket_change");
            g.PassRound();
            Assert.AreEqual(DecisionKind.ChooseFromTop, g.State.Pending?.Kind);
            g.Do(PlayerAction.ChooseOption(me, 0));
            Assert.AreEqual(hand + 1, g.P(me).Hand.Count, "+1 card (the Pocket Change itself was added and cast)");
            Assert.AreEqual(top[1].DefinitionId, g.P(me).Deck.Last().DefinitionId, "the other one went to the bottom");

            var pc = g.AddToHand(me, "pocket_change");
            g.SetMana(me, 1);
            g.P(me).Gold = 1;
            hand = g.P(me).Hand.Count;
            g.Do(PlayerAction.Play(me, pc.Id, System.Array.Empty<Target>(), invest: true));
            g.PassRound();
            Assert.IsNull(g.State.Pending);
            Assert.AreEqual(hand - 1 + 2, g.P(me).Hand.Count);
        }

        [Test]
        public void CallOfTheDeepJungle_PutsTheFirstBigCreatureOntoTheBattlefield()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var deck = g.P(me).Deck;
            var big = new CardInstance
            {
                Id = new ObjectId(g.State.NextObjectId++), DefinitionId = "tusked_mammoth", Owner = me, Controller = me, Zone = Zone.Deck,
            };
            deck.Insert(2, big); // two small cards, then the Mammoth (cost 5)
            int size = deck.Count;
            Cast(g, me, "call_of_the_deep_jungle");
            g.PassRound();
            Assert.IsNotNull(g.OnBattlefield(me, "tusked_mammoth"));
            Assert.AreEqual(size - 1, g.P(me).Deck.Count, "the two revealed cards went to the bottom");
        }

        [Test]
        public void GroveWarden_GivesYourOtherCreaturesAStartOfTurnHeal()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var warden = g.AddToBattlefield(me, "grove_warden", damage: 1);
            var bouncer = g.AddToBattlefield(me, "tavern_bouncer", damage: 3);
            g.PassToStep(Step.Main1, g.Other);
            g.PassToStep(Step.Main1, me);
            Assert.AreEqual(2, bouncer.Damage);
            Assert.AreEqual(1, warden.Damage, "other creatures only");
        }

        [Test]
        public void HexOfWithering_DamagesItAtTheStartOfItsControllersTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var victim = g.AddToBattlefield(g.Other, "tavern_bouncer");
            Cast(g, me, "hex_of_withering", Target.ForObject(victim.Id));
            g.PassRound();
            Assert.AreEqual(0, g.Stats(victim).Power, "-2/-0");
            g.PassToStep(Step.Main1, g.Other);
            Assert.AreEqual(1, victim.Damage);
        }

        [Test]
        public void DebtCollector_TakesUpToTwoGold()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.P(g.Other).Gold = 1;
            Cast(g, me, "debt_collector");
            g.PassRound();
            g.PassRound();
            Assert.AreEqual(0, g.P(g.Other).Gold);
            Assert.AreEqual(1, g.P(me).Gold, "only what they lost");
        }

        [Test]
        public void GooberDemolisher_PingsWhenAnotherGooberDies()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "goober_demolisher");
            var rascal = g.AddToBattlefield(me, "goober_rascal");
            var human = g.AddToBattlefield(me, "hired_sellsword", damage: 2);
            Cast(g, me, "spark_snot", Target.ForObject(rascal.Id));
            g.PassRound();
            g.PassRound();
            Assert.AreEqual(29, g.P(g.Other).Life);
            Cast(g, me, "spark_snot", Target.ForObject(human.Id));
            g.PassRound();
            Assert.IsEmpty(g.State.Chain, "not a Goober");
        }

        [Test]
        public void GrubbyPickpocket_DrainsGoldWhenItAttacks()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var grubby = g.AddToBattlefield(me, "grubby_pickpocket");
            g.P(g.Other).Gold = 2;
            Attack(g, grubby);
            g.PassRound();
            Assert.AreEqual(1, g.P(g.Other).Gold);
            Assert.AreEqual(1, g.P(me).Gold);
        }
    }
}
