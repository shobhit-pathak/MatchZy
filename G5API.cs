using System.Text.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;

namespace MatchZy
{
    public class Get5Status
    {
        [JsonPropertyName("plugin_version")]
        public required string PluginVersion { get; set; }

        [JsonPropertyName("gamestate")]
        public required string GameState { get; set; }

        [JsonPropertyName("paused")]
        public bool Paused { get; set; } = false;

        [JsonPropertyName("loaded_config_file")]
        public string? LoadedConfigFile { get; set; } = null;

        [JsonPropertyName("matchid")]
        public long? MatchId { get; set; } = null;

        [JsonPropertyName("map_number")]
        public int? MapNumber { get; set; } = null;

        [JsonPropertyName("round_number")]
        public int? RoundNumber { get; set; } = -1;

        [JsonPropertyName("round_time")]
        public int? RoundTime { get; set; } = null;

        [JsonPropertyName("team1")]
        public Get5StatusTeam? Team1 { get; set; } = null;

        [JsonPropertyName("team2")]
        public Get5StatusTeam? Team2 { get; set; } = null;

        [JsonPropertyName("maps")]
        public string[]? Maps { get; set; } = null;
    }

    public class Get5StatusTeam
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("series_score")]
        public required int SeriesScore { get; set; } = 0;

        [JsonPropertyName("current_map_score")]
        public required int CurrentMapScore { get; set; } = 0;

        [JsonPropertyName("connected_clients")]
        public required int ConnectedClients { get; set; } = 0;

        [JsonPropertyName("ready")]
        public required bool Ready { get; set; } = false;

        [JsonPropertyName("side")]
        public required string Side { get; set; }
    }

    public enum Get5GameState : int
    {
        None = 0,
        PreVeto = 1,
        Veto = 2,
        Warmup = 3,
        Knife = 4,
        WaitingForKnifeDecision = 5,
        GoingLive = 6,
        Live = 7,
        PendingRestore = 8,
        PostGame = 9
    }

    public class G5WebAvailable
    {
        [JsonPropertyName("gamestate")]
        public required int GameState { get; init; }

        [JsonPropertyName("available")]
        public int Available { get; } = 1;

        [JsonPropertyName("plugin_version")]
        public string PluginVersion { get; } = "0.15.0";
    }

    public partial class MatchZy
    {
        [ConsoleCommand("get5_status", "Returns get5 status")]
        public void Get5StatusCommand(CCSPlayerController? player, CommandInfo command)
        {
            // TODO: Add remaining Get5 status data as specified in https://splewis.github.io/get5/latest/commands/#get5_status
            //       The missing attributes are:
            //       - "matchid"    - It is currently implemented to return long and correspunds to the "liveMatchId".
            //                        However, it does not return the correct values when in scrim or manual mode.
//       - "round_time" - This is not implemented, as it is not currently tracked by the plugin. Currently it returns Null.

            string PluginVersion = "0.15.0";

            Get5GameState gamestate = getGet5Gamestate();
            string gamestateString = mapGet5GameState(gamestate);
            Get5Status get5Status = new Get5Status
            {
                PluginVersion = PluginVersion,
                GameState = gamestateString,
                Paused = isPaused
            };

            if (gamestate != Get5GameState.None)
            {
                get5Status.LoadedConfigFile = loadedConfigFile;
                get5Status.MatchId = liveMatchId;
                get5Status.MapNumber = matchConfig.CurrentMapNumber;
            }

            if (isMatchSetup)
            {
                (int team1, int team2) = GetTeamsScore();
                get5Status.Team1 = GetGet5StatusTeam(matchzyTeam1, team1);
                get5Status.Team2 = GetGet5StatusTeam(matchzyTeam2, team2);
            }

            if (gamestate >= Get5GameState.GoingLive)
            {
                get5Status.Maps = matchConfig.Maplist.ToArray();
            }

            if (gamestate == Get5GameState.Live)
            {
                get5Status.RoundNumber = GetRoundNumer();
            }

            command.ReplyToCommand(JsonSerializer.Serialize(get5Status));
        }

        // Per team, as Get5 reports it: side "ct" / "t", whether that team is ready, and how many of its players are connected.
        private Get5StatusTeam GetGet5StatusTeam(Team team, int mapScore)
        {
            int side = teamSides[team] == "CT" ? (int)CsTeam.CounterTerrorist : (int)CsTeam.Terrorist;
            // Players only, as Get5 counts them (coaches are on the side too).
            int connectedPlayers = playerData.Values.Count(p => p.IsValid && !p.IsBot && p.TeamNum == side && !team.coach.Contains(p));
            return new Get5StatusTeam
            {
                Name = team.teamName,
                SeriesScore = team.seriesScore,
                CurrentMapScore = mapScore,
                ConnectedClients = connectedPlayers,
                Ready = IsTeamReady(side, log: false),
                Side = side == (int)CsTeam.CounterTerrorist ? "ct" : "t"
            };
        }

        [ConsoleCommand("get5_web_available", "Returns get5 web available")]
        public void Get5WebAvailable(CCSPlayerController? player, CommandInfo command)
        {
            command.ReplyToCommand(JsonSerializer.Serialize(new G5WebAvailable { GameState = (int) getGet5Gamestate() }));
        }

        private Get5GameState getGet5Gamestate()
        {
            // Get state from MatchZy state phase data and map to get5 state
            // Get5 states: pre_veto, veto, warmup, knife, waiting_for_knife_decision, going_live, live, pending_restore, post_game
            // Please note, that Get5 have moved from integer based states to string based states, so the integer based states are not used.
            //
            // TODO: Missing "going_live" state, as this is not tracked. It has an event (GoingLiveEvent), but it is not read (only ever dispatched).
            //       Therefore, a "proxy" is used to determine if the match is going live (and no other state is "active")
            Get5GameState state = Get5GameState.None;

            // The order of checks have been checked to work. Please be carefull if you change the order.
            if (!isMatchSetup)
            {
                state = Get5GameState.None; // If the match has not been set up, then the state is none
            }
            else if (isVeto)
            {
                state = Get5GameState.Veto;
            }
            else if (isPreVeto)
            {
                state = Get5GameState.PreVeto;
            }
            else if (isKnifeRound)
            {
                state = Get5GameState.Knife;
            }
            else if (isSideSelectionPhase)
            {
                state = Get5GameState.WaitingForKnifeDecision;
            }
            else if (IsPostGamePhase())
            {
                state = Get5GameState.PostGame;
            }
            else if (isMatchLive)
            {
                state = Get5GameState.Live;
            }
            else if (isRoundRestoring)
            {
                state = Get5GameState.PendingRestore;
            }
            else if (matchStarted)
            {
                state = Get5GameState.Live;
            }
            else if (isWarmup)
            {
                state = Get5GameState.Warmup;
            }

            return state;
        }

        private string mapGet5GameState(Get5GameState state)
        {
            switch(state)
            {
                case Get5GameState.None:
                    return "none";
                case Get5GameState.PreVeto:
                    return "pre_veto";
                case Get5GameState.Veto:
                    return "veto";
                case Get5GameState.Warmup:
                    return "warmup";
                case Get5GameState.Knife:
                    return "knife";
                case Get5GameState.WaitingForKnifeDecision:
                    return "waiting_for_knife_decision";
                case Get5GameState.GoingLive:
                    return "going_live";
                case Get5GameState.Live:
                    return "live";
                case Get5GameState.PendingRestore:
                    return "pending_restore";
                case Get5GameState.PostGame:
                    return "post_game";
                default:
                    return "none";
            }
        }
    }
}
