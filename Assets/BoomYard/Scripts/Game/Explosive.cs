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
            float fuseLen = 0.05f;
            Vector3 tip;
            switch (def.Id)
            {
                case "cherry":
                    Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.04f, Mats.Lit(new Color(0.6f, 0.08f, 0.06f), 0f, 0.5f));
                    tip = new Vector3(0f, 0.02f + fuseLen, 0f);
                    break;
                case "sparkler":
                    Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.06f, Mats.Lit(new Color(0.35f, 0.35f, 0.38f), 0.2f, 0.3f));
                    tip = new Vector3(0f, 0.03f + fuseLen, 0f);
                    break;
                case "dynamite":
                    for (int i = 0; i < 3; i++)
                        Part(root, PrimitiveType.Cylinder, new Vector3((i - 1) * 0.03f, 0f, 0f), new Vector3(0.028f, 0.1f, 0.028f), Mats.Lit(new Color(0.62f, 0.12f, 0.08f), 0f, 0.25f));
                    Part(root, PrimitiveType.Cube, Vector3.zero, new Vector3(0.095f, 0.03f, 0.032f), Mats.Lit(new Color(0.75f, 0.7f, 0.6f), 0f, 0.2f));
                    tip = new Vector3(0f, 0.1f + fuseLen, 0f);
                    break;
                case "keg":
                    Part(root, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.38f, 0.25f, 0.38f), Mats.Lit(Mats.Wood, 0f, 0.2f));
                    Part(root, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.39f, 0.02f, 0.39f), Mats.Lit(Mats.DarkMetal, 0.8f, 0.4f));
                    tip = new Vector3(0f, 0.25f + fuseLen, 0f);
                    break;
                case "string50":
                    for (int i = 0; i < 10; i++)
                        Part(root, PrimitiveType.Cylinder, new Vector3((i - 4.5f) * 0.012f, 0f, 0f), new Vector3(0.009f, 0.018f, 0.009f), Mats.Lit(new Color(0.75f, 0.1f, 0.08f), 0f, 0.3f));
                    tip = new Vector3(-0.07f, 0.02f, 0f);
                    break;
                case "m80":
                case "pipe":
                    var c = def.Id == "pipe" ? Mats.Lit(Mats.Steel, 0.9f, 0.5f) : Mats.Lit(new Color(0.16f, 0.17f, 0.2f), 0f, 0.3f);
                    Part(root, PrimitiveType.Cylinder, Vector3.zero, def.Id == "pipe" ? new Vector3(0.05f, 0.11f, 0.05f) : new Vector3(0.028f, 0.04f, 0.028f), c);
                    tip = new Vector3(0f, (def.Id == "pipe" ? 0.11f : 0.04f) + fuseLen, 0f);
                    break;
                default:
                    // Black Cat and anything without a model yet: a red paper tube with a white band.
                    Part(root, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.022f, 0.035f, 0.022f), Mats.Lit(new Color(0.61f, 0.16f, 0.13f), 0f, 0.3f));
                    Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.022f, 0f), new Vector3(0.0225f, 0.006f, 0.0225f), Mats.Lit(new Color(0.78f, 0.75f, 0.69f), 0f, 0.2f));
                    tip = new Vector3(0f, 0.035f + fuseLen, 0f);
                    break;
            }
            // The fuse: a thin cord from the body to the tip.
            var fuse = Part(root, PrimitiveType.Cylinder, tip - Vector3.up * fuseLen * 0.5f, new Vector3(0.003f, fuseLen * 0.5f, 0.003f), Mats.Lit(new Color(0.85f, 0.82f, 0.72f), 0f, 0.1f, false));
            fuse.name = "Fuse";
            var tipT = new GameObject("FuseTip").transform;
            tipT.SetParent(root.transform, false);
            tipT.localPosition = tip;
            return root;
        }

        static GameObject Part(GameObject root, PrimitiveType type, Vector3 pos, Vector3 scale, UnityEngine.Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
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

        Light glow;
        bool done;

        public static LiveExplosive Spawn(ExplosiveDef def, Vector3 pos, Quaternion rot, float remaining, Placement placement)
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

            var tip = go.transform.Find("FuseTip");
            if (tip != null && def.Fuse > 0f)
            {
                Fx.FuseSparks(tip);
                live.glow = tip.gameObject.AddComponent<Light>();
                live.glow.type = LightType.Point;
                live.glow.color = new Color(1f, 0.7f, 0.35f);
                live.glow.range = 1.5f;
                live.glow.intensity = 0.8f;
                tip.gameObject.AddComponent<Flicker>();
            }
            Active.Add(live);
            return live;
        }

        void OnDestroy() { Active.Remove(this); }

        void Update()
        {
            if (done) return;
            Remaining -= Time.deltaTime;
            if (Remaining <= 0f) Detonate();
        }

        void OnCollisionEnter(Collision c)
        {
            if (done) return;
            Airborne = false;
            if (Def.Fuse <= 0f) Detonate();   // molotov: breaks on contact
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
