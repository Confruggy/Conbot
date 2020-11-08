using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.InteractiveMessages;
using Conbot.Services.Interactive;
using Discord;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.TimeZones;
using Qmmands;

namespace Conbot.TimeZonePlugin
{
    [Name("Time Zones")]
    [Group("timezone")]
    [Description("Time zone related commands.")]
    [Remarks(
        "Time zones are based on the IANA Time Zone Database. A detailed list can be found " +
        "[here](https://nodatime.org/TimeZones).")]
    public class TimeZoneModule : DiscordModuleBase
    {
        private readonly TimeZoneContext _db;
        private readonly IDateTimeZoneProvider _provider;

        public TimeZoneModule(TimeZoneContext context, IDateTimeZoneProvider provider)
        {
            _db = context;
            _provider = provider;
        }

        [Command]
        [Description("Shows your current time zone.")]
        [RequireTimeZone]
        public async Task TimeZoneAsync()
        {
            var userTimeZone = await _db.GetUserTimeZoneAsync(Context.User);
            var timeZone = _provider.GetZoneOrNull(userTimeZone.TimeZoneId);

            string timeZoneText = timeZone.Id.StartsWith("Etc/GMT")
                ? TimeZoneUtils.TzdbGmtMapping[timeZone.Id]
                : timeZone.Id.Replace("/", ", ").Replace('_', ' ');

            var now = SystemClock.Instance.InZone(timeZone).GetCurrentTimeOfDay();

            var text = new StringBuilder()
                .Append($"Your time zone is **{timeZoneText}**. ")
                .Append($"Your local time is **{now}**.");

            await ReplyAsync(text.ToString());
        }

        [Group("set")]
        [Description("Sets your time zone.")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.AddReactions)]
        public class SetCommands : DiscordModuleBase
        {
            private readonly TimeZoneContext _db;
            private readonly IDateTimeZoneProvider _provider;
            private readonly InteractiveService _interactive;

            public SetCommands(TimeZoneContext context, IDateTimeZoneProvider provider, InteractiveService service)
            {
                _db = context;
                _provider = provider;
                _interactive = service;
            }

            [Command]
            [Description("Sets your time zone location.")]
            [Remarks(
                "Certain commands require your time zone information. " +
                "In order to use those commands it's best to set your time zone location. " +
                "However, if you don't want to provide your location, you can set a GMT offset instead. " +
                "Refer to the overload for more information.")]
            public async Task TimeZoneAsync(
            [Description("The country to set the time zone from.")]
            [Remarks(
                "Can be either the name of the country or a country code. " + 
                "If you can't find your country, please refer to the country names and country codes provided " +
                "[here](https://nodatime.org/TimeZones). If multiple time zones are available for a country, " +
                "then you'll be prompted to select your time zone location from a list.")]
                IList<TzdbZoneLocation> country)
            {
                var location = await SelectZoneLocationAsync(Context, country, _provider, _interactive);

                if (location == null)
                {
                    await ReplyAsync("No time zone has been selected.");
                    return;
                }

                var timeZone = _provider.GetZoneOrNull(location.ZoneId);
                var now = SystemClock.Instance.InZone(timeZone).GetCurrentTimeOfDay();

                await _db.ModifyUserTimeZoneAsync(Context.User, timeZone);

                var response = new StringBuilder()
                    .Append($"Your time zone has been set to **{timeZone.Id.Replace("/", ", ").Replace('_', ' ')}**. ")
                    .Append($"Your local time is **{now}**.");

                await Task.WhenAll(
                    ReplyAsync(response.ToString()),
                    _db.SaveChangesAsync());
            }

            [Command]
            [Description("Sets your time zone using UTC/GMT offsets.")]
            [Remarks(
                "Available offsets are: GMT-12, GMT-11, GMT-10, GMT-9, GMT-8, GMT-7, GMT-6, GMT-5, GMT-4, GMT-3, " +
                "GMT-2, GMT-1, GMT, GMT+1, GMT+2, GMT+3, GMT+4, GMT+5, GMT+6, GMT+7, GMT+8, GMT+9, GMT+10, GMT+11, " +
                "GMT+12, GMT+13 and GMT+14\n\n" +
                "Keep in mind these time zones don't take any day light savings into account.")]
            public async Task TimeZoneAsync([Description("The GMT offset.")] DateTimeZone gmt)
            {
                var now = SystemClock.Instance.InZone(gmt).GetCurrentTimeOfDay();
                await _db.ModifyUserTimeZoneAsync(Context.User, gmt);

                var response = new StringBuilder()
                    .Append($"Your time zone has been set to **{TimeZoneUtils.TzdbGmtMapping[gmt.Id]}**. ")
                    .Append($"Your local time is **{now}**.");

                await Task.WhenAll(
                    ReplyAsync(response.ToString()),
                    _db.SaveChangesAsync());
            }
        }

        public static async Task<TzdbZoneLocation> SelectZoneLocationAsync(
            DiscordCommandContext context, IList<TzdbZoneLocation> locations, IDateTimeZoneProvider provider,
            InteractiveService interactiveService)
        {
            TzdbZoneLocation location = null;
            int count = locations.Count;

            List<Task> tasks = new List<Task>();

            if (count == 1)
                location = locations.First();
            else
            {
                int padding = count.ToString().Length;
                var text = new StringBuilder();
                var now = SystemClock.Instance.GetCurrentInstant();

                for (int i = 0; i < count; i++)
                {
                    var timeZone = provider.GetZoneOrNull(locations[i].ZoneId);

                    var localTime = now.InZone(timeZone).TimeOfDay;

                    text.AppendLine(
                        $"`{(i + 1).ToString().PadLeft(padding)}.` {timeZone.Id.Replace("/", ", ").Replace('_', ' ')} ({localTime})");
                }

                var embed = new EmbedBuilder()
                    .WithColor(Constants.DefaultEmbedColor)
                    .WithTitle("Locations")
                    .WithDescription(text.ToString())
                    .Build();

                var message =
                    await context.Channel.SendMessageAsync("Enter a number to select a location.", embed: embed);

                int number = 0;

                var interactiveMessage = new InteractiveMessageBuilder()
                    .WithPrecondition(x => x.Id == context.User.Id)
                    .AddReactionCallback(x => x
                        .WithEmote("stop:654781462385655849")
                        .ShouldResumeAfterExecution(false))
                        .AddMessageCallback(x => x
                            .WithPrecondition(msg => int.TryParse(msg.Content, out number) &&
                                number <= count && number > 0)
                            .WithCallback(msg =>
                            {
                                _ = msg.DeleteAsync();

                                var selectedLocation = locations.ElementAtOrDefault(number - 1);

                                if (selectedLocation != null)
                                {
                                    location = selectedLocation;
                                    return;
                                }
                                return;
                            })
                            .ShouldResumeAfterExecution(false))
                    .Build();

                await interactiveService.ExecuteInteractiveMessageAsync(interactiveMessage, message, context.User);

                _ = message.DeleteAsync();

                if (location == null)
                    return null;
            }

            return location;
        }
    }
}