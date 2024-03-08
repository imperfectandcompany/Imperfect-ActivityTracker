using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperfectActivityTracker
{
    public class PlayerData
    {
        public required string PlayerName { get; set; }
        public required string SteamId { get; set; }
        public required PlayerCacheData CacheData { get; set; }
    }
}
