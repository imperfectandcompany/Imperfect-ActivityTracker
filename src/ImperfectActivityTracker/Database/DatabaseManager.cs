using ImperfectActivityTracker.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Serilog.Core;


namespace ImperfectActivityTracker.Database
{
    public class DatabaseManager
    {
        private static readonly Lazy<DatabaseManager> _instance = new(() => new DatabaseManager(_logger));
        public static DatabaseManager Instance => _instance.Value;

        public static ILogger<DatabaseManager> _logger { get; set;  }

        private string? _connectionString;

        public DatabaseManager(ILogger<DatabaseManager> logger)
        {
            _logger = logger;
        }

        public void Initialize(Config config)
        {
            _connectionString = BuildConnectionString(config.DatabaseSettings);

            Task.Run(Instance.CreateTablesAsync).Wait();
        }

        private string BuildConnectionString(DatabaseSettings databaseSettings)
        {
            var connectionString = "";

            if (string.IsNullOrEmpty(databaseSettings.DatabaseHost)
                || databaseSettings.DatabasePort <= 0
                || string.IsNullOrEmpty(databaseSettings.DatabaseName)
                || string.IsNullOrEmpty(databaseSettings.DatabaseUser)
                || string.IsNullOrEmpty(databaseSettings.DatabasePassword))
            {
                /// Needed db connection information wasn't in the config
               _logger.LogError("Database connection string could not be created. Make sure to include all database information in the config.");
            }
            else
            {
                connectionString = new MySqlConnectionStringBuilder
                {
                    Server = databaseSettings.DatabaseHost,
                    Database = databaseSettings.DatabaseName,
                    UserID = databaseSettings.DatabaseUser,
                    Password = databaseSettings.DatabasePassword,
                    Port = databaseSettings.DatabasePort
                }.ConnectionString;
            }

            return connectionString;
        }

        public async Task ExecuteWithTransactionAsync(Func<MySqlConnection, MySqlTransaction, Task> executeActions)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (MySqlTransaction transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        await executeActions(connection, transaction);
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError("Something happened executing SQL transaction: {message}", ex.Message);
                    }
                }
            }
        }

        public async Task<bool> CreateTablesAsync()
        {
            string timesModuleTableQuery = @$"CREATE TABLE IF NOT EXISTS `user_activity` (
					`steam_id` VARCHAR(32) COLLATE 'utf8mb4_unicode_ci' UNIQUE NOT NULL,
					`name` VARCHAR(255) COLLATE 'utf8mb4_unicode_ci' NOT NULL,
					`time` INT NOT NULL DEFAULT 0,
					UNIQUE (`steam_id`)
				) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            await ExecuteWithTransactionAsync(async (connection, transaction) =>
            {
                MySqlCommand? command1 = new MySqlCommand(timesModuleTableQuery, connection, transaction);
                await command1.ExecuteNonQueryAsync();
            });

            return true;
        }
    }
}
