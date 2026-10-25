using UnityEngine;
using UnityEngine.EventSystems;

namespace BoomYard.UI
{
    /// <summary>
    /// The game is landscape only. Phone apps are locked to landscape in the player settings; in a phone browser
    /// held upright, the web page turns the game a quarter turn (index.html) so it still fills the screen in
    /// landscape, and tells this object with SendMessage("WebLandscape", "SetRotated", "1"). Touches then arrive
    /// in the page's upright coordinates; <see cref="Fix"/> turns them into the game's.
    /// </summary>
    public sealed class WebLandscape : MonoBehaviour
    {
        public static bool Rotated { get; private set; }

        public static void Create()
        {
            if (GameObject.Find("WebLandscape") != null) return;
            var go = new GameObject("WebLandscape");
            DontDestroyOnLoad(go);
            go.AddComponent<WebLandscape>();
        }

        // Called from the page.
        public void SetRotated(string on) { Rotated = on == "1"; }

        /// <summary>
        /// A pointer position from Unity's input into game screen coordinates. Turned, the page reports x along
        /// the upright width (the game's height) and y from the top of the upright height (the game's width).
        /// </summary>
        public static Vector2 Fix(Vector2 raw)
        {
            if (!Rotated) return raw;
            return new Vector2(Screen.height - raw.y, raw.x);
        }

        public static Vector2 FixDelta(Vector2 d) { return Rotated ? new Vector2(-d.y, d.x) : d; }
    }

    /// <summary>The UI's input, through <see cref="WebLandscape.Fix"/>, so buttons work when the page is turned.</summary>
    public sealed class LandscapeInput : BaseInput
    {
        public override Vector2 mousePosition { get { return WebLandscape.Fix(base.mousePosition); } }

        public override Touch GetTouch(int index)
        {
            var t = base.GetTouch(index);
            t.position = WebLandscape.Fix(t.position);
            t.deltaPosition = WebLandscape.FixDelta(t.deltaPosition);
            return t;
        }
    }
}
