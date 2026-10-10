using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// Replacement effects (MTG 614–616): "If [event] would happen, [something else] instead". The game actions
    /// (MoveCard, Draw, DealDamage, Heal, ChangeGold, entering the battlefield) build a <see cref="ReplaceableEvent"/>,
    /// let every applicable replacement change it, then do what's left.
    /// </summary>
    internal sealed partial class GameRunner
    {
        private struct ReplacementSource
        {
            public ReplacementAbility Ability;
            public PlayerId Controller;
            public ObjectId Source;
            public string SourceDefinitionId;
            public CardInstance SourceObject;
            public ActiveReplacement Active;
            public long Timestamp;
        }

        /// <summary>
        /// Applies the replacement effects that apply to <paramref name="e"/>. Order (MTG 616.1): self-replacement effects
        /// first, then the others oldest first. ❗ MTG lets the affected player choose that order; the engine uses this fixed
        /// order because game actions can't pause for a decision yet. Each replacement applies at most once to an event
        /// (MTG 614.5), and after each one the rest are checked again: "exile it instead" means it no longer dies, so
        /// "if it would die" effects stop applying (MTG 616.1d–e).
        /// </summary>
        private void ApplyReplacements(ReplaceableEvent e)
        {
            var all = CollectReplacements();
            if (all == null || all.Count == 0) return;
            var used = new bool[all.Count];
            while (!e.Skipped)
            {
                int best = -1;
                for (int i = 0; i < all.Count; i++)
                {
                    if (used[i] || !Applies(all[i], e)) continue;
                    if (best < 0 || Before(all[i], all[best])) best = i;
                }
                if (best < 0) return;
                used[best] = true;
                Apply(all[best], e);
            }

            bool Before(ReplacementSource a, ReplacementSource b) =>
                a.Ability.SelfReplacement != b.Ability.SelfReplacement ? a.Ability.SelfReplacement : a.Timestamp < b.Timestamp;
        }

        private List<ReplacementSource> CollectReplacements()
        {
            List<ReplacementSource> result = null;
            foreach (var p in S.Players)
            {
                foreach (var list in new[] { p.TavernDwellerZone, p.Battlefield })
                    foreach (var c in list)
                    {
                        var def = Def(c);
                        if (!def.HasReplacement) continue;
                        if (result == null) result = new List<ReplacementSource>();
                        foreach (var st in def.Statics)
                            if (st is ReplacementAbility r)
                                result.Add(new ReplacementSource
                                {
                                    Ability = r, Controller = c.Controller, Source = c.Id, SourceDefinitionId = def.Id, SourceObject = c,
                                    Timestamp = c.Timestamp,
                                });
                    }
            }
            if (S.Replacements.Count == 0) return result;
            S.Replacements.RemoveAll(a => a.Ability.OnlyChosenObject && S.FindOnBattlefield(a.AffectedObject) == null);
            if (result == null) result = new List<ReplacementSource>();
            foreach (var a in S.Replacements)
                result.Add(new ReplacementSource
                {
                    Ability = a.Ability, Controller = a.Controller, Source = a.Source, SourceDefinitionId = a.SourceDefinitionId,
                    SourceObject = S.FindOnBattlefield(a.Source), Active = a, Timestamp = a.Timestamp,
                });
            return result;
        }

        private bool Applies(ReplacementSource r, ReplaceableEvent e)
        {
            var a = r.Ability;
            if (a.Event != e.Kind) return false;
            if (e.Kind == ReplacementEvent.Dies && !e.StillDies) return false;
            if (!Matches(a.Affects, r.Controller, e.AffectedPlayer)) return false;

            var obj = e.Object;
            if (a.OnlySelf && (obj == null || obj.Id != r.Source)) return false;
            if (a.OnlyAttachedCreature && (obj == null || r.SourceObject == null || r.SourceObject.AttachedToObject != obj.Id)) return false;
            if (a.OnlyChosenObject && (obj == null || r.Active == null || r.Active.AffectedObject != obj.Id)) return false;
            if (a.Subtype != null && (obj == null || !Def(obj).HasSubtype(a.Subtype))) return false;
            if (a.MinHealth > 0 && (obj == null || !Def(obj).IsCreature || Stats(obj).MaxHealth < a.MinHealth)) return false;

            if (e.Kind == ReplacementEvent.DamageDealt)
            {
                if (a.ToCreatures.HasValue && a.ToCreatures.Value != (obj != null)) return false;
                if (a.OnlyCombat && !e.IsCombat) return false;
                if (a.SourceControlledBy != TriggerSubject.Anyone
                    && (!e.DamageSourceController.HasValue || !Matches(a.SourceControlledBy, r.Controller, e.DamageSourceController.Value)))
                    return false;
            }
            return true;
        }

        private bool Matches(TriggerSubject subject, PlayerId controller, PlayerId player)
        {
            switch (subject)
            {
                case TriggerSubject.You: return player == controller;
                case TriggerSubject.Opponents: return S.AreOpponents(controller, player);
                default: return true;
            }
        }

        private void Apply(ReplacementSource r, ReplaceableEvent e)
        {
            var a = r.Ability;
            switch (e.Kind)
            {
                case ReplacementEvent.Dies:
                    if (a.Destination.HasValue)
                    {
                        e.Destination = a.Destination.Value;
                        e.ToBottom = a.ToBottom;
                    }
                    break;
                case ReplacementEvent.Enters:
                    e.Counters += a.Counters;
                    e.EntersTapped |= a.EntersTapped;
                    break;
                default:
                    int amount = a.PreventAll ? 0 : System.Math.Max(0, e.Amount - a.Prevent);
                    if (amount > 0) amount = amount * a.Multiply + a.Add;
                    e.Amount = System.Math.Max(0, amount);
                    break;
            }
            // Skipping a death would leave a lethally damaged creature for the state-based actions to find again.
            if (a.Skip && e.Kind != ReplacementEvent.Dies) e.Skipped = true;
            if (a.Instead.Count > 0) e.Instead.Add((a.Instead, r.Controller, r.Source, r.SourceDefinitionId));
            Emit(new ReplacedEvent { Kind = e.Kind, SourceDefinitionId = r.SourceDefinitionId, AffectedPlayer = e.AffectedPlayer });

            if (r.Active != null && r.Active.UsesLeft > 0 && --r.Active.UsesLeft == 0) S.Replacements.Remove(r.Active);
        }

        /// <summary>The "instead" effects of the replacements that applied. They happen at once, not on the Chain.</summary>
        private void RunInsteadEffects(ReplaceableEvent e)
        {
            foreach (var (effects, controller, source, sourceDefinitionId) in e.Instead)
                RunEffects(effects, controller, source, new List<Target?>(), eventObject: e.Object?.Id ?? ObjectId.None,
                    eventPlayer: e.AffectedPlayer, sourceDefinitionId: sourceDefinitionId);
        }

        /// <summary>A permanent just entered: "enters with N +1/+1 counters" / "enters tapped" (MTG 614.1c, 614.12).</summary>
        private void ApplyEntersReplacements(CardInstance permanent)
        {
            var e = new ReplaceableEvent { Kind = ReplacementEvent.Enters, AffectedPlayer = permanent.Controller, Object = permanent };
            ApplyReplacements(e);
            permanent.PlusOneCounters += e.Counters;
            if (e.EntersTapped) permanent.Tapped = true;
            RunInsteadEffects(e);
        }

        /// <summary>A replacement created by a resolving effect (<see cref="AddReplacementEffect"/>).</summary>
        internal void AddReplacement(ReplacementAbility ability, PlayerId controller, ObjectId source, string sourceDefinitionId,
            ObjectId affectedObject, bool untilEndOfTurn, int uses)
        {
            S.Replacements.Add(new ActiveReplacement
            {
                Ability = ability, Controller = controller, Source = source, SourceDefinitionId = sourceDefinitionId,
                AffectedObject = affectedObject, UntilEndOfTurn = untilEndOfTurn, UsesLeft = uses, Timestamp = S.NextTimestamp++,
            });
        }
    }
}
