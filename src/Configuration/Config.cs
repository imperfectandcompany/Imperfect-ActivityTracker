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
        public override int Version { get; set; } = 1;

        public DatabaseSettings DatabaseSettings { get; set; } = new();
    }
}
