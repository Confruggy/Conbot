using System;
using System.Threading;
using System.Threading.Tasks;
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
        private TzdbZoneLocationsTypeParser _tzdbZoneLocationsTypeParser;
        private GmtTimeZoneTypeParser _gmtTimeZoneTypeParser;
        private Module _module;

        public TimeZonePluginService(IServiceProvider provider, CommandService commandService)
        {
            _services = provider;
            _commandService = commandService;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            UpdateDatabase();

            _tzdbZoneLocationsTypeParser = new TzdbZoneLocationsTypeParser();
            _commandService.AddTypeParser(_tzdbZoneLocationsTypeParser);
            _gmtTimeZoneTypeParser = new GmtTimeZoneTypeParser();
            _commandService.AddTypeParser(_gmtTimeZoneTypeParser);

            _module = _commandService.AddModule<TimeZoneModule>();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _commandService.RemoveModule(_module);

            _commandService.RemoveTypeParser(_tzdbZoneLocationsTypeParser);
            _commandService.RemoveTypeParser(_gmtTimeZoneTypeParser);
            
            return Task.CompletedTask;
        }

        private void UpdateDatabase()
        {
            using var serviceScope = _services
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<TimeZoneContext>();

            context.Database.Migrate();
        }
    }
}
