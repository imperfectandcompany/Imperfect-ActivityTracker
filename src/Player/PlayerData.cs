namespace ImperfectActivityTracker
{
    public class PlayerData
    {
        public required string PlayerName { get; set; }
        public required string SteamId { get; set; }
        public string IpAddress { get; set; }
        public required PlayerCacheData CacheData { get; set; }
    }
}
