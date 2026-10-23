using System.Collections.Generic;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// Explosions, fire, smoke, sparks, debris and glass shards (spec §15), built from code with the legacy particle
    /// system so they run on every platform including WebGL. Debris and shards come from capped pools to keep the
    /// physics budget (150 active bodies).
    /// </summary>
    public static class Fx
    {
        const int MaxDebris = 90;
        const int MaxShards = 60;
        static readonly Queue<Rigidbody> debris = new Queue<Rigidbody>();
        static readonly Queue<Rigidbody> shards = new Queue<Rigidbody>();
        static Transform root;

        static Transform Root
        {
            get
            {
                if (root == null) root = new GameObject("Fx").transform;
                return root;
            }
        }

        /// <summary>Flash, fireball, smoke column and sparks scaled by the blast radius.</summary>
        public static void Explosion(Vector3 pos, float radius)
        {
            float s = Mathf.Clamp(radius, 0.5f, 12f);
            var go = new GameObject("Explosion");
            go.transform.SetParent(Root, false);
            go.transform.position = pos;

            // Flash: a light for a couple of frames, then fading.
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.4f);
            light.range = 6f + s * 4f;
            light.intensity = 6f + s;
            go.AddComponent<FadeLight>().Duration = 0.25f + 0.05f * s;

            Burst(go.transform, Mats.ParticleAdd, (int)(18 + 6 * s), 0.35f + 0.05f * s, 0.5f + 0.4f * s, 1.2f + 0.6f * s,
                new Color(1f, 0.95f, 0.75f), new Color(1f, 0.45f, 0.1f, 0f), 0f, 0.3f * s);
            Burst(go.transform, Mats.ParticleAlpha, (int)(10 + 4 * s), 2.5f + 0.3f * s, 0.6f * s + 0.6f, 0.8f + 0.3f * s,
                new Color(0.22f, 0.2f, 0.19f, 0.75f), new Color(0.12f, 0.11f, 0.11f, 0f), -0.12f, 0.4f * s, 1.5f + 0.4f * s);
            Sparks(go.transform, (int)(20 + 8 * s), 4f + 2f * s);
            Object.Destroy(go, 6f + s);
        }

        /// <summary>Looping fire on a burning object; destroy the returned object to put it out.</summary>
        public static GameObject Fire(Transform parent, Vector3 localPos, float size)
        {
            var go = new GameObject("Fire");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f * size, 1.4f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f * size, 0.9f * size);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.2f), new Color(1f, 0.85f, 0.4f));
            main.gravityModifier = -0.1f;
            var em = ps.emission;
            em.rateOverTime = 26f * size;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f * size;
            Fade(ps, new Color(1f, 0.85f, 0.5f), new Color(0.8f, 0.2f, 0.05f, 0f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mats.ParticleAdd;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.2f);
            light.range = 4f + 3f * size;
            light.intensity = 1.6f;
            go.AddComponent<Flicker>();

            var smoke = Burst(go.transform, Mats.ParticleAlpha, 0, 3f, 0.8f * size, 0.5f, new Color(0.15f, 0.14f, 0.13f, 0.5f), new Color(0.1f, 0.1f, 0.1f, 0f), -0.05f, 0.3f * size, 1.6f * size);
            var sm = smoke.main;
            sm.loop = true;
            var se = smoke.emission;
            se.rateOverTime = 5f * size;
            smoke.Play();
            return go;
        }

        /// <summary>Thin smoke that keeps rising from a destroyed object until the round ends.</summary>
        public static void Smolder(Transform parent, Vector3 localPos, float size)
        {
            var go = new GameObject("Smolder");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var smoke = Burst(go.transform, Mats.ParticleAlpha, 0, 4f, 0.5f * size, 0.35f, new Color(0.13f, 0.12f, 0.12f, 0.35f), new Color(0.1f, 0.1f, 0.1f, 0f), -0.03f, 0.3f * size, 1.4f * size);
            var sm = smoke.main;
            sm.loop = true;
            var se = smoke.emission;
            se.rateOverTime = 3f;
            smoke.Play();
        }

        /// <summary>Fuse sparks at a point (child of the fuse tip).</summary>
        public static ParticleSystem FuseSparks(Transform parent)
        {
            var go = new GameObject("FuseSparks");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.022f);
            main.startColor = new Color(1f, 0.85f, 0.45f);
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 70f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.005f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.ParticleAdd;
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 1.5f;
            ps.Play();
            return ps;
        }

        /// <summary>Dark chunks flying off a destroyed object.</summary>
        public static void Debris(Vector3 pos, Vector3 from, int count, Color color, float size)
        {
            for (int i = 0; i < count; i++)
            {
                var rb = Take(debris, MaxDebris, () => Chunk("Debris", PrimitiveType.Cube));
                rb.transform.position = pos + Random.insideUnitSphere * size * 0.5f;
                rb.transform.rotation = Random.rotation;
                float k = size * Random.Range(0.08f, 0.22f);
                rb.transform.localScale = new Vector3(k, k * Random.Range(0.3f, 1f), k * Random.Range(0.5f, 1.2f));
                rb.GetComponent<Renderer>().sharedMaterial = Mats.Lit(Color.Lerp(color, Mats.Scorched, Random.Range(0.3f, 0.8f)), 0.2f, 0.2f);
                Launch(rb, pos, from, 6f);
            }
        }

        /// <summary>Glass shards: cheap procedural pieces instead of a fractured mesh (spec §15).</summary>
        public static void Shards(Vector3 pos, Vector3 size, Vector3 from)
        {
            int count = Mathf.Clamp((int)(size.x * size.y * 10f), 4, 12);
            for (int i = 0; i < count; i++)
            {
                var rb = Take(shards, MaxShards, () => Chunk("Shard", PrimitiveType.Quad));
                rb.transform.position = pos + new Vector3(Random.Range(-0.5f, 0.5f) * size.x, Random.Range(-0.5f, 0.5f) * size.y, 0f);
                rb.transform.rotation = Random.rotation;
                float k = Random.Range(0.06f, 0.2f);
                rb.transform.localScale = new Vector3(k, k * Random.Range(0.5f, 1.6f), 1f);
                rb.GetComponent<Renderer>().sharedMaterial = Mats.Glass(new Color(0.7f, 0.8f, 0.9f, 0.5f));
                Launch(rb, pos, from, 4f);
            }
        }

        static void Launch(Rigidbody rb, Vector3 pos, Vector3 from, float speed)
        {
            rb.gameObject.SetActive(true);
            Vector3 dir = (pos - from);
            dir = dir.sqrMagnitude < 0.01f ? Random.onUnitSphere : dir.normalized;
            dir = (dir + Vector3.up * 0.6f + Random.insideUnitSphere * 0.6f).normalized;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = dir * speed * Random.Range(0.5f, 1.4f);
#else
            rb.velocity = dir * speed * Random.Range(0.5f, 1.4f);
#endif
            rb.angularVelocity = Random.insideUnitSphere * 12f;
        }

        static Rigidbody Chunk(string name, PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(Root, false);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, type == PrimitiveType.Quad ? 0.05f : 1f);
            go.layer = Layers.Debris;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return rb;
        }

        /// <summary>Reuses the oldest piece once the pool is full.</summary>
        static Rigidbody Take(Queue<Rigidbody> pool, int max, System.Func<Rigidbody> make)
        {
            Rigidbody rb = pool.Count >= max ? pool.Dequeue() : null;
            if (rb == null) rb = make();
            pool.Enqueue(rb);
            return rb;
        }

        static ParticleSystem Burst(Transform parent, Material mat, int count, float life, float size, float speed,
            Color start, Color end, float gravity, float shapeRadius, float sizeGrow = 1f)
        {
            var go = new GameObject("Burst");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var em = ps.emission;
            em.rateOverTime = 0f;
            if (count > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(0.01f, shapeRadius);
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, sizeGrow));
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.15f;
            limit.limit = speed * 0.25f;
            Fade(ps, start, end);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            if (count > 0) ps.Play();
            return ps;
        }

        static void Sparks(Transform parent, int count, float speed)
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = new Color(1f, 0.8f, 0.45f);
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.Planes;
            var floor = new GameObject("Floor").transform;
            floor.SetParent(go.transform, false);
            floor.position = new Vector3(parent.position.x, 0f, parent.position.z);
            col.SetPlane(0, floor);
            col.bounce = 0.35f;
            col.dampen = 0.4f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.ParticleAdd;
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.05f;
            r.lengthScale = 1f;
            ps.Play();
        }

        static void Fade(ParticleSystem ps, Color start, Color end)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(start.a * 0.8f, 0.3f), new GradientAlphaKey(end.a, 1f) });
            col.color = g;
        }
    }

    /// <summary>Fades a light to nothing, then removes it.</summary>
    public sealed class FadeLight : MonoBehaviour
    {
        public float Duration = 0.3f;
        Light l;
        float start, t;

        void Start() { l = GetComponent<Light>(); start = l.intensity; }

        void Update()
        {
            t += Time.deltaTime;
            float k = 1f - t / Duration;
            if (k <= 0f) { Destroy(l); Destroy(this); return; }
            l.intensity = start * k * k;
        }
    }

    /// <summary>Fire-light flicker.</summary>
    public sealed class Flicker : MonoBehaviour
    {
        Light l;
        float baseIntensity, seed;

        void Start() { l = GetComponent<Light>(); baseIntensity = l.intensity; seed = Random.value * 100f; }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * 9f);
            l.intensity = baseIntensity * (0.6f + 0.8f * n);
        }
    }

    public static class Layers
    {
        public const int Default = 0;
        /// <summary>Unity's built-in "Ignore Raycast" layer: the player and debris never block aiming, placing or the look label.</summary>
        public const int NoRaycast = 2;
        public const int Debris = NoRaycast;
        public const int WorldMask = ~(1 << NoRaycast);
    }
}
