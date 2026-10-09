using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>Multiple targets (MTG 115, 608.2b), Fight (§11.1) and the Glitterworld / Wild prototype cards.</summary>
    public class MultiTargetAndNewCardTests
    {
        [Test]
        public void PrimalClash_MyCreatureFightsTheirs()
        {
            var g = TestGame.AtFirstMainPhase();
            var bear = g.AddToBattlefield(g.Active, "ironbark_grizzly");   // 4/5
            var sword = g.AddToBattlefield(g.Other, "hired_sellsword");    // 2/3
            var clash = g.AddToHand(g.Active, "primal_clash");
            g.SetMana(g.Active, 2);

            g.Do(PlayerAction.Play(g.Active, clash.Id, new[] { Target.ForObject(bear.Id), Target.ForObject(sword.Id) }));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(sword.Id));
            Assert.AreEqual(2, bear.Damage, "fight damage is permanent too");
        }

        [Test]
        public void PrimalClash_TargetSlotsAreChecked()
        {
            var g = TestGame.AtFirstMainPhase();
            var bear = g.AddToBattlefield(g.Active, "ironbark_grizzly");
            var sword = g.AddToBattlefield(g.Other, "hired_sellsword");
            var clash = g.AddToHand(g.Active, "primal_clash");
            g.SetMana(g.Active, 2);
            var plays = g.Legal(g.Active).Where(a => a.Card == clash.Id).ToList();
            Assert.AreEqual(1, plays.Count, "slot 1 must be yours, slot 2 must be theirs");
            CollectionAssert.AreEqual(new[] { Target.ForObject(bear.Id), Target.ForObject(sword.Id) }, plays[0].Targets);
        }

        [Test]
        public void OneTargetGone_TheSpellStillResolves_ButTheFightDoesNothing()
        {
            var g = TestGame.AtFirstMainPhase();
            var runt = g.AddToBattlefield(g.Active, "brawling_runt");     // 2/2
            var sword = g.AddToBattlefield(g.Other, "hired_sellsword");   // 2/3
            var clash = g.AddToHand(g.Active, "primal_clash");
            var snot = g.AddToHand(g.Other, "spark_snot");
            g.SetMana(g.Active, 2);
            g.P(g.Other).Gold = 1;

            g.Do(PlayerAction.Play(g.Active, clash.Id, new[] { Target.ForObject(runt.Id), Target.ForObject(sword.Id) }));
            g.Pass();
            g.Do(PlayerAction.Play(g.Other, snot.Id, Target.ForObject(runt.Id))); // kill the fighter in response
            g.PassRound();
            g.PassRound();

            Assert.IsEmpty(g.Events.OfType<FizzledEvent>(), "one target is still legal (MTG 608.2b)");
            Assert.AreEqual(0, sword.Damage, "no fight without both creatures");
            Assert.AreEqual(1, g.P(g.Active).Graveyard.Count(c => c.DefinitionId == "primal_clash"));
        }

        [Test]
        public void ChainZap_UpToThreeDistinctTargets()
        {
            var g = TestGame.AtFirstMainPhase();
            var a = g.AddToBattlefield(g.Other, "hired_sellsword");
            var b = g.AddToBattlefield(g.Other, "tavern_bouncer");
            var c = g.AddToBattlefield(g.Other, "goober_rascal");
            var zap = g.AddToHand(g.Active, "chain_zap");
            g.SetMana(g.Active, 3);

            var plays = g.Legal(g.Active).Where(x => x.Card == zap.Id).ToList();
            Assert.AreEqual(8, plays.Count, "0, 1, 2 or 3 of 3 creatures, no repeats, order doesn't matter");
            Assert.IsTrue(plays.All(p => p.Targets.Distinct().Count() == p.Targets.Length));

            g.Do(plays.Single(p => p.Targets.Length == 3));
            g.PassRound();
            Assert.AreEqual(1, a.Damage);
            Assert.AreEqual(1, b.Damage);
            Assert.IsNull(g.State.FindOnBattlefield(c.Id), "2/1 rascal dies");
        }

        [Test]
        public void ApexInstinct_PumpThenFight_AndLosingTheBuffCannotKill()
        {
            var g = TestGame.AtFirstMainPhase();
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");  // 2/3 → 4/5
            var fighter = g.AddToBattlefield(g.Other, "pit_fighter");     // 4/4
            var apex = g.AddToHand(g.Active, "apex_instinct");
            g.SetMana(g.Active, 3);

            g.Do(PlayerAction.Play(g.Active, apex.Id, new[] { Target.ForObject(sword.Id), Target.ForObject(fighter.Id) }));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(fighter.Id), "4 damage kills the 4/4");
            Assert.AreEqual(4, sword.Damage);
            Assert.AreEqual(1, g.Stats(sword).RemainingHealth);

            g.PassToStep(Step.Main1, g.Other);
            Assert.IsNotNull(g.State.FindOnBattlefield(sword.Id), "§7.3: the +2/+2 ending can't kill it");
            Assert.AreEqual(2, sword.Damage);
        }

        [Test]
        public void SabretoothProwler_FightsOnArrival()
        {
            var g = TestGame.AtFirstMainPhase();
            var sword = g.AddToBattlefield(g.Other, "hired_sellsword");
            var prowler = g.AddToHand(g.Active, "sabretooth_prowler");
            g.SetMana(g.Active, 4);
            g.Do(PlayerAction.Play(g.Active, prowler.Id));
            g.PassRound(); // creature resolves; only one legal target, so the trigger goes straight on
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(sword.Id));
            Assert.AreEqual(2, g.OnBattlefield(g.Active, "sabretooth_prowler").Damage);
        }

        [Test]
        public void RiotSuppressor_HitsEachEnemyCreature_NotYours()
        {
            var g = TestGame.AtFirstMainPhase();
            var mine = g.AddToBattlefield(g.Active, "hired_sellsword");
            var theirs1 = g.AddToBattlefield(g.Other, "hired_sellsword");
            var theirs2 = g.AddToBattlefield(g.Other, "goober_rascal");
            var riot = g.AddToHand(g.Active, "riot_suppressor");
            g.SetMana(g.Active, 4);
            g.Do(PlayerAction.Play(g.Active, riot.Id));
            g.PassRound();
            g.PassRound();
            Assert.AreEqual(0, mine.Damage);
            Assert.AreEqual(1, theirs1.Damage);
            Assert.IsNull(g.State.FindOnBattlefield(theirs2.Id));
        }

        [Test]
        public void CanopyCritter_CannotHealItself()
        {
            var g = TestGame.AtFirstMainPhase();
            var hurt = g.AddToBattlefield(g.Active, "tavern_bouncer", damage: 3);
            var critter = g.AddToHand(g.Active, "canopy_critter");
            g.Do(PlayerAction.Play(g.Active, critter.Id));
            g.PassRound();
            Assert.IsNull(g.State.Pending, "only one legal target (the critter itself is excluded)");
            g.PassRound();
            Assert.AreEqual(1, hurt.Damage);
        }

        [Test]
        public void MossbackTortoise_HealsAtTheStartOfItsTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var first = g.Active;
            var turtle = g.AddToBattlefield(first, "mossback_tortoise", damage: 3);
            g.PassToStep(Step.Main1, g.Other);
            Assert.AreEqual(3, turtle.Damage, "only on its controller's turn");
            g.PassToStep(Step.Main1, first);
            Assert.AreEqual(2, turtle.Damage);
        }

        [Test]
        public void OrbitalStrikeNetwork_HitsEnemyCreaturesAndOpponents()
        {
            var g = TestGame.AtFirstMainPhase();
            var first = g.Active;
            var other = g.Other;
            g.AddToBattlefield(first, "orbital_strike_network");
            var mine = g.AddToBattlefield(first, "hired_sellsword");
            var theirs = g.AddToBattlefield(other, "tavern_bouncer");
            g.PassToStep(Step.Main1, other);
            g.PassToStep(Step.Main1, first);
            Assert.AreEqual(1, theirs.Damage);
            Assert.AreEqual(0, mine.Damage);
            Assert.AreEqual(29, g.P(other).Life);
        }

        [Test]
        public void ZooPatrol_IsALegalDeck()
        {
            Assert.DoesNotThrow(() => DeckValidator.Validate(PrototypeCards.CreateDatabase(), FormatConfig.Standard(), PrototypeCards.ZooPatrolDeck()));
        }
    }
}
