using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// The printed card: immutable once registered in a <see cref="CardDatabase"/>.
    /// Game objects (<see cref="CardInstance"/>) only store the definition id.
    /// Tavern Dwellers are card definitions too (<see cref="CardType.TavernDweller"/>): their passive is a
    /// triggered or static ability and their Power is an activated ability.
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
        /// <summary>Tavern Dwellers only: the two factions the Tavern Dweller unlocks for its deck (GAME_DESIGN §9.2).</summary>
        public string[] TavernDwellerFactions { get; set; } = Array.Empty<string>();
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
        /// <summary>Activated abilities (MTG 602), including Equip and a Tavern Dweller's Power.</summary>
        public List<ActivatedAbility> Abilities { get; set; } = new List<ActivatedAbility>();

        public bool IsPermanent => Type != CardType.Instant && Type != CardType.Sorcery && Type != CardType.TavernDweller;
        public bool IsCreature => Type == CardType.Creature;
        public bool IsTavernDweller => Type == CardType.TavernDweller;
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
        /// <summary>This creature deals combat damage to a player.</summary>
        DealsCombatDamageToPlayer,
        /// <summary>An Equipment becomes attached to this creature.</summary>
        EquipmentAttachedToThis,

        // "Whenever ..." abilities that watch other objects. They work from the battlefield and
        // the Tavern Dweller zone; see TriggeredAbility.Subject for whose objects they watch.

        /// <summary>A creature dies. Subject = its controller.</summary>
        CreatureDies,
        /// <summary>A player casts a spell. Subject = the caster.</summary>
        SpellCast,
        /// <summary>A player activates an Equip ability ("whenever you pay an Equip cost"). Subject = that player.</summary>
        EquipActivated,
        /// <summary>An Equipment becomes unattached. Subject = the Equipment's controller.</summary>
        EquipmentUnattached,
    }

    /// <summary>Whose objects or actions a "whenever ..." trigger watches.</summary>
    public enum TriggerSubject
    {
        Anyone,
        You,
        Opponents,
    }

    /// <summary>A triggered ability. It goes on the Chain when it triggers (GAME_DESIGN §8).</summary>
    public sealed class TriggeredAbility
    {
        public TriggerEvent When { get; set; }
        public TargetSpec Target { get; set; } = TargetSpec.None;
        /// <summary>"another creature": the source itself can't be the target.</summary>
        public bool TargetNotSelf { get; set; }
        /// <summary>"You may ...": the controller can choose no target, and then nothing happens.</summary>
        public bool TargetOptional { get; set; }
        public List<Effect> Effects { get; set; } = new List<Effect>();
        public string Text { get; set; } = "";

        // Conditions for "whenever ..." triggers (CreatureDies, SpellCast, EquipActivated, EquipmentUnattached).
        public TriggerSubject Subject { get; set; } = TriggerSubject.Anyone;
        /// <summary>CreatureDies: the creature's last known Power is at least this.</summary>
        public int MinPower { get; set; }
        /// <summary>SpellCast: the spell's printed cost is at least this.</summary>
        public int MinCost { get; set; }
        /// <summary>"This triggers at most N times each turn" (Skabba). 0 = no limit.</summary>
        public int MaxPerTurn { get; set; }

        /// <summary>
        /// Triggers this many times at once, each with its own target (Archon Lumen: one ping per
        /// Equipment you control). Null = once.
        /// </summary>
        public Func<GameState, CardDatabase, CardInstance, int> RepeatCount { get; set; }
    }
}
