using System.Threading;
using System.Threading.Tasks;

using Conbot.Commands;

using Microsoft.Extensions.Hosting;

namespace Conbot.SplatoonPlugin
{
    public class SplatoonPluginService : IHostedService
    {
        private readonly SlashCommandService _slashCommandService;

        public SplatoonPluginService(SlashCommandService slashCommandService)
            => _slashCommandService = slashCommandService;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _slashCommandService.RegisterModuleAsync<SplatoonModule>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
