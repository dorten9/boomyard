using System;
using System.Collections.Generic;

namespace BoomYard.Core
{
    /// <summary>What the shop sells, all for coins: explosives, lighters, upgrades, styles and graffiti gear.</summary>
    public sealed class ShopItem
    {
        public string Id, Name, Kind;   // Kind: "explosive", "lighter", "upgrade", "glove", "lighterskin", "spray", "cap", "stencil", "maker"
        public string Grants;            // explosive / lighter / upgrade id
        public int Amount;               // units in the pack
        public int Coins;                // price
        public int UnlockLevel;          // shown from this level on
    }

    public static class Shop
    {
        public static readonly ShopItem[] Items =
        {
            // Explosives are unlocked once and are then unlimited in every level.
            new ShopItem { Id = "pack_string", Name = "Firecracker String", Kind = "explosive", Grants = "string50", Amount = 1, Coins = 600 },
            new ShopItem { Id = "pack_sparkler", Name = "Sparkler Bomb", Kind = "explosive", Grants = "sparkler", Amount = 1, Coins = 500 },
            new ShopItem { Id = "pack_rocket", Name = "Rocket", Kind = "explosive", Grants = "rocket", Amount = 1, Coins = 800, UnlockLevel = 3 },
            new ShopItem { Id = "pack_m80", Name = "M-80", Kind = "explosive", Grants = "m80", Amount = 1, Coins = 1200, UnlockLevel = 2 },
            new ShopItem { Id = "pack_cherry", Name = "Cherry Bomb", Kind = "explosive", Grants = "cherry", Amount = 1, Coins = 1000, UnlockLevel = 3 },
            new ShopItem { Id = "pack_timer", Name = "Timer Charge", Kind = "explosive", Grants = "timer", Amount = 1, Coins = 1500, UnlockLevel = 14 },
            new ShopItem { Id = "pack_pipe", Name = "Pipe Bomb", Kind = "explosive", Grants = "pipe", Amount = 1, Coins = 2500, UnlockLevel = 6 },
            new ShopItem { Id = "pack_remote", Name = "Remote Charge", Kind = "explosive", Grants = "remote", Amount = 1, Coins = 2500, UnlockLevel = 22 },
            new ShopItem { Id = "pack_dynamite", Name = "Dynamite", Kind = "explosive", Grants = "dynamite", Amount = 1, Coins = 3500, UnlockLevel = 8 },
            new ShopItem { Id = "pack_keg", Name = "Powder Keg", Kind = "explosive", Grants = "keg", Amount = 1, Coins = 5000, UnlockLevel = 18 },
            new ShopItem { Id = "lighter_zippo", Name = "Zippo", Kind = "lighter", Grants = "zippo", Amount = 1, Coins = 2500 },
            new ShopItem { Id = "lighter_turbo", Name = "Turbo Lighter", Kind = "lighter", Grants = "turbo", Amount = 1, Coins = 5000 },
            new ShopItem { Id = "up_vest", Name = "Vest: +2 pocket slots", Kind = "upgrade", Grants = "vest", Amount = 1, Coins = 2500 },
            new ShopItem { Id = "up_legs", Name = "Lungs: run longer & faster", Kind = "upgrade", Grants = "legs", Amount = 1, Coins = 1800 },
            new ShopItem { Id = "up_watch", Name = "Watch: +10 s every level", Kind = "upgrade", Grants = "watch", Amount = 1, Coins = 3000 },
            // Style: what your hands and lighter look like. Looks only.
            new ShopItem { Id = "glove_none", Name = "Bare Hands", Kind = "glove", Grants = "", Amount = 1, Coins = 0 },
            new ShopItem { Id = "glove_work", Name = "Work Gloves", Kind = "glove", Grants = "work", Amount = 1, Coins = 600 },
            new ShopItem { Id = "glove_garden", Name = "Garden Gloves", Kind = "glove", Grants = "garden", Amount = 1, Coins = 700 },
            new ShopItem { Id = "glove_black", Name = "Black Leather", Kind = "glove", Grants = "black", Amount = 1, Coins = 900 },
            new ShopItem { Id = "glove_hivis", Name = "Hi-Vis Orange", Kind = "glove", Grants = "hivis", Amount = 1, Coins = 1200 },
            new ShopItem { Id = "glove_gold", Name = "Gold Gloves", Kind = "glove", Grants = "gold", Amount = 1, Coins = 3000 },
            new ShopItem { Id = "skin_none", Name = "Grey Lighter", Kind = "lighterskin", Grants = "", Amount = 1, Coins = 0 },
            new ShopItem { Id = "skin_red", Name = "Red Lighter", Kind = "lighterskin", Grants = "red", Amount = 1, Coins = 400 },
            new ShopItem { Id = "skin_chrome", Name = "Chrome Lighter", Kind = "lighterskin", Grants = "chrome", Amount = 1, Coins = 600 },
            new ShopItem { Id = "skin_camo", Name = "Camo Lighter", Kind = "lighterskin", Grants = "camo", Amount = 1, Coins = 800 },
            new ShopItem { Id = "skin_gold", Name = "Gold Lighter", Kind = "lighterskin", Grants = "gold", Amount = 1, Coins = 2500 },
            // Graffiti: spray colours and caps, stencils, and the stencil maker for your own. Each is bought once.
            new ShopItem { Id = "spray_red", Name = "Fire Red Spray", Kind = "spray", Grants = "red", Amount = 1, Coins = 0 },
            new ShopItem { Id = "spray_black", Name = "Black Spray", Kind = "spray", Grants = "black", Amount = 1, Coins = 200 },
            new ShopItem { Id = "spray_white", Name = "White Spray", Kind = "spray", Grants = "white", Amount = 1, Coins = 200 },
            new ShopItem { Id = "spray_yellow", Name = "Sun Yellow Spray", Kind = "spray", Grants = "yellow", Amount = 1, Coins = 300 },
            new ShopItem { Id = "spray_orange", Name = "Orange Spray", Kind = "spray", Grants = "orange", Amount = 1, Coins = 300 },
            new ShopItem { Id = "spray_blue", Name = "Sky Blue Spray", Kind = "spray", Grants = "blue", Amount = 1, Coins = 300 },
            new ShopItem { Id = "spray_green", Name = "Lime Spray", Kind = "spray", Grants = "green", Amount = 1, Coins = 300 },
            new ShopItem { Id = "spray_pink", Name = "Bubblegum Spray", Kind = "spray", Grants = "pink", Amount = 1, Coins = 400 },
            new ShopItem { Id = "spray_purple", Name = "Purple Spray", Kind = "spray", Grants = "purple", Amount = 1, Coins = 400 },
            new ShopItem { Id = "spray_chrome", Name = "Chrome Spray", Kind = "spray", Grants = "chrome", Amount = 1, Coins = 800 },
            new ShopItem { Id = "spray_gold", Name = "Gold Spray", Kind = "spray", Grants = "gold", Amount = 1, Coins = 1500 },
            new ShopItem { Id = "spray_neongreen", Name = "Neon Green Spray", Kind = "spray", Grants = "neongreen", Amount = 1, Coins = 2000 },
            new ShopItem { Id = "spray_neonpink", Name = "Neon Pink Spray", Kind = "spray", Grants = "neonpink", Amount = 1, Coins = 2000 },
            new ShopItem { Id = "cap_standard", Name = "Standard Cap", Kind = "cap", Grants = "standard", Amount = 1, Coins = 0 },
            new ShopItem { Id = "cap_skinny", Name = "Skinny Cap", Kind = "cap", Grants = "skinny", Amount = 1, Coins = 400 },
            new ShopItem { Id = "cap_fat", Name = "Fat Cap", Kind = "cap", Grants = "fat", Amount = 1, Coins = 800 },
            new ShopItem { Id = "maker", Name = "Stencil Maker", Kind = "maker", Grants = "maker", Amount = 1, Coins = 1500 },
            new ShopItem { Id = "stencil_heart", Name = "Heart Stencil", Kind = "stencil", Grants = "heart", Amount = 1, Coins = 300 },
            new ShopItem { Id = "stencil_star", Name = "Star Stencil", Kind = "stencil", Grants = "star", Amount = 1, Coins = 300 },
            new ShopItem { Id = "stencil_smiley", Name = "Smiley Stencil", Kind = "stencil", Grants = "smiley", Amount = 1, Coins = 400 },
            new ShopItem { Id = "stencil_bolt", Name = "Lightning Stencil", Kind = "stencil", Grants = "bolt", Amount = 1, Coins = 500 },
            new ShopItem { Id = "stencil_boom", Name = "BOOM Lettering", Kind = "stencil", Grants = "boom", Amount = 1, Coins = 500 },
            new ShopItem { Id = "stencil_skull", Name = "Skull Stencil", Kind = "stencil", Grants = "skull", Amount = 1, Coins = 600 },
            new ShopItem { Id = "stencil_flame", Name = "Flame Stencil", Kind = "stencil", Grants = "flame", Amount = 1, Coins = 700 },
            new ShopItem { Id = "stencil_cat", Name = "Cat Stencil", Kind = "stencil", Grants = "cat", Amount = 1, Coins = 700 },
            new ShopItem { Id = "stencil_crown", Name = "Crown Stencil", Kind = "stencil", Grants = "crown", Amount = 1, Coins = 900 },
            new ShopItem { Id = "stencil_bomb", Name = "Bomb Stencil", Kind = "stencil", Grants = "bomb", Amount = 1, Coins = 900 },
            new ShopItem { Id = "stencil_tag", Name = "WAS HERE Lettering", Kind = "stencil", Grants = "tag", Amount = 1, Coins = 1000 },
            new ShopItem { Id = "stencil_kaboom", Name = "KABOOM! Lettering", Kind = "stencil", Grants = "kaboom", Amount = 1, Coins = 1200 },
        };

        /// <summary>Bought once and kept (styles and graffiti gear); the free ones are owned from the start.</summary>
        public static bool Keepsake(ShopItem item)
        {
            switch (item.Kind)
            {
                case "glove": case "lighterskin": case "spray": case "cap": case "stencil": case "maker": return true;
                default: return false;
            }
        }

        public static ShopItem Get(string id)
        {
            foreach (var i in Items) if (i.Id == id) return i;
            return null;
        }
    }

    /// <summary>Colour (RGB), metalness and smoothness of a glove or lighter style.</summary>
    public struct Look
    {
        public float R, G, B, Metal, Smooth;
        public Look(float r, float g, float b, float metal, float smooth) { R = r; G = g; B = b; Metal = metal; Smooth = smooth; }
    }

    public static class Styles
    {
        /// <summary>A glove's look, or false for bare hands.</summary>
        public static bool Glove(string id, out Look look)
        {
            switch (id)
            {
                case "work": look = new Look(0.72f, 0.55f, 0.34f, 0f, 0.25f); return true;
                case "garden": look = new Look(0.25f, 0.5f, 0.22f, 0f, 0.45f); return true;
                case "black": look = new Look(0.06f, 0.06f, 0.065f, 0f, 0.55f); return true;
                case "hivis": look = new Look(1f, 0.42f, 0.05f, 0f, 0.4f); return true;
                case "gold": look = new Look(1f, 0.76f, 0.3f, 1f, 0.8f); return true;
            }
            look = default(Look);
            return false;
        }

        public static Look Lighter(string id)
        {
            switch (id)
            {
                case "red": return new Look(0.72f, 0.08f, 0.06f, 0.2f, 0.65f);
                case "chrome": return new Look(0.85f, 0.86f, 0.88f, 1f, 0.92f);
                case "camo": return new Look(0.3f, 0.34f, 0.2f, 0f, 0.3f);
                case "gold": return new Look(1f, 0.76f, 0.3f, 1f, 0.85f);
                default: return new Look(0.36f, 0.37f, 0.4f, 0.3f, 0.5f);
            }
        }
    }

    /// <summary>A daily mission: do something N times today.</summary>
    [Serializable]
    public sealed class Mission
    {
        public string kind;     // destroy, destroy_id, chain, clear, stars, contained, blasts
        public string target;   // object id for destroy_id
        public int goal, progress, coins;
        public bool claimed;
        public bool Done { get { return progress >= goal; } }

        public string Text
        {
            get
            {
                switch (kind)
                {
                    case "destroy": return "Destroy " + goal + " objects";
                    case "destroy_id": return "Destroy " + goal + " × " + Name(target);
                    case "chain": return "Make a chain of " + goal + " links";
                    case "clear": return "Clear " + goal + " level" + (goal > 1 ? "s" : "");
                    case "stars": return "Earn " + goal + " stars";
                    case "contained": return "Score " + goal + " contained blasts";
                    case "blasts": return "Set off " + goal + " explosives";
                    default: return kind;
                }
            }
        }

        static string Name(string id)
        {
            var o = Catalog.Object(id);
            return o != null ? o.NameEn.ToLowerInvariant() : id;
        }
    }

    public static class Missions
    {
        /// <summary>Today's three missions, the same for everyone on a date.</summary>
        public static List<Mission> ForDay(int daySeed)
        {
            var r = new Random(daySeed);
            var pool = new List<Mission>
            {
                new Mission { kind = "destroy", goal = 15 + r.Next(3) * 5, coins = 150 },
                new Mission { kind = "destroy_id", target = r.Next(2) == 0 ? "van" : "sedan", goal = 2, coins = 250 },
                new Mission { kind = "chain", goal = 3 + r.Next(2), coins = 400 },
                new Mission { kind = "clear", goal = 2 + r.Next(2), coins = 200 },
                new Mission { kind = "stars", goal = 4 + r.Next(3), coins = 300 },
                new Mission { kind = "contained", goal = 3, coins = 180 },
                new Mission { kind = "blasts", goal = 20, coins = 120 },
                new Mission { kind = "destroy_id", target = "window", goal = 10, coins = 150 },
            };
            var today = new List<Mission>();
            while (today.Count < 3)
            {
                int i = r.Next(pool.Count);
                today.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return today;
        }

        /// <summary>Counts an event towards every open mission it matches.</summary>
        public static void Count(List<Mission> list, string kind, string target, int amount)
        {
            foreach (var m in list)
            {
                if (m.claimed || m.kind != kind) continue;
                if (kind == "destroy_id" && m.target != target) continue;
                if (kind == "chain") m.progress = Math.Max(m.progress, amount);
                else m.progress = Math.Min(m.goal, m.progress + amount);
            }
        }
    }

    /// <summary>The 7-day login calendar: coins every day, a big pile on day 7.</summary>
    public static class DailyReward
    {
        public static readonly int[] Coins = { 100, 150, 500, 250, 300, 400, 1500 };

        /// <summary>Next streak day (0–6) given the last claim day and today (days as Daily.Seed values).</summary>
        public static int NextDay(int lastClaimDate, int streak, DateTime today)
        {
            if (lastClaimDate == 0) return 0;
            var last = new DateTime(lastClaimDate / 10000, lastClaimDate / 100 % 100, lastClaimDate % 100);
            int gap = (today.Date - last.Date).Days;
            if (gap <= 0) return -1;            // already claimed today
            if (gap == 1) return (streak + 1) % 7;
            return 0;                            // missed a day: start over
        }
    }

    public static class Progress
    {
        /// <summary>
        /// Coins from a round: everything the wreckage was worth, plus on a clear the level's reward (in full the
        /// first time, a fifth on replays) and 40 for every new star.
        /// </summary>
        public static int LevelCoins(LevelDef level, bool cleared, bool firstClear, int newStars, long damage)
        {
            int wreck = (int)ScoreCalculator.CoinsFromDamage(damage);
            if (!cleared) return wreck;
            return wreck + (firstClear ? level.Reward : level.Reward / 5) + newStars * 40;
        }

        /// <summary>The big city pays a quarter of what its wreckage is worth (there is a whole downtown of it).</summary>
        public const int CityShare = 4;

        public static int CityCoins(long damage) { return (int)(ScoreCalculator.CoinsFromDamage(damage) / CityShare); }
    }
}
