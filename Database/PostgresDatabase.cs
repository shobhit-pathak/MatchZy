using CounterStrikeSharp.API;
using Dapper;
using Npgsql;

namespace MatchZy
{
    public class PostgresDatabase : BaseDatabase
    {
        private readonly DatabaseConfig config;

        public PostgresDatabase(DatabaseConfig config)
        {
            this.config = config;
        }

        public override void InitializeDatabase(string directory)
        {
            try
            {
                string connectionString = $"Host={config.PostgresHost};Port={config.PostgresPort ?? 5432};Database={config.PostgresDatabase};Username={config.PostgresUsername};Password={config.PostgresPassword};";
                connection = new NpgsqlConnection(connectionString);
                connection.Open();
                Log("[InitializeDatabase] PostgreSQL Database connection successful");
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
                    matchid BIGSERIAL PRIMARY KEY,
                    start_time TIMESTAMP NOT NULL,
                    end_time TIMESTAMP DEFAULT NULL,
                    winner VARCHAR(255) NOT NULL DEFAULT '',
                    series_type VARCHAR(255) NOT NULL DEFAULT '',
                    team1_name VARCHAR(255) NOT NULL DEFAULT '',
                    team1_score INT NOT NULL DEFAULT 0,
                    team2_name VARCHAR(255) NOT NULL DEFAULT '',
                    team2_score INT NOT NULL DEFAULT 0,
                    server_ip VARCHAR(255) NOT NULL DEFAULT '0'
                )");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS matchzy_stats_maps (
                    matchid BIGINT NOT NULL,
                    mapnumber SMALLINT NOT NULL,
                    start_time TIMESTAMP NOT NULL,
                    end_time TIMESTAMP DEFAULT NULL,
                    winner VARCHAR(16) NOT NULL DEFAULT '',
                    mapname VARCHAR(64) NOT NULL DEFAULT '',
                    team1_score INT NOT NULL DEFAULT 0,
                    team2_score INT NOT NULL DEFAULT 0,
                    PRIMARY KEY (matchid, mapnumber),
                    CONSTRAINT matchzy_stats_maps_matchid FOREIGN KEY (matchid) REFERENCES matchzy_stats_matches (matchid)
                )");

            connection.Execute(@"
                CREATE INDEX IF NOT EXISTS mapnumber_index ON matchzy_stats_maps (mapnumber)");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS matchzy_stats_players (
                    matchid BIGINT NOT NULL,
                    mapnumber SMALLINT NOT NULL,
                    steamid64 BIGINT NOT NULL,
                    team VARCHAR(255) NOT NULL DEFAULT '',
                    name VARCHAR(255) NOT NULL,
                    kills INT NOT NULL,
                    deaths INT NOT NULL,
                    damage INT NOT NULL,
                    assists INT NOT NULL,
                    enemy5ks INT NOT NULL,
                    enemy4ks INT NOT NULL,
                    enemy3ks INT NOT NULL,
                    enemy2ks INT NOT NULL,
                    utility_count INT NOT NULL,
                    utility_damage INT NOT NULL,
                    utility_successes INT NOT NULL,
                    utility_enemies INT NOT NULL,
                    flash_count INT NOT NULL,
                    flash_successes INT NOT NULL,
                    health_points_removed_total INT NOT NULL,
                    health_points_dealt_total INT NOT NULL,
                    shots_fired_total INT NOT NULL,
                    shots_on_target_total INT NOT NULL,
                    v1_count INT NOT NULL,
                    v1_wins INT NOT NULL,
                    v2_count INT NOT NULL,
                    v2_wins INT NOT NULL,
                    entry_count INT NOT NULL,
                    entry_wins INT NOT NULL,
                    equipment_value INT NOT NULL,
                    money_saved INT NOT NULL,
                    kill_reward INT NOT NULL,
                    live_time INT NOT NULL,
                    head_shot_kills INT NOT NULL,
                    cash_earned INT NOT NULL,
                    enemies_flashed INT NOT NULL,
                    PRIMARY KEY (matchid, mapnumber, steamid64),
                    CONSTRAINT fk_player_map_ref FOREIGN KEY (matchid, mapnumber)
                        REFERENCES matchzy_stats_maps (matchid, mapnumber)
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
                            VALUES (@liveMatchId, NOW(), @team1name, @team2name, @seriesType, @serverIp)",
                            new { liveMatchId, team1name, team2name, seriesType, serverIp });
                    }
                    else
                    {
                        liveMatchId = connection.ExecuteScalar<long>(@"
                            INSERT INTO matchzy_stats_matches (start_time, team1_name, team2_name, series_type, server_ip)
                            VALUES (NOW(), @team1name, @team2name, @seriesType, @serverIp)
                            RETURNING matchid",
                            new { team1name, team2name, seriesType, serverIp });
                    }
                }

                connection.Execute(@"
                    INSERT INTO matchzy_stats_maps (matchid, start_time, mapnumber, mapname)
                    VALUES (@liveMatchId, NOW(), @mapNumber, @mapName)",
                    new { liveMatchId, mapNumber, mapName });

                Log($"[InsertMatchData] Data inserted into matchzy_stats_matches with match_id: {liveMatchId}");
                return liveMatchId;
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
                    SET winner = @winnerName, end_time = NOW(), team1_score = @t1score, team2_score = @t2score
                    WHERE matchid = @matchId AND mapnumber = @mapNumber",
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
                    SET winner = @winnerName, end_time = NOW(), team1_score = @t1score, team2_score = @t2score
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
                        INSERT INTO matchzy_stats_players (
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
                            @head_shot_kills, @cash_earned, @enemies_flashed)
                        ON CONFLICT (matchid, mapnumber, steamid64) DO UPDATE SET
                            team = EXCLUDED.team, name = EXCLUDED.name, kills = EXCLUDED.kills,
                            deaths = EXCLUDED.deaths, damage = EXCLUDED.damage, assists = EXCLUDED.assists,
                            enemy5ks = EXCLUDED.enemy5ks, enemy4ks = EXCLUDED.enemy4ks, enemy3ks = EXCLUDED.enemy3ks,
                            enemy2ks = EXCLUDED.enemy2ks, utility_count = EXCLUDED.utility_count,
                            utility_damage = EXCLUDED.utility_damage, utility_successes = EXCLUDED.utility_successes,
                            utility_enemies = EXCLUDED.utility_enemies, flash_count = EXCLUDED.flash_count,
                            flash_successes = EXCLUDED.flash_successes,
                            health_points_removed_total = EXCLUDED.health_points_removed_total,
                            health_points_dealt_total = EXCLUDED.health_points_dealt_total,
                            shots_fired_total = EXCLUDED.shots_fired_total,
                            shots_on_target_total = EXCLUDED.shots_on_target_total,
                            v1_count = EXCLUDED.v1_count, v1_wins = EXCLUDED.v1_wins,
                            v2_count = EXCLUDED.v2_count, v2_wins = EXCLUDED.v2_wins,
                            entry_count = EXCLUDED.entry_count, entry_wins = EXCLUDED.entry_wins,
                            equipment_value = EXCLUDED.equipment_value, money_saved = EXCLUDED.money_saved,
                            kill_reward = EXCLUDED.kill_reward, live_time = EXCLUDED.live_time,
                            head_shot_kills = EXCLUDED.head_shot_kills, cash_earned = EXCLUDED.cash_earned,
                            enemies_flashed = EXCLUDED.enemies_flashed",
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
