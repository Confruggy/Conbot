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

            var module = commandService.GetAllModules()
                    .FirstOrDefault(x =>
                        (x.FullAliases.FirstOrDefault() != null && string.Equals(x.FullAliases.First(), value,
                            StringComparison.OrdinalIgnoreCase)) ||
                        (x.Name != null && string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase)));

            return module != null
                ? TypeParserResult<Module>.Successful(module)
                : TypeParserResult<Module>.Unsuccessful("Module hasn't been found.");
        }
    }
}