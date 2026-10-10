using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules;

namespace RestartedTavern.Client.Logic.Tests
{
    public class TutorialTests
    {
        /// <summary>Plays the tutorial like a player who does what each step asks; returns the tutorial at release.</summary>
        private static Tutorial PlayThrough(ulong seed)
        {
            var session = new MatchSession(Tutorial.Setup(seed));
            var tutorial = new Tutorial(session);
            for (int i = 0; i < 500 && !tutorial.Released; i++)
                Assert.IsTrue(tutorial.AutoPlayStep(), "Stuck at step " + tutorial.Index + ": " + session.State.Fingerprint());
            return tutorial;
        }

        [Test]
        public void Tutorial_RunsToRelease_InRoundThree()
        {
            foreach (ulong seed in new ulong[] { 1, 7, 12345 })
            {
                var t = PlayThrough(seed);
                var s = t.Session.State;
                Assert.IsTrue(t.Released, "Stuck at step " + t.Index + " (" + t.Current?.Title + "): " + s.Fingerprint());
                Assert.AreEqual(3, s.RoundNumber);
                Assert.IsFalse(s.IsGameOver);
                Assert.IsTrue(s.Players[1].Graveyard.Any(c => c.DefinitionId == "vine_spider"), "Spark Snot finished the Spider");
                Assert.IsTrue(s.Players[0].Graveyard.Any(c => c.DefinitionId == "brawling_runt"), "The Runt traded with the Spider");
                Assert.Less(s.Players[1].Life, 28, "Rascal hit, Skabba pinged, and the round 3 attack. " + s.Fingerprint() + " | "
                    + string.Join("; ", t.Session.History.Select(a => t.Session.Text.Describe(s, a))));
                Assert.IsNull(t.Session.HumanFilter);
                Assert.IsNull(t.Session.BotOverride);
            }
        }

        [Test]
        public void Tutorial_GameContinuesAsNormalBotGame()
        {
            var t = PlayThrough(3);
            var session = t.Session;
            for (int i = 0; i < 3000 && !session.State.IsGameOver; i++)
                if (session.BotToAct) session.StepBot();
                else session.AutoStep();
            Assert.IsTrue(session.State.IsGameOver);
        }

        [Test]
        public void Tutorial_OnlyTheLessonIsAllowed()
        {
            var session = new MatchSession(Tutorial.Setup(1));
            var t = new Tutorial(session);
            Assert.IsTrue(t.Frozen, "Starts on the welcome box");
            Assert.IsEmpty(session.LegalForViewer());
            while (t.Frozen) t.Next();
            // Keep first.
            for (int i = 0; i < 5 && t.AutoHumanAction() is PlayerAction a; i++) { session.Submit(a); while (session.BotToAct) session.StepBot(); }
            var legal = session.LegalForViewer();
            Assert.AreEqual(1, legal.Count, string.Join(", ", legal));
            Assert.AreEqual("goober_rascal", session.State.FindObject(legal[0].Card).DefinitionId);
            Assert.AreEqual(session.State.Players[0].Id, session.State.Players[session.State.StartingPlayerIndex].Id, "You go first");
        }
    }
}
