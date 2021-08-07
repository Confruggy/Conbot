using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;
using Conbot.Interactive;

using Disqord;
using Disqord.Bot;
using Disqord.Rest;

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
    public class TimeZoneModule : ConbotModuleBase
    {
        private readonly TimeZoneContext _db;
        private readonly IDateTimeZoneProvider _provider;

        public TimeZoneModule(TimeZoneContext context, IDateTimeZoneProvider provider)
        {
            _db = context;
            _provider = provider;
        }

        [Command("show", "")]
        [Description("Shows your current time zone.")]
        [RequireTimeZone]
        public async Task<DiscordCommandResult> TimeZoneAsync()
        {
            var userTimeZone = await _db.GetUserTimeZoneAsync(Context.Author);
            var timeZone = _provider.GetZoneOrNull(userTimeZone.TimeZoneId)!;

            string timeZoneText = timeZone.Id.StartsWith("Etc/GMT")
                ? TimeZoneUtils.TzdbGmtMapping[timeZone.Id]
                : timeZone.Id.Replace("/", ", ").Replace('_', ' ');

            var now = SystemClock.Instance.InZone(timeZone).GetCurrentTimeOfDay();

            var text = new StringBuilder()
                .Append("Your time zone is ")
                .Append(Markdown.Bold(timeZoneText))
                .Append(". Your local time is ")
                .Append(Markdown.Bold(now.ToString("H:mm:ss", CultureInfo.InvariantCulture)))
                .Append('.');

            return Reply(text.ToString());
        }

        [Group("set")]
        [Description("Sets your time zone.")]
        public class SetCommands : ConbotModuleBase
        {
            private readonly TimeZoneContext _db;
            private readonly IDateTimeZoneProvider _provider;
            private readonly InteractiveService _interactive;
            private readonly IConfiguration _config;

            public SetCommands(TimeZoneContext context, IDateTimeZoneProvider provider,
                InteractiveService service, IConfiguration config)
            {
                _db = context;
                _provider = provider;
                _interactive = service;
                _config = config;
            }

            [Command("location", "")]
            [Description("Sets your time zone using a time zone location.")]
            [Remarks(
                "Certain commands require your time zone information. " +
                "In order to use those commands it's best to set your time zone using a time zone location. " +
                "However, if you don't want to provide your location, you can set a GMT offset instead. " +
                "Refer to the overload for more information.")]
            [Commands.RequireBotChannelPermissions(
                Permission.AddReactions |
                Permission.SendEmbeds |
                Permission.UseExternalEmojis)]
            public async Task<DiscordCommandResult> TimeZoneAsync(
            [Description("The country to set the time zone from.")]
            [Remarks(
                "Can be either the name of the country or a country code. " +
                "If you can't find your country, please refer to the country names and country codes provided " +
                "[here](https://nodatime.org/TimeZones). If multiple time zones are available for a country, " +
                "then you'll be prompted to select your time zone location from a list.")]
                IList<TzdbZoneLocation> country)
            {
                var location = await SelectZoneLocationAsync(country, _provider);

                if (location is null)
                    return Reply("No time zone has been selected.");

                var timeZone = _provider.GetZoneOrNull(location.ZoneId)!;
                var now = SystemClock.Instance.InZone(timeZone).GetCurrentTimeOfDay();

                await _db.ModifyUserTimeZoneAsync(Context.Author, timeZone);

                var response = new StringBuilder()
                    .Append("Your time zone has been set to ")
                    .Append(Markdown.Bold(timeZone.Id.Replace("/", ", ").Replace('_', ' ')))
                    .Append(". ")
                    .Append("Your local time is ")
                    .Append(Markdown.Bold(now.ToString("H:mm:ss", CultureInfo.InvariantCulture)))
                    .Append('.');

                return Reply(response.ToString()).RunWith(_db.SaveChangesAsync());
            }

            [Command("gmt", "")]
            [Description("Sets your time zone using GMT offsets.")]
            [Remarks(
                "Available offsets are: GMT-12, GMT-11, GMT-10, GMT-9, GMT-8, GMT-7, GMT-6, GMT-5, GMT-4, GMT-3, " +
                "GMT-2, GMT-1, GMT, GMT+1, GMT+2, GMT+3, GMT+4, GMT+5, GMT+6, GMT+7, GMT+8, GMT+9, GMT+10, GMT+11, " +
                "GMT+12, GMT+13 and GMT+14\n\n" +
                "Keep in mind these time zones don't take any day light savings into account.")]
            public async Task<DiscordCommandResult> TimeZoneAsync([Description("The GMT offset.")] DateTimeZone gmt)
            {
                var now = SystemClock.Instance.InZone(gmt).GetCurrentTimeOfDay();
                await _db.ModifyUserTimeZoneAsync(Context.Author, gmt);

                var response = new StringBuilder()
                    .Append("Your time zone has been set to ")
                    .Append(Markdown.Bold(TimeZoneUtils.TzdbGmtMapping[gmt.Id]))
                    .Append(". Your local time is **")
                    .Append(now)
                    .Append("**.");

                return Reply(response.ToString()).RunWith(_db.SaveChangesAsync());
            }

            public async Task<TzdbZoneLocation?> SelectZoneLocationAsync(IList<TzdbZoneLocation> locations,
                IDateTimeZoneProvider provider)
            {
                TzdbZoneLocation? location = null;
                int count = locations.Count;

                if (count == 1)
                {
                    location = locations[0];
                }
                else
                {
                    int padding = count.ToString().Length;
                    var text = new StringBuilder();
                    var now = SystemClock.Instance.GetCurrentInstant();

                    for (int i = 0; i < count; i++)
                    {
                        var timeZone = provider.GetZoneOrNull(locations[i].ZoneId)!;

                        var localTime = now.InZone(timeZone).TimeOfDay;

                        text
                            .Append('`')
                            .Append((i + 1).ToString()
                                .PadLeft(padding))
                            .Append(".` ")
                            .Append(timeZone.Id.Replace("/", ", ").Replace('_', ' '))
                            .Append(" (")
                            .Append(localTime)
                            .AppendLine(")");
                    }

                    var embed = new LocalEmbed()
                        .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
                        .WithTitle("Locations")
                        .WithDescription(text.ToString());

                    int number = 0;

                    var interactiveMessage = new LocalInteractiveMessage()
                        .WithContent("Enter a number to select a location")
                        .AddEmbed(embed)
                        .WithPrecondition(x => x.Id == Context.Author.Id)
                        .AddReactionCallback(_config.GetValue<string>("Emotes:Stop"), x => x
                            .WithCallback((msg, _) => msg.Stop()))
                        .AddMessageCallback(x => x
                            .WithPrecondition((msg, _) =>
                                int.TryParse(msg.Content, out number) && number <= count && number > 0)
                            .WithCallback((msg, _) =>
                            {
                                msg.DeleteAsync();

                                var selectedLocation = locations.ElementAtOrDefault(number - 1);

                                if (selectedLocation is not null)
                                    location = selectedLocation;

                                msg.Stop();
                            }));

                    var message = await _interactive.ExecuteInteractiveMessageAsync(interactiveMessage, Context);

                    _ = message.DeleteAsync();

                    if (location is null)
                        return null;
                }

                return location;
            }
        }
    }
}
