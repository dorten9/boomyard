using System.Collections;
using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>The kinds of aerial shell, each with its own look.</summary>
    public enum ShellType
    {
        Peony,          // a ball of coloured stars
        Chrysanthemum,  // a ball of stars with long trails that turn gold
        Willow,         // golden trails that hang and droop slowly down
        Palm,           // a few thick golden comets, like palm fronds
        Ring,           // a flat ring of stars at a tilt
        Crossette,      // stars that split in four mid-air
        Crackling,      // gold stars that end in a storm of white pops
        Strobe,         // white stars blinking as they fall
        Heart,          // a heart drawn in the sky, facing the street
        Kamuro,         // a dense crown of silver-gold glitter
        MultiBreak,     // three small bursts in a quick row
        Dahlia,         // fewer, bigger stars in two colours
    }

    /// <summary>
    /// Firework bursts in the sky (all particle systems, built in code). Every burst flashes the street, is heard
    /// as a delayed boom (Sfx.ShellBurst) and, for crackling ones, a sizzle of pops as the stars burn out.
    /// </summary>
    public static class Fireworks
    {
        public static readonly Color[] Colours =
        {
            new Color(1f, 0.22f, 0.18f), new Color(0.3f, 1f, 0.4f), new Color(0.35f, 0.55f, 1f), new Color(1f, 0.82f, 0.3f),
            new Color(1f, 0.38f, 1f), new Color(1f, 1f, 1f), new Color(0.3f, 0.95f, 1f), new Color(1f, 0.55f, 0.15f),
        };
        static readonly Color Gold = new Color(1f, 0.72f, 0.32f);
        static readonly Color Silver = new Color(0.95f, 0.93f, 0.85f);

        public static ShellType RandomType() { return (ShellType)Random.Range(0, System.Enum.GetValues(typeof(ShellType)).Length); }
        public static Color RandomColour() { return Colours[Random.Range(0, Colours.Length)]; }

        /// <summary>A shell bursting at <paramref name="pos"/>. <paramref name="scale"/> 1 a rocket's, 2 a mortar's.</summary>
        public static void Burst(Vector3 pos, ShellType type, float scale = 1f)
        {
            var a = RandomColour();
            var b = Random.value < 0.5f ? RandomColour() : a;
            var root = new GameObject("Firework " + type).transform;
            root.position = pos;
            bool crackle = false;
            float life = 3f;
            switch (type)
            {
                case ShellType.Peony:
                    Stars(root, 170, 10f, 1.9f, 0.2f, a, a * 0.6f, 0.25f, 1.6f, 0.2f, scale);
                    Stars(root, 60, 4f, 1.3f, 0.16f, b, b, 0.25f, 1.6f, 0.15f, scale);
                    break;
                case ShellType.Chrysanthemum:
                    Stars(root, 150, 10f, 2.3f, 0.18f, a, Gold, 0.3f, 1.4f, 0.7f, scale);
                    break;
                case ShellType.Willow:
                    Stars(root, 130, 8f, 4.2f, 0.16f, Gold, Gold * 0.4f, 0.35f, 2.6f, 1.6f, scale);
                    life = 5.5f;
                    break;
                case ShellType.Palm:
                    Stars(root, 10, 13f, 2.6f, 0.45f, Gold, new Color(1f, 0.45f, 0.15f), 0.6f, 1f, 1f, scale);
                    Stars(root, 40, 3f, 1.4f, 0.14f, a, a, 0.3f, 1.5f, 0.1f, scale);
                    break;
                case ShellType.Ring:
                    {
                        var ring = Stars(root, 110, 11f, 1.8f, 0.18f, a, a * 0.5f, 0.2f, 1.6f, 0.25f, scale);
                        var shape = ring.shape;
                        shape.shapeType = ParticleSystemShapeType.Circle;
                        shape.radius = 0.05f;
                        ring.transform.rotation = Quaternion.Euler(Random.Range(50f, 80f), Random.Range(0f, 360f), 0f);
                        Stars(root, 30, 2.5f, 1.2f, 0.18f, b, b, 0.2f, 1.6f, 0.1f, scale);
                        break;
                    }
                case ShellType.Crossette:
                    {
                        var parent = Stars(root, 14, 9f, 0.9f, 0.3f, a, a, 0.25f, 0.6f, 0.4f, scale);
                        var split = Stars(parent.transform, 4, 6f, 1.1f, 0.18f, a, b, 0.25f, 1.2f, 0.35f, scale, false);
                        var subs = parent.subEmitters;
                        subs.enabled = true;
                        subs.AddSubEmitter(split, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
                        break;
                    }
                case ShellType.Crackling:
                    {
                        var stars = Stars(root, 90, 9f, 1.6f, 0.16f, Gold, Gold, 0.3f, 1.4f, 0.35f, scale);
                        var pops = Stars(stars.transform, 6, 2.5f, 0.12f, 0.12f, Color.white * 1.5f, Color.white, 0f, 0.5f, 0f, scale, false);
                        var subs = stars.subEmitters;
                        subs.enabled = true;
                        subs.AddSubEmitter(pops, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
                        crackle = true;
                        break;
                    }
                case ShellType.Strobe:
                    {
                        var strobe = Stars(root, 90, 8f, 2.8f, 0.2f, Silver, Silver, 0.3f, 1.6f, 0f, scale);
                        var col = strobe.colorOverLifetime;
                        var g = new Gradient();
                        var alphas = new GradientAlphaKey[8];
                        for (int k = 0; k < 8; k++) alphas[k] = new GradientAlphaKey(k % 2 == 0 ? 1f : 0.05f, k / 7f);
                        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, alphas);
                        col.color = g;
                        life = 4f;
                        break;
                    }
                case ShellType.Heart:
                    Heart(root, a, scale);
                    break;
                case ShellType.Kamuro:
                    Stars(root, 220, 8.5f, 3.4f, 0.12f, Silver, Gold * 0.6f, 0.3f, 2.2f, 1f, scale);
                    crackle = true;
                    life = 5f;
                    break;
                case ShellType.Dahlia:
                    Stars(root, 50, 9f, 2.2f, 0.4f, a, a * 0.5f, 0.28f, 1.5f, 0.3f, scale);
                    Stars(root, 30, 6f, 2f, 0.4f, b, b * 0.5f, 0.28f, 1.5f, 0.3f, scale);
                    break;
                case ShellType.MultiBreak:
                    Object.Destroy(root.gameObject);
                    if (GameController.I != null) GameController.I.StartCoroutine(Multi(pos, scale));
                    return;
            }
            Flash(root, a, scale);
            Sfx.I.ShellBurst(pos, scale, crackle);
            Object.Destroy(root.gameObject, life + 1f);
        }

        static IEnumerator Multi(Vector3 pos, float scale)
        {
            for (int i = 0; i < 3; i++)
            {
                var p = pos + new Vector3(Random.Range(-4f, 4f), Random.Range(-2f, 3f), Random.Range(-4f, 4f)) * scale;
                Burst(p, i == 2 ? ShellType.Crackling : ShellType.Peony, scale * 0.6f);
                yield return new WaitForSeconds(0.25f);
            }
        }

        /// <summary>The flash over the street.</summary>
        static void Flash(Transform root, Color c, float scale)
        {
            var light = root.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = c;
            light.range = 45f * scale;
            light.intensity = 3f;
            root.gameObject.AddComponent<FadeLight>().Duration = 0.9f;
        }

        /// <summary>
        /// One layer of stars: <paramref name="count"/> bursting out at <paramref name="speed"/> m/s, living
        /// <paramref name="life"/> s, fading from <paramref name="from"/> to <paramref name="to"/>, falling with
        /// <paramref name="gravity"/>, slowed by <paramref name="drag"/>, with trails <paramref name="trail"/> s long.
        /// </summary>
        static ParticleSystem Stars(Transform parent, int count, float speed, float life, float size, Color from, Color to, float gravity, float drag, float trail, float scale, bool burst = true)
        {
            var go = new GameObject("Stars");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.8f, life * 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.85f * scale, speed * 1.05f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f * Mathf.Sqrt(scale), size * Mathf.Sqrt(scale));
            main.startColor = Color.white;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(count * 8, 64);
            var em = ps.emission;
            em.rateOverTime = 0f;
            if (burst) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(Quality.Tier == QualityTier.Low ? count * 2 / 3 : count)) });
            else em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = drag;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(from * 2.5f, 0f), new GradientColorKey(to * 2f, 0.7f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var trails = ps.trails;
            trails.enabled = trail > 0f && Quality.Tier != QualityTier.Low;
            trails.lifetime = trail;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 0f));
            trails.inheritParticleColor = true;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.ParticleAdd;
            r.trailMaterial = Mats.ParticleAdd;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (burst) ps.Play();
            return ps;
        }

        /// <summary>A single bright flash (a strobe pot's star).</summary>
        public static void Flare(Vector3 pos)
        {
            var go = new GameObject("Flare");
            go.transform.position = pos;
            go.AddComponent<MeshFilter>().sharedMesh = Mats.Quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mats.EmberHDR;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.localScale = Vector3.one * Random.Range(0.25f, 0.45f);
            go.AddComponent<Billboard>();
            Object.Destroy(go, 0.07f);
        }

        /// <summary>A heart in the sky, upright and facing the player.</summary>
        static void Heart(Transform root, Color c, float scale)
        {
            var ps = Stars(root, 0, 0f, 2.2f, 0.2f, c, c * 0.5f, 0.12f, 1.2f, 0.15f, scale, false);
            var cam = Camera.main != null ? Camera.main.transform : root;
            var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            int n = 120;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n * Mathf.PI * 2f;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                var v = (right * x + Vector3.up * y) * 0.6f * scale;
                ps.Emit(new ParticleSystem.EmitParams { velocity = v, startLifetime = 2.2f, startSize = 0.2f * Mathf.Sqrt(scale), startColor = Color.white, position = root.position, applyShapeToPosition = false }, 1);
            }
            ps.Play();
        }

        // ---------- what goes up ----------

        /// <summary>
        /// A star or shell flying up from a tube: a glowing head with a trail of sparks, under gravity. When it
        /// has flown <paramref name="seconds"/> it calls <paramref name="done"/> at its position (the burst).
        /// </summary>
        public static void Comet(Vector3 from, Vector3 velocity, float seconds, Color colour, float size, System.Action<Vector3> done)
        {
            var go = new GameObject("Comet");
            go.transform.position = from;
            var c = go.AddComponent<CometFlight>();
            c.Velocity = velocity;
            c.Seconds = seconds;
            c.Done = done;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.3f, size * 0.7f);
            main.startColor = colour * 2f;
            main.gravityModifier = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 90f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.02f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.3f), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.ParticleAdd;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var head = new GameObject("Head");
            head.transform.SetParent(go.transform, false);
            head.AddComponent<MeshFilter>().sharedMesh = Mats.Quad;
            var hr = head.AddComponent<MeshRenderer>();
            hr.sharedMaterial = Mats.EmberHDR;
            hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            head.transform.localScale = Vector3.one * size;
            head.AddComponent<Billboard>();
            ps.Play();
        }
    }

    /// <summary>Moves a comet under gravity, then hands its position to the burst.</summary>
    public sealed class CometFlight : MonoBehaviour
    {
        public Vector3 Velocity;
        public float Seconds;
        public System.Action<Vector3> Done;
        float age;

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            Velocity += Physics.gravity * dt * 0.8f;
            transform.position += Velocity * dt;
            if (age < Seconds) return;
            if (Done != null) Done(transform.position);
            // Let the trail finish falling, then go.
            var ps = GetComponent<ParticleSystem>();
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            var head = transform.Find("Head");
            if (head != null) Destroy(head.gameObject);
            enabled = false;
            Destroy(gameObject, 1f);
        }
    }

    /// <summary>Keeps a quad facing the camera.</summary>
    public sealed class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
        }
    }

    /// <summary>
    /// A firework that stays on the ground and fires once its fuse has burnt down: a cake (a battery of small
    /// shells), a mortar (one big shell), a Roman candle (a row of coloured balls) or a fountain (a gush of gold
    /// sparks that sets light to anything flammable close by).
    /// </summary>
    public sealed class FireworkDevice : MonoBehaviour
    {
        public static void Begin(LiveExplosive l)
        {
            var d = l.gameObject.AddComponent<FireworkDevice>();
            var rb = l.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            d.StartCoroutine(d.Run(l.Def.Firework));
        }

        Vector3 Muzzle { get { return transform.position + transform.up * 0.2f; } }

        IEnumerator Run(string kind)
        {
            switch (kind)
            {
                case "cake": yield return Cake(); break;
                case "mortar": yield return Mortar(); break;
                case "candle": yield return Candle(); break;
                case "fountain": yield return Fountain(); break;
                case "spinner": yield return Spinner(); break;
                case "ufo": yield return Saucer(); break;
                case "smoke": yield return Smoke(); break;
                case "strobepot": yield return StrobePot(); break;
                case "saturn": yield return Saturn(); break;
                case "willowcake": yield return Battery(12, new[] { ShellType.Willow, ShellType.Kamuro }, 0.8f, 0.55f); break;
                case "finale": yield return Battery(36, new[] { ShellType.Peony, ShellType.Crackling, ShellType.Chrysanthemum, ShellType.Ring, ShellType.Crossette, ShellType.Dahlia, ShellType.Strobe, ShellType.Palm }, 0.8f, 0.33f); break;
                case "bigmortar": yield return BigMortar(); break;
            }
            Destroy(gameObject, 0.5f);
        }

        /// <summary>Sixteen small shells, one after another, fanning out a little.</summary>
        IEnumerator Cake()
        {
            var types = new[] { ShellType.Peony, ShellType.Chrysanthemum, ShellType.Crackling, ShellType.Ring, ShellType.Strobe, ShellType.Dahlia, ShellType.Crossette, ShellType.Willow };
            for (int i = 0; i < 16; i++)
            {
                var dir = (Vector3.up + new Vector3(Random.Range(-0.18f, 0.18f), 0f, Random.Range(-0.18f, 0.18f))).normalized;
                var type = types[(i + Random.Range(0, 2)) % types.Length];
                Sfx.I.Launch(Muzzle, 0.6f);
                Fireworks.Comet(Muzzle, dir * 24f, Random.Range(0.75f, 0.95f), Fireworks.Colours[3], 0.25f, p => Fireworks.Burst(p, type, 0.7f));
                yield return new WaitForSeconds(Random.Range(0.3f, 0.5f));
            }
        }

        /// <summary>A battery of <paramref name="shots"/> shells of the given kinds, one every <paramref name="gap"/> s.</summary>
        IEnumerator Battery(int shots, ShellType[] types, float scale, float gap)
        {
            for (int i = 0; i < shots; i++)
            {
                var dir = (Vector3.up + new Vector3(Random.Range(-0.22f, 0.22f), 0f, Random.Range(-0.22f, 0.22f))).normalized;
                var type = types[Random.Range(0, types.Length)];
                Sfx.I.Launch(Muzzle, 0.6f);
                Fireworks.Comet(Muzzle, dir * Random.Range(22f, 27f), Random.Range(0.75f, 1f), Fireworks.Colours[3], 0.25f, p => Fireworks.Burst(p, type, scale));
                yield return new WaitForSeconds(gap * Random.Range(0.8f, 1.2f));
            }
        }

        /// <summary>The biggest shell: very high, a burst that fills the sky.</summary>
        IEnumerator BigMortar()
        {
            Sfx.I.Launch(Muzzle, 2.2f);
            var big = new[] { ShellType.Kamuro, ShellType.Willow, ShellType.Chrysanthemum, ShellType.MultiBreak };
            var type = big[Random.Range(0, big.Length)];
            Fireworks.Comet(Muzzle, Vector3.up * 40f, 1.9f, Fireworks.Colours[3], 0.45f, p => Fireworks.Burst(p, type, 2.6f));
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>A ring of sparks whirling across the ground for five seconds, ending in a pop.</summary>
        IEnumerator Spinner()
        {
            var sparks = Sparks(transform, Fireworks.RandomColour(), 300f, 3f);
            var dir = Random.insideUnitCircle.normalized;
            for (float t = 0f; t < 5f; t += Time.deltaTime)
            {
                transform.Rotate(0f, 1400f * Time.deltaTime, 0f, Space.World);
                transform.position += new Vector3(dir.x, 0f, dir.y) * Mathf.Sin(t * 1.3f) * 0.3f * Time.deltaTime;
                yield return null;
            }
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Sfx.I.CandlePop(transform.position);
            Fireworks.Burst(transform.position + Vector3.up * 0.3f, ShellType.Peony, 0.15f);
            yield return new WaitForSeconds(0.8f);
        }

        /// <summary>A flying saucer: spins up on the ground, lifts off to about 12 m in a spiral of sparks, bursts.</summary>
        IEnumerator Saucer()
        {
            var sparks = Sparks(transform, new Color(0.6f, 0.9f, 1f), 260f, 2.5f);
            float spin = 0f;
            for (float t = 0f; t < 3.2f; t += Time.deltaTime)
            {
                spin = Mathf.Min(1600f, spin + 900f * Time.deltaTime);
                transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
                if (t > 0.8f) transform.position += Vector3.up * Mathf.Lerp(2f, 6f, (t - 0.8f) / 2.4f) * Time.deltaTime;
                yield return null;
            }
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Fireworks.Burst(transform.position, Random.value < 0.5f ? ShellType.Ring : ShellType.Crackling, 0.6f);
            foreach (var r in GetComponentsInChildren<Renderer>()) if (!(r is ParticleSystemRenderer)) r.enabled = false;
            yield return new WaitForSeconds(0.8f);
        }

        /// <summary>Ten seconds of thick coloured smoke, billowing up and drifting.</summary>
        IEnumerator Smoke()
        {
            var colours = new[] { new Color(0.95f, 0.3f, 0.25f), new Color(0.3f, 0.6f, 1f), new Color(0.4f, 0.9f, 0.4f), new Color(1f, 0.85f, 0.25f), new Color(0.85f, 0.4f, 0.95f) };
            var c = colours[Random.Range(0, colours.Length)];
            var go = new GameObject("Smoke");
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startColor = new Color(c.r, c.g, c.b, 0.85f);
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            var em = ps.emission;
            em.rateOverTime = 45f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.05f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 4f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.4f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Smoke;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            var hiss = Sfx.I.FountainLoop(go.transform);
            hiss.volume *= 0.35f;
            hiss.pitch = 0.6f;
            yield return new WaitForSeconds(10f);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            hiss.Stop();
            go.transform.SetParent(null, true);
            Destroy(go, 6f);
        }

        /// <summary>Six seconds of white flashes popping on the ground.</summary>
        IEnumerator StrobePot()
        {
            var light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.white;
            light.range = 12f;
            for (float t = 0f; t < 6f; t += 0.09f)
            {
                bool on = Random.value < 0.45f;
                light.intensity = on ? 4f : 0f;
                if (on) Fireworks.Flare(transform.position + Vector3.up * Random.Range(0.3f, 1.4f) + Random.insideUnitSphere * 0.4f);
                yield return new WaitForSeconds(0.09f);
            }
            Destroy(light);
        }

        /// <summary>Thirty little missiles fanning out in a quick sweep, each cracking where it ends.</summary>
        IEnumerator Saturn()
        {
            var right = transform.right;
            for (int i = 0; i < 30; i++)
            {
                float sweep = Mathf.Sin(i * 0.4f) * 0.6f;
                var dir = (Vector3.up * 1.2f + right * sweep + Random.insideUnitSphere * 0.1f).normalized;
                Sfx.I.CandlePop(Muzzle);
                Fireworks.Comet(Muzzle, dir * 20f, Random.Range(0.6f, 0.9f), Fireworks.RandomColour(), 0.18f, p => Fireworks.Burst(p, ShellType.Crackling, 0.2f));
                yield return new WaitForSeconds(0.12f);
            }
        }

        /// <summary>A looping spray of sparks round an object (spinners and saucers).</summary>
        static ParticleSystem Sparks(Transform parent, Color c, float rate, float speed)
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * 0.01f;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = c * 2.5f;
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 800;
            var em = ps.emission;
            em.rateOverTime = Quality.Tier == QualityTier.Low ? rate * 0.6f : rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.ParticleAdd;
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 1.5f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        /// <summary>One big shell, high up: the biggest burst there is.</summary>
        IEnumerator Mortar()
        {
            Sfx.I.Launch(Muzzle, 1.6f);
            var big = new[] { ShellType.Kamuro, ShellType.Willow, ShellType.MultiBreak, ShellType.Chrysanthemum, ShellType.Heart, ShellType.Palm };
            var type = big[Random.Range(0, big.Length)];
            Fireworks.Comet(Muzzle, Vector3.up * 34f, 1.5f, Fireworks.Colours[3], 0.35f, p => Fireworks.Burst(p, type, 1.8f));
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>Eight coloured balls, one every half second, each popping at the top of its flight.</summary>
        IEnumerator Candle()
        {
            for (int i = 0; i < 8; i++)
            {
                var c = Fireworks.Colours[i % Fireworks.Colours.Length];
                var dir = (transform.up + Random.insideUnitSphere * 0.06f).normalized;
                Sfx.I.CandlePop(Muzzle);
                bool last = i == 7;
                Fireworks.Comet(Muzzle, dir * 19f, 1.1f, c, 0.4f, p => { if (last) Fireworks.Burst(p, ShellType.Peony, 0.5f); });
                yield return new WaitForSeconds(0.55f);
            }
        }

        /// <summary>Eight seconds of gold sparks gushing three metres up; flammables within a metre catch.</summary>
        IEnumerator Fountain()
        {
            var go = new GameObject("Fountain");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 0.15f;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(1f, 0.75f, 0.35f) * 2.5f;
            main.gravityModifier = 0.9f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500;
            var em = ps.emission;
            em.rateOverTime = Quality.Tier == QualityTier.Low ? 220f : 400f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.02f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.5f, 0.2f), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.ParticleAdd;
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.03f;
            r.lengthScale = 1.5f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.7f, 0.35f);
            light.range = 8f;
            light.intensity = 2f;
            go.AddComponent<Flicker>();
            Sfx.I.FountainLoop(go.transform);
            for (float t = 0f; t < 8f; t += 0.5f)
            {
                foreach (var d in Destructible.All.ToArray())
                    if (d != null && !d.Destroyed && d.Flammable && Vector3.Distance(d.transform.TransformPoint(d.FirePoint), transform.position) < 1.2f) d.Ignite(d.Def.BurnSeconds);
                yield return new WaitForSeconds(0.5f);
            }
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(light);
            var src = go.GetComponent<AudioSource>();
            if (src != null) src.Stop();
            yield return new WaitForSeconds(1.2f);
        }
    }
}
