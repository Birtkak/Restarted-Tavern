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
            bool sorcerySpeed = S.ActivePlayer == player
                && (S.Step == Step.Main1 || S.Step == Step.Main2)
                && S.Chain.Count == 0;
            AddPlayableCards(player, result, sorcerySpeed);
            AddActivatableAbilities(player, result, sorcerySpeed);
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
                case DecisionKind.DiscardCards:
                    foreach (var c in p.Hand) result.Add(PlayerAction.Discard(player, c.Id));
                    break;

                case DecisionKind.ChooseTriggerTarget:
                    var trigger = S.Pending.Trigger;
                    foreach (var t in EnumerateTargets(player, trigger.Ability.Slot,
                                 trigger.Ability.TargetNotSelf ? trigger.SourceId : ObjectId.None))
                        result.Add(PlayerAction.ChooseTarget(player, t));
                    if (trigger.Ability.TargetOptional) // "you may": decline
                        result.Add(new PlayerAction { Kind = ActionKind.ChooseTarget, Player = player });
                    break;

                case DecisionKind.TopOrBottom:
                    result.Add(PlayerAction.ChooseOption(player, 0));
                    result.Add(PlayerAction.ChooseOption(player, 1));
                    break;

                case DecisionKind.ChooseFromTop:
                    for (int i = 0; i < S.Pending.Count; i++) result.Add(PlayerAction.ChooseOption(player, i));
                    break;

                case DecisionKind.PayAnyGold:
                    for (int gold = 0; gold <= p.Gold; gold++) result.Add(PlayerAction.ChooseOption(player, gold));
                    break;

                case DecisionKind.PayTax:
                    result.Add(PlayerAction.ChooseOption(player, 0)); // don't pay: it's countered
                    if (Payment.GoldNeeded(p, S.Pending.Count, true) >= 0) result.Add(PlayerAction.ChooseOption(player, 1));
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
        /// everything else in your own main phase with an empty Chain. Payment is automatic:
        /// permanents use mana only; Instants and Sorceries use mana first, then Gold.
        /// </summary>
        private void AddPlayableCards(PlayerId player, List<PlayerAction> result, bool sorcerySpeed)
        {
            var p = S.GetPlayer(player);

            foreach (var card in p.Hand)
            {
                var def = Def(card);
                bool instant = def.Type == CardType.Instant || def.Flash;
                if (!instant && !sorcerySpeed) continue;
                if (!Payment.CanPay(S, Db, p, def)) continue;
                if (def.ExtraLifeCost > p.Life) continue; // MTG 119.4: you can only pay life you have

                var targetChoices = EnumerateTargetChoices(player, def.SpellTargets);
                if (targetChoices.Count == 0) continue;

                // "As an extra cost, sacrifice a creature": one action per creature you could sacrifice.
                var sacrifices = new List<ObjectId>();
                if (def.SacrificeCreatureCost)
                {
                    foreach (var c in p.Battlefield)
                        if (Def(c).IsCreature) sacrifices.Add(c.Id);
                    if (sacrifices.Count == 0) continue;
                }
                else
                {
                    sacrifices.Add(ObjectId.None);
                }

                bool canInvest = Payment.CanInvest(S, Db, p, def);
                int goldForCost = Payment.GoldNeeded(S, Db, p, def);
                for (int invest = 0; invest <= (canInvest ? 1 : 0); invest++)
                {
                    // "As an extra cost, pay any amount of Gold (X)": every X the remaining Gold allows, 0 included.
                    int investGold = 0;
                    if (invest == 1) Payment.InvestSplit(S, Db, p, def, out _, out investGold);
                    int maxX = def.XGoldExtraCost ? p.Gold - goldForCost - investGold : 0;
                    for (int x = 0; x <= maxX; x++)
                        foreach (var targets in targetChoices)
                            foreach (var sacrifice in sacrifices)
                            {
                                // Targeting the creature you sacrifice would only make the spell fizzle.
                                if (!sacrifice.IsNone && Array.IndexOf(targets, Target.ForObject(sacrifice)) >= 0) continue;
                                var play = PlayerAction.Play(player, card.Id, targets, invest == 1, x);
                                play.Sacrifice = sacrifice;
                                result.Add(play);
                            }
                }
            }
        }
    }
}
