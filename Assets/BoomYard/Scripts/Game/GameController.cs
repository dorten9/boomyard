using System.Collections.Generic;
using BoomYard.Core;
using BoomYard.UI;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>How a level ended (the results screen).</summary>
    public sealed class RoundSummary
    {
        public LevelDef Level;
        public ArenaDef Arena;
        public bool Cleared;
        public string FailReason;   // "time", "empty", "spare"
        public float TimeLeft;
        public int TargetsLeft, Targets;
        public long Total, Base, ChainExtra, PlacementExtra, Bonuses;
        public bool CleanSweep;
        public int LongestChain, Destroyed, Objects, Stars, PreviousStars, ContainedHits;
        public long PreviousBest;
        public bool Record;
        public long Coins;
        public bool FirstClear, Unlocked;
        public string TipKey;
        public List<BonusKind> BonusLog;
    }

    /// <summary>
    /// The game: menus and the level map around it, and inside a level the round (spec §16 RoundController):
    /// destroy every target before the clock runs out. Keeps score, chains, missions, rewards and the save.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        public static GameController I { get; private set; }
        public static bool ReduceMotion { get { return SaveService.Data != null && SaveService.Data.settings.reduceMotion; } }
        public static bool ReduceFlashes { get { return SaveService.Data != null && SaveService.Data.settings.reduceFlashes; } }

        public enum Mode { Menu, Level }

        public Mode Current { get; private set; }
        public RoundModel Round { get; private set; }
        public RoundScore Score { get; private set; }
        public ChainTracker Chain { get; private set; }
        public WindModel Wind { get; private set; }
        public Pocket Pocket { get; private set; }
        public Arena Arena { get; private set; }
        public ArenaDef ArenaDef { get { return Arena != null ? Arena.Def : Catalog.Arena(1); } }
        public LevelDef Level { get; private set; }
        public GameUI UI { get; private set; }
        public Player Player { get; private set; }
        public Tutorial Tutorial { get; private set; }
        public bool Paused { get; private set; }
        public RoundSummary Last { get; private set; }
        public string Shot { get; private set; }

        readonly Dictionary<int, int> killsPerBlast = new Dictionary<int, int>();
        float idle, clearedAt = -1f, lastCloseCall = -10f;
        int seed;

        /// <summary>Startup timing in the log (the browser build's console), to find what makes loading slow.</summary>
        static void Mark(string what) { Debug.Log("BoomYard: " + what + " at " + Time.realtimeSinceStartup.ToString("0.00") + " s"); }
        bool firstFrame;

        void Awake()
        {
            I = this;
            Mark("start");
            var save = SaveService.Load();
            Loc.Init("en");
            var tier = save.settings.quality >= 0 ? (QualityTier)save.settings.quality : Quality.Detect();
            Quality.Apply(tier);
            Physics.defaultMaxDepenetrationVelocity = 6f;
            var sfx = gameObject.AddComponent<Sfx>();
            sfx.Volume = save.settings.sfxVolume;
            sfx.MusicVolume = save.settings.musicVolume;
            Mark("sound ready");
            gameObject.AddComponent<FireGrid>();
            Rendering.Create(Quality.Tier);
            Mark("rendering ready");

            Round = new RoundModel();
            Round.Changed += OnRoundState;
            Score = new RoundScore();
            Chain = new ChainTracker();
            Pocket = new Pocket();
            seed = Daily.Seed(System.DateTime.Now) + save.roundsPlayed;
            Wind = new WindModel(seed);

            // The menus play over a live level: the next one on the map.
            Level = Levels.Get(Mathf.Clamp(save.unlocked, 1, Levels.All.Length));
            BuildArena();
            Mark("arena built");
            Player = Player.Create(Arena.Spawn, Arena.SpawnYaw);
            Player.ResetForRound(Arena.Spawn, Arena.SpawnYaw, Catalog.Lighter(save.lighter), seed);
            Player.Sensitivity = save.settings.sensitivity;
            Player.Hands.ApplyStyle(save.glove, save.lighterSkin);
            Rendering.SetupCamera(Player.Cam, Quality.Tier);
            Tutorial = gameObject.AddComponent<Tutorial>();

            UI = GameUI.Create(this);
            Mark("ui built");
            Shot = ShotMode();
            GoHome();
            Mark("home shown");
            if (Shot != "") StartCoroutine(RunShot(Shot));
        }

        void BuildArena()
        {
            if (Arena != null && Arena.Root != null) Destroy(Arena.Root);
            if (rain != null) Destroy(rain);
            spareBroken = false;
            foreach (var live in LiveExplosive.Active.ToArray()) if (live != null) Destroy(live.gameObject);
            FireGrid.I.Clear();
            var fx = GameObject.Find("Fx");
            if (fx != null) Destroy(fx);
            Arena = ArenaBuilder.Build(Level, Quality.Tier);
            if (Rendering.I != null) Rendering.I.RefreshReflections();
        }

        /// <summary>The level's kit plus what the player owns (bought in the shop), one item per slot.</summary>
        void FillPocket()
        {
            var save = SaveService.Data;
            Pocket.Slots.Clear();
            Pocket.Selected = 0;
            int slots = Core.Pocket.BaseSlots + (save.Has("vest") ? 1 : 0);
            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (var k in Level.Kit) { if (!counts.ContainsKey(k.id)) { counts[k.id] = 0; order.Add(k.id); } counts[k.id] += k.count; }
            foreach (var s in save.inventory)
            {
                if (s.count <= 0 || Catalog.Explosive(s.id) == null) continue;
                if (!counts.ContainsKey(s.id)) { if (order.Count >= slots) continue; counts[s.id] = 0; order.Add(s.id); }
                counts[s.id] += s.count;
            }
            foreach (var id in order)
            {
                if (Pocket.Slots.Count >= slots) break;
                Pocket.Slots.Add(new PocketSlot { id = id, count = counts[id] });
            }
        }

        // ---------- navigation ----------

        public void GoHome()
        {
            ToMenu();
            UI.ShowHome();
        }

        public void GoMap()
        {
            ToMenu();
            UI.ShowMap();
        }

        void ToMenu()
        {
            Current = Mode.Menu;
            Paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (Round.State != RoundState.Loadout)
            {
                Round = new RoundModel();
                Round.Changed += OnRoundState;
                BuildArena();
                Player.ResetForRound(Arena.Spawn, Arena.SpawnYaw, Catalog.Lighter(SaveService.Data.lighter), seed);
            }
            Player.Controllable = false;
            Tutorial.Stop();
            Sfx.I.Music(true);
        }

        public void StartLevel(int number) { StartLevel(Levels.Get(number)); }

        /// <summary>The fireworks show: Holly Lane, no targets, no clock, a pocket full of rockets.</summary>
        public void StartShow()
        {
            StartLevel(Levels.FireworksShow());
            Sfx.I.Music(true);
            UI.Toast(Loc.T("show.intro"), 5f);
        }

        /// <summary>Every rocket standing in the yard goes up, one after another, in the order they were set down.</summary>
        public void LaunchShow()
        {
            var list = new List<LiveExplosive>();
            foreach (var l in LiveExplosive.Active) if (l != null && !l.Lit && l.Def.Id == "rocket") list.Add(l);
            if (list.Count == 0) return;
            StartCoroutine(Show(list));
        }

        System.Collections.IEnumerator Show(List<LiveExplosive> list)
        {
            foreach (var l in list)
            {
                if (l != null && !l.Lit) { l.Remaining = 0.6f; l.Light(); }
                yield return new WaitForSeconds(Random.Range(0.35f, 0.7f));
            }
        }

        /// <summary>Today's challenge.</summary>
        public void StartDaily() { StartLevel(Levels.DailyChallenge(Daily.Seed(System.DateTime.Now))); }

        public void StartLevel(LevelDef level)
        {
            var save = SaveService.Data;
            Level = level;
            Current = Mode.Level;
            Paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Sfx.I.Music(false);
            BuildArena();
            seed = Daily.Seed(System.DateTime.Now) * 31 + save.roundsPlayed;
            Wind = new WindModel(seed);
            if (Level.Rule == LevelRule.Windy) Wind.SpeedScale = 2f;
            Score = new RoundScore();
            Chain = new ChainTracker();
            killsPerBlast.Clear();
            idle = 0f;
            clearedAt = -1f;
            FillPocket();
            Player.ResetForRound(Arena.Spawn, Arena.SpawnYaw, LighterFor(Level, save), seed);
            Player.Controllable = true;
            if (Raining) rain = Fx.Rain(Player.Cam.transform);
            Round = new RoundModel();
            Round.Changed += OnRoundState;
            Round.Start(Level.Time + (save.Has("watch") ? 10f : 0f));
            UI.ShowHud();
            if (Level.Tutorial && !save.tutorialDone) Tutorial.Begin(this);
            else if (Level.Rule != LevelRule.None) UI.RuleCard(Level, Arena.Spare);
        }

        public bool Raining { get { return Current == Mode.Level && Level != null && Level.Rule == LevelRule.Rain; } }

        /// <summary>Delay a timer charge is set to when placed (5, 10 or 15 s; tap it again in the pocket).</summary>
        public float TimerSeconds = 10f;

        public void CycleTimer()
        {
            TimerSeconds = TimerSeconds >= 15f ? 5f : TimerSeconds + 5f;
            Sfx.I.Tap();
            UI.Toast(Loc.F("toast.timerset", Mathf.RoundToInt(TimerSeconds)));
        }

        /// <summary>The detonator: every remote charge in the yard goes at once (a few frames apart, so it reads as a ripple).</summary>
        public void DetonateRemotes()
        {
            var list = new List<LiveExplosive>();
            foreach (var l in LiveExplosive.Active) if (l != null && l.Def.Remote) list.Add(l);
            if (list.Count == 0) return;
            Sfx.I.Click();
            StartCoroutine(Ripple(list));
        }

        System.Collections.IEnumerator Ripple(List<LiveExplosive> list)
        {
            foreach (var l in list)
            {
                if (l != null) { OnIgnite(l.Def); l.Detonate(); }
                yield return new WaitForSeconds(0.06f);
            }
        }
        GameObject rain;
        bool spareBroken;

        /// <summary>The lighter for a level: matches on a matches-only level, slower to catch in the rain.</summary>
        static LighterDef LighterFor(LevelDef level, SaveData save)
        {
            var l = Catalog.Lighter(level.Rule == LevelRule.Matches ? "matches" : save.lighter);
            if (level.Rule != LevelRule.Rain) return l;
            return new LighterDef { Id = l.Id, NameEn = l.NameEn, NameHe = l.NameHe, CatchTime = l.CatchTime * 1.6f + 0.25f, WindBlowout = l.WindBlowout, Flames = l.Flames, BreakChance = l.BreakChance };
        }

        public void Restart() { if (Level.Daily) StartDaily(); else StartLevel(Level.Number); }

        public void NextLevel() { StartLevel(Mathf.Min(Level.Number + 1, Levels.All.Length)); }

        // ---------- the round ----------

        void Update()
        {
            if (!firstFrame) { firstFrame = true; Mark("first frame"); }
            UpdateTime();
            Player.Hands.Down = Current != Mode.Level || Player.Knocked || Round.State == RoundState.Stunned;
            if (Paused || Current != Mode.Level) return;
            Chain.Tick(Time.time);
            var st = Round.State;
            if (Level.Show) { Wind.Update(Time.timeSinceLevelLoad); return; }   // no clock, no targets: just the sky
            if (st == RoundState.Playing || st == RoundState.Stunned || st == RoundState.Settling)
            {
                Wind.Update(Round.Elapsed);
                bool busy = LiveExplosive.LitCount > 0 || FireGrid.I.AnyBurning || Player.HeldLit;
                Round.Tick(Time.deltaTime, busy && Arena.TargetsLeft > 0);

                // Everything down: hold a moment for the smoke, then the win.
                if (Arena.TargetsLeft == 0 && clearedAt < 0f)
                {
                    clearedAt = Time.time;
                    UI.Toast("ALL CLEAR!");
                }
                if (clearedAt >= 0f && Time.time - clearedAt > 2f && Round.State != RoundState.Results) Round.Finish();
                // The one thing that had to survive didn't.
                if (Arena.Spare != null && Arena.Spare.Destroyed && clearedAt < 0f && Round.State != RoundState.Results) { spareBroken = true; Round.Finish(); }
                if (clearedAt >= 0f && Round.State == RoundState.Settling) Round.Tick(0f, false);

                // Out of explosives with targets standing and nothing going off: no need to wait out the clock.
                bool nothingLeft = Pocket.TotalLeft == 0 && !Player.HeldLit && LiveExplosive.Active.Count == 0 && !FireGrid.I.AnyBurning;
                if (Round.CanAct && clearedAt < 0f && nothingLeft)
                {
                    idle += Time.deltaTime;
                    if (idle > 2.5f) Round.Finish();
                }
                else idle = 0f;
            }
        }

        // ---------- time effects ----------

        float stopUntil, slowUntil, slowScale = 1f;

        /// <summary>A frozen instant on a big close blast, so it lands.</summary>
        public void HitStop(float seconds)
        {
            if (ReduceMotion) return;
            stopUntil = Mathf.Max(stopUntil, Time.unscaledTime + seconds);
        }

        /// <summary>Slow motion for the big moments (a chain reaching three links and more).</summary>
        public void SlowMo(float scale, float seconds)
        {
            if (ReduceMotion) return;
            slowScale = scale;
            slowUntil = Mathf.Max(slowUntil, Time.unscaledTime + seconds);
        }

        void UpdateTime()
        {
            if (Paused) return;
            float now = Time.unscaledTime;
            float ts = now < stopUntil ? 0.03f : now < slowUntil ? Mathf.Lerp(slowScale, 1f, Mathf.Clamp01(1f - (slowUntil - now) / 0.25f)) : 1f;
            Time.timeScale = ts;
            Time.fixedDeltaTime = 0.02f * Mathf.Max(ts, 0.25f);
        }

        // ---------- scoring ----------

        /// <summary>Damage to an object turned into dollars (called by Destructible).</summary>
        public void Credit(Destructible d, float fraction, Hit hit, bool destroyed)
        {
            if (Current != Mode.Level) return;
            var st = Round.State;
            if (st != RoundState.Playing && st != RoundState.Stunned && st != RoundState.Settling) return;
            float mult = destroyed ? Chain.OnDestroyed(Time.time, hit.ByObject) : Chain.MultiplierFor(hit.ByObject);
            long v = Score.AddDamage(d.Def.Value, fraction, d.Def.Material, hit.Placement, mult);
            if (destroyed && hit.ByObject && Chain.Links >= 3 && Chain.Links % 3 == 0) SlowMo(0.3f, 0.9f);
            if (destroyed && hit.ByObject && Chain.Links == ScoreCalculator.DominoLinks) Bonus(BonusKind.Domino);
            if (destroyed)
            {
                Score.ObjectsDestroyed++;
                var missions = SaveService.Data.TodaysMissions();
                Missions.Count(missions, "destroy", null, 1);
                Missions.Count(missions, "destroy_id", d.Def.Id, 1);
                if (hit.ByObject) Missions.Count(missions, "chain", null, Chain.Links);
                if (hit.Placement == Placement.Contained) Missions.Count(missions, "contained", null, 1);
                if (hit.BlastId != 0 && !hit.ByObject)
                {
                    int k;
                    killsPerBlast.TryGetValue(hit.BlastId, out k);
                    killsPerBlast[hit.BlastId] = ++k;
                    if (k == ScoreCalculator.MultiCount) Bonus(BonusKind.Multi);
                }
                if (Arena.Targets.Contains(d)) UI.TargetDown(Arena.TargetsLeft, Arena.Targets.Count);
            }
            UI.Popup(d.transform.TransformPoint(d.FirePoint), v, destroyed && hit.ByObject ? mult : 0f);
        }

        public void Bonus(BonusKind kind)
        {
            if (Current != Mode.Level) return;
            var st = Round.State;
            if (st != RoundState.Playing && st != RoundState.Stunned && st != RoundState.Settling) return;
            Score.AddBonus(kind);
            UI.Bonus(kind);
        }

        public void FoundGoldGnome(int level)
        {
            var save = SaveService.Data;
            if (save.goldGnomes.Contains(level)) return;
            save.goldGnomes.Add(level);
            save.shekels += GoldGnome.Coins;
            SaveService.Save();
            Sfx.I.Star();
            UI.Toast(Loc.F("goldgnome", GoldGnome.Coins, save.goldGnomes.Count, Levels.All.Length - 1), 3f);
        }

        /// <summary>A launched object landed (StreetLife.Flight): long flights pay, gnomes get their distance called out.</summary>
        public void OnFlight(Destructible d, float seconds, float metres)
        {
            if (d == null || seconds < ScoreCalculator.AirTimeSeconds || metres < 3f) return;
            Bonus(BonusKind.AirTime);
            UI.Toast(Loc.F("flight", d.Name.ToLowerInvariant(), Mathf.RoundToInt(metres)), 2.6f);
            var save = SaveService.Data;
            if (d.Def.Id == "gnome" && metres > save.bestGnome) save.bestGnome = metres;
        }

        public void OnIgnite(ExplosiveDef def) { Tutorial.OnEvent("lit"); }

        public void OnRelease(LiveExplosive live)
        {
            if (live.ReleasedWith <= ScoreCalculator.LastSecondFuse && live.Def.Fuse > 0f) Bonus(BonusKind.LastSecond);
            Tutorial.OnEvent("thrown");
        }

        public void OnPlaced() { Tutorial.OnEvent("placed"); }

        public void OnPlayerDetonation(LiveExplosive live, Placement placement)
        {
            Missions.Count(SaveService.Data.TodaysMissions(), "blasts", null, 1);
            SaveService.Data.totalBlasts++;
            Tutorial.OnEvent("boom");
        }

        /// <summary>It went off in the hand: white screen, shaking hands for 3 s, a fine (spec §04, §07).</summary>
        public void OnHandBurn()
        {
            Bonus(BonusKind.HandBurn);
            Round.Stun(RoundModel.HandBurnStun);
            UI.Flash(Color.white, 1.4f);
            Sfx.I.Ring();
        }

        public void OnKnockdown()
        {
            Round.Stun(RoundModel.StunSeconds);
            UI.Flash(new Color(1f, 0.9f, 0.8f, 0.8f), 0.6f);
        }

        public void OnCloseCall()
        {
            if (Time.time - lastCloseCall < 1f) return;
            lastCloseCall = Time.time;
            Bonus(BonusKind.CloseCall);
        }

        public void Toast(string key) { UI.Toast(Loc.T(key)); }

        // ---------- the end of a level ----------

        void OnRoundState(RoundState s)
        {
            if (s != RoundState.Results || Current != Mode.Level) return;
            Player.Controllable = false;
            Tutorial.Stop();
            Last = Summarize();
            UI.ShowResults(Last);
            if (Last.Cleared) Sfx.I.Win(); else Sfx.I.Fail();
        }

        RoundSummary Summarize()
        {
            var save = SaveService.Data;
            int total = 0, destroyed = 0;
            foreach (var d in Arena.Objects)
            {
                if (d == null) continue;
                total++;
                if (d.Destroyed) destroyed++;
            }
            int targetsLeft = Arena.TargetsLeft;
            bool cleared = targetsLeft == 0 && !spareBroken;
            Score.CleanSweep = total > 0 && destroyed == total;
            int today = Daily.Seed(System.DateTime.Now);
            if (Level.Daily && save.challengeDate != today) { save.challengeDate = today; save.challengeBest = 0; save.challengeCleared = false; }
            // The daily challenge keeps its own record; it never touches the level it copies.
            var rec = Level.Daily ? new LevelRecord { number = Level.Number, stars = save.challengeCleared ? 1 : 0, bestDamage = save.challengeBest } : save.Level(Level.Number);
            var sum = new RoundSummary
            {
                Level = Level,
                Arena = ArenaDef,
                Cleared = cleared,
                FailReason = cleared ? "" : spareBroken ? "spare" : Round.TimeLeft <= 0f ? "time" : "empty",
                TimeLeft = Round.TimeLeft,
                TargetsLeft = targetsLeft,
                Targets = Arena.Targets.Count,
                Total = Score.Total,
                Base = Score.Base,
                ChainExtra = Score.ChainExtra,
                PlacementExtra = Score.PlacementExtra,
                Bonuses = Score.Bonuses,
                CleanSweep = Score.CleanSweep,
                LongestChain = Chain.LongestChain,
                Destroyed = destroyed,
                Objects = total,
                ContainedHits = Score.ContainedHits,
                PreviousBest = rec.bestDamage,
                PreviousStars = rec.stars,
                BonusLog = new List<BonusKind>(Score.BonusLog),
            };
            sum.Stars = Level.Stars(cleared, Round.TimeLeft);
            sum.Record = cleared && sum.Total > rec.bestDamage;
            sum.FirstClear = cleared && rec.stars == 0;
            int newStars = Level.Daily ? 0 : Mathf.Max(0, sum.Stars - rec.stars);
            sum.Coins = Progress.LevelCoins(Level, cleared, sum.FirstClear, newStars, sum.Total);
            sum.TipKey = cleared ? (Chain.LongestChain < 2 ? "tip.chain" : "tip.again") : sum.FailReason == "spare" ? "tip.spare" : (sum.FailReason == "empty" ? "tip.empty" : "tip.time");

            if (cleared)
            {
                if (sum.Record) rec.bestDamage = sum.Total;
                rec.stars = Mathf.Max(rec.stars, sum.Stars);
                rec.bestTimeLeft = Mathf.Max(rec.bestTimeLeft, Round.TimeLeft);
                if (!Level.Daily && Level.Number >= save.unlocked && Level.Number < Levels.All.Length) { save.unlocked = Level.Number + 1; sum.Unlocked = true; }
                if (Level.Daily) { save.challengeCleared = true; save.challengeBest = rec.bestDamage; }
                var missions = save.TodaysMissions();
                Missions.Count(missions, "clear", null, 1);
                if (newStars > 0) Missions.Count(missions, "stars", null, newStars);
                if (Level.Tutorial) save.tutorialDone = true;
            }
            save.shekels += sum.Coins;
            save.roundsPlayed++;
            save.totalDestroyed += destroyed;
            save.bestChain = Mathf.Max(save.bestChain, Chain.LongestChain);
            // Bought items that were used come out of the inventory (the level's own kit is free).
            foreach (var slot in Pocket.Slots)
            {
                int kit = 0;
                foreach (var k in Level.Kit) if (k.id == slot.id) kit += k.count;
                int owned = save.Count(slot.id);
                int used = (kit + owned) - slot.count;
                int fromOwned = Mathf.Max(0, used - kit);
                if (fromOwned > 0) save.Add(slot.id, -fromOwned);
            }
            SaveService.Save();
            return sum;
        }

        public void TogglePause()
        {
            if (Current != Mode.Level || Round.State == RoundState.Results || Round.State == RoundState.Loadout) return;
            Paused = !Paused;
            Time.timeScale = Paused ? 0f : 1f;
            AudioListener.pause = Paused;
            UI.ShowPause(Paused);
        }

        // ---------- shop, missions, daily reward ----------

        public bool Buy(ShopItem item)
        {
            var save = SaveService.Data;
            if (item.Kind == "lighter" && save.lighters.Contains(item.Grants)) { save.lighter = item.Grants; SaveService.Save(); return true; }
            if (item.Kind == "upgrade" && save.Has(item.Grants)) return false;
            if (item.Kind == "glove" || item.Kind == "lighterskin")
            {
                bool own = item.Coins == 0 || save.styles.Contains(item.Id);
                if (!own)
                {
                    if (save.shekels < item.Coins) return false;
                    save.shekels -= item.Coins;
                    save.styles.Add(item.Id);
                    Sfx.I.Coin();
                }
                else Sfx.I.Confirm();
                if (item.Kind == "glove") save.glove = item.Grants; else save.lighterSkin = item.Grants;
                SaveService.Save();
                Player.Hands.ApplyStyle(save.glove, save.lighterSkin);
                return true;
            }
            if (save.shekels < item.Coins) return false;
            save.shekels -= item.Coins;
            switch (item.Kind)
            {
                case "explosive": save.Add(item.Grants, item.Amount); break;
                case "lighter": save.lighters.Add(item.Grants); save.lighter = item.Grants; break;
                case "upgrade": save.upgrades.Add(item.Grants); break;
            }
            SaveService.Save();
            Sfx.I.Coin();
            return true;
        }

        public void Claim(Mission m)
        {
            if (!m.Done || m.claimed) return;
            m.claimed = true;
            SaveService.Data.shekels += m.coins;
            SaveService.Save();
            Sfx.I.Coin();
        }

        /// <summary>Today's login reward (day index), or -1 if already taken.</summary>
        public int DailyAvailable()
        {
            var s = SaveService.Data;
            return DailyReward.NextDay(s.dailyClaimDate, s.dailyStreak, System.DateTime.Now);
        }

        public void ClaimDaily()
        {
            int day = DailyAvailable();
            if (day < 0) return;
            var s = SaveService.Data;
            s.dailyStreak = day;
            s.dailyClaimDate = Daily.Seed(System.DateTime.Now);
            s.shekels += DailyReward.Coins[day];
            SaveService.Save();
            Sfx.I.Star();
        }

        // ---------- settings ----------

        public void ApplySettings()
        {
            var st = SaveService.Data.settings;
            Sfx.I.Volume = st.sfxVolume;
            Sfx.I.MusicVolume = st.musicVolume;
            Sfx.I.Music(Current == Mode.Menu);
            Player.Sensitivity = st.sensitivity;
            var tier = st.quality >= 0 ? (QualityTier)st.quality : Quality.Detect();
            Quality.Apply(tier);
            Rendering.ApplyTier(tier);
            Rendering.SetupCamera(Player.Cam, tier);
            SaveService.Save();
        }

        public void ResetProgress()
        {
            var settings = SaveService.Data.settings;
            SaveService.Wipe();
            SaveService.Data.settings = settings;
            SaveService.Data.disclaimerSeen = true;
            SaveService.Save();
            Level = Levels.Get(1);
            Round = new RoundModel();
            Round.Changed += OnRoundState;
            BuildArena();
            GoHome();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && Current == Mode.Level && (Round.State == RoundState.Playing || Round.State == RoundState.Stunned) && !Paused) TogglePause();
            if (paused) SaveService.Save();
        }

        // ---------- screenshot runs (CI) ----------

        static string ShotMode()
        {
            var url = Application.absoluteURL ?? "";
            int i = url.IndexOf("shot=");
            if (i < 0) return "";
            var v = url.Substring(i + 5);
            int amp = v.IndexOf('&');
            return amp >= 0 ? v.Substring(0, amp) : v;
        }

        /// <summary>?shot=title|map|mapall|shop|play|boom|level&lt;n&gt; on the web page URL.</summary>
        System.Collections.IEnumerator RunShot(string mode)
        {
            SaveService.Data.disclaimerSeen = true;
            SaveService.Data.tutorialDone = true;
            if (mode == "title") { GoHome(); yield break; }
            if (mode == "map") { GoMap(); yield break; }
            if (mode == "shop") { GoHome(); UI.ShowShop(); yield break; }
            if (mode == "mapall") { SaveService.Data.unlocked = Levels.All.Length; GoMap(); yield break; }
            int n;
            if (mode.StartsWith("level") && int.TryParse(mode.Substring(5), out n)) { StartLevel(Mathf.Clamp(n, 1, Levels.All.Length)); yield break; }
            StartLevel(mode == "boom" ? 9 : 4);
            if (mode != "boom") yield break;
            // A scripted chain for the explosion screenshots: an M-80 under the van, then the barrels go.
            yield return new WaitForSeconds(2f);
            Destructible van = null;
            foreach (var t in Arena.Targets) if (t.Def.Id == "van") van = t;
            if (van != null)
            {
                var pos = van.transform.TransformPoint(new Vector3(0.6f, 0f, 1.4f));
                pos.y = 0.08f;
                var live = LiveExplosive.Spawn(Catalog.Explosive("m80"), pos, Quaternion.identity, 0.3f, Placement.Contained);
                live.Container = van;
            }
        }
    }
}
