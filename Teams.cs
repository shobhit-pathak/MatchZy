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
            List<CCSPlayerController?> coaches = new()
            {
                matchzyTeam1.coach,
                matchzyTeam2.coach
            };

            foreach (var coach in coaches) 
            {
                if (coach == null) continue;
                Team coachTeam = coach == matchzyTeam1.coach ? matchzyTeam1 : matchzyTeam2;
                int coachTeamNum = teamSides[coachTeam] == "CT" ? 3 : 2;
                coach.InGameMoneyServices!.Account = 0;
                AddTimer(0.5f, () => HandleCoachTeam(coach, true));

                coach.ActionTrackingServices!.MatchStats.Kills = 0;
                coach.ActionTrackingServices!.MatchStats.Deaths = 0;
                coach.ActionTrackingServices!.MatchStats.Assists = 0;
                coach.ActionTrackingServices!.MatchStats.Damage = 0;

                bool isCompetitiveSpawn = false;

                Position coachPosition = new(coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin, coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsRotation);
                List<Position> teamPositions = spawnsData[(byte)coachTeamNum];

                // Elevating the coach so that they don't block the players.
                coach!.PlayerPawn.Value!.Teleport(new Vector(coach.PlayerPawn.Value.CBodyComponent!.SceneNode!.AbsOrigin.X, coach.PlayerPawn.Value.CBodyComponent!.SceneNode!.AbsOrigin.Y, coach.PlayerPawn.Value.CBodyComponent!.SceneNode!.AbsOrigin.Z + 150.0f), coach.PlayerPawn.Value.EyeAngles, new Vector(0, 0, 0));
                coach.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_NONE;
                coach.PlayerPawn.Value!.ActualMoveType = MoveType_t.MOVETYPE_NONE;

                foreach (Position position in teamPositions) 
                {
                    if (position.Equals(coachPosition)) 
                    {
                        isCompetitiveSpawn = true;
                        break;
                    }
                }
                if (isCompetitiveSpawn)
                {
                    foreach (var key in playerData.Keys)
                    {
                        CCSPlayerController player = playerData[key];
                        if (!IsPlayerValid(player) || player == coach || player.TeamNum != (byte)coachTeamNum) continue;
                        bool playerOnCompetitiveSpawn = false;
                        Position playerPosition = new(player.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin, player.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsRotation);
                        foreach (Position position in teamPositions)
                        {
                            if (position.Equals(playerPosition))
                            {
                                playerOnCompetitiveSpawn = true;
                                break;
                            }
                        }
                        // No need to swap the player if they are already on a competitive spawn.
                        if (playerOnCompetitiveSpawn) continue;
                        // Swapping positions of the coach and the player so that the coach doesn't take any competitive spawn.
                        AddTimer(0.1f, () => 
                        {
                            coach!.PlayerPawn.Value.Teleport(new Vector(playerPosition.PlayerPosition.X, playerPosition.PlayerPosition.Y, playerPosition.PlayerPosition.Z + 150.0f), playerPosition.PlayerAngle, new Vector(0, 0, 0));
                            player!.PlayerPawn.Value.Teleport(coachPosition.PlayerPosition, coachPosition.PlayerAngle, new Vector(0, 0, 0));
                        });
                    }
                }
                HandleCoachWeapons(coach);
            }
        }

        private void HandleCoachWeapons(CCSPlayerController coach)
        {
            if (!IsPlayerValid(coach)) return;

            var coachWeaponServices = coach.PlayerPawn.Value!.WeaponServices;
            if (coachWeaponServices == null) return;
            bool coachHasC4 = false;
            foreach (var weapon in coachWeaponServices.MyWeapons)
            {
                if (weapon == null || !weapon.IsValid || weapon.Value == null || !weapon.Value.IsValid) continue;
                if (weapon.Value.DesignerName == "weapon_c4") coachHasC4 = true;
            }

            coach.RemoveWeapons();

            if (!coachHasC4) return;

            // Giving a random player from coach's team the C4
            var randomKeys = playerData.Keys.OrderBy(k => Guid.NewGuid());
            foreach (var key in randomKeys)
            {
                CCSPlayerController player = playerData[key];
                if (!IsPlayerValid(player) || player == coach || player.TeamNum != coach.TeamNum) continue;
                player.GiveNamedItem("weapon_c4");
                break;
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
