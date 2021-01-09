using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.TagPlugin
{
    public class TagAliasOwnerChange
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int TagAliasId { get; set; }
        public virtual TagAlias TagAlias { get; set; } = null!;

        public ulong NewOwnerId { get; set; }
        public ulong OldOwnerId { get; set; }

        public OwnerChangeType Type { get; set; }

        public ulong UserId { get; set; }
        public DateTime ChangedAt { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong? MessageId { get; set; }
        public ulong? InteractionId { get; set; }

        [NotMapped]
        public string Url
            => $"https://discordapp.com/channels/{GuildId}/{ChannelId}/{MessageId ?? InteractionId}";

        public TagAliasOwnerChange(int tagAliasId, ulong newOwnerId, ulong oldOwnerId, OwnerChangeType type,
            ulong userId, DateTime changedAt, ulong guildId, ulong channelId, ulong? messageId, ulong? interactionId)
        {
            TagAliasId = tagAliasId;
            NewOwnerId = newOwnerId;
            OldOwnerId = oldOwnerId;
            Type = type;
            UserId = userId;
            ChangedAt = changedAt;
            GuildId = guildId;
            ChannelId = channelId;
            MessageId = messageId;
            InteractionId = interactionId;
        }

        public TagAliasOwnerChange(TagAlias alias, ulong newOwnerId, ulong oldOwnerId, OwnerChangeType type,
            ulong userId, DateTime changedAt, ulong guildId, ulong channelId, ulong? messageId, ulong? interactionId)
            : this(alias.Id, newOwnerId, oldOwnerId, type, userId, changedAt, guildId, channelId, messageId,
                interactionId)
        {
        }
    }
}
