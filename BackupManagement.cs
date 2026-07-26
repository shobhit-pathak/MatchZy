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

        // =========================================================================
        // Restore Vote System
        // =========================================================================
        public bool isRestoreVoteInProgress = false;
        public Dictionary<ulong, bool> restoreVoteData = new();        // SteamID -> voted yes?
        public Dictionary<ulong, bool> restoreVotePlayers = new();     // SteamID -> player on server
        public CounterStrikeSharp.API.Modules.Timers.Timer? restoreVoteTimer = null;
        public CCSPlayerController? restoreVoteInitiator = null;
        public long restoreVoteMatchId = -1;                           // match_id for which we're voting
        public int restoreVoteMapNumber = 0;
        public int restoreVoteThreshold = 75;                          // percentage needed
        public int restoreVoteTimeout = 20;                            // seconds


        public void SetupRoundBackupFile()
        {
            string backupFilePrefix = $"matchzy_{liveMatchId}_{matchConfig.CurrentMapNumber}";
            Server.ExecuteCommand($"mp_backup_round_file {backupFilePrefix}");
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



        private void RestoreRoundBackup(CCSPlayerController? player, string fileName)
        {
            if (IsHalfTimePhase())
            {
                ReplyToUserCommand(player, Localizer["matchzy.backup.restoreduringhalftime"]);
                return;
            }
            if (IsPostGamePhase())
            {
                ReplyToUserCommand(player, Localizer["matchzy.backup.restorematchended"]);
                return;
            }
            if (IsTacticalTimeoutActive())
            {
                ReplyToUserCommand(player, Localizer["matchzy.backup.restoretacticaltimeout"]);
                return;
            }

            // Try DB lookup first by backup id, then by old-style filename
            BackupRecord? backup = null;
            string valveBackupContent = "";

            // Try parsing as backup ID (numeric)
            if (long.TryParse(fileName.Replace(".json", ""), out long backupId))
            {
                backup = database.GetRoundBackupById(backupId);
            }

            // Fallback: try old-style filename lookup (matchzy_<matchId>_<mapNumber>_round<XX>.json)
            if (backup == null)
            {
                // Parse old filename format
                var match = Regex.Match(fileName, @"matchzy_(\d+)_(\d+)_round(\d+)");
                if (match.Success)
                {
                    long legacyMatchId = long.Parse(match.Groups[1].Value);
                    int legacyMapNum = int.Parse(match.Groups[2].Value);
                    int legacyRound = int.Parse(match.Groups[3].Value);
                    backup = database.GetBackupByMatchAndRound(legacyMatchId, legacyMapNum, legacyRound);
                }
            }

            if (backup == null)
            {
                ReplyToUserCommand(player, Localizer["matchzy.backup.restoredoesntexist", fileName]);
                Log($"[RestoreRoundBackup FATAL] Backup not found in DB: {fileName}");
                return;
            }

            valveBackupContent = backup.valve_backup;

            var gameRules = GetGameRules();
            bool liveSetupRequired = false;

            gameRules.CTTimeOutActive = gameRules.TerroristTimeOutActive = false;

            isRoundRestoring = true;

            // Restore match state from backup record
            liveMatchId = backup.match_id;
            isMatchSetup = backup.match_loaded == 1;

            if (!string.IsNullOrEmpty(backup.match_config))
            {
                matchConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<MatchConfig>(backup.match_config)!;
                SetupRoundBackupFile();
            }
            if (!string.IsNullOrEmpty(backup.team1_config))
            {
                matchzyTeam1 = Newtonsoft.Json.JsonConvert.DeserializeObject<Team>(backup.team1_config)!;
            }
            if (!string.IsNullOrEmpty(backup.team2_config))
            {
                matchzyTeam2 = Newtonsoft.Json.JsonConvert.DeserializeObject<Team>(backup.team2_config)!;
            }

            // Restore sides
            if (backup.team1_side == "CT")
            {
                teamSides[matchzyTeam1] = "CT";
                reverseTeamSides["CT"] = matchzyTeam1;
                teamSides[matchzyTeam2] = "TERRORIST";
                reverseTeamSides["TERRORIST"] = matchzyTeam2;
            }
            else if (backup.team1_side == "TERRORIST")
            {
                teamSides[matchzyTeam1] = "TERRORIST";
                reverseTeamSides["TERRORIST"] = matchzyTeam1;
                teamSides[matchzyTeam2] = "CT";
                reverseTeamSides["CT"] = matchzyTeam2;
            }

            // Map change handling
            if (backup.map_name != Server.MapName)
            {
                ChangeMap(backup.map_name, 0);
                isRoundRestorePending = true;
                pendingRestoreFileName = backup.id.ToString();
                return;
            }

            // Handle warmup
            if (gameRules.WarmupPeriod)
            {
                if (!isRoundRestorePending)
                {
                    isRoundRestorePending = true;
                    pendingRestoreFileName = backup.id.ToString();
                    PrintToAllChat(Localizer["matchzy.restore.loadedsuccessfully", $"round {backup.round_number}"]);
                    return;
                }
                else
                {
                    liveSetupRequired = true;
                }
            }

            gameRules.TerroristTimeOuts = backup.terrorist_timeouts;
            gameRules.CTTimeOuts = backup.ct_timeouts;

            // Write valve backup to temp file and restore
            string tempFileName = $"matchzy_{backup.match_id}_{backup.map_number}_round{backup.round_number:D2}.txt";
            string tempFilePath = Path.Combine(Server.GameDirectory, "csgo", tempFileName);
            File.WriteAllText(tempFilePath, valveBackupContent);

            int restoreTimer = liveSetupRequired ? 2 : 0;
            if (liveSetupRequired)
            {
                Log($"Game was in warmup, setting up Live!");
                SetupLiveFlagsAndCfg();
            }
            AddTimer(restoreTimer, () => {
                string fn = Path.GetFileName(tempFilePath);
                Server.ExecuteCommand($"mp_backup_restore_load_file {fn}");
                StartDemoRecording();
            });

            PrintToAllChat(Localizer["matchzy.restore.restoredsuccessfully", $"round {backup.round_number}"]);
            if (pauseAfterRoundRestore)
            {
                Server.ExecuteCommand("mp_pause_match;");
                stopData["ct"] = false;
                stopData["t"] = false;
                isPaused = true;
                unpauseData["pauseTeam"] = "RoundRestore";
                pausedStateTimer ??= AddTimer(chatTimerDelay, SendPausedStateMessage, TimerFlags.REPEAT);
            }

            // Reset vote state
            ResetRestoreVote();
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

                // Still write Valve backup to disk (mp_backup_restore_load_file needs it)
                string lastBackupFilePath = $"matchzy_{liveMatchId}_{matchConfig.CurrentMapNumber}_round{round}.txt";
                bool lastBackupExists = File.Exists(Path.Combine(Server.GameDirectory, "csgo", lastBackupFilePath));
                lastBackupFilePath = Path.Combine(Server.GameDirectory, "csgo", lastBackupFilePath);
                string valveBackupContent = lastBackupExists ? File.ReadAllText(lastBackupFilePath) : "";

                var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

                // Write backup metadata to database
                database.SaveRoundBackup(
                    matchId: liveMatchId,
                    mapNumber: matchConfig.CurrentMapNumber,
                    roundNumber: roundNumber,
                    mapName: Server.MapName,
                    team1Name: matchzyTeam1.teamName,
                    team1Score: t1score,
                    team2Name: matchzyTeam2.teamName,
                    team2Score: t2score,
                    team1Side: teamSides[matchzyTeam1],
                    team2Side: teamSides[matchzyTeam2],
                    team1SeriesScore: matchzyTeam1.seriesScore,
                    team2SeriesScore: matchzyTeam2.seriesScore,
                    matchConfigJson: GetMatchConfig(),
                    team1ConfigJson: GetTeamConfig("team1"),
                    team2ConfigJson: GetTeamConfig("team2"),
                    valveBackup: valveBackupContent,
                    terroristTimeouts: gameRules.TerroristTimeOuts,
                    ctTimeouts: gameRules.CTTimeOuts,
                    matchLoaded: isMatchSetup,
                    team1Flag: matchzyTeam1.teamFlag,
                    team1Tag: matchzyTeam1.teamTag,
                    team2Flag: matchzyTeam2.teamFlag,
                    team2Tag: matchzyTeam2.teamTag,
                    serverId: matchzyServerId.Value
                );

                // Also write legacy file backup for backward compatibility (optional, can be disabled)
                string legacyFilePath = Path.Combine(Server.GameDirectory, "csgo", "MatchZyDataBackup",
                    $"matchzy_{liveMatchId}_{matchConfig.CurrentMapNumber}_round{round}.json");
                // Legacy file backup disabled — using database instead.
                // To re-enable, uncomment the block below.

                Task.Run(async () => {
                    // Upload valve backup to configured URL if set
                    if (!string.IsNullOrEmpty(backupUploadURL))
                    {
                        string tempFile = Path.Combine(Server.GameDirectory, "csgo",
                            $"matchzy_{liveMatchId}_{matchConfig.CurrentMapNumber}_round{round}.txt");
                        if (File.Exists(tempFile))
                        {
                            await UploadFileAsync(tempFile, backupUploadURL, backupUploadHeaderKey, backupUploadHeaderValue,
                                liveMatchId, matchConfig.CurrentMapNumber, roundNumber);
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Log($"[CreateMatchZyRoundDataBackup FATAL] Error creating backup: {e.Message}");
            }
        }

        public List<string> GetBackups(string matchID)
        {
            if (!long.TryParse(matchID, out long mid)) return new List<string>();

            var records = database.GetRoundBackups(mid, matchConfig.CurrentMapNumber, matchzyServerId.Value, 30);
            return records.Select(r => $"ID:{r.id} R{r.round_number} {r.timestamp} {r.team1_name}({r.team1_score}) vs {r.team2_name}({r.team2_score}) {r.map_name}").ToList();
        }

        public string GetBackupInfo(long backupId)
        {
            var backup = database.GetRoundBackupById(backupId);
            if (backup == null) return "";

            return $"ID:{backup.id} R{backup.round_number} {backup.timestamp} {backup.team1_name} {backup.team2_name} {backup.map_name} {backup.team1_score} {backup.team2_score}";
        }

        private string FormatBackupInfo(BackupRecord r)
        {
            return $"#{r.id} | Round {r.round_number} | {r.team1_name} {r.team1_score}:{r.team2_score} {r.team2_name} | {r.map_name} | {r.timestamp}";
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

            Log($"[LoadBackupFromURL] Backup Restore request received with URL: {url} headerName: {headerName} and headerValue: {headerValue}");

            if (!IsValidUrl(url))
            {
                ReplyToUserCommand(player, Localizer["matchzy.mm.invalidurl", url]);
                Log($"[LoadBackupFromURL] Invalid URL: {url}. Please provide a valid URL to load the backup!");
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
                    Log($"[LoadBackupFromURL] Received following data: {jsonData}");
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
            if (!long.TryParse(matchId, out long mid))
            {
                command.ReplyToCommand("Invalid match ID.");
                return;
            }
            var records = database.GetRoundBackups(mid, matchConfig.CurrentMapNumber, matchzyServerId.Value, 30);

            if (!records.Any())
            {
                command.ReplyToCommand("Found no backups matching the provided parameters.");
                return;
            }

            foreach (var r in records)
            {
                command.ReplyToCommand(FormatBackupInfo(r));
            }
        }

        // =========================================================================
        // Restore Vote System
        // =========================================================================

        public void StartRestoreVote(CCSPlayerController? initiator)
        {
            if (isRestoreVoteInProgress)
            {
                ReplyToUserCommand(initiator, Localizer["matchzy.vote.alreadyinprogress"]);
                return;
            }

            // Collect eligible voters (all non-bot, non-HLTV players on server)
            restoreVotePlayers.Clear();
            restoreVoteData.Clear();
            foreach (var kvp in playerData)
            {
                var p = kvp.Value;
                if (p != null && IsPlayerValid(p) && !p.IsBot && !p.IsHLTV)
                {
                    ulong steamId = p.SteamID;
                    restoreVotePlayers[steamId] = true;
                    restoreVoteData[steamId] = false; // defaults to NO
                }
            }

            if (restoreVotePlayers.Count < 2)
            {
                ReplyToUserCommand(initiator, Localizer["matchzy.vote.notenoughplayers"]);
                return;
            }

            isRestoreVoteInProgress = true;
            restoreVoteInitiator = initiator;
            restoreVoteMatchId = liveMatchId;
            restoreVoteMapNumber = matchConfig.CurrentMapNumber;

            string initiatorName = initiator?.PlayerName ?? "Console";
            PrintToAllChat(Localizer["matchzy.vote.started", initiatorName, restoreVoteThreshold, restoreVoteTimeout]);
            PrintToAllChat(Localizer["matchzy.vote.instructions"]);

            // Set 20-second timeout
            restoreVoteTimer = AddTimer(restoreVoteTimeout, () =>
            {
                ProcessRestoreVoteResult();
            });
        }

        public void OnRestoreVoteYes(CCSPlayerController? player)
        {
            if (player == null || !isRestoreVoteInProgress) return;
            ulong steamId = player.SteamID;
            if (!restoreVotePlayers.ContainsKey(steamId)) return;

            restoreVoteData[steamId] = true;
            PrintToPlayerChat(player, Localizer["matchzy.vote.youvotedyes"]);
            CheckRestoreVoteThreshold();
        }

        public void OnRestoreVoteNo(CCSPlayerController? player)
        {
            if (player == null || !isRestoreVoteInProgress) return;
            ulong steamId = player.SteamID;
            if (!restoreVotePlayers.ContainsKey(steamId)) return;

            restoreVoteData[steamId] = false;
            PrintToPlayerChat(player, Localizer["matchzy.vote.youvotedno"]);
            CheckRestoreVoteThreshold();
        }

        private void CheckRestoreVoteThreshold()
        {
            if (!isRestoreVoteInProgress) return;

            int totalVoters = restoreVotePlayers.Count;
            int yesVotes = restoreVoteData.Count(v => v.Value);
            int noVotes = restoreVoteData.Count(v => !v.Value);
            int voted = yesVotes + noVotes;

            // Need ALL players to vote, then check 75%
            if (voted >= totalVoters)
            {
                ProcessRestoreVoteResult();
            }
        }

        private void ProcessRestoreVoteResult()
        {
            if (!isRestoreVoteInProgress) return;

            // Kill timer
            if (restoreVoteTimer != null)
            {
                restoreVoteTimer.Kill();
                restoreVoteTimer = null;
            }

            int totalVoters = restoreVotePlayers.Count;
            int yesVotes = restoreVoteData.Count(v => v.Value);
            int noVotes = totalVoters - yesVotes;
            double yesPercent = totalVoters > 0 ? (double)yesVotes / totalVoters * 100.0 : 0;

            if (yesPercent >= restoreVoteThreshold)
            {
                PrintToAllChat(Localizer["matchzy.vote.passed", $"{yesPercent:F0}"]);
                ShowBackupListForVote();
            }
            else
            {
                PrintToAllChat(Localizer["matchzy.vote.failed", $"{yesPercent:F0}", restoreVoteThreshold]);
                ResetRestoreVote();
            }
        }

        private void ShowBackupListForVote()
        {
            long matchIdToUse = isMatchLive ? liveMatchId : restoreVoteMatchId;
            int mapNumToUse = isMatchLive ? matchConfig.CurrentMapNumber : restoreVoteMapNumber;
            string serverId = matchzyServerId.Value;

            if (matchIdToUse <= 0)
            {
                // No specific match — show recent backups for THIS server only
                var recent = database.GetRecentBackups(serverId, 5);
                if (!recent.Any())
                {
                    PrintToAllChat(Localizer["matchzy.vote.nobackups"]);
                    ResetRestoreVote();
                    return;
                }
                PrintToAllChat(Localizer["matchzy.vote.availablebackups"]);
                foreach (var r in recent)
                {
                    PrintToAllChat($"  {FormatBackupInfo(r)} → .restore {r.id}");
                }
            }
            else
            {
                var backups = database.GetRoundBackups(matchIdToUse, mapNumToUse, serverId, 5);
                if (!backups.Any())
                {
                    PrintToAllChat(Localizer["matchzy.vote.nobackups"]);
                    ResetRestoreVote();
                    return;
                }
                PrintToAllChat(Localizer["matchzy.vote.availablebackups"]);
                foreach (var r in backups)
                {
                    PrintToAllChat($"  {FormatBackupInfo(r)} → .restore {r.id}");
                }
            }

            PrintToAllChat(Localizer["matchzy.vote.pickround"]);
            // Keep vote state active for 30 more seconds so players can pick
            restoreVoteTimer = AddTimer(30, () =>
            {
                PrintToAllChat(Localizer["matchzy.vote.timedout"]);
                ResetRestoreVote();
            });
        }

        public void HandleVoteRestore(CCSPlayerController? player, string arg)
        {
            if (!isRestoreVoteInProgress)
            {
                // No vote in progress — check if we should start one
                if (string.IsNullOrWhiteSpace(arg) && !isMatchLive)
                {
                    StartRestoreVote(player);
                    return;
                }

                // If match is live, only admin can restore
                if (!IsPlayerAdmin(player, "css_restore", "@css/config"))
                {
                    SendPlayerNotAdminMessage(player);
                    return;
                }

                HandleRestoreCommand(player, arg);
                return;
            }

            // Vote is in progress, arg should be backup ID
            if (!string.IsNullOrWhiteSpace(arg))
            {
                RestoreRoundBackup(player, arg);
            }
            else
            {
                ReplyToUserCommand(player, Localizer["matchzy.vote.alreadyinprogress"]);
            }
        }

        private void ResetRestoreVote()
        {
            isRestoreVoteInProgress = false;
            restoreVoteData.Clear();
            restoreVotePlayers.Clear();
            restoreVoteInitiator = null;

            if (restoreVoteTimer != null)
            {
                restoreVoteTimer.Kill();
                restoreVoteTimer = null;
            }
        }
    }
}
