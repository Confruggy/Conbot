using System;
using System.Threading;
using System.Threading.Tasks;

using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;

namespace Conbot.Commands;

public static class DiscordCommandContextExtensions
{
    public static Task<MessageReceivedEventArgs> WaitForReplyAsync(this DiscordCommandContext context, Snowflake messageId,
        Predicate<IGatewayMessage>? predicate = null, TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
        => context.WaitForMessageAsync(
            e => (e.Message as IUserMessage)?.Reference?.MessageId == messageId &&
                 (predicate?.Invoke(e.Message) ?? true),
            timeout,
            cancellationToken);
}
