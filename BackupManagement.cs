using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace MatchZy
{
    public partial class MatchZy
    {
        public bool isStopCommandAvailable = true;
        public bool pauseAfterRoundRestore = true;
        public string lastBackupFileName = "";
        public string lastMatchZyBackupFileName = "";

        public bool isRoundRestoring = false;
        public bool isRoundRestorePending = false;
        public string pendingRestoreFileName = "";

        public Dictionary<string, bool> stopData = new()
        {
            { "ct", false },
            { "t", false }
        };

        public string backupUploadURL = "";
        public string backupUploadHeaderKey = "";
        public string backupUploadHeaderValue = "";


        public void SetupRoundBackupFile()
        {
            string backupFilePrefix = $"matchzy_{liveMatchId}_{matchConfig.CurrentMapNumber}";
            Server.ExecuteCommand($"mp_backup_round_file {CsgoPathArg(backupFilePrefix)}");
        }

        // Relative paths in tv_record / mp_backup_* resolve under csgo/addons/metamod when Metamod is installed, so files are
        // passed as absolute csgo paths (forward slashes, quoted).
        private static string CsgoFullPath(string csgoRelativePath)
        {
            return Path.Join(Server.GameDirectory, "csgo", csgoRelativePath).Replace('\\', '/');
        }

        private static string CsgoPathArg(string csgoRelativePath)
        {
            // Always quoted: the console splits arguments at spaces and at ':', which every Windows path has (C:/...).
            return $"\"{CsgoFullPath(csgoRelativePath)}\"";
        }

        [ConsoleCommand("css_stop", "Restore the backup of the current round (Both teams need to type .stop to restore the current round)")]
        public void OnStopCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;

            Log($"[!stop command] Sent by: {player.UserId}, TeamNum: {player.TeamNum}, connectedPlayers: {connectedPlayers}");
            if (isStopCommandAvailable && isMatchLive)
            {
                if (IsHalfTimePhase())
                {
                    // ReplyToUserCommand(player, "You cannot use this command during halftime.");
                    ReplyToUserCommand(player, Localizer["matchzy.backup.stopduringhalftime"]);
                    return;
                }
                if (IsPostGamePhase())
                {
                    // ReplyToUserCommand(player, "You cannot use this command after the game has ended.");
                    ReplyToUserCommand(player, Localizer["matchzy.backup.stopmatchended"]);
                    return;
                }
                if (IsTacticalTimeoutActive())
                {
                    // ReplyToUserCommand(player, "You cannot use this command when tactical timeout is active.");
                    ReplyToUserCommand(player, Localizer["matchzy.backup.stoptacticaltimeout"]);
                    return;
                }
                if (playerHasTakenDamage && stopCommandNoDamage.Value)
                {
                    ReplyToUserCommand(player, Localizer["matchzy.restore.stopcommandrequiresnodamage"]);
                    return;
                }
                string stopTeamName = "";
                string remainingStopTeam = "";
                if (player.TeamNum == 2)
                {
                    stopTeamName = reverseTeamSides["TERRORIST"].teamName;
                    remainingStopTeam = reverseTeamSides["CT"].teamName;
                    if (!stopData["t"])
                    {
                        stopData["t"] = true;
                    }

                }
                else if (player.TeamNum == 3)
                {
                    stopTeamName = reverseTeamSides["CT"].teamName;
                    remainingStopTeam = reverseTeamSides["TERRORIST"].teamName;
                    if (!stopData["ct"])
                    {
                        stopData["ct"] = true;
                    }
                }
                else
                {
                    return;
                }
                if (stopData["t"] && stopData["ct"])
                {
                    if (lastMatchZyBackupFileName != "")
                    {
                        RestoreRoundBackup(player, lastMatchZyBackupFileName);
                    }
                    else
                    {
                        // This should not happen, lastMatchZyBackupFileName should not be empty in a live game!
                        Log($"[OnStopCommand] lastMatchZyBackupFileName not found, unable to restore round!");
                    }

                }
                else
                {
                    PrintToAllChat(Localizer["matchzy.restore.teamwantstorestore", stopTeamName, remainingStopTeam]);
                    // Server.PrintToChatAll($"{chatPrefix} {ChatColors.Green}{stopTeamName}{ChatColors.Default} wants to restore the game to the beginning of the current round. {ChatColors.Green}{remainingStopTeam}{ChatColors.Default}, please write !stop to confirm.");
                }
            }
        }

        [ConsoleCommand("css_restore", "Restores the specified round")]
        public void OnRestoreCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerAdmin(player, "css_restore", "@css/config"))
            {
                SendPlayerNotAdminMessage(player);
                return;
            }
            if (command.ArgCount >= 2)
            {
                string commandArg = command.ArgByIndex(1);
                HandleRestoreCommand(player, commandArg);
            }
            else
            {
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", "!restore <round>"]);
            }
        }

        private void HandleRestoreCommand(CCSPlayerController? player, string commandArg)
        {
            if (!IsPlayerAdmin(player, "css_restore", "@css/config"))
            {
                SendPlayerNotAdminMessage(player);
                return;
            }
            if (!isMatchLive) return;

            if (!string.IsNullOrWhiteSpace(commandArg))
            {
                if (int.TryParse(commandArg, out int roundNumber) && roundNumber >= 0)
                {
                    string round = roundNumber.ToString("D2");
                    string requiredBackupFileName = $"matchzy_{liveMatchId}_{matchConfig.CurrentMapNumber}_round{round}.json";
                    RestoreRoundBackup(player, requiredBackupFileName);
                }
                else
                {
                    // ReplyToUserCommand(player, $"Invalid value for restore command. Please specify a valid non-negative number. Usage: !restore <round>");
                    ReplyToUserCommand(player, Localizer["matchzy.backup.restoreinvalidvalue"]);
                }
            }
            else
            {
                // ReplyToUserCommand(player, $"Usage: !restore <round>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", "!restore <round>"]);
            }
        }
        public static string ExtractJsonFileName(string input)
        {
           
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            
            if (!input.Contains('\\') && !input.Contains('/'))
            {
                // If no directory separators are found, return the input as-is
                return input;
            }

            // Find the index of ".json" in the input
            int jsonIndex = input.IndexOf(".json", StringComparison.OrdinalIgnoreCase);
            if (jsonIndex != -1)
            {
               
                int startIndex = input.LastIndexOfAny(new[] { '\\', '/' }, jsonIndex);

               
                if (startIndex >= 0)
                {
                   
                    int length = jsonIndex - startIndex + 5;

                  
                    if (length > 0 && startIndex + 1 + length <= input.Length)
                    {
                        string fileName = input.Substring(startIndex + 1, length);
                        return fileName;
                    }
                }
            }

            return string.Empty;
        }



        // Returns true when the restore was started or queued (map change, warmup), false when it was refused.
        private bool RestoreRoundBackup(CCSPlayerController? player, string fileName)
        {

 

            if (IsHalfTimePhase())
            {
                ReplyToUserCommand(player, Localizer["matchzy.backup.restoreduringhalftime"]);
                return false;
            }
            if (IsPostGamePhase())
            {
                ReplyToUserCommand(player, Localizer["matchzy.backup.restorematchended"]);
                return false;
            }
            if (IsTacticalTimeoutActive())
            {
                ReplyToUserCommand(player, Localizer["matchzy.backup.restoretacticaltimeout"]);
                return false;
            }
            string backupFolder = Path.Combine(Server.GameDirectory, "csgo", "MatchZyDataBackup");
     
            string filePath = Path.Combine(backupFolder, fileName);
 
            if (!File.Exists(filePath))
            {
                ReplyToUserCommand(player, Localizer["matchzy.backup.restoredoesntexist", fileName]);
                Log($"[RestoreRoundBackup FATAL] Required backup data file does not exist! File: {filePath}");
                return false;
            }

            var gameRules = GetGameRules();
            bool liveSetupRequired = false;

            // We set active timeouts to false so that timeout does not start after the round has been restored.
            // This is to prevent any buggish behaviour with timeouts (like incorrect timeout used showing, or force-unpausing the match once timeout ends)
            gameRules.CTTimeOutActive = gameRules.TerroristTimeOutActive = false;

            // Server.ExecuteCommand($"mp_backup_restore_load_file {fileName}");

            Dictionary<string, string> backupData = new();
            try
            {
                using (StreamReader fileReader = File.OpenText(filePath))
                {
                    string jsonContent = fileReader.ReadToEnd();
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        JsonSerializerOptions options = new()
                        {
                            AllowTrailingCommas = true,
                        };
                        backupData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, options) ?? new Dictionary<string, string>();
                    }
                    else
                    {
                        // Handle the case where the JSON content is empty or null
                        backupData = new();
                    }
                }

                // Checked before anything is changed: a refused restore must leave the running match as it is.
                backupData.TryGetValue("round", out var roundNumber);
                backupData.TryGetValue("matchid", out var backupMatchId);
                backupData.TryGetValue("mapnumber", out var backupMapNumber);
                // The server's own round file for this round (it may be missing, or left over from an earlier match with the same id).
                string serverRoundFile = Path.Combine(Server.GameDirectory, "csgo", $"matchzy_{backupMatchId}_{backupMapNumber}_round{roundNumber}.txt");
                string? valveBackupContent = null;
                if (backupData.TryGetValue("valve_backup", out var valveBackup) && BackupLogic.IsCompleteValveBackup(valveBackup))
                {
                    valveBackupContent = valveBackup;
                }
                else if (roundNumber != null && File.Exists(serverRoundFile) && BackupLogic.IsCompleteValveBackup(File.ReadAllText(serverRoundFile)))
                {
                    // The copy in the backup was taken while the server was still writing the file.
                    Log($"[RestoreRoundBackup] The round data in {fileName} is incomplete, using {Path.GetFileName(serverRoundFile)}");
                    valveBackupContent = File.ReadAllText(serverRoundFile);
                }
                if (valveBackupContent == null)
                {
                    Log($"[RestoreRoundBackup] {fileName} has no complete round data, nothing restored.");
                    ReplyToUserCommand(player, Localizer["matchzy.restore.incomplete", fileName]);
                    return false;
                }

                isRoundRestoring = true;

                // Decided before the backup replaces the match id and map number. Applied when the round is actually restored
                // (after a map change or once a queued restore goes live), since the counts are reset on map start.
                backupData.TryGetValue("map_name", out var backupMapName);
                if (BackupLogic.IsForDifferentMatch(isMatchLive, liveMatchId, matchConfig.CurrentMapNumber, Server.MapName, backupMatchId, backupMapNumber, backupMapName))
                {
                    backupData.TryGetValue("team1_tech_pauses_used", out var team1TechPauses);
                    backupData.TryGetValue("team2_tech_pauses_used", out var team2TechPauses);
                    pendingRestoreTechPauses = (BackupLogic.ParseCount(team1TechPauses), BackupLogic.ParseCount(team2TechPauses));
                }

                // MatchID is set first to avoid generating a new one.
                if (backupData.TryGetValue("matchid", out var matchId))
                {
                    liveMatchId = long.Parse(matchId);
                }
                if (backupData.TryGetValue("match_loaded", out var matchLoaded))
                {
                    isMatchSetup = bool.Parse(matchLoaded);
                }
                if (backupData.TryGetValue("match_config", out var matchConfigValue))
                {
                    matchConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<MatchConfig>(matchConfigValue)!;
                    SetupRoundBackupFile();
                }
                // Coaches are not stored in the backup; keep the current ones.
                var team1Coaches = matchzyTeam1.coach;
                var team2Coaches = matchzyTeam2.coach;
                if (backupData.TryGetValue("team1", out var team1config))
                {
                    matchzyTeam1 = Newtonsoft.Json.JsonConvert.DeserializeObject<Team>(team1config)!;
                    matchzyTeam1.coach = team1Coaches;
                    // Backup files can come from a URL, so sanitize the name the same way as a match config (it is used in server commands).
                    matchzyTeam1.teamName = RemoveSpecialCharacters(matchzyTeam1.teamName ?? "");
                }
                if (backupData.TryGetValue("team2", out var team2config))
                {
                    matchzyTeam2 = Newtonsoft.Json.JsonConvert.DeserializeObject<Team>(team2config)!;
                    matchzyTeam2.coach = team2Coaches;
                    matchzyTeam2.teamName = RemoveSpecialCharacters(matchzyTeam2.teamName ?? "");
                }
                if (backupData.TryGetValue("team1_side", out var team1Side))
                {

                    if (team1Side == "CT")
                    {
                        teamSides[matchzyTeam1] = "CT";
                        reverseTeamSides["CT"] = matchzyTeam1;
                        teamSides[matchzyTeam2] = "TERRORIST";
                        reverseTeamSides["TERRORIST"] = matchzyTeam2;
                        // SwapSidesInTeamData(false);
                    }
                    else if (team1Side == "TERRORIST")
                    {
                        teamSides[matchzyTeam1] = "TERRORIST";
                        reverseTeamSides["TERRORIST"] = matchzyTeam1;
                        teamSides[matchzyTeam2] = "CT";
                        reverseTeamSides["CT"] = matchzyTeam2;
                        // SwapSidesInTeamData(false);
                    }
                }
                if (backupData.TryGetValue("map_name", out var map_name))
                {
                    if (map_name != Server.MapName)
                    {
                        ChangeMap(map_name, 0);
                        isRoundRestorePending = true;
                        pendingRestoreFileName = fileName;
                        // Returning from here, backup will be restored again once the map is changed.
                        return true;
                    }
                }

                // This is done after checking map_name so that we load the correct map first
                if (gameRules.WarmupPeriod)
                {
                    if (!isRoundRestorePending)
                    {
                        isRoundRestorePending = true;
                        pendingRestoreFileName = fileName;
                        // Nothing is restored yet: it happens when the match goes live (or when the command is run again).
                        PrintToAllChat(Localizer["matchzy.restore.queued", fileName]);
                        return true;
                    }
                    else
                    {
                        liveSetupRequired = true;
                    }
                }
                if (pendingRestoreTechPauses.HasValue)
                {
                    SetTechPausesUsed(pendingRestoreTechPauses.Value.Team1, pendingRestoreTechPauses.Value.Team2);
                    pendingRestoreTechPauses = null;
                }
                if (backupData.TryGetValue("TerroristTimeOuts", out var terroristTimeouts))
                {
                    gameRules.TerroristTimeOuts = int.Parse(terroristTimeouts);
                }

                if (backupData.TryGetValue("CTTimeOuts", out var ctTimeouts))
                {
                    gameRules.CTTimeOuts = int.Parse(ctTimeouts);
                }
                // Always writtento MatchZy's own file: an existing file of the same name may be from another match.
                string restoreFileName = $"matchzy_restore_{liveMatchId}_{matchConfig.CurrentMapNumber}_round{roundNumber}.txt";
                File.WriteAllText(Path.Combine(Server.GameDirectory, "csgo", restoreFileName), valveBackupContent);

                int restoreTimer = liveSetupRequired ? 2 : 0;
                if (liveSetupRequired)
                {
                    Log($"Game was in warmup, setting up Live!");
                    SetupLiveFlagsAndCfg();
                }
                string restoredFileName = fileName;
                AddTimer(restoreTimer, () => {
                    Server.ExecuteCommand($"mp_backup_restore_load_file {CsgoPathArg(restoreFileName)}");
                    StartDemoRecording();

                    // Announced (and paused) once the load has been issued, not before.
                    PrintToAllChat(Localizer["matchzy.restore.restoredsuccessfully", restoredFileName]);
                    if (pauseAfterRoundRestore)
                    {
                        Server.ExecuteCommand("mp_pause_match;");
                        stopData["ct"] = false;
                        stopData["t"] = false;
                        isPaused = true;
                        unpauseData["pauseTeam"] = "RoundRestore";
                        pausedStateTimer ??= AddTimer(chatTimerDelay, SendPausedStateMessage, TimerFlags.REPEAT);
                        StartPauseTracking(PauseType.Backup, 0);
                    }
                });
            }
            catch (Exception e)
            {
                Log($"[RestoreRoundBackup FATAL] An error occurred: {e.Message}");
                return false;
            }
            return true;
        }

        public void CreateMatchZyRoundDataBackup()
        {
            Log($"[CreateMatchZyRoundDataBackup] isRoundRestoring: {isRoundRestoring} isMatchLive: {isMatchLive}");
            if (!isMatchLive || isRoundRestoring) return;
            try
            {
                (int t1score, int t2score) = GetTeamsScore();
                int roundNumber = t1score + t2score;
                string round = roundNumber.ToString("D2");
                string matchZyBackupFileName = $"matchzy_{liveMatchId}_{matchConfig.CurrentMapNumber}_round{round}.json";
                string filePath = Path.Combine(Server.GameDirectory, "csgo", "MatchZyDataBackup", matchZyBackupFileName);

                string? directoryPath = Path.GetDirectoryName(filePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
                string lastBackupFilePath = $"matchzy_{liveMatchId}_{matchConfig.CurrentMapNumber}_round{round}.txt"; ;
                bool lastBackupExists = File.Exists(Path.Combine(Server.GameDirectory, "csgo", lastBackupFilePath));
                lastBackupFilePath = Path.Combine(Server.GameDirectory, "csgo", lastBackupFilePath);

                string valveBackupContent = lastBackupExists ? File.ReadAllText(lastBackupFilePath) : "";
                // The server writes its round file at round start too, so it may still be incomplete here.
                bool valveBackupComplete = BackupLogic.IsCompleteValveBackup(valveBackupContent);
                if (!valveBackupComplete) valveBackupContent = "";

                Dictionary<string, string> roundData = new()
                    {
                        { "matchid", liveMatchId.ToString() },
                        { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "map_name", Server.MapName },
                        { "mapnumber", matchConfig.CurrentMapNumber.ToString() },
                        { "round", round },
                        { "team1", GetTeamConfig("team1") },
                        { "team2", GetTeamConfig("team2") },
                        { "team1_name", matchzyTeam1.teamName },
                        { "team1_flag", matchzyTeam1.teamFlag },
                        { "team1_tag", matchzyTeam1.teamTag },
                        { "team1_side", teamSides[matchzyTeam1] },
                        { "team2_name", matchzyTeam2.teamName },
                        { "team2_flag", matchzyTeam2.teamFlag },
                        { "team2_tag", matchzyTeam2.teamTag },
                        { "team2_side", teamSides[matchzyTeam2] },
                        { "team1_score", t1score.ToString() },
                        { "team2_score", t2score.ToString() },
                        { "team1_series_score", matchzyTeam1.seriesScore.ToString() },
                        { "team2_series_score", matchzyTeam2.seriesScore.ToString() },
                        { "TerroristTimeOuts", gameRules.TerroristTimeOuts.ToString() },
                        { "CTTimeOuts", gameRules.CTTimeOuts.ToString() },
                        { "team1_tech_pauses_used", techPausesUsed[1].ToString() },
                        { "team2_tech_pauses_used", techPausesUsed[2].ToString() },
                        { "match_loaded", isMatchSetup.ToString() },
                        { "match_config", GetMatchConfig() },
                        { "valve_backup", valveBackupContent }
                    };
                JsonSerializerOptions options = new()
                {
                    WriteIndented = true,
                };
                string defaultJson = JsonSerializer.Serialize(roundData, options);

                File.WriteAllText(filePath, defaultJson);

                long matchId = liveMatchId;
                int mapNumber = matchConfig.CurrentMapNumber;
                string uploadUrl = backupUploadURL;
                string uploadHeaderKey = backupUploadHeaderKey;
                string uploadHeaderValue = backupUploadHeaderValue;
                if (valveBackupComplete)
                {
                    Task.Run(async () => {
                        await UploadFileAsync(filePath, uploadUrl, uploadHeaderKey, uploadHeaderValue, matchId, mapNumber, roundNumber);
                    });
                    return;
                }

                // Fill in the round data once the server has finished writing it, then upload the complete backup.
                AddTimer(2.0f, () =>
                {
                    try
                    {
                        string content = File.Exists(lastBackupFilePath) ? File.ReadAllText(lastBackupFilePath) : "";
                        if (BackupLogic.IsCompleteValveBackup(content))
                        {
                            roundData["valve_backup"] = content;
                            File.WriteAllText(filePath, JsonSerializer.Serialize(roundData, options));
                        }
                        else
                        {
                            Log($"[CreateMatchZyRoundDataBackup] Round file {lastBackupFilePath} is missing or incomplete; {matchZyBackupFileName} has no round data.");
                        }
                        Task.Run(async () => {
                            await UploadFileAsync(filePath, uploadUrl, uploadHeaderKey, uploadHeaderValue, matchId, mapNumber, roundNumber);
                        });
                    }
                    catch (Exception e)
                    {
                        Log($"[CreateMatchZyRoundDataBackup FATAL] Error completing the JSON file: {e.Message}");
                    }
                });

            }
            catch (Exception e)
            {
                Log($"[CreateMatchZyRoundDataBackup FATAL] Error creating the JSON file: {e.Message}");
            }
        }

        public List<string> GetBackups(string matchID)
        {
            string backupDir = Path.Combine(Server.GameDirectory, "csgo", "MatchZyDataBackup");


            if (!Directory.Exists(backupDir))
            {
                return [];
            }

            var directoryInfo = new DirectoryInfo(backupDir);
            var files = directoryInfo.GetFiles();

            var pattern = $"matchzy_{matchID}_";
            var backups = new List<string>();

            foreach (var file in files)
            {
                if (file.Name.Contains(pattern))
                {
                    backups.Add(file.FullName);
                }
            }

            backups.Sort((x, y) => string.Compare(y, x, StringComparison.Ordinal));
            return backups;
        }

        public string GetBackupInfo(string filePath)
        {
            string info = "";
            if (!File.Exists(filePath))
            {
                return "";
            }

            Dictionary<string, string> backupData = new();
            try
            {
                using (StreamReader fileReader = File.OpenText(filePath))
                {
                    string jsonContent = fileReader.ReadToEnd();
                    if (string.IsNullOrEmpty(jsonContent))
                    {
                        return "";
                    }
                    else
                    {
                        JsonSerializerOptions options = new()
                        {
                            AllowTrailingCommas = true,
                        };
                        backupData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, options) ?? new Dictionary<string, string>();

                    }
                }

                info = $"{filePath.Split("/")[^1]} {backupData["timestamp"]} {backupData["team1_name"]} {backupData["team2_name"]} {backupData["map_name"]} {backupData["team1_score"]} {backupData["team2_score"]}";

            }
            catch (Exception e)
            {
                Log($"[GetBackupInfo FATAL] An error occurred: {e.Message}");
                return "";
            }

            return info;
        }

        public string GetMatchConfig()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(matchConfig);
        }

        public string GetTeamConfig(string team)
        {
            Team teamConfig = team == "team1" ? matchzyTeam1 : matchzyTeam2;
            return Newtonsoft.Json.JsonConvert.SerializeObject(teamConfig);
        }

        [ConsoleCommand("get5_loadbackup", "Restore the backup from the provided file")]
        [ConsoleCommand("matchzy_loadbackup", "Restore the backup from the provided file")]
        [CommandHelper(minArgs: 1, usage: "<backup_file_name>")]
        public void OnLoadBackupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerAdmin(player, "css_restore", "@css/config"))
            {
                SendPlayerNotAdminMessage(player);
                return;
            }
            
       
           // var fileName = command.GetArg(1);
           var  fileName = ExtractJsonFileName(command.ArgString);


            RestoreRoundBackup(player, fileName);
        }

        [ConsoleCommand("get5_loadbackup_url", "Loads a backup from the given URL")]
        [ConsoleCommand("matchzy_loadbackup_url", "Loads a backup from the given URL")]
        public void LoadBackupFromURL(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;

            string url = command.ArgByIndex(1);

            string headerName = command.ArgCount > 3 ? command.ArgByIndex(2) : "";
            string headerValue = command.ArgCount > 3 ? command.ArgByIndex(3) : "";

            Log($"[LoadBackupFromURL] Backup Restore request received with URL: {MatchZySecurity.RedactUrl(url)} headerName: {headerName} and headerValue: {MatchZySecurity.RedactSecret(headerValue)}");

            if (!IsValidUrl(url))
            {
                ReplyToUserCommand(player, Localizer["matchzy.mm.invalidurl", MatchZySecurity.RedactUrl(url)]);
                Log($"[LoadBackupFromURL] Invalid URL: {MatchZySecurity.RedactUrl(url)}. Please provide a valid URL to load the backup!");
                return;
            }
            try
            {
                HttpClient httpClient = new();
                if (headerName != "")
                {
                    httpClient.DefaultRequestHeaders.Add(headerName, headerValue);
                }
                HttpResponseMessage response = httpClient.GetAsync(url).Result;

                if (response.IsSuccessStatusCode)
                {
                    string jsonData = response.Content.ReadAsStringAsync().Result;
                    Log($"[LoadBackupFromURL] Received backup ({jsonData.Length} characters)");
                    string fileName = Guid.NewGuid().ToString() + ".json";
                    string filePath = Path.Combine(Server.GameDirectory, "csgo", "MatchZyDataBackup", fileName);

                    string? directoryPath = Path.GetDirectoryName(filePath);
                    if (directoryPath != null && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    File.WriteAllText(filePath, jsonData);
                    Log($"[LoadBackupFromURL] Data saved to: {filePath}");

                    RestoreRoundBackup(player, fileName);
                }
                else
                {
                    ReplyToUserCommand(player, Localizer["matchzy.mm.httprequestfailed", response.StatusCode]);
                    Log($"[LoadBackupFromURL] HTTP request failed with status code: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                Log($"[LoadBackupFromURL - FATAL] An error occured: {e.Message}");
                return;
            }
        }

        [ConsoleCommand("get5_listbackups", "List all the backups for the provided matchid")]
        [ConsoleCommand("matchzy_listbackups", "List all the backups for the provided matchid")]
        public void OnListBackupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerAdmin(player, "css_restore", "@css/config"))
            {
                SendPlayerNotAdminMessage(player);
                return;
            }
            var matchId = command.ArgCount >= 2 ? command.GetArg(1) : liveMatchId.ToString();
            List<string> backups = GetBackups(matchId);

            if (backups.Count == 0)
            {
                command.ReplyToCommand("Found no backup files matching the provided parameters.");
            }

            foreach (string backup in backups)
            {
                string backupInfo = GetBackupInfo(backup);
                if (backupInfo != "")
                {
                    command.ReplyToCommand(backupInfo);
                }
                else
                {
                    command.ReplyToCommand(backup);
                }
            }
        }
    }
}
