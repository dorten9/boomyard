using System.Collections.Generic;
using BoomYard.Core;
using BoomYard.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoomYard.UI
{
    /// <summary>
    /// Every screen, built from code. This part: the canvas, feedback (toasts, money popups, flashes) and the
    /// in-level HUD. Menus are in GameUI.Menus.cs. Landscape reference 1688×780.
    /// </summary>
    public sealed partial class GameUI : MonoBehaviour
    {
        GameController game;
        RectTransform root, safe, hud, screen, overlay;
        TouchControls controls;
        Image flash, vignette;
        float flashLeft, flashTotal;
        Text toast;
        float toastLeft;

        // HUD
        Text timer, targets, damage, chain, wind, prompt, fuseTime, tutText;
        Image chainBadge, windArrow, joyBase, joyKnob, catchRing, powerFill, tutCard, targetsPill, hudCoin;
        RectTransform fuseBar, fusePointer, powerBar, markers;
        RectTransform lightBtn, throwBtn, placeBtn, boomBtn, sprayBtn, strip;
        Text boomCaption, lightCaption, placeCaption, sprayCaption;
        Image lightIcon, sprayRing;
        string stripKey = "";
        float wreckedAt;
        int wrecked;
        Image fuseRed, fuseOrange, fuseGreen;
        readonly List<Image> slotFrames = new List<Image>();
        readonly List<Text> slotCounts = new List<Text>();
        readonly List<Image> slotChips = new List<Image>();
        readonly List<Image> markerPool = new List<Image>();
        readonly List<Image> tutDots = new List<Image>();
        Vector2 joyIdle;
        float targetPulse;

        sealed class Pop { public Text Text; public Vector3 World; public float T; }
        readonly List<Pop> pops = new List<Pop>();

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
                var module = es.AddComponent<StandaloneInputModule>();
                module.inputOverride = es.AddComponent<LandscapeInput>();
                WebLandscape.Create();
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

        void Build()
        {
            root = UIKit.Stretch(UIKit.Rect(transform, "Root"));
            vignette = UIKit.Image(root, "Vignette", new Color(0f, 0f, 0f, 0.35f), UIKit.Vignette);
            UIKit.Stretch(vignette.rectTransform);
            safe = UIKit.Stretch(UIKit.Rect(root, "Safe"));
            safe.gameObject.AddComponent<SafeArea>();
            markers = UIKit.Stretch(UIKit.Rect(safe, "Markers"));
            BuildHud();
            screen = UIKit.Stretch(UIKit.Rect(safe, "Screen"));
            overlay = UIKit.Stretch(UIKit.Rect(safe, "Overlay"));

            toast = UIKit.Label(safe, "Toast", "", 34, UIKit.Fg, TextAnchor.MiddleCenter, true);
            UIKit.Place(toast.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 60f));
            toast.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            toast.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.85f);

            var fx = UIKit.Stretch(UIKit.Rect(root, "Fx"));
            flash = UIKit.Image(fx, "Flash", new Color(1f, 1f, 1f, 0f));
            UIKit.Stretch(flash.rectTransform);
        }

        /// <summary>Clears the menu screen layer and returns it for a new screen.</summary>
        RectTransform NewScreen(string name, bool dim = true)
        {
            for (int i = screen.childCount - 1; i >= 0; i--) Destroy(screen.GetChild(i).gameObject);
            for (int i = overlay.childCount - 1; i >= 0; i--) Destroy(overlay.GetChild(i).gameObject);
            var s = UIKit.Stretch(UIKit.Rect(screen, name));
            if (dim)
            {
                var d = UIKit.Image(s, "Dim", new Color(0.03f, 0.025f, 0.03f, 0.78f));
                UIKit.Stretch(d.rectTransform);
                d.raycastTarget = true;
            }
            return s;
        }

        /// <summary>A modal on top of the current screen.</summary>
        RectTransform Modal(string name, Vector2 size)
        {
            for (int i = overlay.childCount - 1; i >= 0; i--) Destroy(overlay.GetChild(i).gameObject);
            var m = UIKit.Stretch(UIKit.Rect(overlay, name));
            var d = UIKit.Image(m, "Dim", new Color(0f, 0f, 0f, 0.7f));
            UIKit.Stretch(d.rectTransform);
            d.raycastTarget = true;
            var p = UIKit.Image(m, "Panel", new Color(0.09f, 0.08f, 0.09f, 0.98f), UIKit.Rounded);
            UIKit.Place(p.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            p.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return p.rectTransform;
        }

        void CloseModal()
        {
            for (int i = overlay.childCount - 1; i >= 0; i--) Destroy(overlay.GetChild(i).gameObject);
        }

        void ShowLayer(bool hudOn)
        {
            hud.gameObject.SetActive(hudOn);
            markers.gameObject.SetActive(hudOn);
            vignette.color = new Color(0f, 0f, 0f, hudOn ? 0.35f : 0.55f);
        }

        public void ShowHud()
        {
            for (int i = screen.childCount - 1; i >= 0; i--) Destroy(screen.GetChild(i).gameObject);
            CloseModal();
            ShowLayer(true);
            for (int i = 0; i < slotFrames.Count; i++) slotFrames[i].gameObject.SetActive(false);
            TargetDown(game.Arena.TargetsLeft, game.Arena.Targets.Count);
            ApplyHanded(SaveService.Data.settings.leftHanded);
        }

        /// <summary>Left-handed: the action buttons move to the left edge and the pocket to the right.</summary>
        void ApplyHanded(bool left)
        {
            foreach (var b in new[] { lightBtn, throwBtn, placeBtn, boomBtn, sprayBtn, fuseBar })
            {
                var x = Mathf.Abs(b.anchoredPosition.x);
                b.anchorMin = b.anchorMax = new Vector2(left ? 0f : 1f, 0f);
                b.anchoredPosition = new Vector2(left ? x : -x, b.anchoredPosition.y);
            }
            for (int i = 0; i < slotFrames.Count; i++)
            {
                var r = slotFrames[i].rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(left ? 1f : 0f, 0f);
                r.pivot = new Vector2(left ? 1f : 0f, 0f);
                r.anchoredPosition = new Vector2(left ? -(30f + i * 112f) : 30f + i * 112f, 24f);
            }
            joyIdle = new Vector2(left ? 480f : -480f, 150f);
        }

        // ---------- HUD ----------

        void BuildHud()
        {
            hud = UIKit.Stretch(UIKit.Rect(safe, "Hud"));

            // Top centre: the clock and the targets left.
            var clockBg = UIKit.Image(hud, "ClockBg", new Color(0.03f, 0.03f, 0.035f, 0.6f), UIKit.Pill);
            UIKit.Place(clockBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(210f, 74f));
            clockBg.rectTransform.pivot = new Vector2(0.5f, 1f);
            timer = UIKit.Label(clockBg.transform, "Timer", "1:30", 50, UIKit.Fg, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(timer.rectTransform);
            targetsPill = UIKit.Image(hud, "Targets", UIKit.Ember, UIKit.Pill);
            UIKit.Place(targetsPill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(250f, 42f));
            targetsPill.rectTransform.pivot = new Vector2(0.5f, 1f);
            targets = UIKit.Label(targetsPill.transform, "Text", "", 24, new Color(0.07f, 0.04f, 0.02f), TextAnchor.MiddleCenter, true);
            UIKit.Stretch(targets.rectTransform);

            // Top right: the coins this round's wreckage is worth, and the chain multiplier.
            damage = UIKit.Label(hud, "Damage", Loc.Money(0), 40, UIKit.Fg, TextAnchor.UpperRight, true);
            UIKit.Place(damage.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -22f), new Vector2(400f, 52f));
            damage.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            hudCoin = UIKit.Image(hud, "Coin", Color.white, UIKit.Coin);
            UIKit.Place(hudCoin.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -26f), new Vector2(40f, 40f));
            var dl = UIKit.Label(hud, "DamageLabel", "COINS", 18, UIKit.Muted, TextAnchor.UpperRight);
            UIKit.Place(dl.rectTransform, new Vector2(1f, 1f), new Vector2(-42f, -72f), new Vector2(300f, 24f));
            chainBadge = UIKit.Image(hud, "Chain", UIKit.Ember, UIKit.Pill);
            UIKit.Place(chainBadge.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -104f), new Vector2(210f, 44f));
            chain = UIKit.Label(chainBadge.transform, "Text", "", 24, new Color(0.07f, 0.04f, 0.02f), TextAnchor.MiddleCenter, true);
            UIKit.Stretch(chain.rectTransform);

            // Top left: pause and the wind.
            var pauseBtn = UIKit.Image(hud, "Pause", new Color(0.03f, 0.03f, 0.035f, 0.6f), UIKit.Circle);
            UIKit.Place(pauseBtn.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -20f), new Vector2(70f, 70f));
            for (int i = 0; i < 2; i++)
            {
                var bar = UIKit.Image(pauseBtn.transform, "Bar", UIKit.Fg, UIKit.Rounded);
                UIKit.Place(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -8f : 8f, 0f), new Vector2(9f, 28f));
                bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            controls.PauseButton = pauseBtn.rectTransform;
            // The shop, open from inside a level (it pauses the game).
            var shopBtn = UIKit.Image(hud, "Shop", new Color(0.03f, 0.03f, 0.035f, 0.6f), UIKit.Circle);
            UIKit.Place(shopBtn.rectTransform, new Vector2(0f, 1f), new Vector2(450f, -20f), new Vector2(70f, 70f));
            var bag = UIKit.Image(shopBtn.transform, "Bag", UIKit.Flame, UIKit.Bag);
            UIKit.Place(bag.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
            bag.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            controls.ShopButton = shopBtn.rectTransform;
            controls.OpenShop = () => game.OpenShopInLevel();
            var windRing = UIKit.Image(hud, "Wind", new Color(1f, 1f, 1f, 0.25f), UIKit.Ring);
            UIKit.Place(windRing.rectTransform, new Vector2(0f, 1f), new Vector2(124f, -24f), new Vector2(60f, 60f));
            windArrow = UIKit.Image(windRing.transform, "Arrow", UIKit.SteelBlue, UIKit.Arrow);
            UIKit.Place(windArrow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            windArrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            wind = UIKit.Label(hud, "WindText", "", 20, UIKit.Muted, TextAnchor.MiddleLeft);
            UIKit.Place(wind.rectTransform, new Vector2(0f, 1f), new Vector2(194f, -40f), new Vector2(240f, 30f));

            // Centre: a crosshair, the context prompt and the tutorial card.
            var dot = UIKit.Image(hud, "Crosshair", new Color(1f, 1f, 1f, 0.6f), UIKit.Circle);
            UIKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            prompt = UIKit.Label(hud, "Prompt", "", 30, UIKit.Flame, TextAnchor.MiddleCenter, true);
            UIKit.Place(prompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(1100f, 44f));
            prompt.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            prompt.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            tutCard = UIKit.Image(hud, "Tutorial", new Color(0.05f, 0.045f, 0.05f, 0.88f), UIKit.Rounded);
            UIKit.Place(tutCard.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(900f, 104f));
            tutCard.rectTransform.pivot = new Vector2(0.5f, 1f);
            tutText = UIKit.Label(tutCard.transform, "Text", "", 27, UIKit.Fg, TextAnchor.MiddleCenter);
            UIKit.Place(tutText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(860f, 66f));
            tutText.rectTransform.pivot = new Vector2(0.5f, 1f);
            tutText.horizontalOverflow = HorizontalWrapMode.Wrap;
            for (int i = 0; i < Tutorial.Count; i++)
            {
                var d = UIKit.Image(tutCard.transform, "Dot", UIKit.Line, UIKit.Circle);
                UIKit.Place(d.rectTransform, new Vector2(0.5f, 0f), new Vector2((i - (Tutorial.Count - 1) * 0.5f) * 22f, 12f), new Vector2(12f, 12f));
                d.rectTransform.pivot = new Vector2(0.5f, 0f);
                tutDots.Add(d);
            }

            BuildPocket();

            // Floating joystick.
            joyIdle = new Vector2(-480f, 150f);
            joyBase = UIKit.Image(hud, "JoyBase", new Color(1f, 1f, 1f, 0.2f), UIKit.Ring);
            UIKit.Place(joyBase.rectTransform, new Vector2(0.5f, 0f), joyIdle, new Vector2(180f, 180f));
            joyBase.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            joyKnob = UIKit.Image(joyBase.transform, "Knob", new Color(1f, 1f, 1f, 0.22f), UIKit.Circle);
            UIKit.Place(joyKnob.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86f, 86f));
            joyKnob.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Right thumb: LIGHT (hold), THROW (drag up for power), PLACE.
            lightBtn = RoundButton("Light", new Vector2(-120f, 150f), 150f, UIKit.Flame, UIKit.FlameIcon, "LIGHT");
            catchRing = UIKit.Image(lightBtn, "Catch", UIKit.Flame, UIKit.Ring);
            UIKit.Stretch(catchRing.rectTransform);
            catchRing.type = Image.Type.Filled;
            catchRing.fillMethod = Image.FillMethod.Radial360;
            catchRing.fillOrigin = (int)Image.Origin360.Top;
            catchRing.fillAmount = 0f;
            throwBtn = RoundButton("Throw", new Vector2(-290f, 110f), 118f, UIKit.SteelBlue, UIKit.Arrow, "THROW");
            placeBtn = RoundButton("Place", new Vector2(-150f, 340f), 118f, UIKit.Fg, UIKit.Drop, "PLACE");
            controls.LighterButton = lightBtn;
            controls.ThrowButton = throwBtn;
            controls.PlaceButton = placeBtn;
            // The detonator: shows while remote charges are waiting.
            boomBtn = RoundButton("Boom", new Vector2(-330f, 300f), 128f, UIKit.Danger, UIKit.FlameIcon, "BOOM");
            controls.DetonateButton = boomBtn;
            boomCaption = boomBtn.GetComponentInChildren<Text>();
            // The spray can: in and out of the hand. With it out, LIGHT sprays and PLACE stamps the stencil.
            sprayBtn = RoundButton("Spray", new Vector2(-170f, 490f), 104f, UIKit.Ok, UIKit.Can, "SPRAY CAN");
            controls.SprayButton = sprayBtn;
            sprayCaption = sprayBtn.Find("Caption").GetComponent<Text>();
            sprayRing = sprayBtn.Find("Ring").GetComponent<Image>();
            lightCaption = lightBtn.Find("Caption").GetComponent<Text>();
            lightIcon = lightBtn.Find("Icon").GetComponent<Image>();
            placeCaption = placeBtn.Find("Caption").GetComponent<Text>();
            strip = UIKit.Rect(hud, "SprayStrip");
            UIKit.Place(strip, new Vector2(0f, 0f), new Vector2(30f, 262f), new Vector2(1000f, 64f));   // clear of the joystick
            powerBar = UIKit.Rect(throwBtn, "Power");
            UIKit.Place(powerBar, new Vector2(0.5f, 1f), new Vector2(0f, 36f), new Vector2(14f, 150f));
            powerBar.pivot = new Vector2(0.5f, 0f);
            UIKit.Stretch(UIKit.Image(powerBar, "Bg", new Color(0.17f, 0.16f, 0.18f, 0.9f), UIKit.Rounded).rectTransform);
            powerFill = UIKit.Image(powerBar, "Fill", UIKit.Fg, UIKit.Rounded);
            UIKit.Stretch(powerFill.rectTransform);
            powerFill.type = Image.Type.Filled;
            powerFill.fillMethod = Image.FillMethod.Vertical;

            // Fuse bar for an item lit in the hand.
            fuseBar = UIKit.Rect(hud, "FuseBar");
            UIKit.Place(fuseBar, new Vector2(1f, 0f), new Vector2(-420f, 300f), new Vector2(320f, 22f));
            UIKit.Stretch(UIKit.Image(fuseBar, "Bg", UIKit.Line).rectTransform);
            fuseRed = Zone("Red", UIKit.Danger);
            fuseOrange = Zone("Orange", UIKit.Flame);
            fuseGreen = Zone("Green", UIKit.Ok);
            fusePointer = UIKit.Image(fuseBar, "Pointer", UIKit.Fg).rectTransform;
            fusePointer.anchorMin = fusePointer.anchorMax = new Vector2(0f, 0.5f);
            fusePointer.pivot = new Vector2(0.5f, 0.5f);
            fusePointer.sizeDelta = new Vector2(6f, 42f);
            fuseTime = UIKit.Label(fuseBar, "Time", "", 32, UIKit.Fg, TextAnchor.LowerCenter, true);
            UIKit.Place(fuseTime.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(300f, 42f));
            fuseTime.rectTransform.pivot = new Vector2(0.5f, 0f);
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

        RectTransform RoundButton(string name, Vector2 pos, float size, Color ringColor, Sprite icon, string caption)
        {
            var b = UIKit.Image(hud, name, new Color(0.03f, 0.03f, 0.035f, 0.7f), UIKit.Circle);
            UIKit.Place(b.rectTransform, new Vector2(1f, 0f), pos, new Vector2(size, size));
            b.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var ring = UIKit.Image(b.transform, "Ring", ringColor, UIKit.Ring);
            UIKit.Stretch(ring.rectTransform);
            var ic = UIKit.Image(b.transform, "Icon", icon == UIKit.FlameIcon ? UIKit.Flame : UIKit.Fg, icon);
            UIKit.Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(size * 0.4f, size * 0.4f));
            ic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var cap = UIKit.Label(b.transform, "Caption", caption, 17, UIKit.Fg, TextAnchor.MiddleCenter, true);
            UIKit.Place(cap.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, size * 0.2f), new Vector2(size, 24f));
            cap.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return b.rectTransform;
        }

        void BuildPocket()
        {
            int n = Core.Pocket.MaxSlots;
            for (int i = 0; i < n; i++)
            {
                var frame = UIKit.Image(hud, "Slot" + i, new Color(0.03f, 0.03f, 0.035f, 0.7f), UIKit.Rounded);
                UIKit.Place(frame.rectTransform, new Vector2(0f, 0f), new Vector2(30f + i * 112f, 24f), new Vector2(104f, 104f));
                var chip = UIKit.Image(frame.transform, "Chip", Color.white, UIKit.Rounded);
                UIKit.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(24f, 42f));
                chip.rectTransform.pivot = new Vector2(0.5f, 1f);
                var count = UIKit.Label(frame.transform, "Count", "", 18, UIKit.Fg, TextAnchor.LowerCenter);
                UIKit.Place(count.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(104f, 46f));
                count.rectTransform.pivot = new Vector2(0.5f, 0f);
                slotFrames.Add(frame);
                slotChips.Add(chip);
                slotCounts.Add(count);
                controls.PocketSlots.Add(frame.rectTransform);
            }
        }

        // ---------- feedback ----------

        public void Toast(string text, float seconds = 2.2f)
        {
            toast.text = text;
            toastLeft = seconds;
        }

        /// <summary>The level's twist, shown for a few seconds as the round starts.</summary>
        public void RuleCard(LevelDef level, Destructible spare)
        {
            string text = level.Rule == LevelRule.Spare
                ? Loc.F("rule.Spare", spare != null ? spare.Name.ToUpperInvariant() : "IT")
                : Loc.T("rule." + level.Rule);
            Toast(text, 4.5f);
        }

        public void Bonus(BonusKind kind) { Toast(Loc.T("bonus." + kind)); }

        public void Flash(Color c, float seconds)
        {
            if (GameController.ReduceFlashes) c.a *= 0.3f;
            flash.color = c;
            flashTotal = flashLeft = seconds;
        }

        public void TargetDown(int left, int total)
        {
            targets.text = left == 0 ? "ALL CLEAR" : "TARGETS  " + (total - left) + " / " + total;
            targetPulse = 1f;
        }

        /// <summary>"+94" coins rising from what broke; chain links also show their multiplier.</summary>
        public void Popup(Vector3 world, long value, float chainMult)
        {
            long coins = game.Level != null && game.Level.Show ? Progress.CityCoins(value) : ScoreCalculator.CoinsFromDamage(value);
            if (coins <= 0) return;
            if (pops.Count > 24) { Destroy(pops[0].Text.gameObject); pops.RemoveAt(0); }
            string s = "+" + Loc.Coins(coins) + (chainMult > 1f ? "  ×" + chainMult.ToString("0.0") : "");
            var t = UIKit.Label(markers, "Pop", s, value >= 5000 ? 34 : 26, chainMult > 1f ? UIKit.Ember : UIKit.Flame, TextAnchor.MiddleCenter, true);
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
            if (hud.gameObject.activeSelf && game.Current == GameController.Mode.Level) UpdateHud(dt);
        }

        bool ToLocal(Vector3 world, out Vector2 local, out bool behind)
        {
            var cam = Player.I != null ? Player.I.Cam : null;
            local = Vector2.zero;
            behind = false;
            if (cam == null) return false;
            var sp = cam.WorldToScreenPoint(world);
            behind = sp.z < 0f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(markers, sp, null, out local);
            local -= markers.rect.min;
            return true;
        }

        void UpdatePops(float dt)
        {
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var p = pops[i];
                p.T += dt;
                Vector2 local;
                bool behind;
                if (p.T > 1.4f || !ToLocal(p.World + Vector3.up * p.T * 0.8f, out local, out behind)) { Destroy(p.Text.gameObject); pops.RemoveAt(i); continue; }
                p.Text.enabled = !behind;
                p.Text.rectTransform.anchoredPosition = local;
                var c = p.Text.color;
                c.a = Mathf.Clamp01((1.4f - p.T) * 2.5f);
                p.Text.color = c;
            }
        }

        void UpdateHud(float dt)
        {
            var g = game;
            var player = Player.I;
            damage.text = g.Level.Show ? Loc.Coins(Progress.CityCoins(g.Score.Total)) : Loc.Money(g.Score.Total);
            boomBtn.gameObject.SetActive(LiveExplosive.RemoteCount > 0 || g.Level.Show && LiveExplosive.UnlitCount > 0);
            boomCaption.text = g.Level.Show && LiveExplosive.RemoteCount == 0 ? "SHOW" : "BOOM";
            if (g.Level.Show)
            {
                // How much of the city is wrecked so far (counted twice a second).
                if (Time.unscaledTime > wreckedAt)
                {
                    wreckedAt = Time.unscaledTime + 0.5f;
                    wrecked = 0;
                    foreach (var d in g.Arena.Objects) if (d != null && d.Destroyed) wrecked++;
                }
                targets.text = "WRECKED  " + wrecked + " / " + g.Arena.Objects.Count;
            }
            UpdateSpray(player);
            // The coin sits just left of the number, however wide it is.
            hudCoin.rectTransform.anchoredPosition = new Vector2(-40f - damage.preferredWidth - 30f, -26f);
            float tl = g.Round.TimeLeft;
            timer.text = g.Level.Show ? "BIG CITY" : Loc.Time(tl);
            bool urgent = tl <= 10f && !g.Round.Frozen;
            timer.color = urgent ? Color.Lerp(UIKit.Danger, UIKit.Fg, Mathf.PingPong(Time.unscaledTime * 2f, 1f)) : UIKit.Fg;
            targetPulse = Mathf.MoveTowards(targetPulse, 0f, dt * 2f);
            targetsPill.rectTransform.localScale = Vector3.one * (1f + 0.15f * targetPulse);
            chainBadge.gameObject.SetActive(g.Chain.Active);
            if (g.Chain.Active) chain.text = "CHAIN ×" + g.Chain.Multiplier.ToString("0.0");

            float rel = WindModel.DeltaAngle(player.Yaw, g.Wind.Direction);
            windArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -rel);
            wind.text = "Wind " + g.Wind.Speed.ToString("0.0") + " m/s";

            for (int i = 0; i < slotFrames.Count; i++)
            {
                bool has = i < g.Pocket.Slots.Count;
                slotFrames[i].gameObject.SetActive(has);
                if (!has) continue;
                var s = g.Pocket.Slots[i];
                var def = Catalog.Explosive(s.id);
                slotFrames[i].color = i == g.Pocket.Selected && !player.Spraying ? new Color(0.36f, 0.18f, 0.05f, 0.9f) : new Color(0.03f, 0.03f, 0.035f, 0.7f);
                slotChips[i].color = s.count > 0 ? ItemModel.Chip(s.id) : new Color(0.3f, 0.3f, 0.3f, 0.4f);
                string name = def != null ? def.NameEn : s.id;
                slotCounts[i].text = g.Pocket.Unlimited ? name : name + "\n×" + s.count;
                slotCounts[i].fontSize = name.Length > 10 ? 15 : 18;
            }

            Vector2 local;
            if (controls.JoystickActive && RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, controls.JoyOrigin, null, out local))
            {
                joyBase.rectTransform.anchorMin = joyBase.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                joyBase.rectTransform.anchoredPosition = local;
                joyKnob.rectTransform.anchoredPosition = controls.JoyVector * 64f;
            }
            else
            {
                joyBase.rectTransform.anchorMin = joyBase.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                joyBase.rectTransform.anchoredPosition = joyIdle;
                joyKnob.rectTransform.anchoredPosition = player.In.Move * 64f;
            }

            var ig = player.Ignition;
            catchRing.fillAmount = ig != null ? ig.CatchProgress : 0f;
            bool lit = player.HeldLit;
            fuseBar.gameObject.SetActive(lit);
            powerBar.gameObject.SetActive(controls.ThrowHeld && player.HeldDef != null);
            powerFill.fillAmount = Mathf.InverseLerp(0.35f, 1f, controls.ThrowPower);
            if (lit) UpdateFuse(player);

            prompt.text = Prompt(player);
            UpdateTutorial();
            UpdateMarkers();
        }

        // ---------- the spray can ----------

        void UpdateSpray(Player player)
        {
            bool on = player.Spraying;
            throwBtn.gameObject.SetActive(!on);
            lightCaption.text = on ? "SPRAY" : "LIGHT";
            lightIcon.sprite = on ? UIKit.Can : UIKit.FlameIcon;
            lightIcon.color = on ? new Color(Spray.I.Color.R, Spray.I.Color.G, Spray.I.Color.B) : UIKit.Flame;
            placeCaption.text = on ? "STENCIL" : "PLACE";
            placeBtn.gameObject.SetActive(!on || Spray.I.Stencil != null);
            sprayCaption.text = on ? "PUT AWAY" : "SPRAY CAN";
            sprayRing.color = on ? UIKit.Flame : UIKit.Ok;
            strip.gameObject.SetActive(on);
            if (on) BuildStrip();
        }

        /// <summary>Above the pocket while the can is out: a swatch per colour owned, the stencil and the cap.</summary>
        void BuildStrip()
        {
            var save = SaveService.Data;
            var colours = new List<SprayColor>();
            foreach (var c in Graffiti.Colors) if (save.Owns(Shop.Get("spray_" + c.Id))) colours.Add(c);
            var st = Spray.I.Stencil;
            string key = save.sprayColor + "|" + save.sprayCap + "|" + save.stencil + "|" + colours.Count + "|" + (st != null ? st.Name : "");
            if (key == stripKey) return;
            stripKey = key;
            for (int i = strip.childCount - 1; i >= 0; i--) Destroy(strip.GetChild(i).gameObject);
            controls.Taps.Clear();
            float x = 0f;
            foreach (var c in colours)
            {
                var col = c;
                bool sel = c.Id == save.sprayColor;
                var sw = UIKit.Image(strip, "Swatch", sel ? UIKit.Fg : new Color(0f, 0f, 0f, 0.5f), UIKit.Circle);
                UIKit.Place(sw.rectTransform, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(sel ? 60f : 52f, sel ? 60f : 52f));
                sw.rectTransform.pivot = new Vector2(0f, 0.5f);
                var dot = UIKit.Image(sw.transform, "Paint", new Color(c.R, c.G, c.B), UIKit.Circle);
                UIKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(sel ? 48f : 44f, sel ? 48f : 44f));
                dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                controls.Taps.Add(new KeyValuePair<RectTransform, System.Action>(sw.rectTransform, () => { save.sprayColor = col.Id; Sfx.I.Tap(); Player.I.RefreshCan(); }));
                x += sel ? 66f : 58f;
            }
            x += 10f;
            Chip("STENCIL: " + (st != null ? st.Name.ToUpperInvariant() : "NONE"), ref x, 300f, game.CycleStencil);
            Chip(Graffiti.Cap(save.sprayCap).Name.ToUpperInvariant(), ref x, 210f, game.CycleCap);
        }

        void Chip(string text, ref float x, float width, System.Action onTap)
        {
            var chip = UIKit.Image(strip, "Chip", new Color(0.03f, 0.03f, 0.035f, 0.75f), UIKit.Pill);
            UIKit.Place(chip.rectTransform, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(width, 52f));
            chip.rectTransform.pivot = new Vector2(0f, 0.5f);
            var t = UIKit.Label(chip.transform, "Text", text, 19, UIKit.Fg, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(t.rectTransform);
            controls.Taps.Add(new KeyValuePair<RectTransform, System.Action>(chip.rectTransform, () => { Sfx.I.Tap(); onTap(); }));
            x += width + 10f;
        }

        /// <summary>What to do next, in a few words.</summary>
        string Prompt(Player p)
        {
            if (p.Knocked) return "";
            if (p.Spraying)
            {
                if (Spray.I.Painting) return "";
                return Spray.I.Stencil != null ? "Hold SPRAY to paint · tap a wall for the " + Spray.I.Stencil.Name.ToUpperInvariant() + " stencil" : "Hold SPRAY to paint the wall at the crosshair";
            }
            if (p.HeldLit) return "THROW IT!";
            if (p.LightTarget != null) return p.Ignition != null && p.Ignition.Flame == FlameState.Lit ? "Keep the flame on the fuse…" : "Hold LIGHT to light the fuse";
            foreach (var l in LiveExplosive.Active)
                if (l != null && l.Lit && Vector3.Distance(l.transform.position, p.transform.position) < l.Def.Radius * 2.5f + 2f) return "RUN!";
            return "";
        }

        void UpdateTutorial()
        {
            var t = game.Tutorial;
            tutCard.gameObject.SetActive(t.Active);
            if (!t.Active) { Pulse(null); return; }
            tutText.text = t.Text;
            for (int i = 0; i < tutDots.Count; i++) tutDots[i].color = i < t.Index ? UIKit.Ok : i == t.Index ? UIKit.Ember : UIKit.Line;
            var h = t.Highlight;
            Pulse(h == "light" ? lightBtn : h == "throw" ? throwBtn : h == "place" ? placeBtn : null);
        }

        void Pulse(RectTransform which)
        {
            float k = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 8f);
            foreach (var b in new[] { lightBtn, throwBtn, placeBtn }) b.localScale = b == which ? Vector3.one * k : Vector3.one;
        }

        /// <summary>A marker over every target still standing; off-screen ones sit on the edge, pointing the way.</summary>
        void UpdateMarkers()
        {
            int used = 0;
            var focus = game.Tutorial.Focus;
            var size = markers.rect.size;
            foreach (var t in game.Arena.Targets)
            {
                if (t == null || t.Destroyed) continue;
                Vector2 local;
                bool behind;
                var top = t.transform.position + Vector3.up * (t.Size().y + 0.4f);
                if (!ToLocal(top, out local, out behind)) continue;
                if (used >= markerPool.Count)
                {
                    var m = UIKit.Image(markers, "Marker", UIKit.Ember, UIKit.Diamond);
                    m.rectTransform.anchorMin = m.rectTransform.anchorMax = Vector2.zero;
                    m.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    markerPool.Add(m);
                }
                var img = markerPool[used++];
                img.gameObject.SetActive(true);
                bool off = behind || local.x < 40f || local.y < 40f || local.x > size.x - 40f || local.y > size.y - 40f;
                if (behind) local = new Vector2(size.x - local.x, 40f);
                local = new Vector2(Mathf.Clamp(local.x, 40f, size.x - 40f), Mathf.Clamp(local.y, 40f, size.y - 40f));
                img.rectTransform.anchoredPosition = local;
                float s = t == focus ? 34f + 6f * Mathf.Sin(Time.unscaledTime * 8f) : off ? 18f : 22f;
                img.rectTransform.sizeDelta = new Vector2(s, s);
                img.color = off ? new Color(1f, 0.48f, 0.1f, 0.6f) : UIKit.Ember;
            }
            for (int i = used; i < markerPool.Count; i++) markerPool[i].gameObject.SetActive(false);

            // The one to keep: a green KEEP label over it.
            var spare = game.Arena.Spare;
            if (spareLabel == null)
            {
                spareLabel = UIKit.Label(markers, "Keep", "KEEP", 22, UIKit.Ok, TextAnchor.MiddleCenter, true);
                spareLabel.rectTransform.anchorMin = spareLabel.rectTransform.anchorMax = Vector2.zero;
                spareLabel.rectTransform.sizeDelta = new Vector2(120f, 30f);
                spareLabel.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.7f);
            }
            Vector2 at = Vector2.zero;
            bool back = true;
            bool show = spare != null && !spare.Destroyed && ToLocal(spare.transform.position + Vector3.up * (spare.Size().y + 0.35f), out at, out back) && !back;
            spareLabel.gameObject.SetActive(show);
            if (show) spareLabel.rectTransform.anchoredPosition = at;
        }
        Text spareLabel;

        /// <summary>Zones are laid out by the predicted flight time of the current throw (spec §04).</summary>
        void UpdateFuse(Player player)
        {
            float total = Mathf.Max(0.1f, player.HeldTotal);
            float w = fuseBar.rect.width;
            float flight = Mathf.Clamp(player.PredictedFlight, 0f, total);
            float land = Mathf.Clamp(flight + BoomYard.Core.Fuse.LandWindow, 0f, total);
            bool cb = SaveService.Data.settings.colorblind;
            fuseGreen.color = cb ? new Color(0.25f, 0.55f, 1f) : UIKit.Ok;
            fuseRed.color = cb ? new Color(0.35f, 0.35f, 0.38f) : UIKit.Danger;
            SetZone(fuseGreen, 0f, flight / total, w);
            SetZone(fuseOrange, flight / total, land / total, w);
            SetZone(fuseRed, land / total, 1f, w);
            fusePointer.anchoredPosition = new Vector2(Mathf.Clamp01(player.HeldRemaining / total) * w, 0f);
            fuseTime.text = Mathf.Max(0f, player.HeldRemaining).ToString("0.0") + "s";
        }

        static void SetZone(Image img, float from, float to, float width)
        {
            var r = img.rectTransform;
            r.anchoredPosition = new Vector2(from * width, 0f);
            r.sizeDelta = new Vector2(Mathf.Max(0f, (to - from) * width), 0f);
        }
    }
}
