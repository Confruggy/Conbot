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
        private readonly IServiceProvider _provider = new ServiceCollection().BuildServiceProvider();

        public CommandHandler(DiscordShardedClient client)
        {
            _discordClient = client;
            _service = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Sync,
                LogLevel = LogSeverity.Debug,
            });
        }

        public async Task InstallAsync()
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
