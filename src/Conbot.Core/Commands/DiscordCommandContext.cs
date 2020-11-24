using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Qmmands;

namespace Conbot.Commands
{
    public class DiscordCommandContext : CommandContext
    {
        public DiscordShardedClient Client { get; }
        public SocketGuild Guild { get; }
        private readonly List<IUserMessage> _messages;
        public SocketUserMessage Message => (SocketUserMessage)_messages.FirstOrDefault();
        public ReadOnlyCollection<IUserMessage> Messages => _messages.AsReadOnly();
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
            _messages = new List<IUserMessage>() { message };
        }

        public async Task<RestUserMessage> SendMessageAsync(string text = null, bool isTTS = false, Embed embed = null,
            RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference reference = null)
        {
            var message = await Channel.SendMessageAsync(text, isTTS, embed, options, allowedMentions, reference);
            _messages.Add(message);
            return message;
        }

        public async Task<RestUserMessage> ReplyAsync(string text = null, bool isTTS = false, Embed embed = null,
            AllowedMentions allowedMentions = null, RequestOptions options = null)
        {
            var message =
                await Messages[^1].ReplyAsync(text, isTTS, embed, allowedMentions ?? AllowedMentions.None, options);
            _messages.Add(message);
            return (RestUserMessage)message;
        }

        public void AddMessage(IUserMessage message) => _messages.Add(message);
    }
}