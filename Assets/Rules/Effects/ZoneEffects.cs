using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// "Look at the top 2 cards of your deck. Put one in your hand and the other on the bottom. Invest 1: Put
    /// both in your hand." (Pocket Change). Must be the last effect.
    /// </summary>
    public sealed class LookAtTopPutOneInHandRestOnBottomEffect : Effect
    {
        public int Count { get; set; } = 2;
        /// <summary>With Invest paid, all of them go to your hand.</summary>
        public bool AllToHandIfInvested { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (AllToHandIfInvested && ctx.Invested)
            {
                var deck = ctx.State.GetPlayer(ctx.Controller).Deck;
                for (int i = 0; i < Count && deck.Count > 0; i++) ctx.MoveTo(deck[0], Zone.Hand);
                return;
            }
            ctx.AskChooseFromTop(Count, restToBottom: true);
        }
    }

    /// <summary>"Return all creature cards with cost N or less from your graveyard to the battlefield." (Madame Morbida)</summary>
    public sealed class ReturnAllFromGraveyardToBattlefieldEffect : Effect
    {
        public int MaxCost { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var cards = new List<CardInstance>();
            foreach (var c in ctx.State.GetPlayer(ctx.Controller).Graveyard)
            {
                var def = ctx.Cards.Get(c.DefinitionId);
                if (def.IsCreature && def.Cost <= MaxCost) cards.Add(c);
            }
            foreach (var c in cards) ctx.PutOntoBattlefield(c);
        }
    }

    /// <summary>
    /// "Reveal cards from the top of your deck until you reveal a creature card with cost N or more. Put it onto
    /// the battlefield. Put the other revealed cards on the bottom of your deck in a random order." (Call of the Deep Jungle)
    /// </summary>
    public sealed class RevealUntilCreatureEffect : Effect
    {
        public int MinCost { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var deck = ctx.State.GetPlayer(ctx.Controller).Deck;
            var revealed = new List<CardInstance>();
            CardInstance hit = null;
            foreach (var c in deck)
            {
                var def = ctx.Cards.Get(c.DefinitionId);
                if (def.IsCreature && def.Cost >= MinCost) { hit = c; break; }
                revealed.Add(c);
            }
            if (hit != null) ctx.PutOntoBattlefield(hit);
            ctx.State.Rng.Shuffle(revealed);
            foreach (var c in revealed) ctx.MoveToBottom(c);
        }
    }

    /// <summary>"Look at the top N cards of your deck. Put one into your hand and the rest into your graveyard." (Grave Gossip). Must be the last effect.</summary>
    public sealed class LookAtTopPutOneInHandEffect : Effect
    {
        public int Count { get; set; } = 3;

        public override void Resolve(EffectContext ctx) => ctx.AskChooseFromTop(Count);
    }

    /// <summary>
    /// "Return target creature card from your graveyard to the battlefield. At the end of your turn, exile it."
    /// (Encore From Beyond)
    /// </summary>
    public sealed class ReanimateEffect : Effect
    {
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
