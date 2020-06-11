using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
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
            ChannelPermissions = new ChannelPermission[0];
        }

        public RequireUserPermissionAttribute(params ChannelPermission[] permission)
        {
            ChannelPermissions = permission;
            GuildPermissions = new GuildPermission[0];
        }

        public override ValueTask<CheckResult> CheckAsync(CommandContext context)
        {
            var discordCommandContext = context as DiscordCommandContext;
            
            return RequirePermissionUtils.CheckPermissionsAsync(discordCommandContext.User,
                discordCommandContext.Channel, GuildPermissions, ChannelPermissions);
        }
    }
}