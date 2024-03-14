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

                var currentServerTimeData = playerCacheData
                    .PlayerTimeData
                    .ServerTimeDataList
                    .FirstOrDefault(server => server.ServerIp == ServerIpAddress);

                var currentMapTimeData = currentServerTimeData
                    .Maps
                    .FirstOrDefault(map => map.MapName == CurrentMapName);

                if (playerCacheData is null)
                    return HookResult.Continue;

                if ((CsTeam)@event.Oldteam != CsTeam.None)
                {
                    if ((CsTeam)@event.Oldteam == CsTeam.Terrorist
                        ||(CsTeam)@event.Oldteam == CsTeam.CounterTerrorist)
                    {
                        /// total surfing time
                        currentServerTimeData.TotalSurfingTime += (int)(now - playerTimeData.Times["Surfing"]).TotalSeconds;
                        currentMapTimeData.SurfingTime += (int)(now - playerTimeData.Times["Surfing"]).TotalSeconds;
                    }
                    else if ((CsTeam)@event.Oldteam == CsTeam.Spectator)
                    {
                        /// total spec time
                        currentServerTimeData.TotalSpecTime += (int)(now - playerTimeData.Times["Spec"]).TotalSeconds;
                        currentMapTimeData.SpecTime += (int)(now - playerTimeData.Times["Spec"]).TotalSeconds;
                    }
                }

                if ((CsTeam)@event.Team == CsTeam.Terrorist
                    || (CsTeam)@event.Team == CsTeam.CounterTerrorist)
                {
                    playerTimeData.Times["Surfing"] = now;
                }
                else if ((CsTeam)@event.Team == CsTeam.Spectator)
                {
                    playerTimeData.Times["Spec"] = now;
                }
                
                return HookResult.Continue;
            });
        }
    }
}
