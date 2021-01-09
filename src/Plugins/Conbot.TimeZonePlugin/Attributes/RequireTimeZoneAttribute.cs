using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Conbot.Commands;

using NodaTime;

using Qmmands;

namespace Conbot.TimeZonePlugin
{
    public class RequireTimeZoneAttribute : CheckAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(CommandContext context)
        {
            var db = context.ServiceProvider.GetRequiredService<TimeZoneContext>();
            var discordCommandContext = (DiscordCommandContext)context;
            var userTimeZone = await db.GetUserTimeZoneAsync(discordCommandContext.User);

            if (userTimeZone == null)
            {
                return CheckResult.Unsuccessful(
                    "This command requires a time zone to be set. Use the `timezone set` command to set a time zone.");
            }

            var provider = context.ServiceProvider.GetRequiredService<IDateTimeZoneProvider>();
            var timeZone = provider.GetZoneOrNull(userTimeZone.TimeZoneId);

            if (timeZone == null)
                return CheckResult.Unsuccessful("Your time zone has been removed. Please set a new one.");

            return CheckResult.Successful;
        }
    }
}