using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
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

        // This is the FakeConVar to override the IP from config.
        private FakeConVar<string> _imperfectActivityIp = new(
            "imperfect_activity_ip",  // ConVar name used on cmd line
            "Allows dynamic override of ServerIp for ImperfectActivityTracker plugin.",
            "",                       // Default is empty => fallback to config
            ConVarFlags.FCVAR_NONE
        );
        
        public Config Config { get; set; } = new Config();
        public string ServerIpAddress { get; set; } = "";
        public string CurrentMapName { get; set; } = "";

        public override string ModuleName => "Imperfect-ActivityTracker";
        public override string ModuleVersion => "1.3.4";
        public override string ModuleAuthor => "Imperfect Gamers - raz & Olionkey, Borrowed code from K4ryuu";
        public override string ModuleDescription => "A user activity tracker plugin.";

        public override void Load(bool hotReload)
        {
            // Register the FakeConVar so the engine sees it
            RegisterFakeConVars(GetType());            
            
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

                GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
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
            _logger = Logger; // To log any warnings/errors about the config

            // Check config version
            if (config.Version < Config.Version)
            {
                _logger.LogWarning(
                    "Config version mismatch: Expected: {0} | Provided: {1}",
                    Config.Version, config.Version
                );
            }

            // Initialize database if needed
            _databaseManager = new DatabaseManager();
            _databaseManager.Initialize(config);

            // If ConVar is set, override config.ServerIp
            string overrideIp = _imperfectActivityIp.Value?.Trim() ?? "";
            if (!string.IsNullOrEmpty(overrideIp))
            {
                config.ServerIp = overrideIp;
                _logger.LogInformation(
                    "[ImperfectActivityTracker] Overriding config.ServerIp with '{0}' from +imperfect_activity_ip",
                    overrideIp
                );
            }

            // Validate we have an IP
            if (string.IsNullOrEmpty(config.ServerIp))
            {
                _logger.LogError("Server IP is missing from config or ConVar. A valid IP is required.");
                throw new NullReferenceException("ServerIp not set in ImperfectActivityTracker config or ConVar.");
            }
            else
            {
                // Final IP
                ServerIpAddress = config.ServerIp;
            }

            Config = config;
        }
    }
}
