using Newtonsoft.Json.Linq;

namespace MatchZy.Tests;

public class GameDataTests
{
    private static JObject LoadGameData()
    {
        // tests/MatchZy.Tests/bin/<config>/<tfm>/ -> repository root
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "MatchZy.csproj"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return JObject.Parse(File.ReadAllText(Path.Combine(dir!, "gamedata", "matchzy.json")));
    }

    [Theory]
    [InlineData("CSmokeGrenadeProjectile_Create")]
    [InlineData("CHEGrenadeProjectile_Create")]
    [InlineData("CMolotovProjectile_Create")]
    [InlineData("CDecoyProjectile_Create")]
    public void HasSignaturesForBothPlatforms(string key)
    {
        JToken? signatures = LoadGameData()[key]?["signatures"];
        Assert.NotNull(signatures);
        Assert.Equal("server", signatures!["library"]?.ToString());
        foreach (string platform in new[] { "windows", "linux" })
        {
            string? signature = signatures[platform]?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(signature), $"{key} has no {platform} signature");
            // Space separated hex bytes or '?' wildcards, which is what CounterStrikeSharp expects.
            Assert.Matches(@"^([0-9A-F]{2}|\?)( ([0-9A-F]{2}|\?))*$", signature!);
        }
    }
}
