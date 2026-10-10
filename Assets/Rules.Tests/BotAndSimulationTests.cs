using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.AI;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>The GreedyBot, the match runner and the simulation report.</summary>
    public class BotAndSimulationTests
    {
        [Test]
        public void GreedyBot_FinishesGames_AndIsDeterministic()
        {
            var db = CardPool.CreateDatabase();
            var cfg = new MatchConfig
            {
                Name = "smoke", DeckAName = "G", DeckA = CardPool.GooberMobDeck(), TavernDwellerA = CardPool.GooberMobTavernDweller,
                DeckBName = "J", DeckB = CardPool.JungleStampedeDeck(), TavernDwellerB = CardPool.JungleStampedeTavernDweller, Games = 20,
            };
            var a = MatchRunner.Run(cfg, db);
            var b = MatchRunner.Run(cfg, db);
            Assert.AreEqual(20, a.Games);
            Assert.AreEqual(0, a.Draws, "games should end well before the turn limit");
            Assert.AreEqual(a.WinsA, b.WinsA);
            CollectionAssert.AreEqual(a.GameLengths, b.GameLengths);
            Assert.Greater(a.CreatureRoundSamples, 0);
        }

        [Test]
        public void GreedyBot_UsesTavernDwellerPowersAndEquip()
        {
            var db = CardPool.CreateDatabase();
            var cfg = new MatchConfig
            {
                Name = "arsenal", DeckAName = "A", DeckA = CardPool.AuditorsArsenalDeck(), TavernDwellerA = CardPool.AuditorsArsenalTavernDweller,
                DeckBName = "Z", DeckB = CardPool.ZooPatrolDeck(), TavernDwellerB = CardPool.ZooPatrolTavernDweller, Games = 10,
            };
            var r = MatchRunner.Run(cfg, db);
            Assert.AreEqual(0, r.Draws);
            Assert.Greater(r.PowersUsed, 0, "Tavern Dweller Powers get used");
            Assert.Greater(r.PowersOnOpponentsTurn, 0, "some while the opponent had the action, with Gold");
            Assert.Greater(r.AbilitiesActivated, 0, "Equip and Tap abilities get used");
        }

        [Test]
        public void GreedyBot_EquipsItsBestCreature()
        {
            var g = TestGame.AtFirstMainPhase();
            var shiv = g.AddToBattlefield(g.Active, "megacorp_exosuit");
            g.AddToBattlefield(g.Active, "goober_rascal");
            var bouncer = g.AddToBattlefield(g.Active, "ironbark_grizzly");
            g.SetMana(g.Active, 3);
            var choice = new GreedyBot(g.Engine).Choose(g.State, g.Active);
            Assert.AreEqual(ActionKind.ActivateAbility, choice.Kind);
            Assert.AreEqual(Target.ForObject(bouncer.Id), choice.Target);
        }

        [Test]
        public void GreedyBot_TakesAKill_WithSparkSnot()
        {
            var g = TestGame.AtFirstMainPhase();
            g.AddToBattlefield(g.Other, "hired_sellsword");               // 2/3: Snot doesn't kill
            var wounded = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 3); // 2/5, 2 left: Snot kills
            var snot = g.AddToHand(g.Active, "spark_snot");
            var choice = new GreedyBot(g.Engine).Choose(g.State, g.Active);
            Assert.AreEqual(PlayerAction.Play(g.Active, snot.Id, Target.ForObject(wounded.Id)), choice);
        }

        [Test]
        public void GreedyBot_DoesNotBurnACreatureItsChainAlreadyKills()
        {
            // Playtest 2026-10-10_192133: two Spark Snots at one creature, the second fizzled.
            var g = TestGame.AtFirstMainPhase();
            var wounded = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 3); // 2 left: one Snot kills
            var first = g.AddToHand(g.Active, "spark_snot");
            var second = g.AddToHand(g.Active, "spark_snot");
            g.SetMana(g.Active, 10);
            g.Do(PlayerAction.Play(g.Active, first.Id, Target.ForObject(wounded.Id)));
            Assert.AreEqual(1, g.State.Chain.Count);
            var me = g.State.PriorityPlayer ?? g.Active;
            Assume.That(me == g.Active, "the caster keeps priority");
            var choice = new GreedyBot(g.Engine).Choose(g.State, g.Active);
            Assert.AreNotEqual(PlayerAction.Play(g.Active, second.Id, Target.ForObject(wounded.Id)), choice);
        }

        [Test]
        public void GreedyBot_DoesNotAttackIntoABadBlock()
        {
            var g = TestGame.AtFirstMainPhase();
            g.AddToBattlefield(g.Active, "goober_rascal");   // 2/1
            g.AddToBattlefield(g.Other, "tavern_bouncer");   // 2/5 eats it
            g.GoToCombat();
            var choice = new GreedyBot(g.Engine).Choose(g.State, g.Active);
            Assert.AreEqual(ActionKind.FinishAttacks, choice.Kind);
        }

        [Test]
        public void GreedyBot_BeatsRandomPlay()
        {
            var db = CardPool.CreateDatabase();
            var engine = new GameEngine(db);
            var bot = new GreedyBot(engine);
            int botWins = 0;
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var state = engine.CreateGame(FormatConfig.Standard(), new[]
                {
                    new PlayerSetup { Deck = CardPool.GooberMobDeck(), TavernDwellerId = CardPool.GooberMobTavernDweller },
                    new PlayerSetup { Deck = CardPool.GooberMobDeck(), TavernDwellerId = CardPool.GooberMobTavernDweller },
                }, seed);
                var rng = new DeterministicRng(seed);
                var botPlayer = new PlayerId(seed % 2 == 0 ? 1 : 2);
                while (!state.IsGameOver)
                {
                    var who = engine.WaitingOn(state).Value;
                    var legal = engine.GetLegalActions(state, who);
                    engine.Apply(state, who == botPlayer ? bot.Choose(state, who) : legal[rng.Next(legal.Count)]);
                }
                if (state.Winners.Contains(botPlayer)) botWins++;
            }
            Assert.GreaterOrEqual(botWins, 32, "the bot should win at least 80% against random play");
        }

        [Test]
        public void GoldBankedEvent_ReportsWhatTheCapWasted()
        {
            var g = TestGame.AtFirstMainPhase();
            var first = g.Active;
            g.P(first).Gold = 2;
            g.SetMana(first, 3);
            g.PassUntil(s => s.RoundNumber == 2);
            var banked = g.Events.OfType<GoldBankedEvent>().First(e => e.Player == first);
            Assert.AreEqual(3, banked.UnspentMana);
            Assert.AreEqual(1, banked.Banked);
        }

        [Test]
        public void SimulationReport_RendersEverySection()
        {
            var sections = Experiments.Build(2);
            Experiments.Run(sections, CardPool.CreateDatabase());
            var md = Experiments.ToMarkdown(sections, 2, System.TimeSpan.FromSeconds(1));
            foreach (var s in sections) StringAssert.Contains(s.Title, md);
            Assert.AreEqual(sections.Sum(s => s.Configs.Count), md.Split('\n').Count(l => l.StartsWith("| ") && !l.StartsWith("| Matchup")));
        }
    }
}
