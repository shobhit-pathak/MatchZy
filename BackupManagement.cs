using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;


namespace MatchZy
{
    public partial class MatchZy
    {
        public bool isStopCommandAvailable = true;
        public bool pauseAfterRoundRestore = true;
        public string lastBackupFileName = "";

        public Dictionary<string, bool> stopData = new Dictionary<string, bool> {
            { "ct", false },
            { "t", false }
        };

        public void SetupRoundBackupFile() {
            string backupFilePrefix  = $"matchzy_{liveMatchId}";
            Server.ExecuteCommand($"mp_backup_round_file {backupFilePrefix}");
        }
        [ConsoleCommand("css_stop", "Marks the player ready")]
        public void OnStopCommand(CCSPlayerController? player, CommandInfo? command) {
            if (player == null) return;

            Log($"[!stop command] Sent by: {player.UserId}, TeamNum: {player.TeamNum}, connectedPlayers: {connectedPlayers}");
            if (isStopCommandAvailable && isMatchLive) {
                string stopTeamName = "";
                string remainingStopTeam = "";
                if (player.TeamNum == 2) {
                    stopTeamName = T_TEAM_NAME;
                    remainingStopTeam = CT_TEAM_NAME;
                    if (!stopData["t"]) {
                        stopData["t"] = true;
                    }
                    
                } else if (player.TeamNum == 3) {
                    stopTeamName = CT_TEAM_NAME;
                    remainingStopTeam = T_TEAM_NAME;
                    if (!stopData["ct"]) {
                        stopData["ct"] = true;
                    }
                } else {
                    return;
                } 
                if (stopData["t"] && stopData["ct"]) {
                    if (lastBackupFileName != "") {
                        RestoreRoundBackup(player, lastBackupFileName);
                    } else {
                        // This should not happen, lastBackupFileName should not be empty in a live game!
                        Log($"[OnStopCommand] lastBackupFileName not found, unable to restore round!");
                    }

                } else {
                    Server.PrintToChatAll($"{chatPrefix} {ChatColors.Green}{stopTeamName}{ChatColors.Default} wants to restore the game to the beginning of the current round. {ChatColors.Green}{remainingStopTeam}{ChatColors.Default}, please write !stop to confirm.");
                }
            }
        }

        [ConsoleCommand("css_restore", "Restores the specified round")]
        public void OnRestoreCommand(CCSPlayerController? player, CommandInfo command) {
            if (IsPlayerAdmin(player)) {
                if (command.ArgCount >= 2) {
                    string commandArg = command.ArgByIndex(1);
                    HandleRestoreCommand(player, commandArg);
                }
                else {
                    ReplyToUserCommand(player, $"Usage: !restore <round>");
                }                
            } else {
                SendPlayerNotAdminMessage(player);
            }
        }

        private void HandleRestoreCommand(CCSPlayerController? player, string commandArg) {
            if (!IsPlayerAdmin(player)) {
                SendPlayerNotAdminMessage(player);
                return;
            }
            if (!isMatchLive) return;
            
            if (!string.IsNullOrWhiteSpace(commandArg)) {
                if (int.TryParse(commandArg, out int roundNumber) && roundNumber >= 0) {
                    string round = roundNumber.ToString("D2");
                    string requiredBackupFileName = $"matchzy_{liveMatchId}_round{round}.txt";
                    RestoreRoundBackup(player, requiredBackupFileName);
                }
                else {
                    ReplyToUserCommand(player, $"Invalid value for restore command. Please specify a valid non-negative number. Usage: !restore <round>");
                }
            }
            else {
                ReplyToUserCommand(player, $"Usage: !restore <round>");
            }
        }

        private void RestoreRoundBackup(CCSPlayerController? player, string fileName) {
            if (!File.Exists(Path.Join(Server.GameDirectory + "/csgo/", fileName))) {
                ReplyToUserCommand(player, $"Backup file {fileName} does not exist, please make sure you are restoring a valid backup.");
                return;
            }
            Server.ExecuteCommand($"mp_backup_restore_load_file {fileName}");
            Server.PrintToChatAll($"{chatPrefix} Backup file restored successfully: {fileName}");
            if (pauseAfterRoundRestore) {
                Server.ExecuteCommand("mp_pause_match;");
                stopData["ct"] = false;
                stopData["t"] = false;
                isPaused = true;
                unpauseData["pauseTeam"] = "RoundRestore";
                if (pausedStateTimer == null) {
                    pausedStateTimer = AddTimer(defaultChatTimerDelay, SendPausedStateMessage, TimerFlags.REPEAT);
                }
            }
        }
    }
}
