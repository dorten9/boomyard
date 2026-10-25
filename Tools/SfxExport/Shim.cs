// The few UnityEngine pieces the synthesis code touches, for rendering it outside Unity.
namespace UnityEngine
{
    public sealed class AudioClip
    {
        public string name; public int frequency; public float[] data;
        public float length { get { return data.Length / (float)frequency; } }
        public static AudioClip Create(string name, int samples, int channels, int frequency, bool stream)
        { return new AudioClip { name = name, frequency = frequency, data = new float[samples] }; }
        public bool SetData(float[] d, int offset) { System.Array.Copy(d, 0, data, offset, d.Length); return true; }
    }

    public static class Mathf
    {
        public const float PI = 3.14159274f;
        public static float Sin(float f) { return (float)System.Math.Sin(f); }
        public static float Exp(float f) { return (float)System.Math.Exp(f); }
        public static float Abs(float f) { return System.Math.Abs(f); }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
        public static float Clamp01(float v) { return Clamp(v, 0f, 1f); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float Repeat(float t, float l) { return Clamp(t - (float)System.Math.Floor(t / l) * l, 0f, l); }
    }
}
