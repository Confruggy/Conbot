using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot.Hosting;

using Qmmands;

namespace Conbot.TagPlugin;

public class TagPluginService : DiscordBotService
{
    private Module? _module;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await UpdateDatabaseAsync();
        _module = Bot.Commands.AddModule<TagModule>();

        await base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Bot.Commands.RemoveModule(_module);
        return base.StopAsync(cancellationToken);
    }

    private async Task UpdateDatabaseAsync()
    {
        using var serviceScope = Bot.Services.CreateScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<TagContext>();

        await context.Database.MigrateAsync();
    }
}