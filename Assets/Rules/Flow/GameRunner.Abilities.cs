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
                        var granted = st.GrantedTriggers(S, source, obj);
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
            foreach (var source in p.Battlefield) AddAbilitiesOf(p, source, sorcerySpeed, result);
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

                foreach (var targets in choices)
                {
                    if (ab.TargetsAllowed != null && !ab.TargetsAllowed(S, source, targets)) continue;
                    foreach (var sacrifice in sacrifices)
                    {
                        if (!sacrifice.IsNone && System.Array.IndexOf(targets, Target.ForObject(sacrifice)) >= 0) continue;
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
            return !source.SummoningSick || Stats(source).Has(Keyword.Haste);
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

        private void AnswerOption(PlayerAction a)
        {
            var decision = S.Pending;
            S.Pending = null;
            if (decision.Kind == DecisionKind.TopOrBottom && a.Option == 1)
            {
                var card = S.GetPlayer(a.Player).Deck.Find(c => c.Id == decision.Card);
                if (card != null) MoveCard(card, Zone.Deck, toBottom: true);
            }
            GivePriority(S.ResumePriorityTo ?? S.ActivePlayer);
        }
    }
}
