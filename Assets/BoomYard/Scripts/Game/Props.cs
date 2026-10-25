using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// The destructible objects, modelled in code: bevelled panels, lathe-turned tyres and cylinders and PBR
    /// surfaces, in real dimensions. <see cref="Spawn"/> places one by its catalog id; levels are lists of these.
    /// </summary>
    public static partial class Props
    {
        public static Destructible Spawn(string id, Transform parent, Vector3 pos, float yaw, int variant = 0)
        {
            var root = new GameObject(id);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var t = root.transform;
            Destructible d;
            switch (id)
            {
                case "van": d = Van(t, variant); break;
                case "sedan": d = Sedan(t, variant); break;
                case "bin": d = Bin(t, variant); break;
                case "mailbox": d = Mailbox(t); break;
                case "acunit": d = AcUnit(t); break;
                case "bicycle": d = Bicycle(t); break;
                case "pallets": d = Pallets(t); break;
                case "gascyl": d = GasCylinder(t); break;
                case "barrel": d = Barrel(t, variant); break;
                case "fence": d = Fence(t); break;
                case "crate": d = Crate(t); break;
                default: d = Street(id, t, variant) ?? Crate(t); break;
            }
            return d;
        }

        // ---------- vehicles ----------

        static readonly Color[] VanPaint = { new Color(0.86f, 0.85f, 0.81f), new Color(0.2f, 0.31f, 0.5f), new Color(0.62f, 0.13f, 0.1f) };
        static readonly Color[] CarPaint = { new Color(0.5f, 0.05f, 0.05f), new Color(0.08f, 0.12f, 0.2f), new Color(0.72f, 0.72f, 0.7f), new Color(0.12f, 0.12f, 0.13f) };

        /// <summary>Old panel van, 5.0 × 2.0 × 2.3 m: body, high roof, raked windscreen, glass, 4 wheels and 2 doors that fly off.</summary>
        static Destructible Van(Transform t, int variant)
        {
            var paint = Mats.Surface("paint", VanPaint[variant % VanPaint.Length], 0.35f, 0.82f);
            var trim = Mats.Surface("paint", new Color(0.09f, 0.09f, 0.1f), 0f, 0.35f);
            var glass = Mats.Glass(new Color(0.08f, 0.1f, 0.12f, 0.75f));
            Part(t, MeshKit.RoundedBox(new Vector3(2.0f, 1.15f, 5.0f), 0.12f), new Vector3(0f, 0.97f, 0f), paint, true);
            Part(t, MeshKit.RoundedBox(new Vector3(1.97f, 0.95f, 3.85f), 0.16f, 3, new Vector2(0.97f, 0.99f)), new Vector3(0f, 1.98f, -0.55f), paint, true);
            Part(t, MeshKit.RoundedBox(new Vector3(1.9f, 0.78f, 1.05f), 0.1f, 3, new Vector2(0.93f, 0.55f)), new Vector3(0f, 1.9f, 1.42f), glass, false);
            Part(t, MeshKit.RoundedBox(new Vector3(1.94f, 0.42f, 0.85f), 0.14f, 3, new Vector2(0.96f, 0.8f)), new Vector3(0f, 1.66f, 2.07f), paint, false);
            Part(t, MeshKit.RoundedBox(new Vector3(2.06f, 0.26f, 0.22f), 0.08f), new Vector3(0f, 0.52f, 2.48f), trim, false);
            Part(t, MeshKit.RoundedBox(new Vector3(2.06f, 0.26f, 0.2f), 0.08f), new Vector3(0f, 0.52f, -2.49f), trim, false);
            Part(t, MeshKit.RoundedBox(new Vector3(2.02f, 0.14f, 4.4f), 0.05f), new Vector3(0f, 0.46f, 0f), Mats.Surface("rust", Color.white, 0.4f, 1f), false);   // rusty sills
            Part(t, MeshKit.RoundedBox(new Vector3(1.1f, 0.3f, 0.06f), 0.03f), new Vector3(0f, 1.1f, 2.5f), Mats.Lit(new Color(0.05f, 0.05f, 0.05f), 0.5f, 0.4f), false);   // grille
            Part(t, MeshKit.RoundedBox(new Vector3(0.36f, 0.2f, 0.06f), 0.05f), new Vector3(-0.72f, 1.14f, 2.5f), Mats.Emissive(new Color(0.9f, 0.9f, 0.85f), new Color(0.4f, 0.38f, 0.33f)), false);
            Part(t, MeshKit.RoundedBox(new Vector3(0.36f, 0.2f, 0.06f), 0.05f), new Vector3(0.72f, 1.14f, 2.5f), Mats.Emissive(new Color(0.9f, 0.9f, 0.85f), new Color(0.4f, 0.38f, 0.33f)), false);
            Part(t, MeshKit.RoundedBox(new Vector3(0.14f, 0.36f, 0.05f), 0.03f), new Vector3(-0.86f, 1.25f, -2.5f), Mats.Emissive(new Color(0.5f, 0.04f, 0.03f), new Color(0.3f, 0.01f, 0.01f)), false);
            Part(t, MeshKit.RoundedBox(new Vector3(0.14f, 0.36f, 0.05f), 0.03f), new Vector3(0.86f, 1.25f, -2.5f), Mats.Emissive(new Color(0.5f, 0.04f, 0.03f), new Color(0.3f, 0.01f, 0.01f)), false);
            Part(t, MeshKit.RoundedBox(new Vector3(0.5f, 0.12f, 0.02f), 0.01f), new Vector3(0f, 0.75f, -2.51f), Mats.Lit(new Color(0.9f, 0.9f, 0.86f), 0f, 0.5f), false);   // plate
            for (int s = -1; s <= 1; s += 2)
                Part(t, MeshKit.RoundedBox(new Vector3(0.06f, 0.2f, 0.16f), 0.03f), new Vector3(s * 1.1f, 1.85f, 1.25f), trim, false);   // mirrors

            var d = Destructible.Add(t.gameObject, "van");
            d.FirePoint = new Vector3(0f, 0.8f, 1.6f);
            d.FireSize = 1.6f;

            var ws = new GameObject("Windscreen").transform;
            ws.SetParent(t, false);
            ws.localPosition = new Vector3(0f, 1.95f, 1.93f);
            ws.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            Part(ws, MeshKit.RoundedBox(new Vector3(1.78f, 0.7f, 0.03f), 0.012f), Vector3.zero, Mats.Glass(new Color(0.15f, 0.2f, 0.24f, 0.35f)), true);
            Destructible.Add(ws.gameObject, "window").FirePoint = Vector3.zero;

            Wheels(t, d, 0.37f, 0.24f, 0.93f, 1.62f, -1.62f);
            for (int s = -1; s <= 1; s += 2)
            {
                var door = new GameObject("Door").transform;
                door.SetParent(t, false);
                door.localPosition = new Vector3(s * 1.005f, 1.45f, 1.25f);
                Part(door, MeshKit.RoundedBox(new Vector3(0.05f, 1.15f, 0.95f), 0.02f), Vector3.zero, paint, true);
                Part(door, MeshKit.RoundedBox(new Vector3(0.02f, 0.45f, 0.75f), 0.01f), new Vector3(s * 0.02f, 0.33f, 0.02f), glass, false);
                d.Parts.Add(door);
            }
            return d;
        }

        /// <summary>Family sedan, 4.6 × 1.8 × 1.45 m.</summary>
        static Destructible Sedan(Transform t, int variant)
        {
            var paint = Mats.Surface("paint", CarPaint[variant % CarPaint.Length], 0.55f, 0.9f);
            var trim = Mats.Lit(new Color(0.06f, 0.06f, 0.07f), 0.1f, 0.35f);
            Part(t, MeshKit.RoundedBox(new Vector3(1.8f, 0.72f, 4.6f), 0.22f, 4, new Vector2(0.97f, 0.97f)), new Vector3(0f, 0.68f, 0f), paint, true);
            Part(t, MeshKit.RoundedBox(new Vector3(1.6f, 0.58f, 2.45f), 0.18f, 4, new Vector2(0.84f, 0.6f)), new Vector3(0f, 1.3f, -0.2f), Mats.Glass(new Color(0.06f, 0.08f, 0.1f, 0.8f)), true);
            Part(t, MeshKit.RoundedBox(new Vector3(1.36f, 0.07f, 1.45f), 0.03f), new Vector3(0f, 1.6f, -0.3f), paint, false);
            Part(t, MeshKit.RoundedBox(new Vector3(1.86f, 0.22f, 0.2f), 0.09f), new Vector3(0f, 0.45f, 2.27f), trim, false);
            Part(t, MeshKit.RoundedBox(new Vector3(1.86f, 0.22f, 0.2f), 0.09f), new Vector3(0f, 0.45f, -2.27f), trim, false);
            for (int s = -1; s <= 1; s += 2)
            {
                Part(t, MeshKit.RoundedBox(new Vector3(0.42f, 0.12f, 0.06f), 0.04f), new Vector3(s * 0.6f, 0.82f, 2.29f), Mats.Emissive(new Color(0.9f, 0.9f, 0.85f), new Color(0.35f, 0.34f, 0.3f)), false);
                Part(t, MeshKit.RoundedBox(new Vector3(0.42f, 0.12f, 0.06f), 0.04f), new Vector3(s * 0.6f, 0.86f, -2.29f), Mats.Emissive(new Color(0.5f, 0.04f, 0.03f), new Color(0.35f, 0.01f, 0.01f)), false);
            }
            var d = Destructible.Add(t.gameObject, "sedan");
            d.FirePoint = new Vector3(0f, 0.6f, 1.5f);
            d.FireSize = 1.4f;
            Wheels(t, d, 0.33f, 0.22f, 0.8f, 1.42f, -1.38f);
            return d;
        }

        static void Wheels(Transform t, Destructible d, float r, float w, float x, float zf, float zr)
        {
            var tyre = Mats.Surface("tyre", Color.white, 0f, 1f, 0.5f);
            var rim = Mats.Lit(new Color(0.7f, 0.71f, 0.72f), 0.9f, 0.6f);
            for (int i = 0; i < 4; i++)
            {
                float s = i % 2 == 0 ? -1f : 1f;
                var wheel = new GameObject("Wheel").transform;
                wheel.SetParent(t, false);
                wheel.localPosition = new Vector3(s * x, r, i < 2 ? zf : zr);
                wheel.localRotation = Quaternion.Euler(0f, 0f, 90f);   // lathe Y onto the axle
                var tg = Part(wheel, MeshKit.Tyre(r, w), Vector3.zero, tyre, false);
                var col = tg.AddComponent<CapsuleCollider>();
                col.direction = 0;
                col.radius = r;
                col.height = r * 2f;
                var rg = Part(wheel, MeshKit.Rim(r * 0.62f, w * 0.4f), new Vector3(0f, -s * w * 0.42f, 0f), rim, false);
                rg.transform.localRotation = Quaternion.Euler(s > 0 ? 180f : 0f, 0f, 0f);
                d.Parts.Add(wheel);
            }
        }

        // ---------- street furniture ----------

        static readonly Color[] BinColours = { new Color(0.1f, 0.26f, 0.14f), new Color(0.12f, 0.18f, 0.32f), new Color(0.22f, 0.22f, 0.23f) };

        /// <summary>Wheelie bin, 0.58 × 1.07 × 0.72 m, wider at the top, with lid and wheels.</summary>
        static Destructible Bin(Transform t, int variant)
        {
            var plastic = Mats.Surface("paint", BinColours[variant % BinColours.Length], 0f, 0.45f, 1f, 0.5f);
            Part(t, MeshKit.RoundedBox(new Vector3(0.56f, 0.98f, 0.68f), 0.04f, 2, new Vector2(1.07f, 1.08f)), new Vector3(0f, 0.54f, 0f), plastic, true);
            var lid = new GameObject("Lid").transform;
            lid.SetParent(t, false);
            lid.localPosition = new Vector3(0f, 1.06f, 0f);
            Part(lid, MeshKit.RoundedBox(new Vector3(0.64f, 0.06f, 0.8f), 0.03f), Vector3.zero, plastic, true);
            var tyre = Mats.Lit(Mats.Rubber, 0f, 0.3f);
            for (int s = -1; s <= 1; s += 2)
            {
                var w = Part(t, MeshKit.Tyre(0.1f, 0.05f), new Vector3(s * 0.3f, 0.1f, -0.34f), tyre, false);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.mass = 15f;
            var d = Destructible.Add(t.gameObject, "bin");
            d.FirePoint = new Vector3(0f, 0.55f, 0f);
            d.FireSize = 0.6f;
            d.Parts.Add(lid);
            return d;
        }

        /// <summary>Kerbside mailbox: arched steel box on a timber post.</summary>
        static Destructible Mailbox(Transform t)
        {
            Part(t, MeshKit.RoundedBox(new Vector3(0.1f, 1.15f, 0.1f), 0.01f), new Vector3(0f, 0.575f, 0f), Mats.Surface("wood", new Color(0.8f, 0.75f, 0.7f), 0f, 0.5f), true);
            Part(t, MeshKit.RoundedBox(new Vector3(0.28f, 0.3f, 0.5f), 0.12f), new Vector3(0f, 1.3f, 0f), Mats.Surface("paint", new Color(0.16f, 0.17f, 0.2f), 0.6f, 0.7f), true);
            var flag = Part(t, MeshKit.RoundedBox(new Vector3(0.02f, 0.2f, 0.07f), 0.005f), new Vector3(0.15f, 1.42f, 0.1f), Mats.Lit(new Color(0.75f, 0.08f, 0.06f), 0.3f, 0.5f), false);
            flag.name = "Flag";
            var d = Destructible.Add(t.gameObject, "mailbox");
            d.FirePoint = new Vector3(0f, 1.3f, 0f);
            d.FireSize = 0.4f;
            return d;
        }

        /// <summary>Outdoor AC unit with a fan grille, on wall brackets.</summary>
        static Destructible AcUnit(Transform t)
        {
            var body = Mats.Surface("paint", new Color(0.86f, 0.85f, 0.8f), 0.2f, 0.55f);
            Part(t, MeshKit.RoundedBox(new Vector3(0.86f, 0.6f, 0.32f), 0.025f), new Vector3(0f, 0f, -0.18f), body, true);
            var fan = Part(t, MeshKit.Lathe("acfan", new[] { new Vector2(0.22f, 0f), new Vector2(0.2f, 0.01f), new Vector2(0.04f, 0.02f), new Vector2(0f, 0.025f) }, 24), new Vector3(0.15f, 0f, -0.345f), Mats.Lit(new Color(0.12f, 0.12f, 0.13f), 0.5f, 0.3f), false);
            fan.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            for (int i = 0; i < 7; i++)
                Part(t, MeshKit.RoundedBox(new Vector3(0.015f, 0.5f, 0.01f), 0.004f), new Vector3(-0.3f + i * 0.03f, 0f, -0.345f), Mats.Lit(new Color(0.5f, 0.5f, 0.5f), 0.6f, 0.4f), false);
            Part(t, MeshKit.RoundedBox(new Vector3(0.9f, 0.04f, 0.38f), 0.01f), new Vector3(0f, -0.33f, -0.2f), Mats.Surface("rust", Color.white, 0.5f), false);
            var d = Destructible.Add(t.gameObject, "acunit");
            d.FirePoint = new Vector3(0f, 0f, -0.18f);
            d.FireSize = 0.5f;
            return d;
        }

        static Destructible Bicycle(Transform t)
        {
            var frame = Mats.Lit(new Color(0.6f, 0.1f, 0.1f), 0.7f, 0.65f);
            var steel = Mats.Lit(Mats.Steel, 0.9f, 0.6f);
            var tyre = Mats.Lit(Mats.Rubber, 0f, 0.3f);
            t.localRotation *= Quaternion.Euler(0f, 0f, -11f);   // leaning
            for (int i = 0; i < 2; i++)
            {
                var w = Part(t, MeshKit.Tyre(0.34f, 0.04f), new Vector3(0f, 0.34f, i == 0 ? 0.52f : -0.52f), tyre, false);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var r = Part(t, MeshKit.Rim(0.3f, 0.01f), new Vector3(0.01f, 0.34f, i == 0 ? 0.52f : -0.52f), steel, false);
                r.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            Tube(t, new Vector3(0f, 0.34f, -0.52f), new Vector3(0f, 0.55f, 0.02f), 0.018f, frame);
            Tube(t, new Vector3(0f, 0.55f, 0.02f), new Vector3(0f, 0.86f, -0.3f), 0.018f, frame);
            Tube(t, new Vector3(0f, 0.86f, -0.3f), new Vector3(0f, 0.92f, 0.38f), 0.02f, frame);
            Tube(t, new Vector3(0f, 0.55f, 0.02f), new Vector3(0f, 0.92f, 0.38f), 0.022f, frame);
            Tube(t, new Vector3(0f, 0.34f, 0.52f), new Vector3(0f, 1.02f, 0.4f), 0.016f, steel);
            Tube(t, new Vector3(0f, 0.86f, -0.3f), new Vector3(0f, 0.34f, -0.52f), 0.012f, frame);
            Part(t, MeshKit.RoundedBox(new Vector3(0.56f, 0.025f, 0.025f), 0.012f), new Vector3(0f, 1.02f, 0.4f), steel, false);
            Part(t, MeshKit.RoundedBox(new Vector3(0.12f, 0.05f, 0.26f), 0.025f), new Vector3(0f, 0.95f, -0.33f), tyre, false);
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.55f, 0f);
            box.size = new Vector3(0.5f, 1.05f, 1.75f);
            var d = Destructible.Add(t.gameObject, "bicycle");
            d.FirePoint = new Vector3(0f, 0.5f, 0f);
            d.FireSize = 0.5f;
            return d;
        }

        /// <summary>Stack of six EUR pallets (1.2 × 0.8 m), each 3 stringers and 5 deck boards.</summary>
        static Destructible Pallets(Transform t)
        {
            for (int p = 0; p < 6; p++)
            {
                var wood = Mats.Surface("wood", Color.Lerp(new Color(0.92f, 0.85f, 0.75f), new Color(0.7f, 0.62f, 0.52f), (p % 3) / 2f), 0f, 0.8f, 0.6f);
                float y = p * 0.144f;
                float twist = (p % 2) * 3f - 1.5f;
                var pal = new GameObject("Pallet").transform;
                pal.SetParent(t, false);
                pal.localPosition = new Vector3((p % 2) * 0.03f, y, 0f);
                pal.localRotation = Quaternion.Euler(0f, twist, 0f);
                for (int i = 0; i < 3; i++) Part(pal, MeshKit.RoundedBox(new Vector3(0.1f, 0.078f, 0.8f), 0.008f, 1), new Vector3(-0.55f + i * 0.55f, 0.039f, 0f), wood, false);
                for (int i = 0; i < 5; i++) Part(pal, MeshKit.RoundedBox(new Vector3(1.2f, 0.022f, 0.12f), 0.006f, 1), new Vector3(0f, 0.089f, -0.34f + i * 0.17f), wood, false);
                for (int i = 0; i < 3; i++) Part(pal, MeshKit.RoundedBox(new Vector3(1.2f, 0.022f, 0.1f), 0.006f, 1), new Vector3(0f, 0.011f, -0.35f + i * 0.35f), wood, false);
            }
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.44f, 0f);
            box.size = new Vector3(1.25f, 0.87f, 0.85f);
            var d = Destructible.Add(t.gameObject, "pallets");
            d.FirePoint = new Vector3(0f, 0.45f, 0f);
            d.FireSize = 1.1f;
            return d;
        }

        /// <summary>12 kg gas cylinder: turned steel body, collar and valve.</summary>
        static Destructible GasCylinder(Transform t)
        {
            var prof = new[] { new Vector2(0.13f, 0f), new Vector2(0.15f, 0.04f), new Vector2(0.15f, 0.48f), new Vector2(0.12f, 0.56f), new Vector2(0.05f, 0.6f), new Vector2(0f, 0.6f) };
            Part(t, MeshKit.Lathe("gascyl", prof, 28, false, true), Vector3.zero, Mats.Surface("paint", new Color(0.72f, 0.16f, 0.1f), 0.3f, 0.7f), false);
            Part(t, MeshKit.Lathe("gascollar", new[] { new Vector2(0.075f, 0.58f), new Vector2(0.075f, 0.68f), new Vector2(0.065f, 0.7f) }, 16), Vector3.zero, Mats.Lit(Mats.Steel, 0.9f, 0.5f), false);
            Part(t, MeshKit.RoundedBox(new Vector3(0.04f, 0.06f, 0.04f), 0.01f), new Vector3(0f, 0.65f, 0f), Mats.Lit(new Color(0.7f, 0.6f, 0.3f), 1f, 0.6f), false);
            var col = t.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.32f, 0f);
            col.radius = 0.15f;
            col.height = 0.66f;
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.mass = 25f;
            var d = Destructible.Add(t.gameObject, "gascyl");
            d.FirePoint = new Vector3(0f, 0.35f, 0f);
            d.FireSize = 0.6f;
            return d;
        }

        /// <summary>200 l oil drum with its two rolling hoops.</summary>
        static Destructible Barrel(Transform t, int variant)
        {
            var c = variant % 2 == 0 ? new Color(0.1f, 0.22f, 0.4f) : new Color(0.55f, 0.1f, 0.08f);
            var prof = new[] { new Vector2(0.27f, 0f), new Vector2(0.29f, 0.02f), new Vector2(0.29f, 0.86f), new Vector2(0.27f, 0.88f) };
            Part(t, MeshKit.Lathe("barrel", prof, 28, true, true), Vector3.zero, Mats.Surface("paint", c, 0.5f, 0.6f), false);
            for (int i = 0; i < 2; i++)
                Part(t, MeshKit.Lathe("hoop", new[] { new Vector2(0.29f, -0.015f), new Vector2(0.3f, 0f), new Vector2(0.29f, 0.015f) }, 28), new Vector3(0f, 0.3f + i * 0.28f, 0f), Mats.Surface("rust", Color.white, 0.5f), false);
            var col = t.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.44f, 0f);
            col.radius = 0.29f;
            col.height = 0.88f;
            var box = t.gameObject.AddComponent<BoxCollider>();   // flat bottom so it stands
            box.center = new Vector3(0f, 0.44f, 0f);
            box.size = new Vector3(0.5f, 0.88f, 0.5f);
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.mass = 60f;
            var d = Destructible.Add(t.gameObject, "barrel");
            d.FirePoint = new Vector3(0f, 0.45f, 0f);
            d.FireSize = 0.8f;
            return d;
        }

        /// <summary>3 m of galvanised chain-link fence with posts and a top rail.</summary>
        static Destructible Fence(Transform t)
        {
            var steel = Mats.Lit(new Color(0.6f, 0.62f, 0.63f), 0.85f, 0.5f);
            Part(t, MeshKit.Lathe("fencepost", new[] { new Vector2(0.03f, 0f), new Vector2(0.03f, 1.85f), new Vector2(0.034f, 1.88f), new Vector2(0f, 1.9f) }, 12), new Vector3(0f, 0f, -1.5f), steel, false);
            Part(t, MeshKit.RoundedBox(new Vector3(0.035f, 0.035f, 3f), 0.017f, 2), new Vector3(0f, 1.8f, 0f), steel, false);
            Part(t, MeshKit.RoundedBox(new Vector3(0.01f, 1.75f, 3f), 0.004f, 1), new Vector3(0f, 0.92f, 0f), Mats.Mesh("chainlink", Color.white), true);
            var d = Destructible.Add(t.gameObject, "fence");
            d.FirePoint = new Vector3(0f, 0.9f, 0f);
            d.FireSize = 0.5f;
            return d;
        }

        static Destructible Crate(Transform t)
        {
            var wood = Mats.Surface("wood", new Color(0.85f, 0.75f, 0.6f), 0f, 0.6f, 0.5f);
            Part(t, MeshKit.RoundedBox(new Vector3(0.9f, 0.9f, 0.9f), 0.02f), new Vector3(0f, 0.45f, 0f), wood, true);
            var d = Destructible.Add(t.gameObject, "crate");
            d.FirePoint = new Vector3(0f, 0.45f, 0f);
            return d;
        }

        // ---------- helpers ----------

        public static GameObject Part(Transform parent, Mesh mesh, Vector3 localPos, UnityEngine.Material mat, bool collider)
        {
            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (collider)
            {
                var b = go.AddComponent<BoxCollider>();
                b.center = mesh.bounds.center;
                b.size = mesh.bounds.size;
            }
            return go;
        }

        /// <summary>A round tube between two local points.</summary>
        public static GameObject Tube(Transform parent, Vector3 a, Vector3 b, float radius, UnityEngine.Material mat)
        {
            float len = Vector3.Distance(a, b);
            var mesh = MeshKit.RoundedBox(new Vector3(radius * 2f, len, radius * 2f), radius * 0.99f, 3);
            var go = Part(parent, mesh, (a + b) * 0.5f, mat, false);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            return go;
        }
    }
}
