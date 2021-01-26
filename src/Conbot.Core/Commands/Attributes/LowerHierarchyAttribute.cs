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

            if (argument is SocketRole role)
            {
                if (role.Position >= user.Hierarchy)
                {
                    return CheckResult.Unsuccessful("Role must be lower than your highest role.");
                }
                else if (role.Position >= currentUser.Hierarchy)
                {
                    return CheckResult.Unsuccessful("Role must be lower than the bot's highest role.");
                }
            }

            if (argument is SocketGuildUser target)
            {
                if (target.Hierarchy >= user.Hierarchy)
                {
                    return CheckResult.Unsuccessful(
                        "Member's highest role must be lower than your highest role.");
                }
                else if (target.Hierarchy >= currentUser.Hierarchy)
                {
                    return CheckResult.Unsuccessful(
                        "Member's highest role must be lower than the bot's highest role.");
                }
            }

            return CheckResult.Successful;
        }
    }
}
