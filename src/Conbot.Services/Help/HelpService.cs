using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conbot.Extensions;
using Conbot.InteractiveMessages;
using Conbot.Services.Interactive;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Conbot.Services.Help
{
    public class HelpService
    {
        private readonly CommandService _commandService;
        private readonly InteractiveService _interactiveService;

        public HelpService(CommandService commandService, InteractiveService interactiveService)
        {
            _commandService = commandService;
            _interactiveService = interactiveService;
        }

        public async Task ExecuteHelpMessageAsync(SocketCommandContext context, ModuleInfo startModule = null,
            CommandInfo startCommand = null, IUserMessage message = null)
        {
            var moduleDictionary = new Dictionary<string, ModuleInfo>();
            var commandDictionary = new Dictionary<string, CommandInfo>();

            ModuleInfo currentModule = null;
            CommandInfo currentCommand = null;

            Embed embed;

            if (startModule != null)
            {
                embed = CreateModuleEmbed(startModule, out moduleDictionary, out commandDictionary);
                currentModule = startModule;
            }
            else if (startCommand != null)
            {
                embed = CreateCommandEmbed(startCommand, out commandDictionary);
                currentCommand = startCommand;
            }
            else
            {
                embed = CreateStartEmbed(context, out moduleDictionary);
            }

            if (message == null)
                message = await context.Channel.SendMessageAsync(embed: embed);
            else await message.ModifyAsync(x => x.Embed = embed);

            var interactiveMessage = new InteractiveMessageBuilder()
                .WithPrecondition(x => x.Id == context.User.Id)
                .AddReactionCallback(x => x
                    .WithEmote("first:654781462490644501")
                    .WithCallback(async x =>
                    {
                        if (currentModule != null || currentCommand != null)
                            await message.ModifyAsync(x => x.Embed = CreateStartEmbed(context, out moduleDictionary));
                    })
                    .ShouldResumeAfterExecution(true))
                .AddReactionCallback(x => x
                    .WithEmote("backward:654781463027515402")
                    .WithCallback(async x =>
                    {
                        if (currentCommand != null)
                        {
                            await message.ModifyAsync(x =>
                                x.Embed = CreateModuleEmbed(currentCommand.Module, out moduleDictionary,
                                    out commandDictionary));
                            currentModule = currentCommand.Module;
                            currentCommand = null;
                        }
                        else if (currentModule?.Parent != null)
                        {
                            await message.ModifyAsync(x =>
                                x.Embed = CreateModuleEmbed(currentModule.Parent, out moduleDictionary,
                                    out commandDictionary));
                            currentModule = currentModule.Parent;
                        }
                        else if (currentModule != null)
                        {
                            await message.ModifyAsync(x => x.Embed = CreateStartEmbed(context, out moduleDictionary));
                        }
                    })
                    .ShouldResumeAfterExecution(true))
                .AddReactionCallback(x => x
                    .WithEmote("stop:654781462385655849")
                    .ShouldResumeAfterExecution(false))
                .AddMessageCallback(x => x
                    .WithPrecondition(msg => int.TryParse(msg.Content, out int number))
                    .WithCallback(async msg =>
                    {
                        var tasks = new List<Task>();

                        var currentEmbed = message.Embeds.FirstOrDefault() as Embed;

                        if (moduleDictionary.TryGetValue(msg.Content, out var moduleInfo))
                        {
                            tasks.Add(message.ModifyAsync(x => x.Embed = CreateModuleEmbed(moduleInfo,
                                out moduleDictionary, out commandDictionary)));
                            currentModule = moduleInfo;
                            currentCommand = null;
                        }
                        else if (commandDictionary.TryGetValue(msg.Content, out var commandInfo))
                        {
                            tasks.Add(message.ModifyAsync(x => x.Embed = CreateCommandEmbed(commandInfo,
                                out commandDictionary)));
                            currentCommand = commandInfo;
                            currentModule = null;
                        }
                        else return;

                        tasks.Add(msg.TryDeleteAsync());

                        await Task.WhenAll(tasks);
                    })
                    .ShouldResumeAfterExecution(true))
                .Build();

            await _interactiveService.ExecuteInteractiveMessageAsync(interactiveMessage, message, context.User);
        }

        public Embed CreateStartEmbed(SocketCommandContext context, out Dictionary<string, ModuleInfo> moduleDictionary)
        {
            var embed = new EmbedBuilder()
                .WithAuthor(context.Guild.CurrentUser.Username, context.Guild.CurrentUser.GetAvatarUrl())
                .WithDescription(
                    "Below you see every modules you can use. " +
                    "Each module has one or several commands.")
                .WithColor(Constants.DefaultEmbedColor)
                .WithFooter("Enter a number for more information about a command.");

            var modules = _commandService.Modules
                .Where(x => !x.IsSubmodule)
                .OrderBy(x => x.Name)
                .ToArray();

            moduleDictionary = new Dictionary<string, ModuleInfo>();

            var modulesText = new StringBuilder();

            for (int i = 0; i < modules.Length; i++)
            {
                var module = modules[i];

                modulesText.AppendLine(GetShortModule(module, i + 1));
                moduleDictionary.Add((i + 1).ToString(), module);
            }

            embed.AddField("Modules", modulesText.ToString());

            return embed.Build();
        }

        public Embed CreateModuleEmbed(ModuleInfo module,
            out Dictionary<string, ModuleInfo> moduleDictionary,
            out Dictionary<string, CommandInfo> commandDictionary)
        {
            var embed = new EmbedBuilder()
                .WithAuthor(GetPath(module))
                .WithTitle(module.Group)
                .WithColor(Constants.DefaultEmbedColor);

            var descriptionText = new StringBuilder()
                    .AppendLine(module.Summary ?? "No summary.");

            if (!string.IsNullOrEmpty(module.Remarks))
                descriptionText
                    .AppendLine()
                    .AppendLine($"\n\n> {module.Remarks}");

            embed.WithDescription(descriptionText.ToString());

            int i = 1;

            commandDictionary = new Dictionary<string, CommandInfo>();

            var commands = module.Commands
                .Where(x => x.Aliases.First() == module.Group)
                .OrderBy(x => x.Name);

            var commandsText = new StringBuilder();
            if (commands.Any())
            {
                foreach (var command in commands)
                {
                    commandsText.AppendLine(GetShortCommand(command, i));
                    commandDictionary.Add(i.ToString(), command);
                    i++;
                }
                embed.AddField("Commands", commandsText);
            }

            var subcommandsAndSubmodules = new Dictionary<string, object>();

            var subcommands = module.Commands
                .Where(x => x.Aliases.First() != module.Group);

            foreach (var command in subcommands)
                subcommandsAndSubmodules.Add(command.Aliases.First(), command);

            foreach (var submodule in module.Submodules)
                subcommandsAndSubmodules.Add(submodule.Aliases.First(), submodule);

            moduleDictionary = new Dictionary<string, ModuleInfo>();

            var subcommandText = new StringBuilder();

            bool containsGroupedCommands = false;

            foreach (var keyValuePair in subcommandsAndSubmodules.OrderBy(x => x.Key))
            {
                if (keyValuePair.Value is CommandInfo commandInfo)
                {
                    subcommandText.AppendLine(GetShortCommand(commandInfo, i));
                    commandDictionary.Add(i.ToString(), commandInfo);
                }
                else if (keyValuePair.Value is ModuleInfo moduleInfo)
                {
                    subcommandText.AppendLine(GetShortModule(moduleInfo, i));
                    moduleDictionary.Add(i.ToString(), moduleInfo);
                    containsGroupedCommands = true;
                }
                i++;
            }

            if (subcommandText.Length != 0)
                embed.AddField(string.IsNullOrEmpty(module.Group) ? "Commands" : "Subcommands", subcommandText);

            var footerText = new StringBuilder();

            if (commandsText.Length != 0 || subcommandText.Length != 0)
                footerText.Append("Enter a number for more information about a command.");

            if (containsGroupedCommands)
                footerText.Append(" *Contains subcommands or overloads.");

            embed.WithFooter(footerText.ToString());

            return embed.Build();
        }

        public Embed CreateCommandEmbed(CommandInfo command, out Dictionary<string, CommandInfo> commandDictionary)
        {
            var embed = new EmbedBuilder()
                .WithAuthor(GetPath(command))
                .WithTitle($"{command.Aliases.First()} {FormatParameters(command)}")
                .WithColor(Constants.DefaultEmbedColor);

            var descriptionText = new StringBuilder()
                    .AppendLine(command.Summary ?? "No summary.");

            if (!string.IsNullOrEmpty(command.Remarks))
                descriptionText.Append($"> {command.Remarks}");
            embed.WithDescription(descriptionText.ToString());

            var parameters = command.Parameters;
            if (parameters.Any())
            {
                var parameterText = new StringBuilder();

                foreach (var parameter in command.Parameters)
                {
                    parameterText
                        .AppendLine($"{ParameterToString(parameter, true)}")
                        .Append($"> {parameter.Summary ?? "No summary."}");

                    if (parameter.DefaultValue != null)
                        parameterText.Append($" Default value is {parameter.DefaultValue}.");

                    parameterText.AppendLine();
                }

                embed.AddField("Parameters", parameterText);
            }

            var overloads = command.Module.Commands
                .Where(x => x.Aliases.First() == command.Aliases.First() && x != command)
                .ToArray();

            commandDictionary = new Dictionary<string, CommandInfo>();

            var overloadsText = new StringBuilder();
            if (overloads.Any())
            {
                for (int i = 0; i < overloads.Length; i++)
                {
                    var subcommand = overloads[i];

                    overloadsText.AppendLine(GetShortCommand(subcommand, i + 1));
                    commandDictionary.Add((i + 1).ToString(), subcommand);
                }

                embed.AddField("Overloads", overloadsText);
            }

            if (overloadsText.Length != 0)
                embed.WithFooter("Enter a number for more information about an overload.");

            return embed.Build();
        }

        private string GetShortCommand(CommandInfo command, int index) =>
            $"`{index}.` **{command.Aliases.First()}** {FormatParameters(command)}\n" +
            $"> {command.Summary ?? "No summary."}";

        private string GetShortModule(ModuleInfo module, int index) =>
            $"`{index}.` **{(module.IsSubmodule ? $"{module.Aliases.First()}*" : module.Name)}**\n" +
            $"> {module.Summary ?? "No summary."}";

        private string GetPath(ModuleInfo module)
        {
            if (!module.IsSubmodule && string.IsNullOrEmpty(module.Group))
                return module.Name;

            var parent = module.Parent;

            while (parent?.Parent != null)
                parent = module.Parent;

            return $"{parent?.Name ?? module.Name} › {string.Join(" › ", module.Aliases.First().Split(' '))}";
        }

        private string GetPath(CommandInfo command)
        {
            var module = command.Module;

            while (module.IsSubmodule)
                module = module.Parent;

            return $"{module.Name} › {command.Aliases.First().Replace(" ", " › ")}";
        }

        private string ParameterToString(ParameterInfo parameter, bool literal = false)
        {
            if (literal)
            {
                if (parameter.IsMultiple)
                    return ($"{parameter.Name} *(multiple)*");
                else if (parameter.IsOptional)
                    return $"{parameter.Name} *(optional)*";
                else if (parameter.IsRemainder && !parameter.IsOptional)
                    return $"{parameter.Name} *(required, remainder)*";
                else if (parameter.IsRemainder && parameter.IsOptional)
                    return $"{parameter.Name} *(optional, remainder)*";
                else
                    return $"{parameter.Name} *(required)*";
            }
            else
            {
                if (parameter.IsMultiple)
                    return ($"[{parameter.Name}] [...]");
                else if (parameter.IsOptional)
                    return $"[{parameter.Name}]";
                else if (parameter.IsRemainder && !parameter.IsOptional)
                    return $"<{parameter.Name}...>";
                else if (parameter.IsRemainder && parameter.IsOptional)
                    return $"[{parameter.Name}...]";
                else
                    return $"<{parameter.Name}>";
            }
        }

        private string FormatParameters(CommandInfo command)
            => string.Join(" ", command.Parameters.Select(x => ParameterToString(x)));
    }
}