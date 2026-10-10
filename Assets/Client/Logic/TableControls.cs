using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Rules;

namespace RestartedTavern.Client.Logic
{
    /// <summary>What the big LoR-style button at the right edge does right now (CLIENT_DESIGN §2.1).</summary>
    public enum ButtonMode
    {
        /// <summary>Not the viewer's call (the opponent or a bot is acting).</summary>
        Waiting,
        /// <summary>Hot-seat: the table is covered until the next player confirms they're at the screen.</summary>
        Handoff,
        GameOver,
        /// <summary>A choice is being made on the table or in the choice panel; the button does nothing.</summary>
        ChooseOnTable,
        Keep,
        /// <summary>Pass the action (the round goes on).</summary>
        Pass,
        /// <summary>Pass when everyone else has passed: the round ends.</summary>
        EndRound,
        /// <summary>Pass with something on the Chain: let the top item resolve.</summary>
        Resolve,
        /// <summary>Pass in a priority window that isn't an action (combat steps).</summary>
        Continue,
        /// <summary>Confirm a staged attack (<see cref="CombatStage"/>).</summary>
        Attack,
        /// <summary>Declaring attackers with nothing staged: attack with nothing.</summary>
        SkipAttack,
        /// <summary>Confirm staged blocks.</summary>
        Block,
        /// <summary>Declaring blockers with nothing staged.</summary>
        NoBlocks,
    }

    public sealed class ContextButton
    {
        public ButtonMode Mode;
        public string Label;
        public bool Enabled;
        /// <summary>The engine action a click submits. Null for Attack / Block (commit the stage) and Handoff (acknowledge).</summary>
        public PlayerAction Action;
        /// <summary>One line under the button: what pressing it does, or what the game waits for (LoR clarity).</summary>
        public string Hint = "";
    }

    /// <summary>A labelled action for the choice panel (options, declining a target, mulligan, X...).</summary>
    public sealed class ChoiceButton
    {
        public string Label;
        public PlayerAction Action;
    }

    /// <summary>
    /// The table's controls, worked out from the session: the context button and the choice panel. The visual client
    /// only draws them and sends the click back (<see cref="MatchSession.Submit"/>, <see cref="MatchSession.CommitCombat"/>
    /// or <see cref="MatchSession.AcknowledgeHandoff"/>).
    /// </summary>
    public static class TableControls
    {
        public static ContextButton Main(MatchSession s, CombatStage stage = null)
        {
            var b = MainButton(s, stage);
            // An action the human may not take right now (the tutorial's filter) leaves the button dark.
            if (b.Enabled && b.Action != null && !s.LegalForViewer().Contains(b.Action)) b.Enabled = false;
            if (string.IsNullOrEmpty(b.Hint)) b.Hint = HintFor(s, b);
            return b;
        }

        private static ContextButton MainButton(MatchSession s, CombatStage stage)
        {
            var state = s.State;
            if (state.IsGameOver) return Button(ButtonMode.GameOver, "Game over");
            if (s.HandoffPending) return new ContextButton { Mode = ButtonMode.Handoff, Label = s.Viewer + ", take the table", Enabled = true };
            if (!s.HumanToAct) return Button(ButtonMode.Waiting, s.CombatInProgress ? "Attacking..." : "Opponent's turn");

            var legal = s.LegalForViewer();
            var me = s.Viewer;
            switch (state.Pending?.Kind)
            {
                case null:
                    break;
                case DecisionKind.Mulligan:
                    return Submit(ButtonMode.Keep, "Keep", PlayerAction.Keep(me));
                case DecisionKind.DeclareAttackers:
                    return stage != null && !stage.IsBlocking && stage.Staged.Count > 0
                        ? new ContextButton { Mode = ButtonMode.Attack, Label = "Attack", Enabled = true }
                        : Submit(ButtonMode.SkipAttack, "Don't attack", PlayerAction.FinishAttacks(me));
                case DecisionKind.DeclareBlockers:
                    return stage != null && stage.IsBlocking && stage.Staged.Count > 0
                        ? new ContextButton { Mode = ButtonMode.Block, Label = "Block", Enabled = true }
                        : Submit(ButtonMode.NoBlocks, "No blocks", PlayerAction.FinishBlocks(me));
                default:
                    return Button(ButtonMode.ChooseOnTable, state.Pending.Prompt ?? "Choose");
            }

            if (stage != null && !stage.IsBlocking && stage.Staged.Count > 0)
                return new ContextButton { Mode = ButtonMode.Attack, Label = "Attack", Enabled = true };

            var pass = PlayerAction.Pass(me);
            if (!legal.Contains(pass)) return Button(ButtonMode.ChooseOnTable, "Choose");
            // The label says what passing does right now (playtest 2026-10-10).
            if (state.Chain.Count > 0)
            {
                // Your own item on top: passing gives the opponent the chance to respond. Theirs: passing lets it resolve.
                bool ownTop = state.Chain[state.Chain.Count - 1].Controller == me;
                return Submit(ButtonMode.Resolve, ownTop ? "Pass priority" : "Let it resolve", pass);
            }
            // After blocks: the last window before damage (LoR). Say so on the button.
            if (state.Step == Step.DeclareBlockers) return Submit(ButtonMode.Continue, "To damage", pass);
            if (state.Step == Step.BeginCombat || state.Step == Step.DeclareAttackers) return Submit(ButtonMode.Continue, "Pass priority", pass);
            if (state.Step != Step.Main1) return Submit(ButtonMode.Continue, "Continue", pass);
            bool endsRound = state.PassesInRow >= state.LivingPlayerCount - 1;
            return endsRound ? Submit(ButtonMode.EndRound, "End round", pass) : Submit(ButtonMode.Pass, "Pass turn", pass);
        }

        private static string HintFor(MatchSession s, ContextButton b)
        {
            var state = s.State;
            bool attacking = state.Combat != null && state.ActivePlayer == s.Viewer;
            switch (b.Mode)
            {
                case ButtonMode.Waiting:
                    return s.CombatInProgress ? "Waiting for the opponent to respond to your attack" : "Waiting for the opponent";
                case ButtonMode.Keep: return "Keep this hand, or mulligan";
                case ButtonMode.Pass: return "Your action passes to the opponent. If they pass too, the round ends";
                case ButtonMode.EndRound: return "Opponent passed. Passing now ends the round";
                case ButtonMode.Resolve:
                    var top = state.Chain.Count > 0 ? state.Chain[state.Chain.Count - 1] : null;
                    string def = top?.SourceDefinitionId ?? top?.Card?.DefinitionId;
                    string name = def != null ? s.Text.Name(def) : "the top of the Chain";
                    bool ownTop = top != null && top.Controller == s.Viewer;
                    return ownTop ? "The opponent may respond to " + name + ", then it resolves" : "Respond now, or " + name + " resolves";
                case ButtonMode.Continue:
                    switch (state.Step)
                    {
                        case Step.BeginCombat: return "Combat is starting. Last chance before attacks";
                        case Step.DeclareAttackers:
                            return attacking ? "Attack declared. Respond, or let them block" : "You are attacked. Respond before blocks";
                        case Step.DeclareBlockers:
                            return attacking ? "Blocks are in. Pump or remove now, then fight" : "Blocks are in. Last chance before damage";
                        case Step.End: return "The round is ending";
                        default: return "";
                    }
                case ButtonMode.Attack: return "Send the units in the lane";
                case ButtonMode.SkipAttack: return "Attack with nothing";
                case ButtonMode.Block: return "Confirm your blocks";
                case ButtonMode.NoBlocks: return "Take the hits unblocked";
                default: return "";
            }
        }

        /// <summary>
        /// Sourceless actions the table can't express by clicking a card or target: mulligan, option choices,
        /// declining an optional target, finishing an "up to" choice. Labels come from <see cref="GameText"/>.
        /// </summary>
        public static List<ChoiceButton> Choices(MatchSession s)
        {
            if (!s.HumanToAct) return new List<ChoiceButton>();
            var main = Main(s).Action;
            return new ActionPicker(s.LegalForViewer()).SourcelessActions
                .Where(a => !a.Equals(main) && a.Kind != ActionKind.PassPriority && a.Kind != ActionKind.GoToCombat
                    && a.Kind != ActionKind.FinishAttacks && a.Kind != ActionKind.FinishBlocks
                    && !(a.Kind == ActionKind.ChooseTarget && a.Targets.Length > 0))
                .Select(a => new ChoiceButton { Label = s.Text.Describe(s.State, a), Action = a })
                .ToList();
        }

        /// <summary>A short label for a picker option (mode, target, Invest, X...), for the prompt bar or a popup.</summary>
        public static string Describe(MatchSession s, ActionPicker picker, PickerOption o)
        {
            var state = s.State;
            switch (o.Dimension)
            {
                case ChoiceDimension.Mode:
                    if (o.Kind != ActionKind.ActivateAbility) return "Play it";
                    var source = state.FindObject(picker.Source);
                    var abilities = source != null ? s.Engine.GetAbilities(state, source) : null;
                    return abilities != null && o.AbilityIndex < abilities.Count ? abilities[o.AbilityIndex].Text : "Ability " + (o.AbilityIndex + 1);
                case ChoiceDimension.Target:
                    return o.StopTargeting ? "No more targets" : s.Text.Name(state, o.Target);
                case ChoiceDimension.Invest:
                    return o.Invest ? "Invest" : "Don't invest";
                case ChoiceDimension.X:
                    return "X = " + o.X;
                case ChoiceDimension.Division:
                    return string.Join(" / ", o.Division);
                case ChoiceDimension.Sacrifice:
                    return "Sacrifice " + s.Text.Name(state, o.Target);
                default:
                    return s.Text.Name(state, o.Target);
            }
        }

        private static ContextButton Button(ButtonMode mode, string label) => new ContextButton { Mode = mode, Label = label };

        private static ContextButton Submit(ButtonMode mode, string label, PlayerAction action) =>
            new ContextButton { Mode = mode, Label = label, Enabled = true, Action = action };
    }
}
