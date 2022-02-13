using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Disqord.Bot.Hosting;
using Disqord.Rest;

namespace Conbot.ModerationPlugin;

public class MuteService : DiscordBotService
{
    private readonly ILogger<MuteService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public MuteService(ILogger<MuteService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    //TODO remove temporary mute if muted role gets removed from user

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<ModerationContext>();

            var now = DateTime.UtcNow;
            await foreach (var user in context.GetTemporaryMutedUsersAsync()
                               .Where(x => x.EndsAt <= now)
                               .WithCancellation(stoppingToken))
            {
                try
                {
                    await Bot.RevokeRoleAsync(user.GuildId, user.UserId, user.RoleId,
                        new DefaultRestRequestOptions { Reason = "Temporary mute duration expired" },
                        stoppingToken);
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
}