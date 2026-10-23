using System.Collections.Generic;
using BoomYard.Core;
using BoomYard.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoomYard.UI
{
    /// <summary>
    /// Every screen of the base, built from code: title (the game itself behind the logo), the minimal HUD of spec §05
    /// (damage, time, chain, wind, pocket bar, joystick, lighter and throw buttons, fuse bar, object label), results
    /// (spec §12) and pause. Landscape reference 1688×780 (the spec's 844×390 mockups at 2×).
    /// </summary>
    public sealed class GameUI : MonoBehaviour
    {
        GameController game;
        RectTransform safe, hud, title, results, pause, disclaimer, popups, fx;
        TouchControls controls;
        Image flash, vignette;
        float flashLeft, flashTotal;
        Text toast;
        float toastLeft;

        // HUD parts
        Text damage, timer, chain, wind, label, hint, fuseTime, settle;
        Image chainBadge, windArrow, joyBase, joyKnob, catchRing, labelBg, powerFill;
        RectTransform fuseBar, fusePointer, powerBar;
        Image fuseRed, fuseOrange, fuseGreen;
        readonly List<Image> slotFrames = new List<Image>();
        readonly List<Text> slotCounts = new List<Text>();
        readonly List<Image> slotChips = new List<Image>();
        Vector2 joyIdle;

        sealed class Pop { public Text Text; public Vector3 World; public float T; }
        readonly List<Pop> pops = new List<Pop>();
        RoundSummary shownResults;
        bool inTitle;

        public static GameUI Create(GameController game)
        {
            var go = new GameObject("UI");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1688f, 780f);
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            if (FindEventSystem() == null)
            {
                var es = new GameObject("EventSystem");
                DontDestroyOnLoad(es);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            var ui = go.AddComponent<GameUI>();
            ui.game = game;
            ui.controls = go.AddComponent<TouchControls>();
            ui.Build();
            return ui;
        }

        static EventSystem FindEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<EventSystem>();
#else
            return Object.FindObjectOfType<EventSystem>();
#endif
        }

        /// <summary>Rebuilds every screen (after a language change) and returns to the one that was showing.</summary>
        public void Rebuild()
        {
            bool wasTitle = inTitle, wasResults = results != null && results.gameObject.activeSelf, wasPause = pause != null && pause.gameObject.activeSelf;
            foreach (Transform c in transform) Destroy(c.gameObject);
            slotFrames.Clear();
            slotCounts.Clear();
            slotChips.Clear();
            pops.Clear();
            controls.PocketSlots.Clear();
            Build();
            if (wasTitle) ShowTitle();
            else if (wasResults && shownResults != null) ShowResults(shownResults);
            else ShowHud();
            if (wasPause) ShowPause(true);
        }

        void Build()
        {
            var root = UIKit.Stretch(UIKit.Rect(transform, "Root"));
            vignette = UIKit.Image(root, "Vignette", new Color(0f, 0f, 0f, 0.72f), UIKit.Vignette);
            UIKit.Stretch(vignette.rectTransform);
            safe = UIKit.Rect(root, "Safe");
            UIKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            popups = UIKit.Stretch(UIKit.Rect(safe, "Popups"));
            BuildHud();
            BuildTitle();
            BuildResults();
            BuildPause();
            BuildDisclaimer();

            toast = UIKit.Label(safe, "Toast", "", 30, UIKit.Fg, TextAnchor.MiddleCenter);
            UIKit.Place(toast.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1400f, 50f));
            toast.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);

            fx = UIKit.Stretch(UIKit.Rect(root, "Fx"));
            flash = UIKit.Image(fx, "Flash", new Color(1f, 1f, 1f, 0f));
            UIKit.Stretch(flash.rectTransform);
        }

        // ---------- HUD ----------

        void BuildHud()
        {
            hud = UIKit.Stretch(UIKit.Rect(safe, "Hud"));
            bool he = Loc.He;

            damage = UIKit.Label(hud, "Damage", Loc.Money(0), 50, UIKit.Fg, TextAnchor.UpperRight, true);
            UIKit.Place(damage.rectTransform, new Vector2(1f, 1f), new Vector2(-44f, -22f), new Vector2(520f, 64f));
            var dl = UIKit.Label(hud, "DamageLabel", Loc.T("hud.damage"), 22, UIKit.Muted, TextAnchor.UpperRight);
            UIKit.Place(dl.rectTransform, new Vector2(1f, 1f), new Vector2(-46f, -84f), new Vector2(300f, 30f));

            chainBadge = UIKit.Image(hud, "Chain", UIKit.Ember, UIKit.Rounded);
            UIKit.Place(chainBadge.rectTransform, new Vector2(1f, 1f), new Vector2(-44f, -122f), new Vector2(230f, 46f));
            chain = UIKit.Label(chainBadge.transform, "Text", "", 24, new Color(0.07f, 0.04f, 0.02f), TextAnchor.MiddleCenter, true);
            UIKit.Stretch(chain.rectTransform);

            timer = UIKit.Label(hud, "Timer", "1:30", 44, UIKit.Fg, TextAnchor.UpperCenter, true);
            UIKit.Place(timer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(240f, 60f));

            // Pause, then the wind: a ring with an arrow that shows where it blows relative to where you look.
            var pauseBtn = UIKit.Image(hud, "Pause", UIKit.Surface, UIKit.Circle);
            UIKit.Place(pauseBtn.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -24f), new Vector2(64f, 64f));
            for (int i = 0; i < 2; i++)
            {
                var bar = UIKit.Image(pauseBtn.transform, "Bar", UIKit.Fg);
                UIKit.Place(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -7f : 7f, 0f), new Vector2(7f, 24f));
                bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            controls.PauseButton = pauseBtn.rectTransform;

            var windRing = UIKit.Image(hud, "Wind", UIKit.Line, UIKit.Ring);
            UIKit.Place(windRing.rectTransform, new Vector2(0f, 1f), new Vector2(128f, -24f), new Vector2(60f, 60f));
            windArrow = UIKit.Image(windRing.transform, "Arrow", UIKit.SteelBlue, UIKit.Arrow);
            UIKit.Place(windArrow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            windArrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            wind = UIKit.Label(hud, "WindText", "", 22, UIKit.Muted, TextAnchor.MiddleLeft);
            UIKit.Place(wind.rectTransform, new Vector2(0f, 1f), new Vector2(200f, -40f), new Vector2(300f, 30f));

            // Crosshair and the label of what you look at.
            var dot = UIKit.Image(hud, "Crosshair", new Color(1f, 1f, 1f, 0.55f), UIKit.Circle);
            UIKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            labelBg = UIKit.Image(hud, "LookLabel", new Color(0.04f, 0.035f, 0.045f, 0.85f), UIKit.Rounded);
            UIKit.Place(labelBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(520f, 44f));
            labelBg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label = UIKit.Label(labelBg.transform, "Text", "", 22, UIKit.Fg);
            UIKit.Stretch(label.rectTransform);

            BuildPocket();

            // Floating joystick: drawn at rest bottom-centre, follows the thumb when used.
            joyIdle = new Vector2(0f, 130f);
            joyBase = UIKit.Image(hud, "JoyBase", new Color(1f, 1f, 1f, 0.22f), UIKit.Ring);
            UIKit.Place(joyBase.rectTransform, new Vector2(0.5f, 0f), joyIdle, new Vector2(170f, 170f));
            joyBase.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            joyKnob = UIKit.Image(joyBase.transform, "Knob", new Color(1f, 1f, 1f, 0.2f), UIKit.Circle);
            UIKit.Place(joyKnob.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f));
            joyKnob.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var mv = UIKit.Label(hud, "MoveLabel", Loc.T("hud.move"), 20, UIKit.Muted);
            UIKit.Place(mv.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(200f, 26f));
            mv.rectTransform.pivot = new Vector2(0.5f, 0f);

            // Right thumb: lighter (hold) and throw / place.
            var lighter = RoundButton("Lighter", new Vector2(-232f, 150f), UIKit.Flame, UIKit.FlameIcon, Loc.T("hud.lighter"));
            controls.LighterButton = lighter;
            catchRing = UIKit.Image(lighter, "Catch", UIKit.Flame, UIKit.Ring);
            UIKit.Stretch(catchRing.rectTransform);
            catchRing.type = Image.Type.Filled;
            catchRing.fillMethod = Image.FillMethod.Radial360;
            catchRing.fillOrigin = (int)Image.Origin360.Top;
            catchRing.fillAmount = 0f;
            var thr = RoundButton("Throw", new Vector2(-64f, 150f), UIKit.SteelBlue, UIKit.Arrow, Loc.T("hud.throw"));
            controls.ThrowButton = thr;
            powerBar = UIKit.Rect(thr, "Power");
            UIKit.Place(powerBar, new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(14f, 150f));
            powerBar.pivot = new Vector2(0.5f, 0f);
            UIKit.Stretch(UIKit.Image(powerBar, "Bg", new Color(0.17f, 0.16f, 0.18f, 0.9f)).rectTransform);
            powerFill = UIKit.Image(powerBar, "Fill", UIKit.Fg);
            UIKit.Stretch(powerFill.rectTransform);
            powerFill.type = Image.Type.Filled;
            powerFill.sprite = UIKit.Rounded;
            powerFill.fillMethod = Image.FillMethod.Vertical;

            // Fuse bar: red (early, lies and waits), orange (lands and goes), green (airburst), and the release pointer.
            fuseBar = UIKit.Rect(hud, "FuseBar");
            UIKit.Place(fuseBar, new Vector2(1f, 0f), new Vector2(-40f, 330f), new Vector2(330f, 26f));
            UIKit.Stretch(UIKit.Image(fuseBar, "Bg", UIKit.Line).rectTransform);
            fuseRed = Zone("Red", UIKit.Danger);
            fuseOrange = Zone("Orange", UIKit.Flame);
            fuseGreen = Zone("Green", UIKit.Ok);
            fusePointer = UIKit.Image(fuseBar, "Pointer", UIKit.Fg).rectTransform;
            fusePointer.anchorMin = fusePointer.anchorMax = new Vector2(0f, 0.5f);
            fusePointer.pivot = new Vector2(0.5f, 0.5f);
            fusePointer.sizeDelta = new Vector2(6f, 46f);
            fuseTime = UIKit.Label(fuseBar, "Time", "", 34, UIKit.Fg, TextAnchor.LowerCenter, true);
            UIKit.Place(fuseTime.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(300f, 44f));
            fuseTime.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint = UIKit.Label(hud, "Hint", "", 22, UIKit.Flame, he ? TextAnchor.MiddleRight : TextAnchor.MiddleRight);
            UIKit.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 294f), new Vector2(560f, 30f));

            settle = UIKit.Label(hud, "Settle", Loc.T("hud.settle"), 80, UIKit.Fg, TextAnchor.MiddleCenter, true);
            UIKit.Place(settle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(800f, 100f));
            settle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        Image Zone(string name, Color c)
        {
            var img = UIKit.Image(fuseBar, name, c);
            var r = img.rectTransform;
            r.anchorMin = new Vector2(0f, 0f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 0.5f);
            return img;
        }

        RectTransform RoundButton(string name, Vector2 pos, Color ringColor, Sprite icon, string caption)
        {
            var b = UIKit.Image(hud, name, new Color(0.04f, 0.035f, 0.045f, 0.85f), UIKit.Circle);
            UIKit.Place(b.rectTransform, new Vector2(1f, 0f), pos, new Vector2(128f, 128f));
            b.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var ring = UIKit.Image(b.transform, "Ring", ringColor, UIKit.Ring);
            UIKit.Stretch(ring.rectTransform);
            var ic = UIKit.Image(b.transform, "Icon", icon == UIKit.FlameIcon ? UIKit.Flame : UIKit.Fg, icon);
            UIKit.Place(ic.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
            ic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var cap = UIKit.Label(b.transform, "Caption", caption, 19, UIKit.Muted);
            UIKit.Place(cap.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -36f), new Vector2(220f, 26f));
            cap.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return b.rectTransform;
        }

        void BuildPocket()
        {
            int n = Core.Pocket.BaseSlots;
            var bar = UIKit.Image(hud, "Pocket", new Color(0.04f, 0.035f, 0.045f, 0.85f), UIKit.Rounded);
            UIKit.Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(36f, 28f), new Vector2(16f + n * 118f, 136f));
            for (int i = 0; i < n; i++)
            {
                var frame = UIKit.Image(bar.transform, "Slot" + i, UIKit.Surface2, UIKit.Rounded);
                UIKit.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f + i * 118f, 0f), new Vector2(108f, 108f));
                frame.rectTransform.pivot = new Vector2(0f, 0.5f);
                var chip = UIKit.Image(frame.transform, "Chip", Color.white, UIKit.Rounded);
                UIKit.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(26f, 46f));
                chip.rectTransform.pivot = new Vector2(0.5f, 1f);
                var count = UIKit.Label(frame.transform, "Count", "", 19, UIKit.Fg, TextAnchor.LowerCenter);
                UIKit.Place(count.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(108f, 40f));
                count.rectTransform.pivot = new Vector2(0.5f, 0f);
                slotFrames.Add(frame);
                slotChips.Add(chip);
                slotCounts.Add(count);
                controls.PocketSlots.Add(frame.rectTransform);
            }
        }

        // ---------- title, results, pause ----------

        void BuildTitle()
        {
            title = UIKit.Stretch(UIKit.Rect(safe, "Title"));
            var logo = UIKit.Label(title, "Logo", "BOOMYARD", 168, UIKit.Fg, TextAnchor.MiddleCenter, true);
            UIKit.Place(logo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1400f, 190f));
            logo.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            logo.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            var tag = UIKit.Label(title, "Tag", Loc.T("title.tag"), 34, UIKit.Flame, TextAnchor.MiddleCenter);
            UIKit.Place(tag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1000f, 50f));
            tag.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var play = UIKit.Button(title, "Play", Loc.T("title.play"), UIKit.Ember, new Color(0.07f, 0.04f, 0.02f), () => game.StartRound(), 40);
            UIKit.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(240f, 84f));
            ((RectTransform)play.transform).pivot = new Vector2(0.5f, 0.5f);
            var tap = UIKit.Label(title, "Tap", Loc.T("title.tap"), 22, UIKit.Muted);
            UIKit.Place(tap.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(600f, 30f));
            tap.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var save = SaveService.Data;
            var money = UIKit.Label(title, "Wallet", Loc.Money(save.shekels), 30, UIKit.Fg, TextAnchor.UpperRight, true);
            UIKit.Place(money.rectTransform, new Vector2(1f, 1f), new Vector2(-44f, -26f), new Vector2(400f, 40f));
            var gems = UIKit.Label(title, "Gems", save.gems.ToString(), 26, UIKit.Fg, TextAnchor.UpperRight);
            UIKit.Place(gems.rectTransform, new Vector2(1f, 1f), new Vector2(-44f, -70f), new Vector2(200f, 36f));
            var gem = UIKit.Image(title, "Gem", UIKit.SteelBlue, UIKit.Diamond);
            UIKit.Place(gem.rectTransform, new Vector2(1f, 1f), new Vector2(-44f - 12f - gems.preferredWidth, -74f), new Vector2(26f, 26f));

            var lang = UIKit.Button(title, "Lang", Loc.T("lang"), UIKit.Surface2, UIKit.Fg, () => game.SetLanguage(Loc.He ? "en" : "he"), 24);
            UIKit.Place((RectTransform)lang.transform, new Vector2(0f, 1f), new Vector2(40f, -26f), new Vector2(150f, 56f));

            var stars = UIKit.Label(title, "Stars", save.TotalStars + " / 18", 22, UIKit.Muted, TextAnchor.UpperLeft);
            UIKit.Place(stars.rectTransform, new Vector2(0f, 1f), new Vector2(230f, -40f), new Vector2(200f, 30f));
            title.gameObject.SetActive(false);
        }

        void BuildDisclaimer()
        {
            disclaimer = UIKit.Stretch(UIKit.Rect(safe, "Disclaimer"));
            var scrim = UIKit.Image(disclaimer, "Scrim", UIKit.Scrim);
            UIKit.Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;
            var panel = UIKit.Image(disclaimer, "Panel", UIKit.Surface, UIKit.Rounded);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 470f));
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var t = UIKit.Label(panel.transform, "Title", Loc.T("disclaimer.title"), 44, UIKit.Flame, TextAnchor.MiddleCenter, true);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 60f));
            t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 3; i++)
            {
                var l = UIKit.Label(panel.transform, "Line" + i, Loc.T("disclaimer.l" + (i + 1)), 30, i == 2 ? UIKit.Fg : UIKit.Muted);
                UIKit.Place(l.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f - i * 58f), new Vector2(1000f, 44f));
                l.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            var ok = UIKit.Button(panel.transform, "Ok", Loc.T("disclaimer.ok"), UIKit.Ember, new Color(0.07f, 0.04f, 0.02f), () =>
            {
                SaveService.Data.disclaimerSeen = true;
                SaveService.Save();
                disclaimer.gameObject.SetActive(false);
            }, 34);
            UIKit.Place((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(260f, 76f));
            ((RectTransform)ok.transform).pivot = new Vector2(0.5f, 0.5f);
            disclaimer.gameObject.SetActive(false);
        }

        void BuildResults()
        {
            results = UIKit.Stretch(UIKit.Rect(safe, "Results"));
            var scrim = UIKit.Image(results, "Scrim", UIKit.Scrim);
            UIKit.Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;
            results.gameObject.SetActive(false);
        }

        void BuildPause()
        {
            pause = UIKit.Stretch(UIKit.Rect(safe, "Pause"));
            var scrim = UIKit.Image(pause, "Scrim", UIKit.Scrim);
            UIKit.Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;
            var t = UIKit.Label(pause, "Title", Loc.T("pause.title"), 64, UIKit.Fg, TextAnchor.MiddleCenter, true);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(800f, 80f));
            t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            string[] keys = { "pause.resume", "pause.restart", "pause.quit", "lang" };
            System.Action[] acts =
            {
                () => game.TogglePause(),
                () => { game.TogglePause(); game.StartRound(); },
                () => game.QuitToTitle(),
                () => game.SetLanguage(Loc.He ? "en" : "he"),
            };
            for (int i = 0; i < keys.Length; i++)
            {
                var b = UIKit.Button(pause, keys[i], Loc.T(keys[i]), i == 0 ? UIKit.Ember : UIKit.Surface2, i == 0 ? new Color(0.07f, 0.04f, 0.02f) : UIKit.Fg, acts[i], 32);
                UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f - i * 100f), new Vector2(380f, 80f));
                ((RectTransform)b.transform).pivot = new Vector2(0.5f, 0.5f);
            }
            pause.gameObject.SetActive(false);
        }

        // ---------- show ----------

        public void ShowTitle()
        {
            inTitle = true;
            shownResults = null;
            hud.gameObject.SetActive(false);
            results.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            title.gameObject.SetActive(true);
            disclaimer.gameObject.SetActive(!SaveService.Data.disclaimerSeen);
            vignette.color = new Color(0f, 0f, 0f, 0.9f);
        }

        public void ShowHud()
        {
            inTitle = false;
            shownResults = null;
            title.gameObject.SetActive(false);
            results.gameObject.SetActive(false);
            disclaimer.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            hud.gameObject.SetActive(true);
            vignette.color = new Color(0f, 0f, 0f, 0.72f);
        }

        public void ShowPause(bool on) { pause.gameObject.SetActive(on); }

        public void ShowResults(RoundSummary s)
        {
            shownResults = s;
            hud.gameObject.SetActive(false);
            // Results are rebuilt each time: their rows depend on the round.
            for (int i = results.childCount - 1; i >= 1; i--) Destroy(results.GetChild(i).gameObject);
            results.gameObject.SetActive(true);
            var col = UIKit.Rect(results, "Column");
            UIKit.Place(col, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 740f));
            col.pivot = new Vector2(0.5f, 0.5f);
            var align = Loc.He ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            float y = -30f;

            Row(col, Loc.Show((Loc.He ? s.Arena.Number + " · " + s.Arena.NameHe : s.Arena.Number + " · " + s.Arena.NameEn)), 24, UIKit.Muted, ref y, 34f, align);
            Row(col, Loc.Money(s.Total), 104, UIKit.Fg, ref y, 112f, align, true);
            Row(col, s.Record && s.PreviousBest > 0 ? Loc.F("results.record", Loc.Money(s.PreviousBest)) : Loc.F("results.best", Loc.Money(System.Math.Max(s.PreviousBest, s.Total))), 24, s.Record ? UIKit.Ember : UIKit.Muted, ref y, 36f, align);

            // Stars
            for (int i = 0; i < 3; i++)
            {
                var st = UIKit.Image(col, "Star", i < s.Stars ? UIKit.Flame : new Color(1f, 1f, 1f, 0.15f), UIKit.Star);
                float x = (Loc.He ? -1f : 1f) * (i * 76f);
                var anchor = Loc.He ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
                UIKit.Place(st.rectTransform, anchor, new Vector2(x, y - 6f), new Vector2(64f, 64f));
            }
            y -= 84f;

            BreakdownRow(col, Loc.T("results.base"), Loc.Money(s.Base), ref y);
            BreakdownRow(col, Loc.F("results.chain", s.LongestChain), "+" + Loc.Money(s.ChainExtra), ref y);
            BreakdownRow(col, Loc.T("results.placement"), "+" + Loc.Money(s.PlacementExtra), ref y);
            if (s.Bonuses != 0) BreakdownRow(col, Loc.T("results.bonuses"), (s.Bonuses > 0 ? "+" : "−") + Loc.Money(System.Math.Abs(s.Bonuses)), ref y);
            if (s.CleanSweep) BreakdownRow(col, Loc.T("results.sweep"), "×1.5", ref y);
            int pct = s.Objects > 0 ? Mathf.RoundToInt(100f * s.Destroyed / s.Objects) : 0;
            Row(col, Loc.F("results.destroyed", pct, s.Destroyed, s.Objects), 24, UIKit.Muted, ref y, 40f, align);
            Row(col, Loc.F("results.coins", Loc.Money(s.Coins)), 26, UIKit.Flame, ref y, 40f, align);
            Row(col, Loc.T(s.TipKey), 22, UIKit.Muted, ref y, 50f, align);

            var again = UIKit.Button(col, "Again", Loc.T("results.again"), UIKit.Ember, new Color(0.07f, 0.04f, 0.02f), () => game.StartRound(), 36);
            var menu = UIKit.Button(col, "Menu", Loc.T("results.menu"), UIKit.Surface2, UIKit.Fg, () => game.QuitToTitle(), 30);
            var a = Loc.He ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            float dir = Loc.He ? -1f : 1f;
            UIKit.Place((RectTransform)again.transform, a, new Vector2(0f, 10f), new Vector2(260f, 80f));
            UIKit.Place((RectTransform)menu.transform, a, new Vector2(dir * 290f, 10f), new Vector2(220f, 80f));
        }

        void Row(RectTransform col, string text, int size, Color c, ref float y, float h, TextAnchor align, bool display = false)
        {
            var t = UIKit.Label(col, "Row", text, size, c, align, display);
            t.rectTransform.anchorMin = new Vector2(0f, 1f);
            t.rectTransform.anchorMax = new Vector2(1f, 1f);
            t.rectTransform.pivot = new Vector2(0.5f, 1f);
            t.rectTransform.anchoredPosition = new Vector2(0f, y);
            t.rectTransform.sizeDelta = new Vector2(0f, h);
            y -= h + 4f;
        }

        void BreakdownRow(RectTransform col, string name, string value, ref float y)
        {
            bool he = Loc.He;
            var n = UIKit.Label(col, "Name", name, 26, UIKit.Fg, he ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            var v = UIKit.Label(col, "Value", value, 26, UIKit.Fg, he ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, true);
            foreach (var t in new[] { n, v })
            {
                t.rectTransform.anchorMin = new Vector2(0f, 1f);
                t.rectTransform.anchorMax = new Vector2(1f, 1f);
                t.rectTransform.pivot = new Vector2(0.5f, 1f);
                t.rectTransform.anchoredPosition = new Vector2(0f, y);
                t.rectTransform.sizeDelta = new Vector2(0f, 40f);
            }
            y -= 44f;
        }

        // ---------- feedback ----------

        public void Toast(string text)
        {
            toast.text = text;
            toastLeft = 2.2f;
        }

        public void Bonus(BonusKind kind) { Toast(Loc.T("bonus." + kind)); }

        public void Flash(Color c, float seconds)
        {
            flash.color = c;
            flashTotal = flashLeft = seconds;
        }

        /// <summary>"+₪4,680" rising from what broke; chain links also show their multiplier.</summary>
        public void Popup(Vector3 world, long value, float chainMult)
        {
            if (value <= 0) return;
            if (pops.Count > 24) { Destroy(pops[0].Text.gameObject); pops.RemoveAt(0); }
            string s = "+" + Loc.Money(value) + (chainMult > 1f ? "  ×" + chainMult.ToString("0.0") : "");
            var t = UIKit.Label(popups, "Pop", s, value >= 5000 ? 34 : 26, chainMult > 1f ? UIKit.Ember : UIKit.Flame, TextAnchor.MiddleCenter, true);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = Vector2.zero;
            t.rectTransform.sizeDelta = new Vector2(400f, 50f);
            t.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.7f);
            pops.Add(new Pop { Text = t, World = world });
        }

        // ---------- per frame ----------

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            toastLeft -= dt;
            toast.color = new Color(UIKit.Fg.r, UIKit.Fg.g, UIKit.Fg.b, Mathf.Clamp01(toastLeft * 2f));
            if (flashLeft > 0f)
            {
                flashLeft -= dt;
                var c = flash.color;
                c.a = Mathf.Clamp01(flashLeft / flashTotal) * (flashTotal > 1f ? 1f : 0.8f);
                flash.color = c;
            }
            UpdatePops(dt);
            if (hud.gameObject.activeSelf) UpdateHud();
        }

        void UpdatePops(float dt)
        {
            var cam = Player.I != null ? Player.I.Cam : null;
            var canvas = (RectTransform)transform;
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var p = pops[i];
                p.T += dt;
                if (p.T > 1.4f || cam == null) { Destroy(p.Text.gameObject); pops.RemoveAt(i); continue; }
                var sp = cam.WorldToScreenPoint(p.World + Vector3.up * p.T * 0.8f);
                if (sp.z < 0f) { p.Text.enabled = false; continue; }
                p.Text.enabled = true;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)popups, sp, null, out local);
                var pr = ((RectTransform)popups).rect;
                p.Text.rectTransform.anchoredPosition = local - pr.min;
                var c = p.Text.color;
                c.a = Mathf.Clamp01((1.4f - p.T) * 2.5f);
                p.Text.color = c;
            }
        }

        void UpdateHud()
        {
            var g = game;
            var player = Player.I;
            damage.text = Loc.Money(g.Score.Total);
            float tl = Mathf.Ceil(g.Round.TimeLeft);
            timer.text = ((int)tl / 60) + ":" + ((int)tl % 60).ToString("00");
            timer.color = tl <= 10f ? UIKit.Danger : UIKit.Fg;
            chainBadge.gameObject.SetActive(g.Chain.Active);
            if (g.Chain.Active) chain.text = Loc.F("hud.chain", g.Chain.Multiplier.ToString("0.0"));
            settle.gameObject.SetActive(g.Round.State == RoundState.Settling);

            // Wind arrow relative to the view: up means it blows the way you look.
            float rel = WindModel.DeltaAngle(player.Yaw, g.Wind.Direction);
            windArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -rel);
            wind.text = g.ArenaDef.Outdoors ? Loc.F("hud.wind", g.Wind.Speed.ToString("0.0")) : "";

            // Pocket
            for (int i = 0; i < slotFrames.Count; i++)
            {
                bool has = i < g.Pocket.Slots.Count;
                slotFrames[i].gameObject.SetActive(has);
                if (!has) continue;
                var s = g.Pocket.Slots[i];
                var def = Catalog.Explosive(s.id);
                slotFrames[i].color = i == g.Pocket.Selected ? new Color(0.3f, 0.17f, 0.07f, 1f) : UIKit.Surface2;
                slotChips[i].color = s.count > 0 ? ItemModel.Chip(s.id) : new Color(0.3f, 0.3f, 0.3f, 0.4f);
                string name = def != null ? (Loc.He ? def.NameHe : def.NameEn) : s.id;
                slotCounts[i].text = Loc.Show(name) + "\n×" + s.count;
                slotCounts[i].fontSize = name.Length > 9 ? 16 : 19;
            }

            // Joystick follows the thumb.
            Vector2 local;
            if (controls.JoystickActive && RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, controls.JoyOrigin, null, out local))
            {
                joyBase.rectTransform.anchorMin = joyBase.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                joyBase.rectTransform.anchoredPosition = local;
                joyKnob.rectTransform.anchoredPosition = controls.JoyVector * 60f;
            }
            else
            {
                joyBase.rectTransform.anchorMin = joyBase.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                joyBase.rectTransform.anchoredPosition = joyIdle;
                joyKnob.rectTransform.anchoredPosition = player.In.Move * 60f;
            }

            // Lighter catch ring, fuse bar and throw power.
            var ig = player.Ignition;
            catchRing.fillAmount = player.HeldLit ? 1f : ig != null ? ig.CatchProgress : 0f;
            bool lit = player.HeldLit;
            fuseBar.gameObject.SetActive(lit);
            powerBar.gameObject.SetActive(lit && controls.ThrowHeld);
            powerFill.fillAmount = Mathf.InverseLerp(0.35f, 1f, controls.ThrowPower);
            if (lit) UpdateFuse(player);
            if (lit) hint.text = Loc.T("hud.caught");
            else if (player.HeldDef != null && ig != null && ig.Flame == FlameState.Lit) hint.text = Loc.T("hud.hold");
            else hint.text = "";

            // What you are looking at: name, value and how much of it is left.
            labelBg.gameObject.SetActive(false);
            RaycastHit hit;
            if (Physics.Raycast(player.Cam.transform.position, player.Cam.transform.forward, out hit, 30f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                var d = hit.collider.GetComponentInParent<Destructible>();
                if (d != null && !d.Destroyed)
                {
                    labelBg.gameObject.SetActive(true);
                    label.text = Loc.Show(d.Name + " · " + Loc.Money(d.Def.Value) + " · " + Mathf.RoundToInt(d.Health01 * 100f) + "%");
                    labelBg.rectTransform.sizeDelta = new Vector2(label.preferredWidth + 48f, 44f);
                }
            }
        }

        /// <summary>Zones are laid out by the predicted flight time of the current throw (spec §04).</summary>
        void UpdateFuse(Player player)
        {
            float total = Mathf.Max(0.1f, player.HeldTotal);
            float w = fuseBar.rect.width;
            float flight = Mathf.Clamp(player.PredictedFlight, 0f, total);
            float land = Mathf.Clamp(flight + BoomYard.Core.Fuse.LandWindow, 0f, total);
            // Left = time remaining 0, right = full fuse. Green [0, flight], orange [flight, flight+1], red beyond.
            SetZone(fuseGreen, 0f, flight / total, w);
            SetZone(fuseOrange, flight / total, land / total, w);
            SetZone(fuseRed, land / total, 1f, w);
            fusePointer.anchoredPosition = new Vector2(Mathf.Clamp01(player.HeldRemaining / total) * w, 0f);
            fuseTime.text = Mathf.Max(0f, player.HeldRemaining).ToString("0.0") + "s";
            var zone = BoomYard.Core.Fuse.Classify(player.HeldRemaining, player.PredictedFlight);
            fuseTime.color = zone == FuseZone.Air ? UIKit.Ok : zone == FuseZone.Land ? UIKit.Flame : zone == FuseZone.Hand ? UIKit.Danger : UIKit.Fg;
        }

        static void SetZone(Image img, float from, float to, float width)
        {
            var r = img.rectTransform;
            r.anchoredPosition = new Vector2(from * width, 0f);
            r.sizeDelta = new Vector2(Mathf.Max(0f, (to - from) * width), 0f);
        }
    }
}
