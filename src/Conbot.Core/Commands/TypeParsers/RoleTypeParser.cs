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
            var discordCommandContext = context as DiscordCommandContext;

            if (discordCommandContext.Guild == null)
                return TypeParserResult<T>.Unsuccessful("This command must be used in a server.");

            IRole role = null;

            

            if (MentionUtils.TryParseRole(value, out var id))
                role = discordCommandContext.Guild.GetRole(id);
            else if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                role = discordCommandContext.Guild.GetRole(id);
            else 
            {
                var roles = discordCommandContext.Guild.Roles;
                var foundRoles = roles.Where(x => string.Equals(value, x.Name, StringComparison.OrdinalIgnoreCase));

                if (foundRoles.Count() > 1)
                    return TypeParserResult<T>.Unsuccessful(
                        "Role name is ambiguous. Try mentioning the role or enter the id.");
                
                role = foundRoles.FirstOrDefault();
            }

            var tRole = role as T;

            return tRole != null
                ? TypeParserResult<T>.Successful(tRole)
                : TypeParserResult<T>.Unsuccessful("Role hasn't been found.");
        }
    }
}