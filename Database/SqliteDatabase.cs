using System.IO;
using CounterStrikeSharp.API;
using Dapper;
using Microsoft.Data.Sqlite;

namespace MatchZy
{
    public class SqliteDatabase : BaseDatabase
    {
        public override void InitializeDatabase(string directory)
        {
            try
            {
                connection = new SqliteConnection($"Data Source={Path.Join(directory, "matchzy.db")}");
                connection.Open();
                Log("[InitializeDatabase] SQLite Database connection successful");
                CreateRequiredTables();
                Log("[InitializeDatabase] Table matchzy_stats_matches created (or already exists)");
                Log("[InitializeDatabase] Table matchzy_stats_players created (or already exists)");
                Log("[InitializeDatabase] Table matchzy_stats_maps created (or already exists)");
            }
            catch (Exception ex)
            {
                Log($"[InitializeDatabase - FATAL] Database connection or table creation error: {ex.Message}");
            }
        }

        private void CreateRequiredTables()
        {
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS matchzy_stats_matches (
                    matchid INTEGER PRIMARY KEY AUTOINCREMENT,
                    start_time DATETIME NOT NULL,
                    end_time DATETIME DEFAULT NULL,
                    winner TEXT NOT NULL DEFAULT '',
                    series_type TEXT NOT NULL DEFAULT '',
                    team1_name TEXT NOT NULL DEFAULT '',
                    team1_score INTEGER NOT NULL DEFAULT 0,
                    team2_name TEXT NOT NULL DEFAULT '',
                    team2_score INTEGER NOT NULL DEFAULT 0,
                    server_ip TEXT NOT NULL DEFAULT '0'
                )");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS matchzy_stats_maps (
                    matchid INTEGER NOT NULL,
                    mapnumber INTEGER NOT NULL,
                    start_time DATETIME NOT NULL,
                    end_time DATETIME DEFAULT NULL,
                    winner TEXT NOT NULL DEFAULT '',
                    mapname TEXT NOT NULL DEFAULT '',
                    team1_score INTEGER NOT NULL DEFAULT 0,
                    team2_score INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (matchid, mapnumber),
                    FOREIGN KEY (matchid) REFERENCES matchzy_stats_matches (matchid)
                )");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS matchzy_stats_players (
                    matchid INTEGER NOT NULL,
                    mapnumber INTEGER NOT NULL,
                    steamid64 INTEGER NOT NULL,
                    team TEXT NOT NULL DEFAULT '',
                    name TEXT NOT NULL,
                    kills INTEGER NOT NULL,
                    deaths INTEGER NOT NULL,
                    damage INTEGER NOT NULL,
                    assists INTEGER NOT NULL,
                    enemy5ks INTEGER NOT NULL,
                    enemy4ks INTEGER NOT NULL,
                    enemy3ks INTEGER NOT NULL,
                    enemy2ks INTEGER NOT NULL,
                    utility_count INTEGER NOT NULL,
                    utility_damage INTEGER NOT NULL,
                    utility_successes INTEGER NOT NULL,
                    utility_enemies INTEGER NOT NULL,
                    flash_count INTEGER NOT NULL,
                    flash_successes INTEGER NOT NULL,
                    health_points_removed_total INTEGER NOT NULL,
                    health_points_dealt_total INTEGER NOT NULL,
                    shots_fired_total INTEGER NOT NULL,
                    shots_on_target_total INTEGER NOT NULL,
                    v1_count INTEGER NOT NULL,
                    v1_wins INTEGER NOT NULL,
                    v2_count INTEGER NOT NULL,
                    v2_wins INTEGER NOT NULL,
                    entry_count INTEGER NOT NULL,
                    entry_wins INTEGER NOT NULL,
                    equipment_value INTEGER NOT NULL,
                    money_saved INTEGER NOT NULL,
                    kill_reward INTEGER NOT NULL,
                    live_time INTEGER NOT NULL,
                    head_shot_kills INTEGER NOT NULL,
                    cash_earned INTEGER NOT NULL,
                    enemies_flashed INTEGER NOT NULL,
                    PRIMARY KEY (matchid, mapnumber, steamid64),
                    FOREIGN KEY (matchid) REFERENCES matchzy_stats_matches (matchid),
                    FOREIGN KEY (matchid, mapnumber) REFERENCES matchzy_stats_maps (matchid, mapnumber)
                )");
        }

        public override long InitMatch(string team1name, string team2name, string serverIp, bool isMatchSetup, long liveMatchId, int mapNumber, string seriesType, MatchConfig matchConfig)
        {
            try
            {
                string mapName = isMatchSetup ? matchConfig.Maplist[mapNumber] : Server.MapName;

                if (mapNumber == 0)
                {
                    if (isMatchSetup && liveMatchId != -1)
                    {
                        connection.Execute(@"
                            INSERT INTO matchzy_stats_matches (matchid, start_time, team1_name, team2_name, series_type, server_ip)
                            VALUES (@liveMatchId, datetime('now'), @team1name, @team2name, @seriesType, @serverIp)",
                            new { liveMatchId, team1name, team2name, seriesType, serverIp });
                    }
                    else
                    {
                        connection.Execute(@"
                            INSERT INTO matchzy_stats_matches (start_time, team1_name, team2_name, series_type, server_ip)
                            VALUES (datetime('now'), @team1name, @team2name, @seriesType, @serverIp)",
                            new { team1name, team2name, seriesType, serverIp });
                    }
                }

                if (isMatchSetup && liveMatchId != -1)
                {
                    connection.Execute(@"
                        INSERT INTO matchzy_stats_maps (matchid, start_time, mapnumber, mapname)
                        VALUES (@liveMatchId, datetime('now'), @mapNumber, @mapName)",
                        new { liveMatchId, mapNumber, mapName });
                    return liveMatchId;
                }

                long matchId = connection.ExecuteScalar<long>("SELECT last_insert_rowid()");

                connection.Execute(@"
                    INSERT INTO matchzy_stats_maps (matchid, start_time, mapnumber, mapname)
                    VALUES (@matchId, datetime('now'), @mapNumber, @mapName)",
                    new { matchId, mapNumber, mapName });

                Log($"[InsertMatchData] Data inserted into matchzy_stats_matches with match_id: {matchId}");
                return matchId;
            }
            catch (Exception ex)
            {
                Log($"[InsertMatchData - FATAL] Error inserting data: {ex.Message}");
                return liveMatchId;
            }
        }

        public override void UpdateTeamData(int matchId, string team1name, string team2name)
        {
            try
            {
                connection.Execute(@"
                    UPDATE matchzy_stats_matches
                    SET team1_name = @team1name, team2_name = @team2name
                    WHERE matchid = @matchId",
                    new { matchId, team1name, team2name });
                Log($"[UpdateTeamData] Data updated for matchId: {matchId} team1name: {team1name} team2name: {team2name}");
            }
            catch (Exception ex)
            {
                Log($"[UpdateTeamData - FATAL] Error updating data of matchId: {matchId} [ERROR]: {ex.Message}");
            }
        }

        public override async Task SetMapEndData(long matchId, int mapNumber, string winnerName, int t1score, int t2score, int team1SeriesScore, int team2SeriesScore)
        {
            try
            {
                await connection.ExecuteAsync(@"
                    UPDATE matchzy_stats_maps
                    SET winner = @winnerName, end_time = datetime('now'), team1_score = @t1score, team2_score = @t2score
                    WHERE matchid = @matchId AND mapNumber = @mapNumber",
                    new { matchId, winnerName, t1score, t2score, mapNumber });

                await connection.ExecuteAsync(@"
                    UPDATE matchzy_stats_matches
                    SET team1_score = @team1SeriesScore, team2_score = @team2SeriesScore
                    WHERE matchid = @matchId",
                    new { matchId, team1SeriesScore, team2SeriesScore });

                Log($"[SetMapEndData] Data updated for matchId: {matchId} mapNumber: {mapNumber} winnerName: {winnerName}");
            }
            catch (Exception ex)
            {
                Log($"[SetMapEndData - FATAL] Error updating data of matchId: {matchId} mapNumber: {mapNumber} [ERROR]: {ex.Message}");
            }
        }

        public override async Task SetMatchEndData(long matchId, string winnerName, int t1score, int t2score)
        {
            try
            {
                await connection.ExecuteAsync(@"
                    UPDATE matchzy_stats_matches
                    SET winner = @winnerName, end_time = datetime('now'), team1_score = @t1score, team2_score = @t2score
                    WHERE matchid = @matchId",
                    new { matchId, winnerName, t1score, t2score });
                Log($"[SetMatchEndData] Data updated for matchId: {matchId} winnerName: {winnerName}");
            }
            catch (Exception ex)
            {
                Log($"[SetMatchEndData - FATAL] Error updating data of matchId: {matchId} [ERROR]: {ex.Message}");
            }
        }

        public override async Task UpdateMapStatsAsync(long matchId, int mapNumber, int t1score, int t2score)
        {
            try
            {
                await connection.ExecuteAsync(@"
                    UPDATE matchzy_stats_maps
                    SET team1_score = @t1score, team2_score = @t2score
                    WHERE matchid = @matchId AND mapnumber = @mapNumber",
                    new { matchId, mapNumber, t1score, t2score });
            }
            catch (Exception ex)
            {
                Log($"[UpdateMapStatsAsync - FATAL] Error updating data of matchId: {matchId} [ERROR]: {ex.Message}");
            }
        }

        public override async Task UpdatePlayerStatsAsync(long matchId, int mapNumber, Dictionary<ulong, Dictionary<string, object>> playerStatsDictionary)
        {
            try
            {
                foreach (ulong steamid64 in playerStatsDictionary.Keys)
                {
                    Log($"[UpdatePlayerStats] Going to update data for Match: {matchId}, MapNumber: {mapNumber}, Player: {steamid64}");
                    var playerStats = playerStatsDictionary[steamid64];

                    await connection.ExecuteAsync(@"
                        INSERT OR REPLACE INTO matchzy_stats_players (
                            matchid, mapnumber, steamid64, team, name, kills, deaths, damage, assists,
                            enemy5ks, enemy4ks, enemy3ks, enemy2ks, utility_count, utility_damage,
                            utility_successes, utility_enemies, flash_count, flash_successes,
                            health_points_removed_total, health_points_dealt_total, shots_fired_total,
                            shots_on_target_total, v1_count, v1_wins, v2_count, v2_wins, entry_count, entry_wins,
                            equipment_value, money_saved, kill_reward, live_time, head_shot_kills,
                            cash_earned, enemies_flashed)
                        VALUES (
                            @matchId, @mapNumber, @steamid64, @team, @name, @kills, @deaths, @damage, @assists,
                            @enemy5ks, @enemy4ks, @enemy3ks, @enemy2ks, @utility_count, @utility_damage,
                            @utility_successes, @utility_enemies, @flash_count, @flash_successes,
                            @health_points_removed_total, @health_points_dealt_total, @shots_fired_total,
                            @shots_on_target_total, @v1_count, @v1_wins, @v2_count, @v2_wins, @entry_count,
                            @entry_wins, @equipment_value, @money_saved, @kill_reward, @live_time,
                            @head_shot_kills, @cash_earned, @enemies_flashed)",
                        BuildPlayerParams(matchId, mapNumber, steamid64, playerStats));

                    Log($"[UpdatePlayerStats] Data inserted/updated for player {steamid64} in match {matchId}");
                }
            }
            catch (Exception ex)
            {
                Log($"[UpdatePlayerStats - FATAL] Error inserting/updating data: {ex.Message}");
            }
        }

    }
}
