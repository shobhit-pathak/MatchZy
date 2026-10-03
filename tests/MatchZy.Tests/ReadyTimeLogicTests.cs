using MatchZy;

namespace MatchZy.Tests;

public class ReadyTimeLogicTests
{
    [Theory]
    [InlineData(true, true, 600, 10000, ReadyTimeOutcome.Ready)]     // both ready: nothing to do, even past the limit
    [InlineData(false, false, 0, 10000, ReadyTimeOutcome.Ready)]     // 0 = no limit
    [InlineData(false, true, 600, 1, ReadyTimeOutcome.Waiting)]
    [InlineData(false, true, 600, 599, ReadyTimeOutcome.Waiting)]
    [InlineData(false, true, 600, 600, ReadyTimeOutcome.Expired)]
    [InlineData(false, false, 600, 601, ReadyTimeOutcome.Expired)]
    public void Check(bool team1, bool team2, int limit, int used, ReadyTimeOutcome expected) =>
        Assert.Equal(expected, ReadyTimeLogic.Check(team1, team2, limit, used));

    [Theory]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 2)]
    [InlineData(false, false, 0)] // neither ready: tie
    public void ForfeitWinner(bool team1, bool team2, int expected) => Assert.Equal(expected, ReadyTimeLogic.ForfeitWinner(team1, team2));

    [Theory]
    [InlineData(600, true)]
    [InlineData(540, true)]
    [InlineData(300, true)]
    [InlineData(330, false)] // 5+ minutes: whole minutes only
    [InlineData(299, false)]
    [InlineData(270, true)]  // below 5 minutes: every 30 seconds
    [InlineData(30, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    [InlineData(0, false)]
    public void ShouldWarn(int timeLeft, bool expected) => Assert.Equal(expected, ReadyTimeLogic.ShouldWarn(timeLeft));

    [Fact]
    public void WarnsAtGet5Times()
    {
        var warnings = Enumerable.Range(1, 600).Reverse().Where(ReadyTimeLogic.ShouldWarn).ToList();
        Assert.Equal(new[] { 600, 540, 480, 420, 360, 300, 270, 240, 210, 180, 150, 120, 90, 60, 30, 10 }, warnings);
    }

    [Theory]
    [InlineData(270, "4:30")]
    [InlineData(600, "10:00")]
    [InlineData(10, "0:10")]
    [InlineData(-5, "0:00")]
    public void FormatTime(int seconds, string expected) => Assert.Equal(expected, ReadyTimeLogic.FormatTime(seconds));

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(6, 5, true)]
    [InlineData(4, 5, false)]
    [InlineData(0, 0, false)] // nobody joined is never complete
    [InlineData(1, 0, true)]
    public void IsTeamComplete(int joined, int min, bool expected) => Assert.Equal(expected, ReadyTimeLogic.IsTeamComplete(joined, min));

    [Fact]
    public void JoinCountdownRunsToTheStart()
    {
        int? left = null;
        var steps = new List<JoinCountdownStep>();
        for (int i = 0; i < 12; i++)
        {
            var (next, step) = ReadyTimeLogic.NextJoinCountdown(left, true, 10);
            steps.Add(step);
            left = next;
            if (step == JoinCountdownStep.Finished) break;
        }
        Assert.Equal(JoinCountdownStep.Started, steps[0]);
        Assert.Equal(JoinCountdownStep.Finished, steps[^1]);
        Assert.Equal(11, steps.Count); // started at 10, then 9..1, then finished
    }

    [Fact]
    public void JoinCountdownStopsWhenAPlayerLeaves()
    {
        Assert.Equal((null, JoinCountdownStep.Stopped), ReadyTimeLogic.NextJoinCountdown(6, false, 10));
        Assert.Equal((null, JoinCountdownStep.None), ReadyTimeLogic.NextJoinCountdown(null, false, 10));
        // Back to full: starts over with the full delay.
        Assert.Equal(((int?)10, JoinCountdownStep.Started), ReadyTimeLogic.NextJoinCountdown(null, true, 10));
    }

    [Fact]
    public void NoDelayStartsRightAway() => Assert.Equal((null, JoinCountdownStep.Finished), ReadyTimeLogic.NextJoinCountdown(null, true, 0));

    [Theory]
    [InlineData(10, true)]
    [InlineData(20, true)]
    [InlineData(7, false)]
    [InlineData(5, true)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void JoinCountdownAnnouncements(int left, bool expected) => Assert.Equal(expected, ReadyTimeLogic.ShouldAnnounceJoinCountdown(left));
}
