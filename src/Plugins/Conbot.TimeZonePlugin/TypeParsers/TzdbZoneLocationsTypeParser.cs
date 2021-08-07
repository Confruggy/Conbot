using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NodaTime.TimeZones;

using Qmmands;

namespace Conbot.TimeZonePlugin
{
    public class TzdbZoneLocationsTypeParser : TypeParser<IList<TzdbZoneLocation>>
    {
        public override ValueTask<TypeParserResult<IList<TzdbZoneLocation>>> ParseAsync(Parameter parameter,
            string value, CommandContext context)
        {
            var source = TzdbDateTimeZoneSource.Default;

            var locations = TzdbDateTimeZoneSource.Default.ZoneLocations!.Where(x =>
                (string.Equals(x.CountryName, value, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(x.CountryCode, value, StringComparison.InvariantCultureIgnoreCase)) &&
                    source.CanonicalIdMap[x.ZoneId] == x.ZoneId);

            if (!locations.Any())
                return TypeParserResult<IList<TzdbZoneLocation>>.Failed("Country hasn't been found.");

            return TypeParserResult<IList<TzdbZoneLocation>>.Successful(locations.ToList());
        }
    }
}