using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;


namespace MatchZy
{

    public class Team 
    {
        public required string teamName;
        public string teamFlag = "";
        public string teamTag = "";

        public List<CCSPlayerController> teamPlayers = new List<CCSPlayerController>();

        public CCSPlayerController? coach;
    }

    public partial class MatchZy
    {
        [ConsoleCommand("css_coach", "Sets coach for the requested team")]
        public void OnCoachCommand(CCSPlayerController? player, CommandInfo? command) 
        {
            Log($"[OnCoachCommand]");
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

        public void HandleCoachCommand(CCSPlayerController player, string side) {
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
                //     Server.ExecuteCommand("mp_suicide_penalty 0; mp_death_drop_gun 0");
                //     coach.PlayerPawn.Value.CommitSuicide(false, true);
                //     Server.ExecuteCommand("mp_suicide_penalty 1; mp_death_drop_gun 1");
                // });
                coach.ActionTrackingServices!.MatchStats.Kills = 0;
                coach.ActionTrackingServices!.MatchStats.Deaths = 0;
                coach.ActionTrackingServices!.MatchStats.Assists = 0;
                coach.ActionTrackingServices!.MatchStats.Damage = 0;
            }
        }
        // Todo: Organize Teams code which can be later used for setting up matches
    }
}
