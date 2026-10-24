using System;
using System.IO;
using BoomYard.Game;
using UnityEngine;

/// <summary>Writes each procedural surface as raw RGB (albedo lit by a low sun through its normal map) for preview.</summary>
static class TexturePreview
{
    static int Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : "previews";
        Directory.CreateDirectory(dir);
        var sun = new Vector3(-0.5f, 0.4f, 0.75f).normalized;
        foreach (var name in TextureLib.Names)
        {
            var t0 = DateTime.Now;
            var raw = TextureLib.Generate(name);
            var nrm = TextureLib.Normals(raw);
            int n = raw.Size;
            var bytes = new byte[n * n * 3];
            for (int i = 0; i < n * n; i++)
            {
                // Flip to image rows (Unity textures start at the bottom).
                int x = i % n, y = n - 1 - i / n;
                var a = raw.Albedo[y * n + x];
                var c = nrm[y * n + x];
                var v = new Vector3(c.r * 2 - 1, c.g * 2 - 1, c.b * 2 - 1);
                float light = 0.35f + 0.9f * Math.Max(0f, Vector3.Dot(v, sun));
                bytes[i * 3] = (byte)Math.Min(255, a.r * light * 255);
                bytes[i * 3 + 1] = (byte)Math.Min(255, a.g * light * 255);
                bytes[i * 3 + 2] = (byte)Math.Min(255, a.b * light * 255);
            }
            File.WriteAllBytes(Path.Combine(dir, name + "_" + n + ".rgb"), bytes);
            Console.WriteLine(name + " " + n + "px " + (DateTime.Now - t0).TotalMilliseconds.ToString("0") + " ms");
        }
        return 0;
    }
}
