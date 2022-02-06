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
using Disqord.Extensions.Interactivity.Menus;

namespace Conbot.Commands
{
    public class InteractiveArgumentParser : IArgumentParser
    {
        public async ValueTask<ArgumentParserResult> ParseAsync(CommandContext context)
        {
            if (context is not ConbotCommandContext conbotCommandContext)
                return InteractiveArgumentParserResult.Failed("Invalid context.");

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

                object[]? choices;

                if (parameter.Checks.FirstOrDefault(x => x is ChoicesAttribute) is ChoicesAttribute attribute)
                    choices = attribute.Choices;
                else
                    choices = null;

                if (lastEntered is not null)
                {
                    text
                        .Append(lastEntered.Name.Humanize())
                        .Append(" has been entered. Now ");

                    if (choices is not null)
                        text.Append("select");
                    else
                        text.Append("enter");
                }
                else if (choices is not null)
                {
                    text.Append("Select");
                }
                else
                {
                    text.Append("Enter");
                }

                text.Append(' ');

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

                var view = new ArgumentInputView(
                    new LocalMessage()
                        .WithContent(text.ToString())
                        .WithReply(conbotCommandContext.Message!.Id, conbotCommandContext.Message.ChannelId,
                        conbotCommandContext.Message.GuildId),
                    conbotCommandContext,
                    parameter.IsOptional,
                    choices);

                var menu = new InteractiveMenu(conbotCommandContext.Author.Id, view);

                await using var yield = conbotCommandContext.BeginYield();

                await conbotCommandContext.Bot.StartMenuAsync(conbotCommandContext.ChannelId, menu);

                conbotCommandContext.AddMessage(menu.Message);

                if (choices is null)
                    await view.BackgroundTask();
                else
                    await menu.Task;

                if (view.Result is null && !view.Skipped)
                    return InteractiveArgumentParserResult.Failed("Command has been canceled.");

                arguments.Add(parameter, view.Result);
            }

            return InteractiveArgumentParserResult.Successful(arguments);
        }
    }
}
