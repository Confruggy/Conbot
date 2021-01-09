using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public static class RequirePermissionUtils
    {
        public static ValueTask<CheckResult> CheckPermissionsAsync(IGuildUser user, IMessageChannel channel,
            GuildPermission[] guildPermissions, ChannelPermission[] channelPermissions)
        {
            if (guildPermissions.Length != 0)
                return CheckPermissionsAsync(user, guildPermissions);

            if (channelPermissions.Length != 0)
                return CheckPermissionsAsync(user, channel, channelPermissions);

            return CheckResult.Successful;
        }

        public static ValueTask<CheckResult> CheckPermissionsAsync(IGuildUser user,
            GuildPermission[] permissions)
        {
            if (permissions.Any(x => user.GuildPermissions.Has(x)))
                return CheckResult.Successful;

            return CheckResult.Unsuccessful(CreateRequirePermissionErrorReason(permissions, user.IsBot));
        }

        public static ValueTask<CheckResult> CheckPermissionsAsync(IGuildUser user, IMessageChannel channel,
            ChannelPermission[] permissions)
        {
            ChannelPermissions channelPermissions;

            if (channel is IGuildChannel guildChannel)
                channelPermissions = user.GetPermissions(guildChannel);
            else
                channelPermissions = ChannelPermissions.All(channel);

            if (permissions.Any(x => channelPermissions.Has(x)))
                return CheckResult.Successful;

            return CheckResult.Unsuccessful(CreateRequirePermissionErrorReason(permissions, user.IsBot));
        }

        public static string CreateRequirePermissionErrorReason<TEnum>(TEnum[] permissions,
            bool isBot = false)
            where TEnum : struct
        {
            string permissionsText = permissions
                .Select(x =>
                    x.ToString()!.Split(',').Humanize(s => Format.Bold(s.Titleize())).Replace("Guild", "Server"))
                .Humanize("or");

            bool isPlural = permissions.Length > 1 || GetSetBitCount((ulong)(object)permissions[0]) > 1;
            return new StringBuilder()
                .Append(isBot ? "The bot" : "You")
                .Append(" require")
                .Append(isBot ? "s" : "")
                .Append(" the ")
                .Append(typeof(TEnum).Name
                    .Replace("Guild", "Server")
                    .Humanize(LetterCasing.LowerCase)
                    .ToQuantity(isPlural ? 2 : 1, ShowQuantityAs.None))
                .Append(' ')
                .Append(permissionsText)
                .Append(" to use this command.")
                .ToString();
        }

        public static int GetSetBitCount(ulong value)
        {
            int i = 0;
            while (value != 0)
            {
                value &= value - 1;
                i++;
            }

            return i;
        }
    }
}