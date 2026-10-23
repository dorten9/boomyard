using System.Collections.Generic;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// Materials made from code (no art assets in the base): PBR Standard for the world, glass, emissive lamps and
    /// windows, and the particle materials. Cached by key so every bin shares one material.
    /// </summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader standard;
        static Texture2D soft, noise;
        static Mesh quad;

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

        static Shader Standard
        {
            get
            {
                if (standard == null) standard = Shader.Find("Standard");
                return standard;
            }
        }

        /// <summary>Opaque PBR surface.</summary>
        public static Material Lit(Color color, float metallic = 0f, float smoothness = 0.3f, bool grain = true)
        {
            string key = "lit" + color + metallic + smoothness + grain;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Standard) { name = "Lit" };
            m.color = color;
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smoothness);
            if (grain)
            {
                // A little surface noise keeps flat colours from reading as plastic toys.
                m.mainTexture = Noise;
                m.mainTextureScale = new Vector2(2f, 2f);
            }
            cache[key] = m;
            return m;
        }

        /// <summary>Transparent glass (premultiplied, so reflections stay bright).</summary>
        public static Material Glass(Color color, float smoothness = 0.95f)
        {
            string key = "glass" + color + smoothness;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Standard) { name = "Glass" };
            m.color = color;
            m.SetFloat("_Metallic", 0.1f);
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Mode", 3f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            cache[key] = m;
            return m;
        }

        /// <summary>Surface that glows: lit windows, lamp heads, embers.</summary>
        public static Material Emissive(Color color, Color emission)
        {
            string key = "emi" + color + emission;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Standard) { name = "Emissive" };
            m.color = color;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            cache[key] = m;
            return m;
        }

        /// <summary>A private copy for an object that changes (scorched, flickering), so shared materials stay clean.</summary>
        public static Material Copy(Material m) { return new Material(m); }

        public static Material ParticleAdd { get { return Particle("BoomYard/ParticleAdd"); } }
        public static Material ParticleAlpha { get { return Particle("BoomYard/ParticleAlpha"); } }

        static Material Particle(string shader)
        {
            Material m;
            if (cache.TryGetValue(shader, out m)) return m;
            m = new Material(Shader.Find(shader)) { name = shader };
            m.mainTexture = Soft;
            cache[shader] = m;
            return m;
        }

        /// <summary>Soft round blob for particles.</summary>
        public static Texture2D Soft
        {
            get
            {
                if (soft != null) return soft;
                const int n = 64;
                soft = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                        float a = Mathf.Clamp01(1f - d);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * (3f - 2f * a) * 255));
                    }
                soft.SetPixels32(px);
                soft.Apply();
                return soft;
            }
        }

        /// <summary>Greyscale value noise around white, multiplied into PBR colours.</summary>
        public static Texture2D Noise
        {
            get
            {
                if (noise != null) return noise;
                const int n = 128;
                noise = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
                var px = new Color32[n * n];
                var rng = new System.Random(11);
                for (int i = 0; i < px.Length; i++)
                {
                    int x = i % n, y = i / n;
                    float v = 0.86f + 0.1f * Mathf.PerlinNoise(x * 0.07f, y * 0.07f) + 0.04f * (float)rng.NextDouble();
                    byte b = (byte)(Mathf.Clamp01(v) * 255);
                    px[i] = new Color32(b, b, b, 255);
                }
                noise.SetPixels32(px);
                noise.Apply();
                return noise;
            }
        }

        // Palette of the night yard.
        public static readonly Color Asphalt = new Color(0.16f, 0.15f, 0.15f);
        public static readonly Color Grass = new Color(0.13f, 0.17f, 0.1f);
        public static readonly Color Plaster = new Color(0.55f, 0.5f, 0.44f);
        public static readonly Color DarkMetal = new Color(0.22f, 0.23f, 0.25f);
        public static readonly Color Steel = new Color(0.56f, 0.6f, 0.64f);
        public static readonly Color Rust = new Color(0.42f, 0.22f, 0.14f);
        public static readonly Color Wood = new Color(0.45f, 0.33f, 0.2f);
        public static readonly Color Rubber = new Color(0.05f, 0.05f, 0.05f);
        public static readonly Color VanWhite = new Color(0.78f, 0.76f, 0.7f);
        public static readonly Color Skin = new Color(0.72f, 0.54f, 0.4f);
        public static readonly Color Scorched = new Color(0.06f, 0.055f, 0.05f);
        public static readonly Color GlassTint = new Color(0.35f, 0.45f, 0.55f, 0.35f);
    }
}
