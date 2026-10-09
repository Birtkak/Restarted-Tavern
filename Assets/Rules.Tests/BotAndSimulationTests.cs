using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.AI;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>The GreedyBot, the match runner and the experiment-only format switches.</summary>
    public class BotAndSimulationTests
    {
        [Test]
        public void GreedyBot_FinishesGames_AndIsDeterministic()
        {
            var db = PrototypeCards.CreateDatabase();
            var cfg = new MatchConfig
            {
                Name = "smoke", DeckAName = "G", DeckA = PrototypeCards.GooberMobDeck(),
                DeckBName = "J", DeckB = PrototypeCards.JungleStampedeDeck(), Games = 20,
            };
            var a = MatchRunner.Run(cfg, db);
            var b = MatchRunner.Run(cfg, db);
            Assert.AreEqual(20, a.Games);
            Assert.AreEqual(0, a.Draws, "games should end well before the turn limit");
            Assert.AreEqual(a.WinsA, b.WinsA);
            CollectionAssert.AreEqual(a.GameLengths, b.GameLengths);
            Assert.Greater(a.CreatureTurnSamples, 0);
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
        public void GreedyBot_DoesNotAttackIntoABadBlock()
        {
            var g = TestGame.AtFirstMainPhase();
            g.AddToBattlefield(g.Active, "goober_rascal");   // 2/1
            g.AddToBattlefield(g.Other, "tavern_bouncer");   // 2/5 eats it
            g.PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
            var choice = new GreedyBot(g.Engine).Choose(g.State, g.Active);
            Assert.AreEqual(ActionKind.FinishAttacks, choice.Kind);
        }

        [Test]
        public void GreedyBot_BeatsRandomPlay()
        {
            var db = PrototypeCards.CreateDatabase();
            var engine = new GameEngine(db);
            var bot = new GreedyBot(engine);
            int botWins = 0;
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var state = engine.CreateGame(FormatConfig.Standard(), new[]
                {
                    new PlayerSetup { Deck = PrototypeCards.GooberMobDeck() },
                    new PlayerSetup { Deck = PrototypeCards.GooberMobDeck() },
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
        public void DamageWearsOff_ExperimentSwitch_ClearsDamageAtCleanup()
        {
            var g = TestGame.AtFirstMainPhase();
            g.State.Format.DamageWearsOff = true;
            var c = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 3);
            g.PassToStep(Step.Main1, g.Other);
            Assert.AreEqual(0, c.Damage);
        }

        [Test]
        public void SecondPlayerCompensation_ExperimentSwitches()
        {
            var engine = new GameEngine(PrototypeCards.CreateDatabase());
            var format = FormatConfig.Standard();
            format.SecondPlayerExtraCards = 1;
            format.SecondPlayerFirstTurnBonusMana = 1;
            format.SecondPlayerStartingGold = 0;
            var deck = PrototypeCards.GooberMobDeck();
            var s = engine.CreateGame(format, new[] { new PlayerSetup { Deck = deck }, new PlayerSetup { Deck = deck } }, 3);
            s.AutoPass = false;
            engine.Apply(s, PlayerAction.Keep(s.Pending.Player));
            engine.Apply(s, PlayerAction.Keep(s.Pending.Player));

            var first = s.ActivePlayer;
            var second = s.Players.First(p => p.Id != first);
            Assert.AreEqual(8, second.Hand.Count, "+1 card before the first turn");
            Assert.AreEqual(0, second.Gold);

            while (s.ActivePlayer == first)
            {
                var who = engine.WaitingOn(s).Value;
                var legal = engine.GetLegalActions(s, who);
                engine.Apply(s, legal.FirstOrDefault(a => a.Kind == ActionKind.PassPriority || a.Kind == ActionKind.FinishAttacks
                                                          || a.Kind == ActionKind.FinishBlocks) ?? legal[0]);
            }
            Assert.AreEqual(1, second.MaxMana);
            Assert.AreEqual(2, second.Mana, "first turn: 1 mana + 1 bonus");
        }

        [Test]
        public void GoldBankedEvent_ReportsWhatTheCapWasted()
        {
            var g = TestGame.AtFirstMainPhase();
            var first = g.Active;
            g.P(first).Gold = 4;
            g.SetMana(first, 3);
            g.PassToStep(Step.Main1, g.Other);
            var banked = g.Events.OfType<GoldBankedEvent>().First(e => e.Player == first);
            Assert.AreEqual(3, banked.UnspentMana);
            Assert.AreEqual(1, banked.Banked);
        }

        [Test]
        public void ExperimentReport_RendersEverySection()
        {
            var sections = Experiments.Build(2);
            Experiments.Run(sections, PrototypeCards.CreateDatabase());
            var md = Experiments.ToMarkdown(sections, 2, System.TimeSpan.FromSeconds(1));
            foreach (var s in sections) StringAssert.Contains(s.Title, md);
            Assert.AreEqual(sections.Sum(s => s.Configs.Count), md.Split('\n').Count(l => l.StartsWith("| ") && !l.StartsWith("| Matchup")));
        }
    }
}
