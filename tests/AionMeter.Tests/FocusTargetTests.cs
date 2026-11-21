using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.Tests;

/// <summary>Aion2DpsHTMeter additions: damage on the main target (most HP) and the DPS of the last minute.</summary>
public class FocusTargetTests
{
    private const uint Me = 100, Mate = 101, Big = 500, Small = 501;

    private static CombatTracker Fight()
    {
        var t = new CombatTracker(GameData.Empty, new MeterOptions
        {
            // Every NPC hit counts (in boss-only mode a boss pulled during trash starts a fight of its own), and no
            // fight ends by idling while the test jumps ahead in time.
            TargetMode = TargetMode.All,
            IdleTimeoutMs = 600_000,
            BossIdleTimeoutMs = 600_000,
        });
        t.Process(new SelfIdentifiedEvent(0, Me, "Hikari", 1304, GameClass.Sorcerer));
        t.Process(new PlayerSeenEvent(0, Mate, "Tsubasa", 1304, GameClass.Templar));
        t.Process(new NpcSeenEvent(0, Big, 2000003, 5_000_000));
        t.Process(new NpcSeenEvent(0, Small, 2000002, 50_000));
        return t;
    }

    [Fact]
    public void The_npc_with_the_most_hp_is_the_focus_and_its_damage_is_counted_apart()
    {
        var t = Fight();
        t.Process(new DamageEvent(1_000, Me, Small, 16040000, 40_000, HitFlags.None)); // more damage, but a small mob
        t.Process(new DamageEvent(1_500, Me, Big, 16040000, 10_000, HitFlags.None));
        t.Process(new DamageEvent(2_000, Mate, Big, 12010000, 30_000, HitFlags.None));

        var snap = t.Snapshot(null, 3_000)!;
        Assert.Equal(Big, snap.Focus!.ActorId);
        var me = snap.Combatants.Single(c => c.ActorId == Me);
        Assert.Equal(50_000, me.Damage);       // everything
        Assert.Equal(10_000, me.FocusDamage);  // the big one only
        Assert.Equal(30_000, snap.Combatants.Single(c => c.ActorId == Mate).FocusDamage);
    }

    [Fact]
    public void Recent_dps_covers_only_the_last_minute()
    {
        var t = Fight();
        t.Process(new DamageEvent(0, Me, Big, 16040000, 600_000, HitFlags.None));       // second 0: long ago
        t.Process(new DamageEvent(100_000, Me, Big, 16040000, 60_000, HitFlags.None));  // second 100

        var me = t.Snapshot(null, 119_500)!.Combatants.Single(c => c.ActorId == Me);    // window: seconds 60..119
        Assert.Equal(60_000 / 60.0, me.RecentDps, precision: 6);
    }

    [Fact]
    public void Early_in_a_fight_recent_dps_is_over_the_seconds_so_far()
    {
        var t = Fight();
        t.Process(new DamageEvent(0, Me, Big, 16040000, 10_000, HitFlags.None));
        t.Process(new DamageEvent(9_000, Me, Big, 16040000, 10_000, HitFlags.None));

        var me = t.Snapshot(null, 9_500)!.Combatants.Single(c => c.ActorId == Me);      // seconds 0..9
        Assert.Equal(20_000 / 10.0, me.RecentDps, precision: 6);
    }

    [Fact]
    public void A_watched_npc_is_reported_from_its_spawn_and_hp_without_being_hit()
    {
        const uint Core = 700;
        const int CoreCode = 2600078;
        var t = Fight();
        t.Process(new NpcSeenEvent(0, Core, CoreCode, 9_000_000));
        t.Process(new NpcHpEvent(1_000, Core, 6_300_000, 9_000_000));

        var core = Assert.Single(t.NpcsOnMap(new HashSet<int> { CoreCode }));
        Assert.Equal(Core, core.ActorId);
        Assert.Equal(0.7, core.HpFraction, precision: 6);
        Assert.Null(t.Snapshot(null, 2_000)); // nobody hit anything: no fight
    }
}
