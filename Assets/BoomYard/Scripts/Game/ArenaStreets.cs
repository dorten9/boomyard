using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// The houses and dressing of each street (Core/Streets). The stage is the same everywhere: the lot is 26 m
    /// wide (x −13..13), the house front stands at z = 14, the pavement and road are behind the player's start at
    /// z = −8, and the neighbours stand beside the lot and across the road (rotated 180°).
    /// </summary>
    public static partial class ArenaBuilder
    {
        static Color Rgb(float[] c) { return new Color(c[0], c[1], c[2]); }
        static Color Wall(StreetDef s, int i) { return Rgb(s.Walls[i % s.Walls.Length]); }

        static void Houses(Transform root, StreetDef s)
        {
            switch (s.Houses)
            {
                case HouseKind.Terrace:
                    TerraceRow(root, new Vector3(0f, 0f, 14f), 0f, s, 15, 0);
                    TerraceRow(root, new Vector3(0f, 0f, -34f), 180f, s, 15, 2);
                    break;
                case HouseKind.Mobile:
                    Trailer(root, new Vector3(0f, 0f, 15.5f), 0f, Wall(s, 0), s, true);
                    Trailer(root, new Vector3(-24f, 0f, 16f), 0f, Wall(s, 1), s, false);
                    Trailer(root, new Vector3(23f, 0f, 15f), 0f, Wall(s, 2), s, false);
                    Trailer(root, new Vector3(-13f, 0f, -33f), 180f, Wall(s, 3), s, false);
                    Trailer(root, new Vector3(12f, 0f, -34f), 180f, Wall(s, 1), s, false);
                    break;
                case HouseKind.Shops:
                    ShopRow(root, new Vector3(0f, 0f, 14f), 0f, s, 6, 0);
                    ShopRow(root, new Vector3(0f, 0f, -33f), 180f, s, 6, 3);
                    break;
                case HouseKind.Modern:
                    Modern(root, new Vector3(0f, 0f, 14f), 0f, Wall(s, 0), s, true);
                    Modern(root, new Vector3(-30f, 0f, 17f), 0f, Wall(s, 1), s, false);
                    Modern(root, new Vector3(30f, 0f, 16f), 0f, Wall(s, 2), s, false);
                    Modern(root, new Vector3(-16f, 0f, -38f), 180f, Wall(s, 2), s, false);
                    Modern(root, new Vector3(16f, 0f, -39f), 180f, Wall(s, 0), s, false);
                    break;
                case HouseKind.Bungalow:
                    Bungalow(root, new Vector3(0f, 0f, 14f), 0f, Wall(s, 0), s, true);
                    Bungalow(root, new Vector3(-26f, 0f, 15f), 0f, Wall(s, 1), s, false);
                    Bungalow(root, new Vector3(25f, 0f, 15.5f), 0f, Wall(s, 2), s, false);
                    Bungalow(root, new Vector3(-13f, 0f, -34f), 180f, Wall(s, 3), s, false);
                    Bungalow(root, new Vector3(13f, 0f, -35f), 180f, Wall(s, 1), s, false);
                    break;
                default:
                    House(root, new Vector3(0f, 0f, 14f), 0f, Wall(s, 0), true, s);
                    House(root, new Vector3(-27f, 0f, 16f), 0f, Wall(s, 1), false, s);
                    House(root, new Vector3(26f, 0f, 15f), 0f, Wall(s, 2), false, s);
                    House(root, new Vector3(-14f, 0f, -36f), 180f, Wall(s, 3), false, s);
                    House(root, new Vector3(14f, 0f, -37f), 180f, Wall(s, 4), false, s);
                    break;
            }
        }

        static Transform Group(Transform parent, string name, Vector3 pos, float yaw)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            g.position = pos;
            g.rotation = Quaternion.Euler(0f, yaw, 0f);
            return g;
        }

        // ---------- Oak Row: brick terraces ----------

        static readonly Color[] DoorPaint = { new Color(0.12f, 0.2f, 0.36f), new Color(0.5f, 0.08f, 0.08f), new Color(0.1f, 0.3f, 0.2f), new Color(0.12f, 0.12f, 0.13f), new Color(0.75f, 0.6f, 0.2f) };

        /// <summary>A row of two-storey brick terraced houses, 5.2 m each, sharing one slate roof.</summary>
        static void TerraceRow(Transform parent, Vector3 pos, float yaw, StreetDef s, int count, int seed)
        {
            var row = Group(parent, "Terrace", pos, yaw);
            const float unit = 5.2f, depth = 8f, wall = 6f;
            float width = unit * count;
            var trim = Mats.Surface("paint", Rgb(s.Trim), 0f, 0.6f);
            var roof = Mats.Surface("roof", Rgb(s.Roof), 0f, 1f, 1.6f);
            var stone = Mats.Surface("concrete", new Color(0.86f, 0.84f, 0.78f), 0f, 0.6f);
            var roofGo = Props.Part(row, MeshKit.Gable(new Vector3(depth, 2.6f, width), 0.4f), new Vector3(0f, wall, depth * 0.5f), roof, false);
            roofGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            for (int i = 0; i < count; i++)
            {
                float x = -width * 0.5f + unit * (i + 0.5f);
                var brick = Mats.Surface("brick", Color.Lerp(Color.white, Wall(s, i + seed) * 1.6f, 0.45f), 0f, 1f);
                Box(row, "Unit", new Vector3(x, wall * 0.5f, depth * 0.5f), new Vector3(unit - 0.02f, wall, depth), brick, true);
                Box(row, "Sill", new Vector3(x, 3.05f, -0.05f), new Vector3(unit, 0.14f, 0.1f), stone, false);
                // Door and fanlight on one side, a bay window downstairs and two sash windows upstairs.
                bool left = (i + seed) % 2 == 0;
                float dx = x + (left ? -1.6f : 1.6f), wx = x + (left ? 0.8f : -0.8f);
                Box(row, "Door", new Vector3(dx, 1.08f, -0.02f), new Vector3(0.95f, 2.16f, 0.08f), Mats.Surface("paint", DoorPaint[(i + seed) % DoorPaint.Length], 0.1f, 0.7f), false);
                Box(row, "Fanlight", new Vector3(dx, 2.4f, -0.03f), new Vector3(0.95f, 0.3f, 0.04f), Mats.Emissive(new Color(0.35f, 0.25f, 0.16f), new Color(0.7f, 0.45f, 0.2f)), false);
                Box(row, "Step", new Vector3(dx, 0.1f, -0.4f), new Vector3(1.3f, 0.2f, 0.6f), stone, true);
                Box(row, "Bay", new Vector3(wx, 1.45f, -0.4f), new Vector3(2.4f, 2.1f, 0.8f), Mats.Surface("paint", Rgb(s.Trim), 0f, 0.6f), true);
                Window(row, new Vector3(wx, 1.55f, -0.8f), new Vector2(1.8f, 1.4f), (i % 3) == 0, trim);
                Window(row, new Vector3(x - 1.1f, 4.4f, 0f), new Vector2(1f, 1.4f), (i % 4) == 1, trim);
                Window(row, new Vector3(x + 1.1f, 4.4f, 0f), new Vector2(1f, 1.4f), false, trim);
                if (i % 2 == 0) Box(row, "Chimney", new Vector3(x + unit * 0.5f, wall + 2.2f, depth * 0.55f), new Vector3(0.7f, 2.4f, 1.1f), brick, false);
            }
        }

        // ---------- Sunset Park: mobile homes ----------

        /// <summary>A single-wide mobile home, 16 × 4.3 m on a skirt, ribbed siding, windows, door, steps and an awning.</summary>
        static void Trailer(Transform parent, Vector3 pos, float yaw, Color body, StreetDef s, bool main)
        {
            var t = Group(parent, main ? "Trailer" : "NeighbourTrailer", pos, yaw);
            const float len = 16f, wide = 4.3f, high = 2.7f, lift = 0.65f;
            var siding = Mats.Surface("paint", body, 0.25f, 0.55f);
            var stripe = Mats.Surface("paint", Rgb(s.Trim), 0.2f, 0.55f);
            var skirt = Mats.Surface("paint", new Color(0.62f, 0.6f, 0.56f), 0f, 0.4f);
            var trim = Mats.Surface("paint", new Color(0.92f, 0.92f, 0.9f), 0f, 0.6f);
            Box(t, "Body", new Vector3(0f, lift + high * 0.5f, wide * 0.5f), new Vector3(len, high, wide), siding, true);
            Box(t, "Roof", new Vector3(0f, lift + high + 0.08f, wide * 0.5f), new Vector3(len + 0.2f, 0.16f, wide + 0.25f), Mats.Lit(new Color(0.78f, 0.78f, 0.76f), 0.6f, 0.4f), false);
            Box(t, "Skirt", new Vector3(0f, lift * 0.5f, wide * 0.5f + 0.1f), new Vector3(len - 0.3f, lift, wide - 0.2f), skirt, true);
            Box(t, "Stripe", new Vector3(0f, lift + 1.95f, -0.02f), new Vector3(len, 0.22f, 0.04f), stripe, false);
            for (int i = 0; i < 9; i++) Box(t, "Rib", new Vector3(0f, lift + 0.2f + i * 0.28f, -0.015f), new Vector3(len, 0.025f, 0.02f), siding, false);
            // Door with steps and an aluminium awning; windows along the front.
            Box(t, "Door", new Vector3(-3f, lift + 1f, -0.03f), new Vector3(0.85f, 1.95f, 0.06f), trim, false);
            Box(t, "Steps", new Vector3(-3f, lift * 0.5f, -0.6f), new Vector3(1.2f, lift, 0.9f), Mats.Surface("wood", new Color(0.7f, 0.6f, 0.5f), 0f, 0.5f), true);
            Box(t, "Awning", new Vector3(-1f, lift + high + 0.05f, -1.2f), new Vector3(7f, 0.06f, 2.4f), Mats.Lit(new Color(0.86f, 0.86f, 0.84f), 0.5f, 0.45f), false);
            for (int k = -1; k <= 1; k += 2) Props.Tube(t, new Vector3(-1f + k * 3.3f, 0f, -2.3f), new Vector3(-1f + k * 3.3f, lift + high, -2.3f), 0.04f, trim);
            foreach (var x in new[] { -6.2f, -0.6f, 2.4f, 5.6f })
                Window(t, new Vector3(x, lift + 1.45f, 0f), new Vector2(1.3f, 0.95f), x > 2f && main, trim);
        }

        // ---------- Main Street: shops ----------

        static readonly Color[] Awnings = { new Color(0.62f, 0.12f, 0.1f), new Color(0.12f, 0.32f, 0.22f), new Color(0.15f, 0.25f, 0.45f), new Color(0.75f, 0.55f, 0.15f), new Color(0.4f, 0.16f, 0.32f) };

        /// <summary>
        /// Two-storey shops 6.5 m wide: a storefront of big glass panes (they break and pay) under a striped
        /// awning and a sign board, flats above, a parapet on the flat roof.
        /// </summary>
        static void ShopRow(Transform parent, Vector3 pos, float yaw, StreetDef s, int count, int seed)
        {
            var row = Group(parent, "Shops", pos, yaw);
            const float unit = 6.5f, depth = 10f, wall = 7f;
            float width = unit * count;
            var trim = Mats.Surface("paint", Rgb(s.Trim), 0f, 0.6f);
            for (int i = 0; i < count; i++)
            {
                float x = -width * 0.5f + unit * (i + 0.5f);
                var walls = Mats.Surface((i + seed) % 3 == 0 ? "brick" : "plaster", Wall(s, i + seed), 0f, 1f);
                Box(row, "Shop", new Vector3(x, wall * 0.5f, depth * 0.5f), new Vector3(unit - 0.04f, wall, depth), walls, true);
                Box(row, "Parapet", new Vector3(x, wall + 0.35f, 0.15f), new Vector3(unit, 0.7f, 0.3f), walls, false);
                Box(row, "Cornice", new Vector3(x, wall + 0.05f, -0.08f), new Vector3(unit, 0.18f, 0.18f), trim, false);
                // Storefront: stall riser, two panes either side of a door, a sign above.
                var awning = Awnings[(i + seed) % Awnings.Length];
                Box(row, "Riser", new Vector3(x, 0.3f, -0.05f), new Vector3(unit - 0.4f, 0.6f, 0.1f), Mats.Surface("paint", awning * 0.6f, 0.1f, 0.6f), false);
                Box(row, "ShopDoor", new Vector3(x, 1.2f, -0.03f), new Vector3(1.1f, 2.4f, 0.06f), Mats.Glass(new Color(0.1f, 0.12f, 0.12f, 0.8f)), false);
                Window(row, new Vector3(x - 1.85f, 1.6f, 0f), new Vector2(2.2f, 2f), true, trim, "storefront");
                Window(row, new Vector3(x + 1.85f, 1.6f, 0f), new Vector2(2.2f, 2f), (i + seed) % 2 == 0, trim, "storefront");
                Box(row, "Sign", new Vector3(x, 3.15f, -0.1f), new Vector3(unit - 0.6f, 0.7f, 0.12f), Mats.Surface("paint", awning, 0.1f, 0.7f), false);
                Box(row, "SignText", new Vector3(x, 3.15f, -0.17f), new Vector3(unit * 0.55f, 0.24f, 0.02f), Mats.Emissive(new Color(0.95f, 0.92f, 0.85f), new Color(0.6f, 0.55f, 0.45f)), false);
                Awning(row, new Vector3(x, 2.75f, -0.1f), unit - 0.5f, awning);
                Window(row, new Vector3(x - 1.6f, 5.2f, 0f), new Vector2(1.1f, 1.4f), (i % 3) == 1, trim);
                Window(row, new Vector3(x + 1.6f, 5.2f, 0f), new Vector2(1.1f, 1.4f), false, trim);
            }
        }

        /// <summary>A sloped awning in stripes of the shop's colour and off-white.</summary>
        public static void Awning(Transform parent, Vector3 at, float width, Color c)
        {
            var a = new GameObject("Awning").transform;
            a.SetParent(parent, false);
            a.localPosition = at;
            a.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            int stripes = Mathf.Max(4, Mathf.RoundToInt(width / 0.5f));
            var light = Mats.Lit(new Color(0.93f, 0.9f, 0.82f), 0f, 0.2f);
            var dark = Mats.Lit(c, 0f, 0.2f);
            for (int k = 0; k < stripes; k++)
                Box(a, "Stripe", new Vector3(-width * 0.5f + width * (k + 0.5f) / stripes, 0f, -0.7f), new Vector3(width / stripes, 0.04f, 1.4f), k % 2 == 0 ? dark : light, false);
            Box(a, "Valance", new Vector3(0f, -0.12f, -1.4f), new Vector3(width, 0.24f, 0.03f), dark, false);
        }

        /// <summary>The shops' forecourt: painted parking bays.</summary>
        static void ParkingLines(Transform root)
        {
            var paint = Mats.Lit(new Color(0.92f, 0.92f, 0.88f), 0f, 0.4f);
            for (int i = -4; i <= 4; i++) Flat(root, "Bay", new Vector3(i * 2.8f, 0.018f, 2f), new Vector2(0.12f, 5f), paint);
            Flat(root, "BayEnd", new Vector3(0f, 0.018f, 4.5f), new Vector2(22.6f, 0.12f), paint);
            Flat(root, "ShopPavement", new Vector3(0f, 0.03f, 11.8f), new Vector2(40f, 4.4f), Mats.Surface("concrete", new Color(0.95f, 0.93f, 0.9f), 0f, 1f));
        }

        // ---------- Hillcrest: modern houses ----------

        /// <summary>Two offset white boxes with flat roofs, a timber-clad wing, floor-to-ceiling glass and a double garage.</summary>
        static void Modern(Transform parent, Vector3 pos, float yaw, Color render, StreetDef s, bool main)
        {
            var h = Group(parent, main ? "Villa" : "NeighbourVilla", pos, yaw);
            var walls = Mats.Surface("plaster", render, 0f, 1f, 1.4f);
            var cladding = Mats.Surface("wood", new Color(0.62f, 0.45f, 0.32f), 0f, 0.6f);
            var dark = Mats.Surface("paint", Rgb(s.Trim), 0.4f, 0.5f);
            Box(h, "Ground", new Vector3(-1f, 1.6f, 5f), new Vector3(18f, 3.2f, 10f), walls, true);
            Box(h, "Upper", new Vector3(2f, 4.8f, 5.8f), new Vector3(13f, 3.2f, 8.4f), walls, true);
            Box(h, "Wing", new Vector3(-7.5f, 4.6f, 6f), new Vector3(5f, 2.8f, 8f), cladding, true);
            Box(h, "RoofGround", new Vector3(-1f, 3.28f, 4.8f), new Vector3(18.4f, 0.16f, 10.6f), dark, false);
            Box(h, "RoofUpper", new Vector3(2f, 6.48f, 5.6f), new Vector3(13.4f, 0.16f, 9f), dark, false);
            Box(h, "RoofWing", new Vector3(-7.5f, 6.08f, 5.8f), new Vector3(5.3f, 0.16f, 8.4f), dark, false);
            // Glass walls below, a ribbon window above, a pivot door and a double garage.
            foreach (var x in new[] { -6.5f, -4.1f, -1.7f })
                Window(h, new Vector3(x, 1.5f, 0f), new Vector2(2.2f, 2.6f), x > -5f, dark);
            Window(h, new Vector3(1.5f, 4.8f, 1.6f - 0.02f), new Vector2(6.5f, 1.4f), main, dark);
            Window(h, new Vector3(6.2f, 4.8f, 1.6f - 0.02f), new Vector2(2.2f, 1.4f), false, dark);
            Box(h, "Door", new Vector3(0.6f, 1.25f, -0.03f), new Vector3(1.3f, 2.5f, 0.08f), cladding, false);
            Box(h, "Garage", new Vector3(5.2f, 1.25f, -0.03f), new Vector3(5f, 2.5f, 0.08f), Mats.Surface("paint", new Color(0.3f, 0.3f, 0.32f), 0.5f, 0.5f), false);
            Box(h, "DownLight", new Vector3(0.6f, 3.0f, -0.25f), new Vector3(0.3f, 0.05f, 0.3f), Mats.Emissive(new Color(1f, 0.9f, 0.7f), new Color(2f, 1.5f, 0.9f)), false);
        }

        // ---------- Holly Lane: bungalows ----------

        /// <summary>A single-storey bungalow with a low gable roof, a porch on posts and a picture window.</summary>
        static void Bungalow(Transform parent, Vector3 pos, float yaw, Color siding, StreetDef s, bool main)
        {
            var h = Group(parent, main ? "Bungalow" : "NeighbourBungalow", pos, yaw);
            const float w = 16f, depth = 10f, wall = 3.4f;
            var walls = Mats.Surface("paint", siding, 0f, 0.5f, 1f, 0.6f);
            var trim = Mats.Surface("paint", Rgb(s.Trim), 0f, 0.6f);
            var roof = Mats.Surface("roof", Rgb(s.Roof), 0f, 1f);
            var brick = Mats.Surface("brick", Color.white, 0f, 1f);
            Box(h, "Walls", new Vector3(0f, wall * 0.5f, depth * 0.5f), new Vector3(w, wall, depth), walls, true);
            for (int i = 0; i < 12; i++) Box(h, "Board", new Vector3(0f, 0.15f + i * 0.27f, -0.015f), new Vector3(w, 0.02f, 0.02f), walls, false);
            Box(h, "Plinth", new Vector3(0f, 0.3f, -0.04f), new Vector3(w + 0.1f, 0.6f, 0.12f), brick, false);
            var roofGo = Props.Part(h, MeshKit.Gable(new Vector3(depth + 1.4f, 2.4f, w + 1.2f), 0.6f), new Vector3(0f, wall, depth * 0.5f), roof, false);
            roofGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Box(h, "Fascia", new Vector3(0f, wall - 0.05f, -0.72f), new Vector3(w + 1.2f, 0.2f, 0.06f), trim, false);
            // Porch: a deck, two posts and a small gable over the door.
            Box(h, "Deck", new Vector3(-3.5f, 0.15f, -1.1f), new Vector3(4.2f, 0.3f, 2.2f), Mats.Surface("wood", new Color(0.7f, 0.55f, 0.42f), 0f, 0.5f), true);
            for (int k = -1; k <= 1; k += 2) Box(h, "Post", new Vector3(-3.5f + k * 1.9f, wall * 0.5f, -2.05f), new Vector3(0.16f, wall, 0.16f), trim, false);
            Box(h, "PorchRoof", new Vector3(-3.5f, wall + 0.05f, -1.1f), new Vector3(4.5f, 0.12f, 2.4f), trim, false);
            Box(h, "Door", new Vector3(-3.5f, 1.05f, -0.03f), new Vector3(0.95f, 2.1f, 0.08f), Mats.Surface("paint", DoorPaint[(int)(Mathf.Abs(pos.x) + 1) % DoorPaint.Length], 0.1f, 0.7f), false);
            Box(h, "PorchLamp", new Vector3(-2.7f, 2.1f, -0.1f), new Vector3(0.14f, 0.24f, 0.14f), Mats.Emissive(new Color(1f, 0.85f, 0.6f), new Color(2.2f, 1.5f, 0.8f)), false);
            Window(h, new Vector3(1.8f, 1.6f, 0f), new Vector2(3f, 1.5f), true, trim);
            Window(h, new Vector3(5.6f, 1.6f, 0f), new Vector2(1.3f, 1.3f), false, trim);
            Window(h, new Vector3(-6.6f, 1.6f, 0f), new Vector2(1.3f, 1.3f), main, trim);
        }

        // ---------- fences ----------

        static void Fences(Transform root, StreetDef s)
        {
            switch (s.Fence)
            {
                case FenceKind.Timber: TimberFences(root); break;
                case FenceKind.LowWall: LowWalls(root); break;
                case FenceKind.ChainLink: ChainLinkLines(root); break;
                case FenceKind.Hedge: Hedges(root); break;
                case FenceKind.Picket: Pickets(root); break;
            }
        }

        /// <summary>Low brick walls with stone copings on the lot lines and along the pavement, with a gap for the path.</summary>
        static void LowWalls(Transform root)
        {
            var brick = Mats.Surface("brick", Color.white, 0f, 1f);
            var coping = Mats.Surface("concrete", new Color(0.86f, 0.84f, 0.78f), 0f, 0.6f);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(root, "SideWall", new Vector3(s * 13.3f, 0.45f, 2.5f), new Vector3(0.3f, 0.9f, 23f), brick, true);
                Box(root, "SideCoping", new Vector3(s * 13.3f, 0.93f, 2.5f), new Vector3(0.4f, 0.08f, 23f), coping, false);
                Box(root, "FrontWall", new Vector3(s * 8.4f, 0.4f, -9.4f), new Vector3(9.8f, 0.8f, 0.3f), brick, true);
                Box(root, "FrontCoping", new Vector3(s * 8.4f, 0.83f, -9.4f), new Vector3(9.9f, 0.08f, 0.4f), coping, false);
            }
        }

        static void ChainLinkLines(Transform root)
        {
            var steel = Mats.Lit(new Color(0.6f, 0.62f, 0.63f), 0.85f, 0.5f);
            var mesh = Mats.Mesh("chainlink", Color.white);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 8; i++)
                {
                    float z = -7.5f + i * 3f;
                    Props.Part(root, MeshKit.RoundedBox(new Vector3(0.01f, 1.3f, 3f), 0.004f, 1), new Vector3(s * 13.4f, 0.68f, z), mesh, true);
                    Props.Part(root, MeshKit.Lathe("linkpost", new[] { new Vector2(0.03f, 0f), new Vector2(0.03f, 1.38f), new Vector2(0f, 1.4f) }, 10), new Vector3(s * 13.4f, 0f, z - 1.5f), steel, false);
                }
        }

        /// <summary>Tall clipped hedges (2.2 m) on the lot lines, the rich street's walls.</summary>
        static void Hedges(Transform root)
        {
            var leaf = Mats.Lit(new Color(0.13f, 0.22f, 0.08f), 0f, 0.1f);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(root, "Hedge", new Vector3(s * 13.3f, 1.1f, 2.5f), new Vector3(1f, 2.2f, 23f), leaf, true);
                for (int i = 0; i < 7; i++) Props.Part(root, MeshKit.Blob(0.75f, 0.25f, 300 + i + (s > 0 ? 20 : 0)), new Vector3(s * 13.3f, 2.05f, -7.5f + i * 3.3f), leaf, false);
            }
        }

        /// <summary>White picket fences along the pavement and the lot lines, with a gate gap for the path.</summary>
        static void Pickets(Transform root)
        {
            var white = Mats.Surface("paint", new Color(0.95f, 0.95f, 0.93f), 0f, 0.55f);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(root, "Rail", new Vector3(s * 8.4f, 0.75f, -9.4f), new Vector3(9.8f, 0.07f, 0.04f), white, false);
                Box(root, "Rail", new Vector3(s * 8.4f, 0.3f, -9.4f), new Vector3(9.8f, 0.07f, 0.04f), white, true);
                for (int i = 0; i < 40; i++) Box(root, "Picket", new Vector3(s * (3.6f + i * 0.245f), 0.5f, -9.37f), new Vector3(0.08f, 1f, 0.02f), white, false);
                Box(root, "SideRail", new Vector3(s * 13.3f, 0.75f, 2.5f), new Vector3(0.04f, 0.07f, 23f), white, false);
                Box(root, "SideRail", new Vector3(s * 13.3f, 0.3f, 2.5f), new Vector3(0.04f, 0.07f, 23f), white, true);
                for (int i = 0; i < 70; i++) Box(root, "Picket", new Vector3(s * 13.33f, 0.5f, -8.9f + i * 0.33f), new Vector3(0.02f, 1f, 0.08f), white, false);
            }
        }

        // ---------- dressing ----------

        static readonly Color[] Bulbs = { new Color(3f, 0.4f, 0.3f), new Color(0.4f, 2.6f, 0.6f), new Color(3f, 2.2f, 0.5f), new Color(0.5f, 1f, 3f) };

        /// <summary>Strings of coloured bulbs along the eaves of the house and over the porch.</summary>
        static void HolidayLights(Transform root)
        {
            var wire = Mats.Lit(new Color(0.05f, 0.06f, 0.05f), 0f, 0.3f);
            var mats = new UnityEngine.Material[Bulbs.Length];
            for (int i = 0; i < Bulbs.Length; i++) mats[i] = Mats.Emissive(Bulbs[i] * 0.3f, Bulbs[i]);
            var bulb = MeshKit.RoundedBox(new Vector3(0.13f, 0.18f, 0.13f), 0.06f, 2);
            float y = 3.2f, z = 13.12f;
            // Along the eaves, sagging a little between hooks, and on the porch roof.
            for (int i = 0; i < 44; i++)
            {
                float x = -8.4f + i * 0.39f;
                float sag = 0.09f * Mathf.Abs(Mathf.Sin((x + 8.4f) / 1.56f * Mathf.PI));
                Props.Part(root, bulb, new Vector3(x, y - sag, z), mats[i % mats.Length], false);
            }
            Box(root, "Wire", new Vector3(0f, y + 0.02f, z), new Vector3(17f, 0.012f, 0.012f), wire, false);
            for (int i = 0; i < 12; i++)
                Props.Part(root, bulb, new Vector3(-5.6f + i * 0.38f, 3.36f, 11.7f), mats[(i + 1) % mats.Length], false);
            // Round the trunk and up into the crown of the tree by the fence.
            for (int i = 0; i < 18; i++)
            {
                float a = i * 0.9f;
                Props.Part(root, bulb, new Vector3(-11f + Mathf.Cos(a) * 0.32f, 0.6f + i * 0.17f, 9f + Mathf.Sin(a) * 0.32f), mats[(i + 2) % mats.Length], false);
            }
        }

        static readonly string[] KerbIds = { "sedan", "van", "pickup", "sedan" };

        /// <summary>Cars parked along the far kerb: not targets, but they break and pay like anything else.</summary>
        static void KerbCars(Transform root, int seed)
        {
            float[] xs = { -17f, -9.5f, 11f, 19f };
            for (int i = 0; i < xs.Length; i++)
            {
                if ((seed + i) % 3 == 0) continue;
                Props.Spawn(KerbIds[(seed + i) % KerbIds.Length], root, new Vector3(xs[i], 0f, -19.3f), i % 2 == 0 ? 270f : 90f, seed + i);
            }
        }

        /// <summary>The filling-station canopy over the pumps (Corner Gas): a flat roof on four posts, lit underneath.</summary>
        static void Canopy(Transform root)
        {
            var white = Mats.Surface("paint", new Color(0.92f, 0.92f, 0.9f), 0.3f, 0.6f);
            var red = Mats.Surface("paint", new Color(0.7f, 0.1f, 0.08f), 0.2f, 0.6f);
            foreach (var p in new[] { new Vector2(-7.5f, 3.5f), new Vector2(-7.5f, 9.5f), new Vector2(4.4f, 3.5f), new Vector2(4.4f, 9.5f) })
                Box(root, "CanopyPost", new Vector3(p.x, 2.3f, p.y), new Vector3(0.35f, 4.6f, 0.35f), white, true);
            Box(root, "Canopy", new Vector3(-1.55f, 4.75f, 6.5f), new Vector3(13f, 0.3f, 7.6f), white, false);
            Box(root, "CanopyBand", new Vector3(-1.55f, 4.75f, 2.68f), new Vector3(13f, 0.5f, 0.06f), red, false);
            for (int i = 0; i < 4; i++)
                Box(root, "CanopyLight", new Vector3(-6f + i * 3f, 4.58f, 6.5f), new Vector3(1.2f, 0.04f, 0.5f), Mats.Emissive(new Color(1f, 0.95f, 0.85f), new Color(2.4f, 2.2f, 1.9f)), false);
        }
    }
}
