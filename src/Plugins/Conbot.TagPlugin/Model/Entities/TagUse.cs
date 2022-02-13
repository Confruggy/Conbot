using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.TagPlugin;

public class TagUse
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int TagId { get; set; }
    public virtual Tag Tag { get; set; } = null!;

    public int? UsedAliasId { get; set; }
    public virtual TagAlias? UsedAlias { get; set; }

    public ulong UserId { get; set; }
    public DateTime UsedAt { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong? MessageId { get; set; }
    public ulong? InteractionId { get; set; }

    [NotMapped]
    public string Url
        => $"https://discordapp.com/channels/{GuildId}/{ChannelId}/{MessageId ?? InteractionId}";

    public TagUse(int tagId, int? usedAliasId, ulong userId, DateTime usedAt, ulong guildId, ulong channelId,
        ulong? messageId, ulong? interactionId)
    {
        TagId = tagId;
        UsedAliasId = usedAliasId;
        UserId = userId;
        UsedAt = usedAt;
        GuildId = guildId;
        ChannelId = channelId;
        MessageId = messageId;
        InteractionId = interactionId;
    }

    public TagUse(Tag tag, TagAlias? usedAlias, ulong userId, DateTime usedAt, ulong guildId, ulong channelId,
        ulong? messageId, ulong? interactionId)
        : this(tag.Id, usedAlias?.Id, userId, usedAt, guildId, channelId, messageId, interactionId)
    {
    }
}
