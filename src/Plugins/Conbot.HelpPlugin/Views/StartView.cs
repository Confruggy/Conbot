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
    public class StartView : ViewBase
    {
        private readonly IEnumerable<Module> _modules;

        public ConbotCommandContext Context { get; }
        public IConfiguration Configuration { get; }

        public SelectionViewComponent ModuleSelection { get; }
        public ButtonViewComponent? StartButton { get; }
        public ButtonViewComponent? BackButton { get; }
        public ButtonViewComponent TryButton { get; }
        public ButtonViewComponent StopButton { get; }

        public StartView(ConbotCommandContext context, IConfiguration config, LocalMessage? templateMessage = null)
            : base(templateMessage)
        {
            Context = context;
            Configuration = config;

            _modules = GetModules();

            List<LocalSelectionComponentOption> options = new();

            foreach (var module in _modules)
            {
                var option = new LocalSelectionComponentOption()
                    .WithLabel(module.Name)
                    .WithValue(module.Name)
                    .WithDescription(module.Description ?? "No Description.");

                options.Add(option);
            }

            ModuleSelection = new SelectionViewComponent(OnModuleSelectionAsync)
            {
                MaximumSelectedOptions = 1,
                Options = options,
                Placeholder = "Select a category for more information"
            };

            AddComponent(ModuleSelection);

            StartButton = new ButtonViewComponent(null)
            {
                Label = "Start",
                Style = LocalButtonComponentStyle.Primary,
                IsDisabled = true,
            };

            AddComponent(StartButton);

            BackButton = new ButtonViewComponent(null)
            {
                Label = "Back",
                Style = LocalButtonComponentStyle.Primary,
                IsDisabled = true,
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

        public ValueTask OnModuleSelectionAsync(SelectionEventArgs e)
        {
            string? selection = e.SelectedOptions[0].Value;
            var module = _modules.First(x => x.Name == selection);
            Menu.View = new ModuleView(module, this, TemplateMessage);

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

        private IEnumerable<Module> GetModules()
            => Context.Bot.Commands
                .GetAllModules()
                .Where(x => x.Parent is null)
                .OrderBy(x => x.Name);

        public override LocalMessage ToLocalMessage()
        {
            var message = base.ToLocalMessage()
                .AddEmbed(CreateStartEmbed());

            if (message.Reference is null)
                message.WithReply(Context.Messages[^1].Id, Context.ChannelId, Context.GuildId);

            return message;
        }

        public LocalEmbed CreateStartEmbed()
        {
            var embed = new LocalEmbed()
                .WithAuthor(Context.Bot.CurrentUser.Name, Context.Bot.CurrentUser.GetAvatarUrl())
                .WithDescription(
                    "Below you see all available categories. " +
                    "Each category has one or several commands.")
                .WithColor(new Color(Configuration.GetValue<int>("DefaultEmbedColor")));

            var modulesText = new StringBuilder();

            foreach (var module in _modules)
                modulesText.AppendLine(HelpUtils.GetShortModule(module));

            embed.AddField("Categories", modulesText.ToString());

            string? botInviteUrl = Configuration.GetValue<string?>("BotInviteUrl", null);
            string? serverInviteUrl = Configuration.GetValue<string?>("ServerInviteUrl", null);

            string? botInviteText = !string.IsNullOrEmpty(botInviteUrl)
                ? Markdown.Link($"Invite {Context.Bot.CurrentUser.Name}", botInviteUrl)
                : null;
            string? serverInviteText = !string.IsNullOrEmpty(serverInviteUrl)
                ? Markdown.Link("Discord Server", serverInviteUrl)
                : null;

            string linksText = string.Join("｜", new[] { botInviteText, serverInviteText }.Where(x => x is not null));
            if (!string.IsNullOrEmpty(linksText))
                embed.AddField("Links", linksText);

            return embed;
        }
    }
}
