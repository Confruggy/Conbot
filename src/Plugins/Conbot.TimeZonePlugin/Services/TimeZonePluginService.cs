using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot.Hosting;

using Qmmands;

namespace Conbot.TimeZonePlugin;

public class TimeZonePluginService : DiscordBotService
{
    private TzdbZoneLocationsTypeParser _tzdbZoneLocationsTypeParser = null!;
    private GmtTimeZoneTypeParser _gmtTimeZoneTypeParser = null!;
    private ZonedDateTimeTypeParser _zonedDateTimeTypeParser = null!;
    private Module? _module;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await UpdateDatabaseAsync();

        _tzdbZoneLocationsTypeParser = new TzdbZoneLocationsTypeParser();
        Bot.Commands.AddTypeParser(_tzdbZoneLocationsTypeParser);
        _gmtTimeZoneTypeParser = new GmtTimeZoneTypeParser();
        Bot.Commands.AddTypeParser(_gmtTimeZoneTypeParser);
        _zonedDateTimeTypeParser = new ZonedDateTimeTypeParser();
        Bot.Commands.AddTypeParser(_zonedDateTimeTypeParser);

        _module = Bot.Commands.AddModule<TimeZoneModule>();

        await base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Bot.Commands.RemoveModule(_module);

        Bot.Commands.RemoveTypeParser(_tzdbZoneLocationsTypeParser);
        Bot.Commands.RemoveTypeParser(_gmtTimeZoneTypeParser);
        Bot.Commands.RemoveTypeParser(_zonedDateTimeTypeParser);

        return base.StopAsync(cancellationToken);
    }

    private async Task UpdateDatabaseAsync()
    {
        using var serviceScope = Bot.Services.CreateScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<TimeZoneContext>();

        await context.Database.MigrateAsync();
    }
}