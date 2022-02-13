using System.ComponentModel.DataAnnotations;

namespace Conbot.RankingPlugin;

public class RankUserConfiguration
{
    [Key]
    public ulong UserId { get; set; }

    public bool? AnnouncementsAllowMentions { get; set; }
    public bool? AnnouncementsSendDirectMessages { get; set; }

    public RankUserConfiguration(ulong userId) => UserId = userId;
}