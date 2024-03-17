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
        public int ServerSurfingTime { get; set; }
        public int ServerSpecTime { get; set; }
        public DateTime FirstTimeJoined { get; set; }
        public List<MapTimeData> Maps { get; set; } = new();
    }

    public class MapTimeData
    {
        public int SurfingTime { get; set; }
        public int SpecTime { get; set; }
        public string MapName { get; set; } = "";
        public string WorkshopId { get; set; } = "";
        public DateTime FirstTimePlayed { get; set; }
    }
}