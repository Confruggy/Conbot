using System.ComponentModel.DataAnnotations;

namespace Conbot.WelcomePlugin
{
    public class WelcomeConfiguration
    {
        [Key]
        public ulong GuildId { get; set; }

        public bool ShowWelcomeMessages { get; set; }
        public string? WelcomeMessageTemplate { get; set; }
        public ulong? WelcomeChannelId { get; set; }

        public bool ShowGoodbyeMessages { get; set; }
        public string? GoodbyeMessageTemplate { get; set; }
        public ulong? GoodbyeChannelId { get; set; }

        public WelcomeConfiguration(ulong guildId)
        {
            GuildId = guildId;
        }
    }
}
