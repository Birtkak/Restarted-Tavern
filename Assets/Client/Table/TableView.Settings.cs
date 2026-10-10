using System;
using RestartedTavern.Client.Logic;
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
            _volume = PlayerPrefs.GetFloat("volume", 0.6f);
            _passSound = PlayerPrefs.GetInt("passSound", 1) == 1;
            _triggerSound = PlayerPrefs.GetInt("triggerSound", 1) == 1;
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
            _soundBoard = false;
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
            if (_soundBoard) { DrawSoundBoard(); return; }
            Ui.FillPanel(_overlay, "SettingsDim", new Color(0, 0, 0, 0.6f), 0f, raycast: true);
            const float w = 860f, h = 830f;
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

            Row("Sound", new[] { "Off", "Low", "Medium", "High" }, System.Array.FindIndex(Volumes, x => Mathf.Approximately(x, _volume)), i =>
            {
                SetVolume(Volumes[i]);
                PlaySfx(SfxKind.Creature, "goobers");
            });
            Row("Pass sound", new[] { "On", "Off" }, _passSound ? 0 : 1, i =>
            {
                _passSound = i == 0;
                PlayerPrefs.SetInt("passSound", _passSound ? 1 : 0);
                PlaySfx(SfxKind.Pass, null);
            });
            Row("Trigger ticks", new[] { "On", "Off" }, _triggerSound ? 0 : 1, i =>
            {
                _triggerSound = i == 0;
                PlayerPrefs.SetInt("triggerSound", _triggerSound ? 1 : 0);
                PlaySfx(SfxKind.Trigger, "glitterworld");
            });
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
            Ui.Button(t, "Sound board", 290f, h - 140f, 220f, 48f, Ui.Hex("#2A5A6A"), () => { _soundBoard = true; _dirty = true; }, 18);
            if (inGame)
                Ui.Button(t, "Main menu", w - 420f, h - 82f, 180f, 56f, ButtonColor, () =>
                {
                    _settingsOpen = false;
                    OpenMenu();
                }, 20);
            Ui.Button(t, inGame ? "Resume" : "Close", w - 220f, h - 82f, 180f, 56f, ContextOn, CloseSettings, 22);
        }

        // ------------------------------------------------------------------ sound board

        /// <summary>Settings' sound board (user: "make it so you can listen to them yourself"): every sound, per faction.</summary>
        private bool _soundBoard;

        private static readonly (SfxKind Kind, string Label)[] BoardKinds =
        {
            (SfxKind.Creature, "Creature"), (SfxKind.Instant, "Instant"), (SfxKind.Sorcery, "Sorcery"), (SfxKind.Equipment, "Equipment"),
            (SfxKind.Relic, "Relic"), (SfxKind.Curse, "Curse"), (SfxKind.Ability, "Ability"), (SfxKind.Power, "Power"), (SfxKind.Death, "Death"),
        };

        private static readonly (string Id, string Name)[] BoardFactions =
        {
            ("goobers", "Goobers"), ("evergrowing_wild", "Evergrowing Wild"), ("glitterworld", "Glitterworld"),
            ("sensationalists", "Sensationalists"), ("shadow_money_wizards", "Shadow Money Wizards"), ("neutral", "Neutral"),
        };

        private void DrawSoundBoard()
        {
            Ui.FillPanel(_overlay, "SettingsDim", new Color(0, 0, 0, 0.6f), 0f, raycast: true);
            const float w = 1500f, h = 760f;
            float x = (Ui.Width - w) / 2f, y = (Ui.Height - h) / 2f;
            var panel = Ui.Panel(_overlay, "SoundBoard", x, y, w, h, CoachColor, raycast: true);
            panel.sprite = Ui.GradientSprite;
            Ui.Frame(panel.transform, "ring", Ui.Gold, 22f).raycastTarget = false;
            var t = panel.transform;
            Ui.Tmp(t, "SOUND BOARD", 0f, 20f, w, 56f, 40f, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, outline: true);
            Ui.Tmp(t, _volume > 0f ? "Click to listen. Every card kind has its own sound, and each faction its own voice."
                    : "Sound is off: set the volume in Settings first.", 0f, 74f, w, 30f, 18f,
                _volume > 0f ? Ui.Cream * new Color(1, 1, 1, 0.7f) : Ui.Hex("#FF8060"), TextAnchor.MiddleCenter, FontStyle.Italic);

            const float labelW = 260f, cellH = 50f;
            float cellW = (w - 80f - labelW) / BoardKinds.Length, top = 124f;
            for (int k = 0; k < BoardKinds.Length; k++)
                Ui.Tmp(t, BoardKinds[k].Label.ToUpperInvariant(), 40f + labelW + k * cellW, top, cellW, 30f, 15f, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            float rowY = top + 36f;
            foreach (var (id, name) in BoardFactions)
            {
                var style = CardFaces.Style(id);
                var chip = Ui.Panel(t, "Faction", 40f, rowY, labelW - 12f, cellH - 6f, id == "neutral" ? ButtonColor : style.Frame);
                chip.sprite = Ui.GradientSprite;
                Ui.FillTmp(chip.transform, name, 17f, Ui.Cream, TextAnchor.MiddleCenter, FontStyle.Bold, 6f);
                for (int k = 0; k < BoardKinds.Length; k++)
                {
                    var kind = BoardKinds[k].Kind;
                    string faction = id;
                    Ui.Button(t, "Play", 40f + labelW + k * cellW + 4f, rowY, cellW - 8f, cellH - 6f, ButtonColor, () => PlaySfx(kind, faction), 18);
                }
                rowY += cellH + 6f;
            }

            rowY += 14f;
            Ui.Tmp(t, "SHARED", 40f, rowY, labelW, 44f, 17f, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            var shared = new[] { (SfxKind.Pass, "Pass"), (SfxKind.RoundStart, "Round start"), (SfxKind.Attack, "Attack"), (SfxKind.Block, "Block"),
                (SfxKind.Equip, "Equip"), (SfxKind.Trigger, "Trigger"), (SfxKind.Countered, "Countered") };
            float sw = (w - 80f - labelW) / shared.Length;
            for (int i = 0; i < shared.Length; i++)
            {
                var kind = shared[i].Item1;
                Ui.Button(t, shared[i].Item2, 40f + labelW + i * sw + 4f, rowY, sw - 8f, 44f, ButtonColor, () => PlaySfx(kind, null), 16);
            }

            Ui.Button(t, "Back", w - 220f, h - 82f, 180f, 56f, ContextOn, () => { _soundBoard = false; _dirty = true; }, 22);
        }
    }
}
