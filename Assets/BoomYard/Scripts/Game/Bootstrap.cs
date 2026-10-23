using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// Creates the whole game from code when any scene loads, so the project needs no hand-made scene or prefabs
    /// for the base. Press Play in any scene to run.
    /// </summary>
    public static class Bootstrap
    {
        static bool created;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (created) return;
            created = true;

            // Remove any default scene camera/light; the game creates its own.
            foreach (var cam in Camera.allCameras) Object.Destroy(cam.gameObject);
#if UNITY_2023_1_OR_NEWER
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            var lights = Object.FindObjectsOfType<Light>();
#endif
            foreach (var light in lights) Object.Destroy(light.gameObject);

            var go = new GameObject("BoomYard");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameController>();
        }

        // Support "Enter Play Mode Options" with domain reload disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { created = false; }
    }
}
