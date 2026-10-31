namespace BoomYard.Core
{
    public enum HouseKind { Suburban, Terrace, Mobile, Shops, Modern, Bungalow }
    public enum FenceKind { Timber, LowWall, ChainLink, None, Hedge, Picket }

    /// <summary>
    /// One street of the map. Every street keeps the same stage (the player starts on the pavement facing a
    /// front yard 26 m wide, the road behind), so a level's props stand in the same places on any of them; the
    /// street changes what surrounds them: the houses, the fences, the ground, the parked cars and the dressing.
    /// Colours are RGB in 0..1.
    /// </summary>
    public sealed class StreetDef
    {
        public string Id, Name, Blurb;
        public HouseKind Houses;
        public FenceKind Fence;
        public float[][] Walls;        // house wall colours, used in turn
        public float[] Roof;
        public float[] Trim;
        public bool PavedYard;         // a forecourt or car park instead of a lawn
        public bool Gravel;            // a gravel yard (the trailer park)
        public bool HolidayLights;     // string lights on the eaves and in the trees
        public bool KerbCars;          // cars parked along the far kerb
        public int Trees = 12;         // trees near the yard (the horizon ring is always there)
        public float SunYaw;           // turns the golden-hour sun a little, so streets don't all look alike
        public int FirstLevel, LastLevel;
    }

    public static class Streets
    {
        static float[] C(float r, float g, float b) { return new[] { r, g, b }; }

        public static readonly StreetDef[] All =
        {
            new StreetDef { Id = "maple", Name = "Maple Drive", Blurb = "Two-storey family homes, lawns and driveways.",
                Houses = HouseKind.Suburban, Fence = FenceKind.Timber, FirstLevel = 1, LastLevel = 10,
                Walls = new[] { C(0.93f, 0.88f, 0.8f), C(0.78f, 0.82f, 0.86f), C(0.9f, 0.8f, 0.68f), C(0.85f, 0.86f, 0.8f), C(0.92f, 0.84f, 0.78f) },
                Roof = C(0.75f, 0.62f, 0.55f), Trim = C(0.95f, 0.94f, 0.9f) },
            new StreetDef { Id = "oak", Name = "Oak Row", Blurb = "Brick terraces, small front gardens, cars on the kerb.",
                Houses = HouseKind.Terrace, Fence = FenceKind.LowWall, KerbCars = true, Trees = 6, SunYaw = -12f, FirstLevel = 11, LastLevel = 18,
                Walls = new[] { C(0.62f, 0.33f, 0.25f), C(0.55f, 0.3f, 0.24f), C(0.7f, 0.42f, 0.3f), C(0.6f, 0.36f, 0.28f) },
                Roof = C(0.32f, 0.3f, 0.32f), Trim = C(0.93f, 0.92f, 0.88f) },
            new StreetDef { Id = "sunset", Name = "Sunset Park", Blurb = "A trailer park: mobile homes, gravel and chain-link.",
                Houses = HouseKind.Mobile, Fence = FenceKind.ChainLink, Gravel = true, Trees = 8, SunYaw = 14f, FirstLevel = 19, LastLevel = 26,
                Walls = new[] { C(0.93f, 0.91f, 0.84f), C(0.86f, 0.9f, 0.88f), C(0.95f, 0.88f, 0.76f), C(0.88f, 0.86f, 0.9f) },
                Roof = C(0.7f, 0.7f, 0.68f), Trim = C(0.45f, 0.55f, 0.6f) },
            new StreetDef { Id = "main", Name = "Main Street", Blurb = "Small-town shops with awnings and a car park.",
                Houses = HouseKind.Shops, Fence = FenceKind.None, PavedYard = true, KerbCars = true, Trees = 4, SunYaw = -6f, FirstLevel = 27, LastLevel = 34,
                Walls = new[] { C(0.8f, 0.5f, 0.38f), C(0.9f, 0.86f, 0.76f), C(0.5f, 0.6f, 0.58f), C(0.86f, 0.72f, 0.5f) },
                Roof = C(0.4f, 0.38f, 0.36f), Trim = C(0.95f, 0.93f, 0.88f) },
            new StreetDef { Id = "hill", Name = "Hillcrest", Blurb = "Big modern houses, tall hedges and expensive cars.",
                Houses = HouseKind.Modern, Fence = FenceKind.Hedge, Trees = 14, SunYaw = 8f, FirstLevel = 35, LastLevel = 42,
                Walls = new[] { C(0.95f, 0.95f, 0.93f), C(0.82f, 0.8f, 0.76f), C(0.9f, 0.9f, 0.88f) },
                Roof = C(0.25f, 0.25f, 0.27f), Trim = C(0.2f, 0.2f, 0.22f) },
            new StreetDef { Id = "holly", Name = "Holly Lane", Blurb = "Bungalows dressed up for the holidays.",
                Houses = HouseKind.Bungalow, Fence = FenceKind.Picket, HolidayLights = true, KerbCars = true, Trees = 12, SunYaw = -16f, FirstLevel = 43, LastLevel = 50,
                Walls = new[] { C(0.86f, 0.9f, 0.92f), C(0.94f, 0.9f, 0.82f), C(0.8f, 0.86f, 0.78f), C(0.92f, 0.86f, 0.86f) },
                Roof = C(0.45f, 0.24f, 0.2f), Trim = C(0.97f, 0.97f, 0.95f) },
            new StreetDef { Id = "old", Name = "Old Quarter", Blurb = "Derelict tenements, due for demolition. Bring dynamite.",
                Houses = HouseKind.Terrace, Fence = FenceKind.ChainLink, Gravel = true, KerbCars = true, Trees = 4, SunYaw = 10f, FirstLevel = 51, LastLevel = 53,
                Walls = new[] { C(0.55f, 0.38f, 0.3f), C(0.5f, 0.34f, 0.28f), C(0.6f, 0.45f, 0.36f) },
                Roof = C(0.3f, 0.28f, 0.28f), Trim = C(0.75f, 0.72f, 0.66f) },
        };

        public static StreetDef Get(string id)
        {
            foreach (var s in All) if (s.Id == id) return s;
            return All[0];
        }

        public static StreetDef ForLevel(int number)
        {
            foreach (var s in All) if (number >= s.FirstLevel && number <= s.LastLevel) return s;
            return All[0];
        }
    }
}
