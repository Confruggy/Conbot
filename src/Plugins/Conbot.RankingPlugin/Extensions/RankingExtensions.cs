using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Disqord;

namespace Conbot.RankingPlugin;

public static class RankingExtensions
{
    public static async Task<Rank?> GetRankAsync(this RankingContext context, ulong guildId, ulong userId)
        => await context
            .Ranks
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);

    public static Task<Rank?> GetRankAsync(this RankingContext context, IMember user)
        => GetRankAsync(context, user.GuildId, user.Id);

    public static async Task<Rank> GetOrCreateRankAsync(this RankingContext context, IMember user)
    {
        var rank = await GetRankAsync(context, user);

        if (rank is not null)
            return rank;

        rank = new Rank(user.GuildId, user.Id, user.IsBot, 0, 0, 0);
        await context.Ranks.AddAsync(rank);

        return rank;
    }

    public static IAsyncEnumerable<Rank> GetRanksAsync(this RankingContext context, ulong guildId,
        bool includeBots = false)
        => context
            .Ranks
            .AsNoTracking()
            .Where(x => x.GuildId == guildId && (includeBots || !x.IsBot))
            .OrderByDescending(x => x.ExperiencePoints)
            .AsAsyncEnumerable();

    public static IAsyncEnumerable<Rank> GetRanksAsync(this RankingContext context, IGuild guild,
        bool includeBots = false)
        => GetRanksAsync(context, guild.Id, includeBots);

    public static async Task<RankGuildConfiguration?> GetGuildConfigurationAsync(this RankingContext context,
        ulong guildId)
        => await context
            .GuildConfigurations
            .Include(x => x.RoleRewards)
            .Include(x => x.IgnoredChannels)
            .FirstOrDefaultAsync(x => x.GuildId == guildId);

    public static Task<RankGuildConfiguration?> GetGuildConfigurationAsync(this RankingContext context, IGuild guild)
        => GetGuildConfigurationAsync(context, guild.Id);

    public static async Task<RankGuildConfiguration> GetOrCreateGuildConfigurationAsync(this RankingContext context,
        ulong guildId)
    {
        var config = await GetGuildConfigurationAsync(context, guildId);

        if (config is null)
        {
            config = new RankGuildConfiguration(guildId);
            await context.GuildConfigurations.AddAsync(config);
        }

        return config;
    }

    public static Task<RankGuildConfiguration> GetOrCreateGuildConfigurationAsync(this RankingContext context,
        IGuild guild)
        => GetOrCreateGuildConfigurationAsync(context, guild.Id);

    public static IAsyncEnumerable<RankRoleReward> GetRoleRewardsAsync(this RankingContext context, ulong guildId)
        => context
            .RoleRewards
            .AsNoTracking()
            .Where(x => x.GuildId == guildId)
            .OrderBy(x => x.Level)
            .AsAsyncEnumerable();

    public static IAsyncEnumerable<RankRoleReward> GetRoleRewardsAsync(this RankingContext context, IGuild guild)
        => GetRoleRewardsAsync(context, guild.Id);

    public static async Task<RankRoleReward?> GetRoleRewardAsync(this RankingContext context, ulong guildId, int level)
        => await context.RoleRewards.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildId && x.Level == level);

    public static Task<RankRoleReward?> GetRoleRewardAsync(this RankingContext context, IGuild guild, int level)
        => GetRoleRewardAsync(context, guild.Id, level);

    public static async Task<RankRoleReward?> GetRoleRewardAsync(this RankingContext context, ulong roleId)
        => await context.RoleRewards.AsNoTracking().FirstOrDefaultAsync(x => x.RoleId == roleId);

    public static Task<RankRoleReward?> GetRoleRewardAsync(this RankingContext context, IRole role)
        => GetRoleRewardAsync(context, role.Id);

    public static async Task<RankRoleReward> AddRoleRewardAsync(this RankingContext context,
        RankGuildConfiguration config, int level, IRole role)
    {
        var roleReward = new RankRoleReward(config, level, role.Id);
        await context.RoleRewards.AddAsync(roleReward);
        return roleReward;
    }

    public static void RemoveRoleReward(this RankingContext context, RankRoleReward roleReward)
        => context.RoleRewards.Remove(roleReward);

    public static async Task<IgnoredChannel?> GetIgnoredChannelAsync(this RankingContext context,
        ulong channelId)
        => await context.IgnoredChannels.AsNoTracking().FirstOrDefaultAsync(x => x.ChannelId == channelId);

    public static Task<IgnoredChannel?> GetIgnoredChannelAsync(this RankingContext context,
        ITextChannel channel)
        => GetIgnoredChannelAsync(context, channel.Id);

    public static IAsyncEnumerable<IgnoredChannel> GetIgnoredChannelsAsync(this RankingContext context,
        ulong guildId)
        => context.IgnoredChannels.AsNoTracking().Where(x => x.GuildId == guildId).AsAsyncEnumerable();

    public static IAsyncEnumerable<IgnoredChannel> GetIgnoredChannelsAsync(this RankingContext context,
        IGuild guild)
        => GetIgnoredChannelsAsync(context, guild.Id);

    public static void AddIgnoredChannel(this RankingContext context, ulong channelId,
        RankGuildConfiguration guildConfiguration)
    {
        var ignoredChannel = new IgnoredChannel(channelId, guildConfiguration);
        context.IgnoredChannels.Add(ignoredChannel);
    }

    public static void AddIgnoredChannel(this RankingContext context, ITextChannel channel,
        RankGuildConfiguration guildConfiguration)
        => AddIgnoredChannel(context, channel.Id, guildConfiguration);

    public static void RemoveIgnoredChannel(this RankingContext context, IgnoredChannel channel)
        => context.IgnoredChannels.Remove(channel);

    public static async Task<RankUserConfiguration?> GetUserConfigurationAsync(this RankingContext context,
        ulong userId)
        => await context.UserConfigurations.AsQueryable().FirstOrDefaultAsync(x => x.UserId == userId);

    public static Task<RankUserConfiguration?> GetUserConfigurationAsync(this RankingContext context, IUser user)
        => GetUserConfigurationAsync(context, user.Id);

    public static async Task<RankUserConfiguration> GetOrCreateUserConfigurationAsync(this RankingContext context,
        ulong userId)
    {
        var config = await GetUserConfigurationAsync(context, userId);

        if (config is null)
        {
            config = new RankUserConfiguration(userId);
            await context.UserConfigurations.AddAsync(config);
        }

        return config;
    }

    public static Task<RankUserConfiguration> GetOrCreateUserConfigurationAsync(this RankingContext context,
        IUser user)
        => GetOrCreateUserConfigurationAsync(context, user.Id);
}