namespace MatchZy
{
    // The kind of pause, with Get5's names (sent as pause_type in game_paused / game_unpaused).
    public enum PauseType
    {
        None,
        Technical,
        Tactical,
        Admin,
        Backup,
    }

    // Technical pauses as in Get5 (pausing.sp): a team can call a number of them per map (0 = unlimited), and once a pause has
    // lasted the time limit (0 = none), any one team can unpause it instead of both. A pause only takes effect (and is
    // counted) at freeze time; until then the pausing team can cancel it.
    // This file must not depend on CounterStrikeSharp so that it can be unit tested (see tests/).
    public static class PauseLogic
    {
        public static bool CanCallTechPause(int pausesUsed, int maxPauses)
        {
            return maxPauses <= 0 || pausesUsed < maxPauses;
        }

        // secondsElapsed: seconds the pause has been in effect (in freeze time), -1 while it has not taken effect yet.
        // Also when the limit was lowered below the pauses already used, as in Get5.
        public static bool AnyoneCanUnpause(int secondsElapsed, int timeLimit, int pausesUsed, int maxPauses)
        {
            return (timeLimit > 0 && secondsElapsed >= timeLimit) || (maxPauses > 0 && pausesUsed > maxPauses);
        }

        // Seconds until any team can unpause, or -1 when there is no limit (both teams have to unpause).
        public static int SecondsUntilAnyoneCanUnpause(int secondsElapsed, int timeLimit, int pausesUsed, int maxPauses)
        {
            if (timeLimit <= 0 || (maxPauses > 0 && pausesUsed > maxPauses)) return -1;
            return Math.Max(0, timeLimit - Math.Max(0, secondsElapsed));
        }

        // " (1/2)" when there is a limit, "" when tech pauses are unlimited.
        public static string PauseCountSuffix(int pauseNumber, int maxPauses)
        {
            return maxPauses > 0 ? $" ({pauseNumber}/{maxPauses})" : "";
        }

        public static string ToEventName(PauseType type)
        {
            return type switch
            {
                PauseType.Technical => "technical",
                PauseType.Tactical => "tactical",
                PauseType.Admin => "admin",
                PauseType.Backup => "backup",
                _ => "none",
            };
        }
    }
}
