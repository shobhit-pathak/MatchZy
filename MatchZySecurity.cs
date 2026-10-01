using System.Text.RegularExpressions;

namespace MatchZy
{
    // Pure helpers for validating untrusted input and keeping secrets out of logs.
    // This file must not depend on CounterStrikeSharp so that it can be unit tested on its own (see tests/).
    public static class MatchZySecurity
    {
        // MatchZy / Get5 console commands that only change a setting and are therefore allowed in a match config's "cvars" block.
        // Commands that perform an action (loading a match or backup, adding players, ending the match, ...) are deliberately not listed.
        // Settings registered as FakeConVar are allowed automatically (see MatchZy.GetMatchZyFakeConVarNames).
        public static readonly HashSet<string> MatchConfigSettingCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "matchzy_admin_chat_prefix", "matchzy_chat_prefix", "matchzy_chat_messages_timer_delay",
            "matchzy_allow_force_ready", "get5_allow_force_ready",
            "matchzy_autostart_mode",
            "matchzy_demo_name_format", "matchzy_demo_path", "matchzy_demo_recording_enabled",
            "matchzy_demo_upload_url", "matchzy_demo_upload_header_key", "matchzy_demo_upload_header_value",
            "get5_demo_upload_url", "get5_demo_upload_header_key", "get5_demo_upload_header_value",
            "matchzy_remote_backup_url", "matchzy_remote_backup_header_key", "matchzy_remote_backup_header_value",
            "get5_remote_backup_url", "get5_remote_backup_header_key", "get5_remote_backup_header_value",
            "matchzy_remote_log_url", "matchzy_remote_log_header_key", "matchzy_remote_log_header_value",
            "get5_remote_log_url", "get5_remote_log_header_key", "get5_remote_log_header_value",
            "matchzy_kick_when_no_match_loaded", "matchzy_whitelist_enabled_default",
            "matchzy_knife_enabled_default", "matchzy_playout_enabled_default",
            "matchzy_max_saved_last_grenades", "matchzy_save_nades_as_global_enabled",
            "matchzy_minimum_ready_required",
            "matchzy_pause_after_restore", "matchzy_use_pause_command_for_tactical_pause",
            "matchzy_reset_cvars_on_series_end", "matchzy_stop_command_available",
        };

        // Never settable from a match config, even though they are real convars / MatchZy settings.
        public static readonly HashSet<string> BlockedMatchCvars = new(StringComparer.OrdinalIgnoreCase)
        {
            "rcon_password",
            "matchzy_everyone_is_admin",
        };

        // Settings whose value is used as a file path or file name.
        private static readonly HashSet<string> PathCvars = new(StringComparer.OrdinalIgnoreCase)
        {
            "matchzy_demo_path", "matchzy_demo_name_format",
        };

        private static readonly char[] UnsafeValueChars = { '"', ';', '\r', '\n' };
        private static readonly Regex CvarNameRegex = new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);

        // Match configs (and backups built from them) come from outside the server, so only real convars and MatchZy/Get5 settings
        // with a plain value are allowed. Anything else (e.g. "quit", or a value containing a quote or ';') could run console commands.
        public static bool IsAllowedMatchCvar(string name, string value, Func<string, bool> isEngineConVar, ISet<string> pluginFakeConVars, out string reason)
        {
            reason = "";
            if (string.IsNullOrEmpty(name) || !CvarNameRegex.IsMatch(name))
            {
                reason = "invalid name";
                return false;
            }
            if (BlockedMatchCvars.Contains(name))
            {
                reason = "not allowed in a match config";
                return false;
            }
            if (value.IndexOfAny(UnsafeValueChars) >= 0)
            {
                reason = "value contains a quote, ';' or a line break";
                return false;
            }
            if (PathCvars.Contains(name) && !IsSafeRelativePath(value))
            {
                reason = "value must be a relative path without '..'";
                return false;
            }
            bool isPluginName = name.StartsWith("matchzy_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("get5_", StringComparison.OrdinalIgnoreCase);
            if (isPluginName)
            {
                if (MatchConfigSettingCommands.Contains(name) || pluginFakeConVars.Contains(name)) return true;
                reason = "not a MatchZy setting (action commands are not allowed)";
                return false;
            }
            if (!isEngineConVar(name))
            {
                reason = "not a convar";
                return false;
            }
            return true;
        }

        public static bool IsSafeRelativePath(string value)
        {
            if (value == "") return true;
            if (value.StartsWith('/') || value.StartsWith('\\') || value.Contains(':')) return false;
            return !value.Split('/', '\\').Any(part => part == "..");
        }

        // A value that can safely be wrapped in double quotes in a console command.
        public static bool IsQuotableValue(string value)
        {
            return value.IndexOfAny(new[] { '"', '\r', '\n' }) < 0;
        }

        public static bool IsSecretCvar(string name)
        {
            return name.Contains("header_value", StringComparison.OrdinalIgnoreCase)
                || name.Contains("password", StringComparison.OrdinalIgnoreCase)
                || name.Contains("token", StringComparison.OrdinalIgnoreCase);
        }

        // URLs can carry credentials (user:pass@host, API keys or presigned signatures in the query string), so strip them before logging.
        public static string RedactUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return "<invalid url>";
            string redacted = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}{uri.AbsolutePath}";
            if (!string.IsNullOrEmpty(uri.Query)) redacted += "?<redacted>";
            return redacted;
        }

        public static string RedactSecret(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : "<redacted>";
        }

        // For audit logs of console commands: keep the command name, drop the arguments when the command sets a secret.
        public static string RedactConsoleCommand(string commandLine)
        {
            string trimmed = commandLine.Trim();
            int space = trimmed.IndexOfAny(new[] { ' ', '\t' });
            string name = space < 0 ? trimmed : trimmed[..space];
            return IsSecretCvar(name) && space >= 0 ? $"{name} <redacted>" : trimmed;
        }

        private static readonly Regex AdminFlagRegex = new(@"^@[a-z0-9_]+/([a-z0-9_]+|\*)$", RegexOptions.Compiled);

        // MatchZy admins.json values can list CSSharp flags, e.g. "@css/config @css/map" or "@css/config,@css/chat".
        // Only tokens shaped like a flag (@domain/name) count; anything else (a name, a label like "@owner") is ignored.
        public static List<string> GetAdminFlags(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return new();
            return role.Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(flag => flag.ToLowerInvariant())
                .Where(flag => AdminFlagRegex.IsMatch(flag))
                .ToList();
        }

        public static bool AdminFlagsGrant(List<string> adminFlags, IEnumerable<string> requiredPermissions)
        {
            if (adminFlags.Contains("@css/root")) return true;
            foreach (string permission in requiredPermissions)
            {
                string required = permission.ToLowerInvariant();
                foreach (string flag in adminFlags)
                {
                    if (flag == required) return true;
                    // Domain wildcard, e.g. "@css/*" grants "@css/config"
                    if (flag.EndsWith("/*") && required.StartsWith(flag[..^1])) return true;
                }
            }
            return false;
        }
    }
}
