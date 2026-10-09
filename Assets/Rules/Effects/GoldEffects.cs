using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// "That player loses N Gold and you gain N Gold." (Gold-Tooth Bruiser, Pickpocket Boss). "That player"
    /// is the player the trigger is about. The two halves are separate (decided 2026-10-09): you gain the
    /// Gold even if they had none.
    /// </summary>
    public sealed class DrainGoldEffect : Effect
    {
        public int Amount { get; set; } = 1;

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.EventPlayer.HasValue && !ctx.State.GetPlayer(ctx.EventPlayer.Value).HasLost)
                ctx.GainGold(ctx.EventPlayer.Value, -Amount);
            ctx.GainGold(ctx.Controller, Amount);
        }
    }

    /// <summary>
    /// "Each opponent loses all their Gold. Gain that much Gold." (Grand Heist). Your own Gold cap limits
    /// what you keep; the Gold you actually gained is remembered for "for each Gold you gained this way".
    /// </summary>
    public sealed class StealAllGoldEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            int taken = 0;
            foreach (var p in new List<PlayerState>(ctx.Opponents()))
            {
                taken += p.Gold;
                ctx.GainGold(p.Id, -p.Gold);
            }
            var me = ctx.State.GetPlayer(ctx.Controller);
            int before = me.Gold;
            ctx.GainGold(ctx.Controller, taken);
            ctx.Remembered = me.Gold - before;
        }
    }

    /// <summary>
    /// "Each opponent loses up to N Gold. You gain that much Gold." (Debt Collector), or "that player loses up
    /// to N Gold and you gain that much" with <see cref="FromEventPlayer"/> (Gold-Snatcher Crew). Only what they
    /// actually lost is gained (and your Gold cap still applies).
    /// </summary>
    public sealed class StealGoldEffect : Effect
    {
        public int Amount { get; set; }
        public bool FromEventPlayer { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var victims = new List<PlayerState>();
            if (FromEventPlayer)
            {
                if (ctx.EventPlayer.HasValue) victims.Add(ctx.State.GetPlayer(ctx.EventPlayer.Value));
            }
            else
            {
                victims.AddRange(ctx.Opponents());
            }
            int taken = 0;
            foreach (var p in victims)
            {
                if (p.HasLost) continue;
                int n = System.Math.Min(Amount, p.Gold);
                ctx.GainGold(p.Id, -n);
                taken += n;
            }
            if (taken > 0) ctx.GainGold(ctx.Controller, taken);
        }
    }

    /// <summary>"Whenever an opponent casts a spell, they lose 1 Gold. If they couldn't, you gain 1 Gold." (Tax Office)</summary>
    public sealed class TaxOfficeEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            if (!ctx.EventPlayer.HasValue) return;
            var caster = ctx.State.GetPlayer(ctx.EventPlayer.Value);
            if (!caster.HasLost && caster.Gold > 0) ctx.GainGold(caster.Id, -1);
            else ctx.GainGold(ctx.Controller, 1);
        }
    }

    /// <summary>
    /// "Each player may pay any amount of Gold. The player who paid the most draws two cards. If players tie
    /// for the most, each of them draws one card." (Dice Game). Must be the last effect.
    /// </summary>
    public sealed class DiceGameEffect : Effect
    {
        public override void Resolve(EffectContext ctx) => ctx.StartGoldAuction();
    }
}
