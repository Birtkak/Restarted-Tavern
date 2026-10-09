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
        /// <summary>Abilities: the ability's rules text (for UIs and logs).</summary>
        public string Text { get; set; } = "";
        /// <summary>A Tavern Dweller Power (GAME_DESIGN §9.1).</summary>
        public bool IsTavernDwellerPower { get; set; }

        public ChainItem Clone()
        {
            var c = (ChainItem)MemberwiseClone();
            c.Card = Card?.Clone();
            c.Targets = new List<Target>(Targets);
            return c;
        }

        public override string ToString() => Kind + " " + SourceDefinitionId + " (" + Controller + ")";
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

        public PendingTrigger Clone() => (PendingTrigger)MemberwiseClone();
    }
}
