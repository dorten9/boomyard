using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>A built arena: its objects, where the player starts and the 6 slots for the player's own yard props.</summary>
    public sealed class Arena
    {
        public ArenaDef Def;
        public GameObject Root;
        public Vector3 Spawn;
        public float SpawnYaw;
        public readonly List<Destructible> Objects = new List<Destructible>();
        public readonly List<Vector3> PropSlots = new List<Vector3>();
    }

    /// <summary>
    /// Builds the arenas from primitives and code-made PBR materials (spec §10). The base has arena 1, the backyard:
    /// bins, a mailbox, an AC unit, windows, a bicycle, an old van and pallets, at night under street lamps.
    /// Art passes later swap the primitives for the real models; positions and data stay.
    /// </summary>
    public static class ArenaBuilder
    {
        public static Arena Build(int number, QualityTier tier)
        {
            var a = new Arena { Def = Catalog.Arena(number) };
            a.Root = new GameObject("Arena " + a.Def.Id);
            Night(a.Root.transform, tier);
            Backyard(a);
            foreach (var d in a.Root.GetComponentsInChildren<Destructible>()) a.Objects.Add(d);
            return a;
        }

        // ---------- lighting ----------

        static void Night(Transform root, QualityTier tier)
        {
            var sky = new UnityEngine.Material(Shader.Find("BoomYard/Sky"));
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.1f, 0.11f, 0.16f);
            RenderSettings.ambientEquatorColor = new Color(0.09f, 0.075f, 0.07f);
            RenderSettings.ambientGroundColor = new Color(0.03f, 0.03f, 0.03f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.09f, 0.08f, 0.085f);
            RenderSettings.fogDensity = 0.022f;
            RenderSettings.reflectionIntensity = 0.5f;

            var moon = new GameObject("Moon").AddComponent<Light>();
            moon.transform.SetParent(root, false);
            moon.type = LightType.Directional;
            moon.color = new Color(0.55f, 0.62f, 0.8f);
            moon.intensity = 0.28f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.8f;
            moon.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = moon;

            Lamp(root, new Vector3(-8.5f, 0f, -3f), tier >= QualityTier.Medium);
            Lamp(root, new Vector3(9f, 0f, 1f), tier >= QualityTier.High);
            // Porch light over the door.
            var porch = new GameObject("Porch").AddComponent<Light>();
            porch.transform.SetParent(root, false);
            porch.transform.position = new Vector3(-4.5f, 2.6f, 11.4f);
            porch.type = LightType.Point;
            porch.color = new Color(1f, 0.72f, 0.45f);
            porch.range = 7f;
            porch.intensity = 1.3f;
        }

        static void Lamp(Transform root, Vector3 at, bool shadows)
        {
            var lamp = new GameObject("StreetLamp").transform;
            lamp.SetParent(root, false);
            lamp.position = at;
            Box(lamp, "Pole", new Vector3(0f, 2.6f, 0f), new Vector3(0.14f, 5.2f, 0.14f), Mats.Lit(Mats.DarkMetal, 0.6f, 0.4f));
            Box(lamp, "Arm", new Vector3(0.6f, 5.15f, 0f), new Vector3(1.3f, 0.1f, 0.12f), Mats.Lit(Mats.DarkMetal, 0.6f, 0.4f));
            Box(lamp, "Head", new Vector3(1.15f, 5.05f, 0f), new Vector3(0.5f, 0.12f, 0.28f), Mats.Emissive(new Color(1f, 0.85f, 0.6f), new Color(2.2f, 1.6f, 0.9f)), false);
            var l = new GameObject("Light").AddComponent<Light>();
            l.transform.SetParent(lamp, false);
            l.transform.localPosition = new Vector3(1.15f, 4.95f, 0f);
            l.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            l.type = LightType.Spot;
            l.spotAngle = 110f;
            l.range = 16f;
            l.intensity = 2.4f;
            l.color = new Color(1f, 0.8f, 0.55f);
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        // ---------- the backyard ----------

        static void Backyard(Arena a)
        {
            var root = a.Root.transform;
            a.Spawn = new Vector3(0f, 0f, -7f);
            a.SpawnYaw = 8f;

            // Ground: asphalt yard with a strip of dead grass and the driveway.
            Box(root, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(90f, 1f, 90f), Mats.Lit(Mats.Asphalt, 0f, 0.35f));
            Box(root, "Grass", new Vector3(-7f, 0.005f, 5f), new Vector3(8f, 0.01f, 12f), Mats.Lit(Mats.Grass, 0f, 0.1f), false);
            Box(root, "Curb", new Vector3(0f, 0.06f, -12f), new Vector3(40f, 0.12f, 0.3f), Mats.Lit(new Color(0.4f, 0.39f, 0.37f), 0f, 0.2f));

            House(root);
            Neighbourhood(root);

            Van(root, new Vector3(3.6f, 0f, 3.2f), 68f);
            Bin(root, new Vector3(-5.2f, 0f, 9.6f));
            Bin(root, new Vector3(-3.9f, 0f, 9.9f));
            Mailbox(root, new Vector3(-6.6f, 0f, -3.4f));
            AcUnit(root, new Vector3(5.8f, 1.1f, 11.78f));
            Bicycle(root, new Vector3(-10.3f, 0f, 2.2f));
            Pallets(root, new Vector3(-6.6f, 0f, 4.6f));
            for (int i = 0; i < 4; i++) Fence(root, new Vector3(-11f, 0f, -4.5f + i * 3f));

            // The 6 slots where the player's own props from "my yard" appear (spec §11).
            var slots = new[] { new Vector3(7.5f, 0f, -2f), new Vector3(-2f, 0f, 4.5f), new Vector3(8.5f, 0f, 8f), new Vector3(-8.5f, 0f, -7f), new Vector3(1.5f, 0f, 9.5f), new Vector3(10f, 0f, 3.5f) };
            foreach (var s in slots)
            {
                a.PropSlots.Add(s);
                var m = Box(root, "PropSlot", s + Vector3.up * 0.004f, new Vector3(2.2f, 0.008f, 2.2f), Mats.Lit(new Color(0.2f, 0.18f, 0.17f), 0f, 0.15f), false);
                m.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void House(Transform root)
        {
            var house = new GameObject("House").transform;
            house.SetParent(root, false);
            var plaster = Mats.Lit(Mats.Plaster, 0f, 0.15f);
            var trim = Mats.Lit(new Color(0.25f, 0.23f, 0.22f), 0f, 0.3f);
            Box(house, "Walls", new Vector3(0f, 3f, 16f), new Vector3(16f, 6f, 8f), plaster);
            Box(house, "Parapet", new Vector3(0f, 6.15f, 12.1f), new Vector3(16.2f, 0.3f, 0.3f), trim);
            Box(house, "Plinth", new Vector3(0f, 0.2f, 11.95f), new Vector3(16f, 0.4f, 0.12f), trim);
            Box(house, "Door", new Vector3(-4.5f, 1.1f, 11.96f), new Vector3(1.05f, 2.2f, 0.1f), Mats.Lit(new Color(0.22f, 0.13f, 0.08f), 0f, 0.35f));
            Box(house, "Step", new Vector3(-4.5f, 0.08f, 11.5f), new Vector3(1.6f, 0.16f, 0.9f), trim);

            // 10 windows: two floors of five (spec arena 1). Some are lit from inside.
            float[] xs = { -6.5f, -2f, 0.5f, 3f, 6.5f };
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < xs.Length; i++)
                    Window(house, new Vector3(xs[i], row == 0 ? 1.6f : 4.3f, 11.97f), new Vector2(1.1f, 1.25f), (i * 7 + row * 2) % 3 == 0);
        }

        static void Window(Transform parent, Vector3 pos, Vector2 size, bool lit)
        {
            var frame = Mats.Lit(new Color(0.85f, 0.83f, 0.78f), 0f, 0.4f);
            var interior = lit ? Mats.Emissive(new Color(0.3f, 0.22f, 0.14f), new Color(0.9f, 0.55f, 0.25f)) : Mats.Lit(new Color(0.05f, 0.05f, 0.06f), 0f, 0.1f);
            // Frame and the room behind stay; the glass is what breaks.
            Box(parent, "Interior", pos + new Vector3(0f, 0f, 0.025f), new Vector3(size.x, size.y, 0.01f), interior, false);
            Box(parent, "FrameTop", pos + new Vector3(0f, size.y * 0.5f + 0.04f, -0.01f), new Vector3(size.x + 0.16f, 0.08f, 0.06f), frame, false);
            Box(parent, "FrameBottom", pos + new Vector3(0f, -size.y * 0.5f - 0.05f, -0.03f), new Vector3(size.x + 0.22f, 0.1f, 0.12f), frame, false);
            Box(parent, "FrameMid", pos + new Vector3(0f, 0f, -0.012f), new Vector3(0.05f, size.y, 0.04f), frame, false);

            var w = new GameObject("Window");
            w.transform.SetParent(parent, false);
            w.transform.position = pos;
            Box(w.transform, "Glass", Vector3.zero, new Vector3(size.x, size.y, 0.02f), Mats.Glass(Mats.GlassTint));
            var d = Destructible.Add(w, "window");
            d.FirePoint = Vector3.zero;
            d.FireSize = 0.5f;
        }

        static void Neighbourhood(Transform root)
        {
            var dark = Mats.Lit(new Color(0.11f, 0.105f, 0.115f), 0f, 0.1f);
            var warm = Mats.Emissive(new Color(0.25f, 0.18f, 0.1f), new Color(0.8f, 0.45f, 0.18f));
            var rng = new System.Random(4);
            for (int i = 0; i < 9; i++)
            {
                float x = -40f + i * 10f + (float)rng.NextDouble() * 3f;
                float z = 34f + (float)rng.NextDouble() * 8f;
                float h = 6f + (float)rng.NextDouble() * 10f;
                var b = Box(root, "Building", new Vector3(x, h * 0.5f, z), new Vector3(7f + (float)rng.NextDouble() * 4f, h, 6f), dark);
                for (int k = 0; k < 3; k++)
                    if (rng.NextDouble() < 0.6)
                        Box(b.transform.parent, "Lit", new Vector3(x - 2f + k * 2f, 1.5f + (float)rng.NextDouble() * (h - 3f), z - 3.02f), new Vector3(0.8f, 1f, 0.02f), warm, false);
            }
            // Trees along the sides.
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var p = new Vector3(side * (16f + (float)rng.NextDouble() * 4f), 0f, -10f + i * 6f);
                Box(root, "Trunk", p + Vector3.up * 1.5f, new Vector3(0.35f, 3f, 0.35f), Mats.Lit(new Color(0.16f, 0.12f, 0.09f), 0f, 0.1f));
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Crown";
                crown.transform.SetParent(root, false);
                crown.transform.position = p + Vector3.up * 4f;
                crown.transform.localScale = new Vector3(3.5f, 3f, 3.5f);
                crown.GetComponent<Renderer>().sharedMaterial = Mats.Lit(new Color(0.07f, 0.09f, 0.06f), 0f, 0.05f);
            }
        }

        /// <summary>The old van: body, cab, glass, and six parts that fly off first (4 wheels, 2 doors).</summary>
        static void Van(Transform parent, Vector3 pos, float yaw)
        {
            var van = new GameObject("Van");
            van.transform.SetParent(parent, false);
            van.transform.position = pos;
            van.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var t = van.transform;
            var paint = Mats.Lit(Mats.VanWhite, 0.4f, 0.55f);
            var black = Mats.Lit(new Color(0.07f, 0.07f, 0.08f), 0.2f, 0.3f);
            Box(t, "Body", new Vector3(0f, 1.3f, -0.4f), new Vector3(2f, 1.75f, 4.2f), paint);
            Box(t, "Nose", new Vector3(0f, 0.88f, 2.1f), new Vector3(1.95f, 0.9f, 0.8f), paint);
            Box(t, "Bumper", new Vector3(0f, 0.5f, 2.52f), new Vector3(2f, 0.22f, 0.12f), black);
            Box(t, "RearBumper", new Vector3(0f, 0.5f, -2.53f), new Vector3(2f, 0.22f, 0.12f), black);
            Box(t, "Stripe", new Vector3(0f, 1.05f, -0.4f), new Vector3(2.02f, 0.12f, 4.1f), Mats.Lit(new Color(0.6f, 0.15f, 0.1f), 0.3f, 0.5f), false);
            Box(t, "HeadL", new Vector3(-0.72f, 1f, 2.505f), new Vector3(0.32f, 0.16f, 0.02f), Mats.Emissive(Color.white, new Color(0.3f, 0.3f, 0.28f)), false);
            Box(t, "HeadR", new Vector3(0.72f, 1f, 2.505f), new Vector3(0.32f, 0.16f, 0.02f), Mats.Emissive(Color.white, new Color(0.3f, 0.3f, 0.28f)), false);
            Box(t, "TailL", new Vector3(-0.85f, 1.1f, -2.51f), new Vector3(0.14f, 0.32f, 0.02f), Mats.Emissive(new Color(0.5f, 0.05f, 0.05f), new Color(0.25f, 0.01f, 0.01f)), false);
            Box(t, "TailR", new Vector3(0.85f, 1.1f, -2.51f), new Vector3(0.14f, 0.32f, 0.02f), Mats.Emissive(new Color(0.5f, 0.05f, 0.05f), new Color(0.25f, 0.01f, 0.01f)), false);

            var d = Destructible.Add(van, "van");
            d.FirePoint = new Vector3(0f, 0.9f, 1.4f);   // fuel tank / engine
            d.FireSize = 1.6f;

            // The windscreen is its own pane of glass: the blast wave breaks it before the van goes.
            var ws = new GameObject("Windscreen");
            ws.transform.SetParent(t, false);
            ws.transform.localPosition = new Vector3(0f, 1.75f, 1.71f);
            ws.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            Box(ws.transform, "Glass", Vector3.zero, new Vector3(1.8f, 0.72f, 0.03f), Mats.Glass(new Color(0.25f, 0.32f, 0.38f, 0.5f)));
            var wd = Destructible.Add(ws, "window");
            wd.FirePoint = Vector3.zero;

            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.93f : 0.93f, z = i < 2 ? 1.65f : -1.55f;
                var wheel = Cylinder(t, "Wheel", new Vector3(x, 0.36f, z), new Vector3(0.72f, 0.12f, 0.72f), Quaternion.Euler(0f, 0f, 90f), Mats.Lit(Mats.Rubber, 0f, 0.25f));
                Cylinder(wheel.transform, "Hub", new Vector3(0f, i % 2 == 0 ? -1.02f : 1.02f, 0f), new Vector3(0.5f, 0.05f, 0.5f), Quaternion.identity, Mats.Lit(Mats.Steel, 0.9f, 0.5f), false);
                d.Parts.Add(wheel.transform);
            }
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -1.015f : 1.015f;
                var door = Box(t, "Door", new Vector3(x, 1.35f, 1.15f), new Vector3(0.05f, 1.15f, 0.95f), paint);
                Box(door.transform, "DoorGlass", new Vector3(0f, 0.3f, 0f), new Vector3(1.2f, 0.4f, 0.8f), Mats.Glass(new Color(0.2f, 0.25f, 0.3f, 0.6f)), false);
                d.Parts.Add(door.transform);
            }
        }

        static void Bin(Transform parent, Vector3 pos)
        {
            var bin = new GameObject("Bin");
            bin.transform.SetParent(parent, false);
            bin.transform.position = pos;
            var green = Mats.Lit(new Color(0.12f, 0.24f, 0.16f), 0.2f, 0.35f);
            FlatBottom(Cylinder(bin.transform, "Can", new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 0.5f, 0.6f), Quaternion.identity, Mats.Lit(Mats.Steel, 0.85f, 0.45f)));
            var lid = Cylinder(bin.transform, "Lid", new Vector3(0f, 1.02f, 0f), new Vector3(0.66f, 0.03f, 0.66f), Quaternion.identity, green);
            FlatBottom(lid);
            var rb = bin.AddComponent<Rigidbody>();
            rb.mass = 15f;
            var d = Destructible.Add(bin, "bin");
            d.FirePoint = new Vector3(0f, 0.5f, 0f);
            d.FireSize = 0.6f;
            d.Parts.Add(lid.transform);
        }

        static void Mailbox(Transform parent, Vector3 pos)
        {
            var mb = new GameObject("Mailbox");
            mb.transform.SetParent(parent, false);
            mb.transform.position = pos;
            Cylinder(mb.transform, "Post", new Vector3(0f, 0.6f, 0f), new Vector3(0.09f, 0.6f, 0.09f), Quaternion.identity, Mats.Lit(Mats.Wood, 0f, 0.2f));
            Box(mb.transform, "Box", new Vector3(0f, 1.35f, 0f), new Vector3(0.42f, 0.34f, 0.55f), Mats.Lit(new Color(0.18f, 0.2f, 0.32f), 0.7f, 0.5f));
            Box(mb.transform, "Flag", new Vector3(0.23f, 1.45f, 0.1f), new Vector3(0.02f, 0.2f, 0.06f), Mats.Lit(new Color(0.7f, 0.1f, 0.08f), 0.3f, 0.4f), false);
            var d = Destructible.Add(mb, "mailbox");
            d.FirePoint = new Vector3(0f, 1.35f, 0f);
            d.FireSize = 0.4f;
        }

        static void AcUnit(Transform parent, Vector3 pos)
        {
            var ac = new GameObject("AcUnit");
            ac.transform.SetParent(parent, false);
            ac.transform.position = pos;
            Box(ac.transform, "Case", new Vector3(0f, 0f, -0.2f), new Vector3(0.9f, 0.62f, 0.34f), Mats.Lit(new Color(0.78f, 0.78f, 0.75f), 0.3f, 0.4f));
            Cylinder(ac.transform, "Fan", new Vector3(0.15f, 0f, -0.375f), new Vector3(0.42f, 0.01f, 0.42f), Quaternion.Euler(90f, 0f, 0f), Mats.Lit(new Color(0.15f, 0.15f, 0.16f), 0.4f, 0.3f), false);
            Box(ac.transform, "Bracket", new Vector3(0f, -0.36f, -0.2f), new Vector3(0.95f, 0.04f, 0.38f), Mats.Lit(Mats.Rust, 0.6f, 0.2f), false);
            var d = Destructible.Add(ac, "acunit");
            d.FirePoint = new Vector3(0f, 0f, -0.2f);
            d.FireSize = 0.5f;
        }

        static void Bicycle(Transform parent, Vector3 pos)
        {
            var bike = new GameObject("Bicycle");
            bike.transform.SetParent(parent, false);
            bike.transform.position = pos;
            bike.transform.rotation = Quaternion.Euler(0f, 0f, -12f);   // leaning on the fence
            var frame = Mats.Lit(new Color(0.55f, 0.1f, 0.1f), 0.6f, 0.5f);
            var tyre = Mats.Lit(Mats.Rubber, 0f, 0.2f);
            Cylinder(bike.transform, "WheelF", new Vector3(0f, 0.34f, 0.52f), new Vector3(0.68f, 0.015f, 0.68f), Quaternion.Euler(0f, 0f, 90f), tyre);
            Cylinder(bike.transform, "WheelB", new Vector3(0f, 0.34f, -0.52f), new Vector3(0.68f, 0.015f, 0.68f), Quaternion.Euler(0f, 0f, 90f), tyre);
            Box(bike.transform, "Top", new Vector3(0f, 0.78f, 0f), new Vector3(0.035f, 0.035f, 0.9f), frame, false);
            var down = Box(bike.transform, "Down", new Vector3(0f, 0.55f, 0.18f), new Vector3(0.035f, 0.035f, 0.75f), frame, false);
            down.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);
            Box(bike.transform, "Seat", new Vector3(0f, 0.92f, -0.38f), new Vector3(0.12f, 0.05f, 0.25f), tyre, false);
            Box(bike.transform, "Bars", new Vector3(0f, 0.98f, 0.45f), new Vector3(0.55f, 0.03f, 0.03f), Mats.Lit(Mats.Steel, 0.9f, 0.5f), false);
            var d = Destructible.Add(bike, "bicycle");
            d.FirePoint = new Vector3(0f, 0.5f, 0f);
            d.FireSize = 0.5f;
        }

        static void Pallets(Transform parent, Vector3 pos)
        {
            var stack = new GameObject("Pallets");
            stack.transform.SetParent(parent, false);
            stack.transform.position = pos;
            stack.transform.rotation = Quaternion.Euler(0f, 17f, 0f);
            for (int i = 0; i < 6; i++)
            {
                var wood = Mats.Lit(Color.Lerp(Mats.Wood, new Color(0.35f, 0.27f, 0.18f), (i % 3) / 2f), 0f, 0.15f);
                var p = Box(stack.transform, "Pallet", new Vector3((i % 2) * 0.05f, 0.07f + i * 0.145f, 0f), new Vector3(1.2f, 0.14f, 1f), wood);
                p.transform.localRotation = Quaternion.Euler(0f, (i % 2) * 4f, 0f);
            }
            var d = Destructible.Add(stack, "pallets");
            d.FirePoint = new Vector3(0f, 0.45f, 0f);
            d.FireSize = 1.1f;
        }

        static void Fence(Transform parent, Vector3 pos)
        {
            var seg = new GameObject("Fence");
            seg.transform.SetParent(parent, false);
            seg.transform.position = pos;
            var metal = Mats.Lit(Mats.Steel, 0.85f, 0.45f);
            Cylinder(seg.transform, "Post", new Vector3(0f, 0.9f, -1.5f), new Vector3(0.06f, 0.9f, 0.06f), Quaternion.identity, metal);
            Box(seg.transform, "Rail", new Vector3(0f, 1.78f, 0f), new Vector3(0.04f, 0.04f, 3f), metal, false);
            // Chain-link mesh reads as a see-through grey panel in the base.
            Box(seg.transform, "Mesh", new Vector3(0f, 0.9f, 0f), new Vector3(0.02f, 1.75f, 3f), Mats.Glass(new Color(0.32f, 0.33f, 0.35f, 0.35f), 0.4f));
            var d = Destructible.Add(seg, "fence");
            d.FirePoint = new Vector3(0f, 0.9f, 0f);
            d.FireSize = 0.5f;
        }

        // ---------- primitives ----------

        /// <summary>A cylinder's default capsule collider balances on a point; standing objects get a box instead.</summary>
        static void FlatBottom(GameObject cylinder)
        {
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            cylinder.AddComponent<BoxCollider>();
        }

        /// <summary>A box at a position local to its parent (the arena and the house sit at the origin, so theirs are world positions).</summary>
        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, UnityEngine.Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject Cylinder(Transform parent, string name, Vector3 localPos, Vector3 scale, Quaternion localRot, UnityEngine.Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
