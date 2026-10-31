using System.Collections;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// The neighbours: after a big blast someone comes running out to see, waves their arms about and runs back
    /// in; after enough of them a police car comes down the street, lights flashing and siren wailing, and
    /// parks (it can be blown up like any other car).
    /// </summary>
    public sealed partial class StreetLife
    {
        int bigBlasts, neighboursOut;
        float neighbourQuietUntil;
        bool policeCalled;
        const int PoliceAfter = 4, MaxNeighbours = 3;

        void Neighbours(Vector3 pos, float radius)
        {
            if (radius < 1.8f) return;
            bigBlasts++;
            var gc = GameController.I;
            bool city = gc != null && gc.Level != null && gc.Level.Show;
            if (Time.time > neighbourQuietUntil && neighboursOut < MaxNeighbours)
            {
                neighbourQuietUntil = Time.time + 12f;
                neighboursOut++;
                Vector3 from;
                if (city)
                {
                    var dir = Random.insideUnitCircle.normalized;
                    from = pos + new Vector3(dir.x, 0f, dir.y) * 14f;
                }
                else
                {
                    var doors = new[] { new Vector3(-24f, 0f, 12f), new Vector3(23f, 0f, 12f), new Vector3(-14f, 0f, -31f), new Vector3(14f, 0f, -31f) };
                    from = doors[Random.Range(0, doors.Length)];
                }
                RaycastHit hit;
                if (Physics.Raycast(from + Vector3.up * 5f, Vector3.down, out hit, 10f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) from = hit.point;
                Neighbour.Spawn(transform, from, pos);
            }
            if (!policeCalled && bigBlasts >= PoliceAfter)
            {
                policeCalled = true;
                StartCoroutine(Police(city));
            }
        }

        IEnumerator Police(bool city)
        {
            yield return new WaitForSeconds(Random.Range(3f, 6f));
            var gc = GameController.I;
            var player = Player.I != null ? Player.I.transform.position : Vector3.zero;
            // Down the street towards the player: in the city along the nearest road, on a street along the road.
            float roadZ = city ? NearestRoad(player.z) : -16f + 2f;
            float stopX = city ? player.x - 8f : -8f;
            var start = new Vector3(stopX - 60f, 0f, roadZ);
            var car = Props.Spawn("sedan", transform, start, 90f, 1);
            Repaint(car);
            var mover = car.gameObject.AddComponent<PoliceCar>();
            mover.StopX = stopX;
            if (gc != null) gc.UI.Toast("Someone called the police!", 3f);
        }

        static float NearestRoad(float z)
        {
            float best = 0f, d = 1e9f;
            foreach (var r in new[] { -72f, -24f, 24f, 72f }) if (Mathf.Abs(z - r) < d) { d = Mathf.Abs(z - r); best = r; }
            return best + (z > best ? 2.5f : -2.5f);
        }

        /// <summary>Black and white, with a light bar on the roof.</summary>
        static void Repaint(Destructible car)
        {
            var black = Mats.Surface("paint", new Color(0.05f, 0.05f, 0.06f), 0.4f, 0.85f);
            var white = Mats.Surface("paint", new Color(0.93f, 0.93f, 0.92f), 0.3f, 0.8f);
            foreach (var r in car.GetComponentsInChildren<Renderer>())
            {
                var m = r.sharedMaterial;
                if (m == null || m.name != "paint") continue;
                r.sharedMaterial = r.transform.localPosition.y > 0.9f ? white : black;
            }
            var bar = new GameObject("LightBar").transform;
            bar.SetParent(car.transform, false);
            bar.localPosition = new Vector3(0f, 1.48f, -0.2f);
            ArenaBuilder.Box(bar, "Bar", Vector3.zero, new Vector3(1.1f, 0.1f, 0.25f), Mats.Lit(new Color(0.1f, 0.1f, 0.1f), 0.4f, 0.5f), false);
            var red = ArenaBuilder.Box(bar, "Red", new Vector3(-0.3f, 0.08f, 0f), new Vector3(0.45f, 0.1f, 0.22f), Mats.Emissive(new Color(0.8f, 0.05f, 0.05f), new Color(4f, 0.2f, 0.2f)), false);
            var blue = ArenaBuilder.Box(bar, "Blue", new Vector3(0.3f, 0.08f, 0f), new Vector3(0.45f, 0.1f, 0.22f), Mats.Emissive(new Color(0.05f, 0.1f, 0.8f), new Color(0.2f, 0.5f, 4f)), false);
            var flash = bar.gameObject.AddComponent<PoliceLights>();
            flash.Red = red;
            flash.Blue = blue;
        }
    }

    /// <summary>Drives a police car along the road to where it stops, siren on; it stops for good if it is hit.</summary>
    public sealed class PoliceCar : MonoBehaviour
    {
        public float StopX;
        Destructible car;
        AudioSource siren;

        void Start()
        {
            car = GetComponent<Destructible>();
            siren = gameObject.AddComponent<AudioSource>();
            siren.clip = Sfx.I.SirenClip;
            siren.loop = true;
            siren.spatialBlend = 1f;
            siren.minDistance = 6f;
            siren.maxDistance = 120f;
            siren.volume = 0.8f * Sfx.I.Volume;
            siren.dopplerLevel = 0.6f;
            siren.Play();
        }

        void Update()
        {
            if (car == null || car.Destroyed) { if (siren != null) siren.Stop(); enabled = false; return; }
            float x = transform.position.x;
            if (x >= StopX) { if (siren != null && siren.isPlaying && Time.time > 0f) StartCoroutine(Quiet()); enabled = false; return; }
            float speed = Mathf.Clamp((StopX - x) * 0.8f, 2f, 14f);
            transform.position += Vector3.right * speed * Time.deltaTime;
        }

        IEnumerator Quiet()
        {
            yield return new WaitForSeconds(6f);
            if (siren != null) siren.Stop();
        }
    }

    public sealed class PoliceLights : MonoBehaviour
    {
        public GameObject Red, Blue;

        void Update()
        {
            bool a = Mathf.Repeat(Time.time * 3f, 1f) < 0.5f;
            if (Red != null) Red.SetActive(a);
            if (Blue != null) Blue.SetActive(!a);
        }
    }

    /// <summary>A neighbour in a hurry: runs out towards the noise, stops to wave their arms, runs back, gone.</summary>
    public sealed class Neighbour : MonoBehaviour
    {
        Transform legL, legR, armL, armR, body;
        Vector3 home, look;
        float t;

        static readonly Color[] Shirts = { new Color(0.75f, 0.2f, 0.15f), new Color(0.2f, 0.35f, 0.7f), new Color(0.9f, 0.85f, 0.75f), new Color(0.25f, 0.5f, 0.3f), new Color(0.9f, 0.6f, 0.2f) };

        public static void Spawn(Transform parent, Vector3 at, Vector3 blast)
        {
            var go = new GameObject("Neighbour");
            go.transform.SetParent(parent, true);
            go.transform.position = at;
            var n = go.AddComponent<Neighbour>();
            n.home = at;
            var flat = blast - at;
            flat.y = 0f;
            n.look = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward;
            n.Build(Shirts[Random.Range(0, Shirts.Length)]);
        }

        void Build(Color shirt)
        {
            var skin = Mats.Lit(new Color(0.78f, 0.58f, 0.45f) * Random.Range(0.6f, 1.05f), 0f, 0.35f);
            var cloth = Mats.Lit(shirt, 0f, 0.3f);
            var trousers = Mats.Lit(new Color(0.15f, 0.17f, 0.22f), 0f, 0.3f);
            body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            Part(body, PrimitiveType.Capsule, new Vector3(0f, 1.25f, 0f), new Vector3(0.42f, 0.38f, 0.28f), cloth);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.72f, 0f), new Vector3(0.24f, 0.27f, 0.25f), skin);
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.8f, -0.02f), new Vector3(0.25f, 0.14f, 0.26f), Mats.Lit(new Color(0.2f, 0.14f, 0.1f), 0f, 0.3f));
            legL = Limb(new Vector3(-0.11f, 0.88f, 0f), new Vector3(0.15f, 0.45f, 0.15f), trousers);
            legR = Limb(new Vector3(0.11f, 0.88f, 0f), new Vector3(0.15f, 0.45f, 0.15f), trousers);
            armL = Limb(new Vector3(-0.27f, 1.45f, 0f), new Vector3(0.11f, 0.36f, 0.11f), cloth);
            armR = Limb(new Vector3(0.27f, 1.45f, 0f), new Vector3(0.11f, 0.36f, 0.11f), cloth);
        }

        Transform Limb(Vector3 hip, Vector3 size, UnityEngine.Material m)
        {
            var pivot = new GameObject("Limb").transform;
            pivot.SetParent(body, false);
            pivot.localPosition = hip;
            Part(pivot, PrimitiveType.Capsule, new Vector3(0f, -size.y, 0f), size, m);
            return pivot;
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 at, Vector3 scale, UnityEngine.Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.layer = Layers.NoRaycast;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        void Update()
        {
            t += Time.deltaTime;
            // Out (2 s), a look and a wave (2.5 s), back (2 s).
            Vector3 target = home + look * 6f;
            bool waving = t > 2f && t < 4.5f;
            bool back = t >= 4.5f;
            var goal = back ? home : target;
            var to = goal - transform.position;
            to.y = 0f;
            if (!waving && to.sqrMagnitude > 0.05f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), Time.deltaTime * 8f);
                transform.position += to.normalized * 4f * Time.deltaTime;
            }
            else if (waving) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 6f);
            float swing = waving ? 0f : Mathf.Sin(t * 14f) * 40f;
            legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            float arm = waving ? 160f + Mathf.Sin(t * 12f) * 25f : -swing;
            armL.localRotation = Quaternion.Euler(waving ? 0f : arm, 0f, waving ? -arm : 0f);
            armR.localRotation = Quaternion.Euler(waving ? 0f : -arm, 0f, waving ? arm : 0f);
            body.localPosition = new Vector3(0f, waving ? 0f : Mathf.Abs(Mathf.Sin(t * 14f)) * 0.05f, 0f);
            if (back && to.sqrMagnitude < 0.2f || t > 9f) Destroy(gameObject);
        }
    }
}
