using System.Threading;
using System.Threading.Tasks;
using Conbot.Services.Commands;
using Microsoft.Extensions.Hosting;

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
