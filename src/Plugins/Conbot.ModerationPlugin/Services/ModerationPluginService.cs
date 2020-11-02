using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Qmmands;

namespace Conbot.ModerationPlugin
{
    public class ModerationPluginService : IHostedService
    {
        private readonly CommandService _commandService;
        private Module _module;

        public ModerationPluginService(CommandService commandService) => _commandService = commandService;
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _module = _commandService.AddModule<ModerationModule>();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _commandService.RemoveModule(_module);
            return Task.CompletedTask;
        }
    }
}
