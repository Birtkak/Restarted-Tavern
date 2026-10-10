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
            // Choices queued by a resolving effect come first, one after another.
            if (S.Pending == null && S.ChoiceQueue.Count > 0)
            {
                S.Pending = S.ChoiceQueue[0];
                S.ChoiceQueue.RemoveAt(0);
                S.ResumePriorityTo = to;
                return;
            }
            S.ResumePriorityTo = to;
            CheckStateBasedActionsAndTriggers();
            if (S.IsGameOver || S.Pending != null) return;

            // Alternating actions: once an action has fully resolved, the next player has the action.
            if (InActionPhase && S.Chain.Count == 0 && S.ActionInProgress)
            {
                S.ActionInProgress = false;
                S.ActiveIndex = NextLivingPlayer(S.ActivePlayer).Seat;
                to = S.ActivePlayer;
            }

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
                // Alternating actions: passing with an empty Chain hands the action on.
                if (InActionPhase && S.Chain.Count == 0) S.ActiveIndex = S.GetPlayer(S.PriorityPlayer.Value).Seat;
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

            // §5.2: Gold first (Classic: mana first), never Gold for permanents; Invest and "pay X Gold" only with Gold,
            // set aside before the cost takes Gold. Cost modifiers (Tavern Dwellers, Archon Lumen...) are applied first.
            // Silent Partner lets Invest use the mana that's left.
            Payment.TrySplit(S, Db, p, def, a.X, a.Invest, out var pay);
            int goldForCost = pay.GoldForCost;
            int manaPaid = pay.Mana;
            p.Mana -= manaPaid;
            if (manaPaid > 0) Emit(new ManaChangedEvent { Player = p.Id, Mana = p.Mana, MaxMana = p.MaxMana });
            int goldPaid = pay.Gold;
            if (goldPaid > 0) ChangeGold(p.Id, -goldPaid);
            // Extra costs (MTG 601.2h): pay life, sacrifice a creature (its last known Power is kept).
            if (def.ExtraLifeCost > 0) ChangeLife(p.Id, -def.ExtraLifeCost);
            int sacrificedPower = 0;
            if (!a.Sacrifice.IsNone)
            {
                var victim = S.FindOnBattlefield(a.Sacrifice);
                sacrificedPower = Stats(victim).Power;
                MoveCard(victim, Zone.Graveyard);
            }

            var onChain = MoveCard(card, Zone.Chain, a.Player);
            var effects = new List<Effect>(def.SpellEffects);
            if (a.Invest && !def.IsPermanent) effects.AddRange(def.InvestEffects);

            var item = new ChainItem
            {
                Id = S.NextChainId++,
                ObjectId = onChain.Id,
                Kind = ChainItemKind.Spell,
                Controller = a.Player,
                Card = onChain,
                SourceId = onChain.Id,
                SourceDefinitionId = def.Id,
                TargetSlots = def.SpellTargets,
                Effects = effects,
                Invested = a.Invest,
                X = def.XGoldExtraCost || def.XCost ? a.X : 0,
                SacrificedPower = sacrificedPower,
                Division = a.Division,
                GoldPaid = goldPaid,
            };
            item.Targets.AddRange(a.Targets);
            MarkActionStarted(a.Player);
            S.Chain.Add(item);

            Emit(new SpellCastEvent
            {
                Player = a.Player, Card = onChain.Id, DefinitionId = def.Id, Targets = a.Targets,
                ManaPaid = manaPaid, GoldPaid = goldPaid, Invested = a.Invest, X = item.X,
            });
            string spellsKey = "spells:" + a.Player.Value;
            S.UsesThisTurn.TryGetValue(spellsKey, out int spellsCast);
            S.UsesThisTurn[spellsKey] = ++spellsCast;
            QueueWatcherTriggers(TriggerEvent.SpellCast, a.Player,
                (t, _) => def.Cost >= t.MinCost && (t.NthSpellThisTurn == 0 || t.NthSpellThisTurn == spellsCast),
                eventObject: onChain.Id, eventPlayer: a.Player);
            if (goldPaid > 0) GoldSpent(a.Player, goldPaid);
            if (def.IsCreature && goldForCost > 0) QueueWatcherTriggers(TriggerEvent.GoldPaidForCreatureSpell, a.Player);

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
                bool legal = IsLegalTarget(item.Controller, item.TargetSlots[i], item.Targets[i], excluded);
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
                        RunEffects(def.InvestEffects, item.Controller, permanent.Id, targets, sourceDefinitionId: def.Id);
                }
                else
                {
                    RunEffects(item.Effects, item.Controller, item.SourceId, targets, item.X, sacrificedPower: item.SacrificedPower,
                        sourceDefinitionId: item.SourceDefinitionId, invested: item.Invested, division: item.Division, goldSpent: item.GoldPaid);
                    MoveCard(item.Card, Zone.Graveyard);
                }
            }
            else if (item.Condition == null || item.Condition.Holds(S, Db, item.Controller, item.SourceId)) // MTG 603.4
            {
                RunEffects(item.Effects, item.Controller, item.SourceId, targets, item.X, item.EventAmount, item.EventObject, item.EventPlayer,
                    sourceDefinitionId: item.SourceDefinitionId);
            }

            Emit(new ChainItemResolvedEvent { ItemId = item.Id, SourceDefinitionId = item.SourceDefinitionId });
        }

        /// <summary><paramref name="targets"/> has null where a target became illegal.</summary>
        private void RunEffects(List<Effect> effects, PlayerId controller, ObjectId source, List<Target?> targets, int x = 0,
            int eventAmount = 0, ObjectId eventObject = default, PlayerId? eventPlayer = null, int sacrificedPower = 0,
            string sourceDefinitionId = null, bool invested = false, int[] division = null, int goldSpent = 0)
        {
            var ctx = new EffectContext(this, controller, source, targets, x, eventAmount, eventObject, eventPlayer, sacrificedPower,
                sourceDefinitionId, invested, goldSpent);
            if (division != null) ctx.Division = division;
            foreach (var e in effects)
            {
                if (S.IsGameOver) return;
                e.Resolve(ctx);
            }
        }

        // ------------------------------------------------------------------ triggers (MTG 603)

        /// <summary>Queue the triggers of a card definition (used for objects that just left the battlefield).</summary>
        private void QueueTriggers(CardDefinition def, TriggerEvent when, PlayerId controller, ObjectId source)
        {
            foreach (var ability in def.Triggers)
                if (ability.When == when) QueueTrigger(ability, controller, source, def.Id, null);
        }

        /// <summary>
        /// Queue the triggers of an object, including abilities granted to it by other permanents
        /// ("equipped creature has 'Whenever this creature attacks, ...'").
        /// </summary>
        private void QueueTriggers(CardInstance obj, TriggerEvent when, PlayerId? eventPlayer = null)
        {
            var def = Def(obj);
            foreach (var ability in def.Triggers)
                if (ability.When == when) QueueTrigger(ability, obj.Controller, obj.Id, def.Id, obj, eventPlayer: eventPlayer);
            if (obj.Zone != Zone.Battlefield) return;
            foreach (var granted in GrantedTriggers(obj))
                if (granted.When == when) QueueTrigger(granted, obj.Controller, obj.Id, def.Id, obj, eventPlayer: eventPlayer);
        }

        /// <summary>
        /// "Whenever ..." triggers that watch other objects, from every living player's Tavern Dweller and
        /// permanents. <paramref name="subject"/> is the player the event is about (see TriggerEvent).
        /// </summary>
        /// <param name="condition">Extra check per ability; gets the ability and its source.</param>
        /// <param name="amount">The event's amount (Gold banked or spent, damage dealt), for "that much".</param>
        /// <param name="eventObject">The object the event is about (the creature dealt damage, healed or entering).</param>
        /// <param name="eventPlayer">The player the event is about.</param>
        private void QueueWatcherTriggers(TriggerEvent when, PlayerId subject,
            System.Func<TriggeredAbility, CardInstance, bool> condition = null,
            int amount = 0, ObjectId eventObject = default, PlayerId? eventPlayer = null)
        {
            foreach (var p in S.Players)
            {
                if (p.HasLost) continue;
                foreach (var source in p.TavernDwellerZone) QueueWatcher(source, when, subject, condition, amount, eventObject, eventPlayer);
                foreach (var source in new List<CardInstance>(p.Battlefield))
                    QueueWatcher(source, when, subject, condition, amount, eventObject, eventPlayer);
            }
        }

        /// <summary>"Whenever you spend Gold": once per payment (decided 2026-10-09), with the amount paid.</summary>
        private void GoldSpent(PlayerId player, int amount) =>
            QueueWatcherTriggers(TriggerEvent.GoldSpent, player, (t, _) => amount >= t.MinAmount, amount);

        private void QueueWatcher(CardInstance source, TriggerEvent when, PlayerId subject,
            System.Func<TriggeredAbility, CardInstance, bool> condition, int amount, ObjectId eventObject, PlayerId? eventPlayer)
        {
            var def = Def(source);
            for (int i = 0; i < def.Triggers.Count; i++)
            {
                var ability = def.Triggers[i];
                if (ability.When != when) continue;
                if (ability.Subject == TriggerSubject.You && subject != source.Controller) continue;
                if (ability.Subject == TriggerSubject.Opponents && !S.AreOpponents(source.Controller, subject)) continue;
                if (condition != null && !condition(ability, source)) continue;
                if (ability.MaxPerTurn > 0)
                {
                    string key = "trigger:" + source.Id.Value + ":" + i;
                    S.UsesThisTurn.TryGetValue(key, out int used);
                    if (used >= ability.MaxPerTurn) continue;
                    S.UsesThisTurn[key] = used + 1;
                }
                QueueTrigger(ability, source.Controller, source.Id, def.Id, source, amount, eventObject, eventPlayer);
            }
        }

        private void QueueTrigger(TriggeredAbility ability, PlayerId controller, ObjectId sourceId, string sourceDefinitionId, CardInstance source,
            int amount = 0, ObjectId eventObject = default, PlayerId? eventPlayer = null)
        {
            if (ability.Condition != null && !ability.Condition.Holds(S, Db, controller, sourceId)) return; // intervening "if" (MTG 603.4)
            int times = ability.RepeatCount != null && source != null ? ability.RepeatCount.Of(S, Db, source) : 1;
            for (int n = 0; n < times; n++)
            {
                S.PendingTriggers.Add(new PendingTrigger
                {
                    Ability = ability,
                    Controller = controller,
                    SourceId = sourceId,
                    SourceDefinitionId = sourceDefinitionId,
                    Amount = amount,
                    EventObject = eventObject,
                    EventPlayer = eventPlayer,
                });
            }
        }

        private void QueueTurnTriggers(TriggerEvent when)
        {
            foreach (var c in new List<CardInstance>(S.ActivePlayerState.TavernDwellerZone)) QueueTriggers(c, when);
            foreach (var c in new List<CardInstance>(S.ActivePlayerState.Battlefield)) QueueTriggers(c, when);

            if (when == TriggerEvent.EndOfYourTurn)
            {
                // Delayed triggers "at the end of your turn" (MTG 603.7): they trigger once, then they're gone.
                foreach (var d in new List<DelayedTrigger>(S.DelayedTriggers))
                {
                    if (d.Controller != S.ActivePlayer) continue;
                    S.DelayedTriggers.Remove(d);
                    QueueTrigger(d.Ability, d.Controller, d.SourceId, d.SourceDefinitionId, null, eventObject: d.EventObject);
                }
                return;
            }
            if (when != TriggerEvent.StartOfYourTurn) return;

            // "At the start of that player's turn" on Curses attached to the active player (Curse of Rot), and
            // "at the start of its controller's turn" on Curses attached to the active player's creatures (Hex of Withering).
            var active = S.ActivePlayer;
            foreach (var p in S.Players)
            {
                if (p.HasLost) continue;
                foreach (var c in new List<CardInstance>(p.Battlefield))
                {
                    var def = Def(c);
                    if (c.AttachedToPlayer == active)
                    {
                        foreach (var ability in def.Triggers)
                            if (ability.When == TriggerEvent.StartOfEnchantedPlayersTurn)
                                QueueTrigger(ability, c.Controller, c.Id, def.Id, c, eventPlayer: active);
                    }
                    else if (def.Type == CardType.Curse && !c.AttachedToObject.IsNone)
                    {
                        var enchanted = S.FindOnBattlefield(c.AttachedToObject);
                        if (enchanted == null || enchanted.Controller != active) continue;
                        foreach (var ability in def.Triggers)
                            if (ability.When == TriggerEvent.StartOfEnchantedCreatureControllersTurn)
                                QueueTrigger(ability, c.Controller, c.Id, def.Id, c, eventObject: enchanted.Id, eventPlayer: active);
                    }
                }
            }
        }

        /// <summary>
        /// Puts waiting triggers on the Chain in APNAP order (MTG 603.3b): the active player's go
        /// on first, so they resolve last. A player with two or more different triggers waiting picks
        /// the order (decided 2026-10-09). Returns false if it has to wait for a choice.
        /// </summary>
        private bool PutPendingTriggersOnChain()
        {
            while (S.PendingTriggers.Count > 0 && S.Pending == null)
            {
                int pick = 0;
                int bestDistance = int.MaxValue;
                for (int i = 0; i < S.PendingTriggers.Count; i++)
                {
                    int d = TurnOrderDistance(S.ActivePlayer, S.PendingTriggers[i].Controller);
                    if (d < bestDistance) { bestDistance = d; pick = i; }
                }
                var controller = S.PendingTriggers[pick].Controller;
                if (!S.GetPlayer(controller).HasLost && TriggerOrderOptions(controller).Count > 1)
                {
                    S.Pending = new PendingDecision { Kind = DecisionKind.OrderTriggers, Player = controller };
                    return false;
                }
                if (!PutTriggerOnChain(pick)) return false;
            }
            return true;
        }

        /// <summary>
        /// The triggers <paramref name="controller"/> can put on the Chain next, as indexes into
        /// GameState.PendingTriggers. Triggers of the same ability of the same card are listed once (like MTG Arena),
        /// even if they're about different events (Skabba: three Goobers died).
        /// </summary>
        internal List<int> TriggerOrderOptions(PlayerId controller)
        {
            var result = new List<int>();
            for (int i = 0; i < S.PendingTriggers.Count; i++)
            {
                var t = S.PendingTriggers[i];
                if (t.Controller != controller) continue;
                bool seen = false;
                foreach (int j in result)
                    if (SameTrigger(S.PendingTriggers[j], t)) { seen = true; break; }
                if (!seen) result.Add(i);
            }
            return result;
        }

        private static bool SameTrigger(PendingTrigger a, PendingTrigger b) =>
            ReferenceEquals(a.Ability, b.Ability) && a.SourceDefinitionId == b.SourceDefinitionId;

        private void AnswerTriggerOrder(int index)
        {
            S.Pending = null;
            if (PutTriggerOnChain(index)) GivePriority(S.ResumePriorityTo ?? S.ActivePlayer);
        }

        /// <summary>Puts one waiting trigger on the Chain. Returns false if it has to wait for a target choice.</summary>
        private bool PutTriggerOnChain(int index)
        {
            var trigger = S.PendingTriggers[index];
            S.PendingTriggers.RemoveAt(index);

            if (S.GetPlayer(trigger.Controller).HasLost) return true;

            if (trigger.Ability.Target != TargetSpec.None)
            {
                var targets = EnumerateTargets(trigger.Controller, trigger.Ability.Slot,
                    trigger.Ability.TargetNotSelf ? trigger.SourceId : ObjectId.None);
                if (targets.Count == 0) return true; // MTG 603.3d: no legal target → removed
                if (targets.Count > 1 || trigger.Ability.TargetOptional)
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
                return true;
            }

            PushTrigger(trigger, null);
            return true;
        }

        /// <summary>Null: an optional trigger ("you may") was declined, so it does nothing.</summary>
        private void ChooseTriggerTarget(Target? target)
        {
            var trigger = S.Pending.Trigger;
            S.Pending = null;
            if (target.HasValue) PushTrigger(trigger, target);
            GivePriority(S.ResumePriorityTo ?? S.ActivePlayer);
        }

        private void PushTrigger(PendingTrigger t, Target? target)
        {
            var item = new ChainItem
            {
                Id = S.NextChainId++,
                ObjectId = new ObjectId(S.NextObjectId++),
                Kind = ChainItemKind.TriggeredAbility,
                Controller = t.Controller,
                SourceId = t.SourceId,
                SourceDefinitionId = t.SourceDefinitionId,
                Effects = t.Ability.Effects,
                TargetsExcludeSource = t.Ability.TargetNotSelf,
                EventAmount = t.Amount,
                EventObject = t.EventObject,
                EventPlayer = t.EventPlayer,
                Condition = t.Ability.Condition,
            };
            if (t.Ability.Target != TargetSpec.None) item.TargetSlots.Add(t.Ability.Slot);
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
        internal List<Target> EnumerateTargets(PlayerId controller, TargetSpec spec, ObjectId exclude = default) =>
            EnumerateTargets(controller, TargetSlot.Of(spec), exclude);

        internal List<Target> EnumerateTargets(PlayerId controller, TargetSlot slot, ObjectId exclude = default)
        {
            var result = new List<Target>();
            var spec = slot.Spec;
            if (spec == TargetSpec.None) return result;

            if (spec == TargetSpec.CreatureCardInYourGraveyard || spec == TargetSpec.CreatureCardInAGraveyard)
            {
                foreach (var p in S.LivingPlayersFrom(controller))
                {
                    if (spec == TargetSpec.CreatureCardInYourGraveyard && p.Id != controller) continue;
                    foreach (var c in p.Graveyard)
                        if (Def(c).IsCreature && c.Id != exclude && (!slot.MaxCost.HasValue || Def(c).Cost <= slot.MaxCost.Value))
                            result.Add(Target.ForObject(c.Id));
                }
                return result;
            }

            if (spec == TargetSpec.EquipmentRelicOrCurse)
            {
                foreach (var p in S.LivingPlayersFrom(controller))
                    foreach (var c in p.Battlefield)
                    {
                        var type = Def(c).Type;
                        if (c.Id != exclude && (type == CardType.Equipment || type == CardType.Relic || type == CardType.Curse))
                            result.Add(Target.ForObject(c.Id));
                    }
                return result;
            }

            if (spec == TargetSpec.SpellOnChain || spec == TargetSpec.SpellOrAbilityOnChain)
            {
                foreach (var item in S.Chain)
                {
                    if (item.ObjectId == exclude) continue;
                    if (item.Kind != ChainItemKind.Spell && spec == TargetSpec.SpellOnChain) continue;
                    if (slot.MaxCost.HasValue && (item.Card == null || Def(item.Card).Cost > slot.MaxCost.Value)) continue;
                    result.Add(Target.ForObject(item.ObjectId));
                }
                return result;
            }

            if (spec == TargetSpec.EquipmentYouControl)
            {
                foreach (var c in S.GetPlayer(controller).Battlefield)
                    if (Def(c).Type == CardType.Equipment && c.Id != exclude) result.Add(Target.ForObject(c.Id));
                return result;
            }

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
                    var def = Def(c);
                    if (!def.IsCreature || c.Id == exclude) continue;
                    if (slot.Subtype != null && !def.HasSubtype(slot.Subtype)) continue;
                    if (slot.Damaged && c.Damage <= 0) continue;
                    if (slot.MaxCost.HasValue && def.Cost > slot.MaxCost.Value) continue;
                    if (slot.Keyword != Keyword.None && !Stats(c).Has(slot.Keyword)) continue;
                    if (slot.MaxRemainingHealth.HasValue && Stats(c).RemainingHealth > slot.MaxRemainingHealth.Value) continue;
                    if (slot.AttackingOrBlocking && (S.Combat == null || !(S.Combat.IsAttacking(c.Id) || S.Combat.IsBlocking(c.Id)))) continue;
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
                        case TargetSpec.ConstructOrEquippedCreature:
                            ok = def.HasSubtype("Construct") || CharacteristicsCalculator.IsEquipped(S, Db, c); break;
                        default:
                            ok = false; break;
                    }
                    if (ok) result.Add(Target.ForObject(c.Id));
                }
            }
            return result;
        }

        private bool IsLegalTarget(PlayerId controller, TargetSlot slot, Target target, ObjectId exclude = default) =>
            EnumerateTargets(controller, slot, exclude).Contains(target);

        /// <summary>
        /// Every way to fill a spell's target slots (distinct targets; optional slots may stay empty).
        /// Slots with the same spec are filled in enumeration order, so {A,B} and {B,A} aren't both listed.
        /// No slots: one empty choice. A required slot without candidates: no choices.
        /// </summary>
        internal List<Target[]> EnumerateTargetChoices(PlayerId controller, List<TargetSlot> slots, ObjectId exclude = default)
        {
            var result = new List<Target[]>();
            var chosen = new List<Target>();
            var candidates = new List<List<Target>>();
            foreach (var slot in slots) candidates.Add(EnumerateTargets(controller, slot, exclude));
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
                bool sameAsPrevious = slot > 0 && slots[slot].SameFilterAs(slots[slot - 1]);
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
