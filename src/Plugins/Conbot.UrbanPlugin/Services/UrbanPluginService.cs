using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Qmmands;

namespace Conbot.UrbanPlugin
{
    public class UrbanPluginService : IHostedService
    {
        private readonly CommandService _commandService;
        private Module _module;

        public UrbanPluginService(CommandService commandService) => _commandService = commandService;
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _module = _commandService.AddModule<UrbanModule>();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _commandService.RemoveModule(_module);
            return Task.CompletedTask;
        }
    }
}
