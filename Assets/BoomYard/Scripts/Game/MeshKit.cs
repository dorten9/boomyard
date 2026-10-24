using System.Collections.Generic;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// Procedural meshes that read as real objects instead of primitives: rounded (bevelled) boxes that catch
    /// highlights on their edges, tapered shells (a car's glasshouse), lathe shapes (tyres, bins, gas cylinders,
    /// barrels) and noisy blobs (tree crowns, bushes). UVs are in metres so the tiling textures stay to scale.
    /// </summary>
    public static class MeshKit
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        /// <summary>
        /// Box of <paramref name="size"/> with edges rounded by <paramref name="radius"/>. <paramref name="topScale"/>
        /// narrows the top (x, z) for tapered shapes.
        /// </summary>
        public static Mesh RoundedBox(Vector3 size, float radius, int seg = 3, Vector2? topScale = null)
        {
            var taper = topScale ?? Vector2.one;
            string key = "rb" + size + radius + seg + taper;
            Mesh cached;
            if (cache.TryGetValue(key, out cached)) return cached;

            var h = size * 0.5f;
            radius = Mathf.Min(radius, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.99f);
            var inner = h - Vector3.one * radius;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int face = 0; face < 6; face++)
            {
                int axis = face / 2;
                float sign = face % 2 == 0 ? 1f : -1f;
                int ua = (axis + 1) % 3, va = (axis + 2) % 3;
                var lu = Coords(h[ua], radius, seg);
                var lv = Coords(h[va], radius, seg);
                int start = verts.Count;
                for (int j = 0; j < lv.Count; j++)
                    for (int i = 0; i < lu.Count; i++)
                    {
                        var q = Vector3.zero;
                        q[axis] = sign * h[axis];
                        q[ua] = lu[i];
                        q[va] = lv[j];
                        var c = new Vector3(Mathf.Clamp(q.x, -inner.x, inner.x), Mathf.Clamp(q.y, -inner.y, inner.y), Mathf.Clamp(q.z, -inner.z, inner.z));
                        var n = (q - c).normalized;
                        var p = c + n * radius;
                        float k = Mathf.InverseLerp(-h.y, h.y, p.y);
                        p.x *= Mathf.Lerp(1f, taper.x, k);
                        p.z *= Mathf.Lerp(1f, taper.y, k);
                        verts.Add(p);
                        norms.Add(n);
                        uvs.Add(new Vector2(p[ua], p[va]) * (sign > 0 ? 1f : -1f));
                    }
                int w = lu.Count;
                for (int j = 0; j < lv.Count - 1; j++)
                    for (int i = 0; i < w - 1; i++)
                    {
                        int a = start + j * w + i, b = a + 1, c2 = a + w, d = c2 + 1;
                        if (sign > 0) { tris.Add(a); tris.Add(c2); tris.Add(b); tris.Add(b); tris.Add(c2); tris.Add(d); }
                        else { tris.Add(a); tris.Add(b); tris.Add(c2); tris.Add(b); tris.Add(d); tris.Add(c2); }
                    }
            }
            var m = Build("RoundedBox", verts, norms, uvs, tris);
            cache[key] = m;
            return m;
        }

        /// <summary>Grid coordinates along one axis: denser near the ends where the edge rounds over.</summary>
        static List<float> Coords(float half, float r, int seg)
        {
            var l = new List<float>();
            for (int i = 0; i <= seg; i++) l.Add(-half + r * (i / (float)seg));
            float inner = half - r;
            int mid = Mathf.Clamp(Mathf.CeilToInt(inner * 2f / 0.6f), 1, 8);   // a few rows for tapering; flat faces need no more
            for (int i = 1; i < mid; i++) l.Add(-inner + 2f * inner * i / mid);
            for (int i = seg; i >= 0; i--) l.Add(half - r * (i / (float)seg));
            return l;
        }

        /// <summary>
        /// Revolves a profile (x = radius, y = height) around the Y axis. Used for tyres (then rotated), bins,
        /// gas cylinders, barrels, lamp heads. <paramref name="tiling"/> is metres per UV unit.
        /// </summary>
        public static Mesh Lathe(string key, Vector2[] profile, int segments = 32, bool capTop = false, bool capBottom = false)
        {
            Mesh cached;
            if (cache.TryGetValue("la" + key, out cached)) return cached;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float len = 0f;
            var along = new float[profile.Length];
            for (int i = 1; i < profile.Length; i++) { len += Vector2.Distance(profile[i], profile[i - 1]); along[i] = len; }
            float maxR = 0f;
            foreach (var p in profile) maxR = Mathf.Max(maxR, p.x);
            float circ = 2f * Mathf.PI * maxR;
            for (int s = 0; s <= segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                for (int i = 0; i < profile.Length; i++)
                {
                    var p = profile[i];
                    var t = profile[Mathf.Min(i + 1, profile.Length - 1)] - profile[Mathf.Max(i - 1, 0)];
                    var n2 = new Vector2(t.y, -t.x).normalized;   // outward for a profile listed bottom to top
                    verts.Add(new Vector3(p.x * ca, p.y, p.x * sa));
                    norms.Add(new Vector3(n2.x * ca, n2.y, n2.x * sa));
                    uvs.Add(new Vector2(s / (float)segments * circ, along[i]));
                }
            }
            int rows = profile.Length;
            for (int s = 0; s < segments; s++)
                for (int i = 0; i < rows - 1; i++)
                {
                    int a = s * rows + i, b = a + 1, c = a + rows, d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            if (capTop) Cap(verts, norms, uvs, tris, profile[rows - 1], segments, true);
            if (capBottom) Cap(verts, norms, uvs, tris, profile[0], segments, false);
            var m = Build("Lathe", verts, norms, uvs, tris);
            cache["la" + key] = m;
            return m;
        }

        static void Cap(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector2 edge, int segments, bool top)
        {
            int centre = verts.Count;
            var n = top ? Vector3.up : Vector3.down;
            verts.Add(new Vector3(0f, edge.y, 0f));
            norms.Add(n);
            uvs.Add(Vector2.zero);
            for (int s = 0; s <= segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                verts.Add(new Vector3(edge.x * Mathf.Cos(a), edge.y, edge.x * Mathf.Sin(a)));
                norms.Add(n);
                uvs.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * edge.x);
            }
            for (int s = 0; s < segments; s++)
            {
                if (top) { tris.Add(centre); tris.Add(centre + s + 2); tris.Add(centre + s + 1); }
                else { tris.Add(centre); tris.Add(centre + s + 1); tris.Add(centre + s + 2); }
            }
        }

        /// <summary>A tyre (lying in the XY plane, axle along X): rounded rubber cross-section.</summary>
        public static Mesh Tyre(float radius, float width)
        {
            float r = radius, w = width * 0.5f, side = radius * 0.62f;
            var prof = new List<Vector2>();
            // From the inner bead around the outer tread and back, in (radius, axle) space.
            prof.Add(new Vector2(side, -w * 0.9f));
            prof.Add(new Vector2(r * 0.86f, -w));
            for (int i = 0; i <= 6; i++)
            {
                float a = -Mathf.PI * 0.5f + i / 6f * Mathf.PI;
                prof.Add(new Vector2(r - w * 0.35f + Mathf.Cos(a) * w * 0.35f, Mathf.Sin(a) * w));
            }
            prof.Add(new Vector2(r * 0.86f, w));
            prof.Add(new Vector2(side, w * 0.9f));
            var m = Lathe("tyre" + radius + width, prof.ToArray(), 40);
            return m;
        }

        /// <summary>Wheel rim: a dished disc with a hub, facing +Y (rotate onto the axle).</summary>
        public static Mesh Rim(float radius, float depth)
        {
            // Listed from the rim edge to the centre so the face points up (+Y).
            var prof = new[]
            {
                new Vector2(radius, 0f), new Vector2(radius * 0.97f, depth * 0.35f), new Vector2(radius * 0.8f, depth * 0.2f),
                new Vector2(radius * 0.22f, depth * 0.35f), new Vector2(radius * 0.18f, depth * 0.55f), new Vector2(0f, depth * 0.55f),
            };
            return Lathe("rim" + radius + depth, prof, 28);
        }

        /// <summary>Organic lump (tree crowns, bushes): an icosphere pushed in and out by noise.</summary>
        public static Mesh Blob(float radius, float roughness, int seed)
        {
            string key = "blob" + radius + roughness + seed;
            Mesh cached;
            if (cache.TryGetValue(key, out cached)) return cached;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Icosphere(2, verts, tris);
            var rng = new System.Random(seed);
            float ox = (float)rng.NextDouble() * 100f, oy = (float)rng.NextDouble() * 100f;
            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                float n = Mathf.PerlinNoise(v.x * 1.7f + ox, v.z * 1.7f + oy) * 0.7f + Mathf.PerlinNoise(v.y * 3.1f + oy, v.x * 3.1f + ox) * 0.3f;
                float squash = v.y < 0f ? 0.7f : 1f;   // flatter underside
                verts[i] = new Vector3(v.x, v.y * squash, v.z) * radius * (1f + roughness * (n - 0.5f) * 2f);
            }
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            foreach (var v in verts) { norms.Add(v.normalized); uvs.Add(new Vector2(v.x + v.z, v.y)); }
            var m = Build("Blob", verts, norms, uvs, tris);   // winds the icosphere's faces outward
            m.RecalculateNormals();
            m.RecalculateTangents();
            cache[key] = m;
            return m;
        }

        static void Icosphere(int subdivisions, List<Vector3> verts, List<int> tris)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;
            var f = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                                    3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int s = 0; s < subdivisions; s++)
            {
                var mid = new Dictionary<long, int>();
                var nf = new List<int>();
                for (int i = 0; i < f.Count; i += 3)
                {
                    int a = f[i], b = f[i + 1], c = f[i + 2];
                    int ab = Mid(v, mid, a, b), bc = Mid(v, mid, b, c), ca = Mid(v, mid, c, a);
                    nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                f = nf;
            }
            verts.AddRange(v);
            tris.AddRange(f);
        }

        static int Mid(List<Vector3> v, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
            int idx;
            if (cache.TryGetValue(key, out idx)) return idx;
            v.Add(((v[a] + v[b]) * 0.5f).normalized);
            cache[key] = v.Count - 1;
            return v.Count - 1;
        }

        /// <summary>Gable roof prism: <paramref name="size"/> is (width across the ridge, height, length along it).</summary>
        public static Mesh Gable(Vector3 size, float overhang)
        {
            string key = "gable" + size + overhang;
            Mesh cached;
            if (cache.TryGetValue(key, out cached)) return cached;
            float w = size.x * 0.5f + overhang, hgt = size.y, l = size.z * 0.5f + overhang;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            // Two slopes.
            for (int side = -1; side <= 1; side += 2)
            {
                var eave = new Vector3(side * w, 0f, 0f);
                var ridge = new Vector3(0f, hgt, 0f);
                var n = new Vector3(side * hgt, w, 0f).normalized;
                int s = verts.Count;
                float slope = Vector3.Distance(eave, ridge);
                verts.AddRange(new[] { eave + Vector3.back * l, eave + Vector3.forward * l, ridge + Vector3.back * l, ridge + Vector3.forward * l });
                for (int i = 0; i < 4; i++) norms.Add(n);
                uvs.AddRange(new[] { new Vector2(-l, 0f), new Vector2(l, 0f), new Vector2(-l, slope), new Vector2(l, slope) });
                if (side < 0) { tris.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 }); }
                else { tris.AddRange(new[] { s, s + 1, s + 2, s + 1, s + 3, s + 2 }); }
            }
            // Gable ends.
            for (int end = -1; end <= 1; end += 2)
            {
                int s = verts.Count;
                float z = end * (l - overhang);
                verts.AddRange(new[] { new Vector3(-w + overhang, 0f, z), new Vector3(w - overhang, 0f, z), new Vector3(0f, hgt * (1f - overhang / w), z) });
                for (int i = 0; i < 3; i++) norms.Add(new Vector3(0f, 0f, end));
                uvs.AddRange(new[] { new Vector2(-w, 0f), new Vector2(w, 0f), new Vector2(0f, hgt) });
                if (end > 0) tris.AddRange(new[] { s, s + 1, s + 2 }); else tris.AddRange(new[] { s, s + 2, s + 1 });
            }
            var m = Build("Gable", verts, norms, uvs, tris);
            cache[key] = m;
            return m;
        }

        static Mesh Build(string name, List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris)
        {
            var m = new Mesh { name = name };
            if (verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            // Wind every triangle to face the way its normals point, so nothing renders inside out.
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                Vector3 a = verts[tris[i]], b = verts[tris[i + 1]], c = verts[tris[i + 2]];
                var face = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(face, norms[tris[i]] + norms[tris[i + 1]] + norms[tris[i + 2]]) < 0f)
                {
                    int t = tris[i + 1];
                    tris[i + 1] = tris[i + 2];
                    tris[i + 2] = t;
                }
            }
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        }
    }
}
