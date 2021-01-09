using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Conbot.Commands;

namespace Conbot.InfoPlugin
{
    public class InfoPluginService : IHostedService
    {
        private readonly SlashCommandService _slashCommandService;

        public InfoPluginService(SlashCommandService slashCommandService)
            => _slashCommandService = slashCommandService;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _slashCommandService.RegisterModuleAsync<InfoModule>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
