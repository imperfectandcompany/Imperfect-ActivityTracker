using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public string GetFieldForTeam(CsTeam team)
        {
            switch (team)
            {
                case CsTeam.Terrorist:
                    return "t";
                case CsTeam.CounterTerrorist:
                    return "ct";
                default:
                    return "spec";
            }
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
                TimeData? playerData = PlayerCache.Instance.GetPlayerData(player).PlayerTimeData;

                if (playerData is null)
                    return HookResult.Continue;

                if ((CsTeam)@event.Oldteam != CsTeam.None)
                {
                    playerData.TimeFields[GetFieldForTeam((CsTeam)@event.Oldteam)] += (int)(DateTime.UtcNow - playerData.Times["Team"]).TotalSeconds;
                }

                playerData.Times["Team"] = now;

                return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerSpawned>((@event, info) =>
            {
                CCSPlayerController player = @event.Userid;

                if (player is null || !player.IsValid || !player.PlayerPawn.IsValid)
                {
                    return HookResult.Continue;
                }

                if (player.IsBot || player.IsHLTV)
                {
                    return HookResult.Continue;
                }

                if (!PlayerCache.Instance.ContainsPlayer(player))
                {
                    return HookResult.Continue;
                }

                TimeData? playerData = PlayerCache.Instance.GetPlayerData(player).PlayerTimeData;

                if (playerData is null)
                {
                    return HookResult.Continue;
                }

                playerData.TimeFields["dead"] += (int)(DateTime.UtcNow - playerData.Times["Death"]).TotalSeconds;
                playerData.Times["Death"] = DateTime.UtcNow;

                return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                CCSPlayerController player = @event.Userid;

                if (player is null || !player.IsValid || !player.PlayerPawn.IsValid)
                    return HookResult.Continue;

                if (player.IsBot || player.IsHLTV)
                    return HookResult.Continue;

                if (!PlayerCache.Instance.ContainsPlayer(player))
                    return HookResult.Continue;

                TimeData? playerData = PlayerCache.Instance.GetPlayerData(player).PlayerTimeData;

                if (playerData is null)
                    return HookResult.Continue;

                playerData.TimeFields["alive"] += (int)(DateTime.UtcNow - playerData.Times["Death"]).TotalSeconds;
                playerData.Times["Death"] = DateTime.UtcNow;

                return HookResult.Continue;
            });
        }
    }
}
