using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot.Hosting;

using Qmmands;

namespace Conbot.RankingPlugin
{
    public class RankingPluginService : DiscordBotService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private Module? _module;

        public RankingPluginService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateDatabaseAsync();
            _module = Bot.Commands.AddModule<RankingModule>();

            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Bot.Commands.RemoveModule(_module);
            return base.StopAsync(cancellationToken);
        }

        private async Task UpdateDatabaseAsync()
        {
            using var serviceScope = _scopeFactory.CreateScope();
            using var context = serviceScope.ServiceProvider.GetRequiredService<RankingContext>();

            await context.Database.MigrateAsync();
        }
    }
}
