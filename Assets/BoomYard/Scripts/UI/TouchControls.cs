using System.Collections.Generic;
using BoomYard.Game;
using UnityEngine;

namespace BoomYard.UI
{
    /// <summary>
    /// Controls (spec §05): left thumb moves (floating joystick, double tap to run), right side drags to look,
    /// 🔥 held = lighter, ↑ = throw (tap = short, drag up = power), a tap on the ground = place, the pocket bar
    /// selects, the can button takes out the spray can. Mouse and keyboard do the same in the editor and browser: WASD,
    /// Shift, F, Space, E, 1–8, R (detonate), G (spray can), B (shop), Esc.
    /// </summary>
    public sealed class TouchControls : MonoBehaviour
    {
        public RectTransform LighterButton, ThrowButton, PlaceButton, PauseButton, DetonateButton, SprayButton, ShopButton;
        /// <summary>Other HUD buttons that act on a tap (the spray colour strip, the stencil and cap chips).</summary>
        public readonly List<KeyValuePair<RectTransform, System.Action>> Taps = new List<KeyValuePair<RectTransform, System.Action>>();
        public System.Action OpenShop;
        public readonly List<RectTransform> PocketSlots = new List<RectTransform>();

        public bool JoystickActive { get; private set; }
        public Vector2 JoyOrigin { get; private set; }
        public Vector2 JoyVector { get; private set; }
        public bool ThrowHeld { get; private set; }
        public float ThrowPower { get; private set; }

        const float LookDegreesPerScreen = 130f;
        const float JoyRadiusShare = 0.11f;   // of the screen height
        const float TapSeconds = 0.25f;

        enum Role { None, Move, Look, Lighter, Throw, Place, Detonate, Ui }

        sealed class Finger
        {
            public Role Role;
            public Vector2 Start, Last;
            public float Time;
            public float Travel;
        }

        readonly Dictionary<int, Finger> fingers = new Dictionary<int, Finger>();
        float lastMoveEnd = -1f;
        bool runLatched;
        float keyThrowStart = -1f;

        struct Pointer { public int Id; public TouchPhase Phase; public Vector2 Pos; }
        readonly List<Pointer> pointers = new List<Pointer>();
        Vector2 lastMouse;

        void Update()
        {
            var player = Player.I;
            if (player == null) return;
            var gc = GameController.I;
            if (gc != null && gc.Current != GameController.Mode.Level) { fingers.Clear(); return; }
            var input = player.In;
            var settings = SaveService.Data.settings;

            Gather();
            input.Move = Vector2.zero;
            input.LighterHeld = false;
            ThrowHeld = false;
            JoystickActive = false;

            foreach (var p in pointers)
            {
                Finger f;
                if (p.Phase == TouchPhase.Began)
                {
                    f = new Finger { Role = RoleAt(p.Pos, input), Start = p.Pos, Last = p.Pos, Time = Time.unscaledTime };
                    fingers[p.Id] = f;
                    if (f.Role == Role.Lighter) input.LighterDown = true;
                    if (f.Role == Role.Move && Time.unscaledTime - lastMoveEnd < 0.3f) runLatched = true;
                }
                if (!fingers.TryGetValue(p.Id, out f)) continue;

                var delta = p.Pos - f.Last;
                f.Travel += delta.magnitude;
                f.Last = p.Pos;
                bool ended = p.Phase == TouchPhase.Ended || p.Phase == TouchPhase.Canceled;

                switch (f.Role)
                {
                    case Role.Move:
                        float r = Screen.height * JoyRadiusShare;
                        var v = Vector2.ClampMagnitude((p.Pos - f.Start) / r, 1f);
                        JoystickActive = !ended;
                        JoyOrigin = f.Start;
                        JoyVector = v;
                        if (!ended) input.Move = v;
                        if (ended) { lastMoveEnd = Time.unscaledTime; runLatched = false; }
                        break;
                    case Role.Look:
                        var deg = delta / Screen.height * LookDegreesPerScreen;   // the player applies the sensitivity setting
                        input.Look += new Vector2(deg.x, settings.invertY ? -deg.y : deg.y);
                        if (ended && Time.unscaledTime - f.Time < TapSeconds && f.Travel < Screen.height * 0.025f)
                        {
                            input.Place = true;
                            input.PlaceScreen = p.Pos;
                        }
                        break;
                    case Role.Lighter:
                        if (!ended) input.LighterHeld = true;
                        else input.LighterUp = true;
                        break;
                    case Role.Place:
                        if (ended) { input.Place = true; input.PlaceScreen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); }
                        break;
                    case Role.Throw:
                        float power = 0.35f + 0.65f * Mathf.Clamp01((p.Pos.y - f.Start.y) / (Screen.height * 0.3f));
                        ThrowPower = power;
                        input.ThrowPower = power;
                        if (!ended) ThrowHeld = true;
                        else input.ThrowUp = true;
                        break;
                }
                if (ended) fingers.Remove(p.Id);
            }

            Keyboard(input);
            input.ThrowHeld = ThrowHeld;
            input.Run = runLatched || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        Role RoleAt(Vector2 pos, InputFrame input)
        {
            if (Hit(PauseButton, pos)) { input.Pause = true; return Role.Ui; }
            if (Hit(ShopButton, pos)) { if (OpenShop != null) OpenShop(); return Role.Ui; }
            if (Hit(SprayButton, pos)) { input.SprayToggle = true; return Role.Ui; }
            foreach (var t in Taps) if (Hit(t.Key, pos)) { t.Value(); return Role.Ui; }
            for (int i = 0; i < PocketSlots.Count; i++)
                if (Hit(PocketSlots[i], pos)) { input.Select = i; return Role.Ui; }
            if (Hit(LighterButton, pos)) return Role.Lighter;
            if (Hit(ThrowButton, pos)) return Role.Throw;
            if (Hit(PlaceButton, pos)) return Role.Place;
            if (Hit(DetonateButton, pos)) { input.Detonate = true; return Role.Ui; }
            bool mouse = Input.touchCount == 0;
            bool left = SaveService.Data.settings.leftHanded;
            if (!mouse && (left ? pos.x > Screen.width * 0.58f : pos.x < Screen.width * 0.42f)) return Role.Move;
            return Role.Look;
        }

        static bool Hit(RectTransform r, Vector2 pos)
        {
            return r != null && r.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(r, pos, null);
        }

        /// <summary>Touches, or the mouse as one pointer when there are none (Unity also mirrors touches as a mouse).</summary>
        void Gather()
        {
            pointers.Clear();
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    pointers.Add(new Pointer { Id = t.fingerId, Phase = t.phase, Pos = WebLandscape.Fix(t.position) });
                }
                return;
            }
            if (!Input.mousePresent) return;
            Vector2 m = WebLandscape.Fix(Input.mousePosition);
            if (Input.GetMouseButtonDown(0)) pointers.Add(new Pointer { Id = -1, Phase = TouchPhase.Began, Pos = m });
            else if (Input.GetMouseButtonUp(0)) pointers.Add(new Pointer { Id = -1, Phase = TouchPhase.Ended, Pos = m });
            else if (Input.GetMouseButton(0)) pointers.Add(new Pointer { Id = -1, Phase = m == lastMouse ? TouchPhase.Stationary : TouchPhase.Moved, Pos = m });
            // Right mouse button: look only.
            if (Input.GetMouseButtonDown(1)) pointers.Add(new Pointer { Id = -2, Phase = TouchPhase.Began, Pos = m });
            else if (Input.GetMouseButtonUp(1)) pointers.Add(new Pointer { Id = -2, Phase = TouchPhase.Ended, Pos = m });
            else if (Input.GetMouseButton(1)) pointers.Add(new Pointer { Id = -2, Phase = TouchPhase.Moved, Pos = m });
            lastMouse = m;
        }

        void Keyboard(InputFrame input)
        {
            var k = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) k.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) k.y -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) k.x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) k.x -= 1f;
            if (k != Vector2.zero) input.Move = Vector2.ClampMagnitude(input.Move + k, 1f);

            if (Input.GetKeyDown(KeyCode.F)) input.LighterDown = true;
            if (Input.GetKey(KeyCode.F)) input.LighterHeld = true;
            if (Input.GetKeyUp(KeyCode.F)) input.LighterUp = true;

            // Space: hold to wind up the throw, release to throw.
            if (Input.GetKeyDown(KeyCode.Space)) keyThrowStart = Time.unscaledTime;
            if (keyThrowStart >= 0f)
            {
                float power = 0.35f + 0.65f * Mathf.Clamp01((Time.unscaledTime - keyThrowStart) / 0.9f);
                ThrowPower = power;
                input.ThrowPower = power;
                ThrowHeld = true;
                if (Input.GetKeyUp(KeyCode.Space)) { input.ThrowUp = true; keyThrowStart = -1f; ThrowHeld = false; }
            }

            if (Input.GetKeyDown(KeyCode.E)) { input.Place = true; input.PlaceScreen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); }
            for (int i = 0; i < 8; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) input.Select = i;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) input.Pause = true;
            if (Input.GetKeyDown(KeyCode.R)) input.Detonate = true;
            if (Input.GetKeyDown(KeyCode.G)) input.SprayToggle = true;
            if (Input.GetKeyDown(KeyCode.B) && OpenShop != null) OpenShop();
        }
    }
}
