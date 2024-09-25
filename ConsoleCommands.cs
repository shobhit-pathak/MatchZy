using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.RegularExpressions;

namespace MatchZy
{
    public partial class MatchZy
    {
        [ConsoleCommand("css_whitelist", "Toggles Whitelisting of players")]
        [ConsoleCommand("css_wl", "Toggles Whitelisting of players")]
        public void OnWLCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (IsPlayerAdmin(player, "css_whitelist", "@css/config"))
            {
                isWhitelistRequired = !isWhitelistRequired;
                string WLStatus = isWhitelistRequired ? Localizer["matchzy.cc.enabled"] : Localizer["matchzy.cc.disabled"];
                if (player == null)
                {
                    //ReplyToUserCommand(player, $"Whitelist is now {WLStatus}!");
                    ReplyToUserCommand(player, Localizer["matchzy.cc.wl", WLStatus]);
                }
                else
                {
                    //player.PrintToChat($"{chatPrefix} Whitelist is now {ChatColors.Green}{WLStatus}{ChatColors.Default}!");
                    PrintToPlayerChat(player, Localizer["matchzy.cc.wl", WLStatus]);
                }
            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("css_save_nades_as_global", "Toggles Global Lineups for players")]
        [ConsoleCommand("css_globalnades", "Toggles Global Lineups for players")]
        public void OnSaveNadesAsGlobalCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (IsPlayerAdmin(player, "css_save_nades_as_global", "@css/config"))
            {
                isSaveNadesAsGlobalEnabled = !isSaveNadesAsGlobalEnabled;
                string GlobalNadesStatus = isSaveNadesAsGlobalEnabled ? Localizer["matchzy.cc.enabled"] : Localizer["matchzy.cc.disabled"];
                if (player == null)
                {
                    //ReplyToUserCommand(player, $"Saving/Loading Lineups Globally is now {GlobalNadesStatus}!");
                    ReplyToUserCommand(player, Localizer["matchzy.cc.globalnades", GlobalNadesStatus]);
                }
                else
                {
                    //player.PrintToChat($"{chatPrefix} Saving/Loading Lineups Globally is now {ChatColors.Green}{GlobalNadesStatus}{ChatColors.Default}!");
                    PrintToPlayerChat(player, Localizer["matchzy.cc.globalnades", GlobalNadesStatus]);

                }
            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("css_ready", "Marks the player ready")]
        public void OnPlayerReady(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;
            Log($"[!ready command] Sent by: {player.UserId} readyAvailable: {readyAvailable} matchStarted: {matchStarted}");
            if (readyAvailable && !matchStarted)
            {
                if (player.UserId.HasValue)
                {
                    if (!playerReadyStatus.ContainsKey(player.UserId.Value))
                    {
                        playerReadyStatus[player.UserId.Value] = false;
                    }
                    if (playerReadyStatus[player.UserId.Value])
                    {
                        // player.PrintToChat($"{chatPrefix} You are already ready!");
                        PrintToPlayerChat(player, Localizer["matchzy.ready.markedready"]);
                    }
                    else
                    {
                        playerReadyStatus[player.UserId.Value] = true;
                        // player.PrintToChat($"{chatPrefix} {Localizer["matchzy.youareready"]}");

                        if (enableMatchScrim && (player.TeamNum == (int)CsTeam.CounterTerrorist || player.TeamNum == (int)CsTeam.Terrorist)) {
                            string teamName = player.TeamNum == (int)CsTeam.CounterTerrorist ? reverseTeamSides["CT"].teamName : reverseTeamSides["TERRORIST"].teamName;
                            PrintToPlayerChat(player, Localizer["matchzy.ready.matchedreadyonteam", teamName]);
                        }
                        else {
                            PrintToPlayerChat(player, Localizer["matchzy.ready.markedready"]);
                        }
                    }
                    CheckLiveRequired();
                    HandleClanTags();
                }
            }
        }

        [ConsoleCommand("css_unready", "Marks the player unready")]
        [ConsoleCommand("css_notready", "Marks the player unready")]
        public void OnPlayerUnReady(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;
            Log($"[!unready command] {player.UserId}");
            if (readyAvailable && !matchStarted)
            {
                if (player.UserId.HasValue)
                {
                    if (!playerReadyStatus.ContainsKey(player.UserId.Value))
                    {
                        playerReadyStatus[player.UserId.Value] = false;
                    }
                    if (!playerReadyStatus[player.UserId.Value])
                    {
                        PrintToPlayerChat(player, Localizer["matchzy.ready.markedunready"]);
                    }
                    else
                    {
                        playerReadyStatus[player.UserId.Value] = false;
                        PrintToPlayerChat(player, Localizer["matchzy.ready.markedunready"]);
                    }
                    HandleClanTags();
                }
            }
        }

        [ConsoleCommand("css_stay", "Stays after knife round")]
        public void OnTeamStay(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null || !isSideSelectionPhase) return;

            Log($"[!stay command] {player.UserId}, TeamNum: {player.TeamNum}, knifeWinner: {knifeWinner}, isSideSelectionPhase: {isSideSelectionPhase}");
            if (player.TeamNum == knifeWinner)
            {
                PrintToAllChat(Localizer["matchzy.knife.decidedtostay", knifeWinnerName]);
                // Server.PrintToChatAll($"{chatPrefix} {ChatColors.Green}{knifeWinnerName}{ChatColors.Default} has decided to stay!");
                StartLive();
            }
        }

        [ConsoleCommand("css_switch", "Switch after knife round")]
        [ConsoleCommand("css_swap", "Switch after knife round")]
        public void OnTeamSwitch(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null || !isSideSelectionPhase) return;

            Log($"[!switch command] {player.UserId}, TeamNum: {player.TeamNum}, knifeWinner: {knifeWinner}, isSideSelectionPhase: {isSideSelectionPhase}");

            if (player.TeamNum == knifeWinner)
            {
                Server.ExecuteCommand("mp_swapteams;");
                SwapSidesInTeamData(true);
                PrintToAllChat(Localizer["matchzy.knife.decidedtoswitch", knifeWinnerName]);
                // Server.PrintToChatAll($"{chatPrefix} {ChatColors.Green}{knifeWinnerName}{ChatColors.Default} has decided to switch!");
                StartLive();
            }
        }

        [ConsoleCommand("css_tech", "Pause the match")]
        public void OnTechCommand(CCSPlayerController? player, CommandInfo? command)
        {
            PauseMatch(player, command);
        }

        [ConsoleCommand("css_pause", "Pause the match")]
        public void OnPauseCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (isPauseCommandForTactical)
            {
                OnTacCommand(player, command);
            }
            else
            {
                PauseMatch(player, command);
            }
        }

        [ConsoleCommand("css_fp", "Pause the match an admin")]
        [ConsoleCommand("css_forcepause", "Pause the match as an admin")]
        [ConsoleCommand("sm_pause", "Pause the match as an admin")]
        public void OnForcePauseCommand(CCSPlayerController? player, CommandInfo? command)
        {
            ForcePauseMatch(player, command);
        }

        [ConsoleCommand("css_fup", "Unpause the match an admin")]
        [ConsoleCommand("css_forceunpause", "Unpause the match as an admin")]
        [ConsoleCommand("sm_unpause", "Unpause the match as an admin")]
        public void OnForceUnpauseCommand(CCSPlayerController? player, CommandInfo? command)
        {
            ForceUnpauseMatch(player, command);
        }

        [ConsoleCommand("css_unpause", "Unpause the match")]
        public void OnUnpauseCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (isMatchLive && isPaused)
            {
                var pauseTeamName = unpauseData["pauseTeam"];
                if ((string)pauseTeamName == "Admin" && player != null)
                {
                    PrintToPlayerChat(player, Localizer["matchzy.pause.onlyadmincanunpause"]);
                    return;
                }

                string unpauseTeamName = "Admin";
                string remainingUnpauseTeam = "Admin";
                if (player?.TeamNum == 2)
                {
                    unpauseTeamName = reverseTeamSides["TERRORIST"].teamName;
                    remainingUnpauseTeam = reverseTeamSides["CT"].teamName;
                    if (!(bool)unpauseData["t"])
                    {
                        unpauseData["t"] = true;
                    }

                }
                else if (player?.TeamNum == 3)
                {
                    unpauseTeamName = reverseTeamSides["CT"].teamName;
                    remainingUnpauseTeam = reverseTeamSides["TERRORIST"].teamName;
                    if (!(bool)unpauseData["ct"])
                    {
                        unpauseData["ct"] = true;
                    }
                }
                else
                {
                    return;
                }
                if ((bool)unpauseData["t"] && (bool)unpauseData["ct"])
                {
                    PrintToAllChat(Localizer["matchzy.pause.teamsunpausedthematch"]);
                    Server.ExecuteCommand("mp_unpause_match;");
                    isPaused = false;
                    unpauseData["ct"] = false;
                    unpauseData["t"] = false;
                }
                else if (unpauseTeamName == "Admin")
                {
                    PrintToAllChat(Localizer["matchzy.pause.adminunpausedthematch"]);
                    Server.ExecuteCommand("mp_unpause_match;");
                    isPaused = false;
                    unpauseData["ct"] = false;
                    unpauseData["t"] = false;
                }
                else
                {
                    PrintToAllChat(Localizer["matchzy.pause.teamwantstounpause", unpauseTeamName, remainingUnpauseTeam]);
                    // Server.PrintToChatAll($"{chatPrefix} {ChatColors.Green}{unpauseTeamName}{ChatColors.Default} wants to unpause the match. {ChatColors.Green}{remainingUnpauseTeam}{ChatColors.Default}, please write !unpause to confirm.");
                }
                if (!isPaused && pausedStateTimer != null)
                {
                    pausedStateTimer.Kill();
                    pausedStateTimer = null;
                }
            }
        }

        [ConsoleCommand("css_tac", "Starts a tactical timeout for the requested team")]
        public void OnTacCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;

            if (matchStarted && isMatchLive)
            {
                Log($"[.tac command sent via chat] Sent by: {player.UserId}, connectedPlayers: {connectedPlayers}");
                if (isPaused)
                {
                    // ReplyToUserCommand(player, "Match is already paused, cannot start a tactical timeout!");
                    ReplyToUserCommand(player, Localizer["matchzy.cc.matchpaused"]);
                    return;
                }
                var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
                if (player.TeamNum == 2)
                {
                    if (gameRules.TerroristTimeOuts > 0)
                    {
                        Server.ExecuteCommand("timeout_terrorist_start");
                    }
                    else
                    {
                        // ReplyToUserCommand(player, "You do not have any tactical timeouts left!");
                        ReplyToUserCommand(player, Localizer["matchzy.cc.nomorepauses"]);
                    }
                }
                else if (player.TeamNum == 3)
                {
                    if (gameRules.CTTimeOuts > 0)
                    {
                        Server.ExecuteCommand("timeout_ct_start");
                    }
                    else
                    {
                        // ReplyToUserCommand(player, "You do not have any tactical timeouts left!");
                        ReplyToUserCommand(player, Localizer["matchzy.cc.nomorepauses"]);
                    }
                }
            }
        }

        [ConsoleCommand("css_skipveto", "Skips the current veto phase")]
        [ConsoleCommand("css_sv", "Skips the current veto phase")]
        public void OnSkipVetoCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (IsPlayerAdmin(player, "css_skipveto", "@css/config"))
            {
                if (matchStarted)
                {
                    if (player == null)
                    {
                        // ReplyToUserCommand(player, $"Skip veto command cannot be used if match has already started!");
                        ReplyToUserCommand(player, Localizer["matchzy.cc.skipvetomatchstarted"]);
                    }
                    else
                    {
                        // player.PrintToChat($"{chatPrefix} Skip veto command cannot be used if match has already started!");
                        PrintToPlayerChat(player, Localizer["matchzy.cc.skipvetomatchstarted"]);
                    }
                }
                else
                {
                    SkipVeto();
                    if (player == null)
                    {
                        // ReplyToUserCommand(player, $"Veto phase has been cancelled!");
                        ReplyToUserCommand(player, Localizer["matchzy.cc.skipveto"]);
                    }
                    else
                    {
                        // player.PrintToChat($"{chatPrefix} Veto phase has been cancelled!");
                        PrintToPlayerChat(player, Localizer["matchzy.cc.skipveto"]);
                    }
                }
            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("css_roundknife", "Toggles knife round for the match")]
        [ConsoleCommand("css_rk", "Toggles knife round for the match")]
        public void OnKnifeCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (IsPlayerAdmin(player, "css_roundknife", "@css/config"))
            {
                isKnifeRequired = !isKnifeRequired;
                string knifeStatus = isKnifeRequired ? Localizer["matchzy.cc.enabled"] : Localizer["matchzy.cc.disabled"];
                if (player == null)
                {
                    // ReplyToUserCommand(player, $"Knife round is now {knifeStatus}!");
                    ReplyToUserCommand(player, Localizer["matchzy.cc.roundknife", knifeStatus]);
                }
                else
                {
                    // player.PrintToChat($"{chatPrefix} Knife round is now {ChatColors.Green}{knifeStatus}{ChatColors.Default}!");
                    PrintToPlayerChat(player, Localizer["matchzy.cc.roundknife", knifeStatus]);
                }
            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("css_readyrequired", "Sets number of ready players required to start the match")]
        public void OnReadyRequiredCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (IsPlayerAdmin(player, "css_readyrequired", "@css/config"))
            {
                if (command.ArgCount >= 2)
                {
                    string commandArg = command.ArgByIndex(1);
                    HandleReadyRequiredCommand(player, commandArg);
                }
                else
                {
                    string minimumReadyRequiredFormatted = (player == null) ? $"{minimumReadyRequired}" : $"{ChatColors.Green}{minimumReadyRequired}{ChatColors.Default}";
                    // ReplyToUserCommand(player, $"Current Ready Required: {minimumReadyRequiredFormatted}. Usage: !readyrequired <number_of_ready_players_required>");
                    ReplyToUserCommand(player, Localizer["matchzy.cc.minreadyrequired", minimumReadyRequiredFormatted]);
                }
            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("css_settings", "Shows the current match configuration/settings")]
        public void OnMatchSettingsCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;

            if (IsPlayerAdmin(player, "css_settings", "@css/config"))
            {
                string knifeStatus = isKnifeRequired ? Localizer["matchzy.cc.enabled"] : Localizer["matchzy.cc.disabled"];
                string playoutStatus = isPlayOutEnabled ? Localizer["matchzy.cc.enabled"] : Localizer["matchzy.cc.disabled"];
                // player.PrintToChat($"{chatPrefix} Current Settings:");
                PrintToPlayerChat(player, Localizer["matchzy.cc.currentsettings"]);
                // player.PrintToChat($"{chatPrefix} Knife: {ChatColors.Green}{knifeStatus}{ChatColors.Default}");
                PrintToPlayerChat(player, Localizer["matchzy.cc.knifestatus", knifeStatus]);
                if (isMatchSetup)
                {
                    // player.PrintToChat($"{chatPrefix} Minimum Ready Players Required (Per Team): {ChatColors.Green}{matchConfig.MinPlayersToReady}{ChatColors.Default}");
                    PrintToPlayerChat(player, Localizer["matchzy.cc.minreadyplayersperteam", matchConfig.MinPlayersToReady]);
                    // player.PrintToChat($"{chatPrefix} Minimum Ready Spectators Required: {ChatColors.Green}{matchConfig.MinSpectatorsToReady}{ChatColors.Default}");
                    PrintToPlayerChat(player, Localizer["matchzy.cc.minreadyspecs", matchConfig.MinSpectatorsToReady]);
                }
                else
                {
                    // player.PrintToChat($"{chatPrefix} Minimum Ready Required: {ChatColors.Green}{minimumReadyRequired}{ChatColors.Default}");
                    PrintToPlayerChat(player, Localizer["matchzy.cc.minreadyplayers", minimumReadyRequired]);
                }
                // player.PrintToChat($"{chatPrefix} Playout: {ChatColors.Green}{playoutStatus}{ChatColors.Default}");
                PrintToPlayerChat(player, Localizer["matchzy.cc.playoutstatus", playoutStatus]);
            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("css_endmatch", "Ends and resets the current match")]
        [ConsoleCommand("get5_endmatch", "Ends and resets the current match")]
        [ConsoleCommand("css_forceend", "Ends and resets the current match")]
        public void OnEndMatchCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (IsPlayerAdmin(player, "css_endmatch", "@css/config"))
            {
                if (!isPractice)
                {
                    // Server.PrintToChatAll($"{chatPrefix} An admin force-ended the match.");
                    PrintToAllChat(Localizer["matchzy.cc.endmatch"]);
                    ResetMatch();
                }
                else
                {
                    // ReplyToUserCommand(player, "Practice mode is active, cannot end the match.");
                    ReplyToUserCommand(player, Localizer["matchzy.cc.endmatchispracc"]);
                }
            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("css_restart", "Restarts the match")]
        [ConsoleCommand("css_rr", "Restarts the match")]
        public void OnRestartMatchCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (IsPlayerAdmin(player, "css_restart", "@css/config"))
            {
                if (!isPractice)
                {
                    ResetMatch();
                }
                else
                {
                    // ReplyToUserCommand(player, "Practice mode is active, cannot restart the match.");
                    ReplyToUserCommand(player, Localizer["matchzy.cc.rrispracc"]);
                }
            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("css_map", "Changes the map using changelevel")]
        public void OnChangeMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            var mapName = command.ArgByIndex(1);
            HandleMapChangeCommand(player, mapName);
        }

        [ConsoleCommand("css_rmap", "Reloads the current map")]
        private void OnMapReloadCommand(CCSPlayerController? player, CommandInfo? command)
        {

            if (!IsPlayerAdmin(player))
            {
                SendPlayerNotAdminMessage(player);
                return;
            }
            string currentMapName = Server.MapName;
            if (long.TryParse(currentMapName, out _))
            { // Check if mapName is a long for workshop map ids
                Server.ExecuteCommand($"bot_kick");
                Server.ExecuteCommand($"host_workshop_map \"{currentMapName}\"");
            }
            else if (Server.IsMapValid(currentMapName))
            {
                Server.ExecuteCommand($"bot_kick");
                Server.ExecuteCommand($"changelevel \"{currentMapName}\"");
            }
            else
            {
                // ReplyToUserCommand(player, "Invalid map name!");
                ReplyToUserCommand(player, Localizer["matchzy.cc.invalidmap"]);
            }
        }

        [ConsoleCommand("css_start", "Force starts the match")]
        [ConsoleCommand("css_force", "Force starts the match")]
        [ConsoleCommand("css_forcestart", "Force starts the match")]
        public void OnStartCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (IsPlayerAdmin(player, "css_start", "@css/config"))
            {
                if (isPractice)
                {
                    // ReplyToUserCommand(player, "Cannot start a match while in practice mode. Please use .exitprac command to exit practice mode first!");
                    ReplyToUserCommand(player, Localizer["matchzy.cc.startisprac"]);
                    return;
                }
                if (matchStarted)
                {
                    //ReplyToUserCommand(player, "Start command cannot be used if match is already started! If you want to unpause, please use .unpause");
                    ReplyToUserCommand(player, Localizer["matchzy.cc.startmatchstarted"]);
                }
                else
                {
                    //Server.PrintToChatAll($"{chatPrefix} {ChatColors.Green}Admin{ChatColors.Default} has started the game!");
                    PrintToAllChat(Localizer["matchzy.cc.gamestarted"]);
                    HandleMatchStart();
                }
            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("css_asay", "Say as an admin")]
        public void OnAdminSay(CCSPlayerController? player, CommandInfo? command)
        {
            if (command == null) return;
            if (player == null)
            {
                Server.PrintToChatAll($"{adminChatPrefix} {command.ArgString}");
                return;
            }
            if (!IsPlayerAdmin(player, "css_asay", "@css/chat"))
            {
                SendPlayerNotAdminMessage(player);
                return;
            }
            string message = "";
            for (int i = 1; i < command.ArgCount; i++)
            {
                message += command.ArgByIndex(i) + " ";
            }
            Server.PrintToChatAll($"{adminChatPrefix} {message}");
        }

        [ConsoleCommand("reload_admins", "Reload admins of MatchZy")]
        public void OnReloadAdmins(CCSPlayerController? player, CommandInfo? command)
        {
            if (IsPlayerAdmin(player, "reload_admins", "@css/config"))
            {
                LoadAdmins();
                UpdatePlayersMap();
            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("css_match", "Starts match mode")]
        public void OnMatchCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!IsPlayerAdmin(player, "css_match", "@css/map", "@custom/prac"))
            {
                SendPlayerNotAdminMessage(player);
                return;
            }

            if (matchStarted)
            {
                // ReplyToUserCommand(player, "MatchZy is already in match mode!");
                ReplyToUserCommand(player, Localizer["matchzy.cc.match"]);
                return;
            }

            StartMatchMode();
        }

        [ConsoleCommand("css_exitprac", "Starts match mode")]
        public void OnExitPracCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!IsPlayerAdmin(player, "css_exitprac", "@css/map", "@custom/prac"))
            {
                SendPlayerNotAdminMessage(player);
                return;
            }

            if (matchStarted)
            {
                //ReplyToUserCommand(player, "MatchZy is already in match mode!");
                ReplyToUserCommand(player, Localizer["matchzy.cc.exitprac"]);
                return;
            }

            StartMatchMode();
        }

        [ConsoleCommand("css_rcon", "Triggers provided command on the server")]
        public void OnRconCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsPlayerAdmin(player, "css_rcon", "@css/rcon"))
            {
                SendPlayerNotAdminMessage(player);
                return;
            }
            Server.ExecuteCommand(command.ArgString);
            // ReplyToUserCommand(player, "Command sent successfully!");
            ReplyToUserCommand(player, Localizer["matchzy.cc.rcon"]);

        }

        [ConsoleCommand("css_help", "Triggers provided command on the server")]
        public void OnHelpCommand(CCSPlayerController? player, CommandInfo? command)
        {
            SendAvailableCommandsMessage(player);
        }

        [ConsoleCommand("css_playout", "Toggles playout (Playing of max rounds)")]
        public void OnPlayoutCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (IsPlayerAdmin(player, "css_playout", "@css/config"))
            {
                isPlayOutEnabled = !isPlayOutEnabled;
                string playoutStatus = isPlayOutEnabled ? Localizer["matchzy.cc.enabled"] : Localizer["matchzy.cc.disabled"];
                if (player == null)
                {
                    // ReplyToUserCommand(player, $"Playout is now {playoutStatus}!");
                    ReplyToUserCommand(player, Localizer["matchzy.cc.playout", playoutStatus]);
                }
                else
                {
                    // player.PrintToChat($"{chatPrefix} Playout is now {ChatColors.Green}{playoutStatus}{ChatColors.Default}!");
                    PrintToPlayerChat(player, Localizer["matchzy.cc.playout", playoutStatus]);
                }

                HandlePlayoutConfig();

            }
            else
            {
                SendPlayerNotAdminMessage(player);
            }
        }

        [ConsoleCommand("version", "Returns server version")]
        public void OnVersionCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (command == null) return;
            string steamInfFilePath = Path.Combine(Server.GameDirectory, "csgo", "steam.inf");

            if (!File.Exists(steamInfFilePath))
            {
                command.ReplyToCommand("Unable to locate steam.inf file!");
            }
            var steamInfContent = File.ReadAllText(steamInfFilePath);

            Regex regex = new(@"ServerVersion=(\d+)");
            Match match = regex.Match(steamInfContent);

            // Extract the version number
            string? serverVersion = match.Success ? match.Groups[1].Value : null;

            // Currently returning only server version to show server status as available on Get5
            command.ReplyToCommand((serverVersion != null) ? $"Protocol version {serverVersion} [{serverVersion}/{serverVersion}]" : "Unable to get server version");
        }
    }
}
