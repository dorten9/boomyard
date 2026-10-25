using UnityEngine;
using Mat = UnityEngine.Material;

namespace BoomYard.Game
{
    /// <summary>
    /// The objects of the other streets (trailer park, shops, the rich street, the holidays, the filling station),
    /// modelled the same way as the first street's: bevelled boxes, lathe-turned parts, real dimensions.
    /// </summary>
    public static partial class Props
    {
        static Destructible Street(string id, Transform t, int variant)
        {
            switch (id)
            {
                case "pickup": return Pickup(t, variant);
                case "boxtruck": return BoxTruck(t);
                case "sportscar": return SportsCar(t, variant);
                case "golfcart": return GolfCart(t);
                case "scooter": return Scooter(t, variant);
                case "caravan": return Caravan(t);
                case "fueltruck": return FuelTruck(t);
                case "grill": return Grill(t);
                case "gnome": return Gnome(t, variant);
                case "flamingo": return Flamingo(t);
                case "hydrant": return Hydrant(t);
                case "newsbox": return NewsBox(t, variant);
                case "vending": return Vending(t, variant);
                case "bench": return Bench(t);
                case "cafetable": return CafeTable(t, variant);
                case "inflatable": return Inflatable(t, variant);
                case "giftbox": return GiftBox(t, variant);
                case "lawnchair": return LawnChair(t, variant);
                case "cone": return Cone(t);
                case "shed": return Shed(t);
                case "fortress": return Fortress(t);
                case "generator": return Generator(t);
                case "jacuzzi": return Jacuzzi(t);
                case "foodcart": return FoodCart(t);
                case "fireworkstand": return FireworkStand(t);
                case "fuelpump": return FuelPump(t);
            }
            return null;
        }

        static Destructible Finish(Transform t, string id, Vector3 fire, float fireSize, float rigidMass = 0f)
        {
            if (rigidMass > 0f) t.gameObject.AddComponent<Rigidbody>().mass = rigidMass;
            var d = Destructible.Add(t.gameObject, id);
            d.FirePoint = fire;
            d.FireSize = fireSize;
            return d;
        }

        static GameObject B(Transform t, Vector3 size, Vector3 at, Mat m, bool col = false, float r = -1f)
        {
            if (r < 0f) r = Mathf.Min(0.04f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.3f);
            return Part(t, MeshKit.RoundedBox(size, r, 2), at, m, col);
        }

        static Mat Paint(Color c, float metal = 0.3f, float smooth = 0.75f) { return Mats.Surface("paint", c, metal, smooth); }

        static readonly Color[] TruckPaint = { new Color(0.75f, 0.75f, 0.73f), new Color(0.15f, 0.25f, 0.4f), new Color(0.45f, 0.1f, 0.08f), new Color(0.2f, 0.28f, 0.2f) };

        // ---------- vehicles ----------

        /// <summary>Pickup truck, 5.3 × 1.9 m: cab, open bed with sides and tailgate, 4 wheels and 2 doors.</summary>
        static Destructible Pickup(Transform t, int variant)
        {
            var paint = Mats.Surface("paint", TruckPaint[variant % TruckPaint.Length], 0.4f, 0.8f);
            var trim = Mats.Lit(new Color(0.06f, 0.06f, 0.07f), 0.1f, 0.35f);
            var glass = Mats.Glass(new Color(0.06f, 0.08f, 0.1f, 0.8f));
            B(t, new Vector3(1.9f, 0.75f, 5.3f), new Vector3(0f, 0.8f, 0f), paint, true, 0.12f);
            B(t, new Vector3(1.8f, 0.8f, 1.9f), new Vector3(0f, 1.55f, 0.55f), paint, true, 0.14f);
            Part(t, MeshKit.RoundedBox(new Vector3(1.7f, 0.6f, 1.75f), 0.1f, 3, new Vector2(0.94f, 0.82f)), new Vector3(0f, 1.6f, 0.55f), glass, false);
            B(t, new Vector3(1.86f, 0.1f, 2.3f), new Vector3(0f, 1.18f, -1.45f), trim);   // bed floor
            for (int s = -1; s <= 1; s += 2) B(t, new Vector3(0.08f, 0.45f, 2.3f), new Vector3(s * 0.91f, 1.4f, -1.45f), paint, true);
            B(t, new Vector3(1.86f, 0.45f, 0.08f), new Vector3(0f, 1.4f, -2.6f), paint, true);
            B(t, new Vector3(1.96f, 0.26f, 0.22f), new Vector3(0f, 0.55f, 2.62f), trim);
            B(t, new Vector3(1.96f, 0.22f, 0.2f), new Vector3(0f, 0.55f, -2.66f), trim);
            for (int s = -1; s <= 1; s += 2)
            {
                B(t, new Vector3(0.36f, 0.16f, 0.05f), new Vector3(s * 0.65f, 1.0f, 2.66f), Mats.Emissive(new Color(0.9f, 0.9f, 0.85f), new Color(0.35f, 0.34f, 0.3f)));
                B(t, new Vector3(0.12f, 0.3f, 0.05f), new Vector3(s * 0.85f, 1.1f, -2.66f), Mats.Emissive(new Color(0.5f, 0.04f, 0.03f), new Color(0.3f, 0.01f, 0.01f)));
            }
            var d = Finish(t, "pickup", new Vector3(0f, 0.8f, 1.8f), 1.5f);
            Wheels(t, d, 0.4f, 0.26f, 0.88f, 1.7f, -1.7f);
            CarDoors(t, d, paint, glass, 0.93f, 1.3f, 0.55f);
            return d;
        }

        static void CarDoors(Transform t, Destructible d, Mat paint, Mat glass, float x, float y, float z)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                var door = new GameObject("Door").transform;
                door.SetParent(t, false);
                door.localPosition = new Vector3(s * x, y, z);
                Part(door, MeshKit.RoundedBox(new Vector3(0.05f, 0.9f, 1.0f), 0.02f), Vector3.zero, paint, true);
                Part(door, MeshKit.RoundedBox(new Vector3(0.02f, 0.4f, 0.8f), 0.01f), new Vector3(s * 0.02f, 0.3f, 0f), glass, false);
                d.Parts.Add(door);
            }
        }

        /// <summary>Removal truck, 7.5 m: a cab and a tall box body with a roller door.</summary>
        static Destructible BoxTruck(Transform t)
        {
            var cab = Paint(new Color(0.85f, 0.85f, 0.82f), 0.4f, 0.8f);
            var box = Paint(new Color(0.92f, 0.9f, 0.85f), 0.2f, 0.5f);
            var stripe = Paint(new Color(0.12f, 0.35f, 0.6f), 0.2f, 0.6f);
            var trim = Mats.Lit(new Color(0.06f, 0.06f, 0.07f), 0.1f, 0.35f);
            B(t, new Vector3(2.3f, 0.5f, 7.4f), new Vector3(0f, 0.75f, 0f), trim, true, 0.05f);   // chassis
            B(t, new Vector3(2.3f, 1.6f, 1.9f), new Vector3(0f, 1.75f, 2.75f), cab, true, 0.15f);
            Part(t, MeshKit.RoundedBox(new Vector3(2.2f, 0.75f, 0.1f), 0.03f), new Vector3(0f, 2.1f, 3.7f), Mats.Glass(new Color(0.1f, 0.12f, 0.14f, 0.7f)), false);
            B(t, new Vector3(2.45f, 2.6f, 5.2f), new Vector3(0f, 2.3f, -0.95f), box, true, 0.06f);
            B(t, new Vector3(2.47f, 0.4f, 5.22f), new Vector3(0f, 1.6f, -0.95f), stripe);
            for (int i = 0; i < 8; i++) B(t, new Vector3(2.3f, 0.03f, 0.03f), new Vector3(0f, 1.15f + i * 0.3f, -3.57f), trim);
            for (int s = -1; s <= 1; s += 2)
                B(t, new Vector3(0.32f, 0.18f, 0.05f), new Vector3(s * 0.8f, 1.15f, 3.71f), Mats.Emissive(new Color(0.9f, 0.9f, 0.85f), new Color(0.35f, 0.34f, 0.3f)));
            var d = Finish(t, "boxtruck", new Vector3(0f, 1f, 2.5f), 2f);
            Wheels(t, d, 0.5f, 0.3f, 1.05f, 2.6f, -2.4f);
            return d;
        }

        static readonly Color[] SportsPaint = { new Color(0.85f, 0.1f, 0.06f), new Color(0.95f, 0.7f, 0.05f), new Color(0.1f, 0.12f, 0.14f) };

        /// <summary>Low two-seat sports car, 4.5 × 1.95 × 1.2 m.</summary>
        static Destructible SportsCar(Transform t, int variant)
        {
            var paint = Mats.Surface("paint", SportsPaint[variant % SportsPaint.Length], 0.6f, 0.95f);
            var carbon = Mats.Lit(new Color(0.05f, 0.05f, 0.06f), 0.3f, 0.6f);
            Part(t, MeshKit.RoundedBox(new Vector3(1.95f, 0.55f, 4.5f), 0.25f, 4, new Vector2(0.95f, 0.92f)), new Vector3(0f, 0.55f, 0f), paint, true);
            Part(t, MeshKit.RoundedBox(new Vector3(1.5f, 0.42f, 1.9f), 0.2f, 4, new Vector2(0.78f, 0.5f)), new Vector3(0f, 1.0f, -0.35f), Mats.Glass(new Color(0.05f, 0.06f, 0.08f, 0.85f)), true);
            B(t, new Vector3(1.7f, 0.06f, 0.35f), new Vector3(0f, 1.05f, -2.05f), carbon);   // wing
            for (int s = -1; s <= 1; s += 2)
            {
                B(t, new Vector3(0.04f, 0.25f, 0.06f), new Vector3(s * 0.7f, 0.9f, -2.05f), carbon);
                B(t, new Vector3(0.5f, 0.08f, 0.05f), new Vector3(s * 0.6f, 0.7f, 2.24f), Mats.Emissive(new Color(0.95f, 0.95f, 0.9f), new Color(0.5f, 0.5f, 0.45f)));
                B(t, new Vector3(0.5f, 0.07f, 0.05f), new Vector3(s * 0.6f, 0.75f, -2.25f), Mats.Emissive(new Color(0.6f, 0.03f, 0.02f), new Color(0.6f, 0.02f, 0.01f)));
            }
            var d = Finish(t, "sportscar", new Vector3(0f, 0.5f, -1.4f), 1.3f);
            Wheels(t, d, 0.34f, 0.28f, 0.85f, 1.38f, -1.38f);
            return d;
        }

        /// <summary>Golf cart: a small body, a bench seat, a canopy on four posts, a bag rack.</summary>
        static Destructible GolfCart(Transform t)
        {
            var body = Paint(new Color(0.92f, 0.92f, 0.88f), 0.2f, 0.7f);
            var seat = Mats.Lit(new Color(0.15f, 0.25f, 0.2f), 0f, 0.4f);
            var steel = Mats.Lit(Mats.Steel, 0.8f, 0.5f);
            B(t, new Vector3(1.2f, 0.5f, 2.4f), new Vector3(0f, 0.5f, 0f), body, true, 0.1f);
            B(t, new Vector3(1.1f, 0.2f, 0.6f), new Vector3(0f, 0.85f, -0.3f), seat, true);
            B(t, new Vector3(1.1f, 0.5f, 0.12f), new Vector3(0f, 1.1f, -0.62f), seat);
            for (int i = 0; i < 4; i++)
                Tube(t, new Vector3(i % 2 == 0 ? -0.55f : 0.55f, 0.7f, i < 2 ? 0.75f : -0.75f), new Vector3(i % 2 == 0 ? -0.55f : 0.55f, 1.9f, i < 2 ? 0.6f : -0.75f), 0.025f, steel);
            B(t, new Vector3(1.25f, 0.06f, 1.7f), new Vector3(0f, 1.93f, -0.05f), body);
            Part(t, MeshKit.RoundedBox(new Vector3(1.1f, 0.6f, 0.03f), 0.02f), new Vector3(0f, 1.4f, 0.7f), Mats.Glass(Mats.GlassTint), false);
            var d = Finish(t, "golfcart", new Vector3(0f, 0.5f, 0f), 0.8f);
            Wheels(t, d, 0.22f, 0.18f, 0.6f, 0.8f, -0.8f);
            return d;
        }

        static readonly Color[] ScooterPaint = { new Color(0.75f, 0.15f, 0.1f), new Color(0.85f, 0.85f, 0.8f), new Color(0.3f, 0.55f, 0.6f) };

        /// <summary>Step-through scooter on its side stand.</summary>
        static Destructible Scooter(Transform t, int variant)
        {
            var paint = Paint(ScooterPaint[variant % ScooterPaint.Length], 0.4f, 0.85f);
            var dark = Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.2f, 0.4f);
            var tyre = Mats.Lit(Mats.Rubber, 0f, 0.3f);
            t.localRotation *= Quaternion.Euler(0f, 0f, -8f);
            for (int i = 0; i < 2; i++)
            {
                var w = Part(t, MeshKit.Tyre(0.24f, 0.1f), new Vector3(0f, 0.24f, i == 0 ? 0.62f : -0.6f), tyre, false);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            Part(t, MeshKit.RoundedBox(new Vector3(0.42f, 0.55f, 0.85f), 0.18f, 3, new Vector2(0.85f, 0.75f)), new Vector3(0f, 0.55f, -0.35f), paint, true);
            B(t, new Vector3(0.32f, 0.12f, 0.6f), new Vector3(0f, 0.86f, -0.35f), dark, true);   // seat
            B(t, new Vector3(0.34f, 0.08f, 0.5f), new Vector3(0f, 0.34f, 0.15f), dark);          // footboard
            Part(t, MeshKit.RoundedBox(new Vector3(0.36f, 0.75f, 0.2f), 0.1f, 3, new Vector2(0.7f, 0.6f)), new Vector3(0f, 0.75f, 0.5f), paint, true);
            Tube(t, new Vector3(0f, 1.05f, 0.55f), new Vector3(0f, 1.12f, 0.5f), 0.03f, dark);
            B(t, new Vector3(0.62f, 0.05f, 0.05f), new Vector3(0f, 1.14f, 0.5f), dark);
            B(t, new Vector3(0.14f, 0.1f, 0.05f), new Vector3(0f, 0.98f, 0.62f), Mats.Emissive(new Color(0.9f, 0.9f, 0.85f), new Color(0.3f, 0.3f, 0.26f)));
            return Finish(t, "scooter", new Vector3(0f, 0.5f, -0.3f), 0.5f);
        }

        /// <summary>Touring caravan, 6.5 m: rounded white body with a stripe, windows, door, A-frame and one axle.</summary>
        static Destructible Caravan(Transform t)
        {
            var body = Paint(new Color(0.94f, 0.93f, 0.9f), 0.2f, 0.7f);
            var stripe = Paint(new Color(0.55f, 0.35f, 0.2f), 0.2f, 0.6f);
            var glass = Mats.Glass(new Color(0.1f, 0.1f, 0.11f, 0.85f));
            Part(t, MeshKit.RoundedBox(new Vector3(2.3f, 2.2f, 6.5f), 0.35f, 4, new Vector2(0.98f, 0.97f)), new Vector3(0f, 1.6f, 0f), body, true);
            B(t, new Vector3(2.32f, 0.18f, 6.4f), new Vector3(0f, 1.25f, 0f), stripe);
            for (int s = -1; s <= 1; s += 2)
                foreach (var z in new[] { -2f, 0.6f, 2.3f })
                    Part(t, MeshKit.RoundedBox(new Vector3(0.04f, 0.6f, 1.1f), 0.08f, 2), new Vector3(s * 1.16f, 1.9f, z), glass, false);
            B(t, new Vector3(0.05f, 1.8f, 0.7f), new Vector3(1.16f, 1.4f, -0.6f), body);
            Tube(t, new Vector3(-0.5f, 0.55f, 3.2f), new Vector3(0f, 0.5f, 4.3f), 0.05f, Mats.Lit(Mats.DarkMetal, 0.7f, 0.4f));
            Tube(t, new Vector3(0.5f, 0.55f, 3.2f), new Vector3(0f, 0.5f, 4.3f), 0.05f, Mats.Lit(Mats.DarkMetal, 0.7f, 0.4f));
            B(t, new Vector3(0.1f, 0.5f, 0.1f), new Vector3(0f, 0.25f, 4.2f), Mats.Lit(Mats.DarkMetal, 0.7f, 0.4f), true);   // jockey wheel
            var d = Finish(t, "caravan", new Vector3(0f, 1.4f, 0f), 1.8f);
            var tyre = Mats.Surface("tyre", Color.white, 0f, 1f, 0.5f);
            for (int s = -1; s <= 1; s += 2)
            {
                var w = Part(t, MeshKit.Tyre(0.32f, 0.2f), new Vector3(s * 1.05f, 0.32f, -0.3f), tyre, false);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            return d;
        }

        /// <summary>Fuel tanker, 9 m: a cab and a long polished tank on a chassis.</summary>
        static Destructible FuelTruck(Transform t)
        {
            var cab = Paint(new Color(0.75f, 0.1f, 0.08f), 0.4f, 0.8f);
            var tank = Mats.Lit(new Color(0.82f, 0.83f, 0.84f), 0.95f, 0.8f);
            var trim = Mats.Lit(new Color(0.06f, 0.06f, 0.07f), 0.1f, 0.35f);
            B(t, new Vector3(2.3f, 0.45f, 9f), new Vector3(0f, 0.75f, 0f), trim, true, 0.05f);
            B(t, new Vector3(2.4f, 1.9f, 2.1f), new Vector3(0f, 1.95f, 3.4f), cab, true, 0.18f);
            Part(t, MeshKit.RoundedBox(new Vector3(2.2f, 0.8f, 0.1f), 0.04f), new Vector3(0f, 2.35f, 4.45f), Mats.Glass(new Color(0.1f, 0.12f, 0.14f, 0.7f)), false);
            var tg = Part(t, MeshKit.Lathe("tanker", new[] { new Vector2(0.05f, 0f), new Vector2(0.9f, 0.1f), new Vector2(1.1f, 0.4f), new Vector2(1.1f, 5.6f), new Vector2(0.9f, 5.9f), new Vector2(0.05f, 6f) }, 28, true, true), new Vector3(0f, 2.15f, -3.9f), tank, false);
            tg.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 2.15f, -0.9f);
            box.size = new Vector3(2.2f, 2.2f, 6f);
            B(t, new Vector3(2.42f, 0.25f, 6f), new Vector3(0f, 1.45f, -0.9f), Paint(new Color(0.75f, 0.1f, 0.08f)));
            var d = Finish(t, "fueltruck", new Vector3(0f, 1.8f, -0.9f), 2.6f);
            Wheels(t, d, 0.5f, 0.32f, 1.05f, 3.1f, -3f);
            return d;
        }

        // ---------- yard and street things ----------

        /// <summary>Kettle-free gas grill: a hooded cooker on a cart with a gas bottle underneath.</summary>
        static Destructible Grill(Transform t)
        {
            var black = Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.6f, 0.55f);
            var steel = Mats.Lit(Mats.Steel, 0.9f, 0.6f);
            B(t, new Vector3(1.1f, 0.06f, 0.55f), new Vector3(0f, 0.9f, 0f), steel);
            B(t, new Vector3(0.7f, 0.3f, 0.5f), new Vector3(0f, 1.05f, 0f), black, true);
            Part(t, MeshKit.RoundedBox(new Vector3(0.7f, 0.22f, 0.5f), 0.1f, 3, new Vector2(0.95f, 0.5f)), new Vector3(0f, 1.3f, 0f), black, false);
            B(t, new Vector3(0.5f, 0.03f, 0.03f), new Vector3(0f, 1.32f, -0.28f), steel);
            for (int i = 0; i < 4; i++) Tube(t, new Vector3(i % 2 == 0 ? -0.5f : 0.5f, 0f, i < 2 ? -0.24f : 0.24f), new Vector3(i % 2 == 0 ? -0.5f : 0.5f, 0.9f, i < 2 ? -0.24f : 0.24f), 0.02f, steel);
            Part(t, MeshKit.Lathe("grillgas", new[] { new Vector2(0.14f, 0f), new Vector2(0.15f, 0.04f), new Vector2(0.15f, 0.38f), new Vector2(0.06f, 0.45f), new Vector2(0f, 0.45f) }, 20, true), new Vector3(0.25f, 0.02f, 0f), Paint(new Color(0.2f, 0.35f, 0.6f)), false);
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.6f, 0f);
            box.size = new Vector3(1.1f, 1.2f, 0.55f);
            return Finish(t, "grill", new Vector3(0f, 1f, 0f), 0.5f, 30f);
        }

        static readonly Color[] GnomeHats = { new Color(0.75f, 0.08f, 0.06f), new Color(0.1f, 0.35f, 0.2f), new Color(0.15f, 0.25f, 0.55f) };

        /// <summary>Garden gnome, 45 cm: boots, a round body, a white beard, a face and a tall pointed hat.</summary>
        static Destructible Gnome(Transform t, int variant)
        {
            var coat = Paint(GnomeHats[(variant + 1) % GnomeHats.Length], 0f, 0.6f);
            var hat = Paint(GnomeHats[variant % GnomeHats.Length], 0f, 0.6f);
            Part(t, MeshKit.Lathe("gnomebody", new[] { new Vector2(0.11f, 0f), new Vector2(0.12f, 0.06f), new Vector2(0.115f, 0.18f), new Vector2(0.08f, 0.26f), new Vector2(0f, 0.27f) }, 18, true), Vector3.zero, coat, false);
            Part(t, MeshKit.Lathe("gnomebeard", new[] { new Vector2(0.01f, 0.13f), new Vector2(0.075f, 0.2f), new Vector2(0.07f, 0.3f), new Vector2(0f, 0.31f) }, 14), new Vector3(0f, 0f, 0.05f), Mats.Lit(new Color(0.95f, 0.95f, 0.93f), 0f, 0.3f), false);
            Part(t, MeshKit.Blob(0.07f, 0.05f, 77), new Vector3(0f, 0.32f, 0.01f), Mats.Lit(Mats.Skin, 0f, 0.4f), false);
            Part(t, MeshKit.Lathe("gnomehat", new[] { new Vector2(0.08f, 0f), new Vector2(0.06f, 0.08f), new Vector2(0.03f, 0.16f), new Vector2(0f, 0.22f) }, 14, false, true), new Vector3(0f, 0.36f, -0.01f), hat, false);
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.27f, 0f);
            box.size = new Vector3(0.24f, 0.54f, 0.24f);
            return Finish(t, "gnome", new Vector3(0f, 0.25f, 0f), 0.25f, 4f);
        }

        /// <summary>Pink plastic lawn flamingo on two wire legs.</summary>
        static Destructible Flamingo(Transform t)
        {
            var pink = Mats.Lit(new Color(0.95f, 0.35f, 0.55f), 0f, 0.65f);
            var wire = Mats.Lit(Mats.DarkMetal, 0.8f, 0.4f);
            Tube(t, new Vector3(-0.03f, 0f, 0f), new Vector3(-0.02f, 0.55f, 0f), 0.006f, wire);
            Tube(t, new Vector3(0.03f, 0f, 0f), new Vector3(0.02f, 0.55f, 0f), 0.006f, wire);
            Part(t, MeshKit.Blob(0.16f, 0.05f, 51), new Vector3(0f, 0.65f, -0.03f), pink, false).transform.localScale = new Vector3(0.7f, 0.75f, 1.3f);
            Tube(t, new Vector3(0f, 0.72f, 0.12f), new Vector3(0f, 0.98f, 0.16f), 0.025f, pink);
            Part(t, MeshKit.Blob(0.05f, 0.05f, 52), new Vector3(0f, 1.0f, 0.19f), pink, false);
            B(t, new Vector3(0.025f, 0.025f, 0.08f), new Vector3(0f, 0.98f, 0.26f), Mats.Lit(new Color(0.1f, 0.1f, 0.1f), 0f, 0.4f));
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.55f, 0.05f);
            box.size = new Vector3(0.2f, 1.1f, 0.45f);
            return Finish(t, "flamingo", new Vector3(0f, 0.65f, 0f), 0.2f, 1f);
        }

        /// <summary>Cast-iron fire hydrant: a turned barrel, a bonnet and three outlets.</summary>
        static Destructible Hydrant(Transform t)
        {
            var red = Paint(new Color(0.75f, 0.12f, 0.08f), 0.5f, 0.7f);
            Part(t, MeshKit.Lathe("hydrant", new[] { new Vector2(0.17f, 0f), new Vector2(0.17f, 0.08f), new Vector2(0.13f, 0.1f), new Vector2(0.13f, 0.58f), new Vector2(0.16f, 0.62f), new Vector2(0.14f, 0.7f), new Vector2(0.06f, 0.8f), new Vector2(0.03f, 0.86f), new Vector2(0f, 0.86f) }, 22, false, true), Vector3.zero, red, false);
            foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.forward })
            {
                var o = Part(t, MeshKit.Lathe("hydrantcap", new[] { new Vector2(0.05f, 0f), new Vector2(0.05f, 0.08f), new Vector2(0.055f, 0.1f), new Vector2(0f, 0.11f) }, 12, false, true), dir * 0.11f + Vector3.up * 0.48f, red, false);
                o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            }
            var col = t.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.43f, 0f);
            col.radius = 0.17f;
            col.height = 0.86f;
            return Finish(t, "hydrant", new Vector3(0f, 0.5f, 0f), 0.3f);
        }

        static readonly Color[] NewsColours = { new Color(0.1f, 0.25f, 0.55f), new Color(0.7f, 0.12f, 0.1f), new Color(0.9f, 0.75f, 0.15f) };

        /// <summary>Coin-operated newspaper box on legs, with a glass front.</summary>
        static Destructible NewsBox(Transform t, int variant)
        {
            var paint = Paint(NewsColours[variant % NewsColours.Length], 0.5f, 0.65f);
            B(t, new Vector3(0.5f, 0.75f, 0.45f), new Vector3(0f, 0.75f, 0f), paint, true);
            B(t, new Vector3(0.52f, 0.08f, 0.47f), new Vector3(0f, 1.16f, 0f), paint);
            Part(t, MeshKit.RoundedBox(new Vector3(0.4f, 0.3f, 0.02f), 0.01f), new Vector3(0f, 0.8f, 0.23f), Mats.Glass(Mats.GlassTint), false);
            B(t, new Vector3(0.35f, 0.22f, 0.01f), new Vector3(0f, 0.8f, 0.215f), Mats.Lit(new Color(0.85f, 0.83f, 0.78f), 0f, 0.2f));
            for (int i = 0; i < 4; i++) B(t, new Vector3(0.04f, 0.38f, 0.04f), new Vector3(i % 2 == 0 ? -0.2f : 0.2f, 0.19f, i < 2 ? -0.17f : 0.17f), Mats.Lit(Mats.DarkMetal, 0.7f, 0.4f));
            return Finish(t, "newsbox", new Vector3(0f, 0.75f, 0f), 0.35f, 30f);
        }

        static readonly Color[] VendingColours = { new Color(0.72f, 0.08f, 0.08f), new Color(0.1f, 0.3f, 0.6f) };

        /// <summary>Drinks vending machine, 1.8 m: a lit glass front with rows of cans.</summary>
        static Destructible Vending(Transform t, int variant)
        {
            var paint = Paint(VendingColours[variant % VendingColours.Length], 0.3f, 0.7f);
            B(t, new Vector3(0.95f, 1.85f, 0.8f), new Vector3(0f, 0.925f, 0f), paint, true, 0.05f);
            B(t, new Vector3(0.62f, 1.25f, 0.02f), new Vector3(-0.1f, 1.15f, 0.41f), Mats.Emissive(new Color(0.85f, 0.9f, 0.95f), new Color(0.9f, 0.95f, 1f)));
            var cans = new[] { new Color(0.8f, 0.1f, 0.1f), new Color(0.1f, 0.5f, 0.2f), new Color(0.95f, 0.7f, 0.1f), new Color(0.2f, 0.3f, 0.8f) };
            for (int row = 0; row < 5; row++)
                for (int k = 0; k < 4; k++)
                    B(t, new Vector3(0.1f, 0.16f, 0.04f), new Vector3(-0.35f + k * 0.16f, 0.65f + row * 0.24f, 0.43f), Mats.Lit(cans[(row + k) % cans.Length], 0.6f, 0.7f));
            Part(t, MeshKit.RoundedBox(new Vector3(0.64f, 1.27f, 0.02f), 0.01f), new Vector3(-0.1f, 1.15f, 0.45f), Mats.Glass(Mats.GlassTint), false);
            B(t, new Vector3(0.18f, 0.5f, 0.04f), new Vector3(0.33f, 1.2f, 0.42f), Mats.Lit(Mats.DarkMetal, 0.7f, 0.5f));
            return Finish(t, "vending", new Vector3(0f, 0.9f, 0f), 0.7f);
        }

        /// <summary>Park bench: cast-iron ends and five timber slats.</summary>
        static Destructible Bench(Transform t)
        {
            var wood = Mats.Surface("wood", new Color(0.72f, 0.52f, 0.36f), 0f, 0.5f);
            var iron = Mats.Lit(new Color(0.1f, 0.11f, 0.1f), 0.7f, 0.4f);
            for (int s = -1; s <= 1; s += 2)
            {
                B(t, new Vector3(0.06f, 0.45f, 0.5f), new Vector3(s * 0.85f, 0.23f, 0f), iron);
                B(t, new Vector3(0.06f, 0.5f, 0.06f), new Vector3(s * 0.85f, 0.7f, -0.22f), iron);
                B(t, new Vector3(0.06f, 0.06f, 0.5f), new Vector3(s * 0.85f, 0.65f, 0f), iron);
            }
            for (int i = 0; i < 3; i++) B(t, new Vector3(1.8f, 0.04f, 0.12f), new Vector3(0f, 0.46f, -0.15f + i * 0.15f), wood);
            for (int i = 0; i < 2; i++) B(t, new Vector3(1.8f, 0.12f, 0.035f), new Vector3(0f, 0.65f + i * 0.17f, -0.24f), wood);
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.45f, 0f);
            box.size = new Vector3(1.85f, 0.9f, 0.55f);
            return Finish(t, "bench", new Vector3(0f, 0.45f, 0f), 0.6f, 60f);
        }

        static readonly Color[] Parasols = { new Color(0.85f, 0.2f, 0.15f), new Color(0.2f, 0.45f, 0.35f), new Color(0.92f, 0.88f, 0.78f) };

        /// <summary>Round bistro table with two chairs and a parasol.</summary>
        static Destructible CafeTable(Transform t, int variant)
        {
            var metal = Mats.Lit(new Color(0.15f, 0.16f, 0.17f), 0.8f, 0.5f);
            Part(t, MeshKit.Lathe("cafetop", new[] { new Vector2(0f, 0.74f), new Vector2(0.35f, 0.74f), new Vector2(0.35f, 0.77f), new Vector2(0f, 0.77f) }, 20, true, true), Vector3.zero, Mats.Lit(new Color(0.9f, 0.9f, 0.88f), 0.3f, 0.6f), false);
            Tube(t, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.74f, 0f), 0.025f, metal);
            Part(t, MeshKit.Lathe("cafefoot", new[] { new Vector2(0.22f, 0f), new Vector2(0.2f, 0.03f), new Vector2(0f, 0.04f) }, 16, true), Vector3.zero, metal, false);
            Tube(t, new Vector3(0f, 0.77f, 0f), new Vector3(0f, 2.2f, 0f), 0.018f, metal);
            Part(t, MeshKit.Lathe("parasol", new[] { new Vector2(1.1f, 0f), new Vector2(0.7f, 0.18f), new Vector2(0.05f, 0.32f), new Vector2(0f, 0.33f) }, 12, false, true), new Vector3(0f, 1.95f, 0f), Mats.Lit(Parasols[variant % Parasols.Length], 0f, 0.2f), false);
            for (int s = -1; s <= 1; s += 2)
            {
                B(t, new Vector3(0.4f, 0.04f, 0.4f), new Vector3(s * 0.6f, 0.45f, 0f), metal);
                B(t, new Vector3(0.04f, 0.4f, 0.4f), new Vector3(s * 0.8f, 0.68f, 0f), metal);
                for (int k = 0; k < 4; k++) Tube(t, new Vector3(s * 0.6f + (k % 2 == 0 ? -0.17f : 0.17f), 0f, k < 2 ? -0.17f : 0.17f), new Vector3(s * 0.6f + (k % 2 == 0 ? -0.17f : 0.17f), 0.45f, k < 2 ? -0.17f : 0.17f), 0.012f, metal);
            }
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.45f, 0f);
            box.size = new Vector3(1.7f, 0.9f, 0.75f);
            return Finish(t, "cafetable", new Vector3(0f, 0.6f, 0f), 0.4f, 15f);
        }

        /// <summary>Inflatable snowman, 1.8 m: three white balls, a top hat, a scarf and a fan base.</summary>
        static Destructible Inflatable(Transform t, int variant)
        {
            var white = Mats.Lit(new Color(0.96f, 0.96f, 0.97f), 0f, 0.55f);
            var black = Mats.Lit(new Color(0.06f, 0.06f, 0.07f), 0f, 0.5f);
            var scarf = Mats.Lit(variant % 2 == 0 ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.15f, 0.45f, 0.25f), 0f, 0.4f);
            Part(t, MeshKit.Blob(0.5f, 0.03f, 401), new Vector3(0f, 0.5f, 0f), white, false);
            Part(t, MeshKit.Blob(0.38f, 0.03f, 402), new Vector3(0f, 1.15f, 0f), white, false);
            Part(t, MeshKit.Blob(0.28f, 0.03f, 403), new Vector3(0f, 1.62f, 0f), white, false);
            Part(t, MeshKit.Lathe("snowhat", new[] { new Vector2(0.28f, 0f), new Vector2(0.28f, 0.03f), new Vector2(0.17f, 0.03f), new Vector2(0.17f, 0.3f), new Vector2(0f, 0.3f) }, 18, false, true), new Vector3(0f, 1.84f, 0f), black, false);
            Part(t, MeshKit.Lathe("snowscarf", new[] { new Vector2(0.27f, 0f), new Vector2(0.3f, 0.06f), new Vector2(0.27f, 0.12f) }, 18), new Vector3(0f, 1.36f, 0f), scarf, false);
            for (int s = -1; s <= 1; s += 2) B(t, new Vector3(0.05f, 0.05f, 0.03f), new Vector3(s * 0.09f, 1.7f, 0.26f), black);
            B(t, new Vector3(0.04f, 0.04f, 0.16f), new Vector3(0f, 1.62f, 0.32f), Mats.Lit(new Color(0.95f, 0.5f, 0.1f), 0f, 0.4f));
            var col = t.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.radius = 0.45f;
            col.height = 1.9f;
            return Finish(t, "inflatable", new Vector3(0f, 0.8f, 0f), 0.6f, 3f);
        }

        static readonly Color[] Wrapping = { new Color(0.75f, 0.1f, 0.1f), new Color(0.1f, 0.4f, 0.22f), new Color(0.2f, 0.3f, 0.7f), new Color(0.85f, 0.7f, 0.15f), new Color(0.6f, 0.2f, 0.5f) };

        /// <summary>A wrapped present with a ribbon both ways and a bow.</summary>
        static Destructible GiftBox(Transform t, int variant)
        {
            float size = 0.35f + (variant % 3) * 0.12f;
            var paper = Mats.Lit(Wrapping[variant % Wrapping.Length], 0f, 0.5f);
            var ribbon = Mats.Lit(Wrapping[(variant + 3) % Wrapping.Length] * 1.2f, 0.3f, 0.7f);
            B(t, new Vector3(size, size * 0.8f, size), new Vector3(0f, size * 0.4f, 0f), paper, true, 0.01f);
            B(t, new Vector3(size + 0.01f, size * 0.8f + 0.01f, 0.05f), new Vector3(0f, size * 0.4f, 0f), ribbon, false, 0.005f);
            B(t, new Vector3(0.05f, size * 0.8f + 0.01f, size + 0.01f), new Vector3(0f, size * 0.4f, 0f), ribbon, false, 0.005f);
            Part(t, MeshKit.Blob(0.06f, 0.2f, 450 + variant), new Vector3(0f, size * 0.8f + 0.03f, 0f), ribbon, false);
            return Finish(t, "giftbox", new Vector3(0f, size * 0.4f, 0f), 0.3f, 4f);
        }

        static readonly Color[] Webbing = { new Color(0.2f, 0.45f, 0.65f), new Color(0.85f, 0.55f, 0.15f), new Color(0.2f, 0.55f, 0.35f) };

        /// <summary>Folding aluminium lawn chair with striped webbing.</summary>
        static Destructible LawnChair(Transform t, int variant)
        {
            var alu = Mats.Lit(new Color(0.75f, 0.76f, 0.77f), 0.9f, 0.6f);
            var web = Mats.Lit(Webbing[variant % Webbing.Length], 0f, 0.3f);
            for (int s = -1; s <= 1; s += 2)
            {
                Tube(t, new Vector3(s * 0.27f, 0f, 0.25f), new Vector3(s * 0.27f, 0.4f, -0.2f), 0.012f, alu);
                Tube(t, new Vector3(s * 0.27f, 0f, -0.25f), new Vector3(s * 0.27f, 0.4f, 0.2f), 0.012f, alu);
                Tube(t, new Vector3(s * 0.27f, 0.4f, -0.22f), new Vector3(s * 0.27f, 0.85f, -0.32f), 0.012f, alu);
                Tube(t, new Vector3(s * 0.27f, 0.6f, -0.2f), new Vector3(s * 0.27f, 0.6f, 0.2f), 0.012f, alu);
            }
            for (int i = 0; i < 4; i++) B(t, new Vector3(0.54f, 0.01f, 0.09f), new Vector3(0f, 0.41f, -0.15f + i * 0.1f), web, false, 0.003f);
            for (int i = 0; i < 4; i++)
            {
                var slat = B(t, new Vector3(0.54f, 0.09f, 0.01f), new Vector3(0f, 0.48f + i * 0.1f, -0.24f - i * 0.02f), web, false, 0.003f);
                slat.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            }
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.43f, -0.05f);
            box.size = new Vector3(0.58f, 0.86f, 0.55f);
            return Finish(t, "lawnchair", new Vector3(0f, 0.45f, 0f), 0.3f, 3f);
        }

        /// <summary>Road cone, 70 cm, orange with two reflective bands, on a square base.</summary>
        static Destructible Cone(Transform t)
        {
            var orange = Mats.Lit(new Color(0.98f, 0.38f, 0.05f), 0f, 0.6f);
            B(t, new Vector3(0.38f, 0.04f, 0.38f), new Vector3(0f, 0.02f, 0f), Mats.Lit(new Color(0.08f, 0.08f, 0.08f), 0f, 0.4f));
            Part(t, MeshKit.Lathe("cone", new[] { new Vector2(0.15f, 0.04f), new Vector2(0.03f, 0.7f), new Vector2(0f, 0.7f) }, 18, true), Vector3.zero, orange, false);
            foreach (var y in new[] { 0.3f, 0.46f })
                Part(t, MeshKit.Lathe("coneband" + y, new[] { new Vector2(0.15f - (y - 0.04f) * 0.18f + 0.003f, y), new Vector2(0.15f - (y + 0.06f - 0.04f) * 0.18f + 0.003f, y + 0.06f) }, 18), Vector3.zero, Mats.Lit(new Color(0.95f, 0.95f, 0.95f), 0.3f, 0.8f), false);
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.35f, 0f);
            box.size = new Vector3(0.38f, 0.7f, 0.38f);
            return Finish(t, "cone", new Vector3(0f, 0.3f, 0f), 0.2f, 3f);
        }

        /// <summary>Timber garden shed, 2.4 × 1.8 m, with a felt roof, a door and a small window.</summary>
        static Destructible Shed(Transform t)
        {
            var wood = Mats.Surface("wood", new Color(0.55f, 0.42f, 0.3f), 0f, 0.5f);
            var felt = Mats.Surface("roof", new Color(0.3f, 0.3f, 0.3f), 0f, 1f);
            B(t, new Vector3(2.4f, 2f, 1.8f), new Vector3(0f, 1f, 0f), wood, true, 0.02f);
            for (int i = 0; i < 12; i++) B(t, new Vector3(2.42f, 0.02f, 0.02f), new Vector3(0f, 0.1f + i * 0.17f, 0.91f), wood);
            var roof = Part(t, MeshKit.Gable(new Vector3(2f, 0.6f, 2.7f), 0.15f), new Vector3(0f, 2f, 0f), felt, false);
            roof.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            B(t, new Vector3(0.8f, 1.7f, 0.05f), new Vector3(-0.5f, 0.88f, 0.92f), Mats.Surface("wood", new Color(0.45f, 0.33f, 0.24f), 0f, 0.5f));
            Part(t, MeshKit.RoundedBox(new Vector3(0.6f, 0.45f, 0.02f), 0.01f), new Vector3(0.6f, 1.3f, 0.92f), Mats.Glass(Mats.GlassTint), false);
            return Finish(t, "shed", new Vector3(0f, 1f, 0f), 1.2f);
        }

        /// <summary>The grumpy neighbour's armoured shed: riveted steel plates, a barred window and a padlocked door.</summary>
        static Destructible Fortress(Transform t)
        {
            var plate = Mats.Surface("rust", new Color(0.8f, 0.8f, 0.78f), 0.7f, 0.6f);
            var steel = Mats.Lit(new Color(0.35f, 0.36f, 0.37f), 0.9f, 0.5f);
            var hazard = Mats.Lit(new Color(0.95f, 0.75f, 0.1f), 0.2f, 0.5f);
            B(t, new Vector3(3.6f, 2.6f, 3f), new Vector3(0f, 1.3f, 0f), plate, true, 0.04f);
            B(t, new Vector3(3.9f, 0.2f, 3.3f), new Vector3(0f, 2.7f, 0f), steel, false);
            for (int i = 0; i < 5; i++) B(t, new Vector3(0.08f, 2.6f, 0.05f), new Vector3(-1.6f + i * 0.8f, 1.3f, 1.52f), steel);
            for (int i = 0; i < 4; i++) B(t, new Vector3(3.62f, 0.06f, 0.05f), new Vector3(0f, 0.3f + i * 0.7f, 1.53f), steel);
            B(t, new Vector3(1.1f, 2f, 0.08f), new Vector3(0.6f, 1f, 1.55f), steel);
            B(t, new Vector3(0.12f, 0.16f, 0.06f), new Vector3(0.2f, 1f, 1.6f), Mats.Lit(new Color(0.7f, 0.6f, 0.2f), 1f, 0.7f));
            for (int i = 0; i < 6; i++) B(t, new Vector3(0.3f, 0.12f, 0.02f), new Vector3(-1.5f + i * 0.6f, 2.45f, 1.56f), i % 2 == 0 ? hazard : Mats.Lit(new Color(0.06f, 0.06f, 0.06f), 0f, 0.4f));
            B(t, new Vector3(0.7f, 0.5f, 0.04f), new Vector3(-1f, 1.6f, 1.53f), Mats.Lit(new Color(0.03f, 0.03f, 0.04f), 0f, 0.2f));
            for (int i = 0; i < 4; i++) B(t, new Vector3(0.03f, 0.5f, 0.03f), new Vector3(-1.27f + i * 0.18f, 1.6f, 1.57f), steel);
            return Finish(t, "fortress", new Vector3(0f, 1.3f, 0f), 1.6f);
        }

        /// <summary>Diesel generator in a box on skids, with an exhaust stack and a control panel.</summary>
        static Destructible Generator(Transform t)
        {
            var yellow = Paint(new Color(0.88f, 0.65f, 0.12f), 0.3f, 0.6f);
            var dark = Mats.Lit(Mats.DarkMetal, 0.7f, 0.4f);
            B(t, new Vector3(1.0f, 1.1f, 2f), new Vector3(0f, 0.7f, 0f), yellow, true, 0.04f);
            for (int s = -1; s <= 1; s += 2) B(t, new Vector3(0.1f, 0.15f, 2.2f), new Vector3(s * 0.42f, 0.08f, 0f), dark, true);
            for (int i = 0; i < 6; i++) B(t, new Vector3(1.02f, 0.03f, 0.5f), new Vector3(0f, 0.5f + i * 0.08f, -0.6f), dark);
            Tube(t, new Vector3(0.3f, 1.25f, 0.6f), new Vector3(0.3f, 1.7f, 0.6f), 0.05f, dark);
            B(t, new Vector3(0.02f, 0.3f, 0.4f), new Vector3(0.51f, 0.9f, 0.5f), Mats.Lit(new Color(0.15f, 0.15f, 0.16f), 0.3f, 0.5f));
            return Finish(t, "generator", new Vector3(0f, 0.8f, 0f), 0.9f);
        }

        /// <summary>Outdoor hot tub, 2.1 m square, with water, a cover rolled back and timber cladding.</summary>
        static Destructible Jacuzzi(Transform t)
        {
            var clad = Mats.Surface("wood", new Color(0.45f, 0.33f, 0.25f), 0f, 0.5f);
            B(t, new Vector3(2.1f, 0.9f, 2.1f), new Vector3(0f, 0.45f, 0f), clad, true, 0.08f);
            B(t, new Vector3(1.9f, 0.05f, 1.9f), new Vector3(0f, 0.86f, 0f), Mats.Glass(new Color(0.35f, 0.65f, 0.75f, 0.6f), 0.98f));
            B(t, new Vector3(2.12f, 0.06f, 2.12f), new Vector3(0f, 0.91f, 0f), Mats.Lit(new Color(0.9f, 0.9f, 0.88f), 0f, 0.6f), false);
            var cover = B(t, new Vector3(2.1f, 0.1f, 0.5f), new Vector3(0f, 0.98f, -1.15f), Mats.Lit(new Color(0.3f, 0.3f, 0.32f), 0f, 0.4f));
            cover.transform.localRotation = Quaternion.Euler(-70f, 0f, 0f);
            return Finish(t, "jacuzzi", new Vector3(0f, 0.6f, 0f), 0.9f);
        }

        /// <summary>Street food trailer: a box with a serving hatch and an awning, a gas bottle at the back.</summary>
        static Destructible FoodCart(Transform t)
        {
            var body = Paint(new Color(0.95f, 0.85f, 0.4f), 0.3f, 0.7f);
            var stripe = Paint(new Color(0.75f, 0.15f, 0.1f), 0.2f, 0.6f);
            B(t, new Vector3(2f, 2.1f, 3.6f), new Vector3(0f, 1.45f, 0f), body, true, 0.1f);
            B(t, new Vector3(2.02f, 0.3f, 3.6f), new Vector3(0f, 0.7f, 0f), stripe);
            B(t, new Vector3(0.04f, 0.9f, 2f), new Vector3(1.01f, 1.7f, 0.2f), Mats.Emissive(new Color(0.6f, 0.45f, 0.3f), new Color(0.9f, 0.6f, 0.3f)));
            var awning = B(t, new Vector3(0.9f, 0.05f, 2.3f), new Vector3(1.4f, 2.35f, 0.2f), stripe);
            awning.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            B(t, new Vector3(0.4f, 0.08f, 2f), new Vector3(1.2f, 1.25f, 0.2f), Mats.Lit(Mats.Steel, 0.9f, 0.6f));
            Part(t, MeshKit.Lathe("cartgas", new[] { new Vector2(0.13f, 0f), new Vector2(0.15f, 0.04f), new Vector2(0.15f, 0.48f), new Vector2(0.05f, 0.58f), new Vector2(0f, 0.58f) }, 20, true), new Vector3(0f, 0.4f, -2f), Paint(new Color(0.72f, 0.16f, 0.1f)), false);
            var d = Finish(t, "foodcart", new Vector3(0f, 1.2f, 0f), 1.2f);
            var tyre = Mats.Surface("tyre", Color.white, 0f, 1f, 0.5f);
            for (int s = -1; s <= 1; s += 2)
            {
                var w = Part(t, MeshKit.Tyre(0.3f, 0.18f), new Vector3(s * 0.95f, 0.3f, 0f), tyre, false);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            return d;
        }

        /// <summary>A roadside fireworks stall: a counter under a striped roof, stacked with boxes and rockets.</summary>
        static Destructible FireworkStand(Transform t)
        {
            var wood = Mats.Surface("wood", new Color(0.7f, 0.55f, 0.4f), 0f, 0.5f);
            var red = Mats.Lit(new Color(0.8f, 0.12f, 0.1f), 0f, 0.4f);
            B(t, new Vector3(3f, 1f, 1.2f), new Vector3(0f, 0.5f, 0f), wood, true);
            for (int s = -1; s <= 1; s += 2) B(t, new Vector3(0.1f, 2.4f, 0.1f), new Vector3(s * 1.45f, 1.2f, -0.55f), wood);
            ArenaBuilder.Awning(t, new Vector3(0f, 2.45f, 0.1f), 3.2f, new Color(0.8f, 0.12f, 0.1f));
            var cols = new[] { new Color(0.8f, 0.12f, 0.1f), new Color(0.15f, 0.3f, 0.7f), new Color(0.9f, 0.7f, 0.1f), new Color(0.15f, 0.55f, 0.3f) };
            for (int i = 0; i < 9; i++)
                B(t, new Vector3(0.28f, 0.2f + (i % 3) * 0.08f, 0.3f), new Vector3(-1.2f + i * 0.3f, 1.1f + (i % 3) * 0.04f, 0.1f), Mats.Lit(cols[i % cols.Length], 0f, 0.5f));
            for (int i = 0; i < 6; i++) Tube(t, new Vector3(-1.2f + i * 0.45f, 1f, -0.35f), new Vector3(-1.2f + i * 0.45f, 1.7f, -0.35f), 0.03f, red);
            return Finish(t, "fireworkstand", new Vector3(0f, 1f, 0f), 1.1f);
        }

        /// <summary>Filling-station pump: a tall cabinet with a lit display, two nozzles on hoses and a price panel.</summary>
        static Destructible FuelPump(Transform t)
        {
            var white = Paint(new Color(0.93f, 0.93f, 0.9f), 0.3f, 0.7f);
            var red = Paint(new Color(0.7f, 0.1f, 0.08f), 0.3f, 0.7f);
            var dark = Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.3f, 0.4f);
            B(t, new Vector3(1.6f, 0.2f, 0.9f), new Vector3(0f, 0.1f, 0f), Mats.Surface("concrete", Color.white, 0f, 0.8f), true);   // island
            B(t, new Vector3(1f, 1.9f, 0.55f), new Vector3(0f, 1.15f, 0f), white, true, 0.06f);
            B(t, new Vector3(1.02f, 0.4f, 0.57f), new Vector3(0f, 2.0f, 0f), red);
            for (int s = -1; s <= 1; s += 2)
            {
                B(t, new Vector3(0.6f, 0.3f, 0.02f), new Vector3(0f, 1.55f, s * 0.285f), Mats.Emissive(new Color(0.2f, 0.4f, 0.3f), new Color(0.3f, 0.9f, 0.5f)));
                B(t, new Vector3(0.12f, 0.22f, 0.1f), new Vector3(0.42f, 1.0f, s * 0.3f), dark);
                Tube(t, new Vector3(0.42f, 1.1f, s * 0.3f), new Vector3(0.6f, 0.5f, s * 0.35f), 0.02f, dark);
            }
            return Finish(t, "fuelpump", new Vector3(0f, 1f, 0f), 0.9f);
        }
    }
}
