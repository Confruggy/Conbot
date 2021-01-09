using System.Threading.Tasks;

using Discord.WebSocket;

using Qmmands;

namespace Conbot.Commands
{
    public class LowerHierarchyAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            var discordCommandContext = (DiscordCommandContext)context;

            if (discordCommandContext.Guild == null)
                return CheckResult.Unsuccessful("This command must be used in a server.");

            var user = (SocketGuildUser)discordCommandContext.User;
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