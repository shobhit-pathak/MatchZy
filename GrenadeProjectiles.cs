using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;


namespace MatchZy;

// Native grenade projectile factories used by practice rethrows.
// The byte signatures live in gamedata/matchzy.json (installed to addons/counterstrikesharp/gamedata/), so after a CS2 update
// only that file needs updating, not the plugin. Each factory is resolved on first use; a missing key or a signature that no
// longer matches gives null, and GrenadeThrownData.Throw then creates the projectile through the entity API instead.
public static class GrenadeFunctions
{
    private static readonly HashSet<string> warnedKeys = new();

    private static T? Resolve<T>(string gameDataKey, Func<string, T> create) where T : BaseMemoryFunction
    {
        try
        {
            T function = create(GameData.GetSignature(gameDataKey));
            if (function.Handle != IntPtr.Zero) return function;
            Warn(gameDataKey, "signature not found in this CS2 build");
        }
        catch (Exception e)
        {
            Warn(gameDataKey, e.Message);
        }
        return null;
    }

    private static void Warn(string gameDataKey, string reason)
    {
        if (!warnedKeys.Add(gameDataKey)) return;
        Console.WriteLine($"[MatchZy] Gamedata key {gameDataKey} could not be resolved ({reason}). Rethrows of this grenade use the entity API instead. Make sure addons/counterstrikesharp/gamedata/matchzy.json is installed and up to date.");
    }

    private static readonly Lazy<MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, int, CSmokeGrenadeProjectile>?> smokeCreate =
        new(() => Resolve("CSmokeGrenadeProjectile_Create", sig => new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, int, CSmokeGrenadeProjectile>(sig)));

    private static readonly Lazy<MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CHEGrenadeProjectile>?> heCreate =
        new(() => Resolve("CHEGrenadeProjectile_Create", sig => new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CHEGrenadeProjectile>(sig)));

    private static readonly Lazy<MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CMolotovProjectile>?> molotovCreate =
        new(() => Resolve("CMolotovProjectile_Create", sig => new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CMolotovProjectile>(sig)));

    private static readonly Lazy<MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CDecoyProjectile>?> decoyCreate =
        new(() => Resolve("CDecoyProjectile_Create", sig => new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CDecoyProjectile>(sig)));

    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, int, CSmokeGrenadeProjectile>? CSmokeGrenadeProjectile_CreateFunc => smokeCreate.Value;
    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CHEGrenadeProjectile>? CHEGrenadeProjectile_CreateFunc => heCreate.Value;
    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CMolotovProjectile>? CMolotovProjectile_CreateFunc => molotovCreate.Value;
    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CDecoyProjectile>? CDecoyProjectile_CreateFunc => decoyCreate.Value;
}
