using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Disqord;

using Humanizer;

using NodaTime;
using NodaTime.Extensions;

namespace Conbot.TimeZonePlugin.Extensions;

public static class DateTimeExtensions
{
    public static string ToReadableShortString(this ZonedDateTime dateTime, bool? showSeconds = null)
    {
        var (date, _) = SystemClock.Instance.InZone(dateTime.Zone).GetCurrentLocalDateTime();

        var text = new StringBuilder();

        if (dateTime.Date == date)
            text.Append("Today");
        else if (dateTime.Date == date.PlusDays(1))
            text.Append("Tomorrow");
        else if (dateTime.Date == date.PlusDays(-1))
            text.Append("Yesterday");
        else
            text.Append(dateTime.Date.ToString("d MMM yyyy", CultureInfo.InvariantCulture));

        text.Append(" at ");

        var time = dateTime.TimeOfDay;
        if (showSeconds == true || (showSeconds is null && time.Second != 0))
            text.Append(time.ToString("T", CultureInfo.InvariantCulture));
        else
            text.Append(time.ToString("t", CultureInfo.InvariantCulture));

        return text.ToString();
    }

    public static string ToDurationString(this ZonedDateTime now, ZonedDateTime then,
        DurationLevel startLevel = DurationLevel.Seconds, int accuracy = 0,
        Duration? showDateAt = null, bool formatted = false)
    {
        var difference = then - now;
        var text = new StringBuilder();

        if (Math.Abs(difference.TotalTicks) >= showDateAt?.TotalTicks)
        {
            if (now.Date == then.Date)
            {
                text.Append(formatted ? Markdown.Bold("today") : "today");
            }
            else if (now.Date.PlusDays(1) == then.Date)
            {
                text.Append(formatted ? Markdown.Bold("tomorrow") : "tomorrow");
            }
            else if (now.Date.PlusDays(-1) == then.Date)
            {
                text.Append(formatted ? Markdown.Bold("yesterday") : "yesterday");
            }
            else
            {
                text
                    .Append("on the ")
                    .Append(then.ToString(formatted ? Markdown.Bold("d MMM yyyy") : "d MMM yyyy",
                        CultureInfo.InvariantCulture));
            }

            if (startLevel >= DurationLevel.Days)
                return text.ToString();

            text.Append(" at ");

            if (startLevel >= DurationLevel.Minutes || then.Second == 0)
            {
                text.Append(then.ToString(
                    formatted ? Markdown.Bold("H:mm") : "H:mm", CultureInfo.InvariantCulture));
            }
            else
            {
                text.Append(then.ToString(
                    formatted ? Markdown.Bold("H:mm:ss") : "H:mm:ss", CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }

        int milliseconds = startLevel == DurationLevel.Milliseconds ? Math.Abs(difference.Milliseconds) : 0;
        int seconds = startLevel <= DurationLevel.Seconds ? Math.Abs(difference.Seconds) : 0;
        int minutes = startLevel <= DurationLevel.Minutes ? Math.Abs(difference.Minutes) : 0;
        int hours = startLevel <= DurationLevel.Hours ? Math.Abs(difference.Hours) : 0;
        int days = startLevel <= DurationLevel.Days ? Math.Abs(difference.Days) % 7 : 0;
        int weeks = (difference.Days - days) / 7;

        if (difference.TotalTicks > 0)
            text.Append("in ");

        text.Append(
            CreateDurationString(weeks, days, hours, minutes, seconds, milliseconds, accuracy, formatted));

        if (difference.TotalTicks < 0)
            text.Append(" ago");

        return text.ToString();
    }

    private static string CreateDurationString(int weeks, int days, int hours, int minutes, int seconds,
        int milliseconds, int accuracy = 0, bool formatted = false)
    {
        var strings = new List<string>();

        if ((accuracy == 0 || accuracy > strings.Count) && weeks > 0)
            strings.Add("week".ToQuantity(weeks, formatted ? Markdown.Bold("#") : "#"));

        if ((accuracy == 0 || accuracy > strings.Count) && days > 0)
            strings.Add("day".ToQuantity(days, formatted ? Markdown.Bold("#") : "#"));

        if ((accuracy == 0 || accuracy > strings.Count) && hours > 0)
            strings.Add("hour".ToQuantity(hours, formatted ? Markdown.Bold("#") : "#"));

        if ((accuracy == 0 || accuracy > strings.Count) && minutes > 0)
            strings.Add("minute".ToQuantity(minutes, formatted ? Markdown.Bold("#") : "#"));

        if ((accuracy == 0 || accuracy > strings.Count) && seconds > 0)
            strings.Add("second".ToQuantity(seconds, formatted ? Markdown.Bold("#") : "#"));

        if ((accuracy == 0 || accuracy > strings.Count) && milliseconds > 0)
            strings.Add("milliseconds".ToQuantity(weeks, formatted ? Markdown.Bold("#") : "#"));

        return strings.Count switch
        {
            0 => formatted ? Markdown.Bold("now") : "now",
            1 => strings[0],
            _ => $"{string.Join(", ", strings.Take(strings.Count - 1))} and {strings[^1]}"
        };
    }
}