using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord;
using NodaTime;

namespace Conbot.TimeZonePlugin
{
    public static class TimeZoneExtensions
    {
        public static ValueTask<List<UserTimeZone>> GetUserTimeZonesAsync(this TimeZoneContext context)
            => context.UserTimeZones.ToListAsync();

        public static ValueTask<List<GuildTimeZone>> GetGuildTimeZonesAsync(this TimeZoneContext context)
            => context.GuildTimeZones.ToListAsync();

        public static ValueTask<UserTimeZone> GetUserTimeZoneAsync(this TimeZoneContext context, ulong userId)
            => context.UserTimeZones.FirstOrDefaultAsync(x => x.UserId == userId);

        public static ValueTask<UserTimeZone> GetUserTimeZoneAsync(this TimeZoneContext context, IUser user)
            => GetUserTimeZoneAsync(context, user.Id);

        public static ValueTask<GuildTimeZone> GetGuildTimeZoneAsync(this TimeZoneContext context, ulong guildId)
            => context.GuildTimeZones.FirstOrDefaultAsync(x => x.GuildId == guildId);

        public static ValueTask<GuildTimeZone> GetGuildTimeZoneAsync(this TimeZoneContext context, IGuild guild)
            => GetGuildTimeZoneAsync(context, guild.Id);

        public static async ValueTask<UserTimeZone> ModifyUserTimeZoneAsync(this TimeZoneContext context, ulong userId,
            DateTimeZone timeZone)
        {
            var userTimeZone = await context.UserTimeZones.FirstOrDefaultAsync(x => x.UserId == userId);

            if (userTimeZone == null)
            {
                userTimeZone = new UserTimeZone
                {
                    UserId = userId,
                    TimeZoneId = timeZone.Id
                };

                await context.UserTimeZones.AddAsync(userTimeZone);
            }
            else
            {
                userTimeZone.TimeZoneId = timeZone.Id;
            }

            return userTimeZone;
        }

        public static ValueTask<UserTimeZone> ModifyUserTimeZoneAsync(this TimeZoneContext context, IUser user,
            DateTimeZone timeZone)
            => ModifyUserTimeZoneAsync(context, user.Id, timeZone);

        public static async ValueTask<GuildTimeZone> ModifyGuildTimeZoneAsync(this TimeZoneContext context,
            ulong guildId, DateTimeZone timeZone)
        {
            var guildTimeZone = await context.GuildTimeZones.FirstOrDefaultAsync(x => x.TimeZoneId == timeZone.Id);

            if (guildTimeZone == null)
            {
                guildTimeZone = new GuildTimeZone
                {
                    GuildId = guildId,
                    TimeZoneId = timeZone.Id
                };

                await context.GuildTimeZones.AddAsync(guildTimeZone);
            }
            else
            {
                guildTimeZone.TimeZoneId = timeZone.Id;
            }

            return guildTimeZone;
        }

        public static ValueTask<GuildTimeZone> ModifyGuildTimeZoneAsync(this TimeZoneContext context, IGuild guild,
           DateTimeZone timeZone)
           => ModifyGuildTimeZoneAsync(context, guild.Id, timeZone);
    }
}