using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchZy;

public class PlayerLocationData
{
    public Vector Position { get; set; }
    public QAngle Angle { get; set; }

    public PlayerLocationData(Vector position, QAngle angle)
    {
        this.Position = position;
        this.Angle = angle;
    }
    
    public void LoadPosition(CCSPlayerController player)
    {
        PlayerTeleport.TeleportUpright(player, Position, Angle);
    }
}