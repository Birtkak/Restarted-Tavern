using System;
using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using UnityEngine;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// The battlefield rows, MTG Arena style (docs/handoff/NEXT_ARENA.md):
    /// - tapped units lie sideways (attackers too, once declared)
    /// - Equipment and Curses on a creature are tucked behind it, stacked up and to the right so a corner peeks out; hovering
    ///   the host fans them out beside it (hovering a fanned one zooms it)
    /// - each side shrinks as it fills up, down to two half-size lines before cards overlap.
    /// </summary>
    public sealed partial class TableView
    {
        private const float TappedAngle = -90f;      // a quarter turn clockwise
        private const float TuckStepX = 10f, TuckStepY = 12f; // how far each tucked attachment peeks out (right, up)
        private const float OneLineMinScale = 0.5f;  // below this, the side splits into two lines
        private const float TwoLineScale = 0.5f;
        private const float RowGap = 12f;

        /// <summary>Host creature -> what's attached to it (Equipment, Curses), from this refresh's snapshot.</summary>
        private readonly Dictionary<ObjectId, List<CardView>> _attachments = new Dictionary<ObjectId, List<CardView>>();
        /// <summary>Host creature -> the widgets tucked behind it; attachment -> its host.</summary>
        private readonly Dictionary<ObjectId, List<CardWidget>> _tucked = new Dictionary<ObjectId, List<CardWidget>>();
        private readonly Dictionary<ObjectId, ObjectId> _hostOf = new Dictionary<ObjectId, ObjectId>();
        private ObjectId _fanHost = ObjectId.None;

        /// <summary>Find what's attached to which creature; those cards are drawn with their host, not in the row.</summary>
        private HashSet<ObjectId> CollectAttachments()
        {
            _attachments.Clear();
            _tucked.Clear();
            _hostOf.Clear();
            _fanHost = ObjectId.None;
            var all = _snap.Players.SelectMany(p => p.Battlefield).ToList();
            var creatures = new HashSet<ObjectId>(all.Where(c => c.Type == CardType.Creature).Select(c => c.Id));
            var tucked = new HashSet<ObjectId>();
            foreach (var c in all)
            {
                if (c.AttachedTo.IsNone || !creatures.Contains(c.AttachedTo)) continue;
                if (!_attachments.TryGetValue(c.AttachedTo, out var list)) _attachments[c.AttachedTo] = list = new List<CardView>();
                list.Add(c);
                tucked.Add(c.Id);
                _hostOf[c.Id] = c.AttachedTo;
            }
            return tucked;
        }

        /// <summary>How wide a card is on the table at full size: a tapped unit lies sideways.</summary>
        private static float Footprint(bool sideways) => sideways ? UnitH : UnitW;

        private static bool LiesSideways(CardView c) => c.Type == CardType.Creature && c.Tapped;

        /// <summary>
        /// One side's battlefield (minus the combat lane and tucked attachments), between <paramref name="rowTop"/> and
        /// rowTop + UnitH. Creatures first. It shrinks to fit; below <see cref="OneLineMinScale"/> it becomes two lines
        /// (creatures on the lane side, other permanents behind them), and only then do cards overlap.
        /// </summary>
        private void DrawRow(List<CardView> cards, float rowTop, bool laneIsAbove)
        {
            var creatures = cards.Where(c => c.Type == CardType.Creature).ToList();
            var others = cards.Where(c => c.Type != CardType.Creature).ToList();
            var ordered = creatures.Concat(others).ToList();
            if (ordered.Count == 0) return;
            float avail = CenterRight - CenterLeft - 24f;
            float scale = Mathf.Min(1f, avail / NaturalWidth(ordered));
            if (scale >= OneLineMinScale || creatures.Count == 0 || others.Count == 0)
            {
                DrawLine(ordered, rowTop + UnitH / 2f, Mathf.Max(scale, OneLineMinScale));
                return;
            }
            float half = UnitH * TwoLineScale;
            float front = laneIsAbove ? rowTop + half / 2f : rowTop + UnitH - half / 2f;
            float back = laneIsAbove ? rowTop + UnitH - half / 2f : rowTop + half / 2f;
            DrawLine(others, back, Mathf.Min(TwoLineScale, avail / NaturalWidth(others)));
            DrawLine(creatures, front, Mathf.Min(TwoLineScale, avail / NaturalWidth(creatures)));
        }

        private static float NaturalWidth(List<CardView> cards) =>
            cards.Sum(c => Footprint(LiesSideways(c))) + RowGap * Math.Max(0, cards.Count - 1);

        /// <summary>Lays one line out centred at <paramref name="cy"/>; if it still doesn't fit, the cards overlap evenly.</summary>
        private void DrawLine(List<CardView> cards, float cy, float scale)
        {
            float avail = CenterRight - CenterLeft - 24f;
            var widths = cards.Select(c => Footprint(LiesSideways(c)) * scale).ToList();
            float gap = RowGap * scale;
            float total = widths.Sum() + gap * (cards.Count - 1);
            float step = total > avail && cards.Count > 1 ? (avail - widths.Sum()) / (cards.Count - 1) : gap; // negative: overlap
            float width = widths.Sum() + step * (cards.Count - 1);
            float x = CenterX - width / 2f;
            for (int i = 0; i < cards.Count; i++)
            {
                DrawPermanent(cards[i], x + widths[i] / 2f, cy, scale, LiesSideways(cards[i]));
                x += widths[i] + step;
            }
        }

        /// <summary>
        /// A permanent on the table with its tucked attachments behind it (drawn first, so they're under it), each one
        /// a step further up and to the right. Player Curses ("on Player 2") keep their tag.
        /// </summary>
        private CardWidget DrawPermanent(CardView c, float cx, float cy, float scale, bool sideways, float? tilt = null)
        {
            bool creature = c.Type == CardType.Creature;
            float w = creature ? UnitW : UnitW * 0.95f, h = creature ? UnitH : UnitW * 0.95f * 1.4f;
            float angle = tilt ?? (sideways ? TappedAngle : 0f);
            List<CardWidget> tucked = null;
            if (_attachments.TryGetValue(c.Id, out var gear))
            {
                tucked = new List<CardWidget>();
                for (int k = gear.Count - 1; k >= 0; k--) // the furthest one first: the nearest is drawn last, just under the host
                {
                    var a = MakeWidget(_dynamic, WidgetKind.HandCard, gear[k], cx + (k + 1) * TuckStepX * scale, cy - (k + 1) * TuckStepY * scale, UnitW, UnitH, angle);
                    a.Kind = WidgetKind.Unit;
                    SetHomeScale(a, scale);
                    a.HomeSibling = a.transform.GetSiblingIndex();
                    tucked.Insert(0, a);
                }
            }
            var widget = MakeWidget(_dynamic, creature ? WidgetKind.Unit : WidgetKind.HandCard, c, cx, cy, w, h, angle);
            widget.Kind = WidgetKind.Unit;
            SetHomeScale(widget, scale);
            widget.HomeSibling = widget.transform.GetSiblingIndex();
            if (tucked != null)
            {
                _tucked[c.Id] = tucked;
                // A pip on the host: it carries gear (how many), visible at a glance even when the strips are small.
                var pip = Ui.Circle(widget.transform, "Gear", w - 22, -8, 30, Ui.Hex("#E0A020"));
                Ui.AddOutline(pip.gameObject, new Color(0, 0, 0, 0.8f), 1.5f);
                Ui.FillLabel(pip.transform, (gear.Count > 1 ? gear.Count.ToString() : "") + "E", 13, Ui.Hex("#2A1A08"), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            if (c.AttachedToPlayer != null)
            {
                var tag = Ui.Panel(widget.transform, "Attached", 4, h - 4, w - 8, 18, new Color(0, 0, 0, 0.75f));
                Ui.FillLabel(tag.transform, "on " + c.AttachedToPlayer, 12, Color.white);
            }
            return widget;
        }

        private static void SetHomeScale(CardWidget w, float scale)
        {
            w.HomeScale = scale;
            w.transform.localScale = Vector3.one * scale;
        }

        // ------------------------------------------------------------------ fanning tucked attachments out

        /// <summary>Hovering a host (or one of its strips) fans its attachments out beside it, upright and on top.</summary>
        private void FanOnHover(CardWidget w)
        {
            var host = _tucked.ContainsKey(w.Id) ? w.Id : _hostOf.TryGetValue(w.Id, out var h) ? h : ObjectId.None;
            if (host.IsNone || host == _fanHost) return;
            if (!_fanHost.IsNone) Fan(_fanHost, false);
            Fan(host, true);
        }

        private void Fan(ObjectId host, bool open)
        {
            _fanHost = open ? host : ObjectId.None;
            var hostWidget = WidgetFor(host);
            if (hostWidget == null || !_tucked.TryGetValue(host, out var group)) return;
            float s = hostWidget.HomeScale;
            float hostHalf = Footprint(Mathf.Abs(hostWidget.HomeRotation) > 45f) * s / 2f;
            float dir = hostWidget.HomePosition.x < CenterX ? 1f : -1f; // fan towards the middle of the table
            for (int i = open ? 0 : group.Count - 1; open ? i < group.Count : i >= 0; i += open ? 1 : -1)
            {
                var a = group[i];
                if (a == null) continue;
                PrimeTween.Tween.StopAll(a.Rect);
                if (open)
                {
                    float x = hostWidget.HomePosition.x + dir * (hostHalf + 8f + UnitW * s / 2f + i * (UnitW * s + 6f));
                    a.Rect.anchoredPosition = new Vector2(x, hostWidget.HomePosition.y);
                    a.Rect.localEulerAngles = Vector3.zero;
                    a.transform.SetAsLastSibling();
                }
                else
                {
                    a.Rect.anchoredPosition = a.HomePosition;
                    a.Rect.localEulerAngles = new Vector3(0, 0, a.HomeRotation);
                    // Back under the host (its index now, not the one it was drawn at: hovering other cards moves
                    // siblings around, and a stale index put the gear on top of the creature). Furthest first.
                    a.transform.SetSiblingIndex(hostWidget.transform.GetSiblingIndex());
                }
            }
        }

        /// <summary>
        /// The fan closes once the mouse leaves the area around the host and its attachments (one box around them all,
        /// with a margin: crossing the gap between the host and its gear must not close it, playtest 2026-10-10_140818).
        /// </summary>
        private void UpdateFan()
        {
            if (_fanHost.IsNone) return;
            var host = WidgetFor(_fanHost);
            if (host == null || !_tucked.TryGetValue(_fanHost, out var group)) { Fan(_fanHost, false); return; }
            var corners = new Vector3[4];
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (var r in group.Where(a => a != null).Select(a => a.Rect).Append(host.Rect))
            {
                r.GetWorldCorners(corners);
                foreach (var c in corners)
                {
                    var sp = RectTransformUtility.WorldToScreenPoint(CanvasCamera, c);
                    min = Vector2.Min(min, sp);
                    max = Vector2.Max(max, sp);
                }
            }
            float pad = 16f * Screen.height / Ui.Height;
            Vector2 m = Input.mousePosition;
            if (m.x < min.x - pad || m.x > max.x + pad || m.y < min.y - pad || m.y > max.y + pad) Fan(_fanHost, false);
        }

        // ------------------------------------------------------------------ -board N (layout screenshots)

        /// <summary>
        /// -board N: put N permanents on each side (creatures, every third one tapped, Equipment on some, a Relic now and
        /// then), to check the layout at 3, 8, 14 and 20.
        /// </summary>
        private void FillBoard(int n)
        {
            var st = _s.State;
            var db = _s.Engine.Cards;
            var creatures = db.All.Where(c => c.IsCreature && !c.IsToken && !c.IsTavernDweller).OrderBy(c => c.Id).ToList();
            var gear = db.All.Where(c => c.Type == CardType.Equipment && !c.IsToken).OrderBy(c => c.Id).ToList();
            var relics = db.All.Where(c => c.Type == CardType.Relic && !c.IsToken).OrderBy(c => c.Id).ToList();
            int seed = 0;
            foreach (var p in st.Players)
            {
                CardInstance lastCreature = null;
                for (int i = 0; i < n; i++, seed++)
                {
                    CardDefinition def;
                    if (i % 5 == 3 && lastCreature != null) def = gear[seed % gear.Count];
                    else if (i % 5 == 4 && relics.Count > 0) def = relics[seed % relics.Count];
                    else def = creatures[(seed * 7) % creatures.Count];
                    var c = new CardInstance
                    {
                        Id = new ObjectId(st.NextObjectId++), DefinitionId = def.Id, Owner = p.Id, Controller = p.Id,
                        Zone = Zone.Battlefield, Timestamp = st.NextTimestamp++,
                    };
                    if (def.IsCreature) { c.Tapped = i % 3 == 1; lastCreature = c; }
                    if (def.Type == CardType.Equipment) c.AttachedToObject = lastCreature.Id;
                    p.Battlefield.Add(c);
                }
            }
        }
    }
}
