using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules;
using RestartedTavern.Rules.AI;

namespace RestartedTavern.Client.Logic.Tests
{
    public class ClientLogicTests
    {
        /// <summary>Every action the picker can end on from this source, by trying every answer to every prompt.</summary>
        private static HashSet<PlayerAction> Reachable(List<PlayerAction> legal, ObjectId source)
        {
            var found = new HashSet<PlayerAction>();
            void Walk(List<int> path)
            {
                var picker = new ActionPicker(legal);
                Assert.IsTrue(picker.Begin(source));
                foreach (int i in path) picker.Choose(picker.Prompt.Options[i]);
                if (picker.Ready != null)
                {
                    Assert.IsTrue(found.Add(picker.Ready), "Two answer paths gave the same action: " + picker.Ready);
                    return;
                }
                Assert.IsNotNull(picker.Prompt);
                Assert.Greater(picker.Prompt.Options.Count, 1, "A prompt must offer a real choice.");
                for (int i = 0; i < picker.Prompt.Options.Count; i++) Walk(new List<int>(path) { i });
            }
            Walk(new List<int>());
            return found;
        }

        /// <summary>
        /// Bots play several games; at every decision the picker must reach exactly the legal actions of
        /// each source, and nothing else.
        /// </summary>
        [Test]
        public void Picker_ReachesExactlyTheLegalActions_ThroughBotGames()
        {
            int checkedStates = 0, checkedSources = 0;
            for (int deck = 0; deck < 6; deck++)
            {
                var session = new MatchSession(MatchSetup.Duel(deck, (deck + 1) % 6, SeatKind.Bot, SeatKind.Bot, seed: (ulong)(deck + 7)));
                for (int step = 0; step < 400 && session.BotToAct; step++)
                {
                    var p = session.WaitingOn.Value;
                    var legal = session.Engine.GetLegalActions(session.State, p);
                    var picker = new ActionPicker(legal);
                    foreach (var source in picker.Sources)
                    {
                        var expected = new HashSet<PlayerAction>(legal.Where(a => a.Card == source && picker.CanUse(source)
                            && a.Kind != ActionKind.ChooseTarget && a.Kind != ActionKind.ChooseOption && a.Kind != ActionKind.AssignCombatDamage));
                        var reached = Reachable(legal, source);
                        CollectionAssert.AreEquivalent(expected, reached, "Source " + source + " at step " + step);
                        checkedSources++;
                    }
                    int sourced = legal.Count - picker.SourcelessActions.Count();
                    Assert.AreEqual(sourced, picker.Sources.Sum(s => Reachable(legal, s).Count));
                    checkedStates++;
                    session.StepBot();
                }
            }
            Assert.Greater(checkedSources, 100, "The games should have exercised the picker (" + checkedStates + " states).");
        }

        [Test]
        public void Picker_BeginOnUnusableObject_ReturnsFalse()
        {
            var picker = new ActionPicker(new[] { PlayerAction.Pass(new PlayerId(1)) });
            Assert.IsFalse(picker.Begin(new ObjectId(999)));
            Assert.IsFalse(picker.IsPicking);
            Assert.AreEqual(1, picker.SourcelessActions.Count());
        }

        [Test]
        public void Picker_TargetsThenInvest()
        {
            var p = new PlayerId(1);
            var card = new ObjectId(10);
            var a = Target.ForObject(new ObjectId(20));
            var b = Target.ForObject(new ObjectId(21));
            var legal = new List<PlayerAction>
            {
                PlayerAction.Play(p, card, a), PlayerAction.Play(p, card, a, invest: true),
                PlayerAction.Play(p, card, b), PlayerAction.Play(p, card, b, invest: true),
            };
            var picker = new ActionPicker(legal);
            Assert.IsTrue(picker.Begin(card));
            Assert.AreEqual(ChoiceDimension.Target, picker.Prompt.Dimension);
            CollectionAssert.AreEquivalent(new[] { a, b }, picker.Prompt.Targets);
            Assert.IsTrue(picker.ChooseTarget(b));
            Assert.AreEqual(ChoiceDimension.Invest, picker.Prompt.Dimension);
            picker.Choose(picker.Prompt.Options.First(o => o.Invest));
            Assert.AreEqual(PlayerAction.Play(p, card, b, invest: true), picker.Ready);
        }

        [Test]
        public void Picker_OptionalTargetSlots_OfferStop()
        {
            var p = new PlayerId(1);
            var card = new ObjectId(10);
            var a = Target.ForObject(new ObjectId(20));
            var b = Target.ForObject(new ObjectId(21));
            var legal = new List<PlayerAction>
            {
                PlayerAction.Play(p, card, new Target[0]),
                PlayerAction.Play(p, card, new[] { a }),
                PlayerAction.Play(p, card, new[] { a, b }),
            };
            var picker = new ActionPicker(legal);
            picker.Begin(card);
            Assert.IsTrue(picker.Prompt.Options.Any(o => o.StopTargeting));
            picker.ChooseTarget(a);
            Assert.AreEqual(1, picker.Prompt.Slot);
            picker.Choose(picker.Prompt.Options.First(o => o.StopTargeting));
            Assert.AreEqual(PlayerAction.Play(p, card, new[] { a }), picker.Ready);
        }

        /// <summary>
        /// Two "humans" (driven by the bot) play hot-seat; every attack and block goes through the LoR-style stage:
        /// everything staged is declared, in one commit.
        /// </summary>
        [Test]
        public void CombatStage_StagedAttacksAndBlocks_AreDeclared()
        {
            int attacks = 0, blocks = 0;
            for (int game = 0; game < 4; game++)
            {
                var session = new MatchSession(MatchSetup.Duel(game, 2 + game, SeatKind.Human, SeatKind.Human, seed: (ulong)(game + 1)));
                var bot = new GreedyBot(session.Engine);
                for (int step = 0; step < 3000 && !session.State.IsGameOver; step++)
                {
                    session.AcknowledgeHandoff();
                    Assert.IsTrue(session.HumanToAct, "Hot-seat: a human is always to act");
                    List<GameEvent> events;
                    var attack = CombatStage.ForAttack(session);
                    var block = CombatStage.ForBlock(session);
                    if (attack != null && attack.Candidates().Count > 0)
                    {
                        foreach (var c in attack.Candidates().ToList()) attack.StageAttacker(c);
                        Assert.Greater(attack.Staged.Count, 0);
                        events = session.CommitCombat(attack);
                        while (session.CombatInProgress)
                        {
                            session.AcknowledgeHandoff();
                            Assert.AreEqual(session.WaitingOn, session.Viewer);
                            events.AddRange(session.Submit(bot.Choose(session.State, session.Viewer)));
                        }
                        // An opponent's response in the attack window can remove a staged creature; every other one is declared.
                        var declared = events.OfType<AttackerDeclaredEvent>().Select(e => e.Attacker).ToList();
                        var staged = attack.Staged.Select(c => c.Creature).ToList();
                        CollectionAssert.IsSubsetOf(declared, staged);
                        CollectionAssert.IsSubsetOf(staged.Where(c => session.State.FindOnBattlefield(c) != null), declared);
                        attacks++;
                    }
                    else if (block != null && session.State.Combat.Attacks.Count > 0)
                    {
                        var attacker = session.State.Combat.Attacks[0].Attacker;
                        var blocker = block.Candidates().FirstOrDefault(b => block.BlockableBy(b).Contains(attacker));
                        if (!blocker.IsNone) Assert.IsTrue(block.StageBlocker(blocker, attacker));
                        events = session.CommitCombat(block);
                        Assert.AreEqual(block.Staged.Count, events.OfType<BlockerDeclaredEvent>().Count());
                        if (block.Staged.Count > 0) blocks++;
                    }
                    else events = session.Submit(bot.Choose(session.State, session.Viewer));
                }
            }
            Assert.Greater(attacks, 5);
            Assert.Greater(blocks, 2);
        }

        [Test]
        public void Session_BotsFinishAGame()
        {
            var session = new MatchSession(MatchSetup.Duel(0, 1, SeatKind.Bot, SeatKind.Bot, seed: 3));
            int steps = 0;
            while (session.BotToAct && steps++ < 5000) session.StepBot();
            Assert.IsTrue(session.State.IsGameOver, "Bot game didn't finish in 5000 actions.");
            Assert.IsTrue(session.Snapshot().IsGameOver);
        }

        [Test]
        public void Session_HotSeat_HandsOffAndUndoes()
        {
            var session = new MatchSession(MatchSetup.Duel(0, 1, SeatKind.Human, SeatKind.Human, seed: 5));
            Assert.IsFalse(session.HandoffPending);
            Assert.IsTrue(session.HumanToAct);
            var first = session.Viewer;
            string before = session.State.Fingerprint();

            // Keep the opening hand: the mulligan decision moves to the other player.
            session.Submit(session.LegalForViewer().First(a => a.Kind == ActionKind.Keep));
            Assert.AreNotEqual(first, session.Viewer);
            Assert.IsTrue(session.HandoffPending);
            Assert.IsFalse(session.HumanToAct, "The table stays covered until the handoff is acknowledged.");
            Assert.IsEmpty(session.LegalForViewer());
            session.AcknowledgeHandoff();
            Assert.IsTrue(session.HumanToAct);

            session.Undo();
            Assert.AreEqual(before, session.State.Fingerprint());
            Assert.AreEqual(first, session.Viewer);
        }

        [Test]
        public void Snapshot_HidesOpponentHand()
        {
            var session = new MatchSession(MatchSetup.Duel(2, 3, SeatKind.Human, SeatKind.Bot, seed: 11));
            var snap = session.Snapshot();
            var me = snap.Player(session.Viewer);
            var them = snap.Players.First(p => p.Id != session.Viewer);
            Assert.AreEqual(7, me.Hand.Count);
            Assert.IsTrue(me.Hand.All(c => !c.IsHidden && c.Name != null));
            Assert.AreEqual(7, them.Hand.Count);
            Assert.IsTrue(them.Hand.All(c => c.IsHidden && c.Name == null));
            Assert.IsNotNull(me.TavernDweller?.Name);
            Assert.AreEqual(3, me.GoldCap);
        }

        [Test]
        public void Presentation_HidesOpponentDraws()
        {
            var session = new MatchSession(MatchSetup.Duel(0, 1, SeatKind.Bot, SeatKind.Bot, seed: 2));
            var viewer = session.State.Players[0].Id;
            var queue = new PresentationQueue();
            int hiddenDraws = 0, shownDraws = 0;
            for (int i = 0; i < 300 && session.BotToAct; i++)
            {
                queue.Enqueue(session.StepBot(), session.State, viewer);
                while (queue.TryDequeue(out var beat))
                    if (beat.Event is CardDrawnEvent d)
                    {
                        Assert.AreEqual(d.Player != viewer, beat.Hidden);
                        if (beat.Hidden) hiddenDraws++; else shownDraws++;
                    }
            }
            Assert.Greater(hiddenDraws, 0);
            Assert.Greater(shownDraws, 0);
            Assert.IsTrue(queue.IsIdle);
        }
    }
}
