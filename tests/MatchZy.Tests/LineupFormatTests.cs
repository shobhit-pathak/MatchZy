using System.Globalization;
using MatchZy;

namespace MatchZy.Tests;

public class LineupFormatTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    [Fact]
    public void FormatsWithDotOnEveryLocale()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = German;
            Assert.Equal("123.5 -45.25 64", LineupFormat.Format(123.5f, -45.25f, 64f));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void ReadsDotFormat()
    {
        Assert.True(LineupFormat.TryParse("123.5 -45.25 64", out float x, out float y, out float z, German));
        Assert.Equal((123.5f, -45.25f, 64f), (x, y, z));
    }

    [Fact]
    public void ReadsOldFilesWrittenWithCommaDecimals()
    {
        Assert.True(LineupFormat.TryParse("123,5 -45,25 64", out float x, out float y, out float z, German));
        Assert.Equal((123.5f, -45.25f, 64f), (x, y, z));
    }

    [Fact]
    public void RoundTrips()
    {
        string text = LineupFormat.Format(-1234.567f, 0.001f, 89.99f);
        Assert.True(LineupFormat.TryParse(text, out float x, out float y, out float z));
        Assert.Equal((-1234.567f, 0.001f, 89.99f), (x, y, z));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1 2")]
    [InlineData("1 2 3 4")]
    [InlineData("a b c")]
    public void RejectsInvalid(string? text) => Assert.False(LineupFormat.TryParse(text, out _, out _, out _, German));

    [Theory]
    [InlineData("123.5", true)]
    [InlineData("-45", true)]
    [InlineData("1e3", true)]
    [InlineData("abc", false)]
    [InlineData("NaN", false)]
    [InlineData("", false)]
    public void ImportNumbers(string text, bool expected) => Assert.Equal(expected, LineupFormat.IsValidNumber(text));
}
