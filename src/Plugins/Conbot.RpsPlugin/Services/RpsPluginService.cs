using System.Threading;
using System.Threading.Tasks;

using Disqord.Bot.Hosting;

using Qmmands;

namespace Conbot.RpsPlugin
{
    public class RpsPluginService : DiscordBotService
    {
        private Module? _module;

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _module = Bot.Commands.AddModule<RpsModule>();
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Bot.Commands.RemoveModule(_module);
            return base.StopAsync(cancellationToken);
        }
    }
}
