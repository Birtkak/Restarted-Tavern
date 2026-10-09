using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// Does the actual work for one <see cref="GameEngine"/> call: it holds the state being
    /// changed and the events being produced. Split over several files by rules area:
    /// Flow (turns, priority, the Chain), Combat, StateBasedActions, and GameActions
    /// (the low-level moves every rule uses: draw, damage, zone changes…).
    /// </summary>
    internal sealed partial class GameRunner
    {
        internal readonly CardDatabase Db;
        internal readonly GameState State;
        private readonly List<GameEvent> _events;

        private GameState S => State;

        internal GameRunner(CardDatabase db, GameState state, List<GameEvent> events)
        {
            Db = db;
            State = state;
            _events = events;
        }

        private void Emit(GameEvent e) => _events?.Add(e);
        private CardDefinition Def(CardInstance c) => Db.Get(c.DefinitionId);
        private Characteristics Stats(CardInstance c) => CharacteristicsCalculator.Compute(S, Db, c);

        // ------------------------------------------------------------------ setup & mulligan

        internal void SetUpGame(IReadOnlyList<PlayerSetup> setups)
        {
            for (int i = 0; i < setups.Count; i++)
            {
                var setup = setups[i];
                var p = new PlayerState
                {
                    Id = new PlayerId(i + 1),
                    Seat = i,
                    TeamId = setup.TeamId ?? 1000 + i,
                    PatronId = setup.PatronId,
                    Life = S.Format.StartingLife,
                };
                S.Players.Add(p);
                foreach (var cardId in setup.Deck)
                    p.Deck.Add(NewObject(cardId, p.Id, Zone.Deck));
                S.Rng.Shuffle(p.Deck);
            }

            // GAME_DESIGN §3: who goes first is random.
            S.StartingPlayerIndex = S.Rng.Next(S.Players.Count);
            S.ActiveIndex = S.StartingPlayerIndex;
            Emit(new GameStartedEvent { StartingPlayer = S.ActivePlayer });

            foreach (var p in S.Players) Draw(p.Id, S.Format.StartingHand);

            S.Step = Step.Mulligan;
            S.Pending = new PendingDecision { Kind = DecisionKind.Mulligan, Player = S.ActivePlayer };
        }

        /// <summary>London mulligan (GAME_DESIGN §3): shuffle the hand away and draw 7 again.</summary>
        private void DoMulligan(PlayerId player)
        {
            var p = S.GetPlayer(player);
            foreach (var c in new List<CardInstance>(p.Hand)) MoveCard(c, Zone.Deck);
            S.Rng.Shuffle(p.Deck);
            p.MulligansTaken++;
            Emit(new MulliganEvent { Player = player, MulligansTaken = p.MulligansTaken });
            Draw(player, S.Format.StartingHand);
        }

        private void DoKeep(PlayerId player)
        {
            var p = S.GetPlayer(player);
            if (p.MulligansTaken > 0)
                S.Pending = new PendingDecision { Kind = DecisionKind.BottomCards, Player = player, Count = p.MulligansTaken };
            else
                NextMulliganDecision(player);
        }

        private void DoBottomCard(PlayerId player, ObjectId card)
        {
            var p = S.GetPlayer(player);
            MoveCard(p.Hand.Find(c => c.Id == card), Zone.Deck, toBottom: true);
            if (--S.Pending.Count == 0) NextMulliganDecision(player);
        }

        private void NextMulliganDecision(PlayerId justDecided)
        {
            var next = S.Players[(S.GetPlayer(justDecided).Seat + 1) % S.Players.Count];
            if (next.Seat != S.StartingPlayerIndex)
            {
                S.Pending = new PendingDecision { Kind = DecisionKind.Mulligan, Player = next.Id };
                return;
            }

            S.Pending = null;
            // Going-second compensation (§3) is the first-turn mana bonus in EnterStep(Start); these are experiment switches.
            var second = S.Players[(S.StartingPlayerIndex + 1) % S.Players.Count];
            if (S.Format.SecondPlayerStartingGold > 0) ChangeGold(second.Id, S.Format.SecondPlayerStartingGold);
            if (S.Format.SecondPlayerExtraCards > 0) Draw(second.Id, S.Format.SecondPlayerExtraCards);
            BeginTurn(S.StartingPlayerIndex);
        }

        // ------------------------------------------------------------------ actions

        internal void Perform(PlayerAction a)
        {
            switch (a.Kind)
            {
                case ActionKind.Keep: DoKeep(a.Player); break;
                case ActionKind.Mulligan: DoMulligan(a.Player); break;
                case ActionKind.BottomCard: DoBottomCard(a.Player, a.Card); break;
                case ActionKind.PassPriority: Pass(); break;
                case ActionKind.PlayCard: Cast(a); break;
                case ActionKind.DeclareAttacker: DeclareAttacker(a.Player, a.Card, a.Defender); break;
                case ActionKind.FinishAttacks: FinishAttacks(); break;
                case ActionKind.DeclareBlocker: DeclareBlocker(a.Card, a.BlockedAttacker); break;
                case ActionKind.FinishBlocks: FinishBlocks(a.Player); break;
                case ActionKind.ChooseTarget: ChooseTriggerTarget(a.Target.Value); break;
                case ActionKind.Discard: DiscardToHandSize(a.Player, a.Card); break;
                default: throw new ArgumentOutOfRangeException(nameof(a), a.Kind, null);
            }
        }

        /// <summary>
        /// Auto-pass (GAME_DESIGN §8 UX note): keep passing for players whose only legal action
        /// is to pass, until someone has a real choice or the game ends.
        /// </summary>
        internal void RunAutomaticActions()
        {
            int guard = 0;
            while (S.AutoPass && !S.IsGameOver && S.Pending == null && S.PriorityPlayer.HasValue)
            {
                if (LegalActions(S.PriorityPlayer.Value).Count > 1) return;
                Pass();
                if (++guard > 1_000_000) throw new InvalidOperationException("Auto-pass did not settle.");
            }
        }

        // ------------------------------------------------------------------ turn structure (§6)

        private void BeginTurn(int index)
        {
            S.ActiveIndex = index;
            S.TurnNumber++;
            Emit(new TurnStartedEvent { Player = S.ActivePlayer, Turn = S.TurnNumber });
            EnterStep(Step.Start);
        }

        private void EnterStep(Step step)
        {
            S.Step = step;
            S.PriorityPlayer = null;
            S.PassesInRow = 0;
            Emit(new StepStartedEvent { Step = step });

            var ap = S.ActivePlayerState;
            switch (step)
            {
                case Step.Start:
                    // §5.1: +1 max mana (cap 10), refill. MTG 502: untap. §7.4: summoning sickness ends.
                    ap.MaxMana = Math.Min(S.Format.ManaCap, ap.MaxMana + 1);
                    ap.Mana = ap.MaxMana;
                    if (S.TurnNumber == 2) ap.Mana += S.Format.SecondPlayerFirstTurnBonusMana;
                    Emit(new ManaChangedEvent { Player = ap.Id, Mana = ap.Mana, MaxMana = ap.MaxMana });
                    foreach (var c in ap.Battlefield)
                    {
                        c.Tapped = false;
                        c.SummoningSick = false;
                    }
                    QueueTurnTriggers(TriggerEvent.StartOfYourTurn);
                    GivePriority(ap.Id);
                    break;

                case Step.Draw:
                    // §3 (MTG): the first player skips the turn-1 draw in 1v1.
                    if (!(S.TurnNumber == 1 && S.Format.FirstPlayerSkipsDraw)) Draw(ap.Id, 1);
                    GivePriority(ap.Id);
                    break;

                case Step.DeclareAttackers:
                    S.Combat = new CombatState();
                    if (HasPossibleAttacker(ap))
                        S.Pending = new PendingDecision { Kind = DecisionKind.DeclareAttackers, Player = ap.Id };
                    else
                        GivePriority(ap.Id);
                    break;

                case Step.DeclareBlockers:
                    AskNextDefenderForBlockers();
                    break;

                case Step.CombatDamage:
                    DealCombatDamage();
                    GivePriority(ap.Id);
                    break;

                case Step.Main2:
                    S.Combat = null; // end of combat
                    GivePriority(ap.Id);
                    break;

                case Step.End:
                    QueueTurnTriggers(TriggerEvent.EndOfYourTurn);
                    GivePriority(ap.Id);
                    break;

                case Step.Cleanup:
                    int excess = ap.Hand.Count - S.Format.MaxHandSize;
                    if (excess > 0)
                        S.Pending = new PendingDecision { Kind = DecisionKind.DiscardToHandSize, Player = ap.Id, Count = excess };
                    else
                        FinishCleanup();
                    break;

                default: // Main1, BeginCombat
                    GivePriority(ap.Id);
                    break;
            }
        }

        private void AdvanceStep()
        {
            switch (S.Step)
            {
                case Step.Start: EnterStep(Step.Draw); break;
                case Step.Draw: EnterStep(Step.Main1); break;
                case Step.Main1: EnterStep(Step.BeginCombat); break;
                case Step.BeginCombat: EnterStep(Step.DeclareAttackers); break;
                case Step.DeclareAttackers:
                    // MTG 508.8: no attackers → skip blockers and damage.
                    EnterStep(S.Combat != null && S.Combat.Attacks.Count > 0 ? Step.DeclareBlockers : Step.Main2);
                    break;
                case Step.DeclareBlockers: EnterStep(Step.CombatDamage); break;
                case Step.CombatDamage: EnterStep(Step.Main2); break;
                case Step.Main2: EnterStep(Step.End); break;
                case Step.End: EnterStep(Step.Cleanup); break;
                case Step.Cleanup: FinishCleanup(); break;
                default: throw new InvalidOperationException("Can't advance from " + S.Step);
            }
        }

        /// <summary>
        /// Fixed priority windows (GAME_DESIGN §8): each main phase, start of combat, after
        /// attackers, after blockers, and the end phase. Other steps only give priority while
        /// something is on the Chain (e.g. a Start-of-turn trigger).
        /// </summary>
        private bool StepHasPriorityWindow(Step step)
        {
            switch (step)
            {
                case Step.Main1:
                case Step.BeginCombat:
                case Step.Main2:
                case Step.End:
                    return true;
                case Step.DeclareAttackers:
                case Step.DeclareBlockers:
                    return S.Combat != null && S.Combat.Attacks.Count > 0;
                default:
                    return false;
            }
        }

        /// <summary>§6 end phase: discard to 7 (done), then unspent mana becomes Gold (§5.2), then "until end of turn" ends.</summary>
        private void FinishCleanup()
        {
            S.Pending = null;
            var ap = S.ActivePlayerState;
            int banked = Math.Max(0, Math.Min(ap.Mana, S.Format.GoldCap - ap.Gold));
            if (ap.Mana > 0) Emit(new GoldBankedEvent { Player = ap.Id, UnspentMana = ap.Mana, Banked = banked });
            if (banked > 0) ChangeGold(ap.Id, banked);
            ap.Mana = 0; // mana is only filled during your own turn (§5.2)
            Emit(new ManaChangedEvent { Player = ap.Id, Mana = 0, MaxMana = ap.MaxMana });

            var beforeBuffsEnd = SnapshotRemainingHealth();
            S.UntilEndOfTurn.Clear();
            CapDamageAfterBuffsEnd(beforeBuffsEnd);
            if (S.Format.DamageWearsOff)
                foreach (var c in S.AllPermanents()) c.Damage = 0;
            S.Combat = null;

            var next = NextLivingPlayer(ap.Id);
            BeginTurn(next.Seat);
        }

        private void DiscardToHandSize(PlayerId player, ObjectId card)
        {
            var p = S.GetPlayer(player);
            MoveCard(p.Hand.Find(c => c.Id == card), Zone.Graveyard);
            if (--S.Pending.Count == 0) FinishCleanup();
        }

        private PlayerState NextLivingPlayer(PlayerId after)
        {
            int seat = S.GetPlayer(after).Seat;
            for (int i = 1; i <= S.Players.Count; i++)
            {
                var p = S.Players[(seat + i) % S.Players.Count];
                if (!p.HasLost) return p;
            }
            throw new InvalidOperationException("No living players.");
        }
    }
}
