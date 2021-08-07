using Conbot.Interactive;

using Disqord;
using Disqord.Bot;

namespace Conbot.Commands
{
    public abstract class ConbotModuleBase<TContext> : DiscordModuleBase<TContext>
        where TContext : ConbotCommandContext
    {
        protected override DiscordResponseCommandResult Reply(LocalMessage message)
        {
            var msg = Context.Messages[^1];
            return Response(message.WithReply(msg.Id, Context.ChannelId, Context.GuildId));
        }

        protected override DiscordResponseCommandResult Response(LocalMessage message)
        {
            message.AllowedMentions ??= LocalAllowedMentions.None;
            return new ConbotResponseCommandResult(Context, message);
        }

        protected virtual ConbotModifyCommandResult Modify(IUserMessage message, string newContent)
            => new(Context, message, newContent);

        protected virtual ConbotModifyCommandResult Modify(IUserMessage message, params LocalEmbed[] newEmbeds)
            => new(Context, message, newEmbeds);

        protected virtual ConbotModifyCommandResult Modify(IUserMessage message, string newContent,
            params LocalEmbed[] newEmbeds)
            => new(Context, message, newContent, newEmbeds);

        protected virtual ConbotFailedCommandResult Fail(string failureReason)
            => new(Context, failureReason);

        protected virtual ConbotInteractiveCommandResult Interactive(LocalInteractiveMessage message)
            => new(Context, message);

        protected virtual ConbotInteractiveCommandResult Paginate(Paginator paginator, int startIndex = 0,
            bool reply = true)
            => new(Context, paginator.ToInteractiveMessage(Context, startIndex, reply));

        protected virtual ConbotPromptCommandResult Prompt(string text, int timeout = 60000)
            => new(Context, text, timeout);
    }
}
