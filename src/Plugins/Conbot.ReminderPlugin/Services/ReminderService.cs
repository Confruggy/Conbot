using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Conbot.TimeZonePlugin;
using Conbot.TimeZonePlugin.Extensions;

using Discord;
using Discord.WebSocket;

using Humanizer;

using NodaTime;
using Microsoft.Extensions.DependencyInjection;

namespace Conbot.ReminderPlugin
{
    public class ReminderService : IHostedService
    {
        private readonly ILogger<ReminderService> _logger;
        private readonly DiscordShardedClient _client;
        private readonly IDateTimeZoneProvider _provider;
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private Task? _task;

        public ReminderService(ILogger<ReminderService> logger, DiscordShardedClient client,
            IDateTimeZoneProvider provider, IConfiguration config, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _client = client;
            _provider = provider;
            _config = config;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _task = RunAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_task != null)
                await _task;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                using var reminderContext = scope.ServiceProvider.GetRequiredService<ReminderContext>();
                using var timeZoneContext = scope.ServiceProvider.GetRequiredService<TimeZoneContext>();

                var reminders = await reminderContext.Reminders.ToAsyncEnumerable()
                    .Where(x => !x.Finished && x.EndsAt < DateTime.UtcNow)
                    .ToArrayAsync(cancellationToken);

                foreach (var reminder in reminders)
                    reminder.Finished = true;

                if (reminders.Length > 0)
                    await reminderContext.SaveChangesAsync(cancellationToken);

                var tasks = new List<Task>();

                foreach (var reminder in reminders)
                {
                    var toSendChannel = _client.GetChannel(reminder.ChannelId) as IMessageChannel;

                    if (toSendChannel is ITextChannel textChannel)
                    {
                        var guild = (toSendChannel as IGuildChannel)?.Guild;

                        if (guild != null)
                        {
                            var botUser = await guild.GetCurrentUserAsync();

                            if (!botUser.GetPermissions(textChannel).SendMessages)
                                toSendChannel = null;
                        }
                    }

                    if (toSendChannel == null)
                    {
                        var user = _client.GetUser(reminder.UserId);

                        if (user != null)
                            toSendChannel = await user.GetOrCreateDMChannelAsync();
                    }

                    if (toSendChannel != null)
                    {
                        var userTimeZone = await timeZoneContext.GetUserTimeZoneAsync(reminder.UserId);
                        var timeZone = userTimeZone != null
                            ? _provider.GetZoneOrNull(userTimeZone.TimeZoneId)!
                            : _provider.GetSystemDefault();

                        string time = Instant.FromDateTimeUtc(reminder.EndsAt).InZone(timeZone)
                            .ToDurationString(
                                Instant.FromDateTimeUtc(reminder.CreatedAt).InZone(timeZone),
                                DurationLevel.Seconds,
                                showDateAt: Duration.FromDays(1),
                                formatted: true);

                        string text;
                        MessageReference? reference = null;
                        IMessage? message = null;

                        if (toSendChannel.Id == reminder.ChannelId)
                        {
                            message = await toSendChannel.GetMessageAsync(reminder.MessageId);

                            if (message is IUserMessage)
                            {
                                text = $"You've set a reminder {time}.";
                                reference = new MessageReference(
                                    reminder.MessageId, reminder.ChannelId, reminder.GuildId);
                            }
                            else
                            {
                                text = $"{MentionUtils.MentionUser(reminder.UserId)}, you've set a reminder {time}.";
                            }
                        }
                        else
                        {
                            text = $"You've set a reminder in {MentionUtils.MentionChannel(reminder.ChannelId)} {time}.";
                        }

                        Embed? embed;
                        if (reference != null)
                        {
                            embed = !string.IsNullOrEmpty(reminder.Message)
                                ? new EmbedBuilder()
                                    .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                                    .WithDescription(reminder.Message)
                                    .Build()
                                : null;
                        }
                        else
                        {
                            string hyperLink = $"[jump to message]({reminder.Url})";

                            embed = new EmbedBuilder()
                                .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                                .WithDescription(
                                    string.IsNullOrEmpty(reminder.Message)
                                        ? hyperLink
                                        : $"{reminder.Message.Truncate(2021 - hyperLink.Length)} ({hyperLink})")
                                .Build();
                        }

                        tasks.Add(Task.Run(async () =>
                        {
                            try { await toSendChannel.SendMessageAsync(text, embed: embed, messageReference: reference); }
                            catch (Exception e) { _logger.LogError(e, "Failed sending reminder message"); }
                        }, cancellationToken));
                    }
                }

                await Task.WhenAll(tasks);
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}
