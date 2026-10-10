using System;
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
            public string Name;
        }

        // The baseline palette (docs/COLOR_BASELINE.md), in OKLCH. Factions: one hue each, all at the same lightness and
        // chroma, so none looks louder than another; blue (hue 260) is kept free for mana. Accents are light (L 0.82)
        // for text and emblems on the dark frames.
        private static FactionStyle Faction(float hue, float accentHue, string emblem, string name, float chroma = 0.10f) => new FactionStyle
        {
            Frame = Ui.Oklch(0.40f, chroma, hue), Accent = Ui.Oklch(0.84f, chroma > 0.05f ? 0.12f : 0.02f, accentHue), Emblem = emblem, Name = name,
        };

        private static readonly Dictionary<string, FactionStyle> Factions = new Dictionary<string, FactionStyle>
        {
            ["goobers"] = Faction(30f, 55f, "GOB", "Goobers"),
            ["evergrowing_wild"] = Faction(145f, 130f, "WLD", "Evergrowing Wild"),
            ["glitterworld"] = Faction(205f, 200f, "GLT", "Glitterworld"),
            ["shadow_money_wizards"] = Faction(300f, 90f, "SMW", "Shadow Money Wizards"), // violet with gold: money
            ["sensationalists"] = Faction(350f, 345f, "SEN", "Sensationalists"),
            ["neutral"] = Faction(70f, 70f, "NEU", "Neutral", 0.015f),
        };

        private static readonly FactionStyle Dweller = Faction(65f, 85f, "TD", "Tavern Dweller", 0.07f);

        public static FactionStyle Style(string faction) =>
            faction != null && Factions.TryGetValue(faction, out var s) ? s : Dweller;

        public static readonly Color Back = Ui.Hex("#3A2416");
        public static readonly Color BackAccent = Ui.Hex("#C89A50");
        // Functional colours, the same on every card: mana blue, Power amber, Health red (LoR / MTG Arena convention).
        public static readonly Color PowerColor = Ui.Oklch(0.76f, 0.15f, 80f);
        public static readonly Color HealthColor = Ui.Oklch(0.55f, 0.19f, 27f);
        public static readonly Color CostColor = Ui.Oklch(0.55f, 0.17f, 260f);
        public static readonly Color Damaged = Ui.Hex("#FF4040");
        public static readonly Color Buffed = Ui.Hex("#60E060");
        private static readonly Color TextInk = Ui.Oklch(0.22f, 0.02f, 60f);
        private static readonly Color TextPaper = Ui.Oklch(0.93f, 0.025f, 85f, 0.95f);

        /// <summary>
        /// Rarity in Legends of Runeterra's gem colours (user, 2026-10-10): Common green, Uncommon blue (LoR Rare), Rare
        /// purple (LoR Epic), Legendary gold (LoR Champion). The gem is LoR's faceted diamond; the frame ring takes the colour.
        /// </summary>
        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return Ui.Oklch(0.68f, 0.15f, 245f);
                case Rarity.Rare: return Ui.Oklch(0.62f, 0.20f, 305f);
                case Rarity.Legendary: return Ui.Oklch(0.83f, 0.16f, 85f);
                default: return Ui.Oklch(0.72f, 0.17f, 150f);
            }
        }

        public static void BuildBack(RectTransform root)
        {
            Ui.FillPanel(root, "Back", Back, 0f, raycast: true);
            Ui.FillPanel(root, "Inner", BackAccent * new Color(1f, 1f, 1f, 0.35f), 8f);
            Ui.FillLabel(root, "RT", 22, BackAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>
        /// Builds the face of a visible card into <paramref name="root"/> (sized by the caller), in the Legends of
        /// Runeterra layout (user, 2026-10-10): the art fills the whole card inside a thin gold frame with cut corners;
        /// a subtype tab on the top edge; the name in capitals, keyword plates and the rules text sit on a dark fade
        /// over the lower art; the region (faction) icon and the rarity gem on the bottom edge; a round mana gem top
        /// left, Power and Health shields in the bottom corners. Units on the board are the same card, smaller.
        /// Tokens on the board keep their dome.
        /// </summary>
        public static void Build(RectTransform root, CardView v, FaceStyle style)
        {
            var fs = v.Type == CardType.TavernDweller ? Dweller : Style(v.Faction);
            float w = root.rect.width, h = root.rect.height;
            float k = h / 210f; // scale relative to a 150×210 hand card

            bool unit = style == FaceStyle.Unit;
            bool creature = v.Type == CardType.Creature;
            bool dweller = v.Type == CardType.TavernDweller;
            bool token = v.IsToken;
            bool dome = unit && creature && token; // playtest 2026-10-10_144700
            var gold = Ui.Gold;

            // The frame: dark edge, gold rim, a dark line, then the body that masks the art.
            Image Shape(string name, Color c, float inset, bool raycast = false)
            {
                if (!dome) return Ui.CardShape(root, name, c, inset, 12f * k - inset, raycast);
                var img = Ui.FillPanel(root, name, c, inset, raycast);
                img.sprite = Ui.DomeSprite;
                if (raycast) img.alphaHitTestMinimumThreshold = 0.5f;
                return img;
            }
            Shape("Frame", new Color(0.05f, 0.04f, 0.03f, 1f), 0f, raycast: true);
            // A thick gold rim that glistens (user, 2026-10-10): lit metal (bright at the top, deep gold at the bottom),
            // a pale bevel line and a dark line inside it, and now and then a glint sweeping across (GoldGlint).
            var rim = Shape("Rim", Color.white, 1f * k);
            rim.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var metal = Ui.Fill(rim.transform, "Metal").gameObject.AddComponent<Image>();
            metal.sprite = Ui.GradientSprite;
            metal.color = new Color(1f, 0.86f, 0.5f);
            metal.raycastTarget = false;
            GoldGlint.Add(rim.rectTransform, w, h);
            Shape("Bevel", new Color(1f, 0.93f, 0.7f, 0.9f), 5.6f * k);
            Shape("RimShade", Color.Lerp(gold, Color.black, 0.6f), 6.4f * k);
            var body = Shape("Body", fs.Frame, 7.2f * k);
            body.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            // Art, full bleed (placeholder until Resources/CardArt has it), and the dark fade the text sits on.
            var art = Ui.Fill(body.transform, "Art");
            BuildArt(art, v, fs);

            // Where the text block starts: LoR pushes the name up when the text is long.
            var keywords = KeywordNames(v);
            // A keyword with a plate isn't repeated at the start of the text ("Trample." under a TRAMPLE plate).
            string text = (v.Text ?? "").Trim();
            for (bool cut = true; cut;)
            {
                cut = false;
                foreach (var kw in keywords)
                    if (text.StartsWith(kw, StringComparison.OrdinalIgnoreCase) && (text.Length == kw.Length || ".,; \n".IndexOf(text[kw.Length]) >= 0))
                    {
                        text = text.Substring(kw.Length).TrimStart('.', ',', ';', ' ', '\n');
                        cut = true;
                    }
            }
            float textSize = (unit ? 11f : 12f) * k;
            float lineH = textSize * 1.15f;
            float charsPerLine = Mathf.Max(8f, (w - 20f * k) / (textSize * 0.5f));
            int lines = 0;
            foreach (var para in text.Split('\n')) lines += Mathf.Max(1, Mathf.CeilToInt(para.Length / charsPerLine));
            if (text.Length == 0) lines = 0;
            float bottom = 36f * k;            // above the region icon, the rarity gem and the shields
            float pillsH = keywords.Count > 0 ? 18f * k : 0f;
            float nameH = (unit ? 18f : 20f) * k;
            float nameY = Mathf.Clamp(h - bottom - lines * lineH - pillsH - nameH - 6f * k, h * 0.26f, h * 0.54f);

            var fade = Ui.Panel(body.transform, "Fade", 0f, nameY - 30f * k - 4.2f * k, w, h, new Color(0.03f, 0.03f, 0.05f, 0.92f));
            fade.sprite = Ui.FadeSprite;

            // Type line tab on the top edge: always the card type, then its subtypes after a dash ("CREATURE — CRITTER",
            // "TOKEN CREATURE — GOOBER", "EQUIPMENT"), so board wipes and "destroy target Equipment" are clear (user, 2026-10-10).
            string tab = dweller ? "TAVERN DWELLER" : (token ? "TOKEN " : "") + v.Type.ToString().ToUpperInvariant()
                + (v.Subtypes != null && v.Subtypes.Length > 0 ? " — " + string.Join(" ", v.Subtypes).ToUpperInvariant() : "");
            float tabW = Mathf.Min(w - 40f * k, (22f + tab.Length * 6.2f) * k), tabH = 14f * k;
            float tabX = Mathf.Max(w / 2f - tabW / 2f, 31f * k); // clear of the mana gem
            var tabImg = Ui.Panel(root, "Subtype", tabX, 1f * k, tabW, tabH, new Color(0.07f, 0.07f, 0.1f, 0.95f));
            Ui.AddOutline(tabImg.gameObject, gold, Mathf.Max(1f, 1f * k));
            Ui.FillTmp(tabImg.transform, tab, 9f * k, Ui.Cream, TextAnchor.MiddleCenter, FontStyle.Bold, 1f * k);

            // Name, in capitals.
            Ui.Tmp(root, v.Name.ToUpperInvariant(), 8f * k, nameY, w - 16f * k, nameH, (unit ? 14f : 16f) * k, Ui.Cream,
                TextAnchor.MiddleCenter, FontStyle.Bold, outline: true);
            float y = nameY + nameH;

            // Keyword plates: dark, gold-rimmed, gold capitals.
            if (keywords.Count > 0)
            {
                float pw = 0f;
                var widths = keywords.Select(kw => (24f + kw.Length * 6.4f) * k).ToList();
                pw = widths.Sum() + 4f * k * (keywords.Count - 1);
                float scale = Mathf.Min(1f, (w - 12f * k) / pw);
                float px = w / 2f - pw * scale / 2f;
                for (int i = 0; i < keywords.Count; i++)
                {
                    float kw = widths[i] * scale;
                    var pill = Ui.Panel(root, "Keyword", px, y + 1f * k, kw, 15f * k, new Color(0.08f, 0.07f, 0.1f, 0.95f));
                    Ui.AddOutline(pill.gameObject, gold, Mathf.Max(1f, 1f * k));
                    Ui.FillTmp(pill.transform, keywords[i].ToUpperInvariant(), 10f * k, gold, TextAnchor.MiddleCenter, FontStyle.Bold, 1f * k);
                    px += kw + 4f * k * scale;
                }
                y += pillsH;
            }

            // Rules text, centred, light on the fade.
            float textH = h - bottom - y - 2f * k;
            if (text.Length > 0 && textH > 6f * k)
            {
                var t = Ui.Tmp(root, text, 9f * k, y + 2f * k, w - 18f * k, textH, textSize, Ui.Cream, TextAnchor.UpperCenter);
                t.fontSizeMin = Mathf.Max(5f, 6.5f * k);
            }

            // Region icon (the faction) above the bottom edge, and the rarity gem on it.
            if (!dweller)
            {
                float r = 14f * k;
                var icon = Ui.Circle(root, "Region", w / 2f - r / 2f, h - 27f * k - r / 2f, r, fs.Frame);
                Ui.AddOutline(icon.gameObject, fs.Accent, Mathf.Max(1f, 1f * k));
                Ui.FillTmp(icon.transform, fs.Emblem, 6.5f * k, fs.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            if (!dweller && !token)
            {
                float g = 22f * k; // on the bottom edge, LoR style (user, 2026-10-10)
                Ui.RarityGem(root, w / 2f - g / 2f, h - g * 0.6f, g, RarityColor(v.Rarity));
            }

            // Mana: a round blue gem with a gold rim, over the top-left corner.
            if (!dweller) Gem(root, "Cost", -3f * k, -3f * k, 32f * k, CostColor, v.Cost.ToString(),
                v.Cost < v.PrintedCost ? Buffed : v.Cost > v.PrintedCost ? Damaged : Color.white, k);

            // Power and Health shields over the bottom corners.
            if (creature)
            {
                float g = 32f * k;
                var pColor = v.Power > v.PrintedPower ? Buffed : v.Power < v.PrintedPower ? Damaged : Color.white;
                var hColor = v.Damage > 0 ? Damaged : v.MaxHealth > v.PrintedHealth ? Buffed : Color.white;
                Gem(root, "Power", -3f * k, h - g + 3f * k, g, PowerColor, v.Power.ToString(), pColor, k);
                Gem(root, "Health", w - g + 3f * k, h - g + 3f * k, g, HealthColor, v.RemainingHealth.ToString(), hColor, k);
                if (v.PlusOneCounters > 0)
                {
                    var c = Ui.Circle(root, "Counters", w - 22f * k, 18f * k, 20f * k, Buffed);
                    Ui.FillTmp(c.transform, "+" + v.PlusOneCounters, 10f * k, TextInk, TextAnchor.MiddleCenter, FontStyle.Bold);
                }
            }

            if (v.Tapped)
            {
                var shade = Ui.Fill(body.transform, "Tapped").gameObject.AddComponent<Image>();
                shade.color = new Color(0f, 0f, 0f, 0.22f); // light: it also tilts on the table
                shade.raycastTarget = false;
            }
        }

        /// <summary>The keywords as words for the plates (Can't block too).</summary>
        private static List<string> KeywordNames(CardView v)
        {
            var list = new List<string>();
            foreach (Keyword kw in new[] { Keyword.Flying, Keyword.Trample, Keyword.Lifelink, Keyword.Reach, Keyword.Vigilance })
                if ((v.Keywords & kw) != 0) list.Add(kw.ToString());
            if ((v.Keywords & Keyword.CantBlock) != 0) list.Add("Can't block");
            return list;
        }

        private static void Gem(RectTransform root, string name, float x, float y, float size, Color color, string text, Color textColor, float k, bool dark = false)
        {
            bool mana = name == "Cost";
            var rim = Ui.Panel(root, name, x, y, size, size, Color.Lerp(Ui.Gold, Color.black, 0.2f));
            rim.sprite = mana ? Ui.CircleSprite : Ui.ShieldSprite;
            Ui.AddOutline(rim.gameObject, new Color(0, 0, 0, 0.8f), Mathf.Max(1f, 1.2f * k));
            float inset = 2.5f * k;
            var fill = Ui.Panel(rim.transform, "Fill", inset, inset, size - 2f * inset, size - 2f * inset, color);
            fill.sprite = rim.sprite;
            var t = Ui.FillTmp(fill.transform, text, 20f * k, textColor, TextAnchor.MiddleCenter, FontStyle.Bold, 0f, outline: true);
            if (!mana) t.margin = new Vector4(0, 0, 0, size * 0.12f); // up a little, off the shield's point
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
            // Placeholder art until real art lands in Resources/CardArt: a faction-tinted gradient with the emblem.
            var bg = art.gameObject.AddComponent<Image>();
            bg.sprite = Ui.GradientSprite;
            bg.color = Color.Lerp(fs.Frame, fs.Accent, 0.3f);
            bg.raycastTarget = false;
            var emblem = Ui.Tmp(art, fs.Emblem, 0f, art.rect.height * 0.08f, art.rect.width, art.rect.height * 0.36f,
                art.rect.height * 0.26f, fs.Accent * new Color(1f, 1f, 1f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);
            emblem.enableAutoSizing = false;
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
