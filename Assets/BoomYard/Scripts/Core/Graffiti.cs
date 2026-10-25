using System;
using System.Collections.Generic;

namespace BoomYard.Core
{
    /// <summary>A spray paint colour. Glow paints shine at dusk; metallic ones are chrome and gold.</summary>
    public sealed class SprayColor
    {
        public string Id, Name;
        public float R, G, B;
        public bool Glow;
        public SprayColor(string id, string name, float r, float g, float b, bool glow = false) { Id = id; Name = name; R = r; G = g; B = b; Glow = glow; }
    }

    /// <summary>A spray cap: how wide one pass of the can paints, in metres at arm's length.</summary>
    public sealed class SprayCap
    {
        public string Id, Name;
        public float Width;
        public SprayCap(string id, string name, float width) { Id = id; Name = name; Width = width; }
    }

    /// <summary>
    /// A stencil: a picture in cells, 10 cm each on the wall. '.' is bare wall, '#' is the can's colour and the
    /// letters are fixed colours (see <see cref="Graffiti.Fixed"/>). A picture stencil (made from a photo) has
    /// <see cref="Rgba"/> instead of rows: full colour, '*' in every cell that is not see-through.
    /// </summary>
    public sealed class Stencil
    {
        public string Id, Name;
        public string[] Rows;
        public byte[] Rgba;          // picture stencils: PicWidth × PicHeight pixels, top row first
        public int PicWidth, PicHeight;

        public bool Picture { get { return Rgba != null; } }
        public int Width { get { if (Picture) return PicWidth; int w = 0; foreach (var r in Rows) w = Math.Max(w, r.Length); return w; } }
        public int Height { get { return Picture ? PicHeight : Rows.Length; } }

        public char At(int x, int y)
        {
            if (Picture)
            {
                if (x < 0 || y < 0 || x >= PicWidth || y >= PicHeight) return '.';
                return Rgba[(y * PicWidth + x) * 4 + 3] >= 128 ? '*' : '.';
            }
            if (y < 0 || y >= Rows.Length) return '.';
            var r = Rows[y];
            return x >= 0 && x < r.Length ? r[x] : '.';
        }
    }

    /// <summary>One cell of a stencil to paint: column and row from the top left, its colour code ('*' for a picture's own colour in R, G, B).</summary>
    public struct StencilCell
    {
        public int X, Y;
        public char Code;
        public byte R, G, B;
    }

    /// <summary>
    /// Graffiti: spray colours, caps and stencils (all bought in the shop) and the player's own stencils, drawn
    /// in the stencil maker on a 16 × 16 grid and kept in the save as 256 colour codes each.
    /// </summary>
    public static class Graffiti
    {
        public const int CustomSize = 16, CustomSlots = 3;
        /// <summary>Codes the stencil maker paints with: the can's colour first, then the fixed colours.</summary>
        public const string MakerCodes = "#kwryobgp";

        public static readonly SprayColor[] Colors =
        {
            new SprayColor("red", "Fire Red", 0.82f, 0.1f, 0.08f),
            new SprayColor("black", "Black", 0.04f, 0.04f, 0.045f),
            new SprayColor("white", "White", 0.95f, 0.95f, 0.92f),
            new SprayColor("yellow", "Sun Yellow", 0.98f, 0.8f, 0.08f),
            new SprayColor("orange", "Orange", 1f, 0.45f, 0.05f),
            new SprayColor("blue", "Sky Blue", 0.15f, 0.5f, 0.95f),
            new SprayColor("green", "Lime", 0.4f, 0.85f, 0.15f),
            new SprayColor("pink", "Bubblegum", 1f, 0.42f, 0.7f),
            new SprayColor("purple", "Purple", 0.5f, 0.2f, 0.8f),
            new SprayColor("chrome", "Chrome", 0.78f, 0.8f, 0.83f),
            new SprayColor("gold", "Gold", 0.95f, 0.7f, 0.2f),
            new SprayColor("neongreen", "Neon Green", 0.3f, 1f, 0.3f, true),
            new SprayColor("neonpink", "Neon Pink", 1f, 0.25f, 0.75f, true),
        };

        public static readonly SprayCap[] Caps =
        {
            new SprayCap("standard", "Standard Cap", 0.12f),
            new SprayCap("skinny", "Skinny Cap", 0.05f),
            new SprayCap("fat", "Fat Cap", 0.24f),
        };

        public static SprayColor Color(string id)
        {
            foreach (var c in Colors) if (c.Id == id) return c;
            return Colors[0];
        }

        public static SprayCap Cap(string id)
        {
            foreach (var c in Caps) if (c.Id == id) return c;
            return Caps[0];
        }

        /// <summary>The fixed colour of a stencil code, or null for '#' (the can's colour) and '.' (nothing).</summary>
        public static SprayColor Fixed(char code)
        {
            switch (code)
            {
                case 'k': return Color("black");
                case 'w': return Color("white");
                case 'r': return Color("red");
                case 'y': return Color("yellow");
                case 'o': return Color("orange");
                case 'b': return Color("blue");
                case 'g': return Color("green");
                case 'p': return Color("pink");
                default: return null;
            }
        }

        /// <summary>The colour a cell is painted: the can's own for '#', a fixed one for a letter.</summary>
        public static SprayColor Paint(char code, SprayColor can)
        {
            if (code == '#') return can;
            return Fixed(code);
        }

        /// <summary>Every painted cell of a stencil, in order from the top (so a run reads like real spraying).</summary>
        public static List<StencilCell> Cells(Stencil s)
        {
            var list = new List<StencilCell>();
            for (int y = 0; y < s.Height; y++)
                for (int x = 0; x < s.Width; x++)
                {
                    char c = s.At(x, y);
                    if (c == '*')
                    {
                        int i = (y * s.PicWidth + x) * 4;
                        list.Add(new StencilCell { X = x, Y = y, Code = c, R = s.Rgba[i], G = s.Rgba[i + 1], B = s.Rgba[i + 2] });
                    }
                    else if (c != '.' && (c == '#' || Fixed(c) != null)) list.Add(new StencilCell { X = x, Y = y, Code = c });
                }
            return list;
        }

        // ---------- the player's own stencils ----------

        public static string Blank { get { return new string('.', CustomSize * CustomSize); } }

        /// <summary>A saved custom stencil is 256 codes from the maker's palette (or bare wall).</summary>
        public static bool Valid(string grid)
        {
            if (grid == null || grid.Length != CustomSize * CustomSize) return false;
            foreach (var c in grid) if (c != '.' && MakerCodes.IndexOf(c) < 0) return false;
            return true;
        }

        public static bool Empty(string grid)
        {
            if (!Valid(grid)) return true;
            foreach (var c in grid) if (c != '.') return false;
            return true;
        }

        /// <summary>Sets one cell of a custom grid (returns the new grid).</summary>
        public static string Set(string grid, int x, int y, char code)
        {
            if (!Valid(grid)) grid = Blank;
            if (x < 0 || y < 0 || x >= CustomSize || y >= CustomSize) return grid;
            var a = grid.ToCharArray();
            a[y * CustomSize + x] = code == '.' || MakerCodes.IndexOf(code) >= 0 ? code : '.';
            return new string(a);
        }

        public static Stencil FromGrid(string id, string name, string grid)
        {
            if (!Valid(grid)) grid = Blank;
            var rows = new string[CustomSize];
            for (int y = 0; y < CustomSize; y++) rows[y] = grid.Substring(y * CustomSize, CustomSize);
            return new Stencil { Id = id, Name = name, Rows = rows };
        }

        /// <summary>Id of the custom stencil in a save slot (0..2).</summary>
        public static string CustomId(int slot) { return "custom" + slot; }

        // ---------- pictures: a photo as a stencil ----------

        /// <summary>A picture stencil is at most this many cells across; on the wall it is about 2 m.</summary>
        public const int PictureSize = 48, PictureSlots = 6, PicturePrice = 500;
        public const float PictureMetres = 2f;

        /// <summary>The first picture is free, every one after it costs <see cref="PicturePrice"/> coins.</summary>
        public static int PictureCost(int madeBefore) { return madeBefore <= 0 ? 0 : PicturePrice; }

        public static string PictureId(int slot) { return "picture" + slot; }

        /// <summary>
        /// Turns an image (RGBA, top row first) into a picture stencil: scaled down to fit
        /// <see cref="PictureSize"/> (averaging the pixels under each cell), and, when its border is one plain
        /// colour, that background cut away (a flood from the edges), so a logo or a face on a plain backdrop
        /// comes out as just the shape.
        /// </summary>
        public static Stencil PictureFrom(byte[] src, int w, int h, string id, string name, bool cutBackground = true)
        {
            if (src == null || w <= 0 || h <= 0 || src.Length < w * h * 4) return null;
            float scale = Math.Min(1f, PictureSize / (float)Math.Max(w, h));
            int pw = Math.Max(1, (int)Math.Round(w * scale)), ph = Math.Max(1, (int)Math.Round(h * scale));
            var dst = new byte[pw * ph * 4];
            for (int y = 0; y < ph; y++)
                for (int x = 0; x < pw; x++)
                {
                    int x0 = x * w / pw, x1 = Math.Max(x0 + 1, (x + 1) * w / pw), y0 = y * h / ph, y1 = Math.Max(y0 + 1, (y + 1) * h / ph);
                    long r = 0, g = 0, b = 0, a = 0, n = 0;
                    for (int sy = y0; sy < y1; sy++)
                        for (int sx = x0; sx < x1; sx++)
                        {
                            int i = (sy * w + sx) * 4;
                            int alpha = src[i + 3];
                            r += src[i] * alpha; g += src[i + 1] * alpha; b += src[i + 2] * alpha; a += alpha; n++;
                        }
                    int o = (y * pw + x) * 4;
                    if (a > 0) { dst[o] = (byte)(r / a); dst[o + 1] = (byte)(g / a); dst[o + 2] = (byte)(b / a); }
                    dst[o + 3] = (byte)(a / n);
                }
            if (cutBackground) CutBackground(dst, pw, ph);
            return new Stencil { Id = id, Name = name, Rgba = dst, PicWidth = pw, PicHeight = ph };
        }

        static void CutBackground(byte[] px, int w, int h)
        {
            // The backdrop: the average of the border, if the border is nearly one colour.
            long r = 0, g = 0, b = 0, n = 0;
            var border = new List<int>();
            for (int x = 0; x < w; x++) { border.Add(x); border.Add((h - 1) * w + x); }
            for (int y = 1; y < h - 1; y++) { border.Add(y * w); border.Add(y * w + w - 1); }
            foreach (var p in border) { if (px[p * 4 + 3] < 128) continue; r += px[p * 4]; g += px[p * 4 + 1]; b += px[p * 4 + 2]; n++; }
            if (n < border.Count / 2) return;   // the border is see-through already
            int br = (int)(r / n), bg = (int)(g / n), bb = (int)(b / n);
            int close = 0;
            foreach (var p in border) if (Near(px, p, br, bg, bb, 60)) close++;
            if (close < border.Count * 0.7f) return;   // a photo with no plain backdrop: keep it whole
            var seen = new bool[w * h];
            var stack = new Stack<int>();
            foreach (var p in border) if (Near(px, p, br, bg, bb, 60)) { stack.Push(p); seen[p] = true; }
            while (stack.Count > 0)
            {
                int p = stack.Pop();
                px[p * 4 + 3] = 0;
                int x = p % w, y = p / w;
                if (x > 0) Visit(px, seen, stack, p - 1, br, bg, bb);
                if (x < w - 1) Visit(px, seen, stack, p + 1, br, bg, bb);
                if (y > 0) Visit(px, seen, stack, p - w, br, bg, bb);
                if (y < h - 1) Visit(px, seen, stack, p + w, br, bg, bb);
            }
        }

        static void Visit(byte[] px, bool[] seen, Stack<int> stack, int p, int r, int g, int b)
        {
            if (seen[p]) return;
            seen[p] = true;
            if (Near(px, p, r, g, b, 60)) stack.Push(p);
        }

        static bool Near(byte[] px, int p, int r, int g, int b, int tol)
        {
            if (px[p * 4 + 3] < 128) return true;
            int dr = px[p * 4] - r, dg = px[p * 4 + 1] - g, db = px[p * 4 + 2] - b;
            return dr * dr + dg * dg + db * db <= tol * tol;
        }

        /// <summary>A picture stencil as text for the save: "width,height,base64 of the RGBA".</summary>
        public static string EncodePicture(Stencil s)
        {
            if (s == null || !s.Picture) return "";
            return s.PicWidth + "," + s.PicHeight + "," + Convert.ToBase64String(s.Rgba);
        }

        public static Stencil DecodePicture(string text, string id, string name)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var parts = text.Split(new[] { ',' }, 3);
            int w, h;
            if (parts.Length != 3 || !int.TryParse(parts[0], out w) || !int.TryParse(parts[1], out h)) return null;
            if (w <= 0 || h <= 0 || w > PictureSize || h > PictureSize) return null;
            byte[] rgba;
            try { rgba = Convert.FromBase64String(parts[2]); }
            catch (FormatException) { return null; }
            if (rgba.Length != w * h * 4) return null;
            return new Stencil { Id = id, Name = name, Rgba = rgba, PicWidth = w, PicHeight = h };
        }

        /// <summary>Metres per cell on the wall: 10 cm for drawn stencils, a picture is about 2 m across.</summary>
        public static float CellSize(Stencil s) { return s.Picture ? PictureMetres / Math.Max(s.Width, s.Height) : 0.1f; }

        // ---------- the stencils sold in the shop ----------

        static readonly string[] Font =
        {
            // A 5 × 7 block font for the lettering stencils, A to Z then '!'.
            ".###.#...##...#######...##...##...#", "####.#...##...#####.#...##...#####.", ".#####....#....#....#....#.....####", "####.#...##...##...##...##...#####.",
            "######....#....####.#....#....#####", "######....#....####.#....#....#....", ".####....#....#..###...##...#.####.", "#...##...##...#######...##...##...#",
            "#####..#....#....#....#....#..#####", "....#....#....#....##...##...#.###.", "#...##..#.#.#..##...#.#..#..#.#...#", "#....#....#....#....#....#....#####",
            "#...###.###.#.##.#.##...##...##...#", "#...###..##.#.##..###...##...##...#", ".###.#...##...##...##...##...#.###.", "####.#...##...#####.#....#....#....",
            ".###.#...##...##...##.#.##..#..##.#", "####.#...##...#####.#.#..#..#.#...#", ".#####....#.....###.....#....#####.", "#####..#....#....#....#....#....#..",
            "#...##...##...##...##...##...#.###.", "#...##...##...##...#.#.#..#.#...#..", "#...##...##...##.#.##.#.##.#.#.#.#.", "#...##...#.#.#...#...#.#.#...##...#",
            "#...##...#.#.#...#....#....#....#..", "#####....#...#...#...#...#....#####", "..#....#....#....#....#.........#..",
        };

        static string[] Glyph(char ch)
        {
            int i = ch == '!' ? 26 : ch - 'A';
            if (i < 0 || i >= Font.Length) return null;
            var f = Font[i];
            var rows = new string[7];
            for (int y = 0; y < 7; y++) rows[y] = f.Substring(y * 5, 5);
            return rows;
        }

        /// <summary>Block letters in the can's colour with a black drop shadow, as a stencil.</summary>
        public static Stencil Lettering(string id, string name, string word)
        {
            int w = word.Length * 6 + 1, h = 8;
            var g = new char[h, w];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) g[y, x] = '.';
            for (int k = 0; k < word.Length; k++)
            {
                var glyph = Glyph(char.ToUpperInvariant(word[k]));
                if (glyph == null) continue;
                for (int y = 0; y < 7; y++)
                    for (int x = 0; x < 5; x++)
                        if (glyph[y][x] == '#')
                        {
                            int gx = k * 6 + x, gy = y;
                            g[gy, gx] = '#';
                            if (g[gy + 1, gx + 1] == '.') g[gy + 1, gx + 1] = 'k';
                        }
            }
            var rows = new string[h];
            for (int y = 0; y < h; y++)
            {
                var a = new char[w];
                for (int x = 0; x < w; x++) a[x] = g[y, x];
                rows[y] = new string(a);
            }
            return new Stencil { Id = id, Name = name, Rows = rows };
        }

        public static readonly Stencil[] Stencils =
        {
            new Stencil { Id = "heart", Name = "Heart", Rows = new[]
            {
                "................", "..####....####..", ".######..######.", "################", "################", "################",
                "################", ".##############.", "..############..", "...##########...", "....########....", ".....######.....",
                "......####......", ".......##.......",
            } },
            new Stencil { Id = "star", Name = "Star", Rows = new[]
            {
                ".......##.......", ".......##.......", "......####......", "......####......", ".....######.....", "################",
                ".##############.", "..############..", "...##########...", "....########....", "....########....", "...####..####...",
                "...###....###...", "..###......###..", "..##........##..",
            } },
            new Stencil { Id = "smiley", Name = "Smiley", Rows = new[]
            {
                ".....######.....", "...##########...", "..############..", ".####kk##kk####.", ".####kk##kk####.", "################",
                "################", "################", "###k########k###", "####k######k####", ".####kkkkkk####.", ".##############.",
                "..############..", "...##########...", ".....######.....",
            } },
            new Stencil { Id = "bolt", Name = "Lightning", Rows = new[]
            {
                ".........#####..", "........#####...", ".......#####....", "......#####.....", ".....#####......", "....##########..",
                "...##########...", ".........####...", "........####....", ".......####.....", "......####......", ".....###........",
                "....###.........", "...##...........", "..#.............",
            } },
            new Stencil { Id = "skull", Name = "Skull", Rows = new[]
            {
                "....########....", "..############..", ".##############.", ".##############.", "################", "###kkk####kkk###",
                "###kkk####kkk###", "###kkk####kkk###", "################", ".######kk######.", "..############..", "...##########...",
                "...#k#k##k#k#...", "...##########...", "....########....",
            } },
            new Stencil { Id = "flame", Name = "Flame", Rows = new[]
            {
                ".......#........", "......##........", "......###.......", ".....####...#...", "....#####..##...", "....######.###..",
                "...###########..", "...####yy#####..", "..####yyyy#####.", "..###yyyyyy####.", "..###yywwyy####.", "..###yywwwyy###.",
                "...###yywwyy###.", "....###yyyy###..", ".....########...",
            } },
            new Stencil { Id = "cat", Name = "Cat", Rows = new[]
            {
                ".#............#.", ".##..........##.", ".###........###.", ".##############.", ".##############.", "################",
                "###ww######ww###", "###wk######kw###", "################", "#######kk#######", "###k###kk###k###", "####kkk##kkk####",
                ".##############.", "..############..", "....########....",
            } },
            new Stencil { Id = "crown", Name = "Crown", Rows = new[]
            {
                ".#.....##.....#.", ".##...####...##.", ".###.######.###.", ".##############.", ".##############.", ".###y######y###.",
                ".##yyy####yyy##.", ".###y######y###.", ".##############.", ".##############.", ".kkkkkkkkkkkkkk.",
            } },
            new Stencil { Id = "bomb", Name = "Bomb", Rows = new[]
            {
                "...........y.y..", "..........y.y...", ".........#y.....", "........#.......", ".....kkk#.......", "...kkkkkkkk.....",
                "..kkkkkkkkkk....", ".kkwwkkkkkkkk...", ".kkwkkkkkkkkk...", ".kkkkkkkkkkkk...", ".kkkkkkkkkkkk...", ".kkkkkkkkkkkk...",
                "..kkkkkkkkkk....", "...kkkkkkkk.....", ".....kkkk.......",
            } },
            Lettering("boom", "BOOM", "BOOM"),
            Lettering("kaboom", "KABOOM!", "KABOOM!"),
            Lettering("tag", "WAS HERE", "WAS HERE"),
        };

        public static Stencil Find(string id)
        {
            foreach (var s in Stencils) if (s.Id == id) return s;
            return null;
        }
    }
}
