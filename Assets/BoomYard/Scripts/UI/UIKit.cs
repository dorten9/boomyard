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
        static Sprite rounded, circle, ring, glow, vignette, star, arrow, diamond, flame, play, coin, gear, back, gift, lockIcon, drop, check, pill;

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

        /// <summary>Horizontal slider (0..1 mapped to min..max) with a label on the left and the value on the right.</summary>
        public static Slider SliderRow(Transform parent, string label, float min, float max, float value, Action<float> changed, Func<float, string> format)
        {
            var row = Rect(parent, label);
            var l = Label(row, "Label", label, 26, Fg, TextAnchor.MiddleLeft);
            l.rectTransform.anchorMin = new Vector2(0f, 0f); l.rectTransform.anchorMax = new Vector2(0.42f, 1f);
            l.rectTransform.offsetMin = l.rectTransform.offsetMax = Vector2.zero;
            var v = Label(row, "Value", format(value), 24, Muted, TextAnchor.MiddleRight);
            v.rectTransform.anchorMin = new Vector2(0.86f, 0f); v.rectTransform.anchorMax = new Vector2(1f, 1f);
            v.rectTransform.offsetMin = v.rectTransform.offsetMax = Vector2.zero;

            var area = Rect(row, "Slider");
            area.anchorMin = new Vector2(0.44f, 0.5f); area.anchorMax = new Vector2(0.84f, 0.5f);
            area.sizeDelta = new Vector2(0f, 40f);
            area.anchoredPosition = Vector2.zero;
            var bg = Image(area, "Track", Line, Rounded);
            bg.rectTransform.anchorMin = new Vector2(0f, 0.5f); bg.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            bg.rectTransform.sizeDelta = new Vector2(0f, 10f);
            var fillArea = Rect(area, "FillArea");
            fillArea.anchorMin = new Vector2(0f, 0.5f); fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(0f, 10f);
            var fill = Image(fillArea, "Fill", Ember, Rounded);
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = Rect(area, "HandleArea");
            Stretch(handleArea);
            var handle = Image(handleArea, "Handle", Fg, Circle);
            handle.rectTransform.sizeDelta = new Vector2(36f, 36f);
            handle.raycastTarget = true;
            bg.raycastTarget = true;
            var s = area.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = min;
            s.maxValue = max;
            s.value = value;
            s.onValueChanged.AddListener(x => { v.text = format(x); changed(x); });
            return s;
        }

        /// <summary>A labelled on/off switch.</summary>
        public static Button ToggleRow(Transform parent, string label, string hint, bool value, Action<bool> changed)
        {
            var row = Rect(parent, label);
            var l = Label(row, "Label", label, 26, Fg, TextAnchor.MiddleLeft);
            l.rectTransform.anchorMin = new Vector2(0f, 0.45f); l.rectTransform.anchorMax = new Vector2(0.8f, 1f);
            l.rectTransform.offsetMin = l.rectTransform.offsetMax = Vector2.zero;
            if (!string.IsNullOrEmpty(hint))
            {
                var h = Label(row, "Hint", hint, 19, Muted, TextAnchor.MiddleLeft);
                h.rectTransform.anchorMin = new Vector2(0f, 0f); h.rectTransform.anchorMax = new Vector2(0.8f, 0.48f);
                h.rectTransform.offsetMin = h.rectTransform.offsetMax = Vector2.zero;
            }
            else l.rectTransform.anchorMin = new Vector2(0f, 0f);
            var track = Image(row, "Switch", value ? Ember : Line, Pill);
            track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.pivot = new Vector2(1f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(86f, 44f);
            track.raycastTarget = true;
            var knob = Image(track.transform, "Knob", Fg, Circle);
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(value ? 1f : 0f, 0.5f);
            knob.rectTransform.pivot = new Vector2(value ? 1f : 0f, 0.5f);
            knob.rectTransform.anchoredPosition = new Vector2(value ? -4f : 4f, 0f);
            knob.rectTransform.sizeDelta = new Vector2(36f, 36f);
            var b = track.gameObject.AddComponent<Button>();
            bool on = value;
            b.onClick.AddListener(() =>
            {
                on = !on;
                track.color = on ? Ember : Line;
                knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(on ? 1f : 0f, 0.5f);
                knob.rectTransform.pivot = new Vector2(on ? 1f : 0f, 0.5f);
                knob.rectTransform.anchoredPosition = new Vector2(on ? -4f : 4f, 0f);
                changed(on);
            });
            return b;
        }

        /// <summary>A scrolling area; add children to the returned content (vertical or horizontal).</summary>
        public static RectTransform Scroll(Transform parent, string name, bool vertical, out ScrollRect scroll)
        {
            var view = Rect(parent, name);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var content = Rect(view, "Content");
            if (vertical)
            {
                content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                content.anchorMin = new Vector2(0f, 0f); content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 0.5f);
            }
            content.offsetMin = content.offsetMax = Vector2.zero;
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = !vertical;
            scroll.vertical = vertical;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        /// <summary>Image + label button with an icon on the left.</summary>
        public static Button IconButton(Transform parent, string name, Sprite icon, Color fill, Color iconColor, Action onClick, float size = 72f)
        {
            var img = Image(parent, name, fill, Circle);
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            var ic = Image(img.transform, "Icon", iconColor, icon);
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ic.rectTransform.sizeDelta = new Vector2(size * 0.52f, size * 0.52f);
            var b = img.gameObject.AddComponent<Button>();
            b.onClick.AddListener(() => onClick());
            return b;
        }

        // ---------- generated sprites ----------

        public static Sprite Pill
        {
            get
            {
                if (pill == null) pill = Shape(64, (x, y, n) => RoundRect(x, y, n, 31f), new Vector4(31, 31, 31, 31));
                return pill;
            }
        }

        /// <summary>A gold coin with a rim.</summary>
        public static Sprite Coin
        {
            get
            {
                if (coin == null)
                    coin = ShapeRGBA(64, (x, y, n) =>
                    {
                        float d = Dist(x, y, n) / (n * 0.5f);
                        float a = Mathf.Clamp01((1f - d) * n * 0.5f);
                        var rim = d > 0.78f ? new Color(0.85f, 0.55f, 0.12f) : new Color(1f, 0.78f, 0.25f);
                        float shine = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y) / n, new Vector2(0.38f, 0.62f)) * 3f) * 0.4f;
                        return new Color(rim.r + shine, rim.g + shine, rim.b + shine * 0.5f, a);
                    });
                return coin;
            }
        }

        public static Sprite Gear
        {
            get
            {
                if (gear == null)
                    gear = Shape(96, (x, y, n) =>
                    {
                        float dx = x + 0.5f - n * 0.5f, dy = y + 0.5f - n * 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / (n * 0.5f);
                        float ang = Mathf.Atan2(dy, dx);
                        float teeth = Mathf.Clamp01(Mathf.Cos(ang * 8f) * 3f) * 0.16f;
                        float outer = 0.74f + teeth;
                        return Mathf.Clamp01((outer - d) * 30f) * Mathf.Clamp01((d - 0.3f) * 30f);
                    }, Vector4.zero);
                return gear;
            }
        }

        public static Sprite Back
        {
            get
            {
                if (back == null) back = Polygon(64, new[] { new Vector2(0.18f, 0.5f), new Vector2(0.55f, 0.85f), new Vector2(0.55f, 0.62f), new Vector2(0.85f, 0.62f), new Vector2(0.85f, 0.38f), new Vector2(0.55f, 0.38f), new Vector2(0.55f, 0.15f) });
                return back;
            }
        }

        public static Sprite Drop
        {
            get
            {
                if (drop == null) drop = Polygon(96, new[] { new Vector2(0.5f, 0.12f), new Vector2(0.86f, 0.5f), new Vector2(0.62f, 0.5f), new Vector2(0.62f, 0.9f), new Vector2(0.38f, 0.9f), new Vector2(0.38f, 0.5f), new Vector2(0.14f, 0.5f) });
                return drop;
            }
        }

        public static Sprite Check
        {
            get
            {
                if (check == null) check = Polygon(64, new[] { new Vector2(0.1f, 0.52f), new Vector2(0.24f, 0.66f), new Vector2(0.42f, 0.46f), new Vector2(0.8f, 0.86f), new Vector2(0.92f, 0.72f), new Vector2(0.42f, 0.18f) });
                return check;
            }
        }

        public static Sprite Gift
        {
            get
            {
                if (gift == null)
                    gift = Shape(64, (x, y, n) =>
                    {
                        float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                        bool box = u > 0.15f && u < 0.85f && v > 0.1f && v < 0.62f;
                        bool lid = u > 0.1f && u < 0.9f && v > 0.62f && v < 0.75f;
                        bool bowL = Vector2.Distance(new Vector2(u, v), new Vector2(0.38f, 0.84f)) < 0.1f;
                        bool bowR = Vector2.Distance(new Vector2(u, v), new Vector2(0.62f, 0.84f)) < 0.1f;
                        bool ribbon = Mathf.Abs(u - 0.5f) < 0.06f && v < 0.75f;
                        return (box || lid || bowL || bowR) && !ribbon ? 1f : 0f;
                    }, Vector4.zero);
                return gift;
            }
        }

        public static Sprite Lock
        {
            get
            {
                if (lockIcon == null)
                    lockIcon = Shape(64, (x, y, n) =>
                    {
                        float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                        bool body = u > 0.2f && u < 0.8f && v > 0.1f && v < 0.55f;
                        float r = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.55f));
                        bool shackle = v > 0.55f && r < 0.24f && r > 0.14f;
                        bool hole = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.35f)) < 0.06f;
                        return (body || shackle) && !hole ? 1f : 0f;
                    }, Vector4.zero);
                return lockIcon;
            }
        }

        static Sprite ShapeRGBA(int n, Func<int, int, int, Color> color)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = color(x, y, n);
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        }

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
