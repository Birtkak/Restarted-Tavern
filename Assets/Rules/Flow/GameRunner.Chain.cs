using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>Priority, casting, triggers and the Chain (GAME_DESIGN §8, MTG CR 117, 405, 603, 608).</summary>
    internal sealed partial class GameRunner
    {
        /// <summary>
        /// A player would receive priority. First state-based actions and pending triggers are
        /// handled (MTG 117.5); then, if this step has a priority window or the Chain isn't
        /// empty, <paramref name="to"/> gets priority. Otherwise the game moves on to the next step.
        /// </summary>
        private void GivePriority(PlayerId to)
        {
            S.ResumePriorityTo = to;
            CheckStateBasedActionsAndTriggers();
            if (S.IsGameOver || S.Pending != null) return;

            if (S.Chain.Count > 0 || StepHasPriorityWindow(S.Step))
            {
                S.PriorityPlayer = S.GetPlayer(to).HasLost ? NextLivingPlayer(to).Id : to;
                S.PassesInRow = 0;
            }
            else
            {
                AdvanceStep();
            }
        }

        /// <summary>
        /// When every living player has passed in a row, the top of the Chain resolves and the
        /// active player gets priority again; with an empty Chain the step ends.
        /// </summary>
        private void Pass()
        {
            var passer = S.PriorityPlayer.Value;
            S.PassesInRow++;
            if (S.PassesInRow < S.LivingPlayerCount)
            {
                S.PriorityPlayer = NextLivingPlayer(passer).Id;
                return;
            }

            S.PriorityPlayer = null;
            if (S.Chain.Count > 0)
            {
                ResolveTopOfChain();
                if (!S.IsGameOver) GivePriority(S.ActivePlayer);
            }
            else
            {
                AdvanceStep();
            }
        }

        private void Cast(PlayerAction a)
        {
            var p = S.GetPlayer(a.Player);
            var card = p.Hand.Find(c => c.Id == a.Card);
            var def = Def(card);

            // §5.2: mana first, then Gold (not for permanents); Invest is paid only with Gold.
            int goldForCost = Payment.GoldNeeded(p, def);
            int manaPaid = def.Cost - goldForCost;
            p.Mana -= manaPaid;
            if (manaPaid > 0) Emit(new ManaChangedEvent { Player = p.Id, Mana = p.Mana, MaxMana = p.MaxMana });
            int goldPaid = goldForCost + (a.Invest ? def.InvestCost.Value : 0);
            if (goldPaid > 0) ChangeGold(p.Id, -goldPaid);

            var onChain = MoveCard(card, Zone.Chain, a.Player);
            var effects = new List<Effect>(def.SpellEffects);
            if (a.Invest && !def.IsPermanent) effects.AddRange(def.InvestEffects);

            var item = new ChainItem
            {
                Id = S.NextChainId++,
                Kind = ChainItemKind.Spell,
                Controller = a.Player,
                Card = onChain,
                SourceId = onChain.Id,
                SourceDefinitionId = def.Id,
                TargetSlots = def.SpellTargets,
                Effects = effects,
                Invested = a.Invest,
            };
            item.Targets.AddRange(a.Targets);
            S.Chain.Add(item);

            Emit(new SpellCastEvent
            {
                Player = a.Player, Card = onChain.Id, DefinitionId = def.Id, Targets = a.Targets,
                ManaPaid = manaPaid, GoldPaid = goldPaid, Invested = a.Invest,
            });

            // MTG 117.3c: the player who cast a spell receives priority afterwards.
            GivePriority(a.Player);
        }

        private void ResolveTopOfChain()
        {
            var item = S.Chain[S.Chain.Count - 1];
            S.Chain.RemoveAt(S.Chain.Count - 1);

            // §8 / MTG 608.2b: targets that became illegal are ignored; if every target is
            // illegal, the spell or ability fizzles and does nothing.
            var targets = new List<Target?>(item.Targets.Count);
            bool anyLegal = false;
            var excluded = item.TargetsExcludeSource ? item.SourceId : ObjectId.None;
            for (int i = 0; i < item.Targets.Count; i++)
            {
                bool legal = IsLegalTarget(item.Controller, item.TargetSlots[i].Spec, item.Targets[i], excluded);
                targets.Add(legal ? item.Targets[i] : (Target?)null);
                anyLegal |= legal;
            }
            if (item.Targets.Count > 0 && !anyLegal)
            {
                Emit(new FizzledEvent { ItemId = item.Id, SourceDefinitionId = item.SourceDefinitionId });
                if (item.Card != null) MoveCard(item.Card, Zone.Graveyard);
                return;
            }

            if (item.Kind == ChainItemKind.Spell)
            {
                var def = Db.Get(item.Card.DefinitionId);
                if (def.IsPermanent)
                {
                    var permanent = MoveCard(item.Card, Zone.Battlefield, item.Controller);
                    if (def.Type == CardType.Curse && targets.Count > 0 && targets[0].HasValue)
                    {
                        var t = targets[0].Value;
                        if (t.IsPlayer) permanent.AttachedToPlayer = t.Player;
                        else permanent.AttachedToObject = t.Object;
                    }
                    if (item.Invested)
                        RunEffects(def.InvestEffects, item.Controller, permanent.Id, targets);
                }
                else
                {
                    RunEffects(item.Effects, item.Controller, item.SourceId, targets);
                    MoveCard(item.Card, Zone.Graveyard);
                }
            }
            else
            {
                RunEffects(item.Effects, item.Controller, item.SourceId, targets);
            }

            Emit(new ChainItemResolvedEvent { ItemId = item.Id, SourceDefinitionId = item.SourceDefinitionId });
        }

        /// <summary><paramref name="targets"/> has null where a target became illegal.</summary>
        private void RunEffects(List<Effect> effects, PlayerId controller, ObjectId source, List<Target?> targets)
        {
            var ctx = new EffectContext(this, controller, source, targets);
            foreach (var e in effects)
            {
                if (S.IsGameOver) return;
                e.Resolve(ctx);
            }
        }

        // ------------------------------------------------------------------ triggers (MTG 603)

        private void QueueTriggers(CardDefinition def, TriggerEvent when, PlayerId controller, ObjectId source)
        {
            foreach (var ability in def.Triggers)
            {
                if (ability.When != when) continue;
                S.PendingTriggers.Add(new PendingTrigger
                {
                    Ability = ability,
                    Controller = controller,
                    SourceId = source,
                    SourceDefinitionId = def.Id,
                });
            }
        }

        private void QueueTurnTriggers(TriggerEvent when)
        {
            foreach (var c in new List<CardInstance>(S.ActivePlayerState.Battlefield))
                QueueTriggers(Def(c), when, c.Controller, c.Id);
        }

        /// <summary>
        /// Puts waiting triggers on the Chain in APNAP order (MTG 603.3b): the active player's go
        /// on first, so they resolve last. Returns false if it has to wait for a target choice.
        /// </summary>
        private bool PutPendingTriggersOnChain()
        {
            while (S.PendingTriggers.Count > 0)
            {
                int pick = 0;
                int bestDistance = int.MaxValue;
                for (int i = 0; i < S.PendingTriggers.Count; i++)
                {
                    int d = TurnOrderDistance(S.ActivePlayer, S.PendingTriggers[i].Controller);
                    if (d < bestDistance) { bestDistance = d; pick = i; }
                }
                var trigger = S.PendingTriggers[pick];
                S.PendingTriggers.RemoveAt(pick);

                if (S.GetPlayer(trigger.Controller).HasLost) continue;

                if (trigger.Ability.Target != TargetSpec.None)
                {
                    var targets = EnumerateTargets(trigger.Controller, trigger.Ability.Target,
                        trigger.Ability.TargetNotSelf ? trigger.SourceId : ObjectId.None);
                    if (targets.Count == 0) continue; // MTG 603.3d: no legal target → removed
                    if (targets.Count > 1)
                    {
                        S.Pending = new PendingDecision
                        {
                            Kind = DecisionKind.ChooseTriggerTarget,
                            Player = trigger.Controller,
                            Trigger = trigger,
                        };
                        return false;
                    }
                    PushTrigger(trigger, targets[0]);
                    continue;
                }

                PushTrigger(trigger, null);
            }
            return true;
        }

        private void ChooseTriggerTarget(Target target)
        {
            var trigger = S.Pending.Trigger;
            S.Pending = null;
            PushTrigger(trigger, target);
            GivePriority(S.ResumePriorityTo ?? S.ActivePlayer);
        }

        private void PushTrigger(PendingTrigger t, Target? target)
        {
            var item = new ChainItem
            {
                Id = S.NextChainId++,
                Kind = ChainItemKind.TriggeredAbility,
                Controller = t.Controller,
                SourceId = t.SourceId,
                SourceDefinitionId = t.SourceDefinitionId,
                Effects = t.Ability.Effects,
                TargetsExcludeSource = t.Ability.TargetNotSelf,
            };
            if (t.Ability.Target != TargetSpec.None) item.TargetSlots.Add(TargetSlot.Of(t.Ability.Target));
            if (target.HasValue) item.Targets.Add(target.Value);
            S.Chain.Add(item);
            Emit(new AbilityTriggeredEvent
            {
                Controller = t.Controller, Source = t.SourceId, SourceDefinitionId = t.SourceDefinitionId,
                When = t.Ability.When, Target = target,
            });
        }

        private int TurnOrderDistance(PlayerId from, PlayerId to)
        {
            int n = S.Players.Count;
            return (S.GetPlayer(to).Seat - S.GetPlayer(from).Seat + n) % n;
        }

        // ------------------------------------------------------------------ targeting

        /// <summary>Every legal choice for one target slot. <paramref name="exclude"/>: an object that can't be chosen ("another creature").</summary>
        internal List<Target> EnumerateTargets(PlayerId controller, TargetSpec spec, ObjectId exclude = default)
        {
            var result = new List<Target>();
            if (spec == TargetSpec.None) return result;

            foreach (var p in S.LivingPlayersFrom(controller))
            {
                bool ok;
                switch (spec)
                {
                    case TargetSpec.AnyTarget:
                    case TargetSpec.Player:
                        ok = true; break;
                    case TargetSpec.Opponent:
                    case TargetSpec.EnemyCreatureOrOpponent:
                        ok = S.AreOpponents(controller, p.Id); break;
                    case TargetSpec.CreatureOrYou:
                        ok = p.Id == controller; break;
                    default:
                        ok = false; break;
                }
                if (ok) result.Add(Target.ForPlayer(p.Id));
            }

            foreach (var p in S.LivingPlayersFrom(controller))
            {
                foreach (var c in p.Battlefield)
                {
                    if (!Def(c).IsCreature || c.Id == exclude) continue;
                    bool ok;
                    switch (spec)
                    {
                        case TargetSpec.AnyTarget:
                        case TargetSpec.Creature:
                        case TargetSpec.CreatureOrYou:
                            ok = true; break;
                        case TargetSpec.CreatureYouControl:
                            ok = c.Controller == controller; break;
                        case TargetSpec.CreatureYouDontControl:
                            ok = c.Controller != controller; break;
                        case TargetSpec.EnemyCreatureOrOpponent:
                            ok = S.AreOpponents(controller, c.Controller); break;
                        default:
                            ok = false; break;
                    }
                    if (ok) result.Add(Target.ForObject(c.Id));
                }
            }
            return result;
        }

        private bool IsLegalTarget(PlayerId controller, TargetSpec spec, Target target, ObjectId exclude = default) =>
            EnumerateTargets(controller, spec, exclude).Contains(target);

        /// <summary>
        /// Every way to fill a spell's target slots (distinct targets; optional slots may stay empty).
        /// Slots with the same spec are filled in enumeration order, so {A,B} and {B,A} aren't both listed.
        /// No slots: one empty choice. A required slot without candidates: no choices.
        /// </summary>
        internal List<Target[]> EnumerateTargetChoices(PlayerId controller, List<TargetSlot> slots)
        {
            var result = new List<Target[]>();
            var chosen = new List<Target>();
            var candidates = new List<List<Target>>();
            foreach (var slot in slots) candidates.Add(EnumerateTargets(controller, slot.Spec));
            Fill(0, -1);
            return result;

            void Fill(int slot, int previousIndex)
            {
                if (slot == slots.Count)
                {
                    result.Add(chosen.ToArray());
                    return;
                }
                if (slots[slot].Optional) result.Add(chosen.ToArray()); // stop here: later optional slots stay empty
                bool sameAsPrevious = slot > 0 && slots[slot].Spec == slots[slot - 1].Spec;
                var options = candidates[slot];
                for (int i = sameAsPrevious ? previousIndex + 1 : 0; i < options.Count; i++)
                {
                    if (chosen.Contains(options[i])) continue;
                    chosen.Add(options[i]);
                    Fill(slot + 1, i);
                    chosen.RemoveAt(chosen.Count - 1);
                }
            }
        }
    }
}
