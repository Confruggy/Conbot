using Discord;

namespace Conbot.Core
{
    public class Config
    {
        public string Token { get; set; }
        public string Secret { get; set; }
        public int TotalShards { get; set; } = 1;
        public LogSeverity LogSeverity { get; set; } = LogSeverity.Verbose;
    }
}
