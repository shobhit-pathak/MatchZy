using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchZy;

// Technical pauses as in Get5 (get5_max_tech_pauses / get5_tech_pause_time). Both limits default to 0 (unlimited, both teams
// have to unpause), which is how .tech / .pause have always worked in MatchZy.
public partial class MatchZy
{
    // Technical pauses a team can call per map (0 = unlimited) and seconds after which any one team can unpause (0 = never).
    public int maxTechPauses = 0;
    public int techPauseTime = 0;

    // Technical pauses used this map, by team number (1 / 2).
    private readonly int[] techPausesUsed = new int[3];

    // The pause in progress: its type, the team that called it (1 / 2, 0 = admin / none) and the seconds it has been in effect
    // (counted in freeze time; -1 until it takes effect).
    private PauseType currentPauseType = PauseType.None;
    private int pausingTeamNumber = 0;
    private int pauseSecondsElapsed = -1;
    // The map the pause began on: the unpause event of a pause that lasts until the series moves on is still for that map.
    private int pauseMapNumber = 0;
    private CounterStrikeSharp.API.Modules.Timers.Timer? pauseTimer;

    [ConsoleCommand("matchzy_max_tech_pauses", "Number of technical pauses a team can use per map. 0 = unlimited. Default: 0")]
    [ConsoleCommand("get5_max_tech_pauses", "Number of technical pauses a team can use per map. 0 = unlimited. Default: 0")]
    public void MaxTechPausesCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null) return;
        if (int.TryParse(GetSettingArgument(command), out int value) && value >= 0) maxTechPauses = value;
    }

    [ConsoleCommand("matchzy_tech_pause_time", "Seconds a technical pause lasts before any one team can unpause it (otherwise both teams have to). 0 = no limit. Default: 0")]
    [ConsoleCommand("get5_tech_pause_time", "Seconds a technical pause lasts before any one team can unpause it (otherwise both teams have to). 0 = no limit. Default: 0")]
    public void TechPauseTimeCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null) return;
        if (int.TryParse(GetSettingArgument(command), out int value) && value >= 0) techPauseTime = value;
    }

    [ConsoleCommand("get5_allow_technical_pause", "Whether technical pauses are allowed (same as matchzy_enable_tech_pause). Default: true")]
    public void AllowTechnicalPauseCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null) return;
        techPauseEnabled.Value = ParseBoolSetting(GetSettingArgument(command), techPauseEnabled.Value);
    }

    // Called whenever the match is paused (by a team, an admin or after a round restore).
    private void StartPauseTracking(PauseType type, int teamNumber)
    {
        // A new pause replacing one in progress (e.g. a round restore during a pause) ends that one first.
        if (currentPauseType != PauseType.None) StopPauseTracking();
        currentPauseType = type;
        pauseMapNumber = matchConfig.CurrentMapNumber;
        pausingTeamNumber = teamNumber;
        pauseSecondsElapsed = -1;
        pauseTimer?.Kill();
        pauseTimer = AddTimer(1.0f, PauseTimerTick, TimerFlags.REPEAT);
        SendPauseEvent(true);
    }

    // Called whenever the match is unpaused. sendEvent is false when the match is reset or the map changes while paused.
    private void StopPauseTracking(bool sendEvent = true)
    {
        pauseTimer?.Kill();
        pauseTimer = null;
        if (sendEvent && currentPauseType != PauseType.None) SendPauseEvent(false);
        currentPauseType = PauseType.None;
        pausingTeamNumber = 0;
        pauseSecondsElapsed = -1;
    }

    // Technical pauses used, from a backup of another match or map (Get5), to set when its round is restored.
    private (int Team1, int Team2)? pendingRestoreTechPauses = null;

    // Pause counts are per map (as in Get5, not reset at halftime or by a round restore of the same map).
    public void ResetTechPauses()
    {
        techPausesUsed[1] = 0;
        techPausesUsed[2] = 0;
    }

    private void SetTechPausesUsed(int team1Used, int team2Used)
    {
        techPausesUsed[1] = team1Used;
        techPausesUsed[2] = team2Used;
    }

    private Team? GetTeamByNumber(int teamNumber) => teamNumber == 1 ? matchzyTeam1 : teamNumber == 2 ? matchzyTeam2 : null;

    // Every second while paused (Get5: Timer_PauseTimeCheck). A pause takes effect in freeze time, which is when it starts
    // counting; a technical pause is counted as used on its first second.
    private void PauseTimerTick()
    {
        try
        {
            if (!isPaused || currentPauseType == PauseType.None)
            {
                StopPauseTracking(false);
                return;
            }
            if (!GetGameRules().FreezePeriod) return;

            pauseSecondsElapsed++;
            if (currentPauseType != PauseType.Technical) return;

            if (pauseSecondsElapsed == 0) techPausesUsed[pausingTeamNumber]++;
            int used = techPausesUsed[pausingTeamNumber];
            int secondsLeft = PauseLogic.SecondsUntilAnyoneCanUnpause(pauseSecondsElapsed, techPauseTime, used, maxTechPauses);
            if (secondsLeft == 0 && pauseSecondsElapsed == techPauseTime)
            {
                // Only announced once; the pause goes on until a team unpauses.
                PrintToAllChat(Localizer["matchzy.pause.anyonecanunpause"]);
            }

            Team? team = GetTeamByNumber(pausingTeamNumber);
            if (team == null) return;
            string side = teamSides.TryGetValue(team, out string? teamSide) && teamSide == "CT" ? "CT" : "T";
            string count = PauseLogic.PauseCountSuffix(used, maxTechPauses);
            string hint = secondsLeft > 0
                ? Localizer["matchzy.pause.techhinttime", team.teamName, side, count, ReadyTimeLogic.FormatTime(secondsLeft)]
                : Localizer["matchzy.pause.techhintawaiting", team.teamName, side, count];
            foreach (var player in playerData.Values)
            {
                if (player.IsValid && !player.IsBot) player.PrintToCenter(hint);
            }
        }
        catch (Exception e)
        {
            Log($"[PauseTimerTick FATAL] An error occurred: {e.Message}");
        }
    }

    // game_paused / game_unpaused in Get5's format (team1 / team2 / none, and the pause type).
    private void SendPauseEvent(bool paused)
    {
        var pauseEvent = new MatchZyPauseEvent(paused ? "game_paused" : "game_unpaused")
        {
            MatchId = liveMatchId,
            MapNumber = pauseMapNumber,
            Team = pausingTeamNumber == 1 ? "team1" : pausingTeamNumber == 2 ? "team2" : "none",
            PauseType = PauseLogic.ToEventName(currentPauseType),
        };
        Task.Run(async () => await SendEventAsync(pauseEvent));
    }

    // .unpause during a technical pause, before the both-teams rule (Get5: Command_Unpause). Returns true when handled.
    private bool HandleTechPauseUnpause(CCSPlayerController player, int teamNumber)
    {
        if (currentPauseType != PauseType.Technical) return false;
        Team? team = GetTeamByNumber(teamNumber);
        if (team == null) return false;

        if (teamNumber == pausingTeamNumber && pauseSecondsElapsed < 0)
        {
            // The pause has not taken effect yet (no freeze time): the pausing team can cancel it, which does not use it up.
            PrintToAllChat(Localizer["matchzy.pause.pauserequestcanceled", team.teamName]);
            UnpauseMatch();
            return true;
        }
        if (PauseLogic.AnyoneCanUnpause(pauseSecondsElapsed, techPauseTime, techPausesUsed[pausingTeamNumber], maxTechPauses))
        {
            PrintToAllChat(Localizer["matchzy.pause.unpausedby", team.teamName]);
            UnpauseMatch();
            return true;
        }
        return false;
    }
}
