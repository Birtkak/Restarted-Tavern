using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// "Counter target spell [or ability]. Its controller gains N Gold / Gold equal to its cost and draws
    /// N cards." (Counterfeit Coin, Bribe the Referee). "Its cost" is the printed cost (decided 2026-10-09).
    /// </summary>
    public sealed class CounterTargetEffect : Effect
    {
        public int ItsControllerGainsGold { get; set; }
        public bool ItsControllerGainsGoldEqualToCost { get; set; }
        public int ItsControllerDraws { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var item = ctx.ChainItemAt(TargetIndex);
            if (item == null) return;
            ctx.Counter(item);
            int gold = ItsControllerGainsGold
                       + (ItsControllerGainsGoldEqualToCost && item.Card != null ? ctx.Cards.Get(item.Card.DefinitionId).Cost : 0);
            if (gold > 0) ctx.GainGold(item.Controller, gold);
            if (ItsControllerDraws > 0) ctx.Draw(item.Controller, ItsControllerDraws);
        }
    }

    /// <summary>
    /// "Counter target spell unless its controller pays N. If they pay, you gain G Gold." (Hush Money). The
    /// tax is paid with mana first, then Gold. Must be the last effect (it waits for the controller's choice).
    /// </summary>
    public sealed class CounterUnlessPaysEffect : Effect
    {
        public int Amount { get; set; }
        public int RewardGoldIfPaid { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var item = ctx.ChainItemAt(TargetIndex);
            if (item != null) ctx.AskTax(item, Amount, RewardGoldIfPaid);
        }
    }

    /// <summary>
    /// "Return [target] creature to its owner's hand. Its controller gains N Gold / Gold equal to its cost."
    /// (Bounced Check, Golden Parachute, Golden Handshake). The card comes back as a new object: no damage.
    /// A token stops existing.
    /// </summary>
    public sealed class ReturnToHandEffect : Effect
    {
        public int ItsControllerGainsGold { get; set; }
        public bool ItsControllerGainsGoldEqualToCost { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.CreatureAt(TargetIndex);
            if (c == null) return;
            var controller = c.Controller;
            int cost = ctx.Cards.Get(c.DefinitionId).Cost;
            ctx.MoveTo(c, Zone.Hand);
            int gold = ItsControllerGainsGold + (ItsControllerGainsGoldEqualToCost ? cost : 0);
            if (gold > 0) ctx.GainGold(controller, gold);
        }
    }

    /// <summary>"Return all creatures to their owners' hands. Draw a card for each creature you owned that was returned." (Grand Illusion)</summary>
    public sealed class ReturnAllCreaturesEffect : Effect
    {
        public int DrawPerCreatureYouOwned { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var creatures = new List<CardInstance>();
            foreach (var p in ctx.State.Players)
                foreach (var c in p.Battlefield)
                    if (ctx.Cards.Get(c.DefinitionId).IsCreature) creatures.Add(c);
            int mine = 0;
            foreach (var c in creatures)
            {
                if (c.Owner == ctx.Controller) mine++; // tokens count: they are returned, then stop existing (MTG 111.7)
                ctx.MoveTo(c, Zone.Hand);
            }
            if (DrawPerCreatureYouOwned > 0) ctx.Draw(ctx.Controller, mine * DrawPerCreatureYouOwned);
        }
    }

    /// <summary>
    /// "Gain control of target creature [until end of turn]. Untap it. It gains Haste [until end of turn]."
    /// (Silver-Tongued Deal), "Its controller gains Gold equal to its cost and draws a card." (Hostile Takeover).
    /// </summary>
    public sealed class GainControlEffect : Effect
    {
        public bool UntilEndOfTurn { get; set; }
        public bool Untap { get; set; }
        public bool GainsHaste { get; set; }
        public bool PreviousControllerGainsGoldEqualToCost { get; set; }
        public int PreviousControllerDraws { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.CreatureAt(TargetIndex);
            if (c == null) return;
            var previous = c.Controller;
            ctx.GainControl(c, UntilEndOfTurn);
            if (Untap) ctx.Untap(c);
            if (GainsHaste) ctx.ModifyUntilEndOfTurn(c.Id, 0, 0, Keyword.Haste);
            if (previous == ctx.Controller) return;
            if (PreviousControllerGainsGoldEqualToCost) ctx.GainGold(previous, ctx.Cards.Get(c.DefinitionId).Cost);
            if (PreviousControllerDraws > 0) ctx.Draw(previous, PreviousControllerDraws);
        }
    }

    /// <summary>
    /// "Destroy all creatures. Each opponent loses N life and you gain N life for each creature that died
    /// this way." (The Final Act)
    /// </summary>
    public sealed class DestroyAllCreaturesEffect : Effect
    {
        public int DrainPerCreature { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var creatures = new List<CardInstance>();
            foreach (var p in ctx.State.Players)
                foreach (var c in p.Battlefield)
                    if (ctx.Cards.Get(c.DefinitionId).IsCreature) creatures.Add(c);
            foreach (var c in creatures) ctx.Destroy(c);
            int n = creatures.Count * DrainPerCreature;
            if (n <= 0) return;
            foreach (var p in new List<PlayerState>(ctx.Opponents())) ctx.LoseLife(p.Id, n);
            ctx.Heal(Target.ForPlayer(ctx.Controller), n);
        }
    }

    /// <summary>"Exile [target] card from a graveyard" (Body Snatcher).</summary>
    public sealed class ExileTargetCardEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            var t = ctx.TargetAt(TargetIndex);
            if (!t.HasValue || t.Value.IsPlayer) return;
            var card = ctx.State.FindObject(t.Value.Object);
            if (card != null && card.Zone == Zone.Graveyard) ctx.MoveTo(card, Zone.Exile);
        }
    }
}
