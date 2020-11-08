
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NodaTime;
using NodaTime.Text;

namespace Conbot.TimeZonePlugin
{
    public static class DateTimeUtils
    {
        public static ZonedDateTimeParseResult ParseHumanReadableDateTime(string value, DateTimeZone timeZone)
        {

            var now = SystemClock.Instance.GetCurrentInstant().InZone(timeZone);

            var remainder = value;

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
            }
            else if ((timeMatch = timeRegex.Match(remainder.ToLowerInvariant())).Success)
            {
                remainder = remainder[timeMatch.Length..];
                dateMatch = dateRegex.Match(remainder.ToLowerInvariant());

                if (dateMatch.Success)
                    remainder = remainder[dateMatch.Length..];
            }
            LocalDate? localDate = null;

            if (!dateMatch.Success && !timeMatch.Success)
            {
                var durationRegex = new Regex("^ ?(?:in +)?(?:(-?\\d{1,9}) *y(?:ears?)?)?(?: *(-?\\d{1,9}) *mo(?:nths?)?)?(?: *(-?\\d{1,9}) *w(?:eeks?)?)?(?: *(-?\\d{1,9}) *d(?:ays?)?)?(?: *(-?\\d{1,9}) *h(?:ours?)?)?(?: *(-?\\d{1,9}) *m(?:inutes?)?)?(?: *(-?\\d{1,9}) *s(?:econds?)?)?");
                var durationMatch = durationRegex.Match(remainder.ToLowerInvariant());

                if (!durationMatch.Success)
                    return new ZonedDateTimeParseResult(now, reason: "Invalid time provided.");

                if (durationMatch.Groups.Values.Skip(1).Sum(x => x.Length) == 0)
                    return new ZonedDateTimeParseResult(now, reason: "Invalid duration provided.");

                remainder =  remainder[durationMatch.Length..];

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
                return new ZonedDateTimeParseResult(now, zonedDateTime, remainder.Length > 0 ? remainder : null);
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
                        string day = gbDateMatch.Groups[1].Value
                            .Replace("st", "")
                            .Replace("nd", "")
                            .Replace("rd", "");

                        string month = GetMonthFromString(gbDateMatch.Groups[2].Value);
                        string year = gbDateMatch.Groups[3].Value;

                        if (string.IsNullOrEmpty(year))
                            dateText = $"{day}/{month}";
                        else dateText = $"{day}/{month}/{year}";
                    }

                    var dayMonthPatterns = new[] {
                        LocalDatePattern.Create("d/M", CultureInfo.InvariantCulture),
                        LocalDatePattern.Create("d-M", CultureInfo.InvariantCulture),
                        LocalDatePattern.Create("d.M", CultureInfo.InvariantCulture)
                    };

                    foreach (var pattern in dayMonthPatterns)
                    {
                        var result = pattern.Parse(dateText);

                        if (result.Success)
                        {
                            var date = result.Value;
                            localDate = new LocalDate(now.Year, date.Month, date.Day);

                            if (localDate < now.Date)
                                localDate = localDate.Value.PlusYears(1);

                            break;
                        }
                    }

                    if (localDate == null)
                    {
                        var dayYearPatterns = new[] {
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

                            if (result.Success)
                            {
                                localDate = result.Value;
                                break;
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(dateMatch.Groups[3].Value))
                {
                    var weekOfDay = GetWeekOfDayFromString(dateMatch.Groups[3].Value).Value;

                    if (dateMatch.Groups[2].Value?.ToLowerInvariant() == "last")
                        localDate = now.Date.Previous(weekOfDay);
                    else localDate = now.Date.Next(weekOfDay);
                }
            }

            LocalTime? localTime = null;

            if (timeMatch.Success)
            {
                string timeText = timeMatch.Groups["time"].Value;

                var patterns = new[] {
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

                    if (result.Success)
                    {
                        localTime = result.Value;
                        break;
                    }
                }
            }

            if (localDate != null)
            {
                LocalDateTime dateTime;

                if (localTime != null)
                    dateTime = localDate.Value.At(localTime.Value);
                else
                    dateTime = localDate.Value.AtMidnight().PlusHours(12);

                var zonedDateTime = dateTime.InZoneLeniently(timeZone);
                return new ZonedDateTimeParseResult(now, zonedDateTime, remainder.Length > 0 ? remainder : null);
            }

            if (localTime != null)
            {
                var dateTime = new LocalDate(
                    now.Year,
                    now.Month,
                    localTime > now.TimeOfDay ? now.Day : now.Day + 1)
                    .At(localTime.Value);

                var zonedDateTime = dateTime.InZoneLeniently(timeZone);
                return new ZonedDateTimeParseResult(now, zonedDateTime, remainder.Length > 0 ? remainder : null);
            }

            return new ZonedDateTimeParseResult(now, reason: "Invalid time entered.");
        }

        private static string GetMonthFromString(string input)
        {
            switch (input)
            {
                case "jan":
                case "jan.":
                case "january": return "1";

                case "feb":
                case "feb.":
                case "february": return "2";

                case "mar":
                case "mar.":
                case "march": return "3";

                case "apr":
                case "apr.":
                case "april": return "4";

                case "may": return "5";

                case "jun":
                case "jun.":
                case "june": return "6";

                case "jul":
                case "jul.":
                case "july": return "7";

                case "aug":
                case "aug.":
                case "august": return "8";

                case "sep":
                case "sep.":
                case "sept":
                case "sept.":
                case "september": return "9";

                case "oct":
                case "oct.":
                case "october": return "10";

                case "nov":
                case "nov.":
                case "november": return "11";

                case "dec":
                case "dec.":
                case "december": return "12";

                default: return null;
            }
        }

        private static IsoDayOfWeek? GetWeekOfDayFromString(string input)
        {
            switch (input)
            {
                case "mon":
                case "mon.":
                case "monday": return IsoDayOfWeek.Monday;

                case "tue":
                case "tue.":
                case "tues":
                case "tues.":
                case "tuesday": return IsoDayOfWeek.Tuesday;

                case "wed":
                case "wed.":
                case "wednesday": return IsoDayOfWeek.Wednesday;

                case "thu":
                case "thu.":
                case "thur":
                case "thur.":
                case "thurs":
                case "thurs.":
                case "thursday": return IsoDayOfWeek.Thursday;

                case "fri":
                case "fri.":
                case "friday": return IsoDayOfWeek.Friday;

                case "sat":
                case "sat.":
                case "saturday": return IsoDayOfWeek.Saturday;

                case "sun":
                case "sun.":
                case "sunday": return IsoDayOfWeek.Sunday;

                default: return null;
            }
        }
    }
}