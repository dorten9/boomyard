using System;

namespace BoomYard.Core
{
    /// <summary>
    /// The wind (spec §04): a direction and a speed that change every 20 seconds, from a seed so the daily yard
    /// has the same wind for everyone. Direction is where the wind blows TO, in degrees clockwise from +Z (north).
    /// </summary>
    public sealed class WindModel
    {
        public const float ChangeEvery = 20f;
        public const float MinSpeed = 0.5f, MaxSpeed = 4.5f;

        readonly Random rng;
        float nextChange;

        public float Direction { get; private set; }
        public float Speed { get; private set; }

        public WindModel(int seed)
        {
            rng = new Random(seed);
            Roll();
            nextChange = ChangeEvery;
        }

        /// <summary>Advances to <paramref name="elapsed"/> seconds into the round. Returns true when the wind changed.</summary>
        public bool Update(float elapsed)
        {
            if (elapsed < nextChange) return false;
            while (elapsed >= nextChange) { Roll(); nextChange += ChangeEvery; }
            return true;
        }

        void Roll()
        {
            Direction = (float)(rng.NextDouble() * 360.0);
            Speed = MinSpeed + (float)rng.NextDouble() * (MaxSpeed - MinSpeed);
        }

        /// <summary>
        /// How exposed a flame is: 1 facing into the wind, 0 with the back to it (within 60°), scaled by the wind speed.
        /// <paramref name="facing"/> is the player's yaw in the same convention as <see cref="Direction"/>.
        /// </summary>
        public float Exposure(float facing)
        {
            // The wind blows toward Direction; with the back to it the player faces the same way.
            float diff = Math.Abs(DeltaAngle(facing, Direction));
            if (diff <= 60f) return 0f;
            float t = (diff - 60f) / 120f;
            return t * Math.Min(1f, Speed / 2.5f);
        }

        public static float DeltaAngle(float a, float b)
        {
            float d = (b - a) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }
    }
}
