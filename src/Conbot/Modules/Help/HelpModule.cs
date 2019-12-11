using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conbot.Core.Extensions;
using Conbot.Core.InteractiveMessages;
using Discord;
using Discord.Commands;

namespace Conbot.Modules.Help
{
    [Name("Help")]
    [Group("help")]
    [Summary("Gives information about commands.")]
    public class HelpModule : ModuleBase<ShardedCommandContext>
    {
        private readonly CommandService _service;

        public HelpModule(CommandService service) => _service = service;

        [Command]
        [Summary("Shows all available commands or gives more information about a specific command.")]
        public async Task HelpAsync(
            [Remainder, Summary(
                "The command to give more information about. " +
                "If no command is set it shows all available commands instead.")] string command = null)
        {
            ModuleInfo firstModule;
            CommandInfo firstCommand;

            if (command != null)
            {
                firstModule = _service.Modules
                    .FirstOrDefault(x => x.Group?.ToLower() == command.ToLower()
                        || x.Name?.ToLower() == command.ToLower());

                firstCommand = _service.Commands
                        .FirstOrDefault(c => c.Aliases.Any(a => a.ToLower() == command.ToLower()));

                if (firstModule == null && firstCommand == null)
                {
                    await ReplyAsync("Command or category hasn't been found.");
                    return;
                }
            }
            else
            {
                firstModule = null;
                firstCommand = null;
            }

            var moduleDictionary = new Dictionary<string, ModuleInfo>();
            var commandDictionary = new Dictionary<string, CommandInfo>();

            ModuleInfo currentModule = null;
            CommandInfo currentCommand = null;

            IUserMessage message;

            if (firstModule != null)
            {
                message = await ReplyAsync(embed: CreateModuleEmbed(firstModule, out moduleDictionary,
                        out commandDictionary));
                currentModule = firstModule;
            }
            else if (firstCommand != null)
            {
                message = await ReplyAsync(embed: CreateCommandEmbed(firstCommand, out commandDictionary));
                currentCommand = firstCommand;
            }
            else
            {
                message = await ReplyAsync(embed: CreateStartEmbed(out moduleDictionary));
            }

            if (!moduleDictionary.Any() && !commandDictionary.Any())
                return;

            var interactiveMessage = new InteractiveMessageBuilder()
                .WithPrecondition(x => x.Id == Context.User.Id)
                .AddReactionCallback(x => x
                    .WithEmote("first:314070282643963914")
                    .WithCallback(async x =>
                    {
                        if (currentModule != null)
                            await message.ModifyAsync(x => x.Embed = CreateStartEmbed(out moduleDictionary));
                    })
                    .ShouldResumeAfterExecution(true))
                .AddReactionCallback(x => x
                    .WithEmote("backward:314070282656677898")
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
                            await message.ModifyAsync(x => x.Embed = CreateStartEmbed(out moduleDictionary));
                        }
                    })
                    .ShouldResumeAfterExecution(true))
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

                        if (!moduleDictionary.Any() && !commandDictionary.Any())
                            return;
                    })
                    .ShouldResumeAfterExecution(true))
                .Build();

            await interactiveMessage.ExecuteAsync(Context.Client, message);
        }

        public Embed CreateStartEmbed(out Dictionary<string, ModuleInfo> moduleDictionary)
        {
            var embed = new EmbedBuilder()
                .WithAuthor(Context.Guild.CurrentUser.Username, Context.Guild.CurrentUser.GetAvatarUrl())
                .WithDescription(
                    "Below you see every modules you can use. " +
                    "Each module has one or several commands.")
                .WithColor(Constants.DefaultEmbedColor)
                .WithFooter("Enter a number for more information about a command.");

            var modules = _service.Modules
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
                }
                i++;
            }

            if (subcommandText.Length != 0)
                embed.AddField(string.IsNullOrEmpty(module.Group) ? "Commands" : "Subcommands", subcommandText);

            if (commandsText.Length != 0 || subcommandText.Length != 0)
                embed.WithFooter("Enter a number for more information about a command.");

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
            if (!module.IsSubmodule)
                return module.Name;

            var parent = module.Parent;

            while (parent.Parent != null)
                parent = module.Parent;

            return $"{parent.Name}/{string.Join('/', module.Aliases.First().Split(' ').SkipLast(1))}";
        }

        private string GetPath(CommandInfo command)
        {
            var module = command.Module;

            while (module.Parent != null)
                module = module.Parent;

            return $"{module.Name}/{command.Aliases.First().Replace(' ', '/')}";
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