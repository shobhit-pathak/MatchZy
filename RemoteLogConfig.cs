using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace MatchZy
{
    public partial class MatchZy
    {
        // Remote log settings from config.cfg / the server console. Every new match starts from these; a match config's
        // "cvars" can override them for that match only (see ApplyMatchRemoteLogCvar).
        private string defaultRemoteLogURL = "";
        private string defaultRemoteLogHeaderKey = "";
        private string defaultRemoteLogHeaderValue = "";

        public static readonly HashSet<string> remoteLogCvars = new(StringComparer.OrdinalIgnoreCase)
        {
            "matchzy_remote_log_url", "get5_remote_log_url",
            "matchzy_remote_log_header_key", "get5_remote_log_header_key",
            "matchzy_remote_log_header_value", "get5_remote_log_header_value",
        };

        // Applies a remote log setting from a match config directly to the current match, without changing the server defaults.
        public void ApplyMatchRemoteLogCvar(string name, string value)
        {
            string key = name.ToLowerInvariant();
            if (key.EndsWith("_remote_log_url"))
            {
                if (!IsValidUrl(value))
                {
                    Log($"[ApplyMatchRemoteLogCvar] Invalid URL: {MatchZySecurity.RedactUrl(value)}. Please provide a valid URL!");
                    return;
                }
                matchConfig.RemoteLogURL = value;
            }
            else if (key.EndsWith("_remote_log_header_key"))
            {
                matchConfig.RemoteLogHeaderKey = value.Trim();
            }
            else if (key.EndsWith("_remote_log_header_value"))
            {
                matchConfig.RemoteLogHeaderValue = value.Trim();
            }
        }

        public void ApplyDefaultRemoteLogSettings()
        {
            matchConfig.RemoteLogURL = defaultRemoteLogURL;
            matchConfig.RemoteLogHeaderKey = defaultRemoteLogHeaderKey;
            matchConfig.RemoteLogHeaderValue = defaultRemoteLogHeaderValue;
        }

        public RemoteLogTarget CurrentRemoteLogTarget()
        {
            return new RemoteLogTarget(matchConfig.RemoteLogURL, matchConfig.RemoteLogHeaderKey, matchConfig.RemoteLogHeaderValue);
        }

        [ConsoleCommand("get5_remote_log_url","If defined, all events are sent to this URL over HTTP. If no protocol is provided")]
        [ConsoleCommand("matchzy_remote_log_url", "If defined, all events are sent to this URL over HTTP. If no protocol is provided")]
        public void RemoteLogURLCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string url = command.ArgByIndex(1);

            if (!IsValidUrl(url))
            {
                Log($"[RemoteLogURLCommand] Invalid URL: {MatchZySecurity.RedactUrl(url)}. Please provide a valid URL!");
                return;
            }

            defaultRemoteLogURL = url;
            matchConfig.RemoteLogURL = url;
        }

        [ConsoleCommand("get5_remote_log_header_key", "If defined, a custom HTTP header with this name is added to the HTTP requests for events")]
        [ConsoleCommand("matchzy_remote_log_header_key", "If defined, a custom HTTP header with this name is added to the HTTP requests for events")]
        public void RemoteLogHeaderKeyCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string header = command.ArgByIndex(1).Trim();

            if (header != "")
            {
                defaultRemoteLogHeaderKey = header;
                matchConfig.RemoteLogHeaderKey = header;
            }
        }

        [ConsoleCommand("get5_remote_log_header_value", "If defined, the value of the custom header added to the events sent over HTTP")]
        [ConsoleCommand("matchzy_remote_log_header_value", "If defined, the value of the custom header added to the events sent over HTTP")]
        public void RemoteLogHeaderValueCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string headerValue = command.ArgByIndex(1).Trim();

            if (headerValue != "")
            {
                defaultRemoteLogHeaderValue = headerValue;
                matchConfig.RemoteLogHeaderValue = headerValue;
            }
        }
    }
}

public record RemoteLogTarget(string Url, string HeaderKey, string HeaderValue);
