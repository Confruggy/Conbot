using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;

using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;

using Qmmands;

namespace Conbot.HelpPlugin
{
    public class ModuleView : ViewBase
    {
        private readonly IEnumerable<Command> _commands;
        private readonly Dictionary<string, object> _subcommands;

        public Module Module { get; }
        public ConbotCommandContext Context { get; }
        public IConfiguration Configuration { get; }
        public ModuleView? ParentView { get; }
        public StartView? StartView { get; }

        public SelectionViewComponent? CommandSelection { get; }
        public SelectionViewComponent? SubommandSelection { get; }
        public ButtonViewComponent? StartButton { get; }
        public ButtonViewComponent? BackButton { get; }
        public ButtonViewComponent TryButton { get; }
        public ButtonViewComponent StopButton { get; }

        public ModuleView(Module module, ConbotCommandContext context, IConfiguration configuration,
            LocalMessage? templateMessage = null)
            : this(module, context, configuration, null, null, templateMessage)
        {
        }

        public ModuleView(Module module, StartView startView, LocalMessage? templateMessage)
        : this(module, startView.Context, startView.Configuration, null, startView, templateMessage)
        {
        }

        public ModuleView(Module module, ModuleView parentView, StartView? startView = null,
            LocalMessage? templateMessage = null)
            : this(module, parentView.Context, parentView.Configuration, parentView, startView, templateMessage)
        {
        }

        private ModuleView(Module module, ConbotCommandContext context, IConfiguration configuration,
            ModuleView? parentView, StartView? startView, LocalMessage? templateMessage)
            : base(templateMessage)
        {
            Module = module;
            Context = context;
            Configuration = configuration;
            ParentView = parentView;
            StartView = startView;

            _commands = GetCommands();
            _subcommands = GetSubcommands();

            if (_commands.Count() > 1)
            {
                List<LocalSelectionComponentOption> options = new();

                foreach (var command in _commands)
                {
                    var option = new LocalSelectionComponentOption()
                        .WithLabel($"{command.FullAliases[0]} {HelpUtils.FormatParameters(command)}")
                        .WithValue(command.Aliases[0])
                        .WithDescription(command.Description ?? "No Description.");

                    options.Add(option);
                }

                CommandSelection = new SelectionViewComponent(OnCommandSelectionAsync)
                {
                    Placeholder = "Select a command for more information",
                    Options = options,
                    MaximumSelectedOptions = 1
                };

                AddComponent(CommandSelection);
            }
            else
            {
                CommandSelection = null;
            }

            if (_subcommands.Count > 1)
            {
                List<LocalSelectionComponentOption> options = new();

                foreach (var subcommand in _subcommands.OrderBy(x => x.Key))
                {
                    LocalSelectionComponentOption? option = null;

                    if (subcommand.Value is Command command)
                    {
                        option = new LocalSelectionComponentOption()
                            .WithLabel($"{command.FullAliases[0]} {HelpUtils.FormatParameters(command)}")
                            .WithValue(subcommand.Key)
                            .WithDescription(command.Description ?? "No Description.");
                    }
                    else if (subcommand.Value is Module commandModule)
                    {
                        option = new LocalSelectionComponentOption()
                            .WithLabel($"{commandModule.FullAliases[0]}*")
                            .WithValue(subcommand.Key)
                            .WithDescription(commandModule.Description ?? "No Description.");
                    }

                    if (option is not null)
                        options.Add(option);
                }

                SubommandSelection = new SelectionViewComponent(OnSubommandSelectionAsync)
                {
                    Placeholder = $"Select a {(module.Aliases.Count == 0 ? "" : "sub")}command for more information",
                    Options = options,
                    MaximumSelectedOptions = 1
                };

                AddComponent(SubommandSelection);
            }
            else
            {
                SubommandSelection = null;
            }

            if (StartView is not null)
            {
                StartButton = new ButtonViewComponent(OnStartButtonAsync)
                {
                    Label = "Start",
                    Style = LocalButtonComponentStyle.Primary
                };

                AddComponent(StartButton);
            }

            BackButton = new ButtonViewComponent(OnBackButtonAsync)
            {
                Label = "Back",
                Style = LocalButtonComponentStyle.Primary,
                IsDisabled = ParentView is null && StartView is null
            };

            AddComponent(BackButton);

            TryButton = new ButtonViewComponent(null)
            {
                Label = "Try",
                Style = LocalButtonComponentStyle.Secondary,
                IsDisabled = true
            };

            AddComponent(TryButton);

            StopButton = new ButtonViewComponent(OnStopButtonAsync)
            {
                Emoji = LocalEmoji.FromString(Configuration["Emotes:CrossMark"]),
                Style = LocalButtonComponentStyle.Danger
            };

            AddComponent(StopButton);
        }

        private IEnumerable<Command> GetCommands()
            => Module.Commands
                .Where(x => x.FullAliases[0] == Module.FullAliases.FirstOrDefault())
                .OrderBy(x => x.Name);

        private Dictionary<string, object> GetSubcommands()
        {
            var subcommands = new Dictionary<string, object>();

            var commands = Module.Commands
                .Where(x => x.FullAliases[0] != Module.FullAliases.FirstOrDefault());

            foreach (var command in commands)
                subcommands.Add(command.ToString(), command);

            foreach (var submodule in Module.Submodules)
                subcommands.Add(submodule.ToString(), submodule);

            return subcommands;
        }

        public ValueTask OnCommandSelectionAsync(SelectionEventArgs e)
        {
            var command = _commands.First(x => x.ToString() == e.SelectedOptions[0].Value);
            Menu.View = new CommandView(command, this, StartView, TemplateMessage);

            return default;
        }

        public ValueTask OnSubommandSelectionAsync(SelectionEventArgs e)
        {
            if (_subcommands.TryGetValue(e.SelectedOptions[0].Value, out object? subcommand))
            {
                if (subcommand is Module module)
                    Menu.View = new ModuleView(module, this, StartView, TemplateMessage);
                else if (subcommand is Command command)
                    Menu.View = new CommandView(command, this, StartView, TemplateMessage);
            }

            return default;
        }

        public ValueTask OnStartButtonAsync(ButtonEventArgs e)
        {
            Menu.View = StartView;

            return default;
        }

        public ValueTask OnBackButtonAsync(ButtonEventArgs e)
        {
            if (ParentView is not null)
                Menu.View = ParentView;
            else
                Menu.View = StartView;

            return default;
        }

        public ValueTask OnStopButtonAsync(ButtonEventArgs e)
        {
            if (TemplateMessage is not null)
            {
                _ = e.Interaction.Message.ModifyAsync(x =>
                {
                    x.Content = TemplateMessage.Content;
                    x.Embeds = new(TemplateMessage.Embeds);
                    x.Components = null;
                });
            }
            else
            {
                _ = e.Interaction.Message.DeleteAsync();
            }

            Menu.Stop();

            return default;
        }

        public override LocalMessage ToLocalMessage()
        {
            var message = base.ToLocalMessage()
                .AddEmbed(CreateModuleEmbed());

            if (message.Reference is null)
                message.WithReply(Context.Messages[^1].Id, Context.ChannelId, Context.GuildId);

            return message;
        }

        private LocalEmbed CreateModuleEmbed()
        {
            var embed = new LocalEmbed()
                .WithAuthor(HelpUtils.GetPath(Module))
                .WithColor(new Color(Configuration.GetValue<int>("DefaultEmbedColor")));

            var descriptionText = new StringBuilder()
                .AppendLine(Module.Description ?? "No Description.");

            if (!string.IsNullOrEmpty(Module.Remarks))
            {
                descriptionText
                    .AppendLine()
                    .Append(">>> ")
                    .AppendLine(Module.Remarks);
            }

            embed.WithDescription(descriptionText.ToString());

            int commandsCount = _commands.Count();

            var commandsText = new StringBuilder();

            int i = 1;

            if (commandsCount != 0)
            {
                foreach (var command in _commands)
                {
                    commandsText.AppendLine(HelpUtils.GetShortCommand(command));
                    i++;
                }

                embed.AddField("Commands", commandsText);
            }

            var subcommandText = new StringBuilder();
            bool containsGroupedCommands = false;

            foreach (var keyValuePair in _subcommands.OrderBy(x => x.Key))
            {
                if (keyValuePair.Value is Command commandInfo)
                {
                    subcommandText.AppendLine(HelpUtils.GetShortCommand(commandInfo));
                }
                else if (keyValuePair.Value is Module moduleInfo)
                {
                    subcommandText.AppendLine(HelpUtils.GetShortModule(moduleInfo));
                    containsGroupedCommands = true;
                }

                i++;
            }

            if (subcommandText.Length != 0)
                embed.AddField(Module.Aliases.Count == 0 ? "Commands" : "Subcommands", subcommandText);

            if (containsGroupedCommands)
                embed.WithFooter("*Contains subcommands or overloads.");

            return embed;
        }
    }
}
