using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    // Effects that ask a player to choose during resolution (EffectContext.AskChoice / AskYesNo), and the
    // small follow-up effects that act on the choice (EffectContext.EventObject = the chosen object,
    // EventPlayer = the player who chose).

    /// <summary>Who makes a choice.</summary>
    public enum Chooser
    {
        You,
        EachOpponent,
    }

    /// <summary>
    /// "[You / Each opponent] sacrifice(s) a creature" (Spectacle of Blood), "You may sacrifice another creature.
    /// If you do, ..." (Midnight Ringmaster), "sacrifice another creature or lose 3 life" (Abyssal Headliner).
    /// What follows the sacrifice goes in <see cref="Then"/>; what happens without one in <see cref="Else"/>.
    /// </summary>
    public sealed class SacrificeChoiceEffect : Effect
    {
        public Chooser Who { get; set; }
        /// <summary>"another creature": not the source.</summary>
        public bool OthersOnly { get; set; }
        public bool Optional { get; set; }
        public List<Effect> Then { get; set; } = new List<Effect>();
        public List<Effect> Else { get; set; } = new List<Effect>();

        public override void Resolve(EffectContext ctx)
        {
            var players = new List<PlayerState>();
            if (Who == Chooser.You) players.Add(ctx.State.GetPlayer(ctx.Controller));
            else players.AddRange(ctx.Opponents());
            var then = new List<Effect> { new SacrificeEventObjectEffect() };
            then.AddRange(Then);
            foreach (var p in players)
            {
                var choices = new List<ObjectId>();
                foreach (var c in p.Battlefield)
                    if (ctx.Cards.Get(c.DefinitionId).IsCreature && !(OthersOnly && c.Id == ctx.Source)) choices.Add(c.Id);
                ctx.AskChoice(p.Id, choices, Optional, then, Else, "Sacrifice a creature");
            }
        }
    }

    /// <summary>"You may discard a card. If you do, [Then]." (Goober Shaman)</summary>
    public sealed class DiscardChoiceEffect : Effect
    {
        public bool Optional { get; set; } = true;
        public List<Effect> Then { get; set; } = new List<Effect>();

        public override void Resolve(EffectContext ctx)
        {
            var choices = new List<ObjectId>();
            foreach (var c in ctx.State.GetPlayer(ctx.Controller).Hand) choices.Add(c.Id);
            var then = new List<Effect> { new DiscardEventObjectEffect() };
            then.AddRange(Then);
            ctx.AskChoice(ctx.Controller, choices, Optional, then, null, "Discard a card");
        }
    }

    /// <summary>"You may [Then]." as a yes/no question to the controller (Ledger Imp: "You may lose 2 life. If you do, gain 1 Gold.").</summary>
    public sealed class YouMayEffect : Effect
    {
        public string Prompt { get; set; } = "";
        public List<Effect> Then { get; set; } = new List<Effect>();

        public override void Resolve(EffectContext ctx) => ctx.AskYesNo(ctx.Controller, Then, null, Prompt);
    }

    /// <summary>
    /// "Each opponent may give you N Gold. For each one who doesn't, draw a card." (The Dealer). An opponent
    /// needs the full N Gold to give (decided 2026-10-09).
    /// </summary>
    public sealed class EachOpponentMayPayGoldEffect : Effect
    {
        public int Gold { get; set; } = 2;
        public List<Effect> IfNot { get; set; } = new List<Effect>();

        public override void Resolve(EffectContext ctx)
        {
            var pay = new List<Effect> { new EventPlayerGivesYouGoldEffect { Amount = Gold } };
            foreach (var p in new List<PlayerState>(ctx.Opponents()))
            {
                if (p.Gold >= Gold) ctx.AskYesNo(p.Id, pay, IfNot, "Give " + Gold + " Gold (or they draw a card)?");
                else foreach (var e in IfNot) e.Resolve(ctx);
            }
        }
    }

    /// <summary>
    /// "Choose a creature card in each graveyard. Put them onto the battlefield under your control." (Exhumation
    /// Broadcast), or "Return a creature card with cost N or less from your graveyard to the battlefield."
    /// (Midnight Ritual's Invest, with <see cref="OnlyYourGraveyard"/>). Not targeted: chosen on resolution.
    /// </summary>
    public sealed class ChooseCreatureCardToBattlefieldEffect : Effect
    {
        public bool OnlyYourGraveyard { get; set; }
        public int? MaxCost { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var then = new List<Effect> { new PutEventObjectOntoBattlefieldEffect() };
            foreach (var p in ctx.State.LivingPlayersFrom(ctx.Controller))
            {
                if (OnlyYourGraveyard && p.Id != ctx.Controller) continue;
                var choices = new List<ObjectId>();
                foreach (var c in p.Graveyard)
                {
                    var def = ctx.Cards.Get(c.DefinitionId);
                    if (def.IsCreature && (!MaxCost.HasValue || def.Cost <= MaxCost.Value)) choices.Add(c.Id);
                }
                ctx.AskChoice(ctx.Controller, choices, false, then, null, "Put a creature card onto the battlefield");
            }
        }
    }

    /// <summary>
    /// "For each opponent, gain control of the creature they control with the highest cost. That player gains 5
    /// Gold and draws 2 cards." (Everything Has a Price). On a tie, that creature's controller chooses. An
    /// opponent with no creature gets nothing: the Gold and cards are the price for the creature.
    /// </summary>
    public sealed class EverythingHasAPriceEffect : Effect
    {
        public int Gold { get; set; } = 5;
        public int Cards { get; set; } = 2;

        public override void Resolve(EffectContext ctx)
        {
            var then = new List<Effect>
            {
                new GainControlOfEventObjectEffect(),
                new EventPlayerGainsGoldEffect { Amount = Gold },
                new EventPlayerDrawsEffect { Count = Cards },
            };
            foreach (var p in new List<PlayerState>(ctx.Opponents()))
            {
                int best = -1;
                var tied = new List<ObjectId>();
                foreach (var c in p.Battlefield)
                {
                    var def = ctx.Cards.Get(c.DefinitionId);
                    if (!def.IsCreature) continue;
                    if (def.Cost > best) { best = def.Cost; tied.Clear(); }
                    if (def.Cost == best) tied.Add(c.Id);
                }
                ctx.AskChoice(p.Id, tied, false, then, null, "Choose which of your highest-cost creatures they take");
            }
        }
    }

    /// <summary>
    /// "Deal N damage divided as you choose among any number of targets" (Firecracker Volley): each target gets
    /// its share of PlayerAction.Division. A share for a target that became illegal is lost (MTG 608.2b).
    /// </summary>
    public sealed class DealDividedDamageEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            for (int i = 0; i < ctx.Targets.Count && i < ctx.Division.Count; i++)
            {
                var t = ctx.TargetAt(i);
                if (t.HasValue) ctx.DealDamage(t.Value, ctx.Division[i]);
            }
        }
    }

    // ------------------------------------------------------------------ follow-ups that act on the choice

    /// <summary>Sacrifice the chosen permanent (it goes to its owner's graveyard; it can't be prevented).</summary>
    public sealed class SacrificeEventObjectEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.State.FindOnBattlefield(ctx.EventObject);
            if (c != null) ctx.MoveTo(c, Zone.Graveyard);
        }
    }

    /// <summary>Discard the chosen card from its owner's hand.</summary>
    public sealed class DiscardEventObjectEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.State.FindObject(ctx.EventObject);
            if (c != null && c.Zone == Zone.Hand) ctx.MoveTo(c, Zone.Graveyard);
        }
    }

    /// <summary>Put the chosen card onto the battlefield under your control.</summary>
    public sealed class PutEventObjectOntoBattlefieldEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.State.FindObject(ctx.EventObject);
            if (c != null && c.Zone == Zone.Graveyard) ctx.PutOntoBattlefield(c);
        }
    }

    /// <summary>Gain control of the chosen permanent.</summary>
    public sealed class GainControlOfEventObjectEffect : Effect
    {
        public override void Resolve(EffectContext ctx) => ctx.GainControl(ctx.State.FindOnBattlefield(ctx.EventObject), false);
    }

    /// <summary>"That player gains N Gold."</summary>
    public sealed class EventPlayerGainsGoldEffect : Effect
    {
        public int Amount { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.EventPlayer.HasValue) ctx.GainGold(ctx.EventPlayer.Value, Amount);
        }
    }

    /// <summary>"That player draws N cards."</summary>
    public sealed class EventPlayerDrawsEffect : Effect
    {
        public int Count { get; set; } = 1;

        public override void Resolve(EffectContext ctx)
        {
            if (ctx.EventPlayer.HasValue) ctx.Draw(ctx.EventPlayer.Value, Count);
        }
    }

    /// <summary>"[That player] gives you N Gold": they lose it, you gain it.</summary>
    public sealed class EventPlayerGivesYouGoldEffect : Effect
    {
        public int Amount { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            if (!ctx.EventPlayer.HasValue) return;
            ctx.GainGold(ctx.EventPlayer.Value, -Amount);
            ctx.GainGold(ctx.Controller, Amount);
        }
    }
}
