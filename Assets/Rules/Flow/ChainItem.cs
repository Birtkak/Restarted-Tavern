using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    public enum ChainItemKind
    {
        Spell,
        TriggeredAbility,
        ActivatedAbility,
    }

    /// <summary>
    /// One pending spell or ability on the Chain (GAME_DESIGN §8, MTG: the stack).
    /// </summary>
    public sealed class ChainItem
    {
        public int Id { get; set; }
        /// <summary>
        /// The object id it can be targeted by ("counter target spell or ability"). For spells this is the
        /// card's id on the Chain; abilities get a fresh id (MTG: everything on the stack is an object).
        /// </summary>
        public ObjectId ObjectId { get; set; }
        public ChainItemKind Kind { get; set; }
        public PlayerId Controller { get; set; }

        /// <summary>For spells: the card itself, in the Chain zone. Null for abilities.</summary>
        public CardInstance Card { get; set; }

        /// <summary>The object the ability came from (last known), or the spell's own id.</summary>
        public ObjectId SourceId { get; set; }
        public string SourceDefinitionId { get; set; }

        /// <summary>What each chosen target had to be. Targets[i] was chosen for TargetSlots[i].</summary>
        public List<TargetSlot> TargetSlots { get; set; } = new List<TargetSlot>();
        public List<Target> Targets { get; set; } = new List<Target>();
        /// <summary>The source can't be its own target ("another creature").</summary>
        public bool TargetsExcludeSource { get; set; }

        /// <summary>Shared, immutable references into the card definition.</summary>
        public List<Effect> Effects { get; set; } = new List<Effect>();

        public bool Invested { get; set; }
        /// <summary>The X paid: an activated ability's X, or a spell's "pay any amount of Gold (X)".</summary>
        public int X { get; set; }
        /// <summary>Triggered abilities: the amount of the event ("heal that much"), e.g. the Gold banked.</summary>
        public int EventAmount { get; set; }
        /// <summary>Triggered abilities: the object the event was about ("put a counter on it").</summary>
        public ObjectId EventObject { get; set; }
        /// <summary>Triggered abilities: the player the event was about ("its controller loses 1 life").</summary>
        public PlayerId? EventPlayer { get; set; }
        /// <summary>Spells with "sacrifice a creature" as an extra cost: its last known Power.</summary>
        public int SacrificedPower { get; set; }
        /// <summary>Spells: the Gold spent to cast it, Invest included ("if 3 or more Gold was spent to cast it").</summary>
        public int GoldPaid { get; set; }
        /// <summary>Divided damage: the amount for each target (MTG 601.2d).</summary>
        public int[] Division { get; set; } = System.Array.Empty<int>();
        /// <summary>Triggered abilities: the intervening "if", checked again on resolution (MTG 603.4).</summary>
        public TriggerCondition Condition { get; set; }
        /// <summary>Abilities: the ability's rules text (for UIs and logs).</summary>
        public string Text { get; set; } = "";
        /// <summary>A Tavern Dweller Power (GAME_DESIGN §9.1).</summary>
        public bool IsTavernDwellerPower { get; set; }
        /// <summary>
        /// Triggered abilities merged from several events (Scrap Collector: three equipped creatures died at once): the
        /// events after the first. The effects run once per event. Null = one event. Never changed after it's made.
        /// </summary>
        public List<TriggerEventInfo> More { get; set; }
        /// <summary>How many times it does its thing ("×3" on the table).</summary>
        public int Times => 1 + (More?.Count ?? 0);

        public ChainItem Clone()
        {
            var c = (ChainItem)MemberwiseClone();
            c.Card = Card?.Clone();
            c.Targets = new List<Target>(Targets);
            return c;
        }

        public override string ToString() => Kind + " " + SourceDefinitionId + " (" + Controller + ")";
    }

    /// <summary>
    /// A delayed triggered ability (MTG 603.7): "At the end of your turn, exile it." (Encore From Beyond).
    /// It triggers once, at the end of <see cref="Controller"/>'s turn, about <see cref="EventObject"/>.
    /// </summary>
    public sealed class DelayedTrigger
    {
        public TriggeredAbility Ability { get; set; }
        public PlayerId Controller { get; set; }
        public ObjectId SourceId { get; set; }
        public string SourceDefinitionId { get; set; }
        public ObjectId EventObject { get; set; }

        public DelayedTrigger Clone() => (DelayedTrigger)MemberwiseClone();
    }

    /// <summary>One event a merged trigger is about: "that much", "it", "that player".</summary>
    public sealed class TriggerEventInfo
    {
        public int Amount { get; set; }
        public ObjectId EventObject { get; set; }
        public PlayerId? EventPlayer { get; set; }
    }

    /// <summary>A triggered ability that triggered but isn't on the Chain yet (MTG 603.3).</summary>
    public sealed class PendingTrigger
    {
        public TriggeredAbility Ability { get; set; }
        public PlayerId Controller { get; set; }
        public ObjectId SourceId { get; set; }
        public string SourceDefinitionId { get; set; }
        /// <summary>The amount of the event that triggered it (Gold banked or spent, damage dealt), for "that much".</summary>
        public int Amount { get; set; }
        /// <summary>The object the event was about (the creature dealt damage, healed or entering).</summary>
        public ObjectId EventObject { get; set; }
        /// <summary>The player the event was about.</summary>
        public PlayerId? EventPlayer { get; set; }
        /// <summary>Events merged into this trigger after the first (see ChainItem.More). Replaced, never changed in place.</summary>
        public List<TriggerEventInfo> More { get; set; }

        public PendingTrigger Clone() => (PendingTrigger)MemberwiseClone();
    }
}
