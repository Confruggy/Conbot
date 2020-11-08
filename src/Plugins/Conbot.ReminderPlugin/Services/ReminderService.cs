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
        Task _task;

        public ReminderService(ILogger<ReminderService> logger, DiscordShardedClient client,
            IDateTimeZoneProvider provider)
        {
            _logger = logger;
            _client = client;
            _provider = provider;
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

                    if (reminders.Any())
                        await db.SaveChangesAsync();
                }

                var tasks = new List<Task>();

                foreach (var reminder in reminders)
                {
                    IMessageChannel toSendChannel;
                    var channel = _client.GetChannel(reminder.ChannelId) as IMessageChannel;

                    if (channel != null)
                    {
                        toSendChannel = channel;

                        if (channel is ITextChannel textChannel)
                        {
                            var guild = (channel as IGuildChannel)?.Guild;

                            if (guild != null)
                            {
                                var botUser = await guild.GetCurrentUserAsync();

                                if (!botUser.GetPermissions(textChannel).SendMessages)
                                    toSendChannel = await _client.GetDMChannelAsync(reminder.UserId);
                            }
                        }
                    }
                    else toSendChannel = await _client.GetDMChannelAsync(reminder.UserId);

                    if (toSendChannel != null)
                    {
                        string hyperLink = $"[jump to message]({reminder.Url})";

                        var embed = new EmbedBuilder()
                            .WithColor(Constants.DefaultEmbedColor)
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
                            .ToDurationFormattedString(Instant.FromDateTimeUtc(reminder.CreatedAt).InZone(timeZone));

                        string text;
                        if (toSendChannel.Id == channel.Id)
                            text = $"{mention}, you've set a reminder {time}.";
                        else text = $"{mention}, you've set a reminder in {MentionUtils.MentionChannel(reminder.ChannelId)} {time}.";

                        tasks.Add(Task.Run(async () =>
                        {
                            try { await channel.SendMessageAsync(text, embed: embed); }
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