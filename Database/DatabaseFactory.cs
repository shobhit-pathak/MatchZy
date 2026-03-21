namespace MatchZy
{
    public static class DatabaseFactory
    {
        public static IMatchDatabase Create(string directory)
        {
            DatabaseConfig config = BaseDatabase.ReadDatabaseConfig(directory);

            return config.DatabaseType?.Trim().ToLower() switch
            {
                "mysql" => new MySqlDatabase(config),
                "postgresql" or "postgres" => new PostgresDatabase(config),
                _ => new SqliteDatabase()
            };
        }
    }
}
