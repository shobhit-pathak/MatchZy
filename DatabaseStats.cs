using System;
using System.IO;
using System.Data;
using System.Text.Json;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Dapper;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CsvHelper;
using CsvHelper.Configuration;
using MySqlConnector;



namespace MatchZy
{
    public class Database
    {
        private IDbConnection connection;

        DatabaseConfig config;
        public DatabaseType databaseType { get; set; }

        public void InitializeDatabase(string directory)
        {
            try
            {
                SetDatabaseConfig(directory);

                if (databaseType == DatabaseType.SQLite)
                {
                    connection =
                        new SqliteConnection(
                            $"Data Source={Path.Join(directory, "matchzy.db")}");
                }
                else if (databaseType == DatabaseType.MySQL)
                {
                    string connectionString = $"Server={config.MySqlHost};Port={config.MySqlPort};Database={config.MySqlDatabase};User Id={config.MySqlUsername};Password={config.MySqlPassword};";
                    connection = new MySqlConnection(connectionString);           
                }
                else
                {
                    Log($"[InitializeDatabase] Invalid database specified, using SQLite.");
                    connection = new SqliteConnection($"Data Source={Path.Join(directory, "matchzy.db")}");
                    databaseType = DatabaseType.SQLite;
                }
            } 
            catch (Exception ex)
            {
                Log($"[InitializeDatabase - FATAL] Database connection error: {ex.Message}");
            }

            try
            {
                connection.Open();
                string dbType = (connection is SqliteConnection) ? "SQLite" : "MySQL";
                Log($"[InitializeDatabase] {dbType} Database connection successful");

                // Create the `matchzy_match_data` and `matchzy_player_stats` tables if they doesn't exist
                if (connection is SqliteConnection) {
                    connection.Execute($@"
                        CREATE TABLE IF NOT EXISTS matchzy_match_data (
                            matchid INTEGER PRIMARY KEY AUTOINCREMENT,
                            start_time DATETIME NOT NULL,
                            end_time DATETIME DEFAULT NULL,
                            winner TEXT NOT NULL DEFAULT '',
                            map_name TEXT NOT NULL DEFAULT '',
                            team1_name TEXT NOT NULL DEFAULT '',
                            team1_score INTEGER NOT NULL DEFAULT 0,
                            team2_name TEXT NOT NULL DEFAULT '',
                            team2_score INTEGER NOT NULL DEFAULT 0,
                            server_ip TEXT NOT NULL DEFAULT '0'
                        )");
                    connection.Execute(@"
                        CREATE TABLE IF NOT EXISTS matchzy_player_stats (
                            matchid INTEGER NOT NULL,
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
                            PRIMARY KEY (matchid, steamid64),
                            FOREIGN KEY (matchid) REFERENCES matchzy_match_data (matchid)
                        )");
                } else {
                    connection.Execute($@"
                        CREATE TABLE IF NOT EXISTS matchzy_match_data (
                            matchid INT PRIMARY KEY AUTO_INCREMENT,
                            start_time DATETIME NOT NULL,
                            end_time DATETIME DEFAULT NULL,
                            winner VARCHAR(255) NOT NULL DEFAULT '',
                            map_name VARCHAR(255) NOT NULL DEFAULT '',
                            team1_name VARCHAR(255) NOT NULL DEFAULT '',
                            team1_score INT NOT NULL DEFAULT 0,
                            team2_name VARCHAR(255) NOT NULL DEFAULT '',
                            team2_score INT NOT NULL DEFAULT 0,
                            server_ip VARCHAR(255) NOT NULL DEFAULT '0'
                        )");

                    connection.Execute($@"
                    CREATE TABLE IF NOT EXISTS matchzy_player_stats (
                        matchid INT NOT NULL,
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
                        PRIMARY KEY (matchid, steamid64),
                        FOREIGN KEY (matchid) REFERENCES matchzy_match_data (matchid)
                    )");
                }

                Log("[InitializeDatabase] Table matchzy_match_data created (or already exists)");
                Log("[InitializeDatabase] Table matchzy_player_stats created (or already exists)");
            }
            catch (Exception ex)
            {
                Log($"[InitializeDatabase - FATAL] Database connection or table creation error: {ex.Message}");
            }
        }

        public long InitMatch(string team1name, string team2name, string serverIp)
        {
            try
            {
                string mapName = Server.MapName;
                string dateTimeExpression = (connection is SqliteConnection) ? "datetime('now')" : "NOW()";

                connection.Execute(@"
                    INSERT INTO matchzy_match_data (start_time, map_name, team1_name, team2_name, server_ip)
                    VALUES (" + dateTimeExpression + ", @mapName, @team1name, @team2name, @serverIp)",
                    new { mapName, team1name, team2name, serverIp });

                // Retrieve the last inserted match_id
                long matchId = -1;
                if (connection is SqliteConnection)
                {
                    matchId = connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
                }
                else if (connection is MySqlConnection)
                {
                    matchId = connection.ExecuteScalar<long>("SELECT LAST_INSERT_ID()");
                }

                Log($"[InsertMatchData] Data inserted into matchzy_match_data with match_id: {matchId}");
                return matchId;
            }
            catch (Exception ex)
            {
                Log($"[InsertMatchData - FATAL] Error inserting data: {ex.Message}");
                return -1;
            }
        }

        public void UpdateTeamData(int matchId, string team1name, string team2name) {
            try
            {
                connection.Execute(@"
                    UPDATE matchzy_match_data
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

        public void SetMatchEndData(long matchId, string winnerName, int t1score, int t2score)
        {
            try
            {
                string dateTimeExpression = (connection is SqliteConnection) ? "datetime('now')" : "NOW()";

                string sqlQuery = $@"
                    UPDATE matchzy_match_data
                    SET winner = @winnerName, end_time = {dateTimeExpression}, team1_score = @t1score, team2_score = @t2score
                    WHERE matchid = @matchId";

                connection.Execute(sqlQuery, new { matchId, winnerName, t1score, t2score });

                Log($"[SetMatchEndData] Data updated for matchId: {matchId} winnerName: {winnerName}");
            }
            catch (Exception ex)
            {
                Log($"[SetMatchEndData - FATAL] Error updating data of matchId: {matchId} [ERROR]: {ex.Message}");
            }
        }

        public void UpdateMatchStats(long matchId, int t1score, int t2score)
        {
            try
            {
                string sqlQuery = $@"
                    UPDATE matchzy_match_data
                    SET team1_score = @t1score, team2_score = @t2score
                    WHERE matchid = @matchId";

                connection.Execute(sqlQuery, new { matchId, t1score, t2score });
            }
            catch (Exception ex)
            {
                Log($"[UpdatePlayerStats - FATAL] Error updating data of matchId: {matchId} [ERROR]: {ex.Message}");
            }
        }

        public void UpdatePlayerStats(long matchId, string ctTeamName, string tTeamName, Dictionary<int, CCSPlayerController> playerData)
        {
            try
            {
                foreach (int key in playerData.Keys)
                {
                    CCSPlayerController player = playerData[key];
                    if (player.ActionTrackingServices == null) continue;
                    Log($"[UpdatePlayerStats] Going to update data for Match: {matchId}, Player: {player.SteamID}");

                    var playerStats = player.ActionTrackingServices.MatchStats;
                    ulong steamid64 = player.SteamID;
                    string teamName = "Spectator";
                    if (player.TeamNum == 3){
                        teamName = ctTeamName;
                    } else if (player.TeamNum == 2 ) {
                        teamName = tTeamName;
                    }
                    int enemy2Ks = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_iEnemy2Ks");
                    int entryCount = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_iEntryCount");
                    int entryWins = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_iEntryWins");
                    int v1Count = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_i1v1Count");
                    int v1Wins = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_i1v1Wins");
                    int v2Count = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_i1v2Count");
                    int v2Wins = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_i1v2Wins");
                    int utilityCount = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_iUtility_Count");
                    int utilitySuccess = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_iUtility_Successes");
                    int utilityEnemies = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_iUtility_Enemies");
                    int flashCount = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_iFlash_Count");
                    int flashSuccess = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_iFlash_Successes");
                    int healthPointsRemovedTotal = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_nHealthPointsRemovedTotal");
                    int healthPointsDealtTotal = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_nHealthPointsDealtTotal");
                    int shotsFiredTotal = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_nShotsFiredTotal");
                    int shotsOnTargetTotal = Schema.GetRef<Int32>(playerStats.Handle, "CSMatchStats_t", "m_nShotsOnTargetTotal");

                    string sqlQuery = $@"
                    INSERT INTO matchzy_player_stats (
                        matchid, steamid64, team, name, kills, deaths, damage, assists,
                        enemy5ks, enemy4ks, enemy3ks, enemy2ks, utility_count, utility_damage,
                        utility_successes, utility_enemies, flash_count, flash_successes,
                        health_points_removed_total, health_points_dealt_total, shots_fired_total,
                        shots_on_target_total, v1_count, v1_wins, v2_count, v2_wins, entry_count, entry_wins,
                        equipment_value, money_saved, kill_reward, live_time, head_shot_kills,
                        cash_earned, enemies_flashed)
                    VALUES (
                        @matchId, @steamid64, @team, @name, @kills, @deaths, @damage, @assists,
                        @enemy5ks, @enemy4ks, @enemy3ks, @enemy2ks, @utility_count, @utility_damage,
                        @utility_successes, @utility_enemies, @flash_count, @flash_successes,
                        @health_points_removed_total, @health_points_dealt_total, @shots_fired_total,
                        @shots_on_target_total, @v1_count, @v1_wins, @v2_count, @v2_wins, @entry_count,
                        @entry_wins, @equipment_value, @money_saved, @kill_reward, @live_time,
                        @head_shot_kills, @cash_earned, @enemies_flashed)
                    ON DUPLICATE KEY UPDATE
                        team = @team, name = @name, kills = @kills, deaths = @deaths, damage = @damage,
                        assists = @assists, enemy5ks = @enemy5ks, enemy4ks = @enemy4ks, enemy3ks = @enemy3ks,
                        enemy2ks = @enemy2ks, utility_count = @utility_count, utility_damage = @utility_damage,
                        utility_successes = @utility_successes, utility_enemies = @utility_enemies,
                        flash_count = @flash_count, flash_successes = @flash_successes,
                        health_points_removed_total = @health_points_removed_total,
                        health_points_dealt_total = @health_points_dealt_total,
                        shots_fired_total = @shots_fired_total, shots_on_target_total = @shots_on_target_total,
                        v1_count = @v1_count, v1_wins = @v1_wins, v2_count = @v2_count, v2_wins = @v2_wins,
                        entry_count = @entry_count, entry_wins = @entry_wins,
                        equipment_value = @equipment_value, money_saved = @money_saved,
                        kill_reward = @kill_reward, live_time = @live_time, head_shot_kills = @head_shot_kills,
                        cash_earned = @cash_earned, enemies_flashed = @enemies_flashed";

                    if (connection is SqliteConnection) {
                        sqlQuery = @"
                        INSERT OR REPLACE INTO matchzy_player_stats (
                            matchid, steamid64, team, name, kills, deaths, damage, assists,
                            enemy5ks, enemy4ks, enemy3ks, enemy2ks, utility_count, utility_damage,
                            utility_successes, utility_enemies, flash_count, flash_successes,
                            health_points_removed_total, health_points_dealt_total, shots_fired_total,
                            shots_on_target_total, v1_count, v1_wins, v2_count, v2_wins, entry_count, entry_wins,
                            equipment_value, money_saved, kill_reward, live_time, head_shot_kills,
                            cash_earned, enemies_flashed)
                        VALUES (
                            @matchId, @steamid64, @team, @name, @kills, @deaths, @damage, @assists,
                            @enemy5ks, @enemy4ks, @enemy3ks, @enemy2ks, @utility_count, @utility_damage,
                            @utility_successes, @utility_enemies, @flash_count, @flash_successes,
                            @health_points_removed_total, @health_points_dealt_total, @shots_fired_total,
                            @shots_on_target_total, @v1_count, @v1_wins, @v2_count, @v2_wins, @entry_count,
                            @entry_wins, @equipment_value, @money_saved, @kill_reward, @live_time,
                            @head_shot_kills, @cash_earned, @enemies_flashed)";
                    }

                    connection.Execute(sqlQuery,
                        new
                        {
                            matchId,
                            steamid64,
                            team = teamName,
                            name = player.PlayerName,
                            kills = playerStats.Kills,
                            deaths = playerStats.Deaths,
                            damage = playerStats.Damage,
                            assists = playerStats.Assists,
                            enemy5ks = playerStats.Enemy5Ks,
                            enemy4ks = playerStats.Enemy4Ks,
                            enemy3ks = playerStats.Enemy3Ks,
                            enemy2ks = enemy2Ks,
                            utility_count = utilityCount,
                            utility_damage = playerStats.UtilityDamage,
                            utility_successes = utilitySuccess,
                            utility_enemies = utilityEnemies,
                            flash_count = flashCount,
                            flash_successes = flashSuccess,
                            health_points_removed_total = healthPointsRemovedTotal,
                            health_points_dealt_total = healthPointsDealtTotal,
                            shots_fired_total = shotsFiredTotal,
                            shots_on_target_total = shotsOnTargetTotal,
                            v1_count = v1Count,
                            v1_wins = v1Wins,
                            v2_count = v2Count,
                            v2_wins = v2Wins,
                            entry_count = entryCount,
                            entry_wins = entryWins,
                            equipment_value = playerStats.EquipmentValue,
                            money_saved = playerStats.MoneySaved,
                            kill_reward = playerStats.KillReward,
                            live_time = playerStats.LiveTime,
                            head_shot_kills = playerStats.HeadShotKills,
                            cash_earned = playerStats.CashEarned,
                            enemies_flashed = playerStats.EnemiesFlashed
                        });

                    Log($"[UpdatePlayerStats] Data inserted/updated for player {steamid64} in match {matchId}");
                }
            }
            catch (Exception ex)
            {
                Log($"[UpdatePlayerStats - FATAL] Error inserting/updating data: {ex.Message}");
            }
        }

        public void WritePlayerStatsToCsv(string filePath, long matchId)
        {
            try {
                string csvFilePath = $"{filePath}/match_data_{matchId}.csv";
                string? directoryPath = Path.GetDirectoryName(csvFilePath);
                if (directoryPath != null)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }

                using (var writer = new StreamWriter(csvFilePath))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    IEnumerable<dynamic> playerStatsData = connection.Query(
                        "SELECT * FROM matchzy_player_stats WHERE matchid = @MatchId ORDER BY team, kills DESC", new { MatchId = matchId });

                    // Use the first data row to get the column names
                    dynamic? firstDataRow = playerStatsData.FirstOrDefault();
                    if (firstDataRow != null)
                    {
                        foreach (var propertyName in ((IDictionary<string, object>)firstDataRow).Keys)
                        {
                            csv.WriteField(propertyName);
                        }
                        csv.NextRecord(); // End of the column names row

                        // Write data to the CSV file
                        foreach (var playerStats in playerStatsData)
                        {
                            foreach (var propertyValue in ((IDictionary<string, object>)playerStats).Values)
                            {
                                csv.WriteField(propertyValue);
                            }
                            csv.NextRecord();
                        }
                    }
                }
                Log($"[WritePlayerStatsToCsv] Match stats for ID: {matchId} written successfully at: {csvFilePath}");
            }
            catch (Exception ex)
            {
                Log($"[WritePlayerStatsToCsv - FATAL] Error writing data: {ex.Message}");
            }

        }

        private void CreateDefaultConfigFile(string configFile)
        {
            // Create a default configuration
            DatabaseConfig defaultConfig = new DatabaseConfig
            {
                DatabaseType = "SQLite",
                MySqlHost = "your_mysql_host",
                MySqlDatabase = "your_mysql_database",
                MySqlUsername = "your_mysql_username",
                MySqlPassword = "your_mysql_password",
                MySqlPort = 3306
            };

            // Serialize and save the default configuration to the file
            string defaultConfigJson = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFile, defaultConfigJson);

            Log($"[InitializeDatabase] Default configuration file created at: {configFile}");
        }

        private void SetDatabaseConfig(string directory)
        {
            string fileName = "database.json";
            string configFile = Path.Combine(Server.GameDirectory + "/csgo/cfg/MatchZy", fileName);
            if (!File.Exists(configFile))
            {
                // Create a default configuration if the file doesn't exist
                Log($"[InitializeDatabase] database.json doesn't exist, creating default!");
                CreateDefaultConfigFile(configFile);
            }

            try
            {
                string jsonContent = File.ReadAllText(configFile);
                config = JsonSerializer.Deserialize<DatabaseConfig>(jsonContent);
                // Set the database type
                if (config.DatabaseType.Trim().ToLower() == "mysql") {
                    databaseType = DatabaseType.MySQL;
                } else {
                    databaseType = DatabaseType.SQLite;
                }
                
            }
            catch (JsonException ex)
            {
                Log($"[TryDeserializeConfig - ERROR] Error deserializing database.json: {ex.Message}. Using SQLite DB");
                databaseType = DatabaseType.SQLite;
            }
        }

        private void Log(string message)
        {
            Console.WriteLine("[MatchZy] " + message);
        }

        public enum DatabaseType
        {
            SQLite,
            MySQL
        }
    }

    public class DatabaseConfig
    {
        public string DatabaseType { get; set; }
        public string MySqlHost { get; set; }
        public string MySqlDatabase { get; set; }
        public string MySqlUsername { get; set; }
        public string MySqlPassword { get; set; }
        public int MySqlPort { get; set; }
    }

}
