using System;
using System.Linq;
using System.Threading.Tasks;

using Disqord.Bot;

using Microsoft.Extensions.DependencyInjection;

using Qmmands;

namespace Conbot.Commands
{
    public class ModuleTypeParser : TypeParser<Module>
    {
        public override ValueTask<TypeParserResult<Module>> ParseAsync(Parameter parameter, string value,
            CommandContext context)
        {
            var bot = context.Services.GetRequiredService<DiscordBot>();

            var module = bot.Commands
                .GetAllModules()
                .FirstOrDefault(x =>
                    (x.FullAliases.Count > 0 &&
                        string.Equals(x.FullAliases[0], value, StringComparison.OrdinalIgnoreCase)) ||
                        (x.Name is not null && string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase)));

            return module is not null
                ? TypeParserResult<Module>.Successful(module)
                : TypeParserResult<Module>.Failed("Group hasn't been found.");
        }
    }
}
