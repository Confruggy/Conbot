using System.ComponentModel.DataAnnotations;

namespace Conbot.RankingPlugin;

public class IgnoredChannel
{
    [Key]
    public ulong ChannelId { get; set; }

    public ulong GuildId { get; set; }
    public virtual RankGuildConfiguration GuildConfiguration { get; set; } = null!;

    public IgnoredChannel(ulong channelId, ulong guildId)
    {
        ChannelId = channelId;
        GuildId = guildId;
    }

    public IgnoredChannel(ulong channelId, RankGuildConfiguration guildConfiguration)
        : this(channelId, guildConfiguration.GuildId)
    {
    }
}