using System;
using System.Threading;
using System.Threading.Tasks;
using Conbot.Services.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Qmmands;

namespace Conbot.PrefixPlugin
{
    public class PrefixPluginService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly CommandService _commandService;
        private Module _module;

        public PrefixPluginService(IServiceProvider services, CommandService commandService)
        {
            _services = services;
            _commandService = commandService;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            UpdateDatabase();

            var commandHandlingService = _services.GetRequiredService<CommandHandlingService>();
            commandHandlingService.CustomPrefixHandler = new CustomPrefixHandler();

            _module = _commandService.AddModule<PrefixModule>();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _commandService.RemoveModule(_module);
            return Task.CompletedTask;
        }

        private void UpdateDatabase()
        {
            using var serviceScope = _services
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<PrefixContext>();

            context.Database.Migrate();
        }
    }
}