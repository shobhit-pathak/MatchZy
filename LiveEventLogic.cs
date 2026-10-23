using System.Text.Json;

namespace MatchZy
{
    // Bomb plants and defuses per player (SteamID64) on the current map. The game's match stats do not count them, so they
    // are counted from the bomb events, as in Get5, and stored in round backups so a round restore rolls them back too.
    public class BombStats
    {
        private readonly Dictionary<ulong, int[]> counts = new();

        public void AddPlant(ulong steamId) => Get(steamId)[0]++;
        public void AddDefuse(ulong steamId) => Get(steamId)[1]++;
        public int Plants(ulong steamId) => counts.TryGetValue(steamId, out var c) ? c[0] : 0;
        public int Defuses(ulong steamId) => counts.TryGetValue(steamId, out var c) ? c[1] : 0;
        public void Clear() => counts.Clear();

        private int[] Get(ulong steamId)
        {
            if (!counts.TryGetValue(steamId, out var c)) counts[steamId] = c = new int[2];
            return c;
        }

        // {"76561198000000000":[plants,defuses],...}
        public string ToJson() => JsonSerializer.Serialize(counts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));

        // Replaces the counts with those from a backup. Returns false (and keeps the counts) when the value is not valid.
        public bool LoadJson(string? json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, int[]>>(json);
                if (parsed == null) return false;
                var loaded = new Dictionary<ulong, int[]>();
                foreach (var (key, value) in parsed)
                {
                    if (!ulong.TryParse(key, out ulong steamId) || value == null || value.Length != 2 || value[0] < 0 || value[1] < 0) return false;
                    loaded[steamId] = new[] { value[0], value[1] };
                }
                counts.Clear();
                foreach (var (key, value) in loaded) counts[key] = value;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }

    // Helpers for the Get5 live events (player_death, bomb_planted, bomb_defused, round_start, backup_loaded), filled the way
    // Get5 fills them (stats.sp). This file must not depend on CounterStrikeSharp so that it can be unit tested (see tests/).
    public static class LiveEventLogic
    {
        // SourceMod's CSWeaponID (cstrike.inc), which Get5 sends as weapon.id. Weapons without one (e.g. the MP5-SD), fire
        // ("inferno"), the bomb ("planted_c4") and the world are 0, as in Get5.
        private static readonly Dictionary<string, int> WeaponIds = new(StringComparer.OrdinalIgnoreCase)
        {
            ["p228"] = 1, ["glock"] = 2, ["scout"] = 3, ["hegrenade"] = 4, ["xm1014"] = 5, ["c4"] = 6, ["mac10"] = 7,
            ["aug"] = 8, ["smokegrenade"] = 9, ["elite"] = 10, ["fiveseven"] = 11, ["ump45"] = 12, ["sg550"] = 13,
            ["galil"] = 14, ["famas"] = 15, ["usp"] = 16, ["awp"] = 17, ["mp5navy"] = 18, ["m249"] = 19, ["m3"] = 20,
            ["m4a1"] = 21, ["tmp"] = 22, ["g3sg1"] = 23, ["flashbang"] = 24, ["deagle"] = 25, ["sg552"] = 26, ["ak47"] = 27,
            ["knife"] = 28, ["p90"] = 29, ["shield"] = 30, ["kevlar"] = 31, ["assaultsuit"] = 32, ["nightvision"] = 33,
            ["galilar"] = 34, ["bizon"] = 35, ["mag7"] = 36, ["negev"] = 37, ["sawedoff"] = 38, ["tec9"] = 39, ["taser"] = 40,
            ["hkp2000"] = 41, ["mp7"] = 42, ["mp9"] = 43, ["nova"] = 44, ["p250"] = 45, ["scar17"] = 46, ["scar20"] = 47,
            ["sg556"] = 48, ["ssg08"] = 49, ["knifegg"] = 50, ["molotov"] = 51, ["decoy"] = 52, ["incgrenade"] = 53,
            ["defuser"] = 54, ["heavyassaultsuit"] = 55, ["cutters"] = 56, ["healthshot"] = 57, ["knife_t"] = 59,
            ["m4a1_silencer"] = 60, ["usp_silencer"] = 61, ["cz75a"] = 63, ["revolver"] = 64, ["tagrenade"] = 68,
            ["fists"] = 69, ["breachcharge"] = 70, ["tablet"] = 72, ["melee"] = 74, ["axe"] = 75, ["hammer"] = 76,
            ["spanner"] = 78, ["knife_ghost"] = 80, ["firebomb"] = 81, ["diversion"] = 82, ["frag_grenade"] = 83,
            ["snowball"] = 84, ["bumpmine"] = 85, ["bayonet"] = 500, ["knife_css"] = 503, ["knife_flip"] = 505,
            ["knife_gut"] = 506, ["knife_karambit"] = 507, ["knife_m9_bayonet"] = 508, ["knife_tactical"] = 509,
            ["knife_falchion"] = 512, ["knife_survival_bowie"] = 514, ["knife_butterfly"] = 515, ["knife_push"] = 516,
            ["knife_cord"] = 517, ["knife_canis"] = 518, ["knife_ursus"] = 519, ["knife_gypsy_jackknife"] = 520,
            ["knife_outdoor"] = 521, ["knife_stiletto"] = 522, ["knife_widowmaker"] = 523, ["knife_skeleton"] = 525,
        };

        // The weapon name as the game reports it in player_death ("ak47", "knife_t", "weapon_ak47"), and its id.
        public static int WeaponId(string? weapon)
        {
            if (string.IsNullOrEmpty(weapon)) return 0;
            string name = weapon.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase) ? weapon[7..] : weapon;
            return WeaponIds.TryGetValue(name, out int id) ? id : 0;
        }

        // Bomb kills are not suicides; dying with no attacker (falling, the world) or by your own hand is.
        public static bool IsBombKill(string? weapon) => string.Equals(weapon, "planted_c4", StringComparison.OrdinalIgnoreCase);

        public static bool IsSuicide(bool hasAttacker, bool attackerIsVictim, bool killedByBomb)
        {
            return (!hasAttacker || attackerIsVictim) && !killedByBomb;
        }

        // Get5Side: "ct", "t", "spec", or null (no team).
        public static string? SideName(int teamNum)
        {
            return teamNum switch
            {
                3 => "ct",
                2 => "t",
                1 => "spec",
                _ => null,
            };
        }

        // Get5Site from the planted bomb's site (0 = A, 1 = B); null when unknown.
        public static string? BombSiteName(int? bombSite)
        {
            return bombSite switch
            {
                0 => "a",
                1 => "b",
                _ => null,
            };
        }

        // Milliseconds left on the bomb timer when it was defused (never below 0, as in Get5).
        public static int BombTimeRemaining(int c4TimerSeconds, int millisecondsSincePlant)
        {
            return Math.Max(0, c4TimerSeconds * 1000 - millisecondsSincePlant);
        }

        // Milliseconds since freeze time ended (round_time); 0 before that.
        public static int RoundTime(double startedAt, double now)
        {
            if (startedAt <= 0) return 0;
            return (int)Math.Max(0, Math.Round((now - startedAt) * 1000));
        }

        // Get5's ip_address has no port; CounterStrikeSharp may add one ("1.2.3.4:27005"). Other values (e.g. IPv6) are kept.
        public static string IpWithoutPort(string? address)
        {
            if (string.IsNullOrEmpty(address)) return "";
            int colon = address.IndexOf(':');
            return colon >= 0 && colon == address.LastIndexOf(':') ? address[..colon] : address;
        }

        // The text of a say / say_team command: the chat sends it quoted, the console may not.
        public static string ChatMessage(string argString)
        {
            string message = argString.Trim();
            return message.Length >= 2 && message[0] == '"' && message[^1] == '"' ? message[1..^1] : message;
        }

        // Get5Player.steamid: the SteamID64, or BOT-<user id> for bots.
        public static string PlayerSteamId(ulong steamId, bool isBot, int userId)
        {
            return isBot ? $"BOT-{userId}" : steamId.ToString();
        }
    }
}
