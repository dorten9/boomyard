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
    }

    [Serializable]
    public class ArenaRecord
    {
        public int number;
        public long best;
        public int stars;
    }

    [Serializable]
    public class SaveData
    {
        public const int CurrentSchema = 1;
        public int schema = CurrentSchema;
        public long shekels;
        public int gems;
        public string lighter = "basic";
        public bool disclaimerSeen;
        public int roundsPlayed;
        public List<PocketSlot> inventory = new List<PocketSlot>();
        public List<ArenaRecord> arenas = new List<ArenaRecord>();
        public Settings settings = new Settings();

        public ArenaRecord Arena(int number)
        {
            foreach (var a in arenas) if (a.number == number) return a;
            var r = new ArenaRecord { number = number };
            arenas.Add(r);
            return r;
        }

        public int TotalStars { get { int n = 0; foreach (var a in arenas) n += a.stars; return n; } }

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
            }
            catch (Exception e) { Debug.LogWarning("BoomYard: save unreadable, starting fresh. " + e.Message); }
            if (Data == null) Data = Fresh();
            return Data;
        }

        /// <summary>A new player starts with a small gift so the first rounds have more than Black Cats.</summary>
        static SaveData Fresh()
        {
            var d = new SaveData();
            d.Add("string50", 1);
            d.Add("sparkler", 2);
            d.Add("m80", 2);
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
