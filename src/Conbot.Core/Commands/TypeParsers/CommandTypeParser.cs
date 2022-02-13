using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Disqord.Bot;

using Qmmands;

namespace Conbot.Commands;

public class CommandTypeParser : TypeParser<Command>
{
    public override ValueTask<TypeParserResult<Command>> ParseAsync(Parameter parameter, string value,
        CommandContext context)
    {
        var bot = context.Services.GetRequiredService<DiscordBot>();

        var command = bot.Commands
            .GetAllCommands()
            .FirstOrDefault(c => c.FullAliases
                .Any(a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase)));

        return command is not null
            ? TypeParserResult<Command>.Successful(command)
            : TypeParserResult<Command>.Failed("Command hasn't been found.");
    }
}