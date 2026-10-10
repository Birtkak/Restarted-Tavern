using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>The kinds of events a replacement effect can change (MTG 614.1).</summary>
    public enum ReplacementEvent
    {
        None,
        /// <summary>A creature would be put into a graveyard from the battlefield ("If it would die, exile it instead").</summary>
        Dies,
        /// <summary>Damage would be dealt to a creature or a player (prevention and modification, MTG 614.1a, 615).</summary>
        DamageDealt,
        /// <summary>A permanent would enter the battlefield ("enters with a +1/+1 counter", "enters tapped", MTG 614.1c).</summary>
        Enters,
        /// <summary>A player would draw a card ("If you would draw a card, ... instead", MTG 614.1a).</summary>
        Draw,
        /// <summary>A player would gain life (heal their Tavern Dweller, Lifelink, "you gain N life").</summary>
        GainLife,
        /// <summary>A player would gain Gold (banking included).</summary>
        GainGold,
    }

    /// <summary>
    /// "If [event] would happen, [something else] instead" (MTG 614). A static ability of a permanent or a Tavern Dweller,
    /// or created for a while by a resolving effect (<see cref="ActiveReplacement"/>). All data, no code, so it can be
    /// described in card files: filters say which events it applies to, outcome fields say how it changes them.
    /// </summary>
    public sealed class ReplacementAbility : StaticAbility
    {
        public ReplacementEvent Event { get; set; }

        // ---------------------------------------------------------------- which events (all must hold)

        /// <summary>
        /// Whose event, seen from the replacement's controller: the affected object's controller, the damaged player, the
        /// player who draws or gains.
        /// </summary>
        public TriggerSubject Affects { get; set; } = TriggerSubject.Anyone;
        /// <summary>Only the source itself ("If this would die", "This enters with ...").</summary>
        public bool OnlySelf { get; set; }
        /// <summary>Only the creature the source is attached to ("If equipped creature would die").</summary>
        public bool OnlyAttachedCreature { get; set; }
        /// <summary>
        /// Only the object the replacement was created for (<see cref="ActiveReplacement.AffectedObject"/>: "if target creature
        /// would die this turn").
        /// </summary>
        public bool OnlyChosenObject { get; set; }
        /// <summary>Only creatures with this subtype. Null = any.</summary>
        public string Subtype { get; set; }
        /// <summary>Enters: only creatures with this much Health or more, as they would exist on the battlefield (MTG 614.12).</summary>
        public int MinHealth { get; set; }
        /// <summary>DamageDealt: only damage to creatures (true) or only to players (false). Null = both.</summary>
        public bool? ToCreatures { get; set; }
        /// <summary>DamageDealt: whose sources, seen from the replacement's controller ("a source you control").</summary>
        public TriggerSubject SourceControlledBy { get; set; } = TriggerSubject.Anyone;
        /// <summary>DamageDealt: only combat damage.</summary>
        public bool OnlyCombat { get; set; }
        /// <summary>
        /// A self-replacement effect (MTG 614.15): part of the card's own instructions, applied before the others.
        /// </summary>
        public bool SelfReplacement { get; set; }

        // ---------------------------------------------------------------- what changes

        /// <summary>Dies: where the creature goes instead (Exile, Hand, or the bottom of the Deck with <see cref="ToBottom"/>).</summary>
        public Zone? Destination { get; set; }
        public bool ToBottom { get; set; }
        /// <summary>Amounts (damage, cards drawn, life, Gold): prevent this much first (MTG 615). With PreventAll, all of it.</summary>
        public int Prevent { get; set; }
        public bool PreventAll { get; set; }
        /// <summary>Amounts: then multiply ("double") and add ("that much plus 1").</summary>
        public int Multiply { get; set; } = 1;
        public int Add { get; set; }
        /// <summary>Enters: extra +1/+1 counters, and/or tapped.</summary>
        public int Counters { get; set; }
        public bool EntersTapped { get; set; }
        /// <summary>The event doesn't happen at all ("skip that draw"). <see cref="Instead"/> runs in its place.</summary>
        public bool Skip { get; set; }
        /// <summary>
        /// Effects that happen instead (with <see cref="Skip"/>) or as well (MTG "... instead" with the original event changed).
        /// They run at once, not on the Chain, controlled by the replacement's controller; EventPlayer is the affected player
        /// and EventObject the affected object.
        /// </summary>
        public List<Effect> Instead { get; set; } = new List<Effect>();
    }

    /// <summary>
    /// A replacement effect created by a resolving spell or ability ("Until end of turn, if target creature would die, ...",
    /// "The next time ... this turn"). Kept in <see cref="GameState.Replacements"/>.
    /// </summary>
    public sealed class ActiveReplacement
    {
        public ReplacementAbility Ability { get; set; }
        public PlayerId Controller { get; set; }
        public ObjectId Source { get; set; }
        public string SourceDefinitionId { get; set; }
        /// <summary>The object it was created for (<see cref="ReplacementAbility.OnlyChosenObject"/>).</summary>
        public ObjectId AffectedObject { get; set; }
        /// <summary>Removed in the cleanup step.</summary>
        public bool UntilEndOfTurn { get; set; }
        /// <summary>"The next time ...": how many more times it applies. 0 = no limit.</summary>
        public int UsesLeft { get; set; }
        public long Timestamp { get; set; }

        public ActiveReplacement Clone() => (ActiveReplacement)MemberwiseClone();
    }

    /// <summary>One event while replacement effects look at it. They change its fields before it happens.</summary>
    public sealed class ReplaceableEvent
    {
        public ReplacementEvent Kind { get; set; }
        /// <summary>The player it happens to: the affected object's controller, the damaged / drawing / gaining player.</summary>
        public PlayerId AffectedPlayer { get; set; }
        /// <summary>The creature that would die, enter or be dealt damage. Null for player events.</summary>
        public CardInstance Object { get; set; }
        /// <summary>DamageDealt: the source and whether it's combat damage.</summary>
        public ObjectId DamageSource { get; set; }
        /// <summary>DamageDealt: who controls the source (a resolving spell is no longer on the Chain, so it's passed in).</summary>
        public PlayerId? DamageSourceController { get; set; }
        public bool IsCombat { get; set; }
        /// <summary>Damage, cards drawn (1), life or Gold.</summary>
        public int Amount { get; set; }
        /// <summary>Dies: where it goes (starts as the Graveyard).</summary>
        public Zone Destination { get; set; }
        public bool ToBottom { get; set; }
        public int Counters { get; set; }
        public bool EntersTapped { get; set; }
        public bool Skipped { get; set; }
        /// <summary>Follow-up effects from the replacements that applied, with who controls them.</summary>
        public List<(List<Effect> effects, PlayerId controller, ObjectId source, string sourceDefinitionId)> Instead { get; } =
            new List<(List<Effect>, PlayerId, ObjectId, string)>();

        /// <summary>A creature that would die and still would after the replacements so far.</summary>
        public bool StillDies => Kind == ReplacementEvent.Dies && Destination == Zone.Graveyard;
    }
}
