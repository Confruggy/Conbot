using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using NodaTime;
using NodaTime.Text;

namespace Conbot.TimeZonePlugin;

public static class DateTimeUtils
{
    public static ZonedDateTimeParseResult ParseHumanReadableDateTime(string value, DateTimeZone timeZone)
    {
        var now = SystemClock.Instance.GetCurrentInstant().InZone(timeZone);

        string remainder = value;

        var dateRegex = new Regex("^ ?(?:(?:on(?: +the)? +)?(\\d+[\\/.-]\\d+(?:[\\/.-]\\d+)?|\\d+(?:[a-z]{2}|\\.)? +(?:jan(?:\\.|uary)?|feb(?:\\.|ruary)?|mar(?:\\.|ch)?|apr(?:\\.|il)?|may|jun(?:\\.|e)?|jul(?:\\.|y)?|aug(?:\\.|ust)?|sept(?:\\.|ember)?|oct(?:\\.|ober)?|nov(?:\\.|ember)?|dec(?:\\.|ember)?)(?: +(?!\\d+\\:)\\d+)?))|(?:(?:on +)?(?:(?:the +)?(next|last))? +)?(mon(?:\\.|day)?|tue(?:\\.|s(?:\\.|day)?)?|wed(?:\\.|day)?|thu(?:\\.|r(?:\\.|s(?:\\.|day)?)?)?|fri(?:\\.|day)?|sat(?:\\.|day)?|sun(?:\\.|day)?)");
        var timeRegex = new Regex("^ ?(?:(?:at +)?(?<time>\\d+\\:\\d+(?:\\:\\d+)?(?:(?: *[ap]m)?))|(?:at +)?(?<time>\\d+(?: *[ap]m))|(?:at +)(?<time>\\d+))");

        Match dateMatch;
        Match timeMatch;

        if ((dateMatch = dateRegex.Match(remainder.ToLowerInvariant())).Success)
        {
            remainder = remainder[dateMatch.Length..];
            timeMatch = timeRegex.Match(remainder.ToLowerInvariant());

            if (timeMatch.Success)
                remainder = remainder[timeMatch.Length..];

            if (!string.IsNullOrWhiteSpace(remainder))
                return new ZonedDateTimeParseResult(now, reason: "Invalid time provided.");
        }
        else if ((timeMatch = timeRegex.Match(remainder.ToLowerInvariant())).Success)
        {
            remainder = remainder[timeMatch.Length..];
            dateMatch = dateRegex.Match(remainder.ToLowerInvariant());

            if (dateMatch.Success)
                remainder = remainder[dateMatch.Length..];

            if (!string.IsNullOrWhiteSpace(remainder))
                return new ZonedDateTimeParseResult(now, reason: "Invalid time provided.");
        }

        LocalDate? localDate = null;

        if (!dateMatch.Success && !timeMatch.Success)
        {
            var durationRegex = new Regex("^ ?(?:in +)?(?:(-?\\d{1,9}) *y(?:ears?)?)?(?: *(-?\\d{1,9}) *mo(?:nths?)?)?(?: *(-?\\d{1,9}) *w(?:eeks?)?)?(?: *(-?\\d{1,9}) *d(?:ays?)?)?(?: *(-?\\d{1,9}) *h(?:ours?)?)?(?: *(-?\\d{1,9}) *m(?:inutes?)?)?(?: *(-?\\d{1,9}) *s(?:econds?)?)? *$");
            var durationMatch = durationRegex.Match(remainder.ToLowerInvariant());

            if (!durationMatch.Success)
                return new ZonedDateTimeParseResult(now, reason: "Invalid time provided.");

            if (durationMatch.Groups.Values.Skip(1).Sum(x => x.Length) == 0)
                return new ZonedDateTimeParseResult(now, reason: "Invalid duration provided.");

            int years = !string.IsNullOrEmpty(durationMatch.Groups[1].Value)
                ? int.Parse(durationMatch.Groups[1].Value)
                : 0;

            int months = !string.IsNullOrEmpty(durationMatch.Groups[2].Value)
                ? int.Parse(durationMatch.Groups[2].Value)
                : 0;

            int weeks = !string.IsNullOrEmpty(durationMatch.Groups[3].Value)
                ? int.Parse(durationMatch.Groups[3].Value)
                : 0;

            int days = !string.IsNullOrEmpty(durationMatch.Groups[4].Value)
                ? int.Parse(durationMatch.Groups[4].Value)
                : 0;

            int hours = !string.IsNullOrEmpty(durationMatch.Groups[5].Value)
                ? int.Parse(durationMatch.Groups[5].Value)
                : 0;

            int minutes = !string.IsNullOrEmpty(durationMatch.Groups[6].Value)
                ? int.Parse(durationMatch.Groups[6].Value)
                : 0;

            int seconds = !string.IsNullOrEmpty(durationMatch.Groups[7].Value)
                ? int.Parse(durationMatch.Groups[7].Value)
                : 0;

            var date = now.Date
                .PlusYears(years)
                .PlusMonths(months)
                .PlusWeeks(weeks)
                .PlusDays(days);

            var dateTime = date.At(now.TimeOfDay)
                .PlusHours(hours)
                .PlusMinutes(minutes)
                .PlusSeconds(seconds);

            var zonedDateTime = dateTime.InZoneLeniently(timeZone);
            return new ZonedDateTimeParseResult(now, zonedDateTime);
        }

        if (dateMatch.Success)
        {
            if (!string.IsNullOrEmpty(dateMatch.Groups[1].Value))
            {
                string dateText = dateMatch.Groups[1].Value;

                var gbDatePattern = new Regex("^(\\d?(?:[0-9]|1st|2nd|3rd|[04-9]th)) (\\w+)(?: (\\d{2}(?:\\d{2})?))?$");
                Match gbDateMatch;

                if ((gbDateMatch = gbDatePattern.Match(dateText)).Success)
                {
                    string day = gbDateMatch.Groups[1]
                        .Value
                        .Replace("st", "")
                        .Replace("nd", "")
                        .Replace("rd", "")
                        .Replace("th", "");

                    string month = GetMonthFromString(gbDateMatch.Groups[2].Value)!;
                    string year = gbDateMatch.Groups[3].Value;

                    dateText = string.IsNullOrEmpty(year) ? $"{day}/{month}" : $"{day}/{month}/{year}";
                }

                var dayMonthPatterns = new[]
                {
                    LocalDatePattern.Create("d/M", CultureInfo.InvariantCulture),
                    LocalDatePattern.Create("d-M", CultureInfo.InvariantCulture),
                    LocalDatePattern.Create("d.M", CultureInfo.InvariantCulture)
                };

                foreach (var pattern in dayMonthPatterns)
                {
                    var result = pattern.Parse(dateText);

                    if (!result.Success) continue;

                    (_, int month, int day) = result.Value;
                    localDate = new LocalDate(now.Year, month, day);

                    if (localDate < now.Date)
                        localDate = localDate.Value.PlusYears(1);

                    break;
                }

                if (localDate is null)
                {
                    var dayYearPatterns = new[]
                    {
                        LocalDatePattern.Create("d/M/yy", CultureInfo.InvariantCulture),
                        LocalDatePattern.Create("d/M/yyyy", CultureInfo.InvariantCulture),
                        LocalDatePattern.Create("d-M-yy", CultureInfo.InvariantCulture),
                        LocalDatePattern.Create("d-M-yyyy", CultureInfo.InvariantCulture),
                        LocalDatePattern.Create("d.M.yy", CultureInfo.InvariantCulture),
                        LocalDatePattern.Create("d.M.yyyy", CultureInfo.InvariantCulture)
                    };

                    foreach (var pattern in dayYearPatterns)
                    {
                        var result = pattern.Parse(dateText);

                        if (!result.Success)
                            continue;

                        localDate = result.Value;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(dateMatch.Groups[3].Value))
            {
                var weekOfDay = GetWeekOfDayFromString(dateMatch.Groups[3].Value)!.Value;

                localDate = dateMatch.Groups[2].Value.ToLowerInvariant() == "last"
                    ? now.Date.Previous(weekOfDay)
                    : now.Date.Next(weekOfDay);
            }
        }

        LocalTime? localTime = null;

        if (timeMatch.Success)
        {
            string timeText = timeMatch.Groups["time"].Value;

            var patterns = new[]
            {
                LocalTimePattern.Create("h tt", CultureInfo.InvariantCulture),
                LocalTimePattern.Create("htt", CultureInfo.InvariantCulture),
                LocalTimePattern.Create("%H", CultureInfo.InvariantCulture),
                LocalTimePattern.Create("h:mm tt", CultureInfo.InvariantCulture),
                LocalTimePattern.Create("h:mmtt", CultureInfo.InvariantCulture),
                LocalTimePattern.Create("H:mm", CultureInfo.InvariantCulture),
                LocalTimePattern.Create("h:mm:ss tt", CultureInfo.InvariantCulture),
                LocalTimePattern.Create("h:mm:sstt", CultureInfo.InvariantCulture),
                LocalTimePattern.Create("H:mm:ss", CultureInfo.InvariantCulture)
            };

            foreach (var pattern in patterns)
            {
                var result = pattern.Parse(timeText);

                if (!result.Success)
                    continue;

                localTime = result.Value;
                break;
            }
        }

        if (localDate is not null)
        {
            var dateTime = localTime is not null
                ? localDate.Value.At(localTime.Value)
                : localDate.Value.AtMidnight().PlusHours(12);

            var zonedDateTime = dateTime.InZoneLeniently(timeZone);
            return new ZonedDateTimeParseResult(now, zonedDateTime);
        }

        if (localTime is not null)
        {
            var dateTime = new LocalDate(
                    now.Year,
                    now.Month,
                    localTime > now.TimeOfDay ? now.Day : now.Day + 1)
                .At(localTime.Value);

            var zonedDateTime = dateTime.InZoneLeniently(timeZone);
            return new ZonedDateTimeParseResult(now, zonedDateTime);
        }

        return new ZonedDateTimeParseResult(now, reason: "Invalid time entered.");
    }

    private static string? GetMonthFromString(string input)
    {
        return input switch
        {
            "jan" or "jan." or "january" => "1",
            "feb" or "feb." or "february" => "2",
            "mar" or "mar." or "march" => "3",
            "apr" or "apr." or "april" => "4",
            "may" => "5",
            "jun" or "jun." or "june" => "6",
            "jul" or "jul." or "july" => "7",
            "aug" or "aug." or "august" => "8",
            "sep" or "sep." or "sept" or "sept." or "september" => "9",
            "oct" or "oct." or "october" => "10",
            "nov" or "nov." or "november" => "11",
            "dec" or "dec." or "december" => "12",
            _ => null
        };
    }

    private static IsoDayOfWeek? GetWeekOfDayFromString(string input)
    {
        return input switch
        {
            "mon" or "mon." or "monday" => IsoDayOfWeek.Monday,
            "tue" or "tue." or "tues" or "tues." or "tuesday" => IsoDayOfWeek.Tuesday,
            "wed" or "wed." or "wednesday" => IsoDayOfWeek.Wednesday,
            "thu" or "thu." or "thur" or "thur." or "thurs" or "thurs." or "thursday" => IsoDayOfWeek.Thursday,
            "fri" or "fri." or "friday" => IsoDayOfWeek.Friday,
            "sat" or "sat." or "saturday" => IsoDayOfWeek.Saturday,
            "sun" or "sun." or "sunday" => IsoDayOfWeek.Sunday,
            _ => null
        };
    }
}