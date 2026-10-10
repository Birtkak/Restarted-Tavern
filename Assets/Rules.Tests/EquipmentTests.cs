using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>Equip (MTG 701.3, GAME_DESIGN §10), Equipment bonuses and the Equipment cards.</summary>
    public class EquipmentTests
    {
        private static void Equip(TestGame g, CardInstance equipment, CardInstance creature)
        {
            g.Do(g.Activations(g.Active, equipment).Single(a => a.Target == Target.ForObject(creature.Id)));
            g.PassRound();
        }

        [Test]
        public void Equip_GivesTheBonus_OnlyToYourCreatures_AtSorcerySpeed()
        {
            var g = TestGame.AtFirstMainPhase();
            var shiv = g.AddToBattlefield(g.Active, "neon_shiv");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");   // 2/3
            g.AddToBattlefield(g.Other, "tavern_bouncer");
            g.SetMana(g.Active, 3);

            var options = g.Activations(g.Active, shiv);
            Assert.AreEqual(1, options.Count, "target creature you control");
            Equip(g, shiv, sword);
            Assert.AreEqual(3, g.Stats(sword).Power);
            Assert.AreEqual(4, g.Stats(sword).MaxHealth);
            Assert.IsEmpty(g.Activations(g.Active, shiv), "already on the only creature: re-equipping there isn't offered");

            g.AddToBattlefield(g.Active, "tavern_bouncer");
            var shock = g.AddToHand(g.Active, "static_shock");
            g.Do(g.Legal(g.Active).First(a => a.Card == shock.Id));
            Assert.IsEmpty(g.Activations(g.Active, shiv), "Equip is sorcery speed: not while the Chain has something on it");
        }

        [Test]
        public void Reequip_MovesTheBonus_AndLosingItCantKill()
        {
            var g = TestGame.AtFirstMainPhase();
            var shiv = g.AddToBattlefield(g.Active, "neon_shiv");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");   // 2/3
            var bouncer = g.AddToBattlefield(g.Active, "tavern_bouncer");
            g.SetMana(g.Active, 2);
            Equip(g, shiv, sword);
            sword.Damage = 3; // 3/4 with 3 damage: 1 left

            Equip(g, shiv, bouncer);
            Assert.AreEqual(3, g.Stats(bouncer).Power);
            Assert.IsNotNull(g.State.FindOnBattlefield(sword.Id), "§7.3: losing a Health buff can't kill");
            Assert.AreEqual(1, g.Stats(sword).RemainingHealth);
        }

        [Test]
        public void CreatureDies_EquipmentStays_Unattached_AndCanBeReequipped()
        {
            var g = TestGame.AtFirstMainPhase();
            var shiv = g.AddToBattlefield(g.Active, "neon_shiv");
            var runt = g.AddToBattlefield(g.Active, "brawling_runt");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            g.SetMana(g.Active, 2);
            Equip(g, shiv, runt);

            var snot = g.AddToHand(g.Other, "spark_snot");
            g.P(g.Other).Gold = 1;
            runt.Damage = 1; // 3/3 with 1 damage
            g.Pass();
            g.Do(PlayerAction.Play(g.Other, snot.Id, Target.ForObject(runt.Id)));
            g.PassRound();

            Assert.IsNull(g.State.FindOnBattlefield(runt.Id));
            Assert.IsNotNull(g.State.FindOnBattlefield(shiv.Id), "Equipment stays on the battlefield");
            Assert.IsTrue(shiv.AttachedToObject.IsNone, "unattached (state-based action)");
            Equip(g, shiv, sword);
            Assert.AreEqual(3, g.Stats(sword).Power);
        }

        [Test]
        public void PulseBlade_GrantsAnAttackTrigger()
        {
            var g = TestGame.AtFirstMainPhase();
            var blade = g.AddToBattlefield(g.Active, "pulse_blade");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            var target = g.AddToBattlefield(g.Other, "tavern_bouncer");
            g.SetMana(g.Active, 2);
            Equip(g, blade, sword);

            g.GoToCombat();
            g.Do(PlayerAction.Attack(g.Active, sword.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            Assert.AreEqual(DecisionKind.ChooseTriggerTarget, g.State.Pending.Kind, "the granted attack trigger");
            g.Do(PlayerAction.ChooseTarget(g.Active, Target.ForObject(target.Id)));
            g.PassRound();
            Assert.AreEqual(1, target.Damage);
        }

        [Test]
        public void OverclockRig_GrantsATapAbility()
        {
            var g = TestGame.AtFirstMainPhase();
            var rig = g.AddToBattlefield(g.Active, "overclock_rig");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            g.SetMana(g.Active, 2);
            Assert.IsEmpty(g.Activations(g.Active, sword));
            Equip(g, rig, sword);

            g.Do(g.Activations(g.Active, sword).Single(a => a.Target == Target.ForPlayer(g.Other)));
            g.PassRound();
            Assert.AreEqual(29, g.P(g.Other).Life);
            Assert.IsTrue(sword.Tapped);
        }

        [Test]
        public void ArcWelder_TitanFrame_AndScrapCollector_Trigger()
        {
            var g = TestGame.AtFirstMainPhase();
            g.AddToBattlefield(g.Active, "arc_welder");
            g.AddToBattlefield(g.Active, "scrap_collector");
            var titan = g.AddToBattlefield(g.Active, "titan_frame_guardian", damage: 4);
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            var shiv = g.AddToBattlefield(g.Active, "neon_shiv");
            g.SetMana(g.Active, 2);
            int hand = g.P(g.Active).Hand.Count;

            g.Do(g.Activations(g.Active, shiv).Single(a => a.Target == Target.ForObject(titan.Id)));
            Assert.AreEqual(DecisionKind.ChooseTriggerTarget, g.State.Pending.Kind, "Arc Welder: whenever you pay an Equip cost");
            g.Do(PlayerAction.ChooseTarget(g.Active, Target.ForPlayer(g.Other)));
            g.PassRound(); // Welder ping
            Assert.AreEqual(29, g.P(g.Other).Life);
            g.PassRound(); // Equip resolves → Titan-Frame triggers
            g.PassRound();
            Assert.AreEqual(0, titan.Damage, "healed fully");
            Assert.AreEqual(hand + 1, g.P(g.Active).Hand.Count);

            int gold = g.P(g.Active).Gold;
            g.Do(g.Activations(g.Active, shiv).Single(a => a.Target == Target.ForObject(sword.Id)));
            g.Do(PlayerAction.ChooseTarget(g.Active, Target.ForPlayer(g.Other)));
            g.PassRound();
            g.PassRound(); // the Shiv moves: it became unattached from Titan-Frame
            g.PassRound();
            Assert.AreEqual(gold, g.P(g.Active).Gold, "Scrap Collector pays only when an equipped creature dies (no Equip loop)");
        }

        /// <summary>The active player destroys their own creature (Ritual Slaughter) and everything resolves.</summary>
        private static void Kill(TestGame g, CardInstance creature)
        {
            var spell = g.AddToHand(g.Active, "ritual_slaughter");
            g.SetMana(g.Active, 4);
            g.Do(PlayerAction.Play(g.Active, spell.Id, Target.ForObject(creature.Id)));
            g.PassUntil(st => st.Chain.Count == 0 && st.Pending == null && st.ActivePlayer == g.Active && st.PriorityPlayer == g.Active);
        }

        [Test]
        public void ScrapCollector_GoldWhenAnEquippedCreatureDies_NotForAnUnequippedOne()
        {
            var g = TestGame.AtFirstMainPhase();
            g.AddToBattlefield(g.Active, "scrap_collector");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            var bare = g.AddToBattlefield(g.Active, "brawling_runt");
            var shiv = g.AddToBattlefield(g.Active, "neon_shiv");
            g.SetMana(g.Active, 1);
            g.Do(g.Activations(g.Active, shiv).Single(a => a.Target == Target.ForObject(sword.Id)));
            g.PassRound();
            Assert.AreEqual(sword.Id, shiv.AttachedToObject);

            int gold = g.P(g.Active).Gold;
            Kill(g, bare);
            Assert.IsNull(g.State.FindOnBattlefield(bare.Id));
            Assert.AreEqual(gold, g.P(g.Active).Gold, "not equipped: no Gold");

            Kill(g, sword);
            Assert.IsNull(g.State.FindOnBattlefield(sword.Id));
            Assert.AreEqual(gold + 1, g.P(g.Active).Gold, "the equipped Sellsword died: 1 Gold");
        }

        [Test]
        public void ArchonLumen_OneTriggerForXDamage()
        {
            var g = TestGame.AtFirstMainPhase();
            g.AddToBattlefield(g.Active, "archon_lumen");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            var cannon = g.AddToBattlefield(g.Active, "rail_cannon"); // Equip 3
            g.AddToBattlefield(g.Active, "neon_shiv");
            g.SetMana(g.Active, 0);
            Assert.IsFalse(g.Legal(g.Active).Any(a => a.Kind == ActionKind.ActivateAbility && a.Card == cannon.Id),
                "Equip isn't free any more (balance 2026-10-10)");

            g.PassUntil(s => s.Step == Step.End && s.Pending != null);
            Assert.AreEqual(DecisionKind.ChooseTriggerTarget, g.State.Pending.Kind);
            g.Do(PlayerAction.ChooseTarget(g.Active, Target.ForPlayer(g.Other)));
            Assert.AreEqual(1, g.State.Chain.Count, "one trigger, not one per Equipment (playtest 2026-10-10)");
            Assert.IsNull(g.State.Pending);
            g.PassRound();
            g.PassRound();
            Assert.AreEqual(28, g.P(g.Other).Life, "two Equipment: 2 damage");
        }

        [Test]
        public void CourierBot_MayAttachAnEquipment()
        {
            var g = TestGame.AtFirstMainPhase();
            var shiv = g.AddToBattlefield(g.Active, "neon_shiv");
            var bot = g.AddToHand(g.Active, "courier_bot");
            g.SetMana(g.Active, 1);
            g.Do(PlayerAction.Play(g.Active, bot.Id));
            g.PassRound();
            Assert.AreEqual(DecisionKind.ChooseTriggerTarget, g.State.Pending.Kind, "a single target still asks: it's a 'may'");
            Assert.AreEqual(2, g.Legal(g.Active).Count, "attach, or decline");
            g.Do(g.Legal(g.Active).First(a => a.Target.HasValue));
            g.PassRound();
            var onBattlefield = g.OnBattlefield(g.Active, "courier_bot");
            Assert.AreEqual(onBattlefield.Id, shiv.AttachedToObject);
            Assert.AreEqual(2, g.Stats(onBattlefield).Power);
        }

        [Test]
        public void AlleyTinker_MakesAnEquipmentToken_AndPatrolCaptainBuffsEquipped()
        {
            var g = TestGame.AtFirstMainPhase();
            g.AddToBattlefield(g.Active, "patrol_captain");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            var tinker = g.AddToHand(g.Active, "alley_tinker");
            g.SetMana(g.Active, 3);
            g.Do(PlayerAction.Play(g.Active, tinker.Id));
            g.PassRound();
            g.PassRound();
            var plating = g.OnBattlefield(g.Active, CardPool.ScrapPlatingToken);
            Assert.IsNotNull(plating);
            Equip(g, plating, sword);
            Assert.AreEqual(3, g.Stats(sword).Power, "Patrol Captain: +1/+1 to equipped creatures");
            Assert.AreEqual(6, g.Stats(sword).MaxHealth, "3 + 2 (Plating) + 1 (Captain)");
        }

        [Test]
        public void MarksmanScope_TriggersOnCombatDamageToAPlayer()
        {
            var g = TestGame.AtFirstMainPhase();
            var scope = g.AddToBattlefield(g.Active, "marksman_scope");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            g.SetMana(g.Active, 1);
            Equip(g, scope, sword);
            g.GoToCombat();
            g.Do(PlayerAction.Attack(g.Active, sword.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            g.PassUntil(s => s.Step == Step.CombatDamage);
            Assert.AreEqual(27, g.P(g.Other).Life);
            Assert.AreEqual(1, g.State.Chain.Count, "the granted trigger (target: the only creature, itself)");
        }
    }
}
