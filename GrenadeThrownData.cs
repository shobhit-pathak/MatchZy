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

    public UInt16 ItemIndex { get; set; }

    public GrenadeThrownData(Vector nadePosition, QAngle nadeAngle, Vector nadeVelocity, Vector playerPosition, QAngle playerAngle, string grenadeType, DateTime thrownTime, UInt16 itemIndex)
    {
        Position = new Vector(nadePosition.X, nadePosition.Y, nadePosition.Z);
        Angle = new QAngle(nadeAngle.X, nadeAngle.Y, nadeAngle.Z);
        Velocity = new Vector(nadeVelocity.X, nadeVelocity.Y, nadeVelocity.Z);
        PlayerPosition = new Vector(playerPosition.X, playerPosition.Y, playerPosition.Z);
        PlayerAngle = new QAngle(playerAngle.X, playerAngle.Y, playerAngle.Z);
        Type = grenadeType;
        ThrownTime = thrownTime;
        Delay = 0;
        ItemIndex = itemIndex;
    }

    public void LoadPosition(CCSPlayerController player)
    {
        if (player == null || player.PlayerPawn.Value == null) return;
        player.PlayerPawn.Value.Teleport(PlayerPosition, PlayerAngle, new Vector(0, 0, 0));
    }

    public void Throw(CCSPlayerController player)
    {
		CBaseCSGrenadeProjectile? grenadeEntity = null;
		switch (Type)
		{
			case "smoke":
			{
				grenadeEntity = GrenadeFunctions.CSmokeGrenadeProjectile_CreateFunc.Invoke(
					Position.Handle,
					Angle.Handle,
					Velocity.Handle,
					Velocity.Handle,
					IntPtr.Zero,
					ItemIndex,
					(int)player.Team);
				break;
			}
			case "molotov":
			{
				grenadeEntity = GrenadeFunctions.CMolotovProjectile_CreateFunc.Invoke(
					Position.Handle,
					Angle.Handle,
					Velocity.Handle,
					Velocity.Handle,
					IntPtr.Zero,
					ItemIndex);
				break;
			}
			case "hegrenade":
			{
				grenadeEntity = GrenadeFunctions.CHEGrenadeProjectile_CreateFunc.Invoke(
					Position.Handle,
					Angle.Handle,
					Velocity.Handle,
					Velocity.Handle,
					IntPtr.Zero,
					ItemIndex);
				break;
			}
			case "decoy":
			{
				grenadeEntity = GrenadeFunctions.CDecoyProjectile_CreateFunc.Invoke(
					Position.Handle,
					Angle.Handle,
					Velocity.Handle,
					Velocity.Handle,
					IntPtr.Zero,
					ItemIndex);
				break;
			}
			case "flash":
			{
				grenadeEntity = Utilities.CreateEntityByName<CFlashbangProjectile>("flashbang_projectile");
				if (grenadeEntity == null) return;
				grenadeEntity.DispatchSpawn();
				break;
			}
			default:
				Console.WriteLine($"[MatchZy] Unknown Grenade: {Type}");
				break;
		}

		if (grenadeEntity != null && grenadeEntity.DesignerName != "smokegrenade_projectile")
		{
			grenadeEntity.InitialPosition.X = Position.X;
			grenadeEntity.InitialPosition.Y = Position.Y;
			grenadeEntity.InitialPosition.Z = Position.Z;

			grenadeEntity.InitialVelocity.X = Velocity.X;
			grenadeEntity.InitialVelocity.Y = Velocity.Y;
			grenadeEntity.InitialVelocity.Z = Velocity.Z;

			grenadeEntity.AngVelocity.X = Velocity.X;
			grenadeEntity.AngVelocity.Y = Velocity.Y;
			grenadeEntity.AngVelocity.Z = Velocity.Z;

            grenadeEntity.Teleport(Position, Angle, Velocity);
            grenadeEntity.Globalname = "custom";
            grenadeEntity.TeamNum = player.TeamNum;
            grenadeEntity.Thrower.Raw = player.PlayerPawn.Raw;
            grenadeEntity.OriginalThrower.Raw = player.PlayerPawn.Raw;
            grenadeEntity.OwnerEntity.Raw = player.PlayerPawn.Raw;
		}
    }
}
