using System;
using System.Threading.Tasks;
using Discord;

namespace Conbot.Commands
{
    public sealed class DefaultPrefixHandler : IPrefixHandler
    {
        private static readonly Lazy<DefaultPrefixHandler> _lazy =
            new Lazy<DefaultPrefixHandler> (() => new DefaultPrefixHandler());

        public static DefaultPrefixHandler Instance => _lazy.Value;

        private DefaultPrefixHandler() { }

        public Task<bool> HandlePrefixAsync(DiscordCommandContext context, out string output)
        {
            string content = context.Message.Content;
            output = null;

            if (content.StartsWith("!"))
            {
                output = content.Substring(1);
                return Task.FromResult(true);
            }

            int endPos = content.IndexOf(' ');
            if (endPos == -1)
                Task.FromResult(false);

            string mention = content.Substring(0, endPos);

            if (!MentionUtils.TryParseUser(mention, out ulong userId))
                Task.FromResult(false);

            if (userId == context.Client.CurrentUser.Id)
            {
                output = content.Substring(mention.Length + 1);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}