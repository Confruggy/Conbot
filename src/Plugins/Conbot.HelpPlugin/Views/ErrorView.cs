using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;

using Disqord;
using Disqord.Extensions.Interactivity.Menus;

using Qmmands;

namespace Conbot.HelpPlugin
{
    public class ErrorView : ViewBase
    {
        private readonly Command? _command;
        private readonly Module? _module;

        public ConbotCommandContext Context { get; }
        public IConfiguration Configuration { get; }

        public ButtonViewComponent HelpButton { get; }

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
        }

        public ValueTask OnHelpButtonAsync(ButtonEventArgs e)
        {
            if (_command is not null)
                Menu.View = new CommandView(_command, Context, Configuration, TemplateMessage);
            else if (_module is not null)
                Menu.View = new ModuleView(_module, Context, Configuration, TemplateMessage);
            else
                Menu.View = new StartView(Context, Configuration, TemplateMessage);

            return default;
        }
    }
}
