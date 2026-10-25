using System;
using System.Collections.Generic;

namespace BoomYard.Core
{
    /// <summary>
    /// Damage in dollars (spec §07): Σ(Value × MaterialMult × PlacementMult × ChainMult) + Bonuses.
    /// Pure C#, no Unity, so Tools/SimTest can check it.
    /// </summary>
    public static class ScoreCalculator
    {
        public const int CloseCallBonus = 5000;     // was inside the radius and stayed up
        public const int MultiBonus = 8000;         // 5+ objects destroyed by one explosive
        public const int MultiCount = 5;
        public const int LastSecondBonus = 3000;    // released with almost no fuse left
        public const float LastSecondFuse = 0.3f;
        public const float CleanSweepMult = 1.5f;   // the whole arena destroyed
        public const int HandBurnPenalty = -2000;   // went off in the hand

        public static float MaterialMult(Material m)
        {
            switch (m)
            {
                case Material.Glass: return 0.9f;
                case Material.Metal:
                case Material.Carbon:
                case Material.Aluminium: return 1.3f;
                case Material.Concrete: return 1.6f;
                default: return 1f;   // wood, plastic, cardboard, acrylic
            }
        }

        public static float PlacementMult(Placement p)
        {
            switch (p)
            {
                case Placement.Placed: return 1.3f;
                case Placement.Contained: return 1.8f;
                case Placement.Airburst: return 1.5f;
                default: return 1f;
            }
        }

        /// <summary>Dollars for destroying <paramref name="fraction"/> (0..1) of an object worth <paramref name="value"/>.</summary>
        public static long Damage(int value, float fraction, Material material, Placement placement, float chainMult)
        {
            if (fraction <= 0f) return 0;
            if (fraction > 1f) fraction = 1f;
            return (long)Math.Round(value * fraction * MaterialMult(material) * PlacementMult(placement) * chainMult);
        }

        /// <summary>Stars for an arena: ★ at 25%, ★★ at 55%, ★★★ at the spec's threshold.</summary>
        public static int Stars(ArenaDef arena, long damage)
        {
            if (damage >= arena.ThreeStars) return 3;
            if (damage >= arena.ThreeStars * 55 / 100) return 2;
            if (damage >= arena.ThreeStars * 25 / 100) return 1;
            return 0;
        }

        /// <summary>Soft currency earned from a round: 2% of the damage (spec §14).</summary>
        public static long CoinsFromDamage(long damage) { return damage <= 0 ? 0 : damage * 2 / 100; }
    }

    public enum BonusKind { CloseCall, Multi, LastSecond, HandBurn }

    /// <summary>The running total of one round, kept as the breakdown the results screen shows.</summary>
    public sealed class RoundScore
    {
        public long Base;            // value × material
        public long PlacementExtra;  // what placement added on top
        public long ChainExtra;      // what chain multipliers added on top
        public long Bonuses;
        public bool CleanSweep;
        public readonly List<BonusKind> BonusLog = new List<BonusKind>();
        public int ObjectsDestroyed;
        public int ContainedHits;

        public long Total
        {
            get
            {
                long sum = Base + PlacementExtra + ChainExtra + Bonuses;
                return CleanSweep ? (long)Math.Round(sum * ScoreCalculator.CleanSweepMult) : sum;
            }
        }

        /// <summary>Credits damage to an object and returns the dollars it added.</summary>
        public long AddDamage(int value, float fraction, Material material, Placement placement, float chainMult)
        {
            long plain = ScoreCalculator.Damage(value, fraction, material, Placement.Thrown, 1f);
            long placed = ScoreCalculator.Damage(value, fraction, material, placement, 1f);
            long full = ScoreCalculator.Damage(value, fraction, material, placement, chainMult);
            Base += plain;
            PlacementExtra += placed - plain;
            ChainExtra += full - placed;
            if (placement == Placement.Contained) ContainedHits++;
            return full;
        }

        public long AddBonus(BonusKind kind)
        {
            int v;
            switch (kind)
            {
                case BonusKind.CloseCall: v = ScoreCalculator.CloseCallBonus; break;
                case BonusKind.Multi: v = ScoreCalculator.MultiBonus; break;
                case BonusKind.LastSecond: v = ScoreCalculator.LastSecondBonus; break;
                default: v = ScoreCalculator.HandBurnPenalty; break;
            }
            Bonuses += v;
            BonusLog.Add(kind);
            return v;
        }
    }
}
