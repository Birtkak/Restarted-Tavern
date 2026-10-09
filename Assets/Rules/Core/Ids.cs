using System;

namespace RestartedTavern.Rules
{
    /// <summary>Identifies a player (seat) for the whole game.</summary>
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public readonly int Value;

        public PlayerId(int value) { Value = value; }

        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(PlayerId a, PlayerId b) => a.Value == b.Value;
        public static bool operator !=(PlayerId a, PlayerId b) => a.Value != b.Value;
        public override string ToString() => "P" + Value;
    }

    /// <summary>
    /// Identifies one game object. A card gets a new ObjectId every time it changes zone
    /// (MTG CR 400.7), so effects that point at "that creature" stop applying once it leaves.
    /// </summary>
    public readonly struct ObjectId : IEquatable<ObjectId>
    {
        public readonly int Value;

        public ObjectId(int value) { Value = value; }

        public static readonly ObjectId None = new ObjectId(0);
        public bool IsNone => Value == 0;

        public bool Equals(ObjectId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ObjectId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(ObjectId a, ObjectId b) => a.Value == b.Value;
        public static bool operator !=(ObjectId a, ObjectId b) => a.Value != b.Value;
        public override string ToString() => "#" + Value;
    }
}
