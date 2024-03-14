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

                // Do not save cache for each player on mapchange, because it's handled by an optimised query for all players
                if (@event.Reason != 1)
                {
                    /// Save the players cached info (name and time data) to the db and remove them from the cache
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
