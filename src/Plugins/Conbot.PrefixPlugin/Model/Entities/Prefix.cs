namespace Conbot.PrefixPlugin
{
    public class Prefix
    {
        public ulong GuildId { get; set; }
        public string Text { get; set; }

        public Prefix(ulong guildId, string text)
        {
            GuildId = guildId;
            Text = text;
        }
    }
}