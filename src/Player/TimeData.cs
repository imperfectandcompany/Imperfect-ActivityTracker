using System.Text.Json.Serialization;

namespace ImperfectActivityTracker
{
    public class TimeData
    {
        public Dictionary<string, DateTime> Times { get; set; } = new Dictionary<string, DateTime>();

        public SurfingTimeData? PlayerSurfingTimeData { get; set; }
    }
}