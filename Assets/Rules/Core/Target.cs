using System;

namespace RestartedTavern.Rules
{
    /// <summary>What a spell or ability may target.</summary>
    public enum TargetSpec
    {
        None,
        /// <summary>"any target": a creature or a player.</summary>
        AnyTarget,
        Creature,
        CreatureYouControl,
        CreatureYouDontControl,
        /// <summary>"a creature or your Tavern Dweller" (Barkeep's Tonic).</summary>
        CreatureOrYou,
        Player,
        Opponent,
        /// <summary>Curses: an enemy creature or an opponent (GAME_DESIGN §10).</summary>
        EnemyCreatureOrOpponent,
        /// <summary>"target Construct or equipped creature" (Back-Street Mechanic, Patch-Up Drone).</summary>
        ConstructOrEquippedCreature,
        /// <summary>"target Equipment you control" (Courier Bot, Sparkwrench).</summary>
        EquipmentYouControl,
        /// <summary>"a creature card from your graveyard" (The Rotmother).</summary>
        CreatureCardInYourGraveyard,
        /// <summary>"a creature card from a graveyard", any player's (Body Snatcher).</summary>
        CreatureCardInAGraveyard,
        /// <summary>"target Equipment, Relic or Curse", anyone's (Last Call).</summary>
        EquipmentRelicOrCurse,
        /// <summary>"target spell" on the Chain (Hush Money, Counterfeit Coin). Chosen by its ChainItem.ObjectId.</summary>
        SpellOnChain,
        /// <summary>"target spell or ability" on the Chain, triggered abilities and Tavern Dweller Powers included (Bribe the Referee).</summary>
        SpellOrAbilityOnChain,
    }

    /// <summary>
    /// One "target" in a card's text. A spell can have several (Primal Clash: "target creature you
    /// control fights target creature you don't control"). The same object can't be chosen twice.
    /// Optional slots ("up to three target creatures") must come last.
    /// </summary>
    public sealed class TargetSlot
    {
        public TargetSpec Spec { get; set; }
        public bool Optional { get; set; }
        /// <summary>Only creatures with this subtype ("other Goobers you control"). Null = any.</summary>
        public string Subtype { get; set; }
        /// <summary>"target damaged creature": it has damage on it (GAME_DESIGN §11.1).</summary>
        public bool Damaged { get; set; }
        /// <summary>"target creature with 2 or less Health remaining" (Finisher Protocol). Null = any.</summary>
        public int? MaxRemainingHealth { get; set; }
        /// <summary>"target attacking or blocking creature" (Called Shot).</summary>
        public bool AttackingOrBlocking { get; set; }
        /// <summary>"with cost 4 or less": the printed cost of a creature or spell (Bounced Check, Counterfeit Coin). Null = any.</summary>
        public int? MaxCost { get; set; }
        /// <summary>"target creature you control with Trample" (Mukk's Power): it has the keyword right now. None = any.</summary>
        public Keyword Keyword { get; set; }

        public static TargetSlot Of(TargetSpec spec, bool optional = false, string subtype = null) =>
            new TargetSlot { Spec = spec, Optional = optional, Subtype = subtype };

        /// <summary>Same spec and the same filters (for listing {A,B} but not {B,A}).</summary>
        public bool SameFilterAs(TargetSlot other) =>
            Spec == other.Spec && Subtype == other.Subtype && Damaged == other.Damaged
            && MaxRemainingHealth == other.MaxRemainingHealth && AttackingOrBlocking == other.AttackingOrBlocking
            && MaxCost == other.MaxCost && Keyword == other.Keyword;
    }

    /// <summary>A chosen target: either a player or a game object.</summary>
    public readonly struct Target : IEquatable<Target>
    {
        public readonly bool IsPlayer;
        public readonly PlayerId Player;
        public readonly ObjectId Object;

        private Target(bool isPlayer, PlayerId player, ObjectId obj)
        {
            IsPlayer = isPlayer;
            Player = player;
            Object = obj;
        }

        public static Target ForPlayer(PlayerId player) => new Target(true, player, ObjectId.None);
        public static Target ForObject(ObjectId obj) => new Target(false, default, obj);

        public bool Equals(Target other) =>
            IsPlayer == other.IsPlayer && (IsPlayer ? Player == other.Player : Object == other.Object);

        public override bool Equals(object obj) => obj is Target other && Equals(other);
        public static bool operator ==(Target a, Target b) => a.Equals(b);
        public static bool operator !=(Target a, Target b) => !a.Equals(b);
        public override int GetHashCode() => IsPlayer ? Player.Value * 2 + 1 : Object.Value * 2;
        public override string ToString() => IsPlayer ? Player.ToString() : Object.ToString();
    }
}
