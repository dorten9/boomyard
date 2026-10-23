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
        Near(ScoreCalculator.CoinsFromDamage(200000), 4000, "2% coins");
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

        var p = new Pocket();
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
        Check(Catalog.Explosives.Length == 14, "14 explosives");
        Check(Catalog.Objects.Length == 28, "28 objects");
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
