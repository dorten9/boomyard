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
        const int SongRate = 24000;

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
            ambience = Ambience(new System.Random(11));
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

        /// <summary>The spray can's hiss while the nozzle is pressed (made the first time it is used).</summary>
        public void Spray(bool on)
        {
            if (sprayLoop == null)
            {
                if (!on) return;
                var r = new System.Random(23);
                float lp = 0f;
                var d = Samples(Rate, 1.4f, (t, i) => { float n = N(r); lp += (n - lp) * 0.35f; return (n - lp) * (0.8f + 0.2f * Mathf.Sin(t * 37f)); });
                sprayLoop = Source(Loop(Rate, 1.2f, (t, i) => d[i % d.Length], 0.5f), 0.35f, true);
            }
            Toggle(sprayLoop, on);
            sprayLoop.volume = 0.35f * Volume;
        }
        AudioSource sprayLoop;

        /// <summary>The ball rattling in the can as it is shaken.</summary>
        public void Rattle()
        {
            if (rattle == null)
            {
                var r = new System.Random(29);
                rattle = Make(Rate, 0.7f, (t, i) =>
                {
                    float k = (t * 9f) % 1f;   // nine knocks
                    return (Mathf.Sin(2f * Mathf.PI * 2600f * t) * 0.5f + N(r) * 0.5f) * Mathf.Exp(-k * 40f) * 0.5f * (1f - t / 0.7f);
                });
            }
            ui.PlayOneShot(rattle, 0.6f * Volume);
        }
        AudioClip rattle;

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

        // ----- shared helpers for the stereo loops (ambience, music) -----

        static readonly float[] SinTab = SinTable();

        static float[] SinTable()
        {
            var t = new float[4097];
            for (int k = 0; k <= 4096; k++) t[k] = (float)System.Math.Sin(2.0 * System.Math.PI * k / 4096.0);
            return t;
        }

        /// <summary>sin(2π·<paramref name="cycles"/>) from a table: phases are kept as wrapped cycle counts.</summary>
        static float Sn(float cycles)
        {
            int w = (int)cycles;
            if (cycles < w) w--;
            float x = (cycles - w) * 4096f;
            int k = (int)x;
            if (k > 4095) k = 4095;
            return SinTab[k] + (SinTab[k + 1] - SinTab[k]) * (x - k);
        }

        /// <summary>White noise in -1..1 as a pure function of the sample index, so a loop's noise is circular.</summary>
        static float Hn(int i, int seed)
        {
            unchecked
            {
                uint x = (uint)i * 0x9E3779B1u + (uint)seed * 0x85EBCA77u;
                x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16;
                return (x >> 8) * (2f / 16777216f) - 1f;
            }
        }

        /// <summary>Random control points for <see cref="Smooth"/>: a slow random curve that wraps around the loop.</summary>
        static float[] Wander(System.Random r, int points, float lo, float hi)
        {
            var p = new float[points];
            for (int k = 0; k < points; k++) p[k] = lo + (hi - lo) * (float)r.NextDouble();
            return p;
        }

        /// <summary>Cyclic Catmull–Rom through the points at loop position <paramref name="u"/> (0..1, wraps).</summary>
        static float Smooth(float[] p, float u)
        {
            int m = p.Length;
            float x = Mathf.Repeat(u, 1f) * m;
            int i1 = (int)x;
            float f = x - i1;
            i1 %= m;
            float a = p[(i1 + m - 1) % m], b = p[i1], c = p[(i1 + 1) % m], d = p[(i1 + 2) % m];
            return b + 0.5f * f * (c - a + f * (2f * a - 5f * b + 4f * c - d + f * (3f * (b - c) + d - a)));
        }

        static float PanL(float p) { return (float)System.Math.Cos((Mathf.Clamp(p, -1f, 1f) + 1f) * Mathf.PI * 0.25f); }
        static float PanR(float p) { return (float)System.Math.Sin((Mathf.Clamp(p, -1f, 1f) + 1f) * Mathf.PI * 0.25f); }

        /// <summary>Adds a stereo frame to an interleaved loop, wrapping past either end.</summary>
        static void Put(float[] st, int frame, float l, float r)
        {
            int n = st.Length >> 1;
            frame %= n;
            if (frame < 0) frame += n;
            st[frame * 2] += l;
            st[frame * 2 + 1] += r;
        }

        /// <summary>An interleaved stereo clip, scaled to the peak <paramref name="peakTo"/> (0: only kept below full scale).</summary>
        static AudioClip StereoClip(string name, int rate, float[] st, float peakTo)
        {
            float peak = 0f;
            foreach (var v in st) peak = Mathf.Max(peak, Mathf.Abs(v));
            float to = peakTo > 0f ? peakTo : Mathf.Min(peak, 0.98f);   // normalise to peakTo, or only keep below full scale
            if (peak > 0f) for (int i = 0; i < st.Length; i++) st[i] *= to / peak;
            var c = AudioClip.Create(name, st.Length / 2, 2, rate, false);
            c.SetData(st, 0);
            return c;
        }

        /// <summary>Zero-delay-feedback state-variable filter (stable at any cutoff): low, band (unit peak) and high outputs.</summary>
        sealed class Svf
        {
            float ic1, ic2, a1, a2, a3, k = 1f;
            public float Lp, Bp, Hp;

            public Svf Set(float hz, float q, int rate)
            {
                float g = (float)System.Math.Tan(System.Math.PI * Mathf.Min(hz, rate * 0.45f) / rate);
                k = 1f / q;
                a1 = 1f / (1f + g * (g + k));
                a2 = g * a1;
                a3 = g * a2;
                return this;
            }

            public void Run(float x)
            {
                float v3 = x - ic2, v1 = a1 * ic1 + a2 * v3, v2 = ic2 + a2 * ic1 + a3 * v3;
                ic1 = 2f * v1 - ic1;
                ic2 = 2f * v2 - ic2;
                Lp = v2;
                Bp = k * v1;
                Hp = x - k * v1 - v2;
            }
        }

        // ----- evening ambience -----

        /// <summary>
        /// Evening street, 36 s stereo: wind in slow gusts with the trees either side rustling, a far road with a
        /// few cars passing by (pitch and tone sweeping as they go), field crickets and a far tree cricket, a few
        /// birds (sparrows, a blackbird's phrase, a distant dove), and once each a wind chime in a gust and a far
        /// dog. Everything is written around a circle (events past the end wrap to the start, noise is a function
        /// of the sample index and filters are warmed up on the loop's end), so it repeats without a seam.
        /// </summary>
        static AudioClip Ambience(System.Random r)
        {
            int rate = MusicRate, n = 36 * rate;
            var st = new float[n * 2];
            float secs = n / (float)rate;
            float gustAt = AmbBed(st, rate, r);
            AmbCars(st, rate, r);
            AmbCrickets(st, rate, r);
            Sparrows(st, rate, r, secs * 0.17f, -0.45f, 0.11f);
            Sparrows(st, rate, r, secs * 0.71f, 0.55f, 0.09f);
            Blackbird(st, rate, r, secs * 0.38f, 0.3f, 0.1f);
            Blackbird(st, rate, r, secs * 0.53f, 0.25f, 0.09f);
            Dove(st, rate, secs * 0.83f, -0.7f, 0.055f);
            Chime(st, rate, r, gustAt * secs, -0.35f, 0.032f);
            Dog(st, rate, r, secs * 0.93f, 0.75f, 0.09f);
            // Overall level: about -24 dBFS RMS (it plays under everything).
            double sum = 0.0;
            foreach (var v in st) sum += v * v;
            float gain = 0.063f / (float)System.Math.Sqrt(sum / st.Length + 1e-12);
            for (int i = 0; i < st.Length; i++) st[i] *= gain;
            return StereoClip("ambience", rate, st, 0f);
        }

        /// <summary>Wind, leaves and the road's rumble. Returns where in the loop (0..1) the strongest gust is.</summary>
        static float AmbBed(float[] st, int rate, System.Random r)
        {
            int n = st.Length / 2;
            float secs = n / (float)rate;
            var gust = Wander(r, 9, 0f, 1f);
            var flutter = Wander(r, 150, -1f, 1f);
            var roadLvl = Wander(r, 6, 0.75f, 1.25f);
            var treeA = Wander(r, 45, 0f, 1f);
            var treeB = Wander(r, 45, 0f, 1f);
            Svf wl = new Svf(), wr = new Svf(), la = new Svf(), lb = new Svf(), rum = new Svf(), hum = new Svf().Set(320f, 0.5f, rate);
            rum.Set(90f, 0.6f, rate);
            float hpA = 1f - Mathf.Exp(-2f * Mathf.PI * 70f / rate), crA = 1f - Mathf.Exp(-2f * Mathf.PI * 18f / rate);
            float hpL = 0f, hpR = 0f, ca = 0f, ca2 = 0f, cb = 0f, cb2 = 0f;
            float gL = 0f, gR = 0f, eA = 0f, eB = 0f, road = 1f, lag = 0.6f / secs;
            int warm = 3 * rate;
            for (int j = -warm, c = 0; j < n; j++, c++)
            {
                int i = j < 0 ? j + n : j;
                if ((c & 31) == 0)
                {
                    float u = i / (float)n;
                    gL = GustAt(gust, flutter, u);
                    gR = GustAt(gust, flutter, u - lag);   // the gust reaches the right a moment later
                    wl.Set(110f + 480f * gL, 0.6f, rate);
                    wr.Set(110f + 480f * gR, 0.6f, rate);
                    eA = Mathf.Clamp01((gL - 0.3f) * 1.5f) * (0.35f + 0.65f * Mathf.Clamp01(Smooth(treeA, u)));
                    eB = Mathf.Clamp01((gR - 0.35f) * 1.5f) * (0.35f + 0.65f * Mathf.Clamp01(Smooth(treeB, u)));
                    la.Set(2300f + 1600f * eA, 0.8f, rate);
                    lb.Set(2900f + 1400f * eB, 0.8f, rate);
                    road = Smooth(roadLvl, u);
                }
                // Wind: low-passed noise that opens up and swells in the gusts, different noise each side.
                wl.Run(Hn(i, 11));
                wr.Run(Hn(i, 12));
                float windL = wl.Lp * (0.1f + gL * gL), windR = wr.Lp * (0.1f + gR * gR);
                hpL += (windL - hpL) * hpA; windL -= hpL;
                hpR += (windR - hpR) * hpA; windR -= hpR;
                // Leaves: band-passed noise, crinkled by a slow random modulator, only when the gust is up.
                ca += (Hn(i, 13) - ca) * crA; ca2 += (ca - ca2) * crA;
                cb += (Hn(i, 14) - cb) * crA; cb2 += (cb - cb2) * crA;
                float mA = ca2 * 45f, mB = cb2 * 45f;
                la.Run(Hn(i, 15));
                lb.Run(Hn(i, 16));
                float leafA = la.Bp * eA * (0.3f + Mathf.Min(mA * mA, 3f)), leafB = lb.Bp * eB * (0.3f + Mathf.Min(mB * mB, 3f));
                // The town: a deep road rumble and a softer broad hum, nearly mono.
                float nr = Hn(i, 17);
                rum.Run(nr);
                hum.Run(nr);
                float town = (rum.Lp * 0.3f + hum.Bp * 0.05f) * road;
                if (j < 0) continue;
                st[2 * i] += windL * 0.34f + leafA * 0.045f + leafB * 0.014f + town;
                st[2 * i + 1] += windR * 0.34f + leafA * 0.016f + leafB * 0.040f + town * 0.95f;
            }
            int best = 0;
            for (int k = 1; k < gust.Length; k++) if (gust[k] > gust[best]) best = k;
            return best / (float)gust.Length;
        }

        static float GustAt(float[] gust, float[] flutter, float u)
        {
            float v = Mathf.Clamp01(Smooth(gust, u));
            v = v * v * (3f - 2f * v);
            return Mathf.Clamp01(0.12f + 0.85f * v + 0.08f * Smooth(flutter, u));
        }

        /// <summary>A few cars on the road beyond the houses, alternating directions, one of them far away.</summary>
        static void AmbCars(float[] st, int rate, System.Random r)
        {
            float secs = st.Length / 2 / (float)rate;
            float[] at = { 0.1f, 0.35f, 0.6f, 0.84f };
            for (int k = 0; k < at.Length; k++)
            {
                float tc = (at[k] + 0.05f * N(r)) * secs;
                float lateral = k == 2 ? 95f + 30f * (float)r.NextDouble() : 24f + 30f * (float)r.NextDouble();
                float speed = 11f + 5f * (float)r.NextDouble();
                Car(st, rate, tc, lateral, speed, k % 2 == 0 ? 1f : -1f, 0.13f, r.Next(1, 1 << 20));
            }
        }

        /// <summary>
        /// One car passing <paramref name="lateral"/> metres away: tyre roar and a little engine, louder and
        /// brighter as it nears (1/distance, less air absorption), Doppler-shifted (higher coming, lower going)
        /// and panned across.
        /// </summary>
        static void Car(float[] st, int rate, float tc, float lateral, float speed, float dir, float level, int seed)
        {
            float half = 7.5f;
            int len = (int)(2f * half * rate), s0 = (int)((tc - half) * rate);
            Svf tire = new Svf(), eng = new Svf();
            var rnd = new System.Random(seed);
            float engF = 40f + 25f * (float)rnd.NextDouble(), ph = 0f, gl = 0f, gr = 0f, gain = 0f, dop = 1f, hp = 0f;
            float hpA = 1f - Mathf.Exp(-2f * Mathf.PI * 120f / rate);
            int n = st.Length / 2, f = s0 % n;
            if (f < 0) f += n;
            for (int k = 0; k < len; k++, f++)
            {
                if (f >= n) f = 0;
                if ((k & 31) == 0)
                {
                    float t = k / (float)rate - half;
                    float x = dir * speed * t, d = (float)System.Math.Sqrt(x * x + lateral * lateral);
                    dop = 343f / (343f + speed * speed * t / d);
                    float near = lateral / d;
                    float fade = Mathf.Clamp01(k / (1.5f * rate)) * Mathf.Clamp01((len - k) / (1.5f * rate));
                    gain = level * (float)System.Math.Pow(Mathf.Min(1f, 20f / d), 1.3) * fade;
                    tire.Set((260f + 2200f * near * near) * dop, 0.55f, rate);
                    eng.Set((180f + 700f * near) * dop, 0.6f, rate);
                    float p = x / d * 0.85f;
                    gl = PanL(p) * gain;
                    gr = PanR(p) * gain;
                }
                tire.Run(Hn(k, seed));
                ph += engF * dop / rate;
                if (ph >= 1f) ph -= 1f;
                eng.Run(2f * ph - 1f);   // a buzzy sawtooth, low-passed
                float v = tire.Lp * 2.2f + eng.Lp * 0.35f;
                hp += (v - hp) * hpA;
                v -= hp;
                st[2 * f] += v * gl;
                st[2 * f + 1] += v * gr;
            }
        }

        /// <summary>Five field crickets, each with its own pitch, tempo, place and level, and one far tree cricket trilling.</summary>
        static void AmbCrickets(float[] st, int rate, System.Random r)
        {
            float[] pitch = { 4350f, 4650f, 4100f, 4900f, 3900f };
            float[] pan = { -0.55f, 0.6f, 0.15f, -0.85f, 0.85f };
            float[] lvl = { 1f, 0.7f, 0.45f, 0.35f, 0.3f };
            float[] gap = { 0.42f, 0.55f, 0.36f, 0.7f, 0.5f };
            int[] pulses = { 4, 3, 4, 5, 3 };
            for (int k = 0; k < pitch.Length; k++)
                Cricket(st, rate, r, pitch[k] * (1f + 0.01f * N(r)), pan[k], lvl[k] * 0.07f, gap[k], pulses[k], 26f + 8f * (float)r.NextDouble());
            TreeCricket(st, rate, r, 2850f, 0.4f, 0.02f);
        }

        /// <summary>
        /// A field cricket: chirps of a few pulses (each a near-pure tone gliding slightly down, the first one
        /// softer), with jitter in pitch, timing and loudness from chirp to chirp, singing in bouts with pauses.
        /// A faint, delayed copy on the other side stands in for the reflection off the houses.
        /// </summary>
        static void Cricket(float[] st, int rate, System.Random r, float hz, float pan, float level, float gap, int pulses, float pulseRate)
        {
            float secs = st.Length / 2 / (float)rate;
            float gl = PanL(pan), gr = PanR(pan), el = PanL(-pan * 0.5f), er = PanR(-pan * 0.5f);
            float echo = 0.012f + 0.02f * (float)r.NextDouble();
            float t0 = (float)r.NextDouble() * 3f, t = t0, boutEnd = t0 + 3f + 12f * (float)r.NextDouble(), end = t0 + secs - gap;
            while (t < end)
            {
                if (t > boutEnd)
                {
                    t += 1.5f + 5f * (float)r.NextDouble();
                    boutEnd = t + 5f + 14f * (float)r.NextDouble();
                    continue;
                }
                float f = hz * (1f + 0.006f * N(r)), a = level * (0.85f + 0.15f * N(r)), pr = pulseRate * (1f + 0.04f * N(r));
                int np = pulses + (r.NextDouble() < 0.2 ? (r.NextDouble() < 0.5 ? -1 : 1) : 0);
                for (int p = 0; p < np; p++)
                {
                    float pt = t + p / pr + 0.0015f * N(r), plen = 0.55f / pr * (1f + 0.08f * N(r));
                    float pa = a * (p == 0 ? 0.6f : 1f) * (1f + 0.12f * N(r));
                    Pulse(st, rate, pt, plen, f, pa, gl, gr);
                    Pulse(st, rate, pt + echo, plen, f, pa * 0.25f, el, er);
                }
                t += np / pr + gap * (1f + 0.15f * N(r));
            }
        }

        /// <summary>A tree cricket far off: long soft trills of fast pulses that swell in and out.</summary>
        static void TreeCricket(float[] st, int rate, System.Random r, float hz, float pan, float level)
        {
            float secs = st.Length / 2 / (float)rate;
            float gl = PanL(pan), gr = PanR(pan);
            float t0 = (float)r.NextDouble(), t = t0, end = t0 + secs - 1f;
            while (t < end)
            {
                float stop = Mathf.Min(t + 2f + 5f * (float)r.NextDouble(), end);
                float pr = 46f * (1f + 0.03f * N(r)), f = hz * (1f + 0.005f * N(r)), a0 = level * (0.8f + 0.2f * N(r));
                for (float pt = t; pt < stop; pt += 1f / pr)
                {
                    float swell = Mathf.Clamp01((pt - t) / 0.5f) * Mathf.Clamp01((stop - pt) / 0.4f);
                    Pulse(st, rate, pt + 0.001f * N(r), 0.5f / pr, f, a0 * swell * (1f + 0.1f * N(r)), gl, gr);
                }
                t = stop + 0.4f + 1.6f * (float)r.NextDouble();
            }
        }

        static void Pulse(float[] st, int rate, float start, float len, float hz, float amp, float gl, float gr)
        {
            int n = st.Length / 2, m = (int)(len * rate), f = (int)(start * rate) % n;
            if (f < 0) f += n;
            float ph = 0f, inc = hz / rate;
            for (int k = 0; k < m; k++, f++)
            {
                if (f >= n) f = 0;
                float u = k / (float)m;
                float a = Mathf.Min(1f, u * 6.667f), b = Mathf.Min(1f, (1f - u) * 2.5f);
                float env = a * a * (3f - 2f * a) * b * b * (3f - 2f * b);
                ph += inc * (1.015f - 0.03f * u);
                if (ph >= 1f) ph -= 1f;
                float v = (Sn(ph) + 0.08f * Sn(2f * ph)) * env * amp;
                st[2 * f] += v * gl;
                st[2 * f + 1] += v * gr;
            }
        }

        delegate float Contour(float u);

        /// <summary>A whistled bird note: pitch contour over the note (0..1), soft envelope, a little 2nd harmonic.</summary>
        static void Tone(float[] st, int rate, float start, float len, Contour hz, float amp, float h2, float attack, float gl, float gr)
        {
            int s0 = (int)(start * rate), m = (int)(len * rate);
            float ph = 0f;
            for (int k = 0; k < m; k++)
            {
                float u = k / (float)m;
                float a = Mathf.Clamp01(u / attack), b = Mathf.Clamp01((1f - u) / 0.35f);
                float env = a * a * (3f - 2f * a) * b * b * (3f - 2f * b);
                ph += hz(u) / rate;
                if (ph >= 1f) ph -= 1f;
                float v = (Sn(ph) + h2 * Sn(2f * ph)) * env * amp;
                Put(st, s0 + k, v * gl, v * gr);
            }
        }

        /// <summary>A bird note plus its echo off the houses (later, softer, from the other side).</summary>
        static void Call(float[] st, int rate, float start, float len, Contour hz, float amp, float h2, float attack, float pan, float echo)
        {
            Tone(st, rate, start, len, hz, amp, h2, attack, PanL(pan), PanR(pan));
            Tone(st, rate, start + echo, len, hz, amp * 0.18f, h2, attack, PanL(-pan * 0.4f), PanR(-pan * 0.4f));
        }

        /// <summary>House sparrows: a short burst of "cheep"s, each an up-and-down sweep.</summary>
        static void Sparrows(float[] st, int rate, System.Random r, float start, float pan, float amp)
        {
            int count = 3 + r.Next(4);
            float t = start;
            for (int c = 0; c < count; c++)
            {
                float b = 2900f + 900f * (float)r.NextDouble(), bend = 0.3f + 0.25f * (float)r.NextDouble(), len = 0.06f + 0.05f * (float)r.NextDouble();
                Call(st, rate, t, len, u => b * (0.82f + bend * Mathf.Sin(Mathf.PI * u)), amp * (0.65f + 0.35f * (float)r.NextDouble()), 0.25f, 0.2f, pan + 0.1f * N(r), 0.07f);
                t += len + 0.08f + 0.25f * (float)r.NextDouble();
            }
        }

        /// <summary>A blackbird's fluted phrase: a handful of varied notes (rising, falling, arched, warbled) and a soft twitter.</summary>
        static void Blackbird(float[] st, int rate, System.Random r, float start, float pan, float amp)
        {
            float t = start, baseF = 1800f + 500f * (float)r.NextDouble();
            int notes = 5 + r.Next(3);
            for (int k = 0; k < notes; k++)
            {
                float f0 = baseF * (0.85f + 0.5f * (float)r.NextDouble()), len = 0.1f + 0.22f * (float)r.NextDouble();
                Contour c;
                switch (r.Next(4))
                {
                    case 0: c = u => f0 * (0.88f + 0.25f * u); break;
                    case 1: c = u => f0 * (1.15f - 0.3f * u); break;
                    case 2: c = u => f0 * (0.9f + 0.25f * Mathf.Sin(Mathf.PI * u)); break;
                    default: { float l = len; c = u => f0 * (1f + 0.045f * Mathf.Sin(2f * Mathf.PI * 22f * l * u)); break; }
                }
                Call(st, rate, t, len, c, amp * (0.6f + 0.4f * (float)r.NextDouble()), 0.12f, 0.15f, pan, 0.11f);
                t += len + 0.05f + 0.1f * (float)r.NextDouble();
            }
            for (int k = 0; k < 3; k++)
            {
                float f = 4000f + 900f * (float)r.NextDouble();
                Call(st, rate, t, 0.035f, u => f * (1.1f - 0.2f * u), amp * 0.3f, 0.1f, 0.2f, pan, 0.11f);
                t += 0.055f;
            }
        }

        /// <summary>A mourning dove far off: "coo-OOO-oo ... coo, coo, coo".</summary>
        static void Dove(float[] st, int rate, float start, float pan, float amp)
        {
            Call(st, rate, start, 0.32f, u => 470f + 70f * u, amp * 0.6f, 0.3f, 0.4f, pan, 0.15f);
            Call(st, rate, start + 0.4f, 0.75f, u => 540f + 50f * Mathf.Sin(Mathf.PI * u * 0.7f) - 80f * u, amp, 0.3f, 0.3f, pan, 0.15f);
            for (int k = 0; k < 3; k++) Call(st, rate, start + 1.75f + 0.62f * k, 0.48f, u => 505f - 40f * u, amp * 0.8f, 0.3f, 0.35f, pan, 0.15f);
        }

        /// <summary>A wind chime on a porch, stirred by the strongest gust: a few tubes struck (tube partials 1 : 2.76 : 5.40).</summary>
        static void Chime(float[] st, int rate, System.Random r, float start, float pan, float amp)
        {
            float[] scale = { 1047f, 1175f, 1319f, 1568f, 1760f, 2093f };
            float[] ratio = { 1f, 2.756f, 5.404f }, part = { 1f, 0.4f, 0.15f }, decay = { 1.4f, 3.2f, 7f };
            int hits = 3 + r.Next(3);
            float t = start;
            var ph = new float[3];
            var env = new float[3];
            for (int h = 0; h < hits; h++)
            {
                float f = scale[r.Next(scale.Length)], a = amp * (0.5f + 0.5f * (float)r.NextDouble()), p = pan + 0.15f * N(r);
                float gl = PanL(p), gr = PanR(p);
                int s0 = (int)(t * rate), m = 3 * rate;
                for (int q = 0; q < 3; q++) { ph[q] = 0f; env[q] = part[q]; }
                for (int k = 0; k < m; k++)
                {
                    float v = 0f;
                    for (int q = 0; q < 3; q++)
                    {
                        ph[q] += f * ratio[q] / rate;
                        if (ph[q] >= 1f) ph[q] -= 1f;
                        v += Sn(ph[q]) * env[q];
                        env[q] *= 1f - decay[q] / rate;
                    }
                    v *= a * Mathf.Clamp01(k / (0.002f * rate));
                    Put(st, s0 + k, v * gl, v * gr);
                }
                t += 0.2f + 0.55f * (float)r.NextDouble();
            }
        }

        /// <summary>A dog a few gardens away: two barks (harmonics shaped by two formants, rough, muffled by distance) and their echoes.</summary>
        static void Dog(float[] st, int rate, System.Random r, float start, float pan, float amp)
        {
            int barks = 2 + (r.NextDouble() < 0.4 ? 1 : 0);
            float t = start;
            for (int b = 0; b < barks; b++)
            {
                float f0 = 420f + 70f * (float)r.NextDouble(), ph = 0f, a = amp * (0.8f + 0.2f * (float)r.NextDouble());
                int s0 = (int)(t * rate), m = (int)(0.17f * rate), seed = r.Next(1, 1 << 20), e1d = (int)(0.21f * rate), e2d = (int)(0.47f * rate);
                float dl = PanL(pan), dr = PanR(pan), xl = 0.3f * PanL(-pan * 0.3f), xr = 0.3f * PanR(-pan * 0.3f), c = 0.12f * PanL(0f);
                for (int k = 0; k < m; k++)
                {
                    float u = k / (float)m, f = f0 * (1.15f - 0.35f * u);
                    ph += f / rate;
                    if (ph >= 1f) ph -= 1f;
                    float v = 0f;
                    for (int h = 1; h <= 10; h++)
                    {
                        float fh = f * h, e1 = (fh - 800f) / 450f, e2 = (fh - 1700f) / 500f;
                        float w = (Mathf.Exp(-e1 * e1) + 0.6f * Mathf.Exp(-e2 * e2) + 0.15f) / (1f + fh * fh / 1.2e6f);
                        v += Sn(ph * h) * w;
                    }
                    float env = Mathf.Clamp01(k / (0.008f * rate)) * (float)System.Math.Pow(1f - u, 1.5);
                    v = (v * 0.35f + Hn(k, seed) * 0.08f) * env * a;
                    Put(st, s0 + k, v * dl, v * dr);
                    Put(st, s0 + k + e1d, v * xl, v * xr);
                    Put(st, s0 + k + e2d, v * c, v * c);
                }
                t += 0.36f + 0.12f * (float)r.NextDouble();
            }
        }

        // ----- menu music -----

        /// <summary>
        /// Menu music: a relaxed, swung indie-funk groove at 104 BPM in G major, 16 bars (about 37 s, stereo).
        /// A modelled kit (pitch-swept kick with a click, snare body plus wires, hats, a shaker), a plucked
        /// Karplus–Strong bass and guitars, an FM electric piano with chorus comping Gmaj7–Em7–Am7–D7, guitar
        /// licks closing the first half and a whistled tune over Cmaj7–Bm7–Em7–A7 in the second. Timing and
        /// velocities are humanised; the mix gets a stereo room reverb, gentle compression and soft saturation.
        /// Notes wrap around the loop and the effects are warmed up on its end, so it repeats seamlessly.
        /// Made once on first use.
        /// </summary>
        static AudioClip MakeMusic()
        {
            int rate = SongRate;
            float s16 = 60f / 104f / 4f, swing = 0.12f;
            int bars = 16, n = (int)(bars * 16 * s16 * rate);
            var r = new System.Random(21);
            var mx = new Mix(rate, n);
            System.Func<int, float, float> at = (bar, step) => ((bar * 16 + step) + (((int)step & 1) == 1 ? swing : 0f)) * s16 + 0.004f * (N(r) + N(r));

            // Harmony per half bar (indices into the chord tables): EP voicing, guitar triad, bass root (MIDI).
            int[][] epV =
            {
                new[] { 55, 59, 62, 66 }, new[] { 55, 59, 62, 64 }, new[] { 55, 57, 60, 64 }, new[] { 55, 57, 60, 62 },
                new[] { 54, 57, 60, 62 }, new[] { 55, 59, 60, 64 }, new[] { 54, 57, 59, 62 }, new[] { 55, 57, 61, 64 },
            };
            int[][] gtV =
            {
                new[] { 71, 74, 78 }, new[] { 71, 74, 76 }, new[] { 69, 72, 76 }, new[] { 69, 72, 74 },
                new[] { 66, 69, 72 }, new[] { 67, 71, 76 }, new[] { 66, 69, 74 }, new[] { 64, 67, 73 },
            };
            int[] root = { 43, 40, 45, 38, 38, 36, 35, 45 };
            //            Gmaj7 Em7 Am7 D7sus D7 Cmaj7 Bm7 A7
            int[] prog = { 0, 0, 1, 1, 2, 2, 3, 4, 0, 0, 1, 1, 2, 2, 3, 4, 5, 5, 6, 6, 1, 1, 7, 7, 5, 5, 6, 6, 2, 2, 3, 4 };
            System.Func<int, float, int> chordAt = (bar, step) => prog[(bar % bars) * 2 + (step >= 8f ? 1 : 0)];

            for (int bar = 0; bar < bars; bar++)
            {
                bool b = bar >= 8, fill = bar == 15;
                // Kick, snare (backbeat, ghost notes, a fill into the top), hats (open on the "and" of 4 every other bar), shaker in the B half.
                int[] kicks = bar % 2 == 0 || fill ? new[] { 0, 7, 10 } : new[] { 0, 3, 10, 11 };
                foreach (var s in kicks) mx.Add(Kick(mx.Tmp, rate, s == 0 ? 1f : s == 10 ? 0.85f : 0.55f, r), at(bar, s), 0.42f, 0f, 0f);
                for (int s = 0; s < 16; s++)
                {
                    float v = s == 4 || s == 12 ? 0.95f : 0f;
                    if (fill && s >= 13) v = 0.35f + 0.17f * (s - 12);
                    else if (bar == 7 && (s == 13 || s == 15)) v = 0.3f;
                    else if (v == 0f && (s == 7 || s == 9 || s == 15) && r.NextDouble() < 0.45) v = 0.14f + 0.06f * (float)r.NextDouble();
                    if (v > 0f) mx.Add(Snare(mx.Tmp, rate, v * (1f + 0.06f * N(r)), r), at(bar, s), 0.7f, 0.05f, 0.18f);
                    bool open = s == 14 && (bar % 2 == 1);
                    float hv = (s & 1) == 0 ? (s % 4 == 0 ? 0.75f : 0.55f) : (r.NextDouble() < 0.45 ? 0.22f : 0f);
                    if (open) hv = 0.5f;
                    if (hv > 0f) mx.Add(Hat(mx.Tmp, rate, hv * (1f + 0.1f * N(r)), open ? 2f * s16 : 0f, r), at(bar, s), 0.4f, 0.3f, 0.05f);
                    if (b) mx.Add(Shaker(mx.Tmp, rate, (s % 4 == 2 ? 0.55f : s % 2 == 0 ? 0.4f : 0.25f) * (1f + 0.15f * N(r)), r), at(bar, s) - 0.01f, 0.25f, -0.5f, 0.1f);
                }
                if (bar == 0 || bar == 8) mx.Add(Crash(mx.Tmp, rate, bar == 0 ? 0.35f : 0.5f, r), at(bar, 0), 0.25f, -0.3f, 0.2f);

                // Bass: root, ghost, octave pop, fifth, and an approach note into the next bar's root.
                int nextRoot = root[chordAt(bar + 1, 0)];
                float[][] bp = bar % 2 == 0
                    ? new[] { new[] { 0f, 0f, 3f, 1f }, new[] { 3f, 0f, 0.6f, 0.5f }, new[] { 6f, 12f, 1.4f, 0.7f }, new[] { 8f, 7f, 1.8f, 0.8f }, new[] { 10f, 0f, 3f, 0.9f }, new[] { 14f, 99f, 1.8f, 0.75f } }
                    : new[] { new[] { 0f, 0f, 4f, 1f }, new[] { 6f, 7f, 0.8f, 0.6f }, new[] { 7f, 12f, 1f, 0.7f }, new[] { 10f, 0f, 1.8f, 0.85f }, new[] { 12f, 7f, 1.8f, 0.6f }, new[] { 14f, 99f, 1.8f, 0.75f } };
                foreach (var e in bp)
                {
                    int rt = root[chordAt(bar, e[0])];
                    int note = e[1] == 99f ? (nextRoot < rt ? nextRoot + 1 : nextRoot - 1) : rt + (int)e[1];
                    if (e[1] == 99f && nextRoot == rt) note = rt + 7;
                    mx.Add(Bass(mx.Tmp, rate, Hz(note), e[3] * (1f + 0.08f * N(r)), e[2] * s16, r), at(bar, e[0]), 0.5f, 0f, 0f);
                }

                // Electric piano comping (a slight roll through each chord).
                float[][] cp = bar % 4 == 3
                    ? new[] { new[] { 0f, 3f, 0.75f }, new[] { 3f, 3f, 0.6f }, new[] { 8f, 7f, 0.8f } }
                    : new[] { new[] { 0f, 5f, 0.8f }, new[] { 6f, 2f, 0.55f }, new[] { 10f, 5f, 0.7f } };
                foreach (var e in cp)
                {
                    var ch = epV[chordAt(bar, e[0])];
                    float t0 = at(bar, e[0]);
                    for (int k = 0; k < ch.Length; k++)
                        mx.AddEp(EPiano(mx.Tmp, rate, Hz(ch[k]), e[2] * (1f + 0.1f * N(r)), e[1] * s16), t0 + k * 0.006f + 0.003f * N(r), 0.17f, 0.25f);
                }

                // Guitar: muted chord stabs on the backbeat (plus an upstroke in the A half), panned right.
                float[][] gp = b ? new[] { new[] { 4f, 1f, 0.7f }, new[] { 12f, 1f, 0.7f } }
                                 : new[] { new[] { 4f, 1f, 0.8f }, new[] { 12f, 1f, 0.8f }, new[] { 14f, 0.6f, 0.45f } };
                foreach (var e in gp)
                {
                    var tri = gtV[chordAt(bar, e[0])];
                    bool up = e[0] == 14f;
                    float t0 = at(bar, e[0]);
                    for (int k = 0; k < tri.Length; k++)
                    {
                        int q = up ? tri.Length - 1 - k : k;
                        mx.Add(Pluck(mx.Tmp, rate, Hz(tri[q]), e[2] * (1f + 0.1f * N(r)) * (up ? 1f : 1f - 0.12f * k), 0.6f, 1.2f, e[1] * s16, r), t0 + k * 0.007f, 0.4f, 0.45f, 0.25f);
                    }
                }
            }

            // Lead guitar licks closing each A phrase, landing on the B half.
            int[][] licks = { new[] { 3, 8, 69, 2 }, new[] { 3, 10, 71, 2 }, new[] { 3, 12, 74, 2 }, new[] { 3, 14, 76, 2 },
                              new[] { 7, 8, 76, 2 }, new[] { 7, 10, 74, 2 }, new[] { 7, 12, 71, 2 }, new[] { 7, 14, 69, 2 }, new[] { 8, 0, 67, 6 } };
            foreach (var l in licks)
                mx.Add(Pluck(mx.Tmp, rate, Hz(l[2]), 0.8f * (1f + 0.08f * N(r)), 0.55f, 2.5f, l[3] * s16, r), at(l[0], l[1]), 0.55f, -0.25f, 0.3f);

            // The tune, whistled (bar, step, MIDI, length in 16ths); its second half doubled softly on guitar an octave down.
            int[][] tune =
            {
                new[] { 8, 2, 76, 2 }, new[] { 8, 4, 79, 6 }, new[] { 8, 10, 81, 2 }, new[] { 8, 12, 79, 4 },
                new[] { 9, 2, 78, 2 }, new[] { 9, 4, 74, 6 }, new[] { 9, 10, 76, 2 }, new[] { 9, 12, 74, 2 }, new[] { 9, 14, 71, 2 },
                new[] { 10, 2, 79, 2 }, new[] { 10, 4, 83, 6 }, new[] { 10, 10, 81, 2 }, new[] { 10, 12, 79, 4 },
                new[] { 11, 2, 81, 4 }, new[] { 11, 6, 79, 2 }, new[] { 11, 8, 76, 6 },
                new[] { 12, 2, 76, 2 }, new[] { 12, 4, 79, 6 }, new[] { 12, 10, 81, 2 }, new[] { 12, 12, 79, 4 },
                new[] { 13, 2, 78, 2 }, new[] { 13, 4, 74, 6 }, new[] { 13, 10, 76, 2 }, new[] { 13, 12, 78, 2 }, new[] { 13, 14, 81, 2 },
                new[] { 14, 0, 79, 4 }, new[] { 14, 4, 76, 2 }, new[] { 14, 6, 74, 2 }, new[] { 14, 8, 76, 8 },
                new[] { 15, 2, 74, 4 }, new[] { 15, 6, 72, 2 }, new[] { 15, 8, 71, 3 },
            };
            var wAt = new float[tune.Length];
            var wLen = new float[tune.Length];
            var wHz = new float[tune.Length];
            for (int k = 0; k < tune.Length; k++)
            {
                wAt[k] = at(tune[k][0], tune[k][1]);
                wLen[k] = tune[k][3] * s16;
                wHz[k] = Hz(tune[k][2]);
                if (tune[k][0] >= 12)
                    mx.Add(Pluck(mx.Tmp, rate, wHz[k] * 0.5f, 0.6f, 0.5f, 2f, wLen[k], r), wAt[k] + 0.01f, 0.22f, -0.35f, 0.3f);
            }
            Whistle(mx, r, wAt, wLen, wHz, 0.24f, 0.08f, 0.35f);

            Chorus(mx.Ep, mx.St, rate, -0.25f);
            StereoVerb(mx.Send, mx.St, rate, 0.8f, 0.35f, 0.07f);
            Master(mx.St, rate);
            return StereoClip("music", rate, mx.St, 0.89f);
        }

        static float Hz(int midi) { return 440f * (float)System.Math.Pow(2.0, (midi - 69) / 12.0); }

        /// <summary>The music's buses: the interleaved stereo mix, a mono reverb send, a mono electric-piano bus (chorused later), and a scratch buffer for one note.</summary>
        sealed class Mix
        {
            public readonly int Rate, N;
            public readonly float[] St, Send, Ep, Tmp;

            public Mix(int rate, int n)
            {
                Rate = rate;
                N = n;
                St = new float[n * 2];
                Send = new float[n];
                Ep = new float[n];
                Tmp = new float[rate * 3];
            }

            int Frame(float at) { int f = (int)(at * Rate) % N; return f < 0 ? f + N : f; }

            /// <summary>Mixes the first <paramref name="len"/> samples of Tmp in at a time, wrapping around the loop.</summary>
            public void Add(int len, float at, float gain, float pan, float send)
            {
                int f = Frame(at);
                float gl = PanL(pan) * gain, gr = PanR(pan) * gain, gs = gain * send;
                for (int k = 0; k < len; k++, f++)
                {
                    if (f >= N) f = 0;
                    float v = Tmp[k];
                    St[2 * f] += v * gl;
                    St[2 * f + 1] += v * gr;
                    Send[f] += v * gs;
                }
            }

            public void AddEp(int len, float at, float gain, float send)
            {
                int f = Frame(at);
                for (int k = 0; k < len; k++, f++)
                {
                    if (f >= N) f = 0;
                    Ep[f] += Tmp[k] * gain;
                    Send[f] += Tmp[k] * gain * send;
                }
            }

            public void Put(int frame, float v, float gl, float gr, float send)
            {
                int f = frame % N;
                if (f < 0) f += N;
                St[2 * f] += v * gl;
                St[2 * f + 1] += v * gr;
                Send[f] += v * send;
            }
        }

        /// <summary>Soft saturation: a cheap tanh (rational, exact at 0 and ±3, flat beyond).</summary>
        static float Soft(float x)
        {
            if (x > 3f) return 1f;
            if (x < -3f) return -1f;
            float x2 = x * x;
            return x * (27f + x2) / (27f + 9f * x2);
        }

        /// <summary>Kick: a sine whose pitch drops fast from ~150 to 50 Hz (the head), a short click (the beater), softly saturated.</summary>
        static int Kick(float[] o, int rate, float vel, System.Random r)
        {
            int m = (int)(0.42f * rate), tail = (int)(0.02f * rate);
            float ph = 0f, dt = 1f / rate, lp = 0f, drive = 0.6f + 0.6f * vel;
            float eF = 1f, kF = Mathf.Exp(-30f * dt), eA = 1.3f, kA = Mathf.Exp(-9f * dt), eC = 0.6f, kC = Mathf.Exp(-700f * dt);
            for (int k = 0; k < m; k++)
            {
                ph += (50f + 100f * eF) * dt;
                if (ph >= 1f) ph -= 1f;
                lp += (N(r) - lp) * 0.5f;
                o[k] = Soft((Sn(ph) * eA + lp * eC) * drive) * Mathf.Min(1f, (m - k) / (float)tail);
                eF *= kF; eA *= kA; eC *= kC;
            }
            return m;
        }

        /// <summary>Snare: a tuned drum-head body (two modes, the low one bending down) plus high-passed noise for the wires; ghosts are shorter.</summary>
        static int Snare(float[] o, int rate, float vel, System.Random r)
        {
            int m = (int)(0.3f * rate), tail = (int)(0.02f * rate), att = System.Math.Max(1, rate / 1000);
            float p1 = 0f, p2 = 0f, lp = 0f, top = 0f, dt = 1f / rate, g = vel * 1.2f;
            float eP = 0.2f, kP = Mathf.Exp(-50f * dt), eB = 0.9f, kB = Mathf.Exp(-20f * dt), eW = 0.9f, kW = Mathf.Exp(-(14f + (1f - vel) * 25f) * dt);
            for (int k = 0; k < m; k++)
            {
                p1 += 190f * (1f + eP) * dt;
                p2 += 335f * dt;
                if (p1 >= 1f) p1 -= 1f;
                if (p2 >= 1f) p2 -= 1f;
                float nz = N(r);
                lp += (nz - lp) * 0.45f;
                top += (nz - lp - top) * 0.85f;
                float body = (0.65f * Sn(p1) + 0.35f * Sn(p2)) * eB, wires = top * eW * Mathf.Min(1f, k / (float)att);
                o[k] = Soft((body + wires) * g) * Mathf.Min(1f, (m - k) / (float)tail);
                eP *= kP; eB *= kB; eW *= kW;
            }
            return m;
        }

        /// <summary>Hi-hat: high-passed noise with a little metallic band; closed is a tick, open rings until choked at <paramref name="open"/> s.</summary>
        static int Hat(float[] o, int rate, float vel, float open, System.Random r)
        {
            bool isOpen = open > 0f;
            int m = (int)((isOpen ? open + 0.03f : 0.08f) * rate), tail = (int)(0.02f * rate);
            var hp = new Svf().Set(6500f + 1500f * vel, 0.7f, rate);
            var bp = new Svf().Set(9500f, 2f, rate);
            float e = vel, ke = Mathf.Exp(-(isOpen ? 7f : 60f) / rate);
            for (int k = 0; k < m; k++)
            {
                float nz = N(r);
                hp.Run(nz);
                bp.Run(nz);
                o[k] = (hp.Hp * 0.8f + bp.Bp * 0.35f) * e * Mathf.Min(1f, (m - k) / (float)tail);
                e *= ke;
            }
            return m;
        }

        /// <summary>Shaker: band-passed noise with a soft swell (the beads hitting the shell).</summary>
        static int Shaker(float[] o, int rate, float vel, System.Random r)
        {
            int m = (int)(0.1f * rate), att = (int)(0.01f * rate);
            var bp = new Svf().Set(6000f, 1.4f, rate);
            float e = vel, ke = Mathf.Exp(-40f / rate);
            for (int k = 0; k < m; k++)
            {
                bp.Run(N(r));
                o[k] = bp.Bp * e * Mathf.Min(1f, k / (float)att);
                e *= ke;
            }
            return m;
        }

        /// <summary>Crash cymbal: bright noise with a long decay.</summary>
        static int Crash(float[] o, int rate, float vel, System.Random r)
        {
            int m = (int)(2.4f * rate), tail = (int)(0.05f * rate), att = System.Math.Max(1, rate / 1000);
            var hp = new Svf().Set(4500f, 0.7f, rate);
            var bp = new Svf().Set(7000f, 1.5f, rate);
            float e = vel, ke = Mathf.Exp(-1.7f / rate);
            for (int k = 0; k < m; k++)
            {
                float nz = N(r);
                hp.Run(nz);
                bp.Run(nz);
                o[k] = (hp.Hp * 0.7f + bp.Bp * 0.4f) * e * Mathf.Min(1f, k / (float)att) * Mathf.Min(1f, (m - k) / (float)tail);
                e *= ke;
            }
            return m;
        }

        /// <summary>
        /// Karplus–Strong string: a filtered noise burst (with a pick-position comb) circulating in a delay line
        /// tuned with an all-pass, losing highs on every pass. Rings for <paramref name="sustain"/> s (−60 dB),
        /// damped by the hand after <paramref name="off"/> s.
        /// </summary>
        static int Pluck(float[] o, int rate, float hz, float vel, float bright, float sustain, float off, System.Random r)
        {
            float period = rate / hz;
            int len = (int)(period - 0.6f);
            float d = period - 0.5f - len, c = (1f - d) / (1f + d);
            var buf = new float[len];
            float lp = 0f, a = 0.1f + 0.85f * bright, peak = 1e-6f;
            for (int k = 0; k < len; k++) { lp += (N(r) - lp) * a; buf[k] = lp; }
            int pos = System.Math.Max(1, len / 7);
            var ex = new float[len];
            float mean = 0f;
            for (int k = 0; k < len; k++) { ex[k] = buf[k] - buf[(k + len - pos) % len]; mean += ex[k]; }
            mean /= len;
            for (int k = 0; k < len; k++) { buf[k] = ex[k] - mean; peak = Mathf.Max(peak, Mathf.Abs(buf[k])); }
            for (int k = 0; k < len; k++) buf[k] *= vel / peak;
            int total = Mathf.Min(o.Length, (int)((Mathf.Min(off, sustain) + 0.2f) * rate)), offS = (int)(off * rate), idx = 0;
            float g = Mathf.Exp(-6.9f / (sustain * hz)), gOff = Mathf.Exp(-6.9f / (0.07f * hz)), prev = 0f, apIn = 0f, apOut = 0f;
            for (int k = 0; k < total; k++)
            {
                if (k > offS) g += (gOff - g) * 0.01f;
                float cur = buf[idx];
                float x = g * 0.5f * (cur + prev);
                prev = cur;
                float y = c * x + apIn - c * apOut;
                apIn = x;
                apOut = y;
                buf[idx] = y;
                if (++idx >= len) idx = 0;
                o[k] = cur * Mathf.Clamp01((total - k) / (0.01f * rate));
            }
            return total;
        }

        /// <summary>Bass: a dark Karplus–Strong string with its fundamental reinforced, a little saturated so it carries on small speakers.</summary>
        static int Bass(float[] o, int rate, float hz, float vel, float off, System.Random r)
        {
            int m = Pluck(o, rate, hz, vel, 0.22f, 2.5f, off, r), offS = (int)(off * rate), att = (int)(0.004f * rate), tail = (int)(0.01f * rate);
            float ph = 0f, dt = 1f / rate, e = 0.45f * vel, ke = Mathf.Exp(-2f * dt), kr = Mathf.Exp(-40f * dt);
            for (int k = 0; k < m; k++)
            {
                ph += hz * dt;
                if (ph >= 1f) ph -= 1f;
                float env = e * Mathf.Min(1f, k / (float)att) * Mathf.Min(1f, (m - k) / (float)tail);
                o[k] = Soft((o[k] + Sn(ph) * env) * 1.6f) / 1.6f;
                e *= k > offS ? ke * kr : ke;
            }
            return m;
        }

        /// <summary>
        /// Electric piano (FM, Rhodes-like): a 1:1 carrier/modulator pair whose brightness (index) falls quickly
        /// after the hammer, plus a short high tine ping; higher notes die faster; released after <paramref name="off"/> s.
        /// </summary>
        static int EPiano(float[] o, int rate, float hz, float vel, float off)
        {
            int total = Mathf.Min(o.Length, (int)((off + 0.35f) * rate)), offS = (int)(off * rate), att = (int)(0.002f * rate);
            float dt = 1f / rate, ph = 0f, tph = 0f;
            float amp = 0.4f + 0.6f * vel, kAmp = Mathf.Exp(-(0.9f + hz / 450f) * dt), kRel = Mathf.Exp(-14f * dt);
            float idx = 0.4f + 1.6f * vel, kIdx = Mathf.Exp(-5f * dt), tine = 0.08f * vel, kTine = Mathf.Exp(-40f * dt);
            for (int k = 0; k < total; k++)
            {
                ph += hz * dt;
                if (ph >= 1f) ph -= 1f;
                tph += hz * 14f * dt;
                if (tph >= 1f) tph -= 1f;
                float v = Sn(ph + 0.159f * (idx + 0.3f) * Sn(ph)) + tine * Sn(tph);
                o[k] = v * amp * (k < att ? k / (float)att : 1f);
                amp *= k > offS ? kAmp * kRel : kAmp;
                idx *= kIdx;
                tine *= kTine;
            }
            return total;
        }

        /// <summary>
        /// A whistled line: one continuous voice that slides between notes, scoops into each from just below, with
        /// a vibrato that sets in late and a little breath noise around the pitch.
        /// </summary>
        static void Whistle(Mix mx, System.Random r, float[] at, float[] len, float[] hz, float gain, float pan, float send)
        {
            int rate = mx.Rate, s0 = (int)((at[0] - 0.1f) * rate), s1 = (int)((at[at.Length - 1] + len[len.Length - 1] + 0.4f) * rate);
            float gl = PanL(pan) * gain, gr = PanR(pan) * gain, dt = 1f / rate;
            float f = hz[0], a = 0f, ph = 0f, vph = 0f;
            var breath = new Svf();
            int note = 0;
            for (int s = s0, c = 0; s < s1; s++, c++)
            {
                float t = s * dt;
                while (note < at.Length - 1 && t >= at[note + 1]) note++;
                float since = t - at[note];
                bool gate = since >= 0f && since < len[note] - Mathf.Max(0.035f, 0.12f * len[note]);
                float target = hz[note] * (1f - 0.025f * Mathf.Exp(-Mathf.Max(0f, since) * 30f));
                if (a < 0.02f) f = target; else f += (target - f) * 0.004f;
                a += ((gate ? 1f : 0f) - a) * (gate ? 0.0025f : 0.0012f) * 44100f / rate;
                vph += 5.4f * dt;
                if (vph >= 1f) vph -= 1f;
                float vib = 0.011f * Mathf.Clamp01((since - 0.12f) / 0.25f) * Sn(vph);
                ph += f * (1f + vib) * dt;
                if (ph >= 1f) ph -= 1f;
                if ((c & 31) == 0) breath.Set(f, 5f, rate);
                float nz = N(r);
                breath.Run(nz);
                float v = (Sn(ph) + 0.04f * Sn(2f * ph)) * 0.9f + breath.Bp * 0.18f + nz * 0.01f;
                mx.Put(s, v * a, gl, gr, send * gain);
            }
        }

        /// <summary>Stereo chorus for the electric piano: two slowly modulated short delays, opposite phase per side.</summary>
        static void Chorus(float[] mono, float[] st, int rate, float pan)
        {
            int n = mono.Length;
            float gl = PanL(pan), gr = PanR(pan);
            float lfo = (float)System.Math.Max(1.0, System.Math.Round(0.45 * n / rate)) * rate / n, ph = 0f;   // whole cycles per loop
            for (int i = 0; i < n; i++)
            {
                ph += lfo / rate;
                if (ph >= 1f) ph -= 1f;
                float wl = Tap(mono, i, (0.011f + 0.003f * Sn(ph)) * rate), wr = Tap(mono, i, (0.011f + 0.003f * Sn(ph + 0.5f)) * rate);
                st[2 * i] += (mono[i] * 0.8f + wl * 0.45f) * gl;
                st[2 * i + 1] += (mono[i] * 0.8f + wr * 0.45f) * gr;
            }
        }

        /// <summary>The loop's sample <paramref name="delay"/> (fractional, positive) samples before <paramref name="i"/>, wrapping.</summary>
        static float Tap(float[] d, int i, float delay)
        {
            int n = d.Length, di = (int)delay, a = i - di - 1;
            float f = 1f - (delay - di);
            if (a < 0) a += n;
            int b = a + 1 == n ? 0 : a + 1;
            return d[a] + (d[b] - d[a]) * f;
        }

        /// <summary>
        /// Stereo room reverb (Freeverb-style: four damped combs and two all-passes per side, the right side's
        /// delays a little longer). Run around the loop: warmed up on its last seconds so the tail wraps.
        /// </summary>
        static void StereoVerb(float[] send, float[] st, int rate, float feedback, float damp, float wet)
        {
            int n = send.Length;
            float sc = rate / 44100f;
            int[] cl = { 1116, 1188, 1277, 1356 }, al = { 556, 341 };
            var comb = new float[8][];
            var store = new float[8];
            var ci = new int[8];
            for (int c = 0; c < 8; c++) comb[c] = new float[(int)((cl[c % 4] + (c >= 4 ? 23 : 0)) * sc)];
            var ap = new float[4][];
            var ai = new int[4];
            for (int c = 0; c < 4; c++) ap[c] = new float[(int)((al[c % 2] + (c >= 2 ? 23 : 0)) * sc)];
            int pre = (int)(0.015f * rate), warm = Mathf.Min(n, 3 * rate);
            for (int j = -warm; j < n; j++)
            {
                int i = j < 0 ? j + n : j, si = i - pre;
                if (si < 0) si += n;
                float x = send[si] * 0.3f, l = 0f, rr = 0f;
                for (int c = 0; c < 8; c++)
                {
                    var bf = comb[c];
                    float y = bf[ci[c]];
                    store[c] = y * (1f - damp) + store[c] * damp;
                    bf[ci[c]] = x + store[c] * feedback;
                    if (++ci[c] >= bf.Length) ci[c] = 0;
                    if (c < 4) l += y; else rr += y;
                }
                for (int c = 0; c < 4; c++)
                {
                    var bf = ap[c];
                    float inp = c < 2 ? l : rr, bo = bf[ai[c]];
                    bf[ai[c]] = inp + bo * 0.5f;
                    if (++ai[c] >= bf.Length) ai[c] = 0;
                    if (c < 2) l = bo - inp; else rr = bo - inp;
                }
                if (j < 0) continue;
                st[2 * i] += l * wet;
                st[2 * i + 1] += rr * wet;
            }
        }

        /// <summary>Mix bus: a gentle linked compressor (2.5:1 above −12 dB, 6 ms / 200 ms), then soft saturation.</summary>
        static void Master(float[] st, int rate)
        {
            int n = st.Length / 2, warm = rate / 2;
            float peak = 1e-6f;
            for (int i = 0; i < st.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(st[i]));
            float pre = 1f / peak;   // the raw mix's peak to 0 dB, then compress and saturate
            float att = 1f - Mathf.Exp(-1f / (0.006f * rate)), rel = 1f - Mathf.Exp(-1f / (0.2f * rate)), env = 0f, thr = 0.25f, g = 1f;
            for (int j = -warm; j < n; j++)
            {
                int i = j < 0 ? j + n : j;
                float l = st[2 * i] * pre, r = st[2 * i + 1] * pre, lvl = Mathf.Max(Mathf.Abs(l), Mathf.Abs(r));
                env += (lvl - env) * (lvl > env ? att : rel);
                if ((j & 7) == 0) g = env > thr ? (float)System.Math.Pow(thr / env, 0.6) : 1f;
                if (j < 0) continue;
                st[2 * i] = Soft(l * g * 1.3f);
                st[2 * i + 1] = Soft(r * g * 1.3f);
            }
        }
    }
}
