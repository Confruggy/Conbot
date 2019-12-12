using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Conbot.Commands.TypeReaders
{
    public class CommandTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            var commandService = services.GetRequiredService<CommandService>();

            var command = commandService.Commands
                .FirstOrDefault(c => c.Aliases
                    .Any(a => string.Equals(a, input, StringComparison.OrdinalIgnoreCase)));

            if (command != null)
                return Task.FromResult(TypeReaderResult.FromSuccess(command));

            string errorReason = "Command hasn't been found.";
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, errorReason));
        }
    }
}