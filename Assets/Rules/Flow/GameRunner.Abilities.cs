using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// Activated abilities, Equip and Tavern Dweller Powers (MTG 602, GAME_DESIGN §5.2, §7.4, §9.1, §10).
    /// </summary>
    internal sealed partial class GameRunner
    {
        /// <summary>Most choices listed for one "choose up to X" ability (Snik), to keep the action list small.</summary>
        private const int MaxXChoices = 256;

        private static readonly List<TriggeredAbility> NoTriggers = new List<TriggeredAbility>();

        /// <summary>
        /// The object's activated abilities: printed ones first, then ones granted by other
        /// permanents (Overclock Rig: equipped creature has "Tap: ..."). PlayerAction.AbilityIndex
        /// indexes this list.
        /// </summary>
        internal List<ActivatedAbility> AbilitiesOf(CardInstance obj)
        {
            var own = Def(obj).Abilities;
            if (obj.Zone != Zone.Battlefield) return own;
            List<ActivatedAbility> all = null;
            foreach (var p in S.Players)
            {
                foreach (var source in p.Battlefield)
                {
                    foreach (var st in Def(source).Statics)
                    {
                        var granted = st.GrantedAbilities(S, source, obj);
                        if (granted == null) continue;
                        if (all == null) all = new List<ActivatedAbility>(own);
                        all.AddRange(granted);
                    }
                }
            }
            return all ?? own;
        }

        /// <summary>Triggered abilities granted to a permanent by other permanents.</summary>
        private List<TriggeredAbility> GrantedTriggers(CardInstance obj)
        {
            List<TriggeredAbility> all = null;
            foreach (var p in S.Players)
            {
                foreach (var source in p.Battlefield)
                {
                    foreach (var st in Def(source).Statics)
                    {
                        var granted = st.GrantedTriggers(S, Db, source, obj);
                        if (granted == null) continue;
                        if (all == null) all = new List<TriggeredAbility>();
                        all.AddRange(granted);
                    }
                }
            }
            return all ?? NoTriggers;
        }

        private static string UseKey(CardInstance source, int abilityIndex) => "ability:" + source.Id.Value + ":" + abilityIndex;

        private bool UsedThisTurn(CardInstance source, int abilityIndex) => S.UsesThisTurn.ContainsKey(UseKey(source, abilityIndex));

        // ------------------------------------------------------------------ legal actions

        private void AddActivatableAbilities(PlayerId player, List<PlayerAction> result, bool sorcerySpeed)
        {
            var p = S.GetPlayer(player);
            foreach (var source in p.TavernDwellerZone) AddAbilitiesOf(p, source, sorcerySpeed, result);
            foreach (var source in p.Battlefield)
            {
                if (result.Count >= _stopAfter) return;
                AddAbilitiesOf(p, source, sorcerySpeed, result);
            }
        }

        private void AddAbilitiesOf(PlayerState p, CardInstance source, bool sorcerySpeed, List<PlayerAction> result)
        {
            var abilities = AbilitiesOf(source);
            for (int i = 0; i < abilities.Count; i++)
            {
                var ab = abilities[i];
                if (ab.IsSorcerySpeed && !sorcerySpeed) continue;
                if (ab.LimitedPerTurn && UsedThisTurn(source, i)) continue;
                if (ab.TapCost && !CanPayTap(source)) continue;
                if (ab.LifeCost > p.Life) continue;

                var sacrifices = new List<ObjectId>();
                if (ab.SacrificeCreatureCost)
                {
                    foreach (var c in p.Battlefield)
                        if (Def(c).IsCreature) sacrifices.Add(c.Id);
                    if (sacrifices.Count == 0) continue;
                }
                else
                {
                    sacrifices.Add(ObjectId.None);
                }

                int generic = Costs.AbilityCost(S, Db, p.Id, ab);
                var exclude = ab.TargetsExcludeSource ? source.Id : ObjectId.None;
                List<Target[]> choices;
                if (ab.XTargets != null)
                {
                    choices = EnumerateXTargetChoices(p, ab, generic, exclude);
                }
                else
                {
                    if (Payment.GoldNeeded(p, generic, true, ab.GoldCost) < 0) continue;
                    choices = EnumerateTargetChoices(p.Id, ab.Targets, exclude);
                }

                // "(X): ..." (Rampaging Titan): one action per X from 1 up to what can be paid.
                int maxX = 0;
                if (ab.HasX && ab.XTargets == null)
                    while (Payment.GoldNeeded(p, generic + maxX + 1, true, ab.GoldCost) >= 0) maxX++;
                foreach (var targets in choices)
                {
                    if (ab.TargetsAllowed != null && !ab.TargetsAllowed(S, source, targets)) continue;
                    foreach (var sacrifice in sacrifices)
                    {
                        if (!sacrifice.IsNone && System.Array.IndexOf(targets, Target.ForObject(sacrifice)) >= 0) continue;
                        if (ab.HasX && ab.XTargets == null)
                        {
                            for (int x = 1; x <= maxX; x++) result.Add(PlayerAction.Activate(p.Id, source.Id, i, targets, x, sacrifice));
                            continue;
                        }
                        result.Add(PlayerAction.Activate(p.Id, source.Id, i, targets, ab.XTargets != null ? targets.Length : 0, sacrifice));
                    }
                }
            }
        }

        /// <summary>§7.4: creatures can't pay a Tap cost the turn they arrive, unless they have Haste. Other permanents can.</summary>
        private bool CanPayTap(CardInstance source)
        {
            if (source.Zone != Zone.Battlefield || source.Tapped) return false;
            if (!Def(source).IsCreature) return true;
            return !source.SummoningSick || S.Format.NoSummoningSickness || Stats(source).Has(Keyword.Haste);
        }

        /// <summary>
        /// "Choose up to X ..." (Snik): X = the number chosen, at least 1, and it must be payable.
        /// The effect only cares about which cards are chosen, so objects with the same definition
        /// are interchangeable: choices are listed per definition (the first ones in battlefield order).
        /// </summary>
        private List<Target[]> EnumerateXTargetChoices(PlayerState p, ActivatedAbility ab, int generic, ObjectId exclude)
        {
            var groups = new List<List<Target>>();
            var groupDefs = new List<string>();
            foreach (var t in EnumerateTargets(p.Id, ab.XTargets, exclude))
            {
                string defId = S.FindObject(t.Object)?.DefinitionId;
                int g = groupDefs.IndexOf(defId);
                if (g < 0) { groupDefs.Add(defId); groups.Add(new List<Target>()); g = groups.Count - 1; }
                groups[g].Add(t);
            }

            var result = new List<Target[]>();
            var chosen = new List<Target>();
            Fill(0);
            return result;

            void Fill(int group)
            {
                if (result.Count >= MaxXChoices) return;
                if (group == groups.Count)
                {
                    if (chosen.Count > 0 && Payment.GoldNeeded(p, generic + chosen.Count, true, ab.GoldCost) >= 0)
                        result.Add(chosen.ToArray());
                    return;
                }
                int before = chosen.Count;
                for (int k = 0; k <= groups[group].Count; k++)
                {
                    if (k > 0) chosen.Add(groups[group][k - 1]);
                    Fill(group + 1);
                }
                chosen.RemoveRange(before, chosen.Count - before);
            }
        }

        // ------------------------------------------------------------------ activation

        /// <summary>
        /// MTG 602.2: pay the costs and put the ability on the Chain. The generic cost (plus X) is
        /// paid with mana first, then Gold; "Pay N Gold" only with Gold (§5.2). The player keeps
        /// priority afterwards (MTG 117.3c).
        /// </summary>
        private void Activate(PlayerAction a)
        {
            var p = S.GetPlayer(a.Player);
            var source = S.FindObject(a.Card);
            var def = Def(source);
            var ab = AbilitiesOf(source)[a.AbilityIndex];

            if (ab.LimitedPerTurn) S.UsesThisTurn[UseKey(source, a.AbilityIndex)] = 1;
            if (ab.TapCost) source.Tapped = true;

            int generic = Costs.AbilityCost(S, Db, a.Player, ab) + (ab.HasX ? a.X : 0);
            int gold = Payment.GoldNeeded(p, generic, true, ab.GoldCost);
            int mana = generic - (gold - ab.GoldCost);
            p.Mana -= mana;
            if (mana > 0) Emit(new ManaChangedEvent { Player = p.Id, Mana = p.Mana, MaxMana = p.MaxMana });
            if (gold > 0) ChangeGold(p.Id, -gold);
            if (ab.LifeCost > 0) ChangeLife(p.Id, -ab.LifeCost);
            if (!a.Sacrifice.IsNone) MoveCard(S.FindOnBattlefield(a.Sacrifice), Zone.Graveyard);

            var item = new ChainItem
            {
                Id = S.NextChainId++,
                ObjectId = new ObjectId(S.NextObjectId++),
                Kind = ChainItemKind.ActivatedAbility,
                Controller = a.Player,
                SourceId = source.Id,
                SourceDefinitionId = def.Id,
                TargetsExcludeSource = ab.TargetsExcludeSource,
                Effects = ab.Effects,
                X = a.X,
                Text = ab.Text,
                IsTavernDwellerPower = ab.IsTavernDwellerPower,
            };
            if (ab.XTargets != null)
                for (int i = 0; i < a.Targets.Length; i++) item.TargetSlots.Add(ab.XTargets);
            else
                item.TargetSlots = ab.Targets;
            item.Targets.AddRange(a.Targets);
            S.Chain.Add(item);

            Emit(new AbilityActivatedEvent
            {
                Player = a.Player, Source = source.Id, SourceDefinitionId = def.Id, Text = ab.Text,
                IsTavernDwellerPower = ab.IsTavernDwellerPower, Targets = a.Targets, X = a.X, ManaPaid = mana, GoldPaid = gold,
            });
            if (ab.IsEquip) QueueWatcherTriggers(TriggerEvent.EquipActivated, a.Player);
            if (gold > 0) GoldSpent(a.Player, gold);

            GivePriority(a.Player);
        }

        // ------------------------------------------------------------------ choices during resolution

        /// <summary>
        /// "Look at the top card of your deck. You may put it on the bottom." (Auditor Prime).
        /// Asks after the effect resolves; it must be the last effect of its ability.
        /// </summary>
        internal void AskTopOrBottom(PlayerId player)
        {
            var p = S.GetPlayer(player);
            if (p.Deck.Count == 0) return;
            S.Pending = new PendingDecision { Kind = DecisionKind.TopOrBottom, Player = player, Card = p.Deck[0].Id };
        }

        /// <summary>
        /// "Discard N cards" during resolution (Settle the Tab). The player picks; with fewer cards in
        /// hand they discard what they have. Must be the last effect of its spell or ability.
        /// </summary>
        internal void AskDiscard(PlayerId player, int count)
        {
            int n = System.Math.Min(count, S.GetPlayer(player).Hand.Count);
            if (n > 0) S.Pending = new PendingDecision { Kind = DecisionKind.DiscardCards, Player = player, Count = n };
        }

        private void AnswerDiscard(PlayerAction a)
        {
            var p = S.GetPlayer(a.Player);
            MoveCard(p.Hand.Find(c => c.Id == a.Card), Zone.Graveyard);
            if (--S.Pending.Count > 0) return;
            S.Pending = null;
            GivePriority(S.ResumePriorityTo ?? S.ActivePlayer);
        }

        /// <summary>
        /// "Look at the top N cards of your deck. Put one into your hand and the rest into your graveyard."
        /// (Grave Gossip). With one card there's nothing to choose. Must be the last effect.
        /// </summary>
        internal void AskChooseFromTop(PlayerId player, int count, bool restToBottom = false)
        {
            var deck = S.GetPlayer(player).Deck;
            int n = System.Math.Min(count, deck.Count);
            if (n == 0) return;
            if (n == 1)
            {
                MoveCard(deck[0], Zone.Hand);
                return;
            }
            S.Pending = new PendingDecision { Kind = DecisionKind.ChooseFromTop, Player = player, Count = n, RestToBottom = restToBottom };
        }

        private void AnswerChooseFromTop(PlayerAction a, PendingDecision decision)
        {
            var deck = S.GetPlayer(a.Player).Deck;
            var top = deck.GetRange(0, System.Math.Min(decision.Count, deck.Count));
            for (int i = 0; i < top.Count; i++)
            {
                if (i == a.Option) MoveCard(top[i], Zone.Hand);
                else if (decision.RestToBottom) MoveCard(top[i], Zone.Deck, toBottom: true);
                else MoveCard(top[i], Zone.Graveyard);
            }
        }

        /// <summary>
        /// Dice Game: "Each player may pay any amount of Gold. The player who paid the most draws two cards.
        /// If players tie for the most, each of them draws one card." Choices go in turn order from the
        /// active player, in the open (MTG 101.4). Players with no Gold pay 0 without being asked.
        /// </summary>
        internal void StartGoldAuction()
        {
            S.Pending = new PendingDecision { Kind = DecisionKind.PayAnyGold, Bidders = new List<PlayerId>(), Bids = new List<int>() };
            NextBidder();
        }

        private void NextBidder()
        {
            var d = S.Pending;
            foreach (var p in S.LivingPlayersFrom(S.ActivePlayer))
            {
                if (d.Bidders.Contains(p.Id)) continue;
                if (p.Gold > 0)
                {
                    d.Player = p.Id;
                    return;
                }
                d.Bidders.Add(p.Id);
                d.Bids.Add(0);
            }

            S.Pending = null;
            int max = 0;
            foreach (var b in d.Bids) max = System.Math.Max(max, b);
            var top = new List<PlayerId>();
            for (int i = 0; i < d.Bids.Count; i++)
                if (d.Bids[i] == max) top.Add(d.Bidders[i]);
            foreach (var p in top) Draw(p, top.Count == 1 ? 2 : 1);
        }

        private void AnswerBid(PlayerAction a)
        {
            var d = S.Pending;
            if (a.Option > 0)
            {
                ChangeGold(a.Player, -a.Option);
                GoldSpent(a.Player, a.Option);
            }
            d.Bidders.Add(a.Player);
            d.Bids.Add(a.Option);
            NextBidder();
            if (S.Pending == null) GivePriority(S.ResumePriorityTo ?? S.ActivePlayer);
        }

        /// <summary>
        /// Ask <paramref name="chooser"/> to choose one of <paramref name="choices"/> (or nothing, if optional).
        /// Then <paramref name="then"/> runs about the chosen object, or <paramref name="otherwise"/> if they chose
        /// nothing. With no choices the "otherwise" effects run at once; a forced single choice is made at once.
        /// If another choice is already waiting, this one waits its turn (GameState.ChoiceQueue).
        /// </summary>
        internal void AskChoice(PlayerId chooser, List<ObjectId> choices, bool optional, List<Effect> then, List<Effect> otherwise,
            PlayerId controller, ObjectId source, string sourceDefinitionId, string prompt)
        {
            if (choices.Count == 0 || (choices.Count == 1 && !optional))
            {
                bool chosen = choices.Count == 1;
                RunChoiceEffects(chosen ? then : otherwise, controller, source, sourceDefinitionId, chosen ? choices[0] : ObjectId.None, chooser);
                return;
            }
            Enqueue(new PendingDecision
            {
                Kind = DecisionKind.ChooseObject, Player = chooser, Choices = choices, Optional = optional, Then = then, Else = otherwise,
                EffectController = controller, Source = source, SourceDefinitionId = sourceDefinitionId, Prompt = prompt,
            });
        }

        /// <summary>
        /// "Deal N damage divided as you choose among any number of creatures and/or opponents" (Arc Cascade): the chooser
        /// assigns the damage one point at a time (DecisionKind.DivideDamage), then it's all dealt at once.
        /// </summary>
        internal void AskDivideDamage(PlayerId chooser, ObjectId source, string sourceDefinitionId, int amount)
        {
            if (amount <= 0) return;
            var choices = new List<Target>();
            foreach (var p in S.LivingPlayersFrom(chooser))
            {
                if (S.AreOpponents(chooser, p.Id)) choices.Add(Target.ForPlayer(p.Id));
                foreach (var c in p.Battlefield)
                    if (Def(c).IsCreature) choices.Add(Target.ForObject(c.Id));
            }
            if (choices.Count == 0) return;
            Enqueue(new PendingDecision
            {
                Kind = DecisionKind.DivideDamage, Player = chooser, Count = amount, TargetChoices = choices, Assigned = new List<Target>(),
                Source = source, SourceDefinitionId = sourceDefinitionId, Prompt = "Deal 1 damage to",
            });
        }

        private void AnswerDividePoint(Target target)
        {
            var d = S.Pending;
            d.Assigned.Add(target);
            if (d.Assigned.Count < d.Count) return; // the next point
            S.Pending = null;
            var totals = new List<(Target target, int amount)>();
            foreach (var t in d.Assigned)
            {
                int i = totals.FindIndex(x => x.target == t);
                if (i < 0) totals.Add((t, 1));
                else totals[i] = (t, totals[i].amount + 1);
            }
            foreach (var (t, amount) in totals) DealDamage(d.Source, t, amount, false);
            if (S.Pending == null && !S.IsGameOver) GivePriority(S.ResumePriorityTo ?? S.ActivePlayer);
        }

        /// <summary>A yes/no question to <paramref name="chooser"/>: "yes" runs <paramref name="then"/>, "no" runs <paramref name="otherwise"/>.</summary>
        internal void AskYesNo(PlayerId chooser, List<Effect> then, List<Effect> otherwise, PlayerId controller, ObjectId source,
            string sourceDefinitionId, string prompt) =>
            Enqueue(new PendingDecision
            {
                Kind = DecisionKind.YesNo, Player = chooser, Then = then, Else = otherwise,
                EffectController = controller, Source = source, SourceDefinitionId = sourceDefinitionId, Prompt = prompt,
            });

        private void Enqueue(PendingDecision d)
        {
            if (S.Pending == null) S.Pending = d;
            else S.ChoiceQueue.Add(d);
        }

        private void RunChoiceEffects(List<Effect> effects, PlayerId controller, ObjectId source, string sourceDefinitionId,
            ObjectId chosen, PlayerId chooser)
        {
            if (effects == null || effects.Count == 0) return;
            RunEffects(effects, controller, source, new List<Target?>(), eventObject: chosen, eventPlayer: chooser,
                sourceDefinitionId: sourceDefinitionId);
        }

        private void AnswerChoice(Target? target)
        {
            var d = S.Pending;
            S.Pending = null;
            bool chosen = target.HasValue;
            RunChoiceEffects(chosen ? d.Then : d.Else, d.EffectController, d.Source, d.SourceDefinitionId,
                chosen ? target.Value.Object : ObjectId.None, d.Player);
            if (S.Pending == null && !S.IsGameOver) GivePriority(S.ResumePriorityTo ?? S.ActivePlayer);
        }

        private void AnswerOption(PlayerAction a)
        {
            if (S.Pending.Kind == DecisionKind.OrderTriggers)
            {
                AnswerTriggerOrder(a.Option);
                return;
            }
            if (S.Pending.Kind == DecisionKind.PayAnyGold)
            {
                AnswerBid(a);
                return;
            }
            if (S.Pending.Kind == DecisionKind.YesNo)
            {
                var d = S.Pending;
                S.Pending = null;
                RunChoiceEffects(a.Option == 1 ? d.Then : d.Else, d.EffectController, d.Source, d.SourceDefinitionId, ObjectId.None, d.Player);
                if (S.Pending == null && !S.IsGameOver) GivePriority(S.ResumePriorityTo ?? S.ActivePlayer);
                return;
            }
            var decision = S.Pending;
            S.Pending = null;
            if (decision.Kind == DecisionKind.ChooseFromTop) AnswerChooseFromTop(a, decision);
            if (decision.Kind == DecisionKind.TopOrBottom && a.Option == 1)
            {
                var card = S.GetPlayer(a.Player).Deck.Find(c => c.Id == decision.Card);
                if (card != null) MoveCard(card, Zone.Deck, toBottom: true);
            }
            if (decision.Kind == DecisionKind.PayTax)
            {
                if (a.Option == 1)
                {
                    PayGeneric(a.Player, decision.Count);
                    if (decision.RewardGold > 0) ChangeGold(decision.Beneficiary, decision.RewardGold);
                }
                else
                {
                    var item = S.FindOnChain(decision.Card);
                    if (item != null) Counter(item);
                }
            }
            GivePriority(S.ResumePriorityTo ?? S.ActivePlayer);
        }

        /// <summary>
        /// "Counter target spell unless its controller pays N" (Hush Money). A tax is paid with mana first,
        /// then Gold (§5.2). If they can't pay it's countered right away; otherwise they choose.
        /// Must be the last effect of its spell.
        /// </summary>
        internal void AskTax(ChainItem item, int amount, PlayerId beneficiary, int rewardGold)
        {
            if (Payment.GoldNeeded(S.GetPlayer(item.Controller), amount, true) < 0)
            {
                Counter(item);
                return;
            }
            S.Pending = new PendingDecision
            {
                Kind = DecisionKind.PayTax, Player = item.Controller, Count = amount, Card = item.ObjectId,
                Beneficiary = beneficiary, RewardGold = rewardGold,
            };
        }

        /// <summary>Pay a generic amount: mana first, then Gold (§5.2). Gold paid this way is spent Gold.</summary>
        private void PayGeneric(PlayerId player, int amount)
        {
            var p = S.GetPlayer(player);
            int gold = Payment.GoldNeeded(p, amount, true);
            int mana = amount - gold;
            p.Mana -= mana;
            if (mana > 0) Emit(new ManaChangedEvent { Player = p.Id, Mana = p.Mana, MaxMana = p.MaxMana });
            if (gold > 0)
            {
                ChangeGold(p.Id, -gold);
                GoldSpent(p.Id, gold);
            }
        }
    }
}
