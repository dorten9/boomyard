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
        Light flameLight;
        ParticleSystem sparks;
        float throwT = 1f, lightT, downT, sway;
        static readonly Vector3 LeftRest = new Vector3(-0.21f, -0.27f, 0.42f), RightRest = new Vector3(0.2f, -0.25f, 0.42f);
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
            var skin = Mats.Lit(Mats.Skin, 0f, 0.35f);
            var sleeve = Mats.Lit(new Color(0.12f, 0.13f, 0.15f), 0f, 0.2f);
            // Forearm reaching in from the bottom corner, a sleeve, a palm and a thumb.
            var fore = Part(arm, PrimitiveType.Capsule, new Vector3(side * 0.05f, -0.1f, -0.12f), new Vector3(0.06f, 0.14f, 0.06f), skin);
            fore.transform.localRotation = Quaternion.Euler(62f, side * -14f, 0f);
            var sl = Part(arm, PrimitiveType.Capsule, new Vector3(side * 0.09f, -0.16f, -0.24f), new Vector3(0.085f, 0.12f, 0.085f), sleeve);
            sl.transform.localRotation = Quaternion.Euler(62f, side * -14f, 0f);
            Part(arm, PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.075f, 0.05f, 0.085f), skin);
            var thumb = Part(arm, PrimitiveType.Capsule, new Vector3(-side * 0.03f, 0.018f, 0.02f), new Vector3(0.022f, 0.03f, 0.022f), skin);
            thumb.transform.localRotation = Quaternion.Euler(0f, 0f, side * 50f);
            arm.localPosition = isLeft ? LeftRest : RightRest;
            return arm;
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
            go.transform.localRotation = Quaternion.Euler(-10f, 0f, 28f);
            float big = def.Id == "keg" ? 0.35f : 1f;   // the keg is carried small in the placeholder rig
            go.transform.localScale = Vector3.one * big;
            item = go.transform;
            fuseTip = go.transform.Find("FuseTip");
        }

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
            sway += dt;

            float l = Mathf.SmoothStep(0f, 1f, lightT);
            var lp = Vector3.Lerp(LeftRest, LeftLight, l);
            var rp = Vector3.Lerp(RightRest, RightLight, l);
            if (FlameOn && !Lighting) lp += new Vector3(0.03f, 0.05f, 0f);   // lighter lifted and lit, nothing to light

            // Throw: the right hand swings forward and up, then comes back.
            float swing = Mathf.Sin(throwT * Mathf.PI);
            rp += new Vector3(-0.05f, 0.15f, 0.2f) * swing;

            // Hands shake after a close blast and drop out of view on the ground.
            var tremble = new Vector3(Mathf.PerlinNoise(sway * 14f, 0f) - 0.5f, Mathf.PerlinNoise(0f, sway * 14f) - 0.5f, 0f) * 0.02f * downT;
            var drop = new Vector3(0f, -0.25f, 0f) * downT;
            var breathe = new Vector3(0f, Mathf.Sin(sway * 1.3f) * 0.003f, 0f);

            left.localPosition = lp + tremble + drop + breathe;
            right.localPosition = rp - tremble + drop + breathe;
            left.localRotation = Quaternion.Euler(-10f * l, 10f * l, -12f * l);
            right.localRotation = Quaternion.Euler(-25f * swing - 8f * l, -12f * l, 20f * l);

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
