using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.AI;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// Choices the engine used to make by itself (decided 2026-10-09): dividing combat damage among several
    /// creatures (§7.2.6), the Legendary rule (MTG 704.5j) and ordering your own simultaneous triggers (MTG 603.3b).
    /// </summary>
    public class PlayerChoiceTests
    {
        private static CardDefinition StartOfTurn(string id, Effect effect)
        {
            var def = new CardDefinition { Id = id, Name = id, Type = CardType.Creature, Cost = 1, Power = 1, Health = 1 };
            def.Triggers.Add(new TriggeredAbility { When = TriggerEvent.StartOfYourTurn, Effects = { effect } });
            return def;
        }

        private static CardDefinition Legend => new CardDefinition
        {
            Id = "test_legend", Name = "Test Legend", Type = CardType.Creature, Cost = 3, Power = 3, Health = 3,
            Rarity = Rarity.Legendary,
        };

        private static void Attack(TestGame g, params CardInstance[] attackers)
        {
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            foreach (var a in attackers) g.Do(PlayerAction.Attack(g.Active, a.Id, g.Other));
            g.Do(PlayerAction.FinishAttacks(g.Active));
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareBlockers);
        }

        private static void Block(TestGame g, CardInstance blocker, CardInstance attacker) =>
            g.Do(PlayerAction.Block(g.Other, blocker.Id, attacker.Id));

        // ------------------------------------------------------------------ combat damage (§7.2.6)

        [Test]
        public void EnoughDamageToKillAllBlockers_NoChoice()
        {
            var g = TestGame.AtFirstMainPhase();
            var mammoth = g.AddToBattlefield(g.Active, "tusked_mammoth"); // 5/5
            var spider = g.AddToBattlefield(g.Other, "vine_spider");      // 2/3
            var runt = g.AddToBattlefield(g.Other, "brawling_runt");      // 2/2
            Attack(g, mammoth);
            Block(g, spider, mammoth);
            Block(g, runt, mammoth);
            g.Do(PlayerAction.FinishBlocks(g.Other));
            g.PassUntil(s => s.Step == Step.Main2 || s.Pending?.Kind == DecisionKind.AssignCombatDamage);

            Assert.AreEqual(Step.Main2, g.State.Step, "5 damage kills both (3 + 2): nothing to choose");
            Assert.IsNull(g.State.FindOnBattlefield(spider.Id));
            Assert.IsNull(g.State.FindOnBattlefield(runt.Id));
            Assert.AreEqual(4, mammoth.Damage);
        }

        [Test]
        public void Trample_NotEnoughForAllBlockers_DividesAmongBlockersOnly()
        {
            var g = TestGame.AtFirstMainPhase();
            var hog = g.AddToBattlefield(g.Active, "hog_rider");           // 3/3 Trample
            var sellsword = g.AddToBattlefield(g.Other, "hired_sellsword"); // 2/3
            var spider = g.AddToBattlefield(g.Other, "vine_spider");        // 2/3
            Attack(g, hog);
            Block(g, sellsword, hog);
            Block(g, spider, hog);
            g.Do(PlayerAction.FinishBlocks(g.Other));
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.AssignCombatDamage);

            var legal = g.Legal(g.Active);
            Assert.AreEqual(4, legal.Count, "3 damage over 2 blockers: 3/0, 2/1, 1/2, 0/3");
            Assert.IsTrue(legal.All(a => a.Division.Sum() == 3));
            CollectionAssert.AreEqual(new[] { sellsword.Id, spider.Id }, g.State.Pending.Choices, "block order");
            g.Do(PlayerAction.AssignDamage(g.Active, new[] { 0, 3 }));
            g.PassUntil(s => s.Step == Step.Main2);

            Assert.AreEqual(0, sellsword.Damage);
            Assert.IsNull(g.State.FindOnBattlefield(spider.Id), "the second blocker got all 3");
            Assert.AreEqual(30, g.P(g.Other).Life, "Trample only reaches the player once every blocker has lethal damage");
        }

        [Test]
        public void BlockerOfSeveralAttackers_DefenderDividesItsDamage()
        {
            var g = TestGame.AtFirstMainPhase();
            var first = g.AddToBattlefield(g.Active, "hired_sellsword");  // 2/3
            var second = g.AddToBattlefield(g.Active, "hired_sellsword"); // 2/3
            var champion = g.AddToBattlefield(g.Other, "retired_champion"); // 5/6, can block an additional creature
            Attack(g, first, second);
            Block(g, champion, first);
            Block(g, champion, second);
            g.Do(PlayerAction.FinishBlocks(g.Other));
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.AssignCombatDamage);

            Assert.AreEqual(g.Other, g.State.Pending.Player, "the blocker's controller divides its damage");
            Assert.AreEqual(champion.Id, g.State.Pending.Card);
            Assert.AreEqual(5, g.State.Pending.Count);
            g.Do(PlayerAction.AssignDamage(g.Other, new[] { 3, 2 }));
            g.PassUntil(s => s.Step == Step.Main2);

            Assert.IsNull(g.State.FindOnBattlefield(first.Id));
            Assert.AreEqual(2, second.Damage);
            Assert.AreEqual(4, champion.Damage, "each attacker had one blocker: no choice for them");
        }

        [Test]
        public void Bot_DividesDamageToKillWhatItCan()
        {
            var g = TestGame.AtFirstMainPhase();
            var fighter = g.AddToBattlefield(g.Active, "pit_fighter");       // 4/4 Trample
            var grizzly = g.AddToBattlefield(g.Other, "ironbark_grizzly");   // 4/5
            var sellsword = g.AddToBattlefield(g.Other, "hired_sellsword");  // 2/3
            Attack(g, fighter);
            Block(g, grizzly, fighter);
            Block(g, sellsword, fighter);
            g.Do(PlayerAction.FinishBlocks(g.Other));
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.AssignCombatDamage);

            var choice = new GreedyBot(g.Engine).Choose(g.State, g.Active);
            CollectionAssert.AreEqual(new[] { 1, 3 }, choice.Division, "kill the Sellsword, chip the Grizzly");
        }

        // ------------------------------------------------------------------ Legendary rule (704.5j)

        [Test]
        public void LegendaryRule_KeptCopyStaysAndTheOtherDies()
        {
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { Legend });
            var a = g.AddToBattlefield(g.Active, "test_legend");
            var b = g.AddToBattlefield(g.Active, "test_legend");
            var c = g.AddToBattlefield(g.Active, "test_legend");
            g.PassRound();

            Assert.AreEqual(DecisionKind.KeepLegendary, g.State.Pending.Kind);
            Assert.AreEqual(3, g.Legal(g.Active).Count);
            g.Do(PlayerAction.ChooseTarget(g.Active, Target.ForObject(b.Id)));

            Assert.IsNotNull(g.State.FindOnBattlefield(b.Id));
            Assert.IsNull(g.State.FindOnBattlefield(a.Id));
            Assert.IsNull(g.State.FindOnBattlefield(c.Id));
            Assert.AreEqual(2, g.Events.OfType<CreatureDiedEvent>().Count(), "going to the graveyard this way is dying");
        }

        [Test]
        public void Bot_KeepsTheHealthiestLegendary()
        {
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { Legend });
            g.AddToBattlefield(g.Active, "test_legend", damage: 2);
            var healthy = g.AddToBattlefield(g.Active, "test_legend");
            g.PassRound();

            var choice = new GreedyBot(g.Engine).Choose(g.State, g.Active);
            Assert.AreEqual(healthy.Id, choice.Target.Value.Object);
        }

        // ------------------------------------------------------------------ trigger order (603.3b)

        [Test]
        public void DifferentTriggersAtOnce_ControllerOrdersThem()
        {
            var life = StartOfTurn("test_life", new GainLifeEffect { Amount = 1 });
            var gold = StartOfTurn("test_gold", new GainGoldEffect { Amount = 1 });
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { life, gold });
            var me = g.Active;
            g.AddToBattlefield(me, "test_life");
            g.AddToBattlefield(me, "test_gold");
            g.PassUntil(s => s.ActivePlayer != me);
            g.PassUntil(s => s.ActivePlayer == me && s.Pending?.Kind == DecisionKind.OrderTriggers);

            Assert.AreEqual(me, g.State.Pending.Player);
            var options = g.Legal(me);
            Assert.AreEqual(2, options.Count);
            var goldFirst = options.Single(a => g.State.PendingTriggers[a.Option].SourceDefinitionId == "test_gold");
            g.Do(goldFirst);

            Assert.IsNull(g.State.Pending, "the last trigger goes on by itself");
            CollectionAssert.AreEqual(new[] { "test_gold", "test_life" }, g.State.Chain.Select(i => i.SourceDefinitionId),
                "the Gold trigger is at the bottom, so the life trigger resolves first");
        }

        [Test]
        public void IdenticalTriggers_AreNotAskedAbout()
        {
            var life = StartOfTurn("test_life", new GainLifeEffect { Amount = 1 });
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { life });
            var me = g.Active;
            g.AddToBattlefield(me, "test_life");
            g.AddToBattlefield(me, "test_life");
            g.PassUntil(s => s.ActivePlayer != me);
            g.PassUntil(s => s.ActivePlayer == me && (s.Chain.Count > 0 || s.Pending?.Kind == DecisionKind.OrderTriggers));

            Assert.IsNull(g.State.Pending);
            Assert.AreEqual(2, g.State.Chain.Count);
        }
    }
}
