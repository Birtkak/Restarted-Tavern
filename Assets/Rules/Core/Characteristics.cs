using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>A creature's current, computed stats (after continuous effects).</summary>
    public struct Characteristics
    {
        public int Power;
        public int MaxHealth;
        public Keyword Keywords;
        /// <summary>MaxHealth − Damage. The creature dies at 0 or less.</summary>
        public int RemainingHealth;

        public bool Has(Keyword k) => (Keywords & k) != 0;
    }

    /// <summary>
    /// An always-on ability (MTG static ability) of a permanent on the battlefield or of a Tavern Dweller
    /// in the Tavern Dweller zone. Characteristic changes are split by MTG layer (CR 613): keywords and
    /// other abilities (layer 6) first, then Power/Health (layer 7c), so a 7c effect can depend on
    /// keywords ("your creatures with Trample get +1/+0"). See <see cref="CharacteristicsCalculator"/>.
    /// Some statics don't change characteristics at all (cost modifiers, "enters with a counter").
    /// </summary>
    public abstract class StaticAbility
    {
        public string Text { get; set; } = "";

        /// <summary>Layer 6: keywords this gives <paramref name="affected"/>.</summary>
        public virtual Keyword GrantsKeywords(GameState state, CardDatabase db, CardInstance source, CardInstance affected) => Keyword.None;

        /// <summary>Layer 7c: Power/Health changes. <paramref name="keywords"/> are the affected creature's final keywords.</summary>
        public virtual void ModifyPowerHealth(GameState state, CardDatabase db, CardInstance source, CardInstance affected,
            Keyword keywords, ref int power, ref int health) { }

        /// <summary>Layer 6: triggered abilities this gives <paramref name="affected"/> ("equipped creature has ...").</summary>
        public virtual List<TriggeredAbility> GrantedTriggers(GameState state, CardInstance source, CardInstance affected) => null;

        /// <summary>Layer 6: activated abilities this gives <paramref name="affected"/>.</summary>
        public virtual List<ActivatedAbility> GrantedAbilities(GameState state, CardInstance source, CardInstance affected) => null;

        /// <summary>"[It] can't be healed" (GAME_DESIGN §11.1): Heal effects remove no damage from <paramref name="affected"/>.</summary>
        public virtual bool PreventsHealing(GameState state, CardDatabase db, CardInstance source, CardInstance affected) => false;
    }

    /// <summary>
    /// "Your [other] [Subtype] creatures [with Keyword] [that are equipped] get +P/+H [and have Keyword]."
    /// (lords, anthems, Patrol Captain, Mukk the Grub King).
    /// </summary>
    public sealed class AnthemAbility : StaticAbility
    {
        /// <summary>Null means every creature you control.</summary>
        public string Subtype { get; set; }
        public bool OthersOnly { get; set; }
        /// <summary>Only creatures that have this keyword (checked after layer 6).</summary>
        public Keyword RequiresKeyword { get; set; }
        /// <summary>Only equipped creatures (Patrol Captain).</summary>
        public bool RequiresEquipped { get; set; }
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Grants { get; set; }

        private bool Affects(GameState state, CardDatabase db, CardInstance source, CardInstance affected)
        {
            if (affected.Controller != source.Controller) return false;
            if (OthersOnly && affected.Id == source.Id) return false;
            var def = db.Get(affected.DefinitionId);
            if (!def.IsCreature) return false;
            if (Subtype != null && !def.HasSubtype(Subtype)) return false;
            if (RequiresEquipped && !CharacteristicsCalculator.IsEquipped(state, db, affected)) return false;
            return true;
        }

        public override Keyword GrantsKeywords(GameState state, CardDatabase db, CardInstance source, CardInstance affected) =>
            Grants != Keyword.None && RequiresKeyword == Keyword.None && Affects(state, db, source, affected) ? Grants : Keyword.None;

        public override void ModifyPowerHealth(GameState state, CardDatabase db, CardInstance source, CardInstance affected,
            Keyword keywords, ref int power, ref int health)
        {
            if (Power == 0 && Health == 0) return;
            if (RequiresKeyword != Keyword.None && (keywords & RequiresKeyword) == 0) return;
            if (!Affects(state, db, source, affected)) return;
            power += Power;
            health += Health;
        }
    }

    /// <summary>
    /// "Enchanted / equipped creature gets +P/+H [and has Keyword] [and has "ability"]": affects
    /// whatever the source (a Curse or Equipment) is attached to. Negative values for Curses
    /// (Hex of Frailty: -1/-1).
    /// </summary>
    public sealed class AttachedCreatureModifier : StaticAbility
    {
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Grants { get; set; }
        public List<TriggeredAbility> Triggers { get; set; } = new List<TriggeredAbility>();
        public List<ActivatedAbility> Abilities { get; set; } = new List<ActivatedAbility>();
        /// <summary>"Equipped creature can't be dealt more than N damage each turn" (Hardlight Aegis). 0 = no limit.</summary>
        public int MaxDamageEachTurn { get; set; }

        internal static bool Affects(CardInstance source, CardInstance affected) =>
            !source.AttachedToObject.IsNone && affected.Id == source.AttachedToObject;

        public override Keyword GrantsKeywords(GameState state, CardDatabase db, CardInstance source, CardInstance affected) =>
            Affects(source, affected) ? Grants : Keyword.None;

        public override void ModifyPowerHealth(GameState state, CardDatabase db, CardInstance source, CardInstance affected,
            Keyword keywords, ref int power, ref int health)
        {
            if (!Affects(source, affected)) return;
            power += Power;
            health += Health;
        }

        public override List<TriggeredAbility> GrantedTriggers(GameState state, CardInstance source, CardInstance affected) =>
            Triggers.Count > 0 && Affects(source, affected) ? Triggers : null;

        public override List<ActivatedAbility> GrantedAbilities(GameState state, CardInstance source, CardInstance affected) =>
            Abilities.Count > 0 && Affects(source, affected) ? Abilities : null;
    }

    /// <summary>
    /// "Enchanted creature gets -1/-1 for each creature card in your graveyard (up to -4/-4)" (Hex of
    /// Hollow Bones). Layer 7c, recomputed all the time. Lowering max Health is a real penalty, so it can kill (§7.3).
    /// </summary>
    public sealed class AttachedScalingModifier : StaticAbility
    {
        public int PowerPer { get; set; }
        public int HealthPer { get; set; }
        /// <summary>How many times to apply PowerPer/HealthPer. Gets the state, the database and the source (the Curse).</summary>
        public Func<GameState, CardDatabase, CardInstance, int> Count { get; set; }

        public override void ModifyPowerHealth(GameState state, CardDatabase db, CardInstance source, CardInstance affected,
            Keyword keywords, ref int power, ref int health)
        {
            if (!AttachedCreatureModifier.Affects(source, affected)) return;
            int n = Count(state, db, source);
            power += PowerPer * n;
            health += HealthPer * n;
        }
    }

    /// <summary>"This gets +1/+0 for each damage on it" (Scarred Veteran).</summary>
    public sealed class PowerPerDamageAbility : StaticAbility
    {
        public override void ModifyPowerHealth(GameState state, CardDatabase db, CardInstance source, CardInstance affected,
            Keyword keywords, ref int power, ref int health)
        {
            if (affected.Id == source.Id) power += affected.Damage;
        }
    }

    /// <summary>
    /// "It can't be healed" on the creature a Curse is attached to (Hex of Festering), or "creatures they
    /// control can't be healed" for the player a Curse is attached to (Curse of Rot). GAME_DESIGN §11.1.
    /// </summary>
    public sealed class CantBeHealedAbility : StaticAbility
    {
        public override bool PreventsHealing(GameState state, CardDatabase db, CardInstance source, CardInstance affected)
        {
            if (!source.AttachedToObject.IsNone) return affected.Id == source.AttachedToObject;
            return source.AttachedToPlayer.HasValue && affected.Controller == source.AttachedToPlayer.Value;
        }
    }

    /// <summary>
    /// "Your creatures with N or more Health enter with a +1/+1 counter" (Keeper Z-00). Health is
    /// checked as the creature would exist on the battlefield (MTG 614.12), so buffs count.
    /// </summary>
    public sealed class EntersWithCountersAbility : StaticAbility
    {
        public int MinHealth { get; set; }
        public int Counters { get; set; } = 1;
    }

    /// <summary>
    /// "Your Gold cap is N" (Offshore Account, GAME_DESIGN §5.2). The cap is a per-player value that
    /// starts at the format's cap; when several effects set it, the newest wins (MTG timestamp order).
    /// See <see cref="GoldRules.Cap"/>.
    /// </summary>
    public sealed class GoldCapAbility : StaticAbility
    {
        public int Cap { get; set; }
    }

    public enum CostKind
    {
        /// <summary>Casting a card from hand (creatures count: they are spells too).</summary>
        Spell,
        Invest,
        Equip,
    }

    /// <summary>
    /// Changes what its controller pays (Old Mossbank, Sparkwrench, Auditor Prime, Archon Lumen).
    /// Only generic costs go down; a reduction never takes a cost below 0, or below 1 with
    /// <see cref="NotBelowOne"/>. <see cref="SetToZero"/> is applied after all reductions.
    /// </summary>
    public sealed class CostModifierAbility : StaticAbility
    {
        public CostKind Kind { get; set; }
        /// <summary>Spell: only cards whose printed cost is at least this.</summary>
        public int MinPrintedCost { get; set; }
        /// <summary>Spell: only cards of this type (e.g. Equipment). Null = any.</summary>
        public CardType? OnlyType { get; set; }
        public int Reduction { get; set; }
        /// <summary>"(minimum 1)": doesn't reduce a cost below 1 (and leaves a cost of 0 or 1 alone).</summary>
        public bool NotBelowOne { get; set; }
        /// <summary>"Your Equip costs are 0" (Archon Lumen).</summary>
        public bool SetToZero { get; set; }

        public bool AppliesToSpell(CardDefinition def) =>
            Kind == CostKind.Spell && def.Cost >= MinPrintedCost && (OnlyType == null || def.Type == OnlyType.Value);
    }

    /// <summary>
    /// Computes current characteristics, following the MTG layer order (CR 613) for the parts the
    /// engine supports today:
    ///   layer 6 (abilities): printed keywords, then static grants, then until-end-of-turn grants;
    ///   layer 7c (P/T modifiers): static abilities, +1/+1 counters, until-end-of-turn effects.
    /// Only additive effects exist so far, so the order inside 7c can't change the result.
    /// Static sources are permanents on the battlefield and Tavern Dwellers in the Tavern Dweller zone.
    /// </summary>
    public static class CharacteristicsCalculator
    {
        public static Characteristics Compute(GameState state, CardDatabase db, CardInstance card)
        {
            var def = db.Get(card.DefinitionId);
            var keywords = def.Keywords;
            int power = def.Power, health = def.Health;

            if (card.Zone == Zone.Battlefield)
            {
                // Layer 6.
                // A player who lost keeps no Tavern Dweller effects; their permanents leave the game with them (§13).
                foreach (var player in state.Players)
                {
                    if (!player.HasLost)
                        foreach (var source in player.TavernDwellerZone) keywords |= Grants(state, db, source, card);
                    foreach (var source in player.Battlefield) keywords |= Grants(state, db, source, card);
                }
                foreach (var mod in state.UntilEndOfTurn)
                    if (mod.Target == card.Id) keywords |= mod.Grants;

                // Layer 7c.
                foreach (var player in state.Players)
                {
                    if (!player.HasLost)
                        foreach (var source in player.TavernDwellerZone) Modify(state, db, source, card, keywords, ref power, ref health);
                    foreach (var source in player.Battlefield) Modify(state, db, source, card, keywords, ref power, ref health);
                }
                power += card.PlusOneCounters;
                health += card.PlusOneCounters;
                foreach (var mod in state.UntilEndOfTurn)
                {
                    if (mod.Target != card.Id) continue;
                    power += mod.Power;
                    health += mod.Health;
                }
            }

            return new Characteristics
            {
                Power = power,
                MaxHealth = health,
                Keywords = keywords,
                RemainingHealth = health - card.Damage,
            };
        }

        private static Keyword Grants(GameState state, CardDatabase db, CardInstance source, CardInstance card)
        {
            var statics = db.Get(source.DefinitionId).Statics;
            var k = Keyword.None;
            for (int i = 0; i < statics.Count; i++) k |= statics[i].GrantsKeywords(state, db, source, card);
            return k;
        }

        private static void Modify(GameState state, CardDatabase db, CardInstance source, CardInstance card, Keyword keywords,
            ref int power, ref int health)
        {
            var statics = db.Get(source.DefinitionId).Statics;
            for (int i = 0; i < statics.Count; i++) statics[i].ModifyPowerHealth(state, db, source, card, keywords, ref power, ref health);
        }

        /// <summary>"Can't be healed" (§11.1): does any permanent stop Heal effects on this creature?</summary>
        public static bool CantBeHealed(GameState state, CardDatabase db, CardInstance creature)
        {
            foreach (var p in state.Players)
                foreach (var source in p.Battlefield)
                    foreach (var st in db.Get(source.DefinitionId).Statics)
                        if (st.PreventsHealing(state, db, source, creature)) return true;
            return false;
        }

        /// <summary>"Can't be dealt more than N damage each turn" (Hardlight Aegis): the lowest N, or int.MaxValue.</summary>
        public static int MaxDamageEachTurn(GameState state, CardDatabase db, CardInstance creature)
        {
            int cap = int.MaxValue;
            foreach (var p in state.Players)
                foreach (var source in p.Battlefield)
                {
                    if (source.AttachedToObject != creature.Id) continue;
                    foreach (var st in db.Get(source.DefinitionId).Statics)
                        if (st is AttachedCreatureModifier m && m.MaxDamageEachTurn > 0) cap = Math.Min(cap, m.MaxDamageEachTurn);
                }
            return cap;
        }

        /// <summary>Is an Equipment attached to this creature?</summary>
        public static bool IsEquipped(GameState state, CardDatabase db, CardInstance creature)
        {
            foreach (var p in state.Players)
                foreach (var c in p.Battlefield)
                    if (c.AttachedToObject == creature.Id && db.Get(c.DefinitionId).Type == CardType.Equipment) return true;
            return false;
        }
    }

    /// <summary>"Gain control of it until end of turn": control goes back to <see cref="ReturnTo"/> in the cleanup step.</summary>
    public sealed class TemporaryControl
    {
        public ObjectId Object { get; set; }
        public PlayerId ReturnTo { get; set; }

        public TemporaryControl Clone() => (TemporaryControl)MemberwiseClone();
    }

    /// <summary>"... until end of turn" on one object. Removed in the cleanup step.</summary>
    public sealed class TemporaryModifier
    {
        public ObjectId Target { get; set; }
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Grants { get; set; }

        public TemporaryModifier Clone() => (TemporaryModifier)MemberwiseClone();
    }
}
