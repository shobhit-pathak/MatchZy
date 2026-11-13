using System.Text.Json.Serialization;

namespace MatchZy;
public class MatchZyEvent
{
    public MatchZyEvent(string eventName)
    {
        EventName = eventName;
    }

    [JsonPropertyName("event")]
    public string EventName { get; }
}

public class MatchZyMatchEvent : MatchZyEvent
{
    [JsonPropertyName("matchid")]
    public required long MatchId { get; init; }

    protected MatchZyMatchEvent(string eventName) : base(eventName)
    {
    }
}

public class MatchZyMatchTeamEvent : MatchZyMatchEvent
{
    [JsonPropertyName("team")]
    public required string Team { get; init; }

    protected MatchZyMatchTeamEvent(string eventName) : base(eventName)
    {
    }
}

public class MatchZyMapEvent : MatchZyMatchEvent
{
    [JsonPropertyName("map_number")]
    public required int MapNumber { get; init; }

    protected MatchZyMapEvent(string eventName) : base(eventName)
    {
    }
}

public class MatchZyMapTeamEvent : MatchZyMapEvent
{
    [JsonPropertyName("team_int")]
    public required int TeamNumber { get; init; }

    protected MatchZyMapTeamEvent(string eventName) : base(eventName)
    {
    }
}

public class MatchZyRoundEvent : MatchZyMapEvent
{
    [JsonPropertyName("round_number")]
    public required int RoundNumber { get; init; }

    protected MatchZyRoundEvent(string eventName) : base(eventName)
    {
    }
}

public class MatchZyTimedRoundEvent : MatchZyRoundEvent
{
    [JsonPropertyName("round_time")]
    public required int RoundTime { get; init; }

    protected MatchZyTimedRoundEvent(string eventName) : base(eventName)
    {
    }
}

public class MatchZyPlayerRoundEvent : MatchZyRoundEvent
{

    [JsonPropertyName("player")]
    public required MatchZyPlayer Player { get; init; }

    protected MatchZyPlayerRoundEvent(string eventName) : base(eventName)
    {
    }
}

public class MatchZyPlayerTimedRoundEvent : MatchZyTimedRoundEvent
{
    [JsonPropertyName("player")]
    public required MatchZyPlayer Player { get; init; }

    protected MatchZyPlayerTimedRoundEvent(string eventName) : base(eventName)
    {
    }
}

// Get5Player: a player in the live events.
public class MatchZyPlayer
{
    // SteamID64, or BOT-<user_id> for bots.
    [JsonPropertyName("steamid")]
    public required string SteamId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("user_id")]
    public required int UserId { get; init; }

    // "ct", "t", "spec", or null.
    [JsonPropertyName("side")]
    public string? Side { get; init; }

    [JsonPropertyName("is_bot")]
    public required bool IsBot { get; init; }
}

// Get5Weapon: the game's weapon name and SourceMod's weapon id (0 when it has none).
public class MatchZyWeapon
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("id")]
    public required int Id { get; init; }
}

// Get5AssisterObject
public class MatchZyAssist
{
    [JsonPropertyName("player")]
    public required MatchZyPlayer Player { get; init; }

    [JsonPropertyName("friendly_fire")]
    public required bool FriendlyFire { get; init; }

    [JsonPropertyName("flash_assist")]
    public required bool FlashAssist { get; init; }
}

// Get5PlayerDeathEvent. player is the victim; attacker and assist are null when there is none.
public class MatchZyPlayerDeathEvent : MatchZyPlayerTimedRoundEvent
{
    [JsonPropertyName("weapon")]
    public required MatchZyWeapon Weapon { get; init; }

    [JsonPropertyName("bomb")]
    public required bool Bomb { get; init; }

    [JsonPropertyName("headshot")]
    public required bool Headshot { get; init; }

    [JsonPropertyName("thru_smoke")]
    public required bool ThruSmoke { get; init; }

    // Number of objects (players or walls) the bullet went through.
    [JsonPropertyName("penetrated")]
    public required int Penetrated { get; init; }

    [JsonPropertyName("attacker_blind")]
    public required bool AttackerBlind { get; init; }

    [JsonPropertyName("no_scope")]
    public required bool NoScope { get; init; }

    [JsonPropertyName("suicide")]
    public required bool Suicide { get; init; }

    [JsonPropertyName("friendly_fire")]
    public required bool FriendlyFire { get; init; }

    [JsonPropertyName("attacker")]
    public MatchZyPlayer? Attacker { get; init; }

    [JsonPropertyName("assist")]
    public MatchZyAssist? Assist { get; init; }

    public MatchZyPlayerDeathEvent() : base("player_death")
    {
    }
}

// Get5PlayerBombEvent: bomb_planted / bomb_defused. site is "a", "b" or null.
public class MatchZyBombEvent : MatchZyPlayerTimedRoundEvent
{
    [JsonPropertyName("site")]
    public string? Site { get; init; }

    public MatchZyBombEvent(string eventName) : base(eventName)
    {
    }
}

public class MatchZyBombDefusedEvent : MatchZyBombEvent
{
    // Milliseconds left on the bomb timer.
    [JsonPropertyName("bomb_time_remaining")]
    public required int BombTimeRemaining { get; init; }

    public MatchZyBombDefusedEvent() : base("bomb_defused")
    {
    }
}

// round_start: when freeze time begins.
public class MatchZyRoundStartedEvent : MatchZyRoundEvent
{
    public MatchZyRoundStartedEvent() : base("round_start")
    {
    }
}

// backup_loaded: round_number is the round restored to.
public class MatchZyBackupRestoredEvent : MatchZyRoundEvent
{
    [JsonPropertyName("filename")]
    public required string FileName { get; init; }

    public MatchZyBackupRestoredEvent() : base("backup_loaded")
    {
    }
}

// Get5PlayerDisconnectedEvent
public class MatchZyPlayerDisconnectedEvent : MatchZyMatchEvent
{
    [JsonPropertyName("player")]
    public required MatchZyPlayer Player { get; init; }

    public MatchZyPlayerDisconnectedEvent() : base("player_disconnect")
    {
    }
}

// Get5PlayerConnectedEvent
public class MatchZyPlayerConnectedEvent : MatchZyMatchEvent
{
    [JsonPropertyName("player")]
    public required MatchZyPlayer Player { get; init; }

    [JsonPropertyName("ip_address")]
    public required string IpAddress { get; init; }

    public MatchZyPlayerConnectedEvent() : base("player_connect")
    {
    }
}

// Get5PlayerSayEvent: command is "say" or "say_team".
public class MatchZyPlayerSayEvent : MatchZyPlayerTimedRoundEvent
{
    [JsonPropertyName("command")]
    public required string Command { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    public MatchZyPlayerSayEvent() : base("player_say")
    {
    }
}

public class MatchZySeriesStartedEvent : MatchZyMatchEvent
{
    [JsonPropertyName("team1")]
    public required MatchZyTeamWrapper Team1 { get; init; }

    [JsonPropertyName("team2")]
    public required MatchZyTeamWrapper Team2 { get; init; }

    [JsonPropertyName("num_maps")]
    public required int NumberOfMaps { get; init; }

    public MatchZySeriesStartedEvent() : base("series_start")
    {
    }
}

// game_paused / game_unpaused, as in Get5.
public class MatchZyPauseEvent : MatchZyMapEvent
{
    [JsonPropertyName("team")]
    public required string Team { get; init; }

    [JsonPropertyName("pause_type")]
    public required string PauseType { get; init; }

    public MatchZyPauseEvent(string eventName) : base(eventName)
    {
    }
}

public class MatchZySeriesResultEvent : MatchZyMatchEvent
{
    [JsonPropertyName("time_until_restore")]
    public required int TimeUntilRestore { get; init; }

    [JsonPropertyName("winner")]
    public required Winner Winner { get; init; }

    [JsonPropertyName("team1_series_score")]
    public required int Team1SeriesScore { get; init; }

    [JsonPropertyName("team2_series_score")]
    public required int Team2SeriesScore { get; init; }

    public MatchZySeriesResultEvent() : base("series_end")
    {
    }
}

public class GoingLiveEvent : MatchZyMapEvent
{
    public GoingLiveEvent() : base("going_live")
    {
    }
}

public class MatchZyRoundEndedEvent : MatchZyTimedRoundEvent
{

    [JsonPropertyName("reason")]
    public required int Reason { get; init; }

    [JsonPropertyName("winner")]
    public required Winner Winner { get; init; }

    [JsonPropertyName("team1")]
    public required MatchZyStatsTeam StatsTeam1 { get; init; }

    [JsonPropertyName("team2")]
    public required MatchZyStatsTeam StatsTeam2 { get; init; }

    public MatchZyRoundEndedEvent() : base("round_end")
    {
    }
}

public class MapResultEvent : MatchZyMapEvent
{
    [JsonPropertyName("winner")]
    public required Winner Winner { get; init; }

    [JsonPropertyName("team1")]
    public required MatchZyStatsTeam StatsTeam1 { get; init; }

    [JsonPropertyName("team2")]
    public required MatchZyStatsTeam StatsTeam2 { get; init; }

    public MapResultEvent() : base("map_result")
    {
    }
}

public class MatchZyMapSelectionEvent : MatchZyMatchTeamEvent
{
    [JsonPropertyName("map_name")]
    public required string MapName { get; init; }

    protected MatchZyMapSelectionEvent(string eventName) : base(eventName)
    {
    }
}

public class MatchZyMapPickedEvent : MatchZyMapSelectionEvent
{
    [JsonPropertyName("map_number")]
    public required int MapNumber { get; init; }

    public MatchZyMapPickedEvent() : base("map_picked")
    {
    }
}

public class MatchZyMapVetoedEvent : MatchZyMapSelectionEvent
{
    public MatchZyMapVetoedEvent() : base("map_vetoed")
    {
    }
}

public class MatchZySidePickedEvent : MatchZyMapSelectionEvent
{
    [JsonPropertyName("map_number")]
    public required int MapNumber { get; init; }

    [JsonPropertyName("side")]
    public required string Side { get; init; }

    public MatchZySidePickedEvent() : base("side_picked")
    {
    }
}

public class MatchZyDemoUploadedEvent : MatchZyMatchEvent
{
    [JsonPropertyName("map_number")]
    public required int MapNumber { get; init; }

    [JsonPropertyName("filename")]
    public required string FileName { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    public MatchZyDemoUploadedEvent() : base("demo_upload_ended")
    {
    }
}