using System;
using System.Threading.Tasks;

using Disqord;
using Disqord.Gateway;

using Qmmands;

namespace Conbot.Commands
{
    public class RoleTypeParser : ConbotGuildTypeParser<IRole>
    {
        public override ValueTask<TypeParserResult<IRole>> ParseAsync(Parameter parameter, string value,
            ConbotGuildCommandContext context)
        {
            if (!context.Bot.CacheProvider.TryGetRoles(context.GuildId, out var roleCache))
                throw new InvalidOperationException($"The {GetType().Name} requires the role cache.");

            IRole? role;

            if (Snowflake.TryParse(value, out var id) || Mention.TryParseRole(value, out id))
                role = roleCache.GetValueOrDefault(id);
            else
                role = Array.Find(roleCache.Values, x => x.Name == value);

            if (role is not null)
                return Success(role);

            return Failure("No role found matching the input.");
        }
    }
}
