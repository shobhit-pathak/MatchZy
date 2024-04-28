using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchZy;

public partial class MatchZy
{
    public Dictionary<Team, int> technicalPauseUsed = new();
    public int lastTechPauseDuration = 0;

    public void TechPause(CCSPlayerController? player, CommandInfo? command)
    {
        // Tech Pause is WIP
        return;

        if (!isMatchLive) return;

        // Treating .tech command as .forcepause if it is used via server console.
        if (player == null)
        {
            ForcePauseMatch(player, command);
            return;
        }

        if (isPaused)
        {
            // ReplyToUserCommand(player, "Match is already paused!");
            ReplyToUserCommand(player, Localizer["matchzy.pause.ispaused"]);
            return;
        }
        if (IsHalfTimePhase())
        {
            // ReplyToUserCommand(player, "You cannot use this command during halftime.");
            ReplyToUserCommand(player, Localizer["matchzy.pause.duringhalftime"]); ;
            return;
        }
        if (IsPostGamePhase())
        {
            // ReplyToUserCommand(player, "You cannot use this command after the game has ended.");
            ReplyToUserCommand(player, Localizer["matchzy.pause.matchended"]);
            return;
        }
        if (IsTacticalTimeoutActive())
        {
            // ReplyToUserCommand(player, "You cannot use this command when tactical timeout is active.");
            ReplyToUserCommand(player, Localizer["matchzy.pause.tacticaltimeout"]);
            return;
        }

        if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None) return;

        if (!techPauseEnabled.Value && player != null)
        {
            PrintToPlayerChat(player, Localizer["matchzy.ready.techpausenotenabled"]);
            return;
        }

        if (maxTechPausesAllowed.Value <= 0) return;

        Team playerTeam = (player!.Team == CsTeam.CounterTerrorist) ? reverseTeamSides["CT"] : reverseTeamSides["TERRORIST"];
        if (technicalPauseUsed[playerTeam] >= maxTechPausesAllowed.Value)
        {
            PrintToPlayerChat(player, Localizer["matchzy.pause.notechpauseleft", playerTeam.teamName]);
            return;
        }
    }
}