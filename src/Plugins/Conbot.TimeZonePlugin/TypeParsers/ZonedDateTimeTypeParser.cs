using System.Threading.Tasks;

using Conbot.TimeZonePlugin.Extensions;

using Disqord.Bot;

using Qmmands;

namespace Conbot.TimeZonePlugin;

public class ZonedDateTimeTypeParser : DiscordTypeParser<ZonedDateTimeParseResult>
{
    public override async ValueTask<TypeParserResult<ZonedDateTimeParseResult>> ParseAsync(Parameter parameter,
        string value, DiscordCommandContext context)
    {
        var timeZone = await context.GetUserTimeZoneAsync();

        if (timeZone is null)
            return TypeParserResult<ZonedDateTimeParseResult>.Failed("Time zone hasn't been set.");

        var result = DateTimeUtils.ParseHumanReadableDateTime(value, timeZone);

        return result.IsSuccessful
            ? TypeParserResult<ZonedDateTimeParseResult>.Successful(result)
            : TypeParserResult<ZonedDateTimeParseResult>.Failed(result.Reason);
    }
}