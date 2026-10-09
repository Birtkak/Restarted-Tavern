using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// DEVELOPMENT §4: random-play soak tests and determinism. Bots pick random legal actions;
    /// after every action the engine's invariants must hold, and every game must end.
    /// </summary>
    public class SimulationTests
    {
        private const int MaxActionsPerGame = 20000;

        private static List<string> RandomDeck(CardDatabase db, DeterministicRng rng)
        {
            var pool = db.All.Where(c => !c.IsToken).Select(c => c.Id).OrderBy(id => id).ToList();
            rng.Shuffle(pool);
            var deck = new List<string>();
            foreach (var id in pool.Take(15)) deck.AddRange(Enumerable.Repeat(id, 4));
            return deck; // 15 × 4 = 60, a legal Standard deck
        }

        /// <summary>Plays one full game with random bots. Returns the final fingerprint.</summary>
        private static string PlayRandomGame(ulong seed, out int actions) => PlayRandomGame(seed, out actions, out _);

        private static string PlayRandomGame(ulong seed, out int actions, out GameState final)
        {
            var db = PrototypeCards.CreateDatabase();
            var engine = new GameEngine(db);
            var botRng = new DeterministicRng(seed * 7919 + 1);
            var setups = new[]
            {
                new PlayerSetup { Deck = RandomDeck(db, botRng) },
                new PlayerSetup { Deck = RandomDeck(db, botRng) },
            };
            var state = engine.CreateGame(FormatConfig.Standard(), setups, seed);

            actions = 0;
            while (!state.IsGameOver)
            {
                var who = engine.WaitingOn(state);
                Assert.IsTrue(who.HasValue, "game is stuck: nobody can act");
                var legal = engine.GetLegalActions(state, who.Value);
                Assert.IsNotEmpty(legal, "player " + who + " must have at least one action");

                // Lean towards doing things over passing so games actually develop.
                var choice = legal.Count > 1 && botRng.Next(4) != 0
                    ? legal[1 + botRng.Next(legal.Count - 1)]
                    : legal[botRng.Next(legal.Count)];
                engine.Apply(state, choice);
                CheckInvariants(state, db);

                Assert.Less(++actions, MaxActionsPerGame, "game did not end");
            }
            final = state;
            return state.Fingerprint();
        }

        private static void CheckInvariants(GameState s, CardDatabase db)
        {
            foreach (var p in s.Players)
            {
                Assert.GreaterOrEqual(p.Mana, 0, "negative mana");
                Assert.LessOrEqual(p.MaxMana, s.Format.ManaCap);
                Assert.That(p.Gold, Is.InRange(0, s.Format.GoldCap), "Gold out of range");

                // Card conservation: every non-token card the player owns is somewhere.
                int owned = s.Players.Sum(q => q.Battlefield.Count(c => c.Owner == p.Id && !c.IsToken))
                            + p.Deck.Count + p.Hand.Count + p.Graveyard.Count + p.Exile.Count
                            + s.Chain.Count(i => i.Card != null && i.Card.Owner == p.Id);
                Assert.AreEqual(s.Format.DeckSize, owned, "cards were created or lost");

                foreach (var c in p.Battlefield)
                {
                    Assert.AreEqual(Zone.Battlefield, c.Zone);
                    Assert.AreEqual(p.Id, c.Controller);
                }
            }

            // When someone holds priority, state-based actions have been applied.
            if (s.PriorityPlayer.HasValue && !s.IsGameOver)
            {
                foreach (var c in s.AllPermanents())
                    if (db.Get(c.DefinitionId).IsCreature)
                        Assert.Greater(CharacteristicsCalculator.Compute(s, db, c).RemainingHealth, 0, "dead creature on battlefield");
                Assert.IsTrue(s.Players.All(p => p.HasLost || p.Life > 0));
            }
        }

        [Test]
        public void RandomGames_AlwaysEnd_AndKeepInvariants()
        {
            int total = 0, turns = 0, byLife = 0, byDeck = 0;
            for (ulong seed = 1; seed <= 100; seed++)
            {
                PlayRandomGame(seed, out int actions, out var final);
                total += actions;
                turns += final.TurnNumber;
                if (final.Players.Any(p => p.HasLost && p.Life <= 0)) byLife++;
                else byDeck++;
            }
            TestContext.WriteLine("100 random games, " + total + " actions, avg " + turns / 100 + " turns, "
                                  + byLife + " ended by life, " + byDeck + " by empty deck");
        }

        [Test]
        public void SameSeedAndActions_GiveTheSameGame()
        {
            for (ulong seed = 1; seed <= 10; seed++)
            {
                var a = PlayRandomGame(seed, out _);
                var b = PlayRandomGame(seed, out _);
                Assert.AreEqual(a, b, "seed " + seed);
            }
            Assert.AreNotEqual(PlayRandomGame(1, out _), PlayRandomGame(2, out _));
        }

        [Test]
        public void Clone_IsIndependent()
        {
            var db = PrototypeCards.CreateDatabase();
            var engine = new GameEngine(db);
            var rng = new DeterministicRng(5);
            var state = engine.CreateGame(FormatConfig.Standard(),
                new[] { new PlayerSetup { Deck = RandomDeck(db, rng) }, new PlayerSetup { Deck = RandomDeck(db, rng) } }, 5);
            var before = state.Fingerprint();
            var copy = state.Clone();

            int guard = 0;
            while (!copy.IsGameOver)
            {
                var who = engine.WaitingOn(copy).Value;
                var legal = engine.GetLegalActions(copy, who);
                engine.Apply(copy, legal[legal.Count - 1]);
                Assert.Less(++guard, MaxActionsPerGame);
            }
            Assert.AreEqual(before, state.Fingerprint(), "playing the clone must not touch the original");
        }
    }
}
