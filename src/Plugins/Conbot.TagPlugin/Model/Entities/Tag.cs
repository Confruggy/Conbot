using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.TagPlugin;

public class Tag
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string Content { get; set; }

    public ulong OwnerId { get; set; }
    public ulong CreatorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public ulong CreationGuildId { get; set; }
    public ulong CreationChannelId { get; set; }
    public ulong? CreationMessageId { get; set; }
    public ulong? CreationInteractionId { get; set; }

    public virtual List<TagAlias> Aliases { get; set; } = null!;
    public virtual List<TagModification> Modifications { get; set; } = null!;
    public virtual List<TagUse> Uses { get; set; } = null!;
    public virtual List<TagOwnerChange> OwnerChanges { get; set; } = null!;

    [NotMapped]
    public string CreationUrl => $"https://discordapp.com/channels/{CreationGuildId}/{CreationChannelId}/{CreationMessageId ?? CreationInteractionId}";

    public Tag(ulong guildId, string name, string content, ulong ownerId, ulong creatorId,
        DateTime createdAt, ulong creationGuildId, ulong creationChannelId, ulong? creationMessageId,
        ulong? creationInteractionId)
    {
        GuildId = guildId;
        Name = name;
        Content = content;
        OwnerId = ownerId;
        CreatorId = creatorId;
        CreatedAt = createdAt;
        CreationGuildId = creationGuildId;
        CreationChannelId = creationChannelId;
        CreationMessageId = creationMessageId;
        CreationInteractionId = creationInteractionId;
    }
}