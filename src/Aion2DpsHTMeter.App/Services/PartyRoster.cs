using AionMeter.Core.Combat;
using AionMeter.Core.Events;

namespace Aion2DpsHTMeter.App.Services;

/// <summary>
/// Who is in the local player's party, from the party roster packets. Kept free of WPF so the tests can use it.
/// </summary>
/// <remarks>
/// Rosters are also found inside other packets, so one that does not list the local player is someone else's party
/// and is ignored. The latest roster that lists us replaces the previous one; there is no timeout, because the server
/// does not resend the roster on a fixed schedule.
/// </remarks>
public sealed class PartyRoster
{
    private readonly object _gate = new();
    private IReadOnlyList<PartyMemberInfo> _members = [];

    /// <summary>True once a roster of our own party has arrived.</summary>
    public bool Known
    {
        get { lock (_gate) return _members.Count > 0; }
    }

    /// <param name="selfName">The local player's name, or null while it is not known yet (the roster is then taken as ours).</param>
    public void Update(PartyRosterEvent roster, string? selfName)
    {
        if (selfName is not null && roster.Members.All(m => m.Name != selfName)) return;
        lock (_gate) _members = roster.Members;
    }

    public void Clear()
    {
        lock (_gate) _members = [];
    }

    /// <summary>The local player, and the players on the roster: same name, and same server when both are known.</summary>
    public bool Contains(CombatantSnapshot c)
    {
        if (c.IsSelf) return true;
        lock (_gate)
            return _members.Any(m => m.Name == c.Name && (m.ServerId == 0 || c.ServerId == 0 || m.ServerId == c.ServerId));
    }
}
