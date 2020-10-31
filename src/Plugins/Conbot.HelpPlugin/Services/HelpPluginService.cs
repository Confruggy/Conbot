using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Qmmands;

namespace Conbot.HelpPlugin
{
    public class HelpPluginService : IHostedService
    {
        private CommandService _commandService;

        private Module _module;

        public HelpPluginService(CommandService commandService) => _commandService = commandService;
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _module = _commandService.AddModule<HelpModule>();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _commandService.RemoveModule(_module);
            return Task.CompletedTask;
        }
    }
}
