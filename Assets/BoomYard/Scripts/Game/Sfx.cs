using System.Collections.Generic;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// All sound, synthesised in code (no recordings): physically-shaped layers (transient crack, noise body,
    /// sub-bass thump, crackle tail) run through filters and an outdoor reverb with slapback off the houses.
    /// Near and far versions of each blast, firecracker pops, glass, metal debris, fire, the lighter and fuse,
    /// an evening ambience, UI sounds and a menu music loop.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        public static Sfx I { get; private set; }

        const int Rate = 44100;
        const int MusicRate = 22050;

        public float Volume = 1f, MusicVolume = 0.6f;

        AudioClip flick, flame, hiss, popNear, popFar, boomNear, boomFar, bigNear, bigFar, glass, clang, debris, crackle, ring, thud, ambience, uiTap, uiConfirm, uiCoin, uiStar, winSting, failSting;
        AudioClip music;
        AudioSource ui, flameLoop, ringSource, ambienceSource, musicSource;
        Coroutine muffle;
        readonly List<AudioSource> pool = new List<AudioSource>();

        void Awake()
        {
            I = this;
            var r = new System.Random(7);
            flick = Lighter(r);
            flame = Loop(Rate, 1.6f, FlameWave(r), 0.5f);
            hiss = Loop(Rate, 1.2f, HissWave(r), 0.35f);
            popNear = Reverb(Pop(r, 1f), 0.35f, 0.5f, r);
            popFar = Reverb(LowPass(Pop(r, 0.6f), 2500f), 0.6f, 0.8f, r);
            boomNear = Reverb(Boom(r, 1.6f, 70f, 1f), 0.4f, 1.1f, r);
            boomFar = Reverb(LowPass(Boom(r, 1.8f, 60f, 0.6f), 1200f), 0.7f, 1.6f, r);
            bigNear = Reverb(Boom(r, 3.2f, 45f, 1.4f), 0.45f, 2f, r);
            bigFar = Reverb(LowPass(Boom(r, 3.4f, 40f, 0.9f), 900f), 0.8f, 2.6f, r);
            glass = Reverb(Glass(r), 0.25f, 0.4f, r);
            clang = Reverb(Clang(r), 0.3f, 0.6f, r);
            debris = Debris(r);
            crackle = Loop(Rate, 3f, CrackleWave(r), 0.6f);
            ring = Make(Rate, 3.2f, (t, i) => 0.22f * Mathf.Sin(2f * Mathf.PI * (3650f + 30f * Mathf.Sin(t * 5f)) * t) * Mathf.Clamp01(t * 6f) * Mathf.Clamp01((3.2f - t) / 1.8f));
            thud = Make(Rate, 0.5f, (t, i) => Mathf.Sin(2f * Mathf.PI * (65f - 35f * t) * t) * Mathf.Exp(-t * 9f) + 0.25f * N(r) * Mathf.Exp(-t * 35f));
            ambience = Loop(MusicRate, 14f, AmbienceWave(r), 1f);
            uiTap = Make(Rate, 0.06f, (t, i) => Mathf.Sin(2f * Mathf.PI * 1900f * t) * Mathf.Exp(-t * 70f) * 0.5f);
            uiConfirm = Make(Rate, 0.25f, (t, i) => (Mathf.Sin(2f * Mathf.PI * 660f * t) * (t < 0.08f ? 1f : 0f) + Mathf.Sin(2f * Mathf.PI * 990f * t) * (t >= 0.08f ? 1f : 0f)) * Mathf.Exp(-(t % 0.08f) * 20f) * 0.35f);
            uiCoin = Make(Rate, 0.4f, (t, i) => (Mathf.Sin(2f * Mathf.PI * 1568f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 2349f * t)) * Mathf.Exp(-t * 9f) * 0.3f);
            uiStar = Reverb(Samples(Rate, 0.7f, (t, i) => (Mathf.Sin(2f * Mathf.PI * 1046f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 1568f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 2093f * t)) * Mathf.Exp(-t * 5f) * 0.28f), 0.2f, 0.6f, r);
            winSting = Reverb(Chord(new[] { 523f, 659f, 784f, 1046f }, 1.6f, 0.06f), 0.3f, 1f, r);
            failSting = Reverb(Chord(new[] { 392f, 370f, 349f, 311f }, 1.6f, 0.18f), 0.3f, 1f, r);

            ui = gameObject.AddComponent<AudioSource>();
            flameLoop = Source(flame, 0.3f, true);
            ringSource = Source(ring, 0.5f, false);
            ambienceSource = Source(ambience, 0.55f, true);
            ambienceSource.Play();
            musicSource = Source(null, 0.6f, true);
        }

        AudioSource Source(AudioClip clip, float volume, bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = loop;
            s.volume = volume;
            s.playOnAwake = false;
            return s;
        }

        // ---------- playback ----------

        public void Click() { ui.PlayOneShot(flick, 0.9f * Volume); }
        public void Thud() { ui.PlayOneShot(thud, 0.8f * Volume); }
        public void Tap() { ui.PlayOneShot(uiTap, 0.6f * Volume); }
        public void Confirm() { ui.PlayOneShot(uiConfirm, 0.6f * Volume); }
        public void Coin() { ui.PlayOneShot(uiCoin, 0.7f * Volume); }
        public void Star() { ui.PlayOneShot(uiStar, 0.8f * Volume); }
        public void Win() { ui.PlayOneShot(winSting, 0.8f * Volume); }
        public void Fail() { ui.PlayOneShot(failSting, 0.8f * Volume); }

        public void Flame(bool on) { Toggle(flameLoop, on); flameLoop.volume = 0.3f * Volume; }

        /// <summary>A lit fuse hisses where it is (3D), on its own source.</summary>
        public void FuseOn(AudioSource s)
        {
            s.clip = hiss;
            s.loop = true;
            s.volume = 0.45f * Volume;
            s.spatialBlend = 1f;
            s.minDistance = 1.5f;
            s.maxDistance = 25f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.pitch = Random.Range(0.92f, 1.12f);
            s.Play();
        }

        /// <summary>Hiss of the fuse in the hand: rises in pitch as it gets shorter.</summary>
        public void Hiss(bool on, float left01 = 1f)
        {
            if (handHiss == null) handHiss = Source(hiss, 0.4f, true);
            Toggle(handHiss, on);
            handHiss.pitch = Mathf.Lerp(1.35f, 0.95f, left01);
            handHiss.volume = 0.4f * Volume;
        }
        AudioSource handHiss;

        public void Music(bool on)
        {
            if (on && music == null) music = MakeMusic();
            musicSource.clip = music;
            musicSource.volume = MusicVolume * 0.5f;
            Toggle(musicSource, on);
            ambienceSource.volume = on ? 0.2f : 0.55f;
        }

        static void Toggle(AudioSource s, bool on)
        {
            if (on && !s.isPlaying) s.Play();
            else if (!on && s.isPlaying) s.Stop();
        }

        /// <summary>
        /// A blast: firecracker crack, explosion or big explosion by radius, the near or the far recording by
        /// distance, then glass and debris if it broke things.
        /// </summary>
        public void Boom(Vector3 pos, float radius, float echo = 1f)
        {
            var cam = Camera.main;
            float dist = cam != null ? Vector3.Distance(cam.transform.position, pos) : 10f;
            bool far = dist > 22f;
            AudioClip clip;
            float vol;
            if (radius < 1.2f) { clip = far ? popFar : popNear; vol = 0.85f; }
            else if (radius < 3f) { clip = far ? boomFar : boomNear; vol = 1f; }
            else { clip = far ? bigFar : bigNear; vol = 1f; }
            Play(clip, pos, vol * Volume, Random.Range(0.92f, 1.08f), radius < 1.2f ? 6f : 15f);
            if (radius >= 1.2f) Play(debris, pos, 0.5f * Volume, Random.Range(0.9f, 1.1f), 8f, 0.25f);
        }

        public void Glass(Vector3 pos) { Play(glass, pos, 0.7f * Volume, Random.Range(0.85f, 1.2f), 6f); }
        public void Clang(Vector3 pos) { Play(clang, pos, 0.6f * Volume, Random.Range(0.8f, 1.25f), 5f); }

        public AudioSource FireLoop(Transform at)
        {
            var s = at.gameObject.AddComponent<AudioSource>();
            s.clip = crackle;
            s.loop = true;
            s.volume = 0.6f * Volume;
            s.spatialBlend = 1f;
            s.minDistance = 2f;
            s.maxDistance = 35f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.time = Random.Range(0f, crackle.length);
            s.Play();
            return s;
        }

        /// <summary>Ear ringing after a close blast; the world goes muffled for 3 seconds.</summary>
        public void Ring()
        {
            ringSource.volume = 0.45f * Volume;
            ringSource.Play();
            if (muffle != null) StopCoroutine(muffle);
            muffle = StartCoroutine(Muffle());
        }

        System.Collections.IEnumerator Muffle()
        {
            for (float t = 0f; t < 3f; t += Time.unscaledDeltaTime)
            {
                AudioListener.volume = Mathf.Lerp(0.2f, 1f, t / 3f);
                yield return null;
            }
            AudioListener.volume = 1f;
        }

        void Play(AudioClip clip, Vector3 pos, float vol, float pitch, float minDist, float delay = 0f)
        {
            AudioSource s = null;
            foreach (var p in pool) if (p != null && !p.isPlaying) { s = p; break; }
            if (s == null)
            {
                if (pool.Count >= 24) s = pool[0];
                else
                {
                    var go = new GameObject("Sound");
                    DontDestroyOnLoad(go);
                    s = go.AddComponent<AudioSource>();
                    pool.Add(s);
                }
            }
            s.transform.position = pos;
            s.clip = clip;
            s.volume = vol;
            s.pitch = pitch;
            s.spatialBlend = 0.8f;
            s.minDistance = minDist;
            s.maxDistance = 250f;
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.dopplerLevel = 0f;
            if (delay > 0f) s.PlayDelayed(delay); else s.Play();
        }

        // ---------- synthesis ----------

        delegate float Wave(float t, int i);

        static float N(System.Random r) { return (float)(r.NextDouble() * 2.0 - 1.0); }

        static float[] Samples(int rate, float seconds, Wave w)
        {
            int n = (int)(seconds * rate);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = w(i / (float)rate, i);
            return d;
        }

        static AudioClip Clip(string name, int rate, float[] d)
        {
            float peak = 0f;
            foreach (var v in d) peak = Mathf.Max(peak, Mathf.Abs(v));
            if (peak > 0.98f) for (int i = 0; i < d.Length; i++) d[i] *= 0.98f / peak;
            var c = AudioClip.Create(name, d.Length, 1, rate, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip Make(int rate, float seconds, Wave w) { return Clip("sfx", rate, Samples(rate, seconds, w)); }

        /// <summary>A loop with its ends cross-faded so it repeats without a click.</summary>
        static AudioClip Loop(int rate, float seconds, Wave w, float gain)
        {
            int n = (int)(seconds * rate), fade = rate / 4;
            var raw = Samples(rate, seconds + fade / (float)rate, w);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float v = raw[i] * gain;
                if (i < fade) { float k = i / (float)fade; v = v * k + raw[n + i] * gain * (1f - k); }
                d[i] = v;
            }
            return Clip("loop", rate, d);
        }

        /// <summary>One-pole low-pass applied twice (12 dB/oct) to a clip's samples.</summary>
        static float[] LowPass(float[] d, float hz, int rate = Rate)
        {
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * hz / rate);
            float y1 = 0f, y2 = 0f;
            var o = new float[d.Length];
            for (int i = 0; i < d.Length; i++) { y1 += (d[i] - y1) * a; y2 += (y1 - y2) * a; o[i] = y2; }
            return o;
        }

        static float[] HighPass(float[] d, float hz, int rate = Rate)
        {
            var lp = LowPass(d, hz, rate);
            var o = new float[d.Length];
            for (int i = 0; i < d.Length; i++) o[i] = d[i] - lp[i];
            return o;
        }

        /// <summary>
        /// Outdoor reverb: a few discrete slapback echoes off the houses (early reflections), then a diffuse
        /// Schroeder tail (four combs and two all-passes). <paramref name="wet"/> is the reverb level.
        /// </summary>
        static AudioClip Reverb(float[] dry, float wet, float tail, System.Random r)
        {
            int extra = (int)(tail * Rate);
            var o = new float[dry.Length + extra];
            for (int i = 0; i < dry.Length; i++) o[i] = dry[i];
            // Early reflections.
            float[] delays = { 0.043f, 0.071f, 0.112f, 0.168f };
            float[] gains = { 0.32f, 0.24f, 0.18f, 0.12f };
            var dark = LowPass(dry, 3500f);
            for (int k = 0; k < delays.Length; k++)
            {
                int off = (int)(delays[k] * Rate);
                for (int i = 0; i < dark.Length && i + off < o.Length; i++) o[i + off] += dark[i] * gains[k] * wet * 2f;
            }
            // Diffuse tail.
            var input = new float[o.Length];
            for (int i = 0; i < dark.Length; i++) input[i] = dark[i];
            var sum = new float[o.Length];
            int[] combs = { 1557, 1617, 1491, 1422 };
            float fb = Mathf.Clamp(0.7f + tail * 0.08f, 0.7f, 0.88f);
            foreach (var len in combs)
            {
                var buf = new float[len];
                int idx = 0;
                float lp = 0f;
                for (int i = 0; i < o.Length; i++)
                {
                    float y = buf[idx];
                    lp += (y - lp) * 0.35f;
                    buf[idx] = input[i] + lp * fb;
                    idx = (idx + 1) % len;
                    sum[i] += y * 0.25f;
                }
            }
            foreach (var len in new[] { 225, 556 })
            {
                var buf = new float[len];
                int idx = 0;
                for (int i = 0; i < sum.Length; i++)
                {
                    float b = buf[idx];
                    float y = -sum[i] + b;
                    buf[idx] = sum[i] + b * 0.5f;
                    idx = (idx + 1) % len;
                    sum[i] = y;
                }
            }
            for (int i = 0; i < o.Length; i++)
            {
                float fade = i > o.Length - Rate / 5 ? (o.Length - i) / (Rate / 5f) : 1f;
                o[i] = (o[i] + sum[i] * wet) * fade;
            }
            return Clip("reverb", Rate, o);
        }

        /// <summary>Firecracker: a very short, bright crack (pressure spike + high noise), little body.</summary>
        static float[] Pop(System.Random r, float bright)
        {
            var d = Samples(Rate, 0.35f, (t, i) =>
            {
                float spike = t < 0.0015f ? 1f - t / 0.0015f : 0f;
                float noise = N(r) * Mathf.Exp(-t * 55f);
                float body = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 40f) * 0.4f;
                return spike * 1.2f + noise * 0.9f * bright + body;
            });
            return HighPass(d, 120f);
        }

        /// <summary>
        /// Explosion: a sharp transient, a noisy pressure body falling from bright to dark, a sub thump sweeping
        /// down from <paramref name="sub"/> Hz, and a crackling tail of debris and burning fuel.
        /// </summary>
        static float[] Boom(System.Random r, float seconds, float sub, float bright)
        {
            float lp1 = 0f, lp2 = 0f;
            return Samples(Rate, seconds, (t, i) =>
            {
                float n = N(r);
                float cutoff = Mathf.Lerp(0.5f, 0.02f, Mathf.Clamp01(t * 3f));   // filter closes as the blast rolls off
                lp1 += (n - lp1) * cutoff;
                lp2 += (lp1 - lp2) * cutoff;
                float transient = (t < 0.004f ? 1f : 0f) * (1f - t / 0.004f) + n * Mathf.Exp(-t * 60f) * 0.8f * bright;
                float body = lp2 * 3.2f * Mathf.Exp(-t * 2.4f / (seconds * 0.6f));
                float thump = Mathf.Sin(2f * Mathf.PI * sub * t * (1f - 0.35f * Mathf.Clamp01(t / seconds))) * Mathf.Exp(-t * 3.5f) * 0.9f;
                float crack = (r.NextDouble() < 0.0025 * Mathf.Exp(-t * 1.2f) ? N(r) * 0.6f : 0f);
                return transient + body + thump + crack;
            });
        }

        /// <summary>Glass: many small high resonant shards falling over a second, on a noisy break.</summary>
        static float[] Glass(System.Random r)
        {
            int n = (int)(1.1f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)Rate; d[i] = N(r) * Mathf.Exp(-t * 30f) * 0.6f; }
            for (int k = 0; k < 40; k++)
            {
                float start = (float)(r.NextDouble() * r.NextDouble()) * 0.9f;
                float f = 2500f + (float)r.NextDouble() * 5500f, decay = 30f + (float)r.NextDouble() * 60f, amp = 0.15f + (float)r.NextDouble() * 0.2f;
                int s0 = (int)(start * Rate);
                for (int i = s0; i < n; i++)
                {
                    float t = (i - s0) / (float)Rate;
                    if (t > 0.15f) break;
                    d[i] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * decay) * amp;
                }
            }
            return HighPass(d, 800f);
        }

        /// <summary>Metal hit: inharmonic partials ringing down (car panels, bins, cylinders).</summary>
        static float[] Clang(System.Random r)
        {
            float[] ratios = { 1f, 2.76f, 5.4f, 8.93f };
            float f0 = 220f + (float)r.NextDouble() * 120f;
            return Samples(Rate, 1f, (t, i) =>
            {
                float v = N(r) * Mathf.Exp(-t * 80f) * 0.4f;
                for (int k = 0; k < ratios.Length; k++) v += Mathf.Sin(2f * Mathf.PI * f0 * ratios[k] * t) * Mathf.Exp(-t * (4f + k * 3f)) * (0.35f / (k + 1));
                return v;
            });
        }

        /// <summary>Debris raining down: a scatter of thuds and clinks over a second and a half.</summary>
        static AudioClip Debris(System.Random r)
        {
            int n = (int)(1.8f * Rate);
            var d = new float[n];
            for (int k = 0; k < 30; k++)
            {
                float start = 0.15f + (float)r.NextDouble() * 1.5f;
                bool metal = r.NextDouble() < 0.4;
                float f = metal ? 1200f + (float)r.NextDouble() * 2500f : 90f + (float)r.NextDouble() * 120f;
                float amp = (0.08f + (float)r.NextDouble() * 0.15f) * (1f - start / 2f);
                int s0 = (int)(start * Rate);
                for (int i = s0; i < n; i++)
                {
                    float t = (i - s0) / (float)Rate;
                    if (t > 0.2f) break;
                    d[i] += (Mathf.Sin(2f * Mathf.PI * f * t) * 0.7f + N(r) * 0.3f) * Mathf.Exp(-t * (metal ? 35f : 45f)) * amp;
                }
            }
            return Clip("debris", Rate, d);
        }

        /// <summary>Lighter: the flint wheel's rasp, a spark, then the gas catching with a soft whoomp.</summary>
        static AudioClip Lighter(System.Random r)
        {
            var d = Samples(Rate, 0.45f, (t, i) =>
            {
                float rasp = t < 0.07f ? N(r) * (0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI * 180f)) * 0.7f : 0f;
                float spark = t > 0.06f && t < 0.075f ? N(r) * 0.9f : 0f;
                float gas = t > 0.07f ? N(r) * Mathf.Exp(-(t - 0.07f) * 9f) * Mathf.Clamp01((t - 0.07f) * 40f) * 0.5f : 0f;
                return rasp + spark + gas;
            });
            var hp = HighPass(d, 900f);
            return Clip("lighter", Rate, hp);
        }

        static Wave FlameWave(System.Random r)
        {
            float lp = 0f;
            return (t, i) => { lp += (N(r) - lp) * 0.06f; return lp * 2.2f * (0.8f + 0.2f * Mathf.Sin(t * 37f)); };
        }

        static Wave HissWave(System.Random r)
        {
            float lp = 0f;
            return (t, i) =>
            {
                float n = N(r);
                lp += (n - lp) * 0.5f;
                float hiss = (n - lp) * 0.8f;   // high band
                float pop = r.NextDouble() < 0.004 ? N(r) * 1.4f : 0f;
                return hiss + pop;
            };
        }

        static Wave CrackleWave(System.Random r)
        {
            float lp = 0f;
            return (t, i) =>
            {
                lp += (N(r) - lp) * 0.02f;
                float roar = lp * 3f;
                float pop = r.NextDouble() < 0.0015 ? N(r) * 1.2f : 0f;
                float snap = r.NextDouble() < 0.0004 ? 1f : 0f;
                return roar * 0.6f + pop + snap * 0.8f;
            };
        }

        /// <summary>Evening: crickets, the odd bird, a far road and a little wind.</summary>
        static Wave AmbienceWave(System.Random r)
        {
            float wind = 0f, road = 0f;
            float bird = -1f, birdF = 0f;
            return (t, i) =>
            {
                float n = N(r);
                wind += (n - wind) * 0.004f;
                road += (n - road) * 0.0015f;
                float gust = 0.5f + 0.5f * Mathf.Sin(t * 0.4f) * Mathf.Sin(t * 0.13f + 1f);
                float c = Mathf.Repeat(t * 1.3f, 1f) < 0.18f ? Mathf.Sin(2f * Mathf.PI * 4400f * t) * Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 28f * t)) : 0f;
                float c2 = Mathf.Repeat(t * 0.9f + 0.4f, 1f) < 0.15f ? Mathf.Sin(2f * Mathf.PI * 4800f * t) * Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 31f * t)) : 0f;
                if (bird < 0f && r.NextDouble() < 0.00003) { bird = 0f; birdF = 2500f + (float)r.NextDouble() * 1500f; }
                float b = 0f;
                if (bird >= 0f)
                {
                    bird += 1f / MusicRate;
                    b = Mathf.Sin(2f * Mathf.PI * (birdF + 900f * Mathf.Sin(bird * 40f)) * bird) * Mathf.Exp(-bird * 6f) * 0.15f;
                    if (bird > 0.6f) bird = -1f;
                }
                return wind * 0.5f * gust * 6f + road * 4f + (c + c2) * 0.05f + b;
            };
        }

        static float[] Chord(float[] notes, float seconds, float arp)
        {
            return Samples(Rate, seconds, (t, i) =>
            {
                float v = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float s = t - k * arp;
                    if (s < 0f) continue;
                    v += (Mathf.Sin(2f * Mathf.PI * notes[k] * s) + 0.3f * Mathf.Sin(4f * Mathf.PI * notes[k] * s)) * Mathf.Exp(-s * 2.2f) * 0.18f;
                }
                return v;
            });
        }

        /// <summary>
        /// Menu music: a moody cinematic loop at 92 BPM in A minor. Warm detuned pads, a pulsing bass, a soft kick
        /// and shaker, and a sparse plucked motif. 8 bars, made once on first use.
        /// </summary>
        static AudioClip MakeMusic()
        {
            const float bpm = 92f;
            float beat = 60f / bpm;
            int bars = 8;
            float len = bars * 4 * beat;
            int n = (int)(len * MusicRate);
            var d = new float[n];
            var r = new System.Random(21);
            // Chords per bar (A minor: Am, F, C, G, Am, F, Dm, E).
            float[][] chords =
            {
                new[] { 220f, 261.6f, 329.6f }, new[] { 174.6f, 220f, 261.6f }, new[] { 196f, 261.6f, 329.6f }, new[] { 196f, 246.9f, 293.7f },
                new[] { 220f, 261.6f, 329.6f }, new[] { 174.6f, 220f, 261.6f }, new[] { 146.8f, 220f, 293.7f }, new[] { 164.8f, 207.7f, 246.9f },
            };
            float[] bass = { 55f, 43.7f, 65.4f, 49f, 55f, 43.7f, 36.7f, 41.2f };
            float[] motif = { 659.3f, 0f, 587.3f, 523.3f, 0f, 493.9f, 523.3f, 0f };
            float pad = 0f, padLp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)MusicRate;
                int bar = Mathf.Min(bars - 1, (int)(t / (4 * beat)));
                float inBeat = Mathf.Repeat(t, beat);
                // Pad: three detuned saws per chord note, low-passed.
                pad = 0f;
                foreach (var f in chords[bar])
                    for (int k = -1; k <= 1; k++)
                        pad += Saw(f * (1f + k * 0.004f), t) * 0.08f;
                padLp += (pad - padLp) * 0.06f;
                float barT = Mathf.Repeat(t, 4 * beat);
                float swell = Mathf.Clamp01(barT * 2f) * (0.8f + 0.2f * Mathf.Sin(t * 0.7f));
                float v = padLp * swell;
                // Bass on every beat with a short decay.
                v += Mathf.Sin(2f * Mathf.PI * bass[bar] * t) * Mathf.Exp(-inBeat * 4f) * 0.35f;
                // Kick on 1 and 3, shaker on the eighths.
                int beatIdx = (int)(t / beat) % 4;
                if (beatIdx % 2 == 0) v += Mathf.Sin(2f * Mathf.PI * (55f + 90f * Mathf.Exp(-inBeat * 30f)) * inBeat) * Mathf.Exp(-inBeat * 9f) * 0.45f;
                float eighth = Mathf.Repeat(t, beat * 0.5f);
                v += (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-eighth * 60f) * 0.04f;
                // Plucked motif in the second half.
                if (bar >= 4)
                {
                    int step = (int)(barT / (beat * 0.5f)) % motif.Length;
                    float st = Mathf.Repeat(barT, beat * 0.5f);
                    if (motif[step] > 0f) v += Mathf.Sin(2f * Mathf.PI * motif[step] * t) * Mathf.Exp(-st * 7f) * 0.12f;
                }
                d[i] = v;
            }
            // Smooth the loop seam.
            int fade = MusicRate / 10;
            for (int i = 0; i < fade; i++) { float k = i / (float)fade; d[i] *= k; d[n - 1 - i] *= k; }
            return Clip("music", MusicRate, d);
        }

        static float Saw(float f, float t) { return 2f * Mathf.Repeat(f * t, 1f) - 1f; }
    }
}
