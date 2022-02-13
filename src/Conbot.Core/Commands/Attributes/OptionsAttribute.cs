using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Humanizer;

using Qmmands;

namespace Conbot.Commands;

public class ChoicesAttribute : ParameterCheckAttribute
{
    public object[] Choices { get; set; }

    public ChoicesAttribute(params object[] choices) => Choices = choices;

    public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
    {
        var commandService = context.Services.GetRequiredService<CommandService>();

        bool contains = argument is string argumentString
            ? Choices.Any(x => x.ToString()?.Equals(argumentString, commandService.StringComparison) == true)
            : Choices.Contains(argument);

        if (contains)
            return CheckResult.Successful;

        return CheckResult.Failed(
            $"The available choices are {Choices.Select(x => $"**{x}**").Humanize("and")}.");
    }
}