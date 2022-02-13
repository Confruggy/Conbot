using System;
using System.Threading.Tasks;

using Humanizer;

using Qmmands;

namespace Conbot.Commands;

public class MinValueAttribute : ParameterCheckAttribute
{
    public object MinValue { get; set; }

    public MinValueAttribute(object minValue) => MinValue = minValue;

    public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
    {
        return (argument as IComparable)?.CompareTo(Convert.ChangeType(MinValue, argument.GetType())) >= 0
            ? CheckResult.Successful
            : CheckResult.Failed($"{Parameter.Name.Humanize()} must be greater than or equal to {MinValue}.");
    }
}
