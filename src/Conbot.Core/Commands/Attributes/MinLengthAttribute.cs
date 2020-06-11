using System;
using System.Threading.Tasks;
using Qmmands;
using Humanizer;

namespace Conbot.Commands
{
    public class MinLengthAttribute : ParameterCheckAttribute
    {
        public int Length { get; set; }

        public MinLengthAttribute(int length) => Length = length;

        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            return argument is string text
                ? text.Length >= Length
                    ? CheckResult.Successful
                    : CheckResult.Unsuccessful(
                        $"{Parameter.Name.Humanize()} must have at least {"character".ToQuantity(Length)}.")
                : argument is Array array && array.Length >= Length
                    ? CheckResult.Successful
                    : CheckResult.Unsuccessful(
                        $"You must enter at least {Parameter.Name.ToQuantity(Length)}.");
        }
    }
}