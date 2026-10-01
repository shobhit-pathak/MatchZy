using Newtonsoft.Json.Linq;

namespace MatchZy.Tests;

public class TranslationTests
{
    private static string LangDirectory()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "MatchZy.csproj"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir!, "lang");
    }

    public static IEnumerable<object[]> LanguageFiles() =>
        Directory.GetFiles(LangDirectory(), "*.json").Where(f => Path.GetFileName(f) != "en.json").Select(f => new object[] { Path.GetFileName(f) });

    // CounterStrikeSharp falls back to the server's language, not always to English: a key missing in a language file is shown
    // as the raw key on servers whose language is not English. New messages must be added to every file (in English if needed).
    [Theory]
    [MemberData(nameof(LanguageFiles))]
    public void HasEveryEnglishKey(string fileName)
    {
        var english = JObject.Parse(File.ReadAllText(Path.Combine(LangDirectory(), "en.json")));
        var language = JObject.Parse(File.ReadAllText(Path.Combine(LangDirectory(), fileName)));
        var missing = english.Properties().Select(p => p.Name).Where(key => language[key] == null).ToList();
        Assert.True(missing.Count == 0, $"{fileName} is missing: {string.Join(", ", missing)}");
    }
}
