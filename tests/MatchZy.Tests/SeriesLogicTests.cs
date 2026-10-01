using MatchZy;

namespace MatchZy.Tests;

public class SeriesLogicTests
{
    private static SeriesOutcome After(int numMaps, int mapsPlayed, int s1, int s2, bool clinch = true, int? maplistCount = null) =>
        SeriesLogic.GetOutcomeAfterMap(numMaps, maplistCount ?? numMaps, mapsPlayed, s1, s2, clinch);

    [Theory]
    [InlineData(1, 1, 1, 0, SeriesOutcome.Team1Wins)]
    [InlineData(1, 1, 0, 1, SeriesOutcome.Team2Wins)]
    [InlineData(1, 1, 0, 0, SeriesOutcome.Tie)]              // BO1 ending in a draw used to run past the map list
    public void Bo1(int numMaps, int played, int s1, int s2, SeriesOutcome expected) => Assert.Equal(expected, After(numMaps, played, s1, s2));

    [Theory]
    [InlineData(1, 1, 0, SeriesOutcome.Continue)]
    [InlineData(2, 2, 0, SeriesOutcome.Team1Wins)]           // clinched 2-0
    [InlineData(2, 1, 1, SeriesOutcome.Continue)]
    [InlineData(3, 2, 1, SeriesOutcome.Team1Wins)]
    [InlineData(3, 1, 2, SeriesOutcome.Team2Wins)]
    [InlineData(2, 1, 0, SeriesOutcome.Continue)]            // map 2 drawn: 1-0 with one map left, still open
    [InlineData(3, 1, 0, SeriesOutcome.Team1Wins)]           // map 2 drawn, map 3 won: 1-0 after all maps
    [InlineData(3, 1, 1, SeriesOutcome.Tie)]                 // one drawn map: 1-1 after all maps
    [InlineData(3, 0, 0, SeriesOutcome.Tie)]                 // three draws
    public void Bo3(int played, int s1, int s2, SeriesOutcome expected) => Assert.Equal(expected, After(3, played, s1, s2));

    [Theory]
    [InlineData(3, 3, 0, SeriesOutcome.Team1Wins)]
    [InlineData(4, 3, 1, SeriesOutcome.Team1Wins)]
    [InlineData(4, 2, 2, SeriesOutcome.Continue)]
    [InlineData(3, 2, 1, SeriesOutcome.Continue)]
    [InlineData(4, 2, 1, SeriesOutcome.Continue)]            // one draw: 2-1 with one map left
    [InlineData(4, 3, 0, SeriesOutcome.Team1Wins)]           // one draw: 3-0 can no longer be caught
    public void Bo5(int played, int s1, int s2, SeriesOutcome expected) => Assert.Equal(expected, After(5, played, s1, s2));

    [Theory]
    [InlineData(1, 1, 0, SeriesOutcome.Continue)]
    [InlineData(2, 1, 1, SeriesOutcome.Tie)]
    [InlineData(2, 2, 0, SeriesOutcome.Team1Wins)]
    public void Bo2(int played, int s1, int s2, SeriesOutcome expected) => Assert.Equal(expected, After(2, played, s1, s2));

    [Fact]
    public void NoClinchPlaysAllMaps()
    {
        Assert.Equal(SeriesOutcome.Continue, After(3, 2, 2, 0, clinch: false));
        Assert.Equal(SeriesOutcome.Team1Wins, After(3, 3, 2, 1, clinch: false));
    }

    [Fact]
    public void ShortMaplistEndsSeries()
    {
        // Never index past the maps that were actually picked.
        Assert.Equal(SeriesOutcome.Tie, After(3, 2, 1, 1, maplistCount: 2));
    }
}
