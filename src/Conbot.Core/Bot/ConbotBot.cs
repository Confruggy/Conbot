using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Disqord;
using Disqord.Bot;

namespace Conbot
{
    public partial class ConbotBot : DiscordBot
    {
        public ConbotBot(
            IOptions<DiscordBotConfiguration> options,
            ILogger<DiscordBot> logger,
            IServiceProvider services,
            DiscordClient client)
            : base(options, logger, services, client)
        {
        }
    }
}
