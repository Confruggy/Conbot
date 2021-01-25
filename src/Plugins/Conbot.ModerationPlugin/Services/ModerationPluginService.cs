using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Conbot.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Conbot.ModerationPlugin
{
    public class ModerationPluginService : IHostedService
    {
        private readonly SlashCommandService _slashCommandService;
        private readonly IServiceScopeFactory _scopeFactory;

        public ModerationPluginService(SlashCommandService slashCommandService, IServiceScopeFactory scopeFactory)
        {
            _slashCommandService = slashCommandService;
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateDatabaseAsync();
            await _slashCommandService.RegisterModuleAsync<ModerationModule>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task UpdateDatabaseAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ModerationContext>();

            await context.Database.MigrateAsync();
        }
    }
}
