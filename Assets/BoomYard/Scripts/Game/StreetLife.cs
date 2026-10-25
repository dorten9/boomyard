using System.Collections;
using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// The street reacts to what the player does: car alarms go off when a blast rocks a car, a dog next door
    /// barks, lights come on in the houses one after another, and things that fly get their flight measured.
    /// One per level, created by ArenaBuilder.
    /// </summary>
    public sealed class StreetLife : MonoBehaviour
    {
        public static StreetLife I { get; private set; }

        /// <summary>Dark windows the builder made, waiting to be lit by a neighbour who heard the noise.</summary>
        public static readonly List<Renderer> DarkRooms = new List<Renderer>();
        static readonly string[] Vehicles = { "van", "sedan", "pickup", "sportscar", "boxtruck", "fueltruck" };

        readonly List<Renderer> rooms = new List<Renderer>();
        Vector3 dogAt;
        AudioSource dog, rain;
        float dogQuietUntil;

        public static StreetLife Create(Transform root, Vector3 dogPosition)
        {
            var go = new GameObject("StreetLife");
            go.transform.SetParent(root, false);
            var s = go.AddComponent<StreetLife>();
            s.dogAt = dogPosition;
            s.rooms.AddRange(DarkRooms);
            DarkRooms.Clear();
            return s;
        }

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        void Start()
        {
            var d = new GameObject("Dog");
            d.transform.SetParent(transform, false);
            d.transform.position = dogAt + Vector3.up * 0.5f;
            dog = d.AddComponent<AudioSource>();
            dog.spatialBlend = 1f;
            dog.minDistance = 4f;
            dog.maxDistance = 80f;
            dog.rolloffMode = AudioRolloffMode.Logarithmic;
            var gc = GameController.I;
            if (gc != null && gc.Raining)
            {
                rain = gameObject.AddComponent<AudioSource>();
                rain.clip = StreetSounds.Rain;
                rain.loop = true;
                rain.volume = 0.5f * Sfx.I.Volume;
                rain.Play();
            }
        }

        /// <summary>Called for every detonation (Blast.Detonate).</summary>
        public void OnBlast(Vector3 pos, float radius)
        {
            // Car alarms within reach of the blast wave.
            float reach = radius * 3f + 4f;
            foreach (var d in Destructible.All)
            {
                if (d == null || d.Destroyed || System.Array.IndexOf(Vehicles, d.Def.Id) < 0) continue;
                if ((d.transform.position - pos).sqrMagnitude > reach * reach) continue;
                var alarm = d.GetComponent<CarAlarm>();
                if (alarm == null) alarm = d.gameObject.AddComponent<CarAlarm>();
                alarm.Trigger();
            }
            if (radius < 0.7f) return;
            // The dog next door.
            if (Time.time > dogQuietUntil)
            {
                dogQuietUntil = Time.time + 6f;
                StartCoroutine(Barks(3 + Random.Range(0, 4)));
            }
            // Lights come on in the windows nearest the noise, one by one.
            int wake = radius > 2.5f ? 4 : 2;
            for (int i = 0; i < wake && rooms.Count > 0; i++)
            {
                int best = -1;
                float bestD = 45f * 45f;
                for (int k = 0; k < rooms.Count; k++)
                {
                    if (rooms[k] == null) continue;
                    float dd = (rooms[k].transform.position - pos).sqrMagnitude;
                    if (dd < bestD) { bestD = dd; best = k; }
                }
                if (best < 0) break;
                var r = rooms[best];
                rooms.RemoveAt(best);
                StartCoroutine(LightUp(r, Random.Range(0.8f, 4f) + i * 0.7f));
            }
        }

        IEnumerator Barks(int n)
        {
            yield return new WaitForSeconds(Random.Range(0.4f, 1.1f));
            for (int i = 0; i < n; i++)
            {
                dog.pitch = Random.Range(0.92f, 1.08f);
                dog.PlayOneShot(StreetSounds.Bark[Random.Range(0, StreetSounds.Bark.Length)], 0.9f * Sfx.I.Volume);
                yield return new WaitForSeconds(Random.Range(0.28f, 0.6f));
            }
        }

        static UnityEngine.Material roomLit;

        IEnumerator LightUp(Renderer r, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (r == null) yield break;
            if (roomLit == null) roomLit = Mats.Emissive(new Color(0.4f, 0.3f, 0.18f), new Color(1.1f, 0.7f, 0.32f));
            r.sharedMaterial = roomLit;
        }

        /// <summary>A launched object: when it lands, how long it flew and how far (the gnome flew 23 m!).</summary>
        public static void TrackFlight(Destructible d)
        {
            if (d.GetComponent<Flight>() == null) d.gameObject.AddComponent<Flight>();
        }
    }

    /// <summary>Hazard lights blinking and the alarm whooping for 15 seconds, until the car is destroyed.</summary>
    public sealed class CarAlarm : MonoBehaviour
    {
        AudioSource source;
        Light blink;
        float until;
        Destructible car;

        public void Trigger()
        {
            if (car == null) car = GetComponent<Destructible>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.clip = StreetSounds.CarAlarm;
                source.loop = true;
                source.spatialBlend = 1f;
                source.minDistance = 5f;
                source.maxDistance = 120f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.dopplerLevel = 0f;
                var lg = new GameObject("Hazards");
                lg.transform.SetParent(transform, false);
                lg.transform.localPosition = new Vector3(0f, 1f, 0f);
                blink = lg.AddComponent<Light>();
                blink.type = LightType.Point;
                blink.color = new Color(1f, 0.6f, 0.15f);
                blink.range = 6f;
            }
            if (!source.isPlaying)
            {
                source.time = Random.Range(0f, 1f);
                source.volume = 0.55f * Sfx.I.Volume;
                source.Play();
            }
            until = Time.time + 15f;
            enabled = true;
        }

        void Update()
        {
            bool on = Time.time < until && car != null && !car.Destroyed;
            if (!on)
            {
                if (source != null) source.Stop();
                if (blink != null) blink.intensity = 0f;
                enabled = false;
                return;
            }
            blink.intensity = Mathf.Repeat(Time.time * 2f, 1f) < 0.5f ? 2.2f : 0f;
        }
    }

    /// <summary>Measures a launched object's flight; long flights get the Air Time bonus and a callout.</summary>
    public sealed class Flight : MonoBehaviour
    {
        Rigidbody rb;
        Vector3 start;
        float t;
        bool left;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            start = transform.position;
        }

        void FixedUpdate()
        {
            if (rb == null) { Destroy(this); return; }
            t += Time.fixedDeltaTime;
            if (!left) { if (transform.position.y - start.y > 0.4f || t > 0.4f) left = true; return; }
            if (t < 0.5f) return;
            bool landed = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)
#if UNITY_6000_0_OR_NEWER
                && rb.linearVelocity.y > -0.5f;
#else
                && rb.velocity.y > -0.5f;
#endif
            if (!landed && t < 8f) return;
            var flat = transform.position - start;
            flat.y = 0f;
            var gc = GameController.I;
            if (gc != null) gc.OnFlight(GetComponent<Destructible>(), t, flat.magnitude);
            Destroy(this);
        }
    }

    /// <summary>The street's own sounds, synthesised once: a car alarm, dog barks, a gas bottle's whistle and rain.</summary>
    public static class StreetSounds
    {
        const int Rate = 32000;

        static AudioClip alarm, whistle, rainLoop;
        static AudioClip[] barks;

        public static AudioClip CarAlarm { get { return alarm ?? (alarm = Clip("alarm", AlarmData())); } }
        public static AudioClip Whistle { get { return whistle ?? (whistle = Clip("whistle", WhistleData())); } }
        public static AudioClip Rain { get { return rainLoop ?? (rainLoop = Clip("rain", RainData())); } }

        public static AudioClip[] Bark
        {
            get
            {
                if (barks == null) barks = new[] { Clip("bark0", BarkData(1f, 11)), Clip("bark1", BarkData(0.88f, 12)), Clip("bark2", BarkData(1.12f, 13)) };
                return barks;
            }
        }

        static AudioClip Clip(string name, float[] d)
        {
            var c = AudioClip.Create(name, d.Length, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        // ---------- synthesis ----------

        /// <summary>
        /// A classic car alarm, 4 s loop: two rising whoops, a run of fast two-tone chirps, then a honking pulse.
        /// A soft-clipped sine through a small speaker (band-limited), so it isn't a pure tone.
        /// </summary>
        public static float[] AlarmData()
        {
            int n = Rate * 4;
            var d = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, f, a = 1f;
                if (t < 1.6f) { float k = Mathf.Repeat(t, 0.8f) / 0.8f; f = 550f + 1050f * k * k; }
                else if (t < 3f) { f = Mathf.Repeat(t * 9f, 1f) < 0.5f ? 1450f : 1000f; }
                else { f = 420f; a = Mathf.Repeat(t * 5f, 1f) < 0.6f ? 1f : 0f; }
                ph += 2f * Mathf.PI * f / Rate;
                if (ph > 2f * Mathf.PI) ph -= 2f * Mathf.PI;
                float v = Tanh(3.2f * Mathf.Sin(ph)) * a;
                d[i] = v;
            }
            d = Band(d, 380f, 3800f);
            Normalize(d, 0.7f);
            return Seam(d, Rate / 50);
        }

        /// <summary>
        /// One bark (~0.25 s): a buzzy pulse train whose pitch jumps up and falls, through two formants, with a
        /// breathy noise attack. <paramref name="pitch"/> scales it for variety.
        /// </summary>
        public static float[] BarkData(float pitch, int seed)
        {
            var r = new System.Random(seed);
            int n = (int)(Rate * 0.26f);
            var src = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f0 = pitch * (380f + 260f * Mathf.Exp(-t * 18f) - 90f * t * 3f);
                ph += f0 / Rate;
                if (ph >= 1f) ph -= 1f;
                float pulse = Mathf.Exp(-ph * 9f) - 0.11f;                      // glottal-ish pulse
                float noise = (float)(r.NextDouble() * 2.0 - 1.0);
                float env = Mathf.Clamp01(t * 140f) * Mathf.Exp(-t * 10f);
                src[i] = (pulse * 1.4f + noise * (0.25f + 0.6f * Mathf.Exp(-t * 40f))) * env;
            }
            var f1 = Resonator(src, 850f * pitch, 6f);
            var f2 = Resonator(src, 1700f * pitch, 7f);
            var f3 = Resonator(src, 3100f * pitch, 8f);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = f1[i] + 0.6f * f2[i] + 0.25f * f3[i];
            Normalize(d, 0.8f);
            return d;
        }

        /// <summary>A gas bottle venting as it flies: a high wavering whistle over a rushing hiss.</summary>
        public static float[] WhistleData()
        {
            var r = new System.Random(5);
            int n = (int)(Rate * 1.6f);
            var noise = new float[n];
            for (int i = 0; i < n; i++) noise[i] = (float)(r.NextDouble() * 2.0 - 1.0);
            var hiss = Band(noise, 1500f, 9000f);
            var d = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = 2600f + 900f * t + 120f * Mathf.Sin(t * 37f) + 60f * Mathf.Sin(t * 91f);
                ph += 2f * Mathf.PI * f / Rate;
                float env = Mathf.Clamp01(t * 12f) * Mathf.Clamp01((1.6f - t) * 4f);
                d[i] = (Mathf.Sin(ph) * 0.45f + hiss[i] * 0.7f) * env;
            }
            Normalize(d, 0.6f);
            return d;
        }

        /// <summary>
        /// Steady rain, 8 s loop: a bed of filtered noise that swells slowly, plus thousands of tiny drop ticks
        /// (each a short resonant blip at a random pitch and level) hitting roofs, cars and leaves.
        /// </summary>
        public static float[] RainData()
        {
            var r = new System.Random(9);
            int n = Rate * 8;
            var bed = new float[n];
            for (int i = 0; i < n; i++) bed[i] = (float)(r.NextDouble() * 2.0 - 1.0);
            bed = Band(bed, 500f, 7000f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                d[i] = bed[i] * (0.3f + 0.06f * Mathf.Sin(t * 0.8f) + 0.04f * Mathf.Sin(t * 2.3f));
            }
            int drops = 9000;
            for (int k = 0; k < drops; k++)
            {
                int at = r.Next(n);
                float f = 1800f + (float)r.NextDouble() * 4500f;
                float amp = 0.08f + 0.25f * (float)(r.NextDouble() * r.NextDouble());
                float decay = 300f + (float)r.NextDouble() * 500f;
                int len = Rate / 60;
                for (int j = 0; j < len; j++)
                {
                    int idx = (at + j) % n;
                    float tt = j / (float)Rate;
                    d[idx] += Mathf.Sin(2f * Mathf.PI * f * tt) * Mathf.Exp(-tt * decay) * amp;
                }
            }
            Normalize(d, 0.55f);
            return Seam(d, Rate / 4);
        }

        // ---------- DSP helpers ----------

        static float Tanh(float x) { float e = Mathf.Exp(2f * x); return (e - 1f) / (e + 1f); }

        /// <summary>Two-pole resonant band-pass (a formant) at <paramref name="hz"/> with quality <paramref name="q"/>.</summary>
        static float[] Resonator(float[] x, float hz, float q)
        {
            float w = 2f * Mathf.PI * hz / Rate, alpha = Mathf.Sin(w) / (2f * q), cos = Mathf.Cos(w);
            float b0 = alpha, b2 = -alpha, a0 = 1f + alpha, a1 = -2f * cos, a2 = 1f - alpha;
            var y = new float[x.Length];
            float x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                float v = (b0 * x[i] + b2 * x2 - a1 * y1 - a2 * y2) / a0;
                x2 = x1; x1 = x[i]; y2 = y1; y1 = v;
                y[i] = v;
            }
            return y;
        }

        /// <summary>Band-limits with a one-pole high-pass and a two-pass one-pole low-pass.</summary>
        static float[] Band(float[] x, float lowHz, float highHz)
        {
            float ah = Mathf.Exp(-2f * Mathf.PI * lowHz / Rate), al = 1f - Mathf.Exp(-2f * Mathf.PI * highHz / Rate);
            var y = new float[x.Length];
            float hp = 0f, prev = 0f, l1 = 0f, l2 = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                hp = ah * (hp + x[i] - prev);
                prev = x[i];
                l1 += (hp - l1) * al;
                l2 += (l1 - l2) * al;
                y[i] = l2;
            }
            return y;
        }

        static void Normalize(float[] d, float peak)
        {
            float m = 0f;
            foreach (var v in d) m = Mathf.Max(m, Mathf.Abs(v));
            if (m <= 0f) return;
            float k = peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
        }

        /// <summary>
        /// Loops without a click: the last <paramref name="fade"/> samples are cross-faded into the start and cut
        /// off, so the new last sample runs straight on into the first.
        /// </summary>
        static float[] Seam(float[] d, int fade)
        {
            int n = d.Length - fade;
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = d[i];
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                o[i] = d[i] * k + d[n + i] * (1f - k);
            }
            return o;
        }
    }
}
