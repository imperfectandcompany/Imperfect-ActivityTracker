using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Xml.Linq;

namespace ImperfectActivityTracker
{
    public partial class ImperfectActivityTracker
    {
        public void RegisterServerEventHandlers()
        {
            RegisterEventHandler((EventRoundEnd @event, GameEventInfo info) =>
            {
                SaveAllPlayersCache();

                return HookResult.Continue;
            }, HookMode.Post);

            RegisterListener<Listeners.OnMapStart>((mapName) =>
            {
                /// Get current map name on map start
                CurrentMapName = mapName;

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
