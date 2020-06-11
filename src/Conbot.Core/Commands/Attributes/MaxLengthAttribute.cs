using System;
using System.Threading.Tasks;
using Qmmands;
using Humanizer;

namespace Conbot.Commands
{
    public class MaxLengthAttribute : ParameterCheckAttribute
    {
        public int Length { get; set; }

        public MaxLengthAttribute(int length) => Length = length;

        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            return argument is string text
                ? text.Length <= Length
                    ? CheckResult.Successful
                    : CheckResult.Unsuccessful(
                        $"{Parameter.Name.Humanize()} can't be longer than {"character".ToQuantity(Length)}.")
                : argument is Array array && array.Length <= Length
                    ? CheckResult.Successful
                    : CheckResult.Unsuccessful(
                        $"You can't enter more than {Parameter.Name.ToQuantity(Length)}.");
        }
    }
}