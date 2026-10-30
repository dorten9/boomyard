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
            Stencils.MigrateOldSave(save);
            Purchases.Init();
            Purchases.Granted += pack => { Sfx.I.Coin(); if (UI != null) UI.Toast("+" + Loc.Coins(pack.Coins) + " coins. Thank you!", 3f); };
            Loc.Init("en");
            var tier = save.settings.quality >= 0 ? (QualityTier)save.settings.quality : Quality.Detect();
            Quality.Apply(tier);
            Physics.defaultMaxDepenetrationVelocity = 6f;
            var sfx = gameObject.AddComponent<Sfx>();
            sfx.Volume = save.settings.sfxVolume;
            sfx.MusicVolume = save.settings.musicVolume;
            Mark("sound ready");
            gameObject.AddComponent<FireGrid>();
            Spray.Create(gameObject);
            ImagePicker.Create();
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
            Spray.I.Clear();
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
            int slots = Level.Show ? Core.Pocket.MaxSlots : Core.Pocket.BaseSlots + (save.Has("vest") ? 2 : 0);
            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            // The level's kit first, then every kind the player has unlocked in the shop, and the Black Cat always.
            foreach (var k in Level.Kit) { if (!counts.ContainsKey(k.id)) { counts[k.id] = 0; order.Add(k.id); } counts[k.id] += k.count; }
            foreach (var s in save.inventory)
            {
                if (s.count <= 0 || Catalog.Explosive(s.id) == null || counts.ContainsKey(s.id)) continue;
                counts[s.id] = 1;
                order.Add(s.id);
            }
            if (!counts.ContainsKey("blackcat")) { counts["blackcat"] = 1; order.Add("blackcat"); }
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

        /// <summary>The big city: a downtown to wreck and paint, no targets, no clock, everything unlimited.</summary>
        public void StartCity()
        {
            StartLevel(Levels.City());
            UI.Toast(Loc.T("city.intro"), 6f);
        }

        /// <summary>Leaves the city and banks a share of the wreckage as coins (the results screen).</summary>
        public void CashIn()
        {
            if (Current != Mode.Level || !Level.Show) return;
            if (Paused) TogglePause();
            Round.Finish();
            Round.Tick(0f, false);
        }

        /// <summary>Every firework standing in the street goes up, one after another, in the order they were set down.</summary>
        public void LaunchShow()
        {
            var list = new List<LiveExplosive>();
            foreach (var l in LiveExplosive.Active) if (l != null && !l.Lit && l.Def.Firework != null) list.Add(l);
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

        public void Restart() { if (Level.Show) StartCity(); else if (Level.Daily) StartDaily(); else StartLevel(Level.Number); }

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
            if (Level.Show) return SummarizeCity(destroyed, total);
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
            SaveService.Save();
            return sum;
        }

        /// <summary>The city pays a share of its wreckage (there is so much of it); no records, no stars.</summary>
        RoundSummary SummarizeCity(int destroyed, int total)
        {
            var save = SaveService.Data;
            var sum = new RoundSummary
            {
                Level = Level, Arena = ArenaDef, Cleared = true, FailReason = "", Total = Score.Total, Base = Score.Base,
                ChainExtra = Score.ChainExtra, Bonuses = Score.Bonuses, LongestChain = Chain.LongestChain, Destroyed = destroyed, Objects = total,
                BonusLog = new List<BonusKind>(Score.BonusLog), TipKey = "tip.city",
            };
            sum.Coins = Progress.CityCoins(Score.Total);
            save.shekels += sum.Coins;
            save.roundsPlayed++;
            save.totalDestroyed += destroyed;
            save.bestChain = Mathf.Max(save.bestChain, Chain.LongestChain);
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
            if (item.Kind == "explosive" && save.Count(item.Grants) > 0) return false;   // unlocked already
            if (item.Kind == "spray" || item.Kind == "cap" || item.Kind == "stencil" || item.Kind == "maker")
            {
                // Graffiti gear: bought once, then a tap puts it on the can.
                if (!save.Owns(item))
                {
                    if (save.shekels < item.Coins) return false;
                    save.shekels -= item.Coins;
                    save.styles.Add(item.Id);
                    Sfx.I.Coin();
                }
                else Sfx.I.Confirm();
                if (item.Kind == "spray") save.sprayColor = item.Grants;
                else if (item.Kind == "cap") save.sprayCap = item.Grants;
                else if (item.Kind == "stencil") save.stencil = item.Grants;
                SaveService.Save();
                Player.RefreshCan();
                return true;
            }
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
                case "explosive": save.Add(item.Grants, 1); break;
                case "lighter": save.lighters.Add(item.Grants); save.lighter = item.Grants; break;
                case "upgrade": save.upgrades.Add(item.Grants); break;
            }
            SaveService.Save();
            Sfx.I.Coin();
            // Bought from inside a level: an explosive goes straight into the pocket.
            if (Current == Mode.Level && item.Kind == "explosive") RefreshPocket();
            return true;
        }

        /// <summary>Refills the pocket with what is now unlocked, keeping the selected kind in the hand.</summary>
        public void RefreshPocket()
        {
            var keep = Pocket.Current != null ? Pocket.Current.id : null;
            FillPocket();
            for (int i = 0; i < Pocket.Slots.Count; i++) if (Pocket.Slots[i].id == keep) Pocket.Selected = i;
        }

        /// <summary>The shop from inside a level: the game pauses under it, and BACK returns to the pause menu.</summary>
        public void OpenShopInLevel()
        {
            if (Current != Mode.Level || Round.State == RoundState.Results) return;
            if (!Paused) TogglePause();
            if (Paused) UI.ShowShop(true);
        }

        /// <summary>The next stencil owned (and every drawing and picture made), then none.</summary>
        public void CycleStencil()
        {
            var save = SaveService.Data;
            var ids = new List<string> { "" };
            foreach (var st in Graffiti.Stencils) if (save.Owns(Shop.Get("stencil_" + st.Id))) ids.Add(st.Id);
            for (int i = 0; i < Graffiti.CustomSlots; i++) if (Stencils.Has(Stencils.DrawingId(i))) ids.Add(Stencils.DrawingId(i));
            for (int i = 0; i < Graffiti.PictureSlots; i++) if (Stencils.Has(Stencils.PictureId(i))) ids.Add(Stencils.PictureId(i));
            if (ids.Count == 1) { UI.Toast(Loc.T("toast.buystencil")); return; }
            int at = ids.IndexOf(save.stencil);
            save.stencil = ids[(at + 1) % ids.Count];
        }

        public void CycleCap()
        {
            var save = SaveService.Data;
            var ids = new List<string>();
            foreach (var c in Graffiti.Caps) if (save.Owns(Shop.Get("cap_" + c.Id))) ids.Add(c.Id);
            if (ids.Count <= 1) { UI.Toast(Loc.T("toast.buycap")); return; }
            save.sprayCap = ids[(ids.IndexOf(save.sprayCap) + 1) % ids.Count];
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

        /// <summary>?shot=title|map|mapall|shop|play|boom|daily|show|gadgets|level&lt;n&gt; on the web page URL.</summary>
        System.Collections.IEnumerator RunShot(string mode)
        {
            SaveService.Data.disclaimerSeen = true;
            SaveService.Data.tutorialDone = true;
            if (mode == "title") { GoHome(); yield break; }
            if (mode == "map") { GoMap(); yield break; }
            if (mode == "shop") { GoHome(); UI.ShowShop(); yield break; }
            if (mode == "mapall") { SaveService.Data.unlocked = Levels.All.Length; GoMap(); yield break; }
            if (mode == "daily") { StartDaily(); yield break; }
            if (mode == "city") { StartCity(); yield break; }
            if (mode == "citytower" || mode == "citypark" || mode == "citysite")
            {
                StartCity();
                yield return new WaitForSeconds(1f);
                if (mode == "citytower") Player.Teleport(new Vector3(-20f, 0.16f, 6f), 262f, -12f);
                else if (mode == "citypark") Player.Teleport(new Vector3(-6f, 0.16f, 26f), 20f, 8f);
                else Player.Teleport(new Vector3(-21f, 0.16f, -29f), 222f, 6f);
                yield break;
            }
            if (mode == "spray")
            {
                // Paint on the back wall of the flats on the corner block: a few strokes, then two stencils.
                var save = SaveService.Data;
                foreach (var id in new[] { "stencil_boom", "stencil_heart", "spray_yellow", "spray_neonpink", "cap_fat" }) if (!save.styles.Contains(id)) save.styles.Add(id);
                save.sprayColor = "red";
                save.sprayCap = "fat";
                save.stencil = "boom";
                StartCity();
                yield return new WaitForSeconds(1f);
                Player.Teleport(new Vector3(51f, 0.16f, -44.2f), 180f, 8f);
                yield return null;
                Player.SetCan(true);
                yield return new WaitForSeconds(0.5f);
                var eye = Player.Cam.transform.position;
                save.sprayColor = "yellow";
                Spray.I.Stamp(new Ray(eye, new Vector3(50.5f, 3.6f, -48f) - eye));
                yield return new WaitForSeconds(1f);
                save.stencil = "heart";
                save.sprayColor = "neonpink";
                Spray.I.Stamp(new Ray(eye, new Vector3(54.5f, 1.6f, -48f) - eye));
                yield return new WaitForSeconds(1f);
                // Freehand strokes, as if SPRAY were held (the player releases the can when it is not).
                save.sprayColor = "red";
                Player.RefreshCan();
                Spray.I.HeldForShot = true;
                for (int f = 0; f < 40; f++)
                {
                    float t = f / 39f;
                    Player.Teleport(Player.transform.position, 140f + t * 50f, 12f + Mathf.Sin(t * 7f) * 6f);
                    yield return null;
                }
                Spray.I.HeldForShot = false;
                yield break;
            }
            if (mode == "shopgraffiti") { GoHome(); UI.ShopTab = "graffiti"; UI.ShowShop(); yield break; }
            if (mode == "maker")
            {
                var save = SaveService.Data;
                if (!save.styles.Contains("maker")) save.styles.Add("maker");
                string smile = "....................########......############...####kk##kk####..####kk##kk####.####################################w######w#######kw######wk#######kkwwwwkk####.#####kkkk#####..##############...############......########....................................";
                Stencils.Save(Stencils.DrawingId(0), Stencils.Smooth(Graffiti.FromGrid("custom0", "Drawing", smile), Graffiti.Color("red")));
                GoHome();
                UI.ShowStudio(null);
                yield break;
            }
            if (mode == "picture" || mode == "picturestamp")
            {
                // A made-up logo on a white backdrop: a red disc with a yellow ring, as if it came from the photos.
                int px = 120;
                var rgba = new byte[px * px * 4];
                for (int y = 0; y < px; y++)
                    for (int x = 0; x < px; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(px / 2f, px / 2f)) / (px / 2f);
                        var c = d < 0.55f ? new Color32(200, 30, 30, 255) : d < 0.75f ? new Color32(250, 200, 30, 255) : new Color32(250, 250, 248, 255);
                        if (d < 0.25f && Mathf.Abs(x - y) < 6) c = new Color32(20, 20, 20, 255);
                        int o = (y * px + x) * 4;
                        rgba[o] = c.r; rgba[o + 1] = c.g; rgba[o + 2] = c.b; rgba[o + 3] = 255;
                    }
                if (mode == "picture") { GoHome(); UI.ShowStudio(null); UI.OnPicture(rgba, px, px, 0); yield break; }
                var save = SaveService.Data;
                var st = Graffiti.PictureFrom(rgba, px, px, Graffiti.PictureId(0), "Picture 1", true, Graffiti.PictureSharp);
                Stencils.Save(Stencils.PictureId(0), Stencils.FromRgba(st.Rgba, st.PicWidth, st.PicHeight));
                save.stencil = Stencils.PictureId(0);
                StartCity();
                yield return new WaitForSeconds(1f);
                Player.Teleport(new Vector3(51f, 0.16f, -44.2f), 180f, -4f);
                yield return null;
                Player.SetCan(true);
                yield return new WaitForSeconds(0.5f);
                var eye = Player.Cam.transform.position;
                Spray.I.Stamp(new Ray(eye, new Vector3(51f, 2.6f, -48f) - eye));
                yield break;
            }
            if (mode == "fireworks")
            {
                // One of each firework on the road in front of the plaza, then the show.
                StartCity();
                yield return new WaitForSeconds(1.5f);
                Player.Teleport(new Vector3(0f, 0.16f, -30.5f), 0f, -22f);
                string[] kinds = { "fountain", "candle", "cake", "mortar", "rocket", "rocket" };
                for (int i = 0; i < kinds.Length; i++)
                    LiveExplosive.SpawnUnlit(Catalog.Explosive(kinds[i]), new Vector3(-6f + i * 2.4f, 0.05f, -20f), Quaternion.identity, Placement.Placed);
                yield return new WaitForSeconds(1f);
                LaunchShow();
                yield break;
            }
            if (mode == "bursts")
            {
                // Every kind of shell, one after another, high over the plaza.
                StartCity();
                yield return new WaitForSeconds(1.5f);
                Player.Teleport(new Vector3(0f, 0.16f, -30.5f), 0f, -30f);
                foreach (ShellType t in System.Enum.GetValues(typeof(ShellType)))
                {
                    Fireworks.Burst(new Vector3(Random.Range(-8f, 8f), 22f, 6f), t, 1f);
                    yield return new WaitForSeconds(1.2f);
                }
                yield break;
            }
            if (mode == "inshop")
            {
                StartCity();
                yield return new WaitForSeconds(1f);
                OpenShopInLevel();
                yield break;
            }
            if (mode == "show")
            {
                // Six rockets in an arc in front of the player, then the show.
                StartCity();
                yield return new WaitForSeconds(1.5f);
                var rocket = Catalog.Explosive("rocket");
                for (int i = 0; i < 6; i++)
                    LiveExplosive.SpawnUnlit(rocket, new Vector3(-5f + i * 2f, 0.05f, -21f + Mathf.Abs(i - 2.5f)), Quaternion.identity, Placement.Placed);
                yield return new WaitForSeconds(1f);
                LaunchShow();
                yield break;
            }
            if (mode == "gadgets")
            {
                // A timer charge and two remote charges round the van; the remotes go when the timer does.
                StartLevel(4);
                yield return new WaitForSeconds(1.5f);
                var timer = LiveExplosive.Spawn(Catalog.Explosive("timer"), new Vector3(3.5f, 0.05f, 3f), Quaternion.identity, 4f, Placement.Placed);
                var remote = Catalog.Explosive("remote");
                LiveExplosive.SpawnUnlit(remote, new Vector3(-4f, 0.05f, 8f), Quaternion.identity, Placement.Placed);
                LiveExplosive.SpawnUnlit(remote, new Vector3(-3.4f, 0.05f, 8.2f), Quaternion.identity, Placement.Placed);
                yield return new WaitForSeconds(4.5f);
                DetonateRemotes();
                yield break;
            }
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
