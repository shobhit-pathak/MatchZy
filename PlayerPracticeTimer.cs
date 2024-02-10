using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchZy;

public enum PracticeTimerType
{
    OnMovement,
    Immediate
}

public class PlayerPracticeTimer
{
    public DateTime StartTime { get; set; }

    public PracticeTimerType TimerType { get; set; }

    public CounterStrikeSharp.API.Modules.Timers.Timer? Timer { get; set; }

    public PlayerPracticeTimer(PracticeTimerType timerType)
    {
        TimerType = timerType;
    }

    public void DisplayTimerCenter(CCSPlayerController player)
    {
        player.PrintToCenter($"Timer: {GetTimerResult()}s");
    }

    public double GetTimerResult()
    {
        double totalSeconds = (DateTime.Now - StartTime).TotalSeconds;
        return Math.Round(totalSeconds, 2);
    }

    public void KillTimer()
    {
        Timer?.Kill();
    }
}
