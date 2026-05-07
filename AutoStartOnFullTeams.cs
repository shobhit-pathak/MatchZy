using CounterStrikeSharp.API.Modules.Utils;

namespace MatchZy;

public partial class MatchZy
{
    public CounterStrikeSharp.API.Modules.Timers.Timer? autoStartCountdownTimer = null;
    public int autoStartRemainingSeconds = 0;

    private void CheckAutoStartOnFullTeams()
    {
        if (!autoStartOnFullTeamsEnabled.Value || !readyAvailable || matchStarted)
        {
            if (autoStartCountdownTimer != null)
            {
                CancelAutoStartCountdown(silent: true);
            }
            return;
        }

        (int ctCount, _) = GetTeamPlayerCount((int)CsTeam.CounterTerrorist, false);
        (int tCount, _) = GetTeamPlayerCount((int)CsTeam.Terrorist, false);
        int required = matchConfig.PlayersPerTeam;

        bool teamsFull = ctCount >= required && tCount >= required;

        if (teamsFull && autoStartCountdownTimer == null)
        {
            StartAutoStartCountdown();
        }
        else if (!teamsFull && autoStartCountdownTimer != null)
        {
            CancelAutoStartCountdown(silent: false);
        }
    }

    private void StartAutoStartCountdown()
    {
        autoStartRemainingSeconds = autoStartOnFullTeamsDelay.Value;

        unreadyPlayerMessageTimer?.Kill();
        unreadyPlayerMessageTimer = null;

        if (autoStartRemainingSeconds <= 0)
        {
            TriggerAutoStart();
            return;
        }

        PrintToAllChat(Localizer["matchzy.autostart.starting", autoStartRemainingSeconds]);
        autoStartCountdownTimer = AddTimer(1.0f, AutoStartCountdownTick, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
    }

    private void CancelAutoStartCountdown(bool silent)
    {
        if (autoStartCountdownTimer == null) return;

        autoStartCountdownTimer.Kill();
        autoStartCountdownTimer = null;
        autoStartRemainingSeconds = 0;

        if (!silent)
        {
            PrintToAllChat(Localizer["matchzy.autostart.cancelled"]);
        }

        if (readyAvailable && !matchStarted && autoStartOnFullTeamsEnabled.Value)
        {
            // Auto-start is enabled but teams are no longer full; do NOT restart unready timer
            // because we don't want "please type ready" messages when auto-start is on.
            return;
        }

        if (readyAvailable && !matchStarted)
        {
            unreadyPlayerMessageTimer?.Kill();
            unreadyPlayerMessageTimer = null;
            unreadyPlayerMessageTimer ??= AddTimer(chatTimerDelay, SendUnreadyPlayersMessage, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
        }
    }

    private void AutoStartCountdownTick()
    {
        if (matchStarted || !readyAvailable)
        {
            CancelAutoStartCountdown(silent: true);
            return;
        }

        autoStartRemainingSeconds--;

        if (autoStartRemainingSeconds <= 0)
        {
            autoStartCountdownTimer?.Kill();
            autoStartCountdownTimer = null;
            TriggerAutoStart();
        }
        else if (autoStartRemainingSeconds <= 5 || autoStartRemainingSeconds == 10 || autoStartRemainingSeconds == 15 || autoStartRemainingSeconds == 30)
        {
            PrintToAllChat(Localizer["matchzy.autostart.countdown", autoStartRemainingSeconds]);
        }
    }

    private void TriggerAutoStart()
    {
        Log("[AutoStart] Triggering auto-start: force-readying both teams.");

        foreach (var key in playerData.Keys)
        {
            if (!playerData[key].IsValid) continue;
            playerReadyStatus[key] = true;
        }

        teamReadyOverride[CsTeam.CounterTerrorist] = true;
        teamReadyOverride[CsTeam.Terrorist] = true;

        CheckLiveRequired();
    }
}
