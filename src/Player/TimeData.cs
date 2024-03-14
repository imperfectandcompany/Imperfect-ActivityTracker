namespace ImperfectActivityTracker
{
    public class TimeData
    {
        public Dictionary<string, DateTime> Times { get; set; } = new Dictionary<string, DateTime>();

        public Dictionary<string, int> TimeFields { get; set; } = new Dictionary<string, int>();

        public List<ServerTimeData> ServerTimeDataList { get; set; } = new();
    }

    public class ServerTimeData
    {
        public string ServerIp { get; set; } = "";
        public List<MapTimeData> Maps { get; set; } = new();
        public int TotalSurfingTime { get; set; }
        public int TotalSpecTime { get; set; }
    }

    public class MapTimeData
    {
        public string MapName { get; set; } = "";
        public string WorkshopId { get; set; } = "";
        public int SurfingTime { get; set; }
        public int SpecTime { get; set; }
    }
}