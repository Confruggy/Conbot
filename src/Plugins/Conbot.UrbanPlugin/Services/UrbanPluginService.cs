using System.Threading;
using System.Threading.Tasks;

using Disqord.Bot.Hosting;

using Qmmands;

namespace Conbot.UrbanPlugin
{
    public class UrbanPluginService : DiscordBotService
    {
        private Module? _module;

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _module = Bot.Commands.AddModule<UrbanModule>();
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Bot.Commands.RemoveModule(_module);
            return base.StopAsync(cancellationToken);
        }
    }
}
