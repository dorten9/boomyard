using System;
using UnityEngine;
using UnityEngine.UI;

namespace BoomYard.UI
{
    /// <summary>
    /// uGUI built from code with generated sprites (spec §15 UI): Rubik for titles, Heebo for text, cream on black,
    /// ember orange for actions, no colourful icons — "a camera UI, not a kids' game".
    /// </summary>
    public static class UIKit
    {
        public static readonly Color Bg = new Color(0.047f, 0.043f, 0.051f);
        public static readonly Color Surface = new Color(0.082f, 0.075f, 0.086f, 0.92f);
        public static readonly Color Surface2 = new Color(0.118f, 0.106f, 0.122f, 0.95f);
        public static readonly Color Line = new Color(0.173f, 0.157f, 0.176f);
        public static readonly Color Fg = new Color(0.922f, 0.902f, 0.875f);
        public static readonly Color Muted = new Color(0.608f, 0.576f, 0.541f);
        public static readonly Color Ember = new Color(1f, 0.478f, 0.102f);
        public static readonly Color Flame = new Color(1f, 0.702f, 0.278f);
        public static readonly Color SteelBlue = new Color(0.561f, 0.639f, 0.706f);
        public static readonly Color Danger = new Color(0.902f, 0.224f, 0.275f);
        public static readonly Color Ok = new Color(0.482f, 0.769f, 0.498f);
        public static readonly Color Scrim = new Color(0.04f, 0.035f, 0.04f, 0.86f);

        static Font text, display;
        static Sprite rounded, circle, ring, glow, vignette, star, arrow, diamond, flame, play;

        public static Font Text
        {
            get
            {
                if (text == null) text = Resources.Load<Font>("Fonts/BoomYardText");
                if (text == null) text = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return text;
            }
        }

        public static Font Display
        {
            get
            {
                if (display == null) display = Resources.Load<Font>("Fonts/BoomYardDisplay");
                return display != null ? display : Text;
            }
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors a rect at a point of its parent (0..1) with a pixel offset and size in reference units.</summary>
        public static RectTransform Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = anchor;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        public static RectTransform Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var r = Rect(parent, name);
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string name, string value, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool displayFont = false)
        {
            var r = Rect(parent, name);
            var t = r.gameObject.AddComponent<Text>();
            t.font = displayFont ? Display : Text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = value;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Button(Transform parent, string name, string label, Color fill, Color textColor, Action onClick, int size = 34)
        {
            var img = Image(parent, name, fill, Rounded);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            b.colors = colors;
            b.onClick.AddListener(() => onClick());
            var t = Label(img.transform, "Label", label, size, textColor, TextAnchor.MiddleCenter, true);
            Stretch(t.rectTransform);
            return b;
        }

        // ---------- generated sprites ----------

        /// <summary>Small-radius rounded rectangle, 9-sliced (the spec's UI uses 2–4 px corners).</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded == null) rounded = Shape(32, (x, y, n) => RoundRect(x, y, n, 6f), new Vector4(10, 10, 10, 10));
                return rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null) circle = Shape(128, (x, y, n) => Mathf.Clamp01((n * 0.5f - Dist(x, y, n)) * 1.5f), Vector4.zero);
                return circle;
            }
        }

        public static Sprite Ring
        {
            get
            {
                if (ring == null) ring = Shape(128, (x, y, n) => { float d = Dist(x, y, n); return Mathf.Clamp01((n * 0.5f - d) * 1.5f) * Mathf.Clamp01((d - n * 0.5f + 5f) * 1.5f); }, Vector4.zero);
                return ring;
            }
        }

        public static Sprite Glow
        {
            get
            {
                if (glow == null) glow = Shape(128, (x, y, n) => { float k = Mathf.Clamp01(1f - Dist(x, y, n) / (n * 0.5f)); return k * k; }, Vector4.zero);
                return glow;
            }
        }

        /// <summary>Dark edges, clear centre.</summary>
        public static Sprite Vignette
        {
            get
            {
                if (vignette == null) vignette = Shape(128, (x, y, n) => { float d = Dist(x, y, n) / (n * 0.5f); return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.65f) / 0.75f)); }, Vector4.zero);
                return vignette;
            }
        }

        /// <summary>Five-point star (the fonts have no ★).</summary>
        public static Sprite Star
        {
            get
            {
                if (star == null)
                {
                    var pts = new Vector2[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                        float r = i % 2 == 0 ? 0.48f : 0.2f;
                        pts[i] = new Vector2(0.5f + Mathf.Cos(a) * r, 0.53f + Mathf.Sin(a) * r);
                    }
                    star = Polygon(96, pts);
                }
                return star;
            }
        }

        /// <summary>Upward arrow for the throw button.</summary>
        public static Sprite Arrow
        {
            get
            {
                if (arrow == null) arrow = Polygon(96, new[] { new Vector2(0.5f, 0.92f), new Vector2(0.86f, 0.52f), new Vector2(0.62f, 0.52f), new Vector2(0.62f, 0.1f), new Vector2(0.38f, 0.1f), new Vector2(0.38f, 0.52f), new Vector2(0.14f, 0.52f) });
                return arrow;
            }
        }

        /// <summary>Gem (◆) for the hard currency.</summary>
        public static Sprite Diamond
        {
            get
            {
                if (diamond == null) diamond = Polygon(64, new[] { new Vector2(0.5f, 0.95f), new Vector2(0.9f, 0.5f), new Vector2(0.5f, 0.05f), new Vector2(0.1f, 0.5f) });
                return diamond;
            }
        }

        /// <summary>Play triangle (▶) for rewarded-ad offers.</summary>
        public static Sprite Play
        {
            get
            {
                if (play == null) play = Polygon(64, new[] { new Vector2(0.2f, 0.1f), new Vector2(0.9f, 0.5f), new Vector2(0.2f, 0.9f) });
                return play;
            }
        }

        /// <summary>Teardrop flame for the lighter button.</summary>
        public static Sprite FlameIcon
        {
            get
            {
                if (flame == null)
                    flame = Shape(96, (x, y, n) =>
                    {
                        float u = (x + 0.5f) / n - 0.5f, v = (y + 0.5f) / n;
                        float w = v < 0.4f ? Mathf.Sqrt(Mathf.Max(0f, 0.16f - (v - 0.4f) * (v - 0.4f))) * 0.9f : 0.36f * (1f - (v - 0.4f) / 0.55f);
                        return Mathf.Clamp01((w - Mathf.Abs(u)) * n * 0.7f);
                    }, Vector4.zero);
                return flame;
            }
        }

        static Sprite Polygon(int n, Vector2[] pts)
        {
            return Shape(n, (x, y, size) =>
            {
                // 4x supersampled point-in-polygon for smooth edges.
                int inside = 0;
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                        if (Inside(new Vector2((x + 0.25f + sx * 0.5f) / size, (y + 0.25f + sy * 0.5f) / size), pts)) inside++;
                return inside / 4f;
            }, Vector4.zero);
        }

        static bool Inside(Vector2 p, Vector2[] poly)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) && (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)) c = !c;
            return c;
        }

        static float Dist(int x, int y, int n) { return Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)); }

        static float RoundRect(int x, int y, int n, float r)
        {
            float cx = Mathf.Clamp(x + 0.5f, r, n - r), cy = Mathf.Clamp(y + 0.5f, r, n - r);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            return Mathf.Clamp01((r - d) * 1.5f + 0.5f);
        }

        static Sprite Shape(int n, Func<int, int, int, float> alpha, Vector4 border)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x, y, n)) * 255));
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }

    /// <summary>Keeps the UI inside the notch and home-indicator safe area.</summary>
    public sealed class SafeArea : MonoBehaviour
    {
        Rect last;
        RectTransform rt;

        void Awake() { rt = (RectTransform)transform; Apply(); }
        void Update() { if (Screen.safeArea != last) Apply(); }

        void Apply()
        {
            last = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rt.anchorMin = new Vector2(last.xMin / Screen.width, last.yMin / Screen.height);
            rt.anchorMax = new Vector2(last.xMax / Screen.width, last.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
