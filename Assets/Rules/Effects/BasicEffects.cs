using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>"Deal N damage to [target]", or to each chosen target. Damage is permanent (GAME_DESIGN §7.3).</summary>
    public sealed class DealDamageEffect : Effect
    {
        public int Amount { get; set; }
        /// <summary>"Deal 1 damage to each of up to three target creatures" (Chain Zap).</summary>
        public bool EachTarget { get; set; }
        /// <summary>"If it was already damaged, deal N instead" (Kick 'Em While They're Down).</summary>
        public int? AmountIfDamaged { get; set; }
        /// <summary>"Deal damage equal to its Power": the creature sacrificed as an extra cost (Fling the Runt).</summary>
        public bool AmountIsSacrificedPower { get; set; }
        /// <summary>"X is 2 plus the number of Goobers you control" (Scrapheap Inferno): add one per creature you control with this subtype.</summary>
        public string PlusOnePerYourCreatureOfSubtype { get; set; }
        /// <summary>"Deal X damage" (Big Boom, Orbital Laser).</summary>
        public bool AmountIsX { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (!EachTarget)
            {
                var t = ctx.TargetAt(TargetIndex);
                if (t.HasValue) ctx.DealDamage(t.Value, AmountFor(ctx, t.Value));
                return;
            }
            foreach (var t in new List<Target?>(ctx.Targets))
                if (t.HasValue) ctx.DealDamage(t.Value, AmountFor(ctx, t.Value));
        }

        public int AmountFor(EffectContext ctx, Target t)
        {
            if (AmountIsX) return ctx.X;
            if (AmountIsSacrificedPower) return ctx.SacrificedPower;
            if (PlusOnePerYourCreatureOfSubtype != null)
            {
                int n = 0;
                foreach (var c in ctx.State.GetPlayer(ctx.Controller).Battlefield)
                {
                    var def = ctx.Cards.Get(c.DefinitionId);
                    if (def.IsCreature && def.HasSubtype(PlusOnePerYourCreatureOfSubtype)) n++;
                }
                return Amount + n;
            }
            if (AmountIfDamaged.HasValue && !t.IsPlayer)
            {
                var c = ctx.State.FindOnBattlefield(t.Object);
                if (c != null && c.Damage > 0) return AmountIfDamaged.Value;
            }
            return Amount;
        }
    }

    /// <summary>"Deal N damage to each opponent."</summary>
    public sealed class DealDamageToEachOpponentEffect : Effect
    {
        public int Amount { get; set; }
        /// <summary>"Deal X damage to each opponent" (Grand Finale).</summary>
        public bool AmountIsX { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            int amount = AmountIsX ? ctx.X : Amount;
            foreach (var p in new List<PlayerState>(ctx.Opponents()))
                ctx.DealDamage(Target.ForPlayer(p.Id), amount);
        }
    }

    /// <summary>"Deal N damage to each enemy creature [and each opponent]" (Riot Suppressor, Orbital Strike Network).</summary>
    public sealed class DealDamageToEachEnemyCreatureEffect : Effect
    {
        public int Amount { get; set; }
        public bool AlsoOpponents { get; set; }
        /// <summary>"If X is N or more, also ..." (Orbital Laser): does nothing when X is lower.</summary>
        public int MinX { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.X < MinX) return;
            foreach (var c in ctx.EnemyCreatures())
                ctx.DealDamage(Target.ForObject(c.Id), Amount);
            if (AlsoOpponents)
                foreach (var p in new List<PlayerState>(ctx.Opponents()))
                    ctx.DealDamage(Target.ForPlayer(p.Id), Amount);
        }
    }

    /// <summary>"Heal N from [target]" (GAME_DESIGN §11.1), or "heal that much" with <see cref="AmountFromEvent"/>.</summary>
    public sealed class HealEffect : Effect
    {
        public int Amount { get; set; }
        public bool Fully { get; set; }
        /// <summary>"Heal that much" (Grizzled Innkeeper): the triggering event's amount.</summary>
        public bool AmountFromEvent { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var t = ctx.TargetAt(TargetIndex);
            if (t.HasValue) ctx.Heal(t.Value, Fully ? int.MaxValue : AmountFromEvent ? ctx.EventAmount : Amount);
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

    /// <summary>"Draw N cards." (the controller), or "Draw X cards" with <see cref="CountIsX"/>.</summary>
    public sealed class DrawCardsEffect : Effect
    {
        public int Count { get; set; } = 1;
        /// <summary>"Draw X cards" (Settle the Tab): X paid for the spell.</summary>
        public bool CountIsX { get; set; }

        public override void Resolve(EffectContext ctx) => ctx.Draw(ctx.Controller, CountIsX ? ctx.X : Count);
    }

    /// <summary>"Draw N cards. If you have G or more Gold, draw M instead." (Compound Interest)</summary>
    public sealed class DrawIfGoldEffect : Effect
    {
        public int Count { get; set; }
        public int GoldAtLeast { get; set; }
        public int CountIfGold { get; set; }

        public override void Resolve(EffectContext ctx) =>
            ctx.Draw(ctx.Controller, ctx.State.GetPlayer(ctx.Controller).Gold >= GoldAtLeast ? CountIfGold : Count);
    }

    /// <summary>"[Then] discard N cards." The controller chooses; it must be the last effect.</summary>
    public sealed class DiscardCardsEffect : Effect
    {
        public int Count { get; set; } = 1;

        public override void Resolve(EffectContext ctx) => ctx.AskDiscard(ctx.Controller, Count);
    }

    /// <summary>"Tap target creature." (Spilled Drink)</summary>
    public sealed class TapTargetEffect : Effect
    {
        public override void Resolve(EffectContext ctx) => ctx.Tap(ctx.CreatureAt(TargetIndex));
    }

    /// <summary>"Each player draws N cards." (Round on the House)</summary>
    public sealed class EachPlayerDrawsEffect : Effect
    {
        public int Count { get; set; } = 1;

        public override void Resolve(EffectContext ctx)
        {
            foreach (var p in ctx.State.LivingPlayersFrom(ctx.Controller)) ctx.Draw(p.Id, Count);
        }
    }

    /// <summary>"Put N +1/+1 counters on this" (Sproutling, Ghoulish Onlooker, Worldroot Hydra).</summary>
    public sealed class AddCountersToSourceEffect : Effect
    {
        public int Count { get; set; } = 1;

        public override void Resolve(EffectContext ctx) => ctx.AddCounters(ctx.Source, Count);
    }

    /// <summary>"Gain N Gold." (the controller), or "Each opponent gains N Gold." (shady deals)</summary>
    public sealed class GainGoldEffect : Effect
    {
        public int Amount { get; set; }
        public bool EachOpponent { get; set; }
        /// <summary>"You gain X Gold" (Foreclosure).</summary>
        public bool AmountIsX { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            int amount = AmountIsX ? ctx.X : Amount;
            if (!EachOpponent)
            {
                ctx.GainGold(ctx.Controller, amount);
                return;
            }
            foreach (var p in new List<PlayerState>(ctx.Opponents())) ctx.GainGold(p.Id, amount);
        }
    }

    /// <summary>
    /// "You gain N life." Treated like healing your Tavern Dweller, so it can't go above starting life
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
        /// <summary>"Each opponent loses X life and you gain X life" (Final Broadcast).</summary>
        public bool AmountIsX { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            int amount = AmountIsX ? ctx.X : Amount;
            foreach (var p in new List<PlayerState>(ctx.Opponents())) ctx.LoseLife(p.Id, amount);
            ctx.Heal(Target.ForPlayer(ctx.Controller), amount);
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
        /// <summary>"for each Gold you gained this way": the number an earlier effect remembered (Grand Heist).</summary>
        public bool CountFromRemembered { get; set; }
        /// <summary>"Create X ..." (Goober Avalanche).</summary>
        public bool CountIsX { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            int count = CountIsX ? ctx.X : CountFromRemembered ? ctx.Remembered : Count;
            for (int i = 0; i < count; i++)
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
        /// <summary>"+X/+X" or "-X/-X": added once per X (Call of the Deep: 1 and 1; Wither Away: -1 and -1).</summary>
        public int PowerPerX { get; set; }
        public int HealthPerX { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.CreatureAt(TargetIndex);
            if (c != null) ctx.ModifyUntilEndOfTurn(c.Id, Power + PowerPerX * ctx.X, Health + HealthPerX * ctx.X, Grants);
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
        /// <summary>"Your Goobers": only this subtype. Null = all your creatures.</summary>
        public string Subtype { get; set; }
        /// <summary>"Your other Goobers": not the spell's target (Reckless Charge's Invest).</summary>
        public bool OthersThanTarget { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var me = ctx.State.GetPlayer(ctx.Controller);
            var target = OthersThanTarget ? ctx.TargetAt(TargetIndex) : null;
            foreach (var c in me.Battlefield)
            {
                var def = ctx.Cards.Get(c.DefinitionId);
                if (!def.IsCreature || (Subtype != null && !def.HasSubtype(Subtype))) continue;
                if (target.HasValue && target.Value == Target.ForObject(c.Id)) continue;
                ctx.ModifyUntilEndOfTurn(c.Id, Power, Health, Grants);
            }
        }
    }

    /// <summary>"Attacking creatures you control get +P/+H until end of turn." (Rally Drummer)</summary>
    public sealed class PumpAttackingCreaturesEffect : Effect
    {
        public int Power { get; set; }
        public int Health { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.State.Combat == null) return;
            foreach (var attack in ctx.State.Combat.Attacks)
            {
                var c = ctx.State.FindOnBattlefield(attack.Attacker);
                if (c != null && c.Controller == ctx.Controller) ctx.ModifyUntilEndOfTurn(c.Id, Power, Health, Keyword.None);
            }
        }
    }

    /// <summary>"Put N +1/+1 counters on [target]."</summary>
    public sealed class AddCountersEffect : Effect
    {
        public int Count { get; set; } = 1;
        /// <summary>"Put X +1/+1 counters" (Overgrowth).</summary>
        public bool CountIsX { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.CreatureAt(TargetIndex);
            if (c != null) ctx.AddCounters(c.Id, CountIsX ? ctx.X : Count);
        }
    }
}
