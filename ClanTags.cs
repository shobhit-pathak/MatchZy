using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MatchZy;

public partial class MatchZy
{
    /// <summary>Refreshes scoreboard tags after a readiness or match phase change.</summary>
    /// <example>Call after StartLive sets isMatchLive to show configured team tags.</example>
    public void HandleClanTags()
    {
        if (!clanTagsEnabled.Value) return;
        foreach (CCSPlayerController player in playerData.Values) ApplyClanTag(player);
    }

    private void ScheduleClanTag(CCSPlayerController? player)
    {
        if (!clanTagsEnabled.Value || player is not { IsValid: true, IsBot: false, IsHLTV: false }) return;

        // Game-event values expire after the callback, so keep only the SteamID.
        ulong steamId = player!.SteamID;
        AddTimer(0.25f, () => ApplyClanTag(Utilities.GetPlayerFromSteamId64(steamId)));
    }

    private void SetPlayerClanTag(CCSPlayerController player, string clanTag)
    {
        if (!player.IsValid) return;
        player.Clan = clanTag;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
    }

    private bool IsManagedClanTag(string? clanTag)
    {
        if (string.IsNullOrEmpty(clanTag)) return false;
        return clanTag is "Ready" or "Unready"
            || (!string.IsNullOrEmpty(matchzyTeam1.teamTag) && clanTag == matchzyTeam1.teamTag)
            || (!string.IsNullOrEmpty(matchzyTeam2.teamTag) && clanTag == matchzyTeam2.teamTag);
    }

    private void ClearManagedClanTags()
    {
        foreach (CCSPlayerController player in playerData.Values)
        {
            if (player is not { IsValid: true, IsBot: false, IsHLTV: false }) continue;
            if (matchzyTeam1.coach.Contains(player) || matchzyTeam2.coach.Contains(player)) continue;
            if (!IsManagedClanTag(player.Clan)) continue;

            player.Clan = "";
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
        }
    }

    private void ApplyClanTag(CCSPlayerController? player)
    {
        if (!clanTagsEnabled.Value || player is not { IsValid: true, IsBot: false, IsHLTV: false }) return;
        if (matchzyTeam1.coach.Contains(player) || matchzyTeam2.coach.Contains(player))
        {
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
            return;
        }
        if (player.TeamNum is not (2 or 3) && !IsManagedClanTag(player.Clan)) return;

        string clanTag = player.TeamNum is 2 or 3 ? CurrentClanTag(player) : "";
        player.Clan = clanTag;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
    }

    private string CurrentClanTag(CCSPlayerController player)
    {
        if (readyAvailable && !matchStarted)
        {
            // In join ready mode nobody types .ready, so there is no ready status to show.
            if (IsJoinReadyMode()) return "";
            return player.UserId is int userId && playerReadyStatus.GetValueOrDefault(userId)
                ? "Ready" : "Unready";
        }

        if (!isMatchLive) return "";

        string steamId = player.SteamID.ToString();
        if (matchzyTeam1.teamPlayers?[steamId] != null) return matchzyTeam1.teamTag;
        if (matchzyTeam2.teamPlayers?[steamId] != null) return matchzyTeam2.teamTag;

        string side = player.TeamNum == 3 ? "CT" : "TERRORIST";
        return reverseTeamSides.TryGetValue(side, out Team? team) ? team.teamTag : "";
    }
}
