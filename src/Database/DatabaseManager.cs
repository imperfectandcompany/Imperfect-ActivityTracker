using ImperfectActivityTracker.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Serilog.Core;
using System.Data;


namespace ImperfectActivityTracker
{
    public class DatabaseManager
    {
        private string? _connectionString;

        public void Initialize(Config config)
        {
            _connectionString = BuildConnectionString(config.DatabaseSettings);

            Task.Run(CreateTableAsync).Wait();
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
                ImperfectActivityTracker._logger.LogError("Database connection string could not be created. Make sure to include all database information in the config.");
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

        public async Task ExecuteTransactionAsync(Func<MySqlConnection, MySqlTransaction, Task> executeActions)
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
                        ImperfectActivityTracker._logger.LogError("Something happened executing SQL transaction: {message}", ex.Message);
                    }
                }
            }
        }

        public async Task<DataTable> ExecuteReaderAsync(string query, params MySqlParameter[] parameters)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters);
                    using (MySqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        DataTable dataTable = new DataTable();
                        dataTable.Load(reader);
                        return dataTable;
                    }
                }
            }
        }

        public async Task ExecuteNonQueryAsync(string query, params MySqlParameter[] parameters)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> CreateTableAsync()
        {
            string timeTableQuery = @$"CREATE TABLE IF NOT EXISTS `user_activity` (
                                        `steam_id` VARCHAR(32) UNIQUE NOT NULL,
                                        `name` VARCHAR(255) NOT NULL,
					                    `all` INT NOT NULL DEFAULT 0,
					                    `ct` INT NOT NULL DEFAULT 0,
					                    `t` INT NOT NULL DEFAULT 0,
					                    `spec` INT NOT NULL DEFAULT 0,
					                    `dead` INT NOT NULL DEFAULT 0,
					                    `alive` INT NOT NULL DEFAULT 0,
                                        UNIQUE (`steam_id`))";
            try
            {
                await ExecuteTransactionAsync(async (connection, transaction) =>
                {
                    MySqlCommand? command = new MySqlCommand(timeTableQuery, connection, transaction);
                    await command.ExecuteNonQueryAsync();
                });
            }
            catch (Exception ex)
            {
                ImperfectActivityTracker._logger.LogError("Error creating table: {message}", ex.Message);
            }

            return true;
        }
    }
}
