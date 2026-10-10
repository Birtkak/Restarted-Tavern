using System;
using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Rules;

namespace RestartedTavern.Client.Logic
{
    /// <summary>What the next question of an <see cref="ActionPicker"/> is about.</summary>
    public enum ChoiceDimension
    {
        /// <summary>Several things can be done with the source: play it, or one of its abilities.</summary>
        Mode,
        /// <summary>Which creature to sacrifice for the cost.</summary>
        Sacrifice,
        /// <summary>The target for slot <see cref="PickerPrompt.Slot"/> (or stop choosing, for optional slots).</summary>
        Target,
        /// <summary>Pay the Invest cost or not.</summary>
        Invest,
        /// <summary>The value of X.</summary>
        X,
        /// <summary>How to divide damage among the targets.</summary>
        Division,
        /// <summary>Which player an attacker attacks.</summary>
        Defender,
        /// <summary>Which attacker a blocker blocks.</summary>
        BlockedAttacker,
    }

    /// <summary>One answer to a <see cref="PickerPrompt"/>.</summary>
    public sealed class PickerOption
    {
        public ChoiceDimension Dimension;
        /// <summary>Mode: the action kind (PlayCard or ActivateAbility) and <see cref="AbilityIndex"/>.</summary>
        public ActionKind Kind;
        public int AbilityIndex;
        /// <summary>Target / Defender / BlockedAttacker / Sacrifice.</summary>
        public Target Target;
        /// <summary>Target dimension: "no more targets" (the remaining slots are optional).</summary>
        public bool StopTargeting;
        public bool Invest;
        public int X;
        public int[] Division;

        public override string ToString()
        {
            switch (Dimension)
            {
                case ChoiceDimension.Mode: return Kind == ActionKind.ActivateAbility ? "ability " + AbilityIndex : Kind.ToString();
                case ChoiceDimension.Target: return StopTargeting ? "no more targets" : Target.ToString();
                case ChoiceDimension.Invest: return Invest ? "invest" : "don't invest";
                case ChoiceDimension.X: return "X=" + X;
                case ChoiceDimension.Division: return string.Join("/", Division);
                default: return Target.ToString();
            }
        }
    }

    /// <summary>The question an <see cref="ActionPicker"/> needs answered before it has exactly one action.</summary>
    public sealed class PickerPrompt
    {
        public ChoiceDimension Dimension;
        /// <summary>Target dimension: the index of the target slot being chosen.</summary>
        public int Slot;
        public List<PickerOption> Options = new List<PickerOption>();

        /// <summary>The targets offered (Target, Defender, BlockedAttacker, Sacrifice), for highlighting on the table.</summary>
        public IEnumerable<Target> Targets => Options.Where(o => !o.StopTargeting).Select(o => o.Target);
    }

    /// <summary>
    /// Turns the engine's flat legal-action list into the click-by-click flow of a card game UI
    /// (DEVELOPMENT §1.3: the client only ever submits actions from the legal list).
    ///
    /// Pick a source (a card in hand, a permanent, the Tavern Dweller); the picker keeps every legal
    /// action that uses it and asks one question at a time (mode, sacrifice, each target, Invest, X,
    /// division, defender, blocked attacker) until exactly one action is left; a question is skipped when
    /// every candidate gives the same answer, except targets: those are always clicked, even when only one is legal. Actions without a source (pass, keep, attack,
    /// trigger targets, options) are in <see cref="SourcelessActions"/>.
    /// </summary>
    public sealed class ActionPicker
    {
        private readonly List<PlayerAction> _legal;
        private List<PlayerAction> _candidates = new List<PlayerAction>();
        /// <summary>Target dimension: how many target slots are already fixed.</summary>
        private int _targetsFixed;
        private readonly HashSet<ChoiceDimension> _fixed = new HashSet<ChoiceDimension>();

        public ActionPicker(IEnumerable<PlayerAction> legal)
        {
            _legal = legal.ToList();
        }

        public IReadOnlyList<PlayerAction> Legal => _legal;

        /// <summary>The source picked with <see cref="Begin"/>, or None.</summary>
        public ObjectId Source { get; private set; } = ObjectId.None;

        /// <summary>The single action left, once every question is answered. Null while choosing.</summary>
        public PlayerAction Ready { get; private set; }

        /// <summary>The next question, or null when nothing is being picked or the action is ready.</summary>
        public PickerPrompt Prompt { get; private set; }

        public bool IsPicking => !Source.IsNone && Ready == null;

        /// <summary>Kinds whose <see cref="PlayerAction.Card"/> is something the player picks up or clicks.</summary>
        private static bool HasSource(PlayerAction a) =>
            !a.Card.IsNone && a.Kind != ActionKind.ChooseTarget && a.Kind != ActionKind.ChooseOption
            && a.Kind != ActionKind.AssignCombatDamage;

        /// <summary>Every object that starts at least one legal action (glow these on the table).</summary>
        public HashSet<ObjectId> Sources => new HashSet<ObjectId>(_legal.Where(HasSource).Select(a => a.Card));

        public bool CanUse(ObjectId source) => _legal.Any(a => HasSource(a) && a.Card == source);

        /// <summary>Actions that aren't started from an object: pass, keep/mulligan, go to combat, finish attacks or blocks,
        /// trigger targets, option choices, combat damage division.</summary>
        public IEnumerable<PlayerAction> SourcelessActions => _legal.Where(a => !HasSource(a));

        /// <summary>A trigger asking for a target: the ChooseTarget action for a clicked target, if legal.</summary>
        public PlayerAction ChooseTargetAction(Target t) =>
            _legal.FirstOrDefault(a => a.Kind == ActionKind.ChooseTarget && a.Targets.Length > 0 && a.Targets[0] == t);

        /// <summary>Start picking an action of this source. Returns false if it has none.</summary>
        public bool Begin(ObjectId source)
        {
            Cancel();
            var list = _legal.Where(a => HasSource(a) && a.Card == source).ToList();
            if (list.Count == 0) return false;
            Source = source;
            _candidates = list;
            Advance();
            return true;
        }

        public void Cancel()
        {
            Source = ObjectId.None;
            Ready = null;
            Prompt = null;
            _candidates = new List<PlayerAction>();
            _targetsFixed = 0;
            _fixed.Clear();
        }

        /// <summary>Answer the current prompt with one of its options.</summary>
        public void Choose(PickerOption option)
        {
            if (Prompt == null) throw new InvalidOperationException("Nothing to choose.");
            if (!Prompt.Options.Contains(option)) throw new ArgumentException("Not an option of the current prompt.");
            _candidates = _candidates.Where(a => Matches(a, option, Prompt.Slot)).ToList();
            if (option.Dimension == ChoiceDimension.Target)
            {
                if (option.StopTargeting) _fixed.Add(ChoiceDimension.Target);
                else _targetsFixed = Prompt.Slot + 1;
            }
            else _fixed.Add(option.Dimension);
            Advance();
        }

        /// <summary>Answer the current prompt by clicking a target (Target, Defender, BlockedAttacker, Sacrifice). False if it isn't offered.</summary>
        public bool ChooseTarget(Target t)
        {
            var option = Prompt?.Options.FirstOrDefault(o => !o.StopTargeting && o.Target == t
                && (o.Dimension == ChoiceDimension.Target || o.Dimension == ChoiceDimension.Defender
                    || o.Dimension == ChoiceDimension.BlockedAttacker || o.Dimension == ChoiceDimension.Sacrifice));
            if (option == null) return false;
            Choose(option);
            return true;
        }

        private static bool Matches(PlayerAction a, PickerOption o, int slot)
        {
            switch (o.Dimension)
            {
                case ChoiceDimension.Mode: return a.Kind == o.Kind && (a.Kind != ActionKind.ActivateAbility || a.AbilityIndex == o.AbilityIndex);
                case ChoiceDimension.Sacrifice: return a.Sacrifice == o.Target.Object;
                case ChoiceDimension.Target: return o.StopTargeting ? a.Targets.Length == slot : a.Targets.Length > slot && a.Targets[slot] == o.Target;
                case ChoiceDimension.Invest: return a.Invest == o.Invest;
                case ChoiceDimension.X: return a.X == o.X;
                case ChoiceDimension.Division: return a.Division.SequenceEqual(o.Division);
                case ChoiceDimension.Defender: return a.Defender == o.Target.Player;
                case ChoiceDimension.BlockedAttacker: return a.BlockedAttacker == o.Target.Object;
                default: return false;
            }
        }

        /// <summary>Ask the first question whose answer still differs between the candidates.</summary>
        private void Advance()
        {
            if (_candidates.Count == 0) { Cancel(); return; }
            // Targets are always clicked, even a single one (playtest 2026-10-10_155528). NextPrompt is null only if
            // distinct actions match on every other dimension, which can't happen: take the first rather than hang.
            Prompt = NextPrompt();
            if (Prompt == null) Ready = _candidates[0];
        }

        private PickerPrompt NextPrompt()
        {
            var c = _candidates;
            if (!_fixed.Contains(ChoiceDimension.Mode) && c.Select(a => (a.Kind, a.Kind == ActionKind.ActivateAbility ? a.AbilityIndex : -1)).Distinct().Count() > 1)
                return Make(ChoiceDimension.Mode, 0, c.GroupBy(a => (a.Kind, a.Kind == ActionKind.ActivateAbility ? a.AbilityIndex : -1))
                    .Select(g => new PickerOption { Dimension = ChoiceDimension.Mode, Kind = g.Key.Item1, AbilityIndex = Math.Max(0, g.Key.Item2) }));

            if (!_fixed.Contains(ChoiceDimension.Sacrifice) && c.Select(a => a.Sacrifice).Distinct().Count() > 1)
                return Make(ChoiceDimension.Sacrifice, 0, c.Select(a => a.Sacrifice).Distinct()
                    .Select(s => new PickerOption { Dimension = ChoiceDimension.Sacrifice, Target = Target.ForObject(s) }));

            if (!_fixed.Contains(ChoiceDimension.Target))
            {
                int slot = _targetsFixed;
                bool someStop = c.Any(a => a.Targets.Length == slot);
                var next = c.Where(a => a.Targets.Length > slot).Select(a => a.Targets[slot]).Distinct().ToList();
                if (next.Count > 0)
                {
                    var options = next.Select(t => new PickerOption { Dimension = ChoiceDimension.Target, Target = t }).ToList();
                    if (someStop) options.Add(new PickerOption { Dimension = ChoiceDimension.Target, StopTargeting = true });
                    return Make(ChoiceDimension.Target, slot, options);
                }
            }

            if (!_fixed.Contains(ChoiceDimension.Invest) && c.Select(a => a.Invest).Distinct().Count() > 1)
                return Make(ChoiceDimension.Invest, 0, new[] { true, false }
                    .Select(i => new PickerOption { Dimension = ChoiceDimension.Invest, Invest = i }));

            if (!_fixed.Contains(ChoiceDimension.X) && c.Select(a => a.X).Distinct().Count() > 1)
                return Make(ChoiceDimension.X, 0, c.Select(a => a.X).Distinct().OrderBy(x => x)
                    .Select(x => new PickerOption { Dimension = ChoiceDimension.X, X = x }));

            if (!_fixed.Contains(ChoiceDimension.Division) && c.Select(a => string.Join("/", a.Division)).Distinct().Count() > 1)
                return Make(ChoiceDimension.Division, 0, c.GroupBy(a => string.Join("/", a.Division))
                    .Select(g => new PickerOption { Dimension = ChoiceDimension.Division, Division = g.First().Division }));

            if (!_fixed.Contains(ChoiceDimension.Defender) && c.Select(a => a.Defender).Distinct().Count() > 1)
                return Make(ChoiceDimension.Defender, 0, c.Select(a => a.Defender).Distinct()
                    .Select(p => new PickerOption { Dimension = ChoiceDimension.Defender, Target = Target.ForPlayer(p) }));

            if (!_fixed.Contains(ChoiceDimension.BlockedAttacker) && c.Select(a => a.BlockedAttacker).Distinct().Count() > 1)
                return Make(ChoiceDimension.BlockedAttacker, 0, c.Select(a => a.BlockedAttacker).Distinct()
                    .Select(o => new PickerOption { Dimension = ChoiceDimension.BlockedAttacker, Target = Target.ForObject(o) }));

            return null;
        }

        private static PickerPrompt Make(ChoiceDimension d, int slot, IEnumerable<PickerOption> options) =>
            new PickerPrompt { Dimension = d, Slot = slot, Options = options.ToList() };
    }
}
