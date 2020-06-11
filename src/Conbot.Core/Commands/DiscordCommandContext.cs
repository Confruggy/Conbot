using System;
using Discord.WebSocket;
using Qmmands;

namespace Conbot.Commands
{
    public class DiscordCommandContext : CommandContext
    {
        public DiscordShardedClient Client { get; }
        public SocketGuild Guild { get; }
        public SocketUserMessage Message { get; set; }
        public ISocketMessageChannel Channel { get; set; }
        public SocketUser User { get; }

        public DiscordCommandContext(DiscordShardedClient client, SocketUserMessage message,
            IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Client = client;
            Guild = (message.Channel as SocketTextChannel)?.Guild;
            Channel = message.Channel;
            User = message.Author;
            Message = message;
        }
    }
}