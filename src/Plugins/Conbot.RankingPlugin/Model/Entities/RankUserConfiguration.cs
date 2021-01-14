using System.ComponentModel.DataAnnotations;

namespace Conbot.RankingPlugin
{
    public class RankUserConfiguration
    {
        [Key]
        public ulong UserId { get; set; }

        public bool? AnnouncementsAllowMentions { get; set; } = null!;
        public bool? AnnouncementsSendDirectMessages { get; set; } = null!;

        public RankUserConfiguration(ulong userId) => UserId = userId;
    }
}
