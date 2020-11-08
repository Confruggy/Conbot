using NodaTime;
using Qmmands;

namespace Conbot.TimeZonePlugin
{
    public class ZonedDateTimeParseResult : IResult
    {
        public ZonedDateTime Now { get; }
        public ZonedDateTime? Then { get; }
        public bool IsSuccessful => Then != null;
        public string Reason { get; }
        public string Remainder { get; }

        public ZonedDateTimeParseResult(ZonedDateTime now, ZonedDateTime? then = null, string remainder = null,
            string reason = null)
        {
            Now = now;
            Then = then;
            Remainder = remainder;
            Reason = reason;
        }
    }
}