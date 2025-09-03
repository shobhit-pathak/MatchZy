using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Cvars;
using System.Text.Json;

namespace MatchZy;

public partial class MatchZy
{

    public CounterStrikeSharp.API.Modules.Timers.Timer? coachKillTimer = null;

    public HashSet<CCSPlayerController> GetAllCoaches()
    {
        HashSet<CCSPlayerController> coaches = new(matchzyTeam1.coach);
        coaches.UnionWith(matchzyTeam2.coach);

        return coaches;
    }

    public void HandleCoachCommand(CCSPlayerController? player, string side)
    {
        if (!IsPlayerValid(player)) return;
        if (isPractice)
        {
            ReplyToUserCommand(player, "Coach command can only be used in match mode!");
            return;
        }
        if (IsWingmanMode())
        {
            ReplyToUserCommand(player, "Coach command cannot be used in wingman!");
            return;
        }

        side = side.Trim().ToLower();

        if (side != "t" && side != "ct")
        {
            ReplyToUserCommand(player, "Usage: .coach t or .coach ct");
            return;
        }

        if (matchzyTeam1.coach.Contains(player!) || matchzyTeam2.coach.Contains(player!))
        {
            ReplyToUserCommand(player, "You are already coaching a team!");
            return;
        }

        Team matchZyCoachTeam;

        if (side == "t")
        {
            matchZyCoachTeam = reverseTeamSides["TERRORIST"];
        }
        else if (side == "ct")
        {
            matchZyCoachTeam = reverseTeamSides["CT"];
        }
        else
        {
            return;
        }

        // if (matchZyCoachTeam.coach != null) {
        //     ReplyToUserCommand(player, "Coach slot for this team has been already taken!");
        //     return;
        // }

        matchZyCoachTeam.coach.Add(player!);
        player!.Clan = $"[{matchZyCoachTeam.teamName} COACH]";
        if (player.InGameMoneyServices != null) player.InGameMoneyServices.Account = 0;
        ReplyToUserCommand(player, $"You are now coaching {matchZyCoachTeam.teamName}! Use .uncoach to stop coaching");
        PrintToAllChat($"{ChatColors.Green}{player.PlayerName}{ChatColors.Default} is now coaching {ChatColors.Green}{matchZyCoachTeam.teamName}{ChatColors.Default}!");
    }

    public void HandleCoaches()
    {
        coachKillTimer?.Kill();
        coachKillTimer = null;
        HashSet<CCSPlayerController> coaches = GetAllCoaches();
        if (IsWingmanMode() || coaches.Count == 0) return;
        if (spawnsData.Values.Any(list => list.Count == 0)) GetSpawns();
        if (coachSpawns.Count == 0 || 
            coachSpawns[(byte)CsTeam.CounterTerrorist].Count == 0 || 
            coachSpawns[(byte)CsTeam.Terrorist].Count == 0)
        {
            Log($"[HandleCoaches] No coach spawns found, player positions will not be swapped!");
            return;
        }

        int freezeTime = ConVar.Find("mp_freezetime")!.GetPrimitiveValue<int>();
        freezeTime = freezeTime > 2 ? freezeTime: 2;
        coachKillTimer ??= AddTimer(freezeTime - 1f, KillCoaches);

        Random random = new();
        foreach (CCSPlayerController coach in coaches)
        {
            if (!IsPlayerValid(coach)) continue;
            Team coachTeam = matchzyTeam1.coach.Contains(coach) ? matchzyTeam1 : matchzyTeam2;
            int coachTeamNum = teamSides[coachTeam] == "CT" ? 3 : 2;
            coach.InGameMoneyServices!.Account = 0;

            AddTimer(0.5f, () => HandleCoachTeam(coach));

            coach.ActionTrackingServices!.MatchStats.Kills = 0;
            coach.ActionTrackingServices!.MatchStats.Deaths = 0;
            coach.ActionTrackingServices!.MatchStats.Assists = 0;
            coach.ActionTrackingServices!.MatchStats.Damage = 0;

            SetPlayerInvisible(player: coach, setWeaponsInvisible: false);
            // Stopping the coaches from moving, so that they don't block the players.
            coach.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_NONE;
            coach.PlayerPawn.Value!.ActualMoveType = MoveType_t.MOVETYPE_NONE;

            List<Position> coachTeamSpawns = coachSpawns[coach.TeamNum];
            Position coachPosition = new(coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin, coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsRotation);

            // Picking a random position for the coach (from coachSpawns) to teleport them.
            Position newPosition = coachTeamSpawns[random.Next(0, coachTeamSpawns.Count)];

            // Elevating coach before dropping the C4 to prevent it going inside the ground.
            AddTimer(0.05f, () =>
            {
                // coach!.PlayerPawn.Value!.Teleport(new Vector(coachPosition.PlayerPosition.X, coachPosition.PlayerPosition.Y, coachPosition.PlayerPosition.Z + 20.0f), coachPosition.PlayerAngle, new Vector(0, 0, 0));
                HandleCoachWeapons(coach);
                coach!.PlayerPawn.Value.Teleport(newPosition.PlayerPosition, newPosition.PlayerAngle, new Vector(0, 0, 0));
            });

        }

        List<CCSPlayerController> players = Utilities.GetPlayers();
        HashSet<Position> occupiedSpawns = new();
        HashSet<CCSPlayerController> incorrectSpawnedPlayers = new();

        // We will loop on the players 2 times, first loop is to get all the players who are on a non-competitive spawn, and to get all the non-occupied competitive spawn.
        // In the next loop, we will teleport the non-competitive spawned players to an available competitive spawn.

        foreach (CCSPlayerController player in players)
        {
            if (!IsPlayerValid(player) || coaches.Contains(player)) continue;

            List<Position> teamPositions = spawnsData[player.TeamNum];
            Position playerPosition = new(player.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin, player.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsRotation);
            bool isCompetitiveSpawn = false;
            foreach (Position position in teamPositions)
            {
                if (position.Equals(playerPosition))
                {
                    occupiedSpawns.Add(position);
                    isCompetitiveSpawn = true;
                    break;
                }
            }
            if (isCompetitiveSpawn) continue;

            // The player is not on a competitive spawn, we will put them on one in the next loop.
            incorrectSpawnedPlayers.Add(player);
        }

        foreach (CCSPlayerController player in incorrectSpawnedPlayers)
        {
            if (!IsPlayerValid(player) || coaches.Contains(player)) continue;

            List<Position> teamPositions = spawnsData[player.TeamNum];
            Position playerPosition = new(player.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin, player.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsRotation);
            foreach (Position position in teamPositions)
            {
                if (occupiedSpawns.Contains(position)) continue;
                occupiedSpawns.Add(position);
                AddTimer(0.1f, () =>
                {
                    player!.PlayerPawn.Value.Teleport(position.PlayerPosition, position.PlayerAngle, new Vector(0, 0, 0));
                });
                break;
            }
        }
    }

    private void HandleCoachWeapons(CCSPlayerController coach)
    {
        if (!IsPlayerValid(coach)) return;
        coach.RemoveWeapons();
    }

    /// <summary>
    /// Transfers bomb from coach to first available non-coach terrorist.
    /// </summary> 
    public void TransferCoachBomb(CCSPlayerController coach) {
        if (coach.TeamNum != (int)CsTeam.Terrorist) return; // can't have bomb

        // find bomb and new target
        var bomb = coach.PlayerPawn.Value!.WeaponServices!.MyWeapons
            .Where(w => w != null && w.IsValid && w.Value!.DesignerName == "weapon_c4")
            .FirstOrDefault();
        if (bomb == null || bomb.Value == null) return; // should never trigger

        var target = Utilities.GetPlayers()
            .FirstOrDefault(
                p => IsPlayerValid(p)
                && !reverseTeamSides["TERRORIST"].coach.Contains(p)
                && p.TeamNum == (int)CsTeam.Terrorist
                && p.PawnIsAlive
            );
        if (!IsPlayerValid(target)) return; // should never trigger

        // transfer bomb
        Log($"[EventPlayerGivenC4 INFO] Transferred bomb from {coach.PlayerName} (Coach) to {target.PlayerName}.");
        bomb.Value!.Remove();
        target.GiveNamedItem("weapon_c4");
    }

    public CsTeam GetCoachTeam(CCSPlayerController coach)
    {
        if (matchzyTeam1.coach.Contains(coach))
        {
            if (teamSides[matchzyTeam1] == "CT")
            {
                return CsTeam.CounterTerrorist;
            }
            else if (teamSides[matchzyTeam1] == "TERRORIST")
            {
                return CsTeam.Terrorist;
            }
        }
        if (matchzyTeam2.coach.Contains(coach))
        {
            if (teamSides[matchzyTeam2] == "CT")
            {
                return CsTeam.CounterTerrorist;
            }
            else if (teamSides[matchzyTeam2] == "TERRORIST")
            {
                return CsTeam.Terrorist;
            }
        }
        return CsTeam.Spectator;
    }

    private void HandleCoachTeam(CCSPlayerController playerController)
    {
        CsTeam oldTeam = GetCoachTeam(playerController);
        if (playerController.Team != oldTeam)
        {
            playerController.ChangeTeam(CsTeam.Spectator);
            AddTimer(0.01f, () => playerController.ChangeTeam(oldTeam));
        }
        if (playerController.InGameMoneyServices != null) playerController.InGameMoneyServices.Account = 0;
    }

    private void KillCoaches()
    {
        if (isPaused || IsTacticalTimeoutActive()) return;
        HashSet<CCSPlayerController> coaches = GetAllCoaches();
        if (IsWingmanMode() || coaches.Count == 0) return;
        string suicidePenalty = GetConvarStringValue(ConVar.Find("mp_suicide_penalty"));
        string specFreezeTime = GetConvarStringValue(ConVar.Find("spec_freeze_time"));
        string specFreezeTimeLock = GetConvarStringValue(ConVar.Find("spec_freeze_time_lock"));
        string specFreezeDeathanim = GetConvarStringValue(ConVar.Find("spec_freeze_deathanim_time"));
        Server.ExecuteCommand("mp_suicide_penalty 0;spec_freeze_time 0; spec_freeze_time_lock 0; spec_freeze_deathanim_time 0;");

        foreach (var coach in coaches)
        {
            if (!IsPlayerValid(coach)) continue;
            if (isPaused || IsTacticalTimeoutActive()) continue;

            Position coachPosition = new(coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin, coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsRotation);
            coach!.PlayerPawn.Value!.Teleport(new Vector(coachPosition.PlayerPosition.X, coachPosition.PlayerPosition.Y, coachPosition.PlayerPosition.Z + 20.0f), coachPosition.PlayerAngle, new Vector(0, 0, 0));
            coach.PlayerPawn.Value!.CommitSuicide(explode: false, force: true);
        }
        Server.ExecuteCommand($"mp_suicide_penalty {suicidePenalty}; spec_freeze_time {specFreezeTime}; spec_freeze_time_lock {specFreezeTimeLock}; spec_freeze_deathanim_time {specFreezeDeathanim};");
    }

    private void GetCoachSpawns()
    {
        coachSpawns = GetEmptySpawnsData();
        try
        {
            string spawnsConfigPath = Path.Combine(ModuleDirectory, "spawns", "coach", $"{Server.MapName}.json");
            string spawnsConfig = File.ReadAllText(spawnsConfigPath);

            var jsonDictionary = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(spawnsConfig);
            if (jsonDictionary is null) return;
            foreach (var entry in jsonDictionary)
            {
                byte team = byte.Parse(entry.Key);
                List<Position> positionList = new();

                foreach (var positionData in entry.Value)
                {
                    string[] vectorArray = positionData["Vector"].Split(' ');
                    string[] angleArray = positionData["QAngle"].Split(' ');

                    // Parse position and angle
                    Vector vector = new(float.Parse(vectorArray[0]), float.Parse(vectorArray[1]), float.Parse(vectorArray[2]));
                    QAngle qAngle = new(float.Parse(angleArray[0]), float.Parse(angleArray[1]), float.Parse(angleArray[2]));

                    Position position = new(vector, qAngle);

                    positionList.Add(position);
                }
                coachSpawns[team] =  positionList;
            }
            Log($"[GetCoachSpawns] Loaded {coachSpawns.Count} coach spawns");
        }
        catch (Exception ex)
        {
            Log($"[GetCoachSpawns - FATAL] Error getting coach spawns. [ERROR]: {ex.Message}");
        }
    }
}
