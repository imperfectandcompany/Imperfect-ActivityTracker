﻿using CounterStrikeSharp.API.Core;

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

        public void RegisterPlayerEventHandlers()
        {
            RegisterEventHandler((EventPlayerConnectFull @event, GameEventInfo info) =>
            {
                CCSPlayerController player = @event.Userid;

                if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsBot || player.IsHLTV)
                    return HookResult.Continue;

                // The player was found in the cache, no need to load info from db
                if (PlayerCache.Instance.ContainsPlayer(player))
                    return HookResult.Continue;

                /// The player connected, let's load their info from the db and put them into the cache
                LoadPlayerCache(player);

                return HookResult.Continue;
            });

            RegisterEventHandler((EventPlayerDisconnect @event, GameEventInfo info) =>
            {
                CCSPlayerController player = @event.Userid;

                if (player is null || !player.IsValid || !player.PlayerPawn.IsValid)
                    return HookResult.Continue;

                if (player.IsBot || player.IsHLTV)
                    return HookResult.Continue;

                if (!PlayerCache.Instance.ContainsPlayer(player))
                    return HookResult.Continue;

                /// The player is disconnecting, we need to save their current surf/spec times
                BeforeDisconnect(player);

                // Do not save cache for each player on map change, because it's handled by an optimized query for all players
                if (@event.Reason != 1)
                {
                    /// Save the players cached info (name and time data) to the db and remove them from the cache
                    SavePlayerCache(player, true);
                }

                return HookResult.Continue;
            });

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

                /// Get the current server and map time data for this specific 
                /// server/map using ip address from config and current map name
                (var currentServerTimeData, var currentMapTimeData) = GetOrCreateCurrentServerAndMapData(playerTimeData);

                /// When the player changes a team, check that the old team was either ct/t/spec
                if ((CsTeam)@event.Oldteam != CsTeam.None)
                {
                    if ((CsTeam)@event.Oldteam == CsTeam.Terrorist
                        || (CsTeam)@event.Oldteam == CsTeam.CounterTerrorist)
                    {
                        /// If team they changed from was ct or t, save the current surfing time
                        /// Overall total surfing time
                        playerTimeData.PlayerSurfingTimeData.TotalSurfingTime += (int)(now - playerTimeData.Times["Surfing"]).TotalSeconds;
                        /// Current server surfing time
                        currentServerTimeData.ServerSurfingTime += (int)(now - playerTimeData.Times["Surfing"]).TotalSeconds;
                        /// Current map surfing time
                        currentMapTimeData.SurfingTime += (int)(now - playerTimeData.Times["Surfing"]).TotalSeconds;
                    }
                    else if ((CsTeam)@event.Oldteam == CsTeam.Spectator)
                    {
                        /// If team they changed from was spectator, save the current spec time
                        /// Overall total spectating time
                        playerTimeData.PlayerSurfingTimeData.TotalSpecTime += (int)(now - playerTimeData.Times["Spec"]).TotalSeconds;
                        /// Current server spectating time
                        currentMapTimeData.SpecTime += (int)(now - playerTimeData.Times["Spec"]).TotalSeconds;
                        /// Current map spectating time
                        currentMapTimeData.SpecTime += (int)(now - playerTimeData.Times["Spec"]).TotalSeconds;
                    }
                }

                if ((CsTeam)@event.Team == CsTeam.Terrorist
                    || (CsTeam)@event.Team == CsTeam.CounterTerrorist)
                {
                    /// If the new team they joined is a ct/t, reset the starting time for surfing since they are now surfing
                    playerTimeData.Times["Surfing"] = now;
                }
                else if ((CsTeam)@event.Team == CsTeam.Spectator)
                {
                    /// If the new team they joined is spectator, reset the starting time for spec since they are now spectating
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

            /// Get the current server and map time data for this specific 
            /// server/map using ip address from config and current map name
            (var currentServerTimeData, var currentMapTimeData) = GetOrCreateCurrentServerAndMapData(playerTimeData);

            /// If the current team is greater than spectator, they are either on ct or t
            if ((CsTeam)player.TeamNum > CsTeam.Spectator)
            {
                /// Since they are ct/t, they were surfing, save the time data for surfing
                /// Overall total surfing time
                playerTimeData.PlayerSurfingTimeData.TotalSurfingTime += (int)Math.Round((now - playerTimeData.Times["Surfing"]).TotalSeconds);
                /// Current server surfing time
                currentServerTimeData.ServerSurfingTime += (int)Math.Round((now - playerTimeData.Times["Surfing"]).TotalSeconds);
                /// Current map surfing time
                currentMapTimeData.SurfingTime += (int)Math.Round((now - playerTimeData.Times["Surfing"]).TotalSeconds);
            }
            else if ((CsTeam)player.TeamNum == CsTeam.Spectator)
            {
                /// Since are currently on the spectator 'team', they were spectating, save the time data for spec
                /// Overall total spectating time
                playerTimeData.PlayerSurfingTimeData.TotalSpecTime += (int)Math.Round((now - playerTimeData.Times["Spec"]).TotalSeconds);
                /// Current server spectating time
                currentServerTimeData.ServerSpecTime += (int)Math.Round((now - playerTimeData.Times["Spec"]).TotalSeconds);
                /// Current map spectating time
                currentMapTimeData.SpecTime += (int)Math.Round((now - playerTimeData.Times["Spec"]).TotalSeconds);
            }
        }
    }
}
