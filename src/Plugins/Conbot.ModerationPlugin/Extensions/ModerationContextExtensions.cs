using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;

using Microsoft.EntityFrameworkCore;

namespace Conbot.ModerationPlugin
{
    public static class ModerationContextExtensions
    {
        public static Task<ModerationGuildConfiguration> GetGuildConfigurationAsync(this ModerationContext context,
            ulong guildId) => context.GuildConfigurations.AsQueryable().FirstOrDefaultAsync(x => x.GuildId == guildId);

        public static Task<ModerationGuildConfiguration> GetGuildConfigurationAsync(this ModerationContext context,
            IGuild guild) => GetGuildConfigurationAsync(context, guild.Id);

        public static async Task<ModerationGuildConfiguration> GetOrCreateGuildConfigurationAsync(
            this ModerationContext context, ulong guildId)
        {
            var configuration = await GetGuildConfigurationAsync(context, guildId);

            if (configuration is null)
            {
                configuration = new ModerationGuildConfiguration(guildId);
                context.GuildConfigurations.Add(configuration);
            }

            return configuration;
        }

        public static Task<ModerationGuildConfiguration> GetOrCreateGuildConfigurationAsync(
            this ModerationContext context, IGuild guild)
            => GetOrCreateGuildConfigurationAsync(context, guild.Id);

        public static IAsyncEnumerable<TemporaryMutedUser> GetTemporaryMutedUsersAsync(this ModerationContext context)
            => context.TemporaryMutedUsers.AsAsyncEnumerable();

        public static IAsyncEnumerable<TemporaryMutedUser> GetTemporaryMutedUsersAsync(this ModerationContext context,
            ulong guildId)
            => context.TemporaryMutedUsers.AsQueryable().Where(x => x.GuildId == guildId).AsAsyncEnumerable();

        public static async Task<TemporaryMutedUser?> GetTemporaryMutedUserAsync(this ModerationContext context,
            ulong guildId, ulong userId)
            => await context
                .TemporaryMutedUsers
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);

        public static Task<TemporaryMutedUser?> GetTemporaryMutedUserAsync(this ModerationContext context,
            IGuildUser user)
            => GetTemporaryMutedUserAsync(context, user.Guild.Id, user.Id);

        public static async Task<TemporaryMutedUser> CreateOrUpdateTemporaryMutedUserAsync(
            this ModerationContext context, ulong guildId, ulong userId, ulong roleId, DateTime startedAt,
            DateTime endsAt)
        {
            var mutedUser = await GetTemporaryMutedUserAsync(context, guildId, userId);

            if (mutedUser is null)
            {
                mutedUser = new TemporaryMutedUser(guildId, userId, roleId, startedAt, endsAt);
                context.TemporaryMutedUsers.Add(mutedUser);
            }
            else
            {
                mutedUser.RoleId = roleId;
                mutedUser.StartedAt = startedAt;
                mutedUser.EndsAt = endsAt;
            }

            return mutedUser;
        }

        public static Task<TemporaryMutedUser> CreateOrUpdateTemporaryMutedUserAsync(this ModerationContext context,
            IGuildUser user, IRole role, DateTime startedAt, DateTime endsAt)
            => CreateOrUpdateTemporaryMutedUserAsync(context, user.Guild.Id, user.Id, role.Id, startedAt, endsAt);

        public static void RemoveTemporaryMutedUser(this ModerationContext context, TemporaryMutedUser user)
            => context.TemporaryMutedUsers.Remove(user);

        public static async Task<TemporaryMutedUser?> TryRemoveTemporaryMutedUserAsync(this ModerationContext context,
            ulong guildId, ulong userId)
        {
            var user = await GetTemporaryMutedUserAsync(context, guildId, userId);

            if (user is not null)
                context.TemporaryMutedUsers.Remove(user);

            return user;
        }

        public static Task<TemporaryMutedUser?> TryRemoveTemporaryMutedUserAsync(this ModerationContext context,
            IGuildUser user)
            => TryRemoveTemporaryMutedUserAsync(context, user.Guild.Id, user.Id);

        public static IAsyncEnumerable<PreconfiguredMutedRole> GetPreconfiguredMutedRolesAsync(
            this ModerationContext context, ulong guildId)
            => context.PreconfiguredMutedRoles.AsNoTracking().Where(x => x.GuildId == guildId).AsAsyncEnumerable();

        public static Task<PreconfiguredMutedRole> GetPreconfiguredMutedRoleAsync(this ModerationContext context,
            ulong roleId)
            => context.PreconfiguredMutedRoles.AsNoTracking().FirstOrDefaultAsync(x => x.RoleId == roleId);

        public static Task<PreconfiguredMutedRole> GetPreconfiguredMutedRoleAsync(this ModerationContext context,
            IRole role)
            => GetPreconfiguredMutedRoleAsync(context, role.Id);

        public static void AddPreconfiguredMutedRole(this ModerationContext context, ulong roleId, ulong guildId)
            => context.PreconfiguredMutedRoles.Add(new PreconfiguredMutedRole(roleId, guildId));

        public static void AddPreconfiguredMutedRole(this ModerationContext context, IRole role)
            => AddPreconfiguredMutedRole(context, role.Id, role.Guild.Id);
    }
}
