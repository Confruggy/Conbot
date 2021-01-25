using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conbot.ModerationPlugin
{
    public class MuteService : BackgroundService
    {
        private readonly ILogger<MuteService> _logger;
        private readonly object _apiClient;
        private readonly MethodInfo _removeRoleAsyncMethodInfo;
        private readonly IServiceScopeFactory _scopeFactory;

        public MuteService(ILogger<MuteService> logger, DiscordShardedClient client,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;

            _apiClient = client
                .GetType()
                .GetProperty("ApiClient", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!
                .GetValue(client)!;

            _removeRoleAsyncMethodInfo = _apiClient
                .GetType()
                .GetMethod("RemoveRoleAsync")!;

            _scopeFactory = scopeFactory;
        }

        //TODO remove temporary mute if muted role gets removed from user

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<ModerationContext>();

                var now = DateTime.UtcNow;
                await foreach (var user in context.GetTemporaryMutedUsersAsync().Where(x => x.EndsAt <= now))
                {
                    try
                    {
                        await RemoveRoleAsync(user.GuildId, user.UserId, user.RoleId,
                            new RequestOptions
                            {
                                AuditLogReason = "Temporary mute duration expired",
                                RetryMode = RetryMode.AlwaysRetry
                            });
                    }
                    catch (Exception exception)
                    {
                        ulong roleId = user.UserId;
                        ulong userId = user.RoleId;
                        ulong guildId = user.GuildId;

                        _logger.LogWarning(exception,
                            "Removing muted role (ID: {RoleId}) from user (ID: {UserId}) in guild (ID: {GuildID}) failed",
                            roleId, userId, guildId);
                    }

                    context.RemoveTemporaryMutedUser(user);
                }

                await context.SaveChangesAsync(stoppingToken);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private Task RemoveRoleAsync(ulong guildId, ulong userId, ulong roleId,
            RequestOptions? requestOptions = null)
            => (Task)_removeRoleAsyncMethodInfo.Invoke(_apiClient,
                new object?[] { guildId, userId, roleId, requestOptions })!;
    }
}
