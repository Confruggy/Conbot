using System;
using System.Threading.Tasks;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public class MaxValueAttribute : ParameterCheckAttribute
    {
        public object MaxValue { get; set; }

        public MaxValueAttribute(object maxValue) => MaxValue = maxValue;

        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            return (argument as IComparable)?.CompareTo(MaxValue) <= 0
                ? CheckResult.Successful
                : CheckResult.Failed($"{Parameter.Name.Humanize()} must be less than or equal to {MaxValue}.");
        }
    }
}