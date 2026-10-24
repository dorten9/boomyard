// Minimal stand-ins for the UnityEngine types the procedural generators use, so they run under plain Mono
// (the real UnityEngine.dll needs the engine's native side). Preview builds only; never part of the game.
using System;

namespace UnityEngine
{
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color operator *(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a * k); }
        public static Color Lerp(Color x, Color y, float t) { t = Mathf.Clamp01(t); return new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t); }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3 normalized { get { float m = (float)Math.Sqrt(x * x + y * y + z * z); return m > 1e-6f ? new Vector3(x / m, y / m, z / m) : this; } }
        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Clamp01(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float Floor(float v) { return (float)Math.Floor(v); }
        public static int FloorToInt(float v) { return (int)Math.Floor(v); }
        public static float Sin(float v) { return (float)Math.Sin(v); }
        public static float Cos(float v) { return (float)Math.Cos(v); }
        public static float Abs(float v) { return Math.Abs(v); }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static float Sqrt(float v) { return (float)Math.Sqrt(v); }
        public static float SmoothStep(float a, float b, float t) { t = Clamp01(t); t = t * t * (3f - 2f * t); return a + (b - a) * t; }
    }

    public enum TextureFormat { RGBA32 }
    public enum TextureWrapMode { Repeat, Clamp }

    public class Texture2D
    {
        public string name;
        public TextureWrapMode wrapMode;
        public int anisoLevel;
        public Texture2D(int w, int h, TextureFormat f, bool mips, bool linear) { }
        public void SetPixels(Color[] c) { }
        public void Apply(bool a, bool b) { }
    }
}
