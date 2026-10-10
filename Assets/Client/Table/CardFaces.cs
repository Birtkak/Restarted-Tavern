using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace RestartedTavern.Client.Table
{
    public enum FaceStyle
    {
        /// <summary>The full card in hand (MTGA hand, LoR card layout): cost, art, name, text, Power / Health.</summary>
        Hand,
        /// <summary>A LoR unit on the board: portrait crop, big Power / Health, keyword icons, damage in red.</summary>
        Unit,
        /// <summary>The hover / right-click zoom: the full card, large.</summary>
        Zoom,
    }

    /// <summary>
    /// Placeholder card frames (CLIENT_DESIGN §1: faction colours, cost / Power / Health gems, rarity, name, text
    /// box, the faction emblem in the art window). Art drops in by card id: Resources/CardArt/&lt;card id&gt;.png.
    /// </summary>
    public static class CardFaces
    {
        public sealed class FactionStyle
        {
            public Color Frame;
            public Color Accent;
            public string Emblem;
        }

        // Proposal (NEXT_CLIENT step 3 asks the user to confirm).
        private static readonly Dictionary<string, FactionStyle> Factions = new Dictionary<string, FactionStyle>
        {
            ["shadow_money_wizards"] = new FactionStyle { Frame = Ui.Hex("#4A2A6A"), Accent = Ui.Hex("#D9B44A"), Emblem = "SMW" },
            ["goobers"] = new FactionStyle { Frame = Ui.Hex("#9A2D20"), Accent = Ui.Hex("#F0A050"), Emblem = "GOB" },
            ["sensationalists"] = new FactionStyle { Frame = Ui.Hex("#241B2E"), Accent = Ui.Hex("#A070E0"), Emblem = "SEN" },
            ["evergrowing_wild"] = new FactionStyle { Frame = Ui.Hex("#2E6230"), Accent = Ui.Hex("#9AD86A"), Emblem = "WLD" },
            ["glitterworld"] = new FactionStyle { Frame = Ui.Hex("#156A78"), Accent = Ui.Hex("#5FF2FF"), Emblem = "GLT" },
            ["neutral"] = new FactionStyle { Frame = Ui.Hex("#5E5A55"), Accent = Ui.Hex("#C8C0B0"), Emblem = "NEU" },
        };

        private static readonly FactionStyle Dweller = new FactionStyle { Frame = Ui.Hex("#7A5A30"), Accent = Ui.Hex("#F0D080"), Emblem = "TD" };

        public static FactionStyle Style(string faction) =>
            faction != null && Factions.TryGetValue(faction, out var s) ? s : Dweller;

        public static readonly Color Back = Ui.Hex("#3A2416");
        public static readonly Color BackAccent = Ui.Hex("#C89A50");
        public static readonly Color PowerColor = Ui.Hex("#E0B040");
        public static readonly Color HealthColor = Ui.Hex("#C03030");
        public static readonly Color CostColor = Ui.Hex("#2F6FD0");
        public static readonly Color Damaged = Ui.Hex("#FF4040");
        public static readonly Color Buffed = Ui.Hex("#60E060");

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return Ui.Hex("#A8C8E8");
                case Rarity.Rare: return Ui.Hex("#F0C040");
                case Rarity.Legendary: return Ui.Hex("#FF7A20");
                default: return Ui.Hex("#9A9A9A");
            }
        }

        public static void BuildBack(RectTransform root)
        {
            Ui.FillPanel(root, "Back", Back, 0f, raycast: true);
            Ui.FillPanel(root, "Inner", BackAccent * new Color(1f, 1f, 1f, 0.35f), 8f);
            Ui.FillLabel(root, "RT", 22, BackAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>Builds the face of a visible card into <paramref name="root"/> (sized by the caller).</summary>
        public static void Build(RectTransform root, CardView v, FaceStyle style)
        {
            var fs = v.Type == CardType.TavernDweller ? Dweller : Style(v.Faction);
            float w = root.rect.width, h = root.rect.height;
            float k = h / 210f; // scale relative to a 150×210 hand card

            bool unit = style == FaceStyle.Unit;
            bool creature = v.Type == CardType.Creature;
            bool dweller = v.Type == CardType.TavernDweller;
            // Token units on the table are domes, a half circle with a flat underside (playtest 2026-10-10_144700).
            bool dome = unit && creature && v.IsToken;

            var frame = Ui.FillPanel(root, "Frame", fs.Frame, 0f, raycast: true);
            var edge = Ui.FillPanel(root, "Edge", fs.Accent * new Color(1f, 1f, 1f, 0.5f), 3f * k);
            var body = Ui.FillPanel(root, "Body", fs.Frame * 0.85f + new Color(0, 0, 0, 0.15f), 5f * k);
            if (dome)
            {
                foreach (var img in new[] { frame, edge, body }) img.sprite = Ui.DomeSprite;
                frame.alphaHitTestMinimumThreshold = 0.5f;
            }

            // Art window: a portrait crop for units, the upper half for full cards. In a dome it starts where the arc is
            // as wide as the art, so its corners stay inside.
            float artTop = unit ? 6f * k : 8f * k, artH = unit ? h - 6f * k - 70f * k : FullCardArtHeight(v, w, h, k, creature);
            if (dome)
            {
                float r = w / 2f, half = r - 8f * k;
                float top = r - Mathf.Sqrt(r * r - half * half);
                artH -= top - artTop;
                artTop = top;
            }
            var art = Ui.Rect(root, "Art", 8f * k, artTop, w - 16f * k, artH);
            BuildArt(art, v, fs);

            if (unit)
            {
                // Above the Power / Health gems, so they never cover the name.
                var nameBar = Ui.Panel(root, "NameBar", 4f * k, h - 70f * k, w - 8f * k, 20f * k, new Color(0, 0, 0, 0.55f));
                Ui.FillLabel(nameBar.transform, v.Name, Mathf.RoundToInt(15 * k), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 2f).resizeTextMinSize = 7;
            }
            else
            {
                float nameY = artTop + artH + 2f * k;
                var nameBar = Ui.Panel(root, "NameBar", 6f * k, nameY, w - 12f * k, 24f * k, new Color(0, 0, 0, 0.5f));
                Ui.FillLabel(nameBar.transform, v.Name, Mathf.RoundToInt(16 * k), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 2f).resizeTextMinSize = 7;

                string typeLine = dweller ? "Tavern Dweller" : v.Type + (v.Subtypes != null && v.Subtypes.Length > 0 ? " · " + string.Join(" ", v.Subtypes) : "");
                Ui.Label(root, typeLine, 8f * k, nameY + 25f * k, w - 16f * k, 16f * k, Mathf.RoundToInt(12 * k),
                    fs.Accent, TextAnchor.MiddleCenter, FontStyle.Italic);

                float textY = nameY + 42f * k, textH = h - textY - (creature ? 34f * k : 10f * k);
                var box = Ui.Panel(root, "TextBox", 8f * k, textY, w - 16f * k, textH, new Color(0.95f, 0.9f, 0.8f, 0.92f));
                var t = Ui.FillLabel(box.transform, CardText(v), Mathf.RoundToInt(13 * k), new Color(0.15f, 0.1f, 0.05f),
                    TextAnchor.UpperLeft, FontStyle.Normal, 4f * k);
                t.resizeTextMinSize = Mathf.Max(6, Mathf.RoundToInt(7 * k));

                // Rarity gem on the text box edge.
                if (!dweller && !v.IsToken)
                    Ui.Panel(root, "Rarity", w / 2f - 6f * k, textY - 6f * k, 12f * k, 12f * k, RarityColor(v.Rarity));
            }

            // Units in play show no cost (LoR).
            if (!dweller && !(unit && creature)) Gem(root, "Cost", 0f, 0f, 34f * k, CostColor, v.Cost.ToString(),
                v.Cost < v.PrintedCost ? Buffed : v.Cost > v.PrintedCost ? Damaged : Color.white, k);

            if (creature)
            {
                float g = unit ? 40f * k : 34f * k;
                var pColor = v.Power > v.PrintedPower ? Buffed : v.Power < v.PrintedPower ? Damaged : Color.white;
                var hColor = v.Damage > 0 ? Damaged : v.MaxHealth > v.PrintedHealth ? Buffed : Color.white;
                Gem(root, "Power", 0f, h - g, g, PowerColor, v.Power.ToString(), pColor, k * (unit ? 1.25f : 1f), dark: true);
                Gem(root, "Health", w - g, h - g, g, HealthColor, v.RemainingHealth.ToString(), hColor, k * (unit ? 1.25f : 1f), dark: true);
                if (unit) Keywords(root, v, k, dome ? artTop : 6f * k);
            }

            if (v.Tapped)
            {
                var shade = Ui.FillPanel(root, "Tapped", new Color(0f, 0f, 0f, 0.18f));
                if (dome) shade.sprite = Ui.DomeSprite;
            } // light: it also lies sideways on the table
        }

        /// <summary>
        /// The art window of a full card: 42% of the height, smaller when the rules text would not fit at a readable
        /// size (playtest 2026-10-10_141545: Archon Lumen's text was cut off in hand).
        /// </summary>
        private static float FullCardArtHeight(CardView v, float w, float h, float k, bool creature)
        {
            string text = CardText(v);
            var gen = new TextGenerator();
            int readable = Mathf.Max(6, Mathf.RoundToInt(11 * k));
            var settings = new TextGenerationSettings
            {
                font = Ui.Font, fontSize = readable, fontStyle = FontStyle.Normal, lineSpacing = 1f, richText = true,
                scaleFactor = 1f, textAnchor = TextAnchor.UpperLeft, horizontalOverflow = HorizontalWrapMode.Wrap,
                verticalOverflow = VerticalWrapMode.Overflow, generationExtents = new Vector2(w - 24f * k, 0f),
                color = Color.black, updateBounds = false,
            };
            float need = gen.GetPreferredHeight(text, settings) + 8f * k;
            float below = 8f * k + 2f * k + 42f * k + (creature ? 34f * k : 10f * k); // art top, name, type line, gems
            foreach (float frac in new[] { 0.42f, 0.36f, 0.30f, 0.25f })
                if (h - h * frac - below >= need) return h * frac;
            return h * 0.25f;
        }

        private static void Gem(RectTransform root, string name, float x, float y, float size, Color color, string text, Color textColor, float k, bool dark = false)
        {
            var gem = Ui.Panel(root, name, x, y, size, size, color);
            Ui.AddOutline(gem.gameObject, new Color(0, 0, 0, 0.7f), 2f);
            var t = Ui.FillLabel(gem.transform, text, Mathf.RoundToInt(22 * k), textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (dark || textColor != Color.white) Ui.AddOutline(t.gameObject, new Color(0, 0, 0, 0.9f), 1.5f);
        }

        private static void Keywords(RectTransform root, CardView v, float k, float top)
        {
            var icons = new List<string>();
            if ((v.Keywords & Keyword.Flying) != 0) icons.Add("FLY");
            if ((v.Keywords & Keyword.Trample) != 0) icons.Add("TRM");
            if ((v.Keywords & Keyword.Lifelink) != 0) icons.Add("LIF");
            if ((v.Keywords & Keyword.Reach) != 0) icons.Add("RCH");
            if ((v.Keywords & Keyword.Vigilance) != 0) icons.Add("VIG");
            if ((v.Keywords & Keyword.CantBlock) != 0) icons.Add("NOB");
            if (v.PlusOneCounters > 0) icons.Add("+" + v.PlusOneCounters);
            float s = 26f * k, x = root.rect.width - s - 4f * k;
            for (int i = 0; i < icons.Count; i++)
            {
                var icon = Ui.Panel(root, "Keyword", x, top + i * (s + 3f * k), s, s * 0.8f, new Color(0f, 0f, 0f, 0.7f));
                Ui.FillLabel(icon.transform, icons[i], Mathf.RoundToInt(11 * k), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        private static void BuildArt(RectTransform art, CardView v, FactionStyle fs)
        {
            var tex = v.DefinitionId != null ? Resources.Load<Texture2D>("CardArt/" + v.DefinitionId) : null;
            if (tex != null)
            {
                var raw = art.gameObject.AddComponent<RawImage>();
                raw.texture = tex;
                raw.raycastTarget = false;
                return;
            }
            var bg = art.gameObject.AddComponent<Image>();
            bg.color = Color.Lerp(fs.Frame, Color.black, 0.35f);
            bg.raycastTarget = false;
            var emblem = Ui.FillLabel(art, fs.Emblem, 40, fs.Accent * new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
            emblem.resizeTextMaxSize = 60;
        }

        /// <summary>Rules text with keywords spelled out first, like printed cards.</summary>
        public static string CardText(CardView v)
        {
            var kw = new List<string>();
            foreach (Keyword k in new[] { Keyword.Flying, Keyword.Trample, Keyword.Lifelink, Keyword.Reach, Keyword.Vigilance })
                if ((v.Keywords & k) != 0) kw.Add(k.ToString());
            string text = v.Text ?? "";
            if (kw.Count > 0 && !kw.All(k => text.Contains(k))) text = string.Join(", ", kw) + (text.Length > 0 ? "\n" + text : "");
            return text;
        }
    }
}
