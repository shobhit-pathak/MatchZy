using MatchZy;

namespace MatchZy.Tests;

public class ScoreLogicTests
{
    // team1 played the first half as T and has already been moved to CT for the second half.
    private static (string, string) Sides(bool swapped, bool roundWon, bool halftime) =>
        ScoreLogic.ScoreSides("CT", "TERRORIST", swapped, roundWon, halftime);

    [Fact]
    public void RoundEndAfterSwap_ReadsOldSides() => Assert.Equal(("TERRORIST", "CT"), Sides(true, true, false));

    [Fact]
    public void HalftimeAfterSwap_ReadsOldSides() => Assert.Equal(("TERRORIST", "CT"), Sides(true, false, true));

    [Fact]
    public void NextRoundStarted_ReadsNewSides() => Assert.Equal(("CT", "TERRORIST"), Sides(false, false, false));

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void NoSwapPending_ReadsCurrentSides(bool roundWon, bool halftime) => Assert.Equal(("CT", "TERRORIST"), Sides(false, roundWon, halftime));

    [Fact]
    public void SwapPendingButRoundRunning_ReadsCurrentSides() => Assert.Equal(("CT", "TERRORIST"), Sides(true, false, false));
}
