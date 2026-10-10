using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// LoR-style beats (docs/LOR_PRESENTATION.md). The table is still redrawn from a fresh snapshot after every change;
    /// the events of the change then animate on top of the new layout:
    /// - FLIP moves: every card starts where it was drawn before (followed through zone changes) and slides home.
    ///   That covers plays, draws, attackers and blockers walking into the lane, and units returning.
    /// - Effects on the Fx layer, one after another in event order: the opponent's card reveal, Tavern Dweller Power and
    ///   round banners, resolution flashes, damage and heal numbers, dying units, the gem refill, Gold banking, and
    ///   "Passed" callouts.
    /// Input waits while the beats play (LoR); a click or Space skips them.
    /// </summary>
    public sealed partial class TableView
    {
        private struct Place
        {
            public Vector2 Pos;
            public float Rotation;
            public float Scale;
            public Vector2 Size;
            public CardView View;
        }

        private RectTransform _fxLayer;
        private readonly List<GameEvent> _pendingEvents = new List<GameEvent>();
        private readonly Dictionary<ObjectId, Place> _old = new Dictionary<ObjectId, Place>();
        /// <summary>Where a card was dropped at the end of a drag (it's gone from the table by the next redraw).</summary>
        private readonly Dictionary<ObjectId, Place> _dropped = new Dictionary<ObjectId, Place>();
        /// <summary>Players who passed the action since the last real action (the LoR "Pass" callout).</summary>
        private readonly HashSet<PlayerId> _passed = new HashSet<PlayerId>();
        /// <summary>Things in the dynamic layer with looping tweens (pulses); stopped before every redraw.</summary>
        private readonly List<Object> _looping = new List<Object>();
        private readonly Dictionary<PlayerId, List<Image>> _manaGems = new Dictionary<PlayerId, List<Image>>();
        private readonly Dictionary<PlayerId, List<Image>> _goldGems = new Dictionary<PlayerId, List<Image>>();
        private Vector2? _oldChainTop;
        private Vector2 _chainTop;
        private float _busyUntil;
        private RectTransform _token;
        private Vector2? _oldToken;

        /// <summary>The play history (LoR's left column): who played what, at whom.</summary>
        private sealed class PlayEntry
        {
            public PlayerId Player;
            public string DefinitionId;
            public string Text;
        }

        private readonly List<PlayEntry> _playLog = new List<PlayEntry>();
        private bool _wasBusy;

        /// <summary>Beats are playing: input waits (or skips them), the bot waits, the context button is blank.</summary>
        private bool Busy => Time.unscaledTime < _busyUntil;

        private void HookBeats()
        {
            PrimeTweenConfig.SetTweensCapacity(1000);
            ResetBeats();
            _s.EventsApplied += OnEvents;
        }

        private void ResetBeats()
        {
            Tween.StopAll();
            _pendingEvents.Clear();
            _old.Clear();
            _dropped.Clear();
            _passed.Clear();
            _playLog.Clear();
            _busyUntil = 0f;
            if (_fxLayer != null) Ui.Clear(_fxLayer);
        }

        private void OnEvents(IReadOnlyList<GameEvent> events)
        {
            _pendingEvents.AddRange(events);
            LogPlays(events);
            foreach (var e in events)
                switch (e)
                {
                    case PriorityPassedEvent p when p.ChainCount == 0 && p.Step == Step.Main1:
                        _passed.Add(p.Player);
                        break;
                    case SpellCastEvent _:
                    case AttackerDeclaredEvent _:
                    case RoundStartedEvent _:
                        _passed.Clear();
                        break;
                    case AbilityActivatedEvent a:
                        _passed.Remove(a.Player);
                        break;
                }
        }

        private void LogPlays(IReadOnlyList<GameEvent> events)
        {
            var state = _s.State;
            string Who(PlayerId p) => p == _s.Viewer ? "You" : "Opp";
            string At(Target[] targets) => targets == null || targets.Length == 0 ? ""
                : " \u2192 " + string.Join(", ", targets.Select(t => t.IsPlayer ? (t.Player == _s.Viewer ? "you" : "opponent") : _s.Text.Name(state, t.Object)));
            int attackers = 0;
            PlayerId attacker = default;
            var returned = new List<string>(); // cards a resolving spell or ability brought back from a graveyard
            string resolvedSource = null;
            foreach (var e in events)
                switch (e)
                {
                    case ZoneChangedEvent z when z.From == Zone.Graveyard && (z.To == Zone.Battlefield || z.To == Zone.Hand):
                        returned.Add(_s.Text.Name(z.DefinitionId));
                        break;
                    case ChainItemResolvedEvent r:
                        resolvedSource = r.SourceDefinitionId;
                        break;
                    case SpellCastEvent c:
                        _playLog.Add(new PlayEntry { Player = c.Player, DefinitionId = c.DefinitionId, Text = Who(c.Player) + ": " + _s.Text.Name(c.DefinitionId) + At(c.Targets) });
                        break;
                    case AbilityActivatedEvent a:
                        string what = a.IsTavernDwellerPower ? "Power" : "ability";
                        _playLog.Add(new PlayEntry { Player = a.Player, DefinitionId = a.SourceDefinitionId, Text = Who(a.Player) + ": " + _s.Text.Name(a.SourceDefinitionId) + " " + what + At(a.Targets) });
                        break;
                    case AttackerDeclaredEvent ad:
                        attackers++;
                        attacker = state.FindObject(ad.Attacker)?.Controller ?? attacker;
                        break;
                }
            // What came back from a graveyard goes on the play that did it (playtest 2026-10-10_173434: Exhumation
            // Broadcast didn't say which creatures it took).
            if (returned.Count > 0)
            {
                var play = _playLog.LastOrDefault(p => resolvedSource != null && p.DefinitionId == resolvedSource);
                if (play != null) play.Text += " → " + string.Join(", ", returned);
            }
            if (attackers > 0)
                _playLog.Add(new PlayEntry { Player = attacker, Text = Who(attacker) + ": attack with " + attackers + (attackers == 1 ? " unit" : " units") });
            if (_playLog.Count > 30) _playLog.RemoveRange(0, _playLog.Count - 30);
        }

        /// <summary>Finish every beat now (a click or Space while they play).</summary>
        private void SkipBeats()
        {
            Tween.CompleteAll();
            Ui.Clear(_fxLayer);
            _busyUntil = 0f;
            _dirty = true;
        }

        private void UpdateBeats()
        {
            if (_wasBusy && !Busy)
            {
                _wasBusy = false;
                _dirty = true; // the context button comes back
            }
            if (Busy) _nextBot = Mathf.Max(_nextBot, _busyUntil + 0.15f / _speed);
        }

        // ------------------------------------------------------------------ before / after a redraw

        /// <summary>Remember where every card was drawn, so the next layout can slide from there.</summary>
        private void CaptureOld()
        {
            foreach (var o in _looping)
                if (o != null) Tween.StopAll(o);
            _looping.Clear();
            if (_pendingEvents.Count == 0) return;

            _old.Clear();
            foreach (var w in _widgets)
            {
                if (w == null || w.View == null || w.Id.IsNone || w.transform.parent != _dynamic) continue;
                Tween.StopAll(w.Rect);
                _old[w.Id] = new Place
                {
                    Pos = w.Kind == WidgetKind.HandCard ? w.HomePosition : w.Rect.anchoredPosition,
                    Rotation = w.HomeRotation, Scale = w.HomeScale, Size = w.Rect.sizeDelta, View = w.View,
                };
            }
            foreach (var kv in _dropped) _old[kv.Key] = kv.Value;
            _dropped.Clear();
            _oldChainTop = _snap != null && _snap.Chain.Count > 0 ? _chainTop : (Vector2?)null;
            _oldToken = _token != null ? _token.anchoredPosition : (Vector2?)null;
            _token = null;
        }

        private void RememberDrop(CardWidget w)
        {
            if (w.Id.IsNone) return;
            var local = (Vector2)w.Rect.localPosition;
            _dropped[w.Id] = new Place
            {
                Pos = new Vector2(local.x + Ui.Width / 2f, local.y - Ui.Height / 2f),
                Rotation = 0f, Scale = 1f, Size = w.Rect.sizeDelta, View = w.View,
            };
        }

        /// <summary>Play the events of the last change on top of the new layout.</summary>
        private void PlayBeats()
        {
            if (_pendingEvents.Count == 0) return;
            var events = _pendingEvents.ToList();
            _pendingEvents.Clear();
            float k = 1f / Mathf.Max(0.25f, _speed);

            // New id -> the id it had before (cards get a new id on every zone change).
            var origin = new Dictionary<ObjectId, ObjectId>();
            var cameFrom = new Dictionary<ObjectId, Zone>();
            foreach (var z in events.OfType<ZoneChangedEvent>())
            {
                if (z.NewId.IsNone) continue;
                origin[z.NewId] = origin.TryGetValue(z.OldId, out var first) ? first : z.OldId;
                cameFrom[z.NewId] = z.From;
            }
            var drawn = new HashSet<ObjectId>(events.OfType<CardDrawnEvent>().Select(d => d.Card));
            // A new round: banner, token, gems, then the draws (LoR order).
            float drawDelay = events.Any(e => e is RoundStartedEvent) ? 1.0f * k : 0f;
            var tokens = new HashSet<ObjectId>(events.OfType<TokenCreatedEvent>().Select(t => t.Token));

            foreach (var w in _widgets)
            {
                if (w == null || w.Id.IsNone || w.transform.parent != _dynamic) continue;
                var home = w.HomePosition;
                bool found = _old.TryGetValue(w.Id, out var place)
                             || origin.TryGetValue(w.Id, out var before) && _old.TryGetValue(before, out place);
                if (found)
                {
                    // Tap / untap: a quarter turn (Arena). Untapping at a new round waits for the gems, before the draws.
                    if (Mathf.Abs(Mathf.DeltaAngle(place.Rotation, w.HomeRotation)) > 1f && w.Kind == WidgetKind.Unit)
                    {
                        float turnDelay = drawDelay > 0f && Mathf.Abs(w.HomeRotation) < 1f ? 0.6f * k : 0f;
                        w.Rect.localEulerAngles = new Vector3(0f, 0f, place.Rotation);
                        Tween.Rotation(w.Rect, new Vector3(0f, 0f, place.Rotation), new Vector3(0f, 0f, w.HomeRotation), 0.25f * k, Ease.OutCubic,
                            startDelay: turnDelay);
                    }
                    if (Mathf.Abs(place.Scale - w.HomeScale) > 0.01f && place.Scale > 0f)
                        Tween.Scale(w.transform, place.Scale, w.HomeScale, 0.3f * k, Ease.OutCubic);
                    if ((place.Pos - home).sqrMagnitude < 4f) continue;
                    Slide(w, place.Pos, home, 0.3f * k);
                }
                else if (drawn.Contains(w.Id) || origin.TryGetValue(w.Id, out var o) && drawn.Contains(o))
                {
                    var player = w.View.Owner;
                    w.Rect.anchoredPosition = DeckPos(player);
                    w.transform.localScale = Vector3.one * 0.5f;
                    Tween.UIAnchoredPosition(w.Rect, DeckPos(player), home, 0.3f * k, Ease.OutCubic, startDelay: drawDelay);
                    Tween.Scale(w.transform, 0.5f * w.HomeScale, w.HomeScale, 0.3f * k, Ease.OutCubic, startDelay: drawDelay);
                }
                else if (tokens.Contains(w.Id))
                {
                    Tween.Scale(w.transform, 0f, w.HomeScale, 0.3f * k, Ease.OutBack);
                }
                else if (cameFrom.TryGetValue(w.Id, out var src) && src == Zone.Chain && _oldChainTop != null)
                {
                    Slide(w, _oldChainTop.Value, home, 0.3f * k);
                    Tween.Scale(w.transform, 0.6f * w.HomeScale, w.HomeScale, 0.3f * k, Ease.OutCubic);
                }
            }

            // Effects, one after another.
            float t = 0f;
            NextSoundBatch();
            foreach (var e in events)
            {
                SoundFor(e, t);
                switch (e)
                {
                    case SpellCastEvent s when s.Player != _snap.Viewer:
                        Reveal(s.DefinitionId, s.Player, t, k);
                        t += 0.9f * k;
                        break;
                    case AbilityActivatedEvent a when a.IsTavernDwellerPower:
                        Banner(_s.Text.Name(a.SourceDefinitionId) + "'s Power!", a.Player == _snap.Viewer ? "You" : "Opponent", t, k, Ui.Hex("#E0A020"));
                        Flash(DwellerWidget(a.Player), Ui.Hex("#FFD060"), t, k);
                        t += 0.8f * k;
                        break;
                    case AbilityActivatedEvent a when a.Player != _snap.Viewer:
                        Reveal(a.SourceDefinitionId, a.Player, t, k, a.Text);
                        t += 0.8f * k;
                        break;
                    case AbilityTriggeredEvent tr:
                        Pop(WidgetFor(tr.Source), "!", Ui.Hex("#FFD060"), t, k);
                        t += 0.15f * k;
                        break;
                    case ChainItemResolvedEvent r:
                        ChainFlash(_s.Text.Name(r.SourceDefinitionId), Ui.Hex("#FFE0A0"), t, k);
                        t += 0.25f * k;
                        break;
                    case CounteredEvent c:
                        ChainFlash(_s.Text.Name(c.SourceDefinitionId) + ": countered", Ui.Hex("#FF6060"), t, k);
                        t += 0.4f * k;
                        break;
                    case TriggerSkippedEvent skip:
                        ChainFlash(_s.Text.Name(skip.SourceDefinitionId) + (skip.Declined ? ": no target" : ": no legal target"), Ui.Hex("#B0B0B0"), t, k);
                        Pop(WidgetFor(skip.Source), "–", Ui.Hex("#B0B0B0"), t, k);
                        t += 0.4f * k;
                        break;
                    case FizzledEvent f:
                        ChainFlash(_s.Text.Name(f.SourceDefinitionId) + ": fizzles", Ui.Hex("#B0B0B0"), t, k);
                        t += 0.4f * k;
                        break;
                    case DamageDealtEvent d:
                    {
                        var target = TargetWidget(d.Target);
                        FloatText(target, "-" + d.Amount, CardFaces.Damaged, t, k);
                        Flash(target, CardFaces.Damaged, t, k);
                        if (target != null && target.Kind == WidgetKind.TavernDweller)
                            Tween.ShakeLocalPosition(target.transform, new Vector3(14f, 8f, 0f), 0.35f * k, startDelay: t);
                        t += 0.12f * k;
                        break;
                    }
                    case HealedEvent h:
                        FloatText(TargetWidget(h.Target), "+" + h.Amount, CardFaces.Buffed, t, k);
                        t += 0.12f * k;
                        break;
                    case CreatureDiedEvent c:
                        Ghost(c.Card, t, k);
                        t += 0.15f * k;
                        break;
                    case RoundStartedEvent r:
                    {
                        var leader = _snap.Players.FirstOrDefault(p => p.HasAttackToken);
                        string sub = leader == null ? "" : leader.Id == _snap.Viewer ? "You have the attack token" : "Opponent has the attack token";
                        Banner("Round " + r.Round, sub, t, k, Ui.Hex("#FFD070"));
                        TokenFlip(t + 0.25f * k, k);
                        NewGems(t + 0.3f * k, k);
                        Refill(t + 0.45f * k, k);
                        t += 1.0f * k;
                        break;
                    }
                    // Combat steps get a banner too (playtest 2026-10-10_155716), except your own attack's: you just declared it.
                    case StepStartedEvent st when st.Step == Step.DeclareAttackers || st.Step == Step.DeclareBlockers:
                    {
                        var attacker = _snap.Players.FirstOrDefault(p => p.HasAttackToken);
                        bool mine = attacker != null && attacker.Id == _snap.Viewer;
                        if (st.Step == Step.DeclareAttackers && mine) break;
                        if (st.Step == Step.DeclareAttackers) Banner("They attack!", "Declare attackers", t, k * 0.7f, Ui.Hex("#FF7050"));
                        else Banner(mine ? "They block" : "Block!", mine ? "Opponent declares blockers" : "Choose your blockers, then press Block", t, k * 0.7f, Ui.Hex("#FF7050"));
                        t += 0.7f * k;
                        break;
                    }
                    case GoldBankedEvent g when g.Banked > 0:
                        Bank(g.Player, g.Banked, t, k);
                        t += 0.35f * k;
                        break;
                    // "Passed" always (it decides whether passing ends the round); "OK" on the Chain only when they chose to.
                    case PriorityPassedEvent p when p.Player != _snap.Viewer && (p.ChainCount == 0 && p.Step == Step.Main1 || p.ChainCount > 0 && !p.Automatic):
                        Callout(p.Player, p.ChainCount > 0 ? "OK" : "Passed", t, k);
                        break;
                }
            }

            if (t > 0.05f)
            {
                _busyUntil = Time.unscaledTime + t + 0.1f * k;
                _wasBusy = true;
            }
        }

        // ------------------------------------------------------------------ helpers

        private static void Slide(CardWidget w, Vector2 from, Vector2 to, float seconds)
        {
            w.Rect.anchoredPosition = from;
            Tween.UIAnchoredPosition(w.Rect, from, to, seconds, Ease.OutCubic);
        }

        private Vector2 DeckPos(PlayerId p) => new Vector2(1679f, p == _snap.Viewer ? -1010f : -54f);

        private CardWidget WidgetFor(ObjectId id) =>
            id.IsNone ? null : _widgets.FirstOrDefault(w => w != null && w.Id == id && w.transform.parent == _dynamic);

        private CardWidget DwellerWidget(PlayerId p) =>
            _widgets.FirstOrDefault(w => w != null && w.Kind == WidgetKind.TavernDweller && w.Player == p);

        private CardWidget TargetWidget(Target t) => t.IsPlayer ? DwellerWidget(t.Player) : WidgetFor(t.Object);

        /// <summary>A fx object, invisible until its beat (alpha 0), destroyed when it's done.</summary>
        private (RectTransform rt, CanvasGroup group) Fx(string name, Vector2 center, Vector2 size, float lifetime)
        {
            var rt = Ui.Rect(_fxLayer, name, 0, 0, size.x, size.y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            Destroy(rt.gameObject, lifetime + 0.1f);
            return (rt, group);
        }

        private CardView ViewOf(string definitionId, PlayerId owner)
        {
            var def = _db.Get(definitionId);
            return new CardView
            {
                DefinitionId = definitionId, Name = def.Name, Type = def.Type, Faction = def.Faction, Rarity = def.Rarity,
                Text = def.Text, Subtypes = def.Subtypes, Cost = def.Cost, PrintedCost = def.Cost,
                Power = def.Power, PrintedPower = def.Power, MaxHealth = def.Health, PrintedHealth = def.Health,
                Keywords = def.Keywords, Owner = owner, Controller = owner, Zone = Zone.Chain,
            };
        }

        /// <summary>LoR: the opponent's card flies big into the middle of the table, then to the Chain.</summary>
        private void Reveal(string definitionId, PlayerId player, float at, float k, string abilityText = null)
        {
            if (definitionId == null) return;
            const float w = 300f, h = 420f;
            float life = at + 0.9f * k;
            var (rt, group) = Fx("Reveal", new Vector2(CenterX, -LaneMid), new Vector2(w, h), life);
            CardFaces.Build(rt, ViewOf(definitionId, player), FaceStyle.Zoom);
            foreach (var g in rt.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            if (abilityText != null)
            {
                var note = Ui.Panel(rt, "Ability", -20, h + 6, w + 40, 46, new Color(0, 0, 0, 0.85f));
                Ui.FillLabel(note.transform, abilityText, 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 4f);
            }
            var tag = Ui.Panel(rt, "Who", 0, -34, w, 30, new Color(0.55f, 0.12f, 0.1f, 0.95f));
            Ui.FillLabel(tag.transform, "Opponent plays", 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Tween.Alpha(group, 0f, 1f, 0.12f * k, startDelay: at);
            Tween.Scale(rt, 0.55f, 1f, 0.2f * k, Ease.OutBack, startDelay: at);
            // Then off to the Chain column and gone.
            Tween.UIAnchoredPosition(rt, new Vector2(CenterX, -LaneMid), _chainTop, 0.22f * k, Ease.InCubic, startDelay: at + 0.62f * k);
            var landing = _widgets.LastOrDefault(x => x != null && x.Kind == WidgetKind.ChainItem);
            if (landing != null)
            {
                var cg = landing.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                Tween.Alpha(cg, 0f, 1f, 0.15f * k, startDelay: at + 0.8f * k);
            }
            Tween.Scale(rt, 1f, 0.2f, 0.22f * k, Ease.InCubic, startDelay: at + 0.62f * k);
            Tween.Alpha(group, 1f, 0f, 0.22f * k, startDelay: at + 0.66f * k);
        }

        private void Banner(string title, string sub, float at, float k, Color color)
        {
            float life = at + 1.0f * k;
            var (rt, group) = Fx("Banner", new Vector2(CenterX, -LaneMid), new Vector2(CenterRight - CenterLeft, 130f), life);
            Ui.FillPanel(rt, "Band", new Color(0.08f, 0.04f, 0.02f, 0.85f));
            Ui.Panel(rt, "Top", 0, 0, CenterRight - CenterLeft, 3, color);
            Ui.Panel(rt, "Bottom", 0, 127, CenterRight - CenterLeft, 3, color);
            var t = Ui.Label(rt, title, 0, 8, CenterRight - CenterLeft, 76, 56, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.AddOutline(t.gameObject, new Color(0, 0, 0, 0.9f), 2f);
            if (!string.IsNullOrEmpty(sub)) Ui.Label(rt, sub, 0, 84, CenterRight - CenterLeft, 36, 22, Ui.Hex("#F0E0C0"), TextAnchor.MiddleCenter);
            Tween.Alpha(group, 0f, 1f, 0.12f * k, startDelay: at);
            Tween.Scale(rt, 1.15f, 1f, 0.25f * k, Ease.OutCubic, startDelay: at);
            Tween.Alpha(group, 1f, 0f, 0.2f * k, startDelay: at + 0.8f * k);
        }

        private void FloatText(CardWidget target, string text, Color color, float at, float k)
        {
            if (target == null) return;
            var c = Center(target);
            float life = at + 0.8f * k;
            var (rt, group) = Fx("Number", c, new Vector2(140f, 70f), life);
            var label = Ui.FillLabel(rt, text, 48, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.AddOutline(label.gameObject, new Color(0, 0, 0, 0.95f), 3f);
            Tween.Alpha(group, 0f, 1f, 0.06f * k, startDelay: at);
            Tween.Scale(rt, 1.6f, 1f, 0.18f * k, Ease.OutBack, startDelay: at);
            Tween.UIAnchoredPosition(rt, c, c + new Vector2(0f, 60f), 0.75f * k, Ease.OutCubic, startDelay: at);
            Tween.Alpha(group, 1f, 0f, 0.3f * k, startDelay: at + 0.45f * k);
        }

        private void Flash(CardWidget target, Color color, float at, float k)
        {
            if (target == null) return;
            var size = target.Rect.sizeDelta + new Vector2(16f, 16f);
            var (rt, group) = Fx("Flash", Center(target), size, at + 0.45f * k);
            var glow = Ui.FillPanel(rt, "Glow", new Color(color.r, color.g, color.b, 0.55f));
            if (target.Kind == WidgetKind.TavernDweller || target.Kind == WidgetKind.ChainItem) glow.sprite = Ui.CircleSprite; // round things flash round
            Tween.Alpha(group, 0f, 1f, 0.05f * k, startDelay: at);
            Tween.Alpha(group, 1f, 0f, 0.35f * k, startDelay: at + 0.08f * k);
        }

        private void Pop(CardWidget target, string text, Color color, float at, float k)
        {
            if (target == null) return;
            var c = Center(target) + new Vector2(0f, target.Rect.sizeDelta.y / 2f);
            var (rt, group) = Fx("Pop", c, new Vector2(44f, 44f), at + 0.6f * k);
            var img = Ui.FillPanel(rt, "Badge", color);
            Ui.AddOutline(img.gameObject, new Color(0, 0, 0, 0.8f), 2f);
            Ui.FillLabel(rt, text, 30, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            Tween.Alpha(group, 0f, 1f, 0.05f * k, startDelay: at);
            Tween.Scale(rt, 0.2f, 1f, 0.2f * k, Ease.OutBack, startDelay: at);
            Tween.Alpha(group, 1f, 0f, 0.2f * k, startDelay: at + 0.4f * k);
        }

        /// <summary>The top of the Chain resolving (or being countered): a flash where it stood.</summary>
        private void ChainFlash(string text, Color color, float at, float k)
        {
            var pos = _oldChainTop ?? new Vector2(CenterX, -LaneMid);
            var (rt, group) = Fx("Resolve", pos, new Vector2(140f, 140f), at + 0.5f * k);
            var img = Ui.Circle(rt, "Burst", 0, 0, 140f, new Color(color.r, color.g, color.b, 0.9f));
            Ui.AddOutline(img.gameObject, Color.white, 3f);
            Ui.FillLabel(rt, text, 15, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold, 14f);
            Tween.Alpha(group, 0f, 1f, 0.05f * k, startDelay: at);
            Tween.Scale(rt, 1f, 1.2f, 0.4f * k, Ease.OutCubic, startDelay: at);
            Tween.Alpha(group, 1f, 0f, 0.3f * k, startDelay: at + 0.15f * k);
        }

        /// <summary>A dying unit: a copy where it stood shrinks, tilts and fades.</summary>
        private void Ghost(ObjectId card, float at, float k)
        {
            if (!_old.TryGetValue(card, out var place) || place.View == null) return;
            var (rt, group) = Fx("Dies", place.Pos, place.Size, at + 0.55f * k);
            CardFaces.Build(rt, place.View, FaceStyle.Unit);
            foreach (var g in rt.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            group.alpha = 1f;
            Tween.Scale(rt, 1f, 0.6f, 0.45f * k, Ease.InCubic, startDelay: at);
            Tween.Rotation(rt, Vector3.zero, new Vector3(0f, 0f, 18f), 0.45f * k, Ease.InCubic, startDelay: at);
            Tween.UIAnchoredPosition(rt, place.Pos, place.Pos + new Vector2(0f, -40f), 0.45f * k, Ease.InCubic, startDelay: at);
            Tween.Alpha(group, 1f, 0f, 0.45f * k, startDelay: at);
        }

        /// <summary>LoR round start: the attack token flips over to the new leader.</summary>
        private void TokenFlip(float at, float k)
        {
            if (_token == null) return;
            var to = _token.anchoredPosition;
            var from = _oldToken ?? to;
            _token.anchoredPosition = from;
            Tween.UIAnchoredPosition(_token, from, to, 0.35f * k, Ease.InOutCubic, startDelay: at);
            Tween.Rotation(_token, Vector3.zero, new Vector3(0f, 0f, 360f), 0.35f * k, Ease.InOutCubic, startDelay: at);
            Tween.Scale(_token, 1f, 1.4f, 0.18f * k, Ease.OutCubic, cycles: 2, cycleMode: CycleMode.Yoyo, startDelay: at);
        }

        /// <summary>LoR round start: this round's new mana gem crystallizes (pops in with a flash) before the refill.</summary>
        private void NewGems(float at, float k)
        {
            foreach (var kv in _manaGems)
            {
                var p = _snap.Player(kv.Key);
                int i = p.MaxMana - 1;
                if (i < 0 || i >= kv.Value.Count) continue;
                var gem = kv.Value[i];
                gem.transform.localScale = Vector3.zero;
                Tween.Scale(gem.transform, 0f, 1f, 0.3f * k, Ease.OutBack, startDelay: at);
                var c = ((RectTransform)gem.transform).anchoredPosition + new Vector2(11f, -11f);
                var (rt, group) = Fx("Crystal", c, new Vector2(46f, 46f), at + 0.45f * k);
                Ui.Circle(rt, "Flash", 0, 0, 46f, new Color(0.7f, 0.9f, 1f, 0.9f));
                Tween.Alpha(group, 0f, 1f, 0.05f * k, startDelay: at);
                Tween.Scale(rt, 0.3f, 1.4f, 0.35f * k, Ease.OutCubic, startDelay: at);
                Tween.Alpha(group, 1f, 0f, 0.3f * k, startDelay: at + 0.1f * k);
            }
        }

        /// <summary>LoR round start: the mana gems fill one by one.</summary>
        private void Refill(float at, float k)
        {
            foreach (var kv in _manaGems)
            {
                var p = _snap.Player(kv.Key);
                for (int i = 0; i < kv.Value.Count && i < p.Mana; i++)
                {
                    var gem = kv.Value[i];
                    var full = gem.color;
                    gem.color = ManaSpent;
                    Tween.Color(gem, ManaSpent, full, 0.12f * k, startDelay: at + i * 0.06f * k);
                    Tween.Scale(gem.transform, 1.5f, 1f, 0.2f * k, Ease.OutBack, startDelay: at + i * 0.06f * k);
                }
            }
        }

        /// <summary>LoR round end: unspent mana flies into the Gold (spell mana) slots.</summary>
        private void Bank(PlayerId player, int banked, float at, float k)
        {
            if (!_manaGems.TryGetValue(player, out var mana) || !_goldGems.TryGetValue(player, out var gold)) return;
            var p = _snap.Player(player);
            for (int j = 0; j < banked; j++)
            {
                int slot = p.Gold - banked + j;
                if (slot < 0 || slot >= gold.Count || mana.Count == 0) continue;
                var target = gold[slot];
                Vector2 from = ((RectTransform)mana[Mathf.Min(j, mana.Count - 1)].transform).anchoredPosition + new Vector2(11f, -11f);
                Vector2 to = target.rectTransform.anchoredPosition + new Vector2(10f, -10f);
                float start = at + j * 0.1f * k;
                var (rt, group) = Fx("Bank", from, new Vector2(20f, 20f), start + 0.4f * k);
                Ui.FillPanel(rt, "Gem", Ui.Hex("#3AA0FF"));
                rt.localEulerAngles = new Vector3(0, 0, 45);
                group.alpha = 0f;
                Tween.Alpha(group, 0f, 1f, 0.02f, startDelay: start);
                Tween.UIAnchoredPosition(rt, from, to, 0.3f * k, Ease.InOutCubic, startDelay: start);
                var goldColor = target.color;
                target.color = GoldEmpty;
                Tween.Color(target, GoldEmpty, goldColor, 0.1f * k, startDelay: start + 0.3f * k);
                Tween.Scale(target.transform, 1.6f, 1f, 0.25f * k, Ease.OutBack, startDelay: start + 0.3f * k);
            }
        }

        /// <summary>LoR: a "Pass" bubble by the opponent's portrait.</summary>
        private void Callout(PlayerId player, string text, float at, float k)
        {
            var dweller = DwellerWidget(player);
            if (dweller == null) return;
            var c = Center(dweller) + new Vector2(150f, -60f);
            var (rt, group) = Fx("Callout", c, new Vector2(120f, 44f), at + 1.0f * k);
            var img = Ui.FillPanel(rt, "Bubble", Ui.Hex("#F0E6D0"));
            Ui.AddOutline(img.gameObject, new Color(0, 0, 0, 0.8f), 2f);
            Ui.FillLabel(rt, text, 20, Ui.Hex("#2A1A0E"), TextAnchor.MiddleCenter, FontStyle.Bold);
            Tween.Alpha(group, 0f, 1f, 0.08f * k, startDelay: at);
            Tween.Scale(rt, 0.4f, 1f, 0.2f * k, Ease.OutBack, startDelay: at);
            Tween.Alpha(group, 1f, 0f, 0.25f * k, startDelay: at + 0.75f * k);
        }

        /// <summary>The widget's centre in the dynamic layer's coordinates (top-left anchored, y down is negative).</summary>
        private Vector2 Center(CardWidget w)
        {
            var corners = new Vector3[4];
            w.Rect.GetWorldCorners(corners);
            var local = _root.InverseTransformPoint((corners[0] + corners[2]) / 2f);
            return new Vector2(local.x + Ui.Width / 2f, local.y - Ui.Height / 2f);
        }

        // ------------------------------------------------------------------ spend preview

        private static readonly Color ManaFull = Ui.Hex("#3AA0FF");
        private static readonly Color ManaSpent = Ui.Hex("#1A3048");
        private static readonly Color GoldFull = Ui.Hex("#F0C030");
        private static readonly Color GoldEmpty = Ui.Hex("#4A3A18");
        private static readonly Color SpendPreview = Ui.Hex("#FFFFFF");

        private readonly List<(Image gem, Color color)> _previewed = new List<(Image, Color)>();

        /// <summary>LoR: hovering a playable card shows which gems it would use (Gold as the rules spend it, then mana).</summary>
        private void PreviewSpend(CardView v)
        {
            ClearSpendPreview();
            if (v == null || v.IsHidden || !_sources.Contains(v.Id) || !_s.HumanToAct) return;
            var state = _s.State;
            var player = state.GetPlayer(_snap.Viewer);
            var def = _db.Get(v.DefinitionId);
            int cost = v.Cost;
            int gold = Payment.GoldNeeded(player, cost, Payment.GoldAllowed(state, _db, player.Id, def));
            if (gold < 0) return;
            int mana = cost - gold;
            if (_goldGems.TryGetValue(player.Id, out var golds))
                for (int i = player.Gold - 1; i >= 0 && i >= player.Gold - gold && i < golds.Count; i--) Preview(golds[i]);
            if (_manaGems.TryGetValue(player.Id, out var manas))
                for (int i = player.Mana - 1; i >= 0 && i >= player.Mana - mana && i < manas.Count; i--) Preview(manas[i]);
        }

        private void Preview(Image gem)
        {
            _previewed.Add((gem, gem.color));
            Tween.StopAll(gem);
            Tween.Color(gem, gem.color, SpendPreview, 0.35f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
            _looping.Add(gem);
        }

        private void ClearSpendPreview()
        {
            foreach (var (gem, color) in _previewed)
            {
                if (gem == null) continue;
                Tween.StopAll(gem);
                gem.color = color;
            }
            _previewed.Clear();
        }

        /// <summary>A soft endless pulse (the context button and the portrait of whoever has the action).</summary>
        private void Pulse(Transform t, float amount)
        {
            Tween.Scale(t, 1f, 1f + amount, 0.6f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
            _looping.Add(t);
        }
    }
}
