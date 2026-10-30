using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// The hands (spec §15: "the hands are the star"). Placeholder rig from primitives until the 12K-poly model:
    /// lighter in the left hand, the item in the right. Poses are blended procedurally: rest, lighting (both hands
    /// meet so the flame touches the fuse), throw swing, and down after a blast.
    /// </summary>
    public sealed class Hands : MonoBehaviour
    {
        public bool FlameOn, Lighting, Down;

        Transform left, right, item, flame, fuseTip;
        readonly List<Renderer> handRenderers = new List<Renderer>();
        Renderer lighterBody;
        UnityEngine.Material skinMat;
        Light flameLight;
        ParticleSystem sparks;
        float throwT = 1f, lightT, downT, sway;
        static readonly Vector3 LeftRest = new Vector3(-0.21f, -0.27f, 0.42f), RightRest = new Vector3(0.2f, -0.25f, 0.42f);
        // Where each fist closes: round the lighter in the left hand, round the item in the right (tilted with it).
        static readonly Vector3 LeftGrip = new Vector3(0.012f, 0.04f, 0.02f), RightGrip = new Vector3(0f, 0.033f, 0.035f);
        static readonly Quaternion ItemTilt = Quaternion.Euler(-10f, 0f, 28f);
        static readonly Vector3 LeftLight = new Vector3(-0.05f, -0.2f, 0.36f), RightLight = new Vector3(0.035f, -0.16f, 0.38f);

        /// <summary>World position of the held item (where a thrown item leaves from).</summary>
        public Vector3 ItemWorld { get { return item != null ? item.position : right.TransformPoint(new Vector3(0f, 0.06f, 0.05f)); } }

        public static Hands Build(Transform cam)
        {
            var root = new GameObject("Hands");
            root.transform.SetParent(cam, false);
            var h = root.AddComponent<Hands>();
            h.left = h.Arm("Left", true);
            h.right = h.Arm("Right", false);

            // Lighter: body, steel top, flame and its light.
            var lighter = Part(h.left, PrimitiveType.Cube, new Vector3(0.012f, 0.05f, 0.02f), new Vector3(0.024f, 0.05f, 0.012f), Mats.Lit(new Color(0.36f, 0.37f, 0.4f), 0.3f, 0.5f));
            h.lighterBody = lighter.GetComponent<Renderer>();
            Part(lighter.transform, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(0.95f, 0.25f, 1f), Mats.Lit(Mats.Steel, 0.9f, 0.6f));
            // A quad with white vertex colours (primitive meshes have none, and the particle shader multiplies them in).
            var f = new GameObject("Flame");
            f.AddComponent<MeshFilter>().sharedMesh = Mats.Quad;
            f.AddComponent<MeshRenderer>();
            f.transform.SetParent(h.left, false);
            f.transform.localPosition = new Vector3(0.012f, 0.1f, 0.02f);
            f.transform.localScale = new Vector3(0.022f, 0.045f, 1f);
            var fr = f.GetComponent<Renderer>();
            fr.sharedMaterial = Mats.ParticleAdd;
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Tint", new Color(1f, 0.7f, 0.3f, 1f));
            fr.SetPropertyBlock(mpb);
            h.flame = f.transform;
            h.flameLight = f.AddComponent<Light>();
            h.flameLight.type = LightType.Point;
            h.flameLight.color = new Color(1f, 0.62f, 0.3f);
            h.flameLight.range = 3.5f;
            h.flameLight.intensity = 1.4f;
            f.SetActive(false);
            return h;
        }

        Transform Arm(string name, bool isLeft)
        {
            var arm = new GameObject(name).transform;
            arm.SetParent(transform, false);
            float side = isLeft ? -1f : 1f;
            var skin = skinMat != null ? skinMat : (skinMat = Mats.Lit(Mats.Skin, 0f, 0.35f));
            var sleeve = Mats.Lit(new Color(0.12f, 0.13f, 0.15f), 0f, 0.2f);
            // Forearm reaching in from the bottom corner and a sleeve.
            var fore = Part(arm, PrimitiveType.Capsule, new Vector3(side * 0.05f, -0.1f, -0.12f), new Vector3(0.06f, 0.14f, 0.06f), skin);
            fore.transform.localRotation = Quaternion.Euler(62f, side * -14f, 0f);
            var sl = Part(arm, PrimitiveType.Capsule, new Vector3(side * 0.09f, -0.16f, -0.24f), new Vector3(0.085f, 0.12f, 0.085f), sleeve);
            sl.transform.localRotation = Quaternion.Euler(62f, side * -14f, 0f);

            // A fist closed around what the hand holds (Core/HandRig), its axis along the held object.
            var grip = new GameObject("Grip").transform;
            grip.SetParent(arm, false);
            grip.localPosition = isLeft ? LeftGrip : RightGrip;
            grip.localRotation = isLeft ? Quaternion.identity : ItemTilt;
            foreach (var p in HandRig.Fist(side))
            {
                GameObject piece;
                if (p.Kind == HandPartKind.Bone) piece = Bone(grip, V(p.A), V(p.B), p.Diameter, skin);
                else
                {
                    piece = Part(grip, PrimitiveType.Sphere, V(p.A), V(p.Size), skin);
                    piece.transform.localRotation = Quaternion.Euler(0f, p.Yaw, 0f);
                }
                handRenderers.Add(piece.GetComponent<Renderer>());
            }
            // Wrist: from under the back of the hand to the top of the forearm.
            var wrist = arm.InverseTransformPoint(grip.TransformPoint(V(HandRig.WristTop(side))));
            var elbowSide = fore.transform.localPosition + fore.transform.localRotation * Vector3.up * 0.11f;
            handRenderers.Add(Bone(arm, wrist, elbowSide, 0.046f, skin).GetComponent<Renderer>());
            arm.localPosition = isLeft ? LeftRest : RightRest;
            return arm;
        }

        static Vector3 V(Vec3 v) { return new Vector3(v.X, v.Y, v.Z); }

        /// <summary>Gloves on the fists (or bare skin) and the lighter's finish, from the shop's style items.</summary>
        public void ApplyStyle(string glove, string lighterSkin)
        {
            Look g;
            var hand = Styles.Glove(glove, out g) ? Mats.Lit(new Color(g.R, g.G, g.B), g.Metal, g.Smooth) : skinMat;
            foreach (var r in handRenderers) if (r != null) r.sharedMaterial = hand;
            var l = Styles.Lighter(lighterSkin);
            if (lighterBody != null) lighterBody.sharedMaterial = Mats.Lit(new Color(l.R, l.G, l.B), l.Metal, l.Smooth);
        }

        /// <summary>A capsule from a to b (both ends rounded), for finger bones and the wrist.</summary>
        static GameObject Bone(Transform parent, Vector3 a, Vector3 b, float d, UnityEngine.Material mat)
        {
            var dir = b - a;
            var go = Part(parent, PrimitiveType.Capsule, (a + b) * 0.5f, new Vector3(d, (dir.magnitude + d) * 0.5f, d), mat);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            return go;
        }

        static GameObject Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, UnityEngine.Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.layer = Layers.NoRaycast;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public void SetItem(ExplosiveDef def)
        {
            if (item != null) Destroy(item.gameObject);
            item = null;
            fuseTip = null;
            sparks = null;
            if (def == null) return;
            var go = ItemModel.Build(def);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.SetParent(right, false);
            go.transform.localPosition = new Vector3(0.0f, 0.045f, 0.035f);
            go.transform.localRotation = ItemTilt;
            float big = def.Id == "keg" ? 0.35f : 1f;   // the keg is carried small in the placeholder rig
            go.transform.localScale = Vector3.one * big;
            item = go.transform;
            fuseTip = go.transform.Find("FuseTip");
        }

        /// <summary>The spray can in the right hand (its cap the paint's colour), or null to put it away.</summary>
        public void SetCan(SprayColor c)
        {
            if (can != null) { Destroy(can.gameObject); can = null; mist = null; }
            if (c == null) return;
            SetItem(null);
            var go = new GameObject("SprayCan");
            go.transform.SetParent(right, false);
            go.transform.localPosition = new Vector3(0f, -0.04f, 0.035f);
            go.transform.localRotation = Quaternion.Euler(12f, 0f, 8f);   // upright, the nozzle facing ahead
            var paint = Mats.Lit(new Color(c.R, c.G, c.B), c.Id == "chrome" || c.Id == "gold" ? 0.9f : 0.3f, 0.7f);
            var steel = Mats.Lit(Mats.Steel, 0.9f, 0.6f);
            Can(go.transform, MeshKit.Lathe("spraycan", new[] { new Vector2(0.024f, 0f), new Vector2(0.026f, 0.006f), new Vector2(0.026f, 0.15f), new Vector2(0.02f, 0.165f), new Vector2(0.008f, 0.172f), new Vector2(0f, 0.172f) }, 18, true, true), Vector3.zero, paint);
            Can(go.transform, MeshKit.Lathe("spraycap", new[] { new Vector2(0.007f, 0f), new Vector2(0.007f, 0.014f), new Vector2(0f, 0.015f) }, 10, true), new Vector3(0f, 0.17f, 0f), Mats.Lit(new Color(0.9f, 0.9f, 0.88f), 0f, 0.4f));
            Can(go.transform, MeshKit.RoundedBox(new Vector3(0.054f, 0.03f, 0.054f), 0.02f, 2), new Vector3(0f, 0.12f, 0f), steel);
            can = go.transform;
            mist = Fx.SprayMist(can, new Vector3(0f, 0.18f, 0.01f), new Color(c.R, c.G, c.B, 0.35f));
        }

        static void Can(Transform parent, Mesh mesh, Vector3 at, UnityEngine.Material mat)
        {
            var go = new GameObject(mesh.name);
            go.layer = Layers.NoRaycast;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        Transform can;
        ParticleSystem mist;
        /// <summary>The nozzle is pressed: the arm reaches forward and the mist comes out.</summary>
        public bool Spraying;
        float sprayT;

        public void SetLit(bool lit)
        {
            if (lit && fuseTip != null && sparks == null) sparks = Fx.FuseSparks(fuseTip);
            if (!lit && sparks != null) { Destroy(sparks.gameObject); sparks = null; }
        }

        public void Throw() { throwT = 0f; }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            lightT = Mathf.MoveTowards(lightT, Lighting ? 1f : 0f, dt * 5f);
            downT = Mathf.MoveTowards(downT, Down ? 1f : 0f, dt * 4f);
            throwT = Mathf.Min(1f, throwT + dt * 3.5f);
            sprayT = Mathf.MoveTowards(sprayT, can != null ? (Spraying ? 1f : 0.5f) : 0f, dt * 5f);
            if (mist != null)
            {
                var em = mist.emission;
                em.enabled = Spraying && can != null;
                mist.transform.rotation = Quaternion.LookRotation(Spray.I != null ? Spray.I.AimDir : transform.parent.forward);   // at the paint
            }
            sway += dt;

            float l = Mathf.SmoothStep(0f, 1f, lightT);
            var lp = Vector3.Lerp(LeftRest, LeftLight, l);
            var rp = Vector3.Lerp(RightRest, RightLight, l);
            if (FlameOn && !Lighting) lp += new Vector3(0.03f, 0.05f, 0f);   // lighter lifted and lit, nothing to light

            // Throw: the right hand swings forward and up, then comes back.
            float swing = Mathf.Sin(throwT * Mathf.PI);
            rp += new Vector3(-0.05f, 0.15f, 0.2f) * swing;
            // The can held up and out towards the wall, the nozzle pointing ahead.
            rp += new Vector3(-0.05f, 0.06f, 0.08f) * sprayT;

            // Hands shake after a close blast and drop out of view on the ground.
            var tremble = new Vector3(Mathf.PerlinNoise(sway * 14f, 0f) - 0.5f, Mathf.PerlinNoise(0f, sway * 14f) - 0.5f, 0f) * 0.02f * downT;
            var drop = new Vector3(0f, -0.25f, 0f) * downT;
            var breathe = new Vector3(0f, Mathf.Sin(sway * 1.3f) * 0.003f, 0f);

            left.localPosition = lp + tremble + drop + breathe;
            right.localPosition = rp - tremble + drop + breathe;
            left.localRotation = Quaternion.Euler(-10f * l, 10f * l, -12f * l);
            right.localRotation = Quaternion.Euler(-25f * swing - 8f * l - 8f * sprayT, -12f * l, 20f * l - 24f * sprayT);

            flame.gameObject.SetActive(FlameOn);
            if (FlameOn)
            {
                float n = Mathf.PerlinNoise(sway * 12f, 3f);
                flame.localScale = new Vector3(0.02f + 0.006f * n, 0.04f + 0.02f * n, 1f);
                flame.rotation = Quaternion.LookRotation(flame.position - Camera.main.transform.position, Vector3.up);
                flameLight.intensity = 1.1f + 0.7f * n;
            }
        }
    }
}
