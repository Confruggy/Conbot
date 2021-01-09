using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Conbot.Commands;

namespace Conbot.RngPlugin
{
    public class RngPluginService : IHostedService
    {
        private readonly SlashCommandService _slashCommandService;

        public RngPluginService(SlashCommandService slashCommandService)
            => _slashCommandService = slashCommandService;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _slashCommandService.RegisterModuleAsync<RngModule>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
