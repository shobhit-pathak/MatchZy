using MatchZy;
using static MatchZy.BotClaimLogic;

namespace MatchZy.Tests;

public class BotClaimLogicTests
{
    private const int T = 2;
    private const int CT = 3;

    private static int? Select(bool lastAttempt, params NewBot[] bots) => SelectBotToClaim(bots, T, lastAttempt);

    [Fact]
    public void NoBotsYetWaits() => Assert.Null(Select(false));

    [Fact]
    public void TakesRequestedTeam() => Assert.Equal(5, Select(false, new NewBot(5, T, true)));

    [Fact]
    public void PairSpawnKeepsRequestedTeamWhateverTheOrder()
    {
        // The extra bot on the other team is listed first.
        Assert.Equal(6, Select(false, new NewBot(5, CT, true), new NewBot(6, T, true)));
    }

    [Fact]
    public void OnlyWrongTeamWaitsForTheRequestedBot()
    {
        // The requested bot can arrive a tick after the extra: do not give up (or take the wrong one) yet.
        Assert.Null(Select(false, new NewBot(5, CT, true)));
    }

    [Fact]
    public void OnlyWrongTeamOnLastAttemptGivesUp() => Assert.Null(Select(true, new NewBot(5, CT, true)));

    [Fact]
    public void SingleTeamlessBotIsTaken() => Assert.Equal(5, Select(false, new NewBot(5, NoTeam, true)));

    [Fact]
    public void TwoTeamlessBotsWait() => Assert.Null(Select(false, new NewBot(5, NoTeam, true), new NewBot(6, NoTeam, true)));

    [Fact]
    public void TwoTeamlessBotsOnLastAttemptTakeOne() => Assert.Equal(5, Select(true, new NewBot(5, NoTeam, true), new NewBot(6, NoTeam, true)));

    [Fact]
    public void RequestedTeamBeatsTeamless() => Assert.Equal(6, Select(false, new NewBot(5, NoTeam, true), new NewBot(6, T, true)));

    [Fact]
    public void BotWithoutPawnIsNotClaimed()
    {
        Assert.Null(Select(false, new NewBot(5, T, false)));
        Assert.Equal(6, Select(false, new NewBot(5, T, false), new NewBot(6, T, true)));
    }
}
