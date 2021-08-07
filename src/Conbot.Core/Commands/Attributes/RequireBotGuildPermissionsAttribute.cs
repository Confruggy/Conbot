using System.Threading.Tasks;

using Disqord;
using Disqord.Bot;

using Qmmands;

namespace Conbot.Commands
{
    public class RequireBotGuildPermissionsAttribute : DiscordCheckAttribute
    {
        public Permission[] Permissions { get; }

        public RequireBotGuildPermissionsAttribute(params Permission[] permission) => Permissions = permission;

        public override ValueTask<CheckResult> CheckAsync(DiscordCommandContext context)
        {
            if (context is not ConbotGuildCommandContext discordGuildCommandContext)
                return CheckResult.Failed("This command must be used in a server.");

            return RequirePermissionUtils.CheckGuildPermissionsAsync(discordGuildCommandContext.CurrentMember,
                Permissions);
        }
    }
}
