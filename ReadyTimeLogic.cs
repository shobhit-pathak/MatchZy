namespace MatchZy
{
    public enum ReadyTimeOutcome
    {
        // Both teams are ready (the match starts through the ready system) or there is no time limit.
        Ready,
        Waiting,
        Expired,
    }

    public enum JoinCountdownStep
    {
        None,      // not everyone has joined, no countdown
        Started,   // everyone has joined, the countdown starts
        Running,
        Stopped,   // a player left during the countdown
        Finished,  // start the match
    }

    // Get5's get5_time_to_start / get5_time_to_start_veto (CheckReadyWaitingTimes in get5.sp): teams have a number of seconds to
    // ready up; when it runs out, a team that is not ready forfeits, or the series ends in a tie if neither is ready.
    // This file must not depend on CounterStrikeSharp so that it can be unit tested (see tests/).
    public static class ReadyTimeLogic
    {
        // secondsUsed already includes the current second (Get5 counts it before checking).
        public static ReadyTimeOutcome Check(bool team1Ready, bool team2Ready, int timeLimit, int secondsUsed)
        {
            if (team1Ready && team2Ready) return ReadyTimeOutcome.Ready;
            if (timeLimit <= 0) return ReadyTimeOutcome.Ready;
            return timeLimit - secondsUsed > 0 ? ReadyTimeOutcome.Waiting : ReadyTimeOutcome.Expired;
        }

        // The team that wins by forfeit when the time runs out: 1 or 2, or 0 when neither team is ready (a tie).
        public static int ForfeitWinner(bool team1Ready, bool team2Ready)
        {
            if (team1Ready && !team2Ready) return 1;
            if (team2Ready && !team1Ready) return 2;
            return 0;
        }

        // Get5's reminders: every minute while 5 minutes or more are left, every 30 seconds below that, and at 10 seconds.
        public static bool ShouldWarn(int timeLeft)
        {
            if (timeLeft <= 0) return false;
            return (timeLeft >= 300 && timeLeft % 60 == 0) || (timeLeft < 300 && timeLeft % 30 == 0) || timeLeft == 10;
        }

        // Join mode (matchzy_ready_mode 1): a team is ready once enough of its players have joined, no .ready needed.
        public static bool IsTeamComplete(int joinedPlayers, int minPlayersToReady)
        {
            return joinedPlayers > 0 && joinedPlayers >= minPlayersToReady;
        }

        // One second of the join mode start countdown. secondsLeft is null while no countdown runs.
        public static (int? SecondsLeft, JoinCountdownStep Step) NextJoinCountdown(int? secondsLeft, bool everyoneJoined, int delay)
        {
            if (!everyoneJoined) return (null, secondsLeft != null ? JoinCountdownStep.Stopped : JoinCountdownStep.None);
            if (secondsLeft == null) return delay <= 0 ? (null, JoinCountdownStep.Finished) : (delay, JoinCountdownStep.Started);
            int next = secondsLeft.Value - 1;
            return next <= 0 ? (null, JoinCountdownStep.Finished) : (next, JoinCountdownStep.Running);
        }

        // Countdown reminders after the start announcement: every 10 seconds, and each of the last 5.
        public static bool ShouldAnnounceJoinCountdown(int secondsLeft)
        {
            return secondsLeft > 0 && (secondsLeft <= 5 || secondsLeft % 10 == 0);
        }

        // "4:30", "0:10"
        public static string FormatTime(int seconds)
        {
            if (seconds < 0) seconds = 0;
            return $"{seconds / 60}:{seconds % 60:D2}";
        }
    }
}
