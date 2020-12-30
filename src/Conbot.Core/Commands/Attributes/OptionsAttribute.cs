using System;
using System.Threading.Tasks;
using Qmmands;
using Humanizer;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Discord;

namespace Conbot.Commands
{
    public class ChoicesAttribute : ParameterCheckAttribute
    {
        public object[] Choices { get; set; }

        public ChoicesAttribute(params object[] choices) => Choices = choices;

        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            var commandService = context.ServiceProvider.GetRequiredService<CommandService>();

            bool contains = argument.GetType() == typeof(string)
                ? Choices.Any(x => x.ToString().Equals((string)argument, commandService.StringComparison))
                : Choices.Contains(argument);

            if (contains)
                return CheckResult.Successful;

            return CheckResult.Unsuccessful(
                $"The available choices are {Choices.Select(x => Format.Bold(x.ToString())).Humanize("and")}.");
        }
    }
}