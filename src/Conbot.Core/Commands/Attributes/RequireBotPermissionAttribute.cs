using System.Threading.Tasks;
using Discord;
using Qmmands;

namespace Conbot.Commands
{
    public class RequireBotPermissionAttribute : CheckAttribute
    {
        public GuildPermission[] GuildPermissions { get; }
        public ChannelPermission[] ChannelPermissions { get; }

        public RequireBotPermissionAttribute(params GuildPermission[] permission)
        {
            GuildPermissions = permission;
            ChannelPermissions = new ChannelPermission[0];
        }

        public RequireBotPermissionAttribute(params ChannelPermission[] permission)
        {
            ChannelPermissions = permission;
            GuildPermissions = new GuildPermission[0];
        }

        public override ValueTask<CheckResult> CheckAsync(CommandContext context)
        {
            var discordCommandContext = context as DiscordCommandContext;

            return RequirePermissionUtils.CheckPermissionsAsync(discordCommandContext.Guild?.CurrentUser,
                discordCommandContext.Channel, GuildPermissions, ChannelPermissions);
        }
    }
}