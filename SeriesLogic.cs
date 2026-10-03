namespace MatchZy
{
    public enum SeriesOutcome
    {
        Continue,
        Team1Wins,
        Team2Wins,
        Tie,
    }

    // Decides what happens after a map ends. This file must not depend on CounterStrikeSharp so that it can be unit tested (see tests/).
    public static class SeriesLogic
    {
        // mapsPlayed counts every finished map, including drawn ones (which give no series point).
        public static SeriesOutcome GetOutcomeAfterMap(int numMaps, int maplistCount, int mapsPlayed, int team1SeriesScore, int team2SeriesScore, bool seriesCanClinch)
        {
            int remainingMaps = Math.Min(numMaps, maplistCount) - mapsPlayed;
            int lead = team1SeriesScore - team2SeriesScore;
            SeriesOutcome leader = lead > 0 ? SeriesOutcome.Team1Wins : lead < 0 ? SeriesOutcome.Team2Wins : SeriesOutcome.Tie;

            if (remainingMaps <= 0) return leader;
            // The trailing team can no longer catch up.
            if (seriesCanClinch && Math.Abs(lead) > remainingMaps) return leader;
            return SeriesOutcome.Continue;
        }

        // Maps a team needs to win the series (BO1: 1, BO2: 2, BO3: 2, BO5: 3).
        public static int MapsToWin(int numMaps)
        {
            return Math.Max(1, numMaps / 2 + 1);
        }

        // The winner's series score when a series is won by forfeit: raised to the maps needed to win the series and always
        // above the loser's, so panels that judge the result by the score (G5V shows equal scores as a tie) show the win.
        public static int ForfeitWinnerSeriesScore(int numMaps, int winnerSeriesScore, int loserSeriesScore)
        {
            return Math.Max(winnerSeriesScore, Math.Max(MapsToWin(numMaps), loserSeriesScore + 1));
        }
    }
}
