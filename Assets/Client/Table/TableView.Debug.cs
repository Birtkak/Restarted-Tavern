using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using RestartedTavern.Rules.Cards;
using UnityEngine;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// Debugging tools of the table (F1 or the Debug button): engine state, the event log, every legal action as a
    /// button (to get past anything the table can't express yet), revealing the opponent's hand, letting the bot make
    /// a move for the player, and playtest logs saved to Playtests/ next to the game (automatically at game over).
    /// Errors are shown on screen and written to the log.
    /// </summary>
    public sealed partial class TableView
    {
        private const int MaxLog = 400;

        private bool _debugOpen;
        private bool _revealHands;
        /// <summary>The log as the table shows it (hidden draws stay hidden unless the hands are revealed).</summary>
        private readonly List<string> _log = new List<string>();
        /// <summary>Everything, for the saved playtest log.</summary>
        private readonly List<string> _fullLog = new List<string>();
        private string _toast;
        private float _toastUntil;
        private bool _savedThisGame;
        private string _savedPath;

        private void OnEnable() => Application.logMessageReceived += OnUnityLog;
        private void OnDisable() => Application.logMessageReceived -= OnUnityLog;

        private void OnUnityLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            ShowToast(message);
            _fullLog.Add("ERROR " + message + "\n" + stack);
        }

        private void ShowToast(string message)
        {
            _toast = message;
            _toastUntil = Time.unscaledTime + 6f;
            _dirty = true;
        }

        /// <summary>Called by NewGame: a fresh log, listening to the new session.</summary>
        private void StartLog()
        {
            _log.Clear();
            _fullLog.Clear();
            _savedThisGame = false;
            _savedPath = null;
            _s.EventsApplied += AddToLog;
            AddToLog(_s.StartEvents);
        }

        private PlayerId? LogViewer()
        {
            if (_revealHands) return null;
            var humans = _s.State.Players.Where(p => _s.SeatOf(p.Id) == SeatKind.Human).ToList();
            // Hot-seat: nobody's draws are named (both players read the log).
            return humans.Count == 1 ? humans[0].Id : new PlayerId(0);
        }

        private void AddToLog(IReadOnlyList<GameEvent> events)
        {
            var viewer = LogViewer();
            foreach (var e in events)
            {
                var shown = _s.Text.Describe(_s.State, e, viewer);
                if (shown != null) _log.Add(shown);
                var full = _s.Text.Describe(_s.State, e);
                if (full != null) _fullLog.Add(full);
            }
            if (_log.Count > MaxLog) _log.RemoveRange(0, _log.Count - MaxLog);
            if (_s.State.IsGameOver && !_savedThisGame && _autoshot == null) SaveLog();
        }

        private void LogAction(PlayerAction action)
        {
            if (action.Kind == ActionKind.PassPriority) return;
            string line = "> " + action.Player + ": " + _s.Text.Describe(_s.State, action);
            _log.Add(line);
            _fullLog.Add(line);
        }

        private void LogLine(string line)
        {
            _log.Add(line);
            _fullLog.Add(line);
        }

        /// <summary>Writes the game (setup, seed, result and the full log) to Playtests/ next to the game.</summary>
        private void SaveLog()
        {
            try
            {
                string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Playtests");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture) + "_seed" + _seed + ".txt");
                var lines = new List<string>
                {
                    "Restarted Tavern playtest log (table client)",
                    "Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    "Seed: " + _seed,
                };
                var decks = CardPool.PrototypeDecks();
                for (int seat = 0; seat < 2; seat++)
                {
                    var p = _s.State.Players[seat];
                    lines.Add("P" + (seat + 1) + ": " + decks[_deck[seat]].Name + " with " + _s.Text.Name(_s.Setup.Decks[seat].TavernDweller)
                              + ", " + _seats[seat].ToString().ToLowerInvariant() + (p.Seat == _s.State.StartingPlayerIndex ? ", led round 1" : ""));
                }
                lines.Add("Result: " + (_s.State.IsGameOver ? "winner " + string.Join(", ", _s.State.Winners) : "not finished")
                          + " after round " + _s.State.RoundNumber + " | life " + string.Join(" vs ", _s.State.Players.Select(p => p.Life)));
                lines.Add("");
                lines.Add("Notes (fill in): what felt good, what felt bad, confusing rules or cards, misplays caused by the UI:");
                lines.Add("");
                lines.Add("----- log -----");
                lines.AddRange(_fullLog);
                File.WriteAllLines(file, lines);
                _savedPath = file;
                _savedThisGame = _s.State.IsGameOver;
                ShowToast("Saved " + file);
            }
            catch (Exception e)
            {
                _savedPath = "could not save: " + e.Message;
                ShowToast(_savedPath);
            }
        }

        private void UpdateDebug()
        {
            if (Input.GetKeyDown(KeyCode.F1)) ToggleDebug();
            UpdateBugReport();
            if (_toast != null && Time.unscaledTime > _toastUntil)
            {
                _toast = null;
                _dirty = true;
            }
        }

        private void ToggleDebug()
        {
            _debugOpen = !_debugOpen;
            _dirty = true;
        }

        /// <summary>The opponent's hand face up (debug), from a snapshot built for the opponent.</summary>
        private List<CardView> RevealedHand(PlayerView opp) =>
            _revealHands ? TableSnapshot.Build(_s.Engine, _s.State, opp.Id).Player(opp.Id).Hand : opp.Hand;

        private void DrawToast()
        {
            if (_toast == null) return;
            var bar = Ui.Panel(_overlay, "Toast", Ui.Width / 2f - 500, 56, 1000, 44, new Color(0.55f, 0.08f, 0.05f, 0.95f));
            if (_toast.StartsWith("Saved", StringComparison.Ordinal)) bar.color = new Color(0.1f, 0.35f, 0.15f, 0.95f);
            Ui.FillLabel(bar.transform, _toast, 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 4f);
        }

        private void DrawDebug()
        {
            if (!_debugOpen) return;
            const float w = 600f;
            var panel = Ui.Panel(_overlay, "Debug", 0, 46, w, Ui.Height - 46, new Color(0.03f, 0.03f, 0.05f, 0.93f), raycast: true);
            var t = panel.transform;
            Ui.Label(t, "DEBUG  (F1)", 10, 4, 300, 30, 20, Ui.Hex("#80FF80"), TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Button(t, "Close", w - 90, 4, 80, 28, ButtonColor, ToggleDebug, 14);

            float x = 10;
            Ui.Button(t, _revealHands ? "Hide opp. hand" : "Reveal opp. hand", x, 40, 180, 32, _revealHands ? ContextOn : ButtonColor, () =>
            {
                _revealHands = !_revealHands;
                _dirty = true;
            }, 14);
            Ui.Button(t, "Bot moves for me", x + 190, 40, 180, 32, ButtonColor, () =>
            {
                if (_s.WaitingOn == null) return;
                Run(() => _s.AutoStep());
            }, 14, _s.WaitingOn != null);
            Ui.Button(t, "Save log", x + 380, 40, 110, 32, ButtonColor, SaveLog, 14);
            Ui.Button(t, "Copy seed", x + 500, 40, 80, 32, ButtonColor, () => GUIUtility.systemCopyBuffer = _seed.ToString(CultureInfo.InvariantCulture), 12);

            var st = _s.State;
            var waiting = _s.WaitingOn;
            string state =
                "Seed " + _seed + " · round " + st.RoundNumber + " · step " + st.Step + " · leader " + _snap.RoundLeader
                + (_snap.AttackUsed ? " (attacked)" : "") + "\n"
                + "Waiting on " + (waiting?.ToString() ?? "nobody") + (waiting != null ? " (" + _s.SeatOf(waiting.Value) + ")" : "")
                + " · viewer " + _s.Viewer + (_s.HandoffPending ? " · handoff pending" : "") + "\n"
                + "Decision: " + (st.Pending != null ? st.Pending.Kind + " for " + st.Pending.Player + (st.Pending.Prompt != null ? " — " + st.Pending.Prompt : "") : "none")
                + " · passes in a row " + st.PassesInRow + " · Chain " + st.Chain.Count + "\n"
                + "Picker: " + (_picker.IsPicking ? "source " + _picker.Source + ", " + (_picker.Prompt?.Dimension.ToString() ?? "ready") : "idle")
                + " · stage: " + (_stage == null ? "none" : (_stage.IsBlocking ? "blocking" : "attacking") + ", " + _stage.Staged.Count + " staged")
                + (_s.CombatInProgress ? " · combat in progress" : "") + "\n"
                + string.Join("\n", _snap.Players.Select(p => p.Id + ": life " + p.Life + ", mana " + p.Mana + "/" + p.MaxMana
                    + ", Gold " + p.Gold + "/" + p.GoldCap + ", hand " + p.Hand.Count + ", deck " + p.DeckCount))
                + (_savedPath != null ? "\nLog: " + _savedPath : "");
            Ui.Label(t, state, 10, 80, w - 20, 150, 14, Ui.Hex("#D0E0FF"), TextAnchor.UpperLeft);

            // Every legal action as a button: a way past anything the table can't express yet.
            var legal = _s.LegalForViewer();
            Ui.Label(t, "Legal actions (" + legal.Count + ")", 10, 236, 300, 22, 16, Ui.Hex("#FFD080"), TextAnchor.MiddleLeft, FontStyle.Bold);
            const int maxShown = 14;
            for (int i = 0; i < legal.Count && i < maxShown; i++)
            {
                var a = legal[i];
                Ui.Button(t, _s.Text.Describe(st, a), 10, 262 + i * 27, w - 20, 25, Ui.Hex("#2A3040"), () => Submit(a), 12);
            }
            if (legal.Count > maxShown)
                Ui.Label(t, "... " + (legal.Count - maxShown) + " more", 10, 262 + maxShown * 27, 300, 20, 12, Color.gray, TextAnchor.MiddleLeft);

            float logTop = 262 + Math.Min(legal.Count, maxShown + 1) * 27 + 10;
            Ui.Label(t, "Log", 10, logTop, 300, 22, 16, Ui.Hex("#FFD080"), TextAnchor.MiddleLeft, FontStyle.Bold);
            int lines = Mathf.Max(4, (int)((Ui.Height - 46 - logTop - 30) / 16f));
            var text = Ui.Label(t, string.Join("\n", _log.Skip(Math.Max(0, _log.Count - lines))), 10, logTop + 24, w - 20, Ui.Height - 46 - logTop - 30,
                13, Ui.Hex("#E8E0D0"), TextAnchor.LowerLeft);
            text.resizeTextForBestFit = false;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
