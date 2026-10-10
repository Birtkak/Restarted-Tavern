using System.Linq;
using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>Replacement effects (MTG 614–616) with test-only cards: no card in the sets uses them yet, except Keeper Z-00.</summary>
    public class ReplacementEffectTests
    {
        private static CardDefinition Relic(string id, ReplacementAbility r) =>
            new CardDefinition { Id = id, Name = id, Type = CardType.Relic, Cost = 0, Faction = "neutral", Statics = { r } };

        private static CardDefinition Spell(string id, TargetSpec target, params Effect[] effects)
        {
            var def = new CardDefinition { Id = id, Name = id, Type = CardType.Instant, Cost = 0, Faction = "neutral", SpellTarget = target };
            def.SpellEffects.AddRange(effects);
            return def;
        }

        private static readonly CardDefinition Kill = Spell("test_kill", TargetSpec.Creature, new DestroyEffect());
        private static readonly CardDefinition Bolt = Spell("test_bolt", TargetSpec.AnyTarget, new DealDamageEffect { Amount = 3 });
        private static readonly CardDefinition DrawTwo = Spell("test_draw_two", TargetSpec.None, new DrawCardsEffect { Count = 2 });
        private static readonly CardDefinition GoldOne = Spell("test_gold", TargetSpec.None, new GainGoldEffect { Amount = 1 });
        private static readonly CardDefinition LifeFour = Spell("test_life", TargetSpec.None, new GainLifeEffect { Amount = 4 });

        private static TestGame Game(params CardDefinition[] extra) =>
            TestGame.AtFirstMainPhase(extraCards: extra.Concat(new[] { Kill, Bolt, DrawTwo, GoldOne, LifeFour }));

        private static void Cast(TestGame g, string id, CardInstance target = null, PlayerId? player = null)
        {
            var card = g.AddToHand(g.Active, id);
            var action = target != null ? PlayerAction.Play(g.Active, card.Id, Target.ForObject(target.Id))
                : player.HasValue ? PlayerAction.Play(g.Active, card.Id, Target.ForPlayer(player.Value))
                : PlayerAction.Play(g.Active, card.Id);
            g.Do(action);
            g.PassRound();
        }

        [Test]
        public void ExileInsteadOfDying_ItDoesntDie()
        {
            var ward = Relic("test_ward", new ReplacementAbility
            {
                Event = ReplacementEvent.Dies, Affects = TriggerSubject.You, Destination = Zone.Exile,
            });
            var g = Game(ward);
            var me = g.Active;
            g.SetTavernDweller(me, "skabba"); // "whenever one of your creatures dies, 1 damage to each opponent"
            g.AddToBattlefield(me, "test_ward");
            var sword = g.AddToBattlefield(me, "hired_sellsword");
            Cast(g, "test_kill", sword);
            Assert.AreEqual(1, g.P(me).Exile.Count(c => c.DefinitionId == "hired_sellsword"));
            Assert.AreEqual(0, g.P(me).Graveyard.Count(c => c.DefinitionId == "hired_sellsword"));
            Assert.AreEqual(0, g.State.Chain.Count, "it didn't die, so Skabba doesn't trigger");
            Assert.AreEqual(30, g.P(g.Other).Life);
        }

        [Test]
        public void UntilEndOfTurn_TargetCreatureReturnsToHandInstead_ThenItEnds()
        {
            var net = Spell("test_net", TargetSpec.CreatureYouControl, new AddReplacementEffect
            {
                ForTarget = true,
                Replacement = new ReplacementAbility { Event = ReplacementEvent.Dies, OnlyChosenObject = true, Destination = Zone.Hand },
            });
            var g = Game(net);
            var me = g.Active;
            var sword = g.AddToBattlefield(me, "wandering_adventurer");
            var bouncer = g.AddToBattlefield(me, "tavern_bouncer");
            Cast(g, "test_net", sword);
            Assert.AreEqual(1, g.State.Replacements.Count);
            Cast(g, "test_kill", bouncer);
            Assert.AreEqual(1, g.P(me).Graveyard.Count(c => c.DefinitionId == "tavern_bouncer"), "only the chosen creature");
            Cast(g, "test_kill", sword);
            Assert.AreEqual(1, g.P(me).Hand.Count(c => c.DefinitionId == "wandering_adventurer"), "back to its owner's hand");
            var extra = g.AddToBattlefield(me, "hired_sellsword");
            Cast(g, "test_kill", extra); // anything that checks the replacements again
            Assert.AreEqual(0, g.State.Replacements.Count, "the creature it was for is gone, so it's removed");

            var other = g.AddToBattlefield(me, "hired_sellsword");
            Cast(g, "test_net", other);
            Assert.AreEqual(1, g.State.Replacements.Count);
            g.PassUntil(s => s.TurnNumber == 2);
            Assert.AreEqual(0, g.State.Replacements.Count, "until end of turn");
        }

        [Test]
        public void TwoDamageReplacements_AppliedOldestFirst()
        {
            // Their prevention: 1 less damage to their creatures. Your doubler: your sources deal double.
            var shield = Relic("test_shield", new ReplacementAbility
            {
                Event = ReplacementEvent.DamageDealt, Affects = TriggerSubject.You, ToCreatures = true, Prevent = 1,
            });
            var doubler = Relic("test_doubler", new ReplacementAbility
            {
                Event = ReplacementEvent.DamageDealt, SourceControlledBy = TriggerSubject.You, Multiply = 2,
            });
            foreach (bool shieldFirst in new[] { true, false })
            {
                var g = Game(shield, doubler);
                var me = g.Active;
                var target = g.AddToBattlefield(g.Other, "tavern_bouncer"); // 2/5
                if (shieldFirst) g.AddToBattlefield(g.Other, "test_shield");
                g.AddToBattlefield(me, "test_doubler");
                if (!shieldFirst) g.AddToBattlefield(g.Other, "test_shield");
                Cast(g, "test_bolt", target);
                Assert.AreEqual(shieldFirst ? 4 : 5, target.Damage, shieldFirst ? "(3 - 1) x 2" : "3 x 2 - 1");

                Cast(g, "test_bolt", player: g.Other);
                Assert.AreEqual(24, g.P(g.Other).Life, "the shield only covers creatures; the doubler still applies");
            }
        }

        [Test]
        public void EachReplacementAppliesOnce_AndTheOthersAreCheckedAgain()
        {
            // Older: exile instead of dying. Newer: return to hand instead of dying. Once exiled it no longer dies.
            var exile = Relic("test_exile", new ReplacementAbility { Event = ReplacementEvent.Dies, Destination = Zone.Exile });
            var hand = Relic("test_hand", new ReplacementAbility { Event = ReplacementEvent.Dies, Destination = Zone.Hand });
            var g = Game(exile, hand);
            var me = g.Active;
            g.AddToBattlefield(me, "test_exile");
            g.AddToBattlefield(me, "test_hand");
            var victim = g.AddToBattlefield(me, "wandering_adventurer");
            Cast(g, "test_kill", victim);
            Assert.AreEqual(1, g.P(me).Exile.Count(c => c.DefinitionId == "wandering_adventurer"));
            Assert.AreEqual(0, g.P(me).Hand.Count(c => c.DefinitionId == "wandering_adventurer"));
        }

        [Test]
        public void SelfReplacement_GoesFirst()
        {
            var exile = Relic("test_exile", new ReplacementAbility { Event = ReplacementEvent.Dies, Destination = Zone.Exile });
            var phoenix = new CardDefinition
            {
                Id = "test_phoenix", Name = "test_phoenix", Type = CardType.Creature, Cost = 1, Power = 1, Health = 1, Faction = "neutral",
                Statics = { new ReplacementAbility { Event = ReplacementEvent.Dies, OnlySelf = true, SelfReplacement = true, Destination = Zone.Hand } },
            };
            var g = Game(exile, phoenix);
            var me = g.Active;
            g.AddToBattlefield(me, "test_exile"); // older, but not a self-replacement
            var bird = g.AddToBattlefield(me, "test_phoenix");
            Cast(g, "test_kill", bird);
            Assert.AreEqual(1, g.P(me).Hand.Count(c => c.DefinitionId == "test_phoenix"));
        }

        [Test]
        public void EntersTapped_AndKeeperCounters()
        {
            var gate = Relic("test_gate", new ReplacementAbility { Event = ReplacementEvent.Enters, EntersTapped = true });
            var g = Game(gate);
            var me = g.Active;
            g.SetTavernDweller(me, "keeper_z00");
            g.AddToBattlefield(me, "test_gate");
            g.SetMana(me, 3);
            var bouncer = g.AddToHand(me, "tavern_bouncer"); // 2/5: Keeper adds a counter
            g.Do(PlayerAction.Play(me, bouncer.Id));
            g.PassRound();
            var onField = g.OnBattlefield(me, "tavern_bouncer");
            Assert.IsTrue(onField.Tapped);
            Assert.AreEqual(1, onField.PlusOneCounters);
        }

        [Test]
        public void Draw_GainGoldInstead_OrDrawDouble()
        {
            var pension = Relic("test_pension", new ReplacementAbility
            {
                Event = ReplacementEvent.Draw, Affects = TriggerSubject.You, Skip = true, Instead = { new GainGoldEffect { Amount = 1 } },
            });
            var g = Game(pension);
            var me = g.Active;
            g.AddToBattlefield(me, "test_pension");
            int hand = g.P(me).Hand.Count;
            Cast(g, "test_draw_two");
            Assert.AreEqual(hand, g.P(me).Hand.Count, "no cards drawn");
            Assert.AreEqual(2, g.P(me).Gold);

            var library = Relic("test_library", new ReplacementAbility { Event = ReplacementEvent.Draw, Affects = TriggerSubject.You, Multiply = 2 });
            g = Game(library);
            me = g.Active;
            g.AddToBattlefield(me, "test_library");
            hand = g.P(me).Hand.Count;
            Cast(g, "test_draw_two");
            Assert.AreEqual(hand + 4, g.P(me).Hand.Count, "each draw becomes two (and those aren't doubled again)");
        }

        [Test]
        public void GoldAndLifeGains_CanBeChanged()
        {
            var interest = Relic("test_interest", new ReplacementAbility { Event = ReplacementEvent.GainGold, Affects = TriggerSubject.You, Add = 1 });
            var famine = Relic("test_famine", new ReplacementAbility { Event = ReplacementEvent.GainLife, Affects = TriggerSubject.Opponents, Skip = true });
            var g = Game(interest, famine);
            var me = g.Active;
            g.AddToBattlefield(me, "test_interest");
            g.AddToBattlefield(g.Other, "test_famine");
            Cast(g, "test_gold");
            Assert.AreEqual(2, g.P(me).Gold, "1 + 1");
            g.P(me).Life = 20;
            Cast(g, "test_life");
            Assert.AreEqual(20, g.P(me).Life, "the opponent's Famine: no life gained");
        }

        [Test]
        public void TheNextTime_AppliesOnce()
        {
            var fog = Spell("test_fog", TargetSpec.None, new AddReplacementEffect
            {
                Uses = 1,
                Replacement = new ReplacementAbility
                {
                    Event = ReplacementEvent.DamageDealt, Affects = TriggerSubject.You, ToCreatures = false, PreventAll = true,
                },
            });
            var g = Game(fog);
            var me = g.Active;
            Cast(g, "test_fog");
            Cast(g, "test_bolt", player: me);
            Assert.AreEqual(30, g.P(me).Life, "the next time: prevented");
            Assert.AreEqual(0, g.State.Replacements.Count, "used up");
            Cast(g, "test_bolt", player: me);
            Assert.AreEqual(27, g.P(me).Life);
        }
    }
}
