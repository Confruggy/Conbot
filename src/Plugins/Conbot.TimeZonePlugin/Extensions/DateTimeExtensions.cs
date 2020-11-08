using System;
using System.Globalization;
using System.Text;
using Humanizer;
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

        public static string ToDurationFormattedString(this ZonedDateTime now, ZonedDateTime then)
        {
            if (now == then)
                return "**now**";

            var text = new StringBuilder();

            var difference = then - now;

            if (difference.TotalHours < 1 && difference.TotalHours > -1)
            {
                if (difference.TotalTicks > 0)
                    text.Append("in ");

                if (difference.Minutes > 0 || difference.Minutes < 0)
                    text.Append("minute".ToQuantity(Math.Abs(difference.Minutes), "**#**"));

                if ((difference.Minutes > 0 && difference.Seconds > 0) ||
                    (difference.Minutes < 0 && difference.Seconds < 0))
                    text.Append(" and ");

                if (difference.Seconds > 0 || difference.Seconds < 0)
                    text.Append("second".ToQuantity(Math.Abs(difference.Seconds), "**#**"));

                if (difference.TotalTicks < 0)
                    text.Append(" ago");
            }
            else
            {
                if (now.Date == then.Date)
                    text.Append("**today** ");
                else if (now.Date.PlusDays(1) == then.Date)
                    text.Append("**tomorrow** ");
                else if (now.Date.PlusDays(-1) == then.Date)
                    text.Append("**yesterday** ");
                else
                {
                    text.Append("on the **")
                        .Append(then.ToString("d MMM yyyy", CultureInfo.InvariantCulture))
                        .Append("** ");
                }

                text.Append("at **");

                if (then.Second == 0)
                    text.Append(then.ToString("H:mm", CultureInfo.InvariantCulture));
                else text.Append(then.ToString("H:mm:ss", CultureInfo.InvariantCulture));

                text.Append("**");
            }

            return text.ToString();
        }
    }
}