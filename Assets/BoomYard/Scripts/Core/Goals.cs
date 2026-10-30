using System.Collections.Generic;

namespace BoomYard.Core
{
    /// <summary>A level's bonus goal: something extra to try for, on top of destroying every target.</summary>
    public enum GoalKind { None, Chain, HalfClock, Contained, AirTime, HoleInOne }

    /// <summary>
    /// Bonus goals. Every level but the tutorial has one, chosen to suit its props (a goal about blowing
    /// something up from inside only where there is something to put a charge in). Met on a clear, it pays
    /// <see cref="Reward"/> coins the first time.
    /// </summary>
    public static class Goals
    {
        public const int ChainLinks = 3;

        public static GoalKind For(LevelDef l)
        {
            if (l == null || l.Tutorial || l.Show) return GoalKind.None;
            bool contain = false, small = false, launch = false;
            foreach (var p in l.Props)
            {
                var o = Catalog.Object(p.Id);
                if (o == null) continue;
                if ((o.Reaction & Reaction.Contain) != 0) { contain = true; if (o.Mass < 100f) small = true; }
                if ((o.Reaction & Reaction.Launch) != 0) launch = true;
            }
            // A rotation through the kinds, skipping the ones this level's props cannot give.
            var order = new[] { GoalKind.Chain, GoalKind.HalfClock, GoalKind.Contained, GoalKind.AirTime, GoalKind.HoleInOne };
            for (int k = 0; k < order.Length; k++)
            {
                var g = order[(l.Number + k) % order.Length];
                if (g == GoalKind.Contained && !contain) continue;
                if (g == GoalKind.HoleInOne && !small) continue;
                if (g == GoalKind.AirTime && !launch) continue;
                if (g == GoalKind.Chain && l.Props.Length < 3) continue;
                return g;
            }
            return GoalKind.HalfClock;
        }

        public static string Text(GoalKind g)
        {
            switch (g)
            {
                case GoalKind.Chain: return "Make a chain of " + ChainLinks;
                case GoalKind.HalfClock: return "Clear it with half the clock left";
                case GoalKind.Contained: return "Blow something up from inside";
                case GoalKind.AirTime: return "Send something flying";
                case GoalKind.HoleInOne: return "Throw a lit one into a bin or a box";
                default: return "";
            }
        }

        public static int Reward(LevelDef l) { return 100 + l.Number * 10; }

        /// <summary>Whether the goal was met in a round that cleared the level.</summary>
        public static bool Met(GoalKind g, LevelDef l, int longestChain, float timeLeft, int containedHits, IList<BonusKind> bonuses)
        {
            switch (g)
            {
                case GoalKind.Chain: return longestChain >= ChainLinks;
                case GoalKind.HalfClock: return timeLeft >= l.Time * 0.5f;
                case GoalKind.Contained: return containedHits > 0;
                case GoalKind.AirTime: return bonuses.Contains(BonusKind.AirTime);
                case GoalKind.HoleInOne: return bonuses.Contains(BonusKind.HoleInOne);
                default: return false;
            }
        }
    }
}
