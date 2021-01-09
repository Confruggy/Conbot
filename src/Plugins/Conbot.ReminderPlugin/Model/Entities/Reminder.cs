using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.ReminderPlugin
{
    public class Reminder
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public ulong UserId { get; set; }
        public ulong? GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }

        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }

        public DateTime EndsAt { get; set; }

        public bool Finished { get; set; }

        [NotMapped]
        public string Url => $"https://discordapp.com/channels/{GuildId?.ToString() ?? "@me"}/{ChannelId}/{MessageId}";

        public Reminder(ulong userId, ulong? guildId, ulong channelId, ulong messageId, string? message,
            DateTime createdAt, DateTime endsAt)
        {
            UserId = userId;
            GuildId = guildId;
            ChannelId = channelId;
            MessageId = messageId;
            Message = message;
            CreatedAt = createdAt;
            EndsAt = endsAt;
        }
    }
}
