namespace MatchZy
{
    // Which in-game team (CT or TERRORIST) holds each MatchZy team's map score. This file must not depend on CounterStrikeSharp
    // so that it can be unit tested (see tests/).
    public static class ScoreLogic
    {
        // MatchZy moves the teams to their new sides at the end of the last round of a half, but the game moves the scores
        // only when the next round starts. Until then (round end and halftime) each score is still on the team's old side.
        public static (string team1Side, string team2Side) ScoreSides(string team1Side, string team2Side, bool sidesSwappedBeforeGame, bool roundWon, bool halftime)
        {
            if (sidesSwappedBeforeGame && (roundWon || halftime)) return (team2Side, team1Side);
            return (team1Side, team2Side);
        }
    }
}
