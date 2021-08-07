using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot;

using NodaTime;

using Qmmands;

namespace Conbot.TimeZonePlugin
{
    public class RequireTimeZoneAttribute : DiscordCheckAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(DiscordCommandContext context)
        {
            var db = context.Services.GetRequiredService<TimeZoneContext>();
            var userTimeZone = await db.GetUserTimeZoneAsync(context.Author);

            if (userTimeZone is null)
            {
                return CheckResult.Failed(
                    "This command requires a time zone to be set. Use the `timezone set` command to set a time zone.");
            }

            var provider = context.Services.GetRequiredService<IDateTimeZoneProvider>();
            var timeZone = provider.GetZoneOrNull(userTimeZone.TimeZoneId);

            if (timeZone is null)
                return CheckResult.Failed("Your time zone has been removed. Please set a new one.");

            return CheckResult.Successful;
        }
    }
}
