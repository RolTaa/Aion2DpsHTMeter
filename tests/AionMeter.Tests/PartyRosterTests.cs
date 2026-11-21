using Aion2DpsHTMeter.App.Services;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;

namespace AionMeter.Tests;

/// <summary>Aion2DpsHTMeter addition: the all / force / party filter.</summary>
public class PartyRosterTests
{
    private static PartyMemberInfo Member(string name, int slot, int server = 1304) =>
        new(name, server, 50, 0, 0, GameClass.Unknown, slot);

    private static PartyRosterEvent Roster(params PartyMemberInfo[] members) => new(0, members);

    private static CombatantSnapshot Player(string name, int server = 1304, bool self = false) =>
        new(1, name, GameClass.Unknown, self, 0, 0, 0, 0, 0, 0, 0, server);

    /// <summary>A force of two parties: Hikari's (slots 1–4) and Kaede's (slots 5–8).</summary>
    private static PartyRoster Force()
    {
        var roster = new PartyRoster();
        roster.Update(Roster(
            Member("Hikari", 1), Member("Tsubasa", 2), Member("Mio", 3), Member("Rin", 4),
            Member("Kaede", 5), Member("Sora", 6)), "Hikari");
        return roster;
    }

    [Fact]
    public void Before_a_roster_only_the_local_player_is_in_the_force_or_party()
    {
        var roster = new PartyRoster();
        Assert.False(roster.Known);
        Assert.True(roster.Includes(Player("Hikari", self: true), MemberScope.Party));
        Assert.False(roster.Includes(Player("Tsubasa"), MemberScope.Force));
        Assert.True(roster.Includes(Player("Tsubasa"), MemberScope.All));
    }

    [Fact]
    public void Force_is_the_whole_roster_and_party_the_own_group_of_four()
    {
        var roster = Force();
        Assert.True(roster.Includes(Player("Kaede"), MemberScope.Force));
        Assert.False(roster.Includes(Player("Kaede"), MemberScope.Party));
        Assert.True(roster.Includes(Player("Rin"), MemberScope.Party));
        Assert.False(roster.Includes(Player("Stranger"), MemberScope.Force));
        Assert.True(roster.Includes(Player("Stranger"), MemberScope.All));
    }

    [Fact]
    public void Members_match_by_name_and_server()
    {
        var roster = Force();
        Assert.True(roster.Includes(Player("Tsubasa", server: 0), MemberScope.Party));     // server not known yet
        Assert.False(roster.Includes(Player("Tsubasa", server: 2001), MemberScope.Party)); // same name, other server
    }

    [Fact]
    public void A_roster_without_the_local_player_is_someone_elses_and_ignored()
    {
        var roster = Force();
        roster.Update(Roster(Member("Stranger", 1), Member("Other", 2)), "Hikari");
        Assert.True(roster.Includes(Player("Tsubasa"), MemberScope.Party));
        Assert.False(roster.Includes(Player("Other"), MemberScope.Force));
    }

    [Fact]
    public void The_latest_roster_replaces_the_previous_one()
    {
        var roster = Force();
        roster.Update(Roster(Member("Hikari", 5), Member("Kaede", 6), Member("Tsubasa", 1)), "Hikari"); // moved to party 2
        Assert.True(roster.Includes(Player("Kaede"), MemberScope.Party));
        Assert.False(roster.Includes(Player("Tsubasa"), MemberScope.Party));
        Assert.False(roster.Includes(Player("Mio"), MemberScope.Force)); // left
    }
}
