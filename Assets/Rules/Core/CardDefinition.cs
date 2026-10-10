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

        /// <summary>
        /// "As an extra cost, pay any amount of Gold (X)" (Settle the Tab). Paid only with Gold, like
        /// Invest; X can be 0. The effects read it as <see cref="EffectContext.X"/>.
        /// </summary>
        public bool XGoldExtraCost { get; set; }
        /// <summary>
        /// "X" in the cost (set v0.3): the cost is the printed cost plus X, chosen on casting (at least 1) and paid like the
        /// card (§5.2: spells with mana and Gold, permanents with mana only). Effects read it as X.
        /// </summary>
        public bool XCost { get; set; }
        /// <summary>"Target creature with cost X or less" (Eviction Notice): creature targets must cost X or less.</summary>
        public bool TargetMaxCostIsX { get; set; }

        /// <summary>
        /// "Deal N damage divided as you choose among any number of targets" (Firecracker Volley): the total. The
        /// split is chosen on casting (MTG 601.2d), at least 1 per target (PlayerAction.Division).
        /// </summary>
        public int DividedDamage { get; set; }

        /// <summary>"Can block an additional creature each combat" (Retired Champion): how many extra.</summary>
        public int ExtraBlocks { get; set; }

        /// <summary>"You may cast this whenever you could cast an Instant" (Retainer Mage; MTG Flash).</summary>
        public bool Flash { get; set; }
        /// <summary>"You may pay for it with Gold": a permanent that Gold can help pay for, mana first (Retainer Mage).</summary>
        public bool GoldMayPay { get; set; }

        /// <summary>"As an extra cost, pay N life" (Blood Price). You need at least N life (MTG 119.4).</summary>
        public int ExtraLifeCost { get; set; }
        /// <summary>
        /// "As an extra cost, sacrifice a creature" (Fling the Runt). The creature is chosen with the action
        /// (PlayerAction.Sacrifice); its last known Power is <see cref="EffectContext.SacrificedPower"/>.
        /// </summary>
        public bool SacrificeCreatureCost { get; set; }

        public List<TriggeredAbility> Triggers { get; set; } = new List<TriggeredAbility>();
        public List<StaticAbility> Statics { get; set; } = new List<StaticAbility>();
        /// <summary>Activated abilities (MTG 602), including Equip and a Tavern Dweller's Power.</summary>
        public List<ActivatedAbility> Abilities { get; set; } = new List<ActivatedAbility>();

        private bool? _hasReplacement;
        /// <summary>Has a replacement effect among its statics (cached: definitions don't change once the database is built).</summary>
        internal bool HasReplacement => _hasReplacement ?? (_hasReplacement = Statics.Exists(s => s is ReplacementAbility)).Value;

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
        /// <summary>An Equipment becomes attached to a creature (Equip or anything else). Subject = the creature's controller.</summary>
        EquipmentAttached,

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
        /// <summary>
        /// "Whenever you bank Gold" (GAME_DESIGN §5.2): unspent mana became Gold in the cleanup step.
        /// Subject = the player who banked; the amount is the Gold actually gained (EffectContext.EventAmount).
        /// </summary>
        GoldBanked,
        /// <summary>
        /// "Whenever you spend Gold": a player paid Gold for a spell, ability, Invest or tax. Once per
        /// payment (decided 2026-10-09); the amount is the Gold paid. Gold that is lost isn't spent.
        /// </summary>
        GoldSpent,

        /// <summary>
        /// A creature is dealt damage (after prevention). Subject = its controller; EventObject = the creature,
        /// EventPlayer = its controller, EventAmount = the damage. See OnlyAttachedCreature, MaxRemainingHealth.
        /// </summary>
        CreatureDealtDamage,
        /// <summary>A player heals damage from a creature. Subject = the healer (the effect's controller); EventObject = the creature.</summary>
        CreatureHealed,
        /// <summary>A creature enters the battlefield. Subject = its controller; EventObject = the creature. See OthersOnly, MinPower.</summary>
        CreatureEnters,
        /// <summary>
        /// This creature dealt combat damage to a creature that now has lethal damage ("destroys a
        /// creature in combat", Champion's Belt). Once per creature destroyed.
        /// </summary>
        DestroysCreatureInCombat,
        /// <summary>At the start of the turn of the player this Curse is attached to (Curse of Rot). EventPlayer = that player.</summary>
        StartOfEnchantedPlayersTurn,
        /// <summary>
        /// A creature deals combat damage to a player (Pickpocket Boss). Subject = the creature's controller;
        /// EventObject = the creature, EventPlayer = the damaged player. See SubjectSubtype, OthersOnly.
        /// </summary>
        CreatureDealsCombatDamageToPlayer,
        /// <summary>A player attacks: their attackers are declared. Subject = that player; EventAmount = how many attack (Rally Drummer).</summary>
        PlayerAttacks,
        /// <summary>A Curse is put into a graveyard from the battlefield. Subject = its controller (Stage Medium).</summary>
        CurseToGraveyard,
        /// <summary>A player paid Gold for a creature spell's cost (Shady Moneylender's "whenever you do"). Subject = the caster.</summary>
        GoldPaidForCreatureSpell,
        /// <summary>At the start of the turn of the controller of the creature this Curse is attached to (Hex of Withering). EventObject = that creature.</summary>
        StartOfEnchantedCreatureControllersTurn,
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
        /// <summary>"target damaged creature" (Ambush Predator).</summary>
        public bool TargetDamaged { get; set; }
        /// <summary>"with cost 2 or less" on the target (Bone Medium). Null = any.</summary>
        public int? TargetMaxCost { get; set; }

        /// <summary>The target slot, with its filters. Legality is checked again on resolution.</summary>
        public TargetSlot Slot => new TargetSlot { Spec = Target, Damaged = TargetDamaged, MaxCost = TargetMaxCost };

        /// <summary>
        /// An intervening "if" (MTG 603.4): "At the end of your turn, if you have 5 or more Gold, ...". Checked
        /// when it would trigger and again when it resolves. Gets the state, the cards, the controller and the source id.
        /// </summary>
        public Func<GameState, CardDatabase, PlayerId, ObjectId, bool> Condition { get; set; }
        public List<Effect> Effects { get; set; } = new List<Effect>();
        public string Text { get; set; } = "";

        // Conditions for "whenever ..." triggers (CreatureDies, SpellCast, EquipActivated, EquipmentUnattached).
        public TriggerSubject Subject { get; set; } = TriggerSubject.Anyone;
        /// <summary>CreatureDies / CreatureEnters: the creature's (last known) Power is at least this.</summary>
        public int MinPower { get; set; }
        /// <summary>SpellCast: the spell's printed cost is at least this.</summary>
        public int MinCost { get; set; }
        /// <summary>GoldBanked / GoldSpent: "2 or more Gold". 0 = any amount.</summary>
        public int MinAmount { get; set; }
        /// <summary>CreatureEnters / CreatureDealsCombatDamageToPlayer: "another creature": not the source itself.</summary>
        public bool OthersOnly { get; set; }
        /// <summary>CreatureDealsCombatDamageToPlayer: only creatures with this subtype ("another Goober"). Null = any.</summary>
        public string SubjectSubtype { get; set; }
        /// <summary>CreatureDealtDamage: only the creature this Curse or Equipment is attached to (Hex of Festering).</summary>
        public bool OnlyAttachedCreature { get; set; }
        /// <summary>CreatureDealtDamage: "whenever this is dealt damage" (Worldroot Hydra).</summary>
        public bool OnlySelf { get; set; }
        /// <summary>CreatureDealtDamage: "... and survives": it still has Health left after the damage.</summary>
        public bool OnlyIfSurvives { get; set; }
        /// <summary>CreatureDies: only creatures controlled by the player this Curse is attached to (Curse of the Spotlight).</summary>
        public bool OnlyEnchantedPlayer { get; set; }
        /// <summary>SpellCast: "your second spell each turn" (Card Shark). 0 = every spell.</summary>
        public int NthSpellThisTurn { get; set; }
        /// <summary>
        /// CreatureDealtDamage: "if it has N or less Health remaining" (Neon Executioner). An intervening
        /// "if" (MTG 603.4): checked when it triggers; the effect checks again when it resolves.
        /// </summary>
        public int? MaxRemainingHealth { get; set; }
        /// <summary>"This triggers at most N times each turn" (Skabba). 0 = no limit.</summary>
        public int MaxPerTurn { get; set; }

        /// <summary>
        /// Triggers this many times at once, each with its own target (Archon Lumen: one ping per
        /// Equipment you control). Null = once.
        /// </summary>
        public Func<GameState, CardDatabase, CardInstance, int> RepeatCount { get; set; }
    }
}
