using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>"Deal N damage to [target]", or to each chosen target. Damage is permanent (GAME_DESIGN §7.3).</summary>
    public sealed class DealDamageEffect : Effect
    {
        public int Amount { get; set; }
        /// <summary>"Deal 1 damage to each of up to three target creatures" (Chain Zap).</summary>
        public bool EachTarget { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (!EachTarget)
            {
                var t = ctx.TargetAt(TargetIndex);
                if (t.HasValue) ctx.DealDamage(t.Value, Amount);
                return;
            }
            foreach (var t in new List<Target?>(ctx.Targets))
                if (t.HasValue) ctx.DealDamage(t.Value, Amount);
        }
    }

    /// <summary>"Deal N damage to each opponent."</summary>
    public sealed class DealDamageToEachOpponentEffect : Effect
    {
        public int Amount { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            foreach (var p in new List<PlayerState>(ctx.Opponents()))
                ctx.DealDamage(Target.ForPlayer(p.Id), Amount);
        }
    }

    /// <summary>"Deal N damage to each enemy creature [and each opponent]" (Riot Suppressor, Orbital Strike Network).</summary>
    public sealed class DealDamageToEachEnemyCreatureEffect : Effect
    {
        public int Amount { get; set; }
        public bool AlsoOpponents { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            foreach (var c in ctx.EnemyCreatures())
                ctx.DealDamage(Target.ForObject(c.Id), Amount);
            if (AlsoOpponents)
                foreach (var p in new List<PlayerState>(ctx.Opponents()))
                    ctx.DealDamage(Target.ForPlayer(p.Id), Amount);
        }
    }

    /// <summary>"Heal N from [target]" (GAME_DESIGN §11.1).</summary>
    public sealed class HealEffect : Effect
    {
        public int Amount { get; set; }
        public bool Fully { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var t = ctx.TargetAt(TargetIndex);
            if (t.HasValue) ctx.Heal(t.Value, Fully ? int.MaxValue : Amount);
        }
    }

    /// <summary>"Heal N from this [creature]" / "heal this creature fully" (Mossback Tortoise, Apex of the Green Deep).</summary>
    public sealed class HealSelfEffect : Effect
    {
        public int Amount { get; set; }
        public bool Fully { get; set; }

        public override void Resolve(EffectContext ctx) =>
            ctx.Heal(Target.ForObject(ctx.Source), Fully ? int.MaxValue : Amount);
    }

    /// <summary>"Heal all other creatures you control fully" (Primeval Behemoth).</summary>
    public sealed class HealOtherCreaturesYouControlEffect : Effect
    {
        public int Amount { get; set; }
        public bool Fully { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            foreach (var c in new List<CardInstance>(ctx.State.GetPlayer(ctx.Controller).Battlefield))
                if (c.Id != ctx.Source && ctx.Cards.Get(c.DefinitionId).IsCreature)
                    ctx.Heal(Target.ForObject(c.Id), Fully ? int.MaxValue : Amount);
        }
    }

    /// <summary>
    /// "[Target 0] fights [target 1]", or "this fights [target]" with <see cref="SourceFights"/>
    /// (GAME_DESIGN §11.1). If either creature is gone, no damage is dealt (MTG 701.14b).
    /// </summary>
    public sealed class FightEffect : Effect
    {
        public bool SourceFights { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var first = SourceFights ? ctx.State.FindOnBattlefield(ctx.Source) : ctx.CreatureAt(TargetIndex);
            var second = ctx.CreatureAt(SourceFights ? TargetIndex : TargetIndex + 1);
            ctx.Fight(first, second);
        }
    }

    /// <summary>"Draw N cards." (the controller)</summary>
    public sealed class DrawCardsEffect : Effect
    {
        public int Count { get; set; } = 1;

        public override void Resolve(EffectContext ctx) => ctx.Draw(ctx.Controller, Count);
    }

    /// <summary>"Gain N Gold." (the controller), or "Each opponent gains N Gold." (shady deals)</summary>
    public sealed class GainGoldEffect : Effect
    {
        public int Amount { get; set; }
        public bool EachOpponent { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (!EachOpponent)
            {
                ctx.GainGold(ctx.Controller, Amount);
                return;
            }
            foreach (var p in new List<PlayerState>(ctx.Opponents())) ctx.GainGold(p.Id, Amount);
        }
    }

    /// <summary>
    /// "You gain N life." Treated like healing your Patron, so it can't go above starting life
    /// (GAME_DESIGN §11.1, same as Lifelink).
    /// </summary>
    public sealed class GainLifeEffect : Effect
    {
        public int Amount { get; set; }

        public override void Resolve(EffectContext ctx) => ctx.Heal(Target.ForPlayer(ctx.Controller), Amount);
    }

    /// <summary>"You lose N life." Life loss is not damage.</summary>
    public sealed class LoseLifeEffect : Effect
    {
        public int Amount { get; set; }

        public override void Resolve(EffectContext ctx) => ctx.LoseLife(ctx.Controller, Amount);
    }

    /// <summary>"Each opponent loses N life and you gain N life." (Sensationalist drain)</summary>
    public sealed class DrainEffect : Effect
    {
        public int Amount { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            foreach (var p in new List<PlayerState>(ctx.Opponents())) ctx.LoseLife(p.Id, Amount);
            ctx.Heal(Target.ForPlayer(ctx.Controller), Amount);
        }
    }

    /// <summary>"Destroy [target] creature."</summary>
    public sealed class DestroyEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.CreatureAt(TargetIndex);
            if (c != null) ctx.Destroy(c);
        }
    }

    /// <summary>"Create N [token]s [with Keyword until end of turn]."</summary>
    public sealed class CreateTokensEffect : Effect
    {
        public string TokenId { get; set; }
        public int Count { get; set; } = 1;
        public Keyword GrantUntilEndOfTurn { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            for (int i = 0; i < Count; i++)
            {
                var token = ctx.CreateToken(ctx.Controller, TokenId);
                if (GrantUntilEndOfTurn != Keyword.None)
                    ctx.ModifyUntilEndOfTurn(token.Id, 0, 0, GrantUntilEndOfTurn);
            }
        }
    }

    /// <summary>"[Target] gets +P/+H [and Keyword] until end of turn."</summary>
    public sealed class PumpTargetEffect : Effect
    {
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Grants { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.CreatureAt(TargetIndex);
            if (c != null) ctx.ModifyUntilEndOfTurn(c.Id, Power, Health, Grants);
        }
    }

    /// <summary>
    /// "Your creatures get +P/+H [and Keyword] until end of turn." The affected set is locked in
    /// when it resolves (MTG 611.2c): creatures that arrive later don't get the bonus.
    /// </summary>
    public sealed class PumpYourCreaturesEffect : Effect
    {
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Grants { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var me = ctx.State.GetPlayer(ctx.Controller);
            foreach (var c in me.Battlefield)
                if (ctx.Cards.Get(c.DefinitionId).IsCreature)
                    ctx.ModifyUntilEndOfTurn(c.Id, Power, Health, Grants);
        }
    }

    /// <summary>"Put N +1/+1 counters on [target]."</summary>
    public sealed class AddCountersEffect : Effect
    {
        public int Count { get; set; } = 1;

        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.CreatureAt(TargetIndex);
            if (c != null) ctx.AddCounters(c.Id, Count);
        }
    }
}
