using System;
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
            var img = Panel(parent, "Button " + label, x, y, w, h, color, raycast: true);
            AddOutline(img.gameObject, new Color(0f, 0f, 0f, 0.6f), 2f);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            b.colors = colors;
            b.interactable = interactable;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            FillLabel(img.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, 4f);
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

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}
