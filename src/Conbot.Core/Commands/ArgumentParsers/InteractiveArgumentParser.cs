using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Conbot.Interactive;

using Discord;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public class InteractiveArgumentParser : IArgumentParser
    {
        public async ValueTask<ArgumentParserResult> ParseAsync(CommandContext context)
        {
            if (context is not DiscordCommandContext discordCommandContext)
                return ConbotArgumentParserResult.Failed("Invalid context.");

            if (discordCommandContext.Interaction != null || !string.IsNullOrEmpty(context.RawArguments))
            {
                var commandService = context.ServiceProvider.GetRequiredService<CommandService>();
                return await commandService.DefaultArgumentParser.ParseAsync(context);
            }

            var config = context.ServiceProvider.GetRequiredService<IConfiguration>();
            var interactiveService = context.ServiceProvider.GetRequiredService<InteractiveService>();

            var arguments = new Dictionary<Parameter, object?>();

            foreach (var parameter in context.Command.Parameters)
            {
                var text = new StringBuilder();

                var lastEntered = arguments.Keys.LastOrDefault();

                if (lastEntered != null)
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

                var interactiveMessage =
                    new InteractiveMessageBuilder()
                        .WithPrecondition(x => x.Id == discordCommandContext.User.Id);

                if (parameter.IsOptional)
                {
                    text
                        .Append(" Click on ")
                        .Append(config.GetValue<string>("Emotes:CheckMark"))
                        .Append(" to skip this parameter.");

                    interactiveMessage.AddReactionCallback(config.GetValue<string>("Emotes:CheckMark"), x => x
                        .ShouldResumeAfterExecution(false)
                        .WithCallback(_ => skipped = true));
                }

                interactiveMessage
                    .AddReactionCallback(config.GetValue<string>("Emotes:CrossMark"), x => x
                        .ShouldResumeAfterExecution(false))
                    .AddMessageCallback(x => x
                        .WithCallback(message =>
                        {
                            argument = message.Content;
                            discordCommandContext.AddMessage(message);
                        })
                        .ShouldResumeAfterExecution(false));

                var message = await discordCommandContext
                    .ReplyAsync(text.ToString(), allowedMentions: AllowedMentions.None);

                await interactiveService.ExecuteInteractiveMessageAsync(
                    interactiveMessage.Build(), message, discordCommandContext.User);

                if (argument == null && !skipped)
                    return ConbotArgumentParserResult.Failed("Aborted");

                arguments.Add(parameter, argument);
            }

            return ConbotArgumentParserResult.Successful(arguments);
        }
    }
}