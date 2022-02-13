using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot.Hosting;

using Qmmands;

namespace Conbot.ReminderPlugin;

public class ReminderPluginService : DiscordBotService
{
    private Module? _module;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await UpdateDatabaseAsync();
        Bot.Commands.AddArgumentParser(new ReminderArgumentParser());
        _module = Bot.Commands.AddModule<ReminderModule>();

        await base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Bot.Commands.RemoveModule(_module);
        Bot.Commands.RemoveArgumentParser<ReminderArgumentParser>();

        return base.StopAsync(cancellationToken);
    }

    private async Task UpdateDatabaseAsync()
    {
        using var serviceScope = Bot.Services.CreateScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<ReminderContext>();

        await context.Database.MigrateAsync();
    }
}