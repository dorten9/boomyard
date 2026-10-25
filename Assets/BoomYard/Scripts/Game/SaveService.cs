using System;
using System.Collections.Generic;
using System.Text;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    [Serializable]
    public class Settings
    {
        public string lang = "";          // "he" / "en"; empty = follow the device
        public float sfxVolume = 1f;
        public float sensitivity = 1f;
        public bool invertY;
        public bool gyro;
        public bool reduceMotion;
        public bool reduceFlashes;
        public bool haptics = true;
        public float musicVolume = 0.6f;
        public bool leftHanded;
        public bool colorblind;
        public int quality = -1;   // -1 = automatic
    }

    [Serializable]
    public class ArenaRecord
    {
        public int number;
        public long best;
        public int stars;
    }

    [Serializable]
    public class LevelRecord
    {
        public int number;
        public int stars;
        public float bestTimeLeft = -1f;
        public long bestDamage;
    }

    [Serializable]
    public class SaveData
    {
        public const int CurrentSchema = 2;
        public int schema = CurrentSchema;
        public long shekels;              // coins (the field keeps its old name so saves still load)
        public float bestGnome;           // longest gnome flight, metres
        public List<int> goldGnomes = new List<int>();   // levels whose golden gnome was found
        public string glove = "", lighterSkin = "";     // style (looks only)
        public int purchases;                             // coin packs bought for real money
        public int challengeDate;                         // the daily challenge: day played (Daily.Seed), best and whether cleared
        public long challengeBest;
        public bool challengeCleared;
        public List<string> styles = new List<string>();  // style and graffiti items bought (shop ids)
        public string sprayColor = "red", sprayCap = "standard", stencil = "";   // the can as last set; stencil "" = freehand
        public List<string> customStencils = new List<string>();                // the stencil maker's grids (Graffiti.Valid)
        public int gems;                  // retired: gems became coins (100 each) on load
        public string lighter = "basic";
        public List<string> lighters = new List<string> { "basic" };
        public List<string> upgrades = new List<string>();
        public bool disclaimerSeen;
        public bool tutorialDone;
        public int roundsPlayed;
        public int unlocked = 1;           // highest level that can be played
        public List<PocketSlot> inventory = new List<PocketSlot>();
        public List<ArenaRecord> arenas = new List<ArenaRecord>();
        public List<LevelRecord> levels = new List<LevelRecord>();
        public Settings settings = new Settings();

        // Daily missions and the login calendar.
        public int missionsDate;
        public List<Mission> missions = new List<Mission>();
        public int dailyClaimDate, dailyStreak;

        // Lifetime stats (shown in the profile, used by achievements later).
        public int totalDestroyed, totalBlasts, bestChain;

        /// <summary>Owns a style or graffiti item (the free ones count as owned).</summary>
        public bool Owns(ShopItem item) { return item != null && (item.Coins == 0 || styles.Contains(item.Id)); }

        /// <summary>A custom stencil grid by slot (blank if never drawn).</summary>
        public string Custom(int slot)
        {
            while (customStencils.Count < Graffiti.CustomSlots) customStencils.Add(Graffiti.Blank);
            return Graffiti.Valid(customStencils[slot]) ? customStencils[slot] : Graffiti.Blank;
        }

        public ArenaRecord Arena(int number)
        {
            foreach (var a in arenas) if (a.number == number) return a;
            var r = new ArenaRecord { number = number };
            arenas.Add(r);
            return r;
        }

        public LevelRecord Level(int number)
        {
            foreach (var l in levels) if (l.number == number) return l;
            var r = new LevelRecord { number = number };
            levels.Add(r);
            return r;
        }

        public int TotalStars { get { int n = 0; foreach (var l in levels) n += l.stars; return n; } }

        public bool Has(string upgrade) { return upgrades.Contains(upgrade); }

        public int Count(string id)
        {
            foreach (var s in inventory) if (s.id == id) return s.count;
            return 0;
        }

        public void Add(string id, int n)
        {
            foreach (var s in inventory) if (s.id == id) { s.count = Math.Max(0, s.count + n); return; }
            if (n > 0) inventory.Add(new PocketSlot { id = id, count = n });
        }

        /// <summary>Today's missions, rolled over at midnight.</summary>
        public List<Mission> TodaysMissions()
        {
            int today = Daily.Seed(DateTime.Now);
            if (missionsDate != today || missions.Count == 0)
            {
                missionsDate = today;
                missions = Missions.ForDay(today);
            }
            return missions;
        }
    }

    /// <summary>
    /// Local save, no servers (spec §02, §16): JSON, lightly encrypted against casual edits, in PlayerPrefs so it works
    /// the same on iOS, Android and in the browser (where PlayerPrefs live in IndexedDB).
    /// </summary>
    public static class SaveService
    {
        const string Key = "boomyard.save";
        const string Pepper = "BoomYard.night.yard";

        public static SaveData Data { get; private set; }

        public static SaveData Load()
        {
            Data = null;
            try
            {
                var raw = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(raw)) Data = JsonUtility.FromJson<SaveData>(Decode(raw));
                if (Data != null && Data.schema < SaveData.CurrentSchema) Data = null;   // the base's saves predate the level map
            }
            catch (Exception e) { Debug.LogWarning("BoomYard: save unreadable, starting fresh. " + e.Message); }
            if (Data == null) Data = Fresh();
            if (Data.gems > 0) { Data.shekels += Data.gems * 100L; Data.gems = 0; }   // one currency now
            return Data;
        }

        /// <summary>A new player starts with a small gift so the first rounds have more than Black Cats.</summary>
        static SaveData Fresh()
        {
            var d = new SaveData();
            d.shekels = 500;
            return d;
        }

        public static void Save()
        {
            if (Data == null) return;
            PlayerPrefs.SetString(Key, Encode(JsonUtility.ToJson(Data)));
            PlayerPrefs.Save();
        }

        public static void Wipe()
        {
            PlayerPrefs.DeleteKey(Key);
            Data = Fresh();
        }

        static string Encode(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            Xor(bytes);
            return "1:" + Convert.ToBase64String(bytes) + ":" + Checksum(json);
        }

        static string Decode(string raw)
        {
            var parts = raw.Split(':');
            if (parts.Length != 3 || parts[0] != "1") throw new FormatException("unknown save format");
            var bytes = Convert.FromBase64String(parts[1]);
            Xor(bytes);
            var json = Encoding.UTF8.GetString(bytes);
            if (Checksum(json) != parts[2]) throw new FormatException("checksum mismatch");
            return json;
        }

        static void Xor(byte[] bytes)
        {
            var k = Encoding.UTF8.GetBytes(Pepper);
            for (int i = 0; i < bytes.Length; i++) bytes[i] ^= (byte)(k[i % k.Length] + i * 31);
        }

        static string Checksum(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in s + Pepper) { h ^= c; h *= 16777619; }
                return h.ToString("x8");
            }
        }
    }
}
