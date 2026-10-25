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
            { "bonus.CloseCall", "Close call +100 coins" },
            { "bonus.Multi", "Multi-kill +160 coins" },
            { "bonus.LastSecond", "Last second +60 coins" },
            { "bonus.HandBurn", "Went off in your hand −40 coins" },
            { "tip.chain", "Tip: hit things that explode by themselves. Every chain link adds ×0.5" },
            { "tip.again", "Tip: place charges under cars for ×1.8 containment" },
            { "tip.time", "Tip: light several charges before you run, and let the blasts chain" },
            { "tip.empty", "Tip: buy extra explosives in the shop, or aim for the fuel tanks" },
        };
    }
}
