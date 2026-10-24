using System;
using System.Collections.Generic;

namespace BoomYard.Core
{
    /// <summary>What the shop sells: packs of explosives for coins, lighters and upgrades for coins or gems.</summary>
    public sealed class ShopItem
    {
        public string Id, Name, Kind;   // Kind: "explosive", "lighter", "upgrade"
        public string Grants;            // explosive / lighter / upgrade id
        public int Amount;               // units in the pack
        public int Coins, Gems;          // price (one of them)
        public int UnlockLevel;          // shown from this level on
    }

    public static class Shop
    {
        public static readonly ShopItem[] Items =
        {
            new ShopItem { Id = "pack_blackcat", Name = "Black Cats ×10", Kind = "explosive", Grants = "blackcat", Amount = 10, Coins = 120 },
            new ShopItem { Id = "pack_string", Name = "Firecracker String ×2", Kind = "explosive", Grants = "string50", Amount = 2, Coins = 220 },
            new ShopItem { Id = "pack_sparkler", Name = "Sparkler Bombs ×3", Kind = "explosive", Grants = "sparkler", Amount = 3, Coins = 200 },
            new ShopItem { Id = "pack_m80", Name = "M-80 ×3", Kind = "explosive", Grants = "m80", Amount = 3, Coins = 450, UnlockLevel = 2 },
            new ShopItem { Id = "pack_cherry", Name = "Cherry Bombs ×3", Kind = "explosive", Grants = "cherry", Amount = 3, Coins = 380, UnlockLevel = 3 },
            new ShopItem { Id = "pack_pipe", Name = "Pipe Bomb ×1", Kind = "explosive", Grants = "pipe", Amount = 1, Coins = 900, UnlockLevel = 6 },
            new ShopItem { Id = "pack_dynamite", Name = "Dynamite ×2", Kind = "explosive", Grants = "dynamite", Amount = 2, Coins = 1200, UnlockLevel = 8 },
            new ShopItem { Id = "lighter_zippo", Name = "Zippo", Kind = "lighter", Grants = "zippo", Amount = 1, Gems = 20 },
            new ShopItem { Id = "lighter_turbo", Name = "Turbo Lighter", Kind = "lighter", Grants = "turbo", Amount = 1, Gems = 45 },
            new ShopItem { Id = "up_vest", Name = "Vest: +1 pocket slot", Kind = "upgrade", Grants = "vest", Amount = 1, Coins = 1500 },
            new ShopItem { Id = "up_legs", Name = "Lungs: run longer & faster", Kind = "upgrade", Grants = "legs", Amount = 1, Coins = 1000 },
            new ShopItem { Id = "up_watch", Name = "Watch: +10 s every level", Kind = "upgrade", Grants = "watch", Amount = 1, Gems = 30 },
        };

        public static ShopItem Get(string id)
        {
            foreach (var i in Items) if (i.Id == id) return i;
            return null;
        }
    }

    /// <summary>A daily mission: do something N times today.</summary>
    [Serializable]
    public sealed class Mission
    {
        public string kind;     // destroy, destroy_id, chain, clear, stars, contained, blasts
        public string target;   // object id for destroy_id
        public int goal, progress, coins, gems;
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
                new Mission { kind = "chain", goal = 3 + r.Next(2), coins = 200, gems = 2 },
                new Mission { kind = "clear", goal = 2 + r.Next(2), coins = 200 },
                new Mission { kind = "stars", goal = 4 + r.Next(3), gems = 3 },
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

    /// <summary>The 7-day login calendar: coins every day, gems on days 3 and 7.</summary>
    public static class DailyReward
    {
        public static readonly int[] Coins = { 100, 150, 0, 250, 300, 400, 0 };
        public static readonly int[] Gems = { 0, 0, 5, 0, 0, 0, 15 };

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
        /// <summary>Coins for clearing a level: its reward the first time, a share on replays, plus 2% of the damage.</summary>
        public static int LevelCoins(LevelDef level, bool firstClear, int newStars, long damage)
        {
            int baseCoins = firstClear ? level.Reward : level.Reward / 5;
            return baseCoins + newStars * 40 + (int)(ScoreCalculator.CoinsFromDamage(damage) / 10);
        }
    }
}
