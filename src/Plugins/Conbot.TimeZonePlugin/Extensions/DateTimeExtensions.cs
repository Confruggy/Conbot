using System.Text;
using NodaTime;
using NodaTime.Extensions;

namespace Conbot.TimeZonePlugin.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToReadableShortString(this ZonedDateTime dateTime)
        {
            var now = SystemClock.Instance.InZone(dateTime.Zone).GetCurrentLocalDateTime();

            var text = new StringBuilder();

            if (dateTime.Date == now.Date)
                text.Append("Today");
            else if (dateTime.Date == now.Date.PlusDays(1))
                text.Append("Tomorrow");
            else if (dateTime.Date == now.Date.PlusDays(-1))
                text.Append("Yesterday");
            else text.Append($"{dateTime.Date:d}");

            text.Append(" at ")
                .Append($"{dateTime.TimeOfDay:t}");

            return text.ToString();
        }
    }
}