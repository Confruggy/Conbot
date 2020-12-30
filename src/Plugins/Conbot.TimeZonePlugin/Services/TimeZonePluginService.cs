using System;
using System.Threading;
using System.Threading.Tasks;
using Conbot.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Qmmands;

namespace Conbot.TimeZonePlugin
{
    public class TimeZonePluginService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly CommandService _commandService;
        private readonly SlashCommandService _slashCommandService;
        private TzdbZoneLocationsTypeParser _tzdbZoneLocationsTypeParser;
        private GmtTimeZoneTypeParser _gmtTimeZoneTypeParser;
        private ZonedDateTimeTypeParser _zonedDateTimeTypeParser;

        public TimeZonePluginService(IServiceProvider provider, CommandService commandService,
            SlashCommandService slashCommandService)
        {
            _commandService = commandService;
            _services = provider;
            _slashCommandService = slashCommandService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateDatabaseAsync();

            _tzdbZoneLocationsTypeParser = new TzdbZoneLocationsTypeParser();
            _commandService.AddTypeParser(_tzdbZoneLocationsTypeParser);
            _gmtTimeZoneTypeParser = new GmtTimeZoneTypeParser();
            _commandService.AddTypeParser(_gmtTimeZoneTypeParser);
            _zonedDateTimeTypeParser = new ZonedDateTimeTypeParser();
            _commandService.AddTypeParser(_zonedDateTimeTypeParser);

            await _slashCommandService.RegisterModuleAsync<TimeZoneModule>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _commandService.RemoveTypeParser(_tzdbZoneLocationsTypeParser);
            _commandService.RemoveTypeParser(_gmtTimeZoneTypeParser);
            _commandService.RemoveTypeParser(_zonedDateTimeTypeParser);

            return Task.CompletedTask;
        }

        private async Task UpdateDatabaseAsync()
        {
            using var serviceScope = _services
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<TimeZoneContext>();

            await context.Database.MigrateAsync();
        }
    }
}
