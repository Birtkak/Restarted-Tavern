using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// Set v0.2 batch B: damage and healing. "Damaged" and Health-remaining filters, can't be healed
    /// (§11.1), damage prevention (Hardlight Aegis), damage to each creature, damage/heal/arrival
    /// watchers, "destroys a creature in combat", Curses on players, extra costs on spells.
    /// </summary>
    public class DamageAndHealingTests
    {
        private static CardInstance Cast(TestGame g, PlayerId player, string card, params Target[] targets)
        {
            var c = g.AddToHand(player, card);
            g.SetMana(player, 10);
            g.Do(g.Legal(player).First(a => a.Kind == ActionKind.PlayCard && a.Card == c.Id && a.Targets.SequenceEqual(targets)));
            return c;
        }

        private static bool CanTarget(TestGame g, PlayerId player, CardInstance card, CardInstance target) =>
            g.Legal(player).Any(a => a.Kind == ActionKind.PlayCard && a.Card == card.Id && a.Target == Target.ForObject(target.Id));

        [Test]
        public void KickEm_DealsMoreToADamagedCreature()
        {
            var g = TestGame.AtFirstMainPhase();
            var fresh = g.AddToBattlefield(g.Other, "tavern_bouncer");
            var hurt = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 1);
            Cast(g, g.Active, "kick_em_while_theyre_down", Target.ForObject(fresh.Id));
            g.PassRound();
            Cast(g, g.Active, "kick_em_while_theyre_down", Target.ForObject(hurt.Id));
            g.PassRound();
            Assert.AreEqual(2, fresh.Damage);
            Assert.IsNull(g.State.FindOnBattlefield(hurt.Id), "1 + 4 damage kills a 2/5");
        }

        [Test]
        public void FinisherProtocol_OnlyTargetsLowHealth_AndFizzlesIfHealed()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var healthy = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 2);   // 3 left
            var low = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 3);       // 2 left
            var finisher = g.AddToHand(me, "finisher_protocol");
            g.SetMana(me, 2);
            Assert.IsFalse(CanTarget(g, me, finisher, healthy));
            Assert.IsTrue(CanTarget(g, me, finisher, low));

            g.Do(PlayerAction.Play(me, finisher.Id, Target.ForObject(low.Id)));
            var tonic = g.AddToHand(g.Other, "barkeeps_tonic");
            g.P(g.Other).Gold = 1;
            g.Pass();
            g.Do(PlayerAction.Play(g.Other, tonic.Id, Target.ForObject(low.Id)));
            g.PassRound(); // the Tonic heals 3: now 5 left
            g.PassRound();
            Assert.IsNotNull(g.State.FindOnBattlefield(low.Id), "the target is no longer legal: it fizzles");
            Assert.IsTrue(g.Events.OfType<FizzledEvent>().Any());
        }

        [Test]
        public void BloodPrice_PaysLife_AndNeedsADamagedTarget()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var fresh = g.AddToBattlefield(g.Other, "tavern_bouncer");
            var hurt = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 1);
            var price = g.AddToHand(me, "blood_price");
            g.SetMana(me, 2);
            Assert.IsFalse(CanTarget(g, me, price, fresh));
            Assert.IsTrue(CanTarget(g, me, price, hurt));

            g.P(me).Life = 2;
            Assert.IsFalse(CanTarget(g, me, price, hurt), "MTG 119.4: can't pay 3 life with 2");
            g.P(me).Life = 30;
            g.Do(PlayerAction.Play(me, price.Id, Target.ForObject(hurt.Id)));
            Assert.AreEqual(27, g.P(me).Life, "the life is paid on casting");
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(hurt.Id));
        }

        [Test]
        public void CalledShot_OnlyTargetsAttackingOrBlockingCreatures()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var attacker = g.AddToBattlefield(me, "hired_sellsword");
            var shot = g.AddToHand(g.Other, "called_shot");
            g.P(g.Other).Gold = 2;
            g.Pass();
            Assert.IsFalse(g.Legal(g.Other).Any(a => a.Card == shot.Id), "no creature is in combat");

            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            g.Do(PlayerAction.Attack(me, attacker.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(me));
            g.Pass();
            Assert.IsTrue(CanTarget(g, g.Other, shot, attacker));
        }

        [Test]
        public void FlingTheRunt_SacrificesACreature_AndDealsItsPower()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var runt = g.AddToBattlefield(me, "hog_rider");              // 3/3
            var other = g.AddToBattlefield(me, "hired_sellsword");
            var fling = g.AddToHand(me, "fling_the_runt");
            g.SetMana(me, 1);

            var plays = g.Legal(me).Where(a => a.Card == fling.Id).ToList();
            Assert.IsTrue(plays.All(a => !a.Sacrifice.IsNone), "the sacrifice is part of the action");
            Assert.IsFalse(plays.Any(a => a.Target == Target.ForObject(a.Sacrifice)), "can't target what you sacrifice");

            g.Do(plays.Single(a => a.Sacrifice == runt.Id && a.Target == Target.ForPlayer(g.Other)));
            Assert.IsNull(g.State.FindOnBattlefield(runt.Id), "sacrificed on casting");
            Assert.IsNotNull(g.State.FindOnBattlefield(other.Id));
            g.PassRound();
            Assert.AreEqual(27, g.P(g.Other).Life, "its last known Power: 3");
        }

        [Test]
        public void TavernBrawlNight_YourTwoHealthCreaturesSurvive()
        {
            var g = TestGame.AtFirstMainPhase();
            var mine = g.AddToBattlefield(g.Active, "hired_sellsword", damage: 1);   // 2 left
            var theirs = g.AddToBattlefield(g.Other, "hired_sellsword", damage: 1);  // 2 left
            Cast(g, g.Active, "tavern_brawl_night");
            g.PassRound();
            Assert.IsNotNull(g.State.FindOnBattlefield(mine.Id), "state-based actions wait for the whole spell");
            Assert.AreEqual(1, mine.Damage);
            Assert.IsNull(g.State.FindOnBattlefield(theirs.Id));
        }

        [Test]
        public void BarBrawl_HitsEveryCreature()
        {
            var g = TestGame.AtFirstMainPhase();
            var mine = g.AddToBattlefield(g.Active, "tavern_bouncer");
            var theirs = g.AddToBattlefield(g.Other, "tavern_bouncer");
            Cast(g, g.Active, "bar_brawl");
            g.PassRound();
            Assert.AreEqual(1, mine.Damage);
            Assert.AreEqual(1, theirs.Damage);
        }

        [Test]
        public void ChaosEngine_DamagesEachOtherCreature_AtTheStartOfYourTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var engine = g.AddToBattlefield(me, "chaos_engine");
            var mine = g.AddToBattlefield(me, "tavern_bouncer");
            var theirs = g.AddToBattlefield(g.Other, "tavern_bouncer");
            g.PassToStep(Step.Main1, g.Other);
            Assert.AreEqual(0, theirs.Damage, "only at the start of its controller's turn");
            g.PassToStep(Step.Main1, me);
            Assert.AreEqual(0, engine.Damage);
            Assert.AreEqual(1, mine.Damage);
            Assert.AreEqual(1, theirs.Damage);
        }

        [Test]
        public void HardlightAegis_CapsDamageEachTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var bouncer = g.AddToBattlefield(g.Other, "tavern_bouncer"); // 2/5, +0/+3 = 2/8
            var aegis = g.AddToBattlefield(g.Other, "hardlight_aegis");
            aegis.AttachedToObject = bouncer.Id;

            Cast(g, me, "spark_snot", Target.ForObject(bouncer.Id));  // 2
            g.PassRound();
            Cast(g, me, "spark_snot", Target.ForObject(bouncer.Id));  // prevented
            g.PassRound();
            Assert.AreEqual(2, bouncer.Damage, "can't be dealt more than 2 damage each turn");

            g.PassToStep(Step.Main1, g.Other);
            g.P(me).Gold = 1;
            var snot = g.AddToHand(me, "spark_snot");
            g.Pass();
            g.Do(PlayerAction.Play(me, snot.Id, Target.ForObject(bouncer.Id)));
            g.PassRound();
            Assert.AreEqual(4, bouncer.Damage, "a new turn: 2 more");
        }

        [Test]
        public void ChampionsBelt_HealsFullyAfterDestroyingACreatureInCombat()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var champ = g.AddToBattlefield(me, "hired_sellsword", damage: 1); // 2/3 +2/+2 = 4/5
            var belt = g.AddToBattlefield(me, "champions_belt");
            belt.AttachedToObject = champ.Id;
            var blocker = g.AddToBattlefield(g.Other, "tavern_bouncer");     // 2/5, dies to 4 + 1 earlier
            blocker.Damage = 1;

            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            g.Do(PlayerAction.Attack(me, champ.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(me));
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareBlockers);
            g.Do(PlayerAction.Block(g.Other, blocker.Id, champ.Id));
            g.Do(PlayerAction.FinishBlocks(g.Other));
            g.PassUntil(s => s.Step == Step.CombatDamage && s.Chain.Count > 0);
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(blocker.Id));
            Assert.AreEqual(0, champ.Damage, "healed fully");
        }

        [Test]
        public void NeonExecutioner_DestroysEnemiesLeftAtTwoOrLess()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "neon_executioner");
            var big = g.AddToBattlefield(g.Other, "tavern_bouncer");            // 5 → 3 left after 2
            var low = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 1); // 4 → 2 left after 2
            var mine = g.AddToBattlefield(me, "tavern_bouncer", damage: 1);

            Cast(g, me, "spark_snot", Target.ForObject(big.Id));
            g.PassRound();
            Assert.AreEqual(0, g.State.Chain.Count, "3 left: no trigger");
            Cast(g, me, "spark_snot", Target.ForObject(low.Id));
            g.PassRound();
            Assert.AreEqual(1, g.State.Chain.Count);
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(low.Id));

            Cast(g, me, "spark_snot", Target.ForObject(mine.Id));
            g.PassRound();
            Assert.AreEqual(0, g.State.Chain.Count, "only enemy creatures");
        }

        [Test]
        public void HexOfFestering_CantBeHealed_AndDamageCostsLife()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var victim = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 1);
            Cast(g, me, "hex_of_festering", Target.ForObject(victim.Id));
            g.PassRound();

            Cast(g, me, "spark_snot", Target.ForObject(victim.Id));
            g.PassRound();
            Assert.AreEqual(1, g.State.Chain.Count, "the Hex triggers");
            g.PassRound();
            Assert.AreEqual(29, g.P(g.Other).Life, "its controller loses 1 life");

            var tonic = g.AddToHand(g.Other, "barkeeps_tonic");
            g.P(g.Other).Gold = 1;
            g.Pass();
            g.Do(PlayerAction.Play(g.Other, tonic.Id, Target.ForObject(victim.Id)));
            g.PassRound();
            Assert.AreEqual(3, victim.Damage, "can't be healed");
        }

        [Test]
        public void CurseOfRot_HitsTheirCreaturesAtTheStartOfTheirTurn_AndStopsHealing()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var other = g.Other;
            var theirs = g.AddToBattlefield(other, "tavern_bouncer");
            var mine = g.AddToBattlefield(me, "tavern_bouncer", damage: 1);
            Cast(g, me, "curse_of_rot", Target.ForPlayer(other));
            g.PassRound();

            g.PassToStep(Step.Main1, other);
            Assert.AreEqual(1, theirs.Damage);
            Assert.AreEqual(1, mine.Damage, "only that player's creatures");

            var tonic = g.AddToHand(other, "barkeeps_tonic");
            g.SetMana(other, 1);
            g.Do(PlayerAction.Play(other, tonic.Id, Target.ForObject(theirs.Id)));
            g.PassRound();
            Assert.AreEqual(1, theirs.Damage, "creatures they control can't be healed");
        }

        [Test]
        public void HexOfHollowBones_ScalesWithCreatureCardsInYourGraveyard()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var victim = g.AddToBattlefield(g.Other, "tavern_bouncer"); // 2/5
            Cast(g, me, "hex_of_hollow_bones", Target.ForObject(victim.Id));
            g.PassRound();
            Assert.AreEqual(5, g.Stats(victim).MaxHealth, "empty graveyard");

            for (int i = 0; i < 6; i++)
                g.P(me).Graveyard.Add(new CardInstance
                {
                    Id = new ObjectId(g.State.NextObjectId++), DefinitionId = "hired_sellsword", Owner = me, Controller = me, Zone = Zone.Graveyard,
                });
            var st = g.Stats(victim);
            Assert.AreEqual(-2, st.Power);
            Assert.AreEqual(1, st.MaxHealth, "up to -4/-4");
        }

        [Test]
        public void ScarredVeteran_GetsPowerFromItsDamage()
        {
            var g = TestGame.AtFirstMainPhase();
            var vet = g.AddToBattlefield(g.Active, "scarred_veteran", damage: 3);
            Assert.AreEqual(5, g.Stats(vet).Power);
        }

        [Test]
        public void SapMender_PutsACounterOnCreaturesYouHeal()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "sap_mender");
            var hurt = g.AddToBattlefield(me, "tavern_bouncer", damage: 2);
            var healthy = g.AddToBattlefield(me, "hired_sellsword");
            Cast(g, me, "barkeeps_tonic", Target.ForObject(hurt.Id));
            g.PassRound();
            Assert.AreEqual(1, g.State.Chain.Count, "Sap Mender triggers");
            g.PassRound();
            Assert.AreEqual(1, hurt.PlusOneCounters);

            Cast(g, me, "barkeeps_tonic", Target.ForObject(healthy.Id));
            g.PassRound();
            Assert.AreEqual(0, g.State.Chain.Count, "nothing was healed: no trigger");
        }

        [Test]
        public void HerdMatriarch_GrowsBigArrivals()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(me, "herd_matriarch");
            var mammoth = Cast(g, me, "pit_champion"); // 6/5
            g.PassRound();
            Assert.AreEqual(1, g.State.Chain.Count, "6 Power: triggers");
            g.PassRound();
            Assert.AreEqual(2, g.OnBattlefield(me, "pit_champion").PlusOneCounters);

            Cast(g, me, "hired_sellsword");
            g.PassRound();
            Assert.AreEqual(0, g.State.Chain.Count, "2 Power: no trigger");
        }

        [Test]
        public void AmbushPredator_MayFightADamagedEnemy()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            g.AddToBattlefield(g.Other, "tavern_bouncer");                    // not damaged
            var hurt = g.AddToBattlefield(g.Other, "hired_sellsword", damage: 1); // 2/3, 2 left
            Cast(g, me, "ambush_predator");
            g.PassRound();
            Assert.AreEqual(DecisionKind.ChooseTriggerTarget, g.State.Pending?.Kind);
            var choices = g.Legal(me);
            Assert.AreEqual(2, choices.Count, "the damaged creature, or no target");
            g.Do(choices.Single(a => a.Target.HasValue));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(hurt.Id));
            Assert.IsNull(g.OnBattlefield(me, "ambush_predator"), "a fight: the 3/2 takes 2 back and dies too");
        }

        [Test]
        public void OverflowingSpring_HealsOrGrows()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var hurt = g.AddToBattlefield(me, "tavern_bouncer", damage: 4);
            var fresh = g.AddToBattlefield(me, "hired_sellsword");
            Cast(g, me, "overflowing_spring", Target.ForObject(hurt.Id));
            g.PassRound();
            Cast(g, me, "overflowing_spring", Target.ForObject(fresh.Id));
            g.PassRound();
            Assert.AreEqual(0, hurt.Damage);
            Assert.AreEqual(0, hurt.PlusOneCounters);
            Assert.AreEqual(2, fresh.PlusOneCounters);
        }

        [Test]
        public void SmartRounds_ChainsAcrossDamagedCreatures()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var target = g.AddToBattlefield(g.Other, "tavern_bouncer");
            var hurt = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 1);
            var fresh = g.AddToBattlefield(g.Other, "tavern_bouncer");
            Cast(g, me, "smart_rounds", Target.ForObject(target.Id));
            g.PassRound();
            Assert.AreEqual(1, target.Damage, "the target is hit once");
            Assert.AreEqual(2, hurt.Damage);
            Assert.AreEqual(0, fresh.Damage);
        }
    }
}
