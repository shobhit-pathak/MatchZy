using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchZy;

public class SmokeGrenadeProjectile
{
    public static string smokeGrenadeProjectileWindowsSig = @"\x48\x89\x5C\x24\x08\x48\x89\x6C\x24\x10\x48\x89\x74\x24\x18\x57\x41\x56\x41\x57\x48\x83\xEC\x50\x4C\x8B\xB4\x24\x90\x00\x00\x00\x49\x8B\xF8";
    public static string smokeGrenadeProjectileLinuxSig = @"\x55\x4c\x89\xc1\x48\x89\xe5\x41\x57\x41\x56\x49\x89\xd6\x48\x89\xf2\x48\x89\xfe\x41\x55\x45\x89\xcd\x41\x54\x4d\x89\xc4\x53\x48\x83\xec\x28\x48\x89\x7d\xb8\x48";

    public static string smokeGrenadeProjectileSig = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? smokeGrenadeProjectileLinuxSig : smokeGrenadeProjectileWindowsSig;
    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int> CSmokeGrenadeProjectile_CreateFunc = new(smokeGrenadeProjectileSig);

    public static nint Create(Vector position, QAngle angle, Vector velocity, CCSPlayerController player)
    {
        return CSmokeGrenadeProjectile_CreateFunc.Invoke(
            position.Handle,
            angle.Handle,
            velocity.Handle,
            velocity.Handle,
            IntPtr.Zero,
            45,
            player.TeamNum
        );
    }

}