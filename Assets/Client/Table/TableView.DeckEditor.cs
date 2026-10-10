using System;
using System.Collections.Generic;
using System.IO;
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
    /// The deck editor (from the main menu): pick a Tavern Dweller, click cards from its factions (and neutral) to add
    /// them, click deck rows to take them out, save. Saved decks go to the player's own deck file
    /// (<see cref="CardPool.CustomDecksFile"/>) and show up in the menu's deck list. Hovering a card zooms it.
    /// </summary>
    public sealed partial class TableView
    {
        private const int EditorColumns = 8, EditorRows = 3;
        private const float PoolW = 150f, PoolH = 210f;

        private bool _editorOpen;
        private string _edId;              // the custom deck being edited (null: a new one)
        private string _edName = "My deck";
        private string _edDweller;
        private readonly List<string> _edCards = new List<string>();
        private string _edFaction;          // null: every faction the Tavern Dweller allows
        private int _edType;                // 0 all, 1 creatures, 2 spells, 3 other permanents
        private int _edPage;
        private string _edNote;
        private RectTransform _edZoom;

        private void OpenEditor(CardPool.DeckList from)
        {
            _editorOpen = true;
            _edCards.Clear();
            if (from != null)
            {
                _edId = from.Custom ? from.Id : null;
                _edName = from.Custom ? from.Name : from.Name + " (copy)";
                _edDweller = from.TavernDweller;
                _edCards.AddRange(from.Cards);
            }
            else
            {
                _edId = null;
                _edName = "My deck";
                _edDweller = _edDweller ?? _db.All.First(d => d.IsTavernDweller).Id;
            }
            _edFaction = null;
            _edPage = 0;
            _edNote = null;
            _dirty = true;
        }

        private static bool Collectible(CardDefinition d) => !d.IsToken && !d.IsTavernDweller;

        private bool Allowed(CardDefinition d) =>
            d.Faction == "neutral" || _edDweller != null && Array.IndexOf(_db.Get(_edDweller).TavernDwellerFactions, d.Faction) >= 0;

        private List<CardDefinition> EditorPool()
        {
            return _db.All.Where(d => Collectible(d) && Allowed(d))
                .Where(d => _edFaction == null || d.Faction == _edFaction)
                .Where(d => _edType == 0 || _edType == 1 && d.IsCreature || _edType == 2 && (d.Type == CardType.Instant || d.Type == CardType.Sorcery)
                            || _edType == 3 && d.IsPermanent && !d.IsCreature)
                .OrderBy(d => d.Cost).ThenBy(d => d.Name, StringComparer.Ordinal).ToList();
        }

        private string EditorProblem()
        {
            if (_edDweller == null) return "Pick a Tavern Dweller";
            if (_edCards.Count != FormatConfig.Standard().DeckSize) return _edCards.Count + " cards: a deck needs exactly " + FormatConfig.Standard().DeckSize;
            try { DeckValidator.Validate(_db, FormatConfig.Standard(), _edCards, _edDweller); }
            catch (ArgumentException e) { return e.Message; }
            return null;
        }

        private void DrawEditor()
        {
            _edZoom = null;
            Ui.FillPanel(_overlay, "Editor", Ui.Hex("#24160C"), 0f, raycast: true);
            Ui.Label(_overlay, "DECK EDITOR", 20, 4, 400, 40, 30, Ui.Hex("#FFD070"), TextAnchor.MiddleLeft, FontStyle.Bold);

            // Start from: every deck (a prototype one is copied, a custom one edited in place), or an empty one.
            var decks = CardPool.PrototypeDecks();
            Ui.Label(_overlay, "Start from", 420, 8, 120, 24, 16, Ui.Hex("#E0C890"), TextAnchor.MiddleLeft, FontStyle.Bold);
            float bx = 540;
            Ui.Button(_overlay, "Empty", bx, 6, 90, 28, ButtonColor, () => OpenEditor(null), 14);
            bx += 96;
            for (int i = 0; i < decks.Count; i++)
            {
                var d = decks[i];
                float bw = Mathf.Min(200f, 20f + d.Name.Length * 8f);
                if (bx + bw > 1900) break;
                Ui.Button(_overlay, d.Name, bx, 6, bw, 28, d.Custom ? Mine : ButtonColor, () => OpenEditor(d), 14);
                bx += bw + 6;
            }

            // Tavern Dweller: decides which factions the pool shows.
            Ui.Label(_overlay, "Tavern Dweller", 20, 46, 160, 26, 16, Ui.Hex("#E0C890"), TextAnchor.MiddleLeft, FontStyle.Bold);
            var dwellers = _db.All.Where(d => d.IsTavernDweller).OrderBy(d => d.Name, StringComparer.Ordinal).ToList();
            float dx = 180, dy = 44;
            foreach (var d in dwellers)
            {
                float bw = Mathf.Min(220f, 20f + d.Name.Length * 9f);
                if (dx + bw > 1460) { dx = 180; dy += 34; } // two rows: the deck panel starts at 1470
                Ui.Button(_overlay, d.Name, dx, dy, bw, 30, d.Id == _edDweller ? ContextOn : ButtonColor, () =>
                {
                    _edDweller = d.Id;
                    _edFaction = null;
                    _edPage = 0;
                    _dirty = true;
                }, 14);
                dx += bw + 6;
            }
            if (_edDweller != null)
            {
                var dw = _db.Get(_edDweller);
                Ui.Label(_overlay, dw.Name + ": " + dw.Text, 20, 112, 1440, 26, 14, Ui.Hex("#D8C8B0"), TextAnchor.UpperLeft);
            }

            // Filters: faction, card type.
            float fy = 140;
            var factions = new List<string> { null };
            if (_edDweller != null) factions.AddRange(_db.Get(_edDweller).TavernDwellerFactions);
            factions.Add("neutral");
            float fx = 20;
            foreach (var f in factions.Distinct())
            {
                string label = f == null ? "All" : string.Join(" ", f.Split('_').Select(p => char.ToUpper(p[0]) + p.Substring(1)));
                float bw = Mathf.Max(120f, 24f + label.Length * 9f);
                Ui.Button(_overlay, label, fx, fy, bw, 30, f == _edFaction ? ContextOn : ButtonColor, () =>
                {
                    _edFaction = f;
                    _edPage = 0;
                    _dirty = true;
                }, 14);
                fx += bw + 6;
            }
            fx += 30;
            string[] types = { "Any type", "Creatures", "Spells", "Other" };
            for (int t = 0; t < types.Length; t++)
            {
                int tt = t;
                Ui.Button(_overlay, types[t], fx, fy, 110, 30, t == _edType ? ContextOn : ButtonColor, () =>
                {
                    _edType = tt;
                    _edPage = 0;
                    _dirty = true;
                }, 14);
                fx += 116;
            }

            // The pool: a page of cards. Click adds one (up to the copy limit), right-click takes one out.
            var pool = EditorPool();
            int perPage = EditorColumns * EditorRows;
            int pages = Math.Max(1, (pool.Count + perPage - 1) / perPage);
            _edPage = Mathf.Clamp(_edPage, 0, pages - 1);
            int limit = FormatConfig.Standard().CopyLimit;
            for (int i = 0; i < perPage && _edPage * perPage + i < pool.Count; i++)
            {
                var def = pool[_edPage * perPage + i];
                float x = 20 + (i % EditorColumns) * (PoolW + 30), y = 180 + (i / EditorColumns) * (PoolH + 46);
                var rt = Ui.Rect(_overlay, "Pool " + def.Name, x, y, PoolW, PoolH);
                CardFaces.Build(rt, ViewOf(def.Id, _snap.Viewer), FaceStyle.Hand);
                int have = _edCards.Count(c => c == def.Id);
                if (have > 0)
                {
                    var badge = Ui.Circle(rt, "Copies", PoolW - 30, PoolH - 30, 40, have >= limit ? Ui.Hex("#C03030") : Ui.Hex("#2F6FD0"));
                    Ui.AddOutline(badge.gameObject, Color.black, 2f);
                    Ui.FillLabel(badge.transform, "x" + have, 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                }
                string id = def.Id;
                Ui.Label(_overlay, have + "/" + limit, x, y + PoolH + 2, PoolW, 20, 14, have > 0 ? Color.white : Ui.Hex("#907860"));
                EditorCard.Attach(rt.gameObject, () => EditorAdd(id), () => EditorRemove(id),
                    on => EditorZoom(on ? id : null, x + PoolW / 2f));
            }
            float py = 180 + EditorRows * (PoolH + 46);
            Ui.Button(_overlay, "< Prev", 20, py, 120, 36, ButtonColor, () => { _edPage--; _dirty = true; }, 16, _edPage > 0);
            Ui.Label(_overlay, "Page " + (_edPage + 1) + " / " + pages + "  ·  " + pool.Count + " cards  ·  click adds, right-click removes",
                150, py, 700, 36, 16, Ui.Hex("#E0C890"), TextAnchor.MiddleLeft);
            Ui.Button(_overlay, "Next >", 860, py, 120, 36, ButtonColor, () => { _edPage++; _dirty = true; }, 16, _edPage < pages - 1);

            DrawEditorDeck();
        }

        private void DrawEditorDeck()
        {
            const float x = 1480, w = 420;
            Ui.Panel(_overlay, "DeckPanel", x - 10, 60, w + 20, 1010, new Color(0, 0, 0, 0.3f));

            // The name: a text field.
            var nameBox = Ui.Panel(_overlay, "Name", x, 66, w, 40, new Color(0.95f, 0.9f, 0.8f, 0.95f), raycast: true);
            var text = Ui.FillLabel(nameBox.transform, _edName, 20, new Color(0.15f, 0.1f, 0.05f), TextAnchor.MiddleLeft, FontStyle.Bold, 8f);
            text.raycastTarget = false;
            var input = nameBox.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.text = _edName;
            input.characterLimit = 32;
            input.onEndEdit.AddListener(v => { _edName = string.IsNullOrWhiteSpace(v) ? "My deck" : v.Trim(); });

            int size = FormatConfig.Standard().DeckSize;
            string problem = EditorProblem();
            Ui.Label(_overlay, _edCards.Count + " / " + size + " cards", x, 112, 200, 30, 22,
                _edCards.Count == size ? Ui.Hex("#80E080") : Ui.Hex("#FF9070"), TextAnchor.MiddleLeft, FontStyle.Bold);

            // Mana curve: costs 0-7+.
            var curve = new int[8];
            foreach (var id in _edCards) curve[Mathf.Clamp(_db.Get(id).Cost, 0, 7)]++;
            int top = Math.Max(1, curve.Max());
            for (int c = 0; c < 8; c++)
            {
                float bh = 40f * curve[c] / top;
                Ui.Panel(_overlay, "Curve", x + 220 + c * 25, 142 - bh, 20, bh, CardFaces.CostColor);
                Ui.Label(_overlay, c == 7 ? "7+" : c.ToString(), x + 216 + c * 25, 144, 28, 14, 11, Ui.Hex("#E0C890"));
            }

            // The list, by cost: click takes one out.
            var rows = _edCards.GroupBy(id => id).Select(g => (def: _db.Get(g.Key), n: g.Count()))
                .OrderBy(r => r.def.Cost).ThenBy(r => r.def.Name, StringComparer.Ordinal).ToList();
            float rowH = Mathf.Min(30f, 760f / Math.Max(1, rows.Count));
            for (int i = 0; i < rows.Count; i++)
            {
                var (def, n) = rows[i];
                float y = 166 + i * rowH;
                var fs = CardFaces.Style(def.Faction);
                bool ok = Allowed(def);
                var row = Ui.Panel(_overlay, "Row " + def.Name, x, y, w, rowH - 2, ok ? fs.Frame : Ui.Hex("#802020"), raycast: true);
                var cost = Ui.Panel(row.transform, "Cost", 0, 0, rowH - 2, rowH - 2, CardFaces.CostColor);
                Ui.FillLabel(cost.transform, def.Cost.ToString(), 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(row.transform, def.Name + (ok ? "" : "  (outside the factions)"), rowH + 6, 0, w - rowH - 60, rowH - 2, 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(row.transform, "x" + n, w - 50, 0, 44, rowH - 2, 16, Ui.Hex("#FFD070"), TextAnchor.MiddleRight, FontStyle.Bold);
                string id = def.Id;
                EditorCard.Attach(row.gameObject, () => EditorRemove(id), () => EditorRemove(id), on => EditorZoom(on ? id : null, x - 10));
            }

            float by = 936;
            if (_edNote != null || problem != null)
                Ui.Label(_overlay, _edNote ?? problem, x, by - 34, w, 30, 15, _edNote != null ? Ui.Hex("#80E080") : Ui.Hex("#FF9070"), TextAnchor.MiddleLeft);
            Ui.Button(_overlay, "Save", x, by, 136, 54, ContextOn, EditorSave, 22);
            Ui.Button(_overlay, "Clear", x + 142, by, 136, 54, ButtonColor, () => { _edCards.Clear(); _edNote = null; _dirty = true; }, 20);
            Ui.Button(_overlay, "Delete", x + 284, by, 136, 54, Ui.Hex("#5A2A20"), EditorDelete, 20, _edId != null);
            Ui.Button(_overlay, "Back to the menu", x, by + 62, w, 54, ButtonColor, () =>
            {
                _editorOpen = false;
                _dirty = true;
            }, 20);
        }

        private void EditorAdd(string id)
        {
            if (_edCards.Count(c => c == id) >= FormatConfig.Standard().CopyLimit) return;
            _edCards.Add(id);
            _edNote = null;
            _dirty = true;
        }

        private void EditorRemove(string id)
        {
            if (!_edCards.Remove(id)) return;
            _edNote = null;
            _dirty = true;
        }

        /// <summary>The big card beside the one under the mouse (on the other half of the screen).</summary>
        private void EditorZoom(string id, float nearX)
        {
            if (_edZoom != null) Destroy(_edZoom.gameObject);
            _edZoom = null;
            if (id == null) return;
            const float zw = 330f, zh = 462f;
            float zx = nearX < Ui.Width / 2f ? nearX + 100f : nearX - zw - 100f;
            _edZoom = Ui.Rect(_overlay, "EditorZoom", Mathf.Clamp(zx, 10f, Ui.Width - zw - 10f), 300, zw, zh);
            CardFaces.Build(_edZoom, ViewOf(id, _snap.Viewer), FaceStyle.Zoom);
            foreach (var g in _edZoom.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        }

        private void EditorSave()
        {
            var custom = CardPool.PrototypeDecks().Where(d => d.Custom).ToList();
            string desc = "Your deck (" + _edCards.Count + " cards), led by " + (_edDweller != null ? _db.Get(_edDweller).Name : "?") + ".";
            var deck = new CardPool.DeckList
            {
                Id = _edId ?? "custom_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"), Name = _edName, TavernDweller = _edDweller,
                Description = desc, Cards = new List<string>(_edCards), Custom = true,
            };
            int at = custom.FindIndex(d => d.Id == deck.Id);
            if (at >= 0) custom[at] = deck;
            else custom.Add(deck);
            try
            {
                CardPool.SaveCustomDecks(custom);
                _edId = deck.Id;
                _edNote = "Saved. " + (EditorProblem() == null ? "Pick it in the menu to play it." : "Not legal yet: " + EditorProblem());
            }
            catch (Exception e) { _edNote = "Couldn't save: " + e.Message; }
            _dirty = true;
        }

        private void EditorDelete()
        {
            if (_edId == null) return;
            var custom = CardPool.PrototypeDecks().Where(d => d.Custom && d.Id != _edId).ToList();
            try
            {
                CardPool.SaveCustomDecks(custom);
                _edId = null;
                _edNote = "Deleted (still here unsaved).";
            }
            catch (Exception e) { _edNote = "Couldn't delete: " + e.Message; }
            for (int s = 0; s < 2; s++) _deck[s] = Mathf.Clamp(_deck[s], 0, CardPool.PrototypeDecks().Count - 1);
            _dirty = true;
        }

        /// <summary>Click / right-click / hover callbacks for a card or row in the deck editor.</summary>
        private sealed class EditorCard : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
        {
            private Action _click, _rightClick;
            private Action<bool> _hover;

            public static void Attach(GameObject go, Action click, Action rightClick, Action<bool> hover)
            {
                if (go.GetComponent<Graphic>() is Graphic g) g.raycastTarget = true;
                else go.AddComponent<Image>().color = new Color(0, 0, 0, 0);
                var c = go.AddComponent<EditorCard>();
                c._click = click;
                c._rightClick = rightClick;
                c._hover = hover;
            }

            public void OnPointerClick(PointerEventData e)
            {
                if (e.button == PointerEventData.InputButton.Right) _rightClick?.Invoke();
                else if (e.button == PointerEventData.InputButton.Left) _click?.Invoke();
            }

            public void OnPointerEnter(PointerEventData e) => _hover?.Invoke(true);
            public void OnPointerExit(PointerEventData e) => _hover?.Invoke(false);
        }
    }
}
