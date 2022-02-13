using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;

using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;

using Qmmands;

namespace Conbot.HelpPlugin;

public class ErrorView : ViewBase
{
    private readonly Command? _command;
    private readonly Module? _module;

    public ConbotCommandContext Context { get; }
    public IConfiguration Configuration { get; }

    public ButtonViewComponent HelpButton { get; }
    public ButtonViewComponent DismissButton { get; }

    public ErrorView(LocalMessage templateMessage, ConbotCommandContext context, IConfiguration configuration)
        : this(templateMessage, context, configuration, null, null)
    {
    }

    public ErrorView(LocalMessage templateMessage, ConbotCommandContext context, IConfiguration configuration,
        Command command)
        : this(templateMessage, context, configuration, command, null)
    {
    }

    public ErrorView(LocalMessage templateMessage, ConbotCommandContext context, IConfiguration configuration,
        Module module)
        : this(templateMessage, context, configuration, null, module)
    {
    }

    protected ErrorView(LocalMessage templateMessage, ConbotCommandContext context, IConfiguration configuration,
        Command? command, Module? module)
        : base(templateMessage)
    {
        Context = context;
        Configuration = configuration;

        _command = command;
        _module = module;

        HelpButton = new ButtonViewComponent(OnHelpButtonAsync)
        {
            Label = "Help",
            Style = LocalButtonComponentStyle.Primary
        };

        AddComponent(HelpButton);

        DismissButton = new ButtonViewComponent(OnDismissButtonAsync)
        {
            Label = "Dismiss",
            Style = LocalButtonComponentStyle.Secondary
        };

        AddComponent(DismissButton);
    }

    public ValueTask OnHelpButtonAsync(ButtonEventArgs e)
    {
        ViewBase view;

        if (_command is not null)
            view = new CommandView(_command, Context, Configuration);
        else if (_module is not null)
            view = new ModuleView(_module, Context, Configuration);
        else
            view = new StartView(Context, Configuration);

        HelpButton.IsDisabled = true;

        DefaultMenu menu = new(view) { AuthorId = Context.Author.Id };
        _ = Context.Bot.StartMenuAsync(Context.ChannelId, menu);

        return default;
    }

    public ValueTask OnDismissButtonAsync(ButtonEventArgs e)
    {
        _ = e.Interaction.Message.DeleteAsync();

        Menu.Stop();

        return default;
    }
}