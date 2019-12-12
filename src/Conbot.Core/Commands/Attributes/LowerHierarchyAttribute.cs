using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Conbot.Commands.Attributes
{
    public class LowerHierarchyAttribute : ParameterPreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            ParameterInfo parameter, object value, IServiceProvider services)
        {
            if (!(context.User is SocketGuildUser user))
                return PreconditionResult.FromError("This command must be used in a server.");

            var currentUser = await context.Guild.GetCurrentUserAsync() as SocketGuildUser;

            if ((value is SocketRole role && user.Hierarchy > role.Position && currentUser.Hierarchy > role.Position) ||
                (value is SocketGuildUser target && user.Hierarchy > target.Hierarchy &&
                    currentUser.Hierarchy > target.Hierarchy))
                return PreconditionResult.FromSuccess();

            return PreconditionResult.FromError("Roles position is too high.");
        }
    }
}