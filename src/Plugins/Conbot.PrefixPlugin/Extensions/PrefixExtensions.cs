using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Disqord;

namespace Conbot.PrefixPlugin
{
    public static class PrefixExtensions
    {
        public static Task<List<Prefix>> GetPrefixesAsync(this PrefixContext context, ulong? guildId = null)
        {
            if (guildId is null)
                return context.Prefixes.AsQueryable().ToListAsync();

            return context.Prefixes.AsQueryable().Where(x => x.GuildId == guildId).ToListAsync();
        }

        public static Task<List<Prefix>> GetPrefixesAsync(this PrefixContext context, IGuild guild)
            => GetPrefixesAsync(context, guild.Id);

        public static Task<Prefix> GetPrefixAsync(this PrefixContext context, ulong guildId, string text)
            => context.Prefixes.AsQueryable().FirstOrDefaultAsync(x => x.GuildId == guildId && x.Text == text);

        public static Task<Prefix> GetPrefixAsync(this PrefixContext context, IGuild guild, string text)
            => GetPrefixAsync(context, guild.Id, text);

        public static async Task<Prefix> AddPrefixAsync(this PrefixContext context, ulong guildId, string text)
        {
            var prefix = new Prefix(guildId, text);

            await context.Prefixes.AddAsync(prefix);
            return prefix;
        }

        public static Task<Prefix> AddPrefixAsync(this PrefixContext context, IGuild guild, string text)
            => AddPrefixAsync(context, guild.Id, text);

        public static void RemovePrefix(this PrefixContext context, Prefix prefix)
            => context.Prefixes.Remove(prefix);
    }
}
