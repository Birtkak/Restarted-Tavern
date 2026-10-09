using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>"Deal N damage to [the target]." Damage is permanent (GAME_DESIGN §7.3).</summary>
    public sealed class DealDamageEffect : Effect
    {
        public int Amount { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.Target.HasValue) ctx.DealDamage(ctx.Target.Value, Amount);
        }
    }

    /// <summary>"Deal N damage to each opponent."</summary>
    public sealed class DealDamageToEachOpponentEffect : Effect
    {
        public int Amount { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            foreach (var p in new List<PlayerState>(ctx.Opponents()))
                ctx.DealDamage(Rules.Target.ForPlayer(p.Id), Amount);
        }
    }

    /// <summary>"Heal N from [the target]" (GAME_DESIGN §11.1).</summary>
    public sealed class HealEffect : Effect
    {
        public int Amount { get; set; }
        public bool Fully { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.Target.HasValue) ctx.Heal(ctx.Target.Value, Fully ? int.MaxValue : Amount);
        }
    }

    /// <summary>"Draw N cards." (the controller)</summary>
    public sealed class DrawCardsEffect : Effect
    {
        public int Count { get; set; } = 1;

        public override void Resolve(EffectContext ctx) => ctx.Draw(ctx.Controller, Count);
    }

    /// <summary>"Gain N Gold." (the controller)</summary>
    public sealed class GainGoldEffect : Effect
    {
        public int Amount { get; set; }

        public override void Resolve(EffectContext ctx) => ctx.GainGold(ctx.Controller, Amount);
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

    /// <summary>"[The target] gets +P/+H [and Keyword] until end of turn."</summary>
    public sealed class PumpTargetEffect : Effect
    {
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Grants { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.Target.HasValue && !ctx.Target.Value.IsPlayer)
                ctx.ModifyUntilEndOfTurn(ctx.Target.Value.Object, Power, Health, Grants);
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

    /// <summary>"Put N +1/+1 counters on [the target]."</summary>
    public sealed class AddCountersEffect : Effect
    {
        public int Count { get; set; } = 1;

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.Target.HasValue && !ctx.Target.Value.IsPlayer)
                ctx.AddCounters(ctx.Target.Value.Object, Count);
        }
    }
}
