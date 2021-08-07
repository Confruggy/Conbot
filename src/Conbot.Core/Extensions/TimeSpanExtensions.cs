using System;
using System.Collections.Generic;
using System.Linq;

namespace Conbot.Extensions
{
    public static class TimeSpanExtension
    {
        public static string ToRoundedString(this TimeSpan timeSpan) => ToApproximateString(timeSpan, false);

        public static string ToApproximateFormattedString(this TimeSpan timeSpan)
            => ToApproximateString(timeSpan, true);

        private static string ToApproximateString(this TimeSpan timeSpan, bool formatted = false)
        {
            if (timeSpan.TotalDays >= 1)
            {
                if ((int)timeSpan.TotalDays == 1)
                    return $"{(formatted ? "**1**" : "1")} day";
                else
                    return $"{(formatted ? $"**{(int)timeSpan.TotalDays}**" : ((int)timeSpan.TotalDays).ToString())} days";
            }

            if (timeSpan.TotalHours >= 1)
            {
                if ((int)timeSpan.TotalHours == 1)
                    return $"{(formatted ? "**1**" : "1")} hour";
                else
                    return $"{(formatted ? $"**{(int)timeSpan.TotalHours}**" : ((int)timeSpan.TotalHours).ToString())} hours";
            }

            if (timeSpan.TotalMinutes >= 1)
            {
                if ((int)timeSpan.TotalMinutes == 1)
                    return $"{(formatted ? "**1**" : "1")} minute";
                else
                    return $"{(formatted ? $"**{(int)timeSpan.TotalMinutes}**" : ((int)timeSpan.TotalMinutes).ToString())} minutes";
            }

            if ((int)timeSpan.TotalSeconds == 1)
                return $"{(formatted ? "**1**" : "1")} second";

            return $"{(formatted ? $"**{(int)timeSpan.TotalSeconds}**" : ((int)timeSpan.TotalSeconds).ToString())} seconds";
        }

        public static string ToLongFormattedString(this TimeSpan timeSpan) => ToLongStringInternal(timeSpan, true);

        public static string ToLongString(this TimeSpan timeSpan) => ToLongStringInternal(timeSpan, false);

        private static string ToLongStringInternal(TimeSpan timeSpan, bool formatted)
        {
            var strings = new List<string>();

            int weeks = timeSpan.Days / 7;
            int days = timeSpan.Days - (weeks * 7);

            if (weeks == 1)
                strings.Add($"{(formatted ? "**1**" : "1")} week");
            else if (weeks >= 1)
                strings.Add($"{(formatted ? $"**{weeks}**" : weeks.ToString())} weeks");

            if (days == 1)
                strings.Add($"{(formatted ? "**1**" : "1")} day");
            else if (days >= 1)
                strings.Add($"{(formatted ? $"**{days}**" : days.ToString())} days");

            if (timeSpan.Hours == 1)
                strings.Add($"{(formatted ? "**1**" : "1")} hour");
            else if (timeSpan.Hours >= 1)
                strings.Add($"{(formatted ? $"**{timeSpan.Hours}**" : timeSpan.Hours.ToString())} hours");

            if (timeSpan.Minutes == 1)
                strings.Add($"{(formatted ? "**1**" : "1")} minute");
            else if (timeSpan.Minutes >= 1)
                strings.Add($"{(formatted ? $"**{timeSpan.Minutes}**" : timeSpan.Minutes.ToString())} minutes");

            if (timeSpan.Seconds == 1)
                strings.Add($"{(formatted ? "**1**" : "1")} second");
            else if (timeSpan.Seconds >= 1 || strings.Count == 0)
                strings.Add($"{(formatted ? $"**{timeSpan.Seconds}**" : timeSpan.Seconds.ToString())} seconds");

            if (strings.Count == 1)
                return strings[0];

            return $"{string.Join(", ", strings.Take(strings.Count - 1))} and {strings[^1]}";
        }
    }
}
