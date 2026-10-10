using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using RestartedTavern.Rules.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// The Report bug button: the player writes what went wrong, then the table saves BugReports/&lt;time&gt;/ next to
    /// the game with a screenshot (taken without the dialog) and report.txt: the note, setup and seed, both players'
    /// full state, the Chain, legal actions, the table's own state (picker, combat stage), recent errors, the log, and
    /// the action history, which replays the game exactly (MatchSession.History).
    /// </summary>
    public sealed partial class TableView
    {
        private bool _bugOpen;
        private string _bugNote = "";
        /// <summary>Frames until the screenshot (-1 = none pending): the dialog disappears first.</summary>
        private int _bugShotFrame = -1;
        private string _bugDir;

        private void OpenBugReport()
        {
            _bugOpen = true;
            _bugNote = "";
            _dirty = true;
        }

        private void DrawBugDialog()
        {
            if (!_bugOpen) return;
            Ui.FillPanel(_overlay, "BugDim", new Color(0, 0, 0, 0.5f), 0f, raycast: true);
            const float w = 760f, h = 340f;
            float x = (Ui.Width - w) / 2f, y = 300f;
            var panel = Ui.Panel(_overlay, "BugReport", x, y, w, h, Ui.Hex("#24160C"), raycast: true);
            Ui.AddOutline(panel.gameObject, Ui.Hex("#C89A50"), 3f);
            var t = panel.transform;
            Ui.Label(t, "Report a bug", 20, 12, w - 40, 40, 30, Ui.Hex("#FFE0A0"), TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(t, "What went wrong? (A screenshot and the full game state are saved with it.)", 20, 54, w - 40, 26, 16,
                Ui.Hex("#D8C8B0"), TextAnchor.MiddleLeft);

            var bg = Ui.Panel(t, "Input", 20, 88, w - 40, 160, new Color(0.96f, 0.93f, 0.86f), raycast: true);
            var text = Ui.FillLabel(bg.transform, "", 18, new Color(0.12f, 0.08f, 0.04f), TextAnchor.UpperLeft, FontStyle.Normal, 8f);
            text.resizeTextForBestFit = false;
            text.supportRichText = false;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var placeholder = Ui.FillLabel(bg.transform, "e.g. I dragged Fling the Runt onto a Goober and nothing happened", 18,
                new Color(0.4f, 0.35f, 0.3f), TextAnchor.UpperLeft, FontStyle.Italic, 8f);
            placeholder.resizeTextForBestFit = false;
            var field = bg.gameObject.AddComponent<InputField>();
            field.targetGraphic = bg;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.lineType = InputField.LineType.MultiLineNewline;
            field.text = _bugNote;
            field.onValueChanged.AddListener(v => _bugNote = v);
            field.ActivateInputField();

            Ui.Button(t, "Save report", w - 360, h - 72, 200, 54, ContextOn, SaveBugReport, 22);
            Ui.Button(t, "Cancel", w - 150, h - 72, 130, 54, Ui.Hex("#5A2A20"), () =>
            {
                _bugOpen = false;
                _dirty = true;
            }, 20);
        }

        private void SaveBugReport()
        {
            _bugOpen = false;
            _dirty = true;
            try
            {
                string root = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "BugReports");
                _bugDir = Path.Combine(root, DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture));
                Directory.CreateDirectory(_bugDir);
                File.WriteAllText(Path.Combine(_bugDir, "report.txt"), BugReportText());
                _bugShotFrame = 2; // one frame to redraw without the dialog, then the screenshot
            }
            catch (Exception e)
            {
                ShowToast("Could not save the bug report: " + e.Message);
            }
        }

        /// <summary>Called every frame (UpdateDebug): takes the pending screenshot, then says where the report is.</summary>
        private void UpdateBugReport()
        {
            if (_bugShotFrame < 0) return;
            _bugShotFrame--;
            if (_bugShotFrame == 1) ScreenCapture.CaptureScreenshot(Path.Combine(_bugDir, "screenshot.png"));
            if (_bugShotFrame == 0)
            {
                _bugShotFrame = -1;
                ShowToast("Saved bug report: " + _bugDir);
            }
        }

        private string BugReportText()
        {
            var sb = new StringBuilder();
            var st = _s.State;
            var decks = CardPool.PrototypeDecks();
            sb.AppendLine("Restarted Tavern bug report");
            sb.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                          + " · screen " + Screen.width + "x" + Screen.height + " · version " + Application.version);
            sb.AppendLine();
            sb.AppendLine("== What went wrong (player's note) ==");
            sb.AppendLine(string.IsNullOrWhiteSpace(_bugNote) ? "(no note)" : _bugNote.Trim());
            sb.AppendLine();

            sb.AppendLine("== Setup ==");
            sb.AppendLine("Seed: " + _seed);
            if (_tutorial != null)
                sb.AppendLine("TUTORIAL (Tutorial.Setup(seed), stacked decks): step " + (_tutorial.Index + 1) + " " + (_tutorial.Current?.Title ?? "")
                              + (_tutorial.Released ? ", released" : ""));
            for (int seat = 0; seat < 2; seat++)
                sb.AppendLine("P" + (seat + 1) + ": deck " + seat + "=" + _deck[seat] + " " + decks[_deck[seat]].Name + " (" + decks[_deck[seat]].Id + ")"
                              + ", Tavern Dweller " + _s.Setup.Decks[seat].TavernDweller + ", " + _seats[seat]);
            sb.AppendLine();

            sb.AppendLine("== Game state ==");
            sb.AppendLine("Round " + st.RoundNumber + ", step " + st.Step + ", round leader " + _snap.RoundLeader
                          + (_snap.AttackUsed ? " (attack used)" : "") + ", passes in a row " + st.PassesInRow
                          + (st.IsGameOver ? ", GAME OVER, winners " + string.Join(", ", st.Winners) : ""));
            sb.AppendLine("Waiting on " + (_s.WaitingOn?.ToString() ?? "nobody") + ", viewer " + _s.Viewer
                          + (_s.HandoffPending ? ", handoff pending" : "") + (_s.CombatInProgress ? ", committed attack in progress" : ""));
            sb.AppendLine("Decision: " + (st.Pending != null ? st.Pending.Kind + " for " + st.Pending.Player + " — " + st.Pending.Prompt : "none"));
            foreach (var p in st.Players)
            {
                var view = TableSnapshot.Build(_s.Engine, st, p.Id).Player(p.Id);
                sb.AppendLine();
                sb.AppendLine(p.Id + " (" + _s.SeatOf(p.Id) + "): life " + view.Life + ", mana " + view.Mana + "/" + view.MaxMana
                              + ", Gold " + view.Gold + "/" + view.GoldCap + ", deck " + view.DeckCount
                              + (view.HasAttackToken ? ", attack token" : "") + (view.HasLost ? ", LOST" : ""));
                sb.AppendLine("  Tavern Dweller: " + Describe(view.TavernDweller));
                sb.AppendLine("  Hand: " + string.Join(" | ", view.Hand.Select(Describe)));
                sb.AppendLine("  Battlefield:");
                foreach (var c in view.Battlefield) sb.AppendLine("    " + Describe(c));
                sb.AppendLine("  Graveyard: " + string.Join(" | ", view.Graveyard.Select(c => c.Name + " " + c.Id)));
                sb.AppendLine("  Exile: " + string.Join(" | ", view.Exile.Select(c => c.Name + " " + c.Id)));
            }
            sb.AppendLine();
            sb.AppendLine("Chain (bottom first): " + (_snap.Chain.Count == 0 ? "empty" : ""));
            foreach (var item in _snap.Chain)
                sb.AppendLine("  #" + item.Id + " " + item.Kind + " by " + item.Controller + ": " + item.Text
                              + (item.Targets.Count > 0 ? " -> " + string.Join(", ", item.Targets) : ""));
            sb.AppendLine();

            sb.AppendLine("== Table (client) state ==");
            sb.AppendLine("Context button: " + Describe(TableControls.Main(_s, _stage)));
            sb.AppendLine("Picker: " + (_picker.IsPicking
                ? "source " + _picker.Source + ", asking " + _picker.Prompt?.Dimension + " (" + _picker.Prompt?.Options.Count + " options: "
                  + string.Join(", ", _picker.Prompt?.Options.Select(o => o.ToString()) ?? Enumerable.Empty<string>()) + ")"
                : "idle"));
            sb.AppendLine("Combat stage: " + (_stage == null ? "none" : (_stage.IsBlocking ? "blocking" : "attacking")
                + ", staged " + string.Join(", ", _stage.Staged.Select(c => c.Creature + (c.Blocks.IsNone ? "" : " blocks " + c.Blocks)))));
            sb.AppendLine("Glowing sources: " + string.Join(", ", _sources) + " · targets: " + string.Join(", ", _targets)
                          + " · combat candidates: " + string.Join(", ", _combatCandidates));
            sb.AppendLine("Menu open: " + _menuOpen + ", debug open: " + _debugOpen + ", hands revealed: " + _revealHands + ", speed " + _speed);
            sb.AppendLine();

            sb.AppendLine("== Legal actions of " + (_s.WaitingOn?.ToString() ?? "nobody") + " ==");
            if (_s.WaitingOn is PlayerId w)
                foreach (var a in _s.Engine.GetLegalActions(st, w)) sb.AppendLine("  " + a + "   (" + _s.Text.Describe(st, a) + ")");
            sb.AppendLine();

            sb.AppendLine("== Errors this game ==");
            var errors = _fullLog.Where(l => l.StartsWith("ERROR", StringComparison.Ordinal)).ToList();
            sb.AppendLine(errors.Count == 0 ? "none" : string.Join("\n", errors));
            sb.AppendLine();

            sb.AppendLine("== Log ==");
            foreach (var line in _fullLog) sb.AppendLine(line);
            sb.AppendLine();

            sb.AppendLine("== Action history (replay on a new game with this setup and seed) ==");
            for (int i = 0; i < _s.History.Count; i++) sb.AppendLine(i + ": " + _s.History[i]);
            sb.AppendLine();
            sb.AppendLine("Fingerprint: " + st.Fingerprint());
            return sb.ToString();
        }

        private static string Describe(CardView c)
        {
            if (c == null) return "none";
            if (c.IsHidden) return "(hidden " + c.Id + ")";
            var s = c.Name + " " + c.Id + " [" + c.DefinitionId + "]";
            if (c.Type == CardType.Creature && c.Zone == Zone.Battlefield)
                s += " " + c.Power + "/" + c.RemainingHealth + " (printed " + c.PrintedPower + "/" + c.PrintedHealth + ", damage " + c.Damage + ")"
                     + (c.Keywords != Keyword.None ? " " + c.Keywords : "") + (c.Tapped ? " tapped" : "")
                     + (c.IsAttacking ? " attacking " + c.Attacks : "") + (!c.Blocks.IsNone ? " blocking " + c.Blocks : "");
            if (!c.AttachedTo.IsNone) s += " attached to " + c.AttachedTo;
            if (c.AttachedToPlayer != null) s += " attached to " + c.AttachedToPlayer;
            return s;
        }

        private static string Describe(ContextButton b) =>
            b.Mode + " \"" + b.Label + "\"" + (b.Enabled ? "" : " (disabled)") + (b.Action != null ? " -> " + b.Action : "");
    }
}
