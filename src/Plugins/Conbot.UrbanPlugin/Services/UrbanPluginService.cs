using System.Threading;
using System.Threading.Tasks;
using Conbot.Commands;
using Microsoft.Extensions.Hosting;

namespace Conbot.UrbanPlugin
{
    public class UrbanPluginService : IHostedService
    {
        private readonly SlashCommandService _slashCommandService;

        public UrbanPluginService(SlashCommandService slashCommandService)
            => _slashCommandService = slashCommandService;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _slashCommandService.RegisterModuleAsync<UrbanModule>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
