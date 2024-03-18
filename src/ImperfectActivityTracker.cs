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
        public override string ModuleVersion => "1.3.0";
        public override string ModuleAuthor => "Imperfect Gamers - raz & Olionkey, Borrowed code from K4ryuu";
        public override string ModuleDescription => "A user activity tracker plugin.";

        public override void Load(bool hotReload)
        {
            /// Register event handlers related to the server (round start/end, map start/end)
            RegisterServerEventHandlers();

            /// Register event handlers for related to a player (change teams/spec, connect/dc
            RegisterPlayerEventHandlers();

            if (hotReload)
            {
                /// Get current map name on hot reload
                CurrentMapName = NativeAPI.GetMapName();

                /// Load all player cache data on hot reload
                LoadAllPlayersCache();

                GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
            }
        }

        public override void Unload(bool hotReload)
        {
            SaveAllPlayersCache();

            base.Unload(hotReload);

            this.Dispose();
        }

        public void OnConfigParsed(Config config)
        {
            /// Initialize logger on cfg parsing, so we can log warnings/errors regarding cfg versions/info
            _logger = Logger;

            /// Parsed config version needs to match the current config version in case of addition/removal of fields
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
                /// Throw exception because server ip is needed to save time data correctly
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
