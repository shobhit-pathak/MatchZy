using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json.Linq;


namespace MatchZy
{

    public partial class MatchZy
    {
        public MatchConfig matchConfig = new();

        public bool isMatchSetup = false;

        public bool matchModeOnly = false;

        public bool resetCvarsOnSeriesEnd = true;

        public Team matchzyTeam1 = new() {
            teamName = "COUNTER-TERRORISTS"
        };
        public Team matchzyTeam2 = new() {
            teamName = "TERRORISTS"
        };

        public Dictionary<Team, string> teamSides = new();
        public Dictionary<string, Team> reverseTeamSides = new();

        [ConsoleCommand("matchzy_loadmatch", "Loads a match from the given JSON file path (relative to the csgo/ directory)")]
        public void LoadMatch(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            if (isMatchSetup)
            {
                Log($"[LoadMatch] A match is already setup with id: {liveMatchId}, cannot load a new match!");
                return;
            }
            string fileName = command.ArgString;
            string filePath = Path.Join(Server.GameDirectory + "/csgo", fileName);
            if (!File.Exists(filePath)) 
            {
                Log($"[LoadMatch] Provided file does not exist! Usage: matchzy_loadmatch <filename>");
                return;
            }
            string jsonData = File.ReadAllText(filePath);
            LoadMatchFromJSON(jsonData);
        }

        [ConsoleCommand("matchzy_loadmatch_url", "Loads a match from the given URL")]
        public void LoadMatchFromURL(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null) return;
            if (isMatchSetup)
            {
                Log($"[LoadMatchDataCommand] A match is already setup with id: {liveMatchId}, cannot load a new match!");
                return;
            }
            string url = command.ArgByIndex(1);
            Log($"[LoadMatchDataCommand] Match setup request received with URL: {url}");
            if (!IsValidUrl(url))
            {
                Log($"[LoadMatchDataCommand] Invalid URL: {url}. Please provide a valid URL to load the match!");
                return;
            }
            try
            {
                HttpClient httpClient = new();
                HttpResponseMessage response = httpClient.GetAsync(url).Result;

                if (response.IsSuccessStatusCode)
                {
                    string jsonData = response.Content.ReadAsStringAsync().Result;
                    Log($"[LoadMatchFromURL] Received following data: {jsonData}");

                    LoadMatchFromJSON(jsonData);
                }
                else
                {
                    Log($"[LoadMatchFromURL] HTTP request failed with status code: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                Log($"[LoadMatchFromURL - FATAL] An error occured: {e.Message}");
                return;
            }
        }

        static string ValidateMatchJsonStructure(JObject jsonData)
        {
            string[] requiredFields = { "matchid", "maplist", "team1", "team2", "num_maps", "map_sides" };

            // Check if any required field is missing
            foreach (string field in requiredFields)
            {
                if (jsonData[field] == null)
                {
                    return $"Missing mandatory field: {field}";
                }
            }

            foreach (var property in jsonData.Properties())
            {
                string field = property.Name;

                switch (field)
                {
                    case "matchid":
                    case "min_players_to_ready":
                    case "num_maps":
                        int numMaps;
                        if (!int.TryParse(jsonData[field].ToString(), out numMaps))
                        {
                            return $"{field} should be an integer!";
                            
                        }
                        if (field == "num_maps" && numMaps > jsonData["maplist"].ToObject<List<string>>().Count)
                        {
                            return $"{field} should be equal to or greater than maplist!";
                        }
                        
                        break;
                    
                    case "cvars":
                        if (jsonData[field].Type != JTokenType.Object)
                        {
                            return $"{field} should be a JSON structure!";
                        }
                        break;

                    case "team1":
                    case "team2":
                    case "spectators":
                        if (jsonData[field].Type != JTokenType.Object)
                        {
                            return $"{field} should be a JSON structure!";
                        }
                        if (jsonData[field]["players"] == null || jsonData[field]["players"].Type != JTokenType.Object) 
                        {
                            return $"{field} should have 'players' JSON!";
                        }
                        break;

                    case "maplist":
                        if (jsonData[field].Type != JTokenType.Array)
                        {
                            return $"{field} should be an Array!";
                        }
                        if (!jsonData[field].Any())
                        {
                            return $"{field} should contain atleast 1 map!";
                        }

                        break;
                    case "map_sides":
                        if (jsonData[field].Type != JTokenType.Array)
                        {
                            return $"{field} should be an Array!";
                        }
                        string[] allowedValues = { "team1_ct", "team1_t", "team2_ct", "team2_t", "knife" };
                        bool allElementsValid = jsonData[field].All(element => allowedValues.Contains(element.ToString()));

                        if (!allElementsValid) {
                            return $"{field} should be \"team1_ct\", \"team1_t\", or \"knife\"!";
                        }
                        
                        if (jsonData[field].ToObject<List<string>>().Count < jsonData["num_maps"].Value<int>()) {
                            return $"{field} should be equal to or greater than num_maps!";
                        }
                        break;

                    case "clinch_series":
                        if (!Convert.ToBoolean(jsonData[field].ToString()))
                        {
                            return $"{field} should be a boolean!";
                        }
                        break;
                }
            }

            return "";
        }

        public void LoadMatchFromJSON(string jsonData)
        {
            
            JObject jsonDataObject = JObject.Parse(jsonData);

            string validationError = ValidateMatchJsonStructure(jsonDataObject);

            if (validationError != "")
            {
                Log($"[LoadMatchDataCommand] {validationError}");
                return;
            }

            liveMatchId = (long)jsonDataObject["matchid"]!;
            JToken team1 = jsonDataObject["team1"]!;
            JToken team2 = jsonDataObject["team2"]!;
            JToken maplist = jsonDataObject["maplist"]!;

            matchzyTeam1.teamName = RemoveSpecialCharacters(team1["name"].ToString());
            matchzyTeam2.teamName = RemoveSpecialCharacters(team2["name"].ToString());
            matchzyTeam1.teamPlayers = team1["players"];
            matchzyTeam2.teamPlayers = team2["players"];
            if(jsonDataObject["min_players_to_ready"] != null)
            {
                minimumReadyRequired = jsonDataObject["min_players_to_ready"]!.Value<int>();
            }

            matchConfig = new()
            {
                MatchId = liveMatchId,
                Maplist = maplist.ToObject<List<string>>()!,
                NumMaps = jsonDataObject["num_maps"]!.Value<int>(),
                MapSides = jsonDataObject["map_sides"]!.ToObject<List<string>>()!,
                MinPlayersToReady = minimumReadyRequired
            };

            if (jsonDataObject["spectators"]!["players"] != null)
            {
                matchConfig.Spectators = jsonDataObject["spectators"]!["players"];
            }
            if (jsonDataObject["clinch_series"] != null)
            {
                matchConfig.SeriesCanClinch = Convert.ToBoolean(jsonDataObject["clinch_series"]!.ToString());
            }

            GetCvarValues(jsonDataObject);

            Log($"[LOADMATCH] MinPlayersToReady: {matchConfig.MinPlayersToReady} SeriesClinch: {matchConfig.SeriesCanClinch}");

            LoadClientNames();

            string mapName = matchConfig.Maplist[0].ToString();

            if (long.TryParse(mapName, out _)) {
                Server.ExecuteCommand($"host_workshop_map \"{mapName}\"");
            } else if (Server.IsMapValid(mapName)) {
                Server.ExecuteCommand($"changelevel \"{mapName}\"");
            } else {
                Log($"[LoadMatchFromJSON] Invalid map name: {mapName}, cannot setup match!");
                ResetMatch(false);
                return;
            }

            StartWarmup();

            isMatchSetup = true;

            SetMapSides();

            Log($"[LoadMatchFromJSON] Success with matchid: {liveMatchId}!");
        }

        public void SetMapSides() {
            int mapNumber = matchConfig.CurrentMapNumber;
            if (matchConfig.MapSides[mapNumber] == "team1_ct" || matchConfig.MapSides[mapNumber] == "team2_t")
            {
                teamSides[matchzyTeam1] = "CT";
                teamSides[matchzyTeam2] = "TERRORIST";
                reverseTeamSides["CT"] = matchzyTeam1;
                reverseTeamSides["TERRORIST"] = matchzyTeam2;
                isKnifeRequired = false;
            }
            else if (matchConfig.MapSides[mapNumber] == "team2_ct" || matchConfig.MapSides[mapNumber] == "team1_t")
            {
                teamSides[matchzyTeam2] = "CT";
                teamSides[matchzyTeam1] = "TERRORIST";
                reverseTeamSides["CT"] = matchzyTeam2;
                reverseTeamSides["TERRORIST"] = matchzyTeam1;
                isKnifeRequired = false;
            }
            else if (matchConfig.MapSides[mapNumber] == "knife")
            {
                isKnifeRequired = true;
            }
            Server.ExecuteCommand($"mp_teamname_1 {reverseTeamSides["CT"].teamName}");
            Server.ExecuteCommand($"mp_teamname_2 {reverseTeamSides["TERRORIST"].teamName}");
        }

        public void GetCvarValues(JObject jsonDataObject)
        {
            try
            {
                if (jsonDataObject["cvars"] != null)
                {
                    foreach (JProperty cvarData in jsonDataObject["cvars"])
                    {
                        string cvarName = cvarData.Name;
                        string cvarValue = cvarData.Value.ToString();

                        var cvar = ConVar.Find(cvarName);
                        if (cvar != null)
                        {
                            matchConfig.ChangedCvars[cvarName] = cvarValue;
                            matchConfig.OriginalCvars[cvarName] = GetConvarStringValue(cvar);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log($"[GetCvarValues FATAL] An error occurred: {e.Message}");
            }
        }


        [ConsoleCommand("css_team1", "Sets team name for team1")]
        public void OnTeam1Command(CCSPlayerController? player, CommandInfo command) {
            HandleTeamNameChangeCommand(player, command.ArgString, 1);
        }

        [ConsoleCommand("css_team2", "Sets team name for team1")]
        public void OnTeam2Command(CCSPlayerController? player, CommandInfo command) {
            HandleTeamNameChangeCommand(player, command.ArgString, 2);
        }

        public void HandleTeamNameChangeCommand(CCSPlayerController? player, string teamName, int teamNum) {
            if (!IsPlayerAdmin(player, "css_team", "@css/config")) {
                SendPlayerNotAdminMessage(player);
                return;
            }
            if (matchStarted) {
                ReplyToUserCommand(player, "Team names cannot be changed once the match is started!");
                return;
            }
            teamName = RemoveSpecialCharacters(teamName.Trim());
            if (teamName == "") {
                ReplyToUserCommand(player, $"Usage: !team{teamNum} <name>");
            }

            if (teamNum == 1) {
                matchzyTeam1.teamName = teamName;
                teamSides[matchzyTeam1] = "CT";
                reverseTeamSides["CT"] = matchzyTeam1;
                if (matchzyTeam1.coach != null) matchzyTeam1.coach.Clan = $"[{matchzyTeam1.teamName} COACH]";
            } else if (teamNum == 2) {
                matchzyTeam2.teamName = teamName;
                teamSides[matchzyTeam2] = "TERRORIST";
                reverseTeamSides["TERRORIST"] = matchzyTeam2;
                if (matchzyTeam2.coach != null) matchzyTeam2.coach.Clan = $"[{matchzyTeam2.teamName} COACH]";
            }
            Server.ExecuteCommand($"mp_teamname_{teamNum} {teamName};");
        }

        public void SwapSidesInTeamData(bool swapTeams) {
            // if (swapTeams) {
            //     // Here, we sync matchzyTeam1 and matchzyTeam2 with the actual team1 and team2
            //     (matchzyTeam2, matchzyTeam1) = (matchzyTeam1, matchzyTeam2);
            // }

            (teamSides[matchzyTeam1], teamSides[matchzyTeam2]) = (teamSides[matchzyTeam2], teamSides[matchzyTeam1]);
            (reverseTeamSides["CT"], reverseTeamSides["TERRORIST"]) = (reverseTeamSides["TERRORIST"], reverseTeamSides["CT"]);
        }

        private CsTeam GetPlayerTeam(CCSPlayerController player)
        {
            CsTeam playerTeam = CsTeam.None;
            var steamId = player.SteamID;
            try
            {
                if (matchzyTeam1.teamPlayers[steamId.ToString()] != null)
                {
                    if (teamSides[matchzyTeam1] == "CT")
                    {
                        playerTeam = CsTeam.CounterTerrorist;
                    }
                    else if (teamSides[matchzyTeam1] == "TERRORIST")
                    {
                        playerTeam = CsTeam.Terrorist;
                    }

                }
                else if (matchzyTeam2.teamPlayers[steamId.ToString()] != null)
                {
                    if (teamSides[matchzyTeam2] == "CT")
                    {
                        playerTeam = CsTeam.CounterTerrorist;
                    }
                    else if (teamSides[matchzyTeam2] == "TERRORIST")
                    {
                        playerTeam = CsTeam.Terrorist;
                    }
                }
                else if (matchConfig.Spectators[steamId.ToString()] != null)
                {
                    playerTeam = CsTeam.Spectator;
                }
            }
            catch (Exception ex)
            {
                Log($"[GetPlayerTeam] Exception occurred: {ex.Message}");
            }
            return playerTeam;
        }

        public void EndSeries(string winnerName, int restartDelay)
        {
            Server.PrintToChatAll($"{chatPrefix} {ChatColors.Green}{winnerName}{ChatColors.Default} has won the match");
            database.SetMatchEndData(liveMatchId, winnerName, matchzyTeam1.seriesScore, matchzyTeam2.seriesScore);
            if (resetCvarsOnSeriesEnd) ResetChangedConvars();
            AddTimer(restartDelay, () => {
                ResetMatch(false);
            });
        }

    }
}
