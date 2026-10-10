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
    /// The deck editor (from the main menu), laid out like Hearthstone's collection (user, 2026-10-10) in the LoR look:
    /// faction tabs over a book of cards (4 × 2 a page, arrows to turn), mana / type / search filters below, and on the
    /// right "My decks" (every deck as a tile, New deck), then the deck being built (LoR rows: cost, name, copies).
    /// New deck: pick a Tavern Dweller first (its factions plus neutral fill the book), like choosing a class.
    /// Saved decks go to the player's own deck file (<see cref="CardPool.CustomDecksFile"/>) and show up in the menu.
    /// </summary>
    public sealed partial class TableView
    {
        private const int EditorColumns = 4, EditorRows = 2;
        private const float PoolW = 230f, PoolH = 322f;
        private const float BookX = 20f, BookY = 84f, BookW = 1420f, BookH = 880f;
        private const float SideX = 1462f, SideW = 438f;

        private enum EditorMode { Decks, PickDweller, Edit }

        private bool _editorOpen;
        private EditorMode _edMode;
        private string _edId;              // the custom deck being edited (null: a new one)
        private string _edName = "My deck";
        private string _edDweller;
        private readonly List<string> _edCards = new List<string>();
        private string _edFaction;          // null: every faction the book shows
        private int _edType;                // 0 all, 1 creatures, 2 spells, 3 other permanents
        private int _edCost = -1;           // -1 all, 0-6, 7 = 7+
        private string _edSearch = "";
        private int _edPage;
        private string _edNote;
        private RectTransform _edZoom;

        private static readonly Color BookColor = Ui.Hex("#1A2030");
        private static readonly Color PanelColor = Ui.Hex("#141A26");

        /// <summary>Opens the editor on "My decks"; with a deck (-editor), straight into editing it.</summary>
        private void OpenEditor(CardPool.DeckList edit = null)
        {
            _editorOpen = true;
            _edMode = EditorMode.Decks;
            _edFaction = null;
            _edPage = 0;
            _edNote = null;
            _dirty = true;
            if (edit != null) EditDeck(edit);
        }

        private void EditDeck(CardPool.DeckList from)
        {
            _edMode = EditorMode.Edit;
            _edCards.Clear();
            _edId = from != null && from.Custom ? from.Id : null;
            _edName = from == null ? "My deck" : from.Custom ? from.Name : from.Name + " (copy)";
            if (from != null)
            {
                _edDweller = from.TavernDweller;
                _edCards.AddRange(from.Cards);
            }
            _edFaction = null;
            _edPage = 0;
            _edNote = null;
            _dirty = true;
        }

        private static bool Collectible(CardDefinition d) => !d.IsToken && !d.IsTavernDweller;

        private bool Allowed(CardDefinition d) =>
            d.Faction == "neutral" || _edDweller != null && Array.IndexOf(_db.Get(_edDweller).TavernDwellerFactions, d.Faction) >= 0;

        /// <summary>The factions the book shows: the Tavern Dweller's plus neutral while editing, every faction otherwise.</summary>
        private List<string> EditorFactions()
        {
            if (_edMode == EditorMode.Edit && _edDweller != null)
                return _db.Get(_edDweller).TavernDwellerFactions.Concat(new[] { "neutral" }).Distinct().ToList();
            return _db.All.Where(Collectible).Select(d => d.Faction).Distinct().OrderBy(f => f == "neutral" ? 1 : 0).ThenBy(f => f, StringComparer.Ordinal).ToList();
        }

        private List<CardDefinition> EditorPool()
        {
            if (_edMode == EditorMode.PickDweller)
                return _db.All.Where(d => d.IsTavernDweller).OrderBy(d => d.Name, StringComparer.Ordinal).ToList();
            var factions = EditorFactions();
            string q = _edSearch.Trim();
            return _db.All.Where(d => Collectible(d) && factions.Contains(d.Faction))
                .Where(d => _edFaction == null || d.Faction == _edFaction)
                .Where(d => _edType == 0 || _edType == 1 && d.IsCreature || _edType == 2 && (d.Type == CardType.Instant || d.Type == CardType.Sorcery)
                            || _edType == 3 && d.IsPermanent && !d.IsCreature)
                .Where(d => _edCost < 0 || (_edCost == 7 ? d.Cost >= 7 : d.Cost == _edCost))
                .Where(d => q.Length == 0 || d.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                            || (d.Text ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                            || d.Subtypes.Any(s => s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(d => d.Faction == "neutral" ? 1 : 0).ThenBy(d => d.Faction, StringComparer.Ordinal)
                .ThenBy(d => d.Cost).ThenBy(d => d.Name, StringComparer.Ordinal).ToList();
        }

        private string EditorProblem()
        {
            if (_edDweller == null) return "Pick a Tavern Dweller";
            if (_edCards.Count != FormatConfig.Standard().DeckSize) return _edCards.Count + " cards: a deck needs exactly " + FormatConfig.Standard().DeckSize;
            try { DeckValidator.Validate(_db, FormatConfig.Standard(), _edCards, _edDweller); }
            catch (ArgumentException e) { return e.Message; }
            return null;
        }

        private static string FactionName(string f) => CardFaces.Style(f).Name ?? f;

        // ------------------------------------------------------------------ drawing

        private void DrawEditor()
        {
            _edZoom = null;
            Ui.FillPanel(_overlay, "Editor", WoodDark, 0f, raycast: true);
            DrawFactionTabs();
            DrawBook();
            DrawFilters();
            if (_edMode == EditorMode.Edit) DrawDeckList();
            else DrawMyDecks();
        }

        /// <summary>Hearthstone's class tabs along the top of the book: one per faction (emblem, colour), All first.</summary>
        private void DrawFactionTabs()
        {
            if (_edMode == EditorMode.PickDweller) return;
            var tabs = new List<string> { null };
            tabs.AddRange(EditorFactions());
            float x = BookX + 20f;
            foreach (var f in tabs)
            {
                bool on = f == _edFaction;
                var fs = f == null ? null : CardFaces.Style(f);
                float w = f == null ? 90f : 150f;
                var tab = Ui.Panel(_overlay, "Tab", x, on ? 22f : 30f, w, on ? 62f : 54f, fs != null ? fs.Frame : ButtonColor, raycast: true);
                tab.sprite = Ui.GradientSprite;
                Ui.Frame(tab.transform, "ring", on ? Ui.Gold : Ui.Gold * new Color(0.6f, 0.6f, 0.6f, 1f), 18f).raycastTarget = false;
                Ui.FillTmp(tab.transform, f == null ? "ALL" : fs.Emblem + "\n<size=60%>" + FactionName(f) + "</size>", 20f,
                    on ? Ui.Cream : fs != null ? fs.Accent : Ui.Cream, TextAnchor.MiddleCenter, FontStyle.Bold, 6f);
                string ff = f;
                EditorCard.Attach(tab.gameObject, () => { _edFaction = ff; _edPage = 0; _dirty = true; }, null, null);
                x += w + 8f;
            }
        }

        private void DrawBook()
        {
            var book = Ui.Panel(_overlay, "Book", BookX, BookY, BookW, BookH, BookColor);
            book.sprite = Ui.GradientSprite;
            Ui.Frame(book.transform, "ring", Ui.Gold, 26f);

            var pool = EditorPool();
            int perPage = EditorColumns * EditorRows;
            int pages = Math.Max(1, (pool.Count + perPage - 1) / perPage);
            _edPage = Mathf.Clamp(_edPage, 0, pages - 1);

            // Page title plate: the faction on this page (or what we're choosing).
            string title = _edMode == EditorMode.PickDweller ? "Choose a Tavern Dweller"
                : _edPage * perPage < pool.Count ? FactionName(pool[_edPage * perPage].Faction) : "No cards match";
            var plate = Ui.Panel(_overlay, "Title", BookX + BookW / 2f - 200f, BookY + 14f, 400f, 50f, PanelColor);
            Ui.Frame(plate.transform, "ring", Ui.Gold, 18f);
            Ui.FillTmp(plate.transform, title.ToUpperInvariant(), 26f, Ui.Cream, TextAnchor.MiddleCenter, FontStyle.Bold, 6f);

            int limit = FormatConfig.Standard().CopyLimit;
            float gap = 70f;
            float x0 = BookX + (BookW - (EditorColumns * PoolW + (EditorColumns - 1) * gap)) / 2f;
            for (int i = 0; i < perPage && _edPage * perPage + i < pool.Count; i++)
            {
                var def = pool[_edPage * perPage + i];
                float x = x0 + (i % EditorColumns) * (PoolW + gap), y = BookY + 80f + (i / EditorColumns) * (PoolH + 64f);
                var rt = Ui.Rect(_overlay, "Pool " + def.Name, x, y, PoolW, PoolH);
                CardFaces.Build(rt, ViewOf(def.Id, _snap.Viewer), FaceStyle.Hand);
                string id = def.Id;
                if (_edMode == EditorMode.PickDweller)
                {
                    EditorCard.Attach(rt.gameObject, () =>
                    {
                        _edDweller = id;
                        EditDeck(null);
                    }, null, null);
                    continue;
                }
                int have = _edCards.Count(c => c == id);
                bool editing = _edMode == EditorMode.Edit;
                // Hearthstone's copies plate under the card; LoR dims what you can't add any more.
                if (editing)
                {
                    var plateRt = Ui.Panel(_overlay, "Copies", x + PoolW / 2f - 40f, y + PoolH + 6f, 80f, 30f, PanelColor);
                    Ui.Frame(plateRt.transform, "ring", have > 0 ? Ui.Gold : Ui.Gold * new Color(0.5f, 0.5f, 0.5f, 1f), 14f);
                    Ui.FillTmp(plateRt.transform, have + " / " + limit, 18f, have >= limit ? Ui.Hex("#FF9070") : Ui.Cream, TextAnchor.MiddleCenter, FontStyle.Bold, 4f);
                    if (have >= limit) rt.gameObject.AddComponent<CanvasGroup>().alpha = 0.45f;
                }
                EditorCard.Attach(rt.gameObject, editing ? (Action)(() => EditorAdd(id)) : null, editing ? (Action)(() => EditorRemove(id)) : null,
                    on => EditorZoom(on ? id : null, x + PoolW / 2f));
            }

            // Turn the page: big arrows on the book's sides.
            if (_edPage > 0)
                Ui.Button(_overlay, "<", BookX + 18f, BookY + BookH / 2f - 60f, 64f, 120f, ButtonColor, () => { _edPage--; _dirty = true; }, 40);
            if (_edPage < pages - 1)
                Ui.Button(_overlay, ">", BookX + BookW - 82f, BookY + BookH / 2f - 60f, 64f, 120f, ButtonColor, () => { _edPage++; _dirty = true; }, 40);
            Ui.Tmp(_overlay, "Page " + (_edPage + 1) + " / " + pages + (_edMode == EditorMode.Edit ? "   ·   click adds a copy, right-click takes one out" : ""),
                BookX, BookY + BookH - 50f, BookW, 30f, 18f, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Italic);
        }

        /// <summary>Below the book: mana gems (All, 0-7+), card type, and a search box (applies on Enter).</summary>
        private void DrawFilters()
        {
            if (_edMode == EditorMode.PickDweller) return;
            float y = BookY + BookH + 18f, x = BookX + 10f;
            for (int c = -1; c <= 7; c++)
            {
                int cc = c;
                bool on = c == _edCost;
                float d = c < 0 ? 70f : 54f;
                var gem = Ui.Circle(_overlay, "Mana", x, y + 3f, d, 54f, on ? Ui.Gold : CardFaces.CostColor, raycast: true);
                Ui.AddOutline(gem.gameObject, on ? Ui.Cream : new Color(0, 0, 0, 0.8f), 2f);
                Ui.FillTmp(gem.transform, c < 0 ? "ALL" : c == 7 ? "7+" : c.ToString(), c < 0 ? 18f : 24f, on ? Ui.Hex("#1A1408") : Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 0f, outline: !on);
                EditorCard.Attach(gem.gameObject, () => { _edCost = cc; _edPage = 0; _dirty = true; }, null, null);
                x += d + 8f;
            }
            x += 20f;
            string[] types = { "Any type", "Creatures", "Spells", "Other" };
            for (int t = 0; t < types.Length; t++)
            {
                int tt = t;
                Ui.Button(_overlay, types[t], x, y + 6f, 124f, 48f, t == _edType ? ContextOn : ButtonColor, () => { _edType = tt; _edPage = 0; _dirty = true; }, 16);
                x += 130f;
            }
            x += 14f;
            var box = Ui.Panel(_overlay, "Search", x, y + 6f, BookX + BookW - x, 48f, PanelColor, raycast: true);
            Ui.Frame(box.transform, "ring", Ui.Gold * new Color(0.7f, 0.7f, 0.7f, 1f), 16f).raycastTarget = false;
            AddTextField(box, _edSearch, "Search name, text, type... (Enter)", 40, v =>
            {
                _edSearch = v ?? "";
                _edPage = 0;
                _dirty = true;
            });
        }

        /// <summary>
        /// A text field in <paramref name="box"/> (uGUI InputField). It reports on Enter / leaving it: a redraw while
        /// typing would rebuild the field and lose the focus.
        /// </summary>
        private static InputField AddTextField(Image box, string value, string placeholder, int limit, Action<string> onDone)
        {
            var text = Ui.FillLabel(box.transform, value, 20, Ui.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, 12f);
            text.raycastTarget = false;
            text.supportRichText = false;
            var hint = Ui.FillLabel(box.transform, placeholder, 18, Ui.Cream * new Color(1, 1, 1, 0.4f), TextAnchor.MiddleLeft, FontStyle.Italic, 12f);
            var input = box.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = hint;
            input.text = value;
            input.characterLimit = limit;
            input.onEndEdit.AddListener(v => onDone(v));
            return input;
        }

        /// <summary>Hearthstone's "My decks": every deck as a tile (a prototype one opens as a copy), New deck, Back.</summary>
        private void DrawMyDecks()
        {
            var panel = Ui.Panel(_overlay, "Side", SideX, 20f, SideW, 1040f, PanelColor);
            panel.sprite = Ui.GradientSprite;
            Ui.Frame(panel.transform, "ring", Ui.Gold, 26f);
            var head = Ui.Panel(_overlay, "Head", SideX + 60f, 34f, SideW - 120f, 50f, BookColor);
            Ui.Frame(head.transform, "ring", Ui.Gold, 18f);
            Ui.FillTmp(head.transform, _edMode == EditorMode.PickDweller ? "NEW DECK" : "MY DECKS", 26f, Ui.Cream, TextAnchor.MiddleCenter, FontStyle.Bold, 6f);

            var decks = CardPool.PrototypeDecks();
            float y = 100f;
            if (_edMode == EditorMode.Decks)
            {
                foreach (var d in decks.Take(11))
                {
                    var dweller = _db.Get(d.TavernDweller);
                    var fs = CardFaces.Style(dweller.TavernDwellerFactions.FirstOrDefault());
                    var tile = Ui.Panel(_overlay, "Deck " + d.Name, SideX + 24f, y, SideW - 48f, 62f, fs.Frame, raycast: true);
                    tile.sprite = Ui.GradientSprite;
                    Ui.Frame(tile.transform, "ring", d.Custom ? Ui.Gold : Ui.Gold * new Color(0.65f, 0.65f, 0.65f, 1f), 18f).raycastTarget = false;
                    Ui.Tmp(tile.transform, d.Name, 14f, 6f, SideW - 76f, 30f, 22f, Ui.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
                    Ui.Tmp(tile.transform, dweller.Name + (d.Custom ? "  ·  yours" : "  ·  opens a copy"), 14f, 34f, SideW - 76f, 22f, 15f, fs.Accent, TextAnchor.MiddleLeft, FontStyle.Italic);
                    var deck = d;
                    EditorCard.Attach(tile.gameObject, () => EditDeck(deck), null, null);
                    y += 70f;
                }
                Ui.Button(_overlay, "NEW DECK", SideX + 24f, y + 6f, SideW - 48f, 62f, ContextOn, () =>
                {
                    _edMode = EditorMode.PickDweller;
                    _edPage = 0;
                    _dirty = true;
                }, 24);
            }
            Ui.Tmp(_overlay, decks.Count(d => d.Custom) + " of your own", SideX + 30f, 980f, 180f, 50f, 18f, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Button(_overlay, _edMode == EditorMode.PickDweller ? "CANCEL" : "BACK", SideX + SideW - 200f, 976f, 170f, 58f, ButtonColor, () =>
            {
                if (_edMode == EditorMode.PickDweller) _edMode = EditorMode.Decks;
                else _editorOpen = false;
                _edPage = 0;
                _dirty = true;
            }, 22);
        }

        /// <summary>The deck being built: Tavern Dweller, name, LoR rows (cost, name, copies), count, Done / Delete.</summary>
        private void DrawDeckList()
        {
            var panel = Ui.Panel(_overlay, "Side", SideX, 20f, SideW, 1040f, PanelColor);
            panel.sprite = Ui.GradientSprite;
            Ui.Frame(panel.transform, "ring", Ui.Gold, 26f);
            float x = SideX + 22f, w = SideW - 44f;

            // The Tavern Dweller tile (hover to read it).
            var dw = _db.Get(_edDweller);
            var dfs = CardFaces.Style(dw.TavernDwellerFactions.FirstOrDefault());
            var dTile = Ui.Panel(_overlay, "Dweller", x, 34f, w, 56f, dfs.Frame, raycast: true);
            dTile.sprite = Ui.GradientSprite;
            Ui.Frame(dTile.transform, "ring", Ui.Gold, 18f).raycastTarget = false;
            Ui.Tmp(dTile.transform, dw.Name, 14f, 4f, w - 28f, 28f, 22f, Ui.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Tmp(dTile.transform, string.Join(" + ", dw.TavernDwellerFactions.Select(FactionName)) + " + Neutral", 14f, 30f, w - 28f, 22f, 15f, dfs.Accent, TextAnchor.MiddleLeft, FontStyle.Italic);
            string dId = dw.Id;
            EditorCard.Attach(dTile.gameObject, null, null, on => EditorZoom(on ? dId : null, SideX));

            // Deck name.
            var nameBox = Ui.Panel(_overlay, "Name", x, 98f, w, 44f, BookColor, raycast: true);
            Ui.Frame(nameBox.transform, "ring", Ui.Gold * new Color(0.7f, 0.7f, 0.7f, 1f), 16f).raycastTarget = false;
            AddTextField(nameBox, _edName, "Deck name...", 32, v => { _edName = string.IsNullOrWhiteSpace(v) ? "My deck" : v.Trim(); });

            // Rows by cost: a blue cost gem, the name on a faction bar, the copies in gold. Click (or right-click) takes one out.
            var rows = _edCards.GroupBy(id => id).Select(g => (def: _db.Get(g.Key), n: g.Count()))
                .OrderBy(r => r.def.Cost).ThenBy(r => r.def.Name, StringComparer.Ordinal).ToList();
            float rowH = Mathf.Min(40f, 700f / Math.Max(1, rows.Count));
            for (int i = 0; i < rows.Count; i++)
            {
                var (def, n) = rows[i];
                float y = 152f + i * rowH;
                var fs = CardFaces.Style(def.Faction);
                bool ok = Allowed(def);
                var row = Ui.Panel(_overlay, "Row " + def.Name, x + rowH * 0.5f, y + 2f, w - rowH * 0.5f - 46f, rowH - 4f, ok ? fs.Frame : Ui.Hex("#802020"), raycast: true);
                row.sprite = Ui.GradientSprite;
                Ui.AddOutline(row.gameObject, Ui.Gold * new Color(1, 1, 1, 0.8f), 1f);
                Ui.Tmp(row.transform, def.Name + (ok ? "" : "  (outside the factions)"), rowH * 0.6f, 0f, w - rowH * 1.1f - 56f, rowH - 4f, Mathf.Min(20f, rowH * 0.55f), Ui.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
                var gem = Ui.Circle(_overlay, "Cost", x, y, rowH, rowH, CardFaces.CostColor);
                Ui.AddOutline(gem.gameObject, Ui.Gold, 2f);
                Ui.FillTmp(gem.transform, def.Cost.ToString(), rowH * 0.6f, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 0f, outline: true);
                var count = Ui.Panel(_overlay, "Count", x + w - 42f, y + 2f, 42f, rowH - 4f, PanelColor);
                Ui.AddOutline(count.gameObject, Ui.Gold * new Color(1, 1, 1, 0.8f), 1f);
                Ui.FillTmp(count.transform, n.ToString(), Mathf.Min(24f, rowH * 0.65f), Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                string id = def.Id;
                EditorCard.Attach(row.gameObject, () => EditorRemove(id), () => EditorRemove(id), on => EditorZoom(on ? id : null, SideX));
            }

            // Mana curve and the count, LoR's bottom bar.
            int size = FormatConfig.Standard().DeckSize;
            var curve = new int[8];
            foreach (var id in _edCards) curve[Mathf.Clamp(_db.Get(id).Cost, 0, 7)]++;
            int top = Math.Max(1, curve.Max());
            for (int c = 0; c < 8; c++)
            {
                float bh = 44f * curve[c] / top;
                Ui.Panel(_overlay, "Curve", x + 6f + c * 26f, 904f - bh, 20f, bh, CardFaces.CostColor);
                Ui.Tmp(_overlay, c == 7 ? "7+" : c.ToString(), x + 2f + c * 26f, 906f, 28f, 16f, 13f, Ui.Gold, TextAnchor.MiddleCenter);
            }
            Ui.Tmp(_overlay, _edCards.Count + " / " + size, x + 230f, 862f, w - 230f, 40f, 34f,
                _edCards.Count == size ? Ui.Hex("#80E080") : Ui.Hex("#FF9070"), TextAnchor.MiddleRight, FontStyle.Bold);
            Ui.Tmp(_overlay, "CARDS", x + 230f, 900f, w - 230f, 20f, 15f, Ui.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            string problem = EditorProblem();
            if (_edNote != null || problem != null)
                Ui.Tmp(_overlay, _edNote ?? problem, x, 928f, w, 40f, 15f, _edNote != null ? Ui.Hex("#80E080") : Ui.Hex("#FF9070"), TextAnchor.MiddleLeft);

            Ui.Button(_overlay, "DONE", x, 976f, 150f, 58f, ContextOn, () => { if (EditorSave()) { _edMode = EditorMode.Decks; _dirty = true; } }, 24);
            Ui.Button(_overlay, "SAVE", x + 158f, 976f, 110f, 58f, ButtonColor, () => EditorSave(), 20);
            Ui.Button(_overlay, "DELETE", x + 276f, 976f, w - 276f, 58f, Ui.Hex("#5A2A30"), EditorDelete, 18, _edId != null);
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

        /// <summary>The big card beside the one under the mouse (towards the middle of the screen).</summary>
        private void EditorZoom(string id, float nearX)
        {
            if (_edZoom != null) Destroy(_edZoom.gameObject);
            _edZoom = null;
            if (id == null) return;
            const float zw = 360f, zh = 504f;
            float zx = nearX < Ui.Width / 2f ? nearX + 140f : nearX - zw - 30f;
            _edZoom = Ui.Rect(_overlay, "EditorZoom", Mathf.Clamp(zx, 10f, Ui.Width - zw - 10f), 260, zw, zh);
            CardFaces.Build(_edZoom, ViewOf(id, _snap.Viewer), FaceStyle.Zoom);
            foreach (var g in _edZoom.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        }

        private bool EditorSave()
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
                string problem = EditorProblem();
                _edNote = "Saved. " + (problem == null ? "Pick it in the menu to play it." : "Not legal yet: " + problem);
                _dirty = true;
                return true;
            }
            catch (Exception e)
            {
                _edNote = "Couldn't save: " + e.Message;
                _dirty = true;
                return false;
            }
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

        /// <summary>Click / right-click / hover callbacks for a card, tile or row in the deck editor.</summary>
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
