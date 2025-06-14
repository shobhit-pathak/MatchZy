using CounterStrikeSharp.API.Modules.Utils;

namespace MatchZy;

public partial class MatchZy
{
// 示例方法：将所有玩家移动到观察者模式
    public void MoveAllPlayersToSpectators()
    {
        // 获取所有在线玩家
        var allPlayers = playerData;

        foreach (var player in allPlayers.Values)
        {
            if (player != null && player.IsValid)
            {
                // 将玩家移动到观察者模式
                player.ChangeTeam(CsTeam.Spectator);
            }
        }
    }
    
}