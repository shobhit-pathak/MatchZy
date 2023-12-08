using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;
using System.Text.Json;


namespace MatchZy
{
    public partial class MatchZy
    {
        public bool isStopCommandAvailable = true;
        public bool pauseAfterRoundRestore = true;
        public string lastBackupFileName = "";

        public bool isRoundRestoring = false;

        public Dictionary<string, bool> stopData = new Dictionary<string, bool> {
            { "ct", false },
            { "t", false }
        };

        public void SetupRoundBackupFile() {
            string backupFilePrefix  = $"matchzy_{liveMatchId}_{matchConfig.CurrentMapNumber}";
            Server.ExecuteCommand($"mp_backup_round_file {backupFilePrefix}");
        }
        [ConsoleCommand("css_stop", "Restore the backup of the current round (Both teams need to type .stop to restore the current round)")]
        public void OnStopCommand(CCSPlayerController? player, CommandInfo? command) {
            if (player == null) return;

            Log($"[!stop command] Sent by: {player.UserId}, TeamNum: {player.TeamNum}, connectedPlayers: {connectedPlayers}");
            if (isStopCommandAvailable && isMatchLive) {
                if (IsHalfTimePhase())
                {
                    ReplyToUserCommand(player, "You cannot use this command during halftime.");
                    return;
                }
                if (IsPostGamePhase())
                {
                    ReplyToUserCommand(player, "You cannot use this command after the game has ended.");
                    return;
                }
                if (IsTacticalTimeoutActive())
                {
                    ReplyToUserCommand(player, "You cannot use this command when tactical timeout is active.");
                    return;
                }
                string stopTeamName = "";
                string remainingStopTeam = "";
                if (player.TeamNum == 2) {
                    stopTeamName = reverseTeamSides["TERRORIST"].teamName;
                    remainingStopTeam = reverseTeamSides["CT"].teamName;
                    if (!stopData["t"]) {
                        stopData["t"] = true;
                    }
                    
                } else if (player.TeamNum == 3) {
                    stopTeamName = reverseTeamSides["CT"].teamName;
                    remainingStopTeam = reverseTeamSides["TERRORIST"].teamName;
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
            if (IsPlayerAdmin(player, "css_restore", "@css/config")) {
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
            if (!IsPlayerAdmin(player, "css_restore", "@css/config")) {
                SendPlayerNotAdminMessage(player);
                return;
            }
            if (!isMatchLive) return;
            
            if (!string.IsNullOrWhiteSpace(commandArg)) {
                if (int.TryParse(commandArg, out int roundNumber) && roundNumber >= 0) {
                    string round = roundNumber.ToString("D2");
                    string requiredBackupFileName = $"matchzy_{liveMatchId}_{matchConfig.CurrentMapNumber}_round{round}.txt";
                    RestoreRoundBackup(player, requiredBackupFileName, round);
                }
                else {
                    ReplyToUserCommand(player, $"Invalid value for restore command. Please specify a valid non-negative number. Usage: !restore <round>");
                }
            }
            else {
                ReplyToUserCommand(player, $"Usage: !restore <round>");
            }
        }

        private void RestoreRoundBackup(CCSPlayerController? player, string fileName, string round="") {
            if (IsHalfTimePhase())
            {
                ReplyToUserCommand(player, "You cannot load a backup during halftime.");
                return;
            }
            if (IsPostGamePhase())
            {
                ReplyToUserCommand(player, "You cannot use this command after the game has ended.");
                return;
            }
            if (IsTacticalTimeoutActive())
            {
                ReplyToUserCommand(player, "You cannot use this command when tactical timeout is active.");
                return;
            }
            if (!File.Exists(Path.Join(Server.GameDirectory + "/csgo/", fileName))) {
                ReplyToUserCommand(player, $"Backup file {fileName} does not exist, please make sure you are restoring a valid backup.");
                return;
            }
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

            // We set active timeouts to false so that timeout does not start after the round has been restored.
            // This is to prevent any buggish behaviour with timeouts (like incorrect timeout used showing, or force-unpausing the match once timeout ends)
            gameRules.CTTimeOutActive = gameRules.TerroristTimeOutActive = false;

            Server.ExecuteCommand($"mp_backup_restore_load_file {fileName}");

            (int t1score, int t2score) = GetTeamsScore();

            if (round == "") {
                round = (t1score + t2score).ToString("D2");
            }

            string matchZyBackupFileName = $"matchzy_data_backup_{liveMatchId}_{matchConfig.CurrentMapNumber}_round_{round}.json";
            string filePath = Server.GameDirectory + "/csgo/MatchZyDataBackup/" + matchZyBackupFileName;

            if (File.Exists(filePath)) {
                Dictionary<string, string> backupData = new();
                try {
                    using (StreamReader fileReader = File.OpenText(filePath)) {
                        string jsonContent = fileReader.ReadToEnd();
                        if (!string.IsNullOrEmpty(jsonContent)) {
                            JsonSerializerOptions options = new()
                            {
                                AllowTrailingCommas = true,
                            };
                            backupData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, options) ?? new Dictionary<string, string>();
                        }
                        else {
                            // Handle the case where the JSON content is empty or null
                            backupData = new Dictionary<string, string>();
                        }
                    }

                    isRoundRestoring = true;
                    
                    foreach (var kvp in backupData) {

                        if (kvp.Key == "team1_side") {
                            // This means round is being restored after sides were swapped, hence we swap sides in our records as well!
                            if (kvp.Value == "CT" && teamSides[matchzyTeam1] != "CT") {
                                SwapSidesInTeamData(false);
                            } else if (kvp.Value == "TERRORIST" && teamSides[matchzyTeam1] != "TERRORIST") {
                                SwapSidesInTeamData(false);
                            }
                            // Server.ExecuteCommand($"mp_teamname_1 {matchzyTeam1.teamName}");
                            // Server.ExecuteCommand($"mp_teamname_2 {matchzyTeam2.teamName}");
                        }
                        if (kvp.Key == "TerroristTimeOuts")
                        {
                            gameRules.TerroristTimeOuts = int.Parse(kvp.Value);
                        }
                        if (kvp.Key == "CTTimeOuts")
                        {
                            gameRules.CTTimeOuts = int.Parse(kvp.Value);
                        }
                    }
                }
                catch (Exception e) {
                    Log($"[RestoreRoundBackup FATAL] An error occurred: {e.Message}");
                }
            }
            else {
                Log($"[RestoreRoundBackup FATAL] Required backup data file does not exist! File: {filePath}");
            }

            Server.PrintToChatAll($"{chatPrefix} Backup file restored successfully: {fileName}");
            if (pauseAfterRoundRestore) {
                Server.ExecuteCommand("mp_pause_match;");
                stopData["ct"] = false;
                stopData["t"] = false;
                isPaused = true;
                unpauseData["pauseTeam"] = "RoundRestore";
                if (pausedStateTimer == null) {
                    pausedStateTimer = AddTimer(chatTimerDelay, SendPausedStateMessage, TimerFlags.REPEAT);
                }
            }
        }

        public void CreateMatchZyRoundDataBackup()
        {
            if (!isMatchLive) return;
            try
            {
                (int t1score, int t2score) = GetTeamsScore();
                string round = (t1score + t2score).ToString("D2");
                string matchZyBackupFileName = $"matchzy_data_backup_{liveMatchId}_{matchConfig.CurrentMapNumber}_round_{round}.json";
                string filePath = Server.GameDirectory + "/csgo/MatchZyDataBackup/" + matchZyBackupFileName;
                string? directoryPath = Path.GetDirectoryName(filePath);
                if (directoryPath != null)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }

                var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

                Dictionary<string, string> roundData = new()
                    {
                        { "matchid", liveMatchId.ToString() },
                        { "mapnumber", matchConfig.CurrentMapNumber.ToString() },
                        { "team1_name", matchzyTeam1.teamName },
                        { "team1_flag", matchzyTeam1.teamFlag },
                        { "team1_tag", matchzyTeam1.teamTag },
                        { "team1_side", teamSides[matchzyTeam1] },
                        { "team2_name", matchzyTeam2.teamName },
                        { "team2_flag", matchzyTeam2.teamFlag },
                        { "team2_tag", matchzyTeam2.teamTag },
                        { "team2_side", teamSides[matchzyTeam2] },
                        { "TerroristTimeOuts", gameRules.TerroristTimeOuts.ToString()},
                        { "CTTimeOuts", gameRules.CTTimeOuts.ToString() },
                    };
                JsonSerializerOptions options = new()
                {
                    WriteIndented = true,
                };
                string defaultJson = JsonSerializer.Serialize(roundData, options);

                File.WriteAllText(filePath, defaultJson);

            }
            catch (Exception e)
            {
                Log($"[CreateMatchZyRoundDataBackup FATAL] Error creating the JSON file: {e.Message}");
            }
        }
    }
}
