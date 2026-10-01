using Newtonsoft.Json.Linq;

namespace MatchZy
{
    // Helpers for reading Get5-style match configs. This file must not depend on CounterStrikeSharp so that it can be unit tested (see tests/).
    public static class MatchConfigJson
    {
        public static readonly string[] SideTypes = { "standard", "always_knife", "never_knife", "random" };
        public static readonly string[] VetoFirstValues = { "team1", "team2", "random" };

        // Get5 accepts true/false as well as 1/0 (as JSON values or strings).
        public static bool TryParseBool(JToken? token, out bool value)
        {
            value = false;
            if (token == null) return false;
            switch (token.Type)
            {
                case JTokenType.Boolean:
                    value = token.Value<bool>();
                    return true;
                case JTokenType.Integer:
                    long number = token.Value<long>();
                    if (number != 0 && number != 1) return false;
                    value = number == 1;
                    return true;
                case JTokenType.String:
                    string text = token.ToString().Trim().ToLowerInvariant();
                    if (text == "true" || text == "1") { value = true; return true; }
                    if (text == "false" || text == "0") { value = false; return true; }
                    return false;
                default:
                    return false;
            }
        }

        // A roster is either an object { "<steamid64>": "<name>" } or an array [ "<steamid64>", ... ] (strings or numbers).
        public static bool IsValidRoster(JToken? token)
        {
            if (token is JObject) return true;
            if (token is JArray array) return array.All(item => item.Type == JTokenType.String || item.Type == JTokenType.Integer);
            return false;
        }

        // Converts an array roster to the object form used by the rest of the plugin (names are left empty, so they are not forced).
        public static JObject NormalizeRoster(JToken? token)
        {
            if (token is JObject obj) return obj;
            JObject normalized = new();
            if (token is JArray array)
            {
                foreach (JToken item in array)
                {
                    string steamId = item.ToString().Trim();
                    if (steamId != "" && normalized[steamId] == null) normalized[steamId] = "";
                }
            }
            return normalized;
        }

        // "random" is resolved here, so the rest of the plugin only sees "team1" or "team2".
        public static string ResolveVetoFirst(string? value, Random random)
        {
            return (value ?? "team1").Trim().ToLowerInvariant() switch
            {
                "team2" => "team2",
                "random" => random.Next(0, 2) == 0 ? "team1" : "team2",
                _ => "team1",
            };
        }
    }
}
