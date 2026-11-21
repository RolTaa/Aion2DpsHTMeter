using Aion2DpsHTMeter.App.Services;
using AionMeter.Core.Combat;

namespace AionMeter.Tests;

/// <summary>Aion2DpsHTMeter additions: which fight the overlay shows, and the kill-time estimate.</summary>
public class OverlayLogicTests
{
    private const int Artifact = 2600084, Boss = 2300243, Mob = 2000002;

    private static EncounterSnapshot Fight(int npc, bool boss) => new(
        Guid.NewGuid(), "fight", null, DateTimeOffset.UnixEpoch, 0, 1_000, true, default, 0, 0,
        boss ? new BossSnapshot(1, npc, "boss", 100, 100) : null, [], TargetCode: npc);

    [Fact]
    public void Fights_are_sorted_into_artifact_boss_and_trash()
    {
        var filter = new FightFilter(new HashSet<int> { Artifact });
        Assert.Equal(TargetKinds.AbyssArtifact, filter.KindOf(Fight(Artifact, boss: true)));
        Assert.Equal(TargetKinds.Boss, filter.KindOf(Fight(Boss, boss: true)));
        Assert.Equal(TargetKinds.Trash, filter.KindOf(Fight(Mob, boss: false)));
    }

    [Fact]
    public void A_fight_not_chosen_keeps_the_last_chosen_one_on_screen()
    {
        var filter = new FightFilter(new HashSet<int>());
        var boss = Fight(Boss, boss: true);
        var trash = Fight(Mob, boss: false);
        Func<Guid, EncounterSnapshot?> byId = id => id == boss.Id ? boss : null;

        Assert.Null(filter.Pick(trash, TargetKinds.Boss, byId));       // nothing chosen seen yet
        Assert.Same(boss, filter.Pick(boss, TargetKinds.Boss, byId));
        Assert.Same(boss, filter.Pick(trash, TargetKinds.Boss, byId)); // trash after the boss
        Assert.Same(trash, filter.Pick(trash, TargetKinds.Boss | TargetKinds.Trash, byId));

        filter.Forget();
        Assert.Null(filter.Pick(trash, TargetKinds.Boss, byId));
    }

    [Fact]
    public void Kill_time_follows_the_recent_hp_loss()
    {
        var est = new KillTimeEstimator();
        var fight = Guid.NewGuid();
        Assert.Null(est.Update(fight, 0, 1_000_000));          // one sample says nothing
        Assert.Null(est.Update(fight, 2_000, 980_000));        // too short to trust
        var left = est.Update(fight, 10_000, 900_000);         // 100k lost in 10 s → 90 s left
        Assert.Equal(90, left!.Value.TotalSeconds, precision: 3);
    }

    [Fact]
    public void Kill_time_starts_over_on_a_new_fight_or_a_heal()
    {
        var est = new KillTimeEstimator();
        var fight = Guid.NewGuid();
        est.Update(fight, 0, 1_000_000);
        est.Update(fight, 10_000, 900_000);
        Assert.Null(est.Update(fight, 11_000, 950_000));       // the boss healed: no estimate yet
        Assert.Null(est.Update(Guid.NewGuid(), 12_000, 500_000));
    }

    [Fact]
    public void Kill_time_only_uses_the_last_twenty_seconds()
    {
        var est = new KillTimeEstimator();
        var fight = Guid.NewGuid();
        est.Update(fight, 0, 1_000_000);
        est.Update(fight, 10_000, 500_000);                    // fast phase: 50k/s
        est.Update(fight, 30_000, 400_000);                    // slow: 5k/s
        var left = est.Update(fight, 40_000, 350_000);         // window 10 s..40 s: 150k in 30 s → no, samples older than 20 s drop
        Assert.Equal(350_000 / (150_000 / 30.0), left!.Value.TotalSeconds, precision: 3);
    }
}
