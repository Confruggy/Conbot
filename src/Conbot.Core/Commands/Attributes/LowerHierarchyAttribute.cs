using System.Threading.Tasks;

using Disqord;
using Disqord.Gateway;

using Qmmands;

namespace Conbot.Commands;

public class LowerHierarchyAttribute : ParameterCheckAttribute
{
    public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
    {
        if (context is not ConbotGuildCommandContext discordCommandContext)
            return CheckResult.Failed("This command must be used in a server.");

        var author = discordCommandContext.Author;
        var currentMember = discordCommandContext.CurrentMember;

        return argument switch
        {
            IRole role when role.Position >= author.GetHierarchy() => CheckResult.Failed(
                "Role must be lower than your highest role."),
            IRole role when role.Position >= currentMember.GetHierarchy() => CheckResult.Failed(
                "Role must be lower than the bot's highest role."),
            IMember target when target.GetHierarchy() >= author.GetHierarchy() => CheckResult.Failed(
                "Member's highest role must be lower than your highest role."),
            IMember target when target.GetHierarchy() >= currentMember.GetHierarchy() => CheckResult.Failed(
                "Member's highest role must be lower than the bot's highest role."),
            _ => CheckResult.Successful
        };
    }
}
