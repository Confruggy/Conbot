using System.Linq;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Conbot.PrefixPlugin
{
    public class CustomPrefixHandler : IPrefixHandler
    {
        public async ValueTask<PrefixResult> HandlePrefixAsync(DiscordCommandContext context)
        {
            var db = context.ServiceProvider.GetRequiredService<PrefixContext>();

            var prefixes = (await db.GetPrefixesAsync(context.Guild))
                .OrderByDescending(x => x.Text.Length)
                .ThenBy(x => x.Text);

            string output;

            foreach (var prefix in prefixes)
            {
                if (CommandUtilities.HasPrefix(context.Message.Content, prefix.Text, out output))
                    return PrefixResult.Successful(output);
            }

            if (context.Message.HasMentionPrefix(context.Client.CurrentUser, out output))
                return PrefixResult.Successful(output);

            return PrefixResult.Unsuccessful;
        }
    }
}