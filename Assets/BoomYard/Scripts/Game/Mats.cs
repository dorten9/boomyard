using System.Collections.Generic;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// URP materials made from code. Each is a copy of a template material (Generated/Resources/Templates, made by
    /// the editor's PipelineSetup) so its shader variants exist in builds, with a procedural surface from
    /// <see cref="TextureLib"/>. Cached by key so every bin shares one material.
    /// </summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly Dictionary<string, Material> templates = new Dictionary<string, Material>();
        static Texture2D soft;
        static Mesh quad;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        static readonly int BumpScale = Shader.PropertyToID("_BumpScale");
        static readonly int Metallic = Shader.PropertyToID("_Metallic");
        static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        static Material Template(string name)
        {
            Material t;
            if (templates.TryGetValue(name, out t) && t != null) return t;
            t = Resources.Load<Material>("Templates/" + name);
            if (t == null)
            {
                // Editor before the first project setup: build the same thing from the shader.
                var shader = Shader.Find(name == "Smoke" ? "Universal Render Pipeline/Particles/Simple Lit" : name == "Sky" ? "Skybox/Procedural" : "Universal Render Pipeline/Lit");
                t = new Material(shader) { name = name };
                if (name.Contains("Tex")) { t.EnableKeyword("_NORMALMAP"); t.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"); t.SetFloat("_SmoothnessTextureChannel", 1f); }
                if (name.Contains("Emissive")) t.EnableKeyword("_EMISSION");
                Debug.LogWarning("BoomYard: template material " + name + " missing; run BoomYard → Run Project Setup.");
            }
            templates[name] = t;
            return t;
        }

        /// <summary>Copy of a template (for skies and anything changed per object).</summary>
        public static Material From(string template) { return new Material(Template(template)); }

        /// <summary>Plain PBR surface.</summary>
        public static Material Lit(Color color, float metallic = 0f, float smoothness = 0.3f)
        {
            string key = "lit" + color + metallic + smoothness;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Template("Lit")) { name = "Lit" };
            m.SetColor(BaseColor, color);
            m.SetFloat(Metallic, metallic);
            m.SetFloat(Smoothness, smoothness);
            cache[key] = m;
            return m;
        }

        /// <summary>
        /// Textured PBR surface: <paramref name="surface"/> is a <see cref="TextureLib"/> name, tinted by
        /// <paramref name="tint"/>; smoothness comes from the texture's alpha times <paramref name="smoothness"/>.
        /// <paramref name="scale"/> multiplies the surface's own tiling (UVs are in metres).
        /// </summary>
        public static Material Surface(string surface, Color tint, float metallic = 0f, float smoothness = 1f, float scale = 1f, float bump = 1f)
        {
            string key = "srf" + surface + tint + metallic + smoothness + scale + bump;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            var s = TextureLib.Get(surface);
            m = new Material(Template("LitTex")) { name = surface };
            m.SetColor(BaseColor, tint);
            m.SetTexture(BaseMap, s.Albedo);
            m.SetTexture(BumpMap, s.Normal);
            m.SetFloat(BumpScale, bump);
            float tile = 1f / (s.Tiling * scale);
            m.SetTextureScale(BaseMap, new Vector2(tile, tile));
            m.SetFloat(Metallic, metallic);
            m.SetFloat(Smoothness, smoothness);
            cache[key] = m;
            return m;
        }

        /// <summary>Glass: transparent, premultiplied so reflections stay bright.</summary>
        public static Material Glass(Color color, float smoothness = 0.96f)
        {
            string key = "glass" + color + smoothness;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Template("Glass")) { name = "Glass" };
            m.SetColor(BaseColor, color);
            m.SetFloat(Smoothness, smoothness);
            m.SetFloat(Metallic, 0f);
            cache[key] = m;
            return m;
        }

        /// <summary>Wire mesh or anything see-through with a pattern in its alpha.</summary>
        public static Material Mesh(string surface, Color tint)
        {
            string key = "mesh" + surface + tint;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            var s = TextureLib.Get(surface);
            m = new Material(Template("Glass")) { name = surface };
            m.SetColor(BaseColor, tint);
            m.SetTexture(BaseMap, s.Albedo);
            float tile = 1f / s.Tiling;
            m.SetTextureScale(BaseMap, new Vector2(tile, tile));
            m.SetFloat(Smoothness, 0.5f);
            m.SetFloat(Metallic, 0.8f);
            cache[key] = m;
            return m;
        }

        /// <summary>Surface that glows: lamp heads, lit windows, embers (HDR colour drives the bloom).</summary>
        public static Material Emissive(Color color, Color emission)
        {
            string key = "emi" + color + emission;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Template("LitEmissive")) { name = "Emissive" };
            m.SetColor(BaseColor, color);
            m.SetColor(Emission, emission);
            m.SetFloat(Smoothness, 0.6f);
            cache[key] = m;
            return m;
        }

        /// <summary>Smoke lit by the sun (URP Simple Lit particles).</summary>
        public static Material Smoke
        {
            get
            {
                Material m;
                if (cache.TryGetValue("smoke", out m)) return m;
                m = new Material(Template("Smoke")) { name = "Smoke" };
                m.SetTexture(BaseMap, SmokeTexture);
                cache["smoke"] = m;
                return m;
            }
        }

        public static Material ParticleAdd { get { return Particle("BoomYard/ParticleAdd", Soft); } }
        public static Material ParticleAlpha { get { return Particle("BoomYard/ParticleAlpha", Soft); } }
        /// <summary>Fire with HDR brightness so it blooms.</summary>
        public static Material FireballHDR { get { return Tinted("BoomYard/ParticleAdd", FireTexture, "fireHDR", new Color(3.2f, 2.3f, 1.6f, 1f)); } }
        public static Material EmberHDR { get { return Tinted("BoomYard/ParticleAdd", Soft, "emberHDR", new Color(4f, 2.2f, 1f, 1f)); } }
        public static Material Paper { get { return Tinted("BoomYard/ParticleAlpha", Square, "paper", Color.white); } }
        public static Material ShockRing { get { return Tinted("BoomYard/ParticleAdd", RingTexture, "shock", new Color(1f, 0.9f, 0.8f, 0.5f)); } }
        public static Material Scorch { get { return Tinted("BoomYard/ParticleAlpha", ScorchTexture, "scorch", new Color(0.05f, 0.04f, 0.035f, 0.85f)); } }

        /// <summary>
        /// Spray paint: a soft dab with overspray speckle for freehand, a crisper rounded square for stencils.
        /// Glow paint is drawn over-bright so the bloom picks it up.
        /// </summary>
        public static Material SprayPaint(bool glow, bool stencil)
        {
            var tex = stencil ? StencilTexture : SprayTexture;
            return Tinted("BoomYard/ParticleAlpha", tex, "spray" + glow + stencil, glow ? new Color(1.5f, 1.5f, 1.5f, 1f) : new Color(0.92f, 0.92f, 0.92f, 1f));
        }

        static Texture2D sprayTex, stencilTex;
        static readonly Dictionary<Texture2D, Material> decals = new Dictionary<Texture2D, Material>();

        /// <summary>A sprayed stencil: its picture on a quad, unlit like the rest of the paint (one material per picture).</summary>
        public static Material StencilDecal(Texture2D tex, bool glow)
        {
            Material m;
            if (decals.TryGetValue(tex, out m) && m != null) return m;
            m = new Material(Shader.Find("BoomYard/ParticleAlpha")) { name = "Stencil" };
            m.mainTexture = tex;
            m.SetColor("_Tint", glow ? new Color(1.5f, 1.5f, 1.5f, 1f) : new Color(0.95f, 0.95f, 0.95f, 1f));
            decals[tex] = m;
            return m;
        }

        static Texture2D SprayTexture
        {
            get
            {
                if (sprayTex == null)
                    sprayTex = Radial(64, (d, x, y) =>
                    {
                        float core = Mathf.Clamp01((1f - d) * 2.2f);
                        float speck = Mathf.PerlinNoise(x * 40f, y * 40f) > 0.62f ? 0.6f : 0f;
                        return Mathf.Max(core * core, speck * Mathf.Clamp01(1.15f - d));
                    });
                return sprayTex;
            }
        }

        static Texture2D StencilTexture
        {
            get
            {
                if (stencilTex == null)
                    stencilTex = Radial(32, (d, x, y) =>
                    {
                        float e = Mathf.Max(Mathf.Abs(x - 0.5f), Mathf.Abs(y - 0.5f)) * 2f;
                        return Mathf.Clamp01((1f - e) * 5f);
                    });
                return stencilTex;
            }
        }

        static Material Tinted(string shader, Texture2D tex, string key, Color tint)
        {
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = Particle(shader, tex, key);
            m.SetColor("_Tint", tint);
            return m;
        }

        static Texture2D square, ringTex, scorchTex;

        static Texture2D Square
        {
            get
            {
                if (square == null) square = Radial(16, (d, x, y) => 1f);
                return square;
            }
        }

        static Texture2D RingTexture
        {
            get
            {
                if (ringTex == null) ringTex = Radial(128, (d, x, y) => Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.12f) * (d < 1f ? 1f : 0f));
                return ringTex;
            }
        }

        static Texture2D ScorchTexture
        {
            get
            {
                if (scorchTex == null)
                    scorchTex = Radial(128, (d, x, y) =>
                    {
                        float n = Mathf.PerlinNoise(x * 6f + 2f, y * 6f + 9f);
                        return Mathf.Clamp01((1f - d) * 1.8f * (0.4f + n));
                    });
                return scorchTex;
            }
        }

        static Material Particle(string shader, Texture2D tex, string key = null)
        {
            key = key ?? shader;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Shader.Find(shader)) { name = key };
            m.mainTexture = tex;
            cache[key] = m;
            return m;
        }

        /// <summary>Unit quad facing -Z with white vertex colours, for billboards drawn with the particle shaders.</summary>
        public static Mesh Quad
        {
            get
            {
                if (quad != null) return quad;
                quad = new Mesh { name = "Quad" };
                quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
                quad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
                quad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
                quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                quad.RecalculateNormals();
                quad.RecalculateBounds();
                return quad;
            }
        }

        /// <summary>Soft round blob for glows and sparks.</summary>
        public static Texture2D Soft
        {
            get
            {
                if (soft != null) return soft;
                soft = Radial(64, (d, x, y) => { float a = Mathf.Clamp01(1f - d); return a * a * (3f - 2f * a); });
                return soft;
            }
        }

        static Texture2D fire, smoke;

        /// <summary>Billowing fire: a soft disc broken up by noise, so fireballs look turbulent rather than round.</summary>
        public static Texture2D FireTexture
        {
            get
            {
                if (fire != null) return fire;
                fire = Radial(128, (d, x, y) =>
                {
                    float n = Mathf.PerlinNoise(x * 5f + 3f, y * 5f + 7f) * 0.6f + Mathf.PerlinNoise(x * 11f, y * 11f) * 0.4f;
                    return Mathf.Clamp01((1f - d) * 1.6f * (0.45f + n)) ;
                });
                return fire;
            }
        }

        /// <summary>Puffy smoke: lumpy cauliflower edge with a dense core.</summary>
        public static Texture2D SmokeTexture
        {
            get
            {
                if (smoke != null) return smoke;
                smoke = Radial(128, (d, x, y) =>
                {
                    float ang = Mathf.Atan2(y - 0.5f, x - 0.5f);
                    float edge = 0.8f + 0.2f * Mathf.PerlinNoise(Mathf.Cos(ang) * 2f + 5f, Mathf.Sin(ang) * 2f + 5f);
                    float n = Mathf.PerlinNoise(x * 6f, y * 6f) * 0.5f + Mathf.PerlinNoise(x * 13f + 4f, y * 13f) * 0.5f;
                    return Mathf.Clamp01((edge - d) * 3f) * (0.55f + 0.45f * n);
                });
                return smoke;
            }
        }

        static Texture2D Radial(int n, System.Func<float, float, float, float> alpha)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                    float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(d, u, v)) * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>Tints a material copy (per-object scorching) keeping its template.</summary>
        public static void SetColor(Material m, Color c) { if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, c); }
        public static Color GetColor(Material m) { return m != null && m.HasProperty(BaseColor) ? m.GetColor(BaseColor) : Color.white; }

        // Sunset palette.
        public static readonly Color Skin = new Color(0.78f, 0.58f, 0.45f);
        public static readonly Color Scorched = new Color(0.05f, 0.045f, 0.04f);
        public static readonly Color GlassTint = new Color(0.32f, 0.38f, 0.42f, 0.28f);
        public static readonly Color DarkMetal = new Color(0.2f, 0.21f, 0.23f);
        public static readonly Color Steel = new Color(0.62f, 0.64f, 0.66f);
        public static readonly Color Rubber = new Color(0.05f, 0.05f, 0.05f);
        public static readonly Color Wood = new Color(0.62f, 0.5f, 0.38f);
    }
}
