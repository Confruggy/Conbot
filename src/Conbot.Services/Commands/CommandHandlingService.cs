using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Conbot.Commands.TypeReaders;
using Conbot.InteractiveMessages;
using Conbot.Services.Help;
using Conbot.Services.Interactive;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conbot.Services.Commands
{
    public class CommandHandlingService : IHostedService
    {
        private readonly DiscordShardedClient _discordClient;
        private readonly CommandService _commandService;
        private readonly HelpService _helpService;
        private readonly InteractiveService _interactiveService;
        private readonly IServiceProvider _provider;
        private readonly ILogger<CommandHandlingService> _logger;
        private readonly ConcurrentDictionary<ulong, DateTimeOffset> _channelLocks;
        private readonly ConcurrentDictionary<ulong, DateTimeOffset> _userTimeouts;

        public CommandHandlingService(DiscordShardedClient client, CommandService service, HelpService helpService,
            InteractiveService interactiveService, IServiceProvider provider, ILogger<CommandHandlingService> logger)
        {
            _discordClient = client;
            _commandService = service;
            _helpService = helpService;
            _interactiveService = interactiveService;
            _provider = provider;
            _logger = logger;
            _channelLocks = new ConcurrentDictionary<ulong, DateTimeOffset>();
            _userTimeouts = new ConcurrentDictionary<ulong, DateTimeOffset>();
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            AddTypeReaders();
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

            _discordClient.MessageReceived += OnMessageReceivedAsync;
            _commandService.Log += OnLogAsync;
            _commandService.CommandExecuted += OnCommandExecutedAsync;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _discordClient.MessageReceived -= OnMessageReceivedAsync;
            _commandService.Log -= OnLogAsync;
            _commandService.CommandExecuted -= OnCommandExecutedAsync;

            return Task.CompletedTask;
        }

        private void AddTypeReaders()
        {
            _commandService.AddTypeReader<CommandInfo>(new CommandTypeReader());
            _commandService.AddTypeReader<ModuleInfo>(new ModuleTypeReader());
        }

        private Task OnMessageReceivedAsync(SocketMessage message)
        {
            Task.Run(async () =>
            {
                if (_userTimeouts.TryGetValue(message.Author.Id, out var date1))
                {
                    double ms = (date1 - DateTimeOffset.Now).TotalMilliseconds;
                    if (ms > 0)
                        return;
                    _userTimeouts[message.Author.Id] = DateTimeOffset.Now.AddSeconds(1.1);
                }
                else _userTimeouts.TryAdd(message.Author.Id, DateTimeOffset.Now.AddSeconds(1.1));

                if (_channelLocks.TryGetValue(message.Channel.Id, out var date2))
                {
                    double ms = (date2 - DateTimeOffset.Now).TotalMilliseconds;
                    if (ms > 0)
                    {
                        _channelLocks[message.Channel.Id] = DateTimeOffset.Now.AddSeconds(1.1).AddMilliseconds(ms);
                        await Task.Delay((int)ms).ConfigureAwait(false);
                    }
                }
                else _channelLocks.TryAdd(message.Channel.Id, DateTimeOffset.Now.AddSeconds(1.1));

                await HandleCommandAsync(message as SocketUserMessage);
            });
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

            using var scope = _provider.CreateScope();
            await _commandService.ExecuteAsync(context, argPos, scope.ServiceProvider);
        }

        private Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            Task.Run(async () =>
            {
                if (result.IsSuccess)
                    return;

                if (result is ExecuteResult executeResult && executeResult.Exception != null)
                {
                    await context.Channel.SendMessageAsync(
                            $"{Format.Bold("An unexpected error occured")}: {executeResult.ErrorReason}");
                    return;
                }

                var message = await context.Channel.SendMessageAsync(result.ErrorReason);

                var socketContext = context as SocketCommandContext;

                if (command.IsSpecified)
                {
                    var interactiveMessage = new InteractiveMessageBuilder()
                        .WithPrecondition(x => x.Id == context.User.Id)
                        .AddReactionCallback(x => x
                            .WithEmote("info:654781462360490025")
                            .WithCallback(async r =>
                            {
                                await _helpService.ExecuteHelpMessageAsync(socketContext, startCommand: command.Value,
                                    message: message);
                            }))
                        .Build();

                    await _interactiveService.ExecuteInteractiveMessageAsync(interactiveMessage, message, context.User);
                }

            });
            return Task.CompletedTask;
        }

        private Task OnLogAsync(LogMessage message)
        {
            _logger.Log(LogLevelFromSeverity(message.Severity), message.Exception,
                $"{message.Source}: {{Message}}", message.Message);
            return Task.CompletedTask;
        }

        private static LogLevel LogLevelFromSeverity(LogSeverity severity)
            => (LogLevel)Math.Abs((int)severity - 5);
    }
}
