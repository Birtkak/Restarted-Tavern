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

        public static TargetSlot Of(TargetSpec spec, bool optional = false, string subtype = null) =>
            new TargetSlot { Spec = spec, Optional = optional, Subtype = subtype };
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
