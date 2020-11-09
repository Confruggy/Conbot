using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Qmmands;

namespace Conbot.TagPlugin
{
    public class TagPluginService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly CommandService _commandService;
        private Module _module;

        public TagPluginService(IServiceProvider services, CommandService commandService)
        {
            _services = services;
            _commandService = commandService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            UpdateDatabase();
            _module = _commandService.AddModule<TagModule>();
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
            using var context = serviceScope.ServiceProvider.GetService<TagContext>();

            context.Database.Migrate();
        }
    }
}
