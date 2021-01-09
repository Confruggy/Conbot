using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public class ChoicesAttribute : ParameterCheckAttribute
    {
        public object[] Choices { get; set; }

        public ChoicesAttribute(params object[] choices) => Choices = choices;

        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            var commandService = context.ServiceProvider.GetRequiredService<CommandService>();

            bool contains = (argument is string argumentString)
                ? Choices.Any(x => x.ToString()!.Equals(argumentString, commandService.StringComparison))
                : Choices.Contains(argument);

            if (contains)
                return CheckResult.Successful;

            return CheckResult.Unsuccessful(
                $"The available choices are {Choices.Select(x => Format.Bold(x.ToString())).Humanize("and")}.");
        }
    }
}