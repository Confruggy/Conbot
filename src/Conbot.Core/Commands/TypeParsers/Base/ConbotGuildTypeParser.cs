using System;
using System.Threading.Tasks;

using Qmmands;

namespace Conbot.Commands;

public abstract class ConbotGuildTypeParser<T> : ConbotTypeParser<T>
{
    public abstract ValueTask<TypeParserResult<T>> ParseAsync(Parameter parameter, string value,
        ConbotGuildCommandContext context);

    public sealed override ValueTask<TypeParserResult<T>> ParseAsync(Parameter parameter, string value,
        ConbotCommandContext context)
    {
        if (context.GuildId is null)
            return Failure("This command must be used in a server.");

        if (context is not ConbotGuildCommandContext conbotGuildCommandContext)
            throw new InvalidOperationException($"The {GetType().Name} only accepts a ConbotGuildCommandContext.");

        return ParseAsync(parameter, value, conbotGuildCommandContext);
    }
}
