using System;
using System.Collections.Generic;

namespace BoomYard.Core
{
    /// <summary>A prop placed in a level (metres; +Z is away from the player's start, yaw in degrees).</summary>
    public struct PropSpot
    {
        public string Id;
        public float X, Z, Yaw;
        public int Variant;

        public PropSpot(string id, float x, float z, float yaw = 0f, int variant = 0)
        {
            Id = id; X = x; Z = z; Yaw = yaw; Variant = variant;
        }
    }

    /// <summary>A twist on a level's rules.</summary>
    public enum LevelRule
    {
        None,
        Rain,       // wet fuses: lighting takes longer and a burning fuse can fizzle out
        Windy,      // gusts twice as strong
        Spare,      // one object must survive (LevelDef.Spare): breaking it fails the level
        Matches,    // no lighter today, only a box of matches
    }

    /// <summary>
    /// One level on the map: destroy every prop before the clock runs out. Stars by how much time is left;
    /// the kit is what the player gets for the level, on top of anything bought in the shop. The street it is
    /// on comes from its number (Streets).
    /// </summary>
    public sealed class LevelDef
    {
        public int Number;
        public string Name;
        public float Time;
        public PropSpot[] Props;
        public PocketSlot[] Kit;
        public bool Tutorial;
        public int Reward;   // coins for clearing it the first time
        public LevelRule Rule;
        public int Spare = -1;   // index into Props of the object to keep (LevelRule.Spare)
        public bool Boss;        // the street's last level: one big target and everything round it
        public bool Daily;       // today's challenge: a copy of a level with a twist, the same for everyone
        public bool Show;        // the big city: no targets, no clock (and placed rockets go up as a show)

        public StreetDef Street { get { return Streets.ForLevel(Number); } }

        /// <summary>Whether the prop at <paramref name="index"/> has to be destroyed to clear the level.</summary>
        public bool IsTarget(int index) { return !(Rule == LevelRule.Spare && index == Spare); }

        /// <summary>★★★ with 40% of the time left, ★★ with 15%, ★ for clearing it at all.</summary>
        public int Stars(bool cleared, float timeLeft)
        {
            if (!cleared) return 0;
            if (timeLeft >= Time * 0.4f) return 3;
            if (timeLeft >= Time * 0.15f) return 2;
            return 1;
        }
    }

    /// <summary>The level map: ten levels on Maple Drive, then eight on each of the other streets.</summary>
    public static class Levels
    {
        static PropSpot P(string id, float x, float z, float yaw = 0f, int variant = 0) { return new PropSpot(id, x, z, yaw, variant); }

        static LevelDef L(int n, string name, float time, int reward, PocketSlot[] kit, params PropSpot[] props)
        {
            return new LevelDef { Number = n, Name = name, Time = time, Reward = reward, Kit = kit, Props = props };
        }

        static LevelDef With(this LevelDef l, LevelRule rule, int spare = -1) { l.Rule = rule; l.Spare = spare; return l; }
        static LevelDef AsBoss(this LevelDef l) { l.Boss = true; return l; }

        static PocketSlot[] Kit(params object[] pairs)
        {
            var l = new List<PocketSlot>();
            for (int i = 0; i + 1 < pairs.Length; i += 2) l.Add(new PocketSlot { id = (string)pairs[i], count = (int)pairs[i + 1] });
            return l.ToArray();
        }

        public static readonly LevelDef[] All =
        {
            new LevelDef { Number = 1, Name = "First Light", Time = 120f, Tutorial = true, Reward = 150,
                Props = new[] { new PropSpot("bin", -4.5f, 8f, 10f), new PropSpot("mailbox", -6.5f, -7.5f, 90f), new PropSpot("crate", 2.5f, 4f, 20f) },
                Kit = Kit("blackcat", 6) },
            new LevelDef { Number = 2, Name = "Bin Day", Time = 75f, Reward = 200,
                Props = new[] { new PropSpot("bin", -4.5f, 8.5f, 8f, 0), new PropSpot("bin", -3.6f, 8.7f, -6f, 1), new PropSpot("bin", -2.7f, 8.5f, 4f, 2), new PropSpot("mailbox", -6.5f, -7.5f, 90f), new PropSpot("crate", 3f, 2f, 30f) },
                Kit = Kit("blackcat", 6, "m80", 1) },
            new LevelDef { Number = 3, Name = "Pallet Party", Time = 75f, Reward = 250,
                Props = new[] { new PropSpot("pallets", -6.5f, 4f, 17f), new PropSpot("pallets", -5f, 5.5f, -10f), new PropSpot("crate", -3.5f, 3.8f, 5f), new PropSpot("crate", -3.6f, 4.9f, 40f), new PropSpot("bicycle", -9.5f, 1.5f, 0f) },
                Kit = Kit("blackcat", 4, "sparkler", 2) },
            new LevelDef { Number = 4, Name = "The Old Van", Time = 80f, Reward = 300,
                Props = new[] { new PropSpot("van", 5.5f, 1f, 160f), new PropSpot("bin", -4.5f, 8.5f, 8f), new PropSpot("bin", -3.6f, 8.7f, -6f, 1), new PropSpot("mailbox", -6.5f, -7.5f, 90f) },
                Kit = Kit("blackcat", 4, "m80", 2) },
            new LevelDef { Number = 5, Name = "Gas Leak", Time = 70f, Reward = 350,
                Props = new[] { new PropSpot("gascyl", 1f, 6f), new PropSpot("gascyl", 1.4f, 6.3f), new PropSpot("pallets", 2.8f, 6.2f, 10f), new PropSpot("barrel", -1f, 6.5f, 0f, 0), new PropSpot("barrel", -1.7f, 6.2f, 0f, 1), new PropSpot("acunit", 6.2f, 13.6f, 0f) },
                Kit = Kit("blackcat", 4, "m80", 1) },
            new LevelDef { Number = 6, Name = "Driveway", Time = 85f, Reward = 400,
                Props = new[] { new PropSpot("sedan", 6.5f, 2f, 175f, 0), new PropSpot("van", 6.5f, -5.5f, 182f, 1), new PropSpot("bin", -4.5f, 8.5f, 8f), new PropSpot("bicycle", -9.5f, 1.5f, 0f), new PropSpot("barrel", 3.5f, 7.5f) },
                Kit = Kit("blackcat", 4, "m80", 3) },
            new LevelDef { Number = 7, Name = "Fence Line", Time = 70f, Reward = 450,
                Props = new[] { new PropSpot("fence", -11f, -3f, 0f), new PropSpot("fence", -11f, 0f, 0f), new PropSpot("fence", -11f, 3f, 0f), new PropSpot("fence", -11f, 6f, 0f), new PropSpot("bicycle", -10f, 1.5f, 0f), new PropSpot("pallets", -9f, 7f, 5f) },
                Kit = Kit("string50", 2, "blackcat", 4) },
            new LevelDef { Number = 8, Name = "Block Party", Time = 95f, Reward = 550,
                Props = new[] { new PropSpot("sedan", 6.5f, 2f, 175f, 2), new PropSpot("sedan", -1f, -15f, 90f, 1), new PropSpot("van", -8f, -15.5f, 92f, 2), new PropSpot("gascyl", 2f, 6f), new PropSpot("barrel", 2.6f, 6.4f), new PropSpot("bin", -4.5f, 8.5f, 8f), new PropSpot("bin", -3.6f, 8.7f, -6f, 1), new PropSpot("mailbox", -6.5f, -7.5f, 90f) },
                Kit = Kit("blackcat", 6, "m80", 3, "sparkler", 2) },
            new LevelDef { Number = 9, Name = "Chain Reaction", Time = 60f, Reward = 650,
                Props = new[] { new PropSpot("van", 5.5f, 1f, 160f), new PropSpot("sedan", 1.5f, -2f, 120f, 3), new PropSpot("barrel", 3.2f, 3.5f), new PropSpot("barrel", -0.3f, 0.5f, 0f, 1), new PropSpot("gascyl", -2f, 2f), new PropSpot("pallets", -4f, 3.5f), new PropSpot("acunit", 6.2f, 13.6f, 0f) },
                Kit = Kit("m80", 2, "blackcat", 2) },
            new LevelDef { Number = 10, Name = "Whole Street", Time = 120f, Reward = 1000,
                Props = new[] { new PropSpot("van", 6.5f, -5.5f, 182f), new PropSpot("sedan", 6.5f, 2f, 175f, 1), new PropSpot("sedan", -1f, -15f, 90f, 0), new PropSpot("van", -8f, -15.5f, 92f, 1), new PropSpot("fence", -11f, 0f), new PropSpot("fence", -11f, 3f), new PropSpot("pallets", -6.5f, 4f), new PropSpot("bin", -4.5f, 8.5f), new PropSpot("bin", -3.6f, 8.7f, 0f, 1), new PropSpot("mailbox", -6.5f, -7.5f, 90f), new PropSpot("gascyl", 2f, 6f), new PropSpot("barrel", 2.6f, 6.4f), new PropSpot("acunit", 6.2f, 13.6f), new PropSpot("bicycle", -9.5f, 1.5f) },
                Kit = Kit("blackcat", 8, "m80", 4, "sparkler", 2, "dynamite", 1), Boss = true },

            // ---------- Oak Row: brick terraces ----------
            L(11, "Recycling Day", 70f, 1100, Kit("blackcat", 6, "m80", 1),
                P("bin", -9f, 10.4f, 5f, 0), P("bin", -8.1f, 10.5f, -4f, 1), P("bin", -7.2f, 10.4f, 8f, 2), P("bin", 4f, 10.4f, 0f, 1),
                P("newsbox", -4f, -10.9f, 0f), P("cone", 2.5f, -13f), P("cone", 4f, -13.4f)),
            L(12, "Bike Shed", 75f, 1200, Kit("blackcat", 4, "sparkler", 2, "m80", 1),
                P("shed", 6.5f, 8f, 180f), P("bicycle", 4.2f, 7.2f, 90f), P("bicycle", 3.4f, 6.6f, 80f), P("crate", -6f, 6f, 15f), P("bin", -8f, 10.4f)),
            L(13, "Kerbside", 70f, 1300, Kit("rocket", 3, "m80", 2, "blackcat", 4),
                P("sedan", -6f, -13.4f, 90f, 1), P("scooter", 3f, -12.8f, 90f), P("bin", -9f, 10.4f), P("bin", -8.1f, 10.5f, 0f, 2), P("newsbox", 6f, -10.9f)),
            L(14, "Rainy Row", 80f, 1400, Kit("timer", 2, "blackcat", 6, "m80", 2),
                P("bin", -9f, 10.4f), P("bin", -8.1f, 10.5f, 0f, 1), P("bin", -7.2f, 10.4f, 0f, 2), P("gascyl", 2f, 7f), P("barrel", 3f, 7.4f), P("crate", -2f, 4f, 30f)).With(LevelRule.Rain),
            L(15, "Gnome Guard", 75f, 1500, Kit("blackcat", 6, "cherry", 2),
                P("gnome", 0f, 6f, 180f), P("bin", -5f, 9f), P("crate", 4f, 6.5f, 10f), P("bicycle", -3f, 3f, 70f), P("gascyl", 6.5f, 3f), P("bin", 2.5f, 9.5f, 0f, 1)).With(LevelRule.Spare, 0),
            L(16, "Scooter Gang", 70f, 1600, Kit("m80", 3, "blackcat", 4),
                P("scooter", -4f, -12.8f, 90f), P("scooter", -1.5f, -12.8f, 95f), P("scooter", 1f, -12.8f, 85f), P("gascyl", 6.5f, 4f), P("newsbox", 6f, -10.9f)),
            L(17, "Skip Day", 80f, 1700, Kit("string50", 1, "m80", 2, "blackcat", 4),
                P("pallets", -6f, 5f, 10f), P("pallets", -4.6f, 6f, -8f), P("crate", -3f, 4f), P("crate", -3.2f, 5f, 30f), P("crate", 2f, 5f, 12f), P("barrel", 3f, 6f), P("barrel", 3.6f, 6.6f, 0f, 1), P("sedan", 6.5f, 1f, 178f, 2)),
            L(18, "Moving Day", 120f, 2500, Kit("m80", 3, "dynamite", 1, "blackcat", 6),
                P("boxtruck", 6.5f, 1f, 180f), P("pickup", -6f, -13.4f, 90f), P("crate", 3.2f, 4f), P("crate", 3.3f, 5f, 25f), P("crate", 2.4f, 4.5f, 50f), P("bin", -9f, 10.4f), P("bin", -8.1f, 10.5f, 0f, 1)).AsBoss(),

            // ---------- Sunset Park: the trailer park ----------
            L(19, "Lawn Life", 70f, 1800, Kit("blackcat", 6, "sparkler", 1),
                P("lawnchair", -3f, 5f, 160f), P("lawnchair", -1.8f, 5.3f, 200f), P("lawnchair", -2.4f, 6.6f, 10f), P("flamingo", 2f, 4f), P("flamingo", 2.6f, 4.4f, 40f), P("flamingo", 3.1f, 3.8f, -30f), P("grill", -5f, 7f, 20f)),
            L(20, "Cookout", 70f, 1900, Kit("blackcat", 4, "m80", 1),
                P("grill", -4f, 6f, 15f), P("grill", 3f, 6.5f, -20f), P("gascyl", -3.2f, 6.4f), P("gascyl", 3.8f, 6.8f), P("lawnchair", 0f, 5f, 180f), P("barrel", 6.5f, 5f)),
            L(21, "Pink Flock", 60f, 2000, Kit("string50", 2, "blackcat", 4),
                P("flamingo", -6f, 4f, 10f), P("flamingo", -4.5f, 5f, -20f), P("flamingo", -3f, 4.2f), P("flamingo", -1.5f, 5.4f, 30f), P("flamingo", 0f, 4.5f), P("flamingo", 1.5f, 5.2f, -15f), P("flamingo", 3f, 4.4f, 25f), P("flamingo", 4.5f, 5f)),
            L(22, "Windy Afternoon", 80f, 2100, Kit("remote", 3, "m80", 2, "blackcat", 4),
                P("pickup", 6.5f, 1f, 175f), P("gascyl", -3f, 6f), P("lawnchair", -1f, 5f, 170f), P("lawnchair", 0.2f, 5.2f, 200f), P("generator", -6f, 8f, 90f)).With(LevelRule.Windy),
            L(23, "Matchbox", 80f, 2200, Kit("blackcat", 6, "sparkler", 2),
                P("crate", -4f, 5f), P("crate", -3f, 5.5f, 20f), P("pallets", -6f, 7f), P("grill", 2f, 6f), P("barrel", 3.5f, 6.5f, 0f, 1), P("lawnchair", 0f, 4f, 180f)).With(LevelRule.Matches),
            L(24, "Generator Trouble", 80f, 2300, Kit("m80", 2, "cherry", 2, "blackcat", 4),
                P("generator", -5f, 7.5f, 90f), P("barrel", -3.6f, 7.8f), P("barrel", -3.2f, 7.1f, 0f, 1), P("gascyl", -6.6f, 6.6f), P("pickup", 6.5f, 1f, 180f), P("grill", 2f, 6f)),
            L(25, "Gnome Village", 75f, 2400, Kit("blackcat", 8),
                P("gnome", 0f, 5f, 180f), P("gnome", -3f, 4f, 160f), P("gnome", -1.8f, 6.5f, 200f), P("gnome", 2f, 4.5f, 190f), P("gnome", 3.2f, 6.2f, 170f), P("flamingo", -4.5f, 6f), P("flamingo", 4.6f, 5f)).With(LevelRule.Spare, 0),
            L(26, "The Big Caravan", 120f, 3200, Kit("remote", 2, "m80", 3, "dynamite", 1, "blackcat", 4),
                P("caravan", 4.5f, 4f, 170f), P("generator", -4f, 8f, 90f), P("gascyl", -2.6f, 8f), P("gascyl", 1.5f, 6.6f), P("grill", -1f, 5f), P("pickup", -6f, -13.4f, 90f), P("lawnchair", -3f, 3f, 150f)).AsBoss(),

            // ---------- Main Street: small-town shops ----------
            L(27, "Opening Hours", 70f, 2500, Kit("rocket", 3, "blackcat", 6, "m80", 1),
                P("newsbox", -6f, 11f), P("newsbox", 5f, 11f), P("bench", -2f, 11.2f), P("bench", 2f, 11.2f), P("cafetable", -4f, 8f), P("cafetable", 3.5f, 8f, 30f), P("cafetable", 0f, 7.6f)),
            L(28, "Cafe Terrace", 70f, 2600, Kit("string50", 1, "blackcat", 4, "m80", 1),
                P("cafetable", -5f, 8f), P("cafetable", -3f, 8.4f, 20f), P("cafetable", -1f, 8f), P("cafetable", 1f, 8.3f, -15f), P("cafetable", 3f, 8f), P("bench", 6f, 11.2f), P("vending", -9f, 11.4f, 180f)),
            L(29, "Hydrant Fun", 75f, 2700, Kit("m80", 3, "blackcat", 4),
                P("hydrant", -5f, -10.8f), P("hydrant", 5f, -10.8f), P("newsbox", 0f, -10.9f), P("bench", -2f, 11.2f), P("scooter", 6.5f, 4f, 90f)),
            L(30, "Rush Hour", 50f, 2800, Kit("m80", 2, "blackcat", 6),
                P("sedan", -5f, -13.4f, 90f, 0), P("scooter", 1f, -12.8f, 90f), P("scooter", 3f, -12.8f, 85f), P("cone", -1f, -15f), P("cone", 0.5f, -15f), P("cone", 2f, -15f), P("cone", 3.5f, -15f)),
            L(31, "Rain on Main", 80f, 2900, Kit("timer", 2, "m80", 2, "blackcat", 6),
                P("vending", -9f, 11.4f, 180f), P("vending", 9f, 11.4f, 180f), P("newsbox", 0f, 11f), P("bench", -3f, 11.2f), P("bin", 3f, 11f), P("bin", 4f, 11f, 0f, 1)).With(LevelRule.Rain),
            L(32, "The Sheriff's Bike", 75f, 3000, Kit("cherry", 3, "blackcat", 4),
                P("bicycle", 0f, 8f, 90f), P("scooter", -4f, 7f, 80f), P("bench", -2f, 11.2f), P("bench", 2f, 11.2f), P("cafetable", 4f, 7.5f), P("cafetable", 5.5f, 8f, 30f)).With(LevelRule.Spare, 0),
            L(33, "Delivery", 80f, 3100, Kit("m80", 2, "blackcat", 4, "sparkler", 1),
                P("pickup", 6.5f, 1f, 180f), P("crate", 3f, 5f), P("crate", 3.1f, 6f, 30f), P("crate", 2.2f, 5.5f), P("crate", -3f, 6f, 15f), P("pallets", -5f, 7f)),
            L(34, "Food Truck Friday", 120f, 4000, Kit("timer", 2, "m80", 3, "dynamite", 1, "blackcat", 6),
                P("foodcart", -5f, 6f, 90f), P("foodcart", 4.5f, 6f, -90f), P("vending", -9f, 11.4f, 180f), P("cafetable", -1.5f, 5f), P("cafetable", 0f, 6f, 20f), P("cafetable", 1.5f, 5f), P("gascyl", -6.4f, 7.4f), P("gascyl", 5.9f, 7.4f)).AsBoss(),

            // ---------- Hillcrest: big modern houses ----------
            L(35, "Poolside", 75f, 3200, Kit("m80", 2, "blackcat", 4),
                P("jacuzzi", -4f, 6f), P("lawnchair", -1f, 5f, 180f), P("lawnchair", 0.3f, 5.3f, 200f), P("lawnchair", 1.6f, 5f, 170f), P("lawnchair", 2.9f, 5.4f, 190f), P("cafetable", 4.5f, 7f)),
            L(36, "Golf Club", 75f, 3300, Kit("m80", 2, "cherry", 2),
                P("golfcart", 6.5f, 2f, 180f), P("golfcart", 6.5f, 6f, 175f), P("bin", -9f, 10f), P("bin", -8.1f, 10.1f, 0f, 2), P("gnome", -3f, 6f, 180f)),
            L(37, "Supercar", 80f, 3500, Kit("m80", 2, "dynamite", 1, "blackcat", 4),
                P("sportscar", 6.5f, 1f, 178f), P("bin", -9f, 10f), P("bin", -8.1f, 10.1f, 0f, 1), P("gascyl", 4f, 7f)),
            L(38, "Windy Hill", 80f, 3600, Kit("remote", 3, "m80", 2, "blackcat", 4),
                P("golfcart", 6.5f, 2f, 180f), P("jacuzzi", -4f, 6f), P("grill", 0f, 7f), P("lawnchair", -1f, 4f, 180f), P("lawnchair", 1f, 4.2f, 160f)).With(LevelRule.Windy),
            L(39, "The Collector", 85f, 3700, Kit("cherry", 3, "blackcat", 6),
                P("sportscar", 6.5f, 1f, 178f), P("pickup", -6f, -13.4f, 90f), P("golfcart", 2.5f, 7f, 160f), P("bin", -9f, 10f), P("bin", -8.1f, 10.1f, 0f, 2), P("grill", -3f, 6f)).With(LevelRule.Spare, 0),
            L(40, "Garden Party", 80f, 3800, Kit("string50", 1, "m80", 2, "blackcat", 4),
                P("grill", -4f, 7f), P("grill", 4f, 7f, 180f), P("cafetable", -2f, 5f), P("cafetable", 0f, 5.5f, 20f), P("cafetable", 2f, 5f), P("cafetable", 0f, 3.5f), P("gascyl", -5f, 7.6f)),
            L(41, "Matches Only", 85f, 3900, Kit("sparkler", 2, "blackcat", 6),
                P("shed", -6f, 8f, 180f), P("pallets", -3.5f, 7f), P("crate", 2f, 6f), P("crate", 3f, 6.4f, 25f), P("barrel", 4.2f, 7f)).With(LevelRule.Matches),
            L(42, "The Neighbour's Fortress", 150f, 5000, Kit("dynamite", 3, "remote", 4, "m80", 3, "keg", 1, "blackcat", 6),
                P("fortress", -5f, 7f, 180f), P("sportscar", 6.5f, 1f, 178f), P("golfcart", 6.5f, 7f, 180f), P("gascyl", -2f, 7f), P("gascyl", -2.4f, 7.5f), P("gascyl", -1.6f, 7.6f), P("barrel", -7.6f, 4.5f), P("barrel", -7f, 4f, 0f, 1)).AsBoss(),

            // ---------- Holly Lane: dressed up for the holidays ----------
            L(43, "Deck the Yard", 70f, 4000, Kit("blackcat", 8),
                P("inflatable", -4f, 7f), P("inflatable", 0f, 8f), P("inflatable", 4f, 7f), P("giftbox", -2f, 5f), P("giftbox", -1.4f, 5.2f, 30f), P("giftbox", 1.6f, 5f), P("giftbox", 2.2f, 5.3f, 50f)),
            L(44, "Presents", 65f, 4100, Kit("string50", 2, "sparkler", 1),
                P("giftbox", -3f, 5f), P("giftbox", -2.3f, 5.1f, 20f), P("giftbox", -1.6f, 5f), P("giftbox", -2.6f, 5.7f, 45f), P("giftbox", 1.6f, 5f), P("giftbox", 2.3f, 5.2f, 10f), P("giftbox", 3f, 5f, 60f), P("giftbox", 2.4f, 5.8f)),
            L(45, "Snowmen", 70f, 4200, Kit("blackcat", 6, "m80", 1),
                P("inflatable", -6f, 6f), P("inflatable", -3.5f, 8f), P("inflatable", -1f, 6f), P("inflatable", 1.5f, 8f), P("inflatable", 4f, 6f), P("inflatable", 6.5f, 8f)),
            L(46, "Holiday Rain", 80f, 4300, Kit("timer", 3, "m80", 2, "blackcat", 6),
                P("inflatable", -4f, 7f), P("giftbox", -2f, 5f), P("giftbox", 2f, 5f), P("sedan", 6.5f, 1f, 178f, 3), P("bin", -9f, 10.4f), P("bin", -8.1f, 10.5f, 0f, 1)).With(LevelRule.Rain),
            L(47, "Leave the Gnome", 75f, 4400, Kit("cherry", 3, "blackcat", 6),
                P("gnome", 0f, 6f, 180f), P("inflatable", -3f, 7f), P("inflatable", 3f, 7f), P("giftbox", -1.6f, 5f), P("giftbox", 1.6f, 5f), P("giftbox", 0f, 4f, 30f)).With(LevelRule.Spare, 0),
            L(48, "Firework Stand", 75f, 4500, Kit("blackcat", 4, "m80", 1),
                P("fireworkstand", -5f, 6f, 90f), P("giftbox", -2f, 5f), P("giftbox", -1.2f, 5.4f, 30f), P("giftbox", 2f, 5f), P("gascyl", 4f, 7f), P("inflatable", 5f, 9f)),
            L(49, "Last Light", 100f, 4800, Kit("m80", 3, "dynamite", 1, "blackcat", 6),
                P("pickup", 6.5f, 1f, 178f), P("sedan", -6f, -13.4f, 90f, 2), P("inflatable", -4f, 8f), P("inflatable", -1f, 8.5f), P("inflatable", 2f, 8f), P("giftbox", -2f, 5f), P("giftbox", 0f, 5f, 40f), P("giftbox", 2f, 5f), P("grill", -6f, 6f), P("gascyl", -6.6f, 6.6f)),
            L(50, "Corner Gas", 150f, 8000, Kit("remote", 4, "timer", 2, "dynamite", 2, "keg", 1, "blackcat", 6),
                P("fuelpump", -3f, 6f), P("fuelpump", 1f, 6f), P("fueltruck", 6.5f, 2f, 180f), P("barrel", -6f, 8f), P("barrel", -5.4f, 8.4f, 0f, 1), P("gascyl", 3f, 9f), P("gascyl", 3.5f, 9.3f), P("newsbox", -8f, 10.5f)).AsBoss(),
        };

        public const int DailyReward = 300;

        /// <summary>Seconds back on the clock for every target destroyed.</summary>
        public const float TargetBonusSeconds = 3f;

        /// <summary>
        /// The clocks above were too tight to clear (the owner's call): every level gets half as much time again,
        /// at least a minute and a half, rounded to 5 s. Each target destroyed also gives
        /// <see cref="TargetBonusSeconds"/> back.
        /// </summary>
        public static float Relaxed(float time) { return (float)(Math.Round(Math.Max(90.0, time * 1.5) / 5.0) * 5.0); }

        static Levels()
        {
            foreach (var l in All) if (!l.Tutorial) l.Time = Relaxed(l.Time);
        }

        /// <summary>
        /// The big city: a whole downtown to wreck and paint, no targets and no clock. Every kind of firework,
        /// M-80s and the Black Cat come free; everything unlocked in the shop is there too, all unlimited. Pays for the wreckage
        /// when the player cashes in.
        /// </summary>
        public static LevelDef City()
        {
            return new LevelDef { Number = 0, Name = "Big City", Time = 3600f, Reward = 0, Props = new PropSpot[0], Kit = Kit("rocket", 1, "cake", 1, "mortar", 1, "candle", 1, "fountain", 1, "m80", 1, "blackcat", 1), Show = true };
        }

        /// <summary>
        /// Today's challenge (the same for everyone on a date): one of the levels from 2 on, 15% less time, and a
        /// twist if it has none of its own. Pays <see cref="DailyReward"/> coins the first time it is cleared today.
        /// </summary>
        public static LevelDef DailyChallenge(int daySeed)
        {
            var r = new System.Random(daySeed * 7919 + 13);
            var b = All[1 + r.Next(All.Length - 1)];
            var rules = new[] { LevelRule.Rain, LevelRule.Windy, LevelRule.Matches, LevelRule.None };
            var rule = b.Rule != LevelRule.None ? b.Rule : rules[r.Next(rules.Length)];
            return new LevelDef
            {
                Number = b.Number, Name = b.Name, Time = (float)Math.Round(b.Time * 0.85f), Props = b.Props, Kit = b.Kit,
                Reward = DailyReward, Rule = rule, Spare = b.Spare, Boss = b.Boss, Daily = true,
            };
        }

        public static LevelDef Get(int number)
        {
            foreach (var l in All) if (l.Number == number) return l;
            return All[0];
        }
    }
}
