using System.Threading.Tasks;

using Disqord.Bot;

namespace Conbot.Commands;

public class ConbotFailedCommandResult : DiscordCommandResult
{
    public string FailureReason { get; }

    internal ConbotFailedCommandResult(ConbotCommandContext context, string failureReason)
        : base(context)
    {
        FailureReason = failureReason;
    }

    public override async Task ExecuteAsync()
    {
        var context = (ConbotCommandContext)Context;
        await context.Bot.HandleFailedResultAsyncInternal(context, new RuntimeFailedResult(Command, FailureReason));
    }
}