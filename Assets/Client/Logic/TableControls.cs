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
            var state = s.State;
            if (state.IsGameOver) return Button(ButtonMode.GameOver, "Game over");
            if (s.HandoffPending) return new ContextButton { Mode = ButtonMode.Handoff, Label = s.Viewer + ", take the table", Enabled = true };
            if (!s.HumanToAct) return Button(ButtonMode.Waiting, s.CombatInProgress ? "Attacking..." : "Opponent's action");

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
            if (state.Chain.Count > 0) return Submit(ButtonMode.Resolve, "OK", pass);
            if (state.Step != Step.Main1 && state.Step != Step.Main2) return Submit(ButtonMode.Continue, "Continue", pass);
            bool endsRound = state.PassesInRow >= state.LivingPlayerCount - 1;
            return endsRound ? Submit(ButtonMode.EndRound, "End round", pass) : Submit(ButtonMode.Pass, "Pass", pass);
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
