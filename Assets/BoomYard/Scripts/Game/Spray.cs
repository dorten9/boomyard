using System.Collections;
using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// Graffiti. With the can in hand, a finger dragged over the screen paints where it is (on a computer: the
    /// left mouse button, or SPRAY/F at the crosshair), and a quick tap stamps the chosen stencil where it lands,
    /// cell by cell, wrapping round corners. Like a real can, the line is wider and lighter from further away,
    /// builds up where the can lingers and, held still on a wall, the paint starts to run. Paint goes on
    /// anything that does not break (walls, roads, pavements), drawn in a few big meshes so a whole wall of it
    /// costs a handful of draw calls. It lasts until the level is left.
    /// </summary>
    public sealed class Spray : MonoBehaviour
    {
        public static Spray I { get; private set; }

        /// <summary>The can is in the hand (instead of an explosive).</summary>
        public bool Active { get; private set; }
        public bool Painting { get; private set; }
        public const float Reach = 7f;
        /// <summary>Dabs a second while the can stays on one spot (paint builds up at this rate, whatever the frame rate).</summary>
        const float DabRate = 45f;
        /// <summary>SPRAY held by a screenshot run.</summary>
        public bool HeldForShot;
        /// <summary>Bumped when a drawing or picture changes, so the cached stencil is rebuilt.</summary>
        public static int StencilsVersion;

        readonly Dictionary<int, PaintLayer> layers = new Dictionary<int, PaintLayer>();
        Transform root;
        Vector3 lastPoint, dwellPoint;
        bool hasLast;
        float nextStamp, dabClock, dwell;
        int dabs;
        string cachedId;
        int cachedVersion = -1;
        Stencil cached;

        public static Spray Create(GameObject host)
        {
            var s = host.AddComponent<Spray>();
            I = s;
            return s;
        }

        public SprayColor Color { get { return Graffiti.Color(SaveService.Data.sprayColor); } }
        public SprayCap Cap { get { return Graffiti.Cap(SaveService.Data.sprayCap); } }

        /// <summary>The stencil set on the can (shop, drawing or picture), or null for freehand.</summary>
        public Stencil Stencil
        {
            get
            {
                var id = SaveService.Data.stencil;
                if (id != cachedId || cachedVersion != StencilsVersion)
                {
                    cachedId = id;
                    cachedVersion = StencilsVersion;
                    cached = SaveService.Data.StencilById(id);
                }
                return cached;
            }
        }

        public void SetActive(bool on)
        {
            if (Active == on) return;
            Active = on;
            Release();
            if (on) Sfx.I.Rattle();
        }

        /// <summary>A new level: the old paint went with the old street.</summary>
        public void Clear()
        {
            layers.Clear();
            root = null;
            Active = false;
            Release();
        }

        public void Release()
        {
            Painting = false;
            hasLast = false;
            dwell = 0f;
            Sfx.I.Spray(false);
        }

        // ---------- freehand ----------

        /// <summary>One frame of spraying along <paramref name="ray"/> (from the finger, or the crosshair).</summary>
        public void Paint(Ray ray)
        {
            Painting = true;
            Sfx.I.Spray(true);
            RaycastHit hit;
            if (!Surface(ray, Reach, out hit)) { hasLast = false; return; }
            var col = Color;
            float dist = hit.distance;
            float spread = 0.55f + 0.2f * dist;
            float size = Cap.Width * spread;
            float alpha = Mathf.Clamp01(0.42f / spread);
            var p = hit.point;
            dabs = 0;
            if (hasLast && (p - lastPoint).sqrMagnitude < 0.8f * 0.8f && (p - lastPoint).sqrMagnitude > size * size * 0.0025f)
            {
                // A stroke: dabs all along the way from the last spot, each on whatever surface is under it.
                float step = Mathf.Max(0.006f, size * 0.18f);
                float len = Vector3.Distance(lastPoint, p);
                for (float d = step; d <= len && dabs < 64; d += step)
                {
                    var q = Vector3.Lerp(lastPoint, p, d / len);
                    RaycastHit h2;
                    if (Surface(new Ray(q + hit.normal * 0.25f, -hit.normal), 0.5f, out h2)) Dab(h2.point, h2.normal, size, col.R, col.G, col.B, col.Glow, alpha, false);
                }
            }
            else
            {
                // Holding still: the paint builds up at a steady rate.
                dabClock += Time.deltaTime * DabRate;
                if (!hasLast) dabClock = Mathf.Max(dabClock, 1f);
                while (dabClock >= 1f && dabs < 8) { dabClock -= 1f; Dab(p, hit.normal, size, col.R, col.G, col.B, col.Glow, alpha, false); }
            }
            // Too much paint in one spot on a wall runs.
            if ((p - dwellPoint).sqrMagnitude < size * size * 0.25f) dwell += Time.deltaTime;
            else { dwell = 0f; dwellPoint = p; }
            if (dwell > 0.7f && Mathf.Abs(hit.normal.y) < 0.5f)
            {
                dwell = 0.2f;
                StartCoroutine(Drip(p, hit.normal, size, col));
            }
            lastPoint = p;
            hasLast = true;
        }

        /// <summary>A run of paint down the wall from where the can lingered.</summary>
        IEnumerator Drip(Vector3 from, Vector3 n, float size, SprayColor c)
        {
            var down = Vector3.ProjectOnPlane(Vector3.down, n).normalized;
            float length = Random.Range(0.12f, 0.45f), w = Mathf.Max(0.012f, size * 0.12f), y = 0f;
            var at = from + Vector3.Cross(n, down) * Random.Range(-size, size) * 0.25f;
            while (y < length)
            {
                y += w * 0.5f;
                RaycastHit h;
                var q = at + down * y;
                if (!Surface(new Ray(q + n * 0.2f, -n), 0.4f, out h)) yield break;
                Layer(c.Glow, false).Add(h.point + n * 0.011f, h.normal, w * (1f - 0.4f * y / length), new Color(c.R, c.G, c.B, 0.8f), 0f);
                yield return new WaitForSeconds(0.03f);
            }
        }

        /// <summary>Something paint sticks to: solid and not breakable (a wall, the road, a pavement).</summary>
        static bool Surface(Ray ray, float reach, out RaycastHit hit)
        {
            if (!Physics.Raycast(ray, out hit, reach, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.collider.GetComponentInParent<Destructible>() != null) return false;
            if (hit.collider.GetComponentInParent<LiveExplosive>() != null) return false;
            return hit.collider.GetComponent<MeshRenderer>() != null || hit.collider.GetComponentInChildren<MeshRenderer>() != null;
        }

        void Dab(Vector3 at, Vector3 normal, float size, float r, float g, float b, bool glow, float alpha, bool stencil)
        {
            dabs++;
            var jitter = Random.insideUnitSphere * size * 0.12f;
            jitter -= normal * Vector3.Dot(jitter, normal);
            Layer(glow, stencil).Add(at + jitter + normal * 0.01f, normal, size, new Color(r, g, b, alpha), Random.Range(0f, 360f));
        }

        PaintLayer Layer(bool glow, bool stencil)
        {
            int key = (glow ? 1 : 0) + (stencil ? 2 : 0);
            PaintLayer l;
            if (layers.TryGetValue(key, out l) && l != null && !l.Full) return l;
            if (root == null)
            {
                var host = GameController.I != null && GameController.I.Arena != null ? GameController.I.Arena.Root : null;
                root = new GameObject("Graffiti").transform;
                if (host != null) root.SetParent(host.transform, false);
            }
            l = PaintLayer.Create(root, Mats.SprayPaint(glow, stencil));
            layers[key] = l;
            return l;
        }

        // ---------- stencils ----------

        /// <summary>Stamps the can's stencil where <paramref name="ray"/> meets a wall. False if there is no stencil or no wall.</summary>
        public bool Stamp(Ray ray)
        {
            var st = Stencil;
            if (st == null || Time.time < nextStamp) return false;
            RaycastHit hit;
            if (!Surface(ray, Reach + 1f, out hit)) return false;
            nextStamp = Time.time + 0.8f;
            StartCoroutine(StampRun(st, hit.point, hit.normal, ray.direction, Color));
            return true;
        }

        IEnumerator StampRun(Stencil st, Vector3 centre, Vector3 n, Vector3 look, SprayColor can)
        {
            // The stencil's axes on the surface: upright on a wall, pointing away from the player on the ground.
            Vector3 right;
            if (Mathf.Abs(n.y) < 0.7f) right = Vector3.Cross(n, Vector3.up).normalized;
            else right = Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(look, Vector3.up).normalized).normalized * Mathf.Sign(n.y);
            var up = Vector3.Cross(right, n).normalized;
            if (Mathf.Abs(n.y) >= 0.7f) up = Vector3.Cross(n, right).normalized;
            float cell = Graffiti.CellSize(st);
            var cells = Graffiti.Cells(st);
            var origin = centre - right * (st.Width * cell * 0.5f) + up * (st.Height * cell * 0.5f);
            Sfx.I.Spray(true);
            int perFrame = Mathf.Max(6, cells.Count / 24);
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                var at = origin + right * ((c.X + 0.5f) * cell) - up * ((c.Y + 0.5f) * cell);
                RaycastHit h;
                if (Surface(new Ray(at + n * 0.3f, -n), 0.7f, out h))
                {
                    if (c.Code == '*') Dab(h.point, h.normal, cell * 1.5f, c.R / 255f, c.G / 255f, c.B / 255f, false, 0.95f, true);
                    else
                    {
                        var col = Graffiti.Paint(c.Code, can);
                        Dab(h.point, h.normal, cell * 1.5f, col.R, col.G, col.B, col.Glow, 0.95f, true);
                    }
                }
                if (i % perFrame == perFrame - 1) yield return null;
            }
            if (!Painting) Sfx.I.Spray(false);
        }
    }

    /// <summary>A batch of paint dabs in one mesh (quads with vertex colours), rebuilt at most once a frame.</summary>
    public sealed class PaintLayer : MonoBehaviour
    {
        const int MaxQuads = 4000;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color32> colors = new List<Color32>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> tris = new List<int>();
        Mesh mesh;
        bool dirty;

        public bool Full { get { return verts.Count >= MaxQuads * 4; } }

        public static PaintLayer Create(Transform parent, UnityEngine.Material mat)
        {
            var go = new GameObject("Paint");
            go.transform.SetParent(parent, false);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            var l = go.AddComponent<PaintLayer>();
            l.mesh = new Mesh { name = "Paint" };
            l.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = l.mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return l;
        }

        public void Add(Vector3 at, Vector3 normal, float size, Color c, float angle)
        {
            if (Full) return;
            var rot = Quaternion.AngleAxis(angle, normal) * Quaternion.LookRotation(-normal);
            var r = rot * Vector3.right * (size * 0.5f);
            var u = rot * Vector3.up * (size * 0.5f);
            int i = verts.Count;
            verts.Add(at - r - u); verts.Add(at - r + u); verts.Add(at + r + u); verts.Add(at + r - u);
            Color32 c32 = c;
            for (int k = 0; k < 4; k++) colors.Add(c32);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(1f, 0f));
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2); tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            dirty = true;
        }

        void LateUpdate()
        {
            if (!dirty) return;
            dirty = false;
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }
    }
}
