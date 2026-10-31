using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// Fireworks, heard from the street: a shell bursting high up is a deep boom that arrives late (sound takes a
    /// fraction of a second to come down) and rolls round the buildings; crackling stars sizzle and pop as they
    /// fall; a mortar leaves its tube with a hollow thump; a Roman candle coughs out each ball; a fountain hisses
    /// and spits. Made the first time they are needed.
    /// </summary>
    public sealed partial class Sfx
    {
        AudioClip shellBoom, shellBoomBig, shellCrackle, mortarThump, candlePop, fountainLoop;

        /// <summary>A shell bursting at <paramref name="pos"/>; <paramref name="size"/> 1 a rocket, 2 a mortar.</summary>
        public void ShellBurst(Vector3 pos, float size, bool crackle)
        {
            if (shellBoom == null) shellBoom = ShellBoomClip(false);
            if (shellBoomBig == null && size > 1.4f) shellBoomBig = ShellBoomClip(true);
            float delay = Delay(pos);
            Play(size > 1.4f ? shellBoomBig : shellBoom, pos, Mathf.Clamp01(0.75f + 0.15f * size) * Volume, Random.Range(0.9f, 1.1f) / Mathf.Sqrt(size), 25f * size, delay);
            if (crackle) Crackle(pos, delay + Random.Range(0.6f, 1f));
        }

        /// <summary>Crackling stars popping as they fall, from <paramref name="delay"/> s on.</summary>
        public void Crackle(Vector3 pos, float delay)
        {
            if (shellCrackle == null) shellCrackle = CrackleClip();
            Play(shellCrackle, pos, 0.8f * Volume, Random.Range(0.9f, 1.15f), 20f, delay);
        }

        /// <summary>A mortar or cake tube firing.</summary>
        public void Launch(Vector3 pos, float size)
        {
            if (mortarThump == null) mortarThump = ThumpClip();
            Play(mortarThump, pos, Mathf.Clamp01(0.55f + 0.25f * size) * Volume, Random.Range(0.95f, 1.1f) / Mathf.Sqrt(size), 8f);
        }

        /// <summary>A Roman candle coughing out a ball.</summary>
        public void CandlePop(Vector3 pos)
        {
            if (candlePop == null) candlePop = CoughClip();
            Play(candlePop, pos, 0.7f * Volume, Random.Range(0.9f, 1.15f), 6f);
        }

        /// <summary>A fountain's hiss and spit, looping on <paramref name="at"/> until it is destroyed.</summary>
        public AudioSource FountainLoop(Transform at)
        {
            if (fountainLoop == null) fountainLoop = FountainClip(new System.Random(51));
            var s = at.gameObject.AddComponent<AudioSource>();
            s.clip = fountainLoop;
            s.loop = true;
            s.volume = 0.7f * Volume;
            s.spatialBlend = 1f;
            s.minDistance = 3f;
            s.maxDistance = 60f;
            s.Play();
            return s;
        }

        /// <summary>A police siren's wail (made the first time a police car comes).</summary>
        public AudioClip SirenClip { get { if (siren == null) siren = Siren(new System.Random(53)); return siren; } }
        AudioClip siren;

        /// <summary>The time the sound takes to reach the listener (343 m/s).</summary>
        static float Delay(Vector3 pos)
        {
            var cam = Camera.main;
            return cam != null ? Vector3.Distance(cam.transform.position, pos) / 343f : 0f;
        }

        // ---------- synthesis ----------

        internal static AudioClip ShellBoomClip(bool big)
        {
            return big ? Reverb(ShellBoom(new System.Random(43), 1.8f), 0.8f, 3f, new System.Random(44), 3.5f)
                : Reverb(ShellBoom(new System.Random(41), 1f), 0.7f, 2.2f, new System.Random(42), 3f);
        }

        internal static AudioClip CrackleClip() { return Reverb(Crackling(new System.Random(45)), 0.45f, 1f, new System.Random(46), 2.5f); }
        internal static AudioClip ThumpClip() { return Reverb(Thump(new System.Random(47)), 0.4f, 1.2f, new System.Random(48), 2f); }
        internal static AudioClip CoughClip() { return Reverb(Cough(new System.Random(49)), 0.3f, 0.6f, new System.Random(50), 1.5f); }

        /// <summary>
        /// A shell bursting high up, heard from below: the break charge's sharp report (rounded by distance), the
        /// deep boom of the blast wave, and the low rumble as it rolls off the buildings.
        /// </summary>
        static float[] ShellBoom(System.Random r, float size)
        {
            float seconds = 1.6f + 0.6f * size;
            int n = (int)(seconds * Rate);
            var d = new float[n];
            // The report: a rounded N-wave.
            int half = (int)(0.0012f * size * Rate);
            for (int k = 0; k <= 2 * half; k++) d[k] += 0.9f * (1f - k / (float)half);
            // The boom: a long, low Friedlander pulse.
            float T = 0.018f * size;
            var lp = Bq.Lp(220f, 0.7f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                if (t > 14f * T) break;
                d[i] += lp.Run(1.3f * (1f - t / T) * Mathf.Exp(-1.2f * t / T));
            }
            // Its body: low noise, swelling and rolling away.
            var body = new Svf().Set(70f * Mathf.Sqrt(size), 0.9f, Rate);
            var mid = Bq.Bp(420f, 0.8f);
            var roll = new Drift(r, 4f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float x = N(r);
                body.Run(x);
                float env = Mathf.Min(1f, t / 0.012f) * Mathf.Exp(-t / (0.35f * size)) * (1f + 0.35f * roll.Next());
                d[i] += body.Lp * 4.5f * env + mid.Run(x) * 0.35f * env * Mathf.Exp(-t / 0.08f);
            }
            Bq.Lp(1800f, 0.6f).Apply(d);
            Bq.Hp(28f).Apply(d);
            for (int i = 0; i < n; i++) d[i] = SoftClip(d[i] * 1.8f) * 0.85f;
            return d;
        }

        /// <summary>
        /// Crackling stars: hundreds of tiny sharp pops, each a click of hot gas, scattered over two seconds as the
        /// stars burn out, thinning towards the end, a hiss of sparks underneath.
        /// </summary>
        static float[] Crackling(System.Random r)
        {
            float seconds = 2.4f;
            int n = (int)(seconds * Rate);
            var d = new float[n];
            int pops = 420;
            for (int k = 0; k < pops; k++)
            {
                float at = 2.1f * Mathf.Pow(U(r), 1.3f);
                float a = U(r, 0.1f, 1f) * (1f - at / 2.4f);
                int s0 = (int)(at * Rate);
                int len = (int)(U(r, 0.0006f, 0.002f) * Rate);
                float tone = U(r, 2500f, 7000f);
                for (int i = 0; i < len && s0 + i < n; i++)
                {
                    float e = 1f - i / (float)len;
                    d[s0 + i] += (N(r) * 0.7f + Mathf.Sin(2f * Mathf.PI * tone * i / Rate) * 0.3f) * a * e * e;
                }
            }
            var hiss = Bq.Hp(3000f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                d[i] += hiss.Run(N(r)) * 0.05f * Mathf.Min(1f, t / 0.1f) * Mathf.Exp(-t / 0.9f);
            }
            Bq.Lp(9000f, 0.7f).Apply(d);
            return d;
        }

        /// <summary>A shell leaving its mortar tube: a hollow, pitched thump (the tube's air column) and a rush of gas.</summary>
        static float[] Thump(System.Random r)
        {
            int n = (int)(0.6f * Rate);
            var d = new float[n];
            Mode(d, 0, 95f, 18f, 0.9f, 0.002f);
            Mode(d, 0, 190f, 30f, 0.35f, 0.002f);
            Mode(d, 0, 310f, 45f, 0.15f, 0.001f);
            Burst(d, 0, 0.25f, 0.5f, 0.003f, 22f, Bq.Lp(900f, 0.7f), r);
            Burst(d, 0, 0.08f, 0.25f, 0f, 60f, Bq.Bp(2400f, 0.8f), r);
            for (int i = 0; i < n; i++) d[i] = SoftClip(d[i] * 1.5f) * 0.7f;
            return d;
        }

        /// <summary>A Roman candle: a soft cough of powder and the ball's whoosh away.</summary>
        static float[] Cough(System.Random r)
        {
            int n = (int)(0.45f * Rate);
            var d = new float[n];
            Mode(d, 0, 140f, 35f, 0.6f, 0.002f);
            Burst(d, 0, 0.06f, 0.6f, 0.001f, 60f, Bq.Bp(900f, 0.9f), r);
            var whoosh = Bq.Bp(1800f, 1.2f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t / 0.03f) * Mathf.Exp(-t / 0.12f);
                d[i] += whoosh.Run(N(r)) * 0.4f * env;
            }
            return d;
        }

        /// <summary>
        /// A police siren's wail: a buzzy horn (a sawtooth's first harmonics through the horn's resonances, driven
        /// into the amplifier's clipping) rising and falling over four seconds, with a little wobble.
        /// </summary>
        internal static AudioClip Siren(System.Random r)
        {
            var horn1 = Bq.Peak(1800f, 1.4f, 9f);
            var horn2 = Bq.Peak(3200f, 2f, 6f);
            var lp = Bq.Lp(5500f, 0.7f);
            var hp = Bq.Hp(400f);
            var wob = new Drift(r, 6f);
            double phase = 0.0;
            return Loop(Rate, 4f, (t, i) =>
            {
                float u = (t % 4f) / 4f;
                float sweep = u < 0.5f ? Mathf.SmoothStep(0f, 1f, u * 2f) : Mathf.SmoothStep(1f, 0f, (u - 0.5f) * 2f);
                float hz = 650f + 700f * sweep + 6f * wob.Next();
                phase += hz / Rate;
                float p = (float)(phase - System.Math.Floor(phase));
                float x = 0f;
                for (int k = 1; k <= 8; k++) x += Mathf.Sin(2f * Mathf.PI * k * p) / k;
                x = SoftClip(hp.Run(horn2.Run(horn1.Run(x))) * 1.6f);
                return lp.Run(x) * 0.5f;
            }, 0.8f);
        }

        /// <summary>A fountain: a roaring hiss of sparks, fluttering, with spits and crackles all through it.</summary>
        internal static AudioClip FountainClip(System.Random r)
        {
            var hp = Bq.Hp(1500f);
            var body = Bq.Bp(700f, 0.7f);
            var flutter = new Drift(r, 9f);
            var slow = new Drift(r, 1.5f);
            float spit = 0f;
            return Loop(Rate, 2f, (t, i) =>
            {
                float x = N(r);
                if (r.NextDouble() < 0.0015) spit = U(r, 0.4f, 1f);
                spit *= 0.985f;
                float a = 1f + 0.25f * flutter.Next() + 0.15f * slow.Next();
                return hp.Run(x) * 0.55f * a + body.Run(x) * 0.25f * a + x * spit * 0.5f;
            }, 0.5f);
        }
    }
}
