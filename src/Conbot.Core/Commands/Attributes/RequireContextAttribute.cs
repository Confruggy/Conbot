using System.Threading.Tasks;

using Disqord.Bot;

using Qmmands;

namespace Conbot.Commands
{
    public class RequireGuildAttribute : DiscordCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(DiscordCommandContext context)
        {
            if (context.GuildId is null)
                return CheckResult.Failed("This command must be used in a server.");

            return CheckResult.Successful;
        }
    }
}