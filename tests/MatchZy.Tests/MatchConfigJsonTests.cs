using MatchZy;
using Newtonsoft.Json.Linq;

namespace MatchZy.Tests;

public class MatchConfigJsonTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("\"true\"", true)]
    [InlineData("\"False\"", false)]
    [InlineData("\"1\"", true)]
    [InlineData("\"0\"", false)]
    public void ParsesBools(string json, bool expected)
    {
        Assert.True(MatchConfigJson.TryParseBool(JToken.Parse(json), out bool value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("\"yes\"")]
    [InlineData("null")]
    [InlineData("[]")]
    public void RejectsNonBools(string json) => Assert.False(MatchConfigJson.TryParseBool(JToken.Parse(json), out _));

    [Fact]
    public void MissingBoolIsNotParsed() => Assert.False(MatchConfigJson.TryParseBool(null, out _));

    [Theory]
    [InlineData("{\"76561198000000001\": \"Player\"}", true)]
    [InlineData("{}", true)]
    [InlineData("[\"76561198000000001\", \"76561198000000002\"]", true)]
    [InlineData("[76561198000000001]", true)]
    [InlineData("[]", true)]
    [InlineData("\"76561198000000001\"", false)]
    [InlineData("[{\"id\": 1}]", false)]
    public void ValidatesRosters(string json, bool expected) => Assert.Equal(expected, MatchConfigJson.IsValidRoster(JToken.Parse(json)));

    [Fact]
    public void MissingRosterIsInvalid() => Assert.False(MatchConfigJson.IsValidRoster(null));

    [Fact]
    public void NormalizesArrayRoster()
    {
        JObject roster = MatchConfigJson.NormalizeRoster(JToken.Parse("[\"76561198000000001\", 76561198000000002, \"76561198000000001\"]"));
        Assert.Equal(2, roster.Count);
        Assert.NotNull(roster["76561198000000001"]);
        Assert.NotNull(roster["76561198000000002"]);
        Assert.Equal("", roster["76561198000000002"]!.ToString()); // no name, so it is not forced on the player
    }

    [Fact]
    public void KeepsObjectRoster()
    {
        JObject original = JObject.Parse("{\"76561198000000001\": \"Player\"}");
        Assert.Same(original, MatchConfigJson.NormalizeRoster(original));
    }

    [Fact]
    public void MissingRosterBecomesEmpty() => Assert.Empty(MatchConfigJson.NormalizeRoster(null));

    [Theory]
    [InlineData(null, "team1")]
    [InlineData("team1", "team1")]
    [InlineData("team2", "team2")]
    [InlineData("TEAM2", "team2")]
    [InlineData("something", "team1")]
    public void ResolvesVetoFirst(string? value, string expected) => Assert.Equal(expected, MatchConfigJson.ResolveVetoFirst(value, new Random(1)));

    [Fact]
    public void RandomVetoFirstPicksBothTeams()
    {
        var random = new Random(42);
        var results = Enumerable.Range(0, 50).Select(_ => MatchConfigJson.ResolveVetoFirst("random", random)).ToHashSet();
        Assert.Equal(new HashSet<string> { "team1", "team2" }, results);
    }
}
