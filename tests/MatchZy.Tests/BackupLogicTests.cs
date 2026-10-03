using MatchZy;

namespace MatchZy.Tests;

public class BackupLogicTests
{
    private const string Complete = "\"SaveFile\"\n{\n\t\"round\"\t\"5\"\n\t\"FirstHalfScore\"\n\t{\n\t\t\"team1\"\t\"3\"\n\t}\n}\n";

    [Fact]
    public void CompleteFile() => Assert.True(BackupLogic.IsCompleteValveBackup(Complete));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n")]
    [InlineData("\"SaveFile\"\n{\n\t\"round\"\t\"5\"\n\t\"FirstHalfScore\"\n\t{\n\t\t\"team1\"")] // cut off half way
    [InlineData("\"SaveFile\"\n{\n\t\"round\"\t\"5\"\n")]                                        // closing brace missing
    [InlineData("\"SaveFile\"\n{\n\t\"name\"\t\"unterminated")]                                  // cut inside a string
    [InlineData("\"SaveFile\"")]                                                                  // no body
    [InlineData("}{")]
    public void IncompleteFile(string? content) => Assert.False(BackupLogic.IsCompleteValveBackup(content));

    [Fact]
    public void BracesInsideNamesAreIgnored()
    {
        Assert.True(BackupLogic.IsCompleteValveBackup("\"SaveFile\"\n{\n\t\"name\"\t\"{{Pro}} \\\"x\\\" {\"\n}\n"));
    }

    [Theory]
    [InlineData(true, "14", "0", "de_nuke", false)]   // a round of the live map: keep the current pause counts
    [InlineData(false, "14", "0", "de_nuke", true)]   // not live (e.g. server restarted)
    [InlineData(true, "14", "1", "de_nuke", true)]    // another map of the series
    [InlineData(true, "14", "0", "de_mirage", true)]
    [InlineData(true, "15", "0", "de_nuke", true)]    // another match
    [InlineData(true, null, null, null, true)]
    [InlineData(true, "14", "0", "DE_NUKE", false)]
    public void IsForDifferentMatch(bool isLive, string? matchId, string? mapNumber, string? map, bool expected) =>
        Assert.Equal(expected, BackupLogic.IsForDifferentMatch(isLive, 14, 0, "de_nuke", matchId, mapNumber, map));

    [Theory]
    [InlineData("2", 2)]
    [InlineData("0", 0)]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("x", 0)]
    [InlineData("-1", 0)]
    public void ParseCount(string? value, int expected) => Assert.Equal(expected, BackupLogic.ParseCount(value));

    [Fact]
    public void CutOffCopyOfRealFileIsIncomplete()
    {
        for (int length = 1; length < Complete.TrimEnd().Length; length++)
        {
            Assert.False(BackupLogic.IsCompleteValveBackup(Complete[..length]), $"length {length}");
        }
    }
}
