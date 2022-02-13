using System;
using System.Threading.Tasks;

using Humanizer;

using Qmmands;

namespace Conbot.Commands;

public class MaxLengthAttribute : ParameterCheckAttribute
{
    public int Length { get; set; }

    public MaxLengthAttribute(int length) => Length = length;

    public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
    {
        if (argument is string text)
        {
            return text.Length <= Length
                ? CheckResult.Successful
                : CheckResult.Failed(
                    $"{Parameter.Name.Humanize()} can't be longer than {"character".ToQuantity(Length)}.");
        }

        return argument is Array array && array.Length <= Length
            ? CheckResult.Successful
            : CheckResult.Failed(
                $"You can't enter more than {Parameter.Name.ToQuantity(Length)}.");
    }
}