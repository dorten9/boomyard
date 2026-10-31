using System;
using BoomYard.Core;

/// <summary>Checks of the pure game core. Run with Tools/SimTest/run.sh; exits 1 on any failure.</summary>
static class CoreTests
{
    static int failed, passed;

    static void Check(bool ok, string what)
    {
        if (ok) passed++;
        else { failed++; Console.WriteLine("FAIL: " + what); }
    }

    static void Near(double a, double b, string what) { Check(Math.Abs(a - b) < 1e-3, what + " (got " + a + ", want " + b + ")"); }

    static int Main()
    {
        Scoring();
        Chains();
        Fuses();
        Lighters();
        WindChecks();
        Rounds();
        CatalogChecks();
        MetaTests.Run(Check);
        Console.WriteLine(passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }

    static void Scoring()
    {
        Near(ScoreCalculator.Damage(350, 1f, Material.Glass, Placement.Thrown, 1f), 315, "window thrown");
        Near(ScoreCalculator.Damage(18000, 1f, Material.Metal, Placement.Contained, 1f), 42120, "van contained");
        Near(ScoreCalculator.Damage(18000, 0.5f, Material.Metal, Placement.Thrown, 1f), 11700, "half a van");
        Near(ScoreCalculator.Damage(600, 1f, Material.Metal, Placement.Thrown, 2f), 1560, "bin in a chain x2");
        Near(ScoreCalculator.Damage(600, 0f, Material.Metal, Placement.Thrown, 2f), 0, "no damage");

        var s = new RoundScore();
        long got = s.AddDamage(1000, 1f, Material.Wood, Placement.Contained, 2f);
        Near(got, 3600, "breakdown total");
        Near(s.Base, 1000, "breakdown base");
        Near(s.PlacementExtra, 800, "breakdown placement");
        Near(s.ChainExtra, 1800, "breakdown chain");
        s.AddBonus(BonusKind.CloseCall);
        s.AddBonus(BonusKind.HandBurn);
        Near(s.Total, 3600 + 5000 - 2000, "bonuses");
        s.CleanSweep = true;
        Near(s.Total, (3600 + 5000 - 2000) * 1.5, "clean sweep");

        var yard = Catalog.Arena(1);
        Check(ScoreCalculator.Stars(yard, 59999) == 2, "2 stars under the threshold");
        Check(ScoreCalculator.Stars(yard, 60000) == 3, "3 stars at the threshold");
        Check(ScoreCalculator.Stars(yard, 1000) == 0, "no stars");
        Near(ScoreCalculator.CoinsFromDamage(150000), 1000, "a coin per $150 of damage");
        Near(ScoreCalculator.CoinsFromDamage(76), 1, "coins round to the nearest");
    }

    static void Chains()
    {
        var c = new ChainTracker();
        Near(c.OnDestroyed(0f, false), 1f, "direct hit x1");
        Near(c.OnDestroyed(0.5f, true), 1.5f, "first link");
        Near(c.OnDestroyed(1.0f, true), 2f, "second link");
        Near(c.MultiplierFor(true), 2f, "partial damage pays the chain");
        Near(c.MultiplierFor(false), 1f, "partial player damage x1");
        Near(c.OnDestroyed(1.2f, false), 1f, "player hit inside a chain");
        Check(c.Links == 2, "player hit does not break the chain");
        c.Tick(3.9f);
        Check(c.Links == 2, "chain alive inside the window");
        c.Tick(4.3f);
        Check(c.Links == 0, "chain breaks after 3 s");
        Check(c.LongestChain == 2, "longest chain kept");
        for (int i = 0; i < 20; i++) c.OnDestroyed(10f + i * 0.1f, true);
        Near(c.Multiplier, 6f, "capped at x6");
        Check(c.LongestChain == 20, "longest chain updated");
        Near(c.LongestStart, 10f, "longest chain start");
    }

    static void Fuses()
    {
        Check(Fuse.Classify(3f, 1f) == FuseZone.Early, "early is red");
        Check(Fuse.Classify(1.6f, 1f) == FuseZone.Land, "orange lands and goes");
        Check(Fuse.Classify(0.8f, 1f) == FuseZone.Air, "green bursts in the air");
        Check(Fuse.Classify(0.05f, 1f) == FuseZone.Hand, "in the hand");
        Near(Fuse.TotalBurn(Catalog.Explosive("string50")), 1.5 + 0.1 * 49, "string of 50 burns longer");
        Near(Fuse.TotalBurn(Catalog.Explosive("m80")), 3, "m80 fuse");
    }

    static void Lighters()
    {
        int blown = 0;
        var ig = new Ignition(Catalog.Lighter("basic"), 7);
        for (int i = 0; i < 2000; i++) if (ig.Strike(1f, false) == FlameState.BlownOut) blown++;
        Check(blown > 480 && blown < 720, "basic lighter blows out ~30% facing the wind (" + blown + "/2000)");

        blown = 0;
        for (int i = 0; i < 500; i++) if (ig.Strike(0f, false) == FlameState.BlownOut) blown++;
        Check(blown == 0, "back to the wind lights every time");

        Check(ig.Strike(0f, true) == FlameState.BlownOut, "running puts it out");

        var z = new Ignition(Catalog.Lighter("zippo"), 1);
        z.Strike(1f, false);
        Check(z.Flame == FlameState.Lit, "zippo ignores the wind");
        Check(!z.Hold(0.3f, false), "zippo not caught at 0.3 s");
        Check(z.Hold(0.11f, false), "zippo caught at 0.4 s");
        Check(z.FuseCaught, "caught flag");
        z.Release();
        Check(z.FuseCaught, "a caught fuse stays lit after letting go");
        z.NextFuse();
        Check(!z.FuseCaught, "next fuse starts unlit");

        var t = new Ignition(Catalog.Lighter("turbo"), 1);
        t.Strike(1f, false);
        Check(t.Hold(0.001f, false), "turbo catches at once");

        var b = new Ignition(Catalog.Lighter("basic"), 3);
        b.Strike(0f, false);
        b.Hold(0.3f, false);
        b.Release();
        Near(b.CatchProgress, 0, "letting go early starts the catch over");
    }

    static void WindChecks()
    {
        var a = new WindModel(Daily.Seed(new DateTime(2026, 10, 23)));
        var b = new WindModel(Daily.Seed(new DateTime(2026, 10, 23)));
        Near(a.Direction, b.Direction, "same seed, same wind");
        Check(a.Speed >= WindModel.MinSpeed && a.Speed <= WindModel.MaxSpeed, "speed in range");
        Check(!a.Update(19.9f), "no change before 20 s");
        Check(a.Update(20f), "changes at 20 s");
        Near(a.Exposure(a.Direction), 0, "back to the wind is sheltered");
        Check(a.Exposure(a.Direction + 180f) > 0f, "facing the wind is exposed");
        Near(WindModel.DeltaAngle(350f, 10f), 20, "angle wraps");
    }

    static void Rounds()
    {
        var r = new RoundModel();
        Check(r.State == RoundState.Loadout, "starts in loadout");
        r.Start();
        Check(r.CanAct, "playing");
        Check(r.Limit == RoundModel.Duration, "default limit");
        r.Stun(RoundModel.StunSeconds);
        Check(r.State == RoundState.Stunned && !r.CanAct, "stunned");
        r.Tick(2.6f, false);
        Check(r.State == RoundState.Playing, "back up after the stun");
        r.Tick(90f, true);
        Check(r.State == RoundState.Settling, "time up settles");
        r.Tick(1f, true);
        Check(r.State == RoundState.Settling, "waits for what is still burning");
        r.Tick(1f, false);
        Check(r.State == RoundState.Results, "results");
        var lr = new RoundModel();
        lr.Start(30f);
        lr.Frozen = true;
        lr.Tick(10f, false);
        Check(lr.TimeLeft == 30f, "frozen clock (tutorial)");
        lr.Frozen = false;
        lr.Tick(10f, false);
        Check(lr.TimeLeft == 20f, "level time limit");

        var u = new Pocket();
        u.Slots.Add(new PocketSlot { id = "m80", count = 1 });
        for (int i = 0; i < 50; i++) u.Take();
        Check(u.Take() == "m80" && u.TotalLeft > 0, "unlimited pocket never runs out");

        var p = new Pocket { Unlimited = false };
        p.Slots.Add(new PocketSlot { id = "blackcat", count = 1 });
        p.Slots.Add(new PocketSlot { id = "m80", count = 2 });
        Check(p.Take() == "blackcat", "take the selected item");
        Check(p.Take() == null, "empty slot");
        p.SelectNextAvailable();
        Check(p.Current.id == "m80", "moves to the next item");
        Check(p.TotalLeft == 2, "items left");

        Near(Economy.PropPrice(12000, 2), 17280, "props 20% dearer each");
        Near(Economy.PackPrice(1800, 5), 7900, "M-80 pack price");
        Check(Daily.Seed(new DateTime(2026, 10, 23)) == 20261023, "daily seed");
    }

    static void CatalogChecks()
    {
        Check(Catalog.Explosives.Length == 35, "35 explosives and fireworks");
        Check(Catalog.Objects.Length >= 28, "28+ objects");
        Check(Catalog.Arenas.Length == 6, "6 arenas");
        foreach (var o in Catalog.Objects)
        {
            Check(o.Hp > 0 && o.Value > 0, o.Id + " has hp and value");
            if ((o.Reaction & Reaction.Explode) != 0) Check(o.BlastRadius > 0 && o.BlastDamage > 0, o.Id + " explodes with a radius");
        }
        float last = 0f;
        foreach (var id in new[] { "blackcat", "m80", "dynamite" })
        {
            var e = Catalog.Explosive(id);
            Check(e.Fuse > last, "stronger items have longer fuses: " + id);
            last = e.Fuse;
        }
    }
}

static class MetaTests
{
    static void GraffitiTests(System.Action<bool, string> check)
    {
        foreach (var it in Shop.Items)
        {
            if (it.Kind == "spray") check(Graffiti.Color(it.Grants).Id == it.Grants, "spray colour exists: " + it.Grants);
            if (it.Kind == "cap") check(Graffiti.Cap(it.Grants).Id == it.Grants, "cap exists: " + it.Grants);
            if (it.Kind == "stencil") check(Graffiti.Find(it.Grants) != null, "stencil exists: " + it.Grants);
        }
        foreach (var c in Graffiti.Colors) check(Shop.Get("spray_" + c.Id) != null, "every colour is sold: " + c.Id);
        foreach (var c in Graffiti.Caps) check(Shop.Get("cap_" + c.Id) != null, "every cap is sold: " + c.Id);
        foreach (var st in Graffiti.Stencils)
        {
            check(Shop.Get("stencil_" + st.Id) != null, "every stencil is sold: " + st.Id);
            check(st.Width <= 50 && st.Height <= 16, "stencil fits a wall: " + st.Id);
            var cells = Graffiti.Cells(st);
            check(cells.Count > 20, "stencil has a picture: " + st.Id);
            bool codesOk = true;
            foreach (var row in st.Rows) foreach (var ch in row) if (ch != '.' && ch != '#' && Graffiti.Fixed(ch) == null) codesOk = false;
            check(codesOk, "stencil uses known colours: " + st.Id);
        }
        // Lettering: every letter drawn, a shadow under it.
        var boom = Graffiti.Lettering("t", "T", "BOOM");
        check(boom.Width == 25 && boom.Height == 8, "lettering size");
        check(boom.At(0, 0) == '#' && boom.At(1, 1) != '.', "letters with a shadow");
        int letters = 0;
        foreach (var ch in "ABCDEFGHIJKLMNOPQRSTUVWXYZ!")
        {
            var one = Graffiti.Lettering("x", "X", ch.ToString());
            int painted = 0;
            foreach (var c in Graffiti.Cells(one)) if (c.Code == '#') painted++;
            if (painted >= 6) letters++;
        }
        check(letters == 27, "the stencil font has every letter");
        // The stencil maker.
        var g = Graffiti.Blank;
        check(Graffiti.Valid(g) && Graffiti.Empty(g), "a blank custom stencil");
        g = Graffiti.Set(g, 3, 2, 'r');
        g = Graffiti.Set(g, 15, 15, '#');
        g = Graffiti.Set(g, 16, 0, '#');   // off the grid: ignored
        g = Graffiti.Set(g, 0, 0, 'z');    // not a colour: bare wall
        check(Graffiti.Valid(g) && !Graffiti.Empty(g), "drawing on it");
        var st2 = Graffiti.FromGrid("custom0", "Mine", g);
        check(st2.At(3, 2) == 'r' && st2.At(15, 15) == '#' && st2.At(0, 0) == '.', "cells land where they were drawn");
        check(Graffiti.Cells(st2).Count == 2, "two painted cells");
        check(!Graffiti.Valid("abc") && Graffiti.Empty(null), "a broken save reads as blank");
        check(Graffiti.Paint('#', Graffiti.Color("blue")).Id == "blue" && Graffiti.Paint('k', Graffiti.Color("blue")).Id == "black", "'#' is the can, letters are fixed");
        // Pictures: a red disc on a white backdrop, 200 × 100.
        int pw = 200, ph = 100;
        var img = new byte[pw * ph * 4];
        for (int y = 0; y < ph; y++)
            for (int x = 0; x < pw; x++)
            {
                int o = (y * pw + x) * 4;
                bool disc = (x - 100) * (x - 100) + (y - 50) * (y - 50) < 40 * 40;
                img[o] = 250; img[o + 1] = disc ? (byte)20 : (byte)250; img[o + 2] = disc ? (byte)20 : (byte)248; img[o + 3] = 255;
            }
        var pic = Graffiti.PictureFrom(img, pw, ph, "picture0", "P");
        check(pic.Picture && pic.Width == Graffiti.PictureSize && pic.Height == Graffiti.PictureSize / 2, "a picture is scaled to fit, keeping its shape");
        var sharp = Graffiti.PictureFrom(img, pw, ph, "picture0", "P", true, Graffiti.PictureSharp);
        check(sharp.Width == pw && sharp.Height == ph && sharp.At(0, 0) == '.' && sharp.At(100, 50) == '*', "a small picture is kept at full sharpness");
        check(pic.At(0, 0) == '.' && pic.At(pic.Width / 2, pic.Height / 2) == '*', "a plain background is cut away, the subject stays");
        var mid = Graffiti.Cells(pic).Find(c => c.X == pic.Width / 2 && c.Y == pic.Height / 2);
        check(mid.R > 200 && mid.G < 60, "a picture keeps its own colours");
        var back = Graffiti.DecodePicture(Graffiti.EncodePicture(pic), "picture0", "P");
        check(back != null && back.Width == pic.Width && Graffiti.Cells(back).Count == Graffiti.Cells(pic).Count, "a picture survives the save");
        check(Graffiti.DecodePicture("3,3,notbase64!!", "p", "P") == null && Graffiti.DecodePicture("", "p", "P") == null, "a broken picture reads as none");
        // A photo with a busy border is kept whole.
        var noise = new byte[64 * 64 * 4];
        var rnd = new System.Random(3);
        for (int i = 0; i < noise.Length; i++) noise[i] = (i % 4 == 3) ? (byte)255 : (byte)rnd.Next(256);
        var photo = Graffiti.PictureFrom(noise, 64, 64, "picture1", "P");
        check(Graffiti.Cells(photo).Count == photo.Width * photo.Height, "a photo without a plain backdrop keeps every cell");
        check(Graffiti.PictureCost(0) == 0 && Graffiti.PictureCost(1) == Graffiti.PicturePrice && Graffiti.PictureCost(5) == Graffiti.PicturePrice, "the first picture is free, then each costs coins");
        check(System.Math.Abs(Graffiti.CellSize(pic) * pic.Width - Graffiti.PictureMetres) < 0.01f, "a picture is about 2 m across on the wall");
    }

    public static void Run(System.Action<bool, string> check)
    {
        var lvl = Levels.Get(1);
        check(lvl.Stars(true, lvl.Time * 0.5f) == 3, "3 stars with half the time left");
        check(lvl.Stars(true, lvl.Time * 0.2f) == 2, "2 stars");
        check(lvl.Stars(true, 1f) == 1, "1 star");
        check(lvl.Stars(false, 99f) == 0, "no stars if not cleared");
        check(Levels.All.Length >= 10, "10 levels");
        for (int i = 0; i < Levels.All.Length; i++)
        {
            var l = Levels.All[i];
            check(l.Number == i + 1, "levels numbered in order");
            foreach (var p in l.Props) check(Catalog.Object(p.Id) != null, "level " + l.Number + " prop exists: " + p.Id);
            foreach (var k in l.Kit) check(Catalog.Explosive(k.id) != null, "level " + l.Number + " kit item exists: " + k.id);
        }
        foreach (var it in Shop.Items)
            check(it.Coins > 0 || Shop.Keepsake(it), "shop item has a price in coins: " + it.Id);
        GraffitiTests(check);
        // The city's weekly events, jobs, the streak's surprise and rare cars.
        var seen = new System.Collections.Generic.HashSet<CityEvent>();
        for (int w = 0; w < 8; w++) seen.Add(CityEvents.For(new System.DateTime(2026, 11, 2).AddDays(7 * w)));
        check(seen.Count == 4, "every city event comes round within a month");
        check(CityEvents.For(new System.DateTime(2026, 11, 2)) == CityEvents.For(new System.DateTime(2026, 11, 8)), "the event lasts the week");
        var avail = new System.Collections.Generic.Dictionary<string, int> { { "trafficlight", 16 }, { "bin", 3 } };
        var job = Contracts.Next(new System.Random(1), avail);
        check(job != null && job.Target == "trafficlight" && job.Goal >= 3 && job.Seconds > 0f && job.Reward > 0, "a job for what is standing");
        bool done = false;
        for (int i = 0; i < job.Goal; i++) { job.Count("bin"); done = job.Count("trafficlight"); }
        check(done && job.Complete, "the job is done when the count is reached");
        var late = Contracts.Next(new System.Random(2), avail);
        late.Tick(late.Seconds + 1f);
        check(late.Failed && !late.Count("trafficlight"), "a job runs out of time");
        check(Contracts.Next(new System.Random(3), new System.Collections.Generic.Dictionary<string, int>()) == null, "no job when nothing is left");
        check(Surprise.OnDay(2) && !Surprise.OnDay(0), "surprises on some streak days");
        var all = new System.Collections.Generic.List<string>();
        foreach (var it in Shop.Items) if (it.Kind == "explosive") all.Add(it.Grants);
        check(Surprise.Pick(new System.Random(4), all) == null, "no surprise left when everything is owned");
        check(!all.Contains(Surprise.Pick(new System.Random(5), new System.Collections.Generic.List<string>())) == false, "a surprise is a real item");
        foreach (var c in RareCars.All) check(Catalog.Object(c.Prop) != null, "rare car is a real car: " + c.Id);
        // Clocks with room to clear, and a bonus goal on every level but the tutorial that its props allow.
        foreach (var l in Levels.All)
        {
            if (l.Tutorial) continue;
            check(l.Time >= 90f, "level " + l.Number + " has at least a minute and a half");
            var g = Goals.For(l);
            check(g != GoalKind.None && Goals.Text(g) != "", "level " + l.Number + " has a bonus goal");
            if (g == GoalKind.HoleInOne || g == GoalKind.Contained)
            {
                bool room = false;
                foreach (var p in l.Props) { var o = Catalog.Object(p.Id); if (o != null && (o.Reaction & Reaction.Contain) != 0) room = true; }
                check(room, "level " + l.Number + " goal has something to put a charge in");
            }
        }
        check(Goals.For(Levels.Get(1)) == GoalKind.None, "no bonus goal in the tutorial");
        check(Levels.Relaxed(70f) == 105f && Levels.Relaxed(50f) == 90f && Levels.Relaxed(150f) == 225f, "the clocks are half as long again, at least 90 s");
        var lv = Levels.Get(5);
        check(Goals.Met(GoalKind.Chain, lv, 3, 0f, 0, new System.Collections.Generic.List<BonusKind>()), "a chain of 3 meets the chain goal");
        check(!Goals.Met(GoalKind.HalfClock, lv, 0, lv.Time * 0.4f, 0, new System.Collections.Generic.List<BonusKind>()), "40% of the clock is not half");
        check(Goals.Met(GoalKind.AirTime, lv, 0, 0f, 0, new System.Collections.Generic.List<BonusKind> { BonusKind.AirTime }), "air time meets its goal");
        var round = new RoundModel();
        round.Start(60f);
        round.AddTime(Levels.TargetBonusSeconds);
        check(round.TimeLeft == 63f, "a target down gives seconds back");
        var city = Levels.City();
        check(city.Show && city.Props.Length == 0 && city.Kit.Length >= 3, "the big city: no targets, a kit to start");
        check(Progress.CityCoins(150 * 600) == 100, "the city pays a sixth of the wreckage");

        var a = Missions.ForDay(20261024);
        var b = Missions.ForDay(20261024);
        check(a.Count == 3 && a[0].kind == b[0].kind && a[1].kind == b[1].kind, "same missions for the same day");
        var m = new System.Collections.Generic.List<Mission> { new Mission { kind = "destroy", goal = 5 }, new Mission { kind = "chain", goal = 3 } };
        Missions.Count(m, "destroy", null, 3);
        Missions.Count(m, "destroy", null, 4);
        check(m[0].progress == 5 && m[0].Done, "mission progress caps at the goal");
        Missions.Count(m, "chain", null, 2);
        Missions.Count(m, "chain", null, 1);
        check(m[1].progress == 2, "chain missions keep the best");

        check(DailyReward.NextDay(0, 0, new System.DateTime(2026, 10, 24)) == 0, "first daily reward");
        check(DailyReward.NextDay(20261023, 2, new System.DateTime(2026, 10, 24)) == 3, "streak continues");
        check(DailyReward.NextDay(20261024, 2, new System.DateTime(2026, 10, 24)) == -1, "claimed today");
        check(DailyReward.NextDay(20261020, 4, new System.DateTime(2026, 10, 24)) == 0, "missed day restarts");
        check(Progress.LevelCoins(lvl, true, true, 3, 150000) == 1000 + lvl.Reward * Progress.RewardShare / 100 + 75, "level coins: wreckage, part of the reward, stars");
        check(Progress.LevelCoins(lvl, false, false, 0, 150000) == 1000, "a failed round keeps the wreckage coins");

        foreach (var side in new[] { 1f, -1f })
        {
            var hand = HandRig.Fist(side);
            int bones = 0;
            foreach (var part in hand)
            {
                if (part.Kind != HandPartKind.Bone) continue;
                bones++;
                foreach (var e in new[] { part.A, part.B })
                    check(Math.Sqrt(e.X * e.X + e.Z * e.Z) - part.Diameter / 2 >= 0.009, "hand bone " + part.Name + " clears the held object");
            }
            check(bones == 14, "four fingers of three bones and a thumb of two");
        }

        check(Levels.All.Length == 50, "50 levels");
        foreach (var l in Levels.All)
        {
            check(Streets.ForLevel(l.Number).FirstLevel <= l.Number && l.Number <= Streets.ForLevel(l.Number).LastLevel, "level " + l.Number + " is on a street");
            if (l.Rule == LevelRule.Spare) check(l.Spare >= 0 && l.Spare < l.Props.Length && !l.IsTarget(l.Spare), "level " + l.Number + " spares a real prop");
            int targets = 0;
            for (int k = 0; k < l.Props.Length; k++) if (l.IsTarget(k)) targets++;
            check(targets >= 3, "level " + l.Number + " has targets");
            foreach (var p in l.Props)
            {
                check(Math.Abs(p.X) <= 12.6f, "level " + l.Number + " " + p.Id + " inside the lot");
                check((p.Z <= 12f || p.Id == "acunit") && p.Z >= -20f, "level " + l.Number + " " + p.Id + " clear of the houses");
                check(Math.Sqrt(p.X * p.X + (p.Z + 8) * (p.Z + 8)) >= 2.4, "level " + l.Number + " " + p.Id + " not on the player's start");
            }
        }
        foreach (var st in Streets.All)
        {
            check(Levels.Get(st.LastLevel).Boss, st.Name + " ends with a boss level");
            check(st.Walls.Length > 0 && st.Roof.Length == 3, st.Name + " has colours");
        }
        int prev = 0;
        foreach (var st in Streets.All) { check(st.FirstLevel == prev + 1, st.Name + " follows on"); prev = st.LastLevel; }
        check(prev == Levels.All.Length, "streets cover every level");

        var dc1 = Levels.DailyChallenge(20261025);
        var dc2 = Levels.DailyChallenge(20261025);
        check(dc1.Number == dc2.Number && dc1.Rule == dc2.Rule && dc1.Daily, "the daily challenge is the same all day");
        check(dc1.Number >= 2 && dc1.Time < Levels.Get(dc1.Number).Time, "the daily challenge is a real level with less time");
        int differ = 0;
        for (int d = 1; d <= 20; d++) if (Levels.DailyChallenge(20261100 + d).Number != dc1.Number) differ++;
        check(differ >= 15, "the daily challenge changes from day to day");
    }
}
