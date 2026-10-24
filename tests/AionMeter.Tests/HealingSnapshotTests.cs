using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.Tests;

/// <summary>Aion2DpsHTMeter additions: healing per player in the live snapshot.</summary>
public class HealingSnapshotTests
{
    private const uint Healer = 100, Tank = 101, Boss = 500, Pet = 600;
    private const int ManaRestore = 16990002;

    private static CombatTracker StartBossFight()
    {
        var data = GameData.Load("no-such-directory");
        data.ResourceRestoreSkills.Add(ManaRestore);
        var t = new CombatTracker(data, new MeterOptions());
        t.Process(new SelfIdentifiedEvent(0, Healer, "Hikari", 1304, GameClass.Cleric));
        t.Process(new PlayerSeenEvent(0, Tank, "Tsubasa", 1304, GameClass.Templar));
        t.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        t.Process(new SummonSeenEvent(0, Pet, Healer, null, 0));
        t.Process(new DamageEvent(1_000, Tank, Boss, 12010000, 20_000, HitFlags.None));
        return t;
    }

    private static CombatantSnapshot Row(CombatTracker t, uint id) =>
        t.Snapshot(null, 5_000)!.Combatants.Single(c => c.ActorId == id);

    [Fact]
    public void Heals_on_allies_self_heals_and_hots_all_count_for_the_healer()
    {
        var t = StartBossFight();
        t.Process(new DamageEvent(2_000, Healer, Tank, 17020000, 40_000, HitFlags.None)); // heal on an ally (damage packet)
        t.Process(new HealEvent(2_500, Healer, Healer, 17030000, 3_000, HitFlags.None));  // self-heal
        t.Process(new HealEvent(3_000, Healer, Tank, 17040000, 2_000, HitFlags.Dot));     // heal over time
        t.Process(new HealEvent(3_500, Pet, Tank, 17020000, 5_000, HitFlags.None));       // a summon heals for its owner

        Assert.Equal(50_000, Row(t, Healer).Healing);
        Assert.Equal(0, Row(t, Healer).Damage);
        Assert.Equal(0, Row(t, Tank).Healing);
        Assert.Equal(0, Row(t, Tank).DamageTaken); // the heal is not damage
    }

    [Fact]
    public void Mana_restores_and_mob_heals_do_not_count()
    {
        var t = StartBossFight();
        t.Process(new HealEvent(2_000, Healer, Healer, ManaRestore, 20, HitFlags.None));
        t.Process(new HealEvent(2_500, Boss, Boss, 2_300_001, 1_000_000, HitFlags.None));

        Assert.DoesNotContain(t.Snapshot(null, 5_000)!.Combatants, c => c.ActorId == Healer || c.ActorId == Boss);
    }

    [Fact]
    public void Damage_taken_is_counted_from_the_boss()
    {
        var t = StartBossFight();
        t.Process(new DamageEvent(2_000, Boss, Tank, 2_300_001, 50_000, HitFlags.None));

        Assert.Equal(50_000, Row(t, Tank).DamageTaken);
    }
}
