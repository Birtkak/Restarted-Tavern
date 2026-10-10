using UnityEngine;
using UnityEngine.UI;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// The glint on a card's gold rim: a soft white band, tilted, that sweeps across the card every few seconds (each
    /// card at its own moment). It lives under the rim's Mask, and the card body covers the middle, so only the rim
    /// glistens.
    /// </summary>
    public sealed class GoldGlint : MonoBehaviour
    {
        private const float Sweep = 0.7f; // seconds to cross the card

        private RectTransform _band;
        private float _w, _period, _phase;

        public static void Add(RectTransform rim, float w, float h)
        {
            var band = Ui.Rect(rim, "Glint", 0, 0, Mathf.Max(8f, w * 0.22f), h * 1.6f);
            band.pivot = new Vector2(0.5f, 0.5f);
            band.anchorMin = band.anchorMax = new Vector2(0f, 0.5f);
            band.localEulerAngles = new Vector3(0, 0, -25f);
            var img = band.gameObject.AddComponent<Image>();
            img.sprite = BandSprite;
            img.color = new Color(1f, 1f, 0.9f, 0.85f);
            img.raycastTarget = false;
            var g = rim.gameObject.AddComponent<GoldGlint>();
            g._band = band;
            g._w = w;
            g._period = Random.Range(5f, 9f);
            g._phase = Random.Range(0f, g._period);
            g.Place();
        }

        private void Update() => Place();

        private void Place()
        {
            float t = ((Time.unscaledTime + _phase) % _period) / Sweep;
            bool on = t < 1f;
            if (_band.gameObject.activeSelf != on) _band.gameObject.SetActive(on);
            if (on) _band.anchoredPosition = new Vector2(Mathf.Lerp(-_w * 0.4f, _w * 1.4f, t), 0f);
        }

        private static Sprite _bandSprite;

        /// <summary>Clear at both sides, bright in the middle (a smooth bump across x).</summary>
        private static Sprite BandSprite
        {
            get
            {
                if (_bandSprite != null) return _bandSprite;
                const int n = 32;
                var tex = new Texture2D(n, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[n * 4];
                for (int x = 0; x < n; x++)
                {
                    float a = Mathf.Sin(Mathf.PI * (x + 0.5f) / n);
                    for (int y = 0; y < 4; y++) px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _bandSprite = Sprite.Create(tex, new Rect(0, 0, n, 4), new Vector2(0.5f, 0.5f), 100f);
            }
        }
    }
}
