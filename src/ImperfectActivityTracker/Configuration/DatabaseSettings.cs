namespace ImperfectActivityTracker.Configuration
{
    public class DatabaseSettings
    {
        public string DatabaseHost { get; set; } = "";

        public uint DatabasePort { get; set; } = 3306;

        public string DatabaseName { get; set; } = "";

        public string DatabaseUser { get; set; } = "";

        public string DatabasePassword { get; set; } = "";
    }
}
