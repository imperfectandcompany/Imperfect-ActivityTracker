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
                    if ((CsTeam)@event.Oldteam == CsTeam.Terrorist
                        ||(CsTeam)@event.Oldteam == CsTeam.CounterTerrorist)
                    {
                        playerData.TimeFields["surfing"] += (int)(DateTime.UtcNow - playerData.Times["Surfing"]).TotalSeconds;
                        playerData.Times["Spec"] = now;
                    }
                    else if ((CsTeam)@event.Oldteam == CsTeam.Spectator)
                    {
                        playerData.TimeFields["spec"] += (int)(DateTime.UtcNow - playerData.Times["Spec"]).TotalSeconds;
                        playerData.Times["Surfing"] = now;
                    }
                }
                
                return HookResult.Continue;
            });
        }
    }
}
