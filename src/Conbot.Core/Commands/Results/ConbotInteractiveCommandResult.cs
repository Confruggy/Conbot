using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Conbot.Interactive;

using Disqord.Bot;

namespace Conbot.Commands;

public class ConbotInteractiveCommandResult : DiscordCommandResult
{
    public LocalInteractiveMessage Message { get; }

    public ConbotInteractiveCommandResult(ConbotCommandContext context, LocalInteractiveMessage message)
        : base(context)
    {
        Message = message;
    }

    public override async Task ExecuteAsync()
    {
        var interactiveService = Context.Services.GetRequiredService<InteractiveService>();
        await interactiveService.ExecuteInteractiveMessageAsync(Message, (ConbotCommandContext)Context);
    }
}