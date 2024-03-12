using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;

namespace ImperfectActivityTracker
{
    public partial class ImperfectActivityTracker
    {
        public List<PlayerData> PreparePlayersData()
        {
            List<PlayerData> playersData = new List<PlayerData>();
            List<CCSPlayerController> players = Utilities.GetPlayers()
                .Where(p => p?.IsValid == true
                    && p.PlayerPawn?.IsValid == true
                    && !p.IsBot && !p.IsHLTV
                    && p.Connected == PlayerConnectedState.PlayerConnected
                    && p.SteamID.ToString().Length == 17
                    && PlayerCache.Instance.ContainsPlayer(p))
                .ToList();

            foreach (CCSPlayerController player in players)
            {
                try
                {
                    SteamID steamId = new SteamID(player.SteamID);

                    if (!steamId.IsValid())
                        continue;

                    string playerSteamId = steamId.SteamId64.ToString();

                    PlayerData data = new PlayerData
                    {
                        PlayerName = player.PlayerName,
                        SteamId = playerSteamId,
                        CacheData = PlayerCache.Instance.GetPlayerData(player)
                    };

                    playersData.Add(data);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"PreparePlayersData > {player.PlayerName} > {ex.Message}");
                }
            }

            return playersData;
        }

        public void SaveAllPlayersCache()
        {
            List<PlayerData> playersData = PreparePlayersData();
            _ = SaveAllPlayersCacheAsync(playersData);
        }

        public async Task SaveAllPlayersCacheAsync(List<PlayerData> playersData)
        {
            await _databaseManager.ExecuteTransactionAsync(async (connection, transaction) =>
            {
                foreach (PlayerData playerData in playersData)
                {
                    if (playerData.CacheData.PlayerTimeData != null)
                        await ExecuteTimeUpdateAsync(playerData.PlayerName, playerData.SteamId, playerData.CacheData.PlayerTimeData);
                }
            });
        }

        public void SavePlayerCache(CCSPlayerController player, bool remove)
        {
            PlayerCacheData cacheData = PlayerCache.Instance.GetPlayerData(player);

            string playerName = player.PlayerName;
            string playerSteamId = player.SteamID.ToString();

            _ = SavePlayerDataAsync(playerName, playerSteamId, cacheData, remove);
        }

        private async Task SavePlayerDataAsync(string playerName, string steamId, PlayerCacheData cacheData, bool remove)
        {
            await _databaseManager.ExecuteTransactionAsync(async (connection, transaction) =>
            {

                if (cacheData.PlayerTimeData != null)
                    await ExecuteTimeUpdateAsync(playerName, steamId, cacheData.PlayerTimeData);
            });

            if (remove)
                PlayerCache.Instance.RemovePlayer(ulong.Parse(steamId));
        }

        private void LoadPlayerCache(CCSPlayerController player)
        {
            /// TODO: What do we want to call this table? (user_activity)
            string combinedQuery = $@"
					INSERT INTO `user_activity` (`name`, `steam_id`)
					VALUES (
						@escapedName,
						@steamid
					)
					ON DUPLICATE KEY UPDATE
						`name` = @escapedName;

					SELECT *
					FROM `user_activity`
					WHERE `steam_id` = @steamid;
				";

            ulong steamID = player.SteamID;

            MySqlParameter[] parameters = new MySqlParameter[]
            {
                new MySqlParameter("@escapedName", player.PlayerName),
                new MySqlParameter("@steamid", steamID),
            };

            _ = LoadPlayerCacheAsync(steamID, combinedQuery, parameters);
        }

        public async Task LoadPlayerCacheAsync(ulong steamID, string combinedQuery, MySqlParameter[] parameters)
        {
            try
            {
                using (MySqlCommand command = new MySqlCommand(combinedQuery))
                {
                    DataTable dataTable = await _databaseManager.ExecuteReaderAsync(combinedQuery, parameters);

                    if (dataTable.Rows.Count > 0)
                    {
                        foreach (DataRow row in dataTable.Rows)
                        {
                            LoadPlayerRowToCache(steamID, row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"A problem occurred while loading single player cache: {ex.Message}");
            }
        }

        public void LoadPlayerRowToCache(ulong steamID, DataRow row)
        {
            TimeData? timeData = null;

            Dictionary<string, int> TimeFields = new Dictionary<string, int>();

            string[] timeFieldNames = { "surfing", "spec" };

            foreach (string timeField in timeFieldNames)
            {
                TimeFields[timeField] = Convert.ToInt32(row[timeField]);
            }

            DateTime now = DateTime.UtcNow;

            timeData = new TimeData
            {
                TimeFields = TimeFields,
                Times = new Dictionary<string, DateTime>
                    {
                        { "Surfing", now },
                        { "Spec", now }
                    }
            };

            PlayerCache.Instance.AddOrUpdatePlayer(steamID, new PlayerCacheData
            {
                PlayerTimeData = timeData
            });
        }
        private void LoadAllPlayersCache()
        {
            List<CCSPlayerController> players = Utilities.GetPlayers().Where(player => player?.IsValid == true && player.PlayerPawn?.IsValid == true && !player.IsBot && !player.IsHLTV && player.SteamID.ToString().Length == 17).ToList();

            if (players.Count == 0)
                return;

            string combinedQuery = $@"SELECT *
					                  FROM `user_activity`
					                  WHERE `steam_id` = {players.Select(p => p.SteamID)}";

            try
            {
                _ = LoadAllPlayersCacheAsync(combinedQuery);
            }
            catch (Exception ex)
            {
                Logger.LogError($"LoadAllPlayersCache > {ex.Message}");
            }
        }

        public async Task LoadAllPlayersCacheAsync(string combinedQuery)
        {
            try
            {
                using (MySqlCommand command = new MySqlCommand(combinedQuery))
                {
                    DataTable dataTable = await _databaseManager.ExecuteReaderAsync(command.CommandText);

                    if (dataTable.Rows.Count > 0)
                    {
                        foreach (DataRow row in dataTable.Rows)
                        {
                            ulong steamID = Convert.ToUInt64(row["steam_id"]);

                            if (steamID == 0)
                                continue;

                            LoadPlayerRowToCache(steamID, row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"A problem occurred while loading all players cache: {ex.Message}");
            }
        }

        private async Task ExecuteTimeUpdateAsync(string playerName, string steamId, TimeData timeData)
        {
            string fieldsForInsert = string.Join(", ", timeData.TimeFields.Select(f => $"`{f.Key}`"));
            string valuesForInsert = string.Join(", ", timeData.TimeFields.Select(f => $"@{f.Key}"));
            string onDuplicateKeyUpdate = string.Join(", ", timeData.TimeFields.Select(f => $"`{f.Key}` = @{f.Key}"));

            string query = $@"INSERT INTO `user_activity` (`name`, `steam_id`, {fieldsForInsert})
                      VALUES (@playerName, @steamId, {valuesForInsert})
                      ON DUPLICATE KEY UPDATE `name` = @playerName, {onDuplicateKeyUpdate};";

            List<MySqlParameter> parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@playerName", playerName),
                new MySqlParameter("@steamId", steamId)
            };

            parameters.AddRange(timeData.TimeFields.Select(f => new MySqlParameter($"@{f.Key}", f.Value)));

            await _databaseManager.ExecuteNonQueryAsync(query, parameters.ToArray());
        }
    }
}
