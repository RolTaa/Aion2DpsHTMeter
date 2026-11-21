using AionMeter.Core.Combat;
using AionMeter.Core.Events;

namespace Aion2DpsHTMeter.App.Services;

/// <summary>Whose rows the overlay shows.</summary>
public enum MemberScope
{
    /// <summary>Everyone fighting the target.</summary>
    All,
    /// <summary>The local player and everyone on the roster (the whole force).</summary>
    Force,
    /// <summary>The local player and the members of the local player's own party within the force.</summary>
    Party,
}

/// <summary>
/// Who is in the local player's force and party, from the roster packets. Kept free of WPF so the tests can use it.
/// </summary>
/// <remarks>
/// The roster is taken as the force's: it lists up to 12 members by slot, and the slots are taken to be grouped in
/// parties of <see cref="PartySize"/> (slots 1–4 the first party, 5–8 the second, …). Both are assumptions until
/// checked against a capture of a force.
/// Rosters are also found inside other packets, so one that does not list the local player is someone else's and is
/// ignored. The latest roster that lists us replaces the previous one; there is no timeout, because the server does
/// not resend the roster on a fixed schedule.
/// </remarks>
public sealed class PartyRoster
{
    public const int PartySize = 4;

    private readonly object _gate = new();
    private IReadOnlyList<PartyMemberInfo> _members = [];
    private int _selfGroup = -1;

    /// <summary>True once a roster that lists the local player has arrived.</summary>
    public bool Known
    {
        get { lock (_gate) return _members.Count > 0; }
    }

    /// <param name="selfName">The local player's name, or null while it is not known yet (the roster is then taken as ours).</param>
    public void Update(PartyRosterEvent roster, string? selfName)
    {
        var self = selfName is null ? null : roster.Members.FirstOrDefault(m => m.Name == selfName);
        if (selfName is not null && self is null) return;
        lock (_gate)
        {
            _members = roster.Members;
            _selfGroup = self is null ? -1 : Group(self);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _members = [];
            _selfGroup = -1;
        }
    }

    /// <summary>
    /// Whether the player is shown under <paramref name="scope"/>. The local player always is. While our own slot is
    /// not known, the party is the whole force.
    /// </summary>
    public bool Includes(CombatantSnapshot c, MemberScope scope)
    {
        if (scope == MemberScope.All || c.IsSelf) return true;
        lock (_gate)
        {
            var member = _members.FirstOrDefault(m => SamePlayer(m, c));
            if (member is null) return false;
            return scope == MemberScope.Force || _selfGroup < 0 || Group(member) == _selfGroup;
        }
    }

    /// <summary>Same name, and same server when both are known.</summary>
    private static bool SamePlayer(PartyMemberInfo m, CombatantSnapshot c) =>
        m.Name == c.Name && (m.ServerId == 0 || c.ServerId == 0 || m.ServerId == c.ServerId);

    private static int Group(PartyMemberInfo m) => m.Slot > 0 ? (m.Slot - 1) / PartySize : -1;
}
