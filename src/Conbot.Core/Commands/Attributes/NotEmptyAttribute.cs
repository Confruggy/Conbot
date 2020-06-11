using System.Threading.Tasks;
using Qmmands;
using Humanizer;

namespace Conbot.Commands
{
    public class NotEmptyAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            return !string.IsNullOrWhiteSpace(argument?.ToString())
                ? CheckResult.Successful
                : CheckResult.Unsuccessful($"{Parameter.Name.Humanize()} can't be empty.");
        }
    }
}