using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conbot.TimeZonePlugin;
using Conbot.TimeZonePlugin.Extensions;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Conbot.ReminderPlugin
{
    public class ReminderService : IHostedService
    {
        private readonly ILogger<ReminderService> _logger;
        private readonly DiscordShardedClient _client;
        private readonly IDateTimeZoneProvider _provider;
        private readonly IConfiguration _config;
        private Task _task;

        public ReminderService(ILogger<ReminderService> logger, DiscordShardedClient client,
            IDateTimeZoneProvider provider, IConfiguration config)
        {
            _logger = logger;
            _client = client;
            _provider = provider;
            _config = config;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _task = RunAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _task;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Reminder[] reminders;

                using (var db = new ReminderContext())
                {
                    reminders = await db.Reminders.ToAsyncEnumerable()
                        .Where(x => !x.Finished && x.EndsAt < DateTime.UtcNow)
                        .ToArrayAsync();

                    foreach (var reminder in reminders)
                        reminder.Finished = true;

                    if (reminders.Length > 0)
                        await db.SaveChangesAsync();
                }

                var tasks = new List<Task>();

                foreach (var reminder in reminders)
                {
                    var toSendChannel = _client.GetChannel(reminder.ChannelId) as IMessageChannel;

                    if (toSendChannel != null && toSendChannel is ITextChannel textChannel)
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
                        string hyperLink = $"[jump to message]({reminder.Url})";

                        var embed = new EmbedBuilder()
                            .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                            .WithDescription(
                                string.IsNullOrEmpty(reminder.Message)
                                    ? hyperLink
                                    : $"{reminder.Message.Truncate(2021 - hyperLink.Length)} ({hyperLink})")
                            .Build();

                        string mention =
                            _client.GetUser(reminder.UserId)?.Mention
                            ?? MentionUtils.MentionUser(reminder.UserId);

                        DateTimeZone timeZone;

                        using (var timeZoneContext = new TimeZoneContext())
                        {
                            var userTimeZone = await timeZoneContext.GetUserTimeZoneAsync(reminder.UserId);
                            timeZone = userTimeZone != null
                                ? _provider.GetZoneOrNull(userTimeZone.TimeZoneId)
                                : null;
                        }

                        timeZone ??= _provider.GetSystemDefault();

                        string time = Instant.FromDateTimeUtc(reminder.EndsAt).InZone(timeZone)
                            .ToDurationString(
                                Instant.FromDateTimeUtc(reminder.CreatedAt).InZone(timeZone),
                                DurationLevel.Seconds,
                                showDateAt: Duration.FromDays(1),
                                formatted: true);

                        string text;
                        if (toSendChannel.Id == reminder.ChannelId)
                            text = $"{mention}, you've set a reminder {time}.";
                        else text = $"{mention}, you've set a reminder in {MentionUtils.MentionChannel(reminder.ChannelId)} {time}.";

                        tasks.Add(Task.Run(async () =>
                        {
                            try { await toSendChannel.SendMessageAsync(text, embed: embed); }
                            catch (Exception e) { _logger.LogError(e, "Failed sending reminder message"); }
                        }));
                    }
                }

                await Task.WhenAll(tasks);
                await Task.Delay(100);
            }
        }
    }
}