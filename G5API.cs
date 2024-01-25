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
        public string GameState { get; set; }
    }

    public class G5WebAvailable
    {
        [JsonPropertyName("gamestate")]
        public string GameState { get; init; }

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
            // Get state from MatchZy state phase data and map to get5 state
            // Get5 states: pre_veto, veto, warmup, knife, waiting_for_knife_decision, going_live, live, pending_restore, post_game
            // Please note, that Get5 have moved from integer based states to string based states, so the integer based states are not used.
            //
            // TODO: Missing "going_live" state, as this is not tracked. It has an event (GoingLiveEvent), but it is not read (only ever dispatched).
            //       Therefore, the `matchStarted && !isMatchLive` is used to determine if the match is going live (and no other state is "active")

            string state = "none";

            if (!isMatchSetup)
            {
                state = "none"; // If the match has not been set up, then the state is none
            }
            else if (isPreVeto)
            {
                state = "pre_veto";
            }
            else if (isVeto)
            {
                state = "veto";
            }
            else if (isWarmup)
            {
                state = "warmup";
            }
            else if (isKnifeRound)
            {
                state = "knife";
            }
            else if (isSideSelectionPhase)
            {
                state = "waiting_for_knife_decision";
            }
            else if (matchStarted && !isMatchLive)
            {
                state = "going_live";
            }
            else if (isMatchLive)
            {
                state = "live";
            }
            else if (isRoundRestoring)
            {
                state = "pending_restore";
            }
            else if (IsPostGamePhase())
            {
                state = "post_game";
            }

            command.ReplyToCommand(JsonSerializer.Serialize(new Get5Status { PluginVersion = "0.15.0", GameState = state }));
        }

        [ConsoleCommand("get5_web_available", "Returns get5 web available")]
        public void Get5WebAvailable(CCSPlayerController? player, CommandInfo command)
        {
            command.ReplyToCommand(JsonSerializer.Serialize(new G5WebAvailable()));
        }
    }
}
