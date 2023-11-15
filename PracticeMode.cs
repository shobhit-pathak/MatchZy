using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Memory;



namespace MatchZy
{
    public class Position
    {

        public Vector PlayerPosition { get; private set; }
        public QAngle PlayerAngle { get; private set; }
        public Position(Vector playerPosition, QAngle playerAngle)
        {
            // Create deep copies of the Vector and QAngle objects
            PlayerPosition = new Vector(playerPosition.X, playerPosition.Y, playerPosition.Z);
            PlayerAngle = new QAngle(playerAngle.X, playerAngle.Y, playerAngle.Z);
        }
    }

    public partial class MatchZy
    {
        public Dictionary<byte, List<Position>> spawnsData = new Dictionary<byte, List<Position>> {
            { (byte)CsTeam.CounterTerrorist, new List<Position>() },
            { (byte)CsTeam.Terrorist, new List<Position>() }
        };

        public const string practiceCfgPath = "MatchZy/prac.cfg";

        // This map stores the bots which are being used in prac (probably spawned using .bot). Key is the userid of the bot.
        public Dictionary<int, Dictionary<string, object>> pracUsedBots = new Dictionary<int, Dictionary<string, object>>();

        public void StartPracticeMode()
        {
            if (matchStarted) return;
            isPractice = true;
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
                Server.ExecuteCommand("""mp_t_default_grenades "weapon_molotov weapon_hegrenade weapon_smokegrenade weapon_flashbang weapon_decoy"; mp_t_default_primary "weapon_ak47"; mp_warmup_online_enabled "true"; mp_warmup_pausetimer "1"; mp_warmup_start; bot_quota_mode fill; mp_solid_teammates 2; mp_autoteambalance false; mp_teammates_are_enemies true;""");
            }
            GetSpawns();
            Server.PrintToChatAll($"{chatPrefix} Practice mode loaded!");
            Server.PrintToChatAll($"{chatPrefix} Available commands:");
	    Server.PrintToChatAll($"{chatPrefix} \x10.spawn, .ctspawn, .tspawn, .bot, .nobots, .exitprac");
	    Server.PrintToChatAll($"{chatPrefix} \x10.loadnade <name>, .savenade <name>, .importnade <code> .listnades <optional filter>");
        }

        public void GetSpawns()
        {
            // Resetting spawn data to avoid any glitches
            spawnsData = new Dictionary<byte, List<Position>> {
                        { (byte)CsTeam.CounterTerrorist, new List<Position>() },
                        { (byte)CsTeam.Terrorist, new List<Position>() }
                    };

            var spawnsct = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist");

            foreach (var spawn in spawnsct)
            {
                if (spawn.IsValid)
                {
                    spawnsData[(byte)CsTeam.CounterTerrorist].Add(new Position(spawn.CBodyComponent?.SceneNode?.AbsOrigin, spawn.CBodyComponent?.SceneNode?.AbsRotation));
                }
            }

            var spawnst = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist");
            foreach (var spawn in spawnst)
            {
                if (spawn.IsValid)
                {
                    spawnsData[(byte)CsTeam.Terrorist].Add(new Position(spawn.CBodyComponent?.SceneNode?.AbsOrigin, spawn.CBodyComponent?.SceneNode?.AbsRotation));
                }
            }
        }

        private void HandleSpawnCommand(CCSPlayerController? player, string commandArg, byte teamNum, string command)
        {
            if (!isPractice || player == null) return;
            if (teamNum != 2 && teamNum != 3) return;
            if (!string.IsNullOrWhiteSpace(commandArg))
            {
                if (int.TryParse(commandArg, out int spawnNumber) && spawnNumber >= 1)
                {
                    // Adjusting the spawnNumber according to the array index.
                    spawnNumber -= 1;
                    if (spawnsData.ContainsKey(teamNum) && spawnsData[teamNum].Count <= spawnNumber) return;
                    player.PlayerPawn.Value.Teleport(spawnsData[teamNum][spawnNumber].PlayerPosition, spawnsData[teamNum][spawnNumber].PlayerAngle, new Vector(0, 0, 0));

                }
                else
                {
                    ReplyToUserCommand(player, $"Invalid value for {command} command. Please specify a valid non-negative number. Usage: !{command} <number>");
                    return;
                }
            }
            else
            {
                ReplyToUserCommand(player, $"Usage: !{command} <number>");
            }
        }
	
	private void HandleSaveNadeCommand(CCSPlayerController? player, string saveNadeName)
	{
		if (!isPractice || player == null) return;
		
		if (!IsPlayerAdmin(player)) return;
		
		if (!string.IsNullOrWhiteSpace(saveNadeName))
		{
			// Split the saveNadeName into two strings
			string[] saveNadeNameParts = saveNadeName.Split(' ', 2);
		
			// The first word
			string lineupName = saveNadeNameParts[0];
		
			// The remaining words (if any)
			string lineupDesc = saveNadeNameParts.Length > 1 ? saveNadeNameParts[1] : "";
		
			// Save current player pos and ang into a local
			QAngle playerangle = player.PlayerPawn.Value.EyeAngles;
			Vector playerpos = player.Pawn.Value.CBodyComponent!.SceneNode.AbsOrigin;
		
			// add that into a savedNades list that contains the saveNadeName, playerpos, playerangle
			string savednadesfileName = "MatchZy/savednades.cfg";
			string savednadesPath = Path.Join(Server.GameDirectory + "/csgo/cfg", savednadesfileName);
		
			if (!File.Exists(savednadesPath)) File.WriteAllLines(savednadesPath, new[] { "Name Location Viewangle" });
		
			// Check if lineupName already exists in the file
			var existingLines = File.ReadAllLines(savednadesPath);
			if (existingLines.Any(line => line.StartsWith(lineupName + " ")))
			{
				ReplyToUserCommand(player, $" \x0DLineup \x06'{lineupName}'\x0D already exists!");
				ReplyToUserCommand(player, $" \x0DYou can use \x06'.deletenade <name>'\x0D to delete it!");
				return;
			}
		
			// Append saveNadeName playerpos playerangle to a new line and save to savednades.cfg
			var nadeInfo = $"{lineupName} {playerpos} {playerangle} {lineupDesc}";
		
			File.AppendAllLines(savednadesPath, new[] { nadeInfo });
		
			ReplyToUserCommand(player, $" \x0DLineup \x06'{lineupName}' \x0Dsaved successfully!");
			player.PrintToCenter($" \x0DLineup \x06'{lineupName}' \x0Dsaved successfully!");
			ReplyToUserCommand(player, $" \x0DLineup Code: \x06{lineupName} {playerpos} {playerangle}");
		}
		else
		{
			ReplyToUserCommand(player, $"Usage: .savenade <name>");
		}
	}
	
	private void HandleDeleteNadeCommand(CCSPlayerController? player, string saveNadeName)
	{
		if (!isPractice || player == null) return;
		
		if (!IsPlayerAdmin(player)) return;
		
		if (!string.IsNullOrWhiteSpace(saveNadeName))
		{
			// Construct the path to the savednades.cfg file
			string savednadesfileName = "MatchZy/savednades.cfg";
			string savednadesPath = Path.Join(Server.GameDirectory + "/csgo/cfg", savednadesfileName);
		
			if (!File.Exists(savednadesPath))
			{
				ReplyToUserCommand(player, $"The file '{savednadesfileName}' does not exist.");
				return;
			}
		
			// Read all existing lines from the file
			var existingLines = File.ReadAllLines(savednadesPath).ToList();
		
			// Find and remove the line with the specified lineupName
			bool lineupFound = false;
			for (int i = 1; i < existingLines.Count; i++) // Start from index 1 to skip the header line
			{
				if (existingLines[i].StartsWith(saveNadeName + " "))
				{
					existingLines.RemoveAt(i);
					lineupFound = true;
					break;
				}
			}
		
			if (lineupFound)
			{
				// Save the modified lines back to the file
				File.WriteAllLines(savednadesPath, existingLines);
				ReplyToUserCommand(player, $" \x0DLineup \x06'{saveNadeName}'\x0D deleted successfully!");
				}
				else
				{
				ReplyToUserCommand(player, $" \x0DLineup \x06'{saveNadeName}'\x0D is not saved.");
				}
			}
			else
			{
				ReplyToUserCommand(player, $"Usage: .deletenade <name>");
			}
	}



	
	private void HandleImportNadeCommand(CCSPlayerController? player, string saveNadeName)
	{
		if (!isPractice || player == null) return;
		
		if (!IsPlayerAdmin(player)) return;
		
		if (!string.IsNullOrWhiteSpace(saveNadeName))
		{
			
			// add that into a savedNades list that contains the saveNadeName, playerpos, playerangle
			string savednadesfileName = "MatchZy/savednades.cfg";
			string savednadesPath = Path.Join(Server.GameDirectory + "/csgo/cfg", savednadesfileName);
		
			if (!File.Exists(savednadesPath)) File.WriteAllLines(savednadesPath, new[] { "Name Location Viewangle" });
		
			// Append saveNadeName playerpos playerangle to a new line and save to savednades.cfg
			var nadeInfo = saveNadeName;
		
			File.AppendAllLines(savednadesPath, new[] { nadeInfo });
		
			ReplyToUserCommand(player, $" \x0DLineup \x06'{saveNadeName}' \x0Dsaved successfully!");
			player.PrintToCenter($" \x0DLineup \x06'{saveNadeName}' \x0Dsaved successfully!");
			ReplyToUserCommand(player, $" \x0DTo load it use \x06.loadnade {saveNadeName}");
		}
		else
		{
			ReplyToUserCommand(player, $"Usage: .importnade <CODE>");
		}
	}
	
	private void HandleListNadesCommand(CCSPlayerController? player, string nadeFilter)
	{
		if (!isPractice || player == null) return;
		
		if (!IsPlayerAdmin(player)) return;
		
		// Read the file
		string savedNadesFileName = "MatchZy/savednades.cfg";
		string savedNadesPath = Path.Combine(Server.GameDirectory, "csgo", "cfg", savedNadesFileName);
		
		if (!string.IsNullOrWhiteSpace(nadeFilter))
		{
			ReplyToUserCommand(player, $" \x0D All saved lineups filtered by '{nadeFilter}':");
		}
		else
		{
			ReplyToUserCommand(player, $" \x0D All saved lineups:");
		}	
		if (File.Exists(savedNadesPath))
		{
			// Read all lines from the file
			string[] lines = File.ReadAllLines(savedNadesPath);
		
			// Skip the first line
			for (int i = 1; i < lines.Length; i++)
			{
				string line = lines[i];
			
				// Split the line into words
				string[] words = line.Split(' ');
			
				if (!string.IsNullOrWhiteSpace(nadeFilter))
				{
					// Check if the first word contains the nadeFilter
					if (words.Length > 0 && words[0].Contains(nadeFilter))
					{
						ReplyToUserCommand(player, $" \x0D.loadnade \x06{words[0]}");
					}
				}
				else
				{
					// If the filter is empty, just return every first word of the line
					if (words.Length > 0)
					{
						ReplyToUserCommand(player, $"\x0D.loadnade \x06{words[0]}");
					}
				}
			}
		}
		else
		{
			ReplyToUserCommand(player, "There are no saved nades!.");
		}
	}

	
	private void HandleLoadNadeCommand(CCSPlayerController? player, string loadNadeName)
	{
		if (!isPractice || player == null) return;
		
		if (!IsPlayerAdmin(player)) return;
		
		string savednadesfileName = "MatchZy/savednades.cfg";
		string savednadesPath = Path.Join(Server.GameDirectory + "/csgo/cfg", savednadesfileName);
		
		if (!string.IsNullOrWhiteSpace(loadNadeName) && File.Exists(savednadesPath))
		{
			// Read the savednades.cfg, ignore the very first line of the file
			var lines = File.ReadAllLines(savednadesPath).Skip(1);
		
			// Find the line that matches loadNadeName
			var nadeLine = lines.FirstOrDefault(line => line.StartsWith(loadNadeName));
		
			if (nadeLine != null)
			{
				// Split the line into parts
				var parts = nadeLine.Split(' ');
			
				if (parts.Length >= 7)
				{
					// Concatenate parts 7 and beyond into a separate string
					string lineupDesc = string.Join(" ", parts.Skip(7));
			
					// Keep parts 1 to 6 as they are
					string loadedPlayerPosString = string.Join(" ", parts.Take(4));
					string loadedPlayerAngleString = string.Join(" ", parts.Skip(4).Take(3));
			
					// Parse the numbers to create a Vector and QAngle
					float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
					float y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
					float z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
			
					Vector loadedPlayerPos = new Vector(x, y, z);
			
					float pitch = float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture);
					float yaw = float.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture);
					float roll = float.Parse(parts[6], System.Globalization.CultureInfo.InvariantCulture);
			
					QAngle loadedPlayerAngle = new QAngle(pitch, yaw, roll);
			
					ReplyToUserCommand(player, $" \x0D Lineup \x06'{loadNadeName}' \x0Dloaded successfully!");
					
					if (!string.IsNullOrWhiteSpace(lineupDesc))
					{
						player.PrintToCenter($"{lineupDesc}");
						ReplyToUserCommand(player, $" \x0D Description: \x06'{lineupDesc}'");
					}
					
			
					player.PlayerPawn.Value.Teleport(loadedPlayerPos, loadedPlayerAngle, new Vector(0, 0, 0));
					return;
				}
			}
		
			ReplyToUserCommand(player, $"Nade not found! Usage: .loadnade <name>");
		}
		else
		{
			ReplyToUserCommand(player, $"Nade not found! Usage: .loadnade <name>");
		}
	}
	
	[ConsoleCommand("css_god", "Sets Infinite health for player")]
        public void OnGodCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || player == null) return;
		
	    if (!IsPlayerAdmin(player)) return;
	    
	    player.PlayerPawn.Value.Health = 2147483647; // max 32bit int
        }

        [ConsoleCommand("css_prac", "Starts practice mode")]
        public void OnPracCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!IsPlayerAdmin(player)) {
                SendPlayerNotAdminMessage(player);
                return;
            }

            if (matchStarted)
            {
                ReplyToUserCommand(player, "Practice Mode cannot be started when a match has been started!");
                return;
            }
	    
	    if (isPractice)
            {
                StartMatchMode();
                return;
            }

            StartPracticeMode();
        }
	
	// need to find a way to either grab ents or CSS needs a DoEntFire() function
	//[ConsoleCommand("css_killsmoke", "Kills all Smokes, HE and Molly ents")]
        //public void OnKillsmokeCommand(CCSPlayerController? player, CommandInfo? command)
        //{
        //    Log($"[.killsmoke] Sent by: {player.UserId}");
	//    if (!IsPlayerAdmin(player))
	//    { 
	//	Log($"[.killsmoke] failed cuz not admin");
	//	return;
	//    }
	//    
        //    if (matchStarted)
        //    {
        //        ReplyToUserCommand(player, "Cannot kill Smokes when a match has been started!");
        //        return;
        //    }
	//    
	//    Server.ExecuteCommand("ent_fire smokegrenade_projectile kill");
	//    Server.ExecuteCommand("ent_fire molotov_projectile kill");
	//    Server.ExecuteCommand("ent_fire flashbang_projectile kill");
	//    Server.ExecuteCommand("ent_fire hegrenade_projectile kill");
	//    Server.ExecuteCommand("ent_fire decoy_projectile kill");
	//    Log($"[.killsmoke] killed all smokes"); 
        //}


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
                ReplyToUserCommand(player, $"Usage: !spawn <round>");
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
                ReplyToUserCommand(player, $"Usage: !ctspawn <round>");
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
                ReplyToUserCommand(player, $"Usage: !ctspawn <round>");
            }
        }

        [ConsoleCommand("css_bot", "Teleport to spawn")]
        public void OnBotCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || player == null) return;
            // Checking if any of the Position List is empty
            if (spawnsData.Values.Any(list => list.Count == 0)) GetSpawns();

            // !bot/.bot command is made using a lot of workarounds, as there is no direct way to create a bot entity and spawn it in CSSharp
            // Hence there can be some issues with this approach. This will be revamped when we will be able to create entities and manipulate them.
            if (player.TeamNum == 2)
            {
                Server.ExecuteCommand("bot_join_team T");
                Server.ExecuteCommand("bot_add_t");
            }
            else if (player.TeamNum == 3)
            {
                Server.ExecuteCommand("bot_join_team CT");
                Server.ExecuteCommand("bot_add_ct");
            }
            
            // Adding a small timer so that bot can be added in the world
            // Once bot is added, we teleport it to the requested position
            AddTimer(0.1f, () => SpawnBot(player));
            Server.ExecuteCommand("bot_stop 1");
            Server.ExecuteCommand("bot_freeze 1");
            Server.ExecuteCommand("bot_zombie 1");
        }

        private void SpawnBot(CCSPlayerController botOwner)
        {
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            bool unusedBotFound = false;
            foreach (var tempPlayer in playerEntities)
            {
                if (!tempPlayer.IsBot) continue;
                if (tempPlayer.UserId.HasValue)
                {
                    if (!pracUsedBots.ContainsKey(tempPlayer.UserId.Value) && unusedBotFound)
                    {
                        Log($"UNUSED BOT FOUND: {tempPlayer.UserId.Value} EXECUTING: kickid {tempPlayer.UserId.Value}");
                        // Kicking the unused bot. We have to do this because bot_add_t/bot_add_ct may add multiple bots but we need only 1, so we kick the remaining unused ones
                        Server.ExecuteCommand($"kickid {tempPlayer.UserId.Value}");
                        continue;
                    }
                    if (pracUsedBots.ContainsKey(tempPlayer.UserId.Value))
                    {
                        continue;
                    }
                    else
                    {
                        pracUsedBots[tempPlayer.UserId.Value] = new Dictionary<string, object>();
                    }

                    Position botOwnerPosition = new Position(botOwner.PlayerPawn.Value.CBodyComponent?.SceneNode?.AbsOrigin, botOwner.PlayerPawn.Value.CBodyComponent?.SceneNode?.AbsRotation);
                    // Add key-value pairs to the inner dictionary
                    pracUsedBots[tempPlayer.UserId.Value]["controller"] = tempPlayer;
                    pracUsedBots[tempPlayer.UserId.Value]["position"] = botOwnerPosition;
                    pracUsedBots[tempPlayer.UserId.Value]["owner"] = botOwner;

                    tempPlayer.PlayerPawn.Value.Teleport(botOwnerPosition.PlayerPosition, botOwnerPosition.PlayerAngle, new Vector(0, 0, 0));
                    unusedBotFound = true;
                }
            }
            if (!unusedBotFound) {
                Server.PrintToChatAll($"{chatPrefix} Cannot add bots, the team is full! Use .nobots to remove the current bots.");
            }
        }

        [GameEventHandler]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;

            // Respawing a bot where it was actually spawned during practice session
            if (isPractice && player.IsValid && player.IsBot && player.UserId.HasValue)
            {
                if (pracUsedBots.ContainsKey(player.UserId.Value))
                {
                    if (pracUsedBots[player.UserId.Value]["position"] is Position botPosition)
                    {
                        player.PlayerPawn.Value.Teleport(botPosition.PlayerPosition, botPosition.PlayerAngle, new Vector(0, 0, 0));
                    }
                }
            }


            return HookResult.Continue;
        }

        [ConsoleCommand("css_nobots", "Removes bots from the practice session")]
        public void OnNoBotsCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (!isPractice || player == null) return;
            Server.ExecuteCommand("bot_kick");
            pracUsedBots = new Dictionary<int, Dictionary<string, object>>();
        }

        public void ExecUnpracCommands() {
            Server.ExecuteCommand("sv_cheats false;sv_grenade_trajectory_prac_pipreview false;sv_grenade_trajectory_prac_trailtime 0; mp_ct_default_grenades \"\"; mp_ct_default_primary \"\"; mp_t_default_grenades\"\"; mp_t_default_primary\"\"; mp_teammates_are_enemies false;");
            Server.ExecuteCommand("mp_death_drop_breachcharge true; mp_death_drop_defuser true; mp_death_drop_taser true; mp_drop_knife_enable false; mp_death_drop_grenade 2; ammo_grenade_limit_total 4; mp_defuser_allocation 0; sv_infinite_ammo 0; mp_force_pick_time 15");
        }

    }
}
