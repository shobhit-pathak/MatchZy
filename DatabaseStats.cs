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

        DatabaseConfig? config;
        public DatabaseType databaseType { get; set; }

        public void InitializeDatabase(string directory)
        {
            ConnectDatabase(directory);
            try
            {
                connection.Open();
                string dbType = (connection is SqliteConnection) ? "SQLite" : "MySQL";
                Log($"[InitializeDatabase] {dbType} Database connection successful");

                // Create the `matchzy_stats_matches`, `matchzy_stats_players` and `matchzy_stats_maps` tables if they doesn't exist
                if (connection is SqliteConnection) {
                    CreateRequiredTablesSQLite();
                } else {
                    CreateRequiredTablesSQL();
                }

                Log("[InitializeDatabase] Table matchzy_stats_matches created (or already exists)");
                Log("[InitializeDatabase] Table matchzy_stats_players created (or already exists)");
                Log("[InitializeDatabase] Table matchzy_stats_maps created (or already exists)");
            }
            catch (Exception ex)
            {
                Log($"[InitializeDatabase - FATAL] Database connection or table creation error: {ex.Message}");
            }
        }

        public void ConnectDatabase(string directory)
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
                else if (config != null && databaseType == DatabaseType.MySQL)
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

        }

        public void CreateRequiredTablesSQLite()
        {
            connection.Execute($@"
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

        public void CreateRequiredTablesSQL()
        {
            connection.Execute($@"
                CREATE TABLE IF NOT EXISTS matchzy_stats_matches (
                    matchid INT PRIMARY KEY AUTO_INCREMENT,
                    start_time DATETIME NOT NULL,
                    end_time DATETIME DEFAULT NULL,
                    winner VARCHAR(255) NOT NULL DEFAULT '',
                    series_type VARCHAR(255) NOT NULL DEFAULT '',
                    team1_name VARCHAR(255) NOT NULL DEFAULT '',
                    team1_score INT NOT NULL DEFAULT 0,
                    team2_name VARCHAR(255) NOT NULL DEFAULT '',
                    team2_score INT NOT NULL DEFAULT 0,
                    server_ip VARCHAR(255) NOT NULL DEFAULT '0'
                )");
                
            connection.Execute($@"
            CREATE TABLE IF NOT EXISTS matchzy_stats_maps (
                matchid INT NOT NULL,
                mapnumber TINYINT(3) UNSIGNED NOT NULL,
                start_time DATETIME NOT NULL,
                end_time DATETIME DEFAULT NULL,
                winner VARCHAR(16) NOT NULL DEFAULT '',
                mapname VARCHAR(64) NOT NULL DEFAULT '',
                team1_score INT NOT NULL DEFAULT 0,
                team2_score INT NOT NULL DEFAULT 0,
                PRIMARY KEY (matchid, mapnumber),
                INDEX mapnumber_index (mapnumber),
                CONSTRAINT matchzy_stats_maps_matchid FOREIGN KEY (matchid) REFERENCES matchzy_stats_matches (matchid)
            )");

            connection.Execute($@"
            CREATE TABLE IF NOT EXISTS matchzy_stats_players (
                matchid INT NOT NULL,
                mapnumber TINYINT(3) UNSIGNED NOT NULL,
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
                FOREIGN KEY (matchid) REFERENCES matchzy_stats_matches (matchid),
                FOREIGN KEY (mapnumber) REFERENCES matchzy_stats_maps (mapnumber)
            )");
        }

        public long InitMatch(string team1name, string team2name, string serverIp, bool isMatchSetup, long liveMatchId, int mapNumber, string seriesType, MatchConfig matchConfig)
        {
            try
            {
                string mapName = isMatchSetup ? matchConfig.Maplist[mapNumber] : Server.MapName;
                string dateTimeExpression = (connection is SqliteConnection) ? "datetime('now')" : "NOW()";

                if (mapNumber == 0) {
                    if (isMatchSetup && liveMatchId != -1) {
                        connection.Execute(@"
                            INSERT INTO matchzy_stats_matches (matchid, start_time, team1_name, team2_name, series_type, server_ip)
                            VALUES (@liveMatchId, " + dateTimeExpression + ", @team1name, @team2name, @seriesType, @serverIp)",
                            new { liveMatchId, team1name, team2name, seriesType, serverIp });
                    } else {
                        connection.Execute(@"
                            INSERT INTO matchzy_stats_matches (start_time, team1_name, team2_name, series_type, server_ip)
                            VALUES (" + dateTimeExpression + ", @team1name, @team2name, @seriesType, @serverIp)",
                            new { team1name, team2name, seriesType, serverIp });
                    }
                }

                if (isMatchSetup && liveMatchId != -1) {
                    connection.Execute(@"
                        INSERT INTO matchzy_stats_maps (matchid, start_time, mapnumber, mapname)
                        VALUES (@liveMatchId, " + dateTimeExpression + ", @mapNumber, @mapName)",
                        new { liveMatchId, mapNumber, mapName });
                    return liveMatchId;
                }

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

                connection.Execute(@"
                    INSERT INTO matchzy_stats_maps (matchid, start_time, mapnumber, mapname)
                    VALUES (@matchId, " + dateTimeExpression + ", @mapNumber, @mapName)",
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

        public void UpdateTeamData(int matchId, string team1name, string team2name) {
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

        public async Task SetMapEndData(long matchId, int mapNumber, string winnerName, int t1score, int t2score, int team1SeriesScore, int team2SeriesScore)
        {
            try
            {
                string dateTimeExpression = (connection is SqliteConnection) ? "datetime('now')" : "NOW()";

                string sqlQuery = $@"
                    UPDATE matchzy_stats_maps
                    SET winner = @winnerName, end_time = {dateTimeExpression}, team1_score = @t1score, team2_score = @t2score
                    WHERE matchid = @matchId AND mapNumber = @mapNumber";

                await connection.ExecuteAsync(sqlQuery, new { matchId, winnerName, t1score, t2score, mapNumber });

                sqlQuery = $@"
                    UPDATE matchzy_stats_matches
                    SET team1_score = @team1SeriesScore, team2_score = @team2SeriesScore
                    WHERE matchid = @matchId";

                await connection.ExecuteAsync(sqlQuery, new { matchId, team1SeriesScore, team2SeriesScore });

                Log($"[SetMapEndData] Data updated for matchId: {matchId} mapNumber: {mapNumber} winnerName: {winnerName}");
            }
            catch (Exception ex)
            {
                Log($"[SetMapEndData - FATAL] Error updating data of matchId: {matchId} mapNumber: {mapNumber} [ERROR]: {ex.Message}");
            } 
        }

        public async Task SetMatchEndData(long matchId, string winnerName, int t1score, int t2score)
        {
            try
            {
                string dateTimeExpression = (connection is SqliteConnection) ? "datetime('now')" : "NOW()";

                string sqlQuery = $@"
                    UPDATE matchzy_stats_matches
                    SET winner = @winnerName, end_time = {dateTimeExpression}, team1_score = @t1score, team2_score = @t2score
                    WHERE matchid = @matchId";

                await connection.ExecuteAsync(sqlQuery, new { matchId, winnerName, t1score, t2score });

                Log($"[SetMatchEndData] Data updated for matchId: {matchId} winnerName: {winnerName}");
            }
            catch (Exception ex)
            {
                Log($"[SetMatchEndData - FATAL] Error updating data of matchId: {matchId} [ERROR]: {ex.Message}");
            }
        }

        public async Task UpdateMapStatsAsync(long matchId, int mapNumber, int t1score, int t2score)
        {
            try
            {
                string sqlQuery = $@"
                    UPDATE matchzy_stats_maps
                    SET team1_score = @t1score, team2_score = @t2score
                    WHERE matchid = @matchId AND mapnumber = @mapNumber";

                await connection.ExecuteAsync(sqlQuery, new { matchId, mapNumber, t1score, t2score });
            }
            catch (Exception ex)
            {
                Log($"[UpdatePlayerStats - FATAL] Error updating data of matchId: {matchId} [ERROR]: {ex.Message}");
            }
        }

        public async Task UpdatePlayerStatsAsync(long matchId, int mapNumber, Dictionary<ulong, Dictionary<string, object>> playerStatsDictionary)
        {
            try
            {
                foreach (ulong steamid64 in playerStatsDictionary.Keys)
                {
                    Log($"[UpdatePlayerStats] Going to update data for Match: {matchId}, MapNumber: {mapNumber}, Player: {steamid64}");

                    var playerStats = playerStatsDictionary[steamid64];

                    string sqlQuery = $@"
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
                            @head_shot_kills, @cash_earned, @enemies_flashed)";
                    }

                    await connection.ExecuteAsync(sqlQuery,
                        new
                        {
                            matchId,
                            mapNumber,
                            steamid64,
                            team = playerStats["TeamName"],
                            name = playerStats["PlayerName"],
                            kills = playerStats["Kills"],
                            deaths = playerStats["Deaths"],
                            damage = playerStats["Damage"],
                            assists = playerStats["Assists"],
                            enemy5ks = playerStats["Enemy5Ks"],
                            enemy4ks = playerStats["Enemy4Ks"],
                            enemy3ks = playerStats["Enemy3Ks"],
                            enemy2ks = playerStats["Enemy2Ks"],
                            utility_count = playerStats["UtilityCount"],
                            utility_damage = playerStats["UtilityDamage"],
                            utility_successes = playerStats["UtilitySuccess"],
                            utility_enemies = playerStats["UtilityEnemies"],
                            flash_count = playerStats["FlashCount"],
                            flash_successes = playerStats["FlashSuccess"],
                            health_points_removed_total = playerStats["HealthPointsRemovedTotal"],
                            health_points_dealt_total = playerStats["HealthPointsDealtTotal"],
                            shots_fired_total = playerStats["ShotsFiredTotal"],
                            shots_on_target_total = playerStats["ShotsOnTargetTotal"],
                            v1_count = playerStats["1v1Count"],
                            v1_wins = playerStats["1v1Wins"],
                            v2_count = playerStats["1v2Count"],
                            v2_wins = playerStats["1v2Wins"],
                            entry_count = playerStats["EntryCount"],
                            entry_wins = playerStats["EntryWins"],
                            equipment_value = playerStats["EquipmentValue"],
                            money_saved = playerStats["MoneySaved"],
                            kill_reward = playerStats["KillReward"],
                            live_time = playerStats["LiveTime"],
                            head_shot_kills = playerStats["HeadShotKills"],
                            cash_earned = playerStats["CashEarned"],
                            enemies_flashed = playerStats["EnemiesFlashed"]
                        });

                    Log($"[UpdatePlayerStats] Data inserted/updated for player {steamid64} in match {matchId}");
                }
            }
            catch (Exception ex)
            {
                Log($"[UpdatePlayerStats - FATAL] Error inserting/updating data: {ex.Message}");
            }
        }

        public async Task WritePlayerStatsToCsv(string filePath, long matchId, int mapNumber)
        {
            try {
                string csvFilePath = $"{filePath}/match_data_map{mapNumber}_{matchId}.csv";
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
                    IEnumerable<dynamic> playerStatsData = await connection.QueryAsync(
                        "SELECT * FROM matchzy_stats_players WHERE matchid = @MatchId AND mapnumber = @MapNumber ORDER BY team, kills DESC", new { MatchId = matchId, MapNumber = mapNumber });

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
                if (config != null && config.DatabaseType?.Trim().ToLower() == "mysql") {
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
        public string? DatabaseType { get; set; }
        public string? MySqlHost { get; set; }
        public string? MySqlDatabase { get; set; }
        public string? MySqlUsername { get; set; }
        public string? MySqlPassword { get; set; }
        public int? MySqlPort { get; set; }
    }

}
