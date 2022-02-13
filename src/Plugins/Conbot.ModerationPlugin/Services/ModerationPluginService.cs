using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot.Hosting;

using Qmmands;

namespace Conbot.ModerationPlugin;

public class ModerationPluginService : DiscordBotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Module? _module;

    public ModerationPluginService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await UpdateDatabaseAsync();
        _module = Bot.Commands.AddModule<ModerationModule>();

        await base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Bot.Commands.RemoveModule(_module);
        return base.StopAsync(cancellationToken);
    }

    private async Task UpdateDatabaseAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<ModerationContext>();

        await context.Database.MigrateAsync();
    }
}