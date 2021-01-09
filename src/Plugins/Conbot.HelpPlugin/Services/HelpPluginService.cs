using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Conbot.Commands;

namespace Conbot.HelpPlugin
{
    public class HelpPluginService : IHostedService
    {
        private readonly SlashCommandService _slashCommandService;

        public HelpPluginService(SlashCommandService slashCommandService)
            => _slashCommandService = slashCommandService;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _slashCommandService.RegisterModuleAsync<HelpModule>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
