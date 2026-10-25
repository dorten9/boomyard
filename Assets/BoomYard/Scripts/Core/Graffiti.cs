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
    /// letters are fixed colours (see <see cref="Graffiti.Fixed"/>).
    /// </summary>
    public sealed class Stencil
    {
        public string Id, Name;
        public string[] Rows;
        public int Width { get { int w = 0; foreach (var r in Rows) w = Math.Max(w, r.Length); return w; } }
        public int Height { get { return Rows.Length; } }

        public char At(int x, int y)
        {
            if (y < 0 || y >= Rows.Length) return '.';
            var r = Rows[y];
            return x >= 0 && x < r.Length ? r[x] : '.';
        }
    }

    /// <summary>One cell of a stencil to paint: column and row from the top left, and its colour code.</summary>
    public struct StencilCell
    {
        public int X, Y;
        public char Code;
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
                    if (c != '.' && (c == '#' || Fixed(c) != null)) list.Add(new StencilCell { X = x, Y = y, Code = c });
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
