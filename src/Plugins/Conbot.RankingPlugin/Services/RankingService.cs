using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Discord;
using Discord.WebSocket;

namespace Conbot.RankingPlugin
{
    public class RankingService : IHostedService
    {
        private readonly ILogger<RankingService> _logger;
        private readonly DiscordShardedClient _client;
        private readonly Random _random;
        private readonly int[] _levels;

        public RankingService(DiscordShardedClient client, Random random, IConfiguration config,
            ILogger<RankingService> logger)
        {
            _client = client;
            _random = random;
            _levels = config.GetSection("RankingPlugin:Levels").Get<int[]>();
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _client.MessageReceived += OnMessageReceivedAsync;
            _client.UserJoined += OnUserJoinedAsync;
            _client.RoleDeleted += OnRoleDeletedAsync;
            _client.GuildAvailable += GuildAvailableAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.MessageReceived -= OnMessageReceivedAsync;
            _client.UserJoined -= OnUserJoinedAsync;
            _client.RoleDeleted -= OnRoleDeletedAsync;
            _client.GuildAvailable -= GuildAvailableAsync;
            return Task.CompletedTask;
        }

        public Task OnMessageReceivedAsync(SocketMessage message)
        {
            _ = Task.Run(async () =>
            {
                if (message.Author is not SocketGuildUser user)
                    return;

                var now = DateTime.UtcNow;
                var guild = user.Guild;

                using var context = new RankingContext();

                var config = await context.GetGuildConfigurationAsync(guild);
                if (config?.IgnoredChannels.Any(x => x.ChannelId == message.Channel.Id) == true)
                    return;

                var rank = await context.GetOrCreateRankAsync(user);
                rank.TotalMessages++;

                int oldLevel = GetLevel(rank.ExperiencePoints);

                if (rank.LastMessage == null || now >= rank.LastMessage?.AddMinutes(1))
                {
                    rank.ExperiencePoints += rank.ExperiencePoints == 0 ? 10 : _random.Next(5, 15);
                    rank.RankedMessages++;
                    rank.LastMessage = now;
                }

                await context.SaveChangesAsync();

                int newLevel = GetLevel(rank.ExperiencePoints);

                if (config is not null)
                {
                    var roles = GetRolesForLevel(user.Guild.Roles, newLevel, config);

                    if (roles != null)
                        await UpdateRolesAsync(user, roles, config?.RoleRewardsType ?? RoleRewardsType.Stack);
                }

                if (newLevel > oldLevel)
                {
                    var userConfig = await context.GetUserConfigurationAsync(user);
                    IMessageChannel? channel = null;
                    string? text = null;
                    AllowedMentions? allowedMentions = null;

                    if (config?.ShowLevelUpAnnouncements == true &&
                        newLevel >= (config.LevelUpAnnouncementsMinimumLevel ?? 0))
                    {
                        channel = config.LevelUpAnnouncementsChannelId.HasValue
                            ? _client.GetChannel(config.LevelUpAnnouncementsChannelId.Value) as IMessageChannel
                            : message.Channel;

                        if (channel is SocketTextChannel textChannel &&
                            guild.CurrentUser.GetPermissions(textChannel).SendMessages)
                        {
                            text = channel.Id == message.Channel.Id
                                ? $"{user.Mention}, you achieved level **{newLevel}**. Congratulations! 🎉"
                                : $"{user.Mention} achieved level **{newLevel}**. Congratulations! 🎉";

                            allowedMentions = userConfig?.AnnouncementsAllowMentions ?? false
                                ? AllowedMentions.All
                                : AllowedMentions.None;
                        }
                        else
                        {
                            channel = null;
                        }
                    }

                    if (channel is null && userConfig?.AnnouncementsSendDirectMessages == true)
                    {
                        channel = await user.GetOrCreateDMChannelAsync();
                        text = $"You achieved level **{newLevel}** on **{guild.Name}**. Congratulations! 🎉";
                    }

                    if (channel is not null)
                    {
                        try
                        {
                            await channel.SendMessageAsync(text, allowedMentions: allowedMentions);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogWarning(exception,
                                "Sending level up message for {User} failed on {Guild}/{Channel}", user, guild,
                                channel);
                        }
                    }
                }
            });

            return Task.CompletedTask;
        }

        public Task OnUserJoinedAsync(SocketGuildUser user)
        {
            _ = Task.Run(async () =>
            {
                using var context = new RankingContext();

                var rank = await context.GetRankAsync(user);
                int level = rank is null ? 0 : GetLevel(rank.ExperiencePoints);
                var config = await context.GetGuildConfigurationAsync(user.Guild);

                if (config != null)
                {
                    var roles = GetRolesForLevel(user.Guild.Roles, level, config);
                    await UpdateRolesAsync(user, roles, config?.RoleRewardsType ?? RoleRewardsType.Stack);
                }
            });

            return Task.CompletedTask;
        }

        public static Task OnRoleDeletedAsync(SocketRole role)
        {
            _ = Task.Run(async () =>
            {
                using var context = new RankingContext();

                var config = await context.GetGuildConfigurationAsync(role.Guild);
                if (config is null)
                    return;

                var roleReward = config.RoleRewards.Find(x => x.RoleId == role.Id);
                if (roleReward is null)
                    return;

                context.Remove(roleReward);
                await context.SaveChangesAsync();
            });

            return Task.CompletedTask;
        }

        public Task GuildAvailableAsync(SocketGuild guild)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogDebug("Checking role rewards for {Guild}", guild);

                    var roles = (await _client.Rest.GetGuildAsync(guild.Id))?.Roles;
                    if (roles is null)
                        return;

                    using var context = new RankingContext();

                    var config = await context.GetGuildConfigurationAsync(guild);
                    if (config is null)
                        return;

                    if (config.RoleRewards.Count == 0)
                        return;

                    foreach (var roleReward in config.RoleRewards.Where(x => !roles.Any(r => r.Id == x.RoleId)))
                        context.RemoveRoleReward(roleReward);

                    await context.SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    _logger.LogDebug(exception, "Checking role rewards for {Guild} failed", guild);
                }
            });

            return Task.CompletedTask;
        }

        public static IEnumerable<IRole> GetRolesForLevel(IEnumerable<IRole> roles, int level,
            RankGuildConfiguration configuration)
        {

            foreach (var roleReward in configuration.RoleRewards)
            {
                var role = roles.FirstOrDefault(x => x.Id == roleReward.RoleId);
                if (role == null)
                    continue;

                if (level >= roleReward.Level)
                    yield return role;
            }
        }

        public async Task UpdateRolesAsync(IGuildUser user, IEnumerable<IRole> roles, RoleRewardsType type)
        {
            if (!roles.Any())
                return;

            var guild = user.Guild;

            if (type == RoleRewardsType.Stack)
            {
                foreach (var role in roles)
                {
                    try
                    {
                        await user.AddRoleAsync(role);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "Adding role {Role} to {User} failed in {Guild}", role, user,
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
                        await user.RemoveRoleAsync(role);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "Removing role {Role} from {User} failed in {Guild}", role, user,
                            user.Guild);
                    }
                }

                var last = roles.Last();
                try
                {
                    await user.AddRoleAsync(last);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Adding role {Role} to {User} failed in {Guild}", last, user,
                        user.Guild);
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
}
