using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>Activated abilities (MTG 602): costs, payment (§5.2), timing, Tap (no summoning sickness, §7.4), once each round.</summary>
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
        public void TapAbility_UsableTheRoundItArrives()
        {
            var g = TestGame.AtFirstMainPhase();
            var elder = g.AddToHand(g.Active, "grove_elder");
            g.AddToBattlefield(g.Active, "tavern_bouncer", damage: 1);
            g.SetMana(g.Active, 10);
            g.Do(g.Legal(g.Active).First(a => a.Card == elder.Id));
            g.PassRound();
            var onField = g.OnBattlefield(g.Active, "grove_elder");
            Assert.IsNotEmpty(g.Activations(g.Active, onField), "§7.4: no summoning sickness");
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

            // "Once each turn" means once each round: the next round is a new turn.
            var me = g.Active;
            g.NextRound();
            g.Pass();
            Assert.AreEqual(me, g.State.PriorityPlayer);
            Assert.IsNotEmpty(g.Activations(me, jar), "usable again in the next round");
        }

        [Test]
        public void GenericCost_UsesGoldFirst_ThenMana()
        {
            var g = TestGame.AtFirstMainPhase();
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword");
            var blade = g.AddToBattlefield(g.Active, "pulse_blade"); // Equip 2
            g.SetMana(g.Active, 1);
            g.P(g.Active).Gold = 3;

            var equip = g.Activations(g.Active, blade).Single();
            var events = g.Do(equip);
            Assert.AreEqual(1, g.P(g.Active).Mana);
            Assert.AreEqual(1, g.P(g.Active).Gold, "§5.2: Gold first");
            var activated = events.OfType<AbilityActivatedEvent>().Single();
            Assert.AreEqual(0, activated.ManaPaid);
            Assert.AreEqual(2, activated.GoldPaid);
            g.PassRound();
            Assert.AreEqual(blade.AttachedToObject, sword.Id);
        }

        [Test]
        public void SorcerySpeedAbility_OnlyOnYourActionWithAnEmptyChain()
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
        }

        [Test]
        public void Snik_PaysX_ThenChoosesUpToXGoobersOnResolution()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var snik = g.AddToBattlefield(me, "snik");
            var warchief = g.AddToBattlefield(me, "goober_warchief");
            var token = g.AddToBattlefield(me, Cards.CardPool.GooberToken);
            g.AddToBattlefield(me, Cards.CardPool.GooberToken);
            g.AddToBattlefield(me, "hired_sellsword"); // not a Goober
            g.SetMana(me, 2);

            var options = g.Activations(me, snik);
            Assert.AreEqual(new[] { 1, 2 }, options.Select(a => a.X).OrderBy(x => x).ToArray(), "X from 1 up to what can be paid");
            Assert.IsTrue(options.All(a => a.Targets.Length == 0), "nothing is targeted (MTG 608.2d)");
            g.Do(options.Single(a => a.X == 2));
            Assert.AreEqual(0, g.P(me).Mana);
            Assert.IsTrue(snik.Tapped);
            g.PassRound();

            Assert.AreEqual(DecisionKind.ChooseUpTo, g.State.Pending.Kind);
            var picks = g.Legal(me).Where(a => a.Target.HasValue).Select(a => a.Target.Value.Object).ToList();
            Assert.AreEqual(3, picks.Count, "the other Goobers you control, not Snik or the Sellsword");
            Assert.IsFalse(picks.Contains(snik.Id));
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(warchief.Id)));
            Assert.AreEqual(DecisionKind.ChooseUpTo, g.State.Pending.Kind, "one more pick");
            Assert.IsFalse(g.Legal(me).Any(a => a.Target == Target.ForObject(warchief.Id)), "each creature once");
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(token.Id)));
            Assert.IsNull(g.State.Pending, "X picks made");

            var copies = g.P(me).Battlefield.Where(c => c.IsToken && c.DefinitionId == "goober_warchief").ToList();
            Assert.AreEqual(1, copies.Count);
            Assert.AreEqual(3, g.P(me).Battlefield.Count(c => c.DefinitionId == Cards.CardPool.GooberToken));
        }

        [Test]
        public void Snik_AGooberThatLeftInResponse_JustCantBeChosen()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var snik = g.AddToBattlefield(me, "snik");
            var warchief = g.AddToBattlefield(me, "goober_warchief");
            var token = g.AddToBattlefield(me, Cards.CardPool.GooberToken);
            g.SetMana(me, 2);
            g.Do(g.Activations(me, snik).Single(a => a.X == 2));
            g.P(me).Battlefield.Remove(warchief); // gone in response (test shortcut)
            warchief.Zone = Zone.Graveyard;
            g.PassRound();
            // Only the token is left to choose: pick it, then the choice ends by itself.
            Assert.AreEqual(DecisionKind.ChooseUpTo, g.State.Pending.Kind);
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(token.Id)));
            Assert.IsNull(g.State.Pending);
            Assert.AreEqual(2, g.P(me).Battlefield.Count(c => c.DefinitionId == Cards.CardPool.GooberToken));
            Assert.AreEqual(0, g.P(me).Battlefield.Count(c => c.DefinitionId == "goober_warchief"));
        }

        [Test]
        public void Snik_UpTo_CanStopEarly()
        {
            var g = TestGame.AtFirstMainPhase();
            var me = g.Active;
            var snik = g.AddToBattlefield(me, "snik");
            var warchief = g.AddToBattlefield(me, "goober_warchief");
            g.AddToBattlefield(me, Cards.CardPool.GooberToken);
            g.SetMana(me, 2);
            g.Do(g.Activations(me, snik).Single(a => a.X == 2));
            g.PassRound();
            g.Do(PlayerAction.ChooseTarget(me, Target.ForObject(warchief.Id)));
            g.Do(new PlayerAction { Kind = ActionKind.ChooseTarget, Player = me }); // done
            Assert.IsNull(g.State.Pending);
            Assert.AreEqual(1, g.P(me).Battlefield.Count(c => c.IsToken && c.DefinitionId == "goober_warchief"));
            Assert.AreEqual(1, g.P(me).Battlefield.Count(c => c.DefinitionId == Cards.CardPool.GooberToken));
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
