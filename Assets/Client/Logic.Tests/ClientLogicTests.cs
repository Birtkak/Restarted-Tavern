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

        /// <summary>
        /// Hot-seat bot games: at every human decision, every legal action can be reached from the table's controls
        /// (the context button, the choice panel, a card through the picker, a clicked target, the combat stage),
        /// and the context button only submits legal actions.
        /// </summary>
        [Test]
        public void TableControls_CoverEveryLegalAction()
        {
            int states = 0, endRounds = 0;
            for (int game = 0; game < 6; game++)
            {
                var session = new MatchSession(MatchSetup.Duel(game, (game + 3) % 6, SeatKind.Human, SeatKind.Human, seed: (ulong)(game + 20)));
                var bot = new GreedyBot(session.Engine);
                for (int step = 0; step < 2500 && !session.State.IsGameOver; step++)
                {
                    if (session.HandoffPending)
                    {
                        Assert.AreEqual(ButtonMode.Handoff, TableControls.Main(session).Mode);
                        session.AcknowledgeHandoff();
                    }
                    var legal = session.LegalForViewer();
                    var button = TableControls.Main(session);
                    if (button.Action != null) CollectionAssert.Contains(legal, button.Action, "Button " + button.Mode);
                    Assert.AreEqual(button.Action != null, button.Enabled, "Enabled iff it submits something (" + button.Mode + ")");
                    var choices = TableControls.Choices(session).Select(c => c.Action).ToList();
                    Assert.IsTrue(TableControls.Choices(session).All(c => !string.IsNullOrEmpty(c.Label)));
                    var picker = new ActionPicker(legal);
                    var attack = CombatStage.ForAttack(session);
                    foreach (var a in legal)
                    {
                        bool covered = a.Equals(button.Action) || choices.Contains(a)
                            || picker.CanUse(a.Card) && a.Kind != ActionKind.ChooseOption && a.Kind != ActionKind.ChooseTarget && a.Kind != ActionKind.AssignCombatDamage
                            || a.Kind == ActionKind.ChooseTarget && a.Targets.Length > 0
                            || a.Kind == ActionKind.GoToCombat && attack != null && attack.Candidates().Count > 0
                            || a.Kind == ActionKind.PassPriority && button.Mode == ButtonMode.ChooseOnTable;
                        Assert.IsTrue(covered, "No control for " + a + " (" + session.State.Pending?.Kind + ", button " + button.Mode + ")");
                    }
                    foreach (var source in picker.Sources)
                    {
                        picker.Begin(source);
                        if (picker.Prompt != null)
                            Assert.IsTrue(picker.Prompt.Options.All(o => !string.IsNullOrEmpty(TableControls.Describe(session, picker, o))));
                    }
                    states++;
                    var chosen = bot.Choose(session.State, session.Viewer);
                    int round = session.State.RoundNumber;
                    var stepBefore = session.State.Step;
                    session.Submit(chosen);
                    if (chosen.Equals(button.Action) && button.Mode == ButtonMode.Pass)
                        Assert.AreEqual(round, session.State.RoundNumber, "Pass must not end the round");
                    if (chosen.Equals(button.Action) && button.Mode == ButtonMode.EndRound)
                    {
                        endRounds++;
                        Assert.IsTrue(session.State.RoundNumber > round || session.State.Step != stepBefore || session.State.IsGameOver,
                            "End round must leave the action phase");
                    }
                }
            }
            Assert.Greater(states, 500);
            Assert.Greater(endRounds, 5);
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
        public void Setup_EveryLegalTavernDweller_StartsAGame()
        {
            var db = RestartedTavern.Rules.Cards.CardPool.CreateDatabase();
            var decks = RestartedTavern.Rules.Cards.CardPool.PrototypeDecks();
            int games = 0;
            for (int d = 0; d < decks.Count; d++)
            {
                var dwellers = MatchSetup.TavernDwellersFor(decks[d], db);
                Assert.AreEqual(decks[d].TavernDweller, dwellers[0], "The deck's own Tavern Dweller comes first.");
                foreach (var td in dwellers)
                {
                    var session = new MatchSession(MatchSetup.Duel(d, td, (d + 1) % decks.Count, null, SeatKind.Human, SeatKind.Bot, 1));
                    Assert.AreEqual(td, session.Snapshot().Players[0].TavernDweller.DefinitionId);
                    games++;
                }
            }
            Assert.GreaterOrEqual(games, decks.Count);
        }

        [Test]
        public void Session_History_ReplaysToTheSameState()
        {
            var setup = MatchSetup.Duel(1, 3, SeatKind.Human, SeatKind.Bot, seed: 21);
            var session = new MatchSession(setup);
            for (int i = 0; i < 120 && session.WaitingOn != null; i++)
            {
                session.AutoStep();
                if (i == 40 && session.HumanToAct)
                {
                    session.Submit(session.LegalForViewer()[0]);
                    session.Undo();
                }
            }
            var replay = new MatchSession(setup);
            foreach (var a in session.History) replay.Engine.Apply(replay.State, a);
            Assert.AreEqual(session.State.Fingerprint(), replay.State.Fingerprint());
        }

        [Test]
        public void Session_AutoStep_PlaysForHumansToo()
        {
            var session = new MatchSession(MatchSetup.Duel(2, 4, SeatKind.Human, SeatKind.Human, seed: 9));
            int steps = 0;
            while (session.WaitingOn != null && steps++ < 5000) session.AutoStep();
            Assert.IsTrue(session.State.IsGameOver, "AutoStep game didn't finish in 5000 actions.");
            Assert.IsFalse(session.CanUndo, "AutoStep actions aren't undoable.");
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
            Assert.AreEqual(1, snap.Players.Count(p => p.HasAttackToken), "Standard rules: exactly one attack token");
            Assert.IsTrue(snap.Player(snap.RoundLeader).HasAttackToken);
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
