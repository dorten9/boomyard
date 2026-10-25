using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// A golden gnome hidden in every level from the second on: tucked by a fence, behind a corner or at the back
    /// of the yard. Walk up to it to pick it up (+100 coins); the map counts how many have been found.
    /// </summary>
    public sealed class GoldGnome : MonoBehaviour
    {
        public const int Coins = 100;
        public const float Reach = 1.3f;

        static readonly Vector2[] Spots =
        {
            new Vector2(-12.1f, 11.6f), new Vector2(12.1f, 11.2f), new Vector2(-12.2f, -6.5f), new Vector2(12.2f, -7.2f),
            new Vector2(-1.2f, 12.2f), new Vector2(9.6f, 12f), new Vector2(-7.5f, 12.1f), new Vector2(11.9f, 3.5f),
            new Vector2(-12f, 2.5f), new Vector2(4.2f, 12.3f),
        };

        int level;

        /// <summary>Picks a spot for the level (the same every time) clear of its props, and puts the gnome there.</summary>
        public static void Hide(Transform root, LevelDef level)
        {
            for (int k = 0; k < Spots.Length; k++)
            {
                var s = Spots[(level.Number * 7 + k) % Spots.Length];
                bool clear = true;
                foreach (var p in level.Props) if ((new Vector2(p.X, p.Z) - s).sqrMagnitude < 2.5f * 2.5f) clear = false;
                if (!clear) continue;
                Build(root, new Vector3(s.x, 0f, s.y), level.Number);
                return;
            }
        }

        static void Build(Transform root, Vector3 at, int level)
        {
            var t = new GameObject("GoldGnome").transform;
            t.SetParent(root, false);
            t.position = at;
            t.rotation = Quaternion.Euler(0f, Random.Range(150f, 210f), 0f);
            var gold = Mats.Lit(new Color(1f, 0.78f, 0.3f), 1f, 0.85f);
            Props.Part(t, MeshKit.Lathe("gnomebody", new[] { new Vector2(0.11f, 0f), new Vector2(0.12f, 0.06f), new Vector2(0.115f, 0.18f), new Vector2(0.08f, 0.26f), new Vector2(0f, 0.27f) }, 18, true), Vector3.zero, gold, false);
            Props.Part(t, MeshKit.Lathe("gnomebeard", new[] { new Vector2(0.01f, 0.13f), new Vector2(0.075f, 0.2f), new Vector2(0.07f, 0.3f), new Vector2(0f, 0.31f) }, 14), new Vector3(0f, 0f, 0.05f), gold, false);
            Props.Part(t, MeshKit.Blob(0.07f, 0.05f, 77), new Vector3(0f, 0.32f, 0.01f), gold, false);
            Props.Part(t, MeshKit.Lathe("gnomehat", new[] { new Vector2(0.08f, 0f), new Vector2(0.06f, 0.08f), new Vector2(0.03f, 0.16f), new Vector2(0f, 0.22f) }, 14, false, true), new Vector3(0f, 0.36f, -0.01f), gold, false);
            var glow = new GameObject("Glint").AddComponent<Light>();
            glow.transform.SetParent(t, false);
            glow.transform.localPosition = new Vector3(0f, 0.4f, 0.2f);
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.8f, 0.4f);
            glow.range = 1.2f;
            glow.intensity = 0.9f;
            t.gameObject.AddComponent<GoldGnome>().level = level;
        }

        void Update()
        {
            transform.Rotate(0f, 25f * Time.deltaTime, 0f, Space.World);
            var gc = GameController.I;
            var p = Player.I;
            if (gc == null || p == null || gc.Current != GameController.Mode.Level || !p.Controllable) return;
            var d = p.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > Reach * Reach) return;
            gc.FoundGoldGnome(level);
            Destroy(gameObject);
        }
    }
}
