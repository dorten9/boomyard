using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// Picks a picture from the phone's photos or the computer's files, for a picture stencil
    /// (Plugins: ImagePicker.jslib in the browser, BoomImagePicker.mm on iPhone, BoomImagePicker.java on Android).
    /// The result comes back as RGBA pixels, top row first, at most a few hundred pixels across.
    /// </summary>
    public sealed class ImagePicker : MonoBehaviour
    {
        public static ImagePicker I { get; private set; }
        /// <summary>Browser pixels are scaled to this; the stencil itself is smaller still (Graffiti.PictureSize).</summary>
        const int MaxSide = 128;

        Action<byte[], int, int> done;
        Action<string> failed;

        public static ImagePicker Create()
        {
            var go = new GameObject("ImagePicker");   // the plugins send their answers to this name
            DontDestroyOnLoad(go);
            I = go.AddComponent<ImagePicker>();
            return I;
        }

        public static bool Supported
        {
            get
            {
#if UNITY_EDITOR
                return false;
#elif UNITY_WEBGL || UNITY_IOS || UNITY_ANDROID
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// In the browser the file dialog may only open from a real tap: call this on the button's pointer down,
        /// and it opens when the finger lifts. On phones it opens straight away.
        /// </summary>
        public void Pick(Action<byte[], int, int> onPicked, Action<string> onFailed)
        {
            done = onPicked;
            failed = onFailed;
#if UNITY_EDITOR
            Fail("Pictures are added in the app or the web version");
#elif UNITY_WEBGL
            BoomPickImage(gameObject.name, MaxSide);
#elif UNITY_IOS
            BoomPickImageNative(gameObject.name);
#elif UNITY_ANDROID
            using (var c = new AndroidJavaClass("com.boomyard.picker.BoomImagePicker")) c.CallStatic("pick", gameObject.name);
#else
            Fail("Pictures are added in the app or the web version");
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BoomPickImage(string gameObject, int maxSide);
#endif
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BoomPickImageNative(string gameObject);
#endif

        // ---------- answers from the plugins ----------

        /// <summary>The browser: "width,height,base64 RGBA".</summary>
        public void OnPickedPixels(string message)
        {
            var parts = message.Split(new[] { ',' }, 3);
            int w, h;
            byte[] rgba = null;
            if (parts.Length == 3 && int.TryParse(parts[0], out w) && int.TryParse(parts[1], out h))
            {
                try { rgba = Convert.FromBase64String(parts[2]); } catch (FormatException) { }
                if (rgba != null && rgba.Length == w * h * 4) { Done(rgba, w, h); return; }
            }
            Fail("That picture could not be read");
        }

        /// <summary>The phones: a small PNG in the app's cache.</summary>
        public void OnPickedFile(string path)
        {
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(path))) { Destroy(tex); Fail("That picture could not be read"); return; }
                int w = tex.width, h = tex.height;
                var px = tex.GetPixels32();
                Destroy(tex);
                // Unity's rows start at the bottom; stencils start at the top.
                var rgba = new byte[w * h * 4];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        var c = px[(h - 1 - y) * w + x];
                        int o = (y * w + x) * 4;
                        rgba[o] = c.r; rgba[o + 1] = c.g; rgba[o + 2] = c.b; rgba[o + 3] = c.a;
                    }
                Done(rgba, w, h);
            }
            catch (Exception e)
            {
                Debug.LogWarning("BoomYard: picture: " + e.Message);
                Fail("That picture could not be read");
            }
        }

        public void OnPickCancelled(string unused) { done = null; failed = null; }

        public void OnPickFailed(string unused) { Fail("That picture could not be opened"); }

        void Done(byte[] rgba, int w, int h)
        {
            var cb = done;
            done = null;
            failed = null;
            if (cb != null) cb(rgba, w, h);
        }

        void Fail(string why)
        {
            var cb = failed;
            done = null;
            failed = null;
            if (cb != null) cb(why);
        }
    }
}
