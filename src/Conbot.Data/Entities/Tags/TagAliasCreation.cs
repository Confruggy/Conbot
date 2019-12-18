using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.Data.Entities
{
    public class TagAliasCreation
    {
        [Key]
        public int TagAliasId { get; set; }
        public TagAlias TagAlias { get; set; }

        public DateTime CreatedAt { get; set; }
        public ulong? GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong UserId { get; set; }

        [NotMapped]
        public string Url => $"https://discordapp.com/channels/{GuildId?.ToString() ?? "@me"}/{ChannelId}/{MessageId}";
    }
}