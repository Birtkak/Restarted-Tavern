using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>Everything a player may do right now. Apply() only accepts actions from this list.</summary>
    internal sealed partial class GameRunner
    {
        internal List<PlayerAction> LegalActions(PlayerId player)
        {
            var result = new List<PlayerAction>();
            if (S.IsGameOver) return result;

            if (S.Pending != null)
            {
                if (S.Pending.Player == player) AddDecisionActions(player, result);
                return result;
            }

            if (S.PriorityPlayer != player) return result;
            result.Add(PlayerAction.Pass(player));
            AddPlayableCards(player, result);
            return result;
        }

        private void AddDecisionActions(PlayerId player, List<PlayerAction> result)
        {
            var p = S.GetPlayer(player);
            switch (S.Pending.Kind)
            {
                case DecisionKind.Mulligan:
                    result.Add(PlayerAction.Keep(player));
                    if (p.MulligansTaken < S.Format.StartingHand) result.Add(PlayerAction.Mulligan(player));
                    break;

                case DecisionKind.BottomCards:
                    foreach (var c in p.Hand) result.Add(PlayerAction.BottomCard(player, c.Id));
                    break;

                case DecisionKind.DiscardToHandSize:
                    foreach (var c in p.Hand) result.Add(PlayerAction.Discard(player, c.Id));
                    break;

                case DecisionKind.ChooseTriggerTarget:
                    var trigger = S.Pending.Trigger;
                    foreach (var t in EnumerateTargets(player, trigger.Ability.Target,
                                 trigger.Ability.TargetNotSelf ? trigger.SourceId : ObjectId.None))
                        result.Add(PlayerAction.ChooseTarget(player, t));
                    break;

                case DecisionKind.DeclareAttackers:
                    foreach (var c in p.Battlefield)
                    {
                        if (!CanAttack(c) || S.Combat.IsAttacking(c.Id)) continue;
                        foreach (var defender in S.LivingPlayersFrom(player))
                            if (S.AreOpponents(player, defender.Id))
                                result.Add(PlayerAction.Attack(player, c.Id, defender.Id));
                    }
                    result.Add(PlayerAction.FinishAttacks(player));
                    break;

                case DecisionKind.DeclareBlockers:
                    foreach (var c in p.Battlefield)
                    {
                        if (!CanBlock(c) || S.Combat.IsBlocking(c.Id)) continue;
                        foreach (var attack in S.Combat.Attacks)
                        {
                            if (attack.Defender != player) continue;
                            var attacker = S.FindOnBattlefield(attack.Attacker);
                            if (attacker != null && CanBlockAttacker(c, attacker))
                                result.Add(PlayerAction.Block(player, c.Id, attacker.Id));
                        }
                    }
                    result.Add(PlayerAction.FinishBlocks(player));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// GAME_DESIGN §10 timing + §5.2 payment. Timing: Instants whenever you have priority,
        /// everything else in your own main phase with an empty Chain. Payment: creatures mana
        /// only; everything else any mix of mana and Gold (one action per possible split).
        /// </summary>
        private void AddPlayableCards(PlayerId player, List<PlayerAction> result)
        {
            var p = S.GetPlayer(player);
            bool sorcerySpeed = S.ActivePlayer == player
                && (S.Step == Step.Main1 || S.Step == Step.Main2)
                && S.Chain.Count == 0;

            foreach (var card in p.Hand)
            {
                var def = Def(card);
                bool instant = def.Type == CardType.Instant;
                if (!instant && !sorcerySpeed) continue;
                // §5.2: everything except creatures can be paid with any mix of mana and Gold.
                bool goldAllowed = !def.IsCreature;

                var targetChoices = EnumerateTargetChoices(player, def.SpellTargets);
                if (targetChoices.Count == 0) continue;

                int maxGold = goldAllowed ? Math.Min(p.Gold, def.Cost) : 0;
                for (int gold = 0; gold <= maxGold; gold++)
                {
                    if (def.Cost - gold > p.Mana) continue;
                    bool canOvercharge = def.OverchargeCost.HasValue && p.Gold - gold >= def.OverchargeCost.Value;
                    for (int oc = 0; oc <= (canOvercharge ? 1 : 0); oc++)
                        foreach (var targets in targetChoices)
                            result.Add(PlayerAction.Play(player, card.Id, targets, gold, oc == 1));
                }
            }
        }
    }
}
