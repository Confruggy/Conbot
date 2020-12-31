using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.TagPlugin
{
    public class TagAliasOwnerChange
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TagAliasId { get; set; }
        public virtual TagAlias TagAlias { get; set; }

        [Required]
        public ulong NewOwnerId { get; set; }
        [Required]
        public ulong OldOwnerId { get; set; }

        [Required]
        public OwnerChangeType Type { get; set; }

        [Required]
        public ulong UserId { get; set; }
        [Required]
        public DateTime ChangedAt { get; set; }
        [Required]
        public ulong GuildId { get; set; }
        [Required]
        public ulong ChannelId { get; set; }
        public ulong? MessageId { get; set; }
        public ulong? InteractionId { get; set; }

        [NotMapped]
        public string Url =>
            $"https://discordapp.com/channels/{GuildId}/{ChannelId}/{MessageId ?? InteractionId}";
    }
}