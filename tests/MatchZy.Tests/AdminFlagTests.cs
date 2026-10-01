using MatchZy;

namespace MatchZy.Tests;

public class AdminFlagTests
{
    private static bool Grants(string role, params string[] required) =>
        MatchZySecurity.AdminFlagsGrant(MatchZySecurity.GetAdminFlags(role), required.Concat(new[] { "@css/root" }));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("Shobhit")]
    [InlineData("@owner")]      // label, not a flag
    [InlineData("css/config")]  // missing '@'
    [InlineData("#css/admins")] // CSSharp group, not supported here
    public void NoFlags(string? role) => Assert.Empty(MatchZySecurity.GetAdminFlags(role));

    [Fact]
    public void ParsesSeparators()
    {
        Assert.Equal(new[] { "@css/config", "@css/map", "@css/chat", "@custom/prac" },
            MatchZySecurity.GetAdminFlags("@css/config,@css/map @CSS/Chat;\t@custom/prac"));
    }

    [Fact]
    public void IgnoresNonFlagTokens()
    {
        Assert.Equal(new[] { "@css/config" }, MatchZySecurity.GetAdminFlags("Shobhit @css/config @owner"));
    }

    [Theory]
    [InlineData("@css/config", new[] { "@css/config" }, true)]
    [InlineData("@css/chat", new[] { "@css/config" }, false)]
    [InlineData("@css/chat @css/map", new[] { "@css/map", "@custom/prac" }, true)]
    [InlineData("@custom/prac", new[] { "@css/map", "@custom/prac" }, true)]
    [InlineData("@css/root", new[] { "@css/rcon" }, true)]
    [InlineData("@CSS/ROOT", new[] { "@css/config" }, true)]
    [InlineData("@css/*", new[] { "@css/rcon" }, true)]
    [InlineData("@custom/*", new[] { "@css/rcon" }, false)]
    [InlineData("@css/config", new[] { "@css/rcon" }, false)]
    [InlineData("@css/config", new string[0], false)] // commands that need only @css/root
    public void Grants_(string role, string[] required, bool expected) => Assert.Equal(expected, Grants(role, required));
}
