using System;

namespace BoomYard.Core
{
    /// <summary>Where the release falls on the fuse bar (spec §04): red lies and waits, orange lands and goes, green bursts in the air.</summary>
    public enum FuseZone { Early, Land, Air, Hand }

    public static class Fuse
    {
        /// <summary>Seconds after landing that still count as "lands and goes off" (orange).</summary>
        public const float LandWindow = 1f;
        /// <summary>Below this the explosive goes off in the hand.</summary>
        public const float HandMargin = 0.12f;

        public static FuseZone Classify(float remaining, float flightTime)
        {
            if (remaining <= HandMargin) return FuseZone.Hand;
            if (remaining < flightTime) return FuseZone.Air;
            if (remaining <= flightTime + LandWindow) return FuseZone.Land;
            return FuseZone.Early;
        }

        /// <summary>Firecracker strings get longer fuses with every unit (1.5 + 0.1 per unit).</summary>
        public static float TotalBurn(ExplosiveDef def) { return def.Fuse + def.UnitInterval * (def.Units - 1); }
    }

    public enum FlameState { Off, Lit, BlownOut }

    /// <summary>
    /// The lighter flame and catching a fuse (spec §04). Holding the lighter button sparks the lighter; outdoors the wind
    /// can blow a basic lighter out (30% per attempt) unless the player turns their back to it. Running puts it out.
    /// Once lit, the flame touching the fuse for the lighter's catch time lights the fuse.
    /// </summary>
    public sealed class Ignition
    {
        public LighterDef Lighter;
        public FlameState Flame { get; private set; }
        public float CatchProgress { get; private set; }   // 0..1 of the catch time
        public bool FuseCaught { get; private set; }
        public int Attempts { get; private set; }

        readonly Random rng;

        public Ignition(LighterDef lighter, int seed)
        {
            Lighter = lighter;
            rng = new Random(seed);
        }

        /// <summary>
        /// Strikes the lighter. Returns the new flame state.
        /// <paramref name="windFactor"/>: 0 when the wind is at the player's back or indoors, 1 when facing it.
        /// </summary>
        public FlameState Strike(float windFactor, bool running)
        {
            Attempts++;
            if (running) { Flame = FlameState.BlownOut; return Flame; }
            if (Lighter.BreakChance > 0f && rng.NextDouble() < Lighter.BreakChance) { Flame = FlameState.BlownOut; return Flame; }
            double chance = Lighter.WindBlowout * Clamp01(windFactor);
            Flame = rng.NextDouble() < chance ? FlameState.BlownOut : FlameState.Lit;
            return Flame;
        }

        /// <summary>Holds the flame to the fuse for <paramref name="dt"/> seconds. Returns true the frame the fuse catches.</summary>
        public bool Hold(float dt, bool running)
        {
            if (running && Flame == FlameState.Lit) Flame = FlameState.BlownOut;
            if (Flame != FlameState.Lit || FuseCaught) return false;
            if (Lighter.CatchTime <= 0f) CatchProgress = 1f;
            else CatchProgress = Clamp01(CatchProgress + dt / Lighter.CatchTime);
            if (CatchProgress >= 1f) { FuseCaught = true; return true; }
            return false;
        }

        /// <summary>Lets go of the lighter button: the flame goes out, an uncaught fuse starts over.</summary>
        public void Release()
        {
            Flame = FlameState.Off;
            if (!FuseCaught) CatchProgress = 0f;
        }

        /// <summary>Ready for the next item in the hand.</summary>
        public void NextFuse()
        {
            FuseCaught = false;
            CatchProgress = 0f;
        }

        static float Clamp01(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }
    }
}
