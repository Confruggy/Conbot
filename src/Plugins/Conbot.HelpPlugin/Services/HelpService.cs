using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;
using Conbot.Extensions;
using Conbot.Interactive;

using Discord;
using Discord.Rest;

using Qmmands;

namespace Conbot.HelpPlugin
{
    public class HelpService
    {
        private readonly CommandService _commandService;
        private readonly InteractiveService _interactiveService;
        private readonly IConfiguration _config;

        public HelpService(CommandService commandService, InteractiveService interactiveService, IConfiguration config)
        {
            _commandService = commandService;
            _interactiveService = interactiveService;
            _config = config;
        }

        public Task ExecuteHelpMessageAsync(DiscordCommandContext context, IUserMessage? message = null)
            => ExecuteHelpMessageAsync(context, null, null, message);

        public Task ExecuteHelpMessageAsync(DiscordCommandContext context, Command startCommand,
            IUserMessage? message = null)
            => ExecuteHelpMessageAsync(context, null, startCommand, message: message);

        public Task ExecuteHelpMessageAsync(DiscordCommandContext context, Module startModule,
            IUserMessage? message = null)
            => ExecuteHelpMessageAsync(context, startModule, null, message: message);

        private async Task ExecuteHelpMessageAsync(DiscordCommandContext context, Module? startModule = null,
            Command? startCommand = null, IUserMessage? message = null)
        {
            var moduleDictionary = new Dictionary<string, Module>();
            var commandDictionary = new Dictionary<string, Command>();

            Module? currentModule = null;
            Command? currentCommand = null;

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
            {
                message = context.Interaction != null
                    ? (RestUserMessage)await context.Interaction.RespondAsync(embed: embed)
                    : await context.Message.ReplyAsync(embed: embed, allowedMentions: AllowedMentions.None);
            }
            else
            {
                await message.ModifyAsync(x => x.Embed = embed);
            }

            var interactiveMessage = new InteractiveMessageBuilder()
                .WithPrecondition(x => x.Id == context.User.Id)
                .AddReactionCallback(_config.GetValue<string>("Emotes:First"), x => x
                    .WithCallback(async _ =>
                    {
                        if (currentModule != null || currentCommand != null)
                        {
                            await message.ModifyAsync(x => x.Embed = CreateStartEmbed(context, out moduleDictionary));
                            currentModule = null;
                            currentCommand = null;
                        }
                    })
                    .ShouldResumeAfterExecution(true))
                .AddReactionCallback(_config.GetValue<string>("Emotes:Backward"), x => x
                    .WithCallback(async _ =>
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
                .AddReactionCallback(_config.GetValue<string>("Emotes:Stop"), x => x
                    .ShouldResumeAfterExecution(false))
                .AddMessageCallback(x => x
                    .WithPrecondition(msg => int.TryParse(msg.Content, out int number))
                    .WithCallback(async msg =>
                    {
                        List<Task> tasks = new();

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
                        else
                        {
                            return;
                        }

                        tasks.Add(msg.TryDeleteAsync());

                        await Task.WhenAll(tasks);
                    })
                    .ShouldResumeAfterExecution(true))
                .Build();

            await _interactiveService.ExecuteInteractiveMessageAsync(interactiveMessage, message, context.User);
        }

        public Embed CreateStartEmbed(DiscordCommandContext context, out Dictionary<string, Module> moduleDictionary)
        {
            var embed = new EmbedBuilder()
                .WithAuthor(context.Client.CurrentUser.Username, context.Client.CurrentUser.GetAvatarUrl())
                .WithDescription(
                    "Below you see all available categories. " +
                    "Each category has one or several commands.")
                .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                .WithFooter("Enter a number for more information about a category.");

            var modules = _commandService.GetAllModules()
                .Where(x => x.Parent == null)
                .OrderBy(x => x.Name)
                .ToArray();

            moduleDictionary = new Dictionary<string, Module>();

            var modulesText = new StringBuilder();

            for (int i = 0; i < modules.Length; i++)
            {
                var module = modules[i];

                modulesText.AppendLine(GetShortModule(module, i + 1));
                moduleDictionary.Add((i + 1).ToString(), module);
            }

            embed.AddField("Categories", modulesText.ToString());

            string? botInviteUrl = _config.GetValue<string?>("BotInviteUrl", null);
            string? serverInviteUrl = _config.GetValue<string?>("ServerInviteUrl", null);

            string? botInviteText = !string.IsNullOrEmpty(botInviteUrl)
                ? Format.Url($"Invite {context.Client.CurrentUser.Username}", botInviteUrl)
                : null;
            string? serverInviteText = !string.IsNullOrEmpty(serverInviteUrl)
                ? Format.Url("Discord Server", serverInviteUrl)
                : null;

            string linksText = string.Join("｜", new[] { botInviteText, serverInviteText }.Where(x => x is not null));
            if (!string.IsNullOrEmpty(linksText))
                embed.AddField("Links", linksText);

            return embed.Build();
        }

        public Embed CreateModuleEmbed(Module module,
            out Dictionary<string, Module> moduleDictionary,
            out Dictionary<string, Command> commandDictionary)
        {
            var embed = new EmbedBuilder()
                .WithAuthor(GetPath(module))
                .WithColor(_config.GetValue<uint>("DefaultEmbedColor"));

            var descriptionText = new StringBuilder()
                .AppendLine(module.Description ?? "No Description.");

            if (!string.IsNullOrEmpty(module.Remarks))
            {
                descriptionText
                    .AppendLine()
                    .Append(">>> ")
                    .AppendLine(module.Remarks);
            }

            embed.WithDescription(descriptionText.ToString());

            int i = 1;

            commandDictionary = new Dictionary<string, Command>();

            var commands = module.Commands
                .Where(x => x.FullAliases[0] == module.FullAliases.FirstOrDefault())
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
                .Where(x => x.FullAliases[0] != module.FullAliases.FirstOrDefault());

            foreach (var command in subcommands)
                subcommandsAndSubmodules.Add(command.Aliases[0], command);

            foreach (var submodule in module.Submodules)
                subcommandsAndSubmodules.Add(submodule.Aliases[0], submodule);

            moduleDictionary = new Dictionary<string, Module>();

            var subcommandText = new StringBuilder();

            bool containsGroupedCommands = false;

            foreach (var keyValuePair in subcommandsAndSubmodules.OrderBy(x => x.Key))
            {
                if (keyValuePair.Value is Command commandInfo)
                {
                    subcommandText.AppendLine(GetShortCommand(commandInfo, i));
                    commandDictionary.Add(i.ToString(), commandInfo);
                }
                else if (keyValuePair.Value is Module moduleInfo)
                {
                    subcommandText.AppendLine(GetShortModule(moduleInfo, i));
                    moduleDictionary.Add(i.ToString(), moduleInfo);
                    containsGroupedCommands = true;
                }

                i++;
            }

            if (subcommandText.Length != 0)
                embed.AddField(module.Aliases.Count == 0 ? "Commands" : "Subcommands", subcommandText);

            var footerText = new StringBuilder();

            if (commandsText.Length != 0 || subcommandText.Length != 0)
                footerText.Append("Enter a number for more information about a command.");

            if (containsGroupedCommands)
                footerText.Append(" *Contains subcommands or overloads.");

            embed.WithFooter(footerText.ToString());

            return embed.Build();
        }

        public Embed CreateCommandEmbed(Command command, out Dictionary<string, Command> commandDictionary)
        {
            var embed = new EmbedBuilder()
                .WithAuthor(GetPath(command))
                .WithTitle($"/{command.FullAliases[0]} {FormatParameters(command)}")
                .WithColor(_config.GetValue<uint>("DefaultEmbedColor"));

            var descriptionText = new StringBuilder()
                .AppendLine(command.Description ?? "No Description.");

            if (!string.IsNullOrEmpty(command.Remarks))
            {
                descriptionText
                    .AppendLine()
                    .Append(">>> ")
                    .Append(command.Remarks);
            }

            embed.WithDescription(descriptionText.ToString());

            var parameters = command.Parameters;
            if (parameters.Count > 0)
            {
                var parameterText = new StringBuilder();

                foreach (var parameter in command.Parameters)
                {
                    parameterText
                        .AppendLine(ParameterToString(parameter, true))
                        .Append("> ")
                        .Append(parameter.Description ?? "No Description.");

                    if (parameter.Checks.FirstOrDefault(x => x is MinLengthAttribute)
                        is MinLengthAttribute minLengthCheck)
                    {
                        parameterText
                            .Append(" Minimal ")
                            .Append(parameter.IsMultiple ? "amount" : "length")
                            .Append(" is ")
                            .Append(minLengthCheck.Length)
                            .Append('.');
                    }

                    if (parameter.Checks.FirstOrDefault(x => x is MaxLengthAttribute)
                        is MaxLengthAttribute maxLengthCheck)
                    {
                        parameterText
                            .Append(" Maximal ")
                            .Append(parameter.IsMultiple ? "amount" : "length")
                            .Append(" is ")
                            .Append(maxLengthCheck.Length)
                            .Append('.');
                    }

                    if (parameter.Checks.FirstOrDefault(x => x is MinValueAttribute)
                        is MinValueAttribute minValueCheck)
                    {
                        parameterText
                            .Append(" Minimal value is ")
                            .Append(minValueCheck.MinValue)
                            .Append('.');
                    }

                    if (parameter.Checks.FirstOrDefault(x => x is MaxValueAttribute)
                        is MaxValueAttribute maxValueCheck)
                    {
                        parameterText
                            .Append(" Maximal value is ")
                            .Append(maxValueCheck.MaxValue)
                            .Append('.');
                    }

                    if (parameter.DefaultValue is not null && parameter.DefaultValue is not Array)
                    {
                        parameterText.Append(" Default value is ")
                            .Append(parameter.DefaultValue)
                            .Append('.');
                    }

                    if (!string.IsNullOrEmpty(parameter.Remarks))
                    {
                        parameterText
                            .Append(' ')
                            .Append(parameter.Remarks.Replace("\n", "\n> "));
                    }

                    parameterText.AppendLine();
                }

                embed.AddField("Parameters", parameterText);
            }

            var overloads = command.Module.Commands
                .Where(x => x.FullAliases[0] == command.FullAliases[0] && x != command)
                .ToArray();

            commandDictionary = new Dictionary<string, Command>();

            var overloadsText = new StringBuilder();
            if (overloads.Length > 0)
            {
                for (int i = 0; i < overloads.Length; i++)
                {
                    var subcommand = overloads[i];

                    overloadsText.AppendLine(GetShortCommand(subcommand, i + 1));
                    commandDictionary.Add((i + 1).ToString(), subcommand);
                }

                embed.AddField("Overloads", overloadsText);
            }

            var footerText = new StringBuilder();

            if (overloadsText.Length != 0)
                footerText.Append("Enter a number for more information about an overload.");

            if (command.FullAliases.Count > 1)
            {
                var aliasesText = new StringBuilder();

                foreach (string? alias in command.FullAliases.Skip(1))
                {
                    aliasesText
                        .Append(Format.Bold(alias))
                        .Append(' ')
                        .AppendLine(FormatParameters(command));
                }

                if (footerText.Length != 0)
                    footerText.Append(' ');

                footerText.Append("Aliases are only supported by text commands.");

                embed
                    .AddField("Aliases", aliasesText);
            }

            embed.WithFooter(footerText.ToString());

            return embed.Build();
        }

        private static string GetShortCommand(Command command, int index)
            => $"`{index}.` **/{command.FullAliases[0]}** {FormatParameters(command)}\n" +
                $"> {command.Description ?? "No Description."}";

        private static string GetShortModule(Module module, int index)
            => $"`{index}.` **{(module.Parent != null ? $"/{module.FullAliases[0]}*" : module.Name)}**\n" +
                $"> {module.Description ?? "No Description."}";

        private static string GetPath(Module module)
        {
            string? alias = module.FullAliases.FirstOrDefault();

            if (string.IsNullOrEmpty(alias))
                return module.Name;

            var parent = module.Parent;

            while (parent?.Parent != null)
                parent = module.Parent;

            return $"{parent?.Name ?? module.Name} › {string.Join(" › ", alias.Split(' '))}";
        }

        private static string GetPath(Command command)
        {
            var module = command.Module;

            while (module.Parent != null)
                module = module.Parent;

            return $"{module.Name} › {command.FullAliases[0].Replace(" ", " › ")}";
        }

        private static string ParameterToString(Parameter parameter, bool literal = false)
        {
            string name;
            if (parameter.Checks.FirstOrDefault(x => x is ChoicesAttribute)
                is ChoicesAttribute optionsAttribute)
            {
                name = string.Join('|', optionsAttribute.Choices);
            }
            else
            {
                name = parameter.Name;
            }

            if (literal)
            {
                var text = new StringBuilder()
                    .Append(name)
                    .Append(" (*");

                if (parameter.IsMultiple)
                    text.Append("multiple");
                else if (parameter.IsOptional)
                    text.Append("optional");
                else
                    text.Append("required");

                if (parameter.IsRemainder)
                    text.Append(", remainder");

                text.Append("*)");
                return text.ToString();
            }
            else
            {
                if (parameter.IsMultiple)
                    return $"[{name}] […]";
                else if (parameter.IsRemainder && !parameter.IsOptional && !name.Contains('|'))
                    return $"<{name}…>";
                else if (parameter.IsRemainder && parameter.IsOptional && !name.Contains('|'))
                    return $"[{name}…]";
                else if (parameter.IsOptional)
                    return $"[{name}]";
                else
                    return $"<{name}>";
            }
        }

        private static string FormatParameters(Command command)
            => string.Join(" ", command.Parameters.Select(x => ParameterToString(x)));
    }
}
