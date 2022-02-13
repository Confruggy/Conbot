using System.ComponentModel.DataAnnotations;

namespace Conbot.TimeZonePlugin;

public class UserTimeZone
{
    [Key]
    public ulong UserId { get; set; }

    [Required]
    public string TimeZoneId { get; set; }

    public UserTimeZone(ulong userId, string timeZoneId)
    {
        UserId = userId;
        TimeZoneId = timeZoneId;
    }
}