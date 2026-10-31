using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// A detonation (spec §06, §16 Detonator): damage with falloff inside the radius, a wider blast wave that breaks
    /// glass, a push on every rigid body, and what it does to the player (knockdown, close call, camera shake).
    /// </summary>
    public static class Blast
    {
        public struct Charge
        {
            public Vector3 Position;
            public float Damage;
            public float Radius;
            public float Shrapnel;          // glass-breaking radius of a pipe bomb
            public Placement Placement;
            public bool ByObject;
            public bool BreaksConcrete;
            public bool Thermite;
            public int BlastId;
            public Destructible Container;  // the bin / mailbox / car it was placed in or under
        }

        /// <summary>The blast wave reaches this many radii for glass.</summary>
        public const float WaveRadii = 2.5f;
        /// <summary>Within this share of the radius the player is knocked down.</summary>
        public const float KnockdownShare = 0.6f;

        static readonly Collider[] hits = new Collider[256];
        static readonly HashSet<Destructible> seen = new HashSet<Destructible>();
        static readonly HashSet<Rigidbody> pushed = new HashSet<Rigidbody>();

        public static void Detonate(Charge c, Destructible source = null)
        {
            var arena = GameController.I != null ? GameController.I.ArenaDef : null;
            float echo = arena != null ? arena.EchoMult : 1f;
            float radius = c.Radius;
            float wave = Mathf.Max(radius * WaveRadii * echo, c.Shrapnel);

            Fx.Explosion(c.Position, radius);
            Sfx.I.Boom(c.Position, radius, arena != null && !arena.Outdoors ? 1.6f : 1f);
            if (StreetLife.I != null) StreetLife.I.OnBlast(c.Position, radius);

            int n = Physics.OverlapSphereNonAlloc(c.Position, Mathf.Max(wave, radius * 2f), hits, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            if (GameController.I != null && GameController.I.Shot != "") Debug.Log("BoomYard: blast at " + c.Position + " r " + radius + " dmg " + c.Damage + " concrete " + c.BreaksConcrete + " colliders " + n);
            seen.Clear();
            pushed.Clear();
            for (int i = 0; i < n; i++)
            {
                var col = hits[i];
                var d = col.GetComponentInParent<Destructible>();
                if (d != null && d != source && !d.Destroyed && seen.Add(d))
                {
                    float dist = Vector3.Distance(c.Position, col.ClosestPoint(c.Position));
                    float amount = 0f;
                    if (dist <= radius) amount = c.Damage * (1f - 0.7f * dist / radius);
                    else if (d.IsGlass && dist <= wave) amount = c.Damage * 0.15f * (1f - (dist - radius) / Mathf.Max(0.01f, wave - radius)) + d.Def.Hp * 0.5f;
                    if (d == c.Container) amount *= 1.5f;
                    if (GameController.I != null && GameController.I.Shot == "demolition") Debug.Log("BoomYard: blast reaches " + col.name + " of " + d.name + " at " + dist.ToString("0.00") + " m: " + amount.ToString("0"));
                    if (amount > 0f)
                        d.Damage(new Hit
                        {
                            Amount = amount, Origin = c.Position, Placement = c.Placement, ByObject = c.ByObject,
                            BreaksConcrete = c.BreaksConcrete, Thermite = c.Thermite, BlastId = c.BlastId,
                        });
                }

                var rb = col.attachedRigidbody;
                if (rb != null && !rb.isKinematic && pushed.Add(rb))
                {
                    float dv = Mathf.Min(16f, 1.5f + c.Damage / 40f) * Mathf.Clamp(Mathf.Sqrt(30f / Mathf.Max(0.5f, rb.mass)), 0.05f, 1.2f);
                    rb.AddExplosionForce(dv, c.Position, radius * 2f, 0.4f, ForceMode.VelocityChange);
                }
            }

            var player = Player.I;
            if (player != null) player.OnBlast(c.Position, radius, c.Damage, c.BlastId != 0);
        }
    }

    /// <summary>
    /// Fire spread (spec §06): every 0.8 s each fire lights flammable objects within 2 m. Also holds ground fires
    /// (sparkler bombs, molotov puddles) that burn in place for a while.
    /// </summary>
    public sealed class FireGrid : MonoBehaviour
    {
        public static FireGrid I { get; private set; }
        public const float SpreadEvery = 0.8f;
        public const float SpreadRadius = 2f;

        readonly List<Destructible> burning = new List<Destructible>();
        readonly List<GroundFire> ground = new List<GroundFire>();
        float tick;

        sealed class GroundFire { public Vector3 Pos; public float Radius, Left; public GameObject Fx; }

        public bool AnyBurning { get { return burning.Count > 0 || ground.Count > 0; } }

        void Awake() { I = this; }

        public void Add(Destructible d) { if (!burning.Contains(d)) burning.Add(d); }

        public void AddGround(Vector3 pos, float radius, float seconds)
        {
            var holder = new GameObject("GroundFire");
            holder.transform.position = pos;
            ground.Add(new GroundFire { Pos = pos, Radius = radius, Left = seconds, Fx = BoomYard.Game.Fx.Fire(holder.transform, Vector3.zero, Mathf.Clamp(radius, 0.5f, 2f)) });
            Destroy(holder, seconds + 0.5f);
        }

        public void Douse(Vector3 pos, float radius)
        {
            for (int i = burning.Count - 1; i >= 0; i--)
                if (burning[i] == null || Vector3.Distance(burning[i].transform.position, pos) <= radius + 1f)
                {
                    if (burning[i] != null) burning[i].Extinguish();
                    burning.RemoveAt(i);
                }
            for (int i = ground.Count - 1; i >= 0; i--)
                if (Vector3.Distance(ground[i].Pos, pos) <= radius + 1f)
                {
                    if (ground[i].Fx != null) Destroy(ground[i].Fx);
                    ground.RemoveAt(i);
                }
        }

        public void Clear()
        {
            burning.Clear();
            ground.Clear();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = burning.Count - 1; i >= 0; i--)
                if (burning[i] == null || !burning[i].BurnTick(dt)) burning.RemoveAt(i);
            for (int i = ground.Count - 1; i >= 0; i--)
            {
                ground[i].Left -= dt;
                if (ground[i].Left <= 0f) ground.RemoveAt(i);
            }

            tick += dt;
            if (tick < SpreadEvery) return;
            tick = 0f;
            foreach (var d in Destructible.All)
            {
                if (d.Burning || !d.Flammable || d.IsGlass) continue;
                if (d.Destroyed && (d.Def.Reaction & Reaction.Burn) == 0) continue;
                var p = d.transform.TransformPoint(d.FirePoint);
                if (Near(p)) d.Ignite(d.Def.BurnSeconds);
            }
        }

        bool Near(Vector3 p)
        {
            foreach (var b in burning)
                if (b != null && Vector3.Distance(b.transform.TransformPoint(b.FirePoint), p) <= SpreadRadius + b.FireSize * 0.5f) return true;
            foreach (var g in ground)
                if (Vector3.Distance(g.Pos, p) <= SpreadRadius * 0.5f + g.Radius) return true;
            return false;
        }
    }
}
