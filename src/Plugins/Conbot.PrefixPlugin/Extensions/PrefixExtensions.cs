using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Conbot.PrefixPlugin
{
    public static class PrefixExtensions
    {
        public static ValueTask<List<Prefix>> GetPrefixesAsync(this PrefixContext context, ulong? guildId = null)
        {
            if (guildId == null)
                return context.Prefixes.ToListAsync();

            return context.Prefixes.ToAsyncEnumerable().Where(x => x.GuildId == guildId).ToListAsync();
        }

        public static ValueTask<List<Prefix>> GetPrefixesAsync(this PrefixContext context, IGuild guild)
            => GetPrefixesAsync(context, guild.Id);

        public static ValueTask<Prefix> GetPrefixAsync(this PrefixContext context, ulong guildId, string text)
            => context.Prefixes.FirstOrDefaultAsync(x => x.GuildId == guildId && x.Text == text);

        public static ValueTask<Prefix> GetPrefixAsync(this PrefixContext context, IGuild guild, string text)
            => GetPrefixAsync(context, guild.Id, text);

        public async static ValueTask<Prefix> AddPrefixAsync(this PrefixContext context, ulong guildId, string text)
        {
            var prefix = new Prefix
            {
                GuildId = guildId,
                Text = text
            };

            await context.Prefixes.AddAsync(prefix);
            return prefix;
        }

        public static ValueTask<Prefix> AddPrefixAsync(this PrefixContext context, IGuild guild, string text)
            => AddPrefixAsync(context, guild.Id, text);

        public static void RemovePrefix(this PrefixContext context, Prefix prefix)
            => context.Prefixes.Remove(prefix);
    }
}