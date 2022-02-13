using System;
using System.Threading.Tasks;

using Qmmands;

namespace Conbot.Commands;

public abstract class ConbotTypeParser<T> : TypeParser<T>
{
    public abstract ValueTask<TypeParserResult<T>> ParseAsync(Parameter parameter, string value,
        ConbotCommandContext context);

    public sealed override ValueTask<TypeParserResult<T>> ParseAsync(Parameter parameter, string value,
        CommandContext context)
    {
        if (context is not ConbotCommandContext conbotCommandContext)
            throw new InvalidOperationException($"The {GetType().Name} only accepts a ConbotCommandContext.");

        return ParseAsync(parameter, value, conbotCommandContext);
    }
}