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

    [Fact]
    public void CutOffCopyOfRealFileIsIncomplete()
    {
        for (int length = 1; length < Complete.TrimEnd().Length; length++)
        {
            Assert.False(BackupLogic.IsCompleteValveBackup(Complete[..length]), $"length {length}");
        }
    }
}
