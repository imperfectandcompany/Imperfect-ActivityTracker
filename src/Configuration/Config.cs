using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace ImperfectActivityTracker.Configuration
{
    public class Config : BasePluginConfig
    {
        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 2;

        public DatabaseSettings DatabaseSettings { get; set; } = new();

        public string ServerIp { get; set; } = "";
    }
}
