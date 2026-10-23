using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// Placeholder sound made from code until the recorded libraries arrive (spec §15): lighter click and flame,
    /// fuse hiss, layered explosions with an echo, glass, ear ringing and a night ambience. No music during a round.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        public static Sfx I { get; private set; }

        const int Rate = 44100;
        AudioClip click, flame, hiss, boomSmall, boomBig, glass, ring, crickets, thud;
        AudioSource ui, flameLoop, hissLoop, ringSource, ambience;
        public float Volume = 1f;
        Coroutine muffle;

        void Awake()
        {
            I = this;
            var rng = new System.Random(5);
            click = Make("click", 0.06f, (t, r) => (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-t * 90f) + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 3100f) * Mathf.Exp(-t * 60f), rng);
            flame = Loop("flame", 1.5f, Brown(0.97f), 0.5f, rng);
            hiss = Loop("hiss", 1f, (t, r) => (float)(r.NextDouble() * 2 - 1) * (0.6f + 0.4f * (r.NextDouble() < 0.02 ? 1f : 0f)), 0.35f, rng);
            boomSmall = Boom("boomSmall", 1.2f, 90f, 18f, rng);
            boomBig = Boom("boomBig", 3f, 55f, 5f, rng);
            glass = Make("glass", 0.9f, Glass, rng);
            ring = Make("ring", 3f, (t, r) => 0.25f * Mathf.Sin(t * 2f * Mathf.PI * 3800f) * Mathf.Clamp01(t * 8f) * Mathf.Clamp01((3f - t) / 1.5f), rng);
            thud = Make("thud", 0.4f, (t, r) => Mathf.Sin(t * 2f * Mathf.PI * (70f - 40f * t)) * Mathf.Exp(-t * 9f) + 0.2f * (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-t * 30f), rng);
            crickets = Loop("crickets", 4f, Crickets, 0.18f, rng);

            ui = gameObject.AddComponent<AudioSource>();
            flameLoop = Source(flame, 0.35f);
            hissLoop = Source(hiss, 0.3f);
            ringSource = Source(ring, 0.6f);
            ringSource.loop = false;
            ambience = Source(crickets, 0.5f);
            ambience.Play();
        }

        AudioSource Source(AudioClip clip, float volume)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.volume = volume;
            s.playOnAwake = false;
            return s;
        }

        public void Click() { ui.PlayOneShot(click, 0.8f * Volume); }
        public void Thud() { ui.PlayOneShot(thud, 0.7f * Volume); }

        public void Flame(bool on) { Loop(flameLoop, on); }

        /// <summary>The fuse hiss rises in pitch as it gets shorter.</summary>
        public void Hiss(bool on, float left01 = 1f)
        {
            Loop(hissLoop, on);
            hissLoop.pitch = Mathf.Lerp(1.35f, 0.9f, left01);
        }

        static void Loop(AudioSource s, bool on)
        {
            if (on && !s.isPlaying) s.Play();
            else if (!on && s.isPlaying) s.Stop();
        }

        /// <summary>Explosion at a point: louder and lower the bigger it is, with a delayed echo off the yard.</summary>
        public void Boom(Vector3 pos, float radius, float echo = 1f)
        {
            bool big = radius >= 2f;
            float vol = Mathf.Clamp01(0.35f + radius * 0.15f) * Volume;
            Play(big ? boomBig : boomSmall, pos, vol, Random.Range(0.9f, 1.1f) * (big ? 1f : 1.2f - radius * 0.1f));
            if (echo > 0f) StartCoroutine(Echo(big ? boomBig : boomSmall, pos, vol * 0.25f * echo, 0.18f + 0.05f * radius));
        }

        System.Collections.IEnumerator Echo(AudioClip clip, Vector3 pos, float vol, float delay)
        {
            yield return new WaitForSeconds(delay);
            Play(clip, pos, vol, 0.85f);
        }

        public void Glass(Vector3 pos) { Play(glass, pos, 0.6f * Volume, Random.Range(0.85f, 1.2f)); }

        /// <summary>Ear ringing after a close blast; the world goes quiet for 3 seconds.</summary>
        public void Ring()
        {
            ringSource.volume = 0.5f * Volume;
            ringSource.Play();
            if (muffle != null) StopCoroutine(muffle);
            muffle = StartCoroutine(Muffle());
        }

        System.Collections.IEnumerator Muffle()
        {
            for (float t = 0f; t < 3f; t += Time.unscaledDeltaTime)
            {
                AudioListener.volume = Mathf.Lerp(0.25f, 1f, t / 3f);
                yield return null;
            }
            AudioListener.volume = 1f;
        }

        void Play(AudioClip clip, Vector3 pos, float vol, float pitch)
        {
            var go = new GameObject("Sound");
            go.transform.position = pos;
            var s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.volume = vol;
            s.pitch = pitch;
            s.spatialBlend = 0.85f;
            s.minDistance = 4f;
            s.maxDistance = 120f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.dopplerLevel = 0f;
            s.Play();
            Destroy(go, clip.length / Mathf.Max(0.5f, pitch) + 0.2f);
        }

        // ---- synthesis ----

        delegate float Wave(float t, System.Random r);

        static AudioClip Make(string name, float seconds, Wave wave, System.Random rng)
        {
            int n = (int)(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate, rng), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>A loop with its ends cross-faded so it repeats without a click.</summary>
        static AudioClip Loop(string name, float seconds, Wave wave, float gain, System.Random rng)
        {
            int n = (int)(seconds * Rate), fade = Rate / 10;
            var raw = new float[n + fade];
            for (int i = 0; i < raw.Length; i++) raw[i] = wave(i / (float)Rate, rng) * gain;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float v = raw[i];
                if (i < fade) { float k = i / (float)fade; v = v * k + raw[n + i] * (1f - k); }
                data[i] = Mathf.Clamp(v, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static Wave Brown(float leak)
        {
            float last = 0f;
            return (t, r) => { last = last * leak + (float)(r.NextDouble() * 2 - 1) * 0.12f; return last * 3f; };
        }

        /// <summary>Layered blast: a sub thump that drops in pitch, a noisy crack, and a rumbling tail.</summary>
        static AudioClip Boom(string name, float seconds, float subHz, float decay, System.Random rng)
        {
            float lp = 0f, lp2 = 0f;
            return Make(name, seconds, (t, r) =>
            {
                float noise = (float)(r.NextDouble() * 2 - 1);
                lp += (noise - lp) * 0.08f;
                lp2 += (lp - lp2) * 0.05f;
                float crack = noise * Mathf.Exp(-t * 40f);
                float sub = Mathf.Sin(2f * Mathf.PI * subHz * t * (1f - 0.25f * t / seconds)) * Mathf.Exp(-t * decay * 0.5f);
                float rumble = lp2 * 9f * Mathf.Exp(-t * decay * 0.25f);
                return 0.9f * crack + 0.8f * sub + rumble;
            }, rng);
        }

        static float Glass(float t, System.Random r)
        {
            float v = 0f;
            for (int k = 1; k <= 5; k++)
            {
                float start = k * 0.07f * (float)(0.5 + r.NextDouble() * 0.01);
                if (t < start) continue;
                float u = t - start;
                v += Mathf.Sin(2f * Mathf.PI * (2400f + 900f * k) * u) * Mathf.Exp(-u * (25f + 5f * k)) * 0.3f;
            }
            return v + (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-t * 12f) * 0.5f;
        }

        static float Crickets(float t, System.Random r)
        {
            float chirp = Mathf.Sin(2f * Mathf.PI * 4600f * t) * Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 30f * t));
            float gate = Mathf.Repeat(t, 0.9f) < 0.25f ? 1f : 0f;
            float road = Mathf.PerlinNoise(t * 0.5f, 3f) * (float)(r.NextDouble() * 2 - 1) * 0.15f;
            return chirp * gate * 0.25f + road;
        }
    }
}
