using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Conbot.Commands.TypeReaders
{
    public class ModuleTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            var commandService = services.GetRequiredService<CommandService>();

            var command = commandService.Modules
                    .FirstOrDefault(x =>
                        (x.Group != null && string.Equals(x.Group, input, StringComparison.OrdinalIgnoreCase)) ||
                        (x.Name != null && string.Equals(x.Name, input, StringComparison.OrdinalIgnoreCase)));

            if (command != null)
                return Task.FromResult(TypeReaderResult.FromSuccess(command));

            string errorReason = "Command hasn't been found.";
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, errorReason));
        }
    }
}