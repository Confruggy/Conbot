using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Conbot.Commands.TypeReaders;
using Conbot.Logging;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace Conbot.Services.Commands
{
    public class CommandHandlingService : IHostedService
    {
        private readonly DiscordShardedClient _discordClient;
        private readonly CommandService _service;
        private readonly IServiceProvider _provider;

        public CommandHandlingService(DiscordShardedClient client, CommandService service, IServiceProvider provider)
        {
            _discordClient = client;
            _service = service;
            _provider = provider;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            AddTypeReaders();
            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

            _discordClient.MessageReceived += OnMessageReceivedAsync;
            _service.Log += ConsoleLog.LogAsync;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _discordClient.MessageReceived -= OnMessageReceivedAsync;
            _service.Log -= ConsoleLog.LogAsync;

            return Task.CompletedTask;
        }

        private void AddTypeReaders()
        {
            _service.AddTypeReader<CommandInfo>(new CommandTypeReader());
            _service.AddTypeReader<ModuleInfo>(new ModuleTypeReader());
        }

        private Task OnMessageReceivedAsync(SocketMessage message)
        {
            _ = HandleCommandAsync(message as SocketUserMessage);
            return Task.CompletedTask;
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
