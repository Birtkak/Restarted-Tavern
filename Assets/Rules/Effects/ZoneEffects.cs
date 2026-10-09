namespace RestartedTavern.Rules
{
    /// <summary>"Look at the top N cards of your deck. Put one into your hand and the rest into your graveyard." (Grave Gossip). Must be the last effect.</summary>
    public sealed class LookAtTopPutOneInHandEffect : Effect
    {
        public int Count { get; set; } = 3;

        public override void Resolve(EffectContext ctx) => ctx.AskChooseFromTop(Count);
    }

    /// <summary>
    /// "Return target creature card from your graveyard to the battlefield. It gains Haste. At the end of your
    /// turn, exile it." (Encore From Beyond)
    /// </summary>
    public sealed class ReanimateEffect : Effect
    {
        public bool GainsHaste { get; set; }
        public bool ExileAtEndOfTurn { get; set; }

        private static readonly TriggeredAbility ExileIt = new TriggeredAbility
        {
            When = TriggerEvent.EndOfYourTurn, Effects = { new ExileEventObjectEffect() }, Text = "At the end of your turn, exile it.",
        };

        public override void Resolve(EffectContext ctx)
        {
            var t = ctx.TargetAt(TargetIndex);
            if (!t.HasValue || t.Value.IsPlayer) return;
            var card = ctx.State.FindObject(t.Value.Object);
            if (card == null || card.Zone != Zone.Graveyard) return;
            var permanent = ctx.PutOntoBattlefield(card);
            if (permanent == null) return;
            if (GainsHaste) ctx.ModifyUntilEndOfTurn(permanent.Id, 0, 0, Keyword.Haste);
            if (ExileAtEndOfTurn) ctx.AddDelayedTrigger(ExileIt, permanent.Id);
        }
    }

    /// <summary>"Exile it": the permanent a (delayed) trigger is about, if it's still on the battlefield.</summary>
    public sealed class ExileEventObjectEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.State.FindOnBattlefield(ctx.EventObject);
            if (c != null) ctx.MoveTo(c, Zone.Exile);
        }
    }
}
