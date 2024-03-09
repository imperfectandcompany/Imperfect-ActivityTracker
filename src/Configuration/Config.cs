using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace ImperfectActivityTracker.Configuration
{
    public class Config : BasePluginConfig
    {
        /// <summary>
        /// Config version number
        /// </summary>
        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 2;

        public DatabaseSettings DatabaseSettings { get; set; } = new();

        public List<string> ServerIpList { get; set; } = new();
    }
}
