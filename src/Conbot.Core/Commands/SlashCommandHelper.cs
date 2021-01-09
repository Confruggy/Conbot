using System.Collections.Generic;
using System.Linq;

using Discord;

using Humanizer;

using Qmmands;

namespace Conbot.Commands
{
    public static class SlashCommandHelper
    {
        public static List<ApplicationCommandOptionProperties> GetOptionsForCommand(Command command)
        {
            var options = new List<ApplicationCommandOptionProperties>();

            foreach (var parameter in command.Parameters)
            {
                string description = parameter.Description ?? "No Description.";
                ApplicationCommandOptionType type;

                if (parameter.Type == typeof(bool))
                    type = ApplicationCommandOptionType.Boolean;
                else if (parameter.Type == typeof(int))
                    type = ApplicationCommandOptionType.Integer;
                else if (typeof(IUser).IsAssignableFrom(parameter.Type))
                    type = ApplicationCommandOptionType.User;
                else if (typeof(IChannel).IsAssignableFrom(parameter.Type))
                    type = ApplicationCommandOptionType.Channel;
                else if (typeof(IRole).IsAssignableFrom(parameter.Type))
                    type = ApplicationCommandOptionType.Role;
                else
                    type = ApplicationCommandOptionType.String;

                List<ApplicationCommandOptionChoiceProperties>? choices;
                if (parameter.Checks.FirstOrDefault(x => x is ChoicesAttribute) is ChoicesAttribute choicesAttribute)
                {
                    choices = new List<ApplicationCommandOptionChoiceProperties>();

                    foreach (string choice in choicesAttribute.Choices)
                    {
                        choices.Add(new ApplicationCommandOptionChoiceProperties
                        {
                            Name = choice,
                            Value = choice
                        });
                    }
                }
                else
                {
                    choices = null;
                }

                if (parameter.IsMultiple)
                {
                    string name = parameter.Name.Singularize().Kebaberize();

                    var minLengthAttribute =
                        parameter.Checks.FirstOrDefault(x => x is MinLengthAttribute) as MinLengthAttribute;

                    int minLength = minLengthAttribute?.Length ?? 0;
                    int count = options.Count;

                    for (int i = 1; i <= 10 - count; i++)
                    {
                        options.Add(new ApplicationCommandOptionProperties
                        {
                            Name = $"{name}{i}",
                            Description = description,
                            Type = type,
                            Required = i <= minLength,
                            Choices = choices
                        });
                    }
                }
                else
                {
                    options.Add(new ApplicationCommandOptionProperties
                    {
                        Name = parameter.Name.Kebaberize(),
                        Description = description,
                        Type = type,
                        Required = !parameter.IsOptional,
                        Choices = choices
                    });
                }
            }

            return options;
        }
    }
}