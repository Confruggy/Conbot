using System;
using System.Threading.Tasks;
using Discord.Commands;
using Humanizer;

namespace Conbot.Commands.Attributes
{
    public class NotEmptyAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter,
            object value, IServiceProvider services)
        {
            if (!string.IsNullOrWhiteSpace(value?.ToString()))
                return Task.FromResult(PreconditionResult.FromSuccess());

            string message = $"{parameter.Name.Humanize()} can't be empty.";
            return Task.FromResult(PreconditionResult.FromError(message));
        }
    }
}