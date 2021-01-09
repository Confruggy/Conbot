using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Conbot.Commands;
using Conbot.Extensions;

using Qmmands;

namespace Conbot.PrefixPlugin
{
    public class CustomPrefixHandler : IPrefixHandler
    {
        public async ValueTask<PrefixResult> HandlePrefixAsync(DiscordCommandContext context)
        {
            var db = context.ServiceProvider.GetRequiredService<PrefixContext>();

            if (context.Guild == null || context.Message == null)
                return PrefixResult.Unsuccessful;

            var prefixes = (await db.GetPrefixesAsync(context.Guild))
                .OrderByDescending(x => x.Text.Length)
                .ThenBy(x => x.Text);

            string? output;

            foreach (var prefix in prefixes)
            {
                if (CommandUtilities.HasPrefix(context.Message.Content, prefix.Text, out output))
                    return PrefixResult.Successful(output);
            }

            if (context.Message.HasMentionPrefix(context.Client.CurrentUser, out output))
                return PrefixResult.Successful(output!);

            return PrefixResult.Unsuccessful;
        }
    }
}