using System.Collections.Generic;

namespace BoomYard.Core
{
    public enum Material { Glass, Wood, Plastic, Metal, Concrete, Carbon, Aluminium, Cardboard, Acrylic }

    /// <summary>What an object does to its surroundings when it is destroyed (spec §06). Flags: a van both explodes and burns.</summary>
    [System.Flags]
    public enum Reaction
    {
        None = 0,
        Explode = 1,     // secondary blast after a short burn (fuel)
        Launch = 2,      // flies as a rigid body; mass x speed damages what it hits
        Burn = 4,        // spreads fire to flammables within 2 m every 0.8 s
        Shatter = 8,     // breaks from the blast wave, not just the hit
        Collapse = 16,   // whatever stands on it falls and counts
        Douse = 32,      // water puts fire out around it
        Contain = 64,    // an explosive placed inside/under it gets the containment multiplier
    }

    public enum Family { Blast, Push, Fire, Timed }

    /// <summary>How the explosive got to where it went off (spec §07 PlacementMult).</summary>
    public enum Placement { Thrown, Placed, Contained, Airburst }

    /// <summary>One item of the arsenal (spec §08). Times are real game seconds and shown to the player.</summary>
    public sealed class ExplosiveDef
    {
        public string Id, NameHe, NameEn;
        public Family Family;
        public float Fuse;          // seconds from catching to detonation (0 = on contact)
        public float Damage;        // per unit
        public int Units = 1;       // sub-charges (string of 50, cake of 36, cluster of 8)
        public float UnitInterval;  // seconds between sub-charges
        public float Radius;        // metres
        public int Price;           // dollars per unit in the shop (0 = free each round)
        public int FreePerRound;
        public int UnlockArena;     // 1-based; 0 = from the start
        public int UnlockStars;     // stars needed in UnlockArena (0 = just reach it)
        public bool Throwable = true;
        public bool BreaksConcrete;
        public float BurnSeconds;   // fire left behind
        public float ShrapnelRadius;
        public bool Rolls;
        public float Mass = 0.08f;  // kg, for throwing
        public bool Timer;          // placed, it counts down by itself (no lighter); Fuse is the default delay
        public bool Remote;         // placed, it waits for the detonator button
        public string Firework;     // a firework: "rocket" (flies up) or a ground kind FireworkDevice fires; null for the rest
        public bool Sticky;         // thrown, it sticks to whatever it hits
        public string Note;
    }

    /// <summary>One destructible object (spec §09).</summary>
    public sealed class ObjectDef
    {
        public string Id, NameHe, NameEn;
        public float Hp;
        public int Value;           // dollars at full destruction; partial damage pays a share
        public Material Material;
        public Reaction Reaction;
        public float BlastRadius;   // for Explode
        public float BlastDamage;
        public float BlastDelay;    // burn time before the secondary blast
        public float BurnSeconds;   // for Burn
        public int ShopPrice;       // 0 = not sold (arena only)
        public bool ThermiteOnly;
        public float Mass = 50f;
    }

    public sealed class LighterDef
    {
        public string Id, NameHe, NameEn;
        public float CatchTime;     // seconds the flame must touch the fuse
        public float WindBlowout;   // chance per ignition attempt outdoors, facing the wind
        public int Flames = 1;      // turbo lights two fuses at once
        public int GemPrice;
        public float BreakChance;   // matches: 1 in 3 snaps
    }

    public sealed class ArenaDef
    {
        public int Number;
        public string Id, NameHe, NameEn, TeachesHe, TeachesEn;
        public long ThreeStars;     // dollars for ★★★
        public int UnlockStars;     // total stars needed to open
        public float EchoMult = 1f; // enclosed spaces strengthen the blast wave
        public bool Outdoors = true;
    }

    /// <summary>All game data from the spec in one place. A new item is a new entry here.</summary>
    public static class Catalog
    {
        public static readonly ExplosiveDef[] Explosives =
        {
            new ExplosiveDef { Id = "blackcat", NameHe = "נפץ קטן", NameEn = "Black Cat", Family = Family.Blast, Fuse = 2.5f, Damage = 60, Radius = 0.8f, Price = 0, FreePerRound = 4, Note = "The basics. Windows, bins, containment in a mailbox." },
            new ExplosiveDef { Id = "string50", NameHe = "מחרוזת 50", NameEn = "Firecracker string", Family = Family.Blast, Fuse = 1.5f, Damage = 20, Units = 50, UnitInterval = 0.1f, Radius = 0.5f, Price = 900, Note = "5 seconds of bangs along a line." },
            new ExplosiveDef { Id = "sparkler", NameHe = "חזיז", NameEn = "Sparkler bomb", Family = Family.Fire, Fuse = 3f, Damage = 40, Radius = 1f, Price = 400, BurnSeconds = 8f, Note = "Burns 8 s in place, lights anything flammable." },
            new ExplosiveDef { Id = "rocket", NameHe = "רקטה", NameEn = "Rocket", Family = Family.Push, Fuse = 2.5f, Damage = 160, Radius = 1.4f, Price = 350, Mass = 0.1f, Firework = "rocket", Note = "Light it and throw: it flies where you aim and bursts on impact. Light one standing on the ground and it goes up as a firework." },
            new ExplosiveDef { Id = "m80", NameHe = "M-80", NameEn = "M-80", Family = Family.Blast, Fuse = 3f, Damage = 400, Radius = 2f, Price = 1800, UnlockArena = 2, Mass = 0.12f, Note = "First item that pops a fuel tank. Best placed under a car." },
            new ExplosiveDef { Id = "cherry", NameHe = "Cherry bomb", NameEn = "Cherry bomb", Family = Family.Blast, Fuse = 2f, Damage = 250, Radius = 1.5f, Price = 1100, UnlockArena = 2, Rolls = true, Note = "Round, rolls down slopes to where it is needed." },
            new ExplosiveDef { Id = "cake36", NameHe = "עוגת זיקוקים 36", NameEn = "Cake 36", Family = Family.Push, Fuse = 2f, Damage = 50, Units = 36, UnitInterval = 6f / 36f, Radius = 10f, Price = 2400, UnlockArena = 3, Throwable = false, Mass = 1.5f, Note = "Planned chaos. Tip it over for horizontal fire." },
            new ExplosiveDef { Id = "molotov", NameHe = "מולוטוב", NameEn = "Molotov", Family = Family.Fire, Fuse = 0f, Damage = 60, Radius = 2.5f, Price = 600, UnlockArena = 3, BurnSeconds = 8f, Mass = 0.6f, Note = "Light the rag and throw at once: 8 s fire puddle." },
            new ExplosiveDef { Id = "keg", NameHe = "חבית אבק", NameEn = "Powder keg", Family = Family.Blast, Fuse = 6f, Damage = 1500, Radius = 4f, Price = 6500, UnlockArena = 3, UnlockStars = 3, Throwable = false, Mass = 25f, Note = "Roll it, light it, run. 6 s to set up a chain." },
            new ExplosiveDef { Id = "cluster", NameHe = "Cluster (ענב)", NameEn = "Cluster", Family = Family.Blast, Fuse = 2f, Damage = 100, Units = 8, UnitInterval = 0.5f, Radius = 1f, Price = 3200, UnlockArena = 4, Note = "Splits into 8 in the air." },
            new ExplosiveDef { Id = "dynamite", NameHe = "דינמיט", NameEn = "Dynamite", Family = Family.Blast, Fuse = 4f, Damage = 2500, Radius = 3.5f, Price = 9000, UnlockArena = 4, BreaksConcrete = true, Mass = 0.25f, Note = "The only thing that breaks concrete at full radius." },
            new ExplosiveDef { Id = "pipe", NameHe = "פצצת צינור", NameEn = "Pipe bomb", Family = Family.Blast, Fuse = 5f, Damage = 1800, Radius = 2.5f, ShrapnelRadius = 8f, Price = 7000, UnlockArena = 5, Mass = 0.8f, Note = "Shrapnel shatters glass in a huge radius." },
            new ExplosiveDef { Id = "detcord", NameHe = "Det cord", NameEn = "Det cord", Family = Family.Timed, Fuse = 0f, Damage = 300, Radius = 0.6f, Price = 4000, UnlockArena = 5, Throwable = false, Note = "String it between objects, light one end: all at once." },
            new ExplosiveDef { Id = "timer", NameHe = "מטען שעון", NameEn = "Timer charge", Family = Family.Timed, Fuse = 10f, Damage = 500, Radius = 2.2f, Price = 0, Throwable = false, Timer = true, Mass = 0.6f, Note = "Place it and walk away: it goes off when the clock hits zero. Tap it again in the pocket for 5, 10 or 15 s." },
            new ExplosiveDef { Id = "remote", NameHe = "מטען שלט", NameEn = "Remote charge", Family = Family.Timed, Fuse = 0f, Damage = 450, Radius = 2f, Price = 0, Throwable = false, Remote = true, Mass = 0.5f, Note = "Place as many as you like, then press BOOM to set them all off at once." },
            // Fireworks that stay on the ground and fire into the sky (FireworkDevice).
            new ExplosiveDef { Id = "cake", NameHe = "עוגת זיקוקים", NameEn = "Firework cake", Family = Family.Push, Fuse = 3f, Damage = 30, Radius = 0.8f, Throwable = false, Mass = 1.2f, Firework = "cake", Note = "Sixteen shells, one after another: rings, crackles, willows." },
            new ExplosiveDef { Id = "mortar", NameHe = "פגז זיקוקים", NameEn = "Mortar shell", Family = Family.Push, Fuse = 3f, Damage = 60, Radius = 1f, Throwable = false, Mass = 0.8f, Firework = "mortar", Note = "One huge shell, high over the rooftops." },
            new ExplosiveDef { Id = "candle", NameHe = "נר רומי", NameEn = "Roman candle", Family = Family.Push, Fuse = 2.5f, Damage = 20, Radius = 0.5f, Throwable = false, Mass = 0.3f, Firework = "candle", Note = "Eight coloured balls, one every half second." },
            new ExplosiveDef { Id = "fountain", NameHe = "מזרקת זיקוקים", NameEn = "Fountain", Family = Family.Fire, Fuse = 2f, Damage = 20, Radius = 0.6f, Throwable = false, Mass = 0.4f, Firework = "fountain", Note = "A cone you set down and light: it sprays a fountain of gold sparks 3 m high for eight seconds, and sets fire to anything flammable right next to it." },
            // More fireworks.
            new ExplosiveDef { Id = "whistler", NameHe = "רקטה שורקת", NameEn = "Whistling rocket", Family = Family.Push, Fuse = 2.5f, Damage = 140, Radius = 1.3f, Mass = 0.1f, Firework = "rocket", Note = "Screams all the way up, then bursts in strobes and crackles." },
            new ExplosiveDef { Id = "bigrocket", NameHe = "רקטה גדולה", NameEn = "Big rocket", Family = Family.Push, Fuse = 3f, Damage = 300, Radius = 2f, Mass = 0.3f, Firework = "rocket", Note = "A heavy rocket with a shell half as big again." },
            new ExplosiveDef { Id = "spinner", NameHe = "סביבון", NameEn = "Ground spinner", Family = Family.Fire, Fuse = 2f, Damage = 15, Radius = 0.6f, Throwable = false, Mass = 0.05f, Firework = "spinner", Note = "Whirls across the ground in a ring of sparks, then pops." },
            new ExplosiveDef { Id = "ufo", NameHe = "צלחת מעופפת", NameEn = "Flying saucer", Family = Family.Push, Fuse = 2f, Damage = 30, Radius = 0.8f, Throwable = false, Mass = 0.05f, Firework = "ufo", Note = "Spins, lifts off in a whistle of sparks and bursts up high." },
            new ExplosiveDef { Id = "smoke", NameHe = "פצצת עשן", NameEn = "Smoke bomb", Family = Family.Fire, Fuse = 1.5f, Damage = 0, Radius = 0.5f, Mass = 0.1f, Firework = "smoke", Note = "Ten seconds of thick coloured smoke. Harmless, and great in photos." },
            new ExplosiveDef { Id = "strobepot", NameHe = "סיר הבזקים", NameEn = "Strobe pot", Family = Family.Fire, Fuse = 2f, Damage = 10, Radius = 0.5f, Throwable = false, Mass = 0.2f, Firework = "strobepot", Note = "Six seconds of blinding white flashes on the ground." },
            new ExplosiveDef { Id = "saturn", NameHe = "סוללת טילים", NameEn = "Saturn battery", Family = Family.Push, Fuse = 3f, Damage = 40, Radius = 1f, Throwable = false, Mass = 0.6f, Firework = "saturn", Note = "Thirty little whistling missiles fanning out, each with a crack." },
            new ExplosiveDef { Id = "willowcake", NameHe = "עוגת ערבה", NameEn = "Willow cake", Family = Family.Push, Fuse = 3f, Damage = 30, Radius = 0.8f, Throwable = false, Mass = 1.2f, Firework = "willowcake", Note = "Twelve golden willows that hang over the street." },
            new ExplosiveDef { Id = "finale", NameHe = "עוגת פינאלה", NameEn = "Finale cake", Family = Family.Push, Fuse = 3f, Damage = 60, Radius = 1f, Throwable = false, Mass = 2f, Firework = "finale", Note = "Thirty-six shells in twelve seconds: the end of the show." },
            new ExplosiveDef { Id = "bigmortar", NameHe = "מרגמה ענקית", NameEn = "Giant mortar", Family = Family.Push, Fuse = 4f, Damage = 120, Radius = 1.4f, Throwable = false, Mass = 2f, Firework = "bigmortar", Note = "The biggest shell there is: it fills the sky." },
            // More bangs.
            new ExplosiveDef { Id = "megacracker", NameHe = "מגה נפץ", NameEn = "Mega cracker", Family = Family.Blast, Fuse = 2f, Damage = 250, Radius = 1.6f, Mass = 0.08f, Note = "A Black Cat four times over." },
            new ExplosiveDef { Id = "bigbang", NameHe = "כדור ביג בנג", NameEn = "Big Bang ball", Family = Family.Blast, Fuse = 3f, Damage = 900, Radius = 2.8f, Rolls = true, Mass = 0.4f, Note = "A heavy ball that rolls where it is thrown, then goes off big." },
            new ExplosiveDef { Id = "string100", NameHe = "מחרוזת דרקון", NameEn = "Dragon string", Family = Family.Blast, Fuse = 1.5f, Damage = 25, Units = 100, UnitInterval = 0.05f, Radius = 0.6f, Mass = 0.3f, Note = "A hundred crackers in five seconds." },
            new ExplosiveDef { Id = "sticky", NameHe = "פצצה דביקה", NameEn = "Sticky bomb", Family = Family.Blast, Fuse = 3f, Damage = 700, Radius = 2f, Mass = 0.3f, Sticky = true, Note = "Thrown, it sticks to whatever it hits: a car, a wall, a sign." },
            new ExplosiveDef { Id = "tnt", NameHe = "חבילת TNT", NameEn = "TNT bundle", Family = Family.Blast, Fuse = 5f, Damage = 3200, Radius = 4f, BreaksConcrete = true, Mass = 1.5f, Note = "Six sticks taped together. Run." },
            new ExplosiveDef { Id = "thermite", NameHe = "Thermite", NameEn = "Thermite", Family = Family.Fire, Fuse = 3f, Damage = 80, Units = 12, UnitInterval = 1f, Radius = 0.5f, Price = 5500, UnlockArena = 6, Throwable = false, Note = "Burns through metal: opens tanks, lights engines." },
        };

        public static readonly ObjectDef[] Objects =
        {
            new ObjectDef { Id = "window", NameHe = "חלון", NameEn = "Window", Hp = 5, Value = 350, Material = Material.Glass, Reaction = Reaction.Shatter, Mass = 8 },
            new ObjectDef { Id = "bin", NameHe = "פח אשפה", NameEn = "Trash bin", Hp = 25, Value = 600, Material = Material.Metal, Reaction = Reaction.Launch | Reaction.Contain, Mass = 15 },
            new ObjectDef { Id = "mailbox", NameHe = "תא דואר", NameEn = "Mailbox", Hp = 15, Value = 900, Material = Material.Metal, Reaction = Reaction.Contain | Reaction.Launch, Mass = 10 },
            new ObjectDef { Id = "fence", NameHe = "גדר רשת", NameEn = "Chain-link fence", Hp = 40, Value = 1200, Material = Material.Metal, Reaction = Reaction.None, Mass = 30 },
            new ObjectDef { Id = "pallets", NameHe = "משטחי עץ ×6", NameEn = "Wood pallets x6", Hp = 20, Value = 480, Material = Material.Wood, Reaction = Reaction.Burn, BurnSeconds = 12f, Mass = 40 },
            new ObjectDef { Id = "bicycle", NameHe = "אופניים", NameEn = "Bicycle", Hp = 30, Value = 2800, Material = Material.Metal, Reaction = Reaction.Launch, Mass = 14 },
            new ObjectDef { Id = "billboard", NameHe = "שלט חוצות", NameEn = "Billboard", Hp = 90, Value = 14000, Material = Material.Metal, Reaction = Reaction.Collapse, Mass = 400 },
            new ObjectDef { Id = "acunit", NameHe = "מזגן חיצוני", NameEn = "AC unit", Hp = 40, Value = 3900, Material = Material.Metal, Reaction = Reaction.Explode, BlastRadius = 1.5f, BlastDamage = 120, BlastDelay = 0.4f, ShopPrice = 3000, Mass = 45 },
            new ObjectDef { Id = "gascyl", NameHe = "בלון גז 12ק\"ג", NameEn = "Gas cylinder 12kg", Hp = 35, Value = 420, Material = Material.Metal, Reaction = Reaction.Explode | Reaction.Launch, BlastRadius = 5f, BlastDamage = 600, BlastDelay = 0.8f, ShopPrice = 800, Mass = 25 },
            new ObjectDef { Id = "motorbike", NameHe = "אופנוע", NameEn = "Motorbike", Hp = 120, Value = 12000, Material = Material.Metal, Reaction = Reaction.Launch | Reaction.Explode, BlastRadius = 3f, BlastDamage = 500, BlastDelay = 1.2f, ShopPrice = 9000, Mass = 180 },
            new ObjectDef { Id = "van", NameHe = "טרנזיט ישן", NameEn = "Old van", Hp = 300, Value = 18000, Material = Material.Metal, Reaction = Reaction.Explode | Reaction.Burn | Reaction.Contain, BlastRadius = 5f, BlastDamage = 900, BlastDelay = 1.5f, BurnSeconds = 20f, ShopPrice = 12000, Mass = 1800 },
            new ObjectDef { Id = "sedan", NameHe = "סדאן משפחתי", NameEn = "Family sedan", Hp = 280, Value = 22000, Material = Material.Metal, Reaction = Reaction.Explode | Reaction.Burn | Reaction.Contain, BlastRadius = 5f, BlastDamage = 900, BlastDelay = 1.5f, BurnSeconds = 20f, ShopPrice = 20000, Mass = 1400 },
            new ObjectDef { Id = "sportscar", NameHe = "רכב ספורט", NameEn = "Sports car", Hp = 260, Value = 60000, Material = Material.Carbon, Reaction = Reaction.Explode | Reaction.Burn | Reaction.Contain, BlastRadius = 5f, BlastDamage = 900, BlastDelay = 0.5f, BurnSeconds = 15f, ShopPrice = 60000, Mass = 1300 },
            new ObjectDef { Id = "caravan", NameHe = "קרוואן", NameEn = "Caravan", Hp = 350, Value = 30000, Material = Material.Aluminium, Reaction = Reaction.Explode | Reaction.Burn, BlastRadius = 5f, BlastDamage = 700, BlastDelay = 2f, BurnSeconds = 25f, ShopPrice = 28000, Mass = 1500 },
            new ObjectDef { Id = "generator", NameHe = "גנרטור דיזל", NameEn = "Diesel generator", Hp = 150, Value = 12000, Material = Material.Metal, Reaction = Reaction.Explode, BlastRadius = 4f, BlastDamage = 700, BlastDelay = 1.2f, Mass = 600 },
            new ObjectDef { Id = "fuelpump", NameHe = "משאבת דלק", NameEn = "Fuel pump", Hp = 140, Value = 20000, Material = Material.Metal, Reaction = Reaction.Explode | Reaction.Burn, BlastRadius = 8f, BlastDamage = 1500, BlastDelay = 1f, BurnSeconds = 20f, Mass = 500 },
            new ObjectDef { Id = "fueltruck", NameHe = "משאית דלק", NameEn = "Fuel truck", Hp = 800, Value = 60000, Material = Material.Metal, Reaction = Reaction.Explode | Reaction.Burn, BlastRadius = 12f, BlastDamage = 3000, BlastDelay = 2f, BurnSeconds = 30f, Mass = 12000 },
            new ObjectDef { Id = "solarpanel", NameHe = "פאנל סולארי", NameEn = "Solar panel", Hp = 8, Value = 4200, Material = Material.Glass, Reaction = Reaction.Shatter, ShopPrice = 25000, Mass = 20 },
            new ObjectDef { Id = "storefront", NameHe = "לוח ויטרינה", NameEn = "Storefront pane", Hp = 10, Value = 6000, Material = Material.Glass, Reaction = Reaction.Shatter, Mass = 60 },
            new ObjectDef { Id = "warehouse", NameHe = "מחסן סחורה", NameEn = "Warehouse", Hp = 500, Value = 120000, Material = Material.Cardboard, Reaction = Reaction.Burn | Reaction.Collapse, BurnSeconds = 40f, Mass = 5000 },
            new ObjectDef { Id = "scaffold", NameHe = "פיגומים 3 קומות", NameEn = "Scaffolding", Hp = 60, Value = 38000, Material = Material.Metal, Reaction = Reaction.Collapse, Mass = 800 },
            new ObjectDef { Id = "barrier", NameHe = "בטונדות", NameEn = "Concrete barrier", Hp = 400, Value = 3500, Material = Material.Concrete, Reaction = Reaction.None, Mass = 1000 },
            new ObjectDef { Id = "watertank", NameHe = "מיכל מים 10 קוב", NameEn = "Water tank", Hp = 200, Value = 15000, Material = Material.Plastic, Reaction = Reaction.Douse, BlastRadius = 4f, Mass = 300 },
            new ObjectDef { Id = "jacuzzi", NameHe = "ג'קוזי", NameEn = "Jacuzzi", Hp = 100, Value = 15000, Material = Material.Acrylic, Reaction = Reaction.Douse, BlastRadius = 3f, ShopPrice = 11000, Mass = 400 },
            new ObjectDef { Id = "container", NameHe = "מכולת שילוח", NameEn = "Shipping container", Hp = 500, Value = 9000, Material = Material.Metal, Reaction = Reaction.Launch | Reaction.Contain, Mass = 3800 },
            new ObjectDef { Id = "foodcart", NameHe = "עגלת מזון", NameEn = "Food cart", Hp = 90, Value = 15000, Material = Material.Metal, Reaction = Reaction.Explode | Reaction.Burn, BlastRadius = 4f, BlastDamage = 600, BlastDelay = 1f, BurnSeconds = 15f, ShopPrice = 14000, Mass = 350 },
            new ObjectDef { Id = "fireworkstand", NameHe = "מתקן זיקוקים", NameEn = "Firework stand", Hp = 60, Value = 12000, Material = Material.Cardboard, Reaction = Reaction.Explode | Reaction.Burn, BlastRadius = 6f, BlastDamage = 300, BlastDelay = 0.5f, BurnSeconds = 10f, ShopPrice = 18000, Mass = 120 },
            new ObjectDef { Id = "barrel", NameHe = "חבית שמן", NameEn = "Oil drum", Hp = 50, Value = 1500, Material = Material.Metal, Reaction = Reaction.Explode | Reaction.Launch | Reaction.Burn, BlastRadius = 3.5f, BlastDamage = 500, BlastDelay = 0.9f, BurnSeconds = 10f, Mass = 60 },
            new ObjectDef { Id = "crate", NameHe = "ארגז עץ", NameEn = "Wooden crate", Hp = 25, Value = 800, Material = Material.Wood, Reaction = Reaction.Burn | Reaction.Launch, BurnSeconds = 8f, Mass = 40 },
            // Street objects (the map's streets).
            new ObjectDef { Id = "garagedoor", NameHe = "דלת מוסך", NameEn = "Garage door", Hp = 70, Value = 2500, Material = Material.Metal, Reaction = Reaction.Launch, Mass = 80 },
            new ObjectDef { Id = "pickup", NameHe = "טנדר", NameEn = "Pickup truck", Hp = 320, Value = 16000, Material = Material.Metal, Reaction = Reaction.Explode | Reaction.Burn | Reaction.Contain, BlastRadius = 5f, BlastDamage = 900, BlastDelay = 1.5f, BurnSeconds = 20f, Mass = 1900 },
            new ObjectDef { Id = "boxtruck", NameHe = "משאית הובלה", NameEn = "Removal truck", Hp = 650, Value = 30000, Material = Material.Metal, Reaction = Reaction.Explode | Reaction.Burn | Reaction.Contain, BlastRadius = 6f, BlastDamage = 1200, BlastDelay = 1.8f, BurnSeconds = 25f, Mass = 6000 },
            new ObjectDef { Id = "grill", NameHe = "מנגל גז", NameEn = "Gas grill", Hp = 30, Value = 1400, Material = Material.Metal, Reaction = Reaction.Explode | Reaction.Launch, BlastRadius = 2.5f, BlastDamage = 300, BlastDelay = 0.6f, Mass = 30 },
            new ObjectDef { Id = "gnome", NameHe = "גמד גינה", NameEn = "Garden gnome", Hp = 6, Value = 400, Material = Material.Plastic, Reaction = Reaction.Launch, Mass = 4 },
            new ObjectDef { Id = "flamingo", NameHe = "פלמינגו פלסטיק", NameEn = "Plastic flamingo", Hp = 3, Value = 150, Material = Material.Plastic, Reaction = Reaction.Launch, Mass = 1 },
            new ObjectDef { Id = "hydrant", NameHe = "ברז כיבוי", NameEn = "Fire hydrant", Hp = 140, Value = 3000, Material = Material.Metal, Reaction = Reaction.Douse, BlastRadius = 3f, Mass = 150 },
            new ObjectDef { Id = "newsbox", NameHe = "תיבת עיתונים", NameEn = "Newspaper box", Hp = 30, Value = 1000, Material = Material.Metal, Reaction = Reaction.Contain | Reaction.Launch, Mass = 30 },
            new ObjectDef { Id = "vending", NameHe = "מכונת שתייה", NameEn = "Vending machine", Hp = 120, Value = 7000, Material = Material.Metal, Reaction = Reaction.Launch | Reaction.Contain, Mass = 300 },
            new ObjectDef { Id = "bench", NameHe = "ספסל", NameEn = "Park bench", Hp = 40, Value = 1100, Material = Material.Wood, Reaction = Reaction.Burn | Reaction.Launch, BurnSeconds = 10f, Mass = 60 },
            new ObjectDef { Id = "cafetable", NameHe = "שולחן בית קפה", NameEn = "Cafe table", Hp = 18, Value = 800, Material = Material.Metal, Reaction = Reaction.Launch, Mass = 15 },
            new ObjectDef { Id = "scooter", NameHe = "קטנוע", NameEn = "Scooter", Hp = 60, Value = 5000, Material = Material.Metal, Reaction = Reaction.Launch | Reaction.Explode, BlastRadius = 2.5f, BlastDamage = 350, BlastDelay = 1f, Mass = 90 },
            new ObjectDef { Id = "golfcart", NameHe = "רכב גולף", NameEn = "Golf cart", Hp = 150, Value = 12000, Material = Material.Metal, Reaction = Reaction.Launch | Reaction.Explode | Reaction.Burn, BlastRadius = 3.5f, BlastDamage = 500, BlastDelay = 1.2f, BurnSeconds = 12f, Mass = 450 },
            new ObjectDef { Id = "inflatable", NameHe = "בובה מתנפחת", NameEn = "Inflatable snowman", Hp = 4, Value = 900, Material = Material.Plastic, Reaction = Reaction.Launch, Mass = 3 },
            new ObjectDef { Id = "giftbox", NameHe = "קופסת מתנה", NameEn = "Gift box", Hp = 8, Value = 500, Material = Material.Cardboard, Reaction = Reaction.Burn | Reaction.Launch, BurnSeconds = 6f, Mass = 4 },
            new ObjectDef { Id = "lawnchair", NameHe = "כיסא גן", NameEn = "Lawn chair", Hp = 10, Value = 300, Material = Material.Aluminium, Reaction = Reaction.Launch, Mass = 3 },
            new ObjectDef { Id = "cone", NameHe = "קונוס", NameEn = "Traffic cone", Hp = 5, Value = 120, Material = Material.Plastic, Reaction = Reaction.Launch, Mass = 3 },
            new ObjectDef { Id = "shed", NameHe = "מחסן גינה", NameEn = "Garden shed", Hp = 160, Value = 6000, Material = Material.Wood, Reaction = Reaction.Burn | Reaction.Collapse, BurnSeconds = 25f, Mass = 600 },
            new ObjectDef { Id = "fortress", NameHe = "המחסן המשוריין", NameEn = "Armoured shed", Hp = 1400, Value = 40000, Material = Material.Metal, Reaction = Reaction.Contain, Mass = 8000 },
            // The big city's street furniture.
            new ObjectDef { Id = "trafficlight", NameHe = "רמזור", NameEn = "Traffic light", Hp = 70, Value = 3500, Material = Material.Metal, Reaction = Reaction.Launch, Mass = 120 },
            new ObjectDef { Id = "streetlamp", NameHe = "עמוד תאורה", NameEn = "Street lamp", Hp = 60, Value = 2200, Material = Material.Metal, Reaction = Reaction.Launch, Mass = 90 },
            new ObjectDef { Id = "busstop", NameHe = "תחנת אוטובוס", NameEn = "Bus shelter glass", Hp = 10, Value = 4000, Material = Material.Glass, Reaction = Reaction.Shatter, Mass = 40 },
            new ObjectDef { Id = "parkingmeter", NameHe = "מדחן", NameEn = "Parking meter", Hp = 20, Value = 700, Material = Material.Metal, Reaction = Reaction.Launch | Reaction.Contain, Mass = 20 },
            new ObjectDef { Id = "planter", NameHe = "עציץ רחוב", NameEn = "Street planter", Hp = 30, Value = 500, Material = Material.Wood, Reaction = Reaction.Launch | Reaction.Burn, BurnSeconds = 8f, Mass = 60 },
            new ObjectDef { Id = "tenement", NameHe = "בניין נטוש", NameEn = "Derelict tenement", Hp = 1, Value = 1, Material = Material.Concrete, Reaction = Reaction.Collapse, Mass = 60000 },   // a prop of storeys (Building); each storey is the target
            new ObjectDef { Id = "storey", NameHe = "קומה", NameEn = "Storey", Hp = 160, Value = 30000, Material = Material.Concrete, Reaction = Reaction.Collapse, Mass = 20000 },
            new ObjectDef { Id = "industrialtank", NameHe = "מיכל תעשייתי 50 קוב", NameEn = "Industrial tank", Hp = 900, Value = 900000, Material = Material.Metal, Reaction = Reaction.Explode, BlastRadius = 15f, BlastDamage = 5000, BlastDelay = 2f, ThermiteOnly = true, Mass = 20000 },
        };

        public static readonly LighterDef[] Lighters =
        {
            new LighterDef { Id = "basic", NameHe = "מצית רגיל", NameEn = "Lighter", CatchTime = 0.6f, WindBlowout = 0.3f },
            new LighterDef { Id = "zippo", NameHe = "Zippo", NameEn = "Zippo", CatchTime = 0.4f, WindBlowout = 0f, GemPrice = 20 },
            new LighterDef { Id = "turbo", NameHe = "מצית טורבו", NameEn = "Turbo lighter", CatchTime = 0f, WindBlowout = 0f, Flames = 2, GemPrice = 45 },
            new LighterDef { Id = "matches", NameHe = "גפרורים", NameEn = "Matches", CatchTime = 0.6f, WindBlowout = 0.3f, BreakChance = 1f / 3f },
        };

        public static readonly ArenaDef[] Arenas =
        {
            new ArenaDef { Number = 1, Id = "backyard", NameHe = "חצר אחורית", NameEn = "Backyard", TeachesHe = "הדלקה, פתיל, Containment", TeachesEn = "Lighting, fuses, containment", ThreeStars = 60000 },
            new ArenaDef { Number = 2, Id = "junkyard", NameHe = "מגרש גרוטאות", NameEn = "Junkyard", TeachesHe = "שרשרת רכבים, כיבוי", TeachesEn = "Car chains, dousing", ThreeStars = 180000 },
            new ArenaDef { Number = 3, Id = "garage", NameHe = "חניון תת-קרקעי", NameEn = "Underground garage", TeachesHe = "הד: גל הדף ×1.4 בחלל סגור", TeachesEn = "Echo: x1.4 blast wave indoors", ThreeStars = 400000, EchoMult = 1.4f, Outdoors = false },
            new ArenaDef { Number = 4, Id = "gasstation", NameHe = "תחנת דלק נטושה", NameEn = "Abandoned gas station", TeachesHe = "תזמון לפני הפיצוץ הגדול", TeachesEn = "Timing before the big one", ThreeStars = 900000 },
            new ArenaDef { Number = 5, Id = "construction", NameHe = "אתר בנייה", NameEn = "Construction site", TeachesHe = "דינמיט, Det cord, התמוטטות", TeachesEn = "Dynamite, det cord, collapse", ThreeStars = 1600000, UnlockStars = 12 },
            new ArenaDef { Number = 6, Id = "industrial", NameHe = "מתחם תעשייה", NameEn = "Industrial complex", TeachesHe = "Thermite, finale של 10+ חוליות", TeachesEn = "Thermite, 10+ link finale", ThreeStars = 3500000, UnlockStars = 16 },
        };

        static Dictionary<string, ExplosiveDef> explosiveById;
        static Dictionary<string, ObjectDef> objectById;

        public static ExplosiveDef Explosive(string id)
        {
            if (explosiveById == null)
            {
                explosiveById = new Dictionary<string, ExplosiveDef>();
                foreach (var e in Explosives) explosiveById[e.Id] = e;
            }
            ExplosiveDef d;
            return explosiveById.TryGetValue(id, out d) ? d : null;
        }

        public static ObjectDef Object(string id)
        {
            if (objectById == null)
            {
                objectById = new Dictionary<string, ObjectDef>();
                foreach (var o in Objects) objectById[o.Id] = o;
            }
            ObjectDef d;
            return objectById.TryGetValue(id, out d) ? d : null;
        }

        public static LighterDef Lighter(string id)
        {
            foreach (var l in Lighters) if (l.Id == id) return l;
            return Lighters[0];
        }

        public static ArenaDef Arena(int number)
        {
            foreach (var a in Arenas) if (a.Number == number) return a;
            return Arenas[0];
        }
    }
}
