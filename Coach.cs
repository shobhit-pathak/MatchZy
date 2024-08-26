using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Cvars;

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
        int freezeTime = ConVar.Find("mp_freezetime")!.GetPrimitiveValue<int>();
        freezeTime = freezeTime > 2 ? freezeTime: 2;
        coachKillTimer ??= AddTimer(freezeTime - 1.5f, KillCoaches);
        HashSet<CCSPlayerController> coaches = GetAllCoaches();
        HashSet<CCSPlayerController> competitiveSpawnCoaches = new();
        if (spawnsData.Values.Any(list => list.Count == 0)) GetSpawns();

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

            List<Position> teamPositions = spawnsData[coach.TeamNum];
            Position coachPosition = new(coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin, coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsRotation);

            foreach (Position position in teamPositions)
            {
                if (position.Equals(coachPosition))
                {
                    competitiveSpawnCoaches.Add(coach);
                    break;
                }
            }
            SetPlayerInvisible(player: coach, setWeaponsInvisible: false);
            // Elevating coach before dropping the C4 to prevent it going inside the ground.
            AddTimer(0.05f, () =>
            {
                coach!.PlayerPawn.Value!.Teleport(new Vector(coachPosition.PlayerPosition.X, coachPosition.PlayerPosition.Y, coachPosition.PlayerPosition.Z + 20.0f), coachPosition.PlayerAngle, new Vector(0, 0, 0));
                HandleCoachWeapons(coach);
                coach!.PlayerPawn.Value.Teleport(coachPosition.PlayerPosition, coachPosition.PlayerAngle, new Vector(0, 0, 0));
            });
            
        }

        var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

        // foreach (var key in playerData.Keys)
        // {
        foreach (var player in playerEntities)
        {
            if (!IsPlayerValid(player)) continue;
            // CCSPlayerController player = playerData[key];
            List<Position> teamPositions = spawnsData[player.TeamNum];
            Position playerPosition = new(player.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin, player.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsRotation);
            bool isCompetitiveSpawn = false;
            foreach (Position position in teamPositions)
            {
                if (position.Equals(playerPosition))
                {
                    isCompetitiveSpawn = true;
                    break;
                }
            }
            // Player is already on a competitive spawn, no need to swap.
            if (isCompetitiveSpawn) continue;

            CCSPlayerController? coach = competitiveSpawnCoaches.FirstOrDefault((CCSPlayerController coach) => coach.Team == player.Team);
            if (coach is null) continue;
            competitiveSpawnCoaches.Remove(coach);

            Position coachPosition = new(coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin, coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsRotation);
            AddTimer(0.1f, () =>
            {
                coach!.PlayerPawn.Value!.Teleport(new Vector(playerPosition.PlayerPosition.X, playerPosition.PlayerPosition.Y, playerPosition.PlayerPosition.Z), playerPosition.PlayerAngle, new Vector(0, 0, 0));
                player!.PlayerPawn.Value.Teleport(coachPosition.PlayerPosition, coachPosition.PlayerAngle, new Vector(0, 0, 0));
            });

            // Stopping the coaches from moving, so that they don't block the players.
            coach.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_NONE;
            coach.PlayerPawn.Value!.ActualMoveType = MoveType_t.MOVETYPE_NONE;
        }
    }

    private void HandleCoachWeapons(CCSPlayerController coach)
    {
        if (!IsPlayerValid(coach)) return;
        DropWeaponByDesignerName(coach, "weapon_c4");
        coach.RemoveWeapons();
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
        string suicidePenalty = GetConvarStringValue(ConVar.Find("mp_suicide_penalty"));
        string deathDropGunEnabled = GetConvarStringValue(ConVar.Find("mp_death_drop_gun"));
        string specFreezeTime = GetConvarStringValue(ConVar.Find("spec_freeze_time"));
        string specFreezeTimeLock = GetConvarStringValue(ConVar.Find("spec_freeze_time_lock"));
        string specFreezeDeathanim = GetConvarStringValue(ConVar.Find("spec_freeze_deathanim_time"));
        Server.ExecuteCommand("mp_suicide_penalty 0; mp_death_drop_gun 0;spec_freeze_time 0; spec_freeze_time_lock 0; spec_freeze_deathanim_time 0;");

        // Adding timer to make sure above commands are executed successfully.
        AddTimer(0.5f, () =>
        {
            foreach (var coach in coaches)
            {
                if (!IsPlayerValid(coach)) continue;
                if (isPaused || IsTacticalTimeoutActive()) continue;

                Position coachPosition = new(coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin, coach.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsRotation);
                coach!.PlayerPawn.Value!.Teleport(new Vector(coachPosition.PlayerPosition.X, coachPosition.PlayerPosition.Y, coachPosition.PlayerPosition.Z + 20.0f), coachPosition.PlayerAngle, new Vector(0, 0, 0));
                // Dropping the C4 if it was picked up or passed to the coach.
                DropWeaponByDesignerName(coach, "weapon_c4");
                coach.PlayerPawn.Value!.CommitSuicide(explode: false, force: true);
            }
            Server.ExecuteCommand($"mp_suicide_penalty {suicidePenalty}; mp_death_drop_gun {deathDropGunEnabled}; spec_freeze_time {specFreezeTime}; spec_freeze_time_lock {specFreezeTimeLock}; spec_freeze_deathanim_time {specFreezeDeathanim};");
        });
    }
}