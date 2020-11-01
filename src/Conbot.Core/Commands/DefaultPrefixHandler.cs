using System;
using System.Threading.Tasks;
using Conbot.Extensions;
using Discord;

namespace Conbot.Commands
{
    public sealed class DefaultPrefixHandler : IPrefixHandler
    {
        private static readonly Lazy<DefaultPrefixHandler> _lazy =
            new Lazy<DefaultPrefixHandler> (() => new DefaultPrefixHandler());

        public static DefaultPrefixHandler Instance => _lazy.Value;

        private DefaultPrefixHandler() { }

        public ValueTask<PrefixResult> HandlePrefixAsync(DiscordCommandContext context)
        {
            string content = context.Message.Content;
            string output = null;

            if (content.StartsWith("!"))
            {
                output = content.Substring(1);
                return PrefixResult.Successful(output);
            }

            if (context.Message.HasMentionPrefix(context.Client.CurrentUser, out output))
                return PrefixResult.Successful(output);

            return PrefixResult.Unsuccessful;
        }
    }
}