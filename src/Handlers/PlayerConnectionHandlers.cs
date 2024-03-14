using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace ImperfectActivityTracker
{
    public partial class ImperfectActivityTracker
    {
        public void RegisterPlayerConnectionEvents()
        {
            RegisterEventHandler((EventPlayerConnectFull @event, GameEventInfo info) =>
            {
                CCSPlayerController player = @event.Userid;

                if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsBot || player.IsHLTV)
                    return HookResult.Continue;

                // Do not load the data, if the user is in the cache already
                // This free up some resources and prevent plugin to load the same data twice
                if (PlayerCache.Instance.ContainsPlayer(player))
                    return HookResult.Continue;

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

                BeforeDisconnect(player);

                // Do not save cache for each player on mapchange, because it's handled by an optimised query for all players
                if (@event.Reason != 1)
                {
                    SavePlayerCache(player, true);
                }

                return HookResult.Continue;
            });

            RegisterEventHandler((EventRoundEnd @event, GameEventInfo info) =>
            {
                SaveAllPlayersCache();

                return HookResult.Continue;
            }, HookMode.Post);

            RegisterListener<Listeners.OnMapStart>((mapName) =>
            {
                AddTimer(1.0f, () =>
                {
                    GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
                });
            });

            RegisterListener<Listeners.OnMapEnd>(() =>
            {
                GameRules = null;
            });
        }
    }
}
