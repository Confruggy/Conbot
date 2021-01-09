using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Qmmands;

namespace Conbot.Commands
{
    public class CommandTypeParser : TypeParser<Command>
    {
        public override ValueTask<TypeParserResult<Command>> ParseAsync(Parameter parameter, string value,
            CommandContext context)
        {
            var commandService = context.ServiceProvider.GetRequiredService<CommandService>();

            var command = commandService.GetAllCommands()
                .FirstOrDefault(c => c.FullAliases
                    .Any(a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase)));

            return command != null
                ? TypeParserResult<Command>.Successful(command)
                : TypeParserResult<Command>.Unsuccessful("Command hasn't been found.");
        }
    }
}