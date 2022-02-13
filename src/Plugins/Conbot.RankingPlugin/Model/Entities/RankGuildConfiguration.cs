using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Conbot.RankingPlugin;

public class RankGuildConfiguration
{
    [Key]
    public ulong GuildId { get; set; }

    public bool? ShowLevelUpAnnouncements { get; set; }
    public ulong? LevelUpAnnouncementsChannelId { get; set; }
    public int? LevelUpAnnouncementsMinimumLevel { get; set; }
    public RoleRewardsType? RoleRewardsType { get; set; }
    public decimal Multiplier { get; set; }

    public virtual List<RankRoleReward> RoleRewards { get; set; } = null!;
    public virtual List<IgnoredChannel> IgnoredChannels { get; set; } = null!;

    public RankGuildConfiguration(ulong guildId) => GuildId = guildId;
}