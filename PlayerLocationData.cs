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
        if (player == null || player.PlayerPawn.Value == null) return;
        player.PlayerPawn.Value.Teleport(Position, Angle, new Vector(0, 0, 0));
    }
}