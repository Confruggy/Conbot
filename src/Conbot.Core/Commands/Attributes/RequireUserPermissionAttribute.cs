using System;
using System.Threading.Tasks;

using Discord;

using Qmmands;

namespace Conbot.Commands
{
    public class RequireUserPermissionAttribute : CheckAttribute
    {
        public GuildPermission[] GuildPermissions { get; }
        public ChannelPermission[] ChannelPermissions { get; }

        public RequireUserPermissionAttribute(params GuildPermission[] permission)
        {
            GuildPermissions = permission;
            ChannelPermissions = Array.Empty<ChannelPermission>();
        }

        public RequireUserPermissionAttribute(params ChannelPermission[] permission)
        {
            ChannelPermissions = permission;
            GuildPermissions = Array.Empty<GuildPermission>();
        }

        public override ValueTask<CheckResult> CheckAsync(CommandContext context)
        {
            var discordCommandContext = (DiscordCommandContext)context;

            if (discordCommandContext.User is not IGuildUser guildUser)
                return CheckResult.Unsuccessful("This command must be used in a server.");

            return RequirePermissionUtils.CheckPermissionsAsync(guildUser, discordCommandContext.Channel,
                GuildPermissions, ChannelPermissions);
        }
    }
}