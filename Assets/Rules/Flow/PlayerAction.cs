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
        /// <summary>Activate an ability of a permanent or the Tavern Dweller's Power (MTG 602, GAME_DESIGN §9.1).</summary>
        ActivateAbility,
        /// <summary>Answer a yes/no or pick-one choice made while something resolves (PendingDecision.Options).</summary>
        ChooseOption,
        /// <summary>Divide a creature's combat damage among several creatures (§7.2.6): <see cref="PlayerAction.Division"/>.</summary>
        AssignCombatDamage,
        /// <summary>FormatConfig.AlternatingActions: use this action to attack (goes to combat).</summary>
        GoToCombat,
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
        /// <summary>ActivateAbility: index into the source's abilities (printed first, then granted ones).</summary>
        public int AbilityIndex { get; set; }
        /// <summary>ActivateAbility: the value chosen for X. PlayCard: the Gold paid for "pay any amount of Gold (X)".</summary>
        public int X { get; set; }
        /// <summary>ActivateAbility: the creature sacrificed to pay the cost.</summary>
        public ObjectId Sacrifice { get; set; }
        /// <summary>ChooseOption: the option picked.</summary>
        public int Option { get; set; }
        /// <summary>
        /// PlayCard with divided damage: the damage for each target, in target order (MTG 601.2d).
        /// AssignCombatDamage: the damage for each recipient, in the order of PendingDecision.Choices.
        /// </summary>
        public int[] Division { get; set; } = Array.Empty<int>();

        public static PlayerAction Pass(PlayerId p) => new PlayerAction { Kind = ActionKind.PassPriority, Player = p };
        public static PlayerAction Keep(PlayerId p) => new PlayerAction { Kind = ActionKind.Keep, Player = p };
        public static PlayerAction Mulligan(PlayerId p) => new PlayerAction { Kind = ActionKind.Mulligan, Player = p };
        public static PlayerAction BottomCard(PlayerId p, ObjectId card) => new PlayerAction { Kind = ActionKind.BottomCard, Player = p, Card = card };
        public static PlayerAction Discard(PlayerId p, ObjectId card) => new PlayerAction { Kind = ActionKind.Discard, Player = p, Card = card };
        public static PlayerAction GoToCombat(PlayerId p) => new PlayerAction { Kind = ActionKind.GoToCombat, Player = p };
        public static PlayerAction FinishAttacks(PlayerId p) => new PlayerAction { Kind = ActionKind.FinishAttacks, Player = p };
        public static PlayerAction FinishBlocks(PlayerId p) => new PlayerAction { Kind = ActionKind.FinishBlocks, Player = p };
        public static PlayerAction ChooseTarget(PlayerId p, Target t) => new PlayerAction { Kind = ActionKind.ChooseTarget, Player = p, Targets = new[] { t } };

        public static PlayerAction ChooseOption(PlayerId p, int option) => new PlayerAction { Kind = ActionKind.ChooseOption, Player = p, Option = option };

        public static PlayerAction AssignDamage(PlayerId p, int[] division) =>
            new PlayerAction { Kind = ActionKind.AssignCombatDamage, Player = p, Division = division };

        public static PlayerAction Activate(PlayerId p, ObjectId source, int abilityIndex, Target[] targets = null, int x = 0,
            ObjectId sacrifice = default) =>
            new PlayerAction
            {
                Kind = ActionKind.ActivateAbility, Player = p, Card = source, AbilityIndex = abilityIndex,
                Targets = targets ?? Array.Empty<Target>(), X = x, Sacrifice = sacrifice,
            };

        public static PlayerAction Play(PlayerId p, ObjectId card, Target? target = null, bool invest = false) =>
            Play(p, card, target.HasValue ? new[] { target.Value } : Array.Empty<Target>(), invest);

        public static PlayerAction Play(PlayerId p, ObjectId card, Target[] targets, bool invest = false, int x = 0) =>
            new PlayerAction { Kind = ActionKind.PlayCard, Player = p, Card = card, Targets = targets, Invest = invest, X = x };

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
                && Invest == other.Invest && AbilityIndex == other.AbilityIndex && X == other.X
                && Sacrifice == other.Sacrifice && Option == other.Option && Division.SequenceEqual(other.Division);
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
                h = h * 31 + AbilityIndex;
                h = h * 31 + X;
                h = h * 31 + Sacrifice.Value;
                h = h * 31 + Option;
                foreach (var d in Division) h = h * 31 + d;
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
            if (Kind == ActionKind.ActivateAbility) sb.Append(" ability").Append(AbilityIndex);
            if (X != 0) sb.Append(" X=").Append(X);
            if (!Sacrifice.IsNone) sb.Append(" sac ").Append(Sacrifice);
            if (Kind == ActionKind.ChooseOption) sb.Append(' ').Append(Option);
            if (Division.Length > 0) sb.Append(" split ").Append(string.Join("/", Division));
            return sb.ToString();
        }
    }
}
