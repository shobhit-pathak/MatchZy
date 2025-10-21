using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Runtime.InteropServices;


namespace MatchZy;

public static class GrenadeFunctions
{

    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, int, CSmokeGrenadeProjectile>
        CSmokeGrenadeProjectile_CreateFunc = new(
            IsLinux
                ? @"55 4C 89 C1 48 89 E5 41 57 45 89 CF 41 56 49 89 FE"
                : @"48 8B C4 48 89 58 ? 48 89 68 ? 48 89 70 ? 57 41 56 41 57 48 81 EC ? ? ? ? 48 8B B4 24 ? ? ? ? 4D 8B F8"
        );

    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CHEGrenadeProjectile>
        CHEGrenadeProjectile_CreateFunc = new(
            IsLinux
                ? "55 4C 89 C1 48 89 E5 41 57 49 89 D7"
                : "48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 48 83 EC 50 48 8B AC 24 80 00 00 00 49 8B F8"
        );
    
    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CMolotovProjectile>
        CMolotovProjectile_CreateFunc = new(
            IsLinux
                ? "55 48 8D 05 ? ? ? ? 48 89 E5 41 57 41 56 41 55 41 54 49 89 FC 53 48 81 EC ? ? ? ? 4C 8D 35"
                : "48 8B C4 48 89 58 10 4C 89 40 18 48 89 48 08"
        );
    
    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CDecoyProjectile>
        CDecoyProjectile_CreateFunc = new(
            IsLinux
                ? "55 4C 89 C1 48 89 E5 41 57 45 89 CF 41 56 49 89 D6 48 89 F2 48 89 FE 41 55 49 89 FD 41 54 48 8D 3D ?? ?? ?? ?? 4D 89 C4 53 48 83 EC ?? E8 ?? ?? ?? ?? 45 31 C0"
                : "48 8B C4 55 56 48 81 EC 68 01 00 00"
        );
}