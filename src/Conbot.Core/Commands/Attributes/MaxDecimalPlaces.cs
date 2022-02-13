using System;
using System.Globalization;
using System.Threading.Tasks;

using Humanizer;

using Qmmands;

namespace Conbot.Commands;

public class MaxDecimalPlacesAttribute : ParameterCheckAttribute
{
    public int MaxDecimalPlaces { get; set; }

    public MaxDecimalPlacesAttribute(int maxDecimalPlaces) => MaxDecimalPlaces = maxDecimalPlaces;

    public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
    {
        if (argument is not decimal value)
            return CheckResult.Failed("This check must be used on a decimal parameter.");

        string text = value.ToString(CultureInfo.InvariantCulture);

        int decimalPlaces = text.Contains(".")
            ? text[(text.IndexOf(".", StringComparison.Ordinal) + 1)..].Length
            : 0;

        return decimalPlaces > MaxDecimalPlaces
            ? CheckResult.Failed($"{Parameter.Name.Humanize()} can't have more than {MaxDecimalPlaces} decimal places.")
            : CheckResult.Successful;
    }
}