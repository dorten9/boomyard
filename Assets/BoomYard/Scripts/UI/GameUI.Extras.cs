using System.Collections;
using BoomYard.Game;
using UnityEngine;
using UnityEngine.UI;

namespace BoomYard.UI
{
    /// <summary>The replay of a level's best moment and the photo gallery.</summary>
    public sealed partial class GameUI
    {
        // ---------- replay ----------

        /// <summary>A REPLAY button on a results screen, when the round had a moment worth keeping.</summary>
        void ReplayButton(RectTransform s)
        {
            if (Replay.I == null || Replay.I.Count < 4) return;
            Btn(s, "REPLAY", false, ShowReplay, new Vector2(0f, 0f), new Vector2(200f, 100f), new Vector2(260f, 86f), 30);
        }

        /// <summary>The best moment, played back at half speed, over and over.</summary>
        void ShowReplay()
        {
            var p = Modal("Replay", new Vector2(1260f, 700f));
            Text(p, "BEST MOMENT  ·  SLOW MOTION", 30, UIKit.Flame, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1100f, 40f), true);
            var view = new GameObject("View").AddComponent<RawImage>();
            view.transform.SetParent(p, false);
            UIKit.Place(view.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1180f, 1180f * Replay.H / Replay.W));
            view.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var frame = new int[1];
            StartCoroutine(PlayReplay(view, frame));
            Btn(p, "SAVE PHOTO", false, () =>
            {
                var still = Replay.I.Still(frame[0]);
                Gallery.Save(still);
                Destroy(still);
                Sfx.I.Confirm();
                Toast("Saved to the photos", 2f);
            }, new Vector2(0.5f, 0f), new Vector2(-170f, 50f), new Vector2(300f, 76f), 26);
            Btn(p, "CLOSE", true, CloseModal, new Vector2(0.5f, 0f), new Vector2(170f, 50f), new Vector2(260f, 76f), 28);
        }

        IEnumerator PlayReplay(RawImage view, int[] frame)
        {
            int n = Replay.I.Count;
            while (view != null)
            {
                for (int i = 0; i < n && view != null; i++)
                {
                    frame[0] = i;
                    view.texture = Replay.I.Frame(i);
                    yield return new WaitForSecondsRealtime(2f / Replay.Fps);   // half speed
                }
                yield return new WaitForSecondsRealtime(0.6f);
            }
        }

        // ---------- photos ----------

        /// <summary>The photo gallery: photos taken with the camera button, favourites first.</summary>
        public void ShowPhotos(System.Action back)
        {
            ShowLayer(false);
            var s = NewScreen("Photos");
            TopBar(s, "PHOTOS", () => back());
            var files = Gallery.List();
            if (files.Count == 0)
            {
                Text(s, "No photos yet.\nTake one with the camera button at the top of the screen in any level or in the big city.", 30, UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 200f));
                return;
            }
            ScrollRect sr;
            var content = UIKit.Scroll(s, "Grid", true, out sr);
            var view = (RectTransform)sr.transform;
            view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(40f, 30f); view.offsetMax = new Vector2(-40f, -130f);
            const float w = 360f, h = 166f;
            for (int i = 0; i < files.Count; i++)
            {
                string path = files[i];
                int col = i % 4, row = i / 4;
                var tex = Gallery.Load(path);
                if (tex == null) continue;
                var b = UIKit.Button(content, "Photo", "", new Color(0.1f, 0.09f, 0.1f), UIKit.Fg, () => { Sfx.I.Tap(); ShowPhoto(path, back); }, 20);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(col * (w + 20f), -row * (h + 20f));
                rt.sizeDelta = new Vector2(w, h);
                var raw = new GameObject("Pic").AddComponent<RawImage>();
                raw.transform.SetParent(rt, false);
                raw.raycastTarget = false;
                raw.texture = tex;
                UIKit.Stretch(raw.rectTransform);
                raw.rectTransform.offsetMin = raw.rectTransform.offsetMax = Vector2.zero;
                if (Gallery.IsFavourite(path))
                {
                    var star = UIKit.Image(rt, "Fav", UIKit.Flame, UIKit.Star);
                    UIKit.Place(star.rectTransform, new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(36f, 36f));
                    star.rectTransform.pivot = new Vector2(1f, 1f);
                }
            }
            content.sizeDelta = new Vector2(0f, ((files.Count + 3) / 4) * (h + 20f));
        }

        void ShowPhoto(string path, System.Action back)
        {
            var tex = Gallery.Load(path);
            if (tex == null) return;
            var p = Modal("Photo", new Vector2(1300f, 720f));
            var raw = new GameObject("Pic").AddComponent<RawImage>();
            raw.transform.SetParent(p, false);
            raw.texture = tex;
            float aspect = tex.width / (float)tex.height;
            var size = aspect > 2f ? new Vector2(1220f, 1220f / aspect) : new Vector2(560f * aspect, 560f);
            UIKit.Place(raw.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), size);
            raw.rectTransform.pivot = new Vector2(0.5f, 1f);
            bool fav = Gallery.IsFavourite(path);
            Btn(p, fav ? "UNFAVOURITE" : "FAVOURITE", !fav, () => { Gallery.SetFavourite(path, !fav); CloseModal(); ShowPhotos(back); }, new Vector2(0.5f, 0f), new Vector2(-400f, 55f), new Vector2(260f, 76f), 24);
            Btn(p, "DOWNLOAD", false, () =>
            {
                if (Gallery.Download(path)) Toast("Downloading", 2f);
                else Toast("Photos are kept here in the app", 2.5f);
            }, new Vector2(0.5f, 0f), new Vector2(-120f, 55f), new Vector2(260f, 76f), 24);
            Btn(p, "DELETE", false, () => Confirm("DELETE THIS PHOTO?", "It is gone for good.", "DELETE", () => { Gallery.Delete(path); ShowPhotos(back); }), new Vector2(0.5f, 0f), new Vector2(160f, 55f), new Vector2(240f, 76f), 24);
            Btn(p, "CLOSE", true, CloseModal, new Vector2(0.5f, 0f), new Vector2(420f, 55f), new Vector2(220f, 76f), 26);
        }
    }
}
