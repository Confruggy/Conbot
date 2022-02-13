using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Disqord;
using Disqord.Gateway;

using Humanizer;

using Qmmands;

namespace Conbot.Commands;

public static class RequirePermissionUtils
{
    public static ValueTask<CheckResult> CheckPermissionsAsync(IMember member, IMessageChannel channel,
        Permission[] guildPermissions, Permission[] channelPermissions)
    {
        if (guildPermissions.Length != 0)
            return CheckGuildPermissionsAsync(member, guildPermissions);

        if (channelPermissions.Length != 0)
            return CheckChannelPermissionsAsync(member, channel, channelPermissions);

        return CheckResult.Successful;
    }

    public static ValueTask<CheckResult> CheckGuildPermissionsAsync(IMember member,
        Permission[] permissions)
    {
        var guildPermissions = member.GetPermissions();

        return permissions.Any(x => guildPermissions.Has(x))
            ? CheckResult.Successful
            : CheckResult.Failed(CreateRequirePermissionErrorReason(permissions, member.IsBot));
    }

    public static ValueTask<CheckResult> CheckChannelPermissionsAsync(IMember member,
        IMessageChannel channel, Permission[] permissions)
    {
        var channelPermissions = channel is IGuildChannel guildChannel
            ? member.GetPermissions(guildChannel)
            : ChannelPermissions.All;

        return permissions.Any(x => channelPermissions.Has(x))
            ? CheckResult.Successful
            : CheckResult.Failed(CreateRequirePermissionErrorReason(permissions, member.IsBot));
    }

    public static string CreateRequirePermissionErrorReason<TEnum>(TEnum[] permissions,
        bool isBot = false)
        where TEnum : struct
    {
        string permissionsText = permissions
            .Select(x =>
                x.ToString()!.Split(',').Humanize(s => $"**{s.Titleize()}**").Replace("Guild", "Server"))
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
