using CounterStrikeSharp.API.Core;
using ImperfectActivityTracker.Configuration;
using Microsoft.Extensions.Logging;

namespace ImperfectActivityTracker
{
    public class ImperfectActivityTracker : BasePlugin, IPluginConfig<Config>
    {
        private readonly ILogger _logger;

        public Config Config { get; set; }

        public override string ModuleName => "Imperfect-ActivityTracker";
        public override string ModuleVersion => "0.1.0";
        public override string ModuleAuthor => "Imperfect Gamers - raz";
        public override string ModuleDescription => "A user activity tracker plugin.";

        public ImperfectActivityTracker(ILogger logger)
        {
            _logger = logger;
        }

        public override void Load(bool hotReload)
        {

        }

        public override void Unload(bool hotReload)
        {
            base.Unload(hotReload);
        }

        public void OnConfigParsed(Config config)
        {
            throw new NotImplementedException();
        }
    }
}
