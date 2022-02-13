using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot.Hosting;

using Qmmands;

namespace Conbot.PrefixPlugin;

public class PrefixPluginService : DiscordBotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Module? _module;

    public PrefixPluginService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await UpdateDatabaseAsync();
        _module = Bot.Commands.AddModule<PrefixModule>();

        await base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Bot.Commands.RemoveModule(_module);
        return base.StopAsync(cancellationToken);
    }

    private async Task UpdateDatabaseAsync()
    {
        using var serviceScope = _scopeFactory.CreateScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<PrefixContext>();

        await context.Database.MigrateAsync();
    }
}