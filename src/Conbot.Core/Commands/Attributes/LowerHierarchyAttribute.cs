using System.Threading.Tasks;
using Qmmands;
using Discord.WebSocket;

namespace Conbot.Commands
{
    public class LowerHierarchyAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            var discordCommandContext = (DiscordCommandContext)context;

            if (!(discordCommandContext.User is SocketGuildUser user))
                return CheckResult.Unsuccessful("This command must be used in a server.");

            var currentUser = discordCommandContext.Guild.CurrentUser;

            if ((argument is SocketRole role && user.Hierarchy > role.Position && currentUser.Hierarchy > role.Position) ||
                (argument is SocketGuildUser target && user.Hierarchy > target.Hierarchy &&
                    currentUser.Hierarchy > target.Hierarchy))
            {
                return CheckResult.Successful;
            }

            return CheckResult.Unsuccessful("Roles position is too high.");
        }
    }
}