using System;
using System.Threading.Tasks;

using Conbot.Extensions;

namespace Conbot.Commands
{
    public sealed class DefaultPrefixHandler : IPrefixHandler
    {
        private static readonly Lazy<DefaultPrefixHandler> s_lazy = new(() => new DefaultPrefixHandler());

        public static DefaultPrefixHandler Instance => s_lazy.Value;

        private DefaultPrefixHandler() { }

        public ValueTask<PrefixResult> HandlePrefixAsync(DiscordCommandContext context)
        {
            if (context.Message == null)
                return PrefixResult.Unsuccessful;

            string content = context.Message.Content;
            string? output;

            if (content.StartsWith("!"))
            {
                output = content[1..];
                return PrefixResult.Successful(output);
            }

            if (context.Message.HasMentionPrefix(context.Client.CurrentUser, out output))
                return PrefixResult.Successful(output!);

            return PrefixResult.Unsuccessful;
        }
    }
}
