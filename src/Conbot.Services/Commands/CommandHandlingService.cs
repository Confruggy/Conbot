using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Conbot.Commands;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Qmmands;

namespace Conbot.Services.Commands
{
    public class CommandHandlingService : IHostedService
    {
        private readonly DiscordShardedClient _discordClient;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _provider;
        private readonly ILogger<CommandHandlingService> _logger;
        private readonly ConcurrentDictionary<ulong, DateTimeOffset> _channelLocks;
        private readonly ConcurrentDictionary<ulong, DateTimeOffset> _userTimeouts;
        private DefaultPrefixHandler _defaultPrefixHandler;
        public IPrefixHandler CustomPrefixHandler { get; set; }

        public CommandHandlingService(DiscordShardedClient client, CommandService service, IServiceProvider provider,
            ILogger<CommandHandlingService> logger)
        {
            _discordClient = client;
            _commandService = service;
            _provider = provider;
            _logger = logger;
            _channelLocks = new ConcurrentDictionary<ulong, DateTimeOffset>();
            _userTimeouts = new ConcurrentDictionary<ulong, DateTimeOffset>();
            _defaultPrefixHandler = DefaultPrefixHandler.Instance;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            AddTypeParsers();

            _discordClient.MessageReceived += OnMessageReceivedAsync;
            _commandService.CommandExecuted += OnCommandExecutedAsync;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _discordClient.MessageReceived -= OnMessageReceivedAsync;
            _commandService.CommandExecuted -= OnCommandExecutedAsync;

            return Task.CompletedTask;
        }

        private void AddTypeParsers()
        {
            _commandService.AddTypeParser(new CommandTypeParser());
            _commandService.AddTypeParser(new ModuleTypeParser());

            _commandService.AddTypeParser(new UserTypeParser<IUser>());
            _commandService.AddTypeParser(new UserTypeParser<SocketUser>());
            _commandService.AddTypeParser(new UserTypeParser<IGuildUser>());
            _commandService.AddTypeParser(new UserTypeParser<SocketGuildUser>());

            _commandService.AddTypeParser(new RoleTypeParser<IRole>());
            _commandService.AddTypeParser(new RoleTypeParser<SocketRole>());

            _commandService.AddTypeParser(new ChannelTypeParser<IChannel>());
            _commandService.AddTypeParser(new ChannelTypeParser<ITextChannel>());
            _commandService.AddTypeParser(new ChannelTypeParser<SocketTextChannel>());
            _commandService.AddTypeParser(new ChannelTypeParser<IGuildChannel>());
            _commandService.AddTypeParser(new ChannelTypeParser<SocketGuildChannel>());
            _commandService.AddTypeParser(new ChannelTypeParser<IVoiceChannel>());
            _commandService.AddTypeParser(new ChannelTypeParser<SocketVoiceChannel>());
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

            var prefixHandler = CustomPrefixHandler ?? _defaultPrefixHandler;

            using var scope = _provider.CreateScope();
            var context = new DiscordCommandContext(_discordClient, msg, scope.ServiceProvider);

            if (!await prefixHandler.HandlePrefixAsync(context, out string output))
                return;

            var result = await _commandService.ExecuteAsync(output, context);

            if (result.IsSuccessful)
                return;

            await context.Channel.SendMessageAsync(GetErrorMessage(result as FailedResult));
        }

        public string GetErrorMessage(FailedResult result)
        {
            switch (result)
            {
                case OverloadsFailedResult overloadsFailedResult:
                    {
                        var failedOverloads = overloadsFailedResult.FailedOverloads;

                        if (failedOverloads.Count() == 1)
                            return GetErrorMessage(failedOverloads.First().Value);

                        var text = new StringBuilder().AppendLine("Several possible errors occured:");

                        for (int i = 0; i < failedOverloads.Count; i++)
                        {
                            var failedResult = failedOverloads.Values.ElementAt(i);
                            text.AppendLine($"`{i + 1}.` {GetErrorMessage(failedResult)}");
                        }

                        return text.ToString();
                    }
                case ArgumentParseFailedResult argumentParserFailedResult:
                    {
                        var commandParameters = argumentParserFailedResult.Command.Parameters.Where(x => !x.IsOptional);
                        var parsedParameters = argumentParserFailedResult.ParserResult.Arguments
                            .Select(x => x.Key).Where(x => !x.IsOptional);

                        if (commandParameters.Count() > parsedParameters.Count())
                        {
                            var missingParameters = commandParameters.Except(parsedParameters);

                            return new StringBuilder()
                                .Append("Required ")
                                .Append("parameter".ToQuantity(missingParameters.Count(), ShowQuantityAs.None))
                                .Append(" ")
                                .Append(missingParameters.Humanize(x => Format.Bold(x.Name)))
                                .Append(" ")
                                .Append("is".ToQuantity(missingParameters.Count(), ShowQuantityAs.None))
                                .Append(" missing.")
                                .ToString();
                        }

                        goto default;
                    }
                case ChecksFailedResult checksFailedResult:
                    {
                        var failedChecks = checksFailedResult.FailedChecks;

                        if (failedChecks.Count() == 1)
                            return failedChecks.First().Result.Reason;

                        var text = new StringBuilder().AppendLine("Several checks failed:");

                        for (int i = 0; i < failedChecks.Count; i++)
                        {
                            var checkResult = failedChecks.ElementAt(i).Result;
                            text.AppendLine($"`{i + 1}.` {checkResult.Reason}");
                        }

                        return text.ToString();
                    }
                case ParameterChecksFailedResult parameterChecksFailedResult:
                    {
                        var failedChecks = parameterChecksFailedResult.FailedChecks;

                        if (failedChecks.Count() == 1)
                            return failedChecks.First().Result.Reason;

                        var text = new StringBuilder().AppendLine("Several parameter checks failed:");

                        for (int i = 0; i < failedChecks.Count; i++)
                        {
                            var checkResult = failedChecks.ElementAt(i).Result;
                            text.AppendLine($"`{i + 1}.` {checkResult.Reason}");
                        }

                        return text.ToString();

                    }
                default: return result.Reason;
            }
        }

        private Task OnCommandExecutedAsync(CommandExecutedEventArgs args)
        {
            var discordCommandContext = args.Context as DiscordCommandContext;

            string command = discordCommandContext.Command.FullAliases.FirstOrDefault();
            string user = discordCommandContext.User.ToString();
            string guild = discordCommandContext.Guild?.Name;
            string channel = discordCommandContext.Channel.Name;

            if (guild != null)
                _logger.Log(LogLevel.Information,
                    "Commands: Command {command} executed for {user} in {guild}/{channel}",
                        command, user, guild, channel);
            else
                _logger.Log(LogLevel.Information,
                    "Commands: Command {command} executed for {user} in {channel}", command, user, channel);

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
