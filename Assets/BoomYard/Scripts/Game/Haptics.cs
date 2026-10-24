using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>A short buzz on phones for blasts that hit close (off in the settings).</summary>
    public static class Haptics
    {
        static float last;

        public static void Buzz()
        {
            if (SaveService.Data != null && !SaveService.Data.settings.haptics) return;
            if (Time.unscaledTime - last < 0.25f) return;
            last = Time.unscaledTime;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
