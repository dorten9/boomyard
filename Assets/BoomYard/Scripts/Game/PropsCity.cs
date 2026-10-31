using UnityEngine;
using Mat = UnityEngine.Material;

namespace BoomYard.Game
{
    /// <summary>The big city's props: street furniture, the building site and the billboard (all generated).</summary>
    public static partial class Props
    {
        static Destructible City(string id, Transform t, int variant)
        {
            switch (id)
            {
                case "barrier": return Barrier(t);
                case "container": return Container(t, variant);
                case "scaffold": return Scaffold(t);
                case "watertank": return WaterTank(t);
                case "billboard": return Billboard(t, variant);
                case "trafficlight": return TrafficLight(t);
                case "streetlamp": return StreetLamp(t);
                case "parkingmeter": return ParkingMeter(t);
                case "planter": return Planter(t, variant);
                case "tenement": return Tenement(t, Mathf.Max(2, variant));
            }
            return null;
        }

        /// <summary>Concrete jersey barrier, 3 m: a wide foot narrowing to a flat top.</summary>
        static Destructible Barrier(Transform t)
        {
            var conc = Mats.Surface("concrete", new Color(0.86f, 0.85f, 0.82f), 0f, 0.6f);
            Part(t, MeshKit.RoundedBox(new Vector3(0.6f, 0.3f, 3f), 0.04f, 2), new Vector3(0f, 0.15f, 0f), conc, true);
            Part(t, MeshKit.RoundedBox(new Vector3(0.42f, 0.55f, 3f), 0.04f, 2, new Vector2(0.55f, 1f)), new Vector3(0f, 0.57f, 0f), conc, true);
            B(t, new Vector3(0.24f, 0.1f, 3f), new Vector3(0f, 0.86f, 0f), conc);
            return Finish(t, "barrier", new Vector3(0f, 0.5f, 0f), 0.4f);
        }

        static readonly Color[] ContainerPaint = { new Color(0.6f, 0.16f, 0.1f), new Color(0.12f, 0.3f, 0.5f), new Color(0.2f, 0.42f, 0.25f), new Color(0.82f, 0.55f, 0.12f) };

        /// <summary>A 20 ft shipping container: corrugated steel, corner posts and the doors at one end.</summary>
        static Destructible Container(Transform t, int variant)
        {
            var paint = Paint(ContainerPaint[variant % ContainerPaint.Length], 0.5f, 0.45f);
            var dark = Paint(ContainerPaint[variant % ContainerPaint.Length] * 0.7f, 0.5f, 0.4f);
            B(t, new Vector3(2.44f, 2.5f, 6f), new Vector3(0f, 1.3f, 0f), paint, true);
            for (int i = 0; i < 22; i++)
                for (int s = -1; s <= 1; s += 2)
                    B(t, new Vector3(0.05f, 2.3f, 0.12f), new Vector3(s * 1.235f, 1.3f, -2.8f + i * 0.27f), dark);
            foreach (var c in new[] { new Vector2(-1.17f, -2.95f), new Vector2(1.17f, -2.95f), new Vector2(-1.17f, 2.95f), new Vector2(1.17f, 2.95f) })
                B(t, new Vector3(0.16f, 2.6f, 0.16f), new Vector3(c.x, 1.3f, c.y), dark);
            for (int k = -1; k <= 1; k += 2)
            {
                var door = B(t, new Vector3(1.18f, 2.3f, 0.06f), new Vector3(k * 0.6f, 1.3f, 3.02f), dark);
                var d = door.transform;
                for (int b = -1; b <= 1; b += 2) B(d, new Vector3(0.04f, 2.2f, 0.04f), new Vector3(b * 0.25f, 0f, 0.05f), Mats.Lit(Mats.Steel, 0.8f, 0.5f));
            }
            return Finish(t, "container", new Vector3(0f, 1.2f, 0f), 1.2f);
        }

        /// <summary>Three lifts of tube-and-board scaffolding against a wall, 6 m wide. It comes down in pieces.</summary>
        static Destructible Scaffold(Transform t)
        {
            var steel = Mats.Lit(new Color(0.55f, 0.56f, 0.57f), 0.85f, 0.45f);
            var board = Mats.Surface("wood", new Color(0.8f, 0.68f, 0.5f), 0f, 0.4f);
            var d = Destructible.Add(t.gameObject, "scaffold");
            for (int i = 0; i < 4; i++)
                for (int k = 0; k < 2; k++)
                {
                    float x = -3f + i * 2f, z = k * 1.3f;
                    d.Parts.Add(Tube(t, new Vector3(x, 0f, z), new Vector3(x, 6.4f, z), 0.025f, steel).transform);
                }
            for (int lift = 1; lift <= 3; lift++)
            {
                float y = lift * 2f;
                for (int k = 0; k < 2; k++) d.Parts.Add(Tube(t, new Vector3(-3f, y, k * 1.3f), new Vector3(3f, y, k * 1.3f), 0.025f, steel).transform);
                d.Parts.Add(Part(t, MeshKit.RoundedBox(new Vector3(6f, 0.05f, 1.2f), 0.01f, 1), new Vector3(0f, y + 0.05f, 0.65f), board, true).transform);
                d.Parts.Add(Tube(t, new Vector3(-3f, y - 2f, 0f), new Vector3(-1f, y, 0f), 0.02f, steel).transform);
            }
            var foot = t.gameObject.AddComponent<BoxCollider>();
            foot.center = new Vector3(0f, 3.2f, 0.65f);
            foot.size = new Vector3(6.1f, 6.4f, 1.4f);
            d.FirePoint = new Vector3(0f, 2f, 0.6f);
            d.FireSize = 0.8f;
            return d;
        }

        /// <summary>A 10 000 l plastic water tank on a steel stand; burst, it puts out the fires round it.</summary>
        static Destructible WaterTank(Transform t)
        {
            var plastic = Mats.Lit(new Color(0.15f, 0.3f, 0.55f), 0f, 0.35f);
            var steel = Mats.Lit(Mats.DarkMetal, 0.7f, 0.4f);
            for (int i = 0; i < 4; i++) B(t, new Vector3(0.1f, 1f, 0.1f), new Vector3(i % 2 == 0 ? -0.9f : 0.9f, 0.5f, i < 2 ? -0.9f : 0.9f), steel);
            B(t, new Vector3(2.1f, 0.1f, 2.1f), new Vector3(0f, 1.02f, 0f), steel);
            Part(t, MeshKit.Lathe("watertank", new[] { new Vector2(1.05f, 0f), new Vector2(1.1f, 0.1f), new Vector2(1.1f, 2.1f), new Vector2(0.9f, 2.35f), new Vector2(0.3f, 2.45f), new Vector2(0f, 2.46f) }, 28, true, true), new Vector3(0f, 1.07f, 0f), plastic, false);
            var col = t.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 2.3f, 0f);
            col.radius = 1.1f;
            col.height = 2.5f;
            return Finish(t, "watertank", new Vector3(0f, 2.2f, 0f), 1f);
        }

        static readonly Color[][] Adverts =
        {
            new[] { new Color(0.9f, 0.2f, 0.12f), new Color(0.98f, 0.85f, 0.2f), new Color(0.95f, 0.95f, 0.9f) },
            new[] { new Color(0.1f, 0.35f, 0.7f), new Color(0.95f, 0.95f, 0.9f), new Color(0.98f, 0.55f, 0.1f) },
            new[] { new Color(0.15f, 0.55f, 0.3f), new Color(0.98f, 0.92f, 0.7f), new Color(0.1f, 0.1f, 0.12f) },
        };

        /// <summary>A 9 × 3.5 m billboard on two steel legs, lit from below. The face comes down in panels.</summary>
        static Destructible Billboard(Transform t, int variant)
        {
            var steel = Mats.Lit(Mats.DarkMetal, 0.7f, 0.4f);
            var ad = Adverts[variant % Adverts.Length];
            var d = Destructible.Add(t.gameObject, "billboard");
            for (int s = -1; s <= 1; s += 2) Part(t, MeshKit.RoundedBox(new Vector3(0.35f, 5f, 0.35f), 0.04f, 1), new Vector3(s * 2.8f, 2.5f, 0.3f), steel, true);
            B(t, new Vector3(9.2f, 0.12f, 1.2f), new Vector3(0f, 5f, -0.2f), steel);
            // The face: three panels (background, a stripe, a block of "text"), each its own piece.
            for (int p = 0; p < 3; p++)
            {
                var panel = new GameObject("Panel").transform;
                panel.SetParent(t, false);
                panel.localPosition = new Vector3(-3f + p * 3f, 6.8f, 0f);
                B(panel, new Vector3(2.98f, 3.5f, 0.12f), Vector3.zero, Mats.Lit(ad[0], 0f, 0.3f), true);
                B(panel, new Vector3(2.98f, 0.7f, 0.02f), new Vector3(0f, -0.9f, -0.07f), Mats.Lit(ad[1], 0f, 0.3f));
                if (p != 1) B(panel, new Vector3(2.2f, 0.9f, 0.02f), new Vector3(0f, 0.6f, -0.07f), Mats.Lit(ad[2], 0f, 0.3f));
                else Part(panel, MeshKit.Blob(1f, 0.1f, 300 + variant), new Vector3(0f, 0.5f, -0.3f), Mats.Lit(ad[2], 0f, 0.4f), false).transform.localScale = new Vector3(1f, 1f, 0.2f);
                d.Parts.Add(panel);
            }
            for (int i = 0; i < 3; i++)
                B(t, new Vector3(0.4f, 0.12f, 0.2f), new Vector3(-3f + i * 3f, 5.1f, -0.75f), Mats.Emissive(new Color(1f, 0.95f, 0.85f), new Color(2f, 1.8f, 1.5f)));
            d.FirePoint = new Vector3(0f, 6f, 0f);
            d.FireSize = 1.4f;
            return d;
        }

        /// <summary>Traffic light: a steel pole and a three-lamp head facing the road (+Z), the red lit.</summary>
        static Destructible TrafficLight(Transform t)
        {
            var steel = Mats.Lit(new Color(0.22f, 0.23f, 0.22f), 0.6f, 0.45f);
            Part(t, MeshKit.Lathe("signalpole", new[] { new Vector2(0.09f, 0f), new Vector2(0.07f, 0.3f), new Vector2(0.06f, 3.3f), new Vector2(0f, 3.32f) }, 12), Vector3.zero, steel, false);
            B(t, new Vector3(0.36f, 1f, 0.3f), new Vector3(0f, 3.6f, 0f), Mats.Lit(new Color(0.12f, 0.12f, 0.1f), 0.3f, 0.5f));
            var lamps = new[] { new Color(3f, 0.25f, 0.15f), new Color(0.35f, 0.25f, 0.05f), new Color(0.05f, 0.25f, 0.12f) };
            for (int i = 0; i < 3; i++)
            {
                var lit = i == 0 ? Mats.Emissive(new Color(0.9f, 0.15f, 0.1f), lamps[0]) : Mats.Lit(lamps[i], 0f, 0.8f);
                Part(t, MeshKit.RoundedBox(new Vector3(0.2f, 0.2f, 0.05f), 0.09f, 3), new Vector3(0f, 3.92f - i * 0.3f, 0.16f), lit, false);
                B(t, new Vector3(0.26f, 0.04f, 0.14f), new Vector3(0f, 4.04f - i * 0.3f, 0.22f), steel);
            }
            B(t, new Vector3(0.22f, 0.3f, 0.16f), new Vector3(0f, 1.2f, 0.1f), Mats.Lit(new Color(0.85f, 0.75f, 0.15f), 0.2f, 0.5f));   // the push button
            var col = t.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 2f, 0f);
            col.radius = 0.12f;
            col.height = 4.2f;
            return Finish(t, "trafficlight", new Vector3(0f, 3.5f, 0f), 0.3f);
        }

        /// <summary>A street lamp, 6 m, its arm and lit head over the road (+Z).</summary>
        static Destructible StreetLamp(Transform t)
        {
            var metal = Mats.Lit(new Color(0.18f, 0.19f, 0.2f), 0.7f, 0.45f);
            Part(t, MeshKit.Lathe("citylamp", new[] { new Vector2(0.13f, 0f), new Vector2(0.1f, 0.5f), new Vector2(0.065f, 6f), new Vector2(0f, 6.05f) }, 12), Vector3.zero, metal, false);
            B(t, new Vector3(0.08f, 0.08f, 1.6f), new Vector3(0f, 5.9f, 0.8f), metal);
            B(t, new Vector3(0.3f, 0.12f, 0.6f), new Vector3(0f, 5.82f, 1.55f), Mats.Emissive(new Color(1f, 0.85f, 0.6f), new Color(1.8f, 1.2f, 0.65f)));
            var col = t.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 3f, 0f);
            col.radius = 0.14f;
            col.height = 6f;
            return Finish(t, "streetlamp", new Vector3(0f, 3f, 0f), 0.3f);
        }

        /// <summary>A parking meter: a post and a grey head with a little window.</summary>
        static Destructible ParkingMeter(Transform t)
        {
            var grey = Paint(new Color(0.45f, 0.47f, 0.48f), 0.6f, 0.6f);
            B(t, new Vector3(0.07f, 1.05f, 0.07f), new Vector3(0f, 0.52f, 0f), Mats.Lit(Mats.DarkMetal, 0.7f, 0.4f));
            Part(t, MeshKit.RoundedBox(new Vector3(0.24f, 0.36f, 0.18f), 0.08f, 3), new Vector3(0f, 1.2f, 0f), grey, true);
            B(t, new Vector3(0.14f, 0.1f, 0.01f), new Vector3(0f, 1.27f, 0.09f), Mats.Glass(Mats.GlassTint));
            return Finish(t, "parkingmeter", new Vector3(0f, 1.1f, 0f), 0.2f);
        }

        /// <summary>
        /// A derelict tenement, <paramref name="floors"/> storeys of brick, 8 × 6 m: each storey is its own target
        /// (it takes dynamite or TNT, concrete shrugs off the rest). Blow out a storey and everything above it
        /// comes down onto what is left (Building), cracking as it lands. Returns the ground floor.
        /// </summary>
        static Destructible Tenement(Transform t, int floors)
        {
            const float w = 8f, d = 6f, h = 3f;
            var brick = Mats.Surface("brick", new Color(0.85f, 0.62f, 0.5f), 0f, 1f);
            var slabMat = Mats.Surface("concrete", new Color(0.72f, 0.7f, 0.66f), 0f, 0.6f);
            var dark = Mats.Lit(new Color(0.05f, 0.05f, 0.06f), 0f, 0.1f);
            var plank = Mats.Surface("wood", new Color(0.6f, 0.5f, 0.38f), 0f, 0.4f);
            var building = t.gameObject.AddComponent<Building>();
            for (int f = 0; f < floors; f++)
            {
                var storey = new GameObject("Storey " + (f + 1)).transform;
                storey.SetParent(t, false);
                storey.localPosition = new Vector3(0f, f * h, 0f);
                Part(storey, MeshKit.RoundedBox(new Vector3(w, 0.25f, d), 0.02f, 1), new Vector3(0f, h - 0.125f, 0f), slabMat, true);
                var ds = Destructible.Add(storey.gameObject, "storey");
                ds.FirePoint = new Vector3(0f, h * 0.5f, 0f);
                ds.FireSize = 1.6f;
                // Four walls, each its own piece to fly off, with boarded-up windows.
                for (int side = 0; side < 4; side++)
                {
                    bool front = side < 2;
                    float sign = side % 2 == 0 ? -1f : 1f;
                    var wall = new GameObject("Wall").transform;
                    wall.SetParent(storey, false);
                    wall.localPosition = front ? new Vector3(0f, (h - 0.25f) * 0.5f, sign * (d * 0.5f - 0.15f)) : new Vector3(sign * (w * 0.5f - 0.15f), (h - 0.25f) * 0.5f, 0f);
                    float len = front ? w : d - 0.6f;
                    Part(wall, MeshKit.RoundedBox(front ? new Vector3(len, h - 0.25f, 0.3f) : new Vector3(0.3f, h - 0.25f, len), 0.02f, 1), Vector3.zero, brick, true);
                    int windows = front ? 3 : 1;
                    for (int k = 0; k < windows; k++)
                    {
                        float o = (k - (windows - 1) * 0.5f) * 2.4f;
                        var at = front ? new Vector3(o, 0.2f, sign * 0.16f) : new Vector3(sign * 0.16f, 0.2f, o);
                        Part(wall, MeshKit.RoundedBox(front ? new Vector3(1.1f, 1.3f, 0.02f) : new Vector3(0.02f, 1.3f, 1.1f), 0.005f, 1), at, dark, false);
                        if ((k + f) % 2 == 0)
                            Part(wall, MeshKit.RoundedBox(front ? new Vector3(1.2f, 0.18f, 0.03f) : new Vector3(0.03f, 0.18f, 1.2f), 0.005f, 1), at + new Vector3(0f, 0.15f, 0f) + (front ? new Vector3(0f, 0f, sign * 0.02f) : new Vector3(sign * 0.02f, 0f, 0f)), plank, false);
                    }
                    ds.Parts.Add(wall);
                }
                building.Floors.Add(ds);
            }
            return building.Floors[0];
        }

        /// <summary>A timber planter box with a shrub in it.</summary>
        static Destructible Planter(Transform t, int variant)
        {
            var wood = Mats.Surface("wood", new Color(0.55f, 0.4f, 0.28f), 0f, 0.5f);
            B(t, new Vector3(1.4f, 0.6f, 1.4f), new Vector3(0f, 0.3f, 0f), wood, true);
            B(t, new Vector3(1.3f, 0.05f, 1.3f), new Vector3(0f, 0.58f, 0f), Mats.Lit(new Color(0.18f, 0.13f, 0.09f), 0f, 0.1f));
            Part(t, MeshKit.Blob(0.6f, 0.35f, 400 + variant), new Vector3(0f, 1.05f, 0f), Mats.Lit(new Color(0.2f, 0.3f, 0.11f), 0f, 0.15f), false);
            return Finish(t, "planter", new Vector3(0f, 0.6f, 0f), 0.5f);
        }
    }

    /// <summary>
    /// A building that comes down in stages: when a storey is blown out, it is cleared away and every storey
    /// above it falls; a storey that lands hard cracks (most of its strength gone, so the next blast finishes it).
    /// </summary>
    public sealed class Building : MonoBehaviour
    {
        public readonly System.Collections.Generic.List<Destructible> Floors = new System.Collections.Generic.List<Destructible>();
        readonly System.Collections.Generic.HashSet<Destructible> gone = new System.Collections.Generic.HashSet<Destructible>();

        void Update()
        {
            for (int i = 0; i < Floors.Count; i++)
            {
                var f = Floors[i];
                if (f == null || !f.Destroyed || gone.Contains(f)) continue;
                gone.Add(f);
                // The storey is rubble: clear what is left so the ones above can fall into its place.
                foreach (var c in f.GetComponentsInChildren<Collider>()) c.enabled = false;
                foreach (var r in f.GetComponentsInChildren<Renderer>()) r.enabled = false;
                for (int k = i + 1; k < Floors.Count; k++)
                {
                    var above = Floors[k];
                    if (above == null || above.Destroyed || above.GetComponent<Rigidbody>() != null) continue;
                    var rb = above.gameObject.AddComponent<Rigidbody>();
                    rb.mass = 20000f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
                    above.gameObject.AddComponent<FloorFall>();
                    above.gameObject.AddComponent<ImpactDamage>().Arm(6f);
                }
                Sfx.I.Boom(f.transform.position + Vector3.up * 1.5f, 3.5f);
            }
        }
    }

    /// <summary>A falling storey: the first hard landing cracks it.</summary>
    public sealed class FloorFall : MonoBehaviour
    {
        bool landed;

        void OnCollisionEnter(Collision c)
        {
            if (landed || c.relativeVelocity.magnitude < 3f) return;
            landed = true;
            var d = GetComponent<Destructible>();
            if (d == null || d.Destroyed) return;
            Fx.Debris(transform.position + Vector3.up, transform.position + Vector3.down, 8, new Color(0.6f, 0.45f, 0.38f), 2f);
            d.Damage(new Hit { Amount = d.Hp * 0.7f, Origin = transform.position + Vector3.down, ByObject = true, BreaksConcrete = true });
        }
    }
}
