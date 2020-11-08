using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.TimeZonePlugin.Extensions;
using Qmmands;

namespace Conbot.TimeZonePlugin
{
    public class ZonedDateTimeTypeParser : TypeParser<ZonedDateTimeParseResult>
    {
        public override async ValueTask<TypeParserResult<ZonedDateTimeParseResult>> ParseAsync(Parameter parameter, string value,
            CommandContext context)
        {
            var discordCommandContext = (DiscordCommandContext)context;
            var timeZone = await discordCommandContext.GetUserTimeZoneAsync();

            if (timeZone == null)
                return TypeParserResult<ZonedDateTimeParseResult>.Unsuccessful("Time zone hasn't been set.");

            var result = DateTimeUtils.ParseHumanReadableDateTime(value, timeZone);

            if (result.IsSuccessful)
                return TypeParserResult<ZonedDateTimeParseResult>.Successful(result);
            
            return TypeParserResult<ZonedDateTimeParseResult>.Unsuccessful(result.Reason);
        }
    }
}