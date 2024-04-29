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
        public void GetTotalPlayTimeCommand(CCSPlayerController? player, CommandInfo commandInfo)
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
                        var now = DateTime.UtcNow;
                        var currentSurfTime = (int)(now - playerTimeData.Times["Surfing"]).TotalSeconds;
                        var currentSpecTime = (int)(now - playerTimeData.Times["Spec"]).TotalSeconds;
                        var totalSurfingTime = playerTimeData.PlayerSurfingTimeData.TotalSurfingTime + currentSurfTime;
                        var totalSpecTime = playerTimeData.PlayerSurfingTimeData.TotalSpecTime + currentSpecTime;


                        commandInfo.ReplyToCommand($"Total time surfing: {TimeHelpers.FormatPlaytime(totalSurfingTime)}");
                        commandInfo.ReplyToCommand($"Total time spectating: {TimeHelpers.FormatPlaytime(totalSpecTime)}");

                        var mapTimeData = GetMapTimeData(playerTimeData, CurrentMapName);
                        var mapSurfTime = mapTimeData.SurfingTime + currentSurfTime;
                        var mapSpecTime = mapTimeData.SpecTime + currentSpecTime;

                        if (mapTimeData != null)
                        {
                            commandInfo.ReplyToCommand($"-----------------------------------");
                            commandInfo.ReplyToCommand($"Total time surfing on {CurrentMapName}: {TimeHelpers.FormatPlaytime(mapSurfTime)}");
                            commandInfo.ReplyToCommand($"Total time spectating on {CurrentMapName}: {TimeHelpers.FormatPlaytime(mapSpecTime)}");
                        }

                        var serverTimeData = GetServerTimeData(playerTimeData, ServerIpAddress);
                        var serverSurfTime = serverTimeData.ServerSurfingTime + currentSurfTime;
                        var serverSpecTime = serverTimeData.ServerSpecTime + currentSpecTime;

                        if (serverTimeData != null)
                        {
                            commandInfo.ReplyToCommand($"-----------------------------------");
                            commandInfo.ReplyToCommand($"Time surfing on this server: {TimeHelpers.FormatPlaytime(serverSurfTime)}");
                            commandInfo.ReplyToCommand($"Time spectating on this server: {TimeHelpers.FormatPlaytime(serverSpecTime)}");
                        }
                    }
                    else
                    {
                        commandInfo.ReplyToCommand($"No times found for {player.PlayerName}");
                        _logger.LogInformation($"No playtime found for {player.PlayerName ?? "player"} ({player.SteamID}). Either no times exists or something happened loading their surfing time data");
                    }
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
