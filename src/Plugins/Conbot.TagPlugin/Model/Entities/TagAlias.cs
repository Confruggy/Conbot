using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.TagPlugin
{
    public class TagAlias
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public ulong GuildId { get; set; }

        [Required]
        public string Name { get; set; }

        public int TagId { get; set; }
        public virtual Tag Tag { get; set; } = null!;

        public ulong OwnerId { get; set; }
        public ulong CreatorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public ulong CreationGuildId { get; set; }
        public ulong CreationChannelId { get; set; }
        public ulong? CreationMessageId { get; set; }
        public ulong? CreationInteractionId { get; set; }

        public virtual List<TagUse> TagUses { get; set; } = null!;
        public virtual List<TagAliasOwnerChange> OwnerChanges { get; set; } = null!;

        [NotMapped]
        public string CreationUrl => $"https://discordapp.com/channels/{CreationGuildId}/{CreationChannelId}/{CreationMessageId ?? CreationInteractionId}";

        public TagAlias(ulong guildId, string name, int tagId, ulong ownerId, ulong creatorId, DateTime createdAt,
            ulong creationGuildId, ulong creationChannelId, ulong? creationMessageId, ulong? creationInteractionId)
        {
            GuildId = guildId;
            Name = name;
            TagId = tagId;
            OwnerId = ownerId;
            CreatorId = creatorId;
            CreatedAt = createdAt;
            CreationGuildId = creationGuildId;
            CreationChannelId = creationChannelId;
            CreationMessageId = creationMessageId;
            CreationInteractionId = creationInteractionId;
        }

        public TagAlias(ulong guildId, string name, Tag tag, ulong ownerId, ulong creatorId, DateTime createdAt,
            ulong creationGuildId, ulong creationChannelId, ulong? creationMessageId, ulong? creationInteractionId)
            : this(guildId, name, tag.Id, ownerId, creatorId, createdAt, creationGuildId, creationChannelId,
                  creationMessageId, creationInteractionId)
        {
        }
    }
}
