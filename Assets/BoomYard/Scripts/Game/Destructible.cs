using System.Collections;
using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>One hit of damage and where it came from.</summary>
    public struct Hit
    {
        public float Amount;
        public Vector3 Origin;
        public Placement Placement;
        public bool ByObject;        // a secondary blast, a flying object or fire: counts as a chain link
        public bool BreaksConcrete;  // dynamite
        public bool Thermite;
        public int BlastId;          // the player's explosive this came from (0 for chain damage), for the Multi bonus
    }

    /// <summary>
    /// An object that breaks and costs money (spec §06, §09): HP, value in shekels, material and a reaction it has
    /// on the yard when destroyed. Partial damage pays its share of the value; the reaction is what chains.
    /// </summary>
    public sealed class Destructible : MonoBehaviour
    {
        public static readonly List<Destructible> All = new List<Destructible>();

        public ObjectDef Def;
        public float Hp;
        public bool Destroyed;
        public bool Burning;
        /// <summary>Parts that fly off first when it goes (doors, wheels).</summary>
        public readonly List<Transform> Parts = new List<Transform>();
        /// <summary>Local point for fire and smoke.</summary>
        public Vector3 FirePoint = Vector3.up * 0.5f;
        public float FireSize = 1f;

        Renderer[] renderers;
        Color[] baseColors;
        GameObject fire;
        float burnLeft;
        bool burntOut;
        Rigidbody body;

        public float Health01 { get { return Def == null ? 0f : Mathf.Clamp01(Hp / Def.Hp); } }
        public bool Flammable { get { return (Def.Reaction & Reaction.Burn) != 0 || Def.Material == Core.Material.Wood || Def.Material == Core.Material.Cardboard; } }
        public bool IsGlass { get { return (Def.Reaction & Reaction.Shatter) != 0; } }
        public string Name { get { return Loc.He ? Def.NameHe : Def.NameEn; } }

        public static Destructible Add(GameObject go, string id)
        {
            var d = go.AddComponent<Destructible>();
            d.Def = Catalog.Object(id);
            d.Hp = d.Def.Hp;
            return d;
        }

        void Start()
        {
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                var m = renderers[i].sharedMaterial;
                baseColors[i] = m != null && m.HasProperty("_Color") ? m.color : Color.white;
            }
            body = GetComponent<Rigidbody>();
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Damage(Hit hit)
        {
            if (Destroyed || Def == null || hit.Amount <= 0f) return;
            if (Def.Material == Core.Material.Concrete && !hit.BreaksConcrete) hit.Amount *= 0.05f;
            if (Def.ThermiteOnly && !hit.Thermite) return;

            float dealt = Mathf.Min(Hp, hit.Amount);
            Hp -= dealt;
            bool dies = Hp <= 0.001f;
            if (GameController.I != null) GameController.I.Credit(this, dealt / Def.Hp, hit, dies);
            Scorch(1f - Health01);
            if (dies) Die(hit);
        }

        void Scorch(float k)
        {
            if (renderers == null || IsGlass) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled) continue;
                var m = renderers[i].material;   // per-object copy, created on first damage
                if (m.HasProperty("_Color")) m.color = Color.Lerp(baseColors[i], Mats.Scorched, k * 0.85f);
            }
        }

        void Die(Hit hit)
        {
            Destroyed = true;
            Hp = 0f;
            var reaction = Def.Reaction;
            var center = transform.TransformPoint(FirePoint);

            if (IsGlass)
            {
                Fx.Shards(center, Size(), hit.Origin);
                Sfx.I.Glass(center);
                foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
                foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
                return;
            }

            Fx.Debris(center, hit.Origin, Mathf.Clamp((int)(Def.Mass / 25f), 3, 10), baseColors.Length > 0 ? baseColors[0] : Color.grey, Mathf.Clamp(Size().magnitude * 0.4f, 0.4f, 2.5f));
            DetachParts(hit.Origin);

            if ((reaction & Reaction.Launch) != 0) Launch(hit.Origin, hit.Amount);
            if ((reaction & Reaction.Douse) != 0) FireGrid.I.Douse(center, Def.BlastRadius);
            if ((reaction & Reaction.Explode) != 0) StartCoroutine(Secondary());
            else if ((reaction & Reaction.Burn) != 0) Ignite(Def.BurnSeconds);
            else Fx.Smolder(transform, FirePoint, FireSize * 0.7f);
        }

        /// <summary>Fuel burns for a moment, then goes (spec: 1–2 s to run, or not).</summary>
        IEnumerator Secondary()
        {
            if (fire == null) fire = Fx.Fire(transform, FirePoint, FireSize);
            var gas = Def.Id == "gascyl" && Random.value < 0.5f;
            if (gas) StartCoroutine(Rocket(Def.BlastDelay + 0.6f));
            yield return new WaitForSeconds(Def.BlastDelay + (gas ? 0.6f : 0f));
            var pos = transform.TransformPoint(FirePoint);
            Blast.Detonate(new Blast.Charge
            {
                Position = pos, Damage = Def.BlastDamage, Radius = Def.BlastRadius, Placement = Placement.Thrown, ByObject = true,
            }, this);
            if (Def.Mass >= 300f)
            {
                // Heavy things jump on their own blast.
                var rb = MakeDynamic();
                rb.AddForce(Vector3.up * Random.Range(2.5f, 4.5f) + Random.insideUnitSphere, ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * 1.5f, ForceMode.VelocityChange);
            }
            if ((Def.Reaction & Reaction.Burn) != 0) Ignite(Def.BurnSeconds);
            else
            {
                if (fire != null) Destroy(fire);
                Fx.Smolder(transform, FirePoint, FireSize);
            }
        }

        /// <summary>A gas cylinder with a broken valve flies like a rocket before it bursts.</summary>
        IEnumerator Rocket(float seconds)
        {
            var rb = MakeDynamic();
            var dir = (Vector3.up * 1.5f + Random.insideUnitSphere).normalized;
            for (float t = 0f; t < seconds; t += Time.fixedDeltaTime)
            {
                rb.AddForce(dir * rb.mass * 30f, ForceMode.Force);
                yield return new WaitForFixedUpdate();
            }
        }

        void Launch(Vector3 origin, float power)
        {
            var rb = MakeDynamic();
            float dv = Mathf.Clamp(power / 25f, 2f, 14f) * Mathf.Clamp(Mathf.Sqrt(40f / Mathf.Max(1f, rb.mass)), 0.15f, 1f);
            var dir = (transform.position - origin).normalized + Vector3.up * 0.7f;
            rb.AddForce(dir.normalized * dv, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * dv, ForceMode.VelocityChange);
            var impact = gameObject.GetComponent<ImpactDamage>();
            if (impact == null) impact = gameObject.AddComponent<ImpactDamage>();
            impact.Arm(4f);
        }

        Rigidbody MakeDynamic()
        {
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.mass = Def.Mass;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            return body;
        }

        void DetachParts(Vector3 origin)
        {
            foreach (var p in Parts)
            {
                if (p == null) continue;
                p.SetParent(null, true);
                var rb = p.gameObject.AddComponent<Rigidbody>();
                rb.mass = 25f;
                var dir = (p.position - origin).normalized + Vector3.up;
                rb.AddForce(dir.normalized * Random.Range(4f, 9f), ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);
                foreach (var r in p.GetComponentsInChildren<Renderer>())
                    if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                        r.material.color = Color.Lerp(r.sharedMaterial.color, Mats.Scorched, 0.6f);
            }
            Parts.Clear();
        }

        /// <summary>Sets it on fire: it takes damage over time (the value comes in slowly) and spreads to flammables.</summary>
        public void Ignite(float seconds)
        {
            if (burntOut) return;
            if (seconds <= 0f) seconds = 6f;
            burnLeft = Mathf.Max(burnLeft, seconds);
            if (Burning) return;
            Burning = true;
            if (fire == null) fire = Fx.Fire(transform, FirePoint, FireSize);
            FireGrid.I.Add(this);
        }

        /// <summary>Called by the fire grid every tick while burning. Returns false once burnt out.</summary>
        public bool BurnTick(float dt)
        {
            if (!Burning) return false;
            burnLeft -= dt;
            if (!Destroyed)
                Damage(new Hit { Amount = Def.Hp / Mathf.Max(4f, Def.BurnSeconds > 0f ? Def.BurnSeconds : 8f) * dt, Origin = transform.position, ByObject = true });
            if (burnLeft <= 0f) { Extinguish(); return false; }
            return true;
        }

        public void Extinguish()
        {
            if (!Burning) return;
            Burning = false;
            burnLeft = 0f;
            if (fire != null) Destroy(fire);
            fire = null;
            if (Destroyed) burntOut = true;
            if (Destroyed) Fx.Smolder(transform, FirePoint, FireSize);
        }

        public Vector3 Size()
        {
            var c = GetComponentInChildren<Collider>();
            return c != null ? c.bounds.size : Vector3.one;
        }
    }

    /// <summary>A launched object hurts what it hits: mass × speed (spec §06 "push"). Counts as a chain link.</summary>
    public sealed class ImpactDamage : MonoBehaviour
    {
        float until;
        Rigidbody rb;

        public void Arm(float seconds)
        {
            until = Time.time + seconds;
            rb = GetComponent<Rigidbody>();
            enabled = true;
        }

        void Update() { if (Time.time > until) enabled = false; }

        void OnCollisionEnter(Collision c)
        {
            if (!enabled || rb == null) return;
            float v = c.relativeVelocity.magnitude;
            if (v < 3f) return;
            var target = c.collider.GetComponentInParent<Destructible>();
            if (target == null || target.gameObject == gameObject) return;
            float energy = 0.5f * Mathf.Min(rb.mass, 400f) * v * v;
            target.Damage(new Hit { Amount = energy / 50f, Origin = transform.position, ByObject = true });
        }
    }
}
