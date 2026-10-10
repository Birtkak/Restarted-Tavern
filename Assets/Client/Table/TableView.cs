using System;
using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using RestartedTavern.Rules.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// The visual table (CLIENT_DESIGN §1): LoR screen logic with MTGA zones and hand, built in uGUI from code.
    /// It only talks to the client logic layer: <see cref="MatchSession"/> for the game, <see cref="TableSnapshot"/>
    /// for what to draw, <see cref="ActionPicker"/> for clicks, <see cref="CombatStage"/> for the combat lane and
    /// <see cref="TableControls"/> for the context button and the choice panel.
    ///
    /// The table is rebuilt from a fresh snapshot after every change; the change's events then animate on top
    /// (TableView.Beats.cs).
    ///
    /// Command line: -seed N, -deck1/-deck2 N, -bot1, -human2 (hot-seat), -autoplay N (the bot plays N actions for
    /// everyone at startup), -autopick (start picking the first usable card, to show targeting), -autoshot path.png
    /// (take a screenshot, then quit), -until attack|block (with -autoplay: stop early when the first seat can attack /
    /// must block, and stage every possible attacker or blocker, to show the combat lane).
    /// </summary>
    public sealed partial class TableView : MonoBehaviour
    {
        // Layout (1920×1080 reference, top-left origin).
        private const float CenterLeft = 290f, CenterRight = 1620f;
        private const float CenterX = (CenterLeft + CenterRight) / 2f;
        private const float UnitW = 124f, UnitH = 166f;
        private const float HandW = 150f, HandH = 210f;
        private const float OppRowY = 112f, LaneTop = 296f, LaneMid = 471f, LaneBottom = 646f, MyRowY = 656f;
        private const float HandTop = 862f;

        private static readonly Color Wood = Ui.Hex("#4A2F1C");
        private static readonly Color WoodDark = Ui.Hex("#2E1C10");
        private static readonly Color Felt = Ui.Hex("#5A3B24");
        private static readonly Color LaneColor = new Color(0.08f, 0.05f, 0.03f, 0.45f);
        private static readonly Color GlowSource = Ui.Hex("#40C0FF");
        private static readonly Color GlowTarget = Ui.Hex("#FF5050");
        private static readonly Color GlowCombat = Ui.Hex("#FFB020");
        private static readonly Color GlowSelected = Ui.Hex("#FFFFFF");
        /// <summary>Your card whose triggered ability is going off (playtest 2026-10-10_141453), and its Chain bubble.</summary>
        private static readonly Color GlowTrigger = Ui.Hex("#40E060");
        private static readonly Color ButtonColor = Ui.Hex("#7A4E22");
        private static readonly Color ContextOn = Ui.Hex("#C08A2A");
        private static readonly Color ContextOff = Ui.Hex("#4A4038");
        private static readonly Color ActingGlow = Ui.Hex("#FFC840");
        private static readonly Color Mine = Ui.Hex("#2A5A8A");
        private static readonly Color Theirs = Ui.Hex("#8A2A2A");

        private MatchSession _s;
        private ActionPicker _picker;
        private CombatStage _stage;
        private TableSnapshot _snap;
        private bool _dirty;
        private float _nextBot;
        private float _speed = 1f;

        private readonly int[] _deck = { 0, 1 };
        /// <summary>The Tavern Dweller per seat, or null for the deck's own.</summary>
        private readonly string[] _dweller = { null, null };
        private CardDatabase _db;
        /// <summary>The main menu (decks, Tavern Dwellers, human / bot, Battle) is open.</summary>
        private bool _menuOpen;
        private bool _editorAtStart; // -editor: open the deck editor (screenshots)
        private GameObject _loading;
        private readonly SeatKind[] _seats = { SeatKind.Human, SeatKind.Bot };
        private ulong _seed; // 0 = a random seed at startup
        private int _autoplay;
        /// <summary>-board N: N permanents on each side at the start (layout screenshots).</summary>
        private int _board;
        private bool _autopick;
        private string _autoshot;
        private string _until;
        private int _shotFrame = -1;
        /// <summary>-shotat seconds: take the -autoshot that long after the table is drawn (to catch the beats mid-way).</summary>
        private float _shotAt = -1f;
        /// <summary>-zoom chain|dweller: pin that zoom for the -autoshot (hover can't be automated).</summary>
        private string _zoomShot;
        /// <summary>-hover &lt;card id&gt;: the screenshot hovers that card in hand (lifted, full size).</summary>
        private string _hoverShot;
        private float _shotStart;

        private Camera _camera;
        private RectTransform _root, _dynamic, _dragLayer, _zoomLayer, _overlay;
        private readonly List<Image> _arrowBody = new List<Image>();
        private Image _arrowHead;
        private const int ArrowSegments = 22;
        private readonly List<CardWidget> _widgets = new List<CardWidget>();
        private CardWidget _dragging;
        private ObjectId _selectedBlocker = ObjectId.None;
        private ObjectId _pinnedZoom = ObjectId.None;
        private CardWidget _hovered;
        /// <summary>The rect that keeps the hover zoom open, when it isn't the card itself (the Power coin).</summary>
        private RectTransform _hoverRect;
        private int _cancelFrame = -1;
        private Zone? _browsing;
        private PlayerId _browsingPlayer;

        // Highlights for the current refresh.
        private HashSet<ObjectId> _sources = new HashSet<ObjectId>();
        private HashSet<ObjectId> _triggerSources = new HashSet<ObjectId>();
        private HashSet<Target> _targets = new HashSet<Target>();
        private HashSet<ObjectId> _combatCandidates = new HashSet<ObjectId>();

        private bool Declaring => _s.State.Pending?.Kind == DecisionKind.DeclareAttackers || _s.State.Pending?.Kind == DecisionKind.DeclareBlockers;

        // ------------------------------------------------------------------ setup

        private System.Collections.IEnumerator Start()
        {
            CardPool.DataRoot = Application.streamingAssetsPath;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string next = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i])
                {
                    case "-seed": ulong.TryParse(next, out _seed); break;
                    case "-deck1": int.TryParse(next, out _deck[0]); break;
                    case "-deck2": int.TryParse(next, out _deck[1]); break;
                    case "-bot1": _seats[0] = SeatKind.Bot; break;
                    case "-human2": _seats[1] = SeatKind.Human; break;
                    case "-autoplay": int.TryParse(next, out _autoplay); break;
                    case "-autopick": _autopick = true; break;
                    case "-autoshot": _autoshot = next; break;
                    case "-zoom": _zoomShot = next; break;
                    case "-hover": _hoverShot = next; break;
                    case "-shotat": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _shotAt); break;
                    case "-until": _until = next; break;
                    case "-board": int.TryParse(next, out _board); break;
                    case "-menu": _menuOpen = true; break;
                    case "-editor": _menuOpen = true; _editorAtStart = true; break;
                    case "-debug": _debugOpen = true; break;
                    case "-reveal": _revealHands = true; break;
                    case "-bugreport": _bugNote = next ?? ""; _autoBugReport = true; break;
                }
            }
            if (_seed == 0) _seed = (ulong)(Environment.TickCount & 0x7fffffff) % 1000000 + 1;
            BuildCanvas();

            // Loading screen while the cards and decks are read (skipped for automated screenshots).
            bool interactive = _autoshot == null && _autoplay == 0;
            float shown = Time.realtimeSinceStartup;
            if (interactive)
            {
                ShowLoading();
                yield return null;
                yield return null;
            }
            _db = CardPool.CreateDatabase();
            CardPool.CustomDecksFile = System.IO.Path.Combine(Application.persistentDataPath, "custom_decks.json"); // the deck editor's
            CardPool.PrototypeDecks();
            bool menu = _menuOpen; // -menu (NewGame closes the menu)
            NewGame(_seed);
            if (_board > 0) { FillBoard(_board); OnSessionChanged(); }
            _menuOpen = menu;
            if (_editorAtStart) OpenEditor(CardPool.PrototypeDecks()[_deck[0]]);
            if (interactive)
            {
                while (Time.realtimeSinceStartup - shown < 1.2f) yield return null;
                Destroy(_loading);
                _menuOpen = true;
                _dirty = true;
            }

            if (_autoplay > 0)
            {
                for (int i = 0; i < _autoplay && _s.WaitingOn != null; i++)
                {
                    if (_until != null && _s.WaitingOn == _s.State.Players[0].Id && !_s.HandoffPending
                        && (_until == "block" ? CombatStage.CanBlock(_s) : CombatStage.ForAttack(_s)?.Candidates().Count > 0)) break;
                    if (i == _autoplay - 1 || _until != null) _pendingEvents.Clear(); // only the last action animates
                    _s.AutoStep();
                }
                if (_s.HandoffPending) _s.AcknowledgeHandoff();
                OnSessionChanged();
                if (_until != null && _stage != null)
                    foreach (var id in _stage.Candidates().ToList())
                    {
                        if (_stage.IsBlocking) { var b = _stage.BlockableBy(id); if (b.Count > 0) _stage.StageBlocker(id, b[0]); }
                        else _stage.StageAttacker(id);
                    }
            }
            if (_autopick && _s.HumanToAct)
            {
                // Prefer a card that asks for a target, to show the targeting highlights.
                foreach (var src in _picker.Sources)
                {
                    _picker.Begin(src);
                    if (_picker.Prompt != null && _picker.Prompt.Targets.Any()) break;
                }
                _dirty = true;
            }
            if (_autoBugReport)
            {
                Refresh();
                SaveBugReport();
            }
            if (_autoshot != null) { _shotFrame = 0; _shotStart = Time.time; }
        }

        private bool _autoBugReport;
        /// <summary>A game was started from the menu (so "Back to the game" makes sense).</summary>
        private bool _battleStarted;

        private void NewGame(ulong seed)
        {
            _seed = seed;
            var decks = CardPool.PrototypeDecks();
            _deck[0] = Mathf.Clamp(_deck[0], 0, decks.Count - 1);
            _deck[1] = Mathf.Clamp(_deck[1], 0, decks.Count - 1);
            _s = new MatchSession(MatchSetup.Duel(_deck[0], _dweller[0], _deck[1], _dweller[1], _seats[0], _seats[1], seed));
            _menuOpen = false;
            _pinnedZoom = ObjectId.None;
            _browsing = null;
            HookBeats();
            OnSessionChanged();
            StartLog();
        }

        private void BuildCanvas()
        {
            _camera = Camera.main;
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            if (_camera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _camera;
                canvas.planeDistance = 10f;
            }
            else canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Ui.Width, Ui.Height);
            // Expand: the whole 1920x1080 table always fits; extra width or height shows plain wood around it.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            Ui.FillPanel(canvasGo.transform, "Backdrop", WoodDark);
            _root = Ui.Rect(canvasGo.transform, "Table", 0, 0, Ui.Width, Ui.Height);
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0.5f, 0.5f);
            _root.anchoredPosition = Vector2.zero;
            BuildBackground(_root);
            _dynamic = Ui.Fill(_root, "Dynamic");
            _fxLayer = Ui.Fill(_root, "Fx");
            _zoomLayer = Ui.Fill(_root, "Zoom");
            _overlay = Ui.Fill(_root, "Overlay");
            _dragLayer = Ui.Fill(_root, "Drag");

            for (int i = 0; i < ArrowSegments; i++)
            {
                var seg = Ui.Panel(_dragLayer, "Arrow", 0, 0, 10, 10, Color.white);
                seg.rectTransform.anchorMin = seg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                seg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                Ui.AddOutline(seg.gameObject, new Color(0f, 0f, 0f, 0.55f), 1.5f);
                seg.gameObject.SetActive(false);
                _arrowBody.Add(seg);
            }
            _arrowHead = Ui.Panel(_dragLayer, "ArrowHead", 0, 0, 46, 40, Color.white);
            _arrowHead.sprite = Ui.TriangleSprite;
            _arrowHead.rectTransform.anchorMin = _arrowHead.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _arrowHead.rectTransform.pivot = new Vector2(0.3f, 0.5f);
            Ui.AddOutline(_arrowHead.gameObject, new Color(0f, 0f, 0f, 0.6f), 2f);
            _arrowHead.gameObject.SetActive(false);
        }

        private static void BuildBackground(RectTransform root)
        {
            Ui.FillPanel(root, "Wood", Wood, 0f, raycast: true);
            // Plank lines for a hint of the tavern table (the art pass replaces this).
            for (int i = 0; i < 9; i++)
                Ui.Panel(root, "Plank", 0, 60 + i * 120, Ui.Width, 3, WoodDark * new Color(1, 1, 1, 0.6f));
            Ui.Panel(root, "LeftRail", 0, 0, 280, Ui.Height, WoodDark * new Color(1, 1, 1, 0.55f));
            Ui.Panel(root, "RightRail", 1630, 0, 290, Ui.Height, WoodDark * new Color(1, 1, 1, 0.55f));
            Ui.Panel(root, "OppRow", CenterLeft, OppRowY - 6, CenterRight - CenterLeft, UnitH + 12, Felt * new Color(1, 1, 1, 0.6f));
            Ui.Panel(root, "MyRow", CenterLeft, MyRowY - 6, CenterRight - CenterLeft, UnitH + 12, Felt * new Color(1, 1, 1, 0.6f));
            Ui.Panel(root, "Lane", CenterLeft, LaneTop, CenterRight - CenterLeft, LaneBottom - LaneTop, LaneColor);
            Ui.Panel(root, "LaneLine", CenterLeft, LaneMid - 1, CenterRight - CenterLeft, 2, new Color(1f, 0.85f, 0.6f, 0.25f));
            Ui.Label(root, "COMBAT LANE", CenterLeft + 10, LaneMid - 14, 200, 28, 16, new Color(1f, 0.85f, 0.6f, 0.3f), TextAnchor.MiddleLeft, FontStyle.Bold);
        }

        // ------------------------------------------------------------------ session changes

        private void OnSessionChanged()
        {
            _picker = _s.NewPicker();
            _selectedBlocker = ObjectId.None;
            if (_s.CombatInProgress) _stage = null;
            else if (CombatStage.CanBlock(_s)) _stage = CombatStage.ForBlock(_s);
            else if (CombatStage.CanAttack(_s)) _stage = CombatStage.ForAttack(_s);
            else _stage = null;
            _dirty = true;
        }

        private void Run(Action a)
        {
            try { a(); }
            catch (Exception e) { Debug.LogException(e); }
            OnSessionChanged();
            _nextBot = Time.unscaledTime + 0.5f / _speed;
        }

        private void Submit(PlayerAction action)
        {
            if (_s.HumanToAct) LogAction(action);
            Run(() => _s.Submit(action));
        }

        private void PressContext()
        {
            if (Busy) { SkipBeats(); return; }
            var b = TableControls.Main(_s, _stage);
            if (!b.Enabled) return;
            _picker.Cancel();
            switch (b.Mode)
            {
                case ButtonMode.Handoff: Run(_s.AcknowledgeHandoff); break;
                case ButtonMode.Attack:
                case ButtonMode.Block: Run(() => _s.CommitCombat(_stage)); break;
                default:
                    if (b.Action != null) Submit(b.Action);
                    break;
            }
        }

        private void Undo()
        {
            if (!_s.CanUndo) return;
            LogLine("(undo)");
            ResetBeats();
            Run(_s.Undo);
        }

        private void Update()
        {
            if (_loadingBar != null)
                _loadingBar.rectTransform.sizeDelta = new Vector2(Mathf.Min(600f, Time.realtimeSinceStartup / 1.2f * 600f), 14f);
            if (_s == null) return;
            UpdateDebug();

            if (_shotFrame >= 0)
            {
                UpdateBeats();
                if (_dirty) Refresh();
                if (_zoomShot != null && _snap != null)
                {
                    var pin = _zoomShot == "chain" ? _widgets.LastOrDefault(x => x != null && x.Kind == WidgetKind.ChainItem)
                        : _widgets.FirstOrDefault(x => x != null && x.Kind == WidgetKind.TavernDweller && x.Player == _snap.Viewer);
                    _zoomShot = null;
                    if (pin != null) { _pinnedZoom = pin.Id; ShowZoom(pin); }
                }
                if (_hoverShot != null && _snap != null)
                {
                    var card = _widgets.FirstOrDefault(x => x != null && x.Kind == WidgetKind.HandCard && x.View?.DefinitionId == _hoverShot);
                    _hoverShot = null;
                    if (card != null) OnHover(card, true);
                }
                _shotFrame++;
                if (_shotAt >= 0f)
                {
                    // Timed: the beats run in real time; shoot once, quit a few frames later.
                    if (_shotFrame > 0 && _shotFrame < 1000000 && Time.time - _shotStart >= _shotAt)
                    {
                        ScreenCapture.CaptureScreenshot(_autoshot);
                        _shotFrame = 1000000;
                    }
                    if (_shotFrame > 1000008) Application.Quit();
                    return;
                }
                if (_shotFrame == 5) ScreenCapture.CaptureScreenshot(_autoshot);
                if (_shotFrame == 12) Application.Quit();
                return;
            }

            // Keyboard shortcuts act on the table, never on the last clicked button.
            if (EventSystem.current != null && !_bugOpen) EventSystem.current.SetSelectedGameObject(null);
            if (_dragging == null && !_menuOpen && !_bugOpen)
            {
                if (Input.GetKeyDown(KeyCode.Space)) PressContext();
                else if (Busy && Input.GetMouseButtonDown(0)) SkipBeats();
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1) && _picker.IsPicking)
                {
                    _cancelFrame = Time.frameCount;
                    CancelPicking();
                }
                // A pinned zoom closes on the next left click or Escape.
                if (!_pinnedZoom.IsNone && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape)))
                {
                    _pinnedZoom = ObjectId.None;
                    Ui.Clear(_zoomLayer);
                }
                if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) Undo();
            }
            // A hover zoom whose card is gone or no longer under the mouse (redrawn under it, exit never came) closes.
            if (_pinnedZoom.IsNone && _zoomLayer.childCount > 0 && _dragging == null
                && (_hovered == null || !RectTransformUtility.RectangleContainsScreenPoint(_hoverRect != null ? _hoverRect : _hovered.Rect, Input.mousePosition, CanvasCamera)))
            {
                _hovered = null;
                Ui.Clear(_zoomLayer);
            }

            UpdateFan();
            UpdateBeats();
            if (_s.BotToAct && _dragging == null && !_menuOpen && !_bugOpen && _bugShotFrame < 0 && Time.unscaledTime >= _nextBot)
            {
                try { _s.StepBot(); }
                catch (Exception e) { Debug.LogException(e); }
                OnSessionChanged();
                _nextBot = Time.unscaledTime + 0.6f / _speed;
            }

            if (_dirty && _dragging == null) Refresh();
            UpdateArrow();
        }

        private void CancelPicking()
        {
            if (!_picker.IsPicking && _selectedBlocker.IsNone) return;
            _picker.Cancel();
            _selectedBlocker = ObjectId.None;
            _dirty = true;
        }

        // ------------------------------------------------------------------ drawing

        private void Refresh()
        {
            _dirty = false;
            CaptureOld();
            ClearSpendPreview();
            _snap = _s.Snapshot();
            Ui.Clear(_dynamic);
            Ui.Clear(_zoomLayer);
            Ui.Clear(_overlay);
            _widgets.Clear();

            bool myCall = _s.HumanToAct;
            _sources = myCall && !_picker.IsPicking && !Declaring ? _picker.Sources : new HashSet<ObjectId>();
            _targets = new HashSet<Target>();
            if (myCall && _picker.IsPicking && _picker.Prompt != null) _targets.UnionWith(_picker.Prompt.Targets);
            else if (myCall)
                foreach (var a in _picker.Legal.Where(a => a.Kind == ActionKind.ChooseTarget && a.Targets.Length > 0)) _targets.Add(a.Targets[0]);
            _combatCandidates = myCall && _stage != null ? _stage.Candidates() : new HashSet<ObjectId>();
            _triggerSources = new HashSet<ObjectId>();
            if (_snap.Decision == DecisionKind.ChooseTriggerTarget && myCall && !_snap.DecisionSource.IsNone) _triggerSources.Add(_snap.DecisionSource);
            foreach (var item in _snap.Chain.Where(i => i.Kind == ChainItemKind.TriggeredAbility && i.Controller == _snap.Viewer))
            {
                _triggerSources.Add(item.ObjectId);
                if (!item.Source.IsNone) _triggerSources.Add(item.Source);
            }
            if (_stage != null && _stage.IsBlocking && !_selectedBlocker.IsNone)
                foreach (var a in _stage.BlockableBy(_selectedBlocker)) _targets.Add(Target.ForObject(a));

            var me = _snap.Player(_snap.Viewer);
            var opp = _snap.Players.First(p => p.Id != _snap.Viewer);

            DrawDweller(opp, top: true);
            DrawDweller(me, top: false);
            DrawSide(opp, top: true);
            DrawSide(me, top: false);
            DrawBoard(me, opp);
            DrawOpponentHand(opp);
            DrawHand(me);
            DrawChain();
            DrawHistory();
            PlayBeats(); // after the cards are drawn, before the context button (it's blank while beats play)
            DrawContext();
            DrawPromptAndChoices();
            DrawTopBar();
            if (_browsing != null) DrawBrowser();
            if (!_pinnedZoom.IsNone) ShowZoom(_widgets.FirstOrDefault(w => w.Id == _pinnedZoom));

            if (_menuOpen && _editorOpen) DrawEditor();
            else if (_menuOpen) DrawMenu();
            else if (_snap.IsGameOver) DrawGameOver();
            else if (_s.HandoffPending) DrawHandoff();
            DrawDebug();
            DrawBugDialog();
            DrawToast();
        }

        private CardWidget MakeWidget(Transform parent, WidgetKind kind, CardView v, float cx, float cy, float w, float h, float rotation = 0f)
        {
            var rt = Ui.Rect(parent, kind + " " + (v?.Name ?? "?"), 0, 0, w, h);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, -cy);
            rt.localEulerAngles = new Vector3(0, 0, rotation);
            var widget = rt.gameObject.AddComponent<CardWidget>();
            widget.Table = this;
            widget.Kind = kind;
            widget.View = v;
            widget.HomePosition = rt.anchoredPosition;
            widget.HomeRotation = rotation;
            if (kind == WidgetKind.ChainItem || kind == WidgetKind.TavernDweller || kind == WidgetKind.History) { } // DrawChain / DrawDweller draw these
            else if (v == null || v.IsHidden) CardFaces.BuildBack(rt);
            else CardFaces.Build(rt, v, kind == WidgetKind.HandCard || kind == WidgetKind.ChainItem ? FaceStyle.Hand : FaceStyle.Unit);
            _widgets.Add(widget);
            if (kind != WidgetKind.ChainItem && kind != WidgetKind.TavernDweller && kind != WidgetKind.History) ApplyGlow(widget); // these glow once their frame exists
            return widget;
        }

        private void ApplyGlow(CardWidget w)
        {
            var id = w.Id;
            bool target = (!id.IsNone && _targets.Contains(Target.ForObject(id)))
                          || (w.Kind == WidgetKind.TavernDweller && _targets.Contains(Target.ForPlayer(w.Player)));
            if (!id.IsNone && id == _picker.Source || !_selectedBlocker.IsNone && id == _selectedBlocker) w.SetGlow(GlowSelected);
            else if (_triggerSources.Contains(id)) w.SetGlow(GlowTrigger); // over red: it may target itself, the arrow starts here
            else if (target) w.SetGlow(GlowTarget);
            else if (_combatCandidates.Contains(id)) w.SetGlow(GlowCombat);
            else if (_sources.Contains(id) && w.Kind != WidgetKind.TavernDweller) w.SetGlow(GlowSource);
            else w.SetGlow(null);
        }

        /// <summary>
        /// The Tavern Dweller's spot, Hearthstone hero style, in LoR's Nexus place (left edge): an oval portrait in a gold
        /// frame with a name ribbon, life in a red gem at the bottom right, and the Power as a round coin beside it with its
        /// cost on top (lit when usable, dark and "USED" once used this round). The portrait is the player's target.
        /// </summary>
        private void DrawDweller(PlayerView p, bool top)
        {
            const float pw = 186f, ph = 224f;
            float y = top ? 104f : 650f;
            float cx = 118f, cy = y + ph / 2f;
            Ui.Label(_dynamic, p.Id == _snap.Viewer ? "YOU" : "OPPONENT", 20, top ? y - 30 : y + ph + 4, pw, 24, 15,
                Ui.Hex("#E0C890"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var w = MakeWidget(_dynamic, WidgetKind.TavernDweller, p.TavernDweller, cx, cy, pw, ph);
            w.Player = p.Id;
            var def = p.TavernDweller?.DefinitionId != null ? _db.Get(p.TavernDweller.DefinitionId) : null;
            var first = CardFaces.Style(def != null && def.TavernDwellerFactions.Length > 0 ? def.TavernDwellerFactions[0] : null);
            var second = CardFaces.Style(def != null && def.TavernDwellerFactions.Length > 1 ? def.TavernDwellerFactions[1] : null);

            // Frame: gold oval, dark rim, portrait (two faction colours until the art drops in), initials as the face.
            Ui.Circle(w.transform, "Frame", 0, 0, pw, ph, Ui.Hex("#C89A50"), raycast: true);
            Ui.Circle(w.transform, "Rim", 6, 6, pw - 12, ph - 12, Ui.Hex("#2A1A0E"));
            Ui.Circle(w.transform, "PortraitA", 12, 12, pw - 24, ph - 24, first.Frame);
            var lower = Ui.Circle(w.transform, "PortraitB", 12, ph * 0.45f, pw - 24, ph * 0.55f - 12, second.Frame * new Color(1, 1, 1, 0.85f));
            Ui.Circle(w.transform, "Shine", pw * 0.25f, 22, pw * 0.5f, ph * 0.28f, new Color(1f, 1f, 1f, 0.08f));
            string initials = string.Concat((p.TavernDweller?.Name ?? "?").Split(' ', ',').Where(x => x.Length > 0 && char.IsUpper(x[0])).Take(2).Select(x => x[0]));
            var face = Ui.Label(w.transform, initials, 0, 36, pw, 90, 64, first.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.AddOutline(face.gameObject, new Color(0, 0, 0, 0.8f), 2f);

            // Name ribbon across the bottom of the oval.
            var ribbon = Ui.Panel(w.transform, "Ribbon", -12, ph - 58, pw - 44, 30, Ui.Hex("#6A4420"));
            Ui.AddOutline(ribbon.gameObject, Ui.Hex("#E0B060"), 2f);
            Ui.FillLabel(ribbon.transform, p.TavernDweller?.Name ?? p.Id.ToString(), 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 4f).resizeTextForBestFit = true;

            // Life: a red gem at the bottom right (Hearthstone health).
            var lifeGem = Ui.Circle(w.transform, "Life", pw - 62, ph - 64, 70, 70, p.Life <= 5 ? Ui.Hex("#FF3020") : Ui.Hex("#B01818"));
            Ui.AddOutline(lifeGem.gameObject, Ui.Hex("#FFD080"), 2f);
            var lifeText = Ui.FillLabel(lifeGem.transform, p.Life.ToString(), 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.AddOutline(lifeText.gameObject, new Color(0, 0, 0, 0.9f), 2f);
            if (p.HasLost) Ui.Circle(w.transform, "Lost", 0, 0, pw, ph, new Color(0, 0, 0, 0.65f));
            ApplyGlow(w);

            // Whose action it is (LoR): a gold glow on that portrait; a "PASSED" chip on whoever passed the action.
            bool acting = !_snap.IsGameOver && _s.WaitingOn == p.Id;
            float tagY = top ? ph + 4 : -28;
            if (acting && !_targets.Contains(Target.ForPlayer(p.Id)))
            {
                w.SetGlow(ActingGlow);
                var tag = Ui.Panel(w.transform, "Acting", 18, tagY, pw - 36, 24, ActingGlow);
                Ui.FillLabel(tag.transform, p.Id == _snap.Viewer ? "YOUR ACTION" : "THEIR ACTION", 14, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            if (_passed.Contains(p.Id) && !acting)
            {
                var chip = Ui.Panel(w.transform, "Passed", 18, tagY, pw - 36, 24, Ui.Hex("#5A5048"));
                Ui.FillLabel(chip.transform, "PASSED", 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            if (p.HasPower) DrawPowerCoin(p, w, 214f, y + ph - 96f);
        }

        /// <summary>The Power as a Hearthstone hero-power coin. Hover shows the Tavern Dweller card; click uses it.</summary>
        private void DrawPowerCoin(PlayerView p, CardWidget dweller, float x, float y)
        {
            const float d = 72f;
            bool usable = p.TavernDweller != null && _sources.Contains(p.TavernDweller.Id);
            var coin = Ui.Button(_dynamic, "", x, y, d, d, Ui.Hex("#C89A50"),
                () => { if (p.TavernDweller != null && _picker.Begin(p.TavernDweller.Id)) AfterPick(); }, 12, usable);
            coin.image.sprite = Ui.CircleSprite;
            coin.image.alphaHitTestMinimumThreshold = 0.5f;
            var colors = coin.colors;
            colors.disabledColor = Color.white; // the coin shows its own state
            coin.colors = colors;
            var disc = Ui.Circle(coin.transform, "Disc", 6, 6, d - 12, d - 12,
                p.PowerUsed ? Ui.Hex("#2A2420") : usable ? Ui.Hex("#2A70C0") : Ui.Hex("#4A3E30"));
            Ui.Label(disc.transform, p.PowerUsed ? "USED" : "POWER", 0, 0, d - 12, d - 12, 13,
                p.PowerUsed ? Ui.Hex("#8A8070") : Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (!p.PowerUsed)
            {
                var cost = Ui.Circle(coin.transform, "Cost", d / 2f - 15, -16, 30, 30, CardFaces.CostColor);
                Ui.AddOutline(cost.gameObject, new Color(0, 0, 0, 0.8f), 1.5f);
                Ui.FillLabel(cost.transform, p.PowerCost.ToString(), 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            if (usable)
            {
                Ui.AddOutline(coin.gameObject, GlowSource, 4f);
                Pulse(coin.transform, 0.07f);
            }
            // Hover: the Tavern Dweller card (with the Power text) in the zoom.
            var trigger = coin.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => { if (_pinnedZoom.IsNone) { _hovered = dweller; ShowZoom(dweller); _hoverRect = (RectTransform)coin.transform; } });
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => { if (_pinnedZoom.IsNone) { _hovered = null; _hoverRect = null; Ui.Clear(_zoomLayer); } });
            trigger.triggers.Add(exit);
        }

        private void DrawSide(PlayerView p, bool top)
        {
            // Piles: deck, graveyard, exile (click to browse the open ones).
            float pileY = top ? 12f : 968f;
            Pile("Deck", p.DeckCount, 1650, pileY, null, p.Id);
            Pile("Grave", p.Graveyard.Count, 1716, pileY, Zone.Graveyard, p.Id);
            Pile("Exile", p.Exile.Count, 1782, pileY, Zone.Exile, p.Id);

            // Mana as a column of gems (filled = available, outline = spent); Gold as spell mana under it.
            float manaTop = top ? 130f : 694f;
            var manaGems = _manaGems[p.Id] = new List<Image>();
            var goldGems = _goldGems[p.Id] = new List<Image>();
            Ui.Label(_dynamic, p.Mana + "/" + p.MaxMana, 1846, manaTop - 30 + (top ? 0 : 0), 70, 26, 18, Ui.Hex("#A0D0FF"), TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int i = 0; i < 10; i++)
            {
                float gy = top ? manaTop + i * 25f : manaTop + (9 - i) * 25f;
                Color c = i < p.Mana ? Ui.Hex("#3AA0FF") : i < p.MaxMana ? Ui.Hex("#1A3048") : new Color(1, 1, 1, 0.06f);
                var gem = Ui.Panel(_dynamic, "Mana", 1872, gy, 22, 22, c);
                if (i < p.MaxMana) Ui.AddOutline(gem.gameObject, Ui.Hex("#80C0FF"), 1.5f);
                manaGems.Add(gem);
            }
            float goldY = top ? manaTop + 258f : manaTop - 58f;
            for (int i = 0; i < Math.Max(3, p.GoldCap); i++)
            {
                Color c = i < p.Gold ? Ui.Hex("#F0C030") : i < p.GoldCap ? Ui.Hex("#4A3A18") : new Color(1, 1, 1, 0.06f);
                var gem = Ui.Panel(_dynamic, "Gold", 1818 + i * 30, goldY, 20, 20, c);
                gem.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
                if (i < p.GoldCap) Ui.AddOutline(gem.gameObject, Ui.Hex("#FFE090"), 1.5f);
                goldGems.Add(gem);
            }

            // The attack token next to the leader's gems (dim once used this round).
            if (p.HasAttackToken)
            {
                float ty = top ? manaTop : manaTop + 180f;
                var token = Ui.Panel(_dynamic, "AttackToken", 1816, ty, 48, 48, _snap.AttackUsed ? Ui.Hex("#4A3A30") : Ui.Hex("#D04A20"));
                _token = token.rectTransform;
                Ui.AddOutline(token.gameObject, Ui.Hex("#FFD080"), 2f);
                Ui.FillLabel(token.transform, "ATK", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        private void Pile(string label, int count, float x, float y, Zone? zone, PlayerId owner)
        {
            var img = Ui.Panel(_dynamic, label, x, y, 58, 84, label == "Deck" ? CardFaces.Back : Ui.Hex("#2A2420"), raycast: zone != null);
            Ui.AddOutline(img.gameObject, CardFaces.BackAccent * new Color(1, 1, 1, 0.6f), 2f);
            Ui.Label(img.transform, label, 0, 4, 58, 20, 13, Ui.Hex("#E0C890"));
            Ui.Label(img.transform, count.ToString(), 0, 28, 58, 44, 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (zone != null && count > 0)
            {
                var b = img.gameObject.AddComponent<Button>();
                b.onClick.AddListener(() =>
                {
                    _browsing = _browsing == zone && _browsingPlayer == owner ? (Zone?)null : zone;
                    _browsingPlayer = owner;
                    _dirty = true;
                });
            }
        }

        private void DrawBoard(PlayerView me, PlayerView opp)
        {
            // Who stands in the combat lane: declared attackers / blockers, plus what the viewer has staged.
            var laneAttackers = new List<CardView>();
            var laneBlockers = new List<(CardView unit, ObjectId blocks)>();
            foreach (var p in _snap.Players)
                foreach (var c in p.Battlefield)
                {
                    if (c.IsAttacking) laneAttackers.Add(c);
                    else if (!c.Blocks.IsNone) laneBlockers.Add((c, c.Blocks));
                }
            if (_stage != null)
                foreach (var st in _stage.Staged)
                {
                    var c = _snap.Find(st.Creature);
                    if (c == null) continue;
                    if (_stage.IsBlocking) laneBlockers.Add((c, st.Blocks));
                    else laneAttackers.Add(c);
                }
            var inLane = new HashSet<ObjectId>(laneAttackers.Select(c => c.Id).Concat(laneBlockers.Select(b => b.unit.Id)));
            var tucked = CollectAttachments(); // drawn behind their host, wherever it stands

            DrawRow(opp.Battlefield.Where(c => !inLane.Contains(c.Id) && !tucked.Contains(c.Id)).ToList(), OppRowY, false);
            DrawRow(me.Battlefield.Where(c => !inLane.Contains(c.Id) && !tucked.Contains(c.Id)).ToList(), MyRowY, true);

            // Attackers in a row across the lane, tilted once declared (attacking taps them, but lying sideways took too
            // much room, playtest 2026-10-10_144700); each blocker stands in front of its attacker. Staged attackers stay
            // upright until the attack is confirmed. A crowded lane shrinks the cards until they fit (down to half size).
            float laneW = CenterRight - CenterLeft - 40f;
            int laneCount = Math.Max(1, laneAttackers.Count);
            float laneScale = Mathf.Clamp(laneW / (laneCount * (UnitW + 16f)), 0.5f, 1f);
            float step = Math.Min((UnitW + 16f) * laneScale, laneW / laneCount);
            float x0 = CenterX - step * (laneAttackers.Count - 1) / 2f;
            var attackerX = new Dictionary<ObjectId, float>();
            for (int i = 0; i < laneAttackers.Count; i++)
            {
                var c = laneAttackers[i];
                float x = x0 + i * step;
                attackerX[c.Id] = x;
                bool mine = c.Controller == _snap.Viewer;
                float tilt = c.IsAttacking && c.Tapped ? (mine ? -12f : 12f) : 0f;
                float half = UnitH * laneScale / 2f;
                var w = DrawPermanent(c, x, mine ? LaneMid + 6 + half : LaneMid - 6 - half, laneScale, false, tilt);
                if (_stage != null && _stage.Staged.Any(st => st.Creature == c.Id)) Tag(w, "staged");
            }
            var perAttacker = new Dictionary<ObjectId, int>();
            foreach (var (unit, blocks) in laneBlockers)
            {
                perAttacker.TryGetValue(blocks, out int n);
                perAttacker[blocks] = n + 1;
                float x = (attackerX.TryGetValue(blocks, out var ax) ? ax : CenterX) + n * 34f * laneScale;
                bool mine = unit.Controller == _snap.Viewer;
                float half = UnitH * laneScale / 2f;
                DrawPermanent(unit, x, mine ? LaneMid + 6 + half : LaneMid - 6 - half, laneScale, false);
            }
        }

        private void Tag(CardWidget w, string text)
        {
            var tag = Ui.Panel(w.transform, "Tag", 10, -20, UnitW - 20, 18, new Color(0, 0, 0, 0.7f));
            Ui.FillLabel(tag.transform, text, 12, GlowCombat, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void DrawHand(PlayerView me)
        {
            int n = me.Hand.Count;
            if (n == 0) return;
            float step = Math.Min(HandW - 14f, 980f / n);
            float x0 = CenterX - step * (n - 1) / 2f;
            for (int i = 0; i < n; i++)
            {
                float off = i - (n - 1) / 2f;
                var w = MakeWidget(_dynamic, WidgetKind.HandCard, me.Hand[i], x0 + i * step, HandTop + HandH / 2f + 16f + off * off * 2.2f,
                    HandW, HandH, -off * 2.5f);
                w.HomeSibling = w.transform.GetSiblingIndex();
            }
        }

        private void DrawOpponentHand(PlayerView opp)
        {
            var hand = RevealedHand(opp);
            int n = hand.Count;
            float step = Math.Min(70f, 700f / Math.Max(1, n));
            float x0 = CenterX - step * (n - 1) / 2f;
            for (int i = 0; i < n; i++)
            {
                float off = i - (n - 1) / 2f;
                MakeWidget(_dynamic, WidgetKind.OpponentHandCard, hand[i], x0 + i * step, (hand[i].IsHidden ? 22f : 64f) - off * off * 1.5f, 84f, 118f,
                    hand[i].IsHidden ? 180f + off * 3f : off * 3f);
            }
            Ui.Label(_dynamic, "Hand " + n, CenterX + 380, 8, 120, 26, 16, Ui.Hex("#E0C890"), TextAnchor.MiddleLeft);
        }

        /// <summary>
        /// LoR spell stack: the Chain as a row of card bubbles in the middle of the table, oldest on the left, the next
        /// to resolve on the right (larger, "NEXT"). Each bubble is a widget keyed by the item's object id, so
        /// "target spell / ability" picks it by clicking it, and hovering it zooms the full card. Lines run from each
        /// bubble to what it targets.
        /// </summary>
        private void DrawChain()
        {
            int n = _snap.Chain.Count;
            if (n == 0) return;
            // Units in the lane (combat): keep the middle free, the bubbles go small to the right end of the lane.
            bool laneBusy = _snap.Players.Any(p => p.Battlefield.Any(c => c.IsAttacking || !c.Blocks.IsNone)) || _stage != null && _stage.Staged.Count > 0;
            float w = laneBusy ? 64f : 96f, h = w;
            float topW = laneBusy ? 80f : 124f, topH = topW;
            float step = Math.Min(laneBusy ? w + 8f : w + 70f, (laneBusy ? 300f : 1100f) / Math.Max(1, n));
            float right = laneBusy ? CenterRight - topW / 2f - 10f : CenterX + (step * (n - 1)) / 2f + (topW - w) / 2f;
            float cy = LaneMid;

            Ui.Panel(_dynamic, "ChainBand", laneBusy ? CenterRight - 330f : CenterLeft, LaneTop + 4, laneBusy ? 330f : CenterRight - CenterLeft,
                LaneBottom - LaneTop - 8, new Color(0.05f, 0.02f, 0.08f, 0.55f));
            Ui.Label(_dynamic, n == 1 ? "CHAIN" : "CHAIN · " + n + " · resolves right to left", laneBusy ? CenterRight - 330f : CenterX - 300f,
                LaneTop + 8, laneBusy ? 330f : 600f, 24, 16, Ui.Hex("#D8B8FF"), TextAnchor.MiddleCenter, FontStyle.Bold);

            var bubbles = new List<(CardWidget widget, ChainView item)>();
            for (int i = 0; i < n; i++)
            {
                var item = _snap.Chain[i];
                bool top = i == n - 1;
                float x = right - (n - 1 - i) * step;
                var view = ChainItemView(item);
                var widget = MakeWidget(_dynamic, WidgetKind.ChainItem, view, x, cy, top ? topW : w, top ? topH : h);
                Bubble(widget, view, item, top ? topW : w, top, laneBusy);
                ApplyGlow(widget); // the glow goes on the bubble's ring
                if (top) _chainTop = new Vector2(x, -cy);
                bubbles.Add((widget, item));
            }

            // Target lines (drawn after every card is on the table), from the bubble to each target.
            foreach (var (widget, item) in bubbles)
            {
                if (item.Targets == null) continue;
                var color = item.Controller == _snap.Viewer ? new Color(0.35f, 0.7f, 1f, 0.8f) : new Color(1f, 0.35f, 0.3f, 0.8f);
                foreach (var t in item.Targets)
                {
                    var target = t.IsPlayer ? _widgets.FirstOrDefault(x => x != null && x.Kind == WidgetKind.TavernDweller && x.Player == t.Player)
                                            : _widgets.FirstOrDefault(x => x != null && x.Id == t.Object && x != widget);
                    if (target == null) continue;
                    Line(Center(widget), Center(target), color);
                }
                widget.transform.SetAsLastSibling(); // bubbles above the lines
            }
        }

/// <summary>
        /// One LoR spell bubble: a ring in the caster's colour, the faction disc with its emblem and the cost, the
        /// name on a pill underneath, a kind tag (POWER / TRIGGER / ABILITY) for abilities and "NEXT" on the top item.
        /// Hover zooms the full card (the widget's view).
        /// </summary>
        private void Bubble(CardWidget widget, CardView v, ChainView item, float d, bool top, bool small)
        {
            bool mine = item.Controller == _snap.Viewer;
            var style = CardFaces.Style(v.Faction);
            var ring = Ui.Circle(widget.transform, "Frame", 0, 0, d, mine ? Ui.Hex("#3A8AE0") : Ui.Hex("#D04A3A"), raycast: true);
            if (top) Ui.AddOutline(ring.gameObject, Ui.Hex("#FFD080"), 3f);
            float inset = Mathf.Max(5f, d * 0.08f);
            var disc = Ui.Circle(widget.transform, "Disc", inset, inset, d - 2 * inset, style.Frame);
            Ui.Circle(widget.transform, "Shine", d * 0.22f, d * 0.14f, d * 0.36f, new Color(1f, 1f, 1f, 0.12f));
            var emblem = Ui.Label(disc.transform, style.Emblem, 0, 0, d - 2 * inset, d - 2 * inset, Mathf.RoundToInt(d * 0.3f), style.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.AddOutline(emblem.gameObject, new Color(0, 0, 0, 0.8f), 1.5f);
            if (item.Kind == ChainItemKind.Spell && v.Cost > 0)
            {
                var cost = Ui.Circle(widget.transform, "Cost", -4, -4, d * 0.3f, CardFaces.CostColor);
                Ui.FillLabel(cost.transform, v.Cost.ToString(), Mathf.RoundToInt(d * 0.17f), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            if (item.Kind != ChainItemKind.Spell)
            {
                string kind = item.IsTavernDwellerPower ? "POWER" : item.Kind == ChainItemKind.TriggeredAbility ? "TRIGGER" : "ABILITY";
                var tag = Ui.Panel(widget.transform, "Kind", d / 2f - 36, d - 14, 72, 18, Ui.Hex("#5A2A8A"));
                Ui.FillLabel(tag.transform, kind, 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            if (item.Times > 1) // one merged trigger that does it N times (Decision Log 2026-10-10)
            {
                float bd = Mathf.Max(22f, d * 0.32f);
                var badge = Ui.Circle(widget.transform, "Times", d - bd + 4, -4, bd, Ui.Hex("#FFD060"));
                Ui.AddOutline(badge.gameObject, new Color(0, 0, 0, 0.8f), 1.5f);
                Ui.FillLabel(badge.transform, "×" + item.Times, Mathf.RoundToInt(bd * 0.45f), Ui.Hex("#2A1A08"), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            if (small) return;
            float pillW = d + 56f;
            var pill = Ui.Panel(widget.transform, "Name", d / 2f - pillW / 2f, d + 6, pillW, 22, new Color(0, 0, 0, 0.75f));
            Ui.FillLabel(pill.transform, v.Name, 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 2f).resizeTextForBestFit = true;
            var who = Ui.Panel(widget.transform, "Who", d / 2f - 44, -22, 88, 18, mine ? Mine : Theirs);
            Ui.FillLabel(who.transform, (mine ? "YOU" : "OPPONENT") + (top ? " · NEXT" : ""), 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

/// <summary>
        /// LoR's play history: the last few plays down the left rail, newest on top (who, what, at whom). Hover an entry
        /// to zoom the card.
        /// </summary>
        private void DrawHistory()
        {
            if (_playLog.Count == 0) return;
            const float x = 14f, top = 366f, rowH = 36f;
            Ui.Label(_dynamic, "RECENT PLAYS", x, top, 250, 20, 13, Ui.Hex("#C8A878"), TextAnchor.MiddleLeft, FontStyle.Bold);
            int shown = 0;
            for (int i = _playLog.Count - 1; i >= 0 && shown < 5; i--, shown++)
            {
                var e = _playLog[i];
                bool mine = e.Player == _snap.Viewer;
                float y = top + 24 + shown * (rowH + 4);
                var view = e.DefinitionId != null ? ViewOf(e.DefinitionId, e.Player) : null;
                var w = MakeWidget(_dynamic, WidgetKind.History, view, x + 125, y + rowH / 2f, 250, rowH);
                var bg = Ui.FillPanel(w.transform, "Row", shown == 0 ? new Color(0.25f, 0.16f, 0.08f, 0.95f) : new Color(0.12f, 0.08f, 0.05f, 0.85f), 0f, raycast: true);
                Ui.Panel(w.transform, "Side", 0, 0, 5, rowH, mine ? Ui.Hex("#3A8AE0") : Ui.Hex("#D04A3A"));
                var style = CardFaces.Style(view?.Faction);
                var orb = Ui.Circle(w.transform, "Orb", 9, 3, rowH - 6, view == null ? Ui.Hex("#B0301E") : style.Frame);
                Ui.FillLabel(orb.transform, view == null ? "ATK" : style.Emblem, 10, view == null ? Color.white : style.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(w.transform, e.Text, rowH + 10, 0, 250 - rowH - 14, rowH, 13, shown == 0 ? Color.white : Ui.Hex("#D8C8B0"), TextAnchor.MiddleLeft).resizeTextForBestFit = true;
            }
        }

        private CardView ChainItemView(ChainView item)
        {
            var v = item.SourceDefinitionId != null ? ViewOf(item.SourceDefinitionId, item.Controller)
                                                    : new CardView { Name = item.Kind.ToString(), DefinitionId = "?", Text = item.Text, Controller = item.Controller };
            v.Id = item.ObjectId;
            v.Zone = Zone.Chain;
            if (item.Kind != ChainItemKind.Spell && !string.IsNullOrEmpty(item.Text)) v.Text = item.Text;
            return v;
        }

        private void Line(Vector2 from, Vector2 to, Color color)
        {
            var d = to - from;
            var line = Ui.Panel(_dynamic, "TargetLine", 0, 0, d.magnitude, 5f, color);
            var rt = line.rectTransform;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = from;
            rt.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var dot = Ui.Panel(_dynamic, "TargetMark", 0, 0, 22f, 22f, color);
            dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            dot.rectTransform.anchoredPosition = to;
            dot.rectTransform.localEulerAngles = new Vector3(0, 0, 45f);
            Ui.AddOutline(dot.gameObject, new Color(0, 0, 0, 0.8f), 2f);
        }

        private void DrawContext()
        {
            var b = TableControls.Main(_s, _stage);
            // LoR: blank while something animates; lit (colour by verb) when it's yours; grey while the opponent acts.
            bool animating = Busy && !_snap.IsGameOver;
            string label = animating ? "" : b.Label;
            var color = animating || !b.Enabled ? ContextOff : ButtonColorFor(b.Mode);
            int size = b.Mode == ButtonMode.Waiting || label.Length > 10 ? 21 : 24;
            var button = Ui.Button(_dynamic, label, 1650, 456, 160, 112, color, PressContext, size, b.Enabled || animating);
            // Greyed out while the opponent acts (LoR): the label dims too.
            if (!b.Enabled && !animating) foreach (var txt in button.GetComponentsInChildren<Text>()) txt.color = Ui.Hex("#9A9088");
            if (b.Enabled && !animating)
            {
                Ui.AddOutline(button.gameObject, Ui.Hex("#FFE0A0"), 3f);
                Pulse(button.transform, 0.05f);
            }
            Ui.Label(_dynamic, "Round " + _snap.Round, 1636, 572, 190, 22, 16, Ui.Hex("#E0C890"), TextAnchor.MiddleCenter);
            if (!animating && !string.IsNullOrEmpty(b.Hint))
                Ui.Label(_dynamic, b.Hint, 1636, 596, 190, 60, 14, b.Enabled ? Ui.Hex("#FFE8C0") : Ui.Hex("#A89880"), TextAnchor.UpperCenter);
        }

        private static Color ButtonColorFor(ButtonMode mode)
        {
            switch (mode)
            {
                case ButtonMode.Attack:
                case ButtonMode.Block:
                    return Ui.Hex("#C0402A");
                case ButtonMode.EndRound:
                    return Ui.Hex("#2A7AB0");
                case ButtonMode.Continue:
                case ButtonMode.Resolve:
                    return Ui.Hex("#B07A20");
                default:
                    return ContextOn;
            }
        }

        private void DrawTopBar()
        {
            Ui.Button(_dynamic, "Undo", 8, 8, 80, 32, ButtonColor, Undo, 16, _s.CanUndo);
            Ui.Button(_dynamic, "Menu", 94, 8, 110, 32, ButtonColor, OpenMenu, 16);
            Ui.Button(_dynamic, "Debug", 210, 8, 66, 32, _debugOpen ? ContextOn : ButtonColor, ToggleDebug, 14);
            Ui.Button(_dynamic, "Report bug", 290, 8, 120, 32, Ui.Hex("#7A2A20"), OpenBugReport, 15);
            Ui.Label(_dynamic, "Seed " + _seed, 126, 1040, 150, 32, 14, Ui.Hex("#A08060"), TextAnchor.MiddleLeft);
            Ui.Button(_dynamic, "Speed " + _speed + "x", 8, 1040, 110, 32, ButtonColor, () =>
            {
                _speed = _speed >= 4f ? 0.5f : _speed * 2f;
                _dirty = true;
            }, 16);
        }

        private string PickerPromptText()
        {
            var src = _snap.Find(_picker.Source)?.Name ?? "it";
            switch (_picker.Prompt.Dimension)
            {
                case ChoiceDimension.Mode: return "What should " + src + " do?";
                case ChoiceDimension.Sacrifice: return "Choose a creature to sacrifice";
                case ChoiceDimension.Target: return "Choose a target for " + src;
                case ChoiceDimension.Invest: return "Invest?";
                case ChoiceDimension.X: return "Choose X";
                case ChoiceDimension.Division: return "Divide the damage";
                case ChoiceDimension.Defender: return "Choose who to attack";
                default: return "Choose the attacker to block";
            }
        }

        /// <summary>Is this target drawn on the table (so it's chosen by clicking it rather than from a button)?</summary>
        private bool OnTable(Target t) => _widgets.Any(w => w != null
            && (t.IsPlayer ? w.Kind == WidgetKind.TavernDweller && w.Player == t.Player : w.Id == t.Object && w.Kind != WidgetKind.OpponentHandCard));

        private static bool IsTargetLike(ChoiceDimension d) =>
            d == ChoiceDimension.Target || d == ChoiceDimension.Defender || d == ChoiceDimension.BlockedAttacker || d == ChoiceDimension.Sacrifice;

        private void DrawPromptAndChoices()
        {
            bool myCall = _s.HumanToAct;
            string prompt;
            // The big box in the lane is for real choices (modes, X, options); clicking on the table needs only the small ones.
            var buttons = new List<(string label, Action click)>();
            var small = new List<(string label, Action click)>();

            if (myCall && _picker.IsPicking && _picker.Prompt != null)
            {
                prompt = PickerPromptText();
                bool targetLike = IsTargetLike(_picker.Prompt.Dimension);
                foreach (var o in _picker.Prompt.Options)
                {
                    var option = o;
                    var entry = (TableControls.Describe(_s, _picker, option), (Action)(() => { _picker.Choose(option); AfterPick(); }));
                    if (!targetLike) buttons.Add(entry);
                    else if (option.StopTargeting) small.Add(entry);
                    else if (!OnTable(option.Target)) buttons.Add(entry);
                }
                (buttons.Count > 0 ? buttons : small).Add(("Cancel", CancelPicking));
            }
            else if (myCall)
            {
                if (_stage != null && _stage.IsBlocking)
                    prompt = !_selectedBlocker.IsNone ? "Click the attacker to block" : "Drag blockers in front of attackers, then press Block";
                else if (_snap.Decision == DecisionKind.DeclareAttackers)
                    prompt = "Drag units into the lane, then press Attack";
                else if (_snap.Decision != null)
                    prompt = _snap.DecisionPrompt ?? _snap.Decision.ToString();
                else if (_stage != null && _combatCandidates.Count > 0)
                    prompt = "Your action. Drag units into the lane to attack";
                else if (_s.State.Combat != null && (_s.State.Step == Step.DeclareAttackers || _s.State.Step == Step.DeclareBlockers))
                    prompt = TableControls.Main(_s, _stage).Hint; // the response windows in combat (after the attack, after blocks)
                else prompt = "Your action";

                foreach (var c in TableControls.Choices(_s))
                {
                    var action = c.Action;
                    buttons.Add((c.Label, () => Submit(action)));
                }
                foreach (var a in _picker.Legal.Where(a => a.Kind == ActionKind.ChooseTarget && a.Targets.Length > 0 && !OnTable(a.Targets[0])))
                {
                    var action = a;
                    buttons.Add((_s.Text.Describe(_s.State, action), () => Submit(action)));
                }
            }
            else prompt = _s.CombatInProgress ? "Attacking..." : "";

            if (!string.IsNullOrEmpty(prompt))
            {
                var bar = Ui.Panel(_dynamic, "Prompt", CenterX - 320, 826, 640, 32, new Color(0, 0, 0, 0.6f));
                Ui.FillLabel(bar.transform, prompt, 18, Ui.Hex("#FFE8C0"), TextAnchor.MiddleCenter, FontStyle.Bold, 2f);
            }
            for (int i = 0; i < small.Count; i++)
            {
                var (label, click) = small[i];
                Ui.Button(_dynamic, label, CenterX + 330 + i * 158, 826, 150, 32, label == "Cancel" ? Ui.Hex("#5A2A20") : ButtonColor, click, 15);
            }

            if (buttons.Count == 0) return;
            const int perRow = 5;
            const float bw = 240f, bh = 46f, gap = 8f;
            int rows = (buttons.Count + perRow - 1) / perRow;
            float boxW = Math.Min(buttons.Count, perRow) * (bw + gap) + gap, boxH = rows * (bh + gap) + gap;
            var box = Ui.Panel(_dynamic, "Choices", CenterX - boxW / 2f, LaneMid - boxH / 2f, boxW, boxH, new Color(0.1f, 0.06f, 0.03f, 0.92f), raycast: true);
            Ui.AddOutline(box.gameObject, Ui.Hex("#C89A50"), 2f);
            for (int i = 0; i < buttons.Count; i++)
            {
                var (label, click) = buttons[i];
                Ui.Button(box.transform, label, gap + (i % perRow) * (bw + gap), gap + (i / perRow) * (bh + gap), bw, bh,
                    label == "Cancel" ? Ui.Hex("#5A2A20") : ButtonColor, click, 16);
            }
        }

        private void DrawBrowser()
        {
            var p = _snap.Player(_browsingPlayer);
            var cards = _browsing == Zone.Graveyard ? p.Graveyard : p.Exile;
            if (cards.Count == 0) { _browsing = null; return; }
            const int perRow = 8;
            int rows = (cards.Count + perRow - 1) / perRow;
            float w = Math.Min(cards.Count, perRow) * 160f + 20f, h = Math.Min(rows, 3) * 220f + 60f;
            var panel = Ui.Panel(_dynamic, "Browser", CenterX - w / 2f, 540f - h / 2f, w, h, new Color(0.08f, 0.05f, 0.03f, 0.95f), raycast: true);
            Ui.AddOutline(panel.gameObject, Ui.Hex("#C89A50"), 2f);
            Ui.Label(panel.transform, (p.Id == _snap.Viewer ? "Your " : p.Id + "'s ") + _browsing.ToString().ToLowerInvariant(), 10, 6, w - 120, 34, 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Button(panel.transform, "Close", w - 100, 8, 90, 32, ButtonColor, () => { _browsing = null; _dirty = true; }, 16);
            for (int i = 0; i < cards.Count && i < perRow * 3; i++)
                MakeWidget(panel.transform, WidgetKind.HandCard, cards[i], 10 + 80 + (i % perRow) * 160f, 50 + 105 + (i / perRow) * 220f, 150, 210).Kind = WidgetKind.Unit;
        }

        private void DrawHandoff()
        {
            Ui.FillPanel(_overlay, "Cover", Ui.Hex("#1A100A"), 0f, raycast: true);
            Ui.Label(_overlay, "Pass the table", 0, 380, Ui.Width, 80, 54, Ui.Hex("#FFE0A0"), TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Button(_overlay, _s.Viewer + ", take the table", Ui.Width / 2f - 200, 500, 400, 90, ContextOn, PressContext, 30);
        }

        private void DrawGameOver()
        {
            Ui.FillPanel(_overlay, "Dim", new Color(0, 0, 0, 0.6f), 0f, raycast: true);
            string text = _snap.Winners.Count == 0 ? "Draw" :
                _snap.Winners.Contains(_snap.Viewer) && _s.SeatOf(_snap.Viewer) == SeatKind.Human && _seats.Count(k => k == SeatKind.Human) == 1 ? "Victory!" :
                string.Join(", ", _snap.Winners) + " wins";
            Ui.Label(_overlay, text, 0, 360, Ui.Width, 110, 80, Ui.Hex("#FFD060"), TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Button(_overlay, "Rematch", Ui.Width / 2f - 330, 520, 200, 70, ContextOn, () => NewGame(_seed + 1), 26);
            Ui.Button(_overlay, "Main menu", Ui.Width / 2f - 100, 520, 200, 70, ButtonColor, OpenMenu, 24);
            Ui.Button(_overlay, "Undo", Ui.Width / 2f + 130, 520, 200, 70, ButtonColor, Undo, 26, _s.CanUndo);
        }

        private void ShowLoading()
        {
            var panel = Ui.FillPanel(_root, "Loading", Ui.Hex("#1A100A"), 0f, raycast: true);
            _loading = panel.gameObject;
            Ui.Label(panel.transform, "RESTARTED TAVERN", 0, 380, Ui.Width, 120, 96, Ui.Hex("#FFD070"), TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(panel.transform, "Pulling up a chair...", 0, 520, Ui.Width, 50, 28, Ui.Hex("#C8A878"), TextAnchor.MiddleCenter, FontStyle.Italic);
            var bar = Ui.Panel(panel.transform, "Bar", Ui.Width / 2f - 300, 600, 600, 14, new Color(1, 1, 1, 0.1f));
            _loadingBar = Ui.Panel(bar.transform, "Fill", 0, 0, 0, 14, Ui.Hex("#E0A030"));
        }

        private Image _loadingBar;

        private void OpenMenu()
        {
            _picker.Cancel();
            _menuOpen = true;
            _dirty = true;
        }

        /// <summary>The main menu: per seat human / bot, a prototype deck and a Tavern Dweller that can lead it; Battle.</summary>
        private void DrawMenu()
        {
            Ui.FillPanel(_overlay, "Menu", Ui.Hex("#24160C"), 0f, raycast: true);
            Ui.Label(_overlay, "RESTARTED TAVERN", 0, 14, Ui.Width, 80, 56, Ui.Hex("#FFD070"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var decks = CardPool.PrototypeDecks();
            for (int seat = 0; seat < 2; seat++)
            {
                int s = seat;
                _deck[s] = Mathf.Clamp(_deck[s], 0, decks.Count - 1);
                float x = 110 + s * 870;
                var deck = decks[_deck[s]];
                Ui.Panel(_overlay, "Column", x - 20, 100, 840, 840, new Color(0, 0, 0, 0.25f));
                Ui.Label(_overlay, "Player " + (s + 1), x, 110, 400, 50, 34, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Button(_overlay, _seats[s] == SeatKind.Human ? "Human" : "Bot", x + 600, 112, 200, 46,
                    _seats[s] == SeatKind.Human ? Mine : Theirs, () =>
                    {
                        _seats[s] = _seats[s] == SeatKind.Human ? SeatKind.Bot : SeatKind.Human;
                        _dirty = true;
                    }, 22);

                Ui.Label(_overlay, "Deck", x, 170, 400, 30, 20, Ui.Hex("#E0C890"), TextAnchor.MiddleLeft, FontStyle.Bold);
                for (int d = 0; d < decks.Count; d++)
                {
                    int di = d;
                    Ui.Button(_overlay, decks[d].Name, x + (d % 2) * 405, 205 + (d / 2) * 58, 395, 50,
                        d == _deck[s] ? ContextOn : ButtonColor, () =>
                        {
                            _deck[s] = di;
                            _dweller[s] = null;
                            _dirty = true;
                        }, 20);
                }
                float y = 205 + (decks.Count + 1) / 2 * 58 + 6;
                Ui.Label(_overlay, deck.Description ?? "", x, y, 800, 56, 16, Ui.Hex("#D8C8B0"), TextAnchor.UpperLeft);

                y += 70;
                Ui.Label(_overlay, "Tavern Dweller", x, y, 400, 30, 20, Ui.Hex("#E0C890"), TextAnchor.MiddleLeft, FontStyle.Bold);
                y += 35;
                var dwellers = MatchSetup.TavernDwellersFor(deck, _db);
                string chosen = _dweller[s] ?? deck.TavernDweller;
                for (int i = 0; i < dwellers.Count; i++)
                {
                    string id = dwellers[i];
                    Ui.Button(_overlay, _db.Get(id).Name + (id == deck.TavernDweller ? " (deck's own)" : ""),
                        x + (i % 2) * 405, y + (i / 2) * 58, 395, 50, id == chosen ? ContextOn : ButtonColor, () =>
                        {
                            _dweller[s] = id;
                            _dirty = true;
                        }, 20);
                }
                y += (dwellers.Count + 1) / 2 * 58 + 8;
                var def = _db.Get(chosen);
                var box = Ui.Panel(_overlay, "Dweller", x, y, 800, 150, new Color(0.95f, 0.9f, 0.8f, 0.92f));
                Ui.FillLabel(box.transform, def.Name + "\n" + def.Text, 17, new Color(0.15f, 0.1f, 0.05f), TextAnchor.UpperLeft, FontStyle.Normal, 8f);
            }
            var battle = Ui.Button(_overlay, "BATTLE", Ui.Width / 2f - 170, 958, 340, 92, ContextOn, () =>
            {
                // A deck from the editor may not be legal yet (60 cards, copies, factions).
                for (int s = 0; s < 2; s++)
                {
                    var d = decks[_deck[s]];
                    try { DeckValidator.Validate(_db, FormatConfig.Standard(), d.Cards, _dweller[s] ?? d.TavernDweller); }
                    catch (ArgumentException e) { ShowToast("Player " + (s + 1) + "'s deck " + d.Name + ": " + e.Message); return; }
                }
                _battleStarted = true;
                NewGame(_seed + 1);
            }, 44);
            Ui.AddOutline(battle.gameObject, Ui.Hex("#FFE0A0"), 4f);
            if (_battleStarted && !_s.State.IsGameOver)
                Ui.Button(_overlay, "Back to the game", Ui.Width / 2f + 200, 978, 260, 54, ButtonColor, () =>
                {
                    _menuOpen = false;
                    _dirty = true;
                }, 20);
            Ui.Button(_overlay, "Quit", Ui.Width / 2f - 460, 978, 260, 54, Ui.Hex("#5A2A20"), Application.Quit, 20);
            Ui.Button(_overlay, "Deck editor", Ui.Width / 2f - 740, 978, 260, 54, Mine, () => OpenEditor(decks[_deck[0]]), 20);
        }

        // ------------------------------------------------------------------ zoom

        private void ShowZoom(CardWidget w)
        {
            Ui.Clear(_zoomLayer);
            if (w == null || w.View == null || w.View.IsHidden || w.Kind == WidgetKind.HandCard) return;
            var corners = new Vector3[4];
            w.Rect.GetWorldCorners(corners);
            var center = _root.InverseTransformPoint((corners[0] + corners[2]) / 2f);
            float cx = center.x + Ui.Width / 2f, cy = Ui.Height / 2f - center.y;
            const float zw = 300f, zh = 420f;
            float halfW = Mathf.Abs(corners[2].x - corners[0].x) / 2f / _root.lossyScale.x; // wider when tapped
            bool right = cx < Ui.Width / 2f;
            // A host with its gear fanned out (towards the middle): the zoom goes to the other side, not over the gear
            // (playtest 2026-10-10_140818).
            if (w.Id == _fanHost && _tucked.ContainsKey(w.Id)) right = !right;
            float x = Mathf.Clamp(right ? cx + halfW + 28f : cx - halfW - 28f - zw, 10f, Ui.Width - zw - 10f);
            float y = Mathf.Clamp(cy - zh / 2f, 10f, Ui.Height - zh - 10f);
            var rt = Ui.Rect(_zoomLayer, "Zoom", x, y, zw, zh);
            CardFaces.Build(rt, w.View, FaceStyle.Zoom);
            foreach (var g in rt.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            if (w.View.Damage > 0 || w.View.Power != w.View.PrintedPower || w.View.MaxHealth != w.View.PrintedHealth)
            {
                var note = Ui.Panel(_zoomLayer, "Stats", x, y + zh + 4, zw, 26, new Color(0, 0, 0, 0.75f));
                Ui.FillLabel(note.transform, "Printed " + w.View.PrintedPower + "/" + w.View.PrintedHealth + " · now " + w.View.Power + "/" + w.View.MaxHealth
                    + (w.View.Damage > 0 ? " · " + w.View.Damage + " damage" : ""), 14, Color.white);
            }
        }

        // ------------------------------------------------------------------ input from widgets

        public void OnHover(CardWidget w, bool enter)
        {
            if (_dragging != null) return;
            if (w.Kind == WidgetKind.HandCard && w.View != null && w.View.Zone == Zone.Hand)
            {
                PrimeTween.Tween.StopAll(w.Rect);
                if (enter) PreviewSpend(w.View);
                else ClearSpendPreview();
                // MTGA hand: the card lifts, straightens and grows.
                w.Rect.anchoredPosition = enter ? new Vector2(w.HomePosition.x, -(HandTop + HandH / 2f) + 60f) : w.HomePosition;
                w.Rect.localEulerAngles = new Vector3(0, 0, enter ? 0f : w.HomeRotation);
                w.Rect.localScale = Vector3.one * (enter ? 1.35f : 1f);
                if (enter) w.transform.SetAsLastSibling();
                else w.transform.SetSiblingIndex(w.HomeSibling);
                return;
            }
            if (enter) FanOnHover(w);
            if (!_pinnedZoom.IsNone) return;
            if (enter) { _hovered = w; _hoverRect = null; ShowZoom(w); }
            else if (_hovered == w) { _hovered = null; Ui.Clear(_zoomLayer); }
        }

        public void OnRightClick(CardWidget w)
        {
            // A right-click that cancels targeting (here or in Update, same frame) must not also pin the card under the mouse.
            if (_picker.IsPicking || _cancelFrame == Time.frameCount) { CancelPicking(); return; }
            _pinnedZoom = _pinnedZoom == w.Id ? ObjectId.None : w.Id;
            if (_pinnedZoom.IsNone) Ui.Clear(_zoomLayer);
            else ShowZoom(w);
        }

        private static IEnumerable<Target> TargetsOf(CardWidget w)
        {
            if (w.Kind == WidgetKind.TavernDweller) yield return Target.ForPlayer(w.Player);
            if (!w.Id.IsNone) yield return Target.ForObject(w.Id);
        }

        private bool IsMine(CardWidget w) => w.View != null && w.View.Controller == _snap.Viewer;

        private bool IsStaged(ObjectId id) => _stage != null && _stage.Staged.Any(s => s.Creature == id);

        public void OnClick(CardWidget w)
        {
            if (_dragging != null || !_s.HumanToAct) return;
            if (Busy) { SkipBeats(); return; }

            if (_picker.IsPicking)
            {
                foreach (var t in TargetsOf(w))
                    if (_picker.ChooseTarget(t)) { AfterPick(); return; }
                if (w.Id == _picker.Source) CancelPicking();
                return;
            }

            foreach (var t in TargetsOf(w))
            {
                var trigger = _picker.ChooseTargetAction(t);
                if (trigger != null) { Submit(trigger); return; }
            }

            if (_stage != null && TryCombatClick(w, declaringOnly: true)) return;

            if (!w.Id.IsNone && _picker.Begin(w.Id)) { AfterPick(); return; }

            if (_stage != null) TryCombatClick(w, declaringOnly: false);
        }

        /// <summary>Clicking in combat: stage / unstage an attacker, pick a blocker then the attacker it blocks.</summary>
        private bool TryCombatClick(CardWidget w, bool declaringOnly)
        {
            if (declaringOnly && !Declaring && !IsStaged(w.Id)) return false;
            var id = w.Id;
            if (IsMine(w) && IsStaged(id))
            {
                _stage.Unstage(id);
                _dirty = true;
                return true;
            }
            if (_stage.IsBlocking)
            {
                if (IsMine(w) && _combatCandidates.Contains(id))
                {
                    _selectedBlocker = _selectedBlocker == id ? ObjectId.None : id;
                    var blockable = _stage.BlockableBy(id);
                    if (blockable.Count == 1) { _stage.StageBlocker(id, blockable[0]); _selectedBlocker = ObjectId.None; }
                    _dirty = true;
                    return true;
                }
                if (!_selectedBlocker.IsNone && !IsMine(w))
                {
                    _stage.StageBlocker(_selectedBlocker, id);
                    _selectedBlocker = ObjectId.None;
                    _dirty = true;
                    return true;
                }
                return false;
            }
            if (IsMine(w) && _combatCandidates.Contains(id))
            {
                _stage.StageAttacker(id);
                _dirty = true;
                return true;
            }
            return false;
        }

        private void AfterPick()
        {
            if (_picker.Ready != null) Submit(_picker.Ready);
            else _dirty = true;
        }

        // ------------------------------------------------------------------ drag and drop

        public void OnBeginDrag(CardWidget w, PointerEventData e)
        {
            if (!_s.HumanToAct || _picker.IsPicking || Busy) return;
            bool fromHand = w.Kind == WidgetKind.HandCard && w.View?.Zone == Zone.Hand && _sources.Contains(w.Id);
            bool combat = w.Kind == WidgetKind.Unit && IsMine(w) && _stage != null && (_combatCandidates.Contains(w.Id) || IsStaged(w.Id));
            if (!fromHand && !combat) return;
            _dragging = w;
            Ui.Clear(_zoomLayer);
            w.Rect.localScale = Vector3.one;
            w.Rect.localEulerAngles = Vector3.zero;
            w.transform.SetParent(_dragLayer, true);
            w.SetRaycasts(false);
            OnDrag(w, e);
        }

        public void OnDrag(CardWidget w, PointerEventData e)
        {
            if (_dragging != w) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragLayer, e.position, CanvasCamera, out var local))
                w.Rect.localPosition = local;
        }

        public void OnEndDrag(CardWidget w, PointerEventData e)
        {
            if (_dragging != w) return;
            _dragging = null;
            _dirty = true;
            RememberDrop(w);
            Destroy(w.gameObject); // it lives on the drag layer now; the refresh draws the card again where it belongs
            float y = Ui.Height / 2f - w.Rect.localPosition.y; // top-based reference y of the drop
            var drop = e.pointerCurrentRaycast.gameObject != null ? e.pointerCurrentRaycast.gameObject.GetComponentInParent<CardWidget>() : null;

            if (w.Kind == WidgetKind.HandCard)
            {
                // Drop above the hand to play; drop onto a target to choose it right away.
                if (y > HandTop - 20f || !_picker.Begin(w.Id)) return;
                if (_picker.Prompt != null && drop != null)
                    foreach (var t in TargetsOf(drop))
                        if (_picker.ChooseTarget(t)) break;
                AfterPick();
                return;
            }

            bool inLane = y > LaneTop - 20f && y < LaneBottom + 20f;
            if (_stage.IsBlocking)
            {
                var attacker = drop != null && !IsMine(drop) ? drop.Id : ObjectId.None;
                if (attacker.IsNone && inLane)
                {
                    // Dropped in the lane but not on an attacker: block the only one it can, or the nearest.
                    var blockable = _stage.BlockableBy(w.Id);
                    if (IsStaged(w.Id)) { _stage.Unstage(w.Id); blockable = _stage.BlockableBy(w.Id); }
                    attacker = Nearest(blockable, w.Rect.localPosition.x);
                }
                if (IsStaged(w.Id)) _stage.Unstage(w.Id);
                if (!attacker.IsNone) _stage.StageBlocker(w.Id, attacker);
                return;
            }
            if (inLane && !IsStaged(w.Id)) _stage.StageAttacker(w.Id);
            else if (!inLane && IsStaged(w.Id)) _stage.Unstage(w.Id);
        }

        private ObjectId Nearest(List<ObjectId> ids, float localX)
        {
            var best = ObjectId.None;
            float bestDist = float.MaxValue;
            foreach (var id in ids)
            {
                var widget = _widgets.FirstOrDefault(x => x != null && x.Id == id);
                if (widget == null) continue;
                float d = Mathf.Abs(_dragLayer.InverseTransformPoint(widget.transform.position).x - localX);
                if (d < bestDist) { bestDist = d; best = id; }
            }
            return best;
        }

        private Camera CanvasCamera => _camera;

        private static readonly Color ArrowAim = Ui.Hex("#FF6A3A");
        private static readonly Color ArrowLocked = Ui.Hex("#FFD040");

        /// <summary>
        /// MTG Arena targeting arrow while a target is being chosen (a spell or ability you're casting, or your trigger
        /// picking its target, playtest 2026-10-10_141453): it bends up from the source to the pointer and snaps onto a
        /// legal target under it (gold). Once chosen, the Chain bubble's target lines show the pick.
        /// </summary>
        private void UpdateArrow()
        {
            CardWidget source = null;
            if (_picker != null && _picker.IsPicking && _picker.Prompt != null && _picker.Prompt.Targets.Any())
                source = SourceWidget(_picker.Source);
            else if (_snap != null && _s.HumanToAct && _snap.Decision == DecisionKind.ChooseTriggerTarget && _targets.Count > 0)
                source = SourceWidget(_snap.DecisionSource);
            if (source == null || _dragging != null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragLayer, Input.mousePosition, CanvasCamera, out var to))
            {
                foreach (var seg in _arrowBody) seg.gameObject.SetActive(false);
                _arrowHead.gameObject.SetActive(false);
                return;
            }
            Vector2 from = _dragLayer.InverseTransformPoint(source.transform.position);
            var locked = _widgets.FirstOrDefault(x => x != null && x != source && TargetsOf(x).Any(_targets.Contains)
                && RectTransformUtility.RectangleContainsScreenPoint(x.Rect, Input.mousePosition, CanvasCamera));
            if (locked != null) to = _dragLayer.InverseTransformPoint(locked.transform.position);
            var color = locked != null ? ArrowLocked : ArrowAim;

            // A quadratic curve bowed upwards, ending short so the head sits on the end point.
            var d = to - from;
            float len = d.magnitude;
            var normal = len > 1f ? new Vector2(-d.y, d.x) / len : Vector2.up;
            if (normal.y < 0f) normal = -normal;
            var ctrl = (from + to) / 2f + normal * Mathf.Min(160f, len * 0.25f);
            Vector2 Point(float t) => (1 - t) * (1 - t) * from + 2 * (1 - t) * t * ctrl + t * t * to;
            const float headLen = 30f;
            float tEnd = len > headLen ? 1f - headLen / len : 0.01f;
            for (int i = 0; i < ArrowSegments; i++)
            {
                float t0 = tEnd * i / ArrowSegments, t1 = tEnd * (i + 0.8f) / ArrowSegments; // small gaps: a segmented body
                Vector2 a = Point(t0), b = Point(t1), seg = b - a;
                var img = _arrowBody[i];
                img.gameObject.SetActive(len > 20f);
                img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.35f, 0.95f, (float)i / ArrowSegments));
                img.rectTransform.anchoredPosition = (a + b) / 2f;
                img.rectTransform.sizeDelta = new Vector2(seg.magnitude + 1f, Mathf.Lerp(7f, 16f, (float)i / ArrowSegments));
                img.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(seg.y, seg.x) * Mathf.Rad2Deg);
                img.transform.SetAsLastSibling();
            }
            var tip = to - Point(tEnd);
            _arrowHead.gameObject.SetActive(true);
            _arrowHead.color = color;
            _arrowHead.rectTransform.anchoredPosition = Point(tEnd);
            _arrowHead.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(tip.y, tip.x) * Mathf.Rad2Deg);
            _arrowHead.transform.SetAsLastSibling();
        }

        /// <summary>The widget a targeting arrow starts from: the card on the table, its Chain bubble or hand card, or a Tavern Dweller.</summary>
        private CardWidget SourceWidget(ObjectId id)
        {
            if (id.IsNone) return null;
            return _widgets.FirstOrDefault(x => x != null && x.Id == id && x.Kind != WidgetKind.History);
        }
    }
}
