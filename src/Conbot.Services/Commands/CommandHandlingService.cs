using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Conbot.Commands.TypeReaders;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conbot.Services.Commands
{
    public class CommandHandlingService : IHostedService
    {
        private readonly DiscordShardedClient _discordClient;
        private readonly CommandService _service;
        private readonly IServiceProvider _provider;
        private readonly ILogger<CommandHandlingService> _logger;

        public CommandHandlingService(DiscordShardedClient client, CommandService service, IServiceProvider provider,
            ILogger<CommandHandlingService> logger)
        {
            _discordClient = client;
            _service = service;
            _provider = provider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            AddTypeReaders();
            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

            _discordClient.MessageReceived += OnMessageReceivedAsync;
            _service.Log += OnLogAsync;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _discordClient.MessageReceived -= OnMessageReceivedAsync;
            _service.Log -= OnLogAsync;

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

        private Task OnLogAsync(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Debug:
                    _logger.LogDebug($"{{Source}}: {message.Message}", message.Source);
                    break;
                case LogSeverity.Verbose:
                    _logger.LogTrace($"{{Source}}: {message.Message}", message.Source);
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation($"{{Source}}: {message.Message}", message.Source);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning($"{{Source}}: {message.Message}", message.Source);
                    break;
                case LogSeverity.Error:
                    _logger.LogError($"{{Source}}: {message.Message}", message.Source);
                    break;
                case LogSeverity.Critical:
                    _logger.LogCritical($"{{Source}}: {message.Message}", message.Source);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
