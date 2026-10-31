using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// The replay of a level's best moment. While a level is on, the player's camera is also drawn small
    /// (no HUD) 15 times a second into a ring of the last four seconds. When a burst of wreckage beats the
    /// round's best so far, those four seconds (from just before it to just after) are kept; the results screen
    /// plays them back in slow motion.
    /// </summary>
    public sealed class Replay : MonoBehaviour
    {
        public static Replay I { get; private set; }
        public const int W = 256, H = 118, Frames = 60;
        public const float Fps = 15f;

        readonly RenderTexture[] ring = new RenderTexture[Frames];
        readonly RenderTexture[] best = new RenderTexture[Frames];
        int head, filled, bestCount;
        float nextShot, burst, bestBurst, keepAt = -1f;

        /// <summary>The kept moment, oldest frame first (empty if nothing happened).</summary>
        public int Count { get { return bestCount; } }
        public RenderTexture Frame(int i) { return best[Mathf.Clamp(i, 0, Frames - 1)]; }

        public static Replay Create(GameObject host)
        {
            I = host.AddComponent<Replay>();
            return I;
        }

        /// <summary>A new level: nothing kept yet.</summary>
        public void Clear()
        {
            filled = head = bestCount = 0;
            burst = bestBurst = 0f;
            keepAt = -1f;
        }

        /// <summary>Wreckage just happened (its value): a big enough burst of it is the moment to keep.</summary>
        public void Notice(float value)
        {
            burst += value;
            if (burst > bestBurst * 1.1f && burst > 2000f)
            {
                bestBurst = burst;
                keepAt = Time.unscaledTime + 1.3f;   // keep the aftermath too
            }
        }

        void LateUpdate()
        {
            var gc = GameController.I;
            var player = Player.I;
            burst *= Mathf.Exp(-Time.unscaledDeltaTime / 1.5f);
            if (gc == null || player == null || gc.Current != GameController.Mode.Level || gc.Paused) return;
            if (keepAt > 0f && Time.unscaledTime >= keepAt) { Keep(); keepAt = -1f; }
            if (Time.unscaledTime < nextShot) return;
            nextShot = Time.unscaledTime + 1f / Fps;
            var rt = ring[head];
            if (rt == null) rt = ring[head] = New();
            var cam = player.Cam;
            var was = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = was;
            head = (head + 1) % Frames;
            filled = Mathf.Min(Frames, filled + 1);
        }

        static RenderTexture New()
        {
            var rt = new RenderTexture(W, H, 16, RenderTextureFormat.ARGB32) { name = "Replay" };
            rt.Create();
            return rt;
        }

        void Keep()
        {
            bestCount = filled;
            for (int i = 0; i < filled; i++)
            {
                int src = (head - filled + i + Frames) % Frames;
                if (best[i] == null) best[i] = New();
                Graphics.Blit(ring[src], best[i]);
            }
        }

        /// <summary>One frame of the kept moment as a picture (for the gallery).</summary>
        public Texture2D Still(int i)
        {
            var rt = Frame(i);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }
    }

    /// <summary>
    /// The photo gallery: pictures of the street (graffiti, wreckage) taken with PHOTO, kept as PNG files next to
    /// the save. Favourites come first. In the browser a photo can be downloaded.
    /// </summary>
    public static class Gallery
    {
        public const int Max = 30;
        static string Dir { get { return Path.Combine(Application.persistentDataPath, "gallery"); } }

        /// <summary>The camera's view right now, without the HUD, at <paramref name="width"/> pixels across.</summary>
        public static Texture2D Shoot(Camera cam, int width = 1280)
        {
            int height = Mathf.RoundToInt(width * (float)Screen.height / Mathf.Max(1, Screen.width));
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var was = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = was;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        public static void Save(Texture2D tex)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllBytes(Path.Combine(Dir, "photo_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png"), tex.EncodeToPNG());
                var all = List();
                // Keep the newest (and every favourite).
                for (int i = Max; i < all.Count; i++) if (!IsFavourite(all[i])) File.Delete(all[i]);
            }
            catch (System.Exception e) { Debug.LogWarning("BoomYard: photo: " + e.Message); }
        }

        /// <summary>Photo files, favourites first, then newest first.</summary>
        public static List<string> List()
        {
            var l = new List<string>();
            try { if (Directory.Exists(Dir)) l.AddRange(Directory.GetFiles(Dir, "*.png")); }
            catch (System.Exception) { }
            l.Sort((a, b) =>
            {
                int fa = IsFavourite(a) ? 0 : 1, fb = IsFavourite(b) ? 0 : 1;
                return fa != fb ? fa.CompareTo(fb) : string.CompareOrdinal(Path.GetFileName(b), Path.GetFileName(a));
            });
            return l;
        }

        public static Texture2D Load(string path)
        {
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (tex.LoadImage(File.ReadAllBytes(path))) return tex;
                Object.Destroy(tex);
            }
            catch (System.Exception) { }
            return null;
        }

        public static void Delete(string path)
        {
            try { File.Delete(path); File.Delete(path + ".fav"); } catch (System.Exception) { }
        }

        public static bool IsFavourite(string path) { return File.Exists(path + ".fav"); }

        public static void SetFavourite(string path, bool on)
        {
            try { if (on) File.WriteAllText(path + ".fav", "1"); else if (File.Exists(path + ".fav")) File.Delete(path + ".fav"); }
            catch (System.Exception) { }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BoomDownload(string name, string base64);
#endif

        /// <summary>Hands the photo to the player: a download in the browser; in the apps it stays in the gallery.</summary>
        public static bool Download(string path)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { BoomDownload(Path.GetFileName(path), System.Convert.ToBase64String(File.ReadAllBytes(path))); return true; }
            catch (System.Exception) { return false; }
#else
            return false;
#endif
        }
    }
}
