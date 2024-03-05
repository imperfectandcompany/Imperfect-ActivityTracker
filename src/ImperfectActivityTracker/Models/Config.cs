using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace ImperfectActivityTracker.Models
{
    public class Config : BasePluginConfig
    {
        /// <summary>
        /// Config version number
        /// </summary>
        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 1;
    }
}
