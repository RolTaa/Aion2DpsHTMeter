using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.Tests;

/// <summary>Aion2DpsHTMeter addition: the live snapshot carries each player's healing next to damage and damage taken.</summary>
public class HealingSnapshotTests
{
    private const uint Healer = 100, Tank = 101, Boss = 500, Pet = 600;

    [Fact]
    public void Live_snapshot_carries_healing_and_damage_taken()
    {
        var t = new CombatTracker(GameData.Empty, new MeterOptions());
        t.Process(new SelfIdentifiedEvent(0, Healer, "Hikari", 1304, GameClass.Cleric));
        t.Process(new PlayerSeenEvent(0, Tank, "Tsubasa", 1304, GameClass.Templar));
        t.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        t.Process(new SummonSeenEvent(0, Pet, Healer, null, 0));

        t.Process(new DamageEvent(1_000, Healer, Boss, 17010000, 10_000, HitFlags.None));
        t.Process(new DamageEvent(1_500, Tank, Boss, 12010000, 20_000, HitFlags.None));
        t.Process(new DamageEvent(2_000, Boss, Tank, 2_300_001, 50_000, HitFlags.None));
        t.Process(new HealEvent(2_500, Healer, Tank, 17020000, 40_000, HitFlags.None));
        t.Process(new HealEvent(3_000, Pet, Tank, 17020000, 5_000, HitFlags.None)); // a summon's heal counts for its owner

        var snap = t.Snapshot(null, 3_000);
        Assert.NotNull(snap);
        var healer = snap!.Combatants.Single(c => c.ActorId == Healer);
        var tank = snap.Combatants.Single(c => c.ActorId == Tank);
        Assert.Equal(45_000, healer.Healing);
        Assert.Equal(0, tank.Healing);
        Assert.Equal(50_000, tank.DamageTaken);
    }
}
