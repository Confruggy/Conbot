using System;
using System.Reflection;
using System.Threading.Tasks;
using Conbot.Logging;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Conbot.Commands
{
    public class CommandHandler
    {
        private readonly DiscordShardedClient _discordClient;
        private readonly CommandService _service;
        private readonly IServiceProvider _provider;

        public CommandHandler(DiscordShardedClient client, CommandService service, IServiceProvider provider)
        {
            _discordClient = client;
            _service = service;
            _provider = provider;
        }

        public async Task StartAsync()
        {
            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

            _discordClient.MessageReceived += (msg) =>
            {
                _ = OnMessageReceivedAsync(msg);
                return Task.CompletedTask;
            };

            _service.Log += ConsoleLog.LogAsync;
        }

        public async Task OnMessageReceivedAsync(SocketMessage message)
        {
            await HandleCommandAsync(message as SocketUserMessage);
        }

        public async Task HandleCommandAsync(SocketUserMessage msg)
        {
            if (msg == null)
                return;

            if (msg.Author.IsBot)
                return;

            int argPos = 0;

            if (!(msg.HasMentionPrefix(_discordClient.CurrentUser, ref argPos) || msg.HasStringPrefix("!", ref argPos)))
                return;

            if (msg.Content.Length == argPos)
                return;

            var context = new ShardedCommandContext(_discordClient, msg);

            var result = await _service.ExecuteAsync(context, argPos, _provider).ConfigureAwait(false);
        }
    }
}
