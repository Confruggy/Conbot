using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using NodaTime;

using Qmmands;

namespace Conbot.TimeZonePlugin
{
    public class GmtTimeZoneTypeParser : TypeParser<DateTimeZone>
    {
        public override ValueTask<TypeParserResult<DateTimeZone>> ParseAsync(
            Parameter parameter, string value, CommandContext context)
        {
            var mapping = TimeZoneUtils.GmtTzdbMapping;

            if (!mapping.TryGetValue(value.ToUpper(), out string? zoneId))
                return TypeParserResult<DateTimeZone>.Failed("Invalid GMT offset.");

            var provider = context.Services.GetRequiredService<IDateTimeZoneProvider>();
            var timeZone = provider.GetZoneOrNull(zoneId)!;

            return TypeParserResult<DateTimeZone>.Successful(timeZone);
        }
    }
}