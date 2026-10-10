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
    /// The table is rebuilt from a fresh snapshot after every change (animations come later, from the
    /// PresentationQueue).
    ///
    /// Command line: -seed N, -deck1/-deck2 N, -bot1, -human2 (hot-seat), -autoplay N (the bot plays N actions for
    /// everyone at startup), -autopick (start picking the first usable card, to show targeting), -autoshot path.png
    /// (take a screenshot, then quit), -until attack|block (with -autoplay: stop early when the first seat can attack /
    /// must block, and stage every possible attacker or blocker, to show the combat lane).
    /// </summary>
    public sealed class TableView : MonoBehaviour
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
        private static readonly Color ButtonColor = Ui.Hex("#7A4E22");
        private static readonly Color ContextOn = Ui.Hex("#C08A2A");
        private static readonly Color ContextOff = Ui.Hex("#4A4038");
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
        private readonly SeatKind[] _seats = { SeatKind.Human, SeatKind.Bot };
        private ulong _seed = 1;
        private int _autoplay;
        private bool _autopick;
        private string _autoshot;
        private string _until;
        private int _shotFrame = -1;

        private Camera _camera;
        private RectTransform _root, _dynamic, _dragLayer, _zoomLayer, _overlay;
        private Image _arrow, _arrowHead;
        private readonly List<CardWidget> _widgets = new List<CardWidget>();
        private CardWidget _dragging;
        private ObjectId _selectedBlocker = ObjectId.None;
        private ObjectId _pinnedZoom = ObjectId.None;
        private Zone? _browsing;
        private PlayerId _browsingPlayer;

        // Highlights for the current refresh.
        private HashSet<ObjectId> _sources = new HashSet<ObjectId>();
        private HashSet<Target> _targets = new HashSet<Target>();
        private HashSet<ObjectId> _combatCandidates = new HashSet<ObjectId>();

        private bool Declaring => _s.State.Pending?.Kind == DecisionKind.DeclareAttackers || _s.State.Pending?.Kind == DecisionKind.DeclareBlockers;

        // ------------------------------------------------------------------ setup

        private void Start()
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
                    case "-until": _until = next; break;
                }
            }
            if (_seed == 0) _seed = 1;
            BuildCanvas();
            NewGame(_seed);

            if (_autoplay > 0)
            {
                for (int i = 0; i < _autoplay && _s.WaitingOn != null; i++)
                {
                    if (_until != null && _s.WaitingOn == _s.State.Players[0].Id && !_s.HandoffPending
                        && (_until == "block" ? CombatStage.CanBlock(_s) : CombatStage.ForAttack(_s)?.Candidates().Count > 0)) break;
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
            if (_autoshot != null) _shotFrame = 0;
        }

        private void NewGame(ulong seed)
        {
            _seed = seed;
            var decks = CardPool.PrototypeDecks();
            _deck[0] = Mathf.Clamp(_deck[0], 0, decks.Count - 1);
            _deck[1] = Mathf.Clamp(_deck[1], 0, decks.Count - 1);
            _s = new MatchSession(MatchSetup.Duel(_deck[0], _deck[1], _seats[0], _seats[1], seed));
            _pinnedZoom = ObjectId.None;
            _browsing = null;
            OnSessionChanged();
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
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            _root = Ui.Fill(canvasGo.transform, "Table");
            BuildBackground(_root);
            _dynamic = Ui.Fill(_root, "Dynamic");
            _zoomLayer = Ui.Fill(_root, "Zoom");
            _overlay = Ui.Fill(_root, "Overlay");
            _dragLayer = Ui.Fill(_root, "Drag");

            _arrow = Ui.Panel(_dragLayer, "Arrow", 0, 0, 10, 10, new Color(1f, 0.35f, 0.3f, 0.85f));
            _arrow.rectTransform.anchorMin = _arrow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _arrow.rectTransform.pivot = new Vector2(0f, 0.5f);
            _arrowHead = Ui.Panel(_dragLayer, "ArrowHead", 0, 0, 26, 26, new Color(1f, 0.35f, 0.3f, 0.95f));
            _arrowHead.rectTransform.anchorMin = _arrowHead.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _arrowHead.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _arrow.gameObject.SetActive(false);
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

        private void Submit(PlayerAction action) => Run(() => _s.Submit(action));

        private void PressContext()
        {
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
            Run(_s.Undo);
        }

        private void Update()
        {
            if (_s == null) return;

            if (_shotFrame >= 0)
            {
                if (_dirty) Refresh();
                _shotFrame++;
                if (_shotFrame == 5) ScreenCapture.CaptureScreenshot(_autoshot);
                if (_shotFrame == 12) Application.Quit();
                return;
            }

            // Keyboard shortcuts act on the table, never on the last clicked button.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (_dragging == null)
            {
                if (Input.GetKeyDown(KeyCode.Space)) PressContext();
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1) && _picker.IsPicking) CancelPicking();
                if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) Undo();
            }

            if (_s.BotToAct && _dragging == null && Time.unscaledTime >= _nextBot)
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
            DrawContext();
            DrawPromptAndChoices();
            DrawTopBar();
            if (_browsing != null) DrawBrowser();
            if (!_pinnedZoom.IsNone) ShowZoom(_widgets.FirstOrDefault(w => w.Id == _pinnedZoom));

            if (_snap.IsGameOver) DrawGameOver();
            else if (_s.HandoffPending) DrawHandoff();
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
            if (v == null || v.IsHidden) CardFaces.BuildBack(rt);
            else CardFaces.Build(rt, v, kind == WidgetKind.HandCard ? FaceStyle.Hand : FaceStyle.Unit);
            _widgets.Add(widget);
            ApplyGlow(widget);
            return widget;
        }

        private void ApplyGlow(CardWidget w)
        {
            var id = w.Id;
            bool target = (!id.IsNone && _targets.Contains(Target.ForObject(id)))
                          || (w.Kind == WidgetKind.TavernDweller && _targets.Contains(Target.ForPlayer(w.Player)));
            if (!id.IsNone && id == _picker.Source || !_selectedBlocker.IsNone && id == _selectedBlocker) w.SetGlow(GlowSelected);
            else if (target) w.SetGlow(GlowTarget);
            else if (_combatCandidates.Contains(id)) w.SetGlow(GlowCombat);
            else if (_sources.Contains(id) && w.Kind != WidgetKind.TavernDweller) w.SetGlow(GlowSource);
            else w.SetGlow(null);
        }

        private void DrawDweller(PlayerView p, bool top)
        {
            float y = top ? 108f : 640f;
            var name = p.TavernDweller?.Name ?? p.Id.ToString();
            Ui.Label(_dynamic, (p.Id == _snap.Viewer ? "You · " : "") + name, 14, top ? y - 34 : y + 262, 252, 30, 20,
                Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var w = MakeWidget(_dynamic, WidgetKind.TavernDweller, p.TavernDweller, 125, y + 115, 210, 230);
            w.Player = p.Id;

            // Life on the portrait, like a Nexus.
            var life = Ui.Panel(w.transform, "Life", 60, 64, 90, 60, p.Life <= 5 ? Ui.Hex("#A01818") : Ui.Hex("#1E1410"));
            Ui.AddOutline(life.gameObject, new Color(1f, 0.8f, 0.4f, 0.8f), 2f);
            Ui.FillLabel(life.transform, p.Life.ToString(), 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (p.HasLost) Ui.FillPanel(w.transform, "Lost", new Color(0, 0, 0, 0.6f));

            // The Power as a button beside the portrait (lit when usable).
            bool usable = p.TavernDweller != null && _sources.Contains(p.TavernDweller.Id);
            var power = Ui.Button(_dynamic, "Power", 236, y + 92, 40, 46, usable ? Ui.Hex("#E0A020") : Ui.Hex("#3A3028"),
                () => { if (p.TavernDweller != null && _picker.Begin(p.TavernDweller.Id)) AfterPick(); }, 12, usable);
            if (usable) Ui.AddOutline(power.gameObject, GlowSource, 4f);
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
            Ui.Label(_dynamic, p.Mana + "/" + p.MaxMana, 1846, manaTop - 30 + (top ? 0 : 0), 70, 26, 18, Ui.Hex("#A0D0FF"), TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int i = 0; i < 10; i++)
            {
                float gy = top ? manaTop + i * 25f : manaTop + (9 - i) * 25f;
                Color c = i < p.Mana ? Ui.Hex("#3AA0FF") : i < p.MaxMana ? Ui.Hex("#1A3048") : new Color(1, 1, 1, 0.06f);
                var gem = Ui.Panel(_dynamic, "Mana", 1872, gy, 22, 22, c);
                if (i < p.MaxMana) Ui.AddOutline(gem.gameObject, Ui.Hex("#80C0FF"), 1.5f);
            }
            float goldY = top ? manaTop + 258f : manaTop - 58f;
            for (int i = 0; i < Math.Max(3, p.GoldCap); i++)
            {
                Color c = i < p.Gold ? Ui.Hex("#F0C030") : i < p.GoldCap ? Ui.Hex("#4A3A18") : new Color(1, 1, 1, 0.06f);
                var gem = Ui.Panel(_dynamic, "Gold", 1818 + i * 30, goldY, 20, 20, c);
                gem.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
                if (i < p.GoldCap) Ui.AddOutline(gem.gameObject, Ui.Hex("#FFE090"), 1.5f);
            }

            // The attack token next to the leader's gems (dim once used this round).
            if (p.HasAttackToken)
            {
                float ty = top ? manaTop : manaTop + 180f;
                var token = Ui.Panel(_dynamic, "AttackToken", 1816, ty, 48, 48, _snap.AttackUsed ? Ui.Hex("#4A3A30") : Ui.Hex("#D04A20"));
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

            DrawRow(opp.Battlefield.Where(c => !inLane.Contains(c.Id)).ToList(), OppRowY + UnitH / 2f);
            DrawRow(me.Battlefield.Where(c => !inLane.Contains(c.Id)).ToList(), MyRowY + UnitH / 2f);

            // Attackers in a row across the lane; each blocker stands in front of its attacker.
            float step = Math.Min(UnitW + 16f, (CenterRight - CenterLeft - 40f) / Math.Max(1, laneAttackers.Count));
            float x0 = CenterX - step * (laneAttackers.Count - 1) / 2f;
            var attackerX = new Dictionary<ObjectId, float>();
            for (int i = 0; i < laneAttackers.Count; i++)
            {
                var c = laneAttackers[i];
                float x = x0 + i * step;
                attackerX[c.Id] = x;
                bool mine = c.Controller == _snap.Viewer;
                var w = MakeWidget(_dynamic, WidgetKind.Unit, c, x, mine ? LaneMid + 6 + UnitH / 2f : LaneMid - 6 - UnitH / 2f, UnitW, UnitH);
                if (_stage != null && _stage.Staged.Any(st => st.Creature == c.Id)) Tag(w, "staged");
            }
            var perAttacker = new Dictionary<ObjectId, int>();
            foreach (var (unit, blocks) in laneBlockers)
            {
                perAttacker.TryGetValue(blocks, out int n);
                perAttacker[blocks] = n + 1;
                float x = (attackerX.TryGetValue(blocks, out var ax) ? ax : CenterX) + n * 34f;
                bool mine = unit.Controller == _snap.Viewer;
                MakeWidget(_dynamic, WidgetKind.Unit, unit, x, mine ? LaneMid + 6 + UnitH / 2f : LaneMid - 6 - UnitH / 2f, UnitW, UnitH);
            }
        }

        private void Tag(CardWidget w, string text)
        {
            var tag = Ui.Panel(w.transform, "Tag", 10, -20, UnitW - 20, 18, new Color(0, 0, 0, 0.7f));
            Ui.FillLabel(tag.transform, text, 12, GlowCombat, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void DrawRow(List<CardView> cards, float cy)
        {
            // Creatures first (LoR units), then Relics, Equipment and Curses.
            var ordered = cards.Where(c => c.Type == CardType.Creature).Concat(cards.Where(c => c.Type != CardType.Creature)).ToList();
            if (ordered.Count == 0) return;
            float step = Math.Min(UnitW + 12f, (CenterRight - CenterLeft - UnitW) / Math.Max(1, ordered.Count - 1));
            float x0 = CenterX - step * (ordered.Count - 1) / 2f;
            for (int i = 0; i < ordered.Count; i++)
            {
                var c = ordered[i];
                bool creature = c.Type == CardType.Creature;
                var w = MakeWidget(_dynamic, creature ? WidgetKind.Unit : WidgetKind.HandCard, c, x0 + i * step, cy,
                    creature ? UnitW : UnitW * 0.95f, creature ? UnitH : UnitW * 0.95f * 1.4f);
                w.Kind = WidgetKind.Unit;
                if (!c.AttachedTo.IsNone || c.AttachedToPlayer != null)
                {
                    string host = c.AttachedToPlayer != null ? c.AttachedToPlayer.ToString() : _snap.Find(c.AttachedTo)?.Name ?? "?";
                    var tag = Ui.Panel(w.transform, "Attached", 4, UnitH - 4, UnitW - 8, 18, new Color(0, 0, 0, 0.75f));
                    Ui.FillLabel(tag.transform, "on " + host, 12, Color.white);
                }
            }
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
            int n = opp.Hand.Count;
            float step = Math.Min(70f, 700f / Math.Max(1, n));
            float x0 = CenterX - step * (n - 1) / 2f;
            for (int i = 0; i < n; i++)
            {
                float off = i - (n - 1) / 2f;
                MakeWidget(_dynamic, WidgetKind.OpponentHandCard, opp.Hand[i], x0 + i * step, 22f - off * off * 1.5f, 84f, 118f, 180f + off * 3f);
            }
            Ui.Label(_dynamic, "Hand " + n, CenterX + 380, 8, 120, 26, 16, Ui.Hex("#E0C890"), TextAnchor.MiddleLeft);
        }

        private void DrawChain()
        {
            if (_snap.Chain.Count == 0) return;
            Ui.Label(_dynamic, "CHAIN", 1640, 112, 180, 26, 18, Ui.Hex("#FFD080"), TextAnchor.MiddleCenter, FontStyle.Bold);
            float y = 140f;
            // Top of the Chain first and largest (MTGA stack).
            for (int i = _snap.Chain.Count - 1; i >= 0 && y < 440f; i--)
            {
                var item = _snap.Chain[i];
                bool topItem = i == _snap.Chain.Count - 1;
                float h = topItem ? 96f : 60f;
                var panel = Ui.Panel(_dynamic, "ChainItem", 1640, y, 170, h, item.Controller == _snap.Viewer ? Mine : Theirs);
                Ui.AddOutline(panel.gameObject, topItem ? Ui.Hex("#FFD080") : new Color(0, 0, 0, 0.6f), 2f);
                string source = item.SourceDefinitionId != null ? _s.Text.Name(item.SourceDefinitionId) : item.Kind.ToString();
                Ui.Label(panel.transform, source, 6, 2, 158, 22, 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(panel.transform, item.Text ?? "", 6, 24, 158, h - 26, 12, Ui.Hex("#F0E8D8"), TextAnchor.UpperLeft);
                y += h + 6f;
            }
        }

        private void DrawContext()
        {
            var b = TableControls.Main(_s, _stage);
            var button = Ui.Button(_dynamic, b.Label, 1650, 456, 160, 112, b.Enabled ? ContextOn : ContextOff, PressContext, 24, b.Enabled);
            if (b.Enabled) Ui.AddOutline(button.gameObject, Ui.Hex("#FFE0A0"), 3f);
            string whose = _snap.IsGameOver ? "" : _s.HumanToAct ? "Your action" : _s.HandoffPending ? "" : "Opponent's action";
            Ui.Label(_dynamic, "Round " + _snap.Round + (whose.Length > 0 ? " · " + whose : ""), 1636, 572, 190, 24, 16,
                Ui.Hex("#E0C890"), TextAnchor.MiddleCenter);
        }

        private void DrawTopBar()
        {
            Ui.Button(_dynamic, "Undo", 8, 8, 80, 32, ButtonColor, Undo, 16, _s.CanUndo);
            Ui.Button(_dynamic, "New game", 94, 8, 110, 32, ButtonColor, () => NewGame(_seed + 1), 16);
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
                else if (_snap.Decision != null)
                    prompt = _snap.DecisionPrompt ?? _snap.Decision.ToString();
                else if (_stage != null && _combatCandidates.Count > 0)
                    prompt = "Your action. Drag units into the lane to attack";
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
            Ui.Button(_overlay, "Rematch", Ui.Width / 2f - 220, 520, 200, 70, ContextOn, () => NewGame(_seed + 1), 26);
            Ui.Button(_overlay, "Undo", Ui.Width / 2f + 20, 520, 200, 70, ButtonColor, Undo, 26, _s.CanUndo);
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
            float x = cx < Ui.Width / 2f ? cx + 90f : cx - 90f - zw;
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
                // MTGA hand: the card lifts, straightens and grows.
                w.Rect.anchoredPosition = enter ? new Vector2(w.HomePosition.x, -(HandTop + HandH / 2f) + 60f) : w.HomePosition;
                w.Rect.localEulerAngles = new Vector3(0, 0, enter ? 0f : w.HomeRotation);
                w.Rect.localScale = Vector3.one * (enter ? 1.35f : 1f);
                if (enter) w.transform.SetAsLastSibling();
                else w.transform.SetSiblingIndex(w.HomeSibling);
                return;
            }
            if (!_pinnedZoom.IsNone) return;
            if (enter) ShowZoom(w);
            else Ui.Clear(_zoomLayer);
        }

        public void OnRightClick(CardWidget w)
        {
            if (_picker.IsPicking) { CancelPicking(); return; }
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
            if (!_s.HumanToAct || _picker.IsPicking) return;
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

        /// <summary>LoR / MTGA targeting arrow from the source to the pointer while a target is being chosen.</summary>
        private void UpdateArrow()
        {
            bool show = _picker != null && _picker.IsPicking && _picker.Prompt != null && _picker.Prompt.Targets.Any();
            var source = show ? _widgets.FirstOrDefault(x => x != null && x.Id == _picker.Source) : null;
            if (source == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragLayer, Input.mousePosition, CanvasCamera, out var to))
            {
                _arrow.gameObject.SetActive(false);
                _arrowHead.gameObject.SetActive(false);
                return;
            }
            Vector2 from = _dragLayer.InverseTransformPoint(source.transform.position);
            var d = to - from;
            _arrow.gameObject.SetActive(true);
            _arrowHead.gameObject.SetActive(true);
            _arrow.rectTransform.anchoredPosition = from;
            _arrow.rectTransform.sizeDelta = new Vector2(d.magnitude, 10f);
            _arrow.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            _arrowHead.rectTransform.anchoredPosition = to;
            _arrowHead.rectTransform.localEulerAngles = new Vector3(0, 0, 45f);
        }
    }
}
