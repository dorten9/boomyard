namespace BoomYard.Core
{
    /// <summary>
    /// Chain multiplier (spec §07): ×1 for what the player destroys directly, +0.5 for every link caused by an object
    /// (a fuel tank, a launched bin), up to ×6. The chain breaks after 3 seconds without anything new destroyed.
    /// Time is passed in, so the tracker is pure and testable.
    /// </summary>
    public sealed class ChainTracker
    {
        public const float Window = 3f;
        public const float Step = 0.5f;
        public const float MaxMult = 6f;

        float lastDestroyTime = float.NegativeInfinity;

        /// <summary>Links caused by objects in the current chain.</summary>
        public int Links { get; private set; }
        /// <summary>Longest chain of the round, counted in links (what the results screen calls "6 links").</summary>
        public int LongestChain { get; private set; }
        /// <summary>Time the longest chain started and ended, for the replay of the big moment.</summary>
        public float LongestStart { get; private set; }
        public float LongestEnd { get; private set; }
        float currentStart;

        public float Multiplier { get { return MultFor(Links); } }
        public bool Active { get { return Links > 0; } }

        public static float MultFor(int links)
        {
            float m = 1f + Step * links;
            return m > MaxMult ? MaxMult : m;
        }

        /// <summary>Breaks the chain once its window has passed. Call every frame.</summary>
        public void Tick(float now)
        {
            if (Links > 0 && now - lastDestroyTime > Window) Links = 0;
        }

        /// <summary>
        /// Records a destroyed object and returns the multiplier its damage earns.
        /// <paramref name="causedByObject"/>: the blast/impact came from another object, not from the player's explosive.
        /// </summary>
        public float OnDestroyed(float now, bool causedByObject)
        {
            Tick(now);
            lastDestroyTime = now;
            if (!causedByObject) return 1f;
            if (Links == 0) currentStart = now;
            Links++;
            if (Links > LongestChain)
            {
                LongestChain = Links;
                LongestStart = currentStart;
            }
            if (Links == LongestChain) LongestEnd = now;
            return Multiplier;
        }

        /// <summary>Multiplier for partial damage (no destruction): a secondary blast pays the chain it belongs to.</summary>
        public float MultiplierFor(bool causedByObject) { return causedByObject ? Multiplier : 1f; }

        public void Reset()
        {
            Links = 0;
            LongestChain = 0;
            LongestStart = LongestEnd = 0f;
            lastDestroyTime = float.NegativeInfinity;
        }
    }
}
