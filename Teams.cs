using CounterStrikeSharp.API.Core;


namespace MatchZy
{

    public class Team 
    {
        public required string teamName;
        public string teamFlag = "";
        public string teamTag = "";

        List<CCSPlayerController> teamPlayers = new List<CCSPlayerController>();
    }

    public partial class MatchZy
    {
        // Todo: Organize Teams code which can be later used for setting up matches

    }
}
