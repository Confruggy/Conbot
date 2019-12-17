namespace Conbot.Data.Entities
{
    public class Message
    {
        public ulong? GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong UserId { get; set; }
    }
}