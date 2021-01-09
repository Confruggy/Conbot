using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Commands;

namespace Conbot.PrefixPlugin
{
    public class PrefixPluginService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly SlashCommandService _slashCommandService;

        public PrefixPluginService(IServiceProvider services, SlashCommandService slashCommandService)
        {
            _services = services;
            _slashCommandService = slashCommandService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateDatabaseAsync();

            var commandHandlingService = _services.GetRequiredService<CommandHandlingService>();
            commandHandlingService.CustomPrefixHandler = new CustomPrefixHandler();

            await _slashCommandService.RegisterModuleAsync<PrefixModule>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task UpdateDatabaseAsync()
        {
            using var serviceScope = _services
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();

            using var context = serviceScope.ServiceProvider.GetRequiredService<PrefixContext>();
            await context.Database.MigrateAsync();
        }
    }
}