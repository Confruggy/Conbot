using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Conbot.Commands;

using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;

using Humanizer;

using Qmmands;

namespace Conbot
{
    public partial class ConbotBot
    {
        public override DiscordCommandContext CreateCommandContext(IPrefix prefix, string input,
            IGatewayUserMessage message, CachedMessageGuildChannel channel)
        {
            var scope = Services.CreateScope();
            var context = message.GuildId is not null
                ? new ConbotGuildCommandContext(this, prefix, input, message, channel, scope)
                : new ConbotCommandContext(this, prefix, input, message, scope);
            context.Services.GetRequiredService<ICommandContextAccessor>().Context = context;
            return context;
        }

        public new string FormatFailureReason(DiscordCommandContext context, FailedResult result)
        {
            switch (result)
            {
                case ArgumentParseFailedResult argumentParserFailedResult:
                    {
                        var commandParameters = argumentParserFailedResult.Command.Parameters.Where(x => !x.IsOptional);
                        var parsedParameters = argumentParserFailedResult.ParserResult.Arguments
                            .Select(x => x.Key)
                            .Where(x => !x.IsOptional);

                        if (commandParameters.Count() > parsedParameters.Count())
                        {
                            var missingParameters = commandParameters.Except(parsedParameters);

                            return new StringBuilder()
                                .Append("Required ")
                                .Append("parameter".ToQuantity(missingParameters.Count(), ShowQuantityAs.None))
                                .Append(' ')
                                .Append(missingParameters.Humanize(x => $"**{x.Name}**"))
                                .Append(' ')
                                .Append("is".ToQuantity(missingParameters.Count(), ShowQuantityAs.None))
                                .Append(" missing.")
                                .ToString();
                        }

                        break;
                    }
                case ChecksFailedResult checksFailedResult:
                    {
                        var failedChecks = checksFailedResult.FailedChecks;

                        if (failedChecks.Count == 1)
                            return failedChecks[0].Result.FailureReason;

                        var text = new StringBuilder().AppendLine("Several checks failed:");

                        for (int i = 0; i < failedChecks.Count; i++)
                        {
                            var checkResult = failedChecks[i].Result;

                            text
                                .Append('`')
                                .Append(i + 1)
                                .Append(".` ")
                                .AppendLine(checkResult.FailureReason);
                        }

                        return text.ToString();
                    }
                case ParameterChecksFailedResult parameterChecksFailedResult:
                    {
                        var failedChecks = parameterChecksFailedResult.FailedChecks;

                        if (failedChecks.Count == 1)
                            return failedChecks[0].Result.FailureReason;

                        var text = new StringBuilder().AppendLine("Several parameter checks failed:");

                        for (int i = 0; i < failedChecks.Count; i++)
                        {
                            var checkResult = failedChecks[i].Result;

                            text
                                .Append('`')
                                .Append(i + 1)
                                .Append(".` ")
                                .AppendLine(checkResult.FailureReason);
                        }

                        return text.ToString();
                    }
            }

            return result.FailureReason;
        }

        protected override ValueTask HandleFailedResultAsync(DiscordCommandContext context, FailedResult result)
            => HandleFailedResultAsyncInternal((ConbotCommandContext)context, result);

        internal async ValueTask HandleFailedResultAsyncInternal(ConbotCommandContext context, FailedResult result)
        {
            var handler = Services.GetService<ICommandFailedHandler>();

            if (handler is not null)
            {
                await handler.HandleFailedResultAsync(context, result);
                return;
            }

            var message = FormatFailureMessage(context, result);

            if (message is null)
                return;

            await this.SendMessageAsync(context.ChannelId, message);
        }
    }
}
