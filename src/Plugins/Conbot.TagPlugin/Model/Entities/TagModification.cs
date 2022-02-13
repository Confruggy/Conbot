using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.TagPlugin;

public class TagModification
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int TagId { get; set; }
    public virtual Tag Tag { get; set; } = null!;

    [Required]
    public string NewContent { get; set; }

    [Required]
    public string OldContent { get; set; }

    public ulong UserId { get; set; }
    public DateTime ModifiedAt { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong? MessageId { get; set; }
    public ulong? InteractionId { get; set; }

    [NotMapped]
    public string Url
        => $"https://discordapp.com/channels/{GuildId}/{ChannelId}/{MessageId ?? InteractionId}";

    public TagModification(int tagId, string newContent, string oldContent, ulong userId, DateTime modifiedAt,
        ulong guildId, ulong channelId, ulong? messageId, ulong? interactionId)
    {
        TagId = tagId;
        NewContent = newContent;
        OldContent = oldContent;
        UserId = userId;
        ModifiedAt = modifiedAt;
        GuildId = guildId;
        ChannelId = channelId;
        MessageId = messageId;
        InteractionId = interactionId;
    }

    public TagModification(Tag tag, string newContent, string oldContent, ulong userId, DateTime modifiedAt,
        ulong guildId, ulong channelId, ulong? messageId, ulong? interactionId)
        : this(tag.Id, newContent, oldContent, userId, modifiedAt, guildId, channelId, messageId, interactionId)
    {
    }
}