using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// "Deal N damage to each creature" (Bar Brawl), "to each other creature" (Chaos Engine), or "to each
    /// creature that player controls" (Curse of Rot, with <see cref="OnlyEventPlayer"/>). Simultaneous:
    /// state-based actions only run after the whole spell or ability.
    /// </summary>
    public sealed class DealDamageToEachCreatureEffect : Effect
    {
        public int Amount { get; set; }
        /// <summary>"each other creature": not the source.</summary>
        public bool ExcludeSource { get; set; }
        /// <summary>Only creatures controlled by the player the trigger is about (EffectContext.EventPlayer).</summary>
        public bool OnlyEventPlayer { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var victims = new List<CardInstance>();
            foreach (var p in ctx.State.Players)
            {
                if (p.HasLost || (OnlyEventPlayer && p.Id != ctx.EventPlayer)) continue;
                foreach (var c in p.Battlefield)
                    if (ctx.Cards.Get(c.DefinitionId).IsCreature && !(ExcludeSource && c.Id == ctx.Source)) victims.Add(c);
            }
            foreach (var c in victims) ctx.DealDamage(Target.ForObject(c.Id), Amount);
        }
    }

    /// <summary>
    /// "Deal N damage to target creature. Then deal N damage to each other creature that already had
    /// damage." (Smart Rounds). "Already had damage" means before this spell.
    /// </summary>
    public sealed class DamageTargetThenEachDamagedEffect : Effect
    {
        public int Amount { get; set; } = 1;

        public override void Resolve(EffectContext ctx)
        {
            var target = ctx.CreatureAt(TargetIndex);
            var alreadyDamaged = new List<CardInstance>();
            foreach (var p in ctx.State.Players)
                foreach (var c in p.Battlefield)
                    if (ctx.Cards.Get(c.DefinitionId).IsCreature && c.Damage > 0 && (target == null || c.Id != target.Id))
                        alreadyDamaged.Add(c);
            if (target != null) ctx.DealDamage(Target.ForObject(target.Id), Amount);
            foreach (var c in alreadyDamaged) ctx.DealDamage(Target.ForObject(c.Id), Amount);
        }
    }

    /// <summary>
    /// "Destroy it" for the creature a trigger is about (Neon Executioner), with the intervening "if it has
    /// N or less Health remaining" checked again on resolution (MTG 603.4).
    /// </summary>
    public sealed class DestroyEventCreatureEffect : Effect
    {
        public int? MaxRemainingHealth { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.State.FindOnBattlefield(ctx.EventObject);
            if (c == null) return;
            if (MaxRemainingHealth.HasValue && ctx.GetCharacteristics(c).RemainingHealth > MaxRemainingHealth.Value) return;
            ctx.Destroy(c);
        }
    }

    /// <summary>"Deal N damage to it": the creature the trigger is about (Hex of Withering).</summary>
    public sealed class DealDamageToEventCreatureEffect : Effect
    {
        public int Amount { get; set; } = 1;

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.State.FindOnBattlefield(ctx.EventObject) != null) ctx.DealDamage(Target.ForObject(ctx.EventObject), Amount);
        }
    }

    /// <summary>
    /// "All creatures get +P/+H until end of turn. You gain 1 life for each creature that dies this way." (Mass
    /// Hysteria). "Dies this way" = it's left with no Health, so state-based actions will destroy it after the spell.
    /// </summary>
    public sealed class AllCreaturesGetEffect : Effect
    {
        public int Power { get; set; }
        public int Health { get; set; }
        public int GainLifePerDeath { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var creatures = new List<CardInstance>();
            foreach (var p in ctx.State.Players)
                foreach (var c in p.Battlefield)
                    if (ctx.Cards.Get(c.DefinitionId).IsCreature) creatures.Add(c);
            foreach (var c in creatures) ctx.ModifyUntilEndOfTurn(c.Id, Power, Health, Keyword.None);
            if (GainLifePerDeath <= 0) return;
            int dying = 0;
            foreach (var c in creatures)
                if (ctx.GetCharacteristics(c).RemainingHealth <= 0) dying++;
            ctx.Heal(Target.ForPlayer(ctx.Controller), dying * GainLifePerDeath);
        }
    }

    /// <summary>"Its controller loses N life" / "that player loses N life": the player the trigger is about (Hex of Festering).</summary>
    public sealed class EventPlayerLosesLifeEffect : Effect
    {
        public int Amount { get; set; } = 1;

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.EventPlayer.HasValue && !ctx.State.GetPlayer(ctx.EventPlayer.Value).HasLost)
                ctx.LoseLife(ctx.EventPlayer.Value, Amount);
        }
    }

    /// <summary>"Put N +1/+1 counters on it": the creature the trigger is about (Sap Mender, Herd Matriarch).</summary>
    public sealed class AddCountersToEventCreatureEffect : Effect
    {
        public int Count { get; set; } = 1;

        public override void Resolve(EffectContext ctx) => ctx.AddCounters(ctx.EventObject, Count);
    }

    /// <summary>"Heal N from target creature. If it had no damage, put M +1/+1 counters on it instead." (Overflowing Spring)</summary>
    public sealed class HealOrCountersEffect : Effect
    {
        public int Amount { get; set; }
        public int CountersIfUndamaged { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.CreatureAt(TargetIndex);
            if (c == null) return;
            if (c.Damage > 0) ctx.Heal(Target.ForObject(c.Id), Amount);
            else ctx.AddCounters(c.Id, CountersIfUndamaged);
        }
    }
}
