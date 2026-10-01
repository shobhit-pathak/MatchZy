using MatchZy;

namespace MatchZy.Tests;

public class RedactionTests
{
    [Theory]
    [InlineData("https://panel.example.com/api/match/1?token=secret", "https://panel.example.com/api/match/1?<redacted>")]
    [InlineData("https://user:secret@panel.example.com:8443/m.json", "https://panel.example.com:8443/m.json")]
    [InlineData("http://panel.example.com/m.json", "http://panel.example.com/m.json")]
    [InlineData("https://bucket.s3.amazonaws.com/demo.dem?X-Amz-Signature=abc&X-Amz-Credential=def", "https://bucket.s3.amazonaws.com/demo.dem?<redacted>")]
    [InlineData("not a url", "<invalid url>")]
    [InlineData("", "")]
    public void RedactUrl(string url, string expected) => Assert.Equal(expected, MatchZySecurity.RedactUrl(url));

    [Theory]
    [InlineData("", "")]
    [InlineData("Bearer abc", "<redacted>")]
    public void RedactSecret(string value, string expected) => Assert.Equal(expected, MatchZySecurity.RedactSecret(value));

    [Theory]
    [InlineData("rcon_password hunter2", "rcon_password <redacted>")]
    [InlineData("sv_password scrim", "sv_password <redacted>")]
    [InlineData("matchzy_remote_log_header_value Bearer abc", "matchzy_remote_log_header_value <redacted>")]
    [InlineData("  mp_restartgame 1 ", "mp_restartgame 1")]
    [InlineData("status", "status")]
    [InlineData("rcon_password", "rcon_password")]
    public void RedactConsoleCommand(string command, string expected) => Assert.Equal(expected, MatchZySecurity.RedactConsoleCommand(command));

    [Theory]
    [InlineData("matchzy_remote_log_header_value", true)]
    [InlineData("get5_demo_upload_header_value", true)]
    [InlineData("sv_password", true)]
    [InlineData("rcon_password", true)]
    [InlineData("matchzy_remote_log_url", false)]
    [InlineData("hostname", false)]
    public void IsSecretCvar(string name, bool expected) => Assert.Equal(expected, MatchZySecurity.IsSecretCvar(name));
}
