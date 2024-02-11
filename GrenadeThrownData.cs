using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchZy;
public class GrenadeThrownData
{
    public Vector Position { get; private set; }

    public QAngle Angle { get; private set; }

    public Vector Velocity { get; private set; }

    public Vector PlayerPosition { get; private set; }

    public QAngle PlayerAngle { get; private set; }

    public string Type { get; private set; }

    public DateTime ThrownTime { get; private set; }

    public float Delay { get; set; }

    public GrenadeThrownData(Vector nadePosition, QAngle nadeAngle, Vector nadeVelocity, Vector playerPosition, QAngle playerAngle, string grenadeType, DateTime thrownTime)
    {
        Position = new Vector(nadePosition.X, nadePosition.Y, nadePosition.Z);
        Angle = new QAngle(nadeAngle.X, nadeAngle.Y, nadeAngle.Z);
        Velocity = new Vector(nadeVelocity.X, nadeVelocity.Y, nadeVelocity.Z);
        PlayerPosition = new Vector(playerPosition.X, playerPosition.Y, playerPosition.Z);
        PlayerAngle = new QAngle(playerAngle.X, playerAngle.Y, playerAngle.Z);
        Type = grenadeType;
        ThrownTime = thrownTime;
        Delay = 0;
    }

    public void LoadPosition(CCSPlayerController player)
    {
        if (player == null || player.PlayerPawn.Value == null) return;
        player.PlayerPawn.Value.Teleport(PlayerPosition, PlayerAngle, new Vector(0, 0, 0));
    }

    public void Throw(CCSPlayerController player)
    {
        if (Type == "smoke")
        {
            SmokeGrenadeProjectile.Create(Position, Angle, Velocity, player);
        }
        else if (Type == "flash" || Type == "hegrenade" || Type == "decoy" || Type == "molotov")
        {
            var entity = Utilities.CreateEntityByName<CBaseCSGrenadeProjectile>(Constants.NadeProjectileMap[Type]);
            if (entity == null)
            {
                Console.WriteLine($"[GrenadeThrownData Fatal] Failed to create entity!");
                return;
            }
            if (Type == "molotov") entity.SetModel("weapons/models/grenade/incendiary/weapon_incendiarygrenade.vmdl");
            entity.Elasticity = 0.33f;
            entity.IsLive = false;
            entity.DmgRadius = 350.0f;
            entity.Damage = 99.0f;
            entity.InitialPosition.X = Position.X;
            entity.InitialPosition.Y = Position.Y;
            entity.InitialPosition.Z = Position.Z;
            entity.InitialVelocity.X = Velocity.X;
            entity.InitialVelocity.Y = Velocity.Y;
            entity.InitialVelocity.Z = Velocity.Z;
            entity.Teleport(Position, Angle, Velocity);
            entity.DispatchSpawn();
            entity.Globalname = "custom";
            entity.AcceptInput("FireUser1", player, player);
            entity.AcceptInput("InitializeSpawnFromWorld");
            entity.TeamNum = player.TeamNum;
            entity.Thrower.Raw = player.PlayerPawn.Raw;
            entity.OriginalThrower.Raw = player.PlayerPawn.Raw;
            entity.OwnerEntity.Raw = player.PlayerPawn.Raw;
        }
    }
}
