using CounterStrikeSharp.API.Core;

namespace ImperfectActivityTracker
{
    public partial class ImperfectActivityTracker
    {
        public enum CsTeam : byte
        {
            None = 0,
            Spectator = 1,
            Terrorist = 2,
            CounterTerrorist = 3
        }

        public void RegisterPlayerEvents()
        {
            RegisterEventHandler<EventPlayerTeam>((@event, info) =>
            {
                CCSPlayerController player = @event.Userid;

                if (player is null || !player.IsValid || !player.PlayerPawn.IsValid)
                    return HookResult.Continue;

                if (player.IsBot || player.IsHLTV)
                    return HookResult.Continue;

                if (!PlayerCache.Instance.ContainsPlayer(player))
                    return HookResult.Continue;

                DateTime now = DateTime.UtcNow;
                PlayerCacheData? playerCacheData = PlayerCache.Instance.GetPlayerData(player);
                TimeData? playerTimeData = playerCacheData.PlayerTimeData;

                if (playerCacheData is null)
                    return HookResult.Continue;

                /// Get the current server time data for this specific server using ip address in config
                var currentServerTimeData = playerCacheData
                    .PlayerTimeData
                    .ServerTimeDataList
                    .FirstOrDefault(server => server.ServerIp == ServerIpAddress);

                /// Get the current map time data for this specific server and map using the current map name
                var currentMapTimeData = currentServerTimeData
                    .Maps
                    .FirstOrDefault(map => map.MapName == CurrentMapName);

                /// When the player changes a team, check that the old team was either ct/t/spec
                if ((CsTeam)@event.Oldteam != CsTeam.None)
                {
                    if ((CsTeam)@event.Oldteam == CsTeam.Terrorist
                        ||(CsTeam)@event.Oldteam == CsTeam.CounterTerrorist)
                    {
                        /// If team they changed from was ct or t, save the current surfing time
                        currentServerTimeData.TotalSurfingTime += (int)(now - playerTimeData.Times["Surfing"]).TotalSeconds;
                        currentMapTimeData.SurfingTime += (int)(now - playerTimeData.Times["Surfing"]).TotalSeconds;
                    }
                    else if ((CsTeam)@event.Oldteam == CsTeam.Spectator)
                    {
                        /// If team they changed from was spectator, save the current spec time
                        currentServerTimeData.TotalSpecTime += (int)(now - playerTimeData.Times["Spec"]).TotalSeconds;
                        currentMapTimeData.SpecTime += (int)(now - playerTimeData.Times["Spec"]).TotalSeconds;
                    }
                }

                if ((CsTeam)@event.Team == CsTeam.Terrorist
                    || (CsTeam)@event.Team == CsTeam.CounterTerrorist)
                {
                    /// If the new team they joined was a ct/t, reset the starting time for surfing since they are now surfing
                    playerTimeData.Times["Surfing"] = now;
                }
                else if ((CsTeam)@event.Team == CsTeam.Spectator)
                {
                    /// If the new team they joined was spectator, reset the starting time for surfing since they are now spectating
                    playerTimeData.Times["Spec"] = now;
                }
                
                return HookResult.Continue;
            });
        }

        public void BeforeDisconnect(CCSPlayerController player)
        {
            DateTime now = DateTime.UtcNow;

            PlayerCacheData playerCacheData = PlayerCache.Instance.GetPlayerData(player);
            TimeData? playerTimeData = playerCacheData.PlayerTimeData;

            if (playerTimeData is null)
                return;

            /// Get the current server time data for this specific server using ip address in config
            var currentServerTimeData = playerTimeData
                .ServerTimeDataList
                .FirstOrDefault(server => server.ServerIp == ServerIpAddress);

            /// Get the current map time data for this specific server and map using the current map name
            var currentMapTimeData = currentServerTimeData
                .Maps
                .FirstOrDefault(map => map.MapName == CurrentMapName);

            /// If the current team is greater than spectator, they are either on ct or t
            if ((CsTeam)player.TeamNum > CsTeam.Spectator)
            {
                /// Since they are ct/t, they were surfing, save the time data for surfing
                currentServerTimeData.TotalSurfingTime += (int)Math.Round((now - playerTimeData.Times["Surfing"]).TotalSeconds);
                currentMapTimeData.SurfingTime += (int)Math.Round((now - playerTimeData.Times["Surfing"]).TotalSeconds);
            }
            else if ((CsTeam)player.TeamNum == CsTeam.Spectator)
            {
                /// Since are currently on the spectator 'team', they were spectating, save the time data for spec
                currentServerTimeData.TotalSpecTime += (int)Math.Round((now - playerTimeData.Times["Spec"]).TotalSeconds);
                currentMapTimeData.SpecTime += (int)Math.Round((now - playerTimeData.Times["Spec"]).TotalSeconds);
            }
        }
    }
}
