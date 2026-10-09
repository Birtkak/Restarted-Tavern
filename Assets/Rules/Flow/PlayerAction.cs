using System;
using System.Linq;
using System.Text;

namespace RestartedTavern.Rules
{
    public enum ActionKind
    {
        PassPriority,
        Keep,
        Mulligan,
        BottomCard,
        PlayCard,
        DeclareAttacker,
        FinishAttacks,
        DeclareBlocker,
        FinishBlocks,
        ChooseTarget,
        Discard,
    }

    /// <summary>
    /// Everything a player can do, as plain data, so a game can be replayed from its seed and
    /// action list (DEVELOPMENT §1.3). Attacks and blocks are declared one creature at a time,
    /// which keeps the list of legal actions small enough to enumerate for UI and AI.
    /// </summary>
    public sealed class PlayerAction : IEquatable<PlayerAction>
    {
        public ActionKind Kind { get; set; }
        public PlayerId Player { get; set; }
        /// <summary>The card played / attacker / blocker / card to discard or bottom.</summary>
        public ObjectId Card { get; set; }
        /// <summary>DeclareBlocker: the attacker being blocked.</summary>
        public ObjectId BlockedAttacker { get; set; }
        /// <summary>DeclareAttacker: the player being attacked.</summary>
        public PlayerId Defender { get; set; }
        /// <summary>Chosen targets, in the order of the card's target slots. Empty when there are none.</summary>
        public Target[] Targets { get; set; } = Array.Empty<Target>();
        /// <summary>The first target, if any.</summary>
        public Target? Target => Targets.Length > 0 ? Targets[0] : (Target?)null;
        /// <summary>PlayCard: also pay the Invest cost (Gold only).</summary>
        public bool Invest { get; set; }

        public static PlayerAction Pass(PlayerId p) => new PlayerAction { Kind = ActionKind.PassPriority, Player = p };
        public static PlayerAction Keep(PlayerId p) => new PlayerAction { Kind = ActionKind.Keep, Player = p };
        public static PlayerAction Mulligan(PlayerId p) => new PlayerAction { Kind = ActionKind.Mulligan, Player = p };
        public static PlayerAction BottomCard(PlayerId p, ObjectId card) => new PlayerAction { Kind = ActionKind.BottomCard, Player = p, Card = card };
        public static PlayerAction Discard(PlayerId p, ObjectId card) => new PlayerAction { Kind = ActionKind.Discard, Player = p, Card = card };
        public static PlayerAction FinishAttacks(PlayerId p) => new PlayerAction { Kind = ActionKind.FinishAttacks, Player = p };
        public static PlayerAction FinishBlocks(PlayerId p) => new PlayerAction { Kind = ActionKind.FinishBlocks, Player = p };
        public static PlayerAction ChooseTarget(PlayerId p, Target t) => new PlayerAction { Kind = ActionKind.ChooseTarget, Player = p, Targets = new[] { t } };

        public static PlayerAction Play(PlayerId p, ObjectId card, Target? target = null, bool invest = false) =>
            Play(p, card, target.HasValue ? new[] { target.Value } : Array.Empty<Target>(), invest);

        public static PlayerAction Play(PlayerId p, ObjectId card, Target[] targets, bool invest = false) =>
            new PlayerAction { Kind = ActionKind.PlayCard, Player = p, Card = card, Targets = targets, Invest = invest };

        public static PlayerAction Attack(PlayerId p, ObjectId attacker, PlayerId defender) =>
            new PlayerAction { Kind = ActionKind.DeclareAttacker, Player = p, Card = attacker, Defender = defender };

        public static PlayerAction Block(PlayerId p, ObjectId blocker, ObjectId attacker) =>
            new PlayerAction { Kind = ActionKind.DeclareBlocker, Player = p, Card = blocker, BlockedAttacker = attacker };

        public bool Equals(PlayerAction other)
        {
            if (other is null) return false;
            return Kind == other.Kind && Player == other.Player && Card == other.Card
                && BlockedAttacker == other.BlockedAttacker && Defender == other.Defender
                && Targets.SequenceEqual(other.Targets)
                && Invest == other.Invest;
        }

        public override bool Equals(object obj) => obj is PlayerAction other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = (int)Kind;
                h = h * 31 + Player.Value;
                h = h * 31 + Card.Value;
                h = h * 31 + BlockedAttacker.Value;
                h = h * 31 + Defender.Value;
                foreach (var t in Targets) h = h * 31 + t.GetHashCode();
                h = h * 31 + (Invest ? 1 : 0);
                return h;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Player).Append(' ').Append(Kind);
            if (!Card.IsNone) sb.Append(' ').Append(Card);
            if (Kind == ActionKind.DeclareAttacker) sb.Append(" -> ").Append(Defender);
            if (Kind == ActionKind.DeclareBlocker) sb.Append(" blocks ").Append(BlockedAttacker);
            if (Targets.Length > 0) sb.Append(" @").Append(string.Join(",", Targets));
            if (Invest) sb.Append(" invest");
            return sb.ToString();
        }
    }
}
