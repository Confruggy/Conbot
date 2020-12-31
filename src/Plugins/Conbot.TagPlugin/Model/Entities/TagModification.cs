using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.TagPlugin
{
    public class TagModification
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TagId { get; set; }
        public virtual Tag Tag { get; set; }

        [Required]
        public string NewContent { get; set; }
        [Required]
        public string OldContent { get; set; }

        [Required]
        public ulong UserId { get; set; }
        [Required]
        public DateTime ModifiedAt { get; set; }
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