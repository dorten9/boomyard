using System.Collections.Generic;
using BoomYard.Core;
using BoomYard.UI;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>The bill at the end of a round (spec §12).</summary>
    public sealed class RoundSummary
    {
        public ArenaDef Arena;
        public long Total, Base, ChainExtra, PlacementExtra, Bonuses;
        public bool CleanSweep;
        public int LongestChain, Destroyed, Objects, Stars, ContainedHits;
        public long PreviousBest;
        public bool Record;
        public long Coins;
        public string TipKey;
        public List<BonusKind> BonusLog;
    }

    /// <summary>
    /// The game: builds the arena, the player and the UI, runs the round (spec §16 RoundController) and keeps the
    /// score. Title → round (90 s) → results → again.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        public static GameController I { get; private set; }
        public static bool ReduceMotion { get { return SaveService.Data != null && SaveService.Data.settings.reduceMotion; } }

        public const int FreeBlackCats = 4;

        public RoundModel Round { get; private set; }
        public RoundScore Score { get; private set; }
        public ChainTracker Chain { get; private set; }
        public WindModel Wind { get; private set; }
        public Pocket Pocket { get; private set; }
        public Arena Arena { get; private set; }
        public ArenaDef ArenaDef { get { return Arena != null ? Arena.Def : Catalog.Arena(1); } }
        public GameUI UI { get; private set; }
        public Player Player { get; private set; }
        public bool Paused { get; private set; }
        public RoundSummary Last { get; private set; }
        public int ArenaNumber = 1;

        readonly Dictionary<int, int> killsPerBlast = new Dictionary<int, int>();
        readonly Dictionary<string, int> pocketStart = new Dictionary<string, int>();
        float idle, lastCloseCall = -10f;
        int seed;

        void Awake()
        {
            I = this;
            var save = SaveService.Load();
            Loc.Init(save.settings.lang);
            Quality.Apply(Quality.Detect());
            Physics.defaultMaxDepenetrationVelocity = 6f;
            gameObject.AddComponent<Sfx>().Volume = save.settings.sfxVolume;
            gameObject.AddComponent<FireGrid>();

            Round = new RoundModel();
            Round.Changed += OnRoundState;
            Score = new RoundScore();
            Chain = new ChainTracker();
            Pocket = new Pocket();
            seed = Daily.Seed(System.DateTime.Now) + save.roundsPlayed;
            Wind = new WindModel(seed);

            BuildArena();
            Player = Player.Create(Arena.Spawn, Arena.SpawnYaw);
            Player.ResetForRound(Arena.Spawn, Arena.SpawnYaw, Catalog.Lighter(save.lighter), seed);
            Player.Sensitivity = save.settings.sensitivity;
            FillPocket();

            UI = GameUI.Create(this);
            UI.ShowTitle();
        }

        void BuildArena()
        {
            if (Arena != null && Arena.Root != null) Destroy(Arena.Root);
            foreach (var live in LiveExplosive.Active.ToArray()) if (live != null) Destroy(live.gameObject);
            FireGrid.I.Clear();
            var fx = GameObject.Find("Fx");
            if (fx != null) Destroy(fx);
            Arena = ArenaBuilder.Build(ArenaNumber, Quality.Tier);
        }

        /// <summary>Four free Black Cats plus what the player owns, one item per slot (spec §08, §13).</summary>
        void FillPocket()
        {
            var save = SaveService.Data;
            Pocket.Slots.Clear();
            Pocket.Selected = 0;
            pocketStart.Clear();
            Pocket.Slots.Add(new PocketSlot { id = "blackcat", count = FreeBlackCats + save.Count("blackcat") });
            foreach (var s in save.inventory)
            {
                if (Pocket.Slots.Count >= Core.Pocket.BaseSlots) break;
                if (s.count <= 0 || s.id == "blackcat" || Catalog.Explosive(s.id) == null) continue;
                Pocket.Slots.Add(new PocketSlot { id = s.id, count = s.count });
            }
            foreach (var s in Pocket.Slots) pocketStart[s.id] = s.count;
        }

        public void StartRound()
        {
            if (Round.State == RoundState.Results || Round.State == RoundState.Settling) BuildArena();
            Time.timeScale = 1f;
            Paused = false;
            var save = SaveService.Data;
            seed = Daily.Seed(System.DateTime.Now) * 31 + save.roundsPlayed;
            Wind = new WindModel(seed);
            Score = new RoundScore();
            Chain = new ChainTracker();
            killsPerBlast.Clear();
            idle = 0f;
            FillPocket();
            Player.ResetForRound(Arena.Spawn, Arena.SpawnYaw, Catalog.Lighter(save.lighter), seed);
            Player.Controllable = true;
            Round.Start();
            UI.ShowHud();
        }

        void Update()
        {
            if (Paused) return;
            Chain.Tick(Time.time);
            var st = Round.State;
            if (st == RoundState.Playing || st == RoundState.Stunned || st == RoundState.Settling)
            {
                Wind.Update(Round.Elapsed);
                bool busy = LiveExplosive.Active.Count > 0 || FireGrid.I.AnyBurning || Player.HeldLit;
                Round.Tick(Time.deltaTime, busy);

                // Everything used and nothing left going off: no need to wait out the clock.
                if (Round.CanAct && Pocket.TotalLeft == 0 && !Player.HeldLit && LiveExplosive.Active.Count == 0)
                {
                    idle += Time.deltaTime;
                    if (idle > 2.5f) Round.Finish();
                }
                else idle = 0f;
            }
            Player.Hands.Down = Player.Knocked || Round.State == RoundState.Stunned;
        }

        // ---------- scoring ----------

        /// <summary>Damage to an object turned into shekels (called by Destructible).</summary>
        public void Credit(Destructible d, float fraction, Hit hit, bool destroyed)
        {
            var st = Round.State;
            if (st != RoundState.Playing && st != RoundState.Stunned && st != RoundState.Settling) return;
            float mult = destroyed ? Chain.OnDestroyed(Time.time, hit.ByObject) : Chain.MultiplierFor(hit.ByObject);
            long v = Score.AddDamage(d.Def.Value, fraction, d.Def.Material, hit.Placement, mult);
            if (destroyed)
            {
                Score.ObjectsDestroyed++;
                if (hit.BlastId != 0 && !hit.ByObject)
                {
                    int k;
                    killsPerBlast.TryGetValue(hit.BlastId, out k);
                    killsPerBlast[hit.BlastId] = ++k;
                    if (k == ScoreCalculator.MultiCount) Bonus(BonusKind.Multi);
                }
            }
            UI.Popup(d.transform.TransformPoint(d.FirePoint), v, destroyed && hit.ByObject ? mult : 0f);
        }

        void Bonus(BonusKind kind)
        {
            Score.AddBonus(kind);
            UI.Bonus(kind);
        }

        public void OnIgnite(ExplosiveDef def) { }

        public void OnRelease(LiveExplosive live)
        {
            if (live.ReleasedWith <= ScoreCalculator.LastSecondFuse && live.Def.Fuse > 0f) Bonus(BonusKind.LastSecond);
        }

        public void OnPlayerDetonation(LiveExplosive live, Placement placement) { }

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

        // ---------- flow ----------

        void OnRoundState(RoundState s)
        {
            if (s != RoundState.Results) return;
            Player.Controllable = false;
            Last = Summarize();
            UI.ShowResults(Last);
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
            Score.CleanSweep = total > 0 && destroyed == total;
            var rec = save.Arena(ArenaDef.Number);
            var sum = new RoundSummary
            {
                Arena = ArenaDef,
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
                PreviousBest = rec.best,
                BonusLog = new List<BonusKind>(Score.BonusLog),
            };
            sum.Stars = ScoreCalculator.Stars(ArenaDef, sum.Total);
            sum.Record = sum.Total > rec.best;
            sum.Coins = ScoreCalculator.CoinsFromDamage(sum.Total);
            sum.TipKey = Score.ContainedHits == 0 ? "tip.contain" : Chain.LongestChain < 2 ? "tip.chain" : "tip.again";

            // Book it: best, stars, coins, used items.
            if (sum.Record) rec.best = sum.Total;
            rec.stars = Mathf.Max(rec.stars, sum.Stars);
            save.shekels += sum.Coins;
            save.roundsPlayed++;
            foreach (var slot in Pocket.Slots)
            {
                int start;
                if (!pocketStart.TryGetValue(slot.id, out start)) continue;
                int used = start - slot.count;
                if (slot.id == "blackcat") used = Mathf.Max(0, used - FreeBlackCats);
                if (used > 0) save.Add(slot.id, -used);
            }
            SaveService.Save();
            return sum;
        }

        public void TogglePause()
        {
            if (Round.State == RoundState.Results || Round.State == RoundState.Loadout) return;
            Paused = !Paused;
            Time.timeScale = Paused ? 0f : 1f;
            AudioListener.pause = Paused;
            UI.ShowPause(Paused);
        }

        public void QuitToTitle()
        {
            Paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Round = new RoundModel();
            Round.Changed += OnRoundState;
            BuildArena();
            Player.ResetForRound(Arena.Spawn, Arena.SpawnYaw, Catalog.Lighter(SaveService.Data.lighter), seed);
            Player.Controllable = false;
            FillPocket();
            UI.ShowTitle();
        }

        public void SetLanguage(string lang)
        {
            SaveService.Data.settings.lang = lang;
            SaveService.Save();
            Loc.Init(lang);
            UI.Rebuild();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && (Round.State == RoundState.Playing || Round.State == RoundState.Stunned) && !Paused) TogglePause();
            if (paused) SaveService.Save();
        }
    }
}
