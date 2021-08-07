using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Conbot.Interactive;

using Disqord;
using Disqord.Bot;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public class InteractiveArgumentParser : IArgumentParser
    {
        public async ValueTask<ArgumentParserResult> ParseAsync(CommandContext context)
        {
            if (context is not ConbotCommandContext conbotCommandContext)
                return ConbotArgumentParserResult.Failed("Invalid context.");

            if (!string.IsNullOrEmpty(context.RawArguments))
            {
                var bot = context.Services.GetRequiredService<DiscordBot>();
                return await bot.Commands.DefaultArgumentParser.ParseAsync(context);
            }

            var config = context.Services.GetRequiredService<IConfiguration>();
            var interactiveService = context.Services.GetRequiredService<InteractiveService>();

            var arguments = new Dictionary<Parameter, object?>();

            foreach (var parameter in context.Command.Parameters)
            {
                var text = new StringBuilder();

                var lastEntered = arguments.Keys.LastOrDefault();

                if (lastEntered is not null)
                {
                    text
                        .Append(lastEntered.Name.Humanize())
                        .Append(" has been entered. Now enter ");
                }
                else
                {
                    text.Append("Enter ");
                }

                if (!string.IsNullOrEmpty(parameter.Description))
                {
                    text
                        .Append(char.ToLower(parameter.Description[0]))
                        .Append(parameter.Description[1..]);
                }
                else
                {
                    text
                        .Append(parameter.Name)
                        .Append('.');
                }

                string? argument = null;
                bool skipped = false;

                var message = new LocalInteractiveMessage()
                    .WithContent(text.ToString())
                    .WithReply(conbotCommandContext.Message!.Id, conbotCommandContext.Message.ChannelId,
                        conbotCommandContext.Message.GuildId)
                    .WithAllowedMentions(LocalAllowedMentions.None)
                    .WithPrecondition(x => x.Id == conbotCommandContext.Author.Id);

                if (parameter.IsOptional)
                {
                    text
                        .Append(" Click on <:")
                        .Append(config.GetValue<string>("Emotes:MediumSkip"))
                        .Append("> to skip this parameter or on <:")
                        .Append(config.GetValue<string>("Emotes:MediumCrossMark"))
                        .Append("> to cancel the command.");

                    message.AddReactionCallback(config.GetValue<string>("Emotes:Skip"), x => x
                        .WithCallback((msg, _) =>
                        {
                            skipped = true;
                            msg.Stop();
                        }));
                }
                else
                {
                    text
                        .Append(" Click on <:")
                        .Append(config.GetValue<string>("Emotes:MediumCrossMark"))
                        .Append("> to cancel the command.");
                }

                message
                    .AddReactionCallback(config.GetValue<string>("Emotes:CrossMark"), x => x
                        .WithCallback((msg, _) => msg.Stop()))
                    .AddMessageCallback(x => x
                        .WithCallback((msg, e) =>
                        {
                            conbotCommandContext.AddMessage((IUserMessage)e.Message);
                            argument = e.Message.Content;
                            msg.Stop();
                        }));

                var response = await interactiveService.ExecuteInteractiveMessageAsync(message, conbotCommandContext);

                if (argument is null && !skipped)
                    return ConbotArgumentParserResult.Failed("Aborted");

                arguments.Add(parameter, argument);
            }

            return ConbotArgumentParserResult.Successful(arguments);
        }
    }
}
