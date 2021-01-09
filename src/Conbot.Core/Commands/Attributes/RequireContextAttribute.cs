using System.Threading.Tasks;

using Discord;

using Qmmands;

namespace Conbot.Commands
{
    public class RequireContextAttribute : CheckAttribute
    {
        public ContextType Context { get; set; }

        public RequireContextAttribute(ContextType context) => Context = context;

        public override ValueTask<CheckResult> CheckAsync(CommandContext context)
        {
            var discordCommandContext = (DiscordCommandContext)context;

            if (Context == ContextType.Guild && discordCommandContext.Channel is not IGuildChannel)
                return CheckResult.Unsuccessful("This command must be used in a server.");

            if (Context == ContextType.DM && discordCommandContext.Channel is not IDMChannel)
                return CheckResult.Unsuccessful("This command must be used in a DM channel.");

            return CheckResult.Successful;
        }
    }
}