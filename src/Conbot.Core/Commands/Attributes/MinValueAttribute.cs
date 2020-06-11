using System;
using System.Threading.Tasks;
using Qmmands;
using Humanizer;

namespace Conbot.Commands
{
    public class MinValueAttribute : ParameterCheckAttribute
    {
        public object MinValue { get; set; }

        public MinValueAttribute(object minValue) => MinValue = minValue;

        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            return (argument as IComparable)?.CompareTo(MinValue) >= 0
                ? CheckResult.Successful
                : CheckResult.Unsuccessful($"{Parameter.Name.Humanize()} must be greater than or equal to {MinValue}.");
        }
    }
}