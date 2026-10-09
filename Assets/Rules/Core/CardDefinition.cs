using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// The printed card: immutable once registered in a <see cref="CardDatabase"/>.
    /// Game objects (<see cref="CardInstance"/>) only store the definition id.
    /// </summary>
    public sealed class CardDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public CardType Type { get; set; }
        public int Cost { get; set; }
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Keywords { get; set; }
        public string[] Subtypes { get; set; } = Array.Empty<string>();
        public Rarity Rarity { get; set; }
        /// <summary>Faction id, or "neutral".</summary>
        public string Faction { get; set; } = "neutral";
        public string Text { get; set; } = "";
        public bool IsToken { get; set; }

        /// <summary>Targets of an Instant/Sorcery (or the attach target of a Curse), in text order.</summary>
        public List<TargetSlot> SpellTargets { get; set; } = new List<TargetSlot>();

        /// <summary>Shorthand for a spell with exactly one required target.</summary>
        public TargetSpec SpellTarget
        {
            get => SpellTargets.Count > 0 ? SpellTargets[0].Spec : TargetSpec.None;
            set => SpellTargets = value == TargetSpec.None ? new List<TargetSlot>() : new List<TargetSlot> { TargetSlot.Of(value) };
        }
        /// <summary>What an Instant or Sorcery does when it resolves.</summary>
        public List<Effect> SpellEffects { get; set; } = new List<Effect>();

        /// <summary>Invest X (GAME_DESIGN §11): optional extra cost, paid only with Gold.</summary>
        public int? InvestCost { get; set; }
        public List<Effect> InvestEffects { get; set; } = new List<Effect>();

        public List<TriggeredAbility> Triggers { get; set; } = new List<TriggeredAbility>();
        public List<StaticAbility> Statics { get; set; } = new List<StaticAbility>();

        public bool IsPermanent => Type != CardType.Instant && Type != CardType.Sorcery;
        public bool IsCreature => Type == CardType.Creature;
        public bool IsLegendary => Rarity == Rarity.Legendary;

        public bool HasSubtype(string subtype)
        {
            foreach (var s in Subtypes)
                if (string.Equals(s, subtype, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public override string ToString() => Name ?? Id;
    }

    public enum TriggerEvent
    {
        /// <summary>This permanent enters the battlefield (MTG ETB).</summary>
        Arrival,
        /// <summary>This creature dies (MTG "dies").</summary>
        LastBreath,
        /// <summary>This creature is declared as an attacker.</summary>
        Attacks,
        /// <summary>At the start of its controller's turn.</summary>
        StartOfYourTurn,
        /// <summary>At the end of its controller's turn.</summary>
        EndOfYourTurn,
    }

    /// <summary>A triggered ability. It goes on the Chain when it triggers (GAME_DESIGN §8).</summary>
    public sealed class TriggeredAbility
    {
        public TriggerEvent When { get; set; }
        public TargetSpec Target { get; set; } = TargetSpec.None;
        /// <summary>"another creature": the source itself can't be the target.</summary>
        public bool TargetNotSelf { get; set; }
        public List<Effect> Effects { get; set; } = new List<Effect>();
        public string Text { get; set; } = "";
    }
}
