using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public class InteractionArgumentParser : IArgumentParser
    {
        public async ValueTask<ArgumentParserResult> ParseAsync(CommandContext context)
        {
            if (context is not DiscordCommandContext discordCommandContext)
                return ConbotArgumentParserResult.Failed("Invalid context.");

            var interaction = discordCommandContext.Interaction;

            if (interaction == null)
                return await DefaultArgumentParser.Instance.ParseAsync(context);

            var arguments = new Dictionary<Parameter, object?>();

            var options = interaction.Data.Options?.Cast<IApplicationCommandInteractionDataOption>();

            if (options?.First().Options != null)
            {
                options = options.First().Options;

                if (options.First().Options != null)
                    options = options.First().Options;
            }

            foreach (var parameter in context.Command.Parameters)
            {
                if (parameter.IsMultiple)
                {
                    string name = parameter.Name.Singularize().Kebaberize();
                    var multipleArgs = new List<string?>();
                    int i = 1;

                    IApplicationCommandInteractionDataOption? option;
                    while ((option = options?.FirstOrDefault(x => x.Name == $"{name}{i}")) != null)
                    {
                        multipleArgs.Add(option.Value.ToString());
                        i++;
                    }

                    arguments.Add(parameter, multipleArgs);
                }
                else
                {
                    string name = parameter.Name.Kebaberize();
                    var option = options?.FirstOrDefault(x => x.Name == name);

                    if (option?.Value == null)
                    {
                        if (parameter.IsOptional)
                        {
                            arguments.Add(parameter, parameter?.DefaultValue?.ToString());
                        }
                        else
                        {
                            return new DefaultArgumentParserResult(context.Command, parameter, arguments,
                                DefaultArgumentParserFailure.TooFewArguments, null);
                        }
                    }
                    else
                    {
                        arguments.Add(parameter, option.Value.ToString());
                    }
                }
            }

            return ConbotArgumentParserResult.Successful(arguments);
        }
    }
}