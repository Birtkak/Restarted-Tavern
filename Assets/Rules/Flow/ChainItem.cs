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
        public ChainItemKind Kind { get; set; }
        public PlayerId Controller { get; set; }

        /// <summary>For spells: the card itself, in the Chain zone. Null for abilities.</summary>
        public CardInstance Card { get; set; }

        /// <summary>The object the ability came from (last known), or the spell's own id.</summary>
        public ObjectId SourceId { get; set; }
        public string SourceDefinitionId { get; set; }

        public TargetSpec TargetSpec { get; set; }
        public List<Target> Targets { get; set; } = new List<Target>();

        /// <summary>Shared, immutable references into the card definition.</summary>
        public List<Effect> Effects { get; set; } = new List<Effect>();

        public bool Overcharged { get; set; }

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

        public PendingTrigger Clone() => (PendingTrigger)MemberwiseClone();
    }
}
