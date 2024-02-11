using System.Text.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using System.Text.Json.Serialization;

namespace MatchZy
{
    public class Get5Status
    {
        [JsonPropertyName("plugin_version")]
        public required string PluginVersion { get; set; }

        [JsonPropertyName("gamestate")]
        public required string GameState { get; set; }

        [JsonPropertyName("matchid")]
        public required string MatchId { get; set; }
    }

    public class G5WebAvailable
    {
        [JsonPropertyName("gamestate")]
        public int GameState { get; init; }

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
            command.ReplyToCommand(JsonSerializer.Serialize(new Get5Status { PluginVersion = "0.15.0", GameState = GetGet5Gamestate(), MatchId = liveMatchId.ToString() }));
        }

        [ConsoleCommand("get5_web_available", "Returns get5 web available")]
        public void Get5WebAvailable(CCSPlayerController? player, CommandInfo command)
        {
            command.ReplyToCommand(JsonSerializer.Serialize(new G5WebAvailable() {GameState = GetGet5Gamestate() == "none" ? 0 : 1}));
        }

        private string GetGet5Gamestate()
        {
            // Get state from MatchZy state phase data and map to get5 state
            // Get5 states: pre_veto, veto, warmup, knife, waiting_for_knife_decision, going_live, live, pending_restore, post_game
            // Please note, that Get5 have moved from integer based states to string based states, so the integer based states are not used.
            //
            // TODO: Missing "going_live" state, as this is not tracked. It has an event (GoingLiveEvent), but it is not read (only ever dispatched).
            //       Therefore, a "proxy" is used to determine if the match is going live (and no other state is "active")
            string state = "none";

            // The order of checks have been checked to work. Please be carefull if you change the order.
            if (!isMatchSetup)
            {
                state = "none"; // If the match has not been set up, then the state is none
            }
            else if (isVeto)
            {
                state = "veto";
            }
            else if (isPreVeto)
            {
                state = "pre_veto";
            }
            else if (isKnifeRound)
            {
                state = "knife";
            }
            else if (isSideSelectionPhase)
            {
                state = "waiting_for_knife_decision";
            }
            else if (IsPostGamePhase())
            {
                state = "post_game";
            }
            else if (isMatchLive)
            {
                state = "live";
            }
            else if (isRoundRestoring)
            {
                state = "pending_restore";
            }
            else if (matchStarted)
            {
                state = "going_live";
            }
            else if (isWarmup)
            {
                state = "warmup";
            }

            return state;
        }
    }
}
