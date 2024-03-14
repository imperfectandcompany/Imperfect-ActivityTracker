using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using ImperfectActivityTracker.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ImperfectActivityTracker
{
    public partial class ImperfectActivityTracker : BasePlugin, IPluginConfig<Config>
    {
        public static ILogger _logger;
        private static DatabaseManager _databaseManager;

        DateTime lastRoundStartEventTime = DateTime.MinValue;
        public CCSGameRules? GameRules = null;

        public Config Config { get; set; } = new Config();
        public string ServerIpAddress { get; set; } = "";
        public string CurrentMapName { get; set; } = "";

        public override string ModuleName => "ImperfectActivityTracker";
        public override string ModuleVersion => "0.1.0";
        public override string ModuleAuthor => "Imperfect Gamers - raz, Borrowed code from K4ryuu";
        public override string ModuleDescription => "A user activity tracker plugin.";

        public override void Load(bool hotReload)
        {
            /// Register events handlers for when a player connects or disconnects
            RegisterPlayerConnectionEvents();

            RegisterPlayerEvents();

            RegisterListener<Listeners.OnMapStart>(name =>
            {
                CurrentMapName = name;
            });

            if (hotReload)
            {
                CurrentMapName = NativeAPI.GetMapName();

                //LoadAllPlayersCache();

                GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
            }
        }

        public override void Unload(bool hotReload)
        {
            base.Unload(hotReload);
        }

        public void OnConfigParsed(Config config)
        {
            _logger = Logger;

            if (config.Version < Config.Version)
            {
                _logger.LogWarning("The config version does not match current version: Expected: {0} | Current: {1}", Config.Version, config.Version);
            }

            // Initialize database and create table if it doesn't exist
            _databaseManager = new();
            _databaseManager.Initialize(config);

            /// Get server ip from config
            if (string.IsNullOrEmpty(config.ServerIp))
            {
                _logger.LogError("Server IP is missing from config file.");
                throw new NullReferenceException();
            }
            else
            {
                ServerIpAddress = config.ServerIp;
            }

            Config = config;
        }

        private void RegisterPlayerConnectionEvents()
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

        public void BeforeDisconnect(CCSPlayerController player)
        {
            DateTime now = DateTime.UtcNow;

            TimeData? playerData = PlayerCache.Instance.GetPlayerData(player).PlayerTimeData;

            if (playerData is null)
                return;

            if ((CsTeam)player.TeamNum > CsTeam.Spectator)
            {
                playerData.TimeFields["surfing"] += (int)Math.Round((now - playerData.Times["Surfing"]).TotalSeconds);
            }
            else if ((CsTeam)player.TeamNum == CsTeam.Spectator)
            {
                playerData.TimeFields["spec"] += (int)Math.Round((now - playerData.Times["Spec"]).TotalSeconds);
            }
        }

        public string FormatPlaytime(int totalSeconds)
        {
            string[] units = { "y", "mo", "d", "h", "m", "s" };
            int[] values = { totalSeconds / 31536000, totalSeconds % 31536000 / 2592000, totalSeconds % 2592000 / 86400, totalSeconds % 86400 / 3600, totalSeconds % 3600 / 60, totalSeconds % 60 };

            StringBuilder formattedTime = new StringBuilder();

            bool addedValue = false;

            for (int i = 0; i < units.Length; i++)
            {
                if (values[i] > 0)
                {
                    formattedTime.Append($"{values[i]}{units[i]}, ");
                    addedValue = true;
                }
            }

            if (!addedValue)
            {
                formattedTime.Append("0s");
            }

            return formattedTime.ToString().TrimEnd(' ', ',');
        }
    }
}
