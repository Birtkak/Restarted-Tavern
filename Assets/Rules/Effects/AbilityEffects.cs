using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>Equip (MTG 701.3, GAME_DESIGN §10): attach the source Equipment to [target] creature.</summary>
    public sealed class AttachSourceEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            var equipment = ctx.State.FindOnBattlefield(ctx.Source);
            var creature = ctx.CreatureAt(TargetIndex);
            // The Equipment must still be yours, and the creature must still be legal (checked as a target).
            if (equipment == null || equipment.Controller != ctx.Controller) return;
            ctx.Attach(equipment, creature);
        }
    }

    /// <summary>
    /// "Attach [target Equipment you control] to [target creature you control / this creature]"
    /// (Sparkwrench's Power, Courier Bot).
    /// </summary>
    public sealed class AttachTargetEquipmentEffect : Effect
    {
        /// <summary>Attach it to the source (Courier Bot) instead of to the next target.</summary>
        public bool ToSource { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var t = ctx.TargetAt(TargetIndex);
            if (!t.HasValue || t.Value.IsPlayer) return;
            var equipment = ctx.State.FindOnBattlefield(t.Value.Object);
            var creature = ToSource ? ctx.State.FindOnBattlefield(ctx.Source) : ctx.CreatureAt(TargetIndex + 1);
            if (equipment == null || equipment.Controller != ctx.Controller) return;
            ctx.Attach(equipment, creature);
        }
    }

    /// <summary>
    /// "Attach up to one target Equipment you control to target creature you control. If no Equipment became attached,
    /// that creature gets +P/+H until end of turn." (Sparkwrench's Power, 2026-10-10). The creature is target
    /// <see cref="Effect.TargetIndex"/>, the optional Equipment the next one. Re-attaching an Equipment to the creature
    /// it's already on attaches nothing, so the creature gets the bonus.
    /// </summary>
    public sealed class AttachEquipmentOrPumpEffect : Effect
    {
        public int Power { get; set; } = 1;
        public int Health { get; set; } = 1;

        public override void Resolve(EffectContext ctx)
        {
            var creature = ctx.CreatureAt(TargetIndex);
            if (creature == null) return;
            var t = ctx.TargetAt(TargetIndex + 1);
            var equipment = t.HasValue && !t.Value.IsPlayer ? ctx.State.FindOnBattlefield(t.Value.Object) : null;
            if (equipment != null && equipment.Controller == ctx.Controller && equipment.AttachedToObject != creature.Id)
            {
                ctx.Attach(equipment, creature);
                if (equipment.AttachedToObject == creature.Id) return;
            }
            ctx.ModifyUntilEndOfTurn(creature.Id, Power, Health, Keyword.None);
        }
    }

    /// <summary>
    /// "Choose up to X other [subtype] you control. [Then]" (Snik). The choice is made on resolution (MTG 608.2d: it doesn't
    /// target), so a creature that left in response simply can't be chosen. Then runs once with the chosen creatures as
    /// its targets.
    /// </summary>
    public sealed class ChooseUpToXYourCreaturesEffect : Effect
    {
        public string Subtype { get; set; }
        public bool OthersOnly { get; set; } = true;
        public List<Effect> Then { get; set; } = new List<Effect>();

        public override void Resolve(EffectContext ctx)
        {
            var choices = new List<ObjectId>();
            foreach (var c in ctx.State.GetPlayer(ctx.Controller).Battlefield)
            {
                var def = ctx.Cards.Get(c.DefinitionId);
                if (!def.IsCreature || (OthersOnly && c.Id == ctx.Source)) continue;
                if (Subtype != null && !def.HasSubtype(Subtype)) continue;
                choices.Add(c.Id);
            }
            ctx.AskChooseUpTo(choices, ctx.X, Then, "Choose" + (Subtype != null ? " a " + Subtype : " a creature"));
        }
    }

    /// <summary>
    /// "For each chosen creature, create a token copy of it. The copies gain Haste until end of
    /// turn." (Snik). Copies take the printed card (MTG 707.2): no damage, counters or buffs.
    /// </summary>
    public sealed class CreateTokenCopiesEffect : Effect
    {
        public Keyword GrantUntilEndOfTurn { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            for (int i = 0; i < ctx.Targets.Count; i++)
            {
                var original = ctx.CreatureAt(i);
                if (original == null) continue;
                var copy = ctx.CreateToken(ctx.Controller, original.DefinitionId);
                if (GrantUntilEndOfTurn != Keyword.None)
                    ctx.ModifyUntilEndOfTurn(copy.Id, 0, 0, GrantUntilEndOfTurn);
            }
        }
    }

    /// <summary>"Return [target] creature card from your graveyard to your hand" (The Rotmother).</summary>
    public sealed class ReturnToHandFromGraveyardEffect : Effect
    {
        public override void Resolve(EffectContext ctx)
        {
            var t = ctx.TargetAt(TargetIndex);
            if (!t.HasValue || t.Value.IsPlayer) return;
            var card = ctx.State.FindObject(t.Value.Object);
            if (card != null && card.Zone == Zone.Graveyard) ctx.MoveToHand(card);
        }
    }

    /// <summary>
    /// "Look at the top card of your deck. You may put it on the bottom." (Auditor Prime). The
    /// choice is a pending decision, so this must be the last effect of its ability.
    /// </summary>
    public sealed class LookAtTopMayBottomEffect : Effect
    {
        public override void Resolve(EffectContext ctx) => ctx.AskTopOrBottom();
    }

    /// <summary>
    /// "Deal N damage to [target] creature. If it dies, draw a card." (Vox Nocturne). "Dies"
    /// means the damage was lethal: state-based actions will put it into the graveyard.
    /// </summary>
    public sealed class DamageThenDrawIfLethalEffect : Effect
    {
        public int Amount { get; set; } = 1;

        public override void Resolve(EffectContext ctx)
        {
            var c = ctx.CreatureAt(TargetIndex);
            if (c == null) return;
            ctx.DealDamage(Target.ForObject(c.Id), Amount);
            if (ctx.GetCharacteristics(c).RemainingHealth <= 0) ctx.Draw(ctx.Controller, 1);
        }
    }

    /// <summary>"Create a 1/1 [token] that's tapped and attacking" (Grakka, Queen of the Rabble).</summary>
    public sealed class CreateAttackingTokenEffect : Effect
    {
        public string TokenId { get; set; }

        public override void Resolve(EffectContext ctx) => ctx.CreateAttackingToken(TokenId);
    }

    /// <summary>"[This creature] gets +P/+H [and Keyword] until end of turn" (Hired Muscle).</summary>
    public sealed class PumpSourceEffect : Effect
    {
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Grants { get; set; }
        /// <summary>"(X): This gets +X/+0" (Rampaging Titan, Tavern Legend): Power added per X.</summary>
        public int PowerPerX { get; set; }

        public override void Resolve(EffectContext ctx) => ctx.ModifyUntilEndOfTurn(ctx.Source, Power + PowerPerX * ctx.X, Health, Grants);
    }

    /// <summary>"Heal N from each Construct and each equipped creature you control" (Repair Bay).</summary>
    public sealed class HealYourMachinesEffect : Effect
    {
        public int Amount { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            foreach (var c in new List<CardInstance>(ctx.State.GetPlayer(ctx.Controller).Battlefield))
            {
                var def = ctx.Cards.Get(c.DefinitionId);
                if (def.IsCreature && (def.HasSubtype("Construct") || CharacteristicsCalculator.IsEquipped(ctx.State, ctx.Cards, c)))
                    ctx.Heal(Target.ForObject(c.Id), Amount);
            }
        }
    }
}
