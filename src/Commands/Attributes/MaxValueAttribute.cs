using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Conbot.Commands.Attributes
{
    public class MaxValueAttribute : ParameterPreconditionAttribute
    {
        public IComparable MaxValue { get; set; }

        public MaxValueAttribute(IComparable maxValue) => MaxValue = maxValue;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter,
            object value, IServiceProvider services)
        {
            if ((value as IComparable)?.CompareTo(MaxValue) <= 0)
                return Task.FromResult(PreconditionResult.FromSuccess());

            string message =
                $"Parameter {Format.Bold(parameter.Name)} must be less than or equal to {Format.Bold(MaxValue?.ToString())}.";

            return Task.FromResult(PreconditionResult.FromError(message));
        }
    }
}