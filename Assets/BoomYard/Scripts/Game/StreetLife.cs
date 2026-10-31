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
    public sealed partial class StreetLife : MonoBehaviour
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
            try { Neighbours(pos, radius); } catch (System.Exception e) { Debug.LogError("BoomYard: neighbours: " + e.Message); }
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
        /// A cheap six-tone car alarm heard from across the street, 6 s loop cycling its patterns: slow rising
        /// whoops, a fast warble, a two-tone hi-lo and a machine-gun chirp. The source is a pulse wave (a piezo
        /// driver) with a little pitch wander, through a small horn speaker's resonances (about 1.6, 2.9 and
        /// 3.9 kHz) and an overdriven amp; then 10-30 m of outdoor air: duller highs, the ground reflection, slapback
        /// off the houses and a short tail. The effects run twice round the loop so the end flows into the start.
        /// </summary>
        public static float[] AlarmData()
        {
            var r = new System.Random(3);
            int n = Rate * 6;
            var d = new float[n];
            float ph = 0f, wobble = 0f, wobT = 0f, jit = 0f, gate = 0f;
            var buzz = new System.Random(4);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, f, on = 1f;
                if (t < 1.5f)
                {   // whoop: two slow sweeps up, each dropping straight back
                    float k = Mathf.Repeat(t, 0.75f) / 0.75f;
                    f = 620f * Mathf.Pow(2.9f, k * (2f - k) * 0.85f + k * 0.15f);
                    on = Mathf.Repeat(t, 0.75f) < 0.71f ? 1f : 0.15f;
                }
                else if (t < 3f) f = 1500f + 420f * Tri((t - 1.5f) * 13f);                         // warble
                else if (t < 4.5f) { float k = Mathf.Repeat((t - 3f) * 2.7f, 1f); f = k < 0.5f ? 1760f : 1290f; }  // hi-lo
                else
                {   // machine gun: 2.1 kHz chirps, 9 a second, the last one leading into silence before the loop
                    float k = Mathf.Repeat((t - 4.5f) * 9f, 1f);
                    f = 2050f * (1f + 0.06f * (1f - k));
                    on = k < 0.55f && t < 5.92f ? 1f : 0f;
                }
                // Pitch wander of a cheap oscillator on a sagging battery.
                if ((i & 255) == 0) wobT = (float)(r.NextDouble() * 2.0 - 1.0);
                wobble += (wobT - wobble) * 0.0008f;
                jit += ((float)(buzz.NextDouble() * 2.0 - 1.0) - jit) * 0.05f;
                f *= 1f + 0.006f * wobble + 0.0015f * jit;
                // The driver doesn't switch instantly (ramp up 2 ms, ring down 6 ms).
                gate += (on - gate) * (on > gate ? 0.016f : 0.006f);
                float dt = f / Rate;
                ph += dt;
                if (ph >= 1f) ph -= 1f;
                d[i] = Pulse(ph, dt, 0.42f) * gate;
            }
            // Horn speaker and amp.
            var sp = new[] { Bi.Hp(520f, 0.9f), Bi.Peak(1600f, 2.5f, 6f), Bi.Peak(2900f, 3.5f, 10f), Bi.Peak(3900f, 4f, 8f), Bi.Lp(7000f, 0.8f) };
            float[] o = new float[n];
            var lp2 = Bi.Lp(7500f, 0.7f);
            var air = new[] { Bi.Shelf(5000f, -4f), Bi.Lp(10000f, 0.6f), Bi.Hp(200f, 0.7f) };
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < n; i++)
                {
                    float v = d[i] * 0.5f;
                    foreach (var b in sp) v = b.Run(v);
                    v = lp2.Run(Tanh(2.4f * v));
                    foreach (var b in air) v = b.Run(v);
                    o[i] = v;
                }
            o = Outdoors(o, new[] { 0.0004f, 0.047f, 0.083f, 0.121f, 0.17f, 0.24f }, new[] { -0.45f, 0.42f, 0.33f, 0.24f, 0.16f, 0.1f }, 0.9f, 0.16f, true, r);
            Normalize(o, 0.7f);
            return o;
        }

        static float Tri(float x) { float k = Mathf.Repeat(x, 1f); return k < 0.5f ? 4f * k - 1f : 3f - 4f * k; }

        /// <summary>Band-limited pulse wave (PolyBLEP at both edges), duty <paramref name="w"/>, phase step <paramref name="dt"/>.</summary>
        static float Pulse(float ph, float dt, float w)
        {
            float v = ph < w ? 1f : -1f;
            v += Blep(ph, dt);
            float p2 = ph - w;
            if (p2 < 0f) p2 += 1f;
            v -= Blep(p2, dt);
            return v - (2f * w - 1f);
        }

        static float Blep(float t, float dt)
        {
            if (t < dt) { t /= dt; return t + t - t * t - 1f; }
            if (t > 1f - dt) { t = (t - 1f) / dt; return t * t + t + t + 1f; }
            return 0f;
        }

        /// <summary>
        /// One bark of a medium dog next door (~0.2 s plus echoes). A glottal pulse train (Rosenberg pulses, the
        /// pitch jumping up then falling fast, with jitter, shimmer and a rough period-doubled onset) plus breath
        /// noise, through four formants that open and close with the mouth ("wuh-OOF"), then the garden: slightly
        /// dull, with echoes off the houses. <paramref name="pitch"/> and <paramref name="seed"/> vary it.
        /// </summary>
        public static float[] BarkData(float pitch, int seed)
        {
            var r = new System.Random(seed);
            float len = 0.15f + 0.07f * (float)r.NextDouble() / pitch;
            float peak = (470f + 120f * (float)r.NextDouble()) * pitch, rough = 0.25f + 0.35f * (float)r.NextDouble();
            float fs = Mathf.Pow(pitch, 0.6f);
            float F1 = (720f + 140f * (float)r.NextDouble()) * fs, F2 = (1550f + 300f * (float)r.NextDouble()) * fs, F3 = 2700f * fs, F4 = 3700f * fs;
            int n = (int)(Rate * (len + 0.03f));
            var y = new float[n];
            var f = new[] { new Bi(), new Bi(), new Bi(), new Bi() };
            float[] fg = { 1f, 0.9f, 0.5f, 0.22f };
            var chest = Bi.Lp(380f, 0.7f);
            float ph = 0f, prevFlow = 0f, perAmp = 1f, perF = peak, lp = 0f;
            int period = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, u = t / len;
                // Pitch: up for 20 ms, then a quick fall.
                float f0 = t < 0.02f ? peak * (0.82f + 0.18f * t / 0.02f) : peak * (0.58f + 0.42f * Mathf.Exp(-(t - 0.02f) * 14f));
                if ((i & 31) == 0)
                {
                    float open = Mathf.Clamp01(t / 0.035f), close = Mathf.Clamp01((u - 0.45f) / 0.55f);
                    float m = (0.72f + 0.28f * open) * (1f - 0.22f * close);
                    f[0].SetBp(F1 * m, F1 * m / 110f);
                    f[1].SetBp(F2 * (0.8f + 0.2f * open) * (1f - 0.25f * close), F2 / 170f);
                    f[2].SetBp(F3 * (1f - 0.08f * close), F3 / 260f);
                    f[3].SetBp(F4, F4 / 380f);
                }
                ph += perF / Rate;
                if (ph >= 1f)
                {
                    ph -= 1f;
                    period++;
                    perF = f0 * (1f + 0.035f * N(r));                                   // jitter
                    perAmp = (1f + 0.15f * N(r)) * (t < 0.07f && (period & 1) == 1 ? 1f - rough : 1f);   // shimmer, growl
                }
                float flow = Rosenberg(ph, 0.45f, 0.1f) * perAmp;
                float dflow = (flow - prevFlow) * Rate / (perF * 6f);
                prevFlow = flow;
                // Voicing comes in a few ms after the breath; breath noise follows the glottis opening.
                float voice = Mathf.Clamp01((t - 0.004f) / 0.008f);
                float env = Mathf.Min(1f, t / 0.004f) * (u < 0.35f ? 1f : Mathf.Pow(Mathf.Max(0f, 1f - (u - 0.35f) / 0.65f), 1.4f));
                float breath = N(r) * (0.25f + 0.5f * flow) * (t < 0.015f ? 1.6f : 0.6f);
                float tail = u > 0.9f ? N(r) * 0.12f * Mathf.Exp(-(t - 0.9f * len) * 60f) : 0f;   // the exhale after
                float x = (dflow * voice + breath) * env + tail;
                float v = 0f;
                for (int k = 0; k < 4; k++) v += f[k].Run(x) * fg[k];
                v += chest.Run(flow * voice * env) * 0.35f;
                lp += (v - lp) * 0.8f;   // a little dulled by the distance
                y[i] = lp;
            }
            y = Bi.Hp(110f, 0.7f).Apply(y);
            Normalize(y, 1f);
            for (int i = 0; i < n; i++) y[i] = Tanh(1.8f * y[i]);   // a bark is loud: the throat and the mic saturate a little
            var o = Outdoors(y, new[] { 0.0006f, 0.029f, 0.058f, 0.093f, 0.15f }, new[] { -0.3f, 0.32f, 0.22f, 0.14f, 0.08f }, 0.4f, 0.1f, false, r);
            Normalize(o, 0.8f);
            Fade(o, 0.001f, 0.03f);
            return o;
        }

        /// <summary>Glottal flow of one period (0..1): rises over <paramref name="tp"/>, falls over <paramref name="tn"/>, then closed.</summary>
        static float Rosenberg(float p, float tp, float tn)
        {
            if (p < tp) return 0.5f * (1f - Mathf.Cos(Mathf.PI * p / tp));
            if (p < tp + tn) return Mathf.Cos(0.5f * Mathf.PI * (p - tp) / tn);
            return 0f;
        }

        /// <summary>
        /// A gas bottle venting as it flies (1.6 s): a valve pop, a rushing jet of turbulent gas, and the whistle -
        /// that jet's noise ringing a narrow, overdriven resonance whose pitch rises as the pressure changes and
        /// wavers with the flow, with its octave and a low roar of thrust.
        /// </summary>
        public static float[] WhistleData()
        {
            var r = new System.Random(5);
            int n = (int)(Rate * 1.6f);
            var d = new float[n];
            var jet = Bi.Bp(2200f, 0.5f);
            var thrust = Bi.Lp(420f, 0.8f);
            var w1 = new Bi();
            var w2 = new Bi();
            var hp = Bi.Hp(150f, 0.7f);
            float wv = 0f, wvT = 0f, fl = 0f, flT = 0f, pop = 1f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                if ((i & 511) == 0) wvT = N(r);
                if ((i & 63) == 0) flT = N(r);
                wv += (wvT - wv) * 0.0025f;
                fl += (flT - fl) * 0.02f;
                if ((i & 15) == 0)
                {
                    float f = (2300f + 1300f * (1f - Mathf.Exp(-t * 1.6f))) * (1f + 0.025f * wv + 0.008f * fl);
                    w1.SetBp(f, 70f);
                    w2.SetBp(f * 2.01f, 50f);
                }
                float x = N(r);
                float tone = Tanh(w1.Run(x) * 14f) * 0.5f + w2.Run(x) * 2.2f;
                float flow = 1f + 0.18f * fl + 0.1f * wv;
                float env = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((1.6f - t) / 0.3f);
                pop *= 0.9985f;
                d[i] = hp.Run((tone * (0.4f + 0.6f * Mathf.Clamp01(t / 0.15f)) + jet.Run(x) * 0.55f + thrust.Run(x) * 0.5f) * flow * env + x * pop * 0.6f);
            }
            Normalize(d, 1f);
            for (int i = 0; i < n; i++) d[i] = Tanh(1.5f * d[i]);
            Normalize(d, 0.6f);
            Fade(d, 0.001f, 0.02f);
            return d;
        }

        /// <summary>
        /// Steady rain, 8 s loop: a soft wash (filtered noise that swells a little) under thousands of drops of every
        /// size - fine ticks on leaves and paving, duller pats, bright taps on car roofs and gutters, and bigger
        /// drops in puddles that "plink" (a bubble ringing with rising pitch). Drops past the end wrap to the start
        /// and the filters are warmed up on the loop's end, so there is no seam.
        /// </summary>
        public static float[] RainData()
        {
            var r = new System.Random(9);
            int n = Rate * 8;
            var wash = new float[n];
            var ticks = new float[n];
            var pats = new float[n];
            var d = new float[n];
            for (int i = 0; i < n; i++) wash[i] = N(r);
            for (int k = 0; k < 16000; k++)
            {
                int at = r.Next(n);
                float s = (float)r.NextDouble();
                s = s * s * s;   // most drops are small
                double kind = r.NextDouble();
                if (kind < 0.62)
                {   // fine tick
                    int m = 8 + (int)(s * 40f);
                    float a = 0.04f + 0.4f * s;
                    for (int j = 0; j < m; j++) ticks[(at + j) % n] += N(r) * a * (1f - j / (float)m);
                }
                else if (kind < 0.86)
                {   // dull pat on soft ground or leaves
                    int m = 40 + (int)(s * 120f);
                    float a = 0.05f + 0.35f * s;
                    for (int j = 0; j < m; j++) pats[(at + j) % n] += N(r) * a * Mathf.Exp(-j * 6f / m);
                }
                else if (kind < 0.95)
                {   // tap on metal: a short ring
                    float f = 1800f + 2600f * (float)r.NextDouble(), a = 0.03f + 0.2f * s, dec = 300f + 400f * (float)r.NextDouble();
                    Ring(d, at, f, dec, a, false);
                    Ring(d, at, f * 2.3f, dec * 1.6f, a * 0.4f, false);
                }
                else
                {   // a bigger drop in a puddle: a splash tick and a bubble chirping up
                    float f = 900f + 2500f * (1f - s) * (float)r.NextDouble() + 600f, a = 0.03f + 0.15f * s;
                    for (int j = 0; j < 20; j++) ticks[(at + j) % n] += N(r) * a * 0.8f * (1f - j / 20f);
                    if (r.NextDouble() < 0.6) Ring(d, (at + 30) % n, f, 70f + 80f * (float)r.NextDouble(), a, true);
                }
            }
            // Filter each layer round the loop (warm-up on its last half second).
            Circular(wash, new[] { Bi.Hp(350f, 0.6f), Bi.Lp(5200f, 0.5f), Bi.Peak(1800f, 0.7f, -3f) });
            Circular(ticks, new[] { Bi.Hp(2500f, 0.7f), Bi.Peak(5500f, 1f, 4f), Bi.Lp(11000f, 0.7f) });
            Circular(pats, new[] { Bi.Bp(1300f, 0.8f), Bi.Lp(3500f, 0.7f) });
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                float swell = 1f + 0.12f * Mathf.Sin(2f * Mathf.PI * u + 1f) + 0.06f * Mathf.Sin(6f * Mathf.PI * u + 2.3f) + 0.04f * Mathf.Sin(14f * Mathf.PI * u);
                d[i] += wash[i] * 0.22f * swell + ticks[i] * 0.55f + pats[i] * 0.7f;
            }
            Circular(d, new[] { Bi.Hp(60f, 0.7f) });
            Normalize(d, 1f);
            for (int i = 0; i < n; i++) d[i] = Tanh(1.6f * d[i]);   // tame the odd big drop
            Normalize(d, 0.55f);
            return d;
        }

        // ---------- DSP helpers ----------

        static float N(System.Random r) { return (float)(r.NextDouble() * 2.0 - 1.0); }

        static float Tanh(float x) { return (float)System.Math.Tanh(x); }

        /// <summary>RBJ biquad; its coefficients can be changed as it runs (for moving formants and whistles).</summary>
        sealed class Bi
        {
            float b0, b1, b2, a1, a2, z1, z2;

            void Set(float c0, float c1, float c2, float d0, float d1, float d2) { b0 = c0 / d0; b1 = c1 / d0; b2 = c2 / d0; a1 = d1 / d0; a2 = d2 / d0; }

            static void W(float hz, float q, out float cs, out float al)
            {
                float w = 2f * Mathf.PI * Mathf.Min(hz, Rate * 0.46f) / Rate;
                cs = Mathf.Cos(w);
                al = Mathf.Sin(w) / (2f * q);
            }

            public Bi SetBp(float hz, float q) { float c, a; W(hz, q, out c, out a); Set(a, 0f, -a, 1f + a, -2f * c, 1f - a); return this; }
            public static Bi Bp(float hz, float q) { return new Bi().SetBp(hz, q); }
            public static Bi Lp(float hz, float q) { float c, a; W(hz, q, out c, out a); var b = new Bi(); b.Set((1f - c) / 2f, 1f - c, (1f - c) / 2f, 1f + a, -2f * c, 1f - a); return b; }
            public static Bi Hp(float hz, float q) { float c, a; W(hz, q, out c, out a); var b = new Bi(); b.Set((1f + c) / 2f, -(1f + c), (1f + c) / 2f, 1f + a, -2f * c, 1f - a); return b; }
            public static Bi Peak(float hz, float q, float db)
            {
                float c, a, A = Mathf.Pow(10f, db / 40f);
                W(hz, q, out c, out a);
                var b = new Bi();
                b.Set(1f + a * A, -2f * c, 1f - a * A, 1f + a / A, -2f * c, 1f - a / A);
                return b;
            }
            public static Bi Shelf(float hz, float db)
            {
                float c, a, A = Mathf.Pow(10f, db / 40f);
                W(hz, 0.707f, out c, out a);
                float s = 2f * Mathf.Sqrt(A) * a;
                var b = new Bi();
                b.Set(A * ((A + 1f) + (A - 1f) * c + s), -2f * A * ((A - 1f) + (A + 1f) * c), A * ((A + 1f) + (A - 1f) * c - s),
                      (A + 1f) - (A - 1f) * c + s, 2f * ((A - 1f) - (A + 1f) * c), (A + 1f) - (A - 1f) * c - s);
                return b;
            }

            public float Run(float x)
            {
                float y = b0 * x + z1;
                z1 = b1 * x - a1 * y + z2;
                z2 = b2 * x - a2 * y;
                return y;
            }

            public float[] Apply(float[] d) { for (int i = 0; i < d.Length; i++) d[i] = Run(d[i]); return d; }
        }

        /// <summary>Runs a filter chain over a loop in place, warmed up on its last half second so the end flows into the start.</summary>
        static void Circular(float[] d, Bi[] chain)
        {
            int n = d.Length, warm = Mathf.Min(n, Rate / 2);
            for (int i = n - warm; i < n; i++) { float v = d[i]; foreach (var b in chain) v = b.Run(v); }
            for (int i = 0; i < n; i++) { float v = d[i]; foreach (var b in chain) v = b.Run(v); d[i] = v; }
        }

        /// <summary>A decaying sine (a resonance struck) added at <paramref name="at"/>, wrapping round the buffer; optionally chirping up like a bubble.</summary>
        static void Ring(float[] d, int at, float hz, float decay, float amp, bool bubble)
        {
            int n = d.Length, m = (int)(Mathf.Log(amp * 10000f + 1f) / decay * Rate) + 1;
            float ph = 0f;
            for (int j = 0; j < m; j++)
            {
                float t = j / (float)Rate;
                ph += hz * (bubble ? 1f + t * decay * 0.35f : 1f) / Rate;
                d[(at + j) % n] += Mathf.Sin(2f * Mathf.PI * ph) * Mathf.Exp(-t * decay) * amp * Mathf.Min(1f, j / 4f);
            }
        }

        /// <summary>
        /// Outdoor acoustics: discrete reflections (<paramref name="delays"/> in s, <paramref name="gains"/>; a tiny
        /// first delay with negative gain is the ground), each later one duller, plus a short diffuse tail of
        /// <paramref name="rt"/> s at level <paramref name="wet"/>. A <paramref name="loop"/> wraps the echoes round
        /// (same length out); otherwise the result is longer by the tail.
        /// </summary>
        static float[] Outdoors(float[] x, float[] delays, float[] gains, float rt, float wet, bool loop, System.Random r)
        {
            int n = x.Length, extra = loop ? 0 : (int)(rt * 0.6f * Rate), m = n + extra;
            var o = new float[m];
            var dull = (float[])x.Clone();
            var lpA = Bi.Lp(4000f, 0.6f);
            for (int i = 0; i < n; i++) dull[i] = lpA.Run(dull[i]);
            if (loop) { lpA = Bi.Lp(4000f, 0.6f); Circular(dull = (float[])x.Clone(), new[] { lpA }); }
            var diff = new float[m];
            for (int i = 0; i < n; i++) o[i] = x[i];
            for (int k = 0; k < delays.Length; k++)
            {
                int off = (int)(delays[k] * Rate);
                var src = off < Rate / 200 ? x : dull;
                for (int i = 0; i < n; i++)
                {
                    int j = i + off;
                    if (j >= m) { if (!loop) break; j -= m; }
                    o[j] += src[i] * gains[k];
                    if (off > Rate / 200) diff[j] += src[i] * Mathf.Abs(gains[k]);
                }
            }
            // Diffuse tail: two all-passes into four damped combs; for a loop, run twice round.
            int[] combs = { 1116, 1188, 1277, 1356 };
            int[] aps = { 225, 341 };
            var sum = new float[m];
            int passes = loop ? 2 : 1;
            var cb = new float[combs.Length][];
            var ci = new int[combs.Length];
            var cl = new float[combs.Length];
            var cf = new float[combs.Length];
            for (int c = 0; c < combs.Length; c++) { cb[c] = new float[combs[c] + r.Next(17)]; cf[c] = Mathf.Pow(10f, -3f * cb[c].Length / (rt * Rate)); }
            var ab = new float[aps.Length][];
            var ai = new int[aps.Length];
            for (int a = 0; a < aps.Length; a++) ab[a] = new float[aps[a]];
            for (int p = 0; p < passes; p++)
                for (int i = 0; i < m; i++)
                {
                    float v = diff[i];
                    for (int a = 0; a < aps.Length; a++)
                    {
                        float b = ab[a][ai[a]], y = -0.6f * v + b;
                        ab[a][ai[a]] = v + 0.6f * y;
                        if (++ai[a] >= ab[a].Length) ai[a] = 0;
                        v = y;
                    }
                    float s = 0f;
                    for (int c = 0; c < combs.Length; c++)
                    {
                        var buf = cb[c];
                        float y = buf[ci[c]];
                        cl[c] += (y - cl[c]) * 0.45f;
                        buf[ci[c]] = v + cl[c] * cf[c];
                        if (++ci[c] >= buf.Length) ci[c] = 0;
                        s += y;
                    }
                    if (p == passes - 1) sum[i] = s;
                }
            for (int i = 0; i < m; i++) o[i] += sum[i] * wet * 0.25f;
            return o;
        }

        static void Fade(float[] d, float inS, float outS)
        {
            int a = Mathf.Min((int)(inS * Rate), d.Length / 2), b = Mathf.Min((int)(outS * Rate), d.Length / 2);
            for (int i = 0; i < a; i++) d[i] *= 0.5f - 0.5f * Mathf.Cos(Mathf.PI * i / a);
            for (int i = 0; i < b; i++) d[d.Length - 1 - i] *= 0.5f - 0.5f * Mathf.Cos(Mathf.PI * i / b);
        }

        static void Normalize(float[] d, float peak)
        {
            float m = 0f, dc = 0f;
            foreach (var v in d) dc += v;
            dc /= d.Length;
            for (int i = 0; i < d.Length; i++) d[i] -= dc;
            foreach (var v in d) m = Mathf.Max(m, Mathf.Abs(v));
            if (m <= 0f) return;
            float k = peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
        }
    }
}
