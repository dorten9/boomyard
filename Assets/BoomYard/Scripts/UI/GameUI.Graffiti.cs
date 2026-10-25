using System.Collections.Generic;
using BoomYard.Core;
using BoomYard.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoomYard.UI
{
    /// <summary>The shop's GRAFFITI tab (colours, caps, stencils) and the stencil maker for the player's own.</summary>
    public sealed partial class GameUI
    {
        static readonly Dictionary<string, Texture2D> stencilPreviews = new Dictionary<string, Texture2D>();

        /// <summary>A stencil as a small picture (one pixel per cell), in the can's current colour.</summary>
        static Texture2D Preview(Stencil st, SprayColor can)
        {
            string key = st.Id + can.Id;
            Texture2D tex;
            if (stencilPreviews.TryGetValue(key, out tex) && tex != null) return tex;
            int w = Mathf.Max(1, st.Width), h = Mathf.Max(1, st.Height);
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = Graffiti.Paint(st.At(x, y), can);
                    px[(h - 1 - y) * w + x] = c != null ? new Color32((byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255), 255) : new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(px);
            tex.Apply();
            stencilPreviews[key] = tex;
            return tex;
        }

        void GraffitiShop(RectTransform content)
        {
            var save = SaveService.Data;
            var can = Graffiti.Color(save.sprayColor);
            const float cw = 500f, ch = 230f;
            int idx = 0;
            foreach (var item in Shop.Items)
            {
                if (item.Kind != "spray" && item.Kind != "cap" && item.Kind != "stencil" && item.Kind != "maker") continue;
                int col = idx % 3, row = idx / 3;
                idx++;
                var card = UIKit.Image(content, item.Id, Card, UIKit.Rounded);
                card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0f, 1f);
                card.rectTransform.pivot = new Vector2(0f, 1f);
                card.rectTransform.anchoredPosition = new Vector2(col * (cw + 24f), -row * (ch + 24f));
                card.rectTransform.sizeDelta = new Vector2(cw, ch);
                bool owned = save.Owns(item);
                bool inUse = item.Kind == "spray" && save.sprayColor == item.Grants || item.Kind == "cap" && save.sprayCap == item.Grants
                    || item.Kind == "stencil" && save.stencil == item.Grants;

                // The picture: a blob of the paint, the cap's line, the stencil itself, or the can for the maker.
                if (item.Kind == "stencil")
                {
                    var st = Graffiti.Find(item.Grants);
                    var raw = new GameObject("Stencil").AddComponent<RawImage>();
                    raw.transform.SetParent(card.transform, false);
                    raw.texture = Preview(st, can);
                    float aspect = st.Width / (float)st.Height;
                    var size = aspect > 1f ? new Vector2(80f, 80f / aspect) : new Vector2(80f * aspect, 80f);
                    UIKit.Place(raw.rectTransform, new Vector2(0f, 1f), new Vector2(20f + (80f - size.x) * 0.5f, -24f - (80f - size.y) * 0.5f), size);
                }
                else if (item.Kind == "spray")
                {
                    var c = Graffiti.Color(item.Grants);
                    var blob = UIKit.Image(card.transform, "Paint", new Color(c.R, c.G, c.B), UIKit.Circle);
                    UIKit.Place(blob.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(72f, 72f));
                    if (c.Glow)
                    {
                        var glow = UIKit.Image(card.transform, "Glow", new Color(c.R, c.G, c.B, 0.45f), UIKit.Glow);
                        UIKit.Place(glow.rectTransform, new Vector2(0f, 1f), new Vector2(4f, -4f), new Vector2(112f, 112f));
                        glow.transform.SetSiblingIndex(blob.transform.GetSiblingIndex());
                    }
                }
                else if (item.Kind == "cap")
                {
                    float w = Graffiti.Cap(item.Grants).Width;
                    var line = UIKit.Image(card.transform, "Line", new Color(can.R, can.G, can.B), UIKit.Rounded);
                    UIKit.Place(line.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -60f + w * 120f), new Vector2(76f, Mathf.Max(6f, w * 240f)));
                }
                else
                {
                    var ic = UIKit.Image(card.transform, "Can", UIKit.Ok, UIKit.Can);
                    UIKit.Place(ic.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(72f, 72f));
                }

                var title = UIKit.Label(card.transform, "Name", item.Name, 28, UIKit.Fg, TextAnchor.UpperLeft, true);
                UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(118f, -24f), new Vector2(370f, 40f));
                var desc = UIKit.Label(card.transform, "Desc", DescribeGraffiti(item), 19, UIKit.Muted, TextAnchor.UpperLeft);
                UIKit.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(118f, -66f), new Vector2(360f, 80f));
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;

                string label = item.Kind == "maker" ? (owned ? "DRAW" : Loc.Coins(item.Coins)) : inUse ? "IN USE" : owned ? "USE" : Loc.Coins(item.Coins);
                bool canTap = !inUse;
                var price = UIKit.Button(card.transform, "Buy", label, canTap ? UIKit.Ember : new Color(0.2f, 0.19f, 0.2f), canTap ? Dark : UIKit.Muted, () =>
                {
                    if (!canTap) return;
                    if (item.Kind == "maker" && owned) { ShowMaker(0); return; }
                    if (game.Buy(item)) { if (item.Kind == "maker") ShowMaker(0); else ShowShop(shopInLevel); }
                    else { Sfx.I.Fail(); Toast("Not enough coins"); }
                }, 26);
                UIKit.Place((RectTransform)price.transform, new Vector2(1f, 0f), new Vector2(-20f, 18f), new Vector2(190f, 64f));
                if (!owned)
                {
                    var ic = UIKit.Image(price.transform, "Cur", Color.white, UIKit.Coin);
                    UIKit.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(32f, 32f));
                    ic.rectTransform.pivot = new Vector2(0f, 0.5f);
                    ((RectTransform)price.transform.Find("Label")).offsetMin = new Vector2(36f, 0f);
                }
            }
            content.sizeDelta = new Vector2(0f, ((idx + 2) / 3) * (ch + 24f));
        }

        static string DescribeGraffiti(ShopItem item)
        {
            switch (item.Kind)
            {
                case "spray":
                    var c = Graffiti.Color(item.Grants);
                    return c.Glow ? "Glow-in-the-dusk paint: it shines." : item.Coins == 0 ? "Your first can. Hold SPRAY with the can out." : "A can of paint for the walls of every street.";
                case "cap":
                    return "Paints a line about " + Mathf.RoundToInt(Graffiti.Cap(item.Grants).Width * 100f) + " cm wide up close.";
                case "stencil":
                    var st = Graffiti.Find(item.Grants);
                    return "Tap a wall with the can out to stamp it (" + (st.Width * Spray.StencilCell).ToString("0.#") + " × " + (st.Height * Spray.StencilCell).ToString("0.#") + " m).";
                default:
                    return "Draw your own stencils: three of them, 16 × 16, in any colours.";
            }
        }

        // ---------- the stencil maker ----------

        int makerSlot;
        char makerCode = '#';
        RawImage makerCanvas;
        Texture2D makerTex;

        public void ShowMaker(int slot)
        {
            makerSlot = slot;
            ShowLayer(false);
            var s = NewScreen("Maker");
            TopBar(s, "STENCIL MAKER", () => ShowShop(shopInLevel));
            var save = SaveService.Data;

            // The canvas: 16 × 16 cells, drawn by tapping or dragging.
            var frame = UIKit.Image(s, "Frame", new Color(0.05f, 0.045f, 0.05f, 0.95f), UIKit.Rounded);
            UIKit.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(60f, -50f), new Vector2(580f, 580f));
            frame.rectTransform.pivot = new Vector2(0f, 0.5f);
            makerCanvas = new GameObject("Canvas").AddComponent<RawImage>();
            makerCanvas.transform.SetParent(frame.transform, false);
            UIKit.Place(makerCanvas.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(544f, 544f));
            makerCanvas.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            makerCanvas.raycastTarget = true;
            if (makerTex == null) makerTex = new Texture2D(Graffiti.CustomSize, Graffiti.CustomSize, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            makerCanvas.texture = makerTex;
            var painter = makerCanvas.gameObject.AddComponent<MakerPainter>();
            painter.OnCell = (x, y) => PaintCell(x, y);
            DrawMaker();

            // Slots.
            float right = 700f;
            Text(s, "YOUR STENCILS", 24, UIKit.Muted, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(right + 200f, -150f), new Vector2(400f, 34f), true);
            for (int i = 0; i < Graffiti.CustomSlots; i++)
            {
                int k = i;
                Btn(s, (i + 1).ToString(), i == slot, () => ShowMaker(k), new Vector2(0f, 1f), new Vector2(right + 40f + i * 96f, -206f), new Vector2(84f, 64f), 30);
            }

            // Palette: the can's colour, the fixed colours, and bare wall (the eraser).
            Text(s, "PAINT", 24, UIKit.Muted, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(right + 200f, -270f), new Vector2(400f, 34f), true);
            string codes = Graffiti.MakerCodes + ".";
            var can = Graffiti.Color(save.sprayColor);
            for (int i = 0; i < codes.Length; i++)
            {
                char code = codes[i];
                var c = code == '.' ? null : Graffiti.Paint(code, can);
                bool sel = code == makerCode;
                var sw = UIKit.Button(s, "Code", code == '#' ? "CAN" : code == '.' ? "ERASE" : "", c != null ? new Color(c.R, c.G, c.B) : new Color(0.16f, 0.15f, 0.17f), code == 'w' || code == 'y' ? Dark : UIKit.Fg, () => { makerCode = code; ShowMaker(makerSlot); }, 16);
                UIKit.Place((RectTransform)sw.transform, new Vector2(0f, 1f), new Vector2(right + i * 76f, -300f), new Vector2(sel ? 70f : 62f, sel ? 70f : 62f));
                if (sel)
                {
                    var ring = UIKit.Image(sw.transform, "Selected", UIKit.Fg, UIKit.Ring);
                    UIKit.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86f, 86f));
                    ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }
            }
            Text(s, "CAN paints in whatever colour is on the can when you stamp it.", 20, UIKit.Muted, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(right + 380f, -400f), new Vector2(760f, 30f));

            bool inUse = save.stencil == Graffiti.CustomId(slot);
            Btn(s, inUse ? "ON THE CAN" : "PUT ON THE CAN", !inUse, () =>
            {
                if (Graffiti.Empty(save.Custom(makerSlot))) { Sfx.I.Fail(); Toast("Draw something first"); return; }
                save.stencil = Graffiti.CustomId(makerSlot);
                SaveService.Save();
                Sfx.I.Confirm();
                ShowMaker(makerSlot);
            }, new Vector2(0f, 0f), new Vector2(right + 200f, 130f), new Vector2(400f, 90f), 32);
            Btn(s, "CLEAR", false, () => Confirm("CLEAR THIS STENCIL?", "Every cell goes back to bare wall.", "CLEAR", () =>
            {
                SetGrid(Graffiti.Blank);
                if (save.stencil == Graffiti.CustomId(makerSlot)) save.stencil = "";
                SaveService.Save();
                ShowMaker(makerSlot);
            }), new Vector2(0f, 0f), new Vector2(right + 540f, 130f), new Vector2(220f, 80f), 28);
        }

        void SetGrid(string grid)
        {
            var save = SaveService.Data;
            save.Custom(makerSlot);   // makes sure every slot exists
            save.customStencils[makerSlot] = grid;
        }

        void PaintCell(int x, int y)
        {
            var save = SaveService.Data;
            var grid = save.Custom(makerSlot);
            var next = Graffiti.Set(grid, x, y, makerCode);
            if (next == grid) return;
            SetGrid(next);
            DrawMaker();
        }

        void DrawMaker()
        {
            var save = SaveService.Data;
            var grid = save.Custom(makerSlot);
            var can = Graffiti.Color(save.sprayColor);
            int n = Graffiti.CustomSize;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var c = Graffiti.Paint(grid[y * n + x], can);
                    bool dark = (x + y) % 2 == 0;
                    px[(n - 1 - y) * n + x] = c != null ? new Color32((byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255), 255)
                        : dark ? new Color32(52, 50, 56, 255) : new Color32(64, 62, 68, 255);
                }
            makerTex.SetPixels32(px);
            makerTex.Apply();
        }
    }

    /// <summary>Turns taps and drags on the stencil maker's canvas into cells (top-left is 0, 0); saves when the finger lifts.</summary>
    public sealed class MakerPainter : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public System.Action<int, int> OnCell;

        public void OnPointerDown(PointerEventData e) { Paint(e); }
        public void OnDrag(PointerEventData e) { Paint(e); }
        public void OnPointerUp(PointerEventData e) { SaveService.Save(); }

        void Paint(PointerEventData e)
        {
            var r = (RectTransform)transform;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(r, e.position, e.pressEventCamera, out local)) return;
            var rect = r.rect;
            float u = (local.x - rect.xMin) / rect.width, v = (rect.yMax - local.y) / rect.height;
            if (u < 0f || v < 0f || u >= 1f || v >= 1f) return;
            int n = Graffiti.CustomSize;
            if (OnCell != null) OnCell((int)(u * n), (int)(v * n));
        }
    }
}
