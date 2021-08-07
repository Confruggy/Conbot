using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Bot.Hosting;

namespace Conbot.PrefixPlugin
{
    public class CustomPrefixProvider : DiscordBotService, IPrefixProvider
    {
        public async ValueTask<IEnumerable<IPrefix>> GetPrefixesAsync(IGatewayUserMessage message)
        {
            using var scope = Bot.Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<PrefixContext>();

            if (message.GuildId is null)
                return Array.Empty<IPrefix>();

            var prefixes = (await context.GetPrefixesAsync(message.GuildId))
                .OrderByDescending(x => x.Text.Length)
                .ThenBy(x => x.Text);

            var result = new List<IPrefix>() { new MentionPrefix(Bot.CurrentUser.Id) };
            result.AddRange(prefixes.Select(x => new StringPrefix(x.Text) as IPrefix));

            return result;
        }
    }
}
