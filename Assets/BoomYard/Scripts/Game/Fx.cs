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

        /// <summary>
        /// A blast, in layers: a flash (light + white-hot core), a turbulent HDR fireball, a sunlit smoke column,
        /// a dust ring racing out along the ground, a shockwave ring, bouncing embers and a scorch mark. Firecrackers
        /// get a white paper-smoke puff and red paper confetti instead of a fireball.
        /// </summary>
        public static void Explosion(Vector3 pos, float radius)
        {
            float s = Mathf.Clamp(radius, 0.5f, 12f);
            bool cracker = radius < 1.2f;
            var go = new GameObject("Explosion");
            go.transform.SetParent(Root, false);
            go.transform.position = pos;
            float ground = GroundY(pos);
            bool onGround = pos.y - ground < 1.5f;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.5f);
            light.range = 6f + s * 6f;
            light.intensity = cracker ? 6f : 14f + 3f * s;
            light.shadows = LightShadows.None;
            go.AddComponent<FadeLight>().Duration = cracker ? 0.12f : 0.35f + 0.04f * s;

            // White-hot core: a couple of frames.
            Emit(go.transform, Mats.FireballHDR, new E
            {
                Count = 3, Life = new Vector2(0.06f, 0.12f), Size = new Vector2(1.2f * s, 1.8f * s), Speed = Vector2.zero,
                Start = new Color(1f, 0.95f, 0.85f, 1f), End = new Color(1f, 0.6f, 0.2f, 0f), Grow = 1.4f,
            });

            if (cracker)
            {
                Emit(go.transform, Mats.Smoke, new E
                {
                    Count = 8, Life = new Vector2(1.5f, 3f), Size = new Vector2(0.25f, 0.5f), Speed = new Vector2(0.2f, 0.8f), Radius = 0.1f,
                    Start = new Color(0.85f, 0.84f, 0.82f, 0.7f), End = new Color(0.7f, 0.7f, 0.7f, 0f), Grow = 3f, Gravity = -0.03f, Noise = 0.4f,
                });
                Confetti(go.transform, 14);
            }
            else
            {
                // Fireball: rolling, turbulent, from white-yellow to deep orange to soot.
                var fire = new Gradient();
                fire.SetKeys(
                    new[] { new GradientColorKey(new Color(1f, 0.9f, 0.7f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.18f), 0.35f), new GradientColorKey(new Color(0.55f, 0.15f, 0.05f), 0.75f), new GradientColorKey(new Color(0.15f, 0.05f, 0.03f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
                Emit(go.transform, Mats.FireballHDR, new E
                {
                    Count = (int)(10 + 5 * s), Life = new Vector2(0.45f, 0.9f + 0.05f * s), Size = new Vector2(0.7f * s, 1.4f * s), Speed = new Vector2(0.6f * s, 2.2f * s), Radius = 0.25f * s,
                    Gradient = fire, Grow = 1.7f, Gravity = -0.25f, Noise = 0.8f, Spin = 1.5f, Damp = 0.4f,
                });
                // Smoke column, lit by the sun: dark and dense at first, greying as it rises and spreads.
                Emit(go.transform, Mats.Smoke, new E
                {
                    Count = (int)(10 + 5 * s), Life = new Vector2(4f + 0.3f * s, 7f + 0.5f * s), Size = new Vector2(0.8f * s, 1.6f * s), Speed = new Vector2(0.4f * s, 1.2f * s), Radius = 0.3f * s,
                    Start = new Color(0.16f, 0.15f, 0.14f, 0.85f), End = new Color(0.42f, 0.4f, 0.38f, 0f), Grow = 2.8f, Gravity = -0.06f, Noise = 0.5f, Spin = 0.3f, Damp = 0.35f, Delay = 0.05f,
                });
                if (onGround)
                {
                    // Dust thrown out along the ground.
                    Emit(go.transform, Mats.Smoke, new E
                    {
                        Count = (int)(14 + 4 * s), Life = new Vector2(1.5f, 3f), Size = new Vector2(0.4f * s, 0.8f * s), Speed = new Vector2(3f + s, 6f + 2f * s), Radius = 0.2f,
                        Start = new Color(0.55f, 0.5f, 0.43f, 0.6f), End = new Color(0.6f, 0.56f, 0.5f, 0f), Grow = 2.2f, Ring = true, Damp = 0.7f, AtY = ground + 0.15f,
                    });
                    Shockwave(new Vector3(pos.x, ground + 0.05f, pos.z), s);
                }
                Sparks(go.transform, (int)(25 + 10 * s), 5f + 2.5f * s, ground);
            }
            if (onGround) Scorch(new Vector3(pos.x, ground, pos.z), cracker ? 0.5f : 1.3f * s);
            Object.Destroy(go, 12f + s);
        }

        /// <summary>Height of the ground under a point (falls back to 0).</summary>
        static float GroundY(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, 20f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point.y;
            return 0f;
        }

        /// <summary>Settings of one particle burst.</summary>
        struct E
        {
            public int Count;
            public Vector2 Life, Size, Speed;
            public float Radius, Grow, Gravity, Noise, Spin, Damp, Delay, AtY;
            public Color Start, End;
            public Gradient Gradient;
            public bool Ring;
        }

        static ParticleSystem Emit(Transform parent, Material mat, E e)
        {
            var go = new GameObject("Burst");
            go.transform.SetParent(parent, false);
            if (e.AtY != 0f) go.transform.position = new Vector3(parent.position.x, e.AtY, parent.position.z);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.2f;
            main.startDelay = e.Delay;
            main.startLifetime = new ParticleSystem.MinMaxCurve(e.Life.x, e.Life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(e.Speed.x, e.Speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(e.Size.x, e.Size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = e.Gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, e.Count)) });
            var shape = ps.shape;
            if (e.Ring)
            {
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.rotation = new Vector3(-90f, 0f, 0f);
                shape.radius = Mathf.Max(0.05f, e.Radius);
                shape.radiusThickness = 0f;
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = Mathf.Max(0.01f, e.Radius);
            }
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 0.6f + (e.Grow - 0.6f) * 0.6f), new Keyframe(1f, e.Grow)));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            if (e.Gradient != null) col.color = e.Gradient;
            else
            {
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(e.Start, 0f), new GradientColorKey(e.End, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(e.Start.a, 0.06f), new GradientAlphaKey(e.Start.a * 0.7f, 0.5f), new GradientAlphaKey(e.End.a, 1f) });
                col.color = g;
            }
            if (e.Spin > 0f)
            {
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-e.Spin, e.Spin);
            }
            if (e.Noise > 0f)
            {
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = e.Noise;
                noise.frequency = 0.6f;
                noise.scrollSpeed = 0.3f;
                noise.quality = ParticleSystemNoiseQuality.Low;
            }
            if (e.Damp > 0f)
            {
                var limit = ps.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.dampen = e.Damp * 0.25f;
                limit.limit = 0.5f;
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.sortingFudge = mat == Mats.Smoke ? 10f : 0f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        /// <summary>Red paper bits from a firecracker, tumbling down.</summary>
        static void Confetti(Transform parent, int count)
        {
            var go = new GameObject("Confetti");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.12f, 0.08f), new Color(0.9f, 0.85f, 0.75f));
            main.gravityModifier = 0.25f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.2f;
            limit.limit = 0.6f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Paper;
            ps.Play();
        }

        /// <summary>A flat ring of pressure racing out along the ground.</summary>
        static void Shockwave(Vector3 at, float s)
        {
            var go = new GameObject("Shockwave");
            go.transform.SetParent(Root, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = Mats.Quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mats.ShockRing;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var w = go.AddComponent<Expand>();
            w.To = s * 3.5f;
            w.Duration = 0.35f + 0.03f * s;
        }

        /// <summary>A burnt patch left on the ground.</summary>
        static void Scorch(Vector3 at, float size)
        {
            var go = new GameObject("Scorch");
            go.transform.SetParent(Root, false);
            go.transform.position = at + Vector3.up * 0.025f;
            go.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = Mats.Quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mats.Scorch;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f * size, 1.2f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f * size, 1.1f * size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.15f;
            var em = ps.emission;
            em.rateOverTime = 22f * size;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f * size;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.75f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.5f), new GradientColorKey(new Color(0.4f, 0.1f, 0.03f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.1f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.4f));
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 1.2f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var rr = go.GetComponent<ParticleSystemRenderer>();
            rr.sharedMaterial = Mats.FireballHDR;
            rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.2f);
            light.range = 4f + 3f * size;
            light.intensity = 3f;
            go.AddComponent<Flicker>();
            Sfx.I.FireLoop(go.transform);

            var smoke = Emit(go.transform, Mats.Smoke, new E
            {
                Count = 1, Life = new Vector2(4f, 6f), Size = new Vector2(0.8f * size, 1.4f * size), Speed = new Vector2(0.8f, 1.6f), Radius = 0.3f * size,
                Start = new Color(0.14f, 0.13f, 0.12f, 0.75f), End = new Color(0.35f, 0.34f, 0.33f, 0f), Grow = 3f, Gravity = -0.04f, Noise = 0.4f, Spin = 0.3f,
            });
            var sm = smoke.main;
            sm.loop = true;
            var se = smoke.emission;
            se.rateOverTime = 4f * size;
            smoke.Play();
            ps.Play();
            return go;
        }

        /// <summary>Thin smoke that keeps rising from a destroyed object until the level ends.</summary>
        public static void Smolder(Transform parent, Vector3 localPos, float size)
        {
            var go = new GameObject("Smolder");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var smoke = Emit(go.transform, Mats.Smoke, new E
            {
                Count = 1, Life = new Vector2(4f, 6f), Size = new Vector2(0.5f * size, 0.9f * size), Speed = new Vector2(0.4f, 0.9f), Radius = 0.2f * size,
                Start = new Color(0.22f, 0.21f, 0.2f, 0.5f), End = new Color(0.45f, 0.44f, 0.43f, 0f), Grow = 3f, Gravity = -0.02f, Noise = 0.3f, Spin = 0.2f,
            });
            var sm = smoke.main;
            sm.loop = true;
            var se = smoke.emission;
            se.rateOverTime = 2.5f;
            smoke.Play();
        }

        /// <summary>Fuse sparks at a point (child of the fuse tip).</summary>
        /// <summary>The fine cloud out of a spray can's nozzle (emission off until the nozzle is pressed).</summary>
        public static ParticleSystem SprayMist(Transform parent, Vector3 at, Color c)
        {
            var go = new GameObject("SprayMist");
            go.layer = Layers.NoRaycast;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startColor = c;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 260f;
            em.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 7f;
            shape.radius = 0.002f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 4f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.ParticleAlpha;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

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

        /// <summary>Rain around the camera (LevelRule.Rain): falling streaks in a box that moves with the player.</summary>
        public static GameObject Rain(Transform cam)
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(cam, false);
            go.transform.localPosition = new Vector3(0f, 6f, 4f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(9f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.02f);
            main.startColor = new Color(0.75f, 0.78f, 0.82f, 0.35f);
            main.maxParticles = 2500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = Quality.Tier == QualityTier.Low ? 900f : 1800f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(22f, 22f, 0.1f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.ParticleAlpha;
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.035f;
            r.lengthScale = 2f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return go;
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
            // A quad primitive brings a MeshCollider, whose class the web build strips: start from a cube and swap the mesh.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (type == PrimitiveType.Quad) go.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
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

        static void Sparks(Transform parent, int count, float speed, float groundY)
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
            floor.position = new Vector3(parent.position.x, groundY, parent.position.z);
            col.SetPlane(0, floor);
            col.bounce = 0.35f;
            col.dampen = 0.4f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.EmberHDR;
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

    /// <summary>Scales a flat ring out and fades it (the shockwave).</summary>
    public sealed class Expand : MonoBehaviour
    {
        public float To = 5f, Duration = 0.4f;
        float t;
        MaterialPropertyBlock mpb;
        Renderer r;

        void Start() { r = GetComponent<Renderer>(); mpb = new MaterialPropertyBlock(); }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, To, 1f - (1f - k) * (1f - k));
            mpb.SetColor("_Tint", new Color(1f, 0.9f, 0.8f, (1f - k) * 0.55f));
            r.SetPropertyBlock(mpb);
            if (k >= 1f) Destroy(gameObject);
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
            if (l == null) { Destroy(this); return; }   // the light went out (a fuse burnt down)
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
