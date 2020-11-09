using System.Threading.Tasks;
using Conbot.Commands;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Conbot.TimeZonePlugin.Extensions
{
    public static class ContextExtensions
    {
        public static async ValueTask<DateTimeZone> GetUserTimeZoneAsync(this DiscordCommandContext context)
        {
            var db = context.ServiceProvider.GetRequiredService<TimeZoneContext>();
            var provider = context.ServiceProvider.GetRequiredService<IDateTimeZoneProvider>();

            var userTimeZone = await db.GetUserTimeZoneAsync(context.User);

            return userTimeZone != null ? provider.GetZoneOrNull(userTimeZone.TimeZoneId) : null;
        }

        public static async ValueTask<DateTimeZone> GetGuildTimeZoneAsync(this DiscordCommandContext context)
        {
            if (context.Guild == null)
                return null;

            var db = context.ServiceProvider.GetRequiredService<TimeZoneContext>();
            var provider = context.ServiceProvider.GetRequiredService<IDateTimeZoneProvider>();

            var guildTimeZone = await db.GetGuildTimeZoneAsync(context.Guild);

            return guildTimeZone != null ? provider.GetZoneOrNull(guildTimeZone.TimeZoneId) : null;
        }
    }
}