using System.Threading.Tasks;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public class NotEmptyAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
            => !string.IsNullOrWhiteSpace(argument?.ToString())
                ? CheckResult.Successful
                : CheckResult.Unsuccessful($"{Parameter.Name.Humanize()} can't be empty.");
    }
}