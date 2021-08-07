using System;
using System.Threading.Tasks;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public class MinLengthAttribute : ParameterCheckAttribute
    {
        public int Length { get; set; }

        public MinLengthAttribute(int length) => Length = length;

        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            if (argument is string text)
            {
                return text.Length >= Length
                    ? CheckResult.Successful
                    : CheckResult.Failed(
                        $"{Parameter.Name.Humanize()} must have at least {"character".ToQuantity(Length)}.");
            }
            else
            {
                return argument is Array array && array.Length >= Length
                    ? CheckResult.Successful
                    : CheckResult.Failed(
                        $"You must enter at least {Parameter.Name.ToQuantity(Length)}.");
            }
        }
    }
}