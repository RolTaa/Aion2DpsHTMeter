using Aion2DpsHTMeter.App.Services;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;

namespace AionMeter.Tests;

/// <summary>Aion2DpsHTMeter addition: the "party members only" filter.</summary>
public class PartyRosterTests
{
    private static PartyMemberInfo Member(string name, int server = 1304) => new(name, server, 50, 0, 0, GameClass.Unknown);

    private static PartyRosterEvent Roster(params PartyMemberInfo[] members) => new(0, members);

    private static CombatantSnapshot Player(string name, int server = 1304, bool self = false) =>
        new(1, name, GameClass.Unknown, self, 0, 0, 0, 0, 0, 0, 0, server);

    [Fact]
    public void Self_is_always_a_member_and_nobody_else_before_a_roster_arrives()
    {
        var party = new PartyRoster();
        Assert.False(party.Known);
        Assert.True(party.Contains(Player("Hikari", self: true)));
        Assert.False(party.Contains(Player("Tsubasa")));
    }

    [Fact]
    public void Members_match_by_name_and_server()
    {
        var party = new PartyRoster();
        party.Update(Roster(Member("Hikari"), Member("Tsubasa")), "Hikari");

        Assert.True(party.Known);
        Assert.True(party.Contains(Player("Tsubasa")));
        Assert.True(party.Contains(Player("Tsubasa", server: 0)));   // server not known yet
        Assert.False(party.Contains(Player("Tsubasa", server: 2001))); // same name on another server
        Assert.False(party.Contains(Player("Kaede")));
    }

    [Fact]
    public void A_roster_without_the_local_player_is_another_party_and_ignored()
    {
        var party = new PartyRoster();
        party.Update(Roster(Member("Hikari"), Member("Tsubasa")), "Hikari");
        party.Update(Roster(Member("Kaede"), Member("Rin")), "Hikari");

        Assert.True(party.Contains(Player("Tsubasa")));
        Assert.False(party.Contains(Player("Kaede")));
    }

    [Fact]
    public void The_latest_roster_replaces_the_previous_one()
    {
        var party = new PartyRoster();
        party.Update(Roster(Member("Hikari"), Member("Tsubasa")), "Hikari");
        party.Update(Roster(Member("Hikari"), Member("Mio")), "Hikari"); // Tsubasa left, Mio joined

        Assert.False(party.Contains(Player("Tsubasa")));
        Assert.True(party.Contains(Player("Mio")));
    }
}
