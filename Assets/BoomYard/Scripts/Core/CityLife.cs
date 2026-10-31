using System;
using System.Collections.Generic;

namespace BoomYard.Core
{
    /// <summary>This week's twist on the big city.</summary>
    public enum CityEvent { LowGravity, BigBangs, Inferno, GoldRush }

    /// <summary>
    /// The big city changes every week (the same for everyone, from the date): low gravity (things fly far),
    /// big bangs (every blast half as big again), inferno (whatever breaks catches fire) or a gold rush (double
    /// coins when cashing in).
    /// </summary>
    public static class CityEvents
    {
        /// <summary>Weeks counted from Monday 5 January 2026.</summary>
        public static int Week(DateTime date) { return (int)Math.Floor((date.Date - new DateTime(2026, 1, 5)).TotalDays / 7.0); }

        public static CityEvent For(DateTime date)
        {
            int w = Week(date);
            int n = Enum.GetValues(typeof(CityEvent)).Length;
            return (CityEvent)(((w % n) + n) % n);
        }

        public static string Name(CityEvent e)
        {
            switch (e)
            {
                case CityEvent.LowGravity: return "LOW GRAVITY";
                case CityEvent.BigBangs: return "BIG BANGS";
                case CityEvent.Inferno: return "INFERNO";
                default: return "GOLD RUSH";
            }
        }

        public static string Text(CityEvent e)
        {
            switch (e)
            {
                case CityEvent.LowGravity: return "Gravity is a third: cars fly for ages.";
                case CityEvent.BigBangs: return "Every blast is half as big again.";
                case CityEvent.Inferno: return "Whatever breaks catches fire.";
                default: return "Double coins when you cash in.";
            }
        }

        public static float Gravity(CityEvent e) { return e == CityEvent.LowGravity ? 1f / 3f : 1f; }
        public static float BlastScale(CityEvent e) { return e == CityEvent.BigBangs ? 1.5f : 1f; }
        public static int CoinFactor(CityEvent e) { return e == CityEvent.GoldRush ? 2 : 1; }
    }

    /// <summary>A job in the big city: destroy so many of one kind of thing before the clock runs out.</summary>
    public sealed class Contract
    {
        public string Target;      // object id (Catalog)
        public string Label;       // "traffic lights"
        public int Goal, Done;
        public float Seconds, Left;
        public int Reward;

        public bool Complete { get { return Done >= Goal; } }
        public bool Failed { get { return !Complete && Left <= 0f; } }
        public string Text { get { return "Destroy " + Goal + " " + Label + "  " + Done + "/" + Goal; } }

        /// <summary>Something was destroyed: counts if it is the target. True when that completes the job.</summary>
        public bool Count(string id)
        {
            if (Complete || Failed || id != Target) return false;
            Done++;
            return Complete;
        }

        public void Tick(float dt) { if (!Complete) Left = Math.Max(0f, Left - dt); }
    }

    /// <summary>The contracts on offer in the big city, one at a time, a new one after each is done or missed.</summary>
    public static class Contracts
    {
        struct Kind { public string Id, Label; public int Min, Max; public float PerItem; public int Pay; }

        static readonly Kind[] Kinds =
        {
            new Kind { Id = "trafficlight", Label = "traffic lights", Min = 3, Max = 6, PerItem = 12f, Pay = 40 },
            new Kind { Id = "streetlamp", Label = "street lamps", Min = 4, Max = 8, PerItem = 9f, Pay = 30 },
            new Kind { Id = "sedan", Label = "family cars", Min = 2, Max = 4, PerItem = 20f, Pay = 70 },
            new Kind { Id = "storefront", Label = "shop windows", Min = 5, Max = 10, PerItem = 6f, Pay = 20 },
            new Kind { Id = "bin", Label = "bins", Min = 4, Max = 8, PerItem = 8f, Pay = 20 },
            new Kind { Id = "hydrant", Label = "fire hydrants", Min = 2, Max = 4, PerItem = 15f, Pay = 45 },
            new Kind { Id = "newsbox", Label = "newspaper boxes", Min = 2, Max = 4, PerItem = 12f, Pay = 30 },
            new Kind { Id = "busstop", Label = "bus shelter panes", Min = 2, Max = 3, PerItem = 15f, Pay = 35 },
            new Kind { Id = "parkingmeter", Label = "parking meters", Min = 2, Max = 3, PerItem = 14f, Pay = 30 },
            new Kind { Id = "cafetable", Label = "cafe tables", Min = 3, Max = 6, PerItem = 8f, Pay = 20 },
            new Kind { Id = "van", Label = "vans", Min = 1, Max = 3, PerItem = 22f, Pay = 80 },
        };

        /// <summary>A new contract for things that are still standing (<paramref name="available"/>: id → how many).</summary>
        public static Contract Next(Random r, IDictionary<string, int> available)
        {
            var options = new List<Kind>();
            foreach (var k in Kinds)
            {
                int have;
                if (available.TryGetValue(k.Id, out have) && have >= k.Min) options.Add(k);
            }
            if (options.Count == 0) return null;
            var pick = options[r.Next(options.Count)];
            int max = Math.Min(pick.Max, available[pick.Id]);
            int goal = pick.Min + r.Next(max - pick.Min + 1);
            float seconds = (float)Math.Round((20f + goal * pick.PerItem) / 5f) * 5f;
            return new Contract { Target = pick.Id, Label = pick.Label, Goal = goal, Seconds = seconds, Left = seconds, Reward = goal * pick.Pay };
        }
    }

    /// <summary>The daily login streak's surprise: on some days a firework kind is unlocked for good, free.</summary>
    public static class Surprise
    {
        /// <summary>Streak days (0-based) that bring a surprise firework.</summary>
        public static bool OnDay(int day) { return day == 2 || day == 4 || day == 6; }

        /// <summary>A firework or bang the player does not have yet (shop ids not in <paramref name="owned"/>), or null.</summary>
        public static string Pick(Random r, ICollection<string> owned)
        {
            var list = new List<string>();
            foreach (var it in Shop.Items) if (it.Kind == "explosive" && !owned.Contains(it.Grants)) list.Add(it.Grants);
            return list.Count == 0 ? null : list[r.Next(list.Count)];
        }
    }
}

namespace BoomYard.Core
{
    /// <summary>A rare paint job on a car: spot one, blow it up, and it goes in the collection.</summary>
    public sealed class RareCar
    {
        public string Id, Name, Prop;
        public float R, G, B, Metal, Smooth;
        public int Coins;
    }

    public static class RareCars
    {
        public static readonly RareCar[] All =
        {
            new RareCar { Id = "goldsuper", Name = "Gold Supercar", Prop = "sportscar", R = 1f, G = 0.76f, B = 0.3f, Metal = 1f, Smooth = 0.9f, Coins = 400 },
            new RareCar { Id = "chromevan", Name = "Chrome Van", Prop = "van", R = 0.9f, G = 0.92f, B = 0.95f, Metal = 1f, Smooth = 0.95f, Coins = 250 },
            new RareCar { Id = "pinkcruiser", Name = "Pink Cruiser", Prop = "sedan", R = 1f, G = 0.45f, B = 0.75f, Metal = 0.5f, Smooth = 0.85f, Coins = 200 },
            new RareCar { Id = "midnight", Name = "Midnight Pickup", Prop = "pickup", R = 0.25f, G = 0.1f, B = 0.45f, Metal = 0.7f, Smooth = 0.9f, Coins = 250 },
            new RareCar { Id = "mintcart", Name = "Mint Golf Cart", Prop = "golfcart", R = 0.55f, G = 1f, B = 0.8f, Metal = 0.3f, Smooth = 0.8f, Coins = 150 },
            new RareCar { Id = "lavatruck", Name = "Lava Truck", Prop = "boxtruck", R = 1f, G = 0.35f, B = 0.05f, Metal = 0.6f, Smooth = 0.85f, Coins = 300 },
        };

        /// <summary>Chance that a level shows one of its cars in a rare paint job.</summary>
        public const float LevelChance = 0.15f;

        public static RareCar Find(string id)
        {
            foreach (var c in All) if (c.Id == id) return c;
            return null;
        }

        /// <summary>The rare variants a prop can come in.</summary>
        public static List<RareCar> For(string prop)
        {
            var l = new List<RareCar>();
            foreach (var c in All) if (c.Prop == prop) l.Add(c);
            return l;
        }
    }
}
