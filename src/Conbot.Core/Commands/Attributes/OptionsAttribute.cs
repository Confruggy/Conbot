using System;
using System.Threading.Tasks;
using Qmmands;
using Humanizer;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Discord;

namespace Conbot.Commands
{
    public class OptionsAttribute : ParameterCheckAttribute
    {
        public object[] Options { get; set; }

        public OptionsAttribute(params object[] options) => Options = options;

        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            var commandService = context.ServiceProvider.GetRequiredService<CommandService>();

            bool contains = argument.GetType() == typeof(string)
                ? Options.Any(x => x.ToString().Equals((string)argument, commandService.StringComparison))
                : Options.Contains(argument);

            if (contains)
                return CheckResult.Successful;

            return CheckResult.Unsuccessful(
                $"The available options are {Options.Select(x => Format.Bold(x.ToString())).Humanize("or")}.");
        }
    }
}