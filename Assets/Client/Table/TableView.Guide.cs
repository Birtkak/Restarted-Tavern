using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Client.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// The Tavern Guide (user, 2026-10-10): the in-game encyclopedia (<see cref="TavernGuide"/>). Search on the left, the
    /// page on the right: screenshots (Resources/Guide, several play as a slideshow), the explanation, real example cards
    /// and links to related pages. It has its own layer, which the table's redraws don't touch, so the search field keeps
    /// its focus while typing and the list filters live. The game waits while it's open.
    /// </summary>
    public sealed partial class TableView
    {
        private RectTransform _guideLayer;
        private RectTransform _guideList, _guidePage;
        private bool _guideOpen;
        private string _guideQuery = "";
        private string _guideAtStart;
        private GuideEntry _guideEntry;
        private readonly Dictionary<string, Sprite> _guideSprites = new Dictionary<string, Sprite>();

        // The page's slideshow.
        private Image _guideImage;
        private TMPro.TextMeshProUGUI _guideCaption;
        private readonly List<Image> _guideDots = new List<Image>();
        private int _guideFrame;
        private float _guideFrameAt;
        private const float GuideFrameSeconds = 2.4f;

        private const float GuideListX = 60f, GuideListW = 440f, GuidePageX = 540f;

        /// <summary>Opens the guide: on the page with this id, or searching for this text (empty: the first page).</summary>
        private void OpenGuide(string idOrQuery = "")
        {
            _picker.Cancel();
            _guideOpen = true;
            var direct = TavernGuide.Get(idOrQuery ?? "");
            _guideQuery = direct != null ? "" : idOrQuery ?? "";
            BuildGuide();
            ShowGuideEntry(direct ?? TavernGuide.Search(_guideQuery).FirstOrDefault() ?? TavernGuide.Entries[0]);
            _dirty = true;
        }

        private void CloseGuide()
        {
            _guideOpen = false;
            _guideEntry = null;
            Ui.Clear(_guideLayer);
            _nextBot = Time.unscaledTime + 0.4f / _speed;
            _dirty = true;
        }

        private void BuildGuide()
        {
            Ui.Clear(_guideLayer);
            var bg = Ui.FillPanel(_guideLayer, "GuideBg", Ui.Hex("#0C1018"), 0f, raycast: true);
            var glow = Ui.FillPanel(bg.transform, "Glow", Ui.Hex("#2C3650") * new Color(1, 1, 1, 0.55f));
            glow.sprite = Ui.GradientSprite;
            Ui.Tmp(_guideLayer, "TAVERN GUIDE", GuideListX, 22f, 700f, 60f, 46f, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, outline: true);
            Ui.Tmp(_guideLayer, "Every rule and mechanic of Restarted Tavern. Search, or browse by topic.", GuideListX + 2f, 78f, 900f, 30f, 18f,
                Ui.Cream * new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft, FontStyle.Italic);
            Ui.Button(_guideLayer, "Report bug", Ui.Width - 400f, 30f, 150f, 46f, Ui.Hex("#7A2A20"), OpenBugReport, 16);
            Ui.Button(_guideLayer, "Close (Esc)", Ui.Width - 230f, 30f, 170f, 46f, ContextOn, CloseGuide, 18);
            Ui.Panel(_guideLayer, "Rule", GuideListX, 116f, Ui.Width - 2f * GuideListX, 2f, Ui.Gold * new Color(1, 1, 1, 0.45f));

            // Search: filters on every keystroke (only the list and the page are rebuilt, not the field).
            var box = Ui.Panel(_guideLayer, "Search", GuideListX, 134f, GuideListW, 54f, PanelColor, raycast: true);
            Ui.Frame(box.transform, "ring", Ui.Gold * new Color(0.8f, 0.8f, 0.8f, 1f), 16f).raycastTarget = false;
            var field = AddTextField(box, _guideQuery, "Search: flying, gold, block, chain...", 40, _ => { });
            field.onValueChanged.AddListener(v =>
            {
                _guideQuery = v ?? "";
                var hits = TavernGuide.Search(_guideQuery);
                RebuildGuideList(hits);
                if (hits.Count > 0 && !hits.Contains(_guideEntry)) ShowGuideEntry(hits[0], rebuildList: false);
            });
            field.ActivateInputField();

            // The list: a scroll view under the search box.
            var view = Ui.Panel(_guideLayer, "ListView", GuideListX, 200f, GuideListW, 850f, new Color(0, 0, 0, 0.25f), raycast: true);
            view.gameObject.AddComponent<RectMask2D>();
            _guideList = Ui.Rect(view.transform, "List", 0f, 0f, GuideListW, 100f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = _guideList;
            scroll.viewport = (RectTransform)view.transform;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            _guidePage = Ui.Rect(_guideLayer, "Page", 0f, 0f, Ui.Width, Ui.Height);
            RebuildGuideList(TavernGuide.Search(_guideQuery));
        }

        private void RebuildGuideList(List<GuideEntry> entries)
        {
            Ui.Clear(_guideList);
            float y = 6f;
            bool grouped = string.IsNullOrWhiteSpace(_guideQuery);
            string category = null;
            if (entries.Count == 0)
            {
                Ui.Tmp(_guideList, "Nothing found for \"" + _guideQuery.Trim() + "\".\nTry another word: attack, damage, Gold...", 14f, y, GuideListW - 28f, 80f, 18f,
                    Ui.Cream * new Color(1, 1, 1, 0.6f), TextAnchor.UpperLeft, FontStyle.Italic);
                y += 90f;
            }
            foreach (var e in entries)
            {
                if (grouped && e.Category != category)
                {
                    category = e.Category;
                    Ui.Tmp(_guideList, category.ToUpperInvariant(), 14f, y + 6f, GuideListW - 28f, 30f, 17f, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                    y += 40f;
                }
                bool on = e == _guideEntry;
                var row = Ui.Panel(_guideList, "Entry " + e.Id, 8f, y, GuideListW - 16f, 42f, on ? ContextOn : PanelColor, raycast: true);
                row.sprite = Ui.GradientSprite;
                Ui.Tmp(row.transform, e.Title, 16f, 0f, GuideListW - 140f, 42f, 19f, on ? Ui.Hex("#1A1408") : Ui.Cream, TextAnchor.MiddleLeft, on ? FontStyle.Bold : FontStyle.Normal);
                if (!grouped)
                    Ui.Tmp(row.transform, e.Category, GuideListW - 140f, 0f, 116f, 42f, 13f, on ? Ui.Hex("#1A1408") : Ui.Gold * new Color(1, 1, 1, 0.7f), TextAnchor.MiddleRight, FontStyle.Italic);
                var entry = e;
                EditorCard.Attach(row.gameObject, () => ShowGuideEntry(entry), null, null);
                y += 48f;
            }
            _guideList.sizeDelta = new Vector2(GuideListW, y + 10f);
        }

        private void ShowGuideEntry(GuideEntry e, bool rebuildList = true)
        {
            _guideEntry = e;
            Ui.Clear(_guidePage);
            _guideDots.Clear();
            _guideImage = null;
            _guideCaption = null;
            if (rebuildList) RebuildGuideList(TavernGuide.Search(_guideQuery));
            if (e == null) return;

            Ui.Tmp(_guidePage, e.Title.ToUpperInvariant(), GuidePageX, 134f, 900f, 54f, 40f, Ui.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, outline: true);
            var chip = Ui.Panel(_guidePage, "Category", GuidePageX + 920f, 146f, 180f, 32f, Ui.Gold * new Color(0.35f, 0.35f, 0.35f, 1f));
            Ui.FillTmp(chip.transform, e.Category, 15f, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Left: the pictures (a slideshow when there are several) above the text; the text takes their place when there are none.
            const float w = 880f;
            float textY = 206f;
            if (e.Frames.Length > 0)
            {
                const float h = 495f;
                var frame = Ui.Panel(_guidePage, "Visual", GuidePageX, 206f, w, h, Ui.Hex("#05070B"));
                Ui.Frame(frame.transform, "ring", Ui.Gold, 18f).raycastTarget = false;
                _guideImage = Ui.Panel(frame.transform, "Shot", 8f, 8f, w - 16f, h - 16f, Color.white);
                _guideImage.preserveAspect = true;
                _guideCaption = Ui.Tmp(_guidePage, "", GuidePageX, 206f + h + 6f, w - 120f, 34f, 17f, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Italic);
                if (e.Frames.Length > 1)
                    for (int i = 0; i < e.Frames.Length; i++)
                    {
                        int ii = i;
                        var dot = Ui.Circle(_guidePage, "Dot", GuidePageX + w - 30f - (e.Frames.Length - 1 - i) * 30f, 206f + h + 12f, 20f, Ui.Cream, raycast: true);
                        EditorCard.Attach(dot.gameObject, () => SetGuideFrame(ii), null, null);
                        _guideDots.Add(dot);
                    }
                SetGuideFrame(0);
                textY = 206f + h + 50f;
            }
            var text = Ui.Tmp(_guidePage, e.Text, GuidePageX, textY, w, 1050f - textY, e.Frames.Length > 0 ? 20f : 24f, Ui.Cream, TextAnchor.UpperLeft);
            text.fontSizeMin = 14f;
            text.lineSpacing = 4f;

            // Right: example cards, then related pages.
            const float sideX = GuidePageX + w + 40f, sideW = Ui.Width - 60f - (GuidePageX + w + 40f);
            float y = 206f;
            if (e.Cards.Length > 0)
            {
                Ui.Tmp(_guidePage, e.Cards.Length > 1 ? "EXAMPLE CARDS" : "EXAMPLE CARD", sideX, y, sideW, 30f, 17f, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                y += 38f;
                const float cw = 262f, ch = 366f, scale = 0.76f;
                for (int i = 0; i < e.Cards.Length && i < 2; i++)
                {
                    if (!_db.Contains(e.Cards[i])) continue;
                    var rt = Ui.Rect(_guidePage, "Card " + e.Cards[i], sideX + i * (cw * scale + 14f), y, cw, ch);
                    rt.localScale = Vector3.one * scale;
                    CardFaces.Build(rt, ViewOf(e.Cards[i], _s.Viewer), FaceStyle.Hand);
                }
                y += ch * scale + 30f;
            }
            if (e.Related.Length > 0)
            {
                Ui.Tmp(_guidePage, "SEE ALSO", sideX, y, sideW, 30f, 17f, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                y += 38f;
                foreach (var id in e.Related)
                {
                    var other = TavernGuide.Get(id);
                    if (other == null) continue;
                    Ui.Button(_guidePage, other.Title, sideX, y, sideW, 44f, ButtonColor, () => ShowGuideEntry(other), 17);
                    y += 52f;
                }
            }
        }

        private void SetGuideFrame(int i)
        {
            if (_guideEntry == null || _guideImage == null) return;
            _guideFrame = i % _guideEntry.Frames.Length;
            _guideFrameAt = Time.unscaledTime;
            var f = _guideEntry.Frames[_guideFrame];
            var sprite = GuideSprite(f.Image);
            _guideImage.sprite = sprite;
            _guideImage.color = sprite != null ? Color.white : new Color(1, 1, 1, 0.05f);
            if (_guideCaption != null) _guideCaption.text = f.Caption;
            for (int d = 0; d < _guideDots.Count; d++)
                _guideDots[d].color = d == _guideFrame ? Ui.Gold : Ui.Cream * new Color(1, 1, 1, 0.35f);
        }

        private Sprite GuideSprite(string name)
        {
            if (!_guideSprites.TryGetValue(name, out var s))
                _guideSprites[name] = s = Resources.Load<Sprite>("Guide/" + name);
            return s;
        }

        /// <summary>Every frame: the slideshow advances, and the guide hides while the bug report dialog is up.</summary>
        private void UpdateGuide()
        {
            if (_guideLayer != null) _guideLayer.gameObject.SetActive(_guideOpen && !_bugOpen);
            if (!_guideOpen || _guideEntry == null || _guideEntry.Frames.Length < 2) return;
            if (Time.unscaledTime - _guideFrameAt > GuideFrameSeconds) SetGuideFrame(_guideFrame + 1);
        }
    }
}
