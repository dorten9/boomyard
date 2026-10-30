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
        /// <summary>A shop stencil as a picture, in smooth shapes and the can's current colour.</summary>
        static Texture2D Preview(Stencil st, SprayColor can) { return Stencils.Smooth(st, can); }

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
        bool studioPictures;
        Action studioBack;
        string brushColour = "black";
        int brushSize = 1;
        static readonly float[] BrushRadii = { 4f, 9f, 18f, 34f };
        DrawingCanvas canvas;

        /// <summary>
        /// The stencil studio (from the home screen and the shop): three freehand drawings and up to six pictures
        /// made from photos (the first is free, each after it costs coins). Each is sprayed exactly as it looks.
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

            var frame = UIKit.Image(s, "Frame", new Color(0.05f, 0.045f, 0.05f, 0.95f), UIKit.Rounded);
            UIKit.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(60f, -50f), new Vector2(580f, 580f));
            frame.rectTransform.pivot = new Vector2(0f, 0.5f);

            // The two rows of slots: drawings, then pictures.
            float right = 700f;
            Text(s, "DRAWINGS", 24, UIKit.Muted, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(right + 200f, -140f), new Vector2(400f, 34f), true);
            for (int i = 0; i < Graffiti.CustomSlots; i++)
            {
                int k = i;
                var b = Btn(s, "", !pictures && i == slot, () => ShowStudioPage(false, k), new Vector2(0f, 1f), new Vector2(right + 40f + i * 96f, -196f), new Vector2(84f, 72f), 30);
                Thumb(b.transform, Stencils.Load(Stencils.DrawingId(i)), (i + 1).ToString());
            }
            Text(s, "PICTURES", 24, UIKit.Muted, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(right + 200f, -256f), new Vector2(400f, 34f), true);
            for (int i = 0; i < Graffiti.PictureSlots; i++)
            {
                int k = i;
                var b = Btn(s, "", pictures && i == slot, () => ShowStudioPage(true, k), new Vector2(0f, 1f), new Vector2(right + 40f + i * 96f, -312f), new Vector2(84f, 72f), 30);
                Thumb(b.transform, Stencils.Load(Stencils.PictureId(i)), "+");
            }

            string id = pictures ? Stencils.PictureId(slot) : Stencils.DrawingId(slot);
            if (pictures) PicturePage(s, frame.rectTransform, Stencils.Load(id), slot, right);
            else DrawingPage(s, frame.rectTransform, right);

            bool inUse = save.stencil == id && Stencils.Has(id);
            Btn(s, inUse ? "ON THE CAN" : "PUT ON THE CAN", !inUse, () =>
            {
                if (!pictures && canvas != null) canvas.Commit();
                if (!Stencils.Has(id)) { Sfx.I.Fail(); Toast(pictures ? "Add a picture first" : "Draw something first"); return; }
                save.stencil = id;
                SaveService.Save();
                Sfx.I.Confirm();
                Toast("On the can: take it out and tap a wall");
                ShowStudioPage(pictures, slot);
            }, new Vector2(0f, 0f), new Vector2(right + 200f, 110f), new Vector2(400f, 90f), 32);
            Btn(s, pictures ? "DELETE" : "CLEAR", false, () =>
            {
                Confirm(pictures ? "DELETE THIS PICTURE?" : "CLEAR THIS DRAWING?", pictures ? "The slot is free again (a new picture still costs coins)." : "The whole drawing is wiped.", pictures ? "DELETE" : "CLEAR", () =>
                {
                    Stencils.Delete(id);
                    if (save.stencil == id) save.stencil = "";
                    SaveService.Save();
                    ShowStudioPage(pictures, slot);
                });
            }, new Vector2(0f, 0f), new Vector2(right + 540f, 110f), new Vector2(220f, 80f), 28);
        }

        /// <summary>A slot button's picture, or its number / a plus when it is empty.</summary>
        void Thumb(Transform button, Texture2D tex, string empty)
        {
            var label = button.Find("Label").GetComponent<Text>();
            if (tex == null) { label.text = empty; return; }
            label.text = "";
            var raw = new GameObject("Thumb").AddComponent<RawImage>();
            raw.transform.SetParent(button, false);
            raw.raycastTarget = false;
            raw.texture = tex;
            float aspect = tex.width / (float)tex.height;
            UIKit.Place(raw.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, aspect > 1f ? new Vector2(58f, 58f / aspect) : new Vector2(58f * aspect, 58f));
            raw.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>A see-through checkerboard under a drawing or picture.</summary>
        static Texture2D checker;
        static Texture2D Checker
        {
            get
            {
                if (checker != null) return checker;
                checker = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
                var px = new Color32[256];
                for (int i = 0; i < 256; i++) px[i] = ((i % 16) / 8 + (i / 16) / 8) % 2 == 0 ? new Color32(52, 50, 56, 255) : new Color32(64, 62, 68, 255);
                checker.SetPixels32(px);
                checker.Apply();
                return checker;
            }
        }

        RawImage Board(RectTransform frame, Vector2 size)
        {
            var bg = new GameObject("Board").AddComponent<RawImage>();
            bg.transform.SetParent(frame, false);
            bg.texture = Checker;
            bg.uvRect = new Rect(0f, 0f, size.x / 64f, size.y / 64f);
            UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            bg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bg.raycastTarget = false;
            return bg;
        }

        void DrawingPage(RectTransform s, RectTransform frame, float right)
        {
            Board(frame, new Vector2(544f, 544f));
            var raw = new GameObject("Canvas").AddComponent<RawImage>();
            raw.transform.SetParent(frame, false);
            UIKit.Place(raw.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(544f, 544f));
            raw.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            raw.raycastTarget = true;
            canvas = raw.gameObject.AddComponent<DrawingCanvas>();
            canvas.Open(Stencils.DrawingId(makerSlot), raw);
            canvas.Colour = Graffiti.Color(brushColour);
            canvas.Radius = BrushRadii[brushSize];

            // Paints: every colour there is, then the eraser.
            var colours = Graffiti.Colors;
            for (int i = 0; i <= colours.Length; i++)
            {
                bool eraser = i == colours.Length;
                string cid = eraser ? "" : colours[i].Id;
                var c = eraser ? null : colours[i];
                bool sel = cid == brushColour;
                int row = i / 7, col = i % 7;
                var sw = UIKit.Button(s, "Paint", eraser ? "ERASE" : "", c != null ? new Color(c.R, c.G, c.B) : new Color(0.16f, 0.15f, 0.17f), UIKit.Fg, () => { brushColour = cid; if (canvas != null) canvas.Commit(); ShowStudioPage(false, makerSlot); }, 15);
                UIKit.Place((RectTransform)sw.transform, new Vector2(0f, 1f), new Vector2(right + col * 72f, -370f - row * 70f), new Vector2(sel ? 64f : 56f, sel ? 64f : 56f));
                if (sel)
                {
                    var ring = UIKit.Image(sw.transform, "Selected", UIKit.Fg, UIKit.Ring);
                    UIKit.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78f, 78f));
                    ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }
            }
            // Brush sizes and undo.
            string[] sizes = { "FINE", "SMALL", "BIG", "HUGE" };
            for (int i = 0; i < sizes.Length; i++)
            {
                int k = i;
                Btn(s, sizes[i], i == brushSize, () => { brushSize = k; if (canvas != null) canvas.Radius = BrushRadii[k]; ShowStudioPageKeep(); }, new Vector2(0f, 1f), new Vector2(right + 580f + (i % 2) * 120f, -370f - (i / 2) * 70f), new Vector2(110f, 56f), 20);
            }
            Btn(s, "UNDO", false, () => { if (canvas != null) canvas.Undo(); }, new Vector2(0f, 1f), new Vector2(right + 640f, -510f), new Vector2(230f, 56f), 22);
        }

        /// <summary>Re-lays the page after a brush change, keeping the strokes not yet saved.</summary>
        void ShowStudioPageKeep()
        {
            if (canvas != null) canvas.Commit();
            ShowStudioPage(false, makerSlot);
        }

        void PicturePage(RectTransform s, RectTransform frame, Texture2D current, int slot, float right)
        {
            var save = SaveService.Data;
            if (current != null)
            {
                float aspect = current.width / (float)current.height;
                var size = aspect > 1f ? new Vector2(520f, 520f / aspect) : new Vector2(520f * aspect, 520f);
                Board(frame, size);
                var raw = new GameObject("Picture").AddComponent<RawImage>();
                raw.transform.SetParent(frame, false);
                raw.texture = current;
                UIKit.Place(raw.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
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
            Text(s, "A photo or a logo becomes a stencil about 2 m across, sharp and in its own colours. A plain background is cut away.", 20, UIKit.Muted, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(right + 380f, -478f), new Vector2(760f, 50f));
        }

        /// <summary>A picture came back from the picker: show it as a stencil and ask before keeping it.</summary>
        public void OnPicture(byte[] rgba, int w, int h, int slot)
        {
            var save = SaveService.Data;
            var st = Graffiti.PictureFrom(rgba, w, h, Stencils.PictureId(slot), "Picture " + (slot + 1), true, Graffiti.PictureSharp);
            if (st == null || Graffiti.Cells(st).Count < 64) { Sfx.I.Fail(); Toast("That picture came out empty"); return; }
            int cost = Graffiti.PictureCost(save.picturesMade);
            var tex = Stencils.FromRgba(st.Rgba, st.PicWidth, st.PicHeight);
            var p = Modal("KeepPicture", new Vector2(900f, 560f));
            Text(p, "YOUR NEW STENCIL", 40, UIKit.Fg, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(820f, 50f), true);
            float aspect = st.Width / (float)st.Height;
            var size = aspect > 1f ? new Vector2(320f, 320f / aspect) : new Vector2(320f * aspect, 320f);
            var bg = new GameObject("Board").AddComponent<RawImage>();
            bg.transform.SetParent(p, false);
            bg.texture = Checker;
            bg.uvRect = new Rect(0f, 0f, size.x / 64f, size.y / 64f);
            UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), size);
            bg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var raw = new GameObject("Preview").AddComponent<RawImage>();
            raw.transform.SetParent(p, false);
            raw.texture = tex;
            UIKit.Place(raw.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), size);
            raw.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Btn(p, cost == 0 ? "KEEP IT · FREE" : "KEEP IT · " + Loc.Coins(cost), true, () =>
            {
                if (save.shekels < cost) { Sfx.I.Fail(); Toast("Not enough coins"); return; }
                save.shekels -= cost;
                save.picturesMade++;
                save.stencil = Stencils.PictureId(slot);
                Stencils.Save(Stencils.PictureId(slot), tex);
                SaveService.Save();
                if (cost > 0) Sfx.I.Coin(); else Sfx.I.Confirm();
                CloseModal();
                ShowStudioPage(true, slot);
            }, new Vector2(0.5f, 0f), new Vector2(150f, 60f), new Vector2(340f, 80f), 28);
            Btn(p, "CANCEL", false, CloseModal, new Vector2(0.5f, 0f), new Vector2(-200f, 60f), new Vector2(220f, 76f), 28);
        }
    }

    /// <summary>
    /// The stencil studio's drawing board: freehand strokes with a round, soft-edged brush on a 512 × 512
    /// picture, saved when the finger lifts. Keeps a few steps of undo.
    /// </summary>
    public sealed class DrawingCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public SprayColor Colour;   // null: the eraser
        public float Radius = 9f;

        string id;
        RawImage view;
        Texture2D tex;
        Color32[] px;
        readonly List<Color32[]> undo = new List<Color32[]>();
        Vector2 last;
        bool down, dirty, changed;

        public void Open(string stencilId, RawImage target)
        {
            id = stencilId;
            view = target;
            int n = Stencils.DrawingSize;
            tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var saved = Stencils.Load(id);
            if (saved != null && saved.width == n && saved.height == n) px = saved.GetPixels32();
            else if (saved != null)
            {
                // An older, smaller drawing: scaled up to the board.
                px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        px[y * n + x] = saved.GetPixelBilinear((x + 0.5f) / n, (y + 0.5f) / n);
            }
            else px = new Color32[n * n];
            tex.SetPixels32(px);
            tex.Apply(true);
            view.texture = tex;
        }

        public void OnPointerDown(PointerEventData e)
        {
            Vector2 p;
            if (!ToPixel(e, out p)) return;
            if (undo.Count >= 6) undo.RemoveAt(0);
            undo.Add((Color32[])px.Clone());
            down = true;
            last = p;
            Dab(p);
        }

        public void OnDrag(PointerEventData e)
        {
            Vector2 p;
            if (!down || !ToPixel(e, out p)) return;
            // A smooth line: dabs every third of the brush along the way.
            float step = Mathf.Max(1f, Radius * 0.3f);
            float len = Vector2.Distance(last, p);
            for (float d = step; d <= len; d += step) Dab(Vector2.Lerp(last, p, d / len));
            Dab(p);
            last = p;
        }

        public void OnPointerUp(PointerEventData e)
        {
            down = false;
            Commit();
        }

        /// <summary>Saves the drawing if anything changed (also when the page is left).</summary>
        public void Commit()
        {
            if (!changed) return;
            changed = false;
            Flush();
            bool empty = true;
            foreach (var c in px) if (c.a > 0) { empty = false; break; }
            if (empty) Stencils.Delete(id);
            else Stencils.Save(id, tex);
        }

        public void Undo()
        {
            if (undo.Count == 0) return;
            px = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            dirty = changed = true;
            Commit();
        }

        bool ToPixel(PointerEventData e, out Vector2 p)
        {
            var r = (RectTransform)transform;
            Vector2 local;
            p = Vector2.zero;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(r, e.position, e.pressEventCamera, out local)) return false;
            var rect = r.rect;
            p = new Vector2((local.x - rect.xMin) / rect.width * tex.width, (local.y - rect.yMin) / rect.height * tex.height);
            return true;
        }

        /// <summary>One round dab of the brush, its edge anti-aliased.</summary>
        void Dab(Vector2 c)
        {
            int n = tex.width;
            float r = Radius;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(c.x - r - 1)), x1 = Mathf.Min(n - 1, Mathf.CeilToInt(c.x + r + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(c.y - r - 1)), y1 = Mathf.Min(n - 1, Mathf.CeilToInt(c.y + r + 1));
            var col = Colour != null ? new Color32((byte)(Colour.R * 255), (byte)(Colour.G * 255), (byte)(Colour.B * 255), 255) : new Color32(0, 0, 0, 0);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    float a = Mathf.Clamp01(r + 0.5f - d);
                    if (a <= 0f) continue;
                    int i = y * n + x;
                    var o = px[i];
                    if (Colour == null) { o.a = (byte)(o.a * (1f - a)); px[i] = o; continue; }
                    // Paint over: blend towards the brush colour, coverage adds up.
                    float oa = o.a / 255f, na = oa + (1f - oa) * a;
                    float k = na > 0f ? a / na : 1f;
                    px[i] = new Color32((byte)Mathf.Lerp(o.r, col.r, Mathf.Max(k, a)), (byte)Mathf.Lerp(o.g, col.g, Mathf.Max(k, a)), (byte)Mathf.Lerp(o.b, col.b, Mathf.Max(k, a)), (byte)(na * 255f));
                }
            dirty = changed = true;
        }

        void LateUpdate() { Flush(); }

        void Flush()
        {
            if (!dirty || tex == null) return;
            dirty = false;
            tex.SetPixels32(px);
            tex.Apply(true);
        }

        void OnDestroy() { Commit(); }
    }

    /// <summary>Runs an action the moment a button is pressed (before it is released).</summary>
    public sealed class PressAction : MonoBehaviour, IPointerDownHandler
    {
        public Action OnDown;
        public void OnPointerDown(PointerEventData e) { if (OnDown != null) OnDown(); }
    }

}
