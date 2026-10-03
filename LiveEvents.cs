using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MatchZy
{
    // Get5's live events: round_start, player_death, bomb_planted, bomb_defused, backup_loaded and player_disconnect, with Get5's fields
    // (stats.sp / backups.sp), only while the match is live. G5API uses them for the kill feed and bomb plants, and to
    // remove them again for the rounds undone by a round restore.
    public partial class MatchZy
    {
        // Rounds played when the current round started (Get5's round_number, 0 for the first round).
        private int liveRoundNumber = 0;
        // Server.EngineTime when freeze time ended (0 while in freeze time), for round_time.
        private double roundStartedAt = 0;
        private double bombPlantedAt = 0;
        private string? lastBombSite = null;
        // A round restore sends its own round_start (after backup_loaded), so the game's next one is not sent again.
        private bool roundStartSentByRestore = false;
        // Bomb plants / defuses per player on this map, sent in the round_end stats.
        private readonly BombStats bombStats = new();

        private void RegisterLiveEventHandlers()
        {
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                try
                {
                    roundStartedAt = 0;
                    bombPlantedAt = 0;
                    lastBombSite = null;
                    liveRoundNumber = GetRoundNumer();
                    if (roundStartSentByRestore)
                    {
                        roundStartSentByRestore = false;
                        return HookResult.Continue;
                    }
                    if (!IsLiveForEvents()) return HookResult.Continue;
                    var roundStartEvent = new MatchZyRoundStartedEvent
                    {
                        MatchId = liveMatchId,
                        MapNumber = matchConfig.CurrentMapNumber,
                        RoundNumber = liveRoundNumber,
                    };
                    Task.Run(async () => await SendEventAsync(roundStartEvent));
                }
                catch (Exception e)
                {
                    Log($"[LiveEvents round_start FATAL] An error occurred: {e.Message}");
                }
                return HookResult.Continue;
            });

            RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                roundStartedAt = Server.EngineTime;
                roundStartSentByRestore = false;
                return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                try
                {
                    SendPlayerDeathEvent(@event);
                }
                catch (Exception e)
                {
                    Log($"[LiveEvents player_death FATAL] An error occurred: {e.Message}");
                }
                return HookResult.Continue;
            });

            RegisterEventHandler<EventBombPlanted>((@event, info) =>
            {
                try
                {
                    bombPlantedAt = Server.EngineTime;
                    lastBombSite = GetPlantedBombSite();
                    if (!IsLiveForEvents() || !IsPlayerValid(@event.Userid)) return HookResult.Continue;
                    bombStats.AddPlant(@event.Userid!.SteamID);
                    var bombEvent = new MatchZyBombEvent("bomb_planted")
                    {
                        MatchId = liveMatchId,
                        MapNumber = matchConfig.CurrentMapNumber,
                        RoundNumber = liveRoundNumber,
                        RoundTime = GetRoundTime(),
                        Player = GetPlayerObject(@event.Userid!),
                        Site = lastBombSite,
                    };
                    Task.Run(async () => await SendEventAsync(bombEvent));
                }
                catch (Exception e)
                {
                    Log($"[LiveEvents bomb_planted FATAL] An error occurred: {e.Message}");
                }
                return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                try
                {
                    // As in Get5: players (not bots or GOTV) leaving while a match is loaded.
                    CCSPlayerController? player = @event.Userid;
                    if (!isMatchSetup || player == null || !player.IsValid || player.IsBot || player.IsHLTV) return HookResult.Continue;
                    var disconnectEvent = new MatchZyPlayerDisconnectedEvent
                    {
                        MatchId = liveMatchId,
                        Player = GetPlayerObject(player),
                    };
                    Task.Run(async () => await SendEventAsync(disconnectEvent));
                }
                catch (Exception e)
                {
                    Log($"[LiveEvents player_disconnect FATAL] An error occurred: {e.Message}");
                }
                return HookResult.Continue;
            });

            RegisterEventHandler<EventBombDefused>((@event, info) =>
            {
                try
                {
                    if (!IsLiveForEvents() || !IsPlayerValid(@event.Userid)) return HookResult.Continue;
                    bombStats.AddDefuse(@event.Userid!.SteamID);
                    int c4Timer = ConVar.Find("mp_c4timer")?.GetPrimitiveValue<int>() ?? 40;
                    int sincePlant = bombPlantedAt > 0 ? (int)Math.Round((Server.EngineTime - bombPlantedAt) * 1000) : 0;
                    var defuseEvent = new MatchZyBombDefusedEvent
                    {
                        MatchId = liveMatchId,
                        MapNumber = matchConfig.CurrentMapNumber,
                        RoundNumber = liveRoundNumber,
                        RoundTime = GetRoundTime(),
                        Player = GetPlayerObject(@event.Userid!),
                        Site = lastBombSite,
                        BombTimeRemaining = LiveEventLogic.BombTimeRemaining(c4Timer, sincePlant),
                    };
                    Task.Run(async () => await SendEventAsync(defuseEvent));
                }
                catch (Exception e)
                {
                    Log($"[LiveEvents bomb_defused FATAL] An error occurred: {e.Message}");
                }
                return HookResult.Continue;
            });
        }

        // Get5 sends these only in its live state (not in warmup, the knife round or practice).
        private bool IsLiveForEvents() => isMatchLive && matchStarted && !isPractice;

        private int GetRoundTime() => LiveEventLogic.RoundTime(roundStartedAt, Server.EngineTime);

        // Get5's GetPlayerObject.
        private static MatchZyPlayer GetPlayerObject(CCSPlayerController player)
        {
            int userId = player.UserId ?? 0;
            return new MatchZyPlayer
            {
                SteamId = LiveEventLogic.PlayerSteamId(player.SteamID, player.IsBot, userId),
                Name = player.PlayerName,
                UserId = userId,
                Side = LiveEventLogic.SideName(player.TeamNum),
                IsBot = player.IsBot,
            };
        }

        private bool IsCoach(CCSPlayerController player) => matchzyTeam1.coach.Contains(player) || matchzyTeam2.coach.Contains(player);

        private void SendPlayerDeathEvent(EventPlayerDeath @event)
        {
            if (!IsLiveForEvents()) return;
            CCSPlayerController? victim = @event.Userid;
            if (!IsPlayerValid(victim)) return;
            // Coaches are killed at round start, which is not a death in the match.
            if (IsCoach(victim!)) return;

            CCSPlayerController? attacker = IsPlayerValid(@event.Attacker) ? @event.Attacker : null;
            MatchZyPlayer victimPlayer = GetPlayerObject(victim!);
            MatchZyPlayer? attackerPlayer = attacker != null ? GetPlayerObject(attacker) : null;

            string weapon = @event.Weapon ?? "";
            bool killedByBomb = LiveEventLogic.IsBombKill(weapon);
            bool attackerIsVictim = attacker != null && attacker == victim;

            MatchZyAssist? assist = null;
            CCSPlayerController? assister = @event.Assister;
            if (IsPlayerValid(assister))
            {
                MatchZyPlayer assisterPlayer = GetPlayerObject(assister!);
                assist = new MatchZyAssist
                {
                    Player = assisterPlayer,
                    FriendlyFire = assisterPlayer.Side == victimPlayer.Side,
                    FlashAssist = @event.Assistedflash,
                };
            }

            var deathEvent = new MatchZyPlayerDeathEvent
            {
                MatchId = liveMatchId,
                MapNumber = matchConfig.CurrentMapNumber,
                RoundNumber = liveRoundNumber,
                RoundTime = GetRoundTime(),
                Player = victimPlayer,
                Weapon = new MatchZyWeapon { Name = weapon, Id = LiveEventLogic.WeaponId(weapon) },
                Bomb = killedByBomb,
                Headshot = @event.Headshot,
                ThruSmoke = @event.Thrusmoke,
                Penetrated = @event.Penetrated,
                AttackerBlind = @event.Attackerblind,
                NoScope = @event.Noscope,
                Suicide = LiveEventLogic.IsSuicide(attacker != null, attackerIsVictim, killedByBomb),
                // As in Get5: the attacker's side is the victim's (this includes killing yourself).
                FriendlyFire = attackerPlayer != null && attackerPlayer.Side == victimPlayer.Side,
                Attacker = attackerPlayer,
                Assist = assist,
            };
            Task.Run(async () => await SendEventAsync(deathEvent));
        }

        // The site of the planted bomb (Get5 takes the site nearest to the planter, which is the same).
        private static string? GetPlantedBombSite()
        {
            var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            return LiveEventLogic.BombSiteName(plantedBomb?.BombSite);
        }

        // After a round restore: backup_loaded, then round_start for the restored round, in that order (G5API removes the
        // kills and bomb plants of the undone rounds on round_start once it knows the map was restored).
        private void SendBackupRestoredEvents(string fileName, int roundNumber)
        {
            liveRoundNumber = roundNumber;
            roundStartSentByRestore = true;
            var backupEvent = new MatchZyBackupRestoredEvent
            {
                MatchId = liveMatchId,
                MapNumber = matchConfig.CurrentMapNumber,
                RoundNumber = roundNumber,
                FileName = fileName,
            };
            var roundStartEvent = new MatchZyRoundStartedEvent
            {
                MatchId = liveMatchId,
                MapNumber = matchConfig.CurrentMapNumber,
                RoundNumber = roundNumber,
            };
            Task.Run(async () =>
            {
                await SendEventAsync(backupEvent);
                await SendEventAsync(roundStartEvent);
            });
        }
    }
}
