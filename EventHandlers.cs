
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchZy;
public partial class MatchZy
{
    public HookResult EventPlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
    {
        try
        {
            Log($"[FULL CONNECT] Player ID: {@event.Userid.UserId}, Name: {@event.Userid.PlayerName} has connected!");
            CCSPlayerController player = @event.Userid;

            // Handling whitelisted players
            if (!player.IsBot || !player.IsHLTV)
            {
                var steamId = player.SteamID;

                string whitelistfileName = "MatchZy/whitelist.cfg";
                string whitelistPath = Path.Join(Server.GameDirectory + "/csgo/cfg", whitelistfileName);
                string? directoryPath = Path.GetDirectoryName(whitelistPath);
                if (directoryPath != null)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }
                if (!File.Exists(whitelistPath)) File.WriteAllLines(whitelistPath, new[] { "Steamid1", "Steamid2" });

                var whiteList = File.ReadAllLines(whitelistPath);

                if (isWhitelistRequired == true)
                {
                    if (!whiteList.Contains(steamId.ToString()))
                    {
                        Log($"[EventPlayerConnectFull] KICKING PLAYER STEAMID: {steamId}, Name: {player.PlayerName} (Not whitelisted!)");
                        KickPlayer(player);

                        return HookResult.Continue;
                    }
                }
                if (isMatchSetup || matchModeOnly)
                {
                    CsTeam team = GetPlayerTeam(player);
                    Log($"[EventPlayerConnectFull] KICKING PLAYER STEAMID: {steamId}, Name: {player.PlayerName} (NOT ALLOWED!)");
                    if (team == CsTeam.None)
                    {
                        KickPlayer(player);
                        return HookResult.Continue;
                    }
                }
            }

            if (player.UserId.HasValue)
            {
                playerData[player.UserId.Value] = player;
                connectedPlayers++;
                if (readyAvailable && !matchStarted)
                {
                    playerReadyStatus[player.UserId.Value] = false;
                }
                else
                {
                    playerReadyStatus[player.UserId.Value] = true;
                }
            }
            // May not be required, but just to be on safe side so that player data is properly updated in dictionaries
            UpdatePlayersMap();

            if (readyAvailable && !matchStarted)
            {
                // Start Warmup when first player connect and match is not started.
                if (GetRealPlayersCount() == 1)
                {
                    Log($"[FULL CONNECT] First player has connected, starting warmup!");
                    ExecUnpracCommands();
                    AutoStart();
                }
            }
            return HookResult.Continue;

        }
        catch (Exception e)
        {
            Log($"[EventPlayerConnectFull FATAL] An error occurred: {e.Message}");
            return HookResult.Continue;
        }

    }
    public HookResult EventPlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
    {
        try
        {
            CCSPlayerController player = @event.Userid;
            if (!player.UserId.HasValue) return HookResult.Continue;
            int userId = player.UserId.Value;

            if (playerReadyStatus.ContainsKey(userId))
            {
                playerReadyStatus.Remove(userId);
                connectedPlayers--;
            }
            if (playerData.ContainsKey(userId))
            {
                playerData.Remove(userId);
            }

            if (matchzyTeam1.coach == player)
            {
                matchzyTeam1.coach = null;
                player.Clan = "";
            }
            else if (matchzyTeam2.coach == player)
            {
                matchzyTeam2.coach = null;
                player.Clan = "";
            }
            if (noFlashList.Contains(userId))
            {
                noFlashList.Remove(userId);
            }
            lastGrenadesData.Remove(userId);
            nadeSpecificLastGrenadeData.Remove(userId);

            return HookResult.Continue;
        }
        catch (Exception e)
        {
            Log($"[EventPlayerDisconnect FATAL] An error occurred: {e.Message}");
            return HookResult.Continue;
        }
    }

    public HookResult EventCsWinPanelRoundHandler(EventCsWinPanelRound @event, GameEventInfo info)
    {
        // EventCsWinPanelRound has stopped firing after Arms Race update, hence we handle knife round winner in EventRoundEnd.

        // Log($"[EventCsWinPanelRound PRE] finalEvent: {@event.FinalEvent}");
        // if (isKnifeRound && matchStarted)
        // {
        //     HandleKnifeWinner(@event);
        // }
        return HookResult.Continue;
    }

    public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        try
        {
            HandleMatchEnd();
            // ResetMatch();
            return HookResult.Continue;
        }
        catch (Exception e)
        {
            Log($"[EventCsWinPanelMatch FATAL] An error occurred: {e.Message}");
            return HookResult.Continue;
        }
    }

    public HookResult EventRoundStartHandler(EventRoundStart @event, GameEventInfo info)
    {
        try
        {
            HandlePostRoundStartEvent(@event);
            return HookResult.Continue;
        }
        catch (Exception e)
        {
            Log($"[EventRoundStart FATAL] An error occurred: {e.Message}");
            return HookResult.Continue;
        }
    }

    public void OnEntitySpawnedHandler(CEntityInstance entity)
    {
        try
        {
            if (entity == null || entity.Entity == null || !isPractice) return;
            if (!Constants.ProjectileTypeMap.ContainsKey(entity.Entity.DesignerName)) return;

            Server.NextFrame(() => {
                CBaseCSGrenadeProjectile projectile = new CBaseCSGrenadeProjectile(entity.Handle);

                if (!projectile.IsValid ||
                    !projectile.Thrower.IsValid ||
                    projectile.Thrower.Value == null ||
                    projectile.Thrower.Value.Controller.Value == null ||
                    projectile.Globalname == "custom"
                ) return;

                CCSPlayerController player = new(projectile.Thrower.Value.Controller.Value.Handle);
                if(!player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) return;
                int client = player.UserId!.Value;
                
                Vector position = new(projectile.AbsOrigin!.X, projectile.AbsOrigin.Y, projectile.AbsOrigin.Z);
                QAngle angle = new(projectile.AbsRotation!.X, projectile.AbsRotation.Y, projectile.AbsRotation.Z);
                Vector velocity = new(projectile.AbsVelocity.X, projectile.AbsVelocity.Y, projectile.AbsVelocity.Z);
                string nadeType = Constants.ProjectileTypeMap[entity.Entity.DesignerName];

                if (!lastGrenadesData.ContainsKey(client)) {
                    lastGrenadesData[client] = new();
                }

                if (!nadeSpecificLastGrenadeData.ContainsKey(client))
                {
                    nadeSpecificLastGrenadeData[client] = new(){};
                }

                GrenadeThrownData lastGrenadeThrown = new(
                    position, 
                    angle, 
                    velocity, 
                    player.PlayerPawn.Value.CBodyComponent!.SceneNode!.AbsOrigin, 
                    player.PlayerPawn.Value.CBodyComponent!.SceneNode!.AbsRotation, 
                    nadeType,
                    DateTime.Now
                );

                nadeSpecificLastGrenadeData[client][nadeType] = lastGrenadeThrown;
                lastGrenadesData[client].Add(lastGrenadeThrown);

                if (maxLastGrenadesSavedLimit != 0 && lastGrenadesData[client].Count > maxLastGrenadesSavedLimit)
                {
                    lastGrenadesData[client].RemoveAt(0);
                }

                lastGrenadeThrownTime[(int)projectile.Index] = DateTime.Now;
            });
        }
        catch (Exception e)
        {
            Log($"[OnEntitySpawnedHandler FATAL] An error occurred: {e.Message}");
        }
    }

    public HookResult EventPlayerDeathPreHandler(EventPlayerDeath @event, GameEventInfo info)
    {
        try
        {
            // We do not broadcast the suicide of the coach
            if (!matchStarted) return HookResult.Continue;

            if (@event.Attacker == @event.Userid)
            {
                if (matchzyTeam1.coach == @event.Attacker || matchzyTeam2.coach == @event.Attacker)
                {
                    info.DontBroadcast = true;
                }
            }
    
            return HookResult.Continue;
        }
        catch (Exception e)
        {
            Log($"[EventPlayerDeathPreHandler FATAL] An error occurred: {e.Message}");
            return HookResult.Continue;
        }
    }

    public HookResult EventRoundFreezeEndHandler(EventRoundFreezeEnd @event, GameEventInfo info)
    {
        try
        {
            HandlePostRoundFreezeEndEvent(@event);
            return HookResult.Continue;
        }
        catch (Exception e)
        {
            Log($"[EventRoundFreezeEnd FATAL] An error occurred: {e.Message}");
            return HookResult.Continue;
        }
    }

    public HookResult EventSmokegrenadeDetonateHandler(EventSmokegrenadeDetonate @event, GameEventInfo info)
    {
        if (!isPractice || isDryRun) return HookResult.Continue;
        if(lastGrenadeThrownTime.TryGetValue(@event.Entityid, out var thrownTime)) 
        {
            PrintToPlayerChat(@event.Userid, $"Smoke thrown by {@event.Userid.PlayerName} took {(DateTime.Now - thrownTime).TotalSeconds:0.00}s to detonate");
            lastGrenadeThrownTime.Remove(@event.Entityid);
        }
        return HookResult.Continue;
    }

    public HookResult EventFlashbangDetonateHandler(EventFlashbangDetonate @event, GameEventInfo info)
    {
        if (!isPractice || isDryRun) return HookResult.Continue;
        if(lastGrenadeThrownTime.TryGetValue(@event.Entityid, out var thrownTime)) 
        {
            PrintToPlayerChat(@event.Userid, $"Flash thrown by {@event.Userid.PlayerName} took {(DateTime.Now - thrownTime).TotalSeconds:0.00}s to detonate");
            lastGrenadeThrownTime.Remove(@event.Entityid);
        }
        return HookResult.Continue;
    }

    public HookResult EventHegrenadeDetonateHandler(EventHegrenadeDetonate @event, GameEventInfo info)
    {
        if (!isPractice || isDryRun) return HookResult.Continue;
        if(lastGrenadeThrownTime.TryGetValue(@event.Entityid, out var thrownTime)) 
        {
            PrintToPlayerChat(@event.Userid, $"Grenade thrown by {@event.Userid.PlayerName} took {(DateTime.Now - thrownTime).TotalSeconds:0.00}s to detonate");
            lastGrenadeThrownTime.Remove(@event.Entityid);
        }
        return HookResult.Continue;
    }


    public HookResult EventMolotovDetonateHandler(EventMolotovDetonate @event, GameEventInfo info)
    {
        if (!isPractice || isDryRun) return HookResult.Continue;
        if(lastGrenadeThrownTime.TryGetValue(@event.Get<int>("entityid"), out var thrownTime)) 
        {
            PrintToPlayerChat(@event.Userid, $"Molotov thrown by {@event.Userid.PlayerName} took {(DateTime.Now - thrownTime).TotalSeconds:0.00}s to detonate");
            lastGrenadeThrownTime.Remove(@event.Get<int>("entityid"));
        }
        return HookResult.Continue;
    }

    public HookResult EventDecoyDetonateHandler(EventDecoyDetonate @event, GameEventInfo info)
    {
        if (!isPractice || isDryRun) return HookResult.Continue;
        if(lastGrenadeThrownTime.TryGetValue(@event.Entityid, out var thrownTime)) 
        {
            PrintToPlayerChat(@event.Userid, $"Decoy thrown by {@event.Userid.PlayerName} took {(DateTime.Now - thrownTime).TotalSeconds:0.00}s to detonate");
            lastGrenadeThrownTime.Remove(@event.Entityid);
        }
        return HookResult.Continue;
    }
}
