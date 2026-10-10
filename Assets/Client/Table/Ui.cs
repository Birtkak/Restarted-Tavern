using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// Builds uGUI elements from code (the table has no hand-made prefabs yet, so the scene is reproducible headless).
    /// Positions are in the 1920×1080 reference space, measured from the top-left corner of the parent.
    /// </summary>
    public static class Ui
    {
        public const float Width = 1920f;
        public const float Height = 1080f;

        private static Font _font;
        public static Font Font => _font != null ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>A rect that fills its parent (with an optional inset).</summary>
        public static RectTransform Fill(Transform parent, string name, float inset = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Panel(Transform parent, string name, float x, float y, float w, float h, Color color, bool raycast = false)
        {
            var img = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image FillPanel(Transform parent, string name, Color color, float inset = 0f, bool raycast = false)
        {
            var img = Fill(parent, name, inset).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Label(Transform parent, string text, float x, float y, float w, float h, int size,
            Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var t = Rect(parent, "Text", x, y, w, h).gameObject.AddComponent<Text>();
            Setup(t, text, size, color, align, style);
            return t;
        }

        public static Text FillLabel(Transform parent, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, float inset = 0f)
        {
            var t = Fill(parent, "Text", inset).gameObject.AddComponent<Text>();
            Setup(t, text, size, color, align, style);
            return t;
        }

        private static void Setup(Text t, string text, int size, Color color, TextAnchor align, FontStyle style)
        {
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Math.Max(8, size / 2);
            t.resizeTextMaxSize = size;
        }

        public static Button Button(Transform parent, string label, float x, float y, float w, float h, Color color,
            Action onClick, int fontSize = 22, bool interactable = true)
        {
            // LoR style: a body lit from the top in the button's colour, inside a gold rim with corner studs.
            var img = Panel(parent, "Button " + label, x, y, w, h, color, raycast: true);
            img.sprite = GradientSprite;
            AddOutline(img.gameObject, new Color(0f, 0f, 0f, 0.6f), 2f);
            var rim = Frame(img.transform, "ring", Gold, Mathf.Clamp(h * 0.45f, 12f, 22f));
            rim.raycastTarget = false;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            b.colors = colors;
            b.interactable = interactable;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            FillLabel(img.transform, label, fontSize, Cream, TextAnchor.MiddleCenter, FontStyle.Bold, 6f);
            return b;
        }

        private static Sprite _circle;

        /// <summary>A smooth white disc (made once at runtime, no asset needed), for bubbles and round gems.</summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (_circle != null) return _circle;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[n * n];
                float r = n / 2f - 1f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - n / 2f) * (x + 0.5f - n / 2f) + (y + 0.5f - n / 2f) * (y + 0.5f - n / 2f));
                        byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255f);
                        px[y * n + x] = new Color32(255, 255, 255, a);
                    }
                tex.SetPixels32(px);
                tex.Apply();
                return _circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        private static Sprite _triangle;

        /// <summary>A white triangle pointing right (+x), for arrow heads.</summary>
        public static Sprite TriangleSprite
        {
            get
            {
                if (_triangle != null) return _triangle;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        // Inside when |y - centre| <= half-height * (1 - x / n): the tip at the right edge.
                        float half = (n / 2f) * (1f - (x + 0.5f) / n);
                        float d = half - Mathf.Abs(y + 0.5f - n / 2f);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(d + 0.5f) * 255f));
                    }
                tex.SetPixels32(px);
                tex.Apply();
                return _triangle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        private static Sprite _dome;

        /// <summary>
        /// A white dome for token units: a half circle as wide as the card on top, straight sides and a flat underside,
        /// in a unit's 124 x 166 proportions.
        /// </summary>
        public static Sprite DomeSprite
        {
            get
            {
                if (_dome != null) return _dome;
                const int w = 124, h = 166;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[w * h];
                float r = w / 2f, cy = h - r; // texture y runs up: the arc's centre is r below the top
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float fx = x + 0.5f, fy = y + 0.5f, d;
                        if (fy <= cy) d = Mathf.Min(fx, w - fx);
                        else d = r - Mathf.Sqrt((fx - r) * (fx - r) + (fy - cy) * (fy - cy));
                        px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(d + 0.5f) * 255f));
                    }
                tex.SetPixels32(px);
                tex.Apply();
                return _dome = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        /// <summary>A disc of the given colour (top-left x/y like <see cref="Panel"/>). Raycasts follow the round shape.</summary>
        public static Image Circle(Transform parent, string name, float x, float y, float size, Color color, bool raycast = false) =>
            Circle(parent, name, x, y, size, size, color, raycast);

        /// <summary>An ellipse (the disc stretched to w × h).</summary>
        public static Image Circle(Transform parent, string name, float x, float y, float w, float h, Color color, bool raycast = false)
        {
            var img = Panel(parent, name, x, y, w, h, color, raycast);
            img.sprite = CircleSprite;
            if (raycast) img.alphaHitTestMinimumThreshold = 0.5f;
            return img;
        }

        public static Outline AddOutline(GameObject go, Color color, float distance)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(distance, -distance);
            return o;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>
        /// A colour from OKLCH (perceptual lightness 0-1, chroma ~0-0.37, hue in degrees), the baseline palette's space
        /// (docs/COLOR_BASELINE.md): equal L looks equally bright whatever the hue, so factions and rarities can be
        /// set to matching lightness. Out-of-gamut values are clipped.
        /// </summary>
        public static Color Oklch(float l, float c, float hueDeg, float alpha = 1f)
        {
            float hr = hueDeg * Mathf.Deg2Rad, a = c * Mathf.Cos(hr), b = c * Mathf.Sin(hr);
            float l_ = l + 0.3963377774f * a + 0.2158037573f * b;
            float m_ = l - 0.1055613458f * a - 0.0638541728f * b;
            float s_ = l - 0.0894841775f * a - 1.2914855480f * b;
            float L = l_ * l_ * l_, M = m_ * m_ * m_, S = s_ * s_ * s_;
            float r = 4.0767416621f * L - 3.3077115913f * M + 0.2309699292f * S;
            float g = -1.2684380046f * L + 2.6097574011f * M - 0.3413193965f * S;
            float bl = -0.0041960863f * L - 0.7034186147f * M + 1.7076147010f * S;
            return new Color(Encode(r), Encode(g), Encode(bl), alpha);

            float Encode(float x)
            {
                x = Mathf.Clamp01(x);
                return x <= 0.0031308f ? 12.92f * x : 1.055f * Mathf.Pow(x, 1f / 2.4f) - 0.055f;
            }
        }

        // ------------------------------------------------------------ TextMeshPro (card faces)

        private static Material _tmpOutline;

        /// <summary>
        /// Crisp SDF text (TextMeshPro) that shrinks to fit between size/2 and size, for card faces. outline: the
        /// shared outline material (gem numbers), so cards still batch.
        /// </summary>
        public static TMPro.TextMeshProUGUI Tmp(Transform parent, string text, float x, float y, float w, float h, float size,
            Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, bool outline = false)
        {
            var t = Rect(parent, "Tmp", x, y, w, h).gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            SetupTmp(t, text, size, color, align, style, outline);
            return t;
        }

        public static TMPro.TextMeshProUGUI FillTmp(Transform parent, string text, float size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, float inset = 0f, bool outline = false)
        {
            var t = Fill(parent, "Tmp", inset).gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            SetupTmp(t, text, size, color, align, style, outline);
            return t;
        }

        private static void SetupTmp(TMPro.TextMeshProUGUI t, string text, float size, Color color, TextAnchor align, FontStyle style, bool outline)
        {
            t.text = text;
            t.color = color;
            t.raycastTarget = false;
            t.richText = true;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(6f, size / 2f);
            t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            t.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            t.margin = Vector4.zero;
            t.fontStyle = (style == FontStyle.Bold || style == FontStyle.BoldAndItalic ? TMPro.FontStyles.Bold : 0)
                          | (style == FontStyle.Italic || style == FontStyle.BoldAndItalic ? TMPro.FontStyles.Italic : 0);
            switch (align)
            {
                case TextAnchor.UpperLeft: t.alignment = TMPro.TextAlignmentOptions.TopLeft; break;
                case TextAnchor.UpperCenter: t.alignment = TMPro.TextAlignmentOptions.Top; break;
                case TextAnchor.MiddleLeft: t.alignment = TMPro.TextAlignmentOptions.Left; break;
                case TextAnchor.MiddleRight: t.alignment = TMPro.TextAlignmentOptions.Right; break;
                default: t.alignment = TMPro.TextAlignmentOptions.Center; break;
            }
            if (outline)
            {
                if (_tmpOutline == null) _tmpOutline = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
                if (_tmpOutline != null) t.fontSharedMaterial = _tmpOutline;
            }
        }

        // ------------------------------------------------------------ card frame art (placeholders)

        private static readonly Dictionary<string, Sprite> _frames = new Dictionary<string, Sprite>();

        /// <summary>
        /// A 9-slice card frame: corners stay sharp at every card size (Image.type = Sliced). Real art goes in
        /// Resources/CardFrames/{name}.png (sprite with its border set in the importer) and replaces the placeholder,
        /// which is drawn here in white / grey so it can be tinted: "ring" (the rarity ring, bevelled metal with corner
        /// studs), "ring_legendary" (heavier, with corner gems) and "inner" (the thin faction line inside it).
        /// </summary>
        public static Sprite FrameSprite(string name)
        {
            if (_frames.TryGetValue(name, out var cached)) return cached;
            var art = Resources.Load<Sprite>("CardFrames/" + name);
            if (art != null) return _frames[name] = art;
            const int n = 64;
            int border = name == "inner" ? 8 : 22;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            bool legendary = name == "ring_legendary";
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y)); // distance to the edge
                    float v = 0f, a = 0f;
                    if (name == "inner")
                    {
                        if (d >= 3 && d < 5) { v = 1f; a = 1f; }                     // a thin line
                        else if (d >= 5 && d < 7) { v = 0f; a = 0.5f; }             // its shadow inwards
                    }
                    else
                    {
                        float ring = legendary ? 10f : 7f;
                        float t = y / (float)(n - 1);                               // texture y runs up: light at the top
                        if (d < 1) { v = 0.1f; a = 1f; }                            // dark outer line
                        else if (d < ring)
                        {
                            float bevel = 1f - Mathf.Abs((d - 1f) / (ring - 1f) - 0.35f) * 1.2f; // a ridge, lit near the outside
                            v = Mathf.Clamp01(0.55f + 0.35f * bevel + 0.15f * (t - 0.5f));
                            a = 1f;
                        }
                        else if (d < ring + 1) { v = 0.1f; a = 1f; }               // dark inner line
                        else if (d < ring + 3) { v = 0f; a = 0.35f; }              // soft shadow on the card
                        // Corner studs (diamonds) inside each corner; a bigger, brighter gem on Legendary frames.
                        int cx = x < n / 2 ? 0 : n - 1, cy = y < n / 2 ? 0 : n - 1;
                        float sx = Mathf.Abs(x - cx), sy = Mathf.Abs(y - cy);
                        float studC = legendary ? 9f : 6.5f, studR = legendary ? 6.5f : 4f;
                        float sd = Mathf.Abs(sx - studC) + Mathf.Abs(sy - studC);
                        if (sd < studR + 1f) { v = 0.1f; a = 1f; }
                        if (sd < studR) { v = Mathf.Lerp(1f, 0.7f, sd / studR); a = 1f; }
                    }
                    px[y * n + x] = new Color(v, v, v, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            return _frames[name] = sprite;
        }

        /// <summary>An Image with a 9-slice frame sprite over the whole of <paramref name="parent"/>; borderPx: how thick it draws.</summary>
        public static Image Frame(Transform parent, string sprite, Color tint, float borderPx)
        {
            var img = FillPanel(parent, "Frame " + sprite, tint);
            img.sprite = FrameSprite(sprite);
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
            img.pixelsPerUnitMultiplier = img.sprite.border.x / Mathf.Max(1f, borderPx);
            return img;
        }

        private static Sprite _cardShape, _fade, _shield;

        /// <summary>
        /// LoR's card silhouette: a rectangle with its corners cut off diagonally (9-slice, so the cut stays the same
        /// size on every card). White, tinted by the Image.
        /// </summary>
        public static Sprite CardShapeSprite
        {
            get
            {
                if (_cardShape != null) return _cardShape;
                const int n = 64, cut = 12, border = 18;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float u = Mathf.Min(x + 0.5f, n - x - 0.5f), v = Mathf.Min(y + 0.5f, n - y - 0.5f);
                        float a = Mathf.Clamp01((u + v - cut) / 1.4f + 0.5f) * Mathf.Clamp01(u + 0.5f) * Mathf.Clamp01(v + 0.5f);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                tex.SetPixels32(px);
                tex.Apply();
                return _cardShape = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                    new Vector4(border, border, border, border));
            }
        }

        /// <summary>The cut-corner shape as an Image filling <paramref name="parent"/> (inset), the cut drawn cutPx wide.</summary>
        public static Image CardShape(Transform parent, string name, Color color, float inset, float cutPx, bool raycast = false)
        {
            var img = FillPanel(parent, name, color, inset, raycast);
            img.sprite = CardShapeSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 12f / Mathf.Max(0.5f, cutPx);
            if (raycast) img.alphaHitTestMinimumThreshold = 0.5f;
            return img;
        }

        /// <summary>Transparent at the top to opaque at the bottom (eased): the dark fade under LoR's card text.</summary>
        public static Sprite FadeSprite
        {
            get
            {
                if (_fade != null) return _fade;
                const int n = 64;
                var tex = new Texture2D(4, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[4 * n];
                for (int y = 0; y < n; y++)
                {
                    float t = 1f - y / (float)(n - 1); // texture y runs up: opaque at the bottom
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.6f));
                    for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _fade = Sprite.Create(tex, new Rect(0, 0, 4, n), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        /// <summary>A shield (flat top, straight sides, a point at the bottom): LoR's Power / Health plates.</summary>
        public static Sprite ShieldSprite
        {
            get
            {
                if (_shield != null) return _shield;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[n * n];
                float c = n / 2f, half = n / 2f - 1f, knee = n * 0.42f; // below the knee (texture y) the sides taper to the point
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float fy = y + 0.5f, fx = x + 0.5f;
                        float hw = fy >= knee ? half : half * (fy / knee);
                        float d = Mathf.Min(hw - Mathf.Abs(fx - c), n - 1f - fy);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(d + 0.5f) * 255f));
                    }
                tex.SetPixels32(px);
                tex.Apply();
                return _shield = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        private static Sprite _gradient;

        /// <summary>A vertical white-to-grey gradient (light at the top), tinted for placeholder art.</summary>
        public static Sprite GradientSprite
        {
            get
            {
                if (_gradient != null) return _gradient;
                const int n = 64;
                var tex = new Texture2D(4, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[4 * n];
                for (int y = 0; y < n; y++)
                {
                    float v = Mathf.Lerp(0.45f, 1f, y / (float)(n - 1));
                    for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(v, v, v, 1f);
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _gradient = Sprite.Create(tex, new Rect(0, 0, 4, n), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        /// <summary>
        /// The LoR rarity gem: a faceted diamond in the rarity colour with a dark rim and a light facet. x / y: top-left
        /// of its size × size box.
        /// </summary>
        public static void RarityGem(Transform parent, float x, float y, float size, Color color)
        {
            var box = Rect(parent, "Rarity", x, y, size, size);
            float s = size * 0.70f; // a square turned 45 degrees fills the box
            var rim = Panel(box, "Rim", (size - s) / 2f, (size - s) / 2f, s, s, new Color(0.08f, 0.06f, 0.04f, 1f));
            rim.rectTransform.localEulerAngles = new Vector3(0, 0, 45f);
            var gem = Panel(rim.transform, "Gem", s * 0.12f, s * 0.12f, s * 0.76f, s * 0.76f, color);
            var facet = Panel(gem.transform, "Facet", 0, 0, s * 0.38f, s * 0.38f, Color.Lerp(color, Color.white, 0.55f));
            facet.color = new Color(facet.color.r, facet.color.g, facet.color.b, 0.9f);
        }

        /// <summary>LoR trim gold and the cream used for text on buttons.</summary>
        public static readonly Color Gold = new Color(0.85f, 0.70f, 0.40f);
        public static readonly Color Cream = new Color(0.97f, 0.92f, 0.80f);

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}
