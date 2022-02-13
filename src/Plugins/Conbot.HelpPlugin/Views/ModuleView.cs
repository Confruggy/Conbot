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

using Qommon.Collections;

namespace Conbot.HelpPlugin;

public class ModuleView : ViewBase
{
    private readonly IReadOnlyCollection<Command> _commands;
    private readonly IReadOnlyDictionary<string, object> _subcommands;

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

        _commands = GetCommands().ToReadOnlyList();
        _subcommands = GetSubcommands();

        if (_commands.Any())
        {
            var options = _commands.Select(command => new LocalSelectionComponentOption()
                    .WithLabel($"{command.FullAliases[0]} {HelpUtils.FormatParameters(command)}")
                    .WithValue(command.ToString())
                    .WithDescription(command.Description ?? "No Description."))
                .ToList();

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

        if (_subcommands.Count > 0)
        {
            List<LocalSelectionComponentOption> options = new();

            foreach ((string? key, object? value) in _subcommands.OrderBy(x => x.Key))
            {
                var option = value switch
                {
                    Command command => new LocalSelectionComponentOption()
                        .WithLabel($"{command.FullAliases[0]} {HelpUtils.FormatParameters(command)}")
                        .WithValue(key)
                        .WithDescription(command.Description ?? "No Description."),
                    Module commandModule => new LocalSelectionComponentOption()
                        .WithLabel($"{commandModule.FullAliases[0]}*")
                        .WithValue(key)
                        .WithDescription(commandModule.Description ?? "No Description."),
                    _ => null
                };

                if (option is not null)
                    options.Add(option);
            }

            SubommandSelection = new SelectionViewComponent(OnSubcommandSelectionAsync)
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

    public ValueTask OnSubcommandSelectionAsync(SelectionEventArgs e)
    {
        if (!_subcommands.TryGetValue(e.SelectedOptions[0].Value, out object? subcommand))
        {
            return default;
        }

        Menu.View = subcommand switch
        {
            Module module => new ModuleView(module, this, StartView, TemplateMessage),
            Command command => new CommandView(command, this, StartView, TemplateMessage),
            _ => Menu.View
        };

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
                x.Embeds = new Optional<IEnumerable<LocalEmbed>>(TemplateMessage.Embeds);
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

        int commandsCount = _commands.Count;

        var commandsText = new StringBuilder();

        if (commandsCount != 0)
        {
            foreach (var command in _commands)
            {
                commandsText.AppendLine(HelpUtils.GetShortCommand(command));
            }

            embed.AddField("Commands", commandsText);
        }

        var subcommandText = new StringBuilder();
        bool containsGroupedCommands = false;

        foreach ((_, object? value) in _subcommands.OrderBy(x => x.Key))
        {
            switch (value)
            {
                case Command commandInfo:
                    subcommandText.AppendLine(HelpUtils.GetShortCommand(commandInfo));
                    break;
                case Module moduleInfo:
                    subcommandText.AppendLine(HelpUtils.GetShortModule(moduleInfo));
                    containsGroupedCommands = true;
                    break;
            }
        }

        if (subcommandText.Length != 0)
            embed.AddField(Module.Aliases.Count == 0 ? "Commands" : "Subcommands", subcommandText);

        if (containsGroupedCommands)
            embed.WithFooter("*Contains subcommands or overloads.");

        return embed;
    }
}