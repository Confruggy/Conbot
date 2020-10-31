using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Qmmands;

namespace Conbot.RngPlugin
{
    public class RngPluginService : IHostedService
    {
        private CommandService _commandService;

        private Module _module;

        public RngPluginService(CommandService commandService) => _commandService = commandService;
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _module = _commandService.AddModule<RngModule>();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _commandService.RemoveModule(_module);
            return Task.CompletedTask;
        }
    }
}
