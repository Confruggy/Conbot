using System.ComponentModel.DataAnnotations;

namespace Conbot.TimeZonePlugin
{
    public class GuildTimeZone
    {
        [Key]
        public ulong GuildId { get; set; }

        [Required]
        public string TimeZoneId { get; set; }
    }
}