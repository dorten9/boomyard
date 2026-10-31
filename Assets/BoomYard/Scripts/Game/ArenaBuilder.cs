using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>A built level: its set, its targets (everything that has to go), and where the player starts.</summary>
    public sealed class Arena
    {
        public LevelDef Level;
        public ArenaDef Def;
        public GameObject Root;
        public Vector3 Spawn;
        public float SpawnYaw;
        public readonly List<Destructible> Targets = new List<Destructible>();
        public readonly List<Destructible> Objects = new List<Destructible>();
        /// <summary>The object that must survive (LevelRule.Spare), or null.</summary>
        public Destructible Spare;

        public int TargetsLeft
        {
            get { int n = 0; foreach (var t in Targets) if (t != null && !t.Destroyed) n++; return n; }
        }
    }

    /// <summary>
    /// Builds a level: its street at golden hour (houses, yard, road, neighbours, trees, dressing; see
    /// ArenaStreets.cs for each street's houses) and the level's props on it. All geometry and surfaces are
    /// generated (MeshKit, TextureLib).
    /// </summary>
    public static partial class ArenaBuilder
    {
        public static Arena Build(LevelDef level, QualityTier tier)
        {
            if (level.Show) return BuildCity(level, tier);
            var a = new Arena { Level = level, Def = Catalog.Arena(1) };
            StreetLife.DarkRooms.Clear();
            a.Root = new GameObject("Level " + level.Number);
            var root = a.Root.transform;
            a.Spawn = new Vector3(0f, 0f, -8f);
            a.SpawnYaw = 6f;

            var street = level.Street;
            Rendering.Sunset(root, new Vector3(0f, 3f, 2f), street.SunYaw);
            Ground(root, street);
            Houses(root, street);
            Fences(root, street);
            Trees(root, tier, street.Trees);
            Lamps(root);
            if (street.HolidayLights) HolidayLights(root);
            if (street.KerbCars) KerbCars(root, level.Number);
            bool forecourt = false;
            foreach (var spot in level.Props) if (spot.Id == "fuelpump") forecourt = true;
            if (forecourt) Canopy(root);

            for (int i = 0; i < level.Props.Length; i++)
            {
                var spot = level.Props[i];
                var pos = new Vector3(spot.X, spot.Id == "acunit" ? 1.25f : 0f, spot.Z);
                var d = Props.Spawn(spot.Id, root, pos, spot.Yaw, spot.Variant);
                var building = d.GetComponentInParent<Building>();
                if (building != null) a.Targets.AddRange(building.Floors);   // every storey must come down
                else if (level.IsTarget(i)) a.Targets.Add(d);
                else a.Spare = d;
            }
            foreach (var d in a.Root.GetComponentsInChildren<Destructible>()) a.Objects.Add(d);
            MaybeRare(a.Objects, new System.Random(level.Number * 7919 + Daily.Seed(System.DateTime.Now)), RareCars.LevelChance, 1);
            StreetLife.Create(root, new Vector3(-21f, 0f, 6f));   // the dog is next door, behind the fence
            if (level.Number >= 2 && !level.Daily && !SaveService.Data.goldGnomes.Contains(level.Number)) GoldGnome.Hide(root, level);
            return a;
        }

        /// <summary>
        /// With <paramref name="chance"/>, up to <paramref name="count"/> cars come in a rare paint job (RareCars),
        /// ones not yet in the collection first.
        /// </summary>
        static void MaybeRare(List<Destructible> objects, System.Random r, float chance, int count)
        {
            if (r.NextDouble() >= chance) return;
            var owned = SaveService.Data.rareCars;
            var cands = new List<Destructible>();
            foreach (var d in objects) if (d != null && d.Def != null && RareCars.For(d.Def.Id).Count > 0) cands.Add(d);
            for (int k = 0; k < count && cands.Count > 0; k++)
            {
                var d = cands[r.Next(cands.Count)];
                cands.Remove(d);
                var options = RareCars.For(d.Def.Id);
                var fresh = options.FindAll(c => !owned.Contains(c.Id));
                var pick = (fresh.Count > 0 ? fresh : options)[r.Next(fresh.Count > 0 ? fresh.Count : options.Count)];
                RareTag.Apply(d, pick);
            }
        }

        // ---------- ground ----------

        static void Ground(Transform root, StreetDef street)
        {
            var grass = street.PavedYard ? Mats.Surface("asphalt", new Color(0.92f, 0.92f, 0.9f), 0f, 1f)
                : street.Gravel ? Mats.Surface("asphalt", new Color(1.25f, 1.12f, 0.95f), 0f, 0.5f, 1.4f)
                : Mats.Surface("grass", Color.white, 0f, 1f);
            var asphalt = Mats.Surface("asphalt", Color.white, 0f, 1f);
            var concrete = Mats.Surface("concrete", new Color(0.95f, 0.93f, 0.9f), 0f, 1f);
            var kerb = Mats.Surface("concrete", new Color(0.82f, 0.81f, 0.78f), 0f, 0.8f, 0.6f);

            // A wide ground slab carries the collider; surfaces sit just on top of it.
            var slab = Slab(root, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(220f, 1f, 220f), grass);
            slab.GetComponent<MeshRenderer>().sharedMaterial = grass;
            Flat(root, "Street", new Vector3(0f, 0.01f, -16f), new Vector2(220f, 9f), asphalt);
            if (street.PavedYard) ParkingLines(root);
            else
            {
                Flat(root, "Driveway", new Vector3(6.5f, 0.015f, -1f), new Vector2(5.5f, 21f), concrete);
                Flat(root, "Walk", new Vector3(-3.5f, 0.015f, 1.5f), new Vector2(1.4f, 21f), concrete);
            }
            Flat(root, "Sidewalk", new Vector3(0f, 0.02f, -10.4f), new Vector2(220f, 2.2f), concrete);
            Flat(root, "SidewalkFar", new Vector3(0f, 0.02f, -21.6f), new Vector2(220f, 2.2f), concrete);
            Box(root, "Kerb", new Vector3(0f, 0.07f, -11.55f), new Vector3(220f, 0.16f, 0.22f), kerb, true);
            Box(root, "KerbFar", new Vector3(0f, 0.07f, -20.45f), new Vector3(220f, 0.16f, 0.22f), kerb, true);
            // Worn centre line.
            var paint = Mats.Lit(new Color(0.85f, 0.78f, 0.5f), 0f, 0.4f);
            for (int i = -20; i <= 20; i++) Flat(root, "Line", new Vector3(i * 5f, 0.018f, -16f), new Vector2(2.6f, 0.12f), paint);
        }

        // ---------- the houses ----------

        /// <summary>Two-storey house: stucco, brick plinth, gable roof, framed windows, door with porch light, garage.</summary>
        static void House(Transform parent, Vector3 pos, float yaw, Color plaster, bool main, StreetDef street)
        {
            var h = new GameObject(main ? "House" : "Neighbour").transform;
            h.SetParent(parent, false);
            h.position = pos;
            h.rotation = Quaternion.Euler(0f, yaw, 0f);
            const float w = 15f, depth = 9f, wall = 6.1f;
            var walls = Mats.Surface("plaster", plaster, 0f, 1f);
            var brick = Mats.Surface("brick", Color.white, 0f, 1f);
            var trim = Mats.Surface("paint", Rgb(street.Trim), 0f, 0.6f);
            var roof = Mats.Surface("roof", Rgb(street.Roof), 0f, 1f);

            // Walls (front face at local z = 0, building extends back).
            Box(h, "Walls", new Vector3(0f, wall * 0.5f, depth * 0.5f), new Vector3(w, wall, depth), walls, true);
            Box(h, "Plinth", new Vector3(0f, 0.45f, -0.04f), new Vector3(w + 0.1f, 0.9f, 0.12f), brick, false);
            Box(h, "Band", new Vector3(0f, 3.1f, -0.04f), new Vector3(w + 0.06f, 0.16f, 0.1f), trim, false);
            var roofGo = Props.Part(h, MeshKit.Gable(new Vector3(depth, 3f, w), 0.5f), new Vector3(0f, wall, depth * 0.5f), roof, false);
            roofGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Box(h, "Fascia", new Vector3(0f, wall - 0.08f, -0.52f), new Vector3(w + 1f, 0.22f, 0.06f), trim, false);
            Box(h, "Gutter", new Vector3(0f, wall - 0.04f, -0.6f), new Vector3(w + 1f, 0.12f, 0.12f), Mats.Lit(new Color(0.85f, 0.85f, 0.83f), 0.6f, 0.5f), false);
            Box(h, "Chimney", new Vector3(-4.5f, wall + 2.2f, depth * 0.6f), new Vector3(0.9f, 3.2f, 0.9f), brick, false);

            // Door with frame, step and porch canopy and lamp.
            float dx = -3.5f;
            Box(h, "Door", new Vector3(dx, 1.08f, -0.02f), new Vector3(1.0f, 2.16f, 0.08f), Mats.Surface("wood", new Color(0.55f, 0.32f, 0.22f), 0f, 0.7f), false);
            Box(h, "DoorFrame", new Vector3(dx, 2.22f, -0.05f), new Vector3(1.3f, 0.12f, 0.12f), trim, false);
            Box(h, "Step", new Vector3(dx, 0.09f, -0.6f), new Vector3(1.8f, 0.18f, 1.0f), Mats.Surface("concrete", Color.white, 0f, 0.8f), true);
            Box(h, "Canopy", new Vector3(dx, 2.65f, -0.55f), new Vector3(2.0f, 0.1f, 1.1f), trim, false);
            Box(h, "PorchLamp", new Vector3(dx + 0.8f, 2.1f, -0.1f), new Vector3(0.14f, 0.24f, 0.14f), Mats.Emissive(new Color(1f, 0.85f, 0.6f), new Color(2.2f, 1.5f, 0.8f)), false);

            // Garage door (right side), ribbed steel: it breaks, and a blast in front of it sends it flying.
            if (main)
            {
                Box(h, "Recess", new Vector3(5.2f, 1.2f, 0.02f), new Vector3(3.2f, 2.4f, 0.04f), Mats.Lit(new Color(0.05f, 0.05f, 0.055f), 0f, 0.1f), false);
                var door = new GameObject("GarageDoor").transform;
                door.SetParent(h, false);
                door.localPosition = new Vector3(5.2f, 1.2f, -0.06f);
                Box(door, "Garage", Vector3.zero, new Vector3(3.2f, 2.4f, 0.08f), Mats.Surface("paint", new Color(0.88f, 0.88f, 0.85f), 0.3f, 0.5f), true);
                for (int i = 0; i < 5; i++) Box(door, "Rib", new Vector3(0f, -0.9f + i * 0.48f, -0.05f), new Vector3(3.15f, 0.04f, 0.03f), trim, false);
                var gd = Destructible.Add(door.gameObject, "garagedoor");
                gd.FirePoint = Vector3.zero;
                gd.FireSize = 0.8f;
            }

            float[] xs = main ? new[] { -6.2f, -1.2f, 1.6f } : new[] { -5f, 0.5f, 4.5f };
            float[] upper = { -6.2f, -3.5f, -0.6f, 2.4f, 5.2f };
            int lit = 0;
            foreach (var x in xs) Window(h, new Vector3(x, 1.55f, 0f), new Vector2(1.25f, 1.35f), (lit++ % 2) == 0, trim);
            foreach (var x in upper) Window(h, new Vector3(x, 4.5f, 0f), new Vector2(1.1f, 1.25f), (lit++ % 3) == 0, trim);
        }

        static void Window(Transform h, Vector3 p, Vector2 size, bool lit, UnityEngine.Material trim, string id = "window")
        {
            var room = lit ? Mats.Emissive(new Color(0.35f, 0.25f, 0.16f), new Color(0.9f, 0.55f, 0.25f)) : Mats.Lit(new Color(0.04f, 0.04f, 0.05f), 0f, 0.1f);
            // Everything stands proud of the wall face (z = 0): the dark room, the glass, then the frame.
            var roomGo = Box(h, "Room", p + new Vector3(0f, 0f, -0.012f), new Vector3(size.x, size.y, 0.02f), room, false);
            if (!lit) StreetLife.DarkRooms.Add(roomGo.GetComponent<Renderer>());   // a neighbour may switch it on
            Box(h, "FrameT", p + new Vector3(0f, size.y * 0.5f + 0.05f, -0.06f), new Vector3(size.x + 0.2f, 0.1f, 0.1f), trim, false);
            Box(h, "FrameL", p + new Vector3(-size.x * 0.5f - 0.05f, 0f, -0.06f), new Vector3(0.1f, size.y + 0.2f, 0.1f), trim, false);
            Box(h, "FrameR", p + new Vector3(size.x * 0.5f + 0.05f, 0f, -0.06f), new Vector3(0.1f, size.y + 0.2f, 0.1f), trim, false);
            Box(h, "Sill", p + new Vector3(0f, -size.y * 0.5f - 0.07f, -0.12f), new Vector3(size.x + 0.3f, 0.07f, 0.24f), Mats.Surface("concrete", Color.white, 0f, 0.6f), false);
            Box(h, "Mullion", p + new Vector3(0f, 0f, -0.05f), new Vector3(0.05f, size.y, 0.04f), trim, false);

            var g = new GameObject("Window").transform;
            g.SetParent(h, false);
            g.localPosition = p + new Vector3(0f, 0f, -0.035f);
            Props.Part(g, MeshKit.RoundedBox(new Vector3(size.x, size.y, 0.015f), 0.005f, 1), Vector3.zero, Mats.Glass(Mats.GlassTint), true);
            var d = Destructible.Add(g.gameObject, id);
            d.FirePoint = Vector3.zero;
            d.FireSize = 0.5f;
        }

        // ---------- set dressing ----------

        static void TimberFences(Transform root)
        {
            // Timber privacy fences on the lot lines (scenery, not targets).
            var wood = Mats.Surface("wood", new Color(0.72f, 0.62f, 0.5f), 0f, 0.6f);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 9; i++)
                {
                    float z = -8.5f + i * 2.5f;
                    Box(root, "Panel", new Vector3(s * 13.5f, 0.9f, z), new Vector3(0.06f, 1.8f, 2.45f), wood, true);
                    Box(root, "Post", new Vector3(s * 13.5f, 0.95f, z - 1.25f), new Vector3(0.12f, 1.9f, 0.12f), wood, false);
                }
        }

        static void Trees(Transform root, QualityTier tier, int near)
        {
            var bark = Mats.Surface("wood", new Color(0.42f, 0.35f, 0.3f), 0f, 0.3f, 0.4f);
            var rng = new System.Random(3);
            var spots = new List<Vector3>
            {
                new Vector3(-11f, 0f, 9f), new Vector3(11.5f, 0f, -6f), new Vector3(-9f, 0f, -6.5f), new Vector3(10f, 0f, 20f),
                new Vector3(-20f, 0f, -7f), new Vector3(20f, 0f, -8f), new Vector3(-34f, 0f, 4f), new Vector3(33f, 0f, 2f),
                new Vector3(-4f, 0f, -26f), new Vector3(5f, 0f, -27f), new Vector3(-24f, 0f, -26f), new Vector3(26f, 0f, -26f),
            };
            if (near < spots.Count) spots.RemoveRange(near, spots.Count - near);
            // A ring of trees closes the horizon.
            int ring = tier == QualityTier.Low ? 18 : 30;
            for (int i = 0; i < ring; i++)
            {
                float a = i / (float)ring * Mathf.PI * 2f;
                float r = 62f + (float)rng.NextDouble() * 12f;
                spots.Add(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            int k = 0;
            foreach (var p in spots)
            {
                float scale = 0.85f + (float)rng.NextDouble() * 0.5f;
                if (p.magnitude > 50f) scale *= 1.6f;
                var tree = new GameObject("Tree").transform;
                tree.SetParent(root, false);
                tree.position = p;
                var trunk = Props.Part(tree, MeshKit.Lathe("trunk", new[] { new Vector2(0.24f, 0f), new Vector2(0.17f, 0.4f), new Vector2(0.13f, 3.2f), new Vector2(0.06f, 4.2f) }, 10), Vector3.zero, bark, false);
                trunk.transform.localScale = Vector3.one * scale;
                if (p.magnitude < 50f) { var c = trunk.AddComponent<CapsuleCollider>(); c.radius = 0.22f; c.height = 4f; c.center = new Vector3(0f, 2f, 0f); }
                int blobs = 3 + rng.Next(3);
                for (int b = 0; b < blobs; b++)
                {
                    var leaf = Mats.Lit(Color.Lerp(new Color(0.2f, 0.27f, 0.1f), new Color(0.34f, 0.38f, 0.14f), (float)rng.NextDouble()), 0f, 0.15f);
                    var offset = new Vector3((float)rng.NextDouble() * 2.4f - 1.2f, 3.6f + (float)rng.NextDouble() * 1.6f, (float)rng.NextDouble() * 2.4f - 1.2f) * scale;
                    Props.Part(tree, MeshKit.Blob((1.5f + (float)rng.NextDouble() * 0.8f) * scale, 0.35f, k++), offset, leaf, false);
                }
            }
            if (near < 8) return;
            // Low hedges along the house.
            var hedge = Mats.Lit(new Color(0.17f, 0.25f, 0.09f), 0f, 0.12f);
            for (int i = 0; i < 4; i++) Props.Part(root, MeshKit.Blob(0.75f, 0.3f, 100 + i), new Vector3(-7.4f + i * 1.3f, 0.45f, 12.8f), hedge, false);
        }

        static void Lamps(Transform root)
        {
            foreach (var at in new[] { new Vector3(-9f, 0f, -10f), new Vector3(12f, 0f, -21.2f) })
            {
                var lamp = new GameObject("StreetLamp").transform;
                lamp.SetParent(root, false);
                lamp.position = at;
                var metal = Mats.Lit(new Color(0.18f, 0.19f, 0.2f), 0.7f, 0.45f);
                Props.Part(lamp, MeshKit.Lathe("lamppole", new[] { new Vector2(0.11f, 0f), new Vector2(0.09f, 0.4f), new Vector2(0.06f, 5.3f), new Vector2(0f, 5.35f) }, 12), Vector3.zero, metal, false);
                Box(lamp, "Arm", new Vector3(0.65f, 5.2f, 0f), new Vector3(1.3f, 0.08f, 0.08f), metal, false);
                Box(lamp, "Head", new Vector3(1.25f, 5.12f, 0f), new Vector3(0.55f, 0.12f, 0.26f), Mats.Emissive(new Color(1f, 0.85f, 0.6f), new Color(1.6f, 1.1f, 0.6f)), false);
            }
        }

        // ---------- primitives ----------

        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, UnityEngine.Material mat, bool collider)
        {
            float r = Mathf.Min(0.03f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.3f);
            var go = Props.Part(parent, MeshKit.RoundedBox(size, r, 1), pos, mat, collider);
            go.name = name;
            return go;
        }

        static GameObject Slab(Transform parent, string name, Vector3 pos, Vector3 size, UnityEngine.Material mat)
        {
            var go = Props.Part(parent, MeshKit.RoundedBox(size, 0.01f, 1), pos, mat, true);
            go.name = name;
            return go;
        }

        /// <summary>A thin flat surface (no collider): roads and paths lying on the ground slab.</summary>
        static void Flat(Transform parent, string name, Vector3 pos, Vector2 size, UnityEngine.Material mat)
        {
            var go = Props.Part(parent, MeshKit.RoundedBox(new Vector3(size.x, 0.02f, size.y), 0.005f, 1), pos, mat, false);
            go.name = name;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
