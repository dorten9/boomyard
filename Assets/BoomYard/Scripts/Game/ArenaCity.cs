using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// The big city: a downtown of three by three blocks on a street grid (roads every 48 m), ringed by towers.
    /// The plaza in the middle, shops with flats above, an office tower, a filling station, a car park, a
    /// building site, terraces and an apartment tower, with cars at the kerbs and street furniture everywhere.
    /// Buildings are scenery to paint on; everything at street level breaks and pays.
    /// </summary>
    public static partial class ArenaBuilder
    {
        const float Pitch = 48f, Kerb = 0.16f;
        static readonly float[] Roads = { -72f, -24f, 24f, 72f };

        static Arena BuildCity(LevelDef level, QualityTier tier)
        {
            var a = new Arena { Level = level, Def = Catalog.Arena(1) };
            StreetLife.DarkRooms.Clear();
            a.Root = new GameObject("Big City");
            var root = a.Root.transform;
            a.Spawn = new Vector3(0f, Kerb, -30.5f);
            a.SpawnYaw = 0f;
            var shops = Streets.ForLevel(27);
            var rng = new System.Random(2024);

            Rendering.Sunset(root, new Vector3(0f, 3f, 0f), shops.SunYaw);
            CityGround(root);
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    var b = Group(root, "Block " + i + "," + j, new Vector3(i * Pitch, Kerb, j * Pitch), 0f);
                    BlockCorners(b, rng);
                    if (i == 0 && j == 0) Plaza(b, rng, tier);
                    else if (i == 0 && j == -1) ShopsAndFlats(b, shops, rng, false);
                    else if (i == -1 && j == 0) OfficeTower(b, rng);
                    else if (i == 1 && j == 0) FillingStation(b, shops, rng);
                    else if (i == 0 && j == 1) CarPark(b, rng);
                    else if (i == -1 && j == -1) BuildingSite(b, rng);
                    else if (i == 1 && j == -1) ShopsAndFlats(b, shops, rng, true);
                    else if (i == -1 && j == 1) Terraces(b, rng);
                    else ApartmentTower(b, rng);
                }
            Junctions(root);
            CityKerbCars(root, rng);
            Skyline(root, rng);

            foreach (var d in a.Root.GetComponentsInChildren<Destructible>()) a.Objects.Add(d);
            StreetLife.Create(root, new Vector3(-48f, 0f, 46f));   // the dog lives in the terraces' back gardens
            a.Root.AddComponent<CityCull>();
            return a;
        }

        // ---------- streets ----------

        static void CityGround(Transform root)
        {
            var asphalt = Mats.Surface("asphalt", Color.white, 0f, 1f);
            var paint = Mats.Lit(new Color(0.85f, 0.78f, 0.5f), 0f, 0.4f);
            var slab = Slab(root, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(240f, 1f, 240f), asphalt);
            slab.GetComponent<MeshRenderer>().sharedMaterial = asphalt;
            foreach (var r in Roads)
                for (float s = -96f; s < 96f; s += 6f)
                {
                    // Dashed centre lines, not across the junctions.
                    bool junction = false;
                    foreach (var q in Roads) if (Mathf.Abs(s + 1.5f - q) < 7f) junction = true;
                    if (junction) continue;
                    Flat(root, "Line", new Vector3(s + 1.5f, 0.018f, r), new Vector2(3f, 0.14f), paint);
                    Flat(root, "Line", new Vector3(r, 0.019f, s + 1.5f), new Vector2(0.14f, 3f), paint);
                }
            // Pavement round every block (and the outer blocks under the skyline): a raised slab with a kerb.
            var pave = Mats.Surface("concrete", new Color(0.93f, 0.91f, 0.88f), 0f, 1f, 0.7f);
            for (int i = -2; i <= 2; i++)
                for (int j = -2; j <= 2; j++)
                {
                    float half = Mathf.Abs(i) == 2 || Mathf.Abs(j) == 2 ? 0f : 19f;
                    if (half == 0f) continue;
                    Box(root, "Pavement", new Vector3(i * Pitch, Kerb * 0.5f, j * Pitch), new Vector3(half * 2f, Kerb, half * 2f), pave, true);
                }
            // The ring outside the last roads.
            foreach (var side in new[] { -1f, 1f })
            {
                Box(root, "OuterPave", new Vector3(side * 92.5f, Kerb * 0.5f, 0f), new Vector3(31f, Kerb, 200f), pave, true);
                Box(root, "OuterPave", new Vector3(0f, Kerb * 0.5f, side * 92.5f), new Vector3(154f, Kerb, 31f), pave, true);
            }
            // Invisible walls at the edge of town.
            foreach (var side in new[] { -1f, 1f })
            {
                Wall(root, new Vector3(side * 104f, 10f, 0f), new Vector3(1f, 20f, 210f));
                Wall(root, new Vector3(0f, 10f, side * 104f), new Vector3(210f, 20f, 1f));
            }
        }

        static void Wall(Transform root, Vector3 at, Vector3 size)
        {
            var go = new GameObject("EdgeOfTown");
            go.transform.SetParent(root, false);
            go.transform.position = at;
            go.AddComponent<BoxCollider>().size = size;
        }

        /// <summary>Crossings and traffic lights at the four junctions round the plaza.</summary>
        static void Junctions(Transform root)
        {
            var white = Mats.Lit(new Color(0.92f, 0.92f, 0.88f), 0f, 0.4f);
            foreach (var x in new[] { -24f, 24f })
                foreach (var z in new[] { -24f, 24f })
                {
                    for (int k = 0; k < 7; k++)
                    {
                        float o = -4.2f + k * 1.4f;
                        Flat(root, "Zebra", new Vector3(x + o, 0.021f, z - 6.6f), new Vector2(0.7f, 2.6f), white);
                        Flat(root, "Zebra", new Vector3(x + o, 0.021f, z + 6.6f), new Vector2(0.7f, 2.6f), white);
                        Flat(root, "Zebra", new Vector3(x - 6.6f, 0.022f, z + o), new Vector2(2.6f, 0.7f), white);
                        Flat(root, "Zebra", new Vector3(x + 6.6f, 0.022f, z + o), new Vector2(2.6f, 0.7f), white);
                    }
                    for (int c = 0; c < 4; c++)
                    {
                        float sx = c % 2 == 0 ? -1f : 1f, sz = c < 2 ? -1f : 1f;
                        // Each head faces the traffic coming at it.
                        Props.Spawn("trafficlight", root, new Vector3(x + sx * 5.6f, Kerb, z + sz * 5.6f), c == 0 ? 90f : c == 1 ? 0f : c == 2 ? 180f : 270f);
                    }
                }
        }

        static readonly string[] KerbFleet = { "sedan", "van", "sedan", "pickup", "sportscar", "sedan", "scooter", "boxtruck", "van", "sedan" };

        /// <summary>Cars parked along the kerbs of every street between the junctions, both sides.</summary>
        static void CityKerbCars(Transform root, System.Random rng)
        {
            int n = 0;
            for (int r = 0; r < Roads.Length; r++)
                for (int seg = 0; seg < 3; seg++)
                    for (int side = -1; side <= 1; side += 2)
                        for (int axis = 0; axis < 2; axis++)
                        {
                            if (rng.NextDouble() < 0.35) continue;
                            float along = Roads[seg] + 13f + (float)rng.NextDouble() * 22f;
                            float across = Roads[r] + side * 3.6f;
                            string id = KerbFleet[(n + rng.Next(3)) % KerbFleet.Length];
                            // Parked with the traffic: facing along the road, on the right-hand side.
                            var pos = axis == 0 ? new Vector3(along, 0f, across) : new Vector3(across, 0f, along);
                            float yaw = axis == 0 ? (side < 0 ? 90f : 270f) : (side < 0 ? 180f : 0f);
                            Props.Spawn(id, root, pos, yaw, n++);
                        }
        }

        /// <summary>Lamps, a hydrant and a bin at the corners of a block; trees in the pavement between.</summary>
        static void BlockCorners(Transform b, System.Random rng)
        {
            for (int c = 0; c < 4; c++)
            {
                float sx = c % 2 == 0 ? -1f : 1f, sz = c < 2 ? -1f : 1f;
                var corner = new Vector3(sx * 17.6f, 0f, sz * 17.6f);
                // The lamp's arm reaches out over the road.
                Spawn("streetlamp", b, corner + new Vector3(0f, 0f, sz * 0.5f), sz < 0 ? 180f : 0f);
                if (c == 0 || c == 3) Spawn("hydrant", b, corner + new Vector3(-sx * 3f, 0f, sz * 0.4f), 0f);
                Spawn("bin", b, corner + new Vector3(-sx * 1.6f, 0f, -sz * 1.4f), rng.Next(360), rng.Next(3));
            }
            for (int side = 0; side < 4; side++)
                for (int k = -1; k <= 1; k += 2)
                {
                    var p = side == 0 ? new Vector3(k * 8f, 0f, -17.5f) : side == 1 ? new Vector3(k * 8f, 0f, 17.5f)
                        : side == 2 ? new Vector3(-17.5f, 0f, k * 8f) : new Vector3(17.5f, 0f, k * 8f);
                    StreetTree(b, p, 0.85f + (float)rng.NextDouble() * 0.3f, side * 2 + (k + 1) / 2);
                }
        }

        static Destructible Spawn(string id, Transform block, Vector3 local, float yaw, int variant = 0)
        {
            return Props.Spawn(id, block, block.TransformPoint(local), yaw, variant);
        }

        static void StreetTree(Transform parent, Vector3 local, float scale, int seed)
        {
            var bark = Mats.Surface("wood", new Color(0.42f, 0.35f, 0.3f), 0f, 0.3f, 0.4f);
            var tree = new GameObject("Tree").transform;
            tree.SetParent(parent, false);
            tree.localPosition = local;
            Box(tree, "Grate", new Vector3(0f, 0.01f, 0f), new Vector3(1.2f, 0.02f, 1.2f), Mats.Lit(new Color(0.12f, 0.12f, 0.12f), 0.7f, 0.4f), false);
            var trunk = Props.Part(tree, MeshKit.Lathe("trunk", new[] { new Vector2(0.24f, 0f), new Vector2(0.17f, 0.4f), new Vector2(0.13f, 3.2f), new Vector2(0.06f, 4.2f) }, 10), Vector3.zero, bark, false);
            trunk.transform.localScale = Vector3.one * scale;
            var c = trunk.AddComponent<CapsuleCollider>();
            c.radius = 0.22f; c.height = 4f; c.center = new Vector3(0f, 2f, 0f);
            var rng = new System.Random(seed * 13 + 5);
            for (int b = 0; b < 3; b++)
            {
                var leaf = Mats.Lit(Color.Lerp(new Color(0.2f, 0.27f, 0.1f), new Color(0.34f, 0.38f, 0.14f), (float)rng.NextDouble()), 0f, 0.15f);
                var offset = new Vector3((float)rng.NextDouble() * 1.6f - 0.8f, 3.8f + (float)rng.NextDouble() * 1.2f, (float)rng.NextDouble() * 1.6f - 0.8f) * scale;
                Props.Part(tree, MeshKit.Blob((1.3f + (float)rng.NextDouble() * 0.6f) * scale, 0.35f, 500 + seed * 3 + b), offset, leaf, false);
            }
        }

        // ---------- buildings ----------

        static readonly Color[] FacadePaint =
        {
            new Color(0.95f, 0.88f, 0.76f), new Color(0.86f, 0.74f, 0.62f), new Color(0.78f, 0.82f, 0.85f),
            new Color(0.92f, 0.8f, 0.68f), new Color(0.8f, 0.7f, 0.66f), new Color(0.88f, 0.9f, 0.86f),
        };

        /// <summary>A block of flats: window bays all round, a roof slab with a parapet and some plant on top.</summary>
        static void Flats(Transform parent, Vector3 local, Vector3 size, Color paint, int seed)
        {
            var g = new GameObject("Flats").transform;
            g.SetParent(parent, false);
            g.localPosition = local;
            var face = Mats.Surface("facade", paint, 0f, 1f);
            var roof = Mats.Surface("concrete", new Color(0.7f, 0.69f, 0.66f), 0f, 0.6f);
            Box(g, "Flats", new Vector3(0f, size.y * 0.5f, 0f), size, face, true);
            Box(g, "Roof", new Vector3(0f, size.y + 0.1f, 0f), new Vector3(size.x + 0.3f, 0.2f, size.z + 0.3f), roof, false);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(g, "Parapet", new Vector3(s * size.x * 0.5f, size.y + 0.5f, 0f), new Vector3(0.25f, 0.8f, size.z + 0.3f), roof, false);
                Box(g, "Parapet", new Vector3(0f, size.y + 0.5f, s * size.z * 0.5f), new Vector3(size.x + 0.3f, 0.8f, 0.25f), roof, false);
            }
            var rng = new System.Random(seed);
            for (int i = 0; i < 2; i++)
                Box(g, "RoofPlant", new Vector3(((float)rng.NextDouble() - 0.5f) * size.x * 0.6f, size.y + 0.9f, ((float)rng.NextDouble() - 0.5f) * size.z * 0.6f), new Vector3(2.2f, 1.6f, 1.6f), Mats.Lit(new Color(0.6f, 0.61f, 0.6f), 0.5f, 0.4f), false);
        }

        /// <summary>Shops along the plaza road with flats over them; the corner block gets shops on two sides.</summary>
        static void ShopsAndFlats(Transform b, StreetDef s, System.Random rng, bool corner)
        {
            var at = b.position;
            ShopRow(b.parent, at + new Vector3(0f, 0f, 16f), 180f, s, 4, rng.Next(5));
            Flats(b, new Vector3(0f, 7f, 10.5f), new Vector3(26f, 12f, 10f), FacadePaint[rng.Next(FacadePaint.Length)], rng.Next());
            if (corner)
            {
                ShopRow(b.parent, at + new Vector3(-16f, 0f, -3f), 90f, s, 3, rng.Next(5));
                Flats(b, new Vector3(-10.5f, 7f, -3f), new Vector3(10f, 15f, 19.5f), FacadePaint[rng.Next(FacadePaint.Length)], rng.Next());
                Flats(b, new Vector3(6f, 0f, -8f), new Vector3(20f, 24f, 16f), FacadePaint[rng.Next(FacadePaint.Length)], rng.Next());
            }
            else Flats(b, new Vector3(0f, 0f, -6f), new Vector3(30f, 18f, 20f), FacadePaint[rng.Next(FacadePaint.Length)], rng.Next());
            // Out on the pavement: a cafe, papers, scooters and bikes.
            for (int i = 0; i < 3; i++) Spawn("cafetable", b, new Vector3(-6.5f + i * 2.4f, 0f, 17.2f), rng.Next(360), i);
            Spawn("newsbox", b, new Vector3(4f, 0f, 17.4f), 180f, rng.Next(3));
            Spawn("newsbox", b, new Vector3(4.7f, 0f, 17.4f), 180f, rng.Next(3));
            Spawn("scooter", b, new Vector3(10.5f, 0f, 17.6f), 90f, rng.Next(3));
            Spawn("bicycle", b, new Vector3(13f, 0f, 17.6f), 90f);
            Spawn("planter", b, new Vector3(-13f, 0f, 17.2f), 0f, rng.Next(9));
            Spawn("parkingmeter", b, new Vector3(-4f, 0f, 18.4f), 180f);
            Spawn("parkingmeter", b, new Vector3(1f, 0f, 18.4f), 180f);
        }

        /// <summary>An office tower: a glass lobby (its panes break) under a curtain-wall shaft 50 m high.</summary>
        static void OfficeTower(Transform b, System.Random rng)
        {
            const float w = 24f, lobby = 4.6f, height = 50f;
            var core = Mats.Surface("concrete", new Color(0.55f, 0.55f, 0.56f), 0f, 0.6f);
            var steel = Mats.Lit(new Color(0.5f, 0.52f, 0.54f), 0.8f, 0.6f);
            var curtain = Mats.Surface("curtain", Color.white, 0.2f, 1f);
            Box(b, "Lobby", new Vector3(0f, lobby * 0.5f, 0f), new Vector3(w - 1.4f, lobby, w - 1.4f), core, true);
            Box(b, "Shaft", new Vector3(0f, lobby + (height - lobby) * 0.5f, 0f), new Vector3(w, height - lobby, w), curtain, true);
            Box(b, "Crown", new Vector3(0f, height + 1f, 0f), new Vector3(w - 4f, 2f, w - 4f), steel, false);
            Box(b, "Mast", new Vector3(3f, height + 6f, 2f), new Vector3(0.3f, 10f, 0.3f), steel, false);
            Box(b, "Beacon", new Vector3(3f, height + 11.1f, 2f), new Vector3(0.4f, 0.4f, 0.4f), Mats.Emissive(new Color(1f, 0.2f, 0.1f), new Color(4f, 0.5f, 0.3f)), false);
            var trim = Mats.Lit(new Color(0.3f, 0.31f, 0.32f), 0.7f, 0.6f);
            foreach (var outward in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                var face = Group(b, "LobbyFace", b.TransformPoint(outward * (w * 0.5f)), FaceYaw(outward));
                for (int k = 0; k < 6; k++)
                {
                    float x = -w * 0.5f + 2f + k * 4f;
                    Box(face, "Pillar", new Vector3(x - 2f, lobby * 0.5f, 0.2f), new Vector3(0.5f, lobby, 0.5f), core, true);
                    Window(face, new Vector3(x, 2.1f, 0.05f), new Vector2(3.4f, 3.8f), true, trim, "storefront");
                }
            }
            // The plaza in front: planters, benches, a bike rack.
            for (int i = 0; i < 3; i++) Spawn("planter", b, new Vector3(14.6f, 0f, -9f + i * 9f), 0f, i + 3);
            Spawn("bench", b, new Vector3(14.8f, 0f, -4.5f), 90f);
            Spawn("bench", b, new Vector3(14.8f, 0f, 4.5f), 90f);
            Spawn("vending", b, new Vector3(-14.6f, 0f, 6f), 90f, 1);
            for (int i = 0; i < 3; i++) Spawn("bicycle", b, new Vector3(-6f + i * 1f, 0f, -14.8f), 0f);
        }

        /// <summary>Yaw that turns a group's front (local −Z) to face <paramref name="outward"/>.</summary>
        static float FaceYaw(Vector3 outward) { return Mathf.Atan2(-outward.x, -outward.z) * Mathf.Rad2Deg; }

        /// <summary>A filling station: the canopy over four pumps, a fuel tanker, the kiosk, and its gas bottles.</summary>
        static void FillingStation(Transform b, StreetDef s, System.Random rng)
        {
            Box(b, "Forecourt", new Vector3(-2f, 0.01f, 0f), new Vector3(30f, 0.02f, 30f), Mats.Surface("concrete", new Color(0.8f, 0.79f, 0.76f), 0f, 0.8f), false);
            var canopy = Group(b, "Station", b.TransformPoint(new Vector3(-4f, 0f, -6.5f)), 0f);
            Canopy(canopy);
            foreach (var x in new[] { -5.5f, 2.4f })
                foreach (var z in new[] { 5f, 8f })
                    Props.Spawn("fuelpump", canopy, canopy.TransformPoint(new Vector3(x, 0f, z)), 90f);
            ShopRow(b.parent, b.position + new Vector3(6f, 0f, 2f), 90f, s, 2, rng.Next(5));
            Spawn("fueltruck", b, new Vector3(-6f, 0f, 11f), 90f);
            for (int i = 0; i < 4; i++) Spawn("gascyl", b, new Vector3(4.5f, 0f, -10f + i * 0.6f), 0f);
            for (int i = 0; i < 3; i++) Spawn("barrel", b, new Vector3(4.6f, 0f, 9f + i * 0.8f), 0f, i);
            Spawn("vending", b, new Vector3(4.8f, 0f, -6.5f), 270f, 0);
            Spawn("sedan", b, new Vector3(-7.3f, 0f, -0.5f), 0f, rng.Next(4));
            Spawn("billboard", b, new Vector3(-12f, 0f, -15f), 0f, 1);
        }

        /// <summary>A surface car park: two rows of bays off a centre aisle, half of them taken, and a pay booth.</summary>
        static void CarPark(Transform b, System.Random rng)
        {
            var lot = Mats.Surface("asphalt", new Color(0.85f, 0.85f, 0.85f), 0f, 1f, 0.8f);
            var paint = Mats.Lit(new Color(0.92f, 0.92f, 0.88f), 0f, 0.4f);
            Box(b, "Lot", new Vector3(0f, 0.01f, 0f), new Vector3(32f, 0.02f, 32f), lot, false);
            string[] fleet = { "sedan", "van", "pickup", "sportscar", "sedan", "golfcart", "sedan", "caravan", "pickup", "sedan", "van", "sportscar" };
            int car = 0;
            for (int row = -1; row <= 1; row += 2)
                for (int k = 0; k < 10; k++)
                {
                    float x = -14f + k * 3.1f, z = row * 9f;
                    Box(b, "Bay", new Vector3(x - 1.55f, 0.03f, z), new Vector3(0.12f, 0.02f, 5f), paint, false);
                    if (rng.NextDouble() < 0.4 && car < fleet.Length)
                        Spawn(fleet[car], b, new Vector3(x, 0f, z + (fleet[car] == "caravan" ? 0f : row * 0.2f)), row > 0 ? 180f : 0f, car++);
                }
            Spawn("boxtruck", b, new Vector3(9f, 0f, 0.5f), 90f);
            for (int i = 0; i < 4; i++) Spawn("cone", b, new Vector3(-6f + i * 2f, 0f, 2f), 0f);
            for (int i = 0; i < 3; i++) Spawn("barrier", b, new Vector3(-15f, 0f, -12f + i * 3.1f), 0f);
            // Pay booth: a little box with glass all round (it breaks).
            var booth = Group(b, "Booth", b.TransformPoint(new Vector3(13.5f, 0f, -14f)), 0f);
            Box(booth, "Booth", new Vector3(0f, 1.3f, 0f), new Vector3(2f, 2.6f, 2f), Mats.Surface("paint", new Color(0.85f, 0.75f, 0.3f), 0.2f, 0.6f), true);
            Box(booth, "BoothRoof", new Vector3(0f, 2.7f, 0f), new Vector3(2.4f, 0.15f, 2.4f), Mats.Lit(Mats.DarkMetal, 0.6f, 0.4f), false);
            Window(booth, new Vector3(0f, 1.6f, -1f), new Vector2(1.5f, 1f), true, paint);
            Spawn("planter", b, new Vector3(-13f, 0f, 14.5f), 0f, 7);
        }

        /// <summary>A building site behind fences: a half-built concrete frame, scaffolding, containers and materials.</summary>
        static void BuildingSite(Transform b, System.Random rng)
        {
            var dirt = Mats.Surface("asphalt", new Color(1.2f, 1.05f, 0.85f), 0f, 0.4f, 1.4f);
            Box(b, "Dirt", new Vector3(0f, 0.01f, 0f), new Vector3(32f, 0.02f, 32f), dirt, false);
            var conc = Mats.Surface("concrete", new Color(0.78f, 0.77f, 0.74f), 0f, 0.6f);
            // The frame: columns on a 5 m grid, three floor slabs, the top floor unfinished.
            for (int floor = 0; floor < 3; floor++)
            {
                float y = floor * 3.4f;
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 3; j++)
                        if (floor < 2 || (i + j) % 2 == 0)
                            Box(b, "Column", new Vector3(-12f + i * 5f, y + 1.7f, 2f + j * 5f), new Vector3(0.4f, 3.4f, 0.4f), conc, true);
                if (floor < 2) Box(b, "Slab", new Vector3(-4.5f, y + 3.5f, 7f), new Vector3(16f, 0.25f, 11f), conc, true);
            }
            Spawn("scaffold", b, new Vector3(-4.5f, 0f, 1.3f), 180f);
            Spawn("container", b, new Vector3(10f, 0f, 9f), 0f, rng.Next(4));
            Spawn("container", b, new Vector3(10f, 0f, 1.5f), 0f, rng.Next(4));
            Spawn("generator", b, new Vector3(6f, 0f, -6f), 0f);
            Spawn("watertank", b, new Vector3(12f, 0f, -9f), 0f);
            Spawn("boxtruck", b, new Vector3(-9f, 0f, -10f), 90f);
            for (int i = 0; i < 3; i++) Spawn("pallets", b, new Vector3(0f + i * 1.6f, 0f, -11f), rng.Next(30));
            for (int i = 0; i < 4; i++) Spawn("crate", b, new Vector3(5f + (i % 2) * 1f, i / 2 * 0.9f, -11.5f), 0f);
            for (int i = 0; i < 4; i++) Spawn("barrel", b, new Vector3(-14f + i * 0.8f, 0f, -4f), 0f, i);
            for (int i = 0; i < 3; i++) Spawn("gascyl", b, new Vector3(-14f + i * 0.6f, 0f, -6f), 0f);
            for (int i = 0; i < 6; i++) Spawn("cone", b, new Vector3(-12f + i * 2.5f, 0f, -14.6f), 0f);
            // Site fence along the two streets that see it, with a gap for the gate.
            for (int k = 0; k < 10; k++)
            {
                float o = -14.5f + k * 3f;
                if (k != 4) Spawn("fence", b, new Vector3(o + 1.5f, 0f, 15.8f), 90f);
                if (k != 6) Spawn("fence", b, new Vector3(15.8f, 0f, o + 1.5f), 0f);
            }
        }

        /// <summary>Brick terraces back to back, their gardens in between with sheds, grills and gnomes.</summary>
        static void Terraces(Transform b, System.Random rng)
        {
            var oak = Streets.ForLevel(11);
            TerraceRow(b.parent, b.position + new Vector3(0f, 0f, -14f), 0f, oak, 6, 1);
            TerraceRow(b.parent, b.position + new Vector3(0f, 0f, 14f), 180f, oak, 6, 4);
            var wood = Mats.Surface("wood", new Color(0.72f, 0.62f, 0.5f), 0f, 0.6f);
            for (int i = 0; i <= 6; i++) Box(b, "GardenFence", new Vector3(-15.6f + i * 5.2f, 0.9f, 0f), new Vector3(0.06f, 1.8f, 12f), wood, true);
            Box(b, "BackFence", new Vector3(0f, 0.9f, 0f), new Vector3(31.2f, 1.8f, 0.06f), wood, true);
            string[] yard = { "grill", "gnome", "shed", "flamingo", "lawnchair", "gascyl", "bicycle", "grill", "gnome", "lawnchair" };
            for (int i = 0; i < 6; i++)
            {
                float x = -13f + i * 5.2f;
                Spawn(yard[(i * 2) % yard.Length], b, new Vector3(x, 0f, -3f), rng.Next(360), rng.Next(3));
                Spawn(yard[(i * 2 + 1) % yard.Length], b, new Vector3(x + 1.2f, 0f, 3.5f), rng.Next(360), rng.Next(3));
                Spawn("bin", b, new Vector3(x + 1.8f, 0f, -16.2f), 0f, rng.Next(3));
                if (i % 2 == 0) Spawn("mailbox", b, new Vector3(x - 0.8f, 0f, 16.4f), 180f);
            }
        }

        /// <summary>A tall block of flats over a little car park, and a billboard facing the plaza road.</summary>
        static void ApartmentTower(Transform b, System.Random rng)
        {
            Flats(b, new Vector3(4f, 0f, 5f), new Vector3(20f, 36f, 18f), FacadePaint[rng.Next(FacadePaint.Length)], rng.Next());
            Box(b, "Entrance", new Vector3(4f, 1.4f, -4.4f), new Vector3(4f, 2.8f, 1f), Mats.Glass(new Color(0.12f, 0.14f, 0.15f, 0.8f)), false);
            Spawn("billboard", b, new Vector3(-12f, 0f, -14f), 0f, 0);
            for (int i = 0; i < 3; i++) Spawn(i == 1 ? "van" : "sedan", b, new Vector3(-12f, 0f, -2f + i * 3.2f), 90f, rng.Next(4));
            Spawn("bench", b, new Vector3(4f, 0f, -12f), 180f);
            Spawn("vending", b, new Vector3(9f, 0f, -12f), 180f, 0);
            Spawn("watertank", b, new Vector3(-12f, 0f, 12f), 0f);
        }

        /// <summary>The middle of town: a fountain in a paved square, benches, food carts, a fireworks stall.</summary>
        static void Plaza(Transform b, System.Random rng, QualityTier tier)
        {
            var stone = Mats.Surface("concrete", new Color(0.88f, 0.85f, 0.8f), 0f, 0.7f);
            Box(b, "Square", new Vector3(0f, 0.01f, 0f), new Vector3(30f, 0.02f, 30f), Mats.Surface("brick", new Color(1.1f, 1f, 0.95f), 0f, 0.8f, 0.6f), false);
            var basin = Props.Part(b, MeshKit.Lathe("basin", new[] { new Vector2(4.2f, 0f), new Vector2(4.2f, 0.6f), new Vector2(3.9f, 0.62f), new Vector2(3.9f, 0.2f), new Vector2(0f, 0.2f) }, 40), Vector3.zero, stone, false);
            var col = basin.AddComponent<CapsuleCollider>();
            col.radius = 4.2f; col.height = 8.4f; col.center = new Vector3(0f, -3.6f, 0f);
            Props.Part(b, MeshKit.RoundedBox(new Vector3(7.8f, 0.02f, 7.8f), 3.89f, 6), new Vector3(0f, 0.45f, 0f), Mats.Glass(new Color(0.2f, 0.32f, 0.36f, 0.75f), 0.98f), false);
            Props.Part(b, MeshKit.Lathe("fountain", new[] { new Vector2(0.6f, 0f), new Vector2(0.35f, 0.3f), new Vector2(0.3f, 1.8f), new Vector2(1.4f, 2f), new Vector2(1.4f, 2.15f), new Vector2(0.2f, 2.2f), new Vector2(0.15f, 3.2f), new Vector2(0f, 3.25f) }, 24), Vector3.zero, stone, true);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f + 22.5f;
                var p = Quaternion.Euler(0f, ang, 0f) * new Vector3(0f, 0f, 7f);
                Spawn("bench", b, p, ang + 180f);
            }
            foreach (var c in new[] { new Vector3(-10f, 0f, -10f), new Vector3(10f, 0f, 10f), new Vector3(-10f, 0f, 10f), new Vector3(10f, 0f, -10f) }) StreetTree(b, c, 1.1f, (int)(c.x + c.z * 3f) + 40);
            Spawn("foodcart", b, new Vector3(12f, 0f, -4f), 270f);
            Spawn("foodcart", b, new Vector3(-12f, 0f, 4f), 90f);
            Spawn("fireworkstand", b, new Vector3(12f, 0f, 5f), 270f);
            for (int i = 0; i < 4; i++) Spawn("cafetable", b, new Vector3(-13f + (i % 2) * 2.6f, 0f, -6f + (i / 2) * 2.6f), rng.Next(360), i);
            Spawn("newsbox", b, new Vector3(-4f, 0f, -14.5f), 180f, 0);
            Spawn("newsbox", b, new Vector3(-3.3f, 0f, -14.5f), 180f, 1);
            Spawn("vending", b, new Vector3(4f, 0f, -14.6f), 0f, 0);
            Spawn("gnome", b, new Vector3(1.2f, 0.62f, 3.6f), 200f, 1);   // someone left a garden gnome on the fountain
            // A bus shelter on the plaza road, its glass in three panes.
            BusShelter(b, new Vector3(-12.5f, 0f, -16.5f));
        }

        static void BusShelter(Transform b, Vector3 local)
        {
            var s = Group(b, "BusShelter", b.TransformPoint(local), 0f);
            var frame = Mats.Lit(new Color(0.2f, 0.21f, 0.22f), 0.7f, 0.5f);
            foreach (var x in new[] { -2f, 0f, 2f }) Box(s, "Post", new Vector3(x, 1.25f, 0.6f), new Vector3(0.08f, 2.5f, 0.08f), frame, true);
            Box(s, "Roof", new Vector3(0f, 2.55f, 0.1f), new Vector3(4.4f, 0.1f, 1.6f), frame, false);
            Box(s, "Seat", new Vector3(0f, 0.5f, 0.45f), new Vector3(3.6f, 0.06f, 0.35f), frame, true);
            for (int k = 0; k < 3; k++)
            {
                var pane = new GameObject("Pane").transform;
                pane.SetParent(s, false);
                bool back = k < 2;
                pane.localPosition = back ? new Vector3(-1f + k * 2f, 1.3f, 0.62f) : new Vector3(2.05f, 1.3f, 0.1f);
                pane.localRotation = Quaternion.Euler(0f, back ? 0f : 90f, 0f);
                Props.Part(pane, MeshKit.RoundedBox(new Vector3(back ? 1.92f : 1f, 2f, 0.015f), 0.005f, 1), Vector3.zero, Mats.Glass(Mats.GlassTint), true);
                var d = Destructible.Add(pane.gameObject, "busstop");
                d.FirePoint = Vector3.zero;
                d.FireSize = 0.4f;
            }
            Box(s, "Timetable", new Vector3(-2f, 1.4f, 0.55f), new Vector3(0.6f, 0.9f, 0.04f), Mats.Emissive(new Color(0.9f, 0.92f, 0.95f), new Color(0.8f, 0.85f, 0.9f)), false);
        }

        /// <summary>
        /// Towers on the blocks outside the last roads close the horizon (scenery only): three a side between the
        /// roads, one on each corner, and a far row where the streets run out of town.
        /// </summary>
        static void Skyline(Transform root, System.Random rng)
        {
            var spots = new List<Vector4>();   // x, z, width along the road, depth
            foreach (var along in new[] { -48f, 0f, 48f })
            {
                float w = 30f + (float)rng.NextDouble() * 6f, d = 16f + (float)rng.NextDouble() * 8f;
                spots.Add(new Vector4(along, 92f, w, d));
                spots.Add(new Vector4(along, -92f, w, d));
                spots.Add(new Vector4(92f, along, d, w));
                spots.Add(new Vector4(-92f, along, d, w));
            }
            foreach (var cx in new[] { -92f, 92f }) foreach (var cz in new[] { -92f, 92f }) spots.Add(new Vector4(cx, cz, 22f, 22f));
            foreach (var road in Roads)
            {
                spots.Add(new Vector4(road, 116f, 22f, 8f));
                spots.Add(new Vector4(road, -116f, 22f, 8f));
                spots.Add(new Vector4(116f, road, 8f, 22f));
                spots.Add(new Vector4(-116f, road, 8f, 22f));
            }
            var steel = Mats.Lit(new Color(0.5f, 0.52f, 0.54f), 0.8f, 0.6f);
            foreach (var sp in spots)
            {
                float h = 20f + (float)rng.NextDouble() * 45f;
                var size = new Vector3(sp.z, h, sp.w);
                var g = Group(root, "Skyline", new Vector3(sp.x, Kerb, sp.y), 0f);
                if (rng.NextDouble() < 0.4)
                {
                    Box(g, "Tower", new Vector3(0f, h * 0.5f, 0f), size, Mats.Surface("curtain", Color.white, 0.2f, 1f), true);
                    Box(g, "Crown", new Vector3(0f, h + 0.6f, 0f), new Vector3(size.x - 2f, 1.2f, size.z - 2f), steel, false);
                }
                else Flats(g, Vector3.zero, size, FacadePaint[rng.Next(FacadePaint.Length)], rng.Next());
            }
        }
    }

    /// <summary>
    /// The city is big: street-level things far from the player are not drawn (their renderers are forced off,
    /// so breaking glass and burning still work as usual).
    /// </summary>
    public sealed class CityCull : MonoBehaviour
    {
        public const float Distance = 75f;
        sealed class Item { public Transform T; public Renderer[] R; public bool Off; }
        readonly List<Item> items = new List<Item>();
        float next;

        void Start()
        {
            foreach (var d in GetComponentsInChildren<Destructible>())
            {
                // Big things stay: the billboards and the trucks read from far away.
                if (d.Def != null && (d.Def.Id == "billboard" || d.Def.Id == "fueltruck")) continue;
                items.Add(new Item { T = d.transform, R = d.GetComponentsInChildren<Renderer>() });
            }
        }

        void Update()
        {
            if (Time.unscaledTime < next || Player.I == null) return;
            next = Time.unscaledTime + 0.3f;
            var eye = Player.I.transform.position;
            float d2 = Distance * Distance;
            foreach (var it in items)
            {
                if (it.T == null) continue;
                bool off = (it.T.position - eye).sqrMagnitude > d2;
                if (off == it.Off) continue;
                it.Off = off;
                foreach (var r in it.R) if (r != null) r.forceRenderingOff = off;
            }
        }
    }
}
