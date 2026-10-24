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

    /// <summary>
    /// One level on the map: destroy every prop before the clock runs out. Stars by how much time is left;
    /// the kit is what the player gets for the level, on top of anything bought in the shop.
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

        /// <summary>★★★ with 40% of the time left, ★★ with 15%, ★ for clearing it at all.</summary>
        public int Stars(bool cleared, float timeLeft)
        {
            if (!cleared) return 0;
            if (timeLeft >= Time * 0.4f) return 3;
            if (timeLeft >= Time * 0.15f) return 2;
            return 1;
        }
    }

    /// <summary>The level map. Each level reuses the street set and changes what stands in it.</summary>
    public static class Levels
    {
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
                Kit = Kit("blackcat", 8, "m80", 4, "sparkler", 2, "dynamite", 1) },
        };

        public static LevelDef Get(int number)
        {
            foreach (var l in All) if (l.Number == number) return l;
            return All[0];
        }
    }
}
