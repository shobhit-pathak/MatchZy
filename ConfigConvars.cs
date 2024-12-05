using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;


namespace MatchZy
{
    public partial class MatchZy
    {

        public FakeConVar<bool> smokeColorEnabled = new("matchzy_smoke_color_enabled", "Whether player-specific smoke color is enabled or not. Default: false", false);
        public FakeConVar<bool> techPauseEnabled = new("matchzy_enable_tech_pause", "Whether .tech command is enabled or not. Default: true", true);
        public FakeConVar<string> techPausePermission  = new("matchzy_tech_pause_flag", "Flag required to use tech pause", "");
        public FakeConVar<int> techPauseDuration  = new("matchzy_tech_pause_duration", "Tech pause duration in seconds. Default value: 300", 300);

        public FakeConVar<int> maxTechPausesAllowed  = new("matchzy_max_tech_pauses_allowed", " Max tech pauses allowed. Default value: 2", 2);

        public FakeConVar<bool> everyoneIsAdmin = new("matchzy_everyone_is_admin", "If set to true, all the players will have admin privilege. Default: false", false);

        public FakeConVar<bool> showCreditsOnMatchStart = new("matchzy_show_credits_on_match_start", "Whether to show 'MatchZy Plugin by WD-' message on match start. Default: true", true);

        public FakeConVar<string> hostnameFormat = new("matchzy_hostname_format", "The server hostname to use. Set to \"\" to disable/use existing. Default: MatchZy | {TEAM1} vs {TEAM2}", "MatchZy | {TEAM1} vs {TEAM2}");

        public FakeConVar<bool> enableDamageReport = new("matchzy_enable_damage_report", "Whether to show damage report after each round or not. Default: true", true);

        public FakeConVar<bool> stopCommandNoDamage = new("matchzy_stop_command_no_damage", "Whether the stop command becomes unavailable if a player damages a player from the opposing team.", false);

        public FakeConVar<string> matchStartMessage = new("matchzy_match_start_message", "Message to show when the match starts. Use $$$ to break message into multiple lines. Set to \"\" to disable.", "");

        [ConsoleCommand("matchzy_whitelist_enabled_default", "Whether Whitelist is enabled by default or not. Default value: false")]
        public void MatchZyWLConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            isWhitelistRequired = bool.TryParse(args, out bool isWhitelistRequiredValue) ? isWhitelistRequiredValue : args != "0" && isWhitelistRequired;
        }
        
        [ConsoleCommand("matchzy_knife_enabled_default", "Whether knife round is enabled by default or not. Default value: true")]
        public void MatchZyKnifeConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            isKnifeRequired = bool.TryParse(args, out bool isKnifeRequiredValue) ? isKnifeRequiredValue : args != "0" && isKnifeRequired;
        }

        [ConsoleCommand("matchzy_playout_enabled_default", "Whether knife round is enabled by default or not. Default value: true")]
        public void MatchZyPlayoutConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            isPlayOutEnabled = bool.TryParse(args, out bool isPlayOutEnabledValue) ? isPlayOutEnabledValue : args != "0" && isPlayOutEnabled;
        }

        [ConsoleCommand("matchzy_save_nades_as_global_enabled", "Whether nades should be saved globally instead of being privated to players by default or not. Default value: false")]
        public void MatchZySaveNadesAsGlobalConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            isSaveNadesAsGlobalEnabled = bool.TryParse(args, out bool isSaveNadesAsGlobalEnabledValue) ? isSaveNadesAsGlobalEnabledValue : args != "0" && isSaveNadesAsGlobalEnabled;
        }

        [ConsoleCommand("matchzy_kick_when_no_match_loaded", "Whether to kick all clients and prevent anyone from joining the server if no match is loaded. Default value: false")]
        public void MatchZyMatchModeOnlyConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            matchModeOnly = bool.TryParse(args, out bool matchModeOnlyValue) ? matchModeOnlyValue : args != "0" && matchModeOnly;
        }

        [ConsoleCommand("matchzy_reset_cvars_on_series_end", "Whether parameters from the cvars section of a match configuration are restored to their original values when a series ends. Default value: true")]
        public void MatchZyResetCvarsOnSeriesEndConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            resetCvarsOnSeriesEnd = bool.TryParse(args, out bool resetCvarsOnSeriesEndValue) ? resetCvarsOnSeriesEndValue : args != "0" && resetCvarsOnSeriesEnd;
        }

        [ConsoleCommand("matchzy_minimum_ready_required", "Minimum ready players required to start the match. Default: 1")]
        public void MatchZyMinimumReadyRequired(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            // Since there is already a console command for this purpose, we will use the same.   
            OnReadyRequiredCommand(player, command);
        }

        [ConsoleCommand("matchzy_demo_path", "Path of folder in which demos will be saved. If defined, it must not start with a slash and must end with a slash. Set to empty string to use the csgo root.")]
        public void MatchZyDemoPath(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            if (command.ArgCount == 2)
            {
                string path = command.ArgByIndex(1);
                if (path[0] == '/' || path[0] == '.' || path[^1] != '/' || path.Contains("//"))
                {
                    Log($"matchzy_demo_path must end with a slash and must not start with a slash or dot. It will be reset to an empty string! Current value: {demoPath}");
                }
                else
                {
                    demoPath = path;
                }
            }
        }

        [ConsoleCommand("matchzy_demo_name_format", "Format of demo filname")]
        public void MatchZyDemoNameFormat(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            if (command.ArgCount == 2)
            {
                string format = command.ArgByIndex(1).Trim();

                if (!string.IsNullOrEmpty(format)) 
                {
                    demoNameFormat = format;
                }
            }
        }

        [ConsoleCommand("get5_demo_upload_url", "If defined, recorded demos will be uploaded to this URL once the map ends.")]
        [ConsoleCommand("matchzy_demo_upload_url", "If defined, recorded demos will be uploaded to this URL once the map ends.")]
        public void MatchZyDemoUploadURL(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string url = command.ArgByIndex(1);
            if (url.Trim() == "") return;
            if (!IsValidUrl(url))
            {
                Log($"[MatchZyDemoUploadURL] Invalid URL: {url}. Please provide a valid URL for uploading the demo!");
                return;
            }
            demoUploadURL = url;
        }

        [ConsoleCommand("matchzy_stop_command_available", "Whether .stop command is enabled or not (to restore the current round). Default value: false")]
        public void MatchZyStopCommandEnabled(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            isStopCommandAvailable = bool.TryParse(args, out bool isStopCommandAvailableValue) ? isStopCommandAvailableValue : args != "0" && isStopCommandAvailable;
        }

        [ConsoleCommand("matchzy_use_pause_command_for_tactical_pause", "Whether to use !pause/.pause command for tactical pause or normal pause (unpauses only when both teams use unpause command, for admin force-unpauses the game). Default value: false")]
        public void MatchZyPauseForTacticalCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            isPauseCommandForTactical = bool.TryParse(args, out bool isPauseCommandForTacticalValue) ? isPauseCommandForTacticalValue : args != "0" && isPauseCommandForTactical;
        }

        [ConsoleCommand("matchzy_pause_after_restore", "Whether to pause the match after a round is restored using matchzy. Default value: true")]
        public void MatchZyPauseAfterStopEnabled(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            pauseAfterRoundRestore = bool.TryParse(args, out bool pauseAfterRoundRestoreValue) ? pauseAfterRoundRestoreValue : args != "0" && pauseAfterRoundRestore;
        }

        [ConsoleCommand("matchzy_chat_prefix", "Default value of chat prefix for MatchZy messages. Default value: [{Green}MatchZy{Default}]")]
        public void MatchZyChatPrefix(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                chatPrefix = $"[{ChatColors.Green}MatchZy{ChatColors.Default}]";
                return;
            }

            args = GetColorTreatedString(args);

            chatPrefix = args;

            Log($"[MatchZyChatPrefix] chatPrefix: {chatPrefix}");
        }

        [ConsoleCommand("matchzy_admin_chat_prefix", "Chat prefix to show whenever an admin sends message using .asay <message>. Default value: [{Green}MatchZy{Default}]")]
        public void MatchZyAdminChatPrefix(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                chatPrefix = $"[{ChatColors.Red}ADMIN{ChatColors.Default}]";
                return;
            }

            args = GetColorTreatedString(args);

            adminChatPrefix = args;

            Log($"[MatchZyAdminChatPrefix] adminChatPrefix: {adminChatPrefix}");
        }

        [ConsoleCommand("matchzy_chat_messages_timer_delay", "Number of seconds of delay before sending reminder messages from MatchZy (like unready message, paused message, etc). Default: 12")]
        public void MatchZyChatMessagesTimerDelay(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;

            if (command.ArgCount >= 2)
            {
                string commandArg = command.ArgByIndex(1);
                if (!string.IsNullOrWhiteSpace(commandArg))
                {
                    if (int.TryParse(commandArg, out int chatTimerDelayValue) && chatTimerDelayValue >= 0)
                    {
                        chatTimerDelay = chatTimerDelayValue;
                    }
                    else
                    {
                        // ReplyToUserCommand(player, $"Invalid value for matchzy_chat_messages_timer_delay. Please specify a valid non-negative number.");
                        ReplyToUserCommand(player, Localizer["matchzy.cvars.invalidvalue"]);
                    }
                }
            } else if (command.ArgCount == 1) {
                ReplyToUserCommand(player, $"matchzy_chat_messages_timer_delay = {chatTimerDelay}");
            }
        }

        [ConsoleCommand("matchzy_autostart_mode", "Whether the plugin will load the match mode, the practice moder or neither by startup. 0 for neither, 1 for match mode, 2 for practice mode. Default: 1")]
        public void MatchZyAutoStartConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            if (int.TryParse(args, out int autoStartModeValue))
            {
                autoStartMode = autoStartModeValue;
            }

        }

        [ConsoleCommand("matchzy_allow_force_ready", "Whether force ready using !forceready is enabled or not (Currently works in Match Setup only). Default value: True")]
        [ConsoleCommand("get5_allow_force_ready", "Whether force ready using !forceready is enabled or not (Currently works in Match Setup only). Default value: True")]
        public void MatchZyAllowForceReadyConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            allowForceReady = bool.TryParse(args, out bool allowForceReadyValue) ? allowForceReadyValue : args != "0" && allowForceReady;
        }

        [ConsoleCommand("matchzy_max_saved_last_grenades", "Maximum number of grenade history that may be saved per-map, per-client. Set to 0 to disable. Default value: 512")]
        public void MatchZyMaxSavedLastGrenadesConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            if (int.TryParse(args, out int maxLastGrenadesSavedLimitValue))
            {
                maxLastGrenadesSavedLimit = maxLastGrenadesSavedLimitValue;
            }
            else
            {
                // command.ReplyToCommand("Usage: matchzy_max_saved_last_grenades <number>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", $"matchzy_max_saved_last_grenades <number>"]);
            }
        }

        [ConsoleCommand("get5_remote_backup_url", "A URL to send backup files to over HTTP. Leave empty to disable.")]
        [ConsoleCommand("matchzy_remote_backup_url", "A URL to send backup files to over HTTP. Leave empty to disable.")]
        [CommandHelper(minArgs: 1, usage: "<remote_backup_upload_url>")]
        public void MatchZyBackupUploadURL(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string url = command.ArgByIndex(1);
            if (url.Trim() == "") return;
            if (!IsValidUrl(url))
            {
                Log($"[MatchZyBackupUploadURL] Invalid URL: {url}. Please provide a valid URL for uploading the demo!");
                return;
            }
            backupUploadURL = url;
        }

        [ConsoleCommand("get5_remote_backup_header_key", "If defined, a custom HTTP header with this name is added to the backup HTTP request.")]
        [ConsoleCommand("matchzy_remote_backup_header_key", "If defined, a custom HTTP header with this name is added to the backup HTTP request.")]
        [CommandHelper(minArgs: 1, usage: "<remote_backup_header_key>")]
        public void BackupUploadHeaderKeyCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string header = command.ArgByIndex(1).Trim();

            if (header != "") backupUploadHeaderKey = header;
        }

        [ConsoleCommand("get5_remote_backup_header_value", "If defined, the value of the custom header added to the backup HTTP request.")]
        [ConsoleCommand("matchzy_remote_backup_header_value", "If defined, the value of the custom header added to the backup HTTP request.")]
        [CommandHelper(minArgs: 1, usage: "<remote_backup_header_value>")]
        public void BackupUploadHeaderValueCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string headerValue = command.ArgByIndex(1).Trim();

            if (headerValue != "") backupUploadHeaderValue = headerValue;
        }

        [ConsoleCommand("matchzy_enable_match_scrim", "Allows match setup with no players defined and run like a scrim. Default value: False")]
        public void MatchZyEnableMatchScrimConvar(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            string args = command.ArgString;

            enableMatchScrim = bool.TryParse(args, out bool enableMatchScrimValue) ? enableMatchScrimValue : args != "0" && enableMatchScrim;
        }
    }
}
