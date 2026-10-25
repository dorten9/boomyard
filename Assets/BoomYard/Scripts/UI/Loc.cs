using System.Collections.Generic;

namespace BoomYard
{
    /// <summary>UI text (English). Kept behind keys so copy can change in one place.</summary>
    public static class Loc
    {
        public static void Init(string lang) { }

        public static string T(string key)
        {
            string s;
            return Strings.TryGetValue(key, out s) ? s : key;
        }

        public static string F(string key, params object[] args) { return string.Format(T(key), args); }

        /// <summary>Damage shown as what it earns: coins (one currency, see ScoreCalculator.DamagePerCoin).</summary>
        public static string Money(long damage) { return Coins(BoomYard.Core.ScoreCalculator.CoinsFromDamage(damage)); }
        public static string Coins(long v) { return v.ToString("#,0"); }

        public static string Time(float seconds)
        {
            int s = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Max(0f, seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        static readonly Dictionary<string, string> Strings = new Dictionary<string, string>
        {
            { "title.tag", "Light it. Count. Run." },
            { "disclaimer.title", "Before you start" },
            { "disclaimer.l1", "This is a game. No person or animal gets hurt, only property." },
            { "disclaimer.l2", "Real explosives are dangerous and illegal." },
            { "disclaimer.l3", "Never try any of this at home. 16+" },
            { "disclaimer.ok", "GOT IT" },
            { "toast.wind", "The wind blew it out. Turn your back to the wind" },
            { "toast.run", "Running blows the lighter out" },
            { "toast.match", "The match snapped" },
            { "toast.light", "Light it first: hold LIGHT" },
            { "toast.reach", "Too far. Get closer to place it" },
            { "toast.contained", "Contained ×1.8" },
            { "toast.placed", "Placed. Hold LIGHT next to it" },
            { "toast.runaway", "RUN!" },
            { "toast.fizzle", "The rain put a fuse out. Light it again" },
            { "toast.timer", "Timer set: {0} s. Walk away" },
            { "toast.timerset", "Timer: {0} s" },
            { "toast.remote", "Remote charge placed. Press BOOM when you're clear" },
            { "rule.Rain", "RAIN: fuses are slow to catch and can fizzle out" },
            { "rule.Windy", "STRONG WIND: keep your back to it when you light" },
            { "rule.Spare", "DON'T BREAK THE {0}" },
            { "rule.Matches", "NO LIGHTER TODAY: matches only" },
            { "fail.spare", "YOU BROKE IT" },
            { "tip.spare", "Tip: smaller charges and placing them away from it keep it in one piece" },
            { "bonus.CloseCall", "Close call +100 coins" },
            { "bonus.Multi", "Multi-kill +160 coins" },
            { "bonus.LastSecond", "Last second +60 coins" },
            { "bonus.HandBurn", "Went off in your hand −40 coins" },
            { "bonus.AirTime", "Air time +50 coins" },
            { "bonus.Domino", "Domino! Five in a row +120 coins" },
            { "bonus.HoleInOne", "Hole in one! +80 coins" },
            { "flight", "The {0} flew {1} m!" },
            { "city.intro", "The whole city is yours: wreck it, paint it. No clock. Pause to cash in." },
            { "tip.city", "Rockets set down in a row go up together with SHOW. Try the spray can on the tower." },
            { "toast.nostencil", "No stencil on the can: tap STENCIL above the pocket" },
            { "toast.nowall", "Get closer to a wall" },
            { "toast.buystencil", "Stencils are in the shop: GRAFFITI" },
            { "toast.buycap", "More caps are in the shop: GRAFFITI" },
            { "goldgnome", "Golden gnome! +{0} coins  ({1} of {2} found)" },
            { "tip.chain", "Tip: hit things that explode by themselves. Every chain link adds ×0.5" },
            { "tip.again", "Tip: place charges under cars for ×1.8 containment" },
            { "tip.time", "Tip: light several charges before you run, and let the blasts chain" },
            { "tip.empty", "Tip: buy extra explosives in the shop, or aim for the fuel tanks" },
        };
    }
}
