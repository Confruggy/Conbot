using System;
using System.Threading.Tasks;
using Discord.Commands;
using Humanizer;

namespace Conbot.Commands.Attributes
{
    public class MaxValueAttribute : ParameterPreconditionAttribute
    {
        public object MaxValue { get; set; }

        public MaxValueAttribute(object maxValue) => MaxValue = maxValue;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter,
            object value, IServiceProvider services)
        {
            if ((value as IComparable)?.CompareTo(MaxValue) <= 0)
                return Task.FromResult(PreconditionResult.FromSuccess());

            string message = $"{parameter.Name.Humanize()} must be less than or equal to {MaxValue}.";
            return Task.FromResult(PreconditionResult.FromError(message));
        }
    }
}