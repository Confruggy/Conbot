using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Conbot.Commands;

namespace Conbot.RankingPlugin
{
    public class RankingPluginService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly SlashCommandService _slashCommandService;

        public RankingPluginService(IServiceProvider services, SlashCommandService slashCommandService)
        {
            _services = services;
            _slashCommandService = slashCommandService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateDatabaseAsync();

            await _slashCommandService.RegisterModuleAsync<RankingModule>();
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

            using var context = serviceScope.ServiceProvider.GetRequiredService<RankingContext>();
            await context.Database.MigrateAsync();
        }
    }
}
