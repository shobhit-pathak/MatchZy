using System.Data;
using System.Globalization;
using System.Text.Json;
using CounterStrikeSharp.API;
using CsvHelper;
using CsvHelper.Configuration;
using Dapper;

namespace MatchZy
{
    public abstract class BaseDatabase : IMatchDatabase
    {
        protected IDbConnection connection = null!;

        public abstract void InitializeDatabase(string directory);
        public abstract long InitMatch(string team1name, string team2name, string serverIp, bool isMatchSetup, long liveMatchId, int mapNumber, string seriesType, MatchConfig matchConfig);
        public abstract void UpdateTeamData(int matchId, string team1name, string team2name);
        public abstract Task SetMapEndData(long matchId, int mapNumber, string winnerName, int t1score, int t2score, int team1SeriesScore, int team2SeriesScore);
        public abstract Task SetMatchEndData(long matchId, string winnerName, int t1score, int t2score);
        public abstract Task UpdateMapStatsAsync(long matchId, int mapNumber, int t1score, int t2score);
        public abstract Task UpdatePlayerStatsAsync(long matchId, int mapNumber, Dictionary<ulong, Dictionary<string, object>> playerStatsDictionary);

        public async Task WritePlayerStatsToCsv(string filePath, long matchId, int mapNumber)
        {
            try
            {
                string csvFilePath = $"{filePath}/match_data_map{mapNumber}_{matchId}.csv";
                string? directoryPath = System.IO.Path.GetDirectoryName(csvFilePath);
                if (directoryPath != null && !System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                }

                using var writer = new System.IO.StreamWriter(csvFilePath);
                using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

                IEnumerable<dynamic> playerStatsData = await connection.QueryAsync(
                    "SELECT * FROM matchzy_stats_players WHERE matchid = @MatchId AND mapnumber = @MapNumber ORDER BY team, kills DESC",
                    new { MatchId = matchId, MapNumber = mapNumber });

                dynamic? firstDataRow = playerStatsData.FirstOrDefault();
                if (firstDataRow != null)
                {
                    foreach (var propertyName in ((IDictionary<string, object>)firstDataRow).Keys)
                        csv.WriteField(propertyName);
                    csv.NextRecord();

                    foreach (var playerStats in playerStatsData)
                    {
                        foreach (var propertyValue in ((IDictionary<string, object>)playerStats).Values)
                            csv.WriteField(propertyValue);
                        csv.NextRecord();
                    }
                }

                Log($"[WritePlayerStatsToCsv] Match stats for ID: {matchId} written successfully at: {csvFilePath}");
            }
            catch (Exception ex)
            {
                Log($"[WritePlayerStatsToCsv - FATAL] Error writing data: {ex.Message}");
            }
        }

        public static DatabaseConfig ReadDatabaseConfig(string directory)
        {
            string configFile = System.IO.Path.Combine(Server.GameDirectory + "/csgo/cfg/MatchZy", "database.json");

            if (!System.IO.File.Exists(configFile))
            {
                Log($"[InitializeDatabase] database.json doesn't exist, creating default!");
                CreateDefaultConfigFile(configFile);
            }

            try
            {
                string jsonContent = System.IO.File.ReadAllText(configFile);
                return JsonSerializer.Deserialize<DatabaseConfig>(jsonContent) ?? new DatabaseConfig();
            }
            catch (JsonException ex)
            {
                Log($"[ReadDatabaseConfig - ERROR] Error deserializing database.json: {ex.Message}. Using SQLite DB");
                return new DatabaseConfig();
            }
        }

        private static void CreateDefaultConfigFile(string configFile)
        {
            var defaultConfig = new DatabaseConfig
            {
                DatabaseType = "SQLite",
                MySqlHost = "your_mysql_host",
                MySqlDatabase = "your_mysql_database",
                MySqlUsername = "your_mysql_username",
                MySqlPassword = "your_mysql_password",
                MySqlPort = 3306,
                PostgresHost = "your_postgres_host",
                PostgresDatabase = "your_postgres_database",
                PostgresUsername = "your_postgres_username",
                PostgresPassword = "your_postgres_password",
                PostgresPort = 5432
            };

            string json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(configFile, json);
            Log($"[InitializeDatabase] Default configuration file created at: {configFile}");
        }

        protected static void Log(string message) => Console.WriteLine("[MatchZy] " + message);

        protected static object BuildPlayerParams(long matchId, int mapNumber, ulong steamid64, Dictionary<string, object> playerStats) =>
            new
            {
                matchId, mapNumber, steamid64,
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
            };
    }

    public class DatabaseConfig
    {
        public string? DatabaseType { get; set; }
        public string? MySqlHost { get; set; }
        public string? MySqlDatabase { get; set; }
        public string? MySqlUsername { get; set; }
        public string? MySqlPassword { get; set; }
        public int? MySqlPort { get; set; }
        public string? PostgresHost { get; set; }
        public string? PostgresDatabase { get; set; }
        public string? PostgresUsername { get; set; }
        public string? PostgresPassword { get; set; }
        public int? PostgresPort { get; set; }
    }
}
