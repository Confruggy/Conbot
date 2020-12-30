using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.Services.Interactive;
using Conbot.TimeZonePlugin;
using Conbot.TimeZonePlugin.Extensions;
using Discord;
using Humanizer;
using Microsoft.Extensions.Configuration;
using NodaTime;
using NodaTime.Extensions;
using Qmmands;

namespace Conbot.ReminderPlugin
{
    [Name("Reminder")]
    [Description("Create scheduled reminders.")]
    [Group("reminder", "remind", "timer")]
    [RequireTimeZone]
    public class ReminderModule : DiscordModuleBase
    {
        private readonly ReminderContext _db;
        private readonly InteractiveService _interactiveService;
        private readonly IConfiguration _config;

        public ReminderModule(ReminderContext context, InteractiveService interactiveService, IConfiguration config)
        {
            _db = context;
            _interactiveService = interactiveService;
            _config = config;
        }

        [Command("set", "")]
        [Description("Reminds you about something after a certain time.")]
        [Remarks(
            "With text commands the message can be placed directly after the time. However, with Slash Commands it " +
            "must be seperated.\n\n" +
            "Examples for times and dates:\n" +
            "• \"at 7:30 wake up\"\n" +
            "• \"on the 19th April wish them a happy birthday\"\n" +
            "• \"next monday at 8pm watch the new episode\"\n\n" +
            "Examples for durations:\n" +
            "• \"in 20 minutes remove pizza from oven\"\n" +
            "• \"in 5 hours 30 min do laundry\"\n" +
            "• \"2h unmute someone\"")]
        [OverrideArgumentParser(typeof(ReminderArgumentParser))]
        public async Task ReminderAsync(
            [Description("The time when you want to be reminded.")]
            [Remarks(
                "It can be either a human readable time and/or date or a duration. " +
                "However, you can't combine both. " +
                "If a date has been set without a time, the time will default to 12 pm. " +
                "With text commands this parameter has to be entered without quotation marks.")]
            ZonedDateTimeParseResult time,
            [Description("The message for the reminder.")]
            [Remainder]
            string message = null)
        {
            if (time.Then.Value.LocalDateTime < time.Now.LocalDateTime)
            {
                await ReplyAsync("This time is in the past.");
                return;
            }

            await _db.AddReminderAsync(Context, time.Now.ToDateTimeUtc(), time.Then.Value.ToDateTimeUtc(),
                message?.Trim());

            var text = new StringBuilder()
                .Append("⏰ Reminder has been set. You'll be reminded ")
                .Append(time.Now.ToDurationString(time.Then.Value, DurationLevel.Seconds,
                    showDateAt: Duration.FromDays(1), formatted: true))
                .Append(".");

            await Task.WhenAll(
                ReplyAsync(text.ToString()),
                _db.SaveChangesAsync()
            );
        }

        [Command("list", "all")]
        [Description("Lists all your upcoming reminders.")]
        public async Task ListAsync(
            [Description("Wether reminders should be displayed in a compact or detailed view.")]
            [Choices("compact", "detailed")]
            [Remainder]
            string view = "compact")
        {
            var timeZone = await Context.GetUserTimeZoneAsync();
            var now = SystemClock.Instance.InZone(timeZone)
                .GetCurrentZonedDateTime();

            var reminders = (await _db.GetRemindersAsync(Context.User))
                .Where(x => x.EndsAt > now.ToDateTimeUtc())
                .OrderBy(x => x.EndsAt);

            if (!reminders.Any())
            {
                await ReplyAsync("You don't have any upcoming reminders.");
                return;
            }

            int count = reminders.Count();

            var paginator = new Paginator();

            view = view.ToLowerInvariant();
            if (view == "compact")
            {
                int entryPos = 1;
                int pageIndex = 0;
                int pageCount = count / 5;
                if (count % 5 != 0)
                    pageCount++;

                var page = new EmbedBuilder();

                foreach (var reminder in reminders)
                {
                    var endsAt = Instant.FromDateTimeUtc(reminder.EndsAt).InZone(now.Zone);
                    page.AddField(
                        $"{now.ToDurationString(endsAt, accuracy: 3).Humanize()} (ID: {reminder.Id})",
                        !string.IsNullOrEmpty(reminder.Message) ? reminder.Message.Truncate(1024) : "…");

                    if (entryPos % 5 == 0 || entryPos == count)
                    {
                        page
                            .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                            .WithAuthor(Context.User.Username, Context.User.GetAvatarUrl())
                            .WithTitle("Reminders")
                            .WithFooter($"Page {pageIndex + 1}/{pageCount} ({"entry".ToQuantity(count)})");

                        paginator.AddPage(page.Build());
                        page = new EmbedBuilder();
                        pageIndex++;
                    }

                    entryPos++;
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                    paginator.AddPage(CreateReminderEmbed(reminders.ElementAt(i), now, i, count));
            }

            await paginator.RunAsync(_interactiveService, Context);
        }

        private Embed CreateReminderEmbed(Reminder reminder, ZonedDateTime now, int? index, int? total)
        {
            var createdAt = Instant.FromDateTimeUtc(reminder.CreatedAt).InZone(now.Zone);
            var endsAt = Instant.FromDateTimeUtc(reminder.EndsAt).InZone(now.Zone);

            var embed = new EmbedBuilder()
                .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                .WithAuthor(Context.User.Username, Context.User.GetAvatarUrl())
                .WithTitle("Reminder")
                .WithDescription($"Ends {now.ToDurationString(endsAt, formatted: true)}.")
                .AddField("Created", DateTimeToClickableString(createdAt, reminder.Url), true)
                .AddField("Ends", endsAt.ToReadableShortString(), true)
                .AddField("Channel", MentionUtils.MentionChannel(reminder.ChannelId), true);

            if (!string.IsNullOrEmpty(reminder.Message))
                embed.AddField("Message", reminder.Message.Truncate(1024));

            if (index != null && total != null)
                embed.WithFooter($"Reminder {index + 1}/{total} (ID: {reminder.Id})");
            else
                embed.WithFooter($"ID: {reminder.Id})");

            return embed.Build();
        }

        private Embed CreateReminderEmbed(Reminder reminder, ZonedDateTime now)
            => CreateReminderEmbed(reminder, now, null, null);

        private static string DateTimeToClickableString(ZonedDateTime date, string url)
            => $"[{date.ToReadableShortString()}]({url})";
    }
}