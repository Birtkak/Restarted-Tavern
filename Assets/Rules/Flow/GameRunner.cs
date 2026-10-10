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
                    TavernDwellerId = setup.TavernDwellerId,
                    Life = S.Format.StartingLife,
                };
                S.Players.Add(p);
                // §9.1: the Tavern Dweller starts in the Tavern Dweller zone and stays there.
                if (setup.TavernDwellerId != null && S.Format.TavernDwellersEnabled)
                    p.TavernDwellerZone.Add(NewObject(setup.TavernDwellerId, p.Id, Zone.TavernDweller));
                foreach (var cardId in setup.Deck)
                    p.Deck.Add(NewObject(cardId, p.Id, Zone.Deck));
                S.Rng.Shuffle(p.Deck);
            }

            // GAME_DESIGN §3: who goes first is random.
            S.StartingPlayerIndex = S.Rng.Next(S.Players.Count);
            S.ActiveIndex = S.StartingPlayerIndex;
            S.RoundLeaderSeat = S.StartingPlayerIndex;
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
                case ActionKind.ChooseTarget:
                    if (S.Pending.Kind == DecisionKind.ChooseObject) AnswerChoice(a.Target);
                    else if (S.Pending.Kind == DecisionKind.ChooseUpTo) AnswerChooseUpTo(a.Target);
                    else if (S.Pending.Kind == DecisionKind.KeepLegendary) KeepLegendary(a.Target.Value.Object);
                    else if (S.Pending.Kind == DecisionKind.DivideDamage) AnswerDividePoint(a.Target.Value);
                    else ChooseTriggerTarget(a.Target);
                    break;
                case ActionKind.Discard:
                    if (S.Pending.Kind == DecisionKind.DiscardCards) AnswerDiscard(a);
                    else DiscardToHandSize(a.Player, a.Card);
                    break;
                case ActionKind.ActivateAbility: Activate(a); break;
                case ActionKind.ChooseOption: AnswerOption(a); break;
                case ActionKind.AssignCombatDamage: AnswerDamageAssignment(a); break;
                case ActionKind.GoToCombat:
                    S.AttackedThisRound |= 1 << S.ActivePlayerState.Seat;
                    S.ActionInProgress = true;
                    EnterStep(Step.BeginCombat);
                    break;
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
                if (HasMoreThanPass(S.PriorityPlayer.Value)) return;
                Pass();
                if (++guard > 1_000_000) throw new InvalidOperationException("Auto-pass did not settle.");
            }
        }

        // ------------------------------------------------------------------ turn structure (§6)

        private void BeginTurn(int index)
        {
            S.ActiveIndex = index;
            S.TurnNumber++;
            S.UsesThisTurn.Clear(); // "once each turn" (MTG) resets every turn, yours or not
            S.ActionInProgress = false;
            S.AttackedThisRound = 0;
            foreach (var p in S.Players)
                p.PaysGoldFirst = S.Format.GoldFirstAlways || (S.Format.GoldFirstOffTurn && p.Id != S.ActivePlayer);
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
                    // §5.1: +1 max mana (cap 10), refill: your own turn, or with the round pool every player when a round starts.
                    // MTG 502: untap. §7.4: summoning sickness ends.
                    if (!S.Format.ManaPerRound)
                    {
                        if (S.Format.ManaUntilYourNextTurn) BankMana(ap);
                        RefillMana(ap);
                    }
                    else if (S.TurnsThisRound == 0)
                    {
                        foreach (var p in S.Players)
                            if (!p.HasLost) RefillMana(p);
                    }
                    if (S.Format.SecondPlayerFirstTurnBonusMana > 0 && ap.Seat != S.StartingPlayerIndex
                        && S.TurnNumber <= 2 * S.Format.SecondPlayerBonusTurns && S.Players.Count == 2)
                    {
                        ap.Mana += S.Format.SecondPlayerFirstTurnBonusMana;
                        Emit(new ManaChangedEvent { Player = ap.Id, Mana = ap.Mana, MaxMana = ap.MaxMana });
                    }
                    foreach (var p in TurnPlayers())
                        foreach (var c in p.Battlefield)
                        {
                            c.Tapped = false;
                            c.SummoningSick = false;
                        }
                    ForEachTurnPlayer(() => QueueTurnTriggers(TriggerEvent.StartOfYourTurn));
                    GivePriority(ap.Id);
                    break;

                case Step.Draw:
                    // §3 (MTG): the first player skips the turn-1 draw in 1v1. Alternating actions: everyone draws.
                    foreach (var p in TurnPlayers())
                        if (!(S.TurnNumber == 1 && S.Format.FirstPlayerSkipsDraw && p.Seat == S.StartingPlayerIndex)) Draw(p.Id, 1);
                    GivePriority(ap.Id);
                    break;

                case Step.DeclareAttackers:
                    S.Combat = new CombatState();
                    if (HasPossibleAttacker(ap) && HasAttackToken(ap))
                        S.Pending = new PendingDecision { Kind = DecisionKind.DeclareAttackers, Player = ap.Id };
                    else
                        GivePriority(ap.Id);
                    break;

                case Step.DeclareBlockers:
                    AskNextDefenderForBlockers();
                    break;

                case Step.CombatDamage:
                    AskNextDamageAssignment(); // then deals the damage and gives priority
                    break;

                case Step.Main2:
                    S.Combat = null; // end of combat
                    GivePriority(ap.Id);
                    break;

                case Step.End:
                    ForEachTurnPlayer(() => QueueTurnTriggers(TriggerEvent.EndOfYourTurn));
                    GivePriority(ap.Id);
                    break;

                case Step.Cleanup:
                    AskNextDiscardToHandSize();
                    break;

                default: // Main1, BeginCombat
                    GivePriority(ap.Id);
                    break;
            }
        }

        private void AdvanceStep()
        {
            if (S.Format.AlternatingActions)
            {
                switch (S.Step)
                {
                    case Step.Main1:
                        // Everyone passed in a row: the round ends. The leader is active for the end of the round.
                        S.ActiveIndex = RoundLeader().Seat;
                        EnterStep(Step.End);
                        return;
                    case Step.DeclareAttackers when S.Combat == null || S.Combat.Attacks.Count == 0:
                    case Step.CombatDamage:
                        // The attack was the action: the next player has the action.
                        S.Combat = null;
                        S.ActionInProgress = false;
                        S.ActiveIndex = NextLivingPlayer(S.ActivePlayer).Seat;
                        EnterStep(Step.Main1);
                        return;
                }
            }
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
                case Step.Cleanup: EnterStep(Step.Cleanup); break; // MTG 514.3a: another cleanup step
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

        /// <summary>
        /// §6 end phase: discard to 7 (done), then unspent mana becomes Gold (§5.2), then "until end
        /// of turn" ends. Banking Gold can trigger ("whenever you bank Gold"): then, as in MTG 514.3a,
        /// the triggers go on the Chain, players get priority, and the cleanup step repeats once the
        /// Chain is empty (AdvanceStep). In a repeated cleanup there is no mana left to bank.
        /// </summary>
        private void FinishCleanup()
        {
            S.Pending = null;
            var ap = S.ActivePlayerState;
            bool endOfRound = S.Format.AlternatingActions || S.TurnsThisRound + 1 >= S.LivingPlayerCount;
            if (!S.Format.ManaPerRound) { if (!S.Format.ManaUntilYourNextTurn) BankMana(ap); } // mana is only filled during your own turn (§5.2)
            else if (endOfRound)
                foreach (var p in S.LivingPlayersFrom(ap.Id)) BankMana(p); // the round pool empties at the end of the round

            EndTemporaryControl();
            var beforeBuffsEnd = SnapshotRemainingHealth();
            S.UntilEndOfTurn.Clear();
            S.Replacements.RemoveAll(r => r.UntilEndOfTurn);
            CapDamageAfterBuffsEnd(beforeBuffsEnd);
            if (S.Format.DamageWearsOff)
                foreach (var c in S.AllPermanents()) c.Damage = 0;

            // MTG 514.3a: state-based actions and waiting triggers give players priority in cleanup.
            S.ResumePriorityTo = ap.Id;
            CheckStateBasedActionsAndTriggers();
            if (S.IsGameOver || S.Pending != null) return;
            if (S.Chain.Count > 0)
            {
                S.PriorityPlayer = ap.Id;
                S.PassesInRow = 0;
                return;
            }

            S.Combat = null;
            BeginTurn(NextTurnSeat(ap));
        }

        /// <summary>
        /// Who's next: the next living player, unless the round is over and RotateRoundLeader passes the start of the
        /// round to the next seat (A B | B A | A B in 1v1).
        /// </summary>
        private int NextTurnSeat(PlayerState ap)
        {
            if (!S.Format.AlternatingActions && S.TurnsThisRound + 1 < S.LivingPlayerCount)
            {
                S.TurnsThisRound++;
                return NextLivingPlayer(ap.Id).Seat;
            }
            S.TurnsThisRound = 0;
            S.RoundNumber++;
            if (S.Format.RotateRoundLeader) S.RoundLeaderSeat = NextLivingPlayer(S.Players[S.RoundLeaderSeat].Id).Seat;
            else S.RoundLeaderSeat = NextLivingPlayer(ap.Id).Seat;
            return RoundLeader().Seat;
        }

        private void RefillMana(PlayerState p)
        {
            bool skip = S.Format.FirstPlayerSkipsFirstMana && S.TurnNumber == 1 && p.Seat == S.StartingPlayerIndex;
            if (!skip) p.MaxMana = Math.Min(S.Format.ManaCap, p.MaxMana + 1);
            p.Mana = p.MaxMana;
            if (S.Format.FirstPlayerNoManaFirstRound && S.TurnNumber == 1 && p.Seat == S.StartingPlayerIndex) p.Mana = 0;
            Emit(new ManaChangedEvent { Player = p.Id, Mana = p.Mana, MaxMana = p.MaxMana });
        }

        /// <summary>Unspent mana becomes Gold up to the cap (§5.2); the rest is lost.</summary>
        private void BankMana(PlayerState p)
        {
            if (p.Mana <= 0) return;
            int banked = Math.Max(0, Math.Min(p.Mana, GoldRules.Cap(S, Db, p.Id) - p.Gold));
            Emit(new GoldBankedEvent { Player = p.Id, UnspentMana = p.Mana, Banked = banked });
            if (banked > 0)
            {
                ChangeGold(p.Id, banked);
                QueueWatcherTriggers(TriggerEvent.GoldBanked, p.Id, (t, _) => banked >= t.MinAmount, banked);
            }
            p.Mana = 0;
            Emit(new ManaChangedEvent { Player = p.Id, Mana = 0, MaxMana = p.MaxMana });
        }

        /// <summary>The player who starts each round: the first player, or the next one still in the game.</summary>
        private PlayerState RoundLeader() => S.LivingPlayersFrom(S.Players[S.RoundLeaderSeat].Id)[0];

        private void DiscardToHandSize(PlayerId player, ObjectId card)
        {
            var p = S.GetPlayer(player);
            MoveCard(p.Hand.Find(c => c.Id == card), Zone.Graveyard);
            if (--S.Pending.Count == 0)
            {
                S.Pending = null;
                AskNextDiscardToHandSize();
            }
        }

        /// <summary>
        /// Cleanup (§3): the active player discards down to the maximum hand size. Alternating actions: the round is
        /// everyone's turn, so every player does, in turn order from the leader.
        /// </summary>
        private void AskNextDiscardToHandSize()
        {
            foreach (var p in TurnPlayers())
            {
                int excess = p.Hand.Count - S.Format.MaxHandSize;
                if (excess <= 0) continue;
                S.Pending = new PendingDecision { Kind = DecisionKind.DiscardToHandSize, Player = p.Id, Count = excess };
                return;
            }
            FinishCleanup();
        }

        /// <summary>Whose turn it is: the active player, or with alternating actions everyone (from the active player).</summary>
        private List<PlayerState> TurnPlayers() =>
            S.Format.AlternatingActions ? S.LivingPlayersFrom(S.ActivePlayer) : new List<PlayerState> { S.ActivePlayerState };

        /// <summary>Alternating actions: the main phase is the action phase.</summary>
        private bool InActionPhase => S.Format.AlternatingActions && S.Step == Step.Main1;

        /// <summary>An action starts when the player who has the action puts something on an empty Chain.</summary>
        private void MarkActionStarted(PlayerId player)
        {
            if (InActionPhase && S.Chain.Count == 0 && player == S.ActivePlayer) S.ActionInProgress = true;
        }

        /// <summary>Alternating actions: the attack token holder may use an action to attack, once each round.</summary>
        private bool CanStartAttack(PlayerState p) =>
            (S.AttackedThisRound & (1 << p.Seat)) == 0 && HasAttackToken(p) && HasPossibleAttacker(p);

        /// <summary>
        /// "Your turn" triggers: for the active player, or with alternating actions for every player (the round is
        /// everyone's turn), in turn order from the leader.
        /// </summary>
        private void ForEachTurnPlayer(Action queue)
        {
            if (!S.Format.AlternatingActions)
            {
                queue();
                return;
            }
            int active = S.ActiveIndex;
            foreach (var p in S.LivingPlayersFrom(S.ActivePlayer))
            {
                S.ActiveIndex = p.Seat;
                queue();
            }
            S.ActiveIndex = active;
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
