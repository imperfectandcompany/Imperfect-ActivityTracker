using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using ImperfectActivityTracker.Configuration;
using Microsoft.Extensions.Logging;

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
    }
}
