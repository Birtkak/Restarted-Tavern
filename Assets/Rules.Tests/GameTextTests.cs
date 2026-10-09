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
            var db = PrototypeCards.CreateDatabase();
            Assert.DoesNotThrow(() => DeckValidator.Validate(db, FormatConfig.Standard(), PrototypeCards.GooberMobDeck(), PrototypeCards.GooberMobTavernDweller));
            Assert.DoesNotThrow(() => DeckValidator.Validate(db, FormatConfig.Standard(), PrototypeCards.JungleStampedeDeck(), PrototypeCards.JungleStampedeTavernDweller));
        }

        [Test]
        public void EveryActionAndEvent_CanBeDescribed_InRandomGames()
        {
            var db = PrototypeCards.CreateDatabase();
            var engine = new GameEngine(db);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var text = new GameText(db);
                var events = new List<GameEvent>();
                var state = engine.CreateGame(FormatConfig.Standard(), new[]
                {
                    new PlayerSetup { Deck = PrototypeCards.GooberMobDeck(), TavernDwellerId = PrototypeCards.GooberMobTavernDweller },
                    new PlayerSetup { Deck = PrototypeCards.JungleStampedeDeck(), TavernDwellerId = PrototypeCards.JungleStampedeTavernDweller },
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
            var db = PrototypeCards.CreateDatabase();
            var text = new GameText(db);
            var draw = new CardDrawnEvent { Player = new PlayerId(2), Card = new ObjectId(5), DefinitionId = "spark_snot" };
            var g = TestGame.AtFirstMainPhase();
            Assert.AreEqual("P2 draws a card", text.Describe(g.State, draw, new PlayerId(1)));
            Assert.AreEqual("P2 draws Spark Snot", text.Describe(g.State, draw, new PlayerId(2)));
        }
    }
}
