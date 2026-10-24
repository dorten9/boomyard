using System.Collections.Generic;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>A generated PBR surface: albedo with smoothness in alpha, and a tangent-space normal map.</summary>
    public sealed class Surface
    {
        public Texture2D Albedo, Normal;
        public float Tiling = 1f;      // metres per texture repeat
    }

    /// <summary>The pixels of a surface before they become textures (Tools/SimTest renders these to PNG previews).</summary>
    public sealed class RawSurface
    {
        public string Name;
        public int Size;
        public Color[] Albedo;
        public float[] Height;
        public float Tiling, Strength;
    }

    /// <summary>
    /// Procedural, tileable PBR textures (no image assets): asphalt, concrete, brick, plaster, wood, painted metal,
    /// rust, grass, tyre rubber and chain-link. Each is built from a height field (seamless value noise and cellular
    /// noise) that also gives its normal map, so light catches cracks, mortar and grain.
    /// </summary>
    public static class TextureLib
    {
        static readonly Dictionary<string, Surface> cache = new Dictionary<string, Surface>();

        public static Surface Get(string name)
        {
            Surface s;
            if (cache.TryGetValue(name, out s)) return s;
            s = ToTextures(Generate(name));
            cache[name] = s;
            return s;
        }

        /// <summary>Resolution scale: 0.5 on the Low tier (web), so generating the surfaces does not stall loading.</summary>
        public static float Scale = 1f;

        static int Res(int n) { return System.Math.Max(64, (int)(n * Scale)); }

        public static readonly string[] Names = { "asphalt", "concrete", "brick", "plaster", "wood", "paint", "rust", "grass", "tyre", "chainlink", "roof" };

        public static RawSurface Generate(string name)
        {
            switch (name)
            {
                case "asphalt": return Asphalt();
                case "concrete": return Concrete();
                case "brick": return Brick();
                case "plaster": return Plaster();
                case "wood": return Wood();
                case "paint": return Paint();
                case "rust": return Rust();
                case "grass": return Grass();
                case "tyre": return Tyre();
                case "chainlink": return ChainLink();
                case "roof": return Roof();
                default: return Concrete();
            }
        }

        // ---------- surfaces ----------

        static RawSurface Asphalt()
        {
            int n = Res(512);
            var h = new float[n * n];
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float grit = Fbm(u, v, 64, 3, 11);                       // fine aggregate
                    float stones = Cell(u, v, 40, 3) ;                        // embedded stones
                    float patch = Fbm(u, v, 3, 4, 5);                         // repaired patches, wear
                    float crack = Crack(u, v, 5, 21) * Mathf.Clamp01((Fbm(u, v, 3, 2, 23) - 0.5f) * 4f);
                    float stain = Mathf.Clamp01((Fbm(u, v, 2, 3, 31) - 0.62f) * 5f);   // oil stains
                    float hh = 0.5f * grit + 0.35f * (1f - stones) - 0.6f * crack;
                    h[y * n + x] = hh;
                    float tone = 0.19f + 0.08f * grit + 0.06f * (1f - stones) + 0.05f * (patch - 0.5f);
                    tone *= 1f - 0.5f * crack - 0.35f * stain;
                    var col = new Color(tone * 1.02f, tone, tone * 0.97f);
                    float smooth = 0.18f + 0.12f * (1f - grit) + 0.35f * stain;
                    col.a = smooth;
                    c[y * n + x] = col;
                }
            return Make("asphalt", n, c, h, 3.5f, 4f);
        }

        static RawSurface Concrete()
        {
            int n = Res(512);
            var h = new float[n * n];
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float fine = Fbm(u, v, 48, 3, 3);
                    float large = Fbm(u, v, 4, 4, 9);
                    float pores = Mathf.Clamp01((Cell(u, v, 70, 17) - 0.08f) * 12f);
                    float crack = Crack(u, v, 3, 7) * 0.7f;
                    h[y * n + x] = 0.4f * fine + 0.3f * pores - 0.5f * crack;
                    float tone = 0.46f + 0.08f * (large - 0.5f) + 0.05f * (fine - 0.5f);
                    tone *= 0.85f + 0.15f * pores;
                    tone *= 1f - 0.4f * crack;
                    var col = new Color(tone, tone * 0.985f, tone * 0.96f, 0.22f + 0.1f * large);
                    c[y * n + x] = col;
                }
            return Make("concrete", n, c, h, 3f, 2.5f);
        }

        /// <summary>Running-bond brick, 21.5 × 6.5 cm with 1 cm mortar; one texture is 4 bricks wide.</summary>
        static RawSurface Brick()
        {
            int n = Res(512);
            var h = new float[n * n];
            var c = new Color[n * n];
            const int cols = 4, rows = 13;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float rv = v * rows;
                    int row = Mathf.FloorToInt(rv);
                    float ru = u * cols + (row % 2 == 0 ? 0f : 0.5f);
                    int col = Mathf.FloorToInt(ru);
                    float fu = ru - col, fv = rv - row;
                    float mu = 0.045f, mv = 0.13f;
                    float edge = Mathf.Min(Mathf.Min(fu, 1f - fu) / mu, Mathf.Min(fv, 1f - fv) / mv);
                    float brick = Mathf.Clamp01(edge - 0.6f) ;
                    float id = Hash((col % cols + cols) % cols, row, 77);
                    float fine = Fbm(u, v, 64, 3, 41);
                    float chip = Mathf.Clamp01((Fbm(u, v, 16, 3, 43) - 0.7f) * 6f);
                    h[y * n + x] = brick * (0.75f + 0.25f * fine) - chip * 0.3f;
                    Color b = Color.Lerp(new Color(0.42f, 0.17f, 0.1f), new Color(0.58f, 0.3f, 0.2f), id);
                    b = Color.Lerp(b, new Color(0.3f, 0.12f, 0.08f), Hash(col, row, 3) * 0.35f);
                    b *= 0.88f + 0.24f * fine;
                    Color mortar = new Color(0.55f, 0.53f, 0.49f) * (0.85f + 0.2f * fine);
                    var outc = Color.Lerp(mortar, b, brick);
                    outc.a = 0.2f + 0.1f * fine;
                    c[y * n + x] = outc;
                }
            return Make("brick", n, c, h, 0.92f, 3f);
        }

        static RawSurface Plaster()
        {
            int n = Res(512);
            var h = new float[n * n];
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float stucco = Fbm(u, v, 40, 4, 51);
                    float blot = Fbm(u, v, 3, 4, 53);
                    float streak = Fbm(u * 0.4f, v * 3f, 6, 3, 55);   // rain streaks run down
                    h[y * n + x] = stucco;
                    float tone = 0.72f + 0.08f * (stucco - 0.5f) - 0.08f * Mathf.Clamp01(blot - 0.45f) - 0.06f * Mathf.Clamp01(streak - 0.5f);
                    c[y * n + x] = new Color(tone, tone * 0.93f, tone * 0.82f, 0.12f);
                }
            return Make("plaster", n, c, h, 2.5f, 1.6f);
        }

        static RawSurface Wood()
        {
            int n = Res(256);
            var h = new float[n * n];
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    // Grain runs along v; boards are 1/4 of the texture wide.
                    float board = u * 4f;
                    int id = Mathf.FloorToInt(board);
                    float fu = board - id;
                    float warp = Fbm(u, v, 4, 3, 61 + id) * 0.6f;
                    float rings = Mathf.Abs(Mathf.Sin((fu * 3f + warp * 4f + v * 0.3f) * Mathf.PI * 3f));
                    float fine = Fbm(u * 8f, v, 32, 3, 63);
                    float seam = Mathf.Clamp01(Mathf.Min(fu, 1f - fu) / 0.025f);
                    h[y * n + x] = seam * (0.6f + 0.2f * rings + 0.2f * fine);
                    float tone = 0.34f + 0.07f * rings + 0.07f * fine + 0.1f * (Hash(id, 0, 65) - 0.5f);
                    float grey = Fbm(u, v, 3, 3, 67) * 0.5f;   // weathering greys the wood
                    var col = Color.Lerp(new Color(tone * 1.12f, tone * 0.86f, tone * 0.62f), new Color(tone, tone * 0.97f, tone * 0.92f), grey) * (0.55f + 0.45f * seam);
                    col.a = 0.25f;
                    c[y * n + x] = col;
                }
            return Make("wood", n, c, h, 1.2f, 2f);
        }

        /// <summary>Painted metal: near-white so _BaseColor tints it, with dust, scuffs and a few scratches.</summary>
        static RawSurface Paint()
        {
            int n = Res(256);
            var h = new float[n * n];
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float dust = Fbm(u, v, 6, 4, 71);
                    float scratch = Scratches(u, v, 73);
                    float orange = Fbm(u, v, 90, 2, 75);   // orange-peel of the paint
                    h[y * n + x] = 0.5f + 0.1f * orange - 0.5f * scratch;
                    float tone = 0.96f - 0.12f * Mathf.Clamp01(dust - 0.5f) - 0.35f * scratch;
                    c[y * n + x] = new Color(tone, tone, tone, Mathf.Clamp01(0.95f - 0.45f * Mathf.Clamp01(dust - 0.45f) - 0.6f * scratch));
                }
            return Make("paint", n, c, h, 1.5f, 0.6f);
        }

        static RawSurface Rust()
        {
            int n = Res(256);
            var h = new float[n * n];
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float rust = Mathf.Clamp01((Fbm(u, v, 5, 5, 81) - 0.42f) * 4f);
                    float pit = Fbm(u, v, 50, 3, 83);
                    h[y * n + x] = 0.5f + 0.3f * rust * pit;
                    var metal = new Color(0.45f, 0.46f, 0.47f) * (0.85f + 0.3f * pit);
                    var rc = Color.Lerp(new Color(0.42f, 0.2f, 0.09f), new Color(0.6f, 0.33f, 0.14f), pit);
                    var col = Color.Lerp(metal, rc, rust);
                    col.a = Mathf.Lerp(0.55f, 0.12f, rust);
                    c[y * n + x] = col;
                }
            return Make("rust", n, c, h, 1.2f, 1.5f);
        }

        static RawSurface Grass()
        {
            int n = Res(512);
            var h = new float[n * n];
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float blades = Fbm(u, v, 128, 2, 91);
                    float clumps = Fbm(u, v, 12, 3, 93);
                    float dirt = Mathf.Clamp01((Fbm(u, v, 4, 4, 95) - 0.55f) * 4f);
                    h[y * n + x] = blades * 0.7f + clumps * 0.3f;
                    var green = Color.Lerp(new Color(0.18f, 0.22f, 0.08f), new Color(0.36f, 0.38f, 0.14f), blades * 0.6f + clumps * 0.4f);
                    var soil = new Color(0.28f, 0.21f, 0.14f) * (0.8f + 0.3f * blades);
                    var col = Color.Lerp(green, soil, dirt * 0.8f);
                    col.a = 0.08f;
                    c[y * n + x] = col;
                }
            return Make("grass", n, c, h, 2.5f, 2f);
        }

        /// <summary>Tyre rubber with tread blocks running around (u) and across (v) the tread.</summary>
        static RawSurface Tyre()
        {
            int n = Res(256);
            var h = new float[n * n];
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float blocks = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 32f + (v > 0.5f ? 1.2f : 0f))) > 0.25f ? 1f : 0f;
                    float groove = Mathf.Abs(v - 0.5f) < 0.03f || Mathf.Abs(v - 0.2f) < 0.02f || Mathf.Abs(v - 0.8f) < 0.02f ? 0f : 1f;
                    float fine = Fbm(u, v, 64, 2, 101);
                    float hh = blocks * groove;
                    h[y * n + x] = hh * 0.8f + fine * 0.2f;
                    float tone = 0.06f + 0.03f * fine + 0.02f * hh;
                    c[y * n + x] = new Color(tone, tone, tone, 0.25f + 0.1f * hh);
                }
            return Make("tyre", n, c, h, 1f, 3f);
        }

        /// <summary>Galvanised chain-link: diamond wire mesh in alpha (used with the transparent template).</summary>
        static RawSurface ChainLink()
        {
            int n = Res(256);
            var h = new float[n * n];
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n * 8f, v = y / (float)n * 8f;
                    float fa = Frac(u + v), fb = Frac(u - v);
                    float a = Mathf.Min(fa, 1f - fa), b = Mathf.Min(fb, 1f - fb);
                    float wire = Mathf.Max(Mathf.Clamp01(1.5f - a / 0.05f), Mathf.Clamp01(1.5f - b / 0.05f));
                    h[y * n + x] = wire;
                    float tone = 0.62f + 0.1f * Fbm(x / (float)n, y / (float)n, 16, 2, 111);
                    c[y * n + x] = new Color(tone * wire, tone * wire, tone * 1.02f * wire, wire);   // premultiplied
                }
            return Make("chainlink", n, c, h, 0.5f, 1f);
        }

        static RawSurface Roof()
        {
            int n = Res(256);
            var h = new float[n * n];
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float rv = v * 8f;
                    int row = Mathf.FloorToInt(rv);
                    float fv = rv - row;
                    float ru = u * 6f + (row % 2) * 0.5f;
                    int col = Mathf.FloorToInt(ru);
                    float fu = ru - col;
                    float shade = fv;   // each course is thicker at its lower edge
                    float gap = Mathf.Clamp01(Mathf.Min(fu, 1f - fu) / 0.03f);
                    float fine = Fbm(u, v, 48, 3, 121);
                    h[y * n + x] = shade * gap;
                    float tone = (0.2f + 0.08f * Hash(col, row, 123) + 0.05f * fine) * (0.6f + 0.4f * gap) * (0.75f + 0.25f * shade);
                    c[y * n + x] = new Color(tone * 1.05f, tone * 0.95f, tone * 0.92f, 0.15f);
                }
            return Make("roof", n, c, h, 2f, 2.5f);
        }

        // ---------- texture assembly ----------

        static RawSurface Make(string name, int n, Color[] albedo, float[] height, float tiling, float strength)
        {
            return new RawSurface { Name = name, Size = n, Albedo = albedo, Height = height, Tiling = tiling, Strength = strength };
        }

        /// <summary>
        /// Normal map from the height field (wrapping, so it tiles). Stored as (x, y, z, 1): reads correctly with both
        /// URP decoders used on phones (RGB) and desktop (RG-or-AG).
        /// </summary>
        public static Color[] Normals(RawSurface r)
        {
            int n = r.Size;
            var h = r.Height;
            var nrm = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float l = h[y * n + (x + n - 1) % n], rr = h[y * n + (x + 1) % n];
                    float d = h[((y + n - 1) % n) * n + x], u = h[((y + 1) % n) * n + x];
                    var v = new Vector3((l - rr) * r.Strength, (d - u) * r.Strength, 1f).normalized;
                    nrm[y * n + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
                }
            return nrm;
        }

        static Surface ToTextures(RawSurface r)
        {
            int n = r.Size;
            var a = new Texture2D(n, n, TextureFormat.RGBA32, true, false) { name = r.Name, wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            a.SetPixels(r.Albedo);
            a.Apply(true, true);
            var nt = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { name = r.Name + "_n", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            nt.SetPixels(Normals(r));
            nt.Apply(true, true);
            return new Surface { Albedo = a, Normal = nt, Tiling = r.Tiling };
        }

        // ---------- noise ----------

        static float Frac(float v) { return v - Mathf.Floor(v); }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffffff) / 16777215f;
            }
        }

        /// <summary>Seamless value noise: <paramref name="period"/> cells across the texture.</summary>
        static float Value(float u, float v, int period, int seed)
        {
            float x = u * period, y = v * period;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int ax = ((x0 % period) + period) % period, ay = ((y0 % period) + period) % period;
            int bx = (ax + 1) % period, by = (ay + 1) % period;
            float a = Hash(ax, ay, seed), b = Hash(bx, ay, seed), c = Hash(ax, by, seed), d = Hash(bx, by, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float u, float v, int period, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Value(u, v, period, seed + i * 13) * amp;
                norm += amp;
                amp *= 0.5f;
                period *= 2;
            }
            return sum / norm;
        }

        /// <summary>Seamless cellular noise: distance to the nearest random point (0 at points, ~1 between).</summary>
        static float Cell(float u, float v, int period, int seed)
        {
            float x = u * period, y = v * period;
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            float best = 9f;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    int gx = cx + i, gy = cy + j;
                    int wx = ((gx % period) + period) % period, wy = ((gy % period) + period) % period;
                    float px = gx + Hash(wx, wy, seed), py = gy + Hash(wx, wy, seed + 1);
                    float d = (px - x) * (px - x) + (py - y) * (py - y);
                    if (d < best) best = d;
                }
            return Mathf.Clamp01(Mathf.Sqrt(best));
        }

        /// <summary>Thin dark lines along the edges between large cells: cracks.</summary>
        static float Crack(float u, float v, int period, int seed)
        {
            float x = u * period, y = v * period;
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            float d1 = 9f, d2 = 9f;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    int gx = cx + i, gy = cy + j;
                    int wx = ((gx % period) + period) % period, wy = ((gy % period) + period) % period;
                    float px = gx + Hash(wx, wy, seed), py = gy + Hash(wx, wy, seed + 1);
                    float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                    if (d < d1) { d2 = d1; d1 = d; }
                    else if (d < d2) d2 = d;
                }
            float wobble = Fbm(u, v, period * 8, 2, seed + 5) * 0.06f;
            return Mathf.Clamp01(1f - (d2 - d1 + wobble - 0.03f) / 0.04f) * Mathf.Clamp01((Fbm(u, v, period, 2, seed + 9) - 0.35f) * 3f);
        }

        static float Scratches(float u, float v, int seed)
        {
            float s = 0f;
            for (int k = 0; k < 6; k++)
            {
                float cx = Hash(k, 0, seed), cy = Hash(k, 1, seed), ang = Hash(k, 2, seed) * Mathf.PI, len = 0.05f + 0.15f * Hash(k, 3, seed);
                float dx = Frac(u - cx + 0.5f) - 0.5f, dy = Frac(v - cy + 0.5f) - 0.5f;
                float along = dx * Mathf.Cos(ang) + dy * Mathf.Sin(ang), across = -dx * Mathf.Sin(ang) + dy * Mathf.Cos(ang);
                if (Mathf.Abs(along) < len) s = Mathf.Max(s, Mathf.Clamp01(1f - Mathf.Abs(across) / 0.0025f));
            }
            return s * 0.45f;
        }
    }
}
