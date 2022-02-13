using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Disqord;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;

namespace Conbot.RankingPlugin;

public class RankingService : DiscordBotService
{
    private readonly ILogger<RankingService> _logger;
    private readonly DiscordBot _bot;
    private readonly Random _random;
    private readonly int[] _levels;
    private readonly IServiceScopeFactory _scopeFactory;

    public RankingService(DiscordBot bot, Random random, IConfiguration config, IServiceScopeFactory scopeFactory,
        ILogger<RankingService> logger)
    {
        _bot = bot;
        _random = random;
        _levels = config.GetSection("RankingPlugin:Levels").Get<int[]>();
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.Message.Author is not IMember member)
            return;

        var now = DateTime.UtcNow;
        var guildId = member.GuildId;

        using var scope = _scopeFactory.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<RankingContext>();

        var config = await context.GetGuildConfigurationAsync(guildId);

        if (config?.IgnoredChannels.Any(x => x.ChannelId == e.ChannelId) == true)
            return;

        var rank = await context.GetOrCreateRankAsync(member);
        rank.TotalMessages++;

        int oldLevel = GetLevel(rank.ExperiencePoints);

        if (rank.LastMessage is null || now >= rank.LastMessage?.AddMinutes(1))
        {
            rank.ExperiencePoints += rank.ExperiencePoints == 0
                ? 10
                : _random.Next((int)(10 * config?.Multiplier ?? 1), (int)(20 * config?.Multiplier ?? 1));
            rank.RankedMessages++;
            rank.LastMessage = now;
        }

        await context.SaveChangesAsync();

        if (member.IsBot)
            return;

        int newLevel = GetLevel(rank.ExperiencePoints);

        if (config is not null)
        {
            var roles = GetRolesForLevel(_bot.GetRoles(guildId), newLevel, config);

            await UpdateRolesAsync(member, roles, config.RoleRewardsType ?? RoleRewardsType.Stack);
        }

        if (newLevel > oldLevel)
        {
            var userConfig = await context.GetUserConfigurationAsync(member);
            IMessageChannel? channel = null;
            string? text = null;
            LocalAllowedMentions? allowedMentions = null;
            var guild = _bot.GetGuild(guildId);

            if (config?.ShowLevelUpAnnouncements == true &&
                newLevel >= (config.LevelUpAnnouncementsMinimumLevel ?? 0))
            {
                channel = config.LevelUpAnnouncementsChannelId.HasValue
                    ? await _bot.FetchChannelAsync(config.LevelUpAnnouncementsChannelId.Value) as IMessageChannel
                    : e.Channel;

                if (channel is IGuildChannel guildChannel &&
                    guild.GetMember(_bot.CurrentUser.Id).GetPermissions(guildChannel).SendMessages)
                {
                    text = channel.Id == e.ChannelId
                        ? $"{member.Mention}, you achieved level **{newLevel}**. Congratulations! 🎉"
                        : $"{member.Mention} achieved level **{newLevel}**. Congratulations! 🎉";

                    allowedMentions = userConfig?.AnnouncementsAllowMentions ?? false
                        ? LocalAllowedMentions.ExceptEveryone
                        : LocalAllowedMentions.None;
                }
                else
                {
                    channel = null;
                }
            }

            if (channel is null && userConfig?.AnnouncementsSendDirectMessages == true)
            {
                channel = await member.CreateDirectChannelAsync();

                text = $"You achieved level **{newLevel}** on **{guild.Name}**. Congratulations! 🎉";
            }

            if (channel is not null)
            {
                try
                {
                    await channel.SendMessageAsync(
                        new LocalMessage()
                            .WithContent(text)
                            .WithAllowedMentions(allowedMentions)
                    );
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception,
                        "Sending level up message for {Member} failed on {Guild}/{Channel}", member, guildId,
                        channel);
                }
            }
        }
    }

    protected override async ValueTask OnMemberJoined(MemberJoinedEventArgs e)
    {
        using var scope = _scopeFactory.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<RankingContext>();

        var rank = await context.GetRankAsync(e.Member);
        int level = rank is null ? 0 : GetLevel(rank.ExperiencePoints);
        var config = await context.GetGuildConfigurationAsync(e.GuildId);

        if (config is not null)
        {
            var roles = GetRolesForLevel(_bot.GetRoles(e.GuildId), level, config);
            await UpdateRolesAsync(e.Member, roles, config.RoleRewardsType ?? RoleRewardsType.Stack);
        }
    }

    protected override async ValueTask OnRoleDeleted(RoleDeletedEventArgs e)
    {
        using var scope = _scopeFactory.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<RankingContext>();

        var config = await context.GetGuildConfigurationAsync(e.GuildId);
        if (config is null)
            return;

        var roleReward = config.RoleRewards.Find(x => x.RoleId == e.RoleId);
        if (roleReward is null)
            return;

        context.Remove(roleReward);
        await context.SaveChangesAsync();
    }

    protected override async ValueTask OnGuildAvailable(GuildAvailableEventArgs e)
    {
        var guild = e.Guild;

        try
        {
            _logger.LogDebug("Checking role rewards for {Guild}", guild);

            var roles = await _bot.FetchRolesAsync(guild.Id);
            if (roles.Count == 0)
                return;

            using var scope = _scopeFactory.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<RankingContext>();

            var config = await context.GetGuildConfigurationAsync(guild);
            if (config is null)
                return;

            if (config.RoleRewards.Count == 0)
                return;

            foreach (var roleReward in config.RoleRewards.Where(x => roles.All(r => r.Id != x.RoleId)))
                context.RemoveRoleReward(roleReward);

            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Checking role rewards for {Guild} failed", guild);
        }
    }

    public static IEnumerable<IRole> GetRolesForLevel(IReadOnlyDictionary<Snowflake, CachedRole> roles, int level,
        RankGuildConfiguration configuration)
    {
        foreach (var roleReward in configuration.RoleRewards)
        {
            if (!roles.TryGetValue(roleReward.RoleId, out var role))
                continue;

            if (level >= roleReward.Level)
                yield return role;
        }
    }

    public Task UpdateRolesAsync(IMember member, IEnumerable<IRole> roles, RoleRewardsType type)
        => UpdateRolesAsync(member, roles.ToList(), type);

    public async Task UpdateRolesAsync(IMember member, IList<IRole> roles, RoleRewardsType type)
    {
        if (roles.Count == 0)
            return;

        var guild = member.GetGuild();

        if (type == RoleRewardsType.Stack)
        {
            foreach (var role in roles)
            {
                try
                {
                    await member.GrantRoleAsync(role.Id);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Adding role {Role} to {User} failed in {Guild}", role, member,
                        guild);
                }
            }
        }
        else
        {
            foreach (var role in roles.SkipLast(1))
            {
                try
                {
                    await member.RevokeRoleAsync(role.Id);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Removing role {Role} from {User} failed in {Guild}", role,
                        member, guild);
                }
            }

            var last = roles.Last();
            try
            {
                await member.GrantRoleAsync(last.Id);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Adding role {Role} to {User} failed in {Guild}", last, member,
                    guild);
            }
        }
    }

    public int GetLevel(int experiencePoints)
    {
        for (int i = 0; i < _levels.Length; i++)
        {
            if (experiencePoints < _levels[i])
                return i;
        }

        return _levels.Length;
    }

    public int GetTotalExperiencePoints(int level)
    {
        if (level > _levels.Length)
            return _levels.Max(x => x);

        return level <= 0 ? 0 : _levels[level - 1];
    }

    public int GetNextLevelExperiencePoints(int level)
    {
        if (level + 1 > _levels.Length)
            return 0;

        return _levels[level] - (level == 0 ? 0 : _levels[level - 1]);
    }
}