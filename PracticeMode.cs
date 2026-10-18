using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Text.Json;


namespace MatchZy
{
    public class Position
    {

        public Vector PlayerPosition { get; private set; }
        public QAngle PlayerAngle { get; private set; }

        // Copy constructor
        public Position(Position other)
        {
            PlayerPosition = other.PlayerPosition;
            PlayerAngle = other.PlayerAngle;
        }

        public Position(Vector playerPosition, QAngle playerAngle)
        {
            // Create deep copies of the Vector and QAngle objects
            PlayerPosition = new Vector(playerPosition.X, playerPosition.Y, playerPosition.Z);
            PlayerAngle = new QAngle(playerAngle.X, playerAngle.Y, playerAngle.Z);
        }

        public void Teleport(CCSPlayerController player)
        {
            player!.PlayerPawn.Value!.Teleport(PlayerPosition, PlayerAngle, new Vector(0, 0, 0));
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Position otherPosition = (Position)obj;

            return PlayerPosition.X == otherPosition.PlayerPosition.X &&
                PlayerPosition.Y == otherPosition.PlayerPosition.Y &&
                PlayerAngle.X == otherPosition.PlayerAngle.X &&
                PlayerAngle.Y == otherPosition.PlayerAngle.Y &&
                PlayerAngle.Z == otherPosition.PlayerAngle.Z;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + PlayerPosition.X.GetHashCode();
                hash = hash * 23 + PlayerPosition.Y.GetHashCode();
                hash = hash * 23 + PlayerPosition.Z.GetHashCode();
                hash = hash * 23 + PlayerAngle.X.GetHashCode();
                hash = hash * 23 + PlayerAngle.Y.GetHashCode();
                hash = hash * 23 + PlayerAngle.Z.GetHashCode();
                return hash;
            }
        }
    }

    public static class StringSimilarity
    {
        // Dice coefficient function
        public static double DiceCoefficient(string s1, string s2)
        {
            var bigrams1 = GetBigrams(s1);
            var bigrams2 = GetBigrams(s2);

            int intersection = bigrams1.Intersect(bigrams2).Count();
            return (2.0 * intersection) / (bigrams1.Count + bigrams2.Count);
        }

        // Get bigrams function
        private static List<string> GetBigrams(string input)
        {
            var bigrams = new List<string>();
            for (int i = 0; i < input.Length - 1; i++)
            {
                bigrams.Add(input.Substring(i, 2));
            }
            return bigrams;
        }

        /// <summary>
        /// Finds the name from a list of names that is nearest to the input name using the Dice coefficient.
        /// </summary>
        /// <param name="inputName">The input name to match.</param>
        /// <param name="names">The list of names to search from.</param>
        /// <returns>The nearest matching name from the list.</returns>
        public static string FindNearestName(string inputName, List<string> names)
        {
            if (inputName.Length == 1)
            {
                // If input name is a single character, find the name that starts with the same character
                var matchingName = names.FirstOrDefault(name => name.StartsWith(inputName, StringComparison.OrdinalIgnoreCase));
                if (matchingName != null)
                {
                    return matchingName;
                }
            }
            // Otherwise, use the Dice coefficient to find the nearest name
            string nearestName = names.OrderByDescending(name => DiceCoefficient(inputName, name)).FirstOrDefault() ?? inputName;
            return nearestName;
        }
    }

    public partial class MatchZy
    {
        int maxLastGrenadesSavedLimit = 512;
        Dictionary<int, List<GrenadeThrownData>> lastGrenadesData = new();
        Dictionary<int, Dictionary<string, GrenadeThrownData>> nadeSpecificLastGrenadeData = new();
        Dictionary<int, DateTime> lastGrenadeThrownTime = new();
        Dictionary<int, PlayerPracticeTimer> playerTimers = new();
        Dictionary<int, PlayerLocationData> savedPlayerLocationData = new();

        public Dictionary<byte, List<Position>> spawnsData = GetEmptySpawnsData();

        public Dictionary<byte, List<Position>> coachSpawns = GetEmptySpawnsData();

        public const string practiceCfgPath = "MatchZy/prac.cfg";
        public const string dryrunCfgPath = "MatchZy/dryrun.cfg";

        // This map stores the bots which are being used in prac (probably spawned using .bot). Key is the userid of the bot.
        public Dictionary<int, Dictionary<string, object>> pracUsedBots = new Dictionary<int, Dictionary<string, object>>();

        private CounterStrikeSharp.API.Modules.Timers.Timer? collisionGroupTimer;
        private CounterStrikeSharp.API.Modules.Timers.Timer? fastForwardTimer;
        // Players killed by a practice team switch (.t/.ct/.spec), with their score and deaths from before the kill, which are put
        // back so the switch does not count on the scoreboard.
        private readonly Dictionary<int, (int Score, int Deaths)> practiceSwitchNoDeath = new();
        private Dictionary<int, MoveType_t> preFastForwardMoveTypes = new();
        // Molotov/incendiary projectile index -> throw time and thrower, for the burn time message (molotov_detonate has no entity id).
        private Dictionary<int, (DateTime ThrownTime, int UserId)> molotovThrows = new();

        public bool isSpawningBot;

        public bool isDryRun = false;

        public List<int> noFlashList = new List<int>();

        public static Dictionary<byte, List<Position>> GetEmptySpawnsData()
        {
            return new Dictionary<byte, List<Position>>
            {
                { (byte)CsTeam.CounterTerrorist, new List<Position>() },
                { (byte)CsTeam.Terrorist, new List<Position>() }
            };
        }

        public void StartPracticeMode()
        {
            if (matchStarted) return;
            isPractice = true;
            isDryRun = false;
            isWarmup = false;
            readyAvailable = false;

            var absolutePath = Path.Join(Server.GameDirectory + "/csgo/cfg", practiceCfgPath);

            if (File.Exists(Path.Join(Server.GameDirectory + "/csgo/cfg", practiceCfgPath)))
            {
                Log($"[StartWarmup] Starting Practice Mode! Executing Practice CFG from {practiceCfgPath}");
                Server.ExecuteCommand($"exec {practiceCfgPath}");
            }
            else
            {
                Log($"[StartWarmup] Starting Practice Mode! Practice CFG not found in {absolutePath}, using default CFG!");
                Server.ExecuteCommand("""sv_cheats "true"; mp_force_pick_time "0"; bot_quota "0"; sv_showimpacts "1"; mp_limitteams "0"; sv_deadtalk "true"; sv_full_alltalk "true"; sv_ignoregrenaderadio "false"; mp_forcecamera "0"; sv_grenade_trajectory_prac_pipreview "true"; sv_grenade_trajectory_prac_trailtime "3"; sv_infinite_ammo "1"; weapon_auto_cleanup_time "15"; weapon_max_before_cleanup "30"; mp_buy_anywhere "1"; mp_maxmoney "9999999"; mp_startmoney "9999999";""");
                Server.ExecuteCommand("""mp_weapons_allow_typecount "-1"; mp_death_drop_breachcharge "false"; mp_death_drop_defuser "false"; mp_death_drop_taser "false"; mp_drop_knife_enable "true"; mp_death_drop_grenade "0"; ammo_grenade_limit_total "5"; mp_defuser_allocation "2"; mp_free_armor "2"; mp_ct_default_grenades "weapon_incgrenade weapon_hegrenade weapon_smokegrenade weapon_flashbang weapon_decoy"; mp_ct_default_primary "weapon_m4a1";""");
                Server.ExecuteCommand("""mp_t_default_grenades "weapon_molotov weapon_hegrenade weapon_smokegrenade weapon_flashbang weapon_decoy"; mp_t_default_primary "weapon_ak47"; mp_warmup_online_enabled "true"; mp_warmup_pausetimer "1"; mp_warmup_start; bot_quota_mode normal;mp_solid_teammates 2; mp_autoteambalance false; mp_teammates_are_enemies false; buddha 1; buddha_ignore_bots 1; buddha_reset_hp 100; mp_autokick 0; mp_tkpunish 0;""");
            }
            ExecOverrideCfg(practiceCfgPath);
            GetSpawns();
            PrintToAllChat($"Practice mode loaded!");
            Server.PrintToChatAll($" {ChatColors.Green}Spawns: {ChatColors.Default}.spawn, .ctspawn, .tspawn, .bestspawn, .worstspawn");
            Server.PrintToChatAll($" {ChatColors.Green}Bots: {ChatColors.Default}.bot, .nobots, .crouchbot, .boost, .crouchboost");
            Server.PrintToChatAll($" {ChatColors.Green}Nades: {ChatColors.Default}.loadnade, .savenade, .importnade, .listnades");
            Server.PrintToChatAll($" {ChatColors.Green}Nade Throw: {ChatColors.Default}.rethrow, .throwindex <index>, .lastindex, .delay <number>");
            Server.PrintToChatAll($" {ChatColors.Green}Utility & Toggles: {ChatColors.Default}.clear, .fastforward, .last, .back, .solid, .impacts, .traj");
            // On new line to prevent text cutting off
            Server.PrintToChatAll($" {ChatColors.Green}Utility & Toggles: {ChatColors.Default}.savepos, .loadpos");
            Server.PrintToChatAll($" {ChatColors.Green}Sides & Others: {ChatColors.Default}.ct, .t, .spec, .fas, .god, .dryrun, .break, .exitprac");
        }

        public void GetSpawns()
        {
            // Resetting spawn data to avoid any glitches
            spawnsData = GetEmptySpawnsData();

            int minPriority = 1;

            var spawnsct = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist");
            foreach (var spawn in spawnsct)
            {
                if (spawn.IsValid && spawn.Enabled && spawn.Priority < minPriority)
                {
                    minPriority = spawn.Priority;
                }
            }

            foreach (var spawn in spawnsct)
            {
                if (spawn.IsValid && spawn.Enabled && spawn.Priority == minPriority)
                {
                    spawnsData[(byte)CsTeam.CounterTerrorist].Add(new Position(spawn.CBodyComponent?.SceneNode?.AbsOrigin!, spawn.CBodyComponent?.SceneNode?.AbsRotation!));
                }
            }

            var spawnst = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist");
            foreach (var spawn in spawnst)
            {
                if (spawn.IsValid && spawn.Enabled && spawn.Priority == minPriority)
                {
                    spawnsData[(byte)CsTeam.Terrorist].Add(new Position(spawn.CBodyComponent?.SceneNode?.AbsOrigin!, spawn.CBodyComponent?.SceneNode?.AbsRotation!));
                }
            }

            GetCoachSpawns();
        }

        private void HandleSpawnCommand(CCSPlayerController? player, string commandArg, byte teamNum, string command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            if (teamNum != 2 && teamNum != 3) return;
            if (!string.IsNullOrWhiteSpace(commandArg))
            {
                if (int.TryParse(commandArg, out int spawnNumber) && spawnNumber >= 1)
                {
                    // Adjusting the spawnNumber according to the array index.
                    spawnNumber -= 1;
                    if (spawnsData.ContainsKey(teamNum) && spawnsData[teamNum].Count <= spawnNumber) return;
                    player!.PlayerPawn.Value!.Teleport(spawnsData[teamNum][spawnNumber].PlayerPosition, spawnsData[teamNum][spawnNumber].PlayerAngle, new Vector(0, 0, 0));
                    // ReplyToUserCommand(player, $"Moved to spawn: {spawnNumber+1}/{spawnsData[teamNum].Count}");
                    ReplyToUserCommand(player, Localizer["matchzy.pm.movedtospawn", $"{spawnNumber + 1}/{spawnsData[teamNum].Count}"]);
                }
                else
                {
                    // ReplyToUserCommand(player, $"Invalid value for {command} command. Please specify a valid non-negative number. Usage: !{command} <number>");
                    ReplyToUserCommand(player, Localizer["matchzy.pm.negativenumber"]);
                    return;
                }
            }
            else
            {
                // ReplyToUserCommand(player, $"Usage: !{command} <number>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", $"!{command} <number>"]);
            }
        }

        private string GetNadeType(string nadeName)
        {
            switch (nadeName)
            {
                case "weapon_flashbang":
                    return "Flash";
                case "weapon_smokegrenade":
                    return "Smoke";
                case "weapon_hegrenade":
                    return "HE";
                case "weapon_decoy":
                    return "Decoy";
                case "weapon_molotov":
                    return "Molly";
                case "weapon_incgrenade":
                    return "Molly";
                default:
                    return "";
            }
        }

        private void HandleSaveNadeCommand(CCSPlayerController? player, string saveNadeName)
        {
            if (!isPractice || !IsPlayerValid(player)) return;

            if (!string.IsNullOrWhiteSpace(saveNadeName))
            {
                // Split string into 2 parts
                string[] lineupUserString = saveNadeName.Split(' ');
                string lineupName = lineupUserString[0];
                string lineupDesc = string.Join(" ", lineupUserString, 1, lineupUserString.Length - 1);

                // Get player info: steamid, pos, ang
                string playerSteamID;
                if(isSaveNadesAsGlobalEnabled == false)
                {
                    playerSteamID = player!.SteamID.ToString();
                }
                else
                {
                    playerSteamID = "default";
                }

                QAngle playerAngle = player!.PlayerPawn.Value!.EyeAngles;
                Vector playerPos = player.Pawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin;
                string currentMapName = Server.MapName;
                string nadeType = GetNadeType(player.PlayerPawn.Value.WeaponServices!.ActiveWeapon.Value!.DesignerName);

                // Define the file path
                string savednadesfileName = "MatchZy/savednades.json";
                string savednadesPath = Path.Join(Server.GameDirectory + "/csgo/cfg", savednadesfileName);

                // Check if the file exists, if not, create it with an empty JSON object
                if (!File.Exists(savednadesPath))
                {
                    File.WriteAllText(savednadesPath, "{}");
                }

                try
                {
                    // Read existing JSON content
                    string existingJson = ReadSavedNadesJson(savednadesPath);

                    // Deserialize the existing JSON content
                    var savedNadesDict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(existingJson)
                                        ?? new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

                    // Check if the lineup name already exists for the given SteamID
                    if (savedNadesDict.ContainsKey(playerSteamID) && savedNadesDict[playerSteamID].ContainsKey(lineupName))
                    {
                        // Check if the lineup already exists on the same map
                        if (savedNadesDict[playerSteamID][lineupName]["Map"] == currentMapName)
                        {
                            // Lineup already exists on the same map, reply to the user and return
                            // ReplyToUserCommand(player, $"Lineup already exists! Please use a different name or use .delnade <nade>");
                            ReplyToUserCommand(player, Localizer["matchzy.pm.lineupissaved"]);
                            return;
                        }
                    }

                    // Update or add the new lineup information
                    if (!savedNadesDict.ContainsKey(playerSteamID))
                    {
                        savedNadesDict[playerSteamID] = new Dictionary<string, Dictionary<string, string>>();
                    }

                    savedNadesDict[playerSteamID][lineupName] = new Dictionary<string, string>
                    {
                        { "LineupPos", LineupFormat.Format(playerPos.X, playerPos.Y, playerPos.Z) },
                        { "LineupAng", LineupFormat.Format(playerAngle.X, playerAngle.Y, playerAngle.Z) },
                        { "Desc", lineupDesc },
                        { "Map", currentMapName },
                        { "Type", nadeType }
                    };

                    // Serialize the updated dictionary back to JSON
                    string updatedJson = JsonSerializer.Serialize(savedNadesDict, new JsonSerializerOptions { WriteIndented = true });

                    // Write the updated JSON content back to the file
                    File.WriteAllText(savednadesPath, updatedJson);

                    PrintToPlayerChat(player, Localizer["matchzy.pm.lineupsavedsucces", lineupName]);
                    PrintToAllChat(Localizer["matchzy.pm.playersavedlineup", player.PlayerName, $"{lineupName} {playerPos} {playerAngle}"]);
                }
                catch (JsonException ex)
                {
                    Log($"Error handling JSON: {ex.Message}");
                }
            }
            else
            {
                // ReplyToUserCommand(player, $"Usage: .savenade <name>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", $".savenade <name>"]);
            }
        }

        private void HandleDeleteNadeCommand(CCSPlayerController? player, string saveNadeName)
        {
            if (!isPractice || player == null) return;

            if (!string.IsNullOrWhiteSpace(saveNadeName))
            {
                // Grab player steamid
                string playerSteamID;
                if(isSaveNadesAsGlobalEnabled == false)
                {
                    playerSteamID = player.SteamID.ToString();
                }
                else
                {
                    playerSteamID = "default";
                }

                // Define the file path
                string savednadesfileName = "MatchZy/savednades.json";
                string savednadesPath = Path.Join(Server.GameDirectory + "/csgo/cfg", savednadesfileName);

                try
                {
                    // Read existing JSON content
                    string existingJson = ReadSavedNadesJson(savednadesPath);

                    //Console.WriteLine($"Existing JSON Content: {existingJson}");

                    // Deserialize the existing JSON content
                    var savedNadesDict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(existingJson)
                                        ?? new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

                    // Check if the lineup exists for the given SteamID and name
                    if (savedNadesDict.ContainsKey(playerSteamID) && savedNadesDict[playerSteamID].ContainsKey(saveNadeName))
                    {
                        var lineupInfo = savedNadesDict[playerSteamID][saveNadeName];

                        // Check if the lineup is for the current maps
                        if (lineupInfo.ContainsKey("Map") && lineupInfo["Map"] == Server.MapName)
                        {
                            // Remove the specified lineup
                            savedNadesDict[playerSteamID].Remove(saveNadeName);

                            // Serialize the updated dictionary back to JSON
                            string updatedJson = JsonSerializer.Serialize(savedNadesDict, new JsonSerializerOptions { WriteIndented = true });

                            // Write the updated JSON content back to the file
                            File.WriteAllText(savednadesPath, updatedJson);

                            // ReplyToUserCommand(player, $"Lineup '{saveNadeName}' deleted successfully.");
                            ReplyToUserCommand(player, Localizer["matchzy.pm.lineupdeletesuccess", saveNadeName]);
                        }
                        else
                        {
                            // ReplyToUserCommand(player, $"Lineup '{saveNadeName}' not found on the current map!");
                            ReplyToUserCommand(player, Localizer["matchzy.pm.nadenotfoundonmap", saveNadeName]);
                        }
                    }
                    else
                    {
                        // ReplyToUserCommand(player, $"Lineup '{saveNadeName}' not found!");
                        ReplyToUserCommand(player, Localizer["matchzy.pm.lineupnotfound", saveNadeName]);
                    }
                }
                catch (JsonException ex)
                {
                    Log($"Error handling JSON: {ex.Message}");
                }
            }
            else
            {
                // ReplyToUserCommand(player, $"Usage: .delnade <name>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", $".delnade <name>"]);
            }
        }

        private void HandleImportNadeCommand(CCSPlayerController? player, string saveNadeCode)
        {
            if (!isPractice || player == null) return;

            if (!string.IsNullOrWhiteSpace(saveNadeCode))
            {
                try
                {
                    // Split the code into parts
                    string[] parts = saveNadeCode.Split(' ');

                    // Check if there are enough parts
                    if (parts.Length == 7)
                    {
                        // Extract name, pos, and ang from the parts
                        string lineupName = parts[0].Trim();
                        // Codes separate the numbers with ", " so only a trailing ',' is removed: a ',' inside a number (a ',' decimal
                        // separator) makes it invalid instead of a different number.
                        string[] posAng = parts.Skip(1).Select(p => p.TrimEnd(',')).ToArray();
                        if (!posAng.All(LineupFormat.IsValidNumber))
                        {
                            // Saved as is, an invalid number would make the lineup fail every time it is loaded.
                            ReplyToUserCommand(player, Localizer["matchzy.pm.importnadeinvalid"]);
                            return;
                        }

                        // Get player info: steamid
                        string playerSteamID = player.SteamID.ToString();
                        string currentMapName = Server.MapName;

                        // Define the file path
                        string savednadesfileName = "MatchZy/savednades.json";
                        string savednadesPath = Path.Join(Server.GameDirectory + "/csgo/cfg", savednadesfileName);

                        // Read existing JSON content
                        string existingJson = ReadSavedNadesJson(savednadesPath);

                        //Console.WriteLine($"Existing JSON Content: {existingJson}");

                        // Deserialize the existing JSON content
                        var savedNadesDict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(existingJson)
                                            ?? new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

                        // Check if the lineup name already exists for the given SteamID on the same map
                        if (savedNadesDict.ContainsKey(playerSteamID) && savedNadesDict[playerSteamID].ContainsKey(lineupName))
                        {
                            var existingLineup = savedNadesDict[playerSteamID][lineupName];
                            if (existingLineup.ContainsKey("Map") && existingLineup["Map"] == currentMapName)
                            {
                                // Lineup already exists on the same map, reply to the user and return
                                // ReplyToUserCommand(player, $"Lineup '{lineupName}' already exists! Please use a different name or use .delnade <nade>");
                                ReplyToUserCommand(player, Localizer["matchzy.pm.lineupalreadyexists", lineupName]);
                                return;
                            }
                        }

                        // Update or add the new lineup information
                        if (!savedNadesDict.ContainsKey(playerSteamID))
                        {
                            savedNadesDict[playerSteamID] = new Dictionary<string, Dictionary<string, string>>();
                        }

                        savedNadesDict[playerSteamID][lineupName] = new Dictionary<string, string>
                        {
                            { "LineupPos", $"{posAng[0]} {posAng[1]} {posAng[2]}" },
                            { "LineupAng", $"{posAng[3]} {posAng[4]} {posAng[5]}" },
                            { "Desc", "" },
                            { "Map", currentMapName }
                        };

                        // Serialize the updated dictionary back to JSON
                        string updatedJson = JsonSerializer.Serialize(savedNadesDict, new JsonSerializerOptions { WriteIndented = true });

                        // Write the updated JSON content back to the file
                        File.WriteAllText(savednadesPath, updatedJson);

                        // ReplyToUserCommand(player, $"Lineup '{lineupName}' imported and saved successfully.");
                        ReplyToUserCommand(player, Localizer["matchzy.pm.lineupimportedsuccess"]);
                    }
                    else
                    {
                        // ReplyToUserCommand(player, $"Invalid code format. Please provide a valid code with name, pos, and ang.");
                        ReplyToUserCommand(player, Localizer["matchzy.pm.lineupinvalidcode"]);
                    }
                }
                catch (JsonException ex)
                {
                    Log($"Error handling JSON: {ex.Message}");
                }
            }
            else
            {
                // ReplyToUserCommand(player, $"Usage: .importnade <code>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", $".importnade <code>"]);
            }
        }

        private void HandleListNadesCommand(CCSPlayerController? player, string nadeFilter)
        {
            if (!isPractice || player == null) return;

            // Define the file path
            string savednadesfileName = "MatchZy/savednades.json";
            string savednadesPath = Path.Join(Server.GameDirectory + "/csgo/cfg", savednadesfileName);

            try
            {
                // Read existing JSON content
                string existingJson = ReadSavedNadesJson(savednadesPath);

                //Console.WriteLine($"Existing JSON Content: {existingJson}");

                // Deserialize the existing JSON content
                var savedNadesDict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(existingJson)
                                    ?? new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

                ReplyToUserCommand(player, $"\x0D-----All Saved Lineups for \x06{Server.MapName}\x0D-----");

                // List lineups for the specified player
                ListLineups(player, "default", Server.MapName, savedNadesDict, nadeFilter);

                // List lineups for the current player
                ListLineups(player, player.SteamID.ToString(), Server.MapName, savedNadesDict, nadeFilter);
            }
            catch (JsonException ex)
            {
                Log($"Error handling JSON: {ex.Message}");
                ReplyToUserCommand(player, $"Error handling JSON. Please check the server logs.");
            }
        }

        private void ListLineups(CCSPlayerController player, string steamID, string mapName, Dictionary<string, Dictionary<string, Dictionary<string, string>>> savedNadesDict, string nadeFilter)
        {
            if (savedNadesDict.ContainsKey(steamID))
            {
                foreach (var kvp in savedNadesDict[steamID])
                {
                    // Check if a filter is provided, and if so, apply the filter
                    if ((string.IsNullOrWhiteSpace(nadeFilter) || kvp.Key.Contains(nadeFilter, StringComparison.OrdinalIgnoreCase))
                        && kvp.Value.ContainsKey("Map") && kvp.Value["Map"] == mapName)
                    {
                        // Format and reply with the lineup name
                        ReplyToUserCommand(player, $"\x06[{kvp.Value["Type"]}] \x0D.loadnade \x06{kvp.Key}");
                    }
                }
            }
            else
            {
                // ReplyToUserCommand(player, $"No saved lineups found for the specified SteamID: ({steamID}).");
                ReplyToUserCommand(player, Localizer["matchzy.pm.nosavedlineups", steamID]);

            }
        }

        private void HandleLoadNadeCommand(CCSPlayerController? player, string loadNadeName)
        {
            if (!isPractice || player == null || !IsPlayerValid(player)) return;

            if (!string.IsNullOrWhiteSpace(loadNadeName))
            {
                // Get player info: steamid
                string playerSteamID = player.SteamID.ToString();

                // Define the file path
                string savednadesfileName = "MatchZy/savednades.json";
                string savednadesPath = Path.Join(Server.GameDirectory + "/csgo/cfg", savednadesfileName);

                try
                {
                    // Read existing JSON content
                    string existingJson = ReadSavedNadesJson(savednadesPath);

                    //Console.WriteLine($"Existing JSON Content: {existingJson}");

                    // Deserialize the existing JSON content
                    var savedNadesDict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(existingJson)
                                        ?? new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

                    bool lineupFound = false;
                    bool lineupOnWrongMap = false;

                    // Check for the lineup in the player's steamID and the fixed steamID
                    foreach (string currentSteamID in new[] { playerSteamID, "default" })
                    {
                        if (savedNadesDict.ContainsKey(currentSteamID))
                        {
                            // Filter nade names based on the current map
                            var nadeNamesOnCurrentMap = savedNadesDict[currentSteamID]
                                .Where(n => n.Value.ContainsKey("Map") && n.Value["Map"] == Server.MapName)
                                .Select(n => n.Key)
                                .ToList();

                            // Find the nearest matching name
                            string nearestName = StringSimilarity.FindNearestName(loadNadeName, nadeNamesOnCurrentMap);

                            if (savedNadesDict[currentSteamID].ContainsKey(nearestName))
                            {
                                var lineupInfo = savedNadesDict[currentSteamID][nearestName];

                                // Check if the lineup contains the "Map" key and if it matches the current map
                                if (lineupInfo.ContainsKey("Map") && lineupInfo["Map"] == Server.MapName)
                                {
                                    // Extract position and angle from the lineup information
                                    if (!LineupFormat.TryParse(lineupInfo["LineupPos"], out float posX, out float posY, out float posZ)
                                        || !LineupFormat.TryParse(lineupInfo["LineupAng"], out float angX, out float angY, out float angZ))
                                    {
                                        Log($"[LoadNade] Lineup {nearestName} has an invalid position or angle: {lineupInfo["LineupPos"]} / {lineupInfo["LineupAng"]}");
                                        ReplyToUserCommand(player, Localizer["matchzy.pm.lineupinvalid", nearestName]);
                                        return;
                                    }
                                    Vector loadedPlayerPos = new Vector(posX, posY, posZ);
                                    QAngle loadedPlayerAngle = new QAngle(angX, angY, angZ);

                                    // Teleport player
                                    PlayerTeleport.TeleportUpright(player, loadedPlayerPos, loadedPlayerAngle);

                                    // Change player inv slot (imported lineups have no type)
                                    switch (lineupInfo.GetValueOrDefault("Type", ""))
                                    {
                                        case "Flash":
                                            player.ExecuteClientCommand("slot7");
                                            break;
                                        case "Smoke":
                                            player.ExecuteClientCommand("slot8");
                                            break;
                                        case "HE":
                                            player.ExecuteClientCommand("slot6");
                                            break;
                                        case "Decoy":
                                            player.ExecuteClientCommand("slot9");
                                            break;
                                        case "Molly":
                                            player.ExecuteClientCommand("slot10");
                                            break;
                                        case "":
                                            player.ExecuteClientCommand("slot8");
                                            break;
                                    }

                                    // Extract description, if available
                                    string lineupDesc = lineupInfo.ContainsKey("Desc") ? lineupInfo["Desc"] : null;

                                    // Print messages
                                    // ReplyToUserCommand(player, $"Lineup {ChatColors.Green}{nearestName}{ChatColors.Default} loaded successfully!");
                                    ReplyToUserCommand(player, Localizer["matchzy.pm.lineuploadedsuccess", nearestName]);

                                    if (!string.IsNullOrWhiteSpace(lineupDesc))
                                    {
                                        player.PrintToCenter($"{lineupDesc}");
                                        // ReplyToUserCommand(player, $"Description: {ChatColors.Green}{lineupDesc}{ChatColors.Default}");
                                        ReplyToUserCommand(player, Localizer["matchzy.pm.lineupdesc", lineupDesc]);
                                    }

                                    lineupFound = true;
                                    break;
                                }
                                else
                                {
                                    // ReplyToUserCommand(player, $"Nade {ChatColor.Green}{nearestName}{ChatColor.Default} not found on the current map!");
                                    ReplyToUserCommand(player, Localizer["matchzy.pm.nadenotfoundonmap", nearestName]);
                                    lineupOnWrongMap = true;
                                }
                            }
                        }
                    }

                    if (!lineupFound && !lineupOnWrongMap)
                    {
                        // Lineup not found
                        // ReplyToUserCommand(player, $"Nade {ChatColor.Green}{loadNadeName}{ChatColor.Default} not found!");
                        ReplyToUserCommand(player, Localizer["matchzy.pm.nadenotfound", loadNadeName]);
                    }
                }
                catch (JsonException ex)
                {
                    Log($"Error handling JSON: {ex.Message}");
                }
            }
            else
            {
                // ReplyToUserCommand(player, $"Nade not found! Usage: .loadnade <name>");
                ReplyToUserCommand(player, Localizer["matchzy.pm.loadnadenotfound"]);
            }
        }

        public void ShowSpawnBeam(Position spawn, Color color)
        {
            CBeam? beam = Utilities.CreateEntityByName<CBeam>("beam");
            if (beam == null)
            {
                Log($"Failed to create beam for the spawn");
                return;
            }

            beam.LifeState = 1;
            beam.Width = 5;
            beam.Render = color;

            beam.EndPos.X = spawn.PlayerPosition.X;
            beam.EndPos.Y = spawn.PlayerPosition.Y;
            beam.EndPos.Z = spawn.PlayerPosition.Z + 100.0f;

            beam.Teleport(spawn.PlayerPosition, new QAngle(0, 0, 0), new Vector(0, 0, 0));

            beam.DispatchSpawn();
        }

        public void RemoveSpawnBeams()
        {
            var beams = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("beam");
            foreach (var beam in beams)
            {
                if (beam == null) continue;
                beam.Remove();
            }
        }

        [ConsoleCommand("css_god", "Sets Infinite health for player")]
        public void OnGodCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || player == null || !IsPlayerValid(player)) return;
	    
			int currentHP = player!.PlayerPawn!.Value!.Health;
			
			if(currentHP > 100)
			{
				player.PlayerPawn.Value.Health = 100;
				// ReplyToUserCommand(player, $"God mode disabled!");
                		ReplyToUserCommand(player, "God is " + Localizer["matchzy.cc.disabled"]);
				return;
			}
			else
			{
				player.PlayerPawn.Value.Health = 2147483647; // max 32bit int
				// ReplyToUserCommand(player, $"God mode enabled!");
                		ReplyToUserCommand(player, "God is " + Localizer["matchzy.cc.enabled"]);
				return;
			}
        }

        [ConsoleCommand("css_prac", "Starts practice mode")]
        [ConsoleCommand("css_tactics", "Starts practice mode")]
        public void OnPracCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!IsPlayerAdmin(player, "css_prac", "@css/map", "@custom/prac")) {
                SendPlayerNotAdminMessage(player);
                return;
            }

            if (matchStarted)
            {
                // ReplyToUserCommand(player, "Practice Mode cannot be started when a match has been started!");
                ReplyToUserCommand(player, Localizer["matchzy.pm.pracmatchstarted"]);
                return;
            }
	    
			// if (isPractice)
            // {
            //     StartMatchMode();
            //     return;
            // }
	
            StartPracticeMode();
        }

        [ConsoleCommand("css_dry", "Starts dryrun in practice mode")]
        [ConsoleCommand("css_dryrun", "Starts dryrun in practice mode")]
        public void OnDryRunCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!IsPlayerAdmin(player, "css_prac", "@css/map", "@custom/prac")) {
                SendPlayerNotAdminMessage(player);
                return;
            }
            if (matchStarted)
            {
                // ReplyToUserCommand(player, "Dryrun cannot be started when a match has been started!");
                ReplyToUserCommand(player, Localizer["matchzy.pm.dryrunmatchstarted"]);
                return;
            }
            if (!isPractice)
            {
                // ReplyToUserCommand(player, "Dryrun can only be started in practice mode!");
                ReplyToUserCommand(player, Localizer["matchzy.pm.dryrunnopractice"]);
                return;
            }

            KickBots();
            pracUsedBots = new Dictionary<int, Dictionary<string, object>>();
            noFlashList = new();

            ExecUnpracCommands();
            ExecDryRunCFG();

            isDryRun = true;
        }

        [ConsoleCommand("css_spawn", "Teleport to provided spawn")]
        public void OnSpawnCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice) return;
            // Checking if any of the Position List is empty
            if (spawnsData.Values.Any(list => list.Count == 0)) GetSpawns();
            if (player == null || !player.PlayerPawn.IsValid) return;

            if (command.ArgCount >= 2)
            {
                string commandArg = command.ArgByIndex(1);
                HandleSpawnCommand(player, commandArg, player.TeamNum, "spawn");
            }
            else
            {
                // ReplyToUserCommand(player, $"Usage: !spawn <round>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", $"!spawn <round>"]);
            }
        }

        [ConsoleCommand("css_ctspawn", "Teleport to provided CT spawn")]
        public void OnCtSpawnCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice) return;
            // Checking if any of the Position List is empty
            if (spawnsData.Values.Any(list => list.Count == 0)) GetSpawns();
            if (player == null || !player.PlayerPawn.IsValid) return;

            if (command.ArgCount >= 2)
            {
                string commandArg = command.ArgByIndex(1);
                HandleSpawnCommand(player, commandArg, (byte)CsTeam.CounterTerrorist, "ctspawn");
            }
            else
            {
                // ReplyToUserCommand(player, $"Usage: !ctspawn <round>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", $"!ctspawn <round>"]);
            }
        }

        [ConsoleCommand("css_tspawn", "Teleport to provided T spawn")]
        public void OnTSpawnCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice) return;
            // Checking if any of the Position List is empty
            if (spawnsData.Values.Any(list => list.Count == 0)) GetSpawns();
            if (player == null || !player.PlayerPawn.IsValid) return;

            if (command.ArgCount >= 2)
            {
                string commandArg = command.ArgByIndex(1);
                HandleSpawnCommand(player, commandArg, (byte)CsTeam.Terrorist, "tspawn");
            }
            else
            {
                // ReplyToUserCommand(player, $"Usage: !ctspawn <round>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", $"!ctspawn <round>"]);
            }
        }

        [ConsoleCommand("css_bot", "Spawns a bot at the player's position")]
        public void OnBotCommand(CCSPlayerController? player, CommandInfo? command)
        {
            AddBot(player, false);
        }

        [ConsoleCommand("css_cbot", "Spawns a crouched bot at the player's position")]
        [ConsoleCommand("css_crouchbot", "Spawns a crouched bot at the player's position")]
        public void OnCrouchBotCommand(CCSPlayerController? player, CommandInfo? command)
        {
            AddBot(player, true);
        }

        [ConsoleCommand("css_boost", "Spawns a bot at the player's position and boost the player on it")]
        public void OnBoostBotCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice) return;
            AddBot(player, false, boost: true);
        }

        [ConsoleCommand("css_crouchboost", "Spawns a crouched bot at the player's position and boost the player on it")]
        public void OnCrouchBoostBotCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice) return;
            AddBot(player, true, boost: true);
        }

        // A .bot waiting to be added. Spot is where the owner stood when the command was typed.
        private record BotRequest(CCSPlayerController Owner, bool Crouch, bool Boost, CsTeam Team, Position Spot);

        // Bots are added one at a time: while a bot_add is being claimed, any other new bot would look like its extra and be
        // kicked, so .bot typed in quick succession is queued.
        private readonly Queue<BotRequest> pendingBotRequests = new();

        // Incremented by CancelBotRequests, so a claim that is still waiting for its bot gives up (e.g. after .nobots).
        private int botRequestGeneration;

        // How many times ClaimBot looks for the new bot (0.1s after bot_add, then every 0.15s).
        private const int BotClaimAttempts = 5;

        // How practice bots are added:
        // - bot_quota_mode is normal (prac.cfg): with fill, humans count towards the quota, so bots were added and removed
        //   whenever a player joined or changed team.
        // - Only bot_add_t / bot_add_ct: a bot_join_team before it spawned a second bot.
        // - bot_quota is set to the tracked bots + 1 right after bot_add and to the tracked bots after the claim, so the
        //   quota never adds or refills extra bots.
        // - One bot_add can still spawn a bot on each team, and the requested one can arrive a tick later, so the claim
        //   prefers the requested team, retries briefly, and only then kicks the extras. A sweep afterwards catches late ones.
        // Returns false when no bot will be added.
        private bool AddBot(CCSPlayerController? player, bool crouch, bool boost = false)
        {
            try
            {
                if (!isPractice || player == null || !player.IsValid || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null) return false;
                if (HasLaunchOption("-nobots"))
                {
                    // The engine would still create a bot, but without a player model: an invisible bot that can be shot.
                    PrintToPlayerChat(player, Localizer["matchzy.pm.botsdisabled"]);
                    return false;
                }
                CsTeam team = (CsTeam)player.TeamNum switch
                {
                    CsTeam.CounterTerrorist => CsTeam.Terrorist,
                    CsTeam.Terrorist => CsTeam.CounterTerrorist,
                    _ => CsTeam.None,
                };
                if (team == CsTeam.None) return false; // Spectators get no bot, as before.
                if (!HasFreeSlotForBot())
                {
                    PrintToPlayerChat(player, Localizer["matchzy.pm.botserverfull"]);
                    return false;
                }
                var sceneNode = player.PlayerPawn.Value.CBodyComponent?.SceneNode;
                if (sceneNode == null) return false;
                if (player.PlayerPawn.Value.MovementServices != null)
                {
                    CCSPlayer_MovementServices movementService = new(player.PlayerPawn.Value.MovementServices.Handle);
                    if ((int)movementService.DuckAmount == 1)
                    {
                        // Player was crouching while using .bot command
                        crouch = true;
                    }
                }

                pendingBotRequests.Enqueue(new BotRequest(player, crouch, boost, team, new Position(sceneNode.AbsOrigin, sceneNode.AbsRotation)));
                if (!isSpawningBot) StartNextBotRequest();
                return true;
            }
            catch (Exception ex)
            {
                Log($"[AddBot - FATAL] Error: {ex.Message}");
                return false;
            }
        }

        // Drops queued .bot requests and makes a claim in progress give up.
        public void CancelBotRequests()
        {
            pendingBotRequests.Clear();
            botRequestGeneration++;
            isSpawningBot = false;
        }

        // bot_add on a full server frees a slot by kicking the CSTV bot, which stops GOTV and the demo recording.
        private bool HasFreeSlotForBot()
        {
            var occupied = GetControllers(includeConnecting: true);
            if (!occupied.Any(p => p.IsHLTV)) return true; // Nothing to protect (and listen servers can report a wrong max).
            // Two slots per bot: one bot_add can bring an extra bot before it is kicked.
            int neededSlots = 2 * (pendingBotRequests.Count + 1);
            return occupied.Count + neededSlots <= Server.MaxPlayers;
        }

        private void StartNextBotRequest()
        {
            while (pendingBotRequests.Count > 0)
            {
                BotRequest request = pendingBotRequests.Dequeue();
                if (!isPractice || !IsPlayerValid(request.Owner)) continue;

                isSpawningBot = true;
                int trackedBots = GetTrackedBotCount();
                Server.ExecuteCommand("bot_quota_mode normal");
                Server.ExecuteCommand(request.Team == CsTeam.Terrorist ? "bot_add_t" : "bot_add_ct");
                // Set after bot_add (whether or not bot_add raises the quota itself), before the quota manager runs again.
                Server.ExecuteCommand($"bot_quota {trackedBots + 1}");
                Server.ExecuteCommand("bot_stop 1");
                Server.ExecuteCommand("bot_freeze 1");
                Server.ExecuteCommand("bot_zombie 1");
                int generation = botRequestGeneration;
                // Once bot is added, we teleport it to the requested position
                AddTimer(0.1f, () => ClaimBot(request, generation, 1));
                return;
            }
            isSpawningBot = false;
        }

        private void ClaimBot(BotRequest request, int generation, int attempt)
        {
            if (generation != botRequestGeneration) return; // Cancelled (.nobots, practice ended).

            bool retry = false;
            try
            {
                if (!isPractice) return;
                List<CCSPlayerController> untrackedBots = GetUntrackedBots();
                bool lastAttempt = attempt >= BotClaimAttempts;
                var newBots = untrackedBots
                    .Select(bot => new BotClaimLogic.NewBot(bot.UserId!.Value, bot.TeamNum, bot.PlayerPawn.IsValid && bot.PlayerPawn.Value != null))
                    .ToList();
                int? claimedUserId = BotClaimLogic.SelectBotToClaim(newBots, (int)request.Team, lastAttempt);
                CCSPlayerController? claimedBot = untrackedBots.FirstOrDefault(bot => bot.UserId == claimedUserId);

                if (claimedBot == null && !lastAttempt)
                {
                    // The requested bot may still be on its way: kick nothing yet.
                    retry = true;
                    AddTimer(0.15f, () => ClaimBot(request, generation, attempt + 1));
                    return;
                }

                ConVar? quota = ConVar.Find("bot_quota");
                Log($"[ClaimBot] Attempt {attempt}: {untrackedBots.Count} new bot(s), bot_quota {(quota != null ? GetConvarStringValue(quota) : "?")}, tracked {pracUsedBots.Count}");

                foreach (var extra in untrackedBots)
                {
                    if (extra == claimedBot) continue;
                    Log($"[ClaimBot] Kicking extra bot {extra.PlayerName} (team {extra.TeamNum}, wanted {(byte)request.Team})");
                    Server.ExecuteCommand($"kickid {extra.UserId!.Value}");
                }

                if (claimedBot == null)
                {
                    Log("[ClaimBot] No bot arrived for the .bot request");
                    PrintToAllChat(Localizer["matchzy.pm.botlimit"]);
                    return;
                }

                int botUserId = claimedBot.UserId!.Value;
                pracUsedBots[botUserId] = new Dictionary<string, object>
                {
                    ["controller"] = claimedBot,
                    ["position"] = request.Spot,
                    ["owner"] = request.Owner,
                    ["crouchstate"] = request.Crouch
                };

                if (request.Crouch) CrouchBotLater(botUserId);
                claimedBot.PlayerPawn.Value!.Teleport(request.Spot.PlayerPosition, request.Spot.PlayerAngle, new Vector(0, 0, 0));
                if (IsPlayerValid(request.Owner))
                {
                    TemporarilyDisableCollisions(request.Owner, claimedBot);
                    if (request.Boost) AddTimer(0.1f, () => ElevatePlayer(request.Owner));
                }
            }
            catch (Exception ex)
            {
                Log($"[ClaimBot - FATAL] Error: {ex.Message}");
            }
            finally
            {
                // The claim is decided (success, failure or error) unless a retry was scheduled.
                if (!retry && generation == botRequestGeneration)
                {
                    isSpawningBot = false;
                    if (isPractice)
                    {
                        Server.ExecuteCommand($"bot_quota {GetTrackedBotCount()}");
                        AddTimer(0.6f, EnforcePracticeBots);
                        StartNextBotRequest();
                    }
                    else
                    {
                        pendingBotRequests.Clear();
                    }
                }
            }
        }

        // Player controllers on the server (connected, or also connecting), including bots and the CSTV bot.
        private static List<CCSPlayerController> GetControllers(bool includeConnecting = false)
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller")
                .Where(p => p != null && p.IsValid
                    && (p.Connected == PlayerConnectedState.Connected || (includeConnecting && p.Connected == PlayerConnectedState.Connecting)))
                .ToList();
        }

        // Bots that are not tracked practice bots (and not the CSTV bot), also ones that have not spawned yet.
        private List<CCSPlayerController> GetUntrackedBots()
        {
            return GetControllers()
                .Where(bot => bot.IsBot && !bot.IsHLTV && bot.UserId.HasValue && !pracUsedBots.ContainsKey(bot.UserId.Value))
                .ToList();
        }

        // Tracked bots that are still on the server (stale entries are dropped, so the quota is never set too high).
        private int GetTrackedBotCount()
        {
            foreach (int userId in pracUsedBots.Keys.ToList())
            {
                CCSPlayerController? bot = Utilities.GetPlayerFromUserid(userId);
                if (bot == null || !bot.IsValid || !bot.IsBot) pracUsedBots.Remove(userId);
            }
            return pracUsedBots.Count;
        }

        // Keeps exactly the tracked practice bots: pins the quota first (so nothing refills), then kicks every other bot.
        private void EnforcePracticeBots()
        {
            try
            {
                if (!isPractice || isSpawningBot) return;
                Server.ExecuteCommand($"bot_quota {GetTrackedBotCount()}");
                foreach (var bot in GetUntrackedBots())
                {
                    Log($"[EnforcePracticeBots] Kicking untracked bot {bot.PlayerName}");
                    Server.ExecuteCommand($"kickid {bot.UserId!.Value}");
                }
            }
            catch (Exception ex)
            {
                Log($"[EnforcePracticeBots - FATAL] Error: {ex.Message}");
            }
        }

        // The bot is looked up again when each timer fires: it may have been kicked in the meantime, and writing to its old
        // pawn or movement services could crash the server.
        private void CrouchBotLater(int botUserId)
        {
            AddTimer(0.1f, () =>
            {
                CCSPlayerController? bot = Utilities.GetPlayerFromUserid(botUserId);
                if (!IsPlayerValid(bot) || bot!.PlayerPawn.Value!.MovementServices == null) return;
                new CCSPlayer_MovementServices(bot.PlayerPawn.Value.MovementServices.Handle).DuckAmount = 1;
            });
            AddTimer(0.2f, () =>
            {
                CCSPlayerController? bot = Utilities.GetPlayerFromUserid(botUserId);
                if (!IsPlayerValid(bot) || bot!.PlayerPawn.Value!.Bot == null) return;
                bot.PlayerPawn.Value.Bot.IsCrouching = true;
            });
        }

        public void TemporarilyDisableCollisions(CCSPlayerController p1, CCSPlayerController p2)
        {
            Log($"[TemporarilyDisableCollisions] Disabling {p1.PlayerName} {p2.PlayerName}");
            // Reference collision code: https://github.com/Source2ZE/CS2Fixes/blob/f009e399ff23a81915e5a2b2afda20da2ba93ada/src/events.cpp#L150
            p1.PlayerPawn.Value!.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DEBRIS;
            p1.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DEBRIS;
            p2.PlayerPawn.Value!.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DEBRIS;
            p2.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DEBRIS;
            // TODO: call CollisionRulesChanged
            var p1p = p1.PlayerPawn;
            var p2p = p2.PlayerPawn;
            collisionGroupTimer?.Kill();
            collisionGroupTimer = AddTimer(0.1f, () =>
            {
                if (!p1p.IsValid || !p2p.IsValid || !p1p.Value.IsValid || !p2p.Value.IsValid)
                {
                    Log($"player handle invalid p1p {p1p.Value.IsValid} p2p {p2p.Value.IsValid}");
                    collisionGroupTimer?.Kill();
                    return;
                }

                if (!DoPlayersCollide(p1p.Value, p2p.Value))
                {
                    // Once they no longer collide 
                    p1p.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER_MOVEMENT;
                    p1p.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER_MOVEMENT;
                    p2p.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER_MOVEMENT;
                    p2p.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER_MOVEMENT;
                    // TODO: call CollisionRulesChanged
                    collisionGroupTimer?.Kill();
                }

            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        public bool DoPlayersCollide(CCSPlayerPawn p1, CCSPlayerPawn p2)
        {
            Vector p1min, p1max, p2min, p2max;
            var p1pos = p1.AbsOrigin;
            var p2pos = p2.AbsOrigin;
            p1min = p1.Collision.Mins + p1pos!;
            p1max = p1.Collision.Maxs + p1pos!;
            p2min = p2.Collision.Mins + p2pos!;
            p2max = p2.Collision.Maxs + p2pos!;

            return p1min.X <= p2max.X && p1max.X >= p2min.X &&
                    p1min.Y <= p2max.Y && p1max.Y >= p2min.Y &&
                    p1min.Z <= p2max.Z && p1max.Z >= p2min.Z;
        }

        private static void ElevatePlayer(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null) return;
            player.PlayerPawn.Value.Teleport(new Vector(player.PlayerPawn.Value.CBodyComponent!.SceneNode!.AbsOrigin.X, player.PlayerPawn.Value.CBodyComponent!.SceneNode!.AbsOrigin.Y, player.PlayerPawn.Value.CBodyComponent!.SceneNode!.AbsOrigin.Z + 80.0f), player.PlayerPawn.Value.EyeAngles, new Vector(0, 0, 0));
        }

        [GameEventHandler]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (!IsPlayerValid(player)) return HookResult.Continue;

            // disable noclip on spawn -- all no clipping functionality is handled by the plugin!
            // Movement adjustments are consistent with cs2-noclip.
            CBasePlayerPawn pawn = player!.PlayerPawn.Value!;
            if (pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP) {
                pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                pawn.ActualMoveType = MoveType_t.MOVETYPE_WALK;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
            }

            if (matchStarted && (matchzyTeam1.coach.Contains(player!) || matchzyTeam2.coach.Contains(player!)))
            {
                player!.InGameMoneyServices!.Account = 0;

                Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
                pawn.MoveType = MoveType_t.MOVETYPE_NONE;
                pawn.ActualMoveType = MoveType_t.MOVETYPE_NONE;
                
                return HookResult.Continue;
            }

            // Respawing a bot where it was actually spawned during practice session
            if (isPractice && player!.IsValid && player.IsBot && player.UserId.HasValue)
            {
                if (pracUsedBots.ContainsKey(player.UserId.Value))
                {
                    if (pracUsedBots[player.UserId.Value]["position"] is Position botPosition)
                    {
                        player.PlayerPawn.Value?.Teleport(botPosition.PlayerPosition, botPosition.PlayerAngle, new Vector(0, 0, 0));
                        bool isCrouched = (bool)pracUsedBots[player.UserId.Value]["crouchstate"];
                        if (isCrouched)
                        {
                            player.PlayerPawn.Value!.Flags |= (uint)PlayerFlags.FL_DUCKING;
                            CrouchBotLater(player.UserId.Value);
                        }
                        CCSPlayerController? botOwner = (CCSPlayerController)pracUsedBots[player.UserId.Value]["owner"];
                        if (botOwner != null && botOwner.IsValid && botOwner.PlayerPawn != null && botOwner.PlayerPawn.IsValid) {
                            // Either player may leave before the timer fires.
                            AddTimer(0.2f, () =>
                            {
                                if (IsPlayerValid(botOwner) && IsPlayerValid(player)) TemporarilyDisableCollisions(botOwner, player);
                            });
                        }
                    }
                }
                else if (!isSpawningBot && !player.IsHLTV)
                {
                    // Bot has been spawned, but we didn't spawn it, so kick it (e.g. a bot_quota refill).
                    // Extra bots from bot_add are already handled in ClaimBot
                    // Delay this for a few seconds to prevent crashes
                    Log($"Kicking bot {player.PlayerName} due to erroneous spawning");
                    // Bots claimed by a .bot in the meantime are kept; kicks are by user id.
                    AddTimer(2.5f, EnforcePracticeBots);
                }
            }

            return HookResult.Continue;
        }

        [ConsoleCommand("css_nobots", "Removes bots from the practice session")]
        public void OnNoBotsCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || player == null) return;
            CancelBotRequests();
            KickBots();
            pracUsedBots = new Dictionary<int, Dictionary<string, object>>();
        }

        [ConsoleCommand("css_ff", "Fast forwards the timescale to 20 seconds")]
        public void OnFFCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || player == null) return;
            if (fastForwardTimer != null)
            {
                // A second .ff would save the frozen move types of the first one and never unfreeze the players.
                PrintToPlayerChat(player, "Already fastforwarding!");
                return;
            }

            preFastForwardMoveTypes = new();

            foreach (var key in playerData.Keys) {
                if(!IsPlayerValid(playerData[key])) continue;
                preFastForwardMoveTypes[key] = playerData[key].PlayerPawn.Value!.MoveType;
                playerData[key].PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_NONE;
            }

            Server.PrintToChatAll($"{chatPrefix} Fastforwarding 20 seconds!");
            Server.ExecuteCommand("host_timescale 10");
            fastForwardTimer = AddTimer(20.0f, ResetFastForward);

        }

        [ConsoleCommand("css_fastforward", "Fast forwards the timescale to 20 seconds")]
        public void OnFastForwardCommand(CCSPlayerController? player, CommandInfo? command)
        {
            OnFFCommand(player, command);
        }

        // Always restores the timescale, also when practice has ended in the meantime.
        public void ResetFastForward() {
            fastForwardTimer = null;
            Server.ExecuteCommand("host_timescale 1");
            foreach (var (key, moveType) in preFastForwardMoveTypes) {
                if (!playerData.TryGetValue(key, out var ffPlayer) || !IsPlayerValid(ffPlayer)) continue;
                ffPlayer.PlayerPawn.Value!.MoveType = moveType;
            }
            preFastForwardMoveTypes = new();
        }

        // Ends a running .ff and .timer; called when practice mode ends.
        public void StopPracticeTimers()
        {
            if (fastForwardTimer != null)
            {
                fastForwardTimer.Kill();
                ResetFastForward();
            }
            foreach (var timer in playerTimers.Values)
            {
                timer.KillTimer();
            }
            playerTimers.Clear();
        }

        [ConsoleCommand("css_clear", "Removes all the available granades")]
        public void OnClearCommand(CCSPlayerController? player, CommandInfo? command)
        {
            RemoveGrenadeEntities();
        }

        [ConsoleCommand("css_spec", "Switches team to Spectator")]
        public void OnSpecCommand(CCSPlayerController? player, CommandInfo? command) {
            if (!isPractice || player == null) return;

            SideSwitchCommand(player, CsTeam.Spectator);
        }

        [ConsoleCommand("css_fas", "Switches all other players to spectator")]
        [ConsoleCommand("css_watchme", "Switches all other players to spectator")]
        public void OnFASCommand(CCSPlayerController? player, CommandInfo? command) {
            if (!isPractice || player == null) return;

            SideSwitchCommand(player, CsTeam.None);
        }

        [ConsoleCommand("css_noblind", "Disables flash effect for the player")]
        [ConsoleCommand("css_noflash", "Disables flash effect for the player")]
        public void OnNoFlashCommand(CCSPlayerController? player, CommandInfo? command) {
            if (!isPractice || player == null || player.UserId == null) return;

            int userId = player.UserId.Value;

            if (noFlashList.Contains(userId))
            {
                noFlashList.Remove(userId);
                ReplyToUserCommand(player, "Disabled noflash.");
            } else {
                noFlashList.Add(userId);
                ReplyToUserCommand(player, "Enabled noflash. Use .noflash again to disable.");
                Server.NextFrame(() => KillFlashEffect(player));
            }

        }

        [ConsoleCommand("css_break", "Breaks the breakable entities")]
        public void OnBreakCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice) return;
            var entities = Utilities.FindAllEntitiesByDesignerName<CBreakable>("prop_dynamic")
                .Concat(Utilities.FindAllEntitiesByDesignerName<CBreakable>("func_breakable"));
            foreach (var entity in entities)
            {
                entity.AcceptInput("Break");
            }
        }

        public void KillFlashEffect(CCSPlayerController player) {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null) return;
            Log($"[KillFlashEffect] Killing flash effect for player: {player.PlayerName}");
            playerPawn.FlashMaxAlpha = 0.5f;
        }

        // CsTeam.None is a special value to mean force all other players to spectator
        private void SideSwitchCommand(CCSPlayerController player, CsTeam team) {
          if (team > CsTeam.None) {
            if(player.TeamNum == (byte)CsTeam.Spectator && team != CsTeam.Spectator) {
              // ReplyToUserCommand(player, "Switching to a team from spectator is currently broken, use the team menu.");
              ReplyToUserCommand(player, Localizer["matchzy.pm.spectatorbroken"]);
              return;
            }
            MovePlayerToTeamSafely(player, team);
            return;
          }
          Utilities.GetPlayers().ForEach((x) => {
              if(x.IsValid && !x.IsBot && x.UserId != player.UserId) {
                MovePlayerToTeamSafely(x, CsTeam.Spectator);
              }
            });
        }

        // Registered as a post handler, so the engine has already counted the death: take it back (and the point a suicide costs).
        public HookResult OnPracticeSwitchDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var victim = @event.Userid;
            if (victim == null || !victim.IsValid || !victim.UserId.HasValue) return HookResult.Continue;
            if (!practiceSwitchNoDeath.Remove(victim.UserId.Value, out var before)) return HookResult.Continue;
            var matchStats = victim.ActionTrackingServices?.MatchStats;
            if (matchStats != null) matchStats.Deaths = before.Deaths;
            victim.Score = before.Score;
            Utilities.SetStateChanged(victim, "CCSPlayerController", "m_iScore");
            Utilities.SetStateChanged(victim, "CCSPlayerController", "m_pActionTrackingServices");
            return HookResult.Continue;
        }

        // Changing the team of a live player makes the engine strip its weapons inline, and weapon hooks of other plugins can
        // crash the server there. So the player is killed first (a death only drops the weapons) and moved on the next frame.
        // Runs outside the chat/console command that triggered it.
        private void MovePlayerToTeamSafely(CCSPlayerController player, CsTeam team)
        {
            Server.NextFrame(() =>
            {
                if (player == null || !player.IsValid) return;
                bool playTeam = team == CsTeam.Terrorist || team == CsTeam.CounterTerrorist;
                if (player.TeamNum == (byte)team)
                {
                    // Already on that team: nothing to switch, just bring a dead player back.
                    if (playTeam && !player.PawnIsAlive) player.Respawn();
                    return;
                }

                CCSPlayerPawn? pawn = player.PlayerPawn.Value;
                if (pawn != null && pawn.IsValid && player.PawnIsAlive)
                {
                    // Only flagged when a suicide really happens; OnPracticeSwitchDeath restores the scoreboard.
                    if (player.UserId.HasValue)
                    {
                        int userId = player.UserId.Value;
                        practiceSwitchNoDeath[userId] = (player.Score, player.ActionTrackingServices?.MatchStats?.Deaths ?? 0);
                        // If no death event comes for this kill, the flag must not swallow a later real death.
                        AddTimer(1.0f, () => practiceSwitchNoDeath.Remove(userId));
                    }
                    pawn.CommitSuicide(false, true);
                }

                Server.NextFrame(() =>
                {
                    if (player == null || !player.IsValid) return;
                    if (!playTeam)
                    {
                        player.ChangeTeam(team);
                        return;
                    }
                    player.SwitchTeam(team);
                    player.Respawn();
                });
            });
        }

        // savednades.json only exists once a lineup has been saved.
        private static string ReadSavedNadesJson(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : "{}";
        }

        public void RemoveGrenadeEntities()
        {
            if (!isPractice) return;
            var smokes = Utilities.FindAllEntitiesByDesignerName<CSmokeGrenadeProjectile>("smokegrenade_projectile");
            foreach (var entity in smokes)
            {
                entity?.Remove();
            }
            molotovThrows.Clear();
            var mollys = Utilities.FindAllEntitiesByDesignerName<CSmokeGrenadeProjectile>("molotov_projectile");
            foreach (var entity in mollys)
            {
                entity?.Remove();
            }
            var inferno = Utilities.FindAllEntitiesByDesignerName<CSmokeGrenadeProjectile>("inferno");
            foreach (var entity in inferno)
            {
                entity?.Remove();
            }
        }

        public void ExecDryRunCFG()
        {
            var absolutePath = Path.Join(Server.GameDirectory + "/csgo/cfg", dryrunCfgPath);
    
            // We try to find the CFG in the cfg folder, if it is not there then we execute the default CFG.
            if (File.Exists(absolutePath)) {
                Log($"[ExecDryRunCFG] Starting Dryrun! Executing Dryrun CFG from {dryrunCfgPath}");
                Server.ExecuteCommand($"exec {dryrunCfgPath}");
                ExecOverrideCfg(dryrunCfgPath);
                Server.ExecuteCommand("mp_restartgame 1;mp_warmup_end;");
            } else {
                Log($"[ExecDryRunCFG] Starting Dryrun! Dryrun CFG not found in {absolutePath}, using default CFG!");
                Server.ExecuteCommand("ammo_grenade_limit_default 1;ammo_grenade_limit_flashbang 2;ammo_grenade_limit_total 4;bot_quota 0;cash_player_bomb_defused 300;cash_player_bomb_planted 300;cash_player_damage_hostage -30;cash_player_interact_with_hostage 300;cash_player_killed_enemy_default 300;cash_player_killed_enemy_factor 1;cash_player_killed_hostage -1000;cash_player_killed_teammate -300;cash_player_rescued_hostage 1000;cash_team_elimination_bomb_map 3250;cash_team_elimination_hostage_map_ct 3000;cash_team_elimination_hostage_map_t 3000;cash_team_hostage_alive 0;cash_team_hostage_interaction 600;cash_team_loser_bonus 1400;cash_team_loser_bonus_consecutive_rounds 500;cash_team_planted_bomb_but_defused 600;cash_team_rescued_hostage 600;cash_team_terrorist_win_bomb 3500;cash_team_win_by_defusing_bomb 3500;");
                Server.ExecuteCommand("cash_team_win_by_hostage_rescue 2900;cash_team_win_by_time_running_out_bomb 3250;cash_team_win_by_time_running_out_hostage 3250;ff_damage_reduction_bullets 0.33;ff_damage_reduction_grenade 0.85;ff_damage_reduction_grenade_self 1;ff_damage_reduction_other 0.4;mp_afterroundmoney 0;mp_autokick 0;mp_autoteambalance 0;mp_backup_restore_load_autopause 1;mp_backup_round_auto 1;mp_buy_anywhere 0;mp_buy_during_immunity 0;mp_buytime 20;mp_c4timer 40;mp_ct_default_melee weapon_knife;mp_ct_default_primary \"\";mp_ct_default_secondary weapon_hkp2000;mp_death_drop_defuser 1;mp_death_drop_grenade 2;mp_death_drop_gun 1;mp_defuser_allocation 0;mp_display_kill_assists 1;mp_endmatch_votenextmap 0;mp_forcecamera 1;mp_free_armor 0;mp_freezetime 6;mp_friendlyfire 1;mp_give_player_c4 1;mp_halftime 1;mp_halftime_duration 15;mp_halftime_pausetimer 0;mp_ignore_round_win_conditions 0;mp_limitteams 0;mp_match_can_clinch 1;mp_match_end_restart 0;mp_maxmoney 16000;mp_maxrounds 24;mp_overtime_enable 1;mp_overtime_halftime_pausetimer 0;mp_overtime_maxrounds 6;mp_overtime_startmoney 10000;mp_playercashawards 1;mp_randomspawn 0;mp_respawn_immunitytime 0;mp_respawn_on_death_ct 0;mp_respawn_on_death_t 0;mp_round_restart_delay 5;mp_roundtime 1.92;mp_roundtime_defuse 1.92;mp_roundtime_hostage 1.92;mp_solid_teammates 1;mp_starting_losses 1;mp_startmoney 16000;mp_t_default_melee weapon_knife;mp_t_default_primary \"\";mp_t_default_secondary weapon_glock;mp_teamcashawards 1;mp_timelimit 0;mp_weapons_allow_map_placed 1;mp_weapons_allow_zeus 1;mp_win_panel_display_time 3;spec_freeze_deathanim_time 0;spec_freeze_time 2;spec_freeze_time_lock 2;spec_replay_enable 0;sv_allow_votes 1;sv_auto_full_alltalk_during_warmup_half_end 0;sv_damage_print_enable 0;sv_deadtalk 1;sv_hibernate_postgame_delay 300;sv_ignoregrenaderadio 0;sv_infinite_ammo 0;sv_talk_enemy_dead 0;sv_talk_enemy_living 0;sv_voiceenable 1;tv_relayvoice 1;mp_team_timeout_max 3;mp_team_timeout_ot_max 1;mp_team_timeout_ot_add_each 1;mp_team_timeout_time 30;sv_vote_command_delay 0;cash_team_bonus_shorthanded 0;mp_spectators_max 20;mp_team_intro_time 0;mp_restartgame 3;mp_warmup_end;");
                ExecOverrideCfg(dryrunCfgPath);
            }
        }

        public void ExecUnpracCommands() {
            StopPracticeTimers();
            practiceSwitchNoDeath.Clear();
            CancelBotRequests();
            Server.ExecuteCommand("sv_cheats false;sv_grenade_trajectory_prac_pipreview false;sv_grenade_trajectory_prac_trailtime 0; mp_ct_default_grenades \"\"; mp_ct_default_primary \"\"; mp_t_default_grenades\"\"; mp_t_default_primary\"\"; mp_teammates_are_enemies false;");
            Server.ExecuteCommand("mp_death_drop_breachcharge true; mp_death_drop_defuser true; mp_death_drop_taser true; mp_drop_knife_enable false; mp_death_drop_grenade 2; ammo_grenade_limit_total 4; mp_defuser_allocation 0; sv_infinite_ammo 0; mp_force_pick_time 15");
        }

        public bool IsValidPositionForLastGrenade(CCSPlayerController player, int position)
        {
            int userId = player.UserId!.Value;
            if (!lastGrenadesData.ContainsKey(userId) || lastGrenadesData[userId].Count <= 0)
            {
                // PrintToPlayerChat(player, $"You have not thrown any nade yet!");
                PrintToPlayerChat(player, Localizer["matchzy.pm.nothrownnades"]);
                return false;
            }

            if (lastGrenadesData[userId].Count < position)
            {
                // PrintToPlayerChat(player, $"Your grenade history only goes from 1 to {lastGrenadesData[userId].Count}!");
                PrintToPlayerChat(player, Localizer["matchzy.pm.grenadehistory", $"{lastGrenadesData[userId].Count}"]);
                return false;
            }

            return true;
        }

        public void RethrowSpecificNade(CCSPlayerController player, string nadeType)
        {
            if (!isPractice || !player.UserId.HasValue) return;
            int userId = player.UserId.Value;
            if (!nadeSpecificLastGrenadeData.ContainsKey(userId) || !nadeSpecificLastGrenadeData[userId].ContainsKey(nadeType))
            {
                // PrintToPlayerChat(player, $"You have not thrown any {nadeType} yet!");
                PrintToPlayerChat(player, Localizer["matchzy.pm.nothrownnadestype", nadeType]);
                return;
            }
            GrenadeThrownData grenadeThrown = nadeSpecificLastGrenadeData[userId][nadeType];
            AddTimer(grenadeThrown.Delay, () => grenadeThrown.Throw(player));
        }

        public void HandleBackCommand(CCSPlayerController player, string number)
        {
            if (!isPractice || player == null || !player.UserId.HasValue) return;
            int userId = player.UserId.Value;
            if (!string.IsNullOrWhiteSpace(number))
            {
                if (int.TryParse(number, out int positionNumber) && positionNumber >= 1)
                {
                    if (IsValidPositionForLastGrenade(player, positionNumber))
                    {
                        positionNumber -= 1;
                        if (lastGrenadesData[userId][positionNumber].LoadPosition(player))
                        {
                            // PrintToPlayerChat(player, $"Teleported to grenade of history position: {positionNumber+1}/{lastGrenadesData[userId].Count}");
                            PrintToPlayerChat(player, Localizer["matchzy.pm.tptogrenade", $"{positionNumber + 1}/{lastGrenadesData[userId].Count}"]);
                        }
                    }
                }
                else
                {
                    // PrintToPlayerChat(player, $"Invalid value for !back command. Please specify a valid non-negative number. Usage: !back <number>");
                    PrintToPlayerChat(player, Localizer["matchzy.pm.backinvalidvalue"]);
                    return;
                }
            }
            else
            {
                int thrownCount = lastGrenadesData.ContainsKey(userId) ? lastGrenadesData[userId].Count : 0;
                // ReplyToUserCommand(player, $"Usage: !back <number> (You've thrown {thrownCount} grenades till now)");
                ReplyToUserCommand(player, Localizer["matchzy.pm.backtonumber", thrownCount]);
            }
        }

        public void HandleThrowIndexCommand(CCSPlayerController player, string argString)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            int userId = player!.UserId!.Value;

            if (string.IsNullOrEmpty(argString))
            {
                int thrownCount = lastGrenadesData.ContainsKey(userId) ? lastGrenadesData[userId].Count : 0;
                // ReplyToUserCommand(player, $"Usage: !throwindex <number> (You've thrown {thrownCount} grenades till now)");
                ReplyToUserCommand(player, Localizer["matchzy.pm.throwindextonumber", thrownCount]);
                return;
            }

            string[] argsList = argString.Split();

            foreach (string arg in argsList)
            {
                if (int.TryParse(arg, out int positionNumber) && positionNumber >= 1)
                {
                    if (IsValidPositionForLastGrenade(player, positionNumber))
                    {
                        positionNumber -= 1;
                        GrenadeThrownData grenadeThrown = lastGrenadesData[userId][positionNumber];
                        AddTimer(grenadeThrown.Delay, () => grenadeThrown.Throw(player));
                        // PrintToPlayerChat(player, $"Throwing grenade of history position: {positionNumber+1}/{lastGrenadesData[userId].Count}");
                        PrintToPlayerChat(player, Localizer["matchzy.pm.throwgrenadehistory", $"{positionNumber + 1}/{lastGrenadesData[userId].Count}"]);
                    }
                }
                else
                {
                    // PrintToPlayerChat(player, $"'{arg}' is not a valid non-negative number for !throwindex command.");
                    PrintToPlayerChat(player, Localizer["matchzy.pm.backnegativenumber", arg]);
                }
            }
        }

        public void HandleDelayCommand(CCSPlayerController player, string delay)
        {
            if (!isPractice || !IsPlayerValid(player)) return;

            if (!isPractice || player == null || !player.UserId.HasValue) return;
            int userId = player.UserId.Value;
            if (string.IsNullOrWhiteSpace(delay))
            {
                // ReplyToUserCommand(player, $"Usage: !delay <delay_in_seconds>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", $"!delay <delay_in_seconds>"]);
                return;
            }
            
            if (float.TryParse(delay, out float delayInSeconds) && delayInSeconds >= 0)
            {
                if (IsValidPositionForLastGrenade(player, 0))
                {
                    lastGrenadesData[userId].Last().Delay = delayInSeconds;
                    // PrintToPlayerChat(player, $"Delay of {delayInSeconds:0.00}s set for grenade of index: {lastGrenadesData[userId].Count}.");
                    PrintToPlayerChat(player, Localizer["matchzy.pm.delaygrenade", $"{delayInSeconds:0.00}", $"{lastGrenadesData[userId].Count}"]);
                }
            }
            else
            {
                // PrintToPlayerChat(player, $"Delay of {delayInSeconds:0.00}s set for grenade of index: {lastGrenadesData[userId].Count}.);
                int thrownCount = lastGrenadesData.TryGetValue(userId, out var thrownGrenades) ? thrownGrenades.Count : 0;
                PrintToPlayerChat(player, Localizer["matchzy.pm.delayvalidnumber", $"{delayInSeconds:0.00}", $"{thrownCount}"]);
                return;
            }
        }

        public void DisplayPracticeTimerCenter(int userId)
        {
            if (!playerData.ContainsKey(userId) || !playerTimers.ContainsKey(userId)) return;
            if (!IsPlayerValid(playerData[userId])) return;
            playerTimers[userId].DisplayTimerCenter(playerData[userId]);
        }

        [ConsoleCommand("css_throw", "Throws the last thrown grenade")]
        [ConsoleCommand("css_rethrow", "Throws the last thrown grenade")]
        public void OnRethrowCommand(CCSPlayerController? player, CommandInfo? command)
        {

            if (!isPractice || player == null || !player.UserId.HasValue) return;
            int userId = player.UserId.Value;
            if (!lastGrenadesData.ContainsKey(userId) || lastGrenadesData[userId].Count <= 0)
            {
                // PrintToPlayerChat(player, $"You have not thrown any nade yet!");
                PrintToPlayerChat(player, Localizer["matchzy.pm.notthrownnade"]);
                return;
            }
            GrenadeThrownData lastGrenade = lastGrenadesData[userId].Last();
            AddTimer(lastGrenade.Delay, () => lastGrenade.Throw(player));
        }

        [ConsoleCommand("css_savepos", "Saves the player location")]
        public void OnSavePosCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || player == null || !player.UserId.HasValue || player.PlayerPawn.Value == null) return;
            
            int userId = player.UserId.Value;
            var pawn = player.PlayerPawn.Value;
            Vector position = new(pawn.AbsOrigin?.X, pawn.AbsOrigin?.Y, pawn.AbsOrigin?.Z);
            QAngle angle = new(pawn.EyeAngles?.X, pawn.EyeAngles?.Y, pawn.EyeAngles?.Z);
            
            savedPlayerLocationData[userId] = new PlayerLocationData(position, angle);
            Log($"[SavePos] Saved position for UserID {userId}, Position: {position}, Angle: {angle}!");
            PrintToPlayerChat(player, Localizer["matchzy.pm.savepos"]);
        }

        [ConsoleCommand("css_loadpos", "Loads the last saved player location")]
        public void OnLoadPosCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || player == null || !player.UserId.HasValue) return;
            
            int userId = player.UserId.Value;
            if (!savedPlayerLocationData.TryGetValue(userId, out var playerLocationData))
            {
                PrintToPlayerChat(player, Localizer["matchzy.pm.notsavedpos"]);
                return;
            }
            
            Log($"[LoadPos] LoadPos position for UserID {userId}, Position: {playerLocationData.Position}, Angles: {playerLocationData.Angle}!");
            playerLocationData.LoadPosition(player);
            PrintToPlayerChat(player, Localizer["matchzy.pm.loadpos"]);
        }

        [ConsoleCommand("css_throwsmoke", "Throws the last thrown smoke")]
        [ConsoleCommand("css_rethrowsmoke", "Throws the last thrown smoke")]
        public void OnRethrowSmokeCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;
            RethrowSpecificNade(player, "smoke");
        }

        [ConsoleCommand("css_throwflash", "Throws the last thrown flash")]
        [ConsoleCommand("css_rethrowflash", "Throws the last thrown flash")]
        public void OnRethrowFlashCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;
            RethrowSpecificNade(player, "flash");
        }

        [ConsoleCommand("css_throwgrenade", "Throws the last thrown he grenade")]
        [ConsoleCommand("css_rethrowgrenade", "Throws the last thrown he grenade")]
        [ConsoleCommand("css_thrownade", "Throws the last thrown he grenade")]
        [ConsoleCommand("css_rethrownade", "Throws the last thrown he grenade")]
        public void OnRethrowGrenadeCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;
            RethrowSpecificNade(player, "hegrenade");
        }

        [ConsoleCommand("css_throwmolotov", "Throws the last thrown molotov")]
        [ConsoleCommand("css_rethrowmolotov", "Throws the last thrown molotov")]
        public void OnRethrowMolotovCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;
            RethrowSpecificNade(player, "molotov");
        }

        [ConsoleCommand("css_throwdecoy", "Throws the last thrown decoy")]
        [ConsoleCommand("css_rethrowdecoy", "Throws the last thrown decoy")]
        public void OnRethrowDecoyCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;
            RethrowSpecificNade(player, "decoy");
        }

        [ConsoleCommand("css_last", "Teleports to the last thrown grenade position")]
        public void OnLastCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || player == null || !player.UserId.HasValue) return;
            int userId = player.UserId.Value;
            if (!lastGrenadesData.ContainsKey(userId) || lastGrenadesData[userId].Count <= 0)
            {
                // PrintToPlayerChat(player, $"You have not thrown any nade yet!");
                PrintToPlayerChat(player, Localizer["matchzy.pm.notthrownnade"]);
                return;
            }
            lastGrenadesData[userId].Last().LoadPosition(player);
        }

        [ConsoleCommand("css_back", "Teleports to the provided position in grenade thrown history")]
        public void OnBackCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice || player == null || !player.UserId.HasValue) return;
            if (command.ArgCount >= 2) 
            {
                string commandArg = command.ArgByIndex(1);
                HandleBackCommand(player, commandArg);
            }
            else 
            {
                int userId = player!.UserId!.Value;
                int thrownCount = lastGrenadesData.ContainsKey(userId) ? lastGrenadesData[userId].Count : 0;
                // ReplyToUserCommand(player, $"Usage: !back <number> (You've thrown {thrownCount} grenades till now)");
                ReplyToUserCommand(player, Localizer["matchzy.pm.backtonumber", thrownCount]);
            }      
        }

        [ConsoleCommand("css_throwidx", "Throws grenade of provided position in grenade thrown history")]
        [ConsoleCommand("css_throwindex", "Throws grenade of provided position in grenade thrown history")]
        public void OnThrowIndexCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            if (command.ArgCount >= 2) 
            {
                HandleThrowIndexCommand(player!, command.ArgString);
            }
            else 
            {
                int userId = player!.UserId!.Value;
                int thrownCount = lastGrenadesData.ContainsKey(userId) ? lastGrenadesData[userId].Count : 0;
                // ReplyToUserCommand(player, $"Usage: !throwindex <number> (You've thrown {thrownCount} grenades till now)");
                ReplyToUserCommand(player, Localizer["matchzy.pm.throwindextonumber", thrownCount]);
            }      
        }

        [ConsoleCommand("css_lastindex", "Returns index of the last thrown grenade")]
        public void OnLastIndexCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            if (IsValidPositionForLastGrenade(player!, 1))
            {
                // PrintToPlayerChat(player!, $"Index of last thrown grenade: {lastGrenadesData[player!.UserId!.Value].Count}");
                PrintToPlayerChat(player!, Localizer["matchzy.pm.indexlastgrenade", $"{lastGrenadesData[player!.UserId!.Value].Count}"]);
            } 
        }

        [ConsoleCommand("css_delay", "Adds a delay to the last thrown grenade. Usage: !delay <delay_in_seconds>")]
        public void OnDelayCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            if (command.ArgCount >= 2) 
            {
                HandleDelayCommand(player!, command.ArgByIndex(1));
            }
            else 
            {
                // ReplyToUserCommand(player, $"Usage: !delay <delay_in_seconds>");
                ReplyToUserCommand(player, Localizer["matchzy.cc.usage", $"!delay <delay_in_seconds>"]);
            }      
        }

        [ConsoleCommand("css_timer", "Starts a timer, use .timer again to stop it.")]
        public void OnTimerCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            int userId = player!.UserId!.Value;
            if (playerTimers.ContainsKey(userId))
            {
                playerTimers[userId].KillTimer();
                double timerResult = playerTimers[userId].GetTimerResult();
                player.PrintToCenter($"Timer: {timerResult}s");
                PrintToPlayerChat(player, $"Timer stopped! Result: {timerResult}s");
                playerTimers.Remove(userId);
            }
            else
            {
                playerTimers[userId] = new PlayerPracticeTimer(PracticeTimerType.Immediate)
                {
                    StartTime = DateTime.Now,
                    Timer = AddTimer(0.1f, () => DisplayPracticeTimerCenter(userId), TimerFlags.REPEAT)
                };
                PrintToPlayerChat(player, $"Timer started! User !timer to stop it.");
            }
        }

        [ConsoleCommand("css_sn", "Saves current nade position")]
        [ConsoleCommand("css_savenade", "Saves current nade position")]
        public void OnSaveNadeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;

            HandleSaveNadeCommand(player, command.ArgString);
        }

        [ConsoleCommand("css_ln", "Loades the nade with provided filter")]
        [ConsoleCommand("css_loadnade", "Loades the nade with provided filter")]
        public void OnLoadNadeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;

            HandleLoadNadeCommand(player, command.ArgString);
        }

        [ConsoleCommand("css_lin", "Lists the nade with provided filter")]
        [ConsoleCommand("css_listnades", "Lists the nade with provided filter")]
        public void OnListNadesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;

            HandleListNadesCommand(player, command.ArgString);
        }

        [ConsoleCommand("css_importnade", "Imports the nade with the given code")]
        [ConsoleCommand("css_in", "Imports the nade with the given code")]
        public void OnImportNadeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;

            HandleImportNadeCommand(player, command.ArgString);
        }

        [ConsoleCommand("css_deletenade", "Deletes the nade by name")]
        [ConsoleCommand("css_delnade", "Deletes the nade by name")]
        [ConsoleCommand("css_dn", "Deletes the nade by name")]
        public void OnDeleteNadeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;

            HandleDeleteNadeCommand(player, command.ArgString);
        }

        [ConsoleCommand("css_solid", "Toggles mp_solid_teammates in practice mode")]
        public void OnSolidCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;

            int solidValue = ConVar.Find("mp_solid_teammates")!.GetPrimitiveValue<int>();

            int newSolidValue = (solidValue == 0 || solidValue == 1) ? 2 : 1;

            ConVar.Find("mp_solid_teammates")!.SetValue(newSolidValue);

            PrintToAllChat($"mp_solid_teammates is now set to {newSolidValue}");
        }

        [ConsoleCommand("css_impacts", "Toggles sv_showimpacts in practice mode")]
        public void OnImpactsCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;

            int impactValue = ConVar.Find("sv_showimpacts")!.GetPrimitiveValue<int>();

            int newImpactValue = 1 - impactValue;

            Server.ExecuteCommand($"sv_showimpacts {newImpactValue}");

            PrintToAllChat($"sv_showimpacts is now set to {newImpactValue}");
        }

        [ConsoleCommand("css_traj", "Toggles sv_grenade_trajectory_prac_pipreview in practice mode")]
        [ConsoleCommand("css_pip", "Toggles sv_grenade_trajectory_prac_pipreview in practice mode")]
        public void OnTrajCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;

            bool trajValue = ConVar.Find("sv_grenade_trajectory_prac_pipreview")!.GetPrimitiveValue<bool>();

            Server.ExecuteCommand($"sv_grenade_trajectory_prac_pipreview {!trajValue}");

            PrintToAllChat($"sv_grenade_trajectory_prac_pipreview is now set to {!trajValue}");
        }

        [ConsoleCommand("css_bestspawn", "Teleports you to your team's closest spawn from your current position")]
        public void OnBestSpawnCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            TeleportPlayerToBestSpawn(player!, player!.TeamNum);
        }

        [ConsoleCommand("css_worstspawn", "Teleports you to your team's furthest spawn from your current position")]
        public void OnWorstSpawnCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            TeleportPlayerToWorstSpawn(player!, player!.TeamNum);
        }

        [ConsoleCommand("css_bestctspawn", "Teleports you to CT team's closest spawn from your current position")]
        public void OnBestCTSpawnCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            TeleportPlayerToBestSpawn(player!, (byte)CsTeam.CounterTerrorist);
        }

        [ConsoleCommand("css_worstctspawn", "Teleports you to CT team's furthest spawn from your current position")]
        public void OnWorstCTSpawnCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            TeleportPlayerToWorstSpawn(player!, (byte)CsTeam.CounterTerrorist);
        }

        [ConsoleCommand("css_besttspawn", "Teleports you to T team's closest spawn from your current position")]
        public void OnBestTSpawnCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            TeleportPlayerToBestSpawn(player!, (byte)CsTeam.Terrorist);
        }

        [ConsoleCommand("css_worsttspawn", "Teleports you to T team's furthest spawn from your current position")]
        public void OnWorstTSpawnCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            TeleportPlayerToWorstSpawn(player!, (byte)CsTeam.Terrorist);
        }

        [ConsoleCommand("css_showspawns", "Highlights all the competitive spawns")]
        public void OnShowSpawnsCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            RemoveSpawnBeams();
            if (spawnsData.Values.Any(list => list.Count == 0)) GetSpawns();
            foreach (Position spawn in spawnsData[(byte)CsTeam.CounterTerrorist])
            {
                ShowSpawnBeam(spawn, Color.Blue);
            }
            foreach (Position spawn in spawnsData[(byte)CsTeam.Terrorist])
            {
                ShowSpawnBeam(spawn, Color.Orange);
            }
        }

        [ConsoleCommand("css_hidespawns", "Hides the highlighted spawns")]
        public void OnHideSpawnsCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || !IsPlayerValid(player)) return;
            RemoveSpawnBeams();
        }

        public void TeleportPlayerToBestSpawn(CCSPlayerController player, byte teamNum)
        {
            if (spawnsData.Values.Any(list => list.Count == 0)) GetSpawns();
            if (!spawnsData.TryGetValue(teamNum, out List<Position>? teamSpawns) || teamSpawns.Count == 0)
            {
                PrintToPlayerChat(player, Localizer["matchzy.pm.nospawns"]);
                return;
            }
            Vector playerPosition = player!.PlayerPawn!.Value!.CBodyComponent!.SceneNode!.AbsOrigin;
            int closestIndex = -1;
            double minDistance = double.MaxValue;
            for (int index = 0; index < teamSpawns.Count; index++)
            {
                Vector spawnPosition = teamSpawns[index].PlayerPosition;
                Vector diff = playerPosition - spawnPosition;
                float distance = diff.Length();
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = index;
                }
            }
            player!.PlayerPawn.Value!.Teleport(teamSpawns[closestIndex].PlayerPosition, teamSpawns[closestIndex].PlayerAngle, new Vector(0, 0, 0));
        }

        public void TeleportPlayerToWorstSpawn(CCSPlayerController player, byte teamNum)
        {
            if (spawnsData.Values.Any(list => list.Count == 0)) GetSpawns();
            if (!spawnsData.TryGetValue(teamNum, out List<Position>? teamSpawns) || teamSpawns.Count == 0)
            {
                PrintToPlayerChat(player, Localizer["matchzy.pm.nospawns"]);
                return;
            }
            Vector playerPosition = player!.PlayerPawn!.Value!.CBodyComponent!.SceneNode!.AbsOrigin;
            int farthestIndex = -1;
            double maxDistance = double.MinValue;
            for (int index = 0; index < teamSpawns.Count; index++)
            {
                Vector spawnPosition = teamSpawns[index].PlayerPosition;
                Vector diff = playerPosition - spawnPosition;
                float distance = diff.Length();
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestIndex = index;
                }
            }
            player!.PlayerPawn.Value!.Teleport(teamSpawns[farthestIndex].PlayerPosition, teamSpawns[farthestIndex].PlayerAngle, new Vector(0, 0, 0));
        }

        // Todo: Implement timer2 when we have OnPlayerRunCmd in CS#. Using OnTick would be its alternative, but it would be very expensive and not worth it.
        // [ConsoleCommand("css_timer2", "Starts a timer, use .timer2 again to stop it.")]
        // public void OnTimer2Command(CCSPlayerController? player, CommandInfo command)
        // {
        //     if (!isPractice || !IsPlayerValid(player)) return;
        //     int userId = player!.UserId!.Value;
        //     if (playerTimers.ContainsKey(userId))
        //     {
        //         PrintToPlayerChat(player, $"Timer stopped! Result: {playerTimers[userId].GetTimerResult()}s");
        //         playerTimers[userId].KillTimer();
        //         playerTimers.Remove(userId);
        //     }
        //     else
        //     {
        //         playerTimers[userId] = new PlayerPracticeTimer(PracticeTimerType.OnMovement);
        //         PrintToPlayerChat(player, $"When you start moving a timer will run until you stop moving.");
        //     }
        // }
    }
}
