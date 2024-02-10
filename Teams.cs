using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json.Linq;


namespace MatchZy
{

    public class Team 
    {
        public string id = "";
        public required string teamName;
        public string teamFlag = "";
        public string teamTag = "";

        public JToken? teamPlayers;

        public CCSPlayerController? coach;
        public int seriesScore = 0;
    }

    public partial class MatchZy
    {
        [ConsoleCommand("css_coach", "Sets coach for the requested team")]
        public void OnCoachCommand(CCSPlayerController? player, CommandInfo command) 
        {
            HandleCoachCommand(player, command.ArgString);
        }

        [ConsoleCommand("css_uncoach", "Sets coach for the requested team")]
        public void OnUnCoachCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null || !player.PlayerPawn.IsValid) return;
            if (isPractice) {
                ReplyToUserCommand(player, "Uncoach command can only be used in match mode!");
                return;
            }

            Team matchZyCoachTeam;

            if (matchzyTeam1.coach == player) {
                matchZyCoachTeam = matchzyTeam1;
            }
            else if (matchzyTeam2.coach == player) {
                matchZyCoachTeam = matchzyTeam2;
            }
            else {
                ReplyToUserCommand(player, "You are not coaching any team!");
                return;
            }

            player.Clan = "";
            if (player.InGameMoneyServices != null) player.InGameMoneyServices.Account = 0;
            matchZyCoachTeam.coach = null;
            ReplyToUserCommand(player, "You are now not coaching any team!");
        }

        [ConsoleCommand("matchzy_addplayer", "Adds player to the provided team")]
        [ConsoleCommand("get5_addplayer", "Adds player to the provided team")]
        public void OnAddPlayerCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player != null || command == null) return;
            if (!isMatchSetup) {
                command.ReplyToCommand("No match is setup!");
                return;
            }
            if (IsHalfTimePhase())
            {
                command.ReplyToCommand("Cannot add players during halftime. Please wait until the next round starts.");
                return;
            }
            if (command.ArgCount < 3)
            {
                command.ReplyToCommand("Usage: matchzy_addplayertoteam <steam64> <team> \"<name>\"");
                return; 
            }

            string playerSteamId = command.ArgByIndex(1);
            string playerTeam = command.ArgByIndex(2);
            string playerName = command.ArgByIndex(3);
            bool success;
            if (playerTeam == "team1")
            {
                success = AddPlayerToTeam(playerSteamId, playerName, matchzyTeam1.teamPlayers);
            } else if (playerTeam == "team2")
            {
                success = AddPlayerToTeam(playerSteamId, playerName, matchzyTeam2.teamPlayers);
            } else if (playerTeam == "spec")
            {
                success = AddPlayerToTeam(playerSteamId, playerName, matchConfig.Spectators);
            } else 
            {
                command.ReplyToCommand("Unknown team: must be one of team1, team2, spec");
                return; 
            }
            if (!success)
            {
                command.ReplyToCommand($"Failed to add player {playerName} to {playerTeam}. They may already be on a team or you provided an invalid Steam ID.");
                return;
            }
            command.ReplyToCommand($"Player {playerName} added to {playerTeam} successfully!");
        }

        public void HandleCoachCommand(CCSPlayerController? player, string side) {
            if (player == null || !player.PlayerPawn.IsValid) return;
            if (isPractice) {
                ReplyToUserCommand(player, "Coach command can only be used in match mode!");
                return;
            }

            side = side.Trim().ToLower();

            if (side != "t" && side != "ct") {
                ReplyToUserCommand(player, "Usage: .coach t or .coach ct");
                return;
            }

            if (matchzyTeam1.coach == player || matchzyTeam2.coach == player) 
            {
                ReplyToUserCommand(player, "You are already coaching a team!");
                return;
            }

            Team matchZyCoachTeam;

            if (side == "t") {
                matchZyCoachTeam = reverseTeamSides["TERRORIST"];
            } else if (side == "ct") {
                matchZyCoachTeam = reverseTeamSides["CT"];
            } else {
                return;
            }

            if (matchZyCoachTeam.coach != null) {
                ReplyToUserCommand(player, "Coach slot for this team has been already taken!");
                return;
            }

            matchZyCoachTeam.coach = player;
            player.Clan = $"[{matchZyCoachTeam.teamName} COACH]";
            if (player.InGameMoneyServices != null) player.InGameMoneyServices.Account = 0;
            ReplyToUserCommand(player, $"You are now coaching {matchZyCoachTeam.teamName}! Use .uncoach to stop coaching");
            Server.PrintToChatAll($"{chatPrefix} {ChatColors.Green}{player.PlayerName}{ChatColors.Default} is now coaching {ChatColors.Green}{matchZyCoachTeam.teamName}{ChatColors.Default}!");
        }

        public void HandleCoaches() 
        {
            List<CCSPlayerController?> coaches = new List<CCSPlayerController?>
            {
                matchzyTeam1.coach,
                matchzyTeam2.coach
            };

            foreach (var coach in coaches) 
            {
                if (coach == null) continue;
                Log($"Found coach: {coach.PlayerName}");
                coach.InGameMoneyServices!.Account = 0;
                AddTimer(0.5f, () => HandleCoachTeam(coach, true));
                // AddTimer(1, () => {
                    // Server.ExecuteCommand("mp_suicide_penalty 0; mp_death_drop_gun 0");
                    // coach.PlayerPawn.Value.CommitSuicide(false, true);
                    // Server.ExecuteCommand("mp_suicide_penalty 1; mp_death_drop_gun 1");
                // });
                coach.ActionTrackingServices!.MatchStats.Kills = 0;
                coach.ActionTrackingServices!.MatchStats.Deaths = 0;
                coach.ActionTrackingServices!.MatchStats.Assists = 0;
                coach.ActionTrackingServices!.MatchStats.Damage = 0;
            }
        }

        public bool AddPlayerToTeam(string steamId, string name, JToken? team)
        {
            if (matchzyTeam1.teamPlayers != null && matchzyTeam1.teamPlayers[steamId] != null) return false;
            if (matchzyTeam2.teamPlayers != null && matchzyTeam2.teamPlayers[steamId] != null) return false;
            if (matchConfig.Spectators != null && matchConfig.Spectators[steamId] != null) return false;

            if (team is JObject jObjectTeam)
            {
                jObjectTeam.Add(steamId, name);
                LoadClientNames();
                return true;
            }
            else if (team is JArray jArrayTeam)
            {
                jArrayTeam.Add(name);
                LoadClientNames();
                return true;
            }
            return false;
        }
    }
}
