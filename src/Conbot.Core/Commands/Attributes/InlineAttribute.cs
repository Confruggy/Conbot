using System.Threading.Tasks;

using Humanizer;

using Qmmands;

namespace Conbot.Commands;

public class InlineAttribute : ParameterCheckAttribute
{
    public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
    {
        if (argument is string text && text.Contains('\n'))
            return CheckResult.Failed($"{Parameter.Name.Humanize()} can't contain line breaks.");

        return CheckResult.Successful;
    }
}