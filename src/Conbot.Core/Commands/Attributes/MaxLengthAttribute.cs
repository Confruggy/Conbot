using System;
using System.Threading.Tasks;
using Discord.Commands;
using Humanizer;

namespace Conbot.Commands.Attributes
{
    public class MaxLengthAttribute : ParameterPreconditionAttribute
    {
        public int Length { get; set; }

        public MaxLengthAttribute(int length) => Length = length;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter,
            object value, IServiceProvider services)
        {
            if (value is string text)
            {
                if (text.Length <= Length)
                    return Task.FromResult(PreconditionResult.FromSuccess());

                string message = $"{parameter.Name.Humanize()} can't be longer than {"character".ToQuantity(Length)}.";
                return Task.FromResult(PreconditionResult.FromError(message));
            }
            else if (value is Array array && array.Length <= Length)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                string message = $"You can't enter more than {parameter.Name.ToQuantity(Length)}.";
                return Task.FromResult(PreconditionResult.FromError(message));
            }
        }
    }
}