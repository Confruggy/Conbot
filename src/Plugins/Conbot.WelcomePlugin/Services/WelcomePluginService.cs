using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot.Hosting;

using Qmmands;

namespace Conbot.WelcomePlugin
{
    public class WelcomePluginService : DiscordBotService
    {
        private Module? _module;

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateDatabaseAsync();
            _module = Bot.Commands.AddModule<WelcomeAndGoodbyeModule>();

            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Bot.Commands.RemoveModule(_module);
            return base.StopAsync(cancellationToken);
        }

        private async Task UpdateDatabaseAsync()
        {
            using var serviceScope = Bot.Services.CreateScope();
            using var context = serviceScope.ServiceProvider.GetRequiredService<WelcomeContext>();

            await context.Database.MigrateAsync();
        }
    }
}
