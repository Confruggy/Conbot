using System;
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
            ChannelPermissions = Array.Empty<ChannelPermission>();
        }

        public RequireBotPermissionAttribute(params ChannelPermission[] permission)
        {
            ChannelPermissions = permission;
            GuildPermissions = Array.Empty<GuildPermission>();
        }

        public override ValueTask<CheckResult> CheckAsync(CommandContext context)
        {
            var discordCommandContext = (DiscordCommandContext)context;

            if (discordCommandContext.Guild == null)
                return CheckResult.Unsuccessful("This command must be used in a server.");

            return RequirePermissionUtils.CheckPermissionsAsync(discordCommandContext.Guild.CurrentUser,
                discordCommandContext.Channel, GuildPermissions, ChannelPermissions);
        }
    }
}