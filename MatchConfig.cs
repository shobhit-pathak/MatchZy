using Newtonsoft.Json.Linq;


namespace MatchZy
{

    public class MatchConfig
    {
        public List<string> Maplist { get; set; } = new List<string>();
        public List<string> MapsPool { get; set; } = new List<string>();
        public List<string> MapsLeftInVetoPool { get; set; } = new List<string>();
        public List<string> MapBanOrder { get; set; } = new List<string>();
        public bool SkipVeto = true;
        public long MatchId { get; set; }
        public int NumMaps { get; set; } = 1;
        public int PlayersPerTeam { get; set; } = 5;
        public int MinPlayersToReady { get; set; } = 12;
        public int MinSpectatorsToReady { get; set; } = 0;
        public int CurrentMapNumber = 0;
        public List<string> MapSides { get; set; } = new List<string>();

        public bool SeriesCanClinch { get; set; } = true;
        public bool Scrim { get; set; } = false;

        public bool Wingman { get; set; } = false;

        public string MatchSideType { get; set; } = "standard";

        public Dictionary<string, string> ChangedCvars = new();

        public Dictionary<string, string> OriginalCvars = new();
        public JToken? Spectators;
        public string RemoteLogURL = "";
        public string RemoteLogHeaderKey = "";
        public string RemoteLogHeaderValue = "";
    }
}
