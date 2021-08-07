using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;
using Conbot.Extensions;
using Conbot.Interactive;

using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Rest;

using Qmmands;

namespace Conbot.HelpPlugin
{
    public class HelpService : DiscordBotService
    {
        private readonly InteractiveService _interactiveService;
        private readonly IConfiguration _config;

        public HelpService(InteractiveService interactiveService, IConfiguration config)
        {
            _interactiveService = interactiveService;
            _config = config;
        }

        public Task ExecuteHelpMessageAsync(ConbotCommandContext context, bool deleteAfterCompletion = false)
            => ExecuteHelpMessageAsync(context, null, null, deleteAfterCompletion);

        public Task ExecuteHelpMessageAsync(ConbotCommandContext context, Command startCommand,
            bool deleteAfterCompletion = false)
            => ExecuteHelpMessageAsync(context, null, startCommand, deleteAfterCompletion);

        public Task ExecuteHelpMessageAsync(ConbotCommandContext context, Module startModule,
            bool deleteAfterCompletion = false)
            => ExecuteHelpMessageAsync(context, startModule, null, deleteAfterCompletion);

        private async Task ExecuteHelpMessageAsync(ConbotCommandContext context, Module? startModule = null,
            Command? startCommand = null, bool deleteAfterCompletion = false)
        {
            var moduleDictionary = new Dictionary<string, Module>();
            var commandDictionary = new Dictionary<string, Command>();

            Module? currentModule = null;
            Command? currentCommand = null;

            LocalEmbed embed;

            if (startModule is not null)
            {
                embed = CreateModuleEmbed(startModule, out moduleDictionary, out commandDictionary);
                currentModule = startModule;
            }
            else if (startCommand is not null)
            {
                embed = CreateCommandEmbed(startCommand, out commandDictionary);
                currentCommand = startCommand;
            }
            else
            {
                embed = CreateStartEmbed(out moduleDictionary);
            }

            var interactiveMessage = new LocalInteractiveMessage()
                .AddEmbed(embed)
                .WithPrecondition(x => x.Id == context.Author.Id)
                .AddReactionCallback(_config.GetValue<string>("Emotes:Home"), x => x
                    .WithCallback(async (msg, _) =>
                    {
                        if (currentModule is not null || currentCommand is not null)
                        {
                            await msg.ModifyAsync(x => x.Embeds = new[] { CreateStartEmbed(out moduleDictionary) });
                            currentModule = null;
                            currentCommand = null;
                        }
                    }))
                .AddReactionCallback(_config.GetValue<string>("Emotes:Backward"), x => x
                    .WithCallback(async (msg, _) =>
                    {
                        if (currentCommand is not null)
                        {
                            await msg.ModifyAsync(x =>
                                x.Embeds = new[] { CreateModuleEmbed(currentCommand.Module, out moduleDictionary,
                                    out commandDictionary) });
                            currentModule = currentCommand.Module;
                            currentCommand = null;
                        }
                        else if (currentModule?.Parent is not null)
                        {
                            await msg.ModifyAsync(x =>
                                x.Embeds = new[] { CreateModuleEmbed(currentModule.Parent, out moduleDictionary,
                                    out commandDictionary) });
                            currentModule = currentModule.Parent;
                        }
                        else if (currentModule is not null)
                        {
                            await msg.ModifyAsync(x => x.Embeds = new[] { CreateStartEmbed(out moduleDictionary) });
                        }
                    }))
                .AddReactionCallback(_config.GetValue<string>("Emotes:Stop"), x => x
                    .WithCallback((msg, _) => msg.Stop()))
                .AddMessageCallback(x => x
                    .WithPrecondition((_, e) => int.TryParse(e.Message.Content, out int number))
                    .WithCallback(async (msg, e) =>
                    {
                        List<Task> tasks = new();

                        if (moduleDictionary.TryGetValue(e.Message.Content, out var moduleInfo))
                        {
                            tasks.Add(msg.ModifyAsync(x => x.Embeds = new[] { CreateModuleEmbed(moduleInfo,
                                out moduleDictionary, out commandDictionary) }));
                            currentModule = moduleInfo;
                            currentCommand = null;
                        }
                        else if (commandDictionary.TryGetValue(e.Message.Content, out var commandInfo))
                        {
                            tasks.Add(msg.ModifyAsync(x => x.Embeds = new[] { CreateCommandEmbed(commandInfo,
                                out commandDictionary)}));
                            currentCommand = commandInfo;
                            currentModule = null;
                        }
                        else
                        {
                            return;
                        }

                        tasks.Add(e.Message.TryDeleteAsync());

                        await Task.WhenAll(tasks);
                    }));

            var message = await _interactiveService.ExecuteInteractiveMessageAsync(interactiveMessage, context);

            if (deleteAfterCompletion)
                _ = message.TryDeleteAsync();
        }

        public LocalEmbed CreateStartEmbed(out Dictionary<string, Module> moduleDictionary)
        {
            var embed = new LocalEmbed()
                .WithAuthor(Bot.CurrentUser.Name, Bot.CurrentUser.GetDefaultAvatarUrl())
                .WithDescription(
                    "Below you see all available categories. " +
                    "Each category has one or several commands.")
                .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
                .WithFooter("Enter a number for more information about a category.");

            var modules = Bot.Commands
                .GetAllModules()
                .Where(x => x.Parent is null)
                .OrderBy(x => x.Name)
                .ToArray();

            int padding = modules.Length.ToString().Length;
            var modulesText = new StringBuilder();
            moduleDictionary = new Dictionary<string, Module>();

            for (int i = 0; i < modules.Length; i++)
            {
                var module = modules[i];

                modulesText.AppendLine(GetShortModule(module, i + 1, padding));
                moduleDictionary.Add((i + 1).ToString(), module);
            }

            embed.AddField("Categories", modulesText.ToString());

            string? botInviteUrl = _config.GetValue<string?>("BotInviteUrl", null);
            string? serverInviteUrl = _config.GetValue<string?>("ServerInviteUrl", null);

            string? botInviteText = !string.IsNullOrEmpty(botInviteUrl)
                ? Markdown.Link($"Invite {Bot.CurrentUser.Name}", botInviteUrl)
                : null;
            string? serverInviteText = !string.IsNullOrEmpty(serverInviteUrl)
                ? Markdown.Link("Discord Server", serverInviteUrl)
                : null;

            string linksText = string.Join("｜", new[] { botInviteText, serverInviteText }.Where(x => x is not null));
            if (!string.IsNullOrEmpty(linksText))
                embed.AddField("Links", linksText);

            return embed;
        }

        public LocalEmbed CreateModuleEmbed(Module module,
            out Dictionary<string, Module> moduleDictionary,
            out Dictionary<string, Command> commandDictionary)
        {
            var embed = new LocalEmbed()
                .WithAuthor(GetPath(module))
                .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")));

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

            commandDictionary = new Dictionary<string, Command>();

            var commands = module.Commands
                .Where(x => x.FullAliases[0] == module.FullAliases.FirstOrDefault())
                .OrderBy(x => x.Name);

            int commandsCount = commands.Count();
            var subcommandsAndSubmodules = new Dictionary<string, object>();
            var subcommands = module.Commands
                .Where(x => x.FullAliases[0] != module.FullAliases.FirstOrDefault());

            foreach (var command in subcommands)
                subcommandsAndSubmodules.Add(command.Aliases[0], command);

            foreach (var submodule in module.Submodules)
                subcommandsAndSubmodules.Add(submodule.Aliases[0], submodule);

            int padding = (commandsCount + subcommandsAndSubmodules.Count).ToString().Length;
            var commandsText = new StringBuilder();
            int i = 1;

            if (commandsCount != 0)
            {
                foreach (var command in commands)
                {
                    commandsText.AppendLine(GetShortCommand(command, i, padding));
                    commandDictionary.Add(i.ToString(), command);
                    i++;
                }

                embed.AddField("Commands", commandsText);
            }

            var subcommandText = new StringBuilder();
            bool containsGroupedCommands = false;
            moduleDictionary = new Dictionary<string, Module>();

            foreach (var keyValuePair in subcommandsAndSubmodules.OrderBy(x => x.Key))
            {
                if (keyValuePair.Value is Command commandInfo)
                {
                    subcommandText.AppendLine(GetShortCommand(commandInfo, i, padding));
                    commandDictionary.Add(i.ToString(), commandInfo);
                }
                else if (keyValuePair.Value is Module moduleInfo)
                {
                    subcommandText.AppendLine(GetShortModule(moduleInfo, i, padding));
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

            return embed;
        }

        public LocalEmbed CreateCommandEmbed(Command command, out Dictionary<string, Command> commandDictionary)
        {
            var embed = new LocalEmbed()
                .WithAuthor(GetPath(command))
                .WithTitle($"{command.FullAliases[0]} {HelpUtils.FormatParameters(command)}")
                .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")));

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
                        .AppendLine(HelpUtils.FormatParameter(parameter, true))
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

            string permissionsText = GetPermissionsText(command);
            if (!string.IsNullOrEmpty(permissionsText))
                embed.AddField("Permissions", permissionsText);

            var overloads = command.Module.Commands
                .Where(x => x.FullAliases[0] == command.FullAliases[0] && x != command)
                .ToArray();

            commandDictionary = new Dictionary<string, Command>();

            var overloadsText = new StringBuilder();
            if (overloads.Length > 0)
            {
                int padding = overloads.Length.ToString().Length;

                for (int i = 0; i < overloads.Length; i++)
                {
                    var subcommand = overloads[i];

                    overloadsText.AppendLine(GetShortCommand(subcommand, i + 1, padding));
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
                        .Append(Markdown.Bold(alias))
                        .Append(' ')
                        .AppendLine(HelpUtils.FormatParameters(command));
                }

                if (footerText.Length != 0)
                    footerText.Append(' ');

                footerText.Append("Aliases are only supported by text commands.");

                embed
                    .AddField("Aliases", aliasesText);
            }

            if (footerText.Length != 0)
                embed.WithFooter(footerText.ToString());

            return embed;
        }

        private static string GetShortCommand(Command command, int index, int padding)
            => $"`{index.ToString().PadLeft(padding)}.` **{command.FullAliases[0]}** {HelpUtils.FormatParameters(command)}\n" +
                $"> {command.Description ?? "No Description."}";

        private static string GetShortModule(Module module, int index, int padding)
            => $"`{index.ToString().PadLeft(padding)}.` **{(module.Parent is not null ? $"{module.FullAliases[0]}*" : module.Name)}**\n" +
                $"> {module.Description ?? "No Description."}";

        private static string GetPath(Module module)
        {
            string? alias = module.FullAliases.FirstOrDefault();

            if (string.IsNullOrEmpty(alias))
                return module.Name;

            var parent = module.Parent;

            while (parent?.Parent is not null)
                parent = module.Parent;

            return $"{parent?.Name ?? module.Name} › {string.Join(" › ", alias.Split(' '))}";
        }

        private static string GetPath(Command command)
        {
            var module = command.Module;

            while (module.Parent is not null)
                module = module.Parent;

            return $"{module.Name} › {command.FullAliases[0].Replace(" ", " › ")}";
        }

        private static string GetPermissionsText(Command command)
        {
            List<RequireAuthorGuildPermissionsAttribute> authorGuildPermissions = new();
            List<RequireAuthorChannelPermissionsAttribute> authorChannelPermissions = new();
            List<RequireBotGuildPermissionsAttribute> botGuildPermissions = new();
            List<RequireBotChannelPermissionsAttribute> botChannelPermissions = new();

            var module = command.Module;
            while (module is not null)
            {
                authorGuildPermissions.AddRange(module.Checks.OfType<RequireAuthorGuildPermissionsAttribute>());
                authorChannelPermissions.AddRange(module.Checks.OfType<RequireAuthorChannelPermissionsAttribute>());
                botGuildPermissions.AddRange(module.Checks.OfType<RequireBotGuildPermissionsAttribute>());
                botChannelPermissions.AddRange(module.Checks.OfType<RequireBotChannelPermissionsAttribute>());

                module = module.Parent;
            }

            authorGuildPermissions.AddRange(command.Checks.OfType<RequireAuthorGuildPermissionsAttribute>());
            authorChannelPermissions.AddRange(command.Checks.OfType<RequireAuthorChannelPermissionsAttribute>());
            botGuildPermissions.AddRange(command.Checks.OfType<RequireBotGuildPermissionsAttribute>());
            botChannelPermissions.AddRange(command.Checks.OfType<RequireBotChannelPermissionsAttribute>());

            var permissionsText = new StringBuilder();

            foreach (var authorGuildPermission in authorGuildPermissions)
            {
                permissionsText.AppendLine(
                    RequirePermissionUtils.CreateRequirePermissionErrorReason(authorGuildPermission.Permissions));
            }

            foreach (var authorChannelPermission in authorChannelPermissions)
            {
                permissionsText.AppendLine(
                    RequirePermissionUtils.CreateRequirePermissionErrorReason(authorChannelPermission.Permissions));
            }

            foreach (var botGuildPermission in botGuildPermissions)
            {
                permissionsText.AppendLine(
                    RequirePermissionUtils.CreateRequirePermissionErrorReason(botGuildPermission.Permissions, true));
            }

            foreach (var botChannelPermission in botChannelPermissions)
            {
                permissionsText.AppendLine(
                    RequirePermissionUtils.CreateRequirePermissionErrorReason(botChannelPermission.Permissions, true));
            }

            return permissionsText.ToString();
        }
    }
}
