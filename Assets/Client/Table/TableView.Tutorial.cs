using System.Linq;
using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// The scripted tutorial at the table (<see cref="Tutorial"/>): the coach box that explains each step, the
    /// player's passes the script takes for them, and the coin toss that opens every game (user, 2026-10-10).
    /// </summary>
    public sealed partial class TableView
    {
        private Tutorial _tutorial;

        private bool TutorialRunning => _tutorial != null && !_tutorial.Released;
        private bool TutorialFrozen => TutorialRunning && _tutorial.Frozen;

        private static readonly Color CoachColor = Ui.Hex("#141A26");

        private void StartTutorial()
        {
            _battleStarted = true;
            _seats[0] = SeatKind.Human;
            _seats[1] = SeatKind.Bot;
            NewGame(_seed + 1, Tutorial.Setup(_seed + 1), toss: true);
            _tutorial = new Tutorial(_s);
            // Both keep their scripted hands before the first box (no mulligan screen in the tutorial).
            for (int i = 0; i < 4 && _s.State.Pending?.Kind == DecisionKind.Mulligan; i++)
            {
                if (_s.HumanToAct) _s.Submit(PlayerAction.Keep(_s.Viewer));
                else if (_s.BotToAct) _s.StepBot();
            }
            OnSessionChanged();
        }

        // The Info box on screen and since when: it can't be dismissed in its first half second, so a click or Space
        // meant for the animation before it doesn't skip it unread (playtest 2026-10-10_155104).
        private int _coachIndex = -1;
        private float _coachShownAt;

        private bool CoachReady => TutorialFrozen && !Busy && _coachIndex == _tutorial.Index && Time.unscaledTime - _coachShownAt > 0.5f;

        private void TutorialNext()
        {
            if (!CoachReady) return;
            _tutorial.Next();
            OnSessionChanged();
            _nextBot = Time.unscaledTime + 0.4f / _speed;
        }

        private void SkipTutorial()
        {
            if (!TutorialRunning) return;
            _tutorial.Release();
            OnSessionChanged();
        }

        /// <summary>The player's passes outside the lesson (opening Keep, the opponent's spells resolving, combat windows).</summary>
        private void UpdateTutorial()
        {
            if (!TutorialRunning || _menuOpen || Tossing || _dragging != null || Time.unscaledTime < _nextBot) return;
            var auto = _tutorial.AutoHumanAction();
            if (auto != null) Submit(auto);
        }

        /// <summary>
        /// The coach box. Info steps: a big box in the middle of the lane with a button (the game waits). Steps where the
        /// player or Mukk acts: a smaller box at the lane's left end, out of the way of the cards in the lane.
        /// </summary>
        private void DrawTutorial()
        {
            if (!TutorialRunning || _menuOpen || Tossing) return;
            var step = _tutorial.Current;
            if (step == null) return;
            bool info = step.Kind == TutorialStepKind.Info;
            // An Info box waits for the animation before it (the hit, the round start) to finish.
            if (info && Busy) return;
            if (info && _coachIndex != _tutorial.Index)
            {
                _coachIndex = _tutorial.Index;
                _coachShownAt = Time.unscaledTime;
            }
            bool theirs = step.Kind == TutorialStepKind.Opponent;
            float w = info ? 760f : 430f, h = info ? 330f : theirs ? 130f : 236f;
            float x = info ? CenterX - w / 2f : CenterLeft + 12f, y = info ? LaneMid - h / 2f - 10f : LaneTop + 10f;

            if (info) Ui.Panel(_overlay, "CoachDim", CenterLeft, LaneTop, CenterRight - CenterLeft, LaneBottom - LaneTop, new Color(0, 0, 0, 0.35f), raycast: true);
            var box = Ui.Panel(_overlay, "Coach", x, y, w, h, CoachColor, raycast: info);
            box.sprite = Ui.GradientSprite;
            Ui.Frame(box.transform, "ring", theirs ? Ui.Gold * new Color(0.65f, 0.65f, 0.65f, 1f) : Ui.Gold, 22f).raycastTarget = false;
            // A portrait chip: who's talking (the tavern keeper coaching you), or Mukk's step.
            var chip = Ui.Circle(box.transform, "Chip", -22f, -22f, 64f, theirs ? Theirs : Mine);
            Ui.AddOutline(chip.gameObject, Ui.Gold, 2f);
            Ui.FillTmp(chip.transform, theirs ? "!" : "?", 34f, Ui.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);

            int number = _tutorial.Index + 1, total = _tutorial.Steps.Count;
            Ui.Tmp(box.transform, step.Title.ToUpperInvariant(), 52f, 16f, w - 170f, 40f, info ? 30f : 24f, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Tmp(box.transform, number + " / " + total, w - 110f, 20f, 90f, 30f, 16f, Ui.Cream * new Color(1, 1, 1, 0.5f), TextAnchor.MiddleRight);
            Ui.Tmp(box.transform, step.Text, 28f, 64f, w - 56f, h - (info ? 140f : 84f), info ? 22f : 19f, Ui.Cream, TextAnchor.UpperLeft);

            if (info)
            {
                var next = Ui.Button(box.transform, step.Button, w - 220f, h - 72f, 190f, 52f, ContextOn, TutorialNext, 22);
                Pulse(next.transform, 0.04f);
                Ui.Tmp(box.transform, "Space: next", w - 420f, h - 66f, 180f, 40f, 15f, Ui.Cream * new Color(1, 1, 1, 0.45f), TextAnchor.MiddleRight, FontStyle.Italic);
            }
            Ui.Button(info ? box.transform : _overlay, "Skip tutorial", info ? 26f : x + w - 150f, info ? h - 66f : y + h + 8f, 150f, 36f,
                Ui.Hex("#5A2A20"), SkipTutorial, 15);
        }

        // ------------------------------------------------------------------ coin toss

        private RectTransform _tossLayer;
        private RectTransform _coin;
        private Image _coinFace;
        private Text _coinText;
        private CanvasGroup _tossGroup;
        private Text _tossResult;
        private float _tossStart = -1f;
        private bool _tossViewerFirst;
        private string _tossFirstName;

        private const float TossFlight = 1.7f, TossRest = 1.3f, TossFade = 0.4f, TossLead = 0.35f;

        private bool Tossing => _tossStart >= 0f;

        /// <summary>
        /// A coin flip over the table that shows who goes first. The engine already decided (GAME_DESIGN §3: random; the
        /// tutorial: you): the coin is the reveal. The viewer's side is blue with their Tavern Dweller's initials.
        /// </summary>
        private void StartToss()
        {
            EndToss();
            var first = _s.State.Players[_s.State.StartingPlayerIndex];
            _tossViewerFirst = first.Id == _s.Viewer;
            bool hotSeat = _seats.All(k => k == SeatKind.Human);
            _tossFirstName = hotSeat ? "PLAYER " + (first.Seat + 1) + " GOES FIRST"
                : _tossViewerFirst ? "YOU GO FIRST" : (_db.Get(first.TavernDwellerId)?.Name ?? "THE OPPONENT").ToUpperInvariant() + " GOES FIRST";

            var dim = Ui.FillPanel(_tossLayer, "TossDim", new Color(0.03f, 0.04f, 0.07f, 0.72f), 0f, raycast: true);
            _tossGroup = dim.gameObject.AddComponent<CanvasGroup>();
            Ui.Label(dim.transform, "COIN TOSS", 0, 90, Ui.Width, 70, 46, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _coin = Ui.Rect(dim.transform, "Coin", 0, 0, 240f, 240f);
            _coin.anchorMin = _coin.anchorMax = _coin.pivot = new Vector2(0.5f, 0.5f);
            var rim = Ui.Circle(_coin, "Rim", 0, 0, 240f, Ui.Hex("#C89A50"));
            Ui.AddOutline(rim.gameObject, new Color(0, 0, 0, 0.7f), 3f);
            Ui.Circle(_coin, "Edge", 12, 12, 216f, Ui.Hex("#8A6A30"));
            _coinFace = Ui.Circle(_coin, "Face", 22, 22, 196f, Mine);
            Ui.Circle(_coin, "Shine", 60, 34, 120f, 70f, new Color(1, 1, 1, 0.12f));
            _coinText = Ui.Label(_coinFace.transform, "", 0, 0, 196f, 196f, 64, Ui.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.AddOutline(_coinText.gameObject, new Color(0, 0, 0, 0.8f), 2f);
            _tossResult = Ui.Label(dim.transform, "", 0, 700, Ui.Width, 90, 58, Ui.Hex("#FFD070"), TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.AddOutline(_tossResult.gameObject, new Color(0, 0, 0, 0.8f), 3f);
            _tossStart = Time.unscaledTime;
            UpdateToss();
        }

        private string Initials(PlayerState p)
        {
            string name = p.TavernDwellerId != null ? _db.Get(p.TavernDwellerId).Name : p.Id.ToString();
            return string.Concat(name.Split(' ', ',').Where(x => x.Length > 0 && char.IsUpper(x[0])).Take(2).Select(x => x[0]));
        }

        private void UpdateToss()
        {
            if (!Tossing) return;
            float t = Time.unscaledTime - _tossStart;
            // A click skips to the result.
            if (t > 0.3f && t < TossLead + TossFlight && Input.GetMouseButtonDown(0)) { _tossStart = Time.unscaledTime - TossLead - TossFlight; t = TossLead + TossFlight; }
            float p = Mathf.Clamp01((t - TossLead) / TossFlight);
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            // Whole half-turns: an even number lands on the viewer's side, odd on the other.
            float turns = 11f + (_tossViewerFirst ? 1f : 0f);
            float angle = eased * turns * Mathf.PI;
            float c = Mathf.Cos(angle);
            bool viewerSide = c >= 0f;
            var me = _s.State.GetPlayer(_s.Viewer);
            var other = _s.State.Players.First(x => x.Id != _s.Viewer);
            _coinFace.color = viewerSide ? Mine : Theirs;
            _coinText.text = Initials(viewerSide ? me : other);
            _coin.localScale = new Vector3(1f, Mathf.Max(0.04f, Mathf.Abs(c)), 1f);
            _coin.anchoredPosition = new Vector2(0f, 20f + Mathf.Sin(p * Mathf.PI) * 170f);

            if (p >= 1f)
            {
                _coin.localScale = Vector3.one;
                _tossResult.text = _tossFirstName;
                float landed = t - TossLead - TossFlight;
                _tossResult.transform.localScale = Vector3.one * (1f + 0.25f * Mathf.Max(0f, 1f - landed * 4f));
                if (landed > TossRest) _tossGroup.alpha = 1f - (landed - TossRest) / TossFade;
                if (landed > TossRest + TossFade || landed > 0.4f && Input.GetMouseButtonDown(0)) EndToss();
            }
        }

        private void EndToss()
        {
            _tossStart = -1f;
            if (_tossLayer != null) Ui.Clear(_tossLayer);
            _nextBot = Time.unscaledTime + 0.4f / _speed;
            _dirty = true;
        }
    }
}
