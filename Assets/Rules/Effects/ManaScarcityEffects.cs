using System.Collections.Generic;
using System.Linq;

namespace RestartedTavern.Rules
{
    // Effects added for set v0.3 (mana scarcity, 2026-10-09): drains, inevitability and card flow.

    /// <summary>"Each opponent loses N (or X) life." (Foreclosure, Closing Bell). Life loss isn't damage.</summary>
    public sealed class EachOpponentLosesLifeEffect : Effect
    {
        public int Amount { get; set; }
        public bool AmountIsX { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            int amount = AmountIsX ? ctx.X : Amount;
            foreach (var p in new List<PlayerState>(ctx.Opponents())) ctx.LoseLife(p.Id, amount);
        }
    }

    /// <summary>"Each opponent loses life equal to the Gold you have." (Loan Shark)</summary>
    public sealed class EachOpponentLosesLifeEqualToYourGoldEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            int gold = ctx.State.GetPlayer(ctx.Controller).Gold;
            if (gold <= 0) return;
            foreach (var p in new List<PlayerState>(ctx.Opponents())) ctx.LoseLife(p.Id, gold);
        }
    }

    /// <summary>"That player loses N life (and you gain N life)": the player this Curse is attached to (Curse of Prime Time).</summary>
    public sealed class AttachedPlayerLosesLifeEffect : Effect
    {
        public int Amount { get; set; } = 1;
        public bool YouGainLife { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var curse = ctx.State.FindOnBattlefield(ctx.Source);
            if (curse?.AttachedToPlayer == null || ctx.State.GetPlayer(curse.AttachedToPlayer.Value).HasLost) return;
            ctx.LoseLife(curse.AttachedToPlayer.Value, Amount);
            if (YouGainLife) ctx.Heal(Target.ForPlayer(ctx.Controller), Amount);
        }
    }

    /// <summary>"Draw a card for each creature with N or more Power you control (at least one)." (Gift of the Grove)</summary>
    public sealed class DrawPerBigCreatureEffect : Effect
    {
        public int MinPower { get; set; } = 5;

        public override void Resolve(EffectContext ctx)
        {
            int n = 0;
            foreach (var c in ctx.State.GetPlayer(ctx.Controller).Battlefield)
                if (ctx.Cards.Get(c.DefinitionId).IsCreature && ctx.GetCharacteristics(c).Power >= MinPower) n++;
            ctx.Draw(ctx.Controller, System.Math.Max(1, n));
        }
    }

    /// <summary>
    /// "Discard any number of cards, then draw that many cards plus one." (Dumpster Dive). Asks one card at a time
    /// ("discard another?"); declining draws the cards.
    /// </summary>
    public sealed class RummageAnyEffect : Effect
    {
        /// <summary>Cards discarded so far (set on the follow-up copies).</summary>
        public int Discarded { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var hand = ctx.State.GetPlayer(ctx.Controller).Hand.Select(c => c.Id).ToList();
            ctx.AskChoice(ctx.Controller, hand, true,
                new List<Effect> { new DiscardEventObjectEffect(), new RummageAnyEffect { Discarded = Discarded + 1 } },
                new List<Effect> { new DrawCardsEffect { Count = Discarded + 1 } },
                "Discard (then draw " + (Discarded + 2) + ")");
        }
    }

    /// <summary>"This gains [keyword]" for as long as it stays on the battlefield (Gilded Mercenary's Invest).</summary>
    public sealed class GrantKeywordToSourceEffect : Effect
    {
        public Keyword Keyword { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.State.FindOnBattlefield(ctx.Source);
            if (c != null) c.GrantedKeywords |= Keyword;
        }
    }

    /// <summary>"Deal X damage divided as you choose among any number of creatures and/or opponents." (Arc Cascade)</summary>
    public sealed class DivideXDamageEffect : Effect
    {
        public override void Resolve(EffectContext ctx) => ctx.AskDivideDamage(ctx.X);
    }

    /// <summary>
    /// "Return [target creature card] from your graveyard to your hand. Invest: put it onto the battlefield instead."
    /// (Open Casket)
    /// </summary>
    public sealed class ReturnGraveyardCreatureEffect : Effect
    {
        public bool ToBattlefieldIfInvested { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var t = ctx.TargetAt(TargetIndex);
            if (!t.HasValue || t.Value.IsPlayer) return;
            var card = ctx.State.FindObject(t.Value.Object);
            if (card == null || card.Zone != Zone.Graveyard) return;
            if (ToBattlefieldIfInvested && ctx.Invested) ctx.PutOntoBattlefield(card);
            else ctx.MoveToHand(card);
        }
    }
}
