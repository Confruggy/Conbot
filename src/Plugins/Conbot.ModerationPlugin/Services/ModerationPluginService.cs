using System.Threading;
using System.Threading.Tasks;
using Conbot.Services.Commands;
using Microsoft.Extensions.Hosting;

namespace Conbot.ModerationPlugin
{
    public class ModerationPluginService : IHostedService
    {
        private readonly SlashCommandService _slashCommandService;

        public ModerationPluginService(SlashCommandService slashCommandService)
            => _slashCommandService = slashCommandService;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _slashCommandService.RegisterModuleAsync<ModerationModule>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
