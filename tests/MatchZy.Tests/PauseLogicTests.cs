using MatchZy;

namespace MatchZy.Tests;

public class PauseLogicTests
{
    [Theory]
    [InlineData(0, 0, true)]   // unlimited (the default)
    [InlineData(10, 0, true)]
    [InlineData(0, 2, true)]
    [InlineData(1, 2, true)]
    [InlineData(2, 2, false)]
    [InlineData(3, 2, false)]
    public void CanCallTechPause(int used, int max, bool expected) => Assert.Equal(expected, PauseLogic.CanCallTechPause(used, max));

    [Theory]
    [InlineData(100, 0, 1, 0, false)]  // no time limit: both teams always have to unpause (the default)
    [InlineData(-1, 60, 1, 0, false)]  // not in effect yet
    [InlineData(59, 60, 1, 0, false)]
    [InlineData(60, 60, 1, 0, true)]
    [InlineData(61, 60, 1, 2, true)]
    [InlineData(0, 0, 3, 2, true)]     // limit lowered below the pauses used: anyone can unpause (Get5)
    [InlineData(0, 0, 2, 2, false)]
    public void AnyoneCanUnpause(int elapsed, int limit, int used, int max, bool expected) =>
        Assert.Equal(expected, PauseLogic.AnyoneCanUnpause(elapsed, limit, used, max));

    [Theory]
    [InlineData(10, 60, 1, 0, 50)]
    [InlineData(-1, 60, 1, 0, 60)]  // not in effect yet: the full time
    [InlineData(70, 60, 1, 0, 0)]
    [InlineData(10, 0, 1, 0, -1)]   // no limit
    [InlineData(10, 60, 3, 2, -1)]  // over the count: anyone can unpause already
    public void SecondsUntilAnyoneCanUnpause(int elapsed, int limit, int used, int max, int expected) =>
        Assert.Equal(expected, PauseLogic.SecondsUntilAnyoneCanUnpause(elapsed, limit, used, max));

    [Theory]
    [InlineData(1, 2, " (1/2)")]
    [InlineData(1, 0, "")]
    public void PauseCountSuffix(int number, int max, string expected) => Assert.Equal(expected, PauseLogic.PauseCountSuffix(number, max));

    [Theory]
    [InlineData(PauseType.Technical, "technical")]
    [InlineData(PauseType.Tactical, "tactical")]
    [InlineData(PauseType.Admin, "admin")]
    [InlineData(PauseType.Backup, "backup")]
    [InlineData(PauseType.None, "none")]
    public void EventNames(PauseType type, string expected) => Assert.Equal(expected, PauseLogic.ToEventName(type));
}
