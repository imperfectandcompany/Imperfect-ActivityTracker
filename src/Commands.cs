using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using ImperfectActivityTracker.Helpers;
using Microsoft.Extensions.Logging;

namespace ImperfectActivityTracker
{
    public partial class ImperfectActivityTracker
    {
        [ConsoleCommand("css_playtime", "Get a total surf/spec times for a player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void GetAllPlayTimeCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player != null)
            {
                // Get players time data from database using their steam ID
                var playerCacheData = PlayerCache.Instance.GetPlayerData(player);
                if (playerCacheData != null)
                {
                    var playerTimeData = playerCacheData.PlayerTimeData;

                    if (playerTimeData != null
                        && playerTimeData.PlayerSurfingTimeData != null)
                    {
                        var currentSurfTime = GetCurrentSurfingTime(playerTimeData);
                        var currentSpecTime = GetCurrentSpecTime(playerTimeData);

                        OutputTotalTime(commandInfo, playerTimeData, currentSurfTime, currentSpecTime);

                        OutputMapTime(commandInfo, playerTimeData, currentSurfTime, currentSpecTime);

                        OutputServerTime(commandInfo, playerTimeData, currentSurfTime, currentSpecTime);

                    }
                    else
                    {
                        commandInfo.ReplyToCommand($"No times found for {player.PlayerName}");
                        _logger.LogInformation($"No playtime found for {player.PlayerName ?? "player"} ({player.SteamID}). Either no times exists or something happened loading their surfing time data");
                    }
                }
            }
        }

        [ConsoleCommand("css_surftime", "Get a total time for when a player is surfing")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void GetSurfPlayTimeCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player != null)
            {
                // Get players time data from database using their steam ID
                var playerCacheData = PlayerCache.Instance.GetPlayerData(player);
                if (playerCacheData != null)
                {
                    var playerTimeData = playerCacheData.PlayerTimeData;

                    if (playerTimeData != null
                        && playerTimeData.PlayerSurfingTimeData != null)
                    {
                        var currentSurfTime = GetCurrentSurfingTime(playerTimeData);

                        OutputTotalTime(commandInfo, playerTimeData, currentSurfTime, null);

                        OutputMapTime(commandInfo, playerTimeData, currentSurfTime, null);

                        OutputServerTime(commandInfo, playerTimeData, currentSurfTime, null);

                    }
                    else
                    {
                        commandInfo.ReplyToCommand($"No times found for {player.PlayerName}");
                        _logger.LogInformation($"No playtime found for {player.PlayerName ?? "player"} ({player.SteamID}). Either no times exists or something happened loading their surfing time data");
                    }
                }
            }
        }

        [ConsoleCommand("css_spectime", "Get a total time for when a player is spectating")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void GetSpecPlayTimeCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player != null)
            {
                // Get players time data from database using their steam ID
                var playerCacheData = PlayerCache.Instance.GetPlayerData(player);
                if (playerCacheData != null)
                {
                    var playerTimeData = playerCacheData.PlayerTimeData;

                    if (playerTimeData != null
                        && playerTimeData.PlayerSurfingTimeData != null)
                    {
                        var currentSpecTime = GetCurrentSpecTime(playerTimeData);

                        OutputTotalTime(commandInfo, playerTimeData, null, currentSpecTime);

                        OutputMapTime(commandInfo, playerTimeData, null, currentSpecTime);

                        OutputServerTime(commandInfo, playerTimeData, null, currentSpecTime);

                    }
                    else
                    {
                        commandInfo.ReplyToCommand($"No times found for {player.PlayerName}");
                        _logger.LogInformation($"No playtime found for {player.PlayerName ?? "player"} ({player.SteamID}). Either no times exists or something happened loading their surfing time data");
                    }
                }
            }
        }

        private int GetCurrentSpecTime(TimeData playerTimeData)
        {
            var now = DateTime.UtcNow;

            return (int)(now - playerTimeData.Times["Spec"]).TotalSeconds;
        }

        private int GetCurrentSurfingTime(TimeData playerTimeData)
        {
            var now = DateTime.UtcNow;

            return (int)(now - playerTimeData.Times["Surfing"]).TotalSeconds;
        }

        private void OutputTotalTime(CommandInfo commandInfo, TimeData playerTimeData, int? currentSurfTime, int? currentSpecTime)
        {
            var totalSurfingTime = playerTimeData.PlayerSurfingTimeData.TotalSurfingTime + currentSurfTime ?? 0;
            var totalSpecTime = playerTimeData.PlayerSurfingTimeData.TotalSpecTime + currentSpecTime ?? 0;

            if (currentSurfTime != null)
            {
                commandInfo.ReplyToCommand($"Total time surfing: {TimeHelpers.FormatPlaytime(totalSurfingTime)}");
            }

            if (currentSpecTime != null)
            {
                commandInfo.ReplyToCommand($"Total time spectating: {TimeHelpers.FormatPlaytime(totalSpecTime)}");
            }
        }

        private void OutputServerTime(CommandInfo commandInfo, TimeData playerTimeData, int? currentSurfTime, int? currentSpecTime)
        {
            var serverTimeData = GetServerTimeData(playerTimeData, ServerIpAddress);
            var serverSurfTime = serverTimeData.ServerSurfingTime + currentSurfTime ?? 0;
            var serverSpecTime = serverTimeData.ServerSpecTime + currentSpecTime ?? 0;

            if (serverTimeData != null)
            {
                commandInfo.ReplyToCommand($"-----------------------------------");

                if (currentSurfTime != null)
                {
                    commandInfo.ReplyToCommand($"Time surfing on this server: {TimeHelpers.FormatPlaytime(serverSurfTime)}");
                }
                if (currentSpecTime != null)
                {
                    commandInfo.ReplyToCommand($"Time spectating on this server: {TimeHelpers.FormatPlaytime(serverSpecTime)}");
                }
            }
        }

        private void OutputMapTime(CommandInfo commandInfo, TimeData playerTimeData, int? currentSurfTime, int? currentSpecTime)
        {
            var mapTimeData = GetMapTimeData(playerTimeData, CurrentMapName);
            var mapSurfTime = mapTimeData.SurfingTime + currentSurfTime ?? 0;
            var mapSpecTime = mapTimeData.SpecTime + currentSpecTime ?? 0;

            if (mapTimeData != null)
            {
                commandInfo.ReplyToCommand($"-----------------------------------");
                if (currentSurfTime != null)
                {
                    commandInfo.ReplyToCommand($"Total time surfing on {CurrentMapName}: {TimeHelpers.FormatPlaytime(mapSurfTime)}");
                }

                if (currentSpecTime != null)
                {
                    commandInfo.ReplyToCommand($"Total time spectating on {CurrentMapName}: {TimeHelpers.FormatPlaytime(mapSpecTime)}");
                }
            }
        }

        private ServerTimeData GetServerTimeData(TimeData playerTimeData, string serverIpAddress)
        {
            var serverTimeData = playerTimeData.PlayerSurfingTimeData.ServerTimeDataList.FirstOrDefault(x => x.ServerIp == serverIpAddress);

            if (serverTimeData != null)
            {
                return serverTimeData;
            }
            else return null;
        }

        private MapTimeData GetMapTimeData(TimeData playerTimeData, string mapName)
        {
            var combinedMapTimeData = new MapTimeData();

            /// Add all times from all servers for this map
            foreach (var serverTimeData in playerTimeData.PlayerSurfingTimeData.ServerTimeDataList)
            {
                if (serverTimeData.Maps.Any(x => x.MapName == mapName))
                {
                    combinedMapTimeData.SurfingTime += serverTimeData.Maps.FirstOrDefault(x => x.MapName == mapName).SurfingTime;
                    combinedMapTimeData.SpecTime += serverTimeData.Maps.FirstOrDefault(x => x.MapName == mapName).SpecTime;
                }
            }

            return combinedMapTimeData;
        }
    }
}
