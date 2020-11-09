using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Qmmands;

namespace Conbot.ReminderPlugin
{
    public class ReminderPluginService : IHostedService
    {
        private readonly IServiceProvider _provider;
        private readonly CommandService _commandService;
        private Module _module;

        public ReminderPluginService(IServiceProvider provider, CommandService commandService)
        {
            _provider = provider;
            _commandService = commandService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            UpdateDatabase();
            _module = _commandService.AddModule<ReminderModule>();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _commandService.RemoveModule(_module);
            return Task.CompletedTask;
        }

        private void UpdateDatabase()
        {
            using var serviceScope = _provider
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<ReminderContext>();

            context.Database.Migrate();
        }
    }
}
