using System.Threading.Tasks;

using Disqord;
using Disqord.Gateway;

using Qmmands;

namespace Conbot.Commands
{
    public class LowerHierarchyAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            if (context is not ConbotGuildCommandContext discordCommandContext)
                return CheckResult.Failed("This command must be used in a server.");

            var author = discordCommandContext.Author;
            var currentMember = discordCommandContext.CurrentMember;

            if (argument is IRole role)
            {
                if (role.Position >= author.GetHierarchy())
                {
                    return CheckResult.Failed("Role must be lower than your highest role.");
                }
                else if (role.Position >= currentMember.GetHierarchy())
                {
                    return CheckResult.Failed("Role must be lower than the bot's highest role.");
                }
            }

            if (argument is IMember target)
            {
                if (target.GetHierarchy() >= author.GetHierarchy())
                {
                    return CheckResult.Failed(
                        "Member's highest role must be lower than your highest role.");
                }
                else if (target.GetHierarchy() >= currentMember.GetHierarchy())
                {
                    return CheckResult.Failed(
                        "Member's highest role must be lower than the bot's highest role.");
                }
            }

            return CheckResult.Successful;
        }
    }
}
