using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>GAME_DESIGN §8 (the Chain), triggers, continuous effects and the prototype cards.</summary>
    public class ChainAndCardTests
    {
        [Test]
        public void CreatureSpell_UsesTheChain_AndResolvesWhenAllPass()
        {
            var g = TestGame.AtFirstMainPhase();
            g.SetMana(g.Active, 2);
            var sword = g.AddToHand(g.Active, "hired_sellsword");
            g.Do(PlayerAction.Play(g.Active, sword.Id));
            Assert.AreEqual(1, g.State.Chain.Count);
            Assert.AreEqual(g.Active, g.State.PriorityPlayer, "caster keeps priority (MTG 117.3c)");
            g.PassRound();
            Assert.AreEqual(0, g.State.Chain.Count);
            var onField = g.OnBattlefield(g.Active, "hired_sellsword");
            Assert.IsNotNull(onField);
            Assert.AreNotEqual(sword.Id, onField.Id, "new object after each zone change");
            Assert.IsTrue(onField.SummoningSick);
        }

        [Test]
        public void Chain_ResolvesLastInFirstOut()
        {
            var g = TestGame.AtFirstMainPhase();
            var target = g.AddToBattlefield(g.Other, "hired_sellsword", damage: 1); // 2/3, 2 left
            var snot = g.AddToHand(g.Active, "spark_snot");
            var remedy = g.AddToHand(g.Other, "jungle_remedy");

            g.Do(PlayerAction.Play(g.Active, snot.Id, Target.ForObject(target.Id)));
            g.Pass(); // active passes, other may respond
            g.Do(PlayerAction.Play(g.Other, remedy.Id, Target.ForObject(target.Id), goldPaid: 1));
            g.PassRound(); // remedy resolves first: heals the 1 damage
            Assert.AreEqual(0, target.Damage);
            g.PassRound(); // then the snot: 2 damage
            Assert.AreEqual(2, target.Damage);
            Assert.IsNotNull(g.State.FindOnBattlefield(target.Id), "survived thanks to the response");
        }

        [Test]
        public void Spell_Fizzles_WhenItsTargetIsGone()
        {
            var g = TestGame.AtFirstMainPhase();
            var target = g.AddToBattlefield(g.Other, "hired_sellsword", damage: 1);
            var first = g.AddToHand(g.Active, "spark_snot");
            var second = g.AddToHand(g.Active, "spark_snot");
            g.SetMana(g.Active, 2);

            g.Do(PlayerAction.Play(g.Active, first.Id, Target.ForObject(target.Id)));
            g.Do(PlayerAction.Play(g.Active, second.Id, Target.ForObject(target.Id)));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(target.Id));
            g.PassRound();
            Assert.AreEqual(1, g.Events.OfType<FizzledEvent>().Count());
            Assert.AreEqual(2, g.P(g.Active).Graveyard.Count(c => c.DefinitionId == "spark_snot"));
        }

        [Test]
        public void ArrivalTrigger_AsksForATarget_ThenResolves()
        {
            var g = TestGame.AtFirstMainPhase();
            var victim = g.AddToBattlefield(g.Other, "hired_sellsword");
            var bomber = g.AddToHand(g.Active, "barrel_bomber");
            g.SetMana(g.Active, 4);
            g.Do(PlayerAction.Play(g.Active, bomber.Id));
            g.PassRound();

            Assert.AreEqual(DecisionKind.ChooseTriggerTarget, g.State.Pending.Kind);
            g.Do(PlayerAction.ChooseTarget(g.Active, Target.ForObject(victim.Id)));
            Assert.AreEqual(1, g.State.Chain.Count);
            g.PassRound();
            Assert.AreEqual(2, victim.Damage);
        }

        [Test]
        public void LastBreath_TriggersWhenTheCreatureDies()
        {
            var g = TestGame.AtFirstMainPhase();
            var fuse = g.AddToBattlefield(g.Active, "fuse_goober"); // 1/2
            var snot = g.AddToHand(g.Active, "spark_snot");
            g.Do(PlayerAction.Play(g.Active, snot.Id, Target.ForObject(fuse.Id)));
            g.PassRound();

            Assert.AreEqual(1, g.Events.OfType<CreatureDiedEvent>().Count());
            Assert.AreEqual(DecisionKind.ChooseTriggerTarget, g.State.Pending.Kind);
            g.Do(PlayerAction.ChooseTarget(g.Active, Target.ForPlayer(g.Other)));
            g.PassRound();
            Assert.AreEqual(28, g.P(g.Other).Life);
        }

        [Test]
        public void Warchief_BuffsOtherGoobers_NotItself()
        {
            var g = TestGame.AtFirstMainPhase();
            var chief = g.AddToBattlefield(g.Active, "goober_warchief");
            var rascal = g.AddToBattlefield(g.Active, "goober_rascal");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            var enemyGoober = g.AddToBattlefield(g.Other, "brawling_runt");
            Assert.AreEqual(2, g.Stats(chief).Power);
            Assert.AreEqual(3, g.Stats(rascal).Power);
            Assert.AreEqual(2, g.Stats(sword).Power, "not a Goober");
            Assert.AreEqual(2, g.Stats(enemyGoober).Power, "only your Goobers");
        }

        [Test]
        public void MobRush_HastyTokens_AndTheBuffEndsAtEndOfTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var rush = g.AddToHand(g.Active, "mob_rush");
            g.SetMana(g.Active, 4);
            g.Do(PlayerAction.Play(g.Active, rush.Id));
            g.PassRound();

            var tokens = g.P(g.Active).Battlefield.Where(c => c.IsToken).ToList();
            Assert.AreEqual(3, tokens.Count);
            Assert.IsTrue(tokens.All(t => g.Stats(t).Power == 2 && g.Stats(t).Has(Keyword.Haste)));

            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            Assert.AreEqual(3, g.Legal(g.Active).Count(a => a.Kind == ActionKind.DeclareAttacker), "tokens have Haste");
            g.Do(PlayerAction.FinishAttacks(g.Active));

            g.PassToStep(Step.Main1, g.Other);
            Assert.IsTrue(tokens.All(t => g.Stats(t).Power == 1 && !g.Stats(t).Has(Keyword.Haste)));
        }

        [Test]
        public void Tokens_StopExisting_WhenTheyLeaveTheBattlefield()
        {
            var g = TestGame.AtFirstMainPhase();
            var gang = g.AddToHand(g.Active, "gob_gang");
            var snot = g.AddToHand(g.Active, "spark_snot");
            g.SetMana(g.Active, 3);
            g.Do(PlayerAction.Play(g.Active, gang.Id));
            g.PassRound();
            var token = g.P(g.Active).Battlefield.First(c => c.IsToken);
            g.Do(PlayerAction.Play(g.Active, snot.Id, Target.ForObject(token.Id)));
            g.PassRound();

            Assert.AreEqual(1, g.P(g.Active).Battlefield.Count(c => c.IsToken));
            Assert.IsFalse(g.P(g.Active).Graveyard.Any(c => c.IsToken));
            Assert.AreEqual(1, g.Events.OfType<CreatureDiedEvent>().Count(), "tokens still die (Last Breath works)");
        }

        [Test]
        public void JungleRemedy_Overcharge_PaidWithGoldOnly()
        {
            var g = TestGame.AtFirstMainPhase();
            var beast = g.AddToBattlefield(g.Active, "ironbark_grizzly", damage: 4); // 4/5, 1 left
            var remedy = g.AddToHand(g.Active, "jungle_remedy");
            g.SetMana(g.Active, 5);
            g.P(g.Active).Gold = 0;
            Assert.IsFalse(g.Legal(g.Active).Any(a => a.Overcharge), "no Gold, no Overcharge");

            g.P(g.Active).Gold = 1;
            g.Do(PlayerAction.Play(g.Active, remedy.Id, Target.ForObject(beast.Id), overcharge: true));
            Assert.AreEqual(4, g.P(g.Active).Mana);
            Assert.AreEqual(0, g.P(g.Active).Gold);
            g.PassRound();
            Assert.AreEqual(0, beast.Damage);
            Assert.AreEqual(1, beast.PlusOneCounters);
            Assert.AreEqual(5, g.Stats(beast).Power);
            Assert.AreEqual(6, g.Stats(beast).MaxHealth);
        }

        [Test]
        public void BarkeepsTonic_HealsThePatron_UpToStartingLife()
        {
            var g = TestGame.AtFirstMainPhase();
            var tonic = g.AddToHand(g.Active, "barkeeps_tonic");
            g.P(g.Active).Life = 29;
            Assert.IsFalse(g.Legal(g.Active).Any(a => a.Target == Target.ForPlayer(g.Other)), "only your own Patron");
            g.Do(PlayerAction.Play(g.Active, tonic.Id, Target.ForPlayer(g.Active)));
            g.PassRound();
            Assert.AreEqual(30, g.P(g.Active).Life);
        }

        [Test]
        public void ThornbackRavager_ArrivalWithoutTarget_GoesStraightOnTheChain()
        {
            var g = TestGame.AtFirstMainPhase();
            var ravager = g.AddToHand(g.Active, "thornback_ravager");
            g.SetMana(g.Active, 5);
            int handBefore = g.P(g.Active).Hand.Count;
            g.Do(PlayerAction.Play(g.Active, ravager.Id));
            g.PassRound(); // creature resolves, trigger goes on the Chain
            Assert.IsNull(g.State.Pending);
            Assert.AreEqual(1, g.State.Chain.Count);
            g.PassRound();
            Assert.AreEqual(handBefore, g.P(g.Active).Hand.Count, "-1 cast, +1 drawn");
        }
    }
}
