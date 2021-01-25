using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Qmmands;

namespace Conbot.Commands
{
    public class TimeSpanTypeParser : TypeParser<TimeSpan>
    {
        public override ValueTask<TypeParserResult<TimeSpan>> ParseAsync(Parameter parameter, string value,
            CommandContext context)
        {
            var durationRegex = new Regex("^ *(?:for +)?(?: *(-?\\d{1,5}) *w(?:eeks?)?)?(?: *(-?\\d{1,5}) *d(?:ays?)?)?(?: *(-?\\d{1,5}) *h(?:ours?)?)?(?: *(-?\\d{1,5}) *m(?:inutes?)?)?(?: *(-?\\d{1,5}) *s(?:econds?)?)? *$");
            var durationMatch = durationRegex.Match(value.ToLowerInvariant());

            if (!durationMatch.Success || durationMatch.Groups.Values.Skip(1).Sum(x => x.Length) == 0)
                return TypeParserResult<TimeSpan>.Unsuccessful("Enter a valid duration.");

            int weeks = !string.IsNullOrEmpty(durationMatch.Groups[1].Value)
                ? int.Parse(durationMatch.Groups[1].Value)
                : 0;

            int days = !string.IsNullOrEmpty(durationMatch.Groups[2].Value)
                ? int.Parse(durationMatch.Groups[2].Value)
                : 0;

            int hours = !string.IsNullOrEmpty(durationMatch.Groups[3].Value)
                ? int.Parse(durationMatch.Groups[3].Value)
                : 0;

            int minutes = !string.IsNullOrEmpty(durationMatch.Groups[4].Value)
                ? int.Parse(durationMatch.Groups[4].Value)
                : 0;

            int seconds = !string.IsNullOrEmpty(durationMatch.Groups[5].Value)
                ? int.Parse(durationMatch.Groups[5].Value)
                : 0;

            var timeSpan = new TimeSpan((weeks * 7) + days, hours, minutes, seconds);
            return TypeParserResult<TimeSpan>.Successful(timeSpan);
        }
    }
}
