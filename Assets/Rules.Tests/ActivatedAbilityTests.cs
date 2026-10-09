using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>Activated abilities (MTG 602): costs, payment (§5.2), timing, Tap and summoning sickness (§7.4), once each turn.</summary>
    public class ActivatedAbilityTests
    {
        [Test]
        public void TapAbility_UsesTheChain_AndTapsTheCreature()
        {
            var g = TestGame.AtFirstMainPhase();
            var elder = g.AddToBattlefield(g.Active, "grove_elder");
            var hurt = g.AddToBattlefield(g.Active, "tavern_bouncer", damage: 3);

            var heal = g.Activations(g.Active, elder).Single(a => a.Target == Target.ForObject(hurt.Id));
            g.Do(heal);
            Assert.IsTrue(elder.Tapped);
            Assert.AreEqual(1, g.State.Chain.Count);
            Assert.AreEqual(ChainItemKind.ActivatedAbility, g.State.Chain[0].Kind);
            Assert.AreEqual(g.Active, g.State.PriorityPlayer, "the activating player keeps priority (MTG 117.3c)");
            Assert.AreEqual(3, hurt.Damage, "nothing happens until it resolves");

            g.PassRound();
            Assert.AreEqual(1, hurt.Damage);
            Assert.IsEmpty(g.Activations(g.Active, elder), "tapped: can't use it again");
        }

        [Test]
        public void TapAbility_NotOnTheTurnItArrives_UnlessHaste()
        {
            var g = TestGame.AtFirstMainPhase();
            var elder = g.AddToBattlefield(g.Active, "grove_elder");
            g.AddToBattlefield(g.Active, "tavern_bouncer", damage: 1);
            elder.SummoningSick = true;
            Assert.IsEmpty(g.Activations(g.Active, elder), "§7.4: summoning sick");

            // Overclock Rig grants "Tap: Deal 1 damage" — a Haste creature can use it right away.
            var rascal = g.AddToBattlefield(g.Active, "goober_rascal");
            rascal.SummoningSick = true;
            var rig = g.AddToBattlefield(g.Active, "overclock_rig");
            rig.AttachedToObject = rascal.Id;
            Assert.IsNotEmpty(g.Activations(g.Active, rascal), "Haste ignores summoning sickness");
        }

        [Test]
        public void AbilityResolves_EvenIfItsSourceLeft()
        {
            var g = TestGame.AtFirstMainPhase();
            var elder = g.AddToBattlefield(g.Active, "grove_elder");   // 2/3
            var hurt = g.AddToBattlefield(g.Active, "tavern_bouncer", damage: 3);
            var snot = g.AddToHand(g.Other, "spark_snot");
            g.P(g.Other).Gold = 1;
            elder.Damage = 1;

            g.Do(g.Activations(g.Active, elder).Single(a => a.Target == Target.ForObject(hurt.Id)));
            g.Pass();
            g.Do(PlayerAction.Play(g.Other, snot.Id, Target.ForObject(elder.Id)));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(elder.Id));
            g.PassRound();
            Assert.AreEqual(1, hurt.Damage, "MTG 113.7a: an ability on the Chain doesn't need its source");
        }

        [Test]
        public void PayGold_IsGoldOnly_AndOnceEachTurn()
        {
            var g = TestGame.AtFirstMainPhase();
            var jar = g.AddToBattlefield(g.Active, "tip_jar");
            g.SetMana(g.Active, 5);
            g.P(g.Active).Gold = 2;
            Assert.IsEmpty(g.Activations(g.Active, jar), "'Pay 3 Gold' can't use mana (decided 2026-10-09)");

            g.P(g.Active).Gold = 5;
            int hand = g.P(g.Active).Hand.Count;
            g.Do(g.Activations(g.Active, jar).Single());
            Assert.AreEqual(2, g.P(g.Active).Gold);
            Assert.AreEqual(5, g.P(g.Active).Mana, "mana untouched");
            g.PassRound();
            Assert.AreEqual(hand + 1, g.P(g.Active).Hand.Count);

            g.P(g.Active).Gold = 5;
            Assert.IsEmpty(g.Activations(g.Active, jar), "once each turn");

            // MTG "once each turn": the opponent's turn is a new turn.
            var me = g.Active;
            g.PassToStep(Step.Main1, g.Other);
            g.Pass();
            Assert.AreEqual(me, g.State.PriorityPlayer);
            Assert.IsNotEmpty(g.Activations(me, jar), "usable again on the opponent's turn");
        }

        [Test]
        public void GenericCost_UsesManaFirst_ThenGold()
        {
            var g = TestGame.AtFirstMainPhase();
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            var blade = g.AddToBattlefield(g.Active, "pulse_blade"); // Equip 2
            g.SetMana(g.Active, 1);
            g.P(g.Active).Gold = 3;

            var equip = g.Activations(g.Active, blade).Single();
            var events = g.Do(equip);
            Assert.AreEqual(0, g.P(g.Active).Mana);
            Assert.AreEqual(2, g.P(g.Active).Gold, "1 mana, then 1 Gold");
            var activated = events.OfType<AbilityActivatedEvent>().Single();
            Assert.AreEqual(1, activated.ManaPaid);
            Assert.AreEqual(1, activated.GoldPaid);
            g.PassRound();
            Assert.AreEqual(blade.AttachedToObject, sword.Id);
        }

        [Test]
        public void SorcerySpeedAbility_OnlyInYourMainPhaseWithAnEmptyChain()
        {
            var g = TestGame.AtFirstMainPhase();
            var contract = g.AddToBattlefield(g.Active, "mercenary_contract");
            g.P(g.Active).Gold = 4;
            Assert.AreEqual(1, g.Activations(g.Active, contract).Count);

            var shock = g.AddToHand(g.Active, "static_shock");
            g.AddToBattlefield(g.Other, "hired_sellsword");
            g.SetMana(g.Active, 1);
            g.Do(g.Legal(g.Active).First(a => a.Card == shock.Id));
            Assert.IsEmpty(g.Activations(g.Active, contract), "not while the Chain has something on it");
            g.PassRound();

            g.Do(g.Activations(g.Active, contract).Single());
            g.PassRound();
            Assert.IsNotNull(g.OnBattlefield(g.Active, Cards.CardPool.MercenaryToken));

            var me = g.Active;
            g.PassToStep(Step.Main1, g.Other);
            g.Pass();
            Assert.IsEmpty(g.Activations(me, contract), "only as a sorcery: not on the opponent's turn");
        }

        [Test]
        public void Snik_CopiesUpToXGoobers_PayingX()
        {
            var g = TestGame.AtFirstMainPhase();
            var snik = g.AddToBattlefield(g.Active, "snik");
            var warchief = g.AddToBattlefield(g.Active, "goober_warchief");
            g.AddToBattlefield(g.Active, Cards.CardPool.GooberToken);
            g.AddToBattlefield(g.Active, Cards.CardPool.GooberToken);
            g.AddToBattlefield(g.Active, "hired_sellsword"); // not a Goober
            g.SetMana(g.Active, 2);

            var options = g.Activations(g.Active, snik);
            // Warchief 0-1 × tokens 0-2 (tokens are interchangeable), at least 1, at most X = 2 affordable.
            Assert.AreEqual(4, options.Count);
            Assert.IsTrue(options.All(a => a.X == a.Targets.Length && a.X >= 1 && a.X <= 2));
            Assert.IsFalse(options.Any(a => a.Targets.Contains(Target.ForObject(snik.Id))), "other Goobers only");

            g.Do(options.Single(a => a.X == 2 && a.Targets.Contains(Target.ForObject(warchief.Id))));
            Assert.AreEqual(0, g.P(g.Active).Mana);
            Assert.IsTrue(snik.Tapped);
            g.PassRound();
            var copies = g.P(g.Active).Battlefield.Where(c => c.IsToken && c.DefinitionId == "goober_warchief").ToList();
            Assert.AreEqual(1, copies.Count);
            Assert.IsTrue(g.Stats(copies[0]).Has(Keyword.Haste), "the copies gain Haste");
            Assert.AreEqual(3, g.P(g.Active).Battlefield.Count(c => c.DefinitionId == Cards.CardPool.GooberToken));
        }

        [Test]
        public void SacrificeCost_IsPaidOnActivation()
        {
            var g = TestGame.AtFirstMainPhase();
            g.SetTavernDweller(g.Active, "skabba");
            var tavernDweller = g.P(g.Active).TavernDweller;
            var fuse = g.AddToBattlefield(g.Active, "fuse_goober");
            g.SetMana(g.Active, 1);

            var power = g.Activations(g.Active, tavernDweller).Single(a => a.Sacrifice == fuse.Id);
            g.Do(power);
            Assert.IsNull(g.State.FindOnBattlefield(fuse.Id), "sacrificed as a cost, before anything resolves");
            Assert.AreEqual(0, g.P(g.Active).Mana);
        }
    }
}
