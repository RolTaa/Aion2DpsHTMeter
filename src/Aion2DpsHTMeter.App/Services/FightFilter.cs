using AionMeter.Core.Combat;

namespace Aion2DpsHTMeter.App.Services;

/// <summary>What a fight was against: the measurement targets chosen in the settings.</summary>
[Flags]
public enum TargetKinds
{
    None = 0,
    /// <summary>The targets of the Abyss artifact siege (listed in data/abyss_artifact_npcs.json).</summary>
    AbyssArtifact = 1,
    Boss = 2,
    /// <summary>Everything else: normal mobs.</summary>
    Trash = 4,
    All = AbyssArtifact | Boss | Trash,
}

/// <summary>
/// Picks the fight the overlay shows: the running one when its kind is chosen, else the last chosen one until the
/// next starts. Kept free of WPF so the tests can use it.
/// </summary>
public sealed class FightFilter
{
    private readonly IReadOnlySet<int> _artifactNpcs;
    private Guid? _lastShown;

    public FightFilter(IReadOnlySet<int> artifactNpcs) => _artifactNpcs = artifactNpcs;

    public TargetKinds KindOf(EncounterSnapshot fight)
    {
        var npc = fight.Boss?.NpcCode ?? fight.TargetCode;
        if (_artifactNpcs.Contains(npc)) return TargetKinds.AbyssArtifact;
        return fight.Boss is not null ? TargetKinds.Boss : TargetKinds.Trash;
    }

    /// <param name="live">The tracker's live fight (the running one, or the last finished one).</param>
    /// <param name="byId">Looks up an earlier fight by its id (null once the tracker has dropped it).</param>
    public EncounterSnapshot? Pick(EncounterSnapshot? live, TargetKinds kinds, Func<Guid, EncounterSnapshot?> byId)
    {
        if (live is not null && (KindOf(live) & kinds) != 0)
        {
            _lastShown = live.Id;
            return live;
        }
        if (_lastShown is not { } id) return null;
        var last = byId(id);
        return last is not null && (KindOf(last) & kinds) != 0 ? last : null;
    }

    public void Forget() => _lastShown = null;
}
