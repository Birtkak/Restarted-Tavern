using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>The prototype decks and the text the debug table shows.</summary>
    public class GameTextTests
    {
        [Test]
        public void PrototypeDecks_AreLegalStandardDecks()
        {
            var db = CardPool.CreateDatabase();
            Assert.DoesNotThrow(() => DeckValidator.Validate(db, FormatConfig.Standard(), CardPool.GooberMobDeck(), CardPool.GooberMobTavernDweller));
            Assert.DoesNotThrow(() => DeckValidator.Validate(db, FormatConfig.Standard(), CardPool.JungleStampedeDeck(), CardPool.JungleStampedeTavernDweller));
        }

        [Test]
        public void CardDetails_ShowRulesTextDamageAndStatus()
        {
            var g = TestGame.AtFirstMainPhase();
            var bomber = g.AddToBattlefield(g.Active, "barrel_bomber", damage: 1);
            bomber.SummoningSick = true;
            var text = new GameText(g.Engine.Cards);
            string details = text.Details(g.State, bomber);
            StringAssert.StartsWith("Barrel Bomber\nCost 4 · Creature — Goober · Goobers · Common\nPower/Health 3/3\n", details);
            StringAssert.Contains("Damage 1: 2 Health left", details);
            StringAssert.Contains(g.Engine.Cards.Get("barrel_bomber").Text, details);
            StringAssert.Contains("summoning sick", details);
            Assert.AreEqual("Main phase 1", GameText.StepName(Step.Main1));
        }

        [Test]
        public void EveryCard_HasDetails_InEveryZone()
        {
            var db = CardPool.CreateDatabase();
            var engine = new GameEngine(db);
            var state = engine.CreateGame(FormatConfig.Standard(), new[]
            {
                new PlayerSetup { Deck = CardPool.SparkwrenchScrappersDeck(), TavernDwellerId = CardPool.SparkwrenchScrappersTavernDweller },
                new PlayerSetup { Deck = CardPool.VespersLedgerDeck(), TavernDwellerId = CardPool.VespersLedgerTavernDweller },
            }, 3);
            var text = new GameText(db);
            var bot = new AI.GreedyBot(engine);
            for (int i = 0; i < 3000 && !state.IsGameOver; i++)
            {
                foreach (var p in state.Players)
                    foreach (var c in p.Battlefield.Concat(p.Hand).Concat(p.Graveyard).Concat(p.TavernDwellerZone))
                        Assert.IsNotEmpty(text.Details(state, c));
                var who = engine.WaitingOn(state).Value;
                engine.Apply(state, bot.Choose(state, who));
            }
        }

        [Test]
        public void EveryActionAndEvent_CanBeDescribed_InRandomGames()
        {
            var db = CardPool.CreateDatabase();
            var engine = new GameEngine(db);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var text = new GameText(db);
                var events = new List<GameEvent>();
                var state = engine.CreateGame(FormatConfig.Standard(), new[]
                {
                    new PlayerSetup { Deck = CardPool.GooberMobDeck(), TavernDwellerId = CardPool.GooberMobTavernDweller },
                    new PlayerSetup { Deck = CardPool.JungleStampedeDeck(), TavernDwellerId = CardPool.JungleStampedeTavernDweller },
                }, seed, events);
                text.Remember(state, events);
                var rng = new DeterministicRng(seed);

                int guard = 0;
                while (!state.IsGameOver)
                {
                    var who = engine.WaitingOn(state).Value;
                    var legal = engine.GetLegalActions(state, who);
                    foreach (var a in legal) Assert.IsNotEmpty(text.Describe(state, a));
                    foreach (var p in state.Players)
                        foreach (var c in p.Battlefield.Concat(p.Hand))
                            Assert.IsNotEmpty(text.Describe(state, c));

                    var choice = legal.Count > 1 && rng.Next(4) != 0 ? legal[1 + rng.Next(legal.Count - 1)] : legal[0];
                    var produced = engine.Apply(state, choice);
                    text.Remember(state, produced);
                    foreach (var e in produced)
                    {
                        var line = text.Describe(state, e);
                        if (line != null) StringAssert.DoesNotContain("?", line.Replace("Can't", ""), "unknown name in: " + line);
                    }
                    Assert.Less(++guard, 20000);
                }
            }
        }

        [Test]
        public void HiddenDraws_AreNotNamedForOtherViewers()
        {
            var db = CardPool.CreateDatabase();
            var text = new GameText(db);
            var draw = new CardDrawnEvent { Player = new PlayerId(2), Card = new ObjectId(5), DefinitionId = "spark_snot" };
            var g = TestGame.AtFirstMainPhase();
            Assert.AreEqual("P2 draws a card", text.Describe(g.State, draw, new PlayerId(1)));
            Assert.AreEqual("P2 draws Spark Snot", text.Describe(g.State, draw, new PlayerId(2)));
        }
    }
}
