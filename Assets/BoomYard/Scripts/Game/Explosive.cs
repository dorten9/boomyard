using System.Collections;
using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>Placeholder models of the arsenal, built from primitives (replaced by the PBR models in M4).</summary>
    public static class ItemModel
    {
        /// <summary>Builds the model with a child "FuseTip" at the end of the fuse. Size is real (metres).</summary>
        public static GameObject Build(ExplosiveDef def)
        {
            var root = new GameObject(def.Id);
            var paper = Mats.Surface("paint", new Color(0.62f, 0.13f, 0.1f), 0f, 0.35f, 0.1f, 0.4f);
            var band = Mats.Lit(new Color(0.82f, 0.78f, 0.68f), 0f, 0.25f);
            var cap = Mats.Lit(new Color(0.75f, 0.7f, 0.6f), 0f, 0.2f);
            float fuseLen = 0.05f;
            Vector3 tip;
            switch (def.Id)
            {
                case "cherry":
                    Part(root, MeshKit.Blob(0.022f, 0.05f, 900), Vector3.zero, Mats.Lit(new Color(0.58f, 0.07f, 0.05f), 0f, 0.55f));
                    tip = new Vector3(0f, 0.022f + fuseLen, 0f);
                    break;
                case "sparkler":
                    Part(root, MeshKit.Blob(0.032f, 0.08f, 901), Vector3.zero, Mats.Lit(new Color(0.35f, 0.35f, 0.38f), 0.3f, 0.35f));
                    tip = new Vector3(0f, 0.032f + fuseLen, 0f);
                    break;
                case "dynamite":
                    for (int i = 0; i < 3; i++) Tube(root, new Vector3((i - 1) * 0.029f, -0.1f, 0f), 0.0145f, 0.2f, Mats.Lit(new Color(0.62f, 0.12f, 0.08f), 0f, 0.3f), cap);
                    Part(root, MeshKit.RoundedBox(new Vector3(0.094f, 0.03f, 0.033f), 0.012f), Vector3.zero, Mats.Lit(new Color(0.15f, 0.15f, 0.16f), 0f, 0.5f));
                    tip = new Vector3(0f, 0.1f + fuseLen, 0f);
                    break;
                case "keg":
                    Part(root, MeshKit.Lathe("keg", new[] { new Vector2(0.17f, -0.25f), new Vector2(0.2f, -0.12f), new Vector2(0.21f, 0f), new Vector2(0.2f, 0.12f), new Vector2(0.17f, 0.25f) }, 20, true, true), Vector3.zero, Mats.Surface("wood", Color.white, 0f, 0.6f, 0.3f));
                    for (int i = -1; i <= 1; i += 2) Part(root, MeshKit.Lathe("keghoop", new[] { new Vector2(0.205f, -0.012f), new Vector2(0.212f, 0f), new Vector2(0.205f, 0.012f) }, 20), new Vector3(0f, i * 0.16f, 0f), Mats.Lit(Mats.DarkMetal, 0.8f, 0.4f));
                    tip = new Vector3(0f, 0.25f + fuseLen, 0f);
                    break;
                case "string50":
                    for (int i = 0; i < 10; i++)
                    {
                        var c = Tube(root, new Vector3((i - 4.5f) * 0.012f, -0.009f, 0f), 0.0045f, 0.018f, paper, band);
                        c.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 8f : -8f));
                    }
                    tip = new Vector3(-0.07f, 0.02f, 0f);
                    break;
                case "m80":
                    Tube(root, new Vector3(0f, -0.04f, 0f), 0.014f, 0.08f, Mats.Lit(new Color(0.16f, 0.17f, 0.2f), 0f, 0.35f), cap);
                    tip = new Vector3(0f, 0.04f + fuseLen, 0f);
                    break;
                case "pipe":
                    Tube(root, new Vector3(0f, -0.11f, 0f), 0.025f, 0.22f, Mats.Lit(Mats.Steel, 0.9f, 0.55f), Mats.Lit(new Color(0.3f, 0.3f, 0.32f), 0.9f, 0.4f));
                    tip = new Vector3(0f, 0.11f + fuseLen, 0f);
                    break;
                default:
                    // Black Cat: a red paper tube with a cream band and clay end caps.
                    Tube(root, new Vector3(0f, -0.035f, 0f), 0.011f, 0.07f, paper, cap);
                    Part(root, MeshKit.Lathe("bcband", new[] { new Vector2(0.0115f, -0.008f), new Vector2(0.0115f, 0.008f) }, 16), new Vector3(0f, 0.012f, 0f), band);
                    tip = new Vector3(0f, 0.035f + fuseLen, 0f);
                    break;
            }
            // The fuse: a thin waxed cord from the body to the tip, slightly bent.
            var fuse = Part(root, MeshKit.Lathe("fuse", new[] { new Vector2(0.0018f, 0f), new Vector2(0.0018f, fuseLen) }, 6), tip - Vector3.up * fuseLen, Mats.Lit(new Color(0.55f, 0.5f, 0.36f), 0f, 0.35f));
            fuse.transform.localRotation = Quaternion.Euler(0f, 0f, 9f);
            fuse.name = "Fuse";
            var tipT = new GameObject("FuseTip").transform;
            tipT.SetParent(root.transform, false);
            tipT.localPosition = tip + new Vector3(-Mathf.Sin(9f * Mathf.Deg2Rad) * fuseLen, 0f, 0f);
            return root;
        }

        /// <summary>A paper or steel tube standing on <paramref name="bottom"/>, with end caps.</summary>
        static GameObject Tube(GameObject root, Vector3 bottom, float r, float h, UnityEngine.Material body, UnityEngine.Material ends)
        {
            var t = Part(root, MeshKit.Lathe("tube" + r + h, new[] { new Vector2(r * 0.92f, 0f), new Vector2(r, r * 0.15f), new Vector2(r, h - r * 0.15f), new Vector2(r * 0.92f, h) }, 16), bottom, body);
            Part(t, MeshKit.Lathe("cap" + r, new[] { new Vector2(r * 0.93f, 0f), new Vector2(r * 0.5f, 0.0015f), new Vector2(0f, 0.002f) }, 16), new Vector3(0f, h, 0f), ends);
            return t;
        }

        static GameObject Part(GameObject root, Mesh mesh, Vector3 pos, UnityEngine.Material mat)
        {
            var go = Props.Part(root.transform, mesh, pos, mat, false);
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        /// <summary>Colour of the item's chip in the pocket bar.</summary>
        public static Color Chip(string id)
        {
            switch (id)
            {
                case "m80": return new Color(0.2f, 0.22f, 0.26f);
                case "sparkler": return new Color(0.45f, 0.45f, 0.5f);
                case "dynamite": return new Color(0.7f, 0.15f, 0.1f);
                case "keg": return Mats.Wood;
                case "pipe": return Mats.Steel;
                default: return new Color(0.61f, 0.16f, 0.13f);
            }
        }
    }

    /// <summary>
    /// A lit explosive out of the hand (spec §04, §08): thrown, placed or pushed into something. The fuse keeps
    /// burning; on zero it detonates with the placement it ended up in (airburst if it is still in the air).
    /// </summary>
    public sealed class LiveExplosive : MonoBehaviour
    {
        public static readonly List<LiveExplosive> Active = new List<LiveExplosive>();
        static int nextBlastId = 1;

        public ExplosiveDef Def;
        public float Remaining;
        public Placement Placement;
        public Destructible Container;
        public bool Airborne;
        public int BlastId;
        public float ReleasedWith;   // fuse left when it left the hand, for the Last Second bonus
        /// <summary>Placed items wait unlit until the player lights them (place → light → run).</summary>
        public bool Lit { get; private set; }

        Light glow;
        GameObject marker;
        bool done;

        /// <summary>An unlit item lying in the world, waiting for the lighter.</summary>
        public static LiveExplosive SpawnUnlit(ExplosiveDef def, Vector3 pos, Quaternion rot, Placement placement)
        {
            var live = Spawn(def, pos, rot, BoomYard.Core.Fuse.TotalBurn(def) > 0f ? BoomYard.Core.Fuse.TotalBurn(def) : 8f, placement, false);
            // A soft ring on the ground so placed charges are easy to find again.
            live.marker = new GameObject("Marker");
            live.marker.transform.SetParent(live.transform, false);
            var lr = live.marker.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mats.ParticleAdd;
            lr.startColor = lr.endColor = new Color(1f, 0.55f, 0.15f, 0.9f);
            lr.widthMultiplier = 0.012f;
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 24;
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.14f, 0.005f, Mathf.Sin(a) * 0.14f));
            }
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return live;
        }

        public static LiveExplosive Spawn(ExplosiveDef def, Vector3 pos, Quaternion rot, float remaining, Placement placement, bool lit = true)
        {
            var go = ItemModel.Build(def);
            go.transform.SetPositionAndRotation(pos, rot);
            var live = go.AddComponent<LiveExplosive>();
            live.Def = def;
            live.Remaining = remaining;
            live.ReleasedWith = remaining;
            live.Placement = placement;
            live.BlastId = nextBlastId++;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = def.Mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Collider col;
            if (def.Rolls || def.Id == "sparkler") col = go.AddComponent<SphereCollider>();
            else col = go.AddComponent<CapsuleCollider>();
            if (col is SphereCollider) ((SphereCollider)col).radius = def.Id == "cherry" ? 0.02f : 0.03f;
            if (col is CapsuleCollider)
            {
                var cap = (CapsuleCollider)col;
                cap.radius = def.Id == "keg" ? 0.19f : 0.02f;
                cap.height = def.Id == "keg" ? 0.5f : 0.08f;
            }
            go.layer = Layers.Default;

            Active.Add(live);
            if (lit) live.Light();
            return live;
        }

        /// <summary>The fuse catches: sparks, a glow, the hiss, and the countdown starts.</summary>
        public void Light()
        {
            if (Lit || done) return;
            Lit = true;
            if (marker != null) Destroy(marker);
            var tip = transform.Find("FuseTip");
            if (tip != null && Def.Fuse > 0f)
            {
                Fx.FuseSparks(tip);
                glow = tip.gameObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.7f, 0.35f);
                glow.range = 1.5f;
                glow.intensity = 0.8f;
                tip.gameObject.AddComponent<Flicker>();
                var hiss = tip.gameObject.AddComponent<AudioSource>();
                Sfx.I.FuseOn(hiss);
            }
        }

        public Vector3 FuseTipWorld
        {
            get { var tip = transform.Find("FuseTip"); return tip != null ? tip.position : transform.position; }
        }

        /// <summary>Unlit charges in the world, for the "light it" prompt and the tutorial.</summary>
        public static int UnlitCount
        {
            get { int n = 0; foreach (var l in Active) if (l != null && !l.Lit) n++; return n; }
        }

        public static int LitCount
        {
            get { int n = 0; foreach (var l in Active) if (l != null && l.Lit) n++; return n; }
        }

        void OnDestroy() { Active.Remove(this); }

        void Update()
        {
            if (done || !Lit) return;
            Remaining -= Time.deltaTime;
            if (Remaining <= 0f) Detonate();
        }

        void OnCollisionEnter(Collision c)
        {
            if (done) return;
            Airborne = false;
            if (Def.Fuse <= 0f && Lit) Detonate();   // molotov: breaks on contact
        }

        public void Detonate()
        {
            if (done) return;
            done = true;
            var pos = transform.position;
            var placement = Placement;
            if (placement == Placement.Thrown && Airborne && pos.y > 0.35f) placement = Placement.Airburst;
            if (GameController.I != null) GameController.I.OnPlayerDetonation(this, placement);
            if (Def.Units > 1) GameController.I.StartCoroutine(Sequence(Def, pos, placement, Container, BlastId));
            else Charge(Def, pos, placement, Container, BlastId);
            if (Def.BurnSeconds > 0f) FireGrid.I.AddGround(pos, Def.Radius, Def.BurnSeconds);
            Destroy(gameObject);
        }

        static void Charge(ExplosiveDef def, Vector3 pos, Placement placement, Destructible container, int blastId)
        {
            Blast.Detonate(new Blast.Charge
            {
                Position = pos, Damage = def.Damage, Radius = def.Radius, Shrapnel = def.ShrapnelRadius, Placement = placement,
                BreaksConcrete = def.BreaksConcrete, Thermite = def.Id == "thermite", BlastId = blastId, Container = container,
            });
        }

        /// <summary>Strings, cakes and clusters: one small charge after another, scattered along the line or around.</summary>
        static IEnumerator Sequence(ExplosiveDef def, Vector3 pos, Placement placement, Destructible container, int blastId)
        {
            var dir = Random.insideUnitCircle.normalized;
            for (int i = 0; i < def.Units; i++)
            {
                Vector3 p = pos;
                if (def.Id == "string50") p += new Vector3(dir.x, 0f, dir.y) * (i * 0.04f) + Random.insideUnitSphere * 0.1f;
                else p += Random.insideUnitSphere * Mathf.Min(def.Radius, 2f);
                p.y = Mathf.Max(0.05f, p.y);
                Charge(def, p, placement, container, blastId);
                yield return new WaitForSeconds(def.UnitInterval);
            }
        }
    }
}
