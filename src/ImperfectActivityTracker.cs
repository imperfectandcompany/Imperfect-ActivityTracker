using CounterStrikeSharp.API.Core;
using ImperfectActivityTracker.Configuration;
using Microsoft.Extensions.Logging;

namespace ImperfectActivityTracker
{
    public partial class ImperfectActivityTracker : BasePlugin, IPluginConfig<Config>
    {
        public static ILogger _logger;
        private static DatabaseManager _databaseManager;

        public Config Config { get; set; } = new Config();

        public override string ModuleName => "Imperfect-ActivityTracker";
        public override string ModuleVersion => "0.1.0";
        public override string ModuleAuthor => "Imperfect Gamers - raz, Borrowed code from K4ryuu";
        public override string ModuleDescription => "A user activity tracker plugin.";

        public override void Load(bool hotReload)
        {
            _logger = Logger;
        }

        public override void Unload(bool hotReload)
        {
            base.Unload(hotReload);
        }

        public void OnConfigParsed(Config config)
        {
            if (config.Version < Config.Version)
            {
                _logger.LogWarning("The config version does not match current version: Expected: {0} | Current: {1}", Config.Version, config.Version);
            }

            // Initialize database and create table if it doesn't exist
            _databaseManager = new();
            _databaseManager.Initialize(config);

            Config = config;
        }
    }
}
