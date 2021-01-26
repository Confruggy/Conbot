using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Qmmands;

namespace Conbot.Commands
{
    public class ModuleTypeParser : TypeParser<Module>
    {
        public override ValueTask<TypeParserResult<Module>> ParseAsync(Parameter parameter, string value,
            CommandContext context)
        {
            var commandService = context.ServiceProvider.GetRequiredService<CommandService>();

            var module = commandService
                .GetAllModules()
                .FirstOrDefault(x =>
                    (x.FullAliases.Count > 0 &&
                        string.Equals(x.FullAliases[0], value, StringComparison.OrdinalIgnoreCase)) ||
                        (x.Name != null && string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase)));

            return module != null
                ? TypeParserResult<Module>.Successful(module)
                : TypeParserResult<Module>.Unsuccessful("Group hasn't been found.");
        }
    }
}
