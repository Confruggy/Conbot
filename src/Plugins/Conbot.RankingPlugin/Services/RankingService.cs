using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Conbot.RankingPlugin
{
    public class RankingService : IHostedService
    {
        private readonly DiscordShardedClient _client;
        private readonly Random _random;
        private readonly int[] _levels;

        public RankingService(DiscordShardedClient client, Random random, IConfiguration config)
        {
            _client = client;
            _random = random;
            _levels = config.GetSection("RankingPlugin:Levels").Get<int[]>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _client.MessageReceived += OnMessageReceivedAsync;
            _client.UserJoined += OnUserJoinedAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.MessageReceived -= OnMessageReceivedAsync;
            _client.UserJoined -= OnUserJoinedAsync;
            return Task.CompletedTask;
        }

        public Task OnMessageReceivedAsync(SocketMessage message)
        {
            _ = Task.Run(async () =>
            {
                if (!(message.Author is SocketGuildUser user))
                    return;

                var now = DateTime.UtcNow;
                var guild = user.Guild;
                List<IRole> roles = null;

                using var context = new RankingContext();

                var rank = await context.GetOrCreateRankAsync(user);

                rank.TotalMessages++;

                int oldLevel = GetLevel(rank.ExperiencePoints);
                int newLevel = oldLevel;

                if (rank.LastMessage == null || now >= rank.LastMessage?.AddMinutes(1))
                {
                    rank.ExperiencePoints += rank.ExperiencePoints == 0 ? 10 : _random.Next(5, 15);
                    rank.RankedMessages++;
                    rank.LastMessage = now;

                    if (user.IsBot)
                    {
                        await context.SaveChangesAsync();
                        return;
                    }

                    newLevel = GetLevel(rank.ExperiencePoints);
                    roles = await GetRolesAsync(user, newLevel, context);
                }

                await context.SaveChangesAsync();

                var config = await context.GetGuildConfigurationAsync(user.Guild);

                if (roles != null)
                    await UpdateRolesAsync(user, roles, config?.RoleRewardsType ?? RoleRewardsType.Stack);

                if (newLevel > oldLevel && config?.ShowLevelUpAnnouncements == true &&
                    newLevel >= (config.LevelUpAnnouncementsMinimumLevel ?? 0))
                {
                    var channel = config.LevelUpAnnouncementsChannelId.HasValue
                        ? _client.GetChannel(config.LevelUpAnnouncementsChannelId.Value) as SocketTextChannel
                        : message.Channel as SocketTextChannel;

                    if (channel != null && user.Guild.CurrentUser.GetPermissions(channel).SendMessages)
                    {
                        string text = channel.Id == message.Channel.Id
                            ? $"{user.Mention}, you achieved level **{newLevel}**. Congratulations! 🎉"
                            : $"{user.Mention} achieved level **{newLevel}**. Congratulations! 🎉";

                        try { await channel.SendMessageAsync(text); } catch { }
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

                var rank = await context.GetOrCreateRankAsync(user);
                int level = GetLevel(rank.ExperiencePoints);
                var roles = await GetRolesAsync(user, level, context);

                await context.SaveChangesAsync();

                var config = await context.GetGuildConfigurationAsync(user.Guild);
                await UpdateRolesAsync(user, roles, config?.RoleRewardsType ?? RoleRewardsType.Stack);
            });

            return Task.CompletedTask;
        }

        public async Task<List<IRole>> GetRolesAsync(IGuildUser user, int level, RankingContext context)
        {
            List<IRole> roles = new List<IRole>();

            await foreach (var roleReward in context.GetRoleRewardsAsync(user.Guild))
            {
                var role = user.Guild.GetRole(roleReward.RoleId);

                if (role == null)
                {
                    context.RemoveRoleReward(roleReward);
                    continue;
                }

                if (level >= roleReward.Level)
                    roles.Add(role);
            }

            return roles;
        }

        public async Task UpdateRolesAsync(IGuildUser user, List<IRole> roles, RoleRewardsType type)
        {
            if (roles.Count == 0)
                return;

            if (type == RoleRewardsType.Stack)
            {
                foreach (var role in roles)
                {
                    try { await user.AddRoleAsync(role); } catch { }
                }
            }
            else
            {
                foreach (var role in roles.SkipLast(1))
                {
                    try { await user.RemoveRoleAsync(role); } catch { }
                }

                try { await user.AddRoleAsync(roles.Last()); } catch { }
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
            => level > _levels.Length
                ? _levels.Max(x => x)
                : level <= 0
                    ? 0
                    : _levels[level - 1];

        public int GetNextLevelExperiencePoints(int level)
            => level + 1 > _levels.Length ? 0 : _levels[level] - (level == 0 ? 0 : _levels[level - 1]);
    }
}