using System.Collections.Generic;
using System.IO;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>A stencil ready to spray: its picture (alpha is the cut-out) and how wide it is on the wall.</summary>
    public sealed class StencilImage
    {
        public string Id, Name;
        public Texture2D Texture;
        public float Width;   // metres on the wall; the height follows the picture
        public bool Glow;     // drawn in a glowing paint

        public float Height { get { return Texture == null ? Width : Width * Texture.height / Mathf.Max(1, Texture.width); } }
    }

    /// <summary>
    /// The player's own stencils (drawings and pictures), kept as PNG files next to the save (in the browser,
    /// in its storage), and the shop's stencils drawn as smooth shapes. Every stencil is sprayed as one picture,
    /// exactly as drawn.
    /// </summary>
    public static class Stencils
    {
        public const int DrawingSize = 512;
        public const float DrawingMetres = 1.6f;

        static readonly Dictionary<string, Texture2D> files = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> shopArt = new Dictionary<string, Texture2D>();

        static string Dir { get { return Path.Combine(Application.persistentDataPath, "stencils"); } }
        static string PathOf(string id) { return Path.Combine(Dir, id + ".png"); }

        public static string DrawingId(int slot) { return Graffiti.CustomId(slot); }
        public static string PictureId(int slot) { return Graffiti.PictureId(slot); }

        /// <summary>A drawing's or picture's picture, or null if that slot is empty.</summary>
        public static Texture2D Load(string id)
        {
            Texture2D tex;
            if (files.TryGetValue(id, out tex)) return tex;
            tex = null;
            try
            {
                var path = PathOf(id);
                if (File.Exists(path))
                {
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
                    if (!tex.LoadImage(File.ReadAllBytes(path))) { Object.Destroy(tex); tex = null; }
                }
            }
            catch (System.Exception e) { Debug.LogWarning("BoomYard: stencil " + id + ": " + e.Message); tex = null; }
            files[id] = tex;
            return tex;
        }

        public static void Save(string id, Texture2D tex)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllBytes(PathOf(id), tex.EncodeToPNG());
            }
            catch (System.Exception e) { Debug.LogWarning("BoomYard: saving stencil " + id + ": " + e.Message); }
            files[id] = tex;
            Spray.StencilsVersion++;
        }

        public static void Delete(string id)
        {
            try { if (File.Exists(PathOf(id))) File.Delete(PathOf(id)); }
            catch (System.Exception e) { Debug.LogWarning("BoomYard: deleting stencil " + id + ": " + e.Message); }
            files[id] = null;
            Spray.StencilsVersion++;
        }

        public static bool Has(string id) { return Load(id) != null; }

        /// <summary>The stencil an id names (shop, drawing or picture), in the can's colour where it has one; null if none.</summary>
        public static StencilImage Get(string id, SprayColor can)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Graffiti.CustomSlots; i++)
                if (id == DrawingId(i)) { var t = Load(id); return t == null ? null : new StencilImage { Id = id, Name = "Drawing " + (i + 1), Texture = t, Width = DrawingMetres }; }
            for (int i = 0; i < Graffiti.PictureSlots; i++)
                if (id == PictureId(i))
                {
                    var t = Load(id);
                    if (t == null) return null;
                    float longest = Graffiti.PictureMetres;
                    return new StencilImage { Id = id, Name = "Picture " + (i + 1), Texture = t, Width = t.width >= t.height ? longest : longest * t.width / t.height };
                }
            var st = Graffiti.Find(id);
            if (st == null) return null;
            return new StencilImage { Id = id, Name = st.Name, Texture = Smooth(st, can), Width = st.Width * 0.1f, Glow = can.Glow };
        }

        /// <summary>
        /// A shop stencil (a grid of colour codes) drawn as smooth shapes: each colour's cells become a field that is
        /// blended between cell centres and cut at the halfway line, so a staircase of cells reads as a curve and
        /// a lone cell as a dot, with a soft sprayed edge.
        /// </summary>
        public static Texture2D Smooth(Stencil st, SprayColor can)
        {
            string key = st.Id + "|" + can.Id;
            Texture2D cached;
            if (shopArt.TryGetValue(key, out cached) && cached != null) return cached;
            const int k = 16;   // pixels per cell
            int gw = st.Width, gh = st.Height, w = gw * k + 2 * k, h = gh * k + 2 * k;
            // Which codes are used, and each cell's code.
            var codes = new List<char>();
            for (int y = 0; y < gh; y++) for (int x = 0; x < gw; x++) { char c = st.At(x, y); if (c != '.' && !codes.Contains(c)) codes.Add(c); }
            var px = new Color32[w * h];
            for (int py = 0; py < h; py++)
                for (int pxl = 0; pxl < w; pxl++)
                {
                    // Cell coordinates of this pixel (cell centres at integers), top row first.
                    float cx = (pxl + 0.5f) / k - 1f - 0.5f, cy = (h - 1 - py + 0.5f) / k - 1f - 0.5f;
                    float best = 0f, any = 0f;
                    char bestCode = '.';
                    foreach (var code in codes)
                    {
                        float f = Field(st, code, cx, cy);
                        if (f > best) { best = f; bestCode = code; }
                    }
                    any = FieldAny(st, cx, cy);
                    float a = Mathf.Clamp01((any - 0.44f) / 0.12f);
                    if (a <= 0f) { px[py * w + pxl] = new Color32(0, 0, 0, 0); continue; }
                    var col = Graffiti.Paint(bestCode, can) ?? can;
                    px[py * w + pxl] = new Color32((byte)(col.R * 255), (byte)(col.G * 255), (byte)(col.B * 255), (byte)(a * 255));
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            tex.SetPixels32(px);
            tex.Apply(true);
            shopArt[key] = tex;
            return tex;
        }

        /// <summary>
        /// How much of the neighbourhood of (x, y) is <paramref name="code"/> (any paint when it is '\0'): a
        /// Gaussian-weighted share of the nearby cells, so corners round off and diagonals become straight edges.
        /// </summary>
        static float Field(Stencil st, char code, float x, float y)
        {
            const float sigma = 0.62f;
            int cx = Mathf.RoundToInt(x), cy = Mathf.RoundToInt(y);
            float sum = 0f, total = 0f;
            for (int j = -2; j <= 2; j++)
                for (int i = -2; i <= 2; i++)
                {
                    int gx = cx + i, gy = cy + j;
                    float dx = gx - x, dy = gy - y;
                    float w = Mathf.Exp(-(dx * dx + dy * dy) / (2f * sigma * sigma));
                    total += w;
                    char c = st.At(gx, gy);
                    if (code == '\0' ? c != '.' : c == code) sum += w;
                }
            return sum / total;
        }

        static float FieldAny(Stencil st, float x, float y) { return Field(st, '\0', x, y); }

        /// <summary>
        /// Saves from before drawings and pictures were files (16 × 16 grids and 48-cell pictures in the save):
        /// turns them into files once, then clears them from the save.
        /// </summary>
        public static void MigrateOldSave(SaveData save)
        {
            bool changed = false;
            for (int i = 0; i < save.customStencils.Count && i < Graffiti.CustomSlots; i++)
            {
                var grid = save.customStencils[i];
                if (Graffiti.Empty(grid) || Has(DrawingId(i))) continue;
                Save(DrawingId(i), Smooth(Graffiti.FromGrid(DrawingId(i), "Drawing", grid), Graffiti.Color(save.sprayColor)));
                changed = true;
            }
            for (int i = 0; i < save.pictures.Count && i < Graffiti.PictureSlots; i++)
            {
                var st = Graffiti.DecodePicture(save.pictures[i], PictureId(i), "Picture");
                if (st == null || Has(PictureId(i))) continue;
                Save(PictureId(i), FromRgba(st.Rgba, st.PicWidth, st.PicHeight));
                changed = true;
            }
            if (save.customStencils.Count > 0 || save.pictures.Count > 0) { save.customStencils.Clear(); save.pictures.Clear(); changed = true; }
            if (changed) SaveService.Save();
        }

        /// <summary>A texture from RGBA bytes, top row first.</summary>
        public static Texture2D FromRgba(byte[] rgba, int w, int h)
        {
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = (y * w + x) * 4;
                    px[(h - 1 - y) * w + x] = new Color32(rgba[i], rgba[i + 1], rgba[i + 2], rgba[i + 3]);
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }
    }
}
