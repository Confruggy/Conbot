using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot;

using NodaTime;

namespace Conbot.TimeZonePlugin.Extensions;

public static class ContextExtensions
{
    public static async ValueTask<DateTimeZone?> GetUserTimeZoneAsync(this DiscordCommandContext context)
    {
        var db = context.Services.GetRequiredService<TimeZoneContext>();
        var provider = context.Services.GetRequiredService<IDateTimeZoneProvider>();

        var userTimeZone = await db.GetUserTimeZoneAsync(context.Author);

        return userTimeZone is not null ? provider.GetZoneOrNull(userTimeZone.TimeZoneId) : null;
    }

    public static async ValueTask<DateTimeZone?> GetGuildTimeZoneAsync(this DiscordGuildCommandContext context)
    {
        if (context.Guild is null) //TODO can it be null?
            throw new InvalidOperationException("Guild is null");

        var db = context.Services.GetRequiredService<TimeZoneContext>();
        var provider = context.Services.GetRequiredService<IDateTimeZoneProvider>();

        var guildTimeZone = await db.GetGuildTimeZoneAsync(context.Guild);

        return guildTimeZone is not null ? provider.GetZoneOrNull(guildTimeZone.TimeZoneId) : null;
    }
}