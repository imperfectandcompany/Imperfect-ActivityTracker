﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using ImperfectActivityTracker.Helpers;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;
using System.Net;

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

                    string ipAddress = player.IpAddress ?? "0.0.0.0";

                    PlayerData data = new PlayerData
                    {
                        PlayerName = player.PlayerName,
                        SteamId = playerSteamId,
                        IpAddress = ipAddress,
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
                    PlayerCacheData playerCacheData = playerData.CacheData;

                    if (playerCacheData != null)
                    {
                        await ExecuteTimeUpdateAsync(playerData.PlayerName, playerData.SteamId, playerData.IpAddress, playerData.CacheData.PlayerTimeData);
                    }
                }
            });
        }

        public void SavePlayerCache(CCSPlayerController player, bool remove)
        {
            PlayerCacheData cacheData = PlayerCache.Instance.GetPlayerData(player);

            string playerName = player.PlayerName;
            string playerSteamId = player.SteamID.ToString();
            string ipAddress = player.IpAddress ?? "0.0.0.0";

            _ = SavePlayerDataAsync(playerName, playerSteamId, ipAddress, cacheData, remove);
        }

        private async Task SavePlayerDataAsync(string playerName, string steamId, string ipAddress, PlayerCacheData cacheData, bool remove)
        {
            await _databaseManager.ExecuteTransactionAsync(async (connection, transaction) =>
            {

                if (cacheData.PlayerTimeData != null)
                    await ExecuteTimeUpdateAsync(playerName, steamId, ipAddress, cacheData.PlayerTimeData);
            });

            if (remove)
                PlayerCache.Instance.RemovePlayer(ulong.Parse(steamId));
        }

        private void LoadPlayerCache(CCSPlayerController player)
        {
            string combinedQuery = $@"
					INSERT INTO `user_activity` (`name`, `steam_id`, `ip_address`)
					VALUES (
						@escapedName,
						@steamid,
                        @ipAddress
					)
					ON DUPLICATE KEY UPDATE
                        `ip_address` = @ipAddress,
						`name` = @escapedName;

					SELECT *
					FROM `user_activity`
					WHERE `steam_id` = @steamid;
				";

            ulong steamID = player.SteamID;

            string ipAddress = player.IpAddress ?? "0.0.0.0";

            MySqlParameter[] parameters = new MySqlParameter[]
            {
                new MySqlParameter("@escapedName", player.PlayerName),
                new MySqlParameter("@steamid", steamID),
                new MySqlParameter("@ipAddress", ipAddress)
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

            DateTime now = DateTime.UtcNow;

            /// Create the current times and get/create the server time data list
            timeData = new TimeData
            {
                Times = new Dictionary<string, DateTime>
                {
                    { "Surfing", now },
                    { "Spec", now }
                },
                PlayerSurfingTimeData = GetOrCreateJson(row)
            };

            GetOrCreateCurrentServerAndMapData(timeData);

            PlayerCache.Instance.AddOrUpdatePlayer(steamID, new PlayerCacheData
            {
                PlayerTimeData = timeData
            });
        }

        private (ServerTimeData, MapTimeData) GetOrCreateCurrentServerAndMapData(TimeData timeData)
        {
            var currentServerTimeData = GetOrCreateCurrentServerData(timeData);
            var currentMapTimeData = new MapTimeData();

            if (currentServerTimeData != null)
            {
                currentMapTimeData = GetOrCreateCurrentMapTimeData(timeData, currentServerTimeData);
            }
            else
            {
                _logger.LogError("Something went wrong getting the server time data.");
            }

            return (currentServerTimeData, currentMapTimeData);
        }

        private ServerTimeData? GetOrCreateCurrentServerData(TimeData timeData)
        {
            if (string.IsNullOrEmpty(ServerIpAddress)
                || timeData == null)
            {
                _logger.LogError("Unable to create server time data, the server IP or TimeData is null or empty");
                return null;
            }
            else
            {
                /// Get the server time data if it exists, create it if it doesn't
                var currentServerTimeData = timeData
                .PlayerSurfingTimeData
                .ServerTimeDataList
                .FirstOrDefault(server => server.ServerIp == ServerIpAddress);

                if (currentServerTimeData != null)
                {
                    /// Since it already exists in the time data, no need to add it, we're done with this step and can return
                    return currentServerTimeData;
                }
                else
                {
                    /// It doesn't exists, so we need to create the new server time data
                    currentServerTimeData = new ServerTimeData()
                    {
                        ServerIp = ServerIpAddress,
                        ServerSpecTime = 0,
                        ServerSurfingTime = 0,
                        FirstTimeJoined = DateTime.Now
                    };

                    /// And add it to the time data and the return
                    timeData
                        .PlayerSurfingTimeData
                        .ServerTimeDataList
                        .Add(currentServerTimeData);

                    return currentServerTimeData;
                }
            }
        }

        private MapTimeData GetOrCreateCurrentMapTimeData(TimeData timeData, ServerTimeData currentServerTimeData)
        {
            if (string.IsNullOrEmpty(CurrentMapName)
                || timeData == null)
            {
                _logger.LogError("Unable to create map time data, the map name is null or empty");
                return null;
            }
            else
            {
                /// Get the map time data if it exists, create it if it doesn't
                var currentMapTimeData = currentServerTimeData
                    .Maps
                    .FirstOrDefault(map => map.MapName == CurrentMapName);

                if (currentMapTimeData != null)
                {
                    /// If it isn't null, the map already exists in the list, nothing more to do, we can return
                    return currentMapTimeData;
                }
                else
                {
                    /// It is null, the map doesn't exist. Create it and add it
                    currentMapTimeData = new MapTimeData()
                    {
                        MapName = CurrentMapName,
                        //TODO: Get map workshop id
                        //WorkshopID = GetMapWorkshopId(),
                        SurfingTime = 0,
                        SpecTime = 0,
                        FirstTimePlayed = DateTime.Now
                    };

                    currentServerTimeData.Maps.Add(currentMapTimeData);

                    return currentMapTimeData;
                }
            }
        }

        private SurfingTimeData GetOrCreateJson(DataRow rowData)
        {
            /// Get the time data from the SQL row and make sure it's a string
            var surfingTimeDataJson = Convert.ToString(rowData["time_data"]);
            SurfingTimeData surfingTimeData;

            if (string.IsNullOrEmpty(surfingTimeDataJson))
            {
                /// JSON time data was empty, there is not current entry for this user, create a new one

                surfingTimeData = new SurfingTimeData() 
                {
                    ServerTimeDataList = new List<ServerTimeData>(),
                    TotalSurfingTime = 0,
                    TotalSpecTime = 0
                };
            }
            else
            {
                /// JSON time data was not empty, deserialize the it into a list of SurfingTimeData objects
                surfingTimeData = JsonHelpers.DeserializeJson<SurfingTimeData>(surfingTimeDataJson);
            }

            return surfingTimeData;
        }

        private void LoadAllPlayersCache()
        {
            List<CCSPlayerController> players = Utilities
                .GetPlayers()
                .Where(player => player?.IsValid == true
                       && player.PlayerPawn?.IsValid == true
                       && !player.IsBot && !player.IsHLTV
                       && player.SteamID.ToString().Length == 17)
                .ToList();

            if (players.Count == 0)
                return;

            string combinedQuery = $@"SELECT `steam_id`, `time_data`
                                    FROM `user_activity`
                                    WHERE `steam_id` = (" + string.Join(",", players.Select(player => $"'{player.SteamID}'")) + ");";

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

        private async Task ExecuteTimeUpdateAsync(string playerName, string steamId, string ipAddress, TimeData timeData)
        {
            /// Serialize the list of ServerTimeData objects into a JSON string for saving
            var surfingTimeData = JsonHelpers.SerializeJson(timeData.PlayerSurfingTimeData);

            /// Insert the name, steam id and time data into the database
            /// Or update the name and time data if it already exists
            string query = $@"INSERT INTO `user_activity` (`name`, `steam_id`, `ip_address`, `time_data`)
                      VALUES (@playerName, @steamId, @ipAddress, @surfingTimeData)
                      ON DUPLICATE KEY UPDATE `name` = @playerName, `time_data` = @surfingTimeData;";

            List<MySqlParameter> parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@playerName", playerName),
                new MySqlParameter("@steamId", steamId),
                new MySqlParameter("@ipAddress", ipAddress),
                new MySqlParameter("@surfingTimeData", surfingTimeData)
            };

            await _databaseManager.ExecuteNonQueryAsync(query, parameters.ToArray());
        }
    }
}

