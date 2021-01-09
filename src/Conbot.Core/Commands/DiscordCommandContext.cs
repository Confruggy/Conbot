using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Conbot.Interactive;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using Qmmands;

namespace Conbot.Commands
{
    public class DiscordCommandContext : CommandContext
    {
        private readonly List<IUserMessage> _messages;

        public DiscordShardedClient Client { get; }
        public SocketGuild? Guild { get; }
        public SocketUserMessage? Message => _messages.FirstOrDefault() as SocketUserMessage;
        public SocketInteraction? Interaction { get; }
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

        public DiscordCommandContext(DiscordShardedClient client, SocketInteraction interaction,
            IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Client = client;
            Interaction = interaction;
            Guild = Interaction.Guild;
            Channel = Interaction.Channel;
            User = Interaction.Member;
            _messages = new List<IUserMessage>();
        }

        public async Task<RestUserMessage> SendMessageAsync(string? text = null, bool isTTS = false,
            Embed? embed = null, RequestOptions? options = null, AllowedMentions? allowedMentions = null,
            MessageReference? reference = null)
        {
            var message = await Channel.SendMessageAsync(text, isTTS, embed, options, allowedMentions, reference);
            _messages.Add(message);
            return message;
        }

        public async Task<RestUserMessage> ReplyAsync(string? text = null, bool isTTS = false, Embed? embed = null,
            AllowedMentions? allowedMentions = null, RequestOptions? options = null)
        {
            if (Interaction != null)
            {
                var message = await Interaction.RespondAsync(text, isTTS, embed, allowedMentions: allowedMentions);
                return (RestUserMessage)message;
            }
            else
            {
                var message =
                    await Messages[^1].ReplyAsync(text, isTTS, embed, allowedMentions ?? AllowedMentions.None, options);
                _messages.Add(message);
                return (RestUserMessage)message;
            }
        }

        public async Task<(RestUserMessage, bool?)> ConfirmAsync(string? text, bool isTTS = false,
            Embed? embed = null, AllowedMentions? allowedMentions = null, RequestOptions? options = null,
            int timeout = 60000)
        {
            var interactiveService = ServiceProvider.GetRequiredService<InteractiveService>();
            var config = ServiceProvider.GetRequiredService<IConfiguration>();

            var message = await ReplyAsync(text, isTTS, embed, allowedMentions, options);

            bool? confirmed = null;

            var interactiveMessage = new InteractiveMessageBuilder()
                .WithPrecondition(x => x.Id == User.Id)
                .WithTimeout(timeout)
                .AddReactionCallback(config.GetValue<string>("Emotes:CheckMark"), x => x
                    .WithCallback(_ => confirmed = true))
                .AddReactionCallback(config.GetValue<string>("Emotes:CrossMark"), x => x
                    .WithCallback(_ => confirmed = false))
                .Build();

            await interactiveService.ExecuteInteractiveMessageAsync(interactiveMessage, message, User);
            return (message, confirmed);
        }

        public void AddMessage(IUserMessage message) => _messages.Add(message);
    }
}