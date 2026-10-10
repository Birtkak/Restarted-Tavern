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
    /// </summary>
    internal sealed class TestGame
    {
        public readonly GameEngine Engine;
        public readonly GameState State;
        public readonly List<GameEvent> Events = new List<GameEvent>();

        public PlayerId Active => State.ActivePlayer;
        public PlayerId Other => State.Players.First(p => p.Id != State.ActivePlayer).Id;

        private TestGame(GameEngine engine, GameState state)
        {
            Engine = engine;
            State = state;
        }

        /// <summary>
        /// A game at the first player's turn-1 main phase, both players kept 7.
        /// Decks are 20 Hired Sellswords unless given. Extra test-only cards can be added.
        /// Card tests step through MTG turns (<see cref="FormatConfig.MtgTurns"/>: round pool, Gold first, A B A B) unless
        /// a format is given; the Standard rounds (alternating actions) have their own tests (<see cref="Rounds"/>).
        /// </summary>
        public static TestGame AtFirstMainPhase(ulong seed = 1, IEnumerable<CardDefinition> extraCards = null,
            string deckCard = "hired_sellsword", int deckSize = 20, FormatConfig format = null)
        {
            var db = new CardDatabase(CardPool.All().Concat(extraCards ?? Enumerable.Empty<CardDefinition>()));
            var engine = new GameEngine(db);
            format = format ?? FormatConfig.MtgTurns();
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

        /// <summary>
        /// The same under the Classic rules (mana per turn, mana first, Gold cap 5): for tests of card logic
        /// written before the Standard rules changed (2026-10-10), and for the Classic rules themselves.
        /// </summary>
        public static TestGame Classic(ulong seed = 1, IEnumerable<CardDefinition> extraCards = null,
            string deckCard = "hired_sellsword", int deckSize = 20) =>
            AtFirstMainPhase(seed, extraCards, deckCard, deckSize, FormatConfig.Classic());

        public PlayerState P(PlayerId id) => State.GetPlayer(id);

        public List<GameEvent> Do(PlayerAction action)
        {
            var events = Engine.Apply(State, action);
            Events.AddRange(events);
            return events;
        }

        public List<PlayerAction> Legal(PlayerId player) => Engine.GetLegalActions(State, player);

        /// <summary>Put a new card straight into a hand (test setup, bypasses the rules).</summary>
        public CardInstance AddToHand(PlayerId player, string definitionId)
        {
            var c = NewCard(player, definitionId, Zone.Hand);
            P(player).Hand.Add(c);
            return c;
        }

        /// <summary>Put a new permanent straight onto the battlefield, not summoning sick (test setup).</summary>
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
        public void Pass() => Do(PlayerAction.Pass(State.PriorityPlayer.Value));

        /// <summary>Everyone passes once: resolves the top of the Chain (or ends the step).</summary>
        public void PassRound()
        {
            int n = State.LivingPlayerCount;
            for (int i = 0; i < n && State.PriorityPlayer.HasValue && State.Pending == null; i++) Pass();
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
                Pass();
            }
            Assert.Fail("Condition not reached.");
        }

        public void PassToStep(Step step, PlayerId? active = null) =>
            PassUntil(s => s.Step == step && (active == null || s.ActivePlayer == active)
                           && (s.PriorityPlayer.HasValue || s.Pending != null));

        public Characteristics Stats(CardInstance c) => Engine.GetCharacteristics(State, c);

        public CardInstance OnBattlefield(PlayerId player, string definitionId) =>
            P(player).Battlefield.FirstOrDefault(c => c.DefinitionId == definitionId);
    }
}
