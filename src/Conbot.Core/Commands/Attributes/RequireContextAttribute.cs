using System.Threading.Tasks;

using Disqord.Bot;

using Qmmands;

namespace Conbot.Commands;

public class RequireGuildAttribute : DiscordCheckAttribute
{
    public override ValueTask<CheckResult> CheckAsync(DiscordCommandContext context)
    {
        return context.GuildId is null
            ? CheckResult.Failed("This command must be used in a server.")
            : CheckResult.Successful;
    }
}
