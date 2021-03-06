using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Discord;

using Qmmands;

namespace Conbot.Commands
{
    public class RoleTypeParser<T> : TypeParser<T> where T : class, IRole
    {
        public override ValueTask<TypeParserResult<T>> ParseAsync(Parameter parameter, string value,
            CommandContext context)
        {
            var discordCommandContext = (DiscordCommandContext)context;

            if (discordCommandContext.Guild == null)
                return TypeParserResult<T>.Unsuccessful("This command must be used in a server.");

            IRole? role = null;

            if (MentionUtils.TryParseRole(value, out ulong id))
            {
                role = discordCommandContext.Guild.GetRole(id);
            }
            else if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                role = discordCommandContext.Guild.GetRole(id);
            }
            else
            {
                var roles = discordCommandContext.Guild.Roles;
                var foundRoles = roles.Where(x => string.Equals(value, x.Name, StringComparison.OrdinalIgnoreCase));

                if (foundRoles.Count() > 1)
                {
                    return TypeParserResult<T>.Unsuccessful(
                        "Role name is ambiguous. Try mentioning the role or enter the ID.");
                }

                role = foundRoles.FirstOrDefault();
            }

            if (role?.Id == discordCommandContext.Guild.Id)
                return TypeParserResult<T>.Unsuccessful("You can't enter the **@\u200beveryone** role.");

            return role is T tRole
                ? TypeParserResult<T>.Successful(tRole)
                : TypeParserResult<T>.Unsuccessful("Role hasn't been found.");
        }
    }
}
