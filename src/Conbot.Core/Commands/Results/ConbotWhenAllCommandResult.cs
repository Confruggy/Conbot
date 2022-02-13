using System.Linq;
using System.Threading.Tasks;

using Disqord.Bot;

namespace Conbot.Commands;

public class ConbotWhenAllCommandResult : DiscordCommandResult
{
    public DiscordCommandResult Result { get; }
    public Task[] Tasks { get; }

    public ConbotWhenAllCommandResult(DiscordCommandResult result, params Task[] tasks)
        : base(result.Context)
    {
        Result = result;
        Tasks = tasks;
    }

    public override Task ExecuteAsync()
        => Task.WhenAll(Tasks.Append(Result.ExecuteAsync()));
}
