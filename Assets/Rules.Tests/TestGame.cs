using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// Builds 1v1 games in a known situation and drives them by hand. Auto-pass is off, so
    /// every priority pass is explicit and tests control exactly when things resolve.
    /// <see cref="Active"/> is "me": the round leader, who holds the attack token. When my action has resolved, the
    /// other player has the action; if the test then acts as me again (<see cref="Do"/>, <see cref="Legal"/>), the other
    /// player first passes the action back, and <see cref="Pass"/> ("I pass, so they may answer") does nothing. Tests of the action order
    /// itself (<see cref="RoundsTests"/>) use <see cref="PassPriority"/> and State.ActivePlayer.
    /// </summary>
    internal sealed class TestGame
    {
        public readonly GameEngine Engine;
        public readonly GameState State;
        public readonly List<GameEvent> Events = new List<GameEvent>();

        /// <summary>The round leader: holds the attack token, and acts first in the round.</summary>
        public PlayerId Active => State.Players[State.RoundLeaderSeat].Id;
        public PlayerId Other => State.Players.First(p => p.Id != Active).Id;

        private TestGame(GameEngine engine, GameState state)
        {
            Engine = engine;
            State = state;
        }

        /// <summary>
        /// A game at the round leader's first action in round 1, both players kept 7 (and drew 1).
        /// Decks are 20 Hired Sellswords unless given. Extra test-only cards can be added.
        /// </summary>
        public static TestGame AtFirstMainPhase(ulong seed = 1, IEnumerable<CardDefinition> extraCards = null,
            string deckCard = "hired_sellsword", int deckSize = 20, FormatConfig format = null)
        {
            var db = new CardDatabase(CardPool.All().Concat(extraCards ?? Enumerable.Empty<CardDefinition>()));
            var engine = new GameEngine(db);
            format = format ?? FormatConfig.Standard();
            format.EnforceDeckRules = false;
            var deck = Enumerable.Repeat(deckCard, deckSize).ToList();
            var events = new List<GameEvent>();
            var state = engine.CreateGame(format, new[] { new PlayerSetup { Deck = deck }, new PlayerSetup { Deck = new List<string>(deck) } }, seed, events);
            state.AutoPass = false;

            var game = new TestGame(engine, state);
            game.Events.AddRange(events);
            game.Do(PlayerAction.Keep(state.Pending.Player));
            game.Do(PlayerAction.Keep(state.Pending.Player));
            Assert.AreEqual(Step.Main1, state.Step);
            Assert.AreEqual(state.ActivePlayer, state.PriorityPlayer);
            return game;
        }

        public PlayerState P(PlayerId id) => State.GetPlayer(id);

        public List<GameEvent> Do(PlayerAction action)
        {
            if (action.Player == Active && action.Kind != ActionKind.PassPriority && OtherHasAFreshAction) PassPriority();
            var events = Engine.Apply(State, action);
            Events.AddRange(events);
            return events;
        }

        /// <summary>What <paramref name="player"/> may do now. Asking for me right after my action hands the action back first.</summary>
        public List<PlayerAction> Legal(PlayerId player)
        {
            if (player == Active) HandTheActionBack();
            return Engine.GetLegalActions(State, player);
        }

        /// <summary>Put a new card straight into a hand (test setup, bypasses the rules).</summary>
        public CardInstance AddToHand(PlayerId player, string definitionId)
        {
            var c = NewCard(player, definitionId, Zone.Hand);
            P(player).Hand.Add(c);
            return c;
        }

        /// <summary>Put a new permanent straight onto the battlefield (test setup).</summary>
        public CardInstance AddToBattlefield(PlayerId player, string definitionId, int damage = 0)
        {
            var c = NewCard(player, definitionId, Zone.Battlefield);
            c.Damage = damage;
            P(player).Battlefield.Add(c);
            return c;
        }

        private CardInstance NewCard(PlayerId owner, string definitionId, Zone zone) => new CardInstance
        {
            Id = new ObjectId(State.NextObjectId++),
            DefinitionId = definitionId,
            Owner = owner,
            Controller = owner,
            Zone = zone,
            Timestamp = State.NextTimestamp++,
        };

        /// <summary>Give a player a Tavern Dweller (test setup, bypasses deck validation).</summary>
        public CardInstance SetTavernDweller(PlayerId player, string tavernDwellerId)
        {
            var c = NewCard(player, tavernDwellerId, Zone.TavernDweller);
            P(player).TavernDwellerZone.Clear();
            P(player).TavernDwellerZone.Add(c);
            P(player).TavernDwellerId = tavernDwellerId;
            return c;
        }

        /// <summary>The legal activations of the source's ability <paramref name="index"/>.</summary>
        public List<PlayerAction> Activations(PlayerId player, CardInstance source, int index = 0) =>
            Legal(player).Where(a => a.Kind == ActionKind.ActivateAbility && a.Card == source.Id && a.AbilityIndex == index).ToList();

        public void SetMana(PlayerId player, int mana)
        {
            P(player).Mana = mana;
            P(player).MaxMana = Math.Max(P(player).MaxMana, mana);
        }

        /// <summary>The priority holder passes.</summary>
        public void PassPriority() => Do(PlayerAction.Pass(State.PriorityPlayer.Value));

        /// <summary>
        /// My action has just resolved and the other player has the action (they haven't passed or acted yet).
        /// </summary>
        private bool OtherHasAFreshAction =>
            State.Step == Step.Main1 && State.Chain.Count == 0 && State.Pending == null && State.PassesInRow == 0
            && State.ActivePlayer != Active && State.PriorityPlayer == State.ActivePlayer;

        /// <summary>
        /// The priority holder passes. When my action has just resolved, the other player already has priority, so this
        /// does nothing ("I pass, so they may answer").
        /// </summary>
        public void Pass()
        {
            if (!OtherHasAFreshAction) PassPriority();
        }

        /// <summary>Everyone passes once: resolves the top of the Chain (or, with an empty Chain, hands on the action and ends the step).</summary>
        public void PassRound()
        {
            int n = State.LivingPlayerCount;
            for (int i = 0; i < n && State.PriorityPlayer.HasValue && State.Pending == null; i++) PassPriority();
        }

        /// <summary>The other player has the action after mine: they pass, so <see cref="Active"/> has it again.</summary>
        public void HandTheActionBack()
        {
            if (OtherHasAFreshAction) PassPriority();
        }

        /// <summary>Pass priority until <paramref name="done"/> holds. Stops at decisions that aren't passes.</summary>
        public void PassUntil(Func<GameState, bool> done, int maxSteps = 500)
        {
            for (int i = 0; i < maxSteps; i++)
            {
                if (done(State)) return;
                if (State.Pending != null)
                {
                    // Default answers for decisions nobody in the test cares about.
                    switch (State.Pending.Kind)
                    {
                        case DecisionKind.DeclareAttackers: Do(PlayerAction.FinishAttacks(State.Pending.Player)); continue;
                        case DecisionKind.DeclareBlockers: Do(PlayerAction.FinishBlocks(State.Pending.Player)); continue;
                        case DecisionKind.DiscardToHandSize:
                        case DecisionKind.DiscardCards:
                            Do(PlayerAction.Discard(State.Pending.Player, P(State.Pending.Player).Hand[0].Id));
                            continue;
                        case DecisionKind.TopOrBottom: Do(PlayerAction.ChooseOption(State.Pending.Player, 0)); continue;
                        case DecisionKind.YesNo: Do(PlayerAction.ChooseOption(State.Pending.Player, 0)); continue;
                        case DecisionKind.ChooseObject:
                        case DecisionKind.OrderTriggers:
                        case DecisionKind.KeepLegendary:
                        case DecisionKind.DivideDamage:
                            Do(Legal(State.Pending.Player)[0]); continue;
                        default: Assert.Fail("PassUntil hit a decision: " + State.Pending); return;
                    }
                }
                if (State.IsGameOver) Assert.Fail("Game ended before the condition held.");
                PassPriority();
            }
            Assert.Fail("Condition not reached.");
        }

        /// <summary>
        /// The attack token holder attacks: passes until they have the action with an empty Chain, uses it to go to
        /// combat, and stops at declaring attackers.
        /// </summary>
        public void GoToCombat()
        {
            PassUntil(s => s.Step == Step.Main1 && s.Chain.Count == 0 && s.Pending == null && s.PriorityPlayer == Active
                           && s.ActivePlayer == Active);
            Do(PlayerAction.GoToCombat(Active));
            PassUntil(s => s.Pending?.Kind == DecisionKind.DeclareAttackers);
        }

        /// <summary>Pass until combat is over (the defender has the action).</summary>
        public void FinishCombat() =>
            PassUntil(s => s.Combat == null && s.Step == Step.Main1 && (s.PriorityPlayer.HasValue || s.Pending != null));

        /// <summary>Pass until a trigger is on the Chain at the start of the next round ("at the start of your turn").</summary>
        public void ToNextRoundStartTrigger()
        {
            int round = State.RoundNumber;
            PassUntil(s => s.RoundNumber > round && s.Step == Step.Start && s.Chain.Count > 0);
        }

        /// <summary>Pass until the next round's first action (everything "until end of turn" has ended, mana became Gold).</summary>
        public void NextRound()
        {
            int round = State.RoundNumber;
            PassUntil(s => s.RoundNumber > round && s.Step == Step.Main1 && (s.PriorityPlayer.HasValue || s.Pending != null));
        }

        public Characteristics Stats(CardInstance c) => Engine.GetCharacteristics(State, c);

        public CardInstance OnBattlefield(PlayerId player, string definitionId) =>
            P(player).Battlefield.FirstOrDefault(c => c.DefinitionId == definitionId);
    }
}
