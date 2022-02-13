using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Disqord;

namespace Conbot.WelcomePlugin;

public static class WelcomeContextExtensions
{
    public static async Task<WelcomeConfiguration?> GetConfigurationASync(this WelcomeContext context,
        ulong guildId)
        => await context.Configurations.AsQueryable().FirstOrDefaultAsync(x => x.GuildId == guildId);

    public static Task<WelcomeConfiguration?> GetConfigurationAsync(this WelcomeContext context,
        IGuild guild)
        => GetConfigurationASync(context, guild.Id);

    public static async Task<WelcomeConfiguration> GetOrCreateConfigurationAsync(this WelcomeContext context,
        ulong guildId)
    {
        var config = await GetConfigurationASync(context, guildId);

        if (config is not null)
            return config;

        config = new WelcomeConfiguration(guildId);
        await context.Configurations.AddAsync(config);

        return config;
    }

    public static Task<WelcomeConfiguration> GetOrCreateConfigurationAsync(this WelcomeContext context,
        IGuild guild)
        => GetOrCreateConfigurationAsync(context, guild.Id);
}