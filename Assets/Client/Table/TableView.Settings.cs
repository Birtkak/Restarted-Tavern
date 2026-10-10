using System;
using UnityEngine;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// The settings panel: from the main menu's Settings tile, and in game with Escape when there's nothing to cancel
    /// (user, 2026-10-10). The game waits while it's open. Saved in PlayerPrefs; Unity remembers the window itself.
    /// </summary>
    public sealed partial class TableView
    {
        private bool _settingsOpen;
        private bool _keywordHints = true;
        private bool _coinToss = true;

        private static readonly float[] Speeds = { 0.5f, 1f, 2f, 4f };
        private static readonly Vector2Int[] Resolutions = { new(1280, 720), new(1600, 900), new(1920, 1080), new(2560, 1440) };

        /// <summary>Called once at startup. Automated screenshots keep the default speed (their timings assume it).</summary>
        private void LoadSettings()
        {
            if (_autoshot == null) _speed = PlayerPrefs.GetFloat("speed", 1f);
            _keywordHints = PlayerPrefs.GetInt("keywordHints", 1) == 1;
            _coinToss = PlayerPrefs.GetInt("coinToss", 1) == 1;
            QualitySettings.vSyncCount = PlayerPrefs.GetInt("vsync", 1);
        }

        private void OpenSettings()
        {
            _picker.Cancel();
            _settingsOpen = true;
            _dirty = true;
        }

        private void CloseSettings()
        {
            _settingsOpen = false;
            _nextBot = Time.unscaledTime + 0.4f / _speed;
            _dirty = true;
        }

        private void SetSpeed(float speed)
        {
            _speed = speed;
            PlayerPrefs.SetFloat("speed", speed);
            PlayerPrefs.Save();
            _dirty = true;
        }

        private void DrawSettings()
        {
            if (!_settingsOpen) return;
            Ui.FillPanel(_overlay, "SettingsDim", new Color(0, 0, 0, 0.6f), 0f, raycast: true);
            const float w = 860f, h = 640f;
            float x = (Ui.Width - w) / 2f, y = (Ui.Height - h) / 2f;
            var panel = Ui.Panel(_overlay, "Settings", x, y, w, h, CoachColor, raycast: true);
            panel.sprite = Ui.GradientSprite;
            Ui.Frame(panel.transform, "ring", Ui.Gold, 22f).raycastTarget = false;
            var t = panel.transform;
            Ui.Tmp(t, "SETTINGS", 0f, 22f, w, 56f, 40f, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, outline: true);

            float row = 104f;
            void Row(string label, string[] options, int current, Action<int> pick)
            {
                Ui.Tmp(t, label, 50f, row, 250f, 52f, 22f, Ui.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
                float bw = (w - 340f) / options.Length;
                for (int i = 0; i < options.Length; i++)
                {
                    int ii = i;
                    Ui.Button(t, options[i], 300f + i * bw, row + 4f, bw - 8f, 44f, i == current ? ContextOn : ButtonColor, () =>
                    {
                        pick(ii);
                        PlayerPrefs.Save();
                        _dirty = true;
                    }, 17);
                }
                row += 66f;
            }

            Row("Animation speed", new[] { "0.5x", "1x", "2x", "4x" }, Array.IndexOf(Speeds, _speed), i => SetSpeed(Speeds[i]));
            Row("Window", new[] { "Fullscreen", "Windowed" }, Screen.fullScreen ? 0 : 1, i =>
                Screen.fullScreenMode = i == 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            int res = Array.FindIndex(Resolutions, r => r.x == Screen.width && r.y == Screen.height);
            Row("Resolution", new[] { "1280x720", "1600x900", "1920x1080", "2560x1440" }, res, i =>
                Screen.SetResolution(Resolutions[i].x, Resolutions[i].y, Screen.fullScreenMode));
            Row("VSync", new[] { "On", "Off" }, QualitySettings.vSyncCount > 0 ? 0 : 1, i =>
            {
                QualitySettings.vSyncCount = i == 0 ? 1 : 0;
                PlayerPrefs.SetInt("vsync", QualitySettings.vSyncCount);
            });
            Row("Keyword hints", new[] { "Show", "Hide" }, _keywordHints ? 0 : 1, i =>
            {
                _keywordHints = i == 0;
                PlayerPrefs.SetInt("keywordHints", i == 0 ? 1 : 0);
            });
            Row("Coin toss", new[] { "Show", "Skip" }, _coinToss ? 0 : 1, i =>
            {
                _coinToss = i == 0;
                PlayerPrefs.SetInt("coinToss", i == 0 ? 1 : 0);
            });

            Ui.Tmp(t, "Esc: close  ·  F2: report a bug  ·  F3: guide", 50f, h - 78f, 360f, 52f, 16f, Ui.Cream * new Color(1, 1, 1, 0.45f), TextAnchor.MiddleLeft, FontStyle.Italic);
            bool inGame = !_menuOpen;
            Ui.Button(t, "Tavern Guide", 50f, h - 140f, 220f, 48f, Ui.Hex("#2A6A50"), () => OpenGuide(), 18);
            if (inGame)
                Ui.Button(t, "Main menu", w - 420f, h - 82f, 180f, 56f, ButtonColor, () =>
                {
                    _settingsOpen = false;
                    OpenMenu();
                }, 20);
            Ui.Button(t, inGame ? "Resume" : "Close", w - 220f, h - 82f, 180f, 56f, ContextOn, CloseSettings, 22);
        }
    }
}
