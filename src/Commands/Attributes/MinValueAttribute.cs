using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Conbot.Commands.Attributes
{
    public class MinValueAttribute : ParameterPreconditionAttribute
    {
        public object MinValue { get; set; }

        public MinValueAttribute(object minValue) => MinValue = minValue;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter,
            object value, IServiceProvider services)
        {
            if ((value as IComparable)?.CompareTo(MinValue) >= 0)
                return Task.FromResult(PreconditionResult.FromSuccess());

            string message =
                $"Parameter {Format.Bold(parameter.Name)} must be greater than or equal to {Format.Bold(MinValue?.ToString())}.";

            return Task.FromResult(PreconditionResult.FromError(message));
        }
    }
}