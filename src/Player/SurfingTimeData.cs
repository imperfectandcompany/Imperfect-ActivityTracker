using System.Text.Json.Serialization;

namespace ImperfectActivityTracker
{
    public class SurfingTimeData
    {
        public int TotalSurfingTime { get; set; }

        public int TotalSpecTime { get; set; }

        [JsonPropertyName("player_time_data")]
        public List<ServerTimeData> ServerTimeDataList { get; set; } = new();
    }

    public class ServerTimeData
    {
        public string ServerIp { get; set; } = "";
        public List<MapTimeData> Maps { get; set; } = new();
        public int ServerSurfingTime { get; set; }
        public int ServerSpecTime { get; set; }
    }

    public class MapTimeData
    {
        public string MapName { get; set; } = "";
        public string WorkshopId { get; set; } = "";
        public int SurfingTime { get; set; }
        public int SpecTime { get; set; }
    }
}