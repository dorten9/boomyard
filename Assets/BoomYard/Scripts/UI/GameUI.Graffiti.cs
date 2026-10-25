using System;
using System.Collections.Generic;
using BoomYard.Core;
using BoomYard.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoomYard.UI
{
    /// <summary>The shop's GRAFFITI tab (colours, caps, stencils) and the stencil studio for the player's own drawings and pictures.</summary>
    public sealed partial class GameUI
    {
        static readonly Dictionary<string, Texture2D> stencilPreviews = new Dictionary<string, Texture2D>();

        /// <summary>A stencil as a small picture (one pixel per cell), in the can's current colour.</summary>
        static Texture2D Preview(Stencil st, SprayColor can)
        {
            string key = st.Id + can.Id + (st.Id.StartsWith("custom") || st.Picture ? "v" + Spray.StencilsVersion : "");
            Texture2D tex;
            if (stencilPreviews.TryGetValue(key, out tex) && tex != null) return tex;
            int w = Mathf.Max(1, st.Width), h = Mathf.Max(1, st.Height);
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (st.Picture)
                    {
                        int i = (y * w + x) * 4;
                        px[(h - 1 - y) * w + x] = new Color32(st.Rgba[i], st.Rgba[i + 1], st.Rgba[i + 2], st.Rgba[i + 3] >= 128 ? (byte)255 : (byte)0);
                        continue;
                    }
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

                string label = item.Kind == "maker" ? "OPEN" : inUse ? "IN USE" : owned ? "USE" : Loc.Coins(item.Coins);
                bool canTap = !inUse;
                var price = UIKit.Button(card.transform, "Buy", label, canTap ? UIKit.Ember : new Color(0.2f, 0.19f, 0.2f), canTap ? Dark : UIKit.Muted, () =>
                {
                    if (!canTap) return;
                    if (item.Kind == "maker") { ShowStudio(() => ShowShop(shopInLevel)); return; }
                    if (game.Buy(item)) ShowShop(shopInLevel);
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
                    return "Tap a wall with the can out to stamp it (" + (st.Width * Graffiti.CellSize(st)).ToString("0.#") + " × " + (st.Height * Graffiti.CellSize(st)).ToString("0.#") + " m).";
                default:
                    return "Draw your own stencils, or make one from a photo (the first is free).";
            }
        }

        // ---------- the stencil studio ----------

        int makerSlot;
        char makerCode = '#';
        bool studioPictures;
        Action studioBack;
        RawImage makerCanvas;
        Texture2D makerTex;

        /// <summary>
        /// The stencil studio (from the home screen and the shop): three drawings on a 16 × 16 grid, and up to
        /// six pictures made from photos (the first is free, each after it costs coins).
        /// </summary>
        public void ShowStudio(Action back)
        {
            studioBack = back;
            ShowStudioPage(studioPictures, makerSlot);
        }

        void ShowStudioPage(bool pictures, int slot)
        {
            studioPictures = pictures;
            makerSlot = slot;
            ShowLayer(false);
            var s = NewScreen("Studio");
            TopBar(s, "STENCIL STUDIO", () => { if (studioBack != null) studioBack(); else game.GoHome(); });
            var save = SaveService.Data;
            var can = Graffiti.Color(save.sprayColor);

            var frame = UIKit.Image(s, "Frame", new Color(0.05f, 0.045f, 0.05f, 0.95f), UIKit.Rounded);
            UIKit.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(60f, -50f), new Vector2(580f, 580f));
            frame.rectTransform.pivot = new Vector2(0f, 0.5f);

            // The two rows of slots: drawings, then pictures.
            float right = 700f;
            Text(s, "DRAWINGS", 24, UIKit.Muted, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(right + 200f, -140f), new Vector2(400f, 34f), true);
            for (int i = 0; i < Graffiti.CustomSlots; i++)
            {
                int k = i;
                bool sel = !pictures && i == slot;
                var b = Btn(s, "", sel, () => ShowStudioPage(false, k), new Vector2(0f, 1f), new Vector2(right + 40f + i * 96f, -196f), new Vector2(84f, 72f), 30);
                Thumb(b.transform, save.StencilById(Graffiti.CustomId(i)), can, (i + 1).ToString());
            }
            Text(s, "PICTURES", 24, UIKit.Muted, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(right + 200f, -256f), new Vector2(400f, 34f), true);
            for (int i = 0; i < Graffiti.PictureSlots; i++)
            {
                int k = i;
                bool sel = pictures && i == slot;
                var b = Btn(s, "", sel, () => ShowStudioPage(true, k), new Vector2(0f, 1f), new Vector2(right + 40f + i * 96f, -312f), new Vector2(84f, 72f), 30);
                Thumb(b.transform, save.Picture(i), can, "+");
            }

            string id = pictures ? Graffiti.PictureId(slot) : Graffiti.CustomId(slot);
            var current = save.StencilById(id);
            if (pictures) PicturePage(s, frame.rectTransform, current, slot, right);
            else DrawingPage(s, frame.rectTransform, right, can);

            bool inUse = save.stencil == id && current != null;
            Btn(s, inUse ? "ON THE CAN" : "PUT ON THE CAN", !inUse && current != null, () =>
            {
                if (current == null) { Sfx.I.Fail(); Toast(pictures ? "Add a picture first" : "Draw something first"); return; }
                save.stencil = id;
                SaveService.Save();
                Sfx.I.Confirm();
                Toast("On the can: take it out and tap a wall");
                ShowStudioPage(pictures, slot);
            }, new Vector2(0f, 0f), new Vector2(right + 200f, 110f), new Vector2(400f, 90f), 32);
            Btn(s, pictures ? "DELETE" : "CLEAR", false, () =>
            {
                if (current == null) return;
                Confirm(pictures ? "DELETE THIS PICTURE?" : "CLEAR THIS DRAWING?", pictures ? "The slot is free again (a new picture still costs coins)." : "Every cell goes back to bare wall.", pictures ? "DELETE" : "CLEAR", () =>
                {
                    if (pictures) save.pictures[slot] = "";
                    else SetGrid(Graffiti.Blank);
                    if (save.stencil == id) save.stencil = "";
                    Spray.StencilsVersion++;
                    SaveService.Save();
                    ShowStudioPage(pictures, slot);
                });
            }, new Vector2(0f, 0f), new Vector2(right + 540f, 110f), new Vector2(220f, 80f), 28);
        }

        /// <summary>A slot button's picture: the stencil, or its number / a plus when it is empty.</summary>
        void Thumb(Transform button, Stencil st, SprayColor can, string empty)
        {
            var label = button.Find("Label").GetComponent<Text>();
            if (st == null) { label.text = empty; return; }
            label.text = "";
            var raw = new GameObject("Thumb").AddComponent<RawImage>();
            raw.transform.SetParent(button, false);
            raw.raycastTarget = false;
            raw.texture = Preview(st, can);
            float aspect = st.Width / (float)st.Height;
            UIKit.Place(raw.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, aspect > 1f ? new Vector2(56f, 56f / aspect) : new Vector2(56f * aspect, 56f));
            raw.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        void DrawingPage(RectTransform s, RectTransform frame, float right, SprayColor can)
        {
            // The canvas: 16 × 16 cells, drawn by tapping or dragging.
            makerCanvas = new GameObject("Canvas").AddComponent<RawImage>();
            makerCanvas.transform.SetParent(frame, false);
            UIKit.Place(makerCanvas.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(544f, 544f));
            makerCanvas.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            makerCanvas.raycastTarget = true;
            if (makerTex == null) makerTex = new Texture2D(Graffiti.CustomSize, Graffiti.CustomSize, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            makerCanvas.texture = makerTex;
            makerCanvas.gameObject.AddComponent<MakerPainter>().OnCell = (x, y) => PaintCell(x, y);
            DrawMaker();

            // Palette: the can's colour, the fixed colours, and bare wall (the eraser).
            string codes = Graffiti.MakerCodes + ".";
            for (int i = 0; i < codes.Length; i++)
            {
                char code = codes[i];
                var c = code == '.' ? null : Graffiti.Paint(code, can);
                bool sel = code == makerCode;
                var sw = UIKit.Button(s, "Code", code == '#' ? "CAN" : code == '.' ? "ERASE" : "", c != null ? new Color(c.R, c.G, c.B) : new Color(0.16f, 0.15f, 0.17f), code == 'w' || code == 'y' ? Dark : UIKit.Fg, () => { makerCode = code; ShowStudioPage(false, makerSlot); }, 16);
                UIKit.Place((RectTransform)sw.transform, new Vector2(0f, 1f), new Vector2(right + i * 76f, -400f), new Vector2(sel ? 70f : 62f, sel ? 70f : 62f));
                if (sel)
                {
                    var ring = UIKit.Image(sw.transform, "Selected", UIKit.Fg, UIKit.Ring);
                    UIKit.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86f, 86f));
                    ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }
            }
            Text(s, "Tap or drag on the grid to paint. CAN is whatever colour is on the can when you stamp it.", 20, UIKit.Muted, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(right + 380f, -478f), new Vector2(760f, 50f));
        }

        void PicturePage(RectTransform s, RectTransform frame, Stencil current, int slot, float right)
        {
            var save = SaveService.Data;
            if (current != null)
            {
                var raw = new GameObject("Picture").AddComponent<RawImage>();
                raw.transform.SetParent(frame, false);
                raw.texture = Preview(current, Graffiti.Color(save.sprayColor));
                float aspect = current.Width / (float)current.Height;
                UIKit.Place(raw.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, aspect > 1f ? new Vector2(520f, 520f / aspect) : new Vector2(520f * aspect, 520f));
                raw.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            else Text(frame, "No picture here yet", 28, UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 60f), true);

            int cost = Graffiti.PictureCost(save.picturesMade);
            var up = Btn(s, current != null ? "NEW PICTURE" : "ADD A PICTURE", true, () => { if (!ImagePicker.Supported) Toast("Pictures are added in the app or the web version"); }, new Vector2(0f, 1f), new Vector2(right + 200f, -410f), new Vector2(400f, 84f), 30);
            var price = UIKit.Label(up.transform, "Cost", cost == 0 ? "YOUR FIRST ONE IS FREE" : Loc.Coins(cost) + " COINS", 18, Dark, TextAnchor.LowerCenter, true);
            UIKit.Place(price.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(400f, 24f));
            price.rectTransform.pivot = new Vector2(0.5f, 0f);
            ((RectTransform)up.transform.Find("Label")).offsetMin = new Vector2(0f, 20f);
            // The browser opens its file dialog only from a real tap: arm on the press, open on the lift.
            var press = up.gameObject.AddComponent<PressAction>();
            press.OnDown = () =>
            {
                if (!ImagePicker.Supported) return;
                if (save.shekels < cost) { Sfx.I.Fail(); Toast("Not enough coins"); return; }
                ImagePicker.I.Pick((rgba, w, h) => OnPicture(rgba, w, h, slot), why => Toast(why));
            };
            Text(s, "A photo or a logo becomes a stencil about 2 m across, in its own colours. A plain background is cut away.", 20, UIKit.Muted, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(right + 380f, -478f), new Vector2(760f, 50f));
        }

        /// <summary>A picture came back from the picker: show it as a stencil and ask before keeping it.</summary>
        public void OnPicture(byte[] rgba, int w, int h, int slot)
        {
            var save = SaveService.Data;
            var st = Graffiti.PictureFrom(rgba, w, h, Graffiti.PictureId(slot), "Picture " + (slot + 1));
            if (st == null || Graffiti.Cells(st).Count < 8) { Sfx.I.Fail(); Toast("That picture came out empty"); return; }
            int cost = Graffiti.PictureCost(save.picturesMade);
            var p = Modal("KeepPicture", new Vector2(900f, 560f));
            Text(p, "YOUR NEW STENCIL", 40, UIKit.Fg, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(820f, 50f), true);
            var raw = new GameObject("Preview").AddComponent<RawImage>();
            raw.transform.SetParent(p, false);
            var tex = new Texture2D(st.Width, st.Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[st.Width * st.Height];
            for (int y = 0; y < st.Height; y++)
                for (int x = 0; x < st.Width; x++)
                {
                    int i = (y * st.Width + x) * 4;
                    px[(st.Height - 1 - y) * st.Width + x] = new Color32(st.Rgba[i], st.Rgba[i + 1], st.Rgba[i + 2], st.Rgba[i + 3]);
                }
            tex.SetPixels32(px);
            tex.Apply();
            raw.texture = tex;
            float aspect = st.Width / (float)st.Height;
            UIKit.Place(raw.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), aspect > 1f ? new Vector2(300f, 300f / aspect) : new Vector2(300f * aspect, 300f));
            raw.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Btn(p, cost == 0 ? "KEEP IT · FREE" : "KEEP IT · " + Loc.Coins(cost), true, () =>
            {
                if (save.shekels < cost) { Sfx.I.Fail(); Toast("Not enough coins"); return; }
                save.shekels -= cost;
                while (save.pictures.Count < Graffiti.PictureSlots) save.pictures.Add("");
                save.pictures[slot] = Graffiti.EncodePicture(st);
                save.picturesMade++;
                save.stencil = Graffiti.PictureId(slot);
                Spray.StencilsVersion++;
                SaveService.Save();
                if (cost > 0) Sfx.I.Coin(); else Sfx.I.Confirm();
                CloseModal();
                ShowStudioPage(true, slot);
            }, new Vector2(0.5f, 0f), new Vector2(150f, 60f), new Vector2(340f, 80f), 28);
            Btn(p, "CANCEL", false, CloseModal, new Vector2(0.5f, 0f), new Vector2(-200f, 60f), new Vector2(220f, 76f), 28);
        }

        void SetGrid(string grid)
        {
            var save = SaveService.Data;
            save.Custom(makerSlot);   // makes sure every slot exists
            save.customStencils[makerSlot] = grid;
            Spray.StencilsVersion++;
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

    /// <summary>Runs an action the moment a button is pressed (before it is released).</summary>
    public sealed class PressAction : MonoBehaviour, IPointerDownHandler
    {
        public Action OnDown;
        public void OnPointerDown(PointerEventData e) { if (OnDown != null) OnDown(); }
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
