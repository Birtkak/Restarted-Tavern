using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    public sealed class AttackDeclaration
    {
        public ObjectId Attacker { get; set; }
        /// <summary>Each attacker attacks one player (GAME_DESIGN §7.2, §13).</summary>
        public PlayerId Defender { get; set; }
        /// <summary>Stays true even if all its blockers leave combat (MTG 509.1h).</summary>
        public bool Blocked { get; set; }

        public AttackDeclaration Clone() => (AttackDeclaration)MemberwiseClone();
    }

    public sealed class BlockDeclaration
    {
        public ObjectId Blocker { get; set; }
        public ObjectId Attacker { get; set; }

        public BlockDeclaration Clone() => (BlockDeclaration)MemberwiseClone();
    }

    /// <summary>How a creature's controller divided its combat damage (§7.2.6).</summary>
    public sealed class DamageAssignment
    {
        public ObjectId Dealer { get; set; }
        public List<ObjectId> Recipients { get; set; } = new List<ObjectId>();
        /// <summary>Same order as <see cref="Recipients"/>.</summary>
        public int[] Amounts { get; set; }

        public DamageAssignment Clone() =>
            new DamageAssignment { Dealer = Dealer, Recipients = new List<ObjectId>(Recipients), Amounts = (int[])Amounts.Clone() };
    }

    /// <summary>The current combat. Exists from declare attackers until the end of combat.</summary>
    public sealed class CombatState
    {
        public List<AttackDeclaration> Attacks { get; set; } = new List<AttackDeclaration>();
        /// <summary>In declaration order, which is also the damage assignment order.</summary>
        public List<BlockDeclaration> Blocks { get; set; } = new List<BlockDeclaration>();
        /// <summary>Defending players who already declared blockers.</summary>
        public List<PlayerId> DoneBlocking { get; set; } = new List<PlayerId>();
        /// <summary>Damage divisions chosen at the start of the combat damage step.</summary>
        public List<DamageAssignment> Assignments { get; set; } = new List<DamageAssignment>();

        public bool IsAttacking(ObjectId id) => Attacks.Exists(a => a.Attacker == id);
        public bool IsBlocking(ObjectId id) => Blocks.Exists(b => b.Blocker == id);
        public AttackDeclaration AttackOf(ObjectId attacker) => Attacks.Find(a => a.Attacker == attacker);

        /// <summary>MTG 506.4: a creature whose controller changes is removed from combat.</summary>
        public void Remove(ObjectId creature)
        {
            Attacks.RemoveAll(a => a.Attacker == creature);
            Blocks.RemoveAll(b => b.Blocker == creature || b.Attacker == creature);
        }

        public CombatState Clone()
        {
            var c = new CombatState { DoneBlocking = new List<PlayerId>(DoneBlocking) };
            foreach (var a in Attacks) c.Attacks.Add(a.Clone());
            foreach (var b in Blocks) c.Blocks.Add(b.Clone());
            foreach (var a in Assignments) c.Assignments.Add(a.Clone());
            return c;
        }
    }
}
