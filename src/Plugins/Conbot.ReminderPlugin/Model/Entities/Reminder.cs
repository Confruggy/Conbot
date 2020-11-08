using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.ReminderPlugin
{
    public class Reminder
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public ulong UserId { get; set; }

        public ulong? GuildId { get; set; }

        [Required]
        public ulong ChannelId { get; set; }

        [Required]
        public ulong MessageId { get; set;}
        
        public string Message { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime EndsAt { get; set; }

        public bool Finished { get; set; }

        [NotMapped]
        public string Url => $"https://discordapp.com/channels/{GuildId?.ToString() ?? "@me"}/{ChannelId}/{MessageId}";
    }
}