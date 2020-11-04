using System.ComponentModel.DataAnnotations;

namespace Conbot.TimeZonePlugin
{
    public class UserTimeZone
    {
        [Key]
        public ulong UserId { get; set; }

        [Required]
        public string TimeZoneId { get; set; }
    }
}