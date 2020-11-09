using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.TagPlugin
{
    public class TagUse
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TagId { get; set; }
        public virtual Tag Tag { get; set; }

        public int? UsedAliasId { get; set; }
        public virtual TagAlias UsedAlias { get; set; }

        public DateTime UsedAt { get; set; }
        public ulong? GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong UserId { get; set; }

        [NotMapped]
        public string Url => $"https://discordapp.com/channels/{GuildId?.ToString() ?? "@me"}/{ChannelId}/{MessageId}";
    }
}