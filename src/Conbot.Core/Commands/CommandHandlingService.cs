using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Discord;
using Discord.WebSocket;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public partial class CommandHandlingService : IHostedService
    {
        private readonly DiscordShardedClient _discordClient;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _provider;
        private readonly ILogger<CommandHandlingService> _logger;
        private readonly ConcurrentDictionary<ulong, DateTimeOffset> _channelLocks;
        private readonly ConcurrentDictionary<ulong, DateTimeOffset> _userTimeouts;
        private readonly DefaultPrefixHandler _defaultPrefixHandler;

        public IPrefixHandler? CustomPrefixHandler { get; set; }

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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await UnregisterAllCommandsAsync();

            _commandService.SetDefaultArgumentParser(new InteractionArgumentParser());
            AddTypeParsers();

            _discordClient.MessageReceived += OnMessageReceivedAsync;
            _discordClient.InteractionCreated += OnInteractionCreatedAsync;
            _commandService.CommandExecuted += OnCommandExecutedAsync;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discordClient.MessageReceived -= OnMessageReceivedAsync;
            _commandService.CommandExecuted -= OnCommandExecutedAsync;

            return Task.CompletedTask;
        }

        private async Task UnregisterAllCommandsAsync()
        {
            var commands = await _discordClient.Rest.GetGlobalApplicationCommands();

            foreach (var command in commands)
            {
                await command.DeleteAsync();
            }
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

            _commandService.AddTypeParser(new TimeSpanTypeParser());
        }

        private Task OnInteractionCreatedAsync(SocketInteraction interaction)
        {
            if (interaction.Type != InteractionType.ApplicationCommand)
                return Task.CompletedTask;

            var commandString = new StringBuilder()
                .Append(interaction.Data.Name);

            var option = interaction.Data.Options?.Cast<IApplicationCommandInteractionDataOption>()?.First();

            if (option != null && option.Value == null)
            {
                commandString
                    .Append(' ')
                    .Append(option.Name);

                option = option.Options?.First();

                if (option != null && option.Value == null)
                {
                    commandString
                        .Append(' ')
                        .Append(option.Name);
                }
            }

            Task.Run(async () =>
            {
                using var scope = _provider.CreateScope();
                var context = new DiscordCommandContext(_discordClient, interaction, scope.ServiceProvider);

                var result = await _commandService.ExecuteAsync(commandString.ToString(), context);

                if (!result.IsSuccessful)
                    await HandleCommandErrorAsync(result, context);
            });
            return Task.CompletedTask;
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
                else
                {
                    _userTimeouts.TryAdd(message.Author.Id, DateTimeOffset.Now.AddSeconds(1.1));
                }

                if (_channelLocks.TryGetValue(message.Channel.Id, out var date2))
                {
                    double ms = (date2 - DateTimeOffset.Now).TotalMilliseconds;
                    if (ms > 0)
                    {
                        _channelLocks[message.Channel.Id] = DateTimeOffset.Now.AddSeconds(1.1).AddMilliseconds(ms);
                        await Task.Delay((int)ms);
                    }
                }
                else
                {
                    _channelLocks.TryAdd(message.Channel.Id, DateTimeOffset.Now.AddSeconds(1.1));
                }

                if (message is SocketUserMessage socketUserMessage)
                    await HandleCommandAsync(socketUserMessage);
            });
            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketUserMessage msg)
        {
            if (msg.Author.IsBot)
                return;

            var prefixHandler = CustomPrefixHandler ?? _defaultPrefixHandler;

            using var scope = _provider.CreateScope();
            var context = new DiscordCommandContext(_discordClient, msg, scope.ServiceProvider);

            var prefixResult = await prefixHandler.HandlePrefixAsync(context);

            if (!prefixResult.IsSuccessful)
                return;

            var result = await _commandService.ExecuteAsync(prefixResult.Output, context);

            if (!result.IsSuccessful)
                await HandleCommandErrorAsync(result, context);
        }

        private async Task HandleCommandErrorAsync(IResult result, DiscordCommandContext context)
        {
            if (result is RuntimeResult runtimeResult && runtimeResult.ErrorReason is not null)
            {
                var runtimeFailedResult = new RuntimeFailedResult(runtimeResult.Command, runtimeResult.ErrorReason,
                    runtimeResult.Message);

                if (runtimeResult.Message is not null)
                {
                    await _errorMessageSent.InvokeAsync(
                        new CommandErrorMessageSentEventArgs(runtimeResult.Message, context, runtimeFailedResult));
                    return;
                }

                result = runtimeFailedResult;
            }

            if (result is FailedResult failedResult)
            {
                var message = await context
                    .ReplyAsync(GetErrorMessage(failedResult), allowedMentions: AllowedMentions.None);

                await _errorMessageSent.InvokeAsync(
                    new CommandErrorMessageSentEventArgs(message, context, failedResult));
            }
        }

        public string GetErrorMessage(FailedResult result)
        {
            switch (result)
            {
                case OverloadsFailedResult overloadsFailedResult:
                    {
                        var failedOverloads = overloadsFailedResult.FailedOverloads;

                        if (failedOverloads.Count == 1)
                            return GetErrorMessage(failedOverloads.First().Value);

                        var text = new StringBuilder().AppendLine("Several possible errors occurred:");

                        for (int i = 0; i < failedOverloads.Count; i++)
                        {
                            var failedResult = failedOverloads.Values.ElementAt(i);

                            text
                                .Append('`')
                                .Append(i + 1)
                                .Append(".` ")
                                .AppendLine(GetErrorMessage(failedResult));
                        }

                        return text.ToString();
                    }
                case ArgumentParseFailedResult argumentParserFailedResult:
                    {
                        var commandParameters = argumentParserFailedResult.Command.Parameters.Where(x => !x.IsOptional);
                        var parsedParameters = argumentParserFailedResult.ParserResult.Arguments
                            .Select(x => x.Key)
                            .Where(x => !x.IsOptional);

                        if (commandParameters.Count() > parsedParameters.Count())
                        {
                            var missingParameters = commandParameters.Except(parsedParameters);

                            return new StringBuilder()
                                .Append("Required ")
                                .Append("parameter".ToQuantity(missingParameters.Count(), ShowQuantityAs.None))
                                .Append(' ')
                                .Append(missingParameters.Humanize(x => Format.Bold(x.Name)))
                                .Append(' ')
                                .Append("is".ToQuantity(missingParameters.Count(), ShowQuantityAs.None))
                                .Append(" missing.")
                                .ToString();
                        }

                        break;
                    }
                case ChecksFailedResult checksFailedResult:
                    {
                        var failedChecks = checksFailedResult.FailedChecks;

                        if (failedChecks.Count == 1)
                            return failedChecks[0].Result.Reason;

                        var text = new StringBuilder().AppendLine("Several checks failed:");

                        for (int i = 0; i < failedChecks.Count; i++)
                        {
                            var checkResult = failedChecks[i].Result;

                            text
                                .Append('`')
                                .Append(i + 1)
                                .Append(".` ")
                                .AppendLine(checkResult.Reason);
                        }

                        return text.ToString();
                    }
                case ParameterChecksFailedResult parameterChecksFailedResult:
                    {
                        var failedChecks = parameterChecksFailedResult.FailedChecks;

                        if (failedChecks.Count == 1)
                            return failedChecks[0].Result.Reason;

                        var text = new StringBuilder().AppendLine("Several parameter checks failed:");

                        for (int i = 0; i < failedChecks.Count; i++)
                        {
                            var checkResult = failedChecks[i].Result;

                            text
                                .Append('`')
                                .Append(i + 1)
                                .Append(".` ")
                                .AppendLine(checkResult.Reason);
                        }

                        return text.ToString();
                    }
            }

            return result.Reason;
        }

        private Task OnCommandExecutedAsync(CommandExecutedEventArgs args)
        {
            if (args.Context is not DiscordCommandContext discordCommandContext)
                return Task.CompletedTask;

            string? command = discordCommandContext.Command.FullAliases.FirstOrDefault();
            string user = discordCommandContext.User.ToString();
            string? guild = discordCommandContext.Guild?.Name;
            string channel = discordCommandContext.Channel.Name;

            if (guild != null)
            {
                _logger.Log(LogLevel.Information,
                   "Commands: Command {command} executed for {user} in {guild}/{channel}",
                       command, user, guild, channel);
            }
            else
            {
                _logger.Log(LogLevel.Information,
                   "Commands: Command {command} executed for {user} in {channel}", command, user, channel);
            }

            return Task.CompletedTask;
        }
    }
}
