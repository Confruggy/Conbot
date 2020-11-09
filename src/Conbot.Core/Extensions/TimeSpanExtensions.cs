using System;
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace Conbot.Extensions
{
    public static class TimeSpanExtension
    {
        public static string ToRoundedString(this TimeSpan timeSpan) => ToApproximateString(timeSpan, false);

        public static string ToApproximateFormattedString(this TimeSpan timeSpan) => ToApproximateString(timeSpan, true);

        private static string ToApproximateString(this TimeSpan timeSpan, bool formatted = false)
        {
            if (timeSpan.TotalDays >= 1)
            {
                if ((int)timeSpan.TotalDays == 1)
                    return $"{(formatted ? Format.Bold("1") : "1")} day";
                else return $"{(formatted ? Format.Bold(((int)timeSpan.TotalDays).ToString()) : ((int)timeSpan.TotalDays).ToString())} days";
            }
            if (timeSpan.TotalHours >= 1)
            {
                if ((int)timeSpan.TotalHours == 1)
                    return $"{(formatted ? Format.Bold("1") : "1")} hour";
                else return $"{(formatted ? Format.Bold(((int)timeSpan.TotalHours).ToString()) : ((int)timeSpan.TotalHours).ToString())} hours";
            }
            if (timeSpan.TotalMinutes >= 1)
            {
                if ((int)timeSpan.TotalMinutes == 1)
                    return $"{(formatted ? Format.Bold("1") : "1")} minute";
                else return $"{(formatted ? Format.Bold(((int)timeSpan.TotalMinutes).ToString()) : ((int)timeSpan.TotalMinutes).ToString())} minutes";
            }
            if ((int)timeSpan.TotalSeconds == 1)
                return $"{(formatted ? Format.Bold("1") : "1")} second";
            return $"{(formatted ? Format.Bold(((int)timeSpan.TotalSeconds).ToString()) : ((int)timeSpan.TotalSeconds).ToString())} seconds";
        }

        public static string ToLongString(this TimeSpan timeSpan) => ToLongString(timeSpan, false);

        public static string ToLongFormattedString(this TimeSpan timeSpan) => ToLongString(timeSpan, true);

        private static string ToLongString(TimeSpan timeSpan, bool formatted = false)
        {
            var strings = new List<string>();

            int weeks = timeSpan.Days / 7;
            int days = timeSpan.Days - (weeks * 7);

            if (weeks == 1)
                strings.Add($"{(formatted ? Format.Bold("1") : "1")} week");
            else if (weeks >= 1)
                strings.Add($"{(formatted ? Format.Bold(weeks.ToString()) : weeks.ToString())} weeks");
            if (days == 1)
                strings.Add($"{(formatted ? Format.Bold("1") : "1")} day");
            else if (days >= 1)
                strings.Add($"{(formatted ? Format.Bold(days.ToString()) : days.ToString())} days");

            if (timeSpan.Hours == 1)
                strings.Add($"{(formatted ? Format.Bold("1") : "1")} hour");
            else if (timeSpan.Hours >= 1)
                strings.Add($"{(formatted ? Format.Bold(timeSpan.Hours.ToString()) : timeSpan.Hours.ToString())} hours");

            if (timeSpan.Minutes == 1)
                strings.Add($"{(formatted ? Format.Bold("1") : "1")} minute");
            else if (timeSpan.Minutes >= 1)
                strings.Add($"{(formatted ? Format.Bold(timeSpan.Minutes.ToString()) : timeSpan.Minutes.ToString())} minutes");

            if (timeSpan.Seconds == 1)
                strings.Add($"{(formatted ? Format.Bold("1") : "1")} second");
            else if (timeSpan.Seconds >= 1 || strings.Count == 0)
                strings.Add($"{(formatted ? Format.Bold(timeSpan.Seconds.ToString()) : timeSpan.Seconds.ToString())} seconds");

            if (strings.Count == 1)
                return strings[0];

            return $"{string.Join(", ", strings.Take(strings.Count - 1))} and {strings[^1]}";
        }
    }
}
